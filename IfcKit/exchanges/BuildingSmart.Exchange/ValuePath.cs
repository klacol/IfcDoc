using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace BuildingSmart.Exchange
{

	/// <summary>
	/// Resolves attribute path expressions. 
	/// Used for importing or exporting data, constraining attributes, and approving attributes.
	/// </summary>
	public class ValuePath
	{
		Type m_type;
		PropertyInfo m_property;
		string m_identifier; // identifier is specified
		int[] m_position; // index is specified
		bool m_vector; // query is for list, not single value
		ValuePath m_inner;

		/// <summary>
		/// Constructs a blank value path.
		/// </summary>
		public ValuePath()
		{
		}

		/// <summary>
		/// Constructs a value path with all parameters
		/// </summary>
		/// <param name="type"></param>
		/// <param name="property"></param>
		/// <param name="identifier"></param>
		/// <param name="inner"></param>
		public ValuePath(Type type, PropertyInfo property, string identifier, ValuePath inner)
		{
			this.m_type = type;
			this.m_property = property;
			this.m_identifier = identifier;
			this.m_inner = inner;
		}

		/// <summary>
		/// Parses a value path from string in ISO-10303-11 (EXPRESS) format.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static ValuePath Parse(Assembly assembly, string value)
		{
			if (value == null)
				return null;

			string[] tokens = value.Split(new char[] { '\\' }); //???// don't remove empty entries -- if it ends in backslash, then indicates type identifier

			ValuePath rootpath = null;
			ValuePath outerpath = null;
			foreach (string token in tokens)
			{
				ValuePath valuepath = new ValuePath();

				string[] parts = token.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 1)
				{
					foreach (Type t in assembly.GetTypes())
					{
						if (parts[0] == t.Name)
						{
							valuepath.Type = t;
							break;
						}
					}

					if (valuepath.Type != null && parts.Length == 2)
					{
						string propname = parts[1];
						int bracket = propname.IndexOf('[');
						if (bracket >= 0)
						{
							string content = propname.Substring(bracket + 1, propname.Length - bracket - 2);
							int position = 0;
							if (content.StartsWith("'") && content.EndsWith("'"))
							{
								// indexed by name                                
								valuepath.Identifier = content.Substring(1, content.Length - 2);
							}
							else if (content.StartsWith("\"") && content.EndsWith("\""))
							{
								// indexed by name                                
								valuepath.Identifier = content.Substring(1, content.Length - 2);
							}
							else if (Int32.TryParse(content, out position))
							{
								valuepath.Position = new Int32[] { position };
							}
							else if (content.StartsWith("@"))
							{
								// indexed by parameter for each line, e.g. value identifies column by name when importing/exporting spreadsheet
								valuepath.Identifier = content;
							}
							else if (content.Length == 0)
							{
								valuepath.Vector = true;
							}
							else if (content == "*")
							{
								// do nothing
							}
							else
							{
								// assume string but without quites
								valuepath.Identifier = content;
							}

							propname = propname.Substring(0, bracket);
						}

						valuepath.Property = valuepath.Type.GetProperty(propname);
					}

				}

				// chain
				if (outerpath != null)
				{
					outerpath.InnerPath = valuepath;
				}
				else
				{
					rootpath = valuepath;
				}

				outerpath = valuepath;
			}

			// avoid empty head link
			if (rootpath.Type == null && rootpath.Property == null && rootpath.InnerPath != null)
			{
				rootpath = rootpath.InnerPath;
			}

			return rootpath;
		}

		/// <summary>
		/// The type of object for which property applies, or NULL to indicate the type is defined by parameter.
		/// </summary>
		public Type Type
		{
			get
			{
				return this.m_type;
			}
			private set
			{
				this.m_type = value;
			}
		}

		/// <summary>
		/// The property to query, or NULL to indicate casting to specified type.
		/// </summary>
		public PropertyInfo Property
		{
			get
			{
				return this.m_property;
			}
			private set
			{
				this.m_property = value;
			}
		}

		/// <summary>
		/// The identifier of an item within a collection by Name, or NULL to indicate any item in collection.
		/// </summary>
		public string Identifier
		{
			get
			{
				return this.m_identifier;
			}
			private set
			{
				this.m_identifier = value;
			}
		}

		public int[] Position
		{
			get
			{
				return this.m_position;
			}
			private set
			{
				this.m_position = value;
			}
		}

		/// <summary>
		/// True indicates query is for multiple items; False indicates single scalar item.
		/// </summary>
		public bool Vector
		{
			get
			{
				return this.m_vector;
			}
			private set
			{
				this.m_vector = value;
			}
		}

		/// <summary>
		/// The inner chain of path elements.
		/// </summary>
		public ValuePath InnerPath
		{
			get
			{
				return this.m_inner;
			}
			private set
			{
				this.m_inner = value;
			}
		}

		/// <summary>
		/// Formats the value path into an expression.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			this.AppendString(sb);
			return sb.ToString();
		}

		/// <summary>
		/// Internal function for building expression string
		/// </summary>
		/// <param name="sb"></param>
		private void AppendString(StringBuilder sb)
		{
			sb.Append("\\");

			if (this.m_type == null)
			{
				// represents type query, so just backslash
				return;
			}

			sb.Append(this.m_type.Name);
			if (this.m_property == null)
			{
				// represents type cast, so return
				return;
			}

			sb.Append(".");
			sb.Append(this.m_property.Name);

			if (typeof(System.Collections.IEnumerable).IsAssignableFrom(this.m_property.PropertyType))
			{
				sb.Append("[");
				if (!this.m_vector)
				{
					if (this.m_identifier != null)
					{
						if (this.m_identifier.StartsWith("@"))
						{
							sb.Append(this.m_identifier); // @ is a reserved character indicating parameter substitution
						}
						else
						{
							sb.Append("'");
							sb.Append(this.m_identifier);
							sb.Append("'");
						}
					}
					else
					{
						sb.Append("*");
					}
				}
				sb.Append("]");
			}
			else if (this.m_identifier != null)
			{
				// qualify a value by name, e.g. IfcRepresentationMap
				sb.Append("('");
				sb.Append(this.m_identifier);
				sb.Append("')");
			}

			if (this.m_inner != null)
			{
				this.m_inner.AppendString(sb);
			}
		}

		// TimC: there's no longer an explicit SEntity base class for entity-based types, so this replacement function used for code transition
		private static bool IsEntity(object o)
		{
			if (o == null)
				return false;

			Type t = o.GetType();
			if (t.IsValueType || o is string)
				return false;

			if (typeof(System.Collections.IEnumerable).IsInstanceOfType(o))
				return false;

			if (t.IsInterface)
				return true;

			return true;
		}

		private static bool IsEntityType(Type t)
		{
			if (t.IsValueType)
				return false;

			if (t == typeof(string))
				return false;

			if (typeof(IEnumerable).IsAssignableFrom(t))
				return false;

			return true;
		}

		private static bool CollectionContains(IEnumerable col, object elem)
		{
			foreach (object o in col)
			{
				if (o == elem)
					return true;
			}

			return false;
		}

		private static void CollectionAdd(IEnumerable col, object elem)
		{
			if (col is IList)
			{
				((IList)col).Add(elem);
			}
			else
			{
				MethodInfo method = col.GetType().GetMethod("Add");
				if (method != null)
				{
					method.Invoke(col, new object[] { elem }); // perf!!
				}
			}
		}

		private static PropertyInfo GetPropertyDefault(Type t)
		{
			PropertyInfo propinfo = t.GetProperty("Name");
			if (propinfo != null)
				return propinfo;

			// otherwise, find the first property of type IfcIdentifier (e.g. RepresentationIdentifier)
			//...

			return null;
		}

		/// <summary>
		/// Sets value referenced by path.
		/// </summary>
		/// <param name="target">The relative object for which to set the value.</param>
		/// <param name="value">The value to set on an object along the expression path.</param>
		/// <param name="parameters">Optional parameters for substitution; if a parameter value is null is will be populated with the created instance.</param>
		public void SetValue(object target, object value, Dictionary<string, object> parameters)
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (this.m_type == null)
			{
				return;
			}

			if (this.m_property == null)
			{
				return;
			}

			if (!this.m_type.IsInstanceOfType(target))
			{
				throw new ArgumentException("target is not an instance of the expected type.", "target", null);
			}

			try
			{
				object thisvalue = this.m_property.GetValue(target, null);

				if (this.m_property.PropertyType.IsGenericType &&
					typeof(IEnumerable).IsAssignableFrom(this.m_property.PropertyType) &&
					IsEntityType(this.m_property.PropertyType.GetGenericArguments()[0]))
				{
					IEnumerable list = (IEnumerable)thisvalue;
					object colitem = null;
					foreach (object eachelem in list)
					{
						// derived class may have its own specific property (e.g. IfcSIUnit, IfcConversionBasedUnit)
						if (this.m_identifier != null)
						{
							// find of specific identifier
							Type eachtype = eachelem.GetType();
							PropertyInfo propElem = GetPropertyDefault(eachtype);
							if (propElem != null)
							{
								object eachname = propElem.GetValue(eachelem, null);

								if (this.m_identifier.StartsWith("@"))
								{
									// find by parameter
									if (parameters != null && parameters.TryGetValue(this.m_identifier.Substring(1), out colitem))
									{
										break;
									}
								}
								else if (eachname != null && this.m_identifier.Equals(eachname.ToString()))
								{
									// yes -- drill in
									colitem = eachelem;
									break;
								}
							}
						}
						else if (this.m_inner != null)
						{
							// find first that matches type
							if (this.m_inner.Type == null || this.m_inner.Type.IsInstanceOfType(eachelem))
							{
								colitem = eachelem;
								break;
							}
						}
						else
						{
							// find first
							colitem = eachelem;
							break;
						}
					}

					// now tunnel through
					if (this.m_inner != null)
					{
						if (colitem == null && this.m_inner.Type != null)
						{
							if (colitem == null)
							{
								colitem = Activator.CreateInstance(this.m_inner.Type);

								// name the object
								if (this.m_identifier != null)
								{
									if (this.m_identifier.StartsWith("@") && parameters != null)
									{
										// record parameter
										string paramname = this.m_identifier.Substring(1);
										parameters[paramname] = colitem;
									}
									else
									{
										PropertyInfo propDefault = GetPropertyDefault(this.m_inner.Type);
										if (propDefault != null)
										{
											propDefault.SetValue(colitem, value);
										}
									}
								}
							}
							else if (this.m_property.PropertyType.IsClass &&
								this.m_property.PropertyType.IsGenericType &&
								(this.m_property.PropertyType.GetGenericTypeDefinition() == typeof(ISet<>) || this.m_property.PropertyType.GetGenericTypeDefinition() == typeof(IList<>)))
							{
								CollectionAdd(list, colitem);
							}
						}

						if (colitem != null)
						{
							this.m_inner.SetValue(colitem, value, parameters);
						}
					}
					else
					{
						// reference to an existing object
						if (list.GetType().GetGenericArguments()[0].IsInstanceOfType(value))
						{
							if (!CollectionContains(list, value))
							{
								CollectionAdd(list, value);
							}
						}
					}
				}
				else if (this.m_inner != null)
				{
					// must allocate
					if (thisvalue == null && this.m_inner.Type != null)
					{
						Type convtype = this.m_inner.Type;
						if (convtype.IsValueType)
						{
							// convert type if type converter is defined
							if (convtype.IsDefined(typeof(TypeConverterAttribute), true))
							{
								TypeConverterAttribute attr = (TypeConverterAttribute)convtype.GetCustomAttributes(typeof(TypeConverterAttribute), true)[0];
								Type typeconv = Type.GetType(attr.ConverterTypeName);
								if (typeconv != null)
								{
									TypeConverter converter = (TypeConverter)Activator.CreateInstance(typeconv);
									value = converter.ConvertFrom(value);
									this.Property.SetValue(target, value);
								}
							}
						}
						else
						{
							if (thisvalue == null)
							{
								// get type from inner token
								thisvalue = Activator.CreateInstance(this.m_inner.Type);
							}
							this.Property.SetValue(target, thisvalue);
						}
					}

					if (IsEntity(thisvalue))
					{
						this.m_inner.SetValue(thisvalue, value, parameters);
					}
				}
				else
				{
					// get type converter, set value
					Type convtype = this.Property.PropertyType;
					if (convtype.IsInstanceOfType(value))
					{
						// set field instead to avoid perf hit                
						this.Property.SetValue(target, value);
					}
#if false
                    else if (convtype == typeof(IList<IfcLabel>))
                    {
                        // address lines
                        if (value != null)
                        {
                            SList<IfcLabel> listlabel = (SList<IfcLabel>)this.Field.GetValue(target);
                            listlabel.Clear();
                            string strval = value.ToString();
                            string[] parts = strval.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string part in parts)
                            {
                                listlabel.Add(new IfcLabel(part));
                            }
                        }
                    }
#endif
					else
					{
						// value
						if (convtype.IsGenericType && convtype.GetGenericTypeDefinition() == typeof(Nullable<>))
						{
							// special case for Nullable types
							convtype = convtype.GetGenericArguments()[0];
						}

						// convert type if type converter is defined
						if (convtype.IsDefined(typeof(TypeConverterAttribute), true))
						{
							TypeConverterAttribute attr = (TypeConverterAttribute)convtype.GetCustomAttributes(typeof(TypeConverterAttribute), true)[0];
							Type typeconv = Type.GetType(attr.ConverterTypeName);
							if (typeconv != null)
							{
								TypeConverter converter = (TypeConverter)Activator.CreateInstance(typeconv);
								value = converter.ConvertFrom(value);
								this.Property.SetValue(target, value);
							}
						}
						else
						{
							// log warning
							System.Diagnostics.Debug.WriteLine("CvtValuePath::SetValue() - " + this.ToString() + " - " + convtype.Name + " has no type converter defined.");
						}
					}
				}
			}
			catch (Exception e)
			{
				// log it...
				System.Diagnostics.Debug.WriteLine("CvtValuePath::SetValue exception: " + e.ToString());
			}
		}

		/// <summary>
		/// Gets value referenced by path.
		/// </summary>
		/// <param name="target">The relative object to retrieve the value.</param>
		/// <param name="parameters">Optional parameters for substitution.</param>
		/// <returns>The value on the object along the expression path.</returns>
		public object GetValue(object target, Dictionary<string, object> parameters)
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (this.m_type == null)
			{
				return target.GetType();
			}

			if (!this.m_type.IsInstanceOfType(target))
				return null; // doesn't apply

			if (this.m_property == null)
			{
				return target; // for general case, if no attribute specified, then return object itself
							   //return target.GetType(); // need some way to extract type directly such as for COBie
			}

			object value = null;

			if (this.m_property.PropertyType.IsGenericType &&
				typeof(System.Collections.IEnumerable).IsAssignableFrom(this.m_property.PropertyType) &&
				IsEntityType(this.m_property.PropertyType.GetGenericArguments()[0]))
			{
				System.Collections.IEnumerable list = (System.Collections.IEnumerable)this.m_property.GetValue(target, null);

				// if expecting array, then return it.
				//if (this.m_vector)
				if (this.m_vector || (this.m_identifier == null && this.m_inner == null)) // 2014-01-20: RICS export of door schedules to show quantity
				{
					return list;
				}
				else if (this.m_identifier != null && this.m_identifier.StartsWith("@") && parameters == null)
				{
					// return filtered list based on expected type -- may be none if no compatible types -- e.g. COBie properties only return IfcPropertyEnumeratedValue
					if (this.InnerPath != null && this.InnerPath.Type != null)
					{
						List<object> listFilter = null;
						foreach (object ent in list)
						{
							if (this.InnerPath.Type.IsInstanceOfType(ent))
							{
								if (listFilter == null)
								{
									listFilter = new List<object>();
								}
								listFilter.Add(ent);
							}
						}

						return listFilter;
					}
					else
					{
						return list;
					}
				}

				if (list != null)
				{
					foreach (object eachelem in list)
					{
						// derived class may have its own specific property (e.g. IfcSIUnit, IfcConversionBasedUnit)
						if (!String.IsNullOrEmpty(this.m_identifier))
						{
							Type eachtype = eachelem.GetType();
							DefaultPropertyAttribute[] attrs = (DefaultPropertyAttribute[])eachtype.GetCustomAttributes(typeof(DefaultPropertyAttribute), true);
							PropertyInfo propElem = null;
							if (attrs.Length > 0)
							{
								propElem = eachtype.GetProperty(attrs[0].Name);
							}
							else
							{
								propElem = eachtype.GetProperty("Name");

								if (propElem == null)
								{
									propElem = eachtype.GetProperty("RepresentationIdentifier");
								}
							}

							if (propElem != null)
							{
								object eachname = propElem.GetValue(eachelem, null); // IStepValueString, or Enum (e.g. IfcNamedUnit.UnitType)
								if (eachname != null)
								{
									if (eachname is ValueType)
									{
										FieldInfo[] fields = eachname.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
										if (fields.Length == 1)
										{
											eachname = fields[0].GetValue(eachname);
										}
									}

									if (this.m_identifier.StartsWith("@"))
									{
										// parameterized query -- substitute parameter
										if (parameters != null)
										{
											object specelem = null;
											if (parameters.TryGetValue(this.m_identifier.Substring(1), out specelem))
											{
												if (this.m_inner != null)
												{
													object eachvalue = this.m_inner.GetValue(specelem, parameters);
													return eachvalue; // return no matter what, since specific element was requested.
												}
												else
												{
													return specelem;
												}

											}
										}
										else
										{
											return null; // no parameters specified, so can't resolve value.
										}
									}
									else if (this.m_identifier.Equals(eachname.ToString()))
									{
										if (this.m_inner != null)
										{
											// yes -- drill in
											object eachvalue = this.m_inner.GetValue(eachelem, parameters);
											if (eachvalue != null)
											{
												return eachvalue; // if no value, keep going until compatible match is found
											}
										}
										else
										{
											return eachelem;
										}
									}
								}
							}
						}
						else if (this.m_inner != null)
						{
							object eachvalue = this.m_inner.GetValue(eachelem, parameters);
							if (eachvalue != null)
							{
								return eachvalue;
							}
						}
						else
						{
							return eachelem;
						}
					}
				}
			}
			else if (this.m_inner != null)
			{
				value = this.m_property.GetValue(target, null);
				if (IsEntity(value))
				{
					value = this.m_inner.GetValue(value, parameters);

					if (this.m_identifier != null && value != null)
					{
						// qualify the value
						Type eachtype = value.GetType();
						DefaultMemberAttribute[] attrs = (DefaultMemberAttribute[])eachtype.GetCustomAttributes(typeof(DefaultMemberAttribute), true);
						PropertyInfo propElem = null;
						if (attrs.Length > 0)
						{
							propElem = eachtype.GetProperty(attrs[0].MemberName);
						}
						else
						{
							propElem = eachtype.GetProperty("Name");
						}

						if (propElem != null)
						{
							object name = propElem.GetValue(value, null);
							if (name == null || !this.m_identifier.Equals(name.ToString()))
							{
								return null;
							}
						}
					}
				}
			}
			else
			{
				value = this.m_property.GetValue(target, null);
			}

			return value;
		}
	}
}