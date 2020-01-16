// Name:        FormatSQL.cs
// Description: Generates schema definitions and sample data in tabular formats according to SQL and comma delimited text.
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2016 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	class FormatSQL :
		IFormatExtension
	{
		private void BuildFields(StringBuilder sb, DocEntity docEntity, Dictionary<string, DocObject> map)
		{
#if true //... make configurable... -- for now all classes are mapped as separate tables, where lookup must join each
			// super
			DocObject docSuper = null;
			if (docEntity.BaseDefinition != null && map.TryGetValue(docEntity.BaseDefinition, out docSuper) && docSuper is DocEntity)
			{
				BuildFields(sb, (DocEntity)docSuper, map);
			}
#endif
			foreach (DocAttribute docAttr in docEntity.Attributes)
			{
				if (docAttr.GetAggregation() == DocAggregationEnum.NONE && docAttr.Inverse == null) // don't deal with lists -- separate tables
				{
					sb.Append(", ");
					sb.AppendLine();

					sb.Append("\t");
					sb.Append(docAttr.Name);
					sb.Append(" ");

					DocObject docRef = null;

					if (docAttr.DefinedType == null || !map.TryGetValue(docAttr.DefinedType, out docRef) || docRef is DocDefined)
					{
						string deftype = docAttr.DefinedType;
						if (docRef is DocDefined)
						{
							DocDefined docDef = (DocDefined)docRef;
							deftype = docDef.DefinedType;
						}
						switch (deftype)
						{
							case "BOOLEAN":
								sb.Append(" BIT");
								break;

							case "INTEGER":
								sb.Append(" INTEGER");
								break;

							case "REAL":
								sb.Append(" FLOAT");
								break;

							case "BINARY":
								sb.Append(" TEXT");
								break;

							case "STRING":
							default:
								sb.Append(" TEXT");
								break;
						}
					}
					else if (docRef is DocEntity) //... docselect...
					{
						// if non-rooted, then embed as XML...

						sb.Append(" INTEGER"); // oid
					}
					else if (docRef is DocSelect)
					{
						sb.Append(" INTEGER"); // oid... and type...
					}
					else if (docRef is DocEnumeration)
					{
						sb.Append(" VARCHAR");
					}
				}
			}

		}

		public string FormatEntity(DocEntity docEntity, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("CREATE TABLE ");
			sb.Append(docEntity.Name);
			sb.Append(" (oid INTEGER");

			BuildFields(sb, docEntity, map);

			sb.Append(");");
			sb.AppendLine();

			foreach (DocAttribute docAttr in docEntity.Attributes)
			{
				if (docAttr.GetAggregation() != DocAggregationEnum.NONE && docAttr.Inverse == null)
				{
					sb.AppendLine();
					sb.Append("CREATE TABLE ");
					sb.Append(docEntity.Name);
					sb.Append("_");
					sb.Append(docAttr.Name);
					sb.Append(" (source INTEGER, sequence INTEGER, target INTEGER);");
				}
			}

			return sb.ToString();
		}

		public string FormatEnumeration(DocEnumeration docEnumeration, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			return null; // nothing to define
		}

		public string FormatSelect(DocSelect docSelect, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			return null; // nothing to define
		}

		public string FormatDefined(DocDefined docDefined, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			return null; // nothing to define
		}

		public string FormatDefinitions(DocProject docProject, DocPublication docPublication, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();
			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocType docType in docSchema.Types)
					{
						bool use = true;
						if (included != null)
						{
							use = false;
							included.TryGetValue(docType, out use);
						}

						if (use)
						{
							if (docType is DocDefined)
							{
								DocDefined docDefined = (DocDefined)docType;
								string text = this.FormatDefined(docDefined, map, included);
								sb.AppendLine(text);
							}
							else if (docType is DocSelect)
							{
								DocSelect docSelect = (DocSelect)docType;
								string text = this.FormatSelect(docSelect, map, included);
								sb.AppendLine(text);
							}
							else if (docType is DocEnumeration)
							{
								DocEnumeration docEnumeration = (DocEnumeration)docType;
								string text = this.FormatEnumeration(docEnumeration, map, included);
								sb.AppendLine(text);
							}
						}
					}

					foreach (DocEntity docEntity in docSchema.Entities)
					{
						bool use = true;
						if (included != null)
						{
							use = false;
							included.TryGetValue(docEntity, out use);
						}

						if (use)
						{
							string text = this.FormatEntity(docEntity, map, included);
							sb.AppendLine(text);
						}
					}
				}

			}

			return sb.ToString();
		}

		public string FormatDataConcept(DocProject docProject, DocPublication docPublication, DocExchangeDefinition docExchange, Dictionary<string, DocObject> map, Dictionary<string, Type> typemap, Dictionary<long, SEntity> instances, SEntity root, bool markup, DocModelView docView, DocConceptRoot docRoot, DocTemplateUsage docConcept)
		{
			StringBuilder sb = new StringBuilder();

			string table = docConcept.Items[0].GetParameterValue("Table");
			string query = docConcept.Items[0].GetParameterValue("Reference");

			sb.AppendLine("<h4>" + docConcept.Name + "</h4>");
			sb.AppendLine("<table class=\"gridtable\">");

			List<string> colstyles = new List<string>();
			List<string> colformat = new List<string>();
			List<CvtValuePath> colmaps = new List<CvtValuePath>();

			// generate header row
			sb.AppendLine("<tr>");
			foreach (DocTemplateItem docItem in docConcept.Items)
			{
				string name = docItem.GetParameterValue("Name");
				string disp = "#" + docItem.GetColor().ToArgb().ToString("X8"); //docItem.GetParameterValue("Color");docItem.GetParameterValue("Color");
				string expr = docItem.GetParameterValue("Reference");
				string form = docItem.GetParameterValue("Format");

				string style = "";
				if (!String.IsNullOrEmpty(disp))
				{
					style = " style=\"background-color:" + disp + ";\"";
				}
				colstyles.Add(style);

				string format = "";
				if (!String.IsNullOrEmpty(form))
				{
					format = form;
				}
				colformat.Add(format);

				string desc = "";
				CvtValuePath valpath = CvtValuePath.Parse(expr, map);
				colmaps.Add(valpath);
				if (valpath != null)
				{
					desc = /*valpath.GetDescription(map) + "&#10;&#10;" + */valpath.ToString().Replace("\\", "&#10;");
				}

				sb.Append("<th><a href=\"../../schema/views/" + DocumentationISO.MakeLinkName(docView) + "/" + DocumentationISO.MakeLinkName(docExchange) + ".htm#" + DocumentationISO.MakeLinkName(docConcept) + "\" title=\"" + desc + "\">");
				sb.Append(name);
				sb.Append("</a></th>");
			};
			sb.AppendLine("</tr>");

			// generate data rows
			List<DocModelRule> trace = new List<DocModelRule>();

			foreach (SEntity e in instances.Values)
			{
				string eachname = e.GetType().Name;
				if (docRoot.ApplicableEntity.IsInstanceOfType(e))
				{
					bool includerow = true;

					// if root has more complex rules, check them
					if (docRoot.ApplicableTemplate != null && docRoot.ApplicableItems.Count > 0)
					{
						includerow = false;

						// must check1
						foreach (DocTemplateItem docItem in docRoot.ApplicableItems)
						{
							foreach (DocModelRule rule in docRoot.ApplicableTemplate.Rules)
							{
								try
								{
									trace.Clear();
									bool? result = rule.Validate(e, docItem, typemap, trace, e, null, null);
									if (result == true && docRoot.ApplicableOperator == DocTemplateOperator.Or)
									{
										includerow = true;
										break;
									}
								}
								catch
								{
									docRoot.ToString();
								}
							}

							// don't yet support AND or other operators

							if (includerow)
								break;
						}
					}


					if (includerow)
					{
						StringBuilder sbRow = new StringBuilder();

						sbRow.Append("<tr>");
						int iCol = 0;
						foreach (DocTemplateItem docItem in docConcept.Items)
						{
							sbRow.Append("<td" + colstyles[iCol]);
							CvtValuePath valpath = colmaps[iCol];
							string format = colformat[iCol];

							iCol++;

							if (valpath != null)
							{
								string nn = docItem.GetParameterValue("Name");

								object value = valpath.GetValue(e, null);

								if (value == e)
								{
									value = e.GetType().Name;
								}
								else if (value is SEntity)
								{
									// use name
									FieldInfo fieldValue = value.GetType().GetField("Name");
									if (fieldValue != null)
									{
										value = fieldValue.GetValue(value);
									}
								}
								else if (value is System.Collections.IList)
								{
									System.Collections.IList list = (System.Collections.IList)value;
									StringBuilder sbList = new StringBuilder();
									foreach (object elem in list)
									{
										FieldInfo fieldName = elem.GetType().GetField("Name");
										if (fieldName != null)
										{
											object elemname = fieldName.GetValue(elem);
											if (elemname != null)
											{
												FieldInfo fieldValue = elemname.GetType().GetField("Value");
												if (fieldValue != null)
												{
													object elemval = fieldValue.GetValue(elemname);
													sbList.Append(elemval.ToString());
												}
											}
										}
										sbList.Append("; <br/>");
									}
									value = sbList.ToString();
								}
								else if (value is Type)
								{
									value = ((Type)value).Name;
								}

								if (!String.IsNullOrEmpty(format))
								{
									if (format.Equals("Required") && value == null)
									{
										includerow = false;
									}
								}

								if (value != null)
								{
									FieldInfo fieldValue = value.GetType().GetField("Value");
									if (fieldValue != null)
									{
										value = fieldValue.GetValue(value);
									}

									if (format != null && format.Equals("True") && (value == null || !value.ToString().Equals("True")))
									{
										includerow = false;
									}

									if (value is Double)
									{
										sbRow.Append(" align=\"right\">");

										sbRow.Append(((Double)value).ToString("N3"));
									}
									else if (value is List<Int64>)
									{
										sbRow.Append(">");

										// latitude or longitude
										List<Int64> intlist = (List<Int64>)value;
										if (intlist.Count >= 3)
										{
											sbRow.Append(intlist[0] + "° " + intlist[1] + "' " + intlist[2] + "\"");
										}
									}
									else if (value != null)
									{
										sbRow.Append(">");
										sbRow.Append(value.ToString()); // todo: html-encode
									}
								}
								else
								{
									sbRow.Append(">");
									sbRow.Append("&nbsp;");
								}
							}
							else
							{
								sbRow.Append(">");
							}

							sbRow.Append("</td>");
						}
						sbRow.AppendLine("</tr>");

						if (includerow)
						{
							sb.Append(sbRow.ToString());
						}
					}
				}
			}

			sb.AppendLine("</table>");
			sb.AppendLine("<br/>");

			return sb.ToString();
		}

		public void FormatData(Stream stream, DocProject docProject, DocPublication docPublication, DocExchangeDefinition docExchange, Dictionary<string, DocObject> map, Dictionary<string, Type> typemap, Dictionary<long, SEntity> instances, SEntity root, bool markup)
		{
			StringBuilder sb = new StringBuilder();

			foreach (DocModelView docView in docPublication.Views)
			{
				foreach (DocConceptRoot docRoot in docView.ConceptRoots)
				{
					// look for specific concept root dealing with mappings
					foreach (DocTemplateUsage docConcept in docRoot.Concepts)
					{
						if (docConcept.Definition != null && docConcept.Definition.Uuid.Equals(DocTemplateDefinition.guidTemplateMapping) && docConcept.Items.Count > 0)//...
						{
							bool included = true;

							if (docExchange != null)
							{
								included = false;
								// if exhcnage specified, check for inclusion
								foreach (DocExchangeItem docExchangeItem in docConcept.Exchanges)
								{
									if (docExchangeItem.Exchange == docExchange && docExchangeItem.Requirement == DocExchangeRequirementEnum.Mandatory)
									{
										included = true;
										break;
									}
								}
							}

							// check if there are any instances to populate table
							if (included)
							{
								included = false;
								foreach (SEntity e in instances.Values)
								{
									string eachname = e.GetType().Name;
									if (docRoot.ApplicableEntity.IsInstanceOfType(e))
									{
										included = true;
										break;
									}
								}
							}

							if (included)
							{
								string dataconcept = FormatDataConcept(docProject, docPublication, docExchange, map, typemap, instances, root, markup, docView, docRoot, docConcept);
								sb.Append(dataconcept);
							}
						}
					}
				}
			}
		}
	}
}
