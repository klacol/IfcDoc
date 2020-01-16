// Name:        TurtleSerializer.cs
// Description: Reads/writes TTL File (RDF, compliant with ifcOWL).
// Author:      Pieter Pauwels
// Origination: Work performed for BuildingSmart by Pieter Pauwels
// Copyright:   (c) 2016 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

namespace BuildingSmart.Serialization.Turtle
{
	// TimC: refactored from IfcDoc code; not yet tested -- needs verification

	public class TurtleSerializer : Serializer
	{
		//dictionary with extra entities that need to be defined so that the RDF graphs is correct (e.g. Lists and labels and ...)
		Dictionary<string, ObjectProperty> m_fullpropertynames;

		public TurtleSerializer(Type type) : base(type)
		{
			string timeLog = DateTime.Now.ToString("yyyyMMdd_HHmmss"); //h:mm:ss tt

			// load properties
			this.m_fullpropertynames = new Dictionary<string, ObjectProperty>();
			foreach (Type t in this.GetTypes())
			{
				Type docClass = t;
				IEnumerable<PropertyInfo> listFields = GetFieldsOrdered(docClass).Select(x=>x.Value);
				foreach (PropertyInfo fieldInfo in listFields)
				{
					string row0 = docClass.Name;
					string row1 = fieldInfo.Name;
					string row2 = fieldInfo.Name + "_" + docClass.Name;
					string row3 = "ENTITY";

					Type typeField = fieldInfo.PropertyType;
					if (typeField.IsGenericType && typeField.GetGenericTypeDefinition() == typeof(ISet<>))
					{
						row3 = "SET";
					}
					else if (typeField.IsGenericType && typeField.GetGenericTypeDefinition() == typeof(IList<>))
					{
						row3 = "LIST";
					}

					this.m_fullpropertynames.Add(row1 + "_" + row0, new ObjectProperty(row0, row1, row2, row3));

				}
			}
		}

		private class ListObject
		{
			public ListObject() { }
			public ListObject(string URI, string listtype, string ifcowlclass, List<string> values, string XSDType)
			{
				this.URI = URI;
				this.ifcowlclass = ifcowlclass;
				this.values = values;
				this.XSDType = XSDType;
				this.listtype = listtype;
			}
			public string URI;
			public string listtype;
			public string ifcowlclass;
			public List<string> values;
			public string XSDType;
		}

		private class URIObject
		{
			public URIObject(string URI, string ifcowlclass, string encodedvalue, string XSDtype)
			{
				this.URI = URI;
				this.ifcowlclass = ifcowlclass;
				this.encodedvalue = encodedvalue;
				this.XSDType = XSDtype;
			}
			public string URI;
			public string ifcowlclass;
			public string encodedvalue;
			public string XSDType;
		}

		private class ObjectProperty
		{
			public ObjectProperty(string domain, string originalName, string name, string setorlist)
			{
				this.domain = domain;
				this.originalName = originalName;
				this.name = char.ToLower(name[0]) + name;
				this.setorlist = setorlist;
			}
			public string domain;
			public string originalName;
			public string name;
			public string setorlist; //ENTITY, SET, LIST, LISTOFLIST, ARRAY || ACTUALLY, ONLY ENTITY, SET, AND LIST are available here (see FormatTTL.FormatData())
		}

		public string OwlURI
		{
			get
			{
				return this.BaseURI;
			}
		}

		public override object ReadObject(Stream stream)
		{
			throw new NotImplementedException();
		}

		//WRITING ENTITIES
		public override void WriteObject(Stream stream, object root)
		{
			StreamWriter writer = new StreamWriter(stream);
			int nextID = 1;

			//header
			int indent = 0;
			WriteHeader(writer, ref indent);

			Dictionary<string, URIObject> valueObjects = new Dictionary<string, URIObject>();
			Dictionary<string, ListObject> listObjects = new Dictionary<string, ListObject>();
			Dictionary<string, ListObject> listOfListObjects = new Dictionary<string, ListObject>();
			Dictionary<string, URIObject> objectProperties = new Dictionary<string, URIObject>();

			Dictionary<object, long> idmap = new Dictionary<object, long>();
			Queue<object> queue = new Queue<object>();
			queue.Enqueue(root);
			while (queue.Count > 0)
			{
				object ent = queue.Dequeue();

				//contents: entities and properties                    
				this.WriteEntity(writer, ref indent, ent,
					valueObjects, listObjects, listOfListObjects,
					queue, idmap, ref nextID);
			}

			writer.Flush();

			//write additional entities
			Console.Out.WriteLine("\r\n+ start writing additional entities +");
			foreach (KeyValuePair<string, URIObject> entry in valueObjects)
			{
				URIObject obj = entry.Value;
				this.WriteExtraEntity(writer, ref indent, obj);
			}
			writer.Flush();
			Console.Out.WriteLine("+ end writing additional entities +");

			Console.Out.WriteLine("\r\n+ start writing additional list objects +");
			foreach (KeyValuePair<string, ListObject> entry in listObjects)
			{
				ListObject obj = entry.Value;
				this.WriteExtraListObject(writer, ref indent, obj, ref nextID);
			}
			writer.Flush();
			Console.Out.WriteLine("+ end writing additional list objects +");

			Console.Out.WriteLine("\r\n+ start writing additional list of list objects +");
			foreach (KeyValuePair<string, ListObject> entry in listOfListObjects)
			{
				ListObject obj = entry.Value;
				this.WriteExtraListOfListObject(writer, ref indent, obj, ref nextID);
			}
			writer.Flush();
			Console.Out.WriteLine("+ end writing additional list of list objects +");

			writer.Write("\r\n\r\n");
			writer.Flush();
		}

		// TimC: there's no longer an explicit SEntity base class for entity-based types, so this replacement function used for code transition
		private bool IsEntity(object o)
		{
			Type t = o.GetType();
			if (t.IsValueType || o is string)
				return false;

			if (typeof(System.Collections.IEnumerable).IsInstanceOfType(o))
				return false;

			return true;
		}

