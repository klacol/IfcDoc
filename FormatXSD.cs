// Name:        FormatXSD.cs
// Description: XML Schema Definition Exporter
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;

using IfcDoc.Schema.DOC;

namespace IfcDoc.Format.XSD
{
	internal class FormatXSD :
		IFormatExtension,
		IDisposable,
		IComparer<string>
	{
		string m_filename;
		DocProject m_project;
		DocPublication m_publication;
		DocModelView[] m_views;
		Dictionary<DocObject, bool> m_included;

		public FormatXSD(string path)
		{
			this.m_filename = path;
		}

		public DocProject Project
		{
			get
			{
				return this.m_project;
			}
			set
			{
				this.m_project = value;
			}
		}

		public DocPublication Publication
		{
			get
			{
				return this.m_publication;
			}
			set
			{
				this.m_publication = value;
			}
		}

		/// <summary>
		/// Optional model views for filtering definitions.
		/// </summary>
		public DocModelView[] ModelViews
		{
			get
			{
				return this.m_views;
			}
			set
			{
				this.m_views = value;
				this.m_included = null;
				if (this.m_views != null)
				{
					this.m_included = new Dictionary<DocObject, bool>();
					foreach (DocModelView docView in this.m_views)
					{
						this.m_project.RegisterObjectsInScope(docView, this.m_included);
					}
				}
			}
		}

		private static void WriteResource(System.IO.StreamWriter writer, string resourcename)
		{
			using (System.IO.Stream stm = System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(resourcename))
			{
				using (System.IO.StreamReader reader = new System.IO.StreamReader(stm))
				{
					string block = reader.ReadToEnd();
					writer.Write(block);
				}
			}
		}

		public void Save()
		{
			// build map of types
			Dictionary<string, DocObject> map = new Dictionary<string, DocObject>();
			foreach (DocSection docSection in this.m_project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEnt in docSchema.Entities)
					{
						if (!map.ContainsKey(docEnt.Name))
						{
							map.Add(docEnt.Name, docEnt);
						}
					}
					foreach (DocType docType in docSchema.Types)
					{
						if (!map.ContainsKey(docType.Name))
						{
							map.Add(docType.Name, docType);
						}
					}
				}
			}

			string content = this.FormatDefinitions(this.m_project, this.m_publication, map, this.m_included);

			string dirpath = System.IO.Path.GetDirectoryName(this.m_filename);
			if (!System.IO.Directory.Exists(dirpath))
			{
				System.IO.Directory.CreateDirectory(dirpath);
			}

