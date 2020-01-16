// Name:        ValuePath.cs
// Description: Definitions for encapsulating IfcReference instances
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{

	/// <summary>
	/// Resolves attribute path expressions. 
	/// Used for importing or exporting data, constraining attributes, and approving attributes.
	/// </summary>
	public class CvtValuePath
	{
		DocDefinition m_type;
		DocAttribute m_property;
		string m_identifier; // identifier is specified
		bool m_vector; // query is for list, not single value
		CvtValuePath m_inner;

		/// <summary>
		/// Constructs a blank value path.
		/// </summary>
		public CvtValuePath()
		{
		}

		/// <summary>
		/// Constructs a value path with all parameters
		/// </summary>
		/// <param name="type"></param>
		/// <param name="property"></param>
		/// <param name="identifier"></param>
		/// <param name="inner"></param>
		public CvtValuePath(DocDefinition type, DocAttribute property, string identifier, CvtValuePath inner)
		{
			this.m_type = type;
			this.m_property = property;
			this.m_identifier = identifier;
			this.m_inner = inner;
		}

		internal static string GetParameterName(string syntax)
		{
			// quick check -- no escaping necessary as syntax is constrained
			if (syntax != null)
			{
				int iParamHead = syntax.IndexOf('@');
				if (iParamHead > 0)
				{
					int iParamTail = syntax.IndexOf(']', iParamHead + 1);
					if (iParamTail > iParamHead)
					{
						return syntax.Substring(iParamHead + 1, iParamTail - iParamHead - 1);
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Parses a value path from string in ISO-10303-11 (EXPRESS) format.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static CvtValuePath Parse(string value, Dictionary<string, DocObject> map)
		{
			if (value == null)
				return null;

			string[] tokens = value.Split(new char[] { '\\' }); //???// don't remove empty entries -- if it ends in backslash, then indicates type identifier

			CvtValuePath rootpath = null;
			CvtValuePath outerpath = null;
			foreach (string token in tokens)
			{
				CvtValuePath valuepath = new CvtValuePath();

				string[] parts = token.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 1 && map.ContainsKey(parts[0]))
				{
					valuepath.Type = map[parts[0]] as DocDefinition;
					if (valuepath.Type != null && parts.Length == 2)
					{
						string propname = parts[1];
						int bracket = propname.IndexOf('[');
						if (bracket >= 0)
						{
							string content = propname.Substring(bracket + 1, propname.Length - bracket - 2);
							if (content.StartsWith("'") && content.EndsWith("'"))
							{
								// indexed by name                                
								valuepath.Identifier = content.Substring(1, content.Length - 2);
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

							propname = propname.Substring(0, bracket);
						}

						if (valuepath.Type is DocEntity)
						{
							DocEntity docEntity = (DocEntity)valuepath.Type;
							valuepath.Property = docEntity.ResolveAttribute(propname, map);
						}
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
		public DocDefinition Type
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
		public DocAttribute Property
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
		public CvtValuePath InnerPath
		{
			get
			{
				return this.m_inner;
			}
			internal set
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

			if (this.m_property.GetAggregation() != DocAggregationEnum.NONE)
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

			if (this.m_inner != null)
			{
				this.m_inner.AppendString(sb);
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
			}

			object value = null;

			if (this.m_property.PropertyType != null &&
				this.m_property.PropertyType.IsGenericType &&
				typeof(System.Collections.IEnumerable).IsAssignableFrom(this.m_property.PropertyType) &&
				IsEntity(this.m_property.PropertyType))
			// (typeof(SEntity).IsAssignableFrom(this.m_property.PropertyType.GetGenericArguments()[0]) || this.m_property.PropertyType.GetGenericArguments()[0].IsInterface))
			{
				System.Collections.IEnumerable list = (System.Collections.IEnumerable)this.m_property.GetValue(target, null);

				// if expecting array, then return it.
				if (this.m_vector && (this.m_identifier == null && this.m_inner == null))
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
					int listindex = 0; // identify by 1-based numeric index within list
					if (!String.IsNullOrEmpty(this.m_identifier) && Int32.TryParse(this.m_identifier, out listindex) && listindex > 0 && list is System.Collections.IList)// && listindex <= list.Count)
					{
						System.Collections.IList listlist = (System.Collections.IList)list;
						if (listindex <= listlist.Count)
						{
							object eachelem = listlist[listindex - 1];

							if (this.m_inner != null && IsEntity(eachelem))
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

					foreach (object eachelem in list)
					{
						// derived class may have its own specific property (e.g. IfcSIUnit, IfcConversionBasedUnit)
						if (!String.IsNullOrEmpty(this.m_identifier))
						{
							Type eachtype = eachelem.GetType();

							// special cases for properties and quantities
							if (eachtype.Name.Equals("IfcRelDefinesByProperties"))
							{
								PropertyInfo fieldRelatingPropertyDefinition = eachtype.GetProperty("RelatingPropertyDefinition");
								object ifcPropertySet = fieldRelatingPropertyDefinition.GetValue(eachelem);
								if (ifcPropertySet != null)
								{
									Type typePropertySet = ifcPropertySet.GetType();
									PropertyInfo fieldName = typePropertySet.GetProperty("Name");
									object ifcLabel = fieldName.GetValue(ifcPropertySet);
									if (ifcLabel != null)
									{
										PropertyInfo fieldValue = ifcLabel.GetType().GetProperty("Value");
										if (fieldValue != null)
										{
											string sval = fieldValue.GetValue(ifcLabel) as string;
											if (this.m_identifier.Equals(sval))
											{
												// matches!
												if (this.m_inner != null)
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
								}
							}
							else
							{
								// fall back on Name field for properties or quantities
								PropertyInfo fieldName = eachtype.GetProperty("Name");
								if (fieldName != null)
								{
									object ifcLabel = fieldName.GetValue(eachelem);
									if (ifcLabel != null)
									{
										PropertyInfo fieldValue = ifcLabel.GetType().GetProperty("Value");
										if (fieldValue != null)
										{
											object ifcValue = fieldValue.GetValue(ifcLabel);
											while (fieldValue.PropertyType.IsValueType && !fieldValue.PropertyType.IsPrimitive)
											{
												fieldValue = fieldValue.PropertyType.GetProperty("Value");
												ifcValue = fieldValue.GetValue(ifcLabel);
											}

											string sval = ifcValue as string;
											if (this.m_identifier.Equals(sval))
											{
												// matches!
												if (this.m_inner != null)
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
								}
							}
						}
						else
						{
							// use first non-null item within inner reference
							if (this.m_inner != null && IsEntity(eachelem))
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

					return null; // not found
				}



			}
			else if (this.m_inner != null)
			{
				value = this.m_property.GetValue(target, null);
				if (IsEntity(value))
				{
					// hack for GSA
					if (value.GetType().Name.Equals("IfcIrregularTimeSeries"))
					{
						return value;
					}

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

		public static bool IsEntity(object obj)
		{
			if (obj == null)
				return false;

			if (obj is System.Collections.IEnumerable || obj is ValueType) // not a collection or a string or value type
				return false;

			return true; // class or interface
		}

		/// <summary>
		/// Extracts description of referenced data, using properties, quantities, and attributes.
		/// </summary>
		/// <param name="mapEntity"></param>
		/// <param name="docView">Optional model view, for retrieving more specific descriptions such as for ports</param>
		/// <returns></returns>
		public string GetDescription(Dictionary<string, DocObject> mapEntity, DocModelView docView)
		{
			string desc = null;
			CvtValuePath valpath = this;

			if (valpath != null &&
				valpath.Property != null &&
				valpath.Property.Name.Equals("IsDefinedBy") &&
				valpath.InnerPath != null && valpath.InnerPath.Type.Name.Equals("IfcRelDefinesByProperties") &&
				valpath.Identifier != null)
			{
				DocObject docPset = null;
				mapEntity.TryGetValue(valpath.Identifier, out docPset);

				if (docPset is DocPropertySet)
				{
					DocProperty docProp = ((DocPropertySet)docPset).GetProperty(valpath.InnerPath.InnerPath.Identifier);
					if (docProp != null)
					{
						desc = docProp.Documentation;// localize??

						if (String.IsNullOrEmpty(docProp.Documentation))
						{
							this.ToString();
						}
					}
				}
				else if (docPset is DocQuantitySet)
				{
					DocQuantity docProp = ((DocQuantitySet)docPset).GetQuantity(valpath.InnerPath.InnerPath.Identifier);
					if (docProp != null)
					{
						desc = docProp.Documentation;// localize??

						if (String.IsNullOrEmpty(docProp.Documentation))
						{
							this.ToString();
						}
					}
				}
			}
			else if (valpath != null &&
				valpath.Property != null &&
				valpath.Property.Name.Equals("HasAssignments") &&
				valpath.InnerPath != null && valpath.InnerPath.Type.Name.Equals("IfcRelAssignsToControl") &&
				valpath.InnerPath.InnerPath != null && valpath.InnerPath.InnerPath.Type.Name.Equals("IfcPerformanceHistory"))
			{
				DocObject docPset = null;
				if (mapEntity.TryGetValue(valpath.InnerPath.InnerPath.Identifier, out docPset))
				{
					DocProperty docProp = ((DocPropertySet)docPset).GetProperty(valpath.InnerPath.InnerPath.InnerPath.InnerPath.Identifier);
					if (docProp != null)
					{
						desc = docProp.Documentation;// localize??

						if (docProp.Documentation == null)
						{
							DocLocalization docLoc = docProp.GetLocalization(null);
							if (docLoc != null)
							{
								desc = docLoc.Documentation;
							}
						}
					}
				}

				this.ToString();
			}
			else if (valpath != null &&
				valpath.Property != null &&
				valpath.Property.Name.Equals("HasPropertySets") &&
				valpath.InnerPath != null && valpath.InnerPath.Type.Name.Equals("IfcPropertySet") && !string.IsNullOrEmpty(valpath.Identifier))
			{
				DocObject docPset = null;
				mapEntity.TryGetValue(valpath.Identifier, out docPset);

				if (docPset is DocPropertySet)
				{
					DocProperty docProp = ((DocPropertySet)docPset).GetProperty(valpath.InnerPath.Identifier);
					if (docProp != null)
					{
						desc = docProp.Documentation;// localize??

						if (String.IsNullOrEmpty(docProp.Documentation))
						{
							this.ToString();
						}
					}
				}
			}
			else if (valpath != null &&
				valpath.Property != null &&
				valpath.Property.Name.Equals("Material") &&
				valpath.InnerPath != null && valpath.InnerPath.Type.Name.Equals("IfcMaterial") &&
				valpath.InnerPath.InnerPath != null && valpath.InnerPath.InnerPath.Type.Name.Equals("IfcMaterialProperties"))
			{
				DocObject docPset = null;
				mapEntity.TryGetValue(valpath.InnerPath.Identifier, out docPset);

				if (docPset is DocPropertySet)
				{
					DocProperty docProp = ((DocPropertySet)docPset).GetProperty(valpath.InnerPath.InnerPath.Identifier);
					if (docProp != null)
					{
						desc = docProp.Documentation;
					}
				}
			}
			else if (valpath != null &&
				valpath.Property != null &&
				valpath.Property.Name.Equals("HasProperties") &&
				valpath.InnerPath != null && valpath.InnerPath.Type.Name.Equals("IfcMaterialProperties"))
			{
				DocObject docPset = null;
				mapEntity.TryGetValue(valpath.Identifier, out docPset);

				if (docPset is DocPropertySet)
				{
					DocProperty docProp = ((DocPropertySet)docPset).GetProperty(valpath.InnerPath.Identifier);
					if (docProp != null)
					{
						desc = docProp.Documentation;
					}
				}
			}
			else if (valpath != null &&
				valpath.Property != null &&
				valpath.Property.Name.Equals("IsNestedBy") &&
				valpath.InnerPath != null && valpath.InnerPath.InnerPath != null && valpath.InnerPath.InnerPath != null)
			{
				CvtValuePath pathInner = valpath.InnerPath.InnerPath;
				if (pathInner.Type != null && pathInner.Type.Name.Equals("IfcDistributionPort"))
				{
					string portname = valpath.InnerPath.Identifier;
					if (pathInner.Property != null && pathInner.Property.Name.Equals("IsDefinedBy"))
					{
						// lookup description of property at port
						DocObject docPset = null;
						mapEntity.TryGetValue(pathInner.Identifier, out docPset);

						if (docPset is DocPropertySet)
						{
							DocProperty docProp = ((DocPropertySet)docPset).GetProperty(pathInner.InnerPath.InnerPath.Identifier);
							if (docProp != null)
							{
								desc = portname + ": " + docProp.Documentation;
							}
						}

					}
					else
					{
						desc = portname;

						// lookup description of port
						Guid guidPortNesting = new Guid("bafc93b7-d0e2-42d8-84cf-5da20ee1480a");
						if (docView != null)
						{
							foreach (DocConceptRoot docRoot in docView.ConceptRoots)
							{
								if (docRoot.ApplicableEntity == valpath.Type)
								{
									foreach (DocTemplateUsage docConcept in docRoot.Concepts)
									{
										if (docConcept.Definition != null && docConcept.Definition.Uuid == guidPortNesting)
										{
											foreach (DocTemplateItem docItem in docConcept.Items)
											{
												if (docItem.Name != null && docItem.Name.Equals(portname))
												{
													desc = docItem.Documentation;
													break;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}

			if (desc == null)
			{
				while (valpath != null && valpath.InnerPath != null && valpath.InnerPath.Property != null)
				{
					valpath = valpath.InnerPath;
				}
				if (valpath != null && valpath.Property != null)
				{
					desc = valpath.Property.Documentation;
					if (String.IsNullOrEmpty(desc))
					{
						desc = "";
					}
				}
				else if (valpath != null)
				{
					desc = "The IFC class identifier indicating the subtype of object.";
				}
			}

			// clear out any notes
			int block = desc.IndexOf("<blockquote");
			if (block != -1)
			{
				desc = desc.Substring(0, block);
			}

			return desc;
		}

		public static CvtValuePath FromTemplateDefinition(DocTemplateDefinition dtd, DocProject docProject)
		{
			if (dtd.Rules.Count > 0 && dtd.Rules[0] is DocModelRuleAttribute)
			{
				DocModelRuleAttribute docRuleAtt = (DocModelRuleAttribute)dtd.Rules[0];

				DocEntity docEnt = docProject.GetDefinition(dtd.Type) as DocEntity;
				if (docEnt != null)
				{
					CvtValuePath pathInner = null;
					if (docRuleAtt.Rules.Count > 0 && docRuleAtt.Rules[0] is DocModelRuleEntity)
					{
						pathInner = FromModelRule((DocModelRuleEntity)docRuleAtt.Rules[0], docProject);
					}

					DocAttribute docAtt = docEnt.ResolveAttribute(docRuleAtt.Name, docProject);
					string identifier = null;

					if (docRuleAtt.Name.Equals("IsDefinedBy"))
					{
						// hack for compat
						docRuleAtt.ToString();

						// look for identifier
						if (docRuleAtt.Rules.Count > 0)
						{
							try
							{
								DocModelRule rule0 = docRuleAtt.Rules[0];
								if (rule0.Rules.Count > 0)
								{
									DocModelRule rule1 = rule0.Rules[0];
									if (rule1.Rules.Count > 0)
									{
										DocModelRule rule2 = rule1.Rules[0];
										if(rule2.Rules.Count > 1)
										{
											DocModelRule rule3 = rule2.Rules[1];
											if(rule3.Rules.Count > 0)
											{
												DocModelRule rule4 = rule3.Rules[0];
												if (rule4.Rules.Count > 0)
												{
													DocModelRuleConstraint docRuleIndexCon = rule4.Rules[0] as DocModelRuleConstraint;
													if (docRuleIndexCon != null)
													{
														if (docRuleIndexCon.Expression is DocOpStatement)
														{
															DocOpStatement docOpStatement = (DocOpStatement)docRuleIndexCon.Expression;
															if (docOpStatement.Value != null)
															{
																identifier = docOpStatement.Value.ToString();
																if (identifier.StartsWith("'") && identifier.EndsWith("'"))
																{
																	identifier = identifier.Substring(1, identifier.Length - 2);
																}
															}
														}
													}
												}
											}	
										}
									}
								}
							}
							catch
							{

							}
						}
					}

					CvtValuePath pathRoot = new CvtValuePath(docEnt, docAtt, identifier, pathInner);
					return pathRoot;
				}
			}

			return null;
		}

		private static CvtValuePath FromModelRule(DocModelRuleEntity docRuleEntity, DocProject docProject)
		{
			DocDefinition docDef = docProject.GetDefinition(docRuleEntity.Name);
			DocAttribute docAtt = null;
			string identifier = null;
			CvtValuePath pathInner = null;

			if (docDef is DocEntity && docRuleEntity.Rules.Count > 0 && docRuleEntity.Rules[0] is DocModelRuleAttribute)
			{
				DocModelRuleAttribute docRuleAtt = (DocModelRuleAttribute)docRuleEntity.Rules[0];
				DocEntity docEnt = (DocEntity)docDef;
				docAtt = docEnt.ResolveAttribute(docRuleAtt.Name, docProject);

				if (docRuleAtt.Rules.Count > 0 && docRuleAtt.Rules[0] is DocModelRuleEntity)
				{
					DocModelRuleEntity docRuleInner = (DocModelRuleEntity)docRuleAtt.Rules[0];
					pathInner = FromModelRule(docRuleInner, docProject);


					// look for identifier
					if (docRuleInner.Rules.Count > 1 && docRuleInner.Rules[1] is DocModelRuleAttribute)
					{
						DocModelRuleAttribute docRuleIndexAtt = (DocModelRuleAttribute)docRuleInner.Rules[1];
						if (docRuleIndexAtt.Rules.Count > 0)
						{
							DocModelRuleEntity docRuleIndexEnt = (DocModelRuleEntity)docRuleIndexAtt.Rules[0];
							if (docRuleIndexEnt.Rules.Count > 0 && docRuleIndexEnt.Rules[0] is DocModelRuleConstraint)
							{
								DocModelRuleConstraint docRuleIndexCon = (DocModelRuleConstraint)docRuleIndexEnt.Rules[0];
								if (docRuleIndexCon.Expression is DocOpStatement)
								{
									DocOpStatement docOpStatement = (DocOpStatement)docRuleIndexCon.Expression;
									if (docOpStatement.Value != null)
									{
										identifier = docOpStatement.Value.ToString();
										if (identifier.StartsWith("'") && identifier.EndsWith("'"))
										{
											identifier = identifier.Substring(1, identifier.Length - 2);
										}
									}
								}
							}
						}
					}
				}
			}

			CvtValuePath pathOuter = new CvtValuePath(docDef, docAtt, identifier, pathInner);
			return pathOuter;
		}

		public DocTemplateDefinition ToTemplateDefinition()
		{
			DocTemplateDefinition docTemplate = new DocTemplateDefinition();
			DocModelRuleEntity docRule = this.ToModelRule();
			if (docRule != null && docRule.Rules.Count > 0 && docRule.Rules[0] is DocModelRuleAttribute)
			{
				DocModelRuleAttribute docRuleAttr = (DocModelRuleAttribute)docRule.Rules[0];

				docTemplate.Type = docRule.Name;
				docTemplate.Rules.Add(docRuleAttr);
				docRuleAttr.ParentRule = null;
			}
			return docTemplate;
		}

		private DocModelRuleEntity ToModelRule()
		{
			if (this.Type == null)
				return null;

			DocModelRuleEntity docRuleEntity = new DocModelRuleEntity();
			docRuleEntity.Name = this.Type.Name;

			if (this.Property != null)
			{
				DocModelRuleAttribute docRuleAttr = new DocModelRuleAttribute();
				docRuleAttr.Name = this.Property.Name;
				docRuleEntity.Rules.Add(docRuleAttr);
				docRuleAttr.ParentRule = docRuleEntity;

				if (this.InnerPath != null)
				{
					DocModelRuleEntity docInner = this.InnerPath.ToModelRule();
					if (docInner != null)
					{
						docRuleAttr.Rules.Add(docInner);
						docInner.ParentRule = docRuleAttr;
					}

					if (this.Identifier != null)
					{
						// traverse to the default attribute

						DocModelRuleEntity docApplicableRule = docInner;
						if (this.InnerPath.Type.Name.Equals("IfcRelDefinesByProperties"))
						{
							// hack for back compat
							DocModelRuleAttribute docPsetAttr = docInner.Rules[0] as DocModelRuleAttribute;
							DocModelRuleEntity docPsetEnt = docPsetAttr.Rules[0] as DocModelRuleEntity;

							docApplicableRule = docPsetEnt;
						}

						DocModelRuleAttribute docWhereAttr = new DocModelRuleAttribute();
						docWhereAttr.Name = "Name";//... dynamically check...
						docApplicableRule.Rules.Add(docWhereAttr);
						docWhereAttr.ParentRule = docApplicableRule;

						DocModelRuleEntity docWhereEnt = new DocModelRuleEntity();
						docWhereEnt.Name = "IfcLabel";//... dynamically check...
						docWhereAttr.Rules.Add(docWhereEnt);
						docWhereEnt.ParentRule = docWhereAttr;

						DocModelRuleConstraint docRuleConstraint = new DocModelRuleConstraint();

						// general case
						docWhereEnt.Rules.Add(docRuleConstraint);
						docRuleConstraint.ParentRule = docWhereEnt;


						DocOpLiteral oplit = new DocOpLiteral();
						oplit.Operation = DocOpCode.LoadString;
						oplit.Literal = this.Identifier;

						DocOpStatement op = new DocOpStatement();
						op.Operation = DocOpCode.CompareEqual;
						op.Value = oplit;

						DocOpReference opref = new DocOpReference();
						opref.Operation = DocOpCode.NoOperation; // ldfld...
						opref.EntityRule = docWhereEnt;
						op.Reference = opref;

						docRuleConstraint.Expression = op;
					}
				}

			}


			return docRuleEntity;
		}
	}



}