		private long RegisterID(object o, Queue<object> queue, Dictionary<object, long> idmap, ref int nextID)
		{
			long ID = 0L;

			if (!idmap.TryGetValue(o, out ID))
			{
				nextID++;
				ID = nextID;
				idmap.Add(o, nextID);

				// then it may also not yet be serialized
				queue.Enqueue(o);
			}

			return ID;
		}

		private void WriteEntity(StreamWriter writer, ref int indent, object o,
			Dictionary<string, URIObject> valueObjects, Dictionary<string, ListObject> listObjects, Dictionary<string, ListObject> listOfListObjects,
			Queue<object> queue, Dictionary<object, long> idmap, ref int nextID)
		{
			string newline = "\r\n";

			// give object id
			long ID = RegisterID(o, queue, idmap, ref nextID);

			Type t = o.GetType();
			this.WriteStartElement(writer, indent, t.Name + "_" + ID);

			writer.Write(newline);
			indent++;
			Console.Out.WriteLine("\r\n--- Writing entity : " + t.Name.ToString());
			Console.Out.WriteLine("-----------------------------------");
			this.WriteType(writer, indent, "ifcowl:" + t.Name.ToString());
			this.WriteEntityAttributes(writer, indent, o, valueObjects, listObjects, listOfListObjects, queue, idmap, ref nextID);
			Console.Out.WriteLine("--------------done---------------------");
		}

		/// <summary>
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		private void WriteEntityAttributes(StreamWriter writer, int indent, object o,
			Dictionary<string, URIObject> valueObjects, Dictionary<string, ListObject> listObjects, Dictionary<string, ListObject> listOfListObjects,
			Queue<object> queue, Dictionary<object, long> idmap, ref int nextID)
		{
			Type t = o.GetType();

			IEnumerable<PropertyInfo> fields = this.GetFieldsOrdered(t).Select(x=>x.Value);
			foreach (PropertyInfo f in fields)
			{
				object v = f.GetValue(o);

				//don't spend time in empty attributes
				if (v == null)
					continue;

				// write data type properties
				Type ft = f.PropertyType;

				bool isvaluelist = (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>) && ft.GetGenericArguments()[0].IsValueType);
				bool isvaluelistlist = (ft.IsGenericType && // e.g. IfcTriangulatedFaceSet.Normals
					ft.GetGenericTypeDefinition() == typeof(List<>) &&
					ft.GetGenericArguments()[0].IsGenericType &&
					ft.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(List<>) &&
					ft.GetGenericArguments()[0].GetGenericArguments()[0].IsValueType);
				bool isentitylist = (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>));
				bool isentitylistlist = (ft.IsGenericType &&
					ft.GetGenericTypeDefinition() == typeof(List<>) &&
					ft.GetGenericArguments()[0].IsGenericType &&
					ft.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(List<>));


				if (isvaluelistlist || isvaluelist || ft.IsValueType)
				{
					if (isvaluelistlist)
					{
						System.Collections.IList list = (System.Collections.IList)v;
						WriteListOfListWithValues(writer, indent, t, f, list, valueObjects, listObjects, listOfListObjects, ref nextID);
					}
					else if (isvaluelist)
					{
						System.Collections.IList list = (System.Collections.IList)v;
						WriteListWithValues(writer, indent, t, f, list, valueObjects, listObjects, ref nextID);
					}
					else
					{
						WriteTypeValue(writer, indent, t, f, v, valueObjects, listObjects, ref nextID);
					}
				}
				else if (isentitylist || isentitylistlist)
				{
					ObjectProperty p = GetObjectProperty(f.Name + "_" + o.GetType().Name);
					if (isentitylistlist)
					{
						System.Collections.IList list = (System.Collections.IList)v;
						WriteListOfListWithEntities(writer, indent, t, f, list, valueObjects, listObjects, listOfListObjects, queue, idmap, ref nextID);
					}
					else if (isentitylist)
					{
						System.Collections.IList list = (System.Collections.IList)v;
						WriteListWithEntities(writer, indent, t, f, list, valueObjects, listObjects, queue, idmap, ref nextID);
					}
				}
				else
				{
					//non-list attributes
					ObjectProperty p = GetObjectProperty(f.Name + "_" + o.GetType().Name);
					// TimC: review this: p.name is null
					if (p != null)
					{
						WriteAnyOtherThing(writer, indent, f, p, v, valueObjects, queue, idmap, ref nextID);
					}
				}
			}

			writer.Write(".\r\n\r\n");
			//Console.Out.WriteLine("End of attributes");