			using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.m_filename))
			{
				if (writer.BaseStream.CanSeek)
				{
					writer.BaseStream.SetLength(0);
				}
				writer.Write(content);
			}
		}

		public static bool IsAttributeOverridden(DocEntity ent, DocAttribute attr, Dictionary<string, DocObject> map)
		{
			foreach (DocObject obj in map.Values)
			{
				if (obj is DocEntity)
				{
					DocEntity eachEnt = (DocEntity)obj;
					if (eachEnt.BaseDefinition != null && eachEnt.BaseDefinition.Equals(ent.Name))
					{
						foreach (DocAttribute eachAttr in eachEnt.Attributes)
						{
							if (eachAttr.Name.Equals(attr.Name))
								return true;
						}

						// recurse
						bool inner = IsAttributeOverridden(eachEnt, attr, map);
						if (inner)
							return true;
					}
				}
			}

			return false;
		}

		private static string FormatAttribute(DocEntity docEntity, DocAttribute docAttr, Dictionary<string, DocObject> map)
		{
			if (docAttr.Name.Equals("InnerCoordIndices"))
			{
				docAttr.ToString();
			}

			DocObject mapDef = null;
			map.TryGetValue(docAttr.DefinedType, out mapDef);

			string xsdtype = ToXsdType(docAttr.DefinedType);

			StringBuilder sb = new StringBuilder();


			bool sequencewrapper = (mapDef is DocDefined && docAttr.AggregationUpper == "?" && docAttr.AggregationAttribute != null && docAttr.AggregationAttribute.AggregationUpper == "?");
			if (sequencewrapper)
			{
				// IfcIndexPolygonalFaceWithVoids.InnerCoordIndices

				sb.Append("\t\t\t\t\t<xs:element name=\"" + docAttr.Name + "\" minOccurs=\"0\">");
				sb.AppendLine();

				sb.Append("\t\t\t\t\t\t<xs:complexType>");
				sb.AppendLine();

				sb.Append("\t\t\t\t\t\t\t<xs:sequence>");
				sb.AppendLine();

				xsdtype = xsdtype.Replace(":", ":Seq-");

				sb.Append("\t\t\t\t\t\t\t\t<xs:element name=\"Seq-" + mapDef.Name + "-wrapper\" type=\"" + xsdtype + "\" maxOccurs=\"unbounded\" />");
				sb.AppendLine();
				sb.Append("\t\t\t\t\t\t\t</xs:sequence>");
				sb.AppendLine();

				sb.Append("\t\t\t\t\t\t</xs:complexType>");
				sb.AppendLine();

				sb.Append("\t\t\t\t\t</xs:element>");
				sb.AppendLine();

				return sb.ToString();
			}

			sb.Append("\t\t\t\t\t<xs:element");

			sb.Append(" name=\"");
			sb.Append(docAttr.Name);
			sb.Append("\"");

			if ((docAttr.AggregationType == 0 && mapDef is DocEntity) || docAttr.XsdFormat == DocXsdFormatEnum.Attribute)
			{
				sb.Append(" type=\"");
				sb.Append(ToXsdType(docAttr.DefinedType));
				sb.Append("\"");
			}

			if (docAttr.IsOptional ||
				mapDef is DocEntity && (docAttr.GetAggregation() == DocAggregationEnum.NONE || docAttr.GetAggregationNestingLower() == 1 && docAttr.GetAggregationNestingUpper() == 1) ||
				docAttr.Inverse != null)
			{
				sb.Append(" nillable=\"true\"");
			}

			if (docAttr.IsOptional || docAttr.Inverse != null || IsAttributeOverridden(docEntity, docAttr, map))// || (docAttr.GetAggregation() == DocAggregationEnum.SET && docAttr.GetAggregationNestingLower() == 0))// || docAttr.XsdFormat == DocXsdFormatEnum.Attribute)
			{
				sb.Append(" minOccurs=\"0\"");
			}

			if (docAttr.AggregationType != 0 && docAttr.XsdFormat == DocXsdFormatEnum.Attribute)
			{
				sb.Append(" maxOccurs=\"1\"");
			}

			if (mapDef is DocSelect || (docAttr.AggregationType != 0 && docAttr.XsdFormat != DocXsdFormatEnum.Attribute)) // added
			{
				sb.Append(">");
				sb.AppendLine();

				sb.Append("\t\t\t\t\t\t<xs:complexType>");
				sb.AppendLine();

				if (mapDef is DocSelect)
				{
					sb.Append("\t\t\t\t\t\t\t<xs:group ref=\"");
				}
				else
				{
					sb.Append("\t\t\t\t\t\t\t<xs:sequence>");
					sb.AppendLine();

					sb.Append("\t\t\t\t\t\t\t\t<xs:element ref=\"");
				}

				if (mapDef == null)
				{
					xsdtype = xsdtype.Replace("xs:", "ifc:") + "-wrapper";
				}
				else if (sequencewrapper)
				{
					xsdtype = xsdtype.Replace(":", ":Seq-"); // IfcIndexPolygonalFaceWithVoids.InnerCoordIndices
				}
				else if (docAttr.XsdFormat == DocXsdFormatEnum.Element && (mapDef is DocDefined || mapDef is DocEnumeration))
				{
					xsdtype += "-wrapper";
				}
				sb.Append(xsdtype);
				sb.Append("\"");

				if (docAttr.AggregationType != 0)
				{
					int agglower = docAttr.GetAggregationNestingLower();
					if (agglower != 1 || docAttr.AggregationAttribute != null) // was 1
					{
						sb.Append(" minOccurs=\"");
						sb.Append(docAttr.GetAggregationNestingLower());
						sb.Append("\"");
					}

					string maxoccurs = "unbounded";
					if (!String.IsNullOrEmpty(docAttr.AggregationUpper) && docAttr.AggregationUpper != "0" && docAttr.AggregationUpper != "?")
					{
						maxoccurs = docAttr.GetAggregationNestingUpper().ToString();
					}
					sb.Append(" maxOccurs=\"");
					sb.Append(maxoccurs);
					sb.Append("\"");
				}

				sb.Append("/>");
				sb.AppendLine();

				if (!(mapDef is DocSelect))
				{
					sb.Append("\t\t\t\t\t\t\t</xs:sequence>");
					sb.AppendLine();
				}

				if (docAttr.AggregationType != 0)
				{
					sb.Append("\t\t\t\t\t\t\t<xs:attribute ref=\"ifc:itemType\" fixed=\"");
					sb.Append(xsdtype);
					sb.Append("\"/>");
					sb.AppendLine();

					sb.Append("\t\t\t\t\t\t\t<xs:attribute ref=\"ifc:cType\" fixed=\"");

					DocAttribute docAggregation = docAttr;
					while (docAggregation != null)
					{
						// nested collections
						if (docAggregation != docAttr)
						{
							sb.Append(" ");
						}

						sb.Append(docAggregation.GetAggregation().ToString().ToLower());
						if ((docAggregation.AggregationFlag & 2) != 0)
						{
							sb.Append("-unique");
						}

						// next
						docAggregation = docAggregation.AggregationAttribute;
					}

					sb.Append("\"/>");
					sb.AppendLine();

					sb.Append("\t\t\t\t\t\t\t<xs:attribute ref=\"ifc:arraySize\" use=\"optional\"/>");
					sb.AppendLine();
				}

				sb.Append("\t\t\t\t\t\t</xs:complexType>");
				sb.AppendLine();

				sb.Append("\t\t\t\t\t</xs:element>");
				sb.AppendLine();
			}
			else
			{
				sb.Append("/>");
				sb.AppendLine();
			}

			return sb.ToString();
		}

		public static string ToXsdType(string typename)
		{
			string defined = "ifc:" + typename;
			switch (typename)
			{
				case "BOOLEAN":
					defined = "xs:boolean";
					break;

				case "LOGICAL":
					defined = "ifc:logical";
					break;

				case "INTEGER":
					defined = "xs:long";
					break;

				case "STRING":
					defined = "xs:normalizedString";
					break;

				case "REAL":
				case "NUMBER":
					defined = "xs:double";
					break;

				case "BINARY":
				case "BINARY (32)":
					defined = "ifc:hexBinary";
					break;
			}

			return defined;
		}

		public static string FormatTypeWrapper(DocType docDefined, Dictionary<string, DocObject> map)
		{
			StringBuilder sb = new StringBuilder();

			// wrapper
			/*
    <xs:element name="IfcPressureMeasure-wrapper" nillable="true">
        <xs:complexType>
            <xs:simpleContent>
                <xs:extension base="ifc:IfcPressureMeasure">
                    <xs:attributeGroup ref="ifc:instanceAttributes"/>
                </xs:extension>
            </xs:simpleContent>
        </xs:complexType>
    </xs:element>
            */

			bool complex = false;
			if (docDefined is DocDefined)
			{
				DocDefined docDef = (DocDefined)docDefined;
				DocObject docobj = null;
				if (docDef.DefinedType != null && map.TryGetValue(docDef.DefinedType, out docobj) && docobj is DocEntity)
				{
					complex = true;
				}
			}

			sb.Append("\t<xs:element name=\"");
			sb.Append(docDefined.Name);
			sb.Append("-wrapper\" nillable=\"true\">");
			sb.AppendLine();

			sb.AppendLine("\t\t<xs:complexType>");

			if (complex)
			{
				sb.AppendLine("\t\t\t<xs:complexContent>");
			}
			else
			{
				sb.AppendLine("\t\t\t<xs:simpleContent>");
			}

			sb.Append("\t\t\t\t<xs:extension base=\"");
			sb.Append(ToXsdType(docDefined.Name));
			sb.AppendLine("\">");

			sb.AppendLine("\t\t\t\t\t<xs:attributeGroup ref=\"ifc:instanceAttributes\"/>");

			sb.AppendLine("\t\t\t\t</xs:extension>");

			if (complex)
			{
				sb.AppendLine("\t\t\t</xs:complexContent>");
			}
			else
			{
				sb.AppendLine("\t\t\t</xs:simpleContent>");
			}
			sb.AppendLine("\t\t</xs:complexType>");

			sb.Append("\t</xs:element>");

			sb.AppendLine();

			return sb.ToString();
		}

#if false
        public static string FormatDefined(DocDefined docDefined, Dictionary<string, DocObject> map)
        {
            return FormatDefined(docDefined) + FormatTypeWrapper(docDefined, map);
        }
#endif

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion

		#region IComparer Members

		public int Compare(string x, string y)
		{
			return String.CompareOrdinal((string)x, (string)y);
		}

		#endregion

		public string FormatEntity(DocEntity docEntity, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			if (docEntity.Name.Equals("IfcIndexedPolygonalFaceWithVoids"))
			{
				docEntity.ToString();
			}

			string basetype = docEntity.BaseDefinition;
			if (String.IsNullOrEmpty(basetype))
			{
				basetype = "Entity";
			}

			StringBuilder sb = new StringBuilder();

			// if any derived attributes, then must use intermediate type
			bool hasderivedattributes = false;
			foreach (DocAttribute docAttr in docEntity.Attributes)
			{
				if (docAttr.Derived != null && docAttr.Inverse == null)
				{
					// determine the superclass having the attribute                        
					DocEntity found = null;
					DocEntity super = docEntity;
					while (super != null && found == null && super.BaseDefinition != null)
					{
						super = map[super.BaseDefinition] as DocEntity;
						if (super != null)
						{
							foreach (DocAttribute docattrSuper in super.Attributes)
							{
								if (docattrSuper.Name.Equals(docAttr.Name))
								{
									// found class
									found = super;
									break;
								}
							}
						}
					}

					if (found != null)
					{
						hasderivedattributes = true;
					}

					break;
				}
			}

			if (hasderivedattributes)
			{
				sb.Append("\t<xs:complexType name=\"");
				sb.Append(docEntity.Name);
				sb.Append("-temp\" abstract=\"true\">");
				sb.AppendLine();

				sb.Append("\t\t<xs:complexContent>");
				sb.AppendLine();

				sb.Append("\t\t\t<xs:restriction base=\"ifc:");
				sb.Append(docEntity.BaseDefinition);
				sb.Append("\">");
				sb.AppendLine();

				// restate non-derived attributes at superclass, if any
				List<DocAttribute> listRestate = new List<DocAttribute>();
				DocEntity docSuper = map[docEntity.BaseDefinition] as DocEntity;
				foreach (DocAttribute docAttrSuper in docSuper.Attributes)
				{
					// check if attribute not derived by us
					bool derived = false;
					foreach (DocAttribute docAttrThis in docEntity.Attributes)
					{
						if (docAttrSuper.Name.Equals(docAttrThis.Name))
						{
							derived = true;
							break;
						}
					}

					if (!derived)
					{
						DocObject mapDef = null;
						if ((docAttrSuper.Inverse == null/* || docAttrSuper.XsdFormat == DocXsdFormatEnum.Element || docAttrSuper.XsdFormat == DocXsdFormatEnum.Attribute*/) &&
							docAttrSuper.Derived == null &&
							(map.TryGetValue(docAttrSuper.DefinedType, out mapDef) || docAttrSuper.DefinedType.StartsWith("BINARY")))
						{
							if (mapDef is DocEntity || mapDef is DocSelect)
							{
								listRestate.Add(docAttrSuper);
							}
						}
					}
				}

				if (listRestate.Count > 0)
				{
					sb.AppendLine("\t\t\t\t<xs:sequence>");
					foreach (DocAttribute docAttr in listRestate)
					{
						string formatAttr = FormatAttribute(docEntity, docAttr, map);
						sb.Append(formatAttr);
					}
					sb.Append("\t\t\t\t</xs:sequence>");
				}
				else
				{
					sb.Append("\t\t\t\t<xs:sequence/>");
				}
				sb.AppendLine();

				sb.Append("\t\t\t</xs:restriction>");
				sb.AppendLine();

				sb.Append("\t\t</xs:complexContent>");
				sb.AppendLine();

				sb.Append("\t</xs:complexType>");
				sb.AppendLine();
			}

			sb.Append("\t<xs:element");
			sb.Append(" name=\"");
			sb.Append(docEntity.Name);
			sb.Append("\" type=\"ifc:");
			sb.Append(docEntity.Name);
			sb.Append("\"");
			if (docEntity.IsAbstract)
			{
				sb.Append(" abstract=\"true\"");
			}
			sb.Append(" substitutionGroup=\"ifc:");
			sb.Append(basetype);
			sb.Append("\" nillable=\"true\"");
			sb.Append("/>");
			sb.AppendLine();

			sb.Append("\t<xs:complexType name=\"");
			sb.Append(docEntity.Name);
			sb.Append("\"");
			if (docEntity.IsAbstract)
			{
				sb.Append(" abstract=\"true\"");
			}
			sb.Append(">");
			sb.AppendLine();

			sb.Append("\t\t<xs:complexContent>");
			sb.AppendLine();

			sb.Append("\t\t\t<xs:extension base=\"ifc:");
			if (hasderivedattributes)
			{
				sb.Append(docEntity.Name);
				sb.Append("-temp");
			}
			else
			{
				sb.Append(basetype);
			}
			sb.Append("\"");

			bool hascontent = false;

			// inline elements
			bool hassequence = false;
			foreach (DocAttribute docAttr in docEntity.Attributes)
			{
				if (included == null || included.ContainsKey(docAttr))
				{
					if (docAttr.XsdFormat != DocXsdFormatEnum.Hidden && !(docAttr.XsdTagless == true) && docAttr.DefinedType != null)
					{
						DocObject mapDef = null;
						map.TryGetValue(docAttr.DefinedType, out mapDef);

						if ((docAttr.Inverse == null || docAttr.XsdFormat == DocXsdFormatEnum.Element || docAttr.XsdFormat == DocXsdFormatEnum.Attribute) &&
							docAttr.Derived == null)
						{
							// special case for IfcIndexedPolygonalFaceWithVoids.InnerCoordIndices
							bool sequencewrapper = (mapDef is DocDefined && docAttr.AggregationUpper == "?" && docAttr.AggregationAttribute != null && docAttr.AggregationAttribute.AggregationUpper == "?");

							if (sequencewrapper)
							{
								sequencewrapper.ToString();
							}

							if (mapDef is DocEntity ||
								mapDef is DocSelect ||
								sequencewrapper ||
								docAttr.XsdFormat == DocXsdFormatEnum.Element ||
								docAttr.XsdFormat == DocXsdFormatEnum.Attribute)
							{
								if (!hascontent)
								{
									sb.Append(">");
									sb.AppendLine();
									hascontent = true;
								}

								if (!hassequence)
								{
									sb.AppendLine("\t\t\t\t<xs:sequence>");
									hassequence = true;
								}

								string formatAttr = FormatAttribute(docEntity, docAttr, map);
								sb.Append(formatAttr);
							}



						}
					}
				}
			}
			if (hassequence)
			{
				sb.AppendLine("\t\t\t\t</xs:sequence>");
			}

			// then attributes for value types
			foreach (DocAttribute docAttr in docEntity.Attributes)
			{
				if (included == null || included.ContainsKey(docAttr))
				{

					if (docAttr.XsdFormat != DocXsdFormatEnum.Hidden && docAttr.XsdFormat != DocXsdFormatEnum.Attribute)//docAttr.DefinedType != null)// && docAttr.XsdFormat != DocXsdFormatEnum.Element*/)
					{
						DocObject mapDef = null;
						if ((docAttr.Inverse == null || docAttr.XsdFormat == DocXsdFormatEnum.Attribute) &&
							docAttr.Derived == null && (docAttr.XsdFormat != DocXsdFormatEnum.Element || docAttr.XsdTagless == true))
						{

							if (docAttr.DefinedType == null ||
								(!map.TryGetValue(docAttr.DefinedType, out mapDef)) || // native type
								(mapDef is DocDefined || mapDef is DocEnumeration || docAttr.XsdTagless == true))
							{
								bool sequencewrapper = (mapDef is DocDefined && docAttr.AggregationUpper == "?" && docAttr.AggregationAttribute != null && docAttr.AggregationAttribute.AggregationUpper == "?");
								if (!sequencewrapper && (mapDef == null || (included == null || included.ContainsKey(mapDef))))
								{
									if (!hascontent)
									{
										sb.Append(">");
										sb.AppendLine();
										hascontent = true;
									}

									// encode value types as attributes
									sb.Append("\t\t\t\t<xs:attribute");

									sb.Append(" name=\"");
									sb.Append(docAttr.Name);
									sb.Append("\"");

									if (docAttr.AggregationType == 0)
									{
										sb.Append(" type=\"");

										if (mapDef is DocDefined && ((DocDefined)mapDef).Aggregation != null)
										{
											sb.Append("ifc:List-" + docAttr.DefinedType);
										}
										else
										{
											sb.Append(ToXsdType(docAttr.DefinedType));
										}
										sb.Append("\"");

										if (true) // all attributes optional in XSD? docAttr.IsOptional())
										{
											sb.Append(" use=\"optional\"");
										}

										sb.Append("/>");
									}
									else
									{
										// all attributes optional in XSD
										sb.Append(" use=\"optional\"");

										sb.Append(">");
										sb.AppendLine();

										sb.AppendLine("\t\t\t\t\t<xs:simpleType>");
										sb.AppendLine("\t\t\t\t\t\t<xs:restriction>");
										sb.AppendLine("\t\t\t\t\t\t\t<xs:simpleType>");

										sb.Append("\t\t\t\t\t\t\t\t<xs:list itemType=\"");

										if (docAttr.DefinedType == "IfcBinary")
										{
											// workaround: // for lists of BINARY, revert to xs:hexBinary for efficieny; todo: update schema
											sb.Append("xs:hexBinary");
										}
										else
										{
											sb.Append(ToXsdType(docAttr.DefinedType));
										}
										sb.Append("\"/>");
										sb.AppendLine();

										sb.AppendLine("\t\t\t\t\t\t\t</xs:simpleType>");

										int iLower = docAttr.GetAggregationNestingLower();
										int iUpper = docAttr.GetAggregationNestingUpper();

										// minimum
										if (docAttr.GetAggregation() == DocAggregationEnum.ARRAY)
										{
											iLower = iUpper;
										}

										if (iLower != -1 && iLower != 1)
										{
											sb.Append("\t\t\t\t\t\t\t<xs:minLength value=\"");
											sb.Append(iLower);
											sb.Append("\"/>");
											sb.AppendLine();
										}

										if (iUpper != 0)
										{
											sb.Append("\t\t\t\t\t\t\t<xs:maxLength value=\"");
											sb.Append(iUpper);
											sb.Append("\"/>");
											sb.AppendLine();
										}

										sb.AppendLine("\t\t\t\t\t\t</xs:restriction>");
										sb.AppendLine("\t\t\t\t\t</xs:simpleType>");

										sb.Append("\t\t\t\t</xs:attribute>");
									}

									sb.AppendLine();
								}
							}
						}
					}
				}
			}

			if (hascontent)
			{
				sb.Append("\t\t\t</xs:extension>");
				sb.AppendLine();
			}
			else
			{
				sb.Append("/>");
				sb.AppendLine();
			}

			sb.Append("\t\t</xs:complexContent>");
			sb.AppendLine();

			sb.Append("\t</xs:complexType>");
			sb.AppendLine();

			// also capture Sequence types...

			return sb.ToString();
		}

		public string FormatEnumeration(DocEnumeration docEnum, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("\t<xs:simpleType name=\"");
			sb.Append(docEnum.Name);
			sb.Append("\">");
			sb.AppendLine();

			sb.AppendLine("\t\t<xs:restriction base=\"xs:string\">");

			foreach (DocConstant docConst in docEnum.Constants)
			{
				sb.Append("\t\t\t<xs:enumeration value=\"");
				sb.Append(docConst.Name.ToLower());
				sb.Append("\"/>");
				sb.AppendLine();
			}

			sb.AppendLine("\t\t</xs:restriction>");

			sb.Append("\t</xs:simpleType>");
			sb.AppendLine();

			return sb.ToString();
		}

		public string FormatSelect(DocSelect docSelect, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("\t<xs:group name=\"");
			sb.Append(docSelect.Name);
			sb.Append("\">");
			sb.AppendLine();

			sb.Append("\t\t<xs:choice>");
			sb.AppendLine();

			Queue<DocSelectItem> queue = new Queue<DocSelectItem>();
			foreach (DocSelectItem docItem in docSelect.Selects)
			{
				queue.Enqueue(docItem);
			}

			// sort selects alphabetically
			SortedList<string, DocSelectItem> sort = new SortedList<string, DocSelectItem>(new FormatXSD(null));
			while (queue.Count > 0)
			{
				DocSelectItem docItem = queue.Dequeue();

				DocObject mapDef = null;
				if (map.TryGetValue(docItem.Name, out mapDef))
				{
					if (mapDef is DocSelect)
					{
						// expand each
						DocSelect docSub = (DocSelect)mapDef;
						foreach (DocSelectItem dsi in docSub.Selects)
						{
							queue.Enqueue(dsi);
						}
					}
					else if ((included == null || included.ContainsKey(mapDef)) && !sort.ContainsKey(docItem.Name))
					{
						//TODO: if abstract entity, then go through subtypes...
						sort.Add(docItem.Name, docItem);
					}
				}
			}

			// resolve selects into final elements

			// first entities, then wrappers

			// entities
			foreach (DocSelectItem docItem in sort.Values)
			{
				DocObject mapDef = null;
				if (map.TryGetValue(docItem.Name, out mapDef))
				{
					if (included == null || included.ContainsKey(mapDef))
					{
						sb.Append("\t\t\t<xs:element ref=\"ifc:");
						sb.Append(docItem.Name);

						if (mapDef is DocDefined || mapDef is DocEnumeration)
						{
							sb.Append("-wrapper");
						}

						sb.Append("\"/>");
						sb.AppendLine();
					}
				}
			}

			sb.Append("\t\t</xs:choice>");
			sb.AppendLine();

			sb.Append("\t</xs:group>");
			sb.AppendLine();

			return sb.ToString();
		}

		public string FormatDefined(DocDefined docDefined, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			string defined = ToXsdType(docDefined.DefinedType);

			StringBuilder sb = new StringBuilder();

			if (docDefined.Aggregation != null)
			{
				string aggtype = docDefined.Aggregation.GetAggregation().ToString().ToLower();

				if (docDefined.Aggregation.GetAggregation() == DocAggregationEnum.SET)
				{
					sb.Append("\t<xs:complexType name=\"");
					sb.Append(docDefined.Name);
					sb.Append("\">");
					sb.AppendLine();

					sb.AppendLine("\t\t<xs:sequence>");
					sb.Append("\t\t\t<xs:element ref=\"");
					sb.Append(defined);
					sb.AppendLine("\" maxOccurs=\"unbounded\"/>");
					sb.AppendLine("\t\t</xs:sequence>");

					sb.Append("\t\t<xs:attribute ref=\"ifc:itemType\" fixed=\"");
					sb.Append(defined);
					sb.AppendLine("\"/>");

					sb.Append("\t\t<xs:attribute ref=\"ifc:cType\" fixed=\"");
					sb.Append(aggtype);
					sb.AppendLine("\"/>");

					sb.Append("\t\t<xs:attribute ref=\"ifc:arraySize\" use=\"");
					sb.Append("optional");
					sb.AppendLine("\"/>");

					sb.Append("\t</xs:complexType>");
					sb.AppendLine();
				}
				else
				{

					sb.Append("\t<xs:complexType name=\"");
					sb.Append(docDefined.Name);
					sb.Append("\">");
					sb.AppendLine();

					sb.AppendLine("\t\t<xs:simpleContent>");
					sb.Append("\t\t\t<xs:extension base=\"ifc:List-");
					sb.Append(docDefined.Name);
					sb.AppendLine("\">");

					sb.Append("\t\t\t\t<xs:attribute ref=\"ifc:itemType\" fixed=\"");
					sb.Append(defined);
					sb.AppendLine("\"/>");

					sb.Append("\t\t\t\t<xs:attribute ref=\"ifc:cType\" fixed=\"");
					sb.Append(aggtype);
					sb.AppendLine("\"/>");

					sb.Append("\t\t\t\t<xs:attribute ref=\"ifc:arraySize\" use=\"");
					sb.Append("optional");
					sb.AppendLine("\"/>");

					sb.AppendLine("\t\t\t</xs:extension>");
					sb.AppendLine("\t\t</xs:simpleContent>");

					sb.Append("\t</xs:complexType>");
					sb.AppendLine();

					// simple type
					sb.Append("\t<xs:simpleType name=\"List-");
					sb.Append(docDefined.Name);
					sb.Append("\">");
					sb.AppendLine();

					sb.AppendLine("\t\t<xs:restriction>");
					sb.AppendLine("\t\t\t<xs:simpleType>");
					sb.Append("\t\t\t\t<xs:list itemType=\"");
					sb.Append(defined);
					sb.AppendLine("\"/>");
					sb.AppendLine("\t\t\t</xs:simpleType>");

					if (docDefined.Aggregation.GetAggregation() == DocAggregationEnum.ARRAY)
					{
						sb.Append("\t\t\t<xs:minLength value=\"");
						sb.Append(docDefined.Aggregation.GetAggregationNestingUpper());
						sb.AppendLine("\"/>");
					}
					else if (docDefined.Aggregation.AggregationLower != null)
					{
						sb.Append("\t\t\t<xs:minLength value=\"");
						sb.Append(docDefined.Aggregation.GetAggregationNestingLower());
						sb.AppendLine("\"/>");
					}

					if (docDefined.Aggregation.AggregationUpper != null)
					{
						sb.Append("\t\t\t<xs:maxLength value=\"");
						sb.Append(docDefined.Aggregation.GetAggregationNestingUpper());
						sb.AppendLine("\"/>");
					}

					sb.AppendLine("\t\t</xs:restriction>");

					sb.Append("\t</xs:simpleType>");
					sb.AppendLine();
				}
			}
			else if (docDefined.DefinedType != null && docDefined.DefinedType.Equals("BINARY"))
			{
				sb.Append("\t<xs:complexType name=\"");
				sb.Append(docDefined.Name);
				sb.Append("\">");
				sb.AppendLine();

				sb.AppendLine("\t\t<xs:simpleContent>");

				sb.Append("\t\t\t<xs:extension base=\"");
				sb.Append(defined);
				sb.AppendLine("\">");

				sb.AppendLine("\t\t\t</xs:extension>");

				sb.AppendLine("\t\t</xs:simpleContent>");

				sb.Append("\t</xs:complexType>");
				sb.AppendLine();
			}
			else
			{
				sb.Append("\t<xs:simpleType name=\"");
				sb.Append(docDefined.Name);
				sb.Append("\">");
				sb.AppendLine();

				sb.Append("\t\t<xs:restriction base=\"");
				sb.Append(defined);

				if (docDefined.Length > 0)
				{
					sb.Append("\">");
					sb.AppendLine();

					sb.Append("\t\t\t<xs:maxLength value=\"");
					sb.Append(docDefined.Length);
					sb.AppendLine("\"/>");

					sb.AppendLine("\t\t</xs:restriction>");
				}
				else if (docDefined.Length < 0)
				{
					// fixed
					sb.Append("\">");
					sb.AppendLine();

					sb.Append("\t\t\t<xs:minLength value=\"");
					sb.Append(-docDefined.Length);
					sb.AppendLine("\"/>");

					sb.Append("\t\t\t<xs:maxLength value=\"");
					sb.Append(-docDefined.Length);
					sb.AppendLine("\"/>");

					sb.AppendLine("\t\t</xs:restriction>");
				}
				else if (docDefined.Aggregation != null)
				{
					sb.Append("\">");
					sb.AppendLine();

					if (docDefined.Aggregation.AggregationLower != null)
					{
						sb.Append("\t\t\t<xs:minLength value=\"");
						sb.Append(docDefined.Aggregation.GetAggregationNestingLower());
						sb.AppendLine("\"/>");
					}

					if (docDefined.Aggregation.AggregationUpper != null)
					{
						sb.Append("\t\t\t<xs:maxLength value=\"");
						sb.Append(docDefined.Aggregation.GetAggregationNestingUpper());
						sb.AppendLine("\"/>");
					}

					sb.AppendLine("\t\t</xs:restriction>");
				}
				else
				{
					sb.Append("\"/>");
					sb.AppendLine();
				}

				sb.Append("\t</xs:simpleType>");
				sb.AppendLine();
			}

			return sb.ToString();
		}

		public string FormatDefinitions(DocProject docProject, DocPublication docPublication, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			this.m_included = included;

			string xmlns = docProject.GetSchemaURI(docPublication);
			// use XSD configuration of first view
			if (this.m_views != null && this.m_views.Length >= 1 && !String.IsNullOrEmpty(this.m_views[0].Code))
			{
				DocModelView docView = this.m_views[0];

				if (!String.IsNullOrEmpty(docView.XsdUri))
				{
					xmlns = docView.XsdUri;
				}
			}

			SortedList<string, DocDefined> mapDefined = new SortedList<string, DocDefined>(this);
			SortedList<string, DocEnumeration> mapEnum = new SortedList<string, DocEnumeration>(this);
			SortedList<string, DocSelect> mapSelect = new SortedList<string, DocSelect>(this);
			SortedList<string, DocEntity> mapEntity = new SortedList<string, DocEntity>(this);
			SortedList<string, DocFunction> mapFunction = new SortedList<string, DocFunction>(this);
			SortedList<string, DocGlobalRule> mapRule = new SortedList<string, DocGlobalRule>(this);

			SortedList<string, string> sort = new SortedList<string, string>(this);

			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					if (this.m_included == null || this.m_included.ContainsKey(docSchema))
					{
						foreach (DocType docType in docSchema.Types)
						{
							if (this.m_included == null || this.m_included.ContainsKey(docType))
							{
								if (docType is DocDefined)
								{
									if (!mapDefined.ContainsKey(docType.Name))
									{
										mapDefined.Add(docType.Name, (DocDefined)docType);
									}
								}
								else if (docType is DocEnumeration)
								{
									mapEnum.Add(docType.Name, (DocEnumeration)docType);
								}
								else if (docType is DocSelect)
								{
									mapSelect.Add(docType.Name, (DocSelect)docType);
								}
							}
						}

						foreach (DocEntity docEnt in docSchema.Entities)
						{
							if (this.m_included == null || this.m_included.ContainsKey(docEnt))
							{
								if (!mapEntity.ContainsKey(docEnt.Name))
								{
									mapEntity.Add(docEnt.Name, docEnt);
								}

								// check for any attributes that are lists of value types requiring wrapper, e.g. IfcTextFontName
								foreach (DocAttribute docAttr in docEnt.Attributes)
								{
									DocObject docObjRef = null;
									if (docAttr.DefinedType != null &&
										docAttr.GetAggregation() != DocAggregationEnum.NONE &&
										map.TryGetValue(docAttr.DefinedType, out docObjRef) &&
										docObjRef is DocDefined &&
										docAttr.XsdFormat == DocXsdFormatEnum.Element &&
										!(docAttr.XsdTagless == true) &&
										!sort.ContainsKey(docAttr.DefinedType))
									{
										sort.Add(docAttr.DefinedType, docAttr.DefinedType);
									}
								}
							}
						}

						foreach (DocFunction docFunc in docSchema.Functions)
						{
							if ((this.m_included == null || this.m_included.ContainsKey(docFunc)) && !mapFunction.ContainsKey(docFunc.Name))
							{
								mapFunction.Add(docFunc.Name, docFunc);
							}
						}

						foreach (DocGlobalRule docRule in docSchema.GlobalRules)
						{
							if (this.m_included == null || this.m_included.ContainsKey(docRule))
							{
								mapRule.Add(docRule.Name, docRule);
							}
						}
					}
				}
			}

			System.IO.MemoryStream stream = new System.IO.MemoryStream();
			using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
			{
				writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
				writer.WriteLine("<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" " +
					"xmlns:ifc=\"" + xmlns + "\" " +
					"targetNamespace=\"" + xmlns + "\" " +
					"elementFormDefault=\"qualified\" attributeFormDefault=\"unqualified\" >");

				WriteResource(writer, "IfcDoc.xsd1.txt");

				// Entities
				writer.WriteLine("\t<!-- element and complex type declarations (for ENTITY definitions) -->");
				foreach (DocEntity docEntity in mapEntity.Values)
				{
					writer.Write(FormatEntity(docEntity, map, this.m_included));
				}

				// Selects
				writer.WriteLine("\t<!-- group declarations (for SELECT data type definitions) -->");
				foreach (DocSelect docSelect in mapSelect.Values)
				{
					writer.Write(FormatSelect(docSelect, map, this.m_included));
				}

				// Enumerations
				writer.WriteLine("\t<!-- enumeration type declarations (for ENUMERATION data type definitions) -->");
				foreach (DocEnumeration docEnum in mapEnum.Values)
				{
					writer.Write(FormatEnumeration(docEnum, map, this.m_included));
				}

				// Defined Types
				writer.WriteLine("\t<!-- simple type declarations (for TYPE defined data type definitions) -->");
				foreach (DocDefined docDefined in mapDefined.Values)
				{
					writer.Write(FormatDefined(docDefined, map, this.m_included));
				}

				WriteResource(writer, "IfcDoc.xsd2.txt");

				// sort selects alphabetically
				Queue<DocSelectItem> queue = new Queue<DocSelectItem>();
				foreach (DocSelect docSelect in mapSelect.Values)
				{
					foreach (DocSelectItem docSelItem in docSelect.Selects)
					{
						queue.Enqueue(docSelItem);
					}
				}
				List<DocDefined> listWrapper = new List<DocDefined>(); // keep track of wrapped types
				while (queue.Count > 0)
				{
					DocSelectItem docItem = queue.Dequeue();

					DocObject mapDef = null;
					if (map.TryGetValue(docItem.Name, out mapDef))
					{
						if (mapDef is DocSelect)
						{
							// expand each
							DocSelect docSub = (DocSelect)mapDef;
							foreach (DocSelectItem dsi in docSub.Selects)
							{
								queue.Enqueue(dsi);
							}
						}
						else if (!sort.ContainsKey(docItem.Name))
						{
							if (this.m_included == null || this.m_included.ContainsKey(mapDef))
							{
								sort.Add(docItem.Name, docItem.Name);
							}
						}
					}
				}

				writer.WriteLine("\t<!-- base global wrapper declaration for atomic simple types (for embeded base schema definitions) -->");
				foreach (string docItem in sort.Values)
				{
					DocObject mapDef = null;
					if (map.TryGetValue(docItem, out mapDef) && mapDef is DocType)
					{
						if (this.m_included == null || this.m_included.ContainsKey(mapDef))
						{
							writer.Write(FormatTypeWrapper((DocType)mapDef, map));
						}
					}
				}

				writer.WriteLine("</xs:schema>");

				writer.Flush();

				stream.Position = 0;
				System.IO.StreamReader reader = new System.IO.StreamReader(stream);
				return reader.ReadToEnd();
			}
		}
	}
}