			return;
		}

		private void WriteAnyOtherThing(StreamWriter writer, int indent, PropertyInfo f, ObjectProperty p, object v,
			Dictionary<string, URIObject> valueObjects,
			Queue<object> queue, Dictionary<object, long> idmap, ref int nextID)
		{
			if (p == null) // TimC: review this
				return;

			if (f.PropertyType.IsInterface && v is ValueType)
			{
				writer.Write(";" + "\r\n");
				WriteIndent(writer, indent);
				writer.Write("ifcowl:" + p.name + " ");

				Type vt = v.GetType();
				URIObject newUriObject = GetURIObject(vt.Name, vt.GetField("Value").GetValue(v).ToString(), vt.GetField("Value").FieldType.Name, valueObjects, ref nextID);
				writer.Write("inst:" + newUriObject.URI);
				//this.WriteValueWrapper(v);
			}
			else if (f.PropertyType.IsValueType) // must be IfcBinary
			{
				Console.Out.WriteLine("MESSAGE-CHECK: Write IfcBinary 1");
				writer.Write(";" + "\r\n");
				WriteIndent(writer, indent);
				writer.Write("ifcowl:" + p.name + " ");

				PropertyInfo fieldValue = f.PropertyType.GetProperty("Value");
				if (fieldValue != null)
				{
					v = fieldValue.GetValue(v);
					if (v is byte[])
					{
						// binary data type - we don't support anything other than 8-bit aligned, though IFC doesn't either so no point in supporting extraBits
						byte[] bytes = (byte[])v;

						StringBuilder sb = new StringBuilder(bytes.Length * 2);
						for (int i = 0; i < bytes.Length; i++)
						{
							byte b = bytes[i];
							sb.Append(Serializer.HexChars[b / 0x10]);
							sb.Append(Serializer.HexChars[b % 0x10]);
						}
						v = sb.ToString();
						writer.WriteLine("\"" + v + "\"");
					}
				}
			}
			else
			{
				//found simple object property. give propertyname and URI num
				writer.Write(";" + "\r\n");
				WriteIndent(writer, indent);

				writer.Write("ifcowl:" + p.name + " ");

				Type vt = v.GetType();
				long ID = RegisterID(v, queue, idmap, ref nextID);
				writer.Write("inst:" + vt.Name + "_" + ID);
				//There is more in the WriteAttibute method (HTM notation)
			}
		}

		private void WriteListOfListWithEntities(StreamWriter writer, int indent, Type t, PropertyInfo f, System.Collections.IList list,
			Dictionary<string, URIObject> valueObjects, Dictionary<string, ListObject> listObjects, Dictionary<string, ListObject> listOfListObjects,
			Queue<object> queue, Dictionary<object, long> idmap, ref int nextID)
		{
			//example:
			//owl: allValuesFrom expr:INTEGER_List_List;
			//owl: onProperty ifc:coordIndex_IfcTriangulatedFaceSet

			//Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
			ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);
			if (p.setorlist == "SET")
			{
				//SET
				Console.Out.WriteLine("\r\nWARNING: UNHANDLED SET ATTRIBUTE" + f.Name);
			}
			else
			{

				Type ft = f.PropertyType;
				ft = ft.GetGenericArguments()[0].GetGenericArguments()[0];

				writer.Write(";" + "\r\n");
				this.WriteIndent(writer, indent);
				writer.Write("ifcowl:" + p.name + " ");

				List<string> listoflists = new List<string>();
				ListObject newListObject;

				for (int i = 0; i < list.Count; i++)
				{
					//new List of List
					List<string> valuelist = new List<string>();
					System.Collections.IList listInner = (System.Collections.IList)list[i];
					for (int j = 0; j < listInner.Count; j++)
					{
						object e = listInner[j];
						if (e != null) // should never be null, but be safe
						{
							if (IsEntity(e))
							{
								Type vt = e.GetType();
								long ID = RegisterID(e, queue, idmap, ref nextID);
								valuelist.Add(vt.Name + "_" + ID);
							}
							else
							{
								Console.Out.WriteLine("WARNING - unhandled list of list type: " + e.ToString());
							}
						}
					}

					//create listObject
					//Console.Out.WriteLine("Message: Creating WriteListKindOfObjectProperty list with XSDType : " + "###ENTITY###");
					newListObject = GetListObject(ft.Name + "_List_", ft.Name, valuelist, "###ENTITY###", valueObjects, listObjects, ref nextID);

					//add to listOfList
					listoflists.Add(newListObject.URI);
				}

				ListObject newListOfListObject = GetListOfListObject(ft.Name, listoflists, "###LISTOFLIST###", listOfListObjects, ref nextID);
				writer.Write("inst:" + newListOfListObject.URI);
			}
		}

		private void WriteListOfListWithValues(StreamWriter writer, int indent, Type t, PropertyInfo f, System.Collections.IList list,
			Dictionary<string, URIObject> valueObjects, Dictionary<string, ListObject> listObjects, Dictionary<string, ListObject> listOfListObjects, ref int nextID)
		{
			//Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
			ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);
			if (p.setorlist == "SET")
			{
				//SET
				Console.Out.WriteLine("\r\nWARNING: UNHANDLED SET ATTRIBUTE" + f.Name);
			}
			else
			{
				//LIST
				Type ft = f.PropertyType;
				writer.Write(";" + "\r\n");
				this.WriteIndent(writer, indent);
				writer.Write("ifcowl:" + p.name + " ");

				//Console.Out.WriteLine("WriteListOfListWithValues()");
				//example:
				//owl: allValuesFrom expr:INTEGER_List_List;
				//owl: onProperty ifc:coordIndex_IfcTriangulatedFaceSet
				ft = ft.GetGenericArguments()[0].GetGenericArguments()[0];
				PropertyInfo fieldValue = ft.GetProperty("Value");

				List<string> listoflists = new List<string>();
				ListObject newListObject;

				for (int i = 0; i < list.Count; i++)
				{
					//new List of List
					List<string> valuelist = new List<string>();
					System.Collections.IList listInner = (System.Collections.IList)list[i];
					for (int j = 0; j < listInner.Count; j++)
					{
						object elem = listInner[j];
						if (elem != null) // should never be null, but be safe
						{
							elem = fieldValue.GetValue(elem);
							string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
							valuelist.Add(encodedvalue);
						}
					}

					//create listObject
#if VERBOSE
                    Console.Out.WriteLine("Message: Creating ListOfListWithValues with XSDType : " + fieldValue.FieldType.Name);
#endif
					newListObject = GetListObject(ft.Name + "_List_", ft.Name, valuelist, fieldValue.PropertyType.Name, valueObjects, listObjects, ref nextID);

					//add to listOfList
					listoflists.Add(newListObject.URI);
				}

				ListObject newListOfListObject = GetListOfListObject(ft.Name, listoflists, "###LISTOFLIST###", listOfListObjects, ref nextID);
				writer.Write("inst:" + newListOfListObject.URI);
				//Console.Out.WriteLine("written list of list prop (as obj. prop): attr = " + "inst:" + newListOfListObject.URI);
			}
		}

		private void WriteListWithEntities(StreamWriter writer, int indent, Type t, PropertyInfo f, System.Collections.IList list,
			Dictionary<string, URIObject> valueObjects, Dictionary<string, ListObject> listObjects,
			Queue<object> queue, Dictionary<object, long> idmap, ref int nextID)
		{
			//Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
			ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);

			if (p.setorlist == "SET")
			{
				//SET
				for (int i = 0; i < list.Count; i++)
				{
					if (IsEntity(list[i]))
					{
						Type vt = list[i].GetType();
						writer.Write(";" + "\r\n");
						this.WriteIndent(writer, indent);
						writer.Write("ifcowl:" + p.name + " ");
						long ID = RegisterID(list[i], queue, idmap, ref nextID);
						writer.Write("inst:" + vt.Name + "_" + ID);
						//Console.Out.WriteLine("written object prop to SET:" + p.name + " - " + vt.Name + "_" + ((SEntity)list[i]).OID);
					}
					else
					{
						Console.Out.WriteLine("WARNING: We found an unhandled SET of things that are NOT entities: " + f.Name);
					}
				}
			}
			else
			{
				//LIST
				Type ft = f.PropertyType;
				ft = ft.GetGenericArguments()[0];
				List<string> valuelist = new List<string>();
				string ifcowlclass = "";

				object e = list[0];
				Type vt = e.GetType();
				if (IsEntity(e))
				{
					for (int i = 0; i < list.Count; i++)
					{
						e = list[i];
						ifcowlclass = vt.Name;
						long ID = RegisterID(e, queue, idmap, ref nextID);
						valuelist.Add(vt.Name + "_" + ID);
					}

					writer.Write(";" + "\r\n");
					this.WriteIndent(writer, indent);
					writer.Write("ifcowl:" + p.name + " ");
					ListObject newListObject = GetListObject(ifcowlclass + "_List_", ifcowlclass, valuelist, "###ENTITY###", valueObjects, listObjects, ref nextID);
					writer.Write("inst:" + newListObject.URI);
					//IfcRepresentation_List_#121_#145_#137
				}
				else
				{
					//find out whether we have a list of lists; or a list of values
					if (vt.IsValueType && !vt.IsPrimitive)
					{
						PropertyInfo fieldValue = vt.GetProperty("Value");
						if (fieldValue != null)
						{
							e = fieldValue.GetValue(e);
						}
					}

					if (e is System.Collections.IList)
					{
						//e.g. #205= IFCINDEXEDPOLYCURVE($,(IFCLINEINDEX((1,2)),IFCARCINDEX((2,3,4))),$);
						Type typewrap = null;
						for (int i = 0; i < list.Count; i++)
						{
							e = list[i];
							vt = e.GetType();
							while (vt.IsValueType && !vt.IsPrimitive)
							{
								PropertyInfo fieldValue1 = vt.GetProperty("Value");
								if (fieldValue1 != null)
								{
									e = fieldValue1.GetValue(e);
									typewrap = vt;
									vt = fieldValue1.PropertyType;
								}
								else
								{
									break;
								}
							}

							// e.g. IfcBinary
							// e.g. IfcCompoundPlaneAngleMeasure (LIST)
							System.Collections.IList innerlist1 = (System.Collections.IList)e;
							vt = vt.GetGenericArguments()[0];
							PropertyInfo fieldValue = vt.GetProperty("Value");

							if (typewrap.Name != "IfcBinary")
							{
								List<String> innerlist2 = new List<String>();
								for (int j = 0; j < innerlist1.Count; j++)
								{
									object elem = innerlist1[j];
									if (elem != null) // should never be null, but be safe
									{
										elem = fieldValue.GetValue(elem);
										string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
										innerlist2.Add(encodedvalue);
									}
								}
								string s = fieldValue.PropertyType.Name;
								if (s == "Int64" || s == "Double" || s == "String" || s == "Number" || s == "Real" || s == "Integer" || s == "Logical" || s == "Boolean" || s == "Binary")
									s = CheckForExpressPrimaryTypes(s);
								ListObject newInnerListObject = GetListObject(typewrap.Name + "_", vt.Name, innerlist2, s, valueObjects, listObjects, ref nextID);
								valuelist.Add(newInnerListObject.URI);
							}
							//else
							//{
							//    string fullvalue = "";
							//    for (int i = 0; i < list.Count; i++)
							//    {
							//        object elem = list[i];
							//        if (elem != null) // should never be null, but be safe
							//        {
							//            string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
							//            fullvalue += Int32.Parse(encodedvalue).ToString("X");
							//        }
							//    }
							//    Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
							//    this.m_writer.Write(";" + "\r\n");
							//    this.WriteIndent();
							//    ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);
							//    this.m_writer.Write("ifcowl:" + p.name + " ");
							//    this.m_writer.Write(fullvalue);
							//}
						}
						writer.Write(";" + "\r\n");
						this.WriteIndent(writer, indent);
						writer.Write("ifcowl:" + p.name + " ");
						ListObject newListObject = GetListObject(ft.Name + "_List_", ft.Name, valuelist, "###ENTITY###", valueObjects, listObjects, ref nextID);
						writer.Write("inst:" + newListObject.URI);
						//IfcRepresentation_List_#121_#145_#137
					}
					else
					{
						//e.g. #226= IFCPROPERTYENUMERATION($,(IFCLABEL('NEW'),IFCLABEL('EXISTING'),IFCLABEL('DEMOLISH'),IFCLABEL('TEMPORARY'),IFCLABEL('OTHER'),IFCLABEL('NOTKNOWN'),IFCLABEL('UNSET')),$);
						Type typewrap = null;
						for (int i = 0; i < list.Count; i++)
						{
							e = list[i];
							vt = e.GetType();
							while (vt.IsValueType && !vt.IsPrimitive)
							{
								PropertyInfo fieldValue = vt.GetProperty("Value");
								if (fieldValue != null)
								{
									e = fieldValue.GetValue(e);
									if (typewrap == null)
									{
										typewrap = vt;
									}
									vt = fieldValue.PropertyType;
								}
								else
								{
									break;
								}
							}
							if (e != null)
							{
								string encodedvalue = System.Security.SecurityElement.Escape(e.ToString());
								valuelist.Add(encodedvalue);
							}
						}

						writer.Write(";" + "\r\n");
						this.WriteIndent(writer, indent);
						writer.Write("ifcowl:" + p.name + " ");
						ListObject newListObject = GetListObject(ft.Name + "_List_", typewrap.Name, valuelist, vt.Name, valueObjects, listObjects, ref nextID);
						writer.Write("inst:" + newListObject.URI);
						//IfcRepresentation_List_#121_#145_#137
					}
				}
			}
		}

		private void WriteListWithValues(StreamWriter writer, int indent, Type t, PropertyInfo f, System.Collections.IList list,
			Dictionary<string, URIObject> valueObjects, Dictionary<string, ListObject> listObjects, ref int nextID)
		{
			//Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
			ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);

			if (p.setorlist == "SET")
			{
				//SET, such as IfcRecurrencePattern.weekdayComponent > IfcDayInWeekNumber
				for (int i = 0; i < list.Count; i++)
					WriteTypeValue(writer, indent, t, f, list[i], valueObjects, listObjects, ref nextID);
			}
			else
			{
				//LIST, the most common option
				Type ft = f.PropertyType;
				ft = ft.GetGenericArguments()[0];
				List<string> valuelist = new List<string>();
				PropertyInfo fieldValue = ft.GetProperty("Value");

				//Simply retrieving the values and putting them in valuelist
				for (int i = 0; i < list.Count; i++)
				{
					object elem = list[i];
					if (elem != null) // should never be null, but be safe
					{
						elem = fieldValue.GetValue(elem);
						if (elem is byte[])
						{
							// IfcPixelTexture.Pixels
							if (i == 0)
								Console.Out.WriteLine("MESSAGE-CHECK: Write IfcBinary 2");
							byte[] bytes = (byte[])elem;

							char[] s_hexchar = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
							StringBuilder sb = new StringBuilder(bytes.Length * 2);
							for (int z = 0; z < bytes.Length; z++)
							{
								byte b = bytes[z];
								sb.Append(s_hexchar[b / 0x10]);
								sb.Append(s_hexchar[b % 0x10]);
							}
							valuelist.Add(sb.ToString());
						}
						else
						{
							//Simple list: like an IfcCartesianPoint
							string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
							valuelist.Add(encodedvalue);
						}
					}
				}

				writer.Write(";" + "\r\n");
				this.WriteIndent(writer, indent);
				writer.Write("ifcowl:" + p.name + " ");
				string s = fieldValue.PropertyType.Name;
				if (s == "Int64" || s == "Double" || s == "String" || s == "Number" || s == "Real" || s == "Integer" || s == "Logical" || s == "Boolean" || s == "Binary" || s == "Byte[]")
					s = CheckForExpressPrimaryTypes(s);
				ListObject newListObject = GetListObject(ft.Name + "_List_", ft.Name, valuelist, s, valueObjects, listObjects, ref nextID);
				writer.Write("inst:" + newListObject.URI);
			}
		}

		private void WriteTypeValue(StreamWriter writer, int indent, Type t, PropertyInfo f, object v,
			Dictionary<string, URIObject> valueObjects, Dictionary<string, ListObject> listObjects, ref int nextID)
		{
			Type ft = f.PropertyType;
			if (ft.IsGenericType && (ft.GetGenericTypeDefinition() == typeof(Nullable<>) || ft.GetGenericTypeDefinition() == typeof(List<>)))
			{
				// special case for Nullable types
				ft = ft.GetGenericArguments()[0];
			}
			string owlClass = ft.Name;

			Type typewrap = null;
			while (ft.IsValueType && !ft.IsPrimitive)
			{
				PropertyInfo fieldValue = ft.GetProperty("Value");
				if (fieldValue != null)
				{
					v = fieldValue.GetValue(v);
					if (typewrap == null)
					{
						typewrap = ft;
					}
					ft = fieldValue.PropertyType;
				}
				else
				{
					break;
				}
			}

			if (ft.IsEnum)
			{
				//Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
				writer.Write(";" + "\r\n");
				this.WriteIndent(writer, indent);
				ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);
				writer.Write("ifcowl:" + p.name + " ");
				//write enumproperty
				writer.Write("ifcowl:" + v);
			}
			if (ft == typeof(bool))
			{
				v = v.ToString().ToLowerInvariant();
			}

			if (v is System.Collections.IList)
			{
				// e.g. IfcBinary
				// e.g. IfcCompoundPlaneAngleMeasure (LIST)
				//Console.Out.WriteLine("WARNING-TOCHECK: Write IfcBinary 3");
				System.Collections.IList list = (System.Collections.IList)v;

				if (owlClass != "IfcBinary")
				{
					List<String> valuelist = new List<String>();
					for (int i = 0; i < list.Count; i++)
					{
						object elem = list[i];
						if (elem != null) // should never be null, but be safe
						{
							string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
							valuelist.Add(encodedvalue);
						}
					}
					ft = ft.GetGenericArguments()[0];
					string s = ft.Name;
					if (s == "Int64" || s == "Double" || s == "String" || s == "Number" || s == "Real" || s == "Integer" || s == "Logical" || s == "Boolean" || s == "Binary")
						s = CheckForExpressPrimaryTypes(s);
					Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
					writer.Write(";" + "\r\n");
					this.WriteIndent(writer, indent);
					ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);
					writer.Write("ifcowl:" + p.name + " ");
					ListObject newListObject = GetListObject(owlClass + "_", s, valuelist, ft.Name, valueObjects, listObjects, ref nextID);
					writer.Write("inst:" + newListObject.URI);
				}
				else
				{
					string fullvalue = "";
					for (int i = 0; i < list.Count; i++)
					{
						object elem = list[i];
						if (elem != null) // should never be null, but be safe
						{
							string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
							fullvalue += Int32.Parse(encodedvalue).ToString("X");
						}
					}
					Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
					writer.Write(";" + "\r\n");
					this.WriteIndent(writer, indent);
					ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);
					writer.Write("ifcowl:" + p.name + " ");

					string s = ft.Name;
					if (s == "Int64" || s == "Double" || s == "String" || s == "Number" || s == "Real" || s == "Integer" || s == "Logical" || s == "Boolean" || s == "Binary" || s == "Byte[]")
						s = CheckForExpressPrimaryTypes(s);
					URIObject uo = GetURIObject(owlClass, fullvalue, s, valueObjects, ref nextID);
					writer.Write("inst:" + uo.URI);
				}
			}
			else if (v != null && !ft.IsEnum)
			{
				string encodedvalue = System.Security.SecurityElement.Escape(v.ToString());
				encodedvalue = encodedvalue.Replace("\n", "\\n");
				if (owlClass == "Int64" || owlClass == "Double" || owlClass == "String" || owlClass == "Number" || owlClass == "Real" || owlClass == "Integer" || owlClass == "Logical" || owlClass == "Boolean" || owlClass == "Binary")
					owlClass = CheckForExpressPrimaryTypes(owlClass);

				Console.Out.WriteLine("WriteTypeValue.GetURIObject()");
				URIObject newUriObject = GetURIObject(owlClass, encodedvalue, ft.Name, valueObjects, ref nextID);
				Console.Out.WriteLine("\r\n++ writing attribute: " + f.Name);
				writer.Write(";" + "\r\n");
				this.WriteIndent(writer, indent);
				ObjectProperty p = GetObjectProperty(f.Name + "_" + t.Name);
				if (p != null) // TimC: review this -- is null for csg-primitive example - GlobalId_IfcProject
				{
					writer.Write("ifcowl:" + p.name + " ");
					writer.Write("inst:" + newUriObject.URI);
				}
			}
		}

		private void WriteHeader(StreamWriter writer, ref int indent)
		{
			string newline = "\r\n";

			string header = "# baseURI: " + this.BaseURI + newline;
			header += "# imports: " + this.OwlURI + newline + newline;

			string schema = "@prefix ifcowl: <" + this.OwlURI + "#> ." + newline;
			schema += "@prefix inst: <" + this.BaseURI + "> ." + newline;
			schema += "@prefix list: <" + "https://w3id.org/list#" + "> ." + newline;
			schema += "@prefix express: <" + "https://w3id.org/express#" + "> ." + newline;
			schema += "@prefix rdf: <" + "http://www.w3.org/1999/02/22-rdf-syntax-ns#" + "> ." + newline;
			schema += "@prefix xsd: <" + "http://www.w3.org/2001/XMLSchema#" + "> ." + newline;
			schema += "@prefix owl: <" + "http://www.w3.org/2002/07/owl#" + "> ." + newline + newline;

			writer.Write(header);
			writer.Write(schema);

			writer.Write("inst:" + newline);
			indent++;
			this.WriteIndent(writer, indent);
			writer.Write("rdf:type owl:Ontology;" + newline);
			this.WriteIndent(writer, indent);
			writer.Write("owl:imports <" + this.OwlURI + "> ." + newline + newline);
		}

		private void WriteStartElement(StreamWriter writer, int indent, string name)
		{
			this.WriteIndent(writer, indent);
			writer.Write("inst:" + name);
		}

		private void WriteType(StreamWriter writer, int indent, string type)
		{
			this.WriteIndent(writer, indent);
			writer.Write("rdf:type " + type);
		}

		private string CheckForExpressPrimaryTypes(string owlClass)
		{
			if (owlClass.Equals("integer", StringComparison.CurrentCultureIgnoreCase) || owlClass.Equals("Int64", StringComparison.CurrentCultureIgnoreCase))
				return "INTEGER";
			else if (owlClass.Equals("real", StringComparison.CurrentCultureIgnoreCase))
				return "REAL";
			else if (owlClass.Equals("double", StringComparison.CurrentCultureIgnoreCase))
				return "DOUBLE";
			else if (owlClass.Equals("Byte[]", StringComparison.CurrentCultureIgnoreCase))
				return "HexBinary";
			else if (owlClass.Equals("Binary", StringComparison.CurrentCultureIgnoreCase))
				return "HexBinary";
			else if (owlClass.Equals("boolean", StringComparison.CurrentCultureIgnoreCase))
				return "BOOLEAN";
			else if (owlClass.Equals("logical", StringComparison.CurrentCultureIgnoreCase))
				return "LOGICAL";
			else if (owlClass.Equals("string", StringComparison.CurrentCultureIgnoreCase))
				return "STRING";
			else
			{
				Console.Out.WriteLine("WARNING: found xsdType " + owlClass + " - not sure what to do with it");
			}

			return "";
		}

		private void WriteLiteralValue(StreamWriter writer, int indent, string xsdType, string literalString)
		{
#if VERBOSE
            Console.Out.WriteLine("WriteLiteralValue: " + "xsdtype: " + xsdType + " - literalString " + literalString);
#endif
			this.WriteIndent(writer, indent);
			if (xsdType.Equals("integer", StringComparison.CurrentCultureIgnoreCase) || xsdType.Equals("Int64", StringComparison.CurrentCultureIgnoreCase))
				writer.Write("express:hasInteger" + " " + literalString + " ");
			else if (xsdType.Equals("double", StringComparison.CurrentCultureIgnoreCase))
				writer.Write("express:has" + xsdType + " \"" + literalString.Replace(',', '.') + "\"^^xsd:double ");
			else if (xsdType.Equals("hexBinary", StringComparison.CurrentCultureIgnoreCase))
				writer.Write("express:has" + xsdType + " \"" + literalString + "\"^^xsd:hexBinary ");
			else if (xsdType.Equals("boolean", StringComparison.CurrentCultureIgnoreCase))
				writer.Write("express:has" + xsdType + " " + literalString.ToLower() + " ");
			else if (xsdType.Equals("logical", StringComparison.CurrentCultureIgnoreCase))
			{
				if (literalString.Equals(".F.", StringComparison.CurrentCultureIgnoreCase))
					writer.Write("express:has" + xsdType + " express:FALSE ");
				else if (literalString.Equals(".T.", StringComparison.CurrentCultureIgnoreCase))
					writer.Write("express:has" + xsdType + " express:TRUE ");
				else if (literalString.Equals(".U.", StringComparison.CurrentCultureIgnoreCase))
					writer.Write("express:has" + xsdType + " express:UNKNOWN ");
				else
					Console.Out.WriteLine("WARNING: found odd logical value: " + literalString);
			}
			else if (xsdType.Equals("string", StringComparison.CurrentCultureIgnoreCase))
				writer.Write("express:has" + xsdType + " \"" + literalString + "\" ");
			else
			{
				writer.Write("express:has" + xsdType + " \"" + literalString + "\" ");
				Console.Out.WriteLine("WARNING: found xsdType " + xsdType + " - not sure what to do with it");
			}
		}

		//WRITE EXTRAS
		private void WriteExtraEntity(StreamWriter writer, ref int indent, URIObject obj)
		{
			indent = 0;
			writer.Write("inst:" + obj.URI + "\r\n");
			indent++;
			string ns = "ifcowl:";
			if (obj.ifcowlclass.Equals("INTEGER") || obj.ifcowlclass.Equals("REAL") || obj.ifcowlclass.Equals("DOUBLE") || obj.ifcowlclass.Equals("BINARY") || obj.ifcowlclass.Equals("BOOLEAN") || obj.ifcowlclass.Equals("LOGICAL") || obj.ifcowlclass.Equals("STRING"))
				ns = "express:";
			WriteType(writer, indent, ns + obj.ifcowlclass + ";\r\n");
#if VERBOSE
            Console.Out.WriteLine("WriteExtraEntity: " + "obj.URI: " + obj.URI + " - xsdtype " + obj.XSDType);
#endif
			WriteLiteralValue(writer, indent, obj.XSDType, obj.encodedvalue);
			writer.Write(".\r\n\r\n");
#if VERBOSE
            Console.Out.WriteLine("written URIObject: " + "inst:" + obj.URI + " with VALUE " + obj.encodedvalue + " and TYPE " + obj.XSDType);
#endif
			return;
		}

		private void WriteExtraListOfListObject(StreamWriter writer, ref int indent, ListObject obj, ref int nextID)
		{
			if (obj.XSDType == "###LISTOFLIST###")
			{
				string ns = "ifcowl:";
				if (obj.ifcowlclass.Equals("INTEGER") || obj.ifcowlclass.Equals("REAL") || obj.ifcowlclass.Equals("DOUBLE") || obj.ifcowlclass.Equals("BINARY") || obj.ifcowlclass.Equals("BOOLEAN") || obj.ifcowlclass.Equals("LOGICAL") || obj.ifcowlclass.Equals("STRING"))
					ns = "express:";

				//int c = obj.values.Count;                
				for (int i = 0; i < obj.values.Count; i++)
				{
					indent = 0;
					if (i == 0)
					{
						writer.Write("inst:" + obj.URI + "\r\n");
#if VERBOSE
                        Console.Out.WriteLine("written ListOfListObject: " + "inst:" + obj.URI + " with TYPE " + obj.XSDType);
#endif
					}
					else
					{
						writer.Write("inst:" + obj.ifcowlclass + "_List_List_" + nextID + "\r\n");
#if VERBOSE
                        Console.Out.WriteLine("written ListOfListObject: " + "inst:" + obj.ifcowlclass + "_List_List_" + m_nextID + " with TYPE " + obj.XSDType);
#endif
					}

					//m_writer.Write("inst:" + obj.URI + "\r\n");
					indent++;
					WriteType(writer, indent, ns + obj.ifcowlclass + "_List_List" + ";\r\n");
					this.WriteIndent(writer, indent);
					nextID++;
					writer.Write("list:hasContents inst:" + obj.values[i]);

					//generate
					//List<string> values_double = obj.values;
					//values_double.RemoveAt(0);
					//ListObject x = new ListObject(obj.ifcowlclass + "_List_List_" + m_nextID, obj.ifcowlclass, values_double, "###LISTOFLIST###");

					if ((obj.values.Count - i) > 1)
					{
						writer.Write(";\r\n");
						this.WriteIndent(writer, indent);
						writer.Write("list:hasNext inst:" + obj.ifcowlclass + "_List_List_" + nextID);
					}
					writer.Write(".\r\n\r\n");
				}
			}
			else
			{
				Console.Out.WriteLine("Warning: this should not be possible: LISTOFLIST expected.");
			}
		}

		private void WriteExtraListObject(StreamWriter writer, ref int indent, ListObject obj, ref int nextID)
		{
			string ns = "ifcowl:";
			if (obj.listtype.Equals("INTEGER") || obj.listtype.Equals("REAL") || obj.listtype.Equals("DOUBLE") || obj.listtype.Equals("BINARY") || obj.listtype.Equals("BOOLEAN") || obj.listtype.Equals("LOGICAL") || obj.listtype.Equals("STRING"))
			{
				ns = "express:";
			}

			for (int i = 0; i < obj.values.Count; i++)
			{
				indent = 0;

				if (i == 0)
				{
					writer.Write("inst:" + obj.URI + "\r\n");
					// Console.Out.WriteLine("written ListObject: " + "inst:" + obj.URI + " with TYPE " + obj.XSDType);
				}
				else
				{
					writer.Write("inst:" + obj.listtype + "_" + nextID + "\r\n");
					// Console.Out.WriteLine("written ListObject: " + "inst:" + obj.listtype + "_" + m_nextID + " with TYPE " + obj.XSDType);
				}

				indent++;
				this.WriteType(writer, indent, ns + obj.listtype + ";\r\n");
				this.WriteIndent(writer, indent);

				writer.Write("list:hasContents inst:" + obj.values[i]);

				//generate
				nextID++;

				if ((obj.values.Count - i) > 1)
				{
					writer.Write(";\r\n");
					this.WriteIndent(writer, indent);
					writer.Write("list:hasNext inst:" + obj.listtype + "_" + nextID);
				}
				writer.Write(".\r\n\r\n");
			}
		}

		private ObjectProperty GetObjectProperty(string x)
		{
			ObjectProperty p;
			if (m_fullpropertynames.TryGetValue(x, out p))
				return p;
			else
			{
				Console.Out.WriteLine("ERROR: propertyname not found: " + x);
				return null;
			}
		}

		private URIObject GetURIObject(string domain, string encodedvalue, string XSDType, Dictionary<string, URIObject> valueObjects, ref int nextID)
		{
#if VERBOSE
            if (XSDType.Equals("RTFieldInfo", StringComparison.CurrentCultureIgnoreCase))
                Console.Out.WriteLine("Warning: Found RTFieldInfo XSDType for encodedvalue: " + encodedvalue);
            else
                Console.Out.WriteLine("Found seemingly ok XSDType for encodedvalue : " + encodedvalue + " - " + domain);
#endif
			//WARNING: _VALUE_ and _TYPE_ could be in the other strings
			string fullObject = domain + "_VALUE_" + encodedvalue + "_TYPE_" + XSDType;

			URIObject obj;
			if (!valueObjects.ContainsKey(fullObject))
			{
				nextID++;
				obj = new URIObject(domain + "_" + nextID, domain, encodedvalue, XSDType);
				valueObjects.Add(fullObject, obj);
			}
			else
			{
				obj = (URIObject)valueObjects[fullObject];
			}

			return obj;
		}

		private ListObject GetListOfListObject(string ifcowlclass, List<string> values, string XSDType, Dictionary<string, ListObject> listOfListObjects, ref int nextID)
		{
			//Console.Out.WriteLine("started GetListOfListObject run for list with number of elements: " + values.Count);
			ListObject obj;

			string encodedvalue = "";
			foreach (string s in values)
			{
				encodedvalue += s;
				encodedvalue += "_";
			}

			//WARNING: _LIST_ could be in the other strings
			encodedvalue = ifcowlclass + "_LIST_LIST_" + encodedvalue;
			if (!listOfListObjects.ContainsKey(encodedvalue))
			{
				nextID++;
				obj = new ListObject(ifcowlclass + "_List_List_" + nextID, ifcowlclass + "_List_List", ifcowlclass, values, "###LISTOFLIST###"); // ifcowlclass + "_List");
				listOfListObjects.Add(encodedvalue, obj);
				return obj;
			}
			else
			{
				obj = (ListObject)listOfListObjects[encodedvalue];
				return obj;
			}
		}

		//XSDType has 'XSD Data Type', '###ENTITY###', or '###LISTOFLIST###'
		private ListObject GetListObject(string listname, string ifcowlclass, List<string> values, string XSDType,
			Dictionary<string, URIObject> valueObjects, Dictionary<string, ListObject> listObjects, ref int nextID)
		{
			//Console.Out.WriteLine("started GetListObject run for list with number of elements: " + values.Count);
			ListObject obj;

			string encodedvalue = "";
			for (int i = 0; i < values.Count; i++)
			{
				if (XSDType != "###ENTITY###")
				{
#if VERBOSE
                    Console.Out.WriteLine("GetListObject().GetURIObject()");
#endif
					URIObject uo = GetURIObject(ifcowlclass, values[i], XSDType, valueObjects, ref nextID);
					values[i] = uo.URI;
					if (ifcowlclass != "IfcBinary")
						encodedvalue += uo.URI;
				}
				else
				{
					if (ifcowlclass != "IfcBinary")
						encodedvalue += values[i];
				}
				if (ifcowlclass != "IfcBinary")
					encodedvalue += "_";
			}

			//WARNING: _LIST_ could be in the other strings
			//IfcLengthMeasure_List_0_0_0
			//IfcRepresentation_List_#121_#145_#137
			//encodedvalue = ifcowlclass + "_LIST_" + encodedvalue;

			if (ifcowlclass != "IfcBinary")
				encodedvalue = listname + encodedvalue;

			if (listObjects.ContainsKey(encodedvalue) && ifcowlclass != "IfcBinary")
				return (ListObject)listObjects[encodedvalue];

			nextID++;

			if (XSDType == "###ENTITY###")
			{
#if VERBOSE
                Console.Out.WriteLine("Creating GetListObject list with XSDType : " + ifcowlclass);
#endif
				obj = new ListObject(listname + nextID, listname.Substring(0, listname.Length - 1), ifcowlclass, values, ifcowlclass);
			}
			else
			{
#if VERBOSE
                //create the additional datatype value
                Console.Out.WriteLine("Creating GetListObject list with XSDType : " + XSDType);
#endif
				obj = new ListObject(listname + nextID, listname.Substring(0, listname.Length - 1), ifcowlclass, values, XSDType);
			}

			if (ifcowlclass == "IfcBinary")
				listObjects.Add("-1", obj);
			else
				listObjects.Add(encodedvalue, obj);

			//Console.Out.WriteLine("finished GetListObject run for list with number of elements: " + values.Count);
			return obj;
		}

	}
}