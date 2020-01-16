// Name:        FormatEXP.cs
// Description: EXPRESS file importer/exporter
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;

using IfcDoc.Schema.DOC;

namespace IfcDoc.Format.EXP
{
	internal class FormatEXP :
		IDisposable,
		IComparer<string>
	{
		string m_filename;
		DocProject m_project;
		DocSchema m_schema; // optional: only capture specific schema
		DocModelView[] m_views;
		Dictionary<DocObject, bool> m_included;
		bool m_oneof = true; // always use ONEOF, even if only one subtype -- was False for IFC2x3 and IFC4 final; True for IFC4 TC1.

		public FormatEXP(DocProject docProject, DocSchema docSchema, DocModelView[] modelviews, string filename)
		{
			this.m_project = docProject;
			this.m_schema = docSchema;
			this.m_views = modelviews;
			this.m_filename = filename;

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

		/// <summary>
		/// Replaces references to schemas with longform schema name, e.g. IFC4.
		/// </summary>
		/// <param name="expression">The expression to read</param>
		/// <param name="schemaidentifier">The schema identifier to use, such as 'IFC4'</param>
		/// <returns></returns>
		private string MakeLongFormExpression(string expression, string schemaidentifier)
		{
			if (expression == null)
				return null;

			// if schema defined, then short-form specific to schema; otherwise long-form
			if (this.m_schema != null)
				return expression;

			string replace = "'" + schemaidentifier.ToUpper() + ".";
			foreach (DocSection docSection in this.m_project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					string find = "'" + docSchema.Name.ToUpper() + '.';
					if (expression.Contains(find))
					{
						expression = expression.Replace(find, replace);
					}
				}

			}

			return expression.Trim();
		}

		public void Save()
		{
			SortedList<string, DocDefined> mapDefined = new SortedList<string, DocDefined>(this);
			SortedList<string, DocEnumeration> mapEnum = new SortedList<string, DocEnumeration>(this);
			SortedList<string, DocSelect> mapSelect = new SortedList<string, DocSelect>(this);
			SortedList<string, DocEntity> mapEntity = new SortedList<string, DocEntity>(this);
			SortedList<string, DocFunction> mapFunction = new SortedList<string, DocFunction>(this);
			SortedList<string, DocGlobalRule> mapRule = new SortedList<string, DocGlobalRule>(this);

			SortedList<string, DocObject> mapGeneral = new SortedList<string, DocObject>();

			foreach (DocSection docSection in this.m_project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					//if (this.m_schema == null || this.m_schema == docSchema)
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

									if (!mapGeneral.ContainsKey(docType.Name))
									{
										mapGeneral.Add(docType.Name, docType);
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
									if (!mapGeneral.ContainsKey(docEnt.Name))
									{
										mapGeneral.Add(docEnt.Name, docEnt);
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
			}

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

				string schemaid = this.m_project.GetSchemaIdentifier();
				if (this.m_schema != null)
				{
					schemaid = this.m_schema.Name;
				}

				string org = "buildingSMART International Limited";
				System.Globalization.CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
				System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-GB");
				writer.Write("" +
"(*\r\n" +
"Copyright by:\r\n" +
org + ", 1996-" + DateTime.UtcNow.Year + "\r\n" +
"\r\n" +
"Any technical documentation made available by " + org + "\r\n" +
"is the copyrighted work of " + org + " and is owned by the \r\n" +
org + ". It may be photocopied, used in software development, \r\n" +
"or translated into another computer language without prior written consent from \r\n" +
org + " provided that full attribution is given. \r\n" +
"Prior written consent is required if changes are made to the technical specification.\r\n" +
"\r\n" +
"This material is delivered to you as is and " + org + " makes \r\n" +
"no warranty of any kind with regard to it, including, but not limited to, the implied \r\n" +
"warranties as to its accuracy or fitness for a particular purpose. Any use of the \r\n" +
"technical documentation or the information contained therein is at the risk of the user. \r\n" +
"Documentation may include technical or other inaccuracies or typographical errors. \r\n" +
org + " shall not be liable for errors contained therein or \r\n" +
"for incidental consequential damages in connection with the furnishing, performance or use \r\n" +
"of the material. The information contained in this document is subject to change without notice.\r\n" +
"\r\n" +
"Issue date:\r\n" +
DateTime.Today.ToLongDateString() + "\r\n" + //"December 27, 2012\r\n" +
"\r\n" +
"*)\r\n" +
"\r\n");
				System.Threading.Thread.CurrentThread.CurrentCulture = currentCulture;
				writer.WriteLine("SCHEMA " + schemaid.ToUpper() + ";");
				writer.WriteLine();

				if (this.m_schema != null)
				{
					// references
					foreach (DocSchemaRef docSchemaRef in this.m_schema.SchemaRefs)
					{
						writer.Write("REFERENCE FROM ");
						writer.WriteLine(docSchemaRef.Name);
						writer.WriteLine("(");

						foreach (DocDefinitionRef docDefRef in docSchemaRef.Definitions)
						{
							writer.Write("  ");
							writer.Write(docDefRef.Name);

							if (docDefRef != docSchemaRef.Definitions[docSchemaRef.Definitions.Count - 1])
							{
								writer.Write(",");
							}
							writer.WriteLine();
						}

						writer.WriteLine(");");
						writer.WriteLine();
					}
				}

				// stripped optional applicable if MVD is used
				if (this.m_included != null)
				{
					writer.WriteLine("TYPE IfcStrippedOptional = BOOLEAN;");
					writer.WriteLine("END_TYPE;");
					writer.WriteLine();
				}

				// defined types
				foreach (DocDefined docDef in mapDefined.Values)
				{
					if (this.m_schema == null || this.m_schema.Types.Contains(docDef))
					{

						writer.Write("TYPE ");
						writer.Write(docDef.Name);
						writer.Write(" = ");

						if (docDef.Aggregation != null)
						{
							WriteExpressAggregation(writer, docDef.Aggregation);
						}

						writer.Write(docDef.DefinedType);

						string length = "";
						if (docDef.Length > 0)
						{
							length = "(" + docDef.Length.ToString() + ")";
						}
						else if (docDef.Length < 0)
						{
							int len = -docDef.Length;
							length = "(" + len.ToString() + ") FIXED";
						}
						writer.Write(length);

						writer.WriteLine(";");

						if (docDef.WhereRules.Count > 0)
						{
							writer.WriteLine(" WHERE");
							foreach (DocWhereRule where in docDef.WhereRules)
							{
								writer.Write("\t");
								writer.Write(where.Name);
								writer.Write(" : ");
								writer.Write(MakeLongFormExpression(where.Expression, schemaid));
								writer.WriteLine(";");
							}
						}

						writer.WriteLine("END_TYPE;");
						writer.WriteLine();
					}
				}

				// enumerations
				foreach (DocEnumeration docEnum in mapEnum.Values)
				{
					if (this.m_schema == null || this.m_schema.Types.Contains(docEnum))
					{

						writer.Write("TYPE ");
						writer.Write(docEnum.Name);
						writer.Write(" = ENUMERATION OF");
						writer.WriteLine();

						for (int i = 0; i < docEnum.Constants.Count; i++)
						{
							DocConstant docConst = docEnum.Constants[i];
							if (i == 0)
							{
								writer.Write("\t(");
							}
							else
							{
								writer.Write("\t,");
							}

							writer.Write(docConst.Name);

							if (i == docEnum.Constants.Count - 1)
							{
								writer.WriteLine(");");
							}
							else
							{
								writer.WriteLine();
							}
						}

						writer.WriteLine("END_TYPE;");
						writer.WriteLine();
					}
				}

				// selects
				foreach (DocSelect docSelect in mapSelect.Values)
				{
					if (this.m_schema == null || this.m_schema.Types.Contains(docSelect))
					{
						writer.Write("TYPE ");
						writer.Write(docSelect.Name);
						writer.Write(" = SELECT");
						writer.WriteLine();


						SortedList<string, DocSelectItem> sortSelect = new SortedList<string, DocSelectItem>(this);
						foreach (DocSelectItem docSelectItem in docSelect.Selects)
						{
							if (!sortSelect.ContainsKey(docSelectItem.Name))
							{
								sortSelect.Add(docSelectItem.Name, docSelectItem);
							}
							else
							{
								this.ToString();
							}
						}

						int nSelect = 0;
						for (int i = 0; i < sortSelect.Keys.Count; i++)
						{
							DocSelectItem docConst = sortSelect.Values[i];

							DocObject docRefEnt = null;
							if (mapGeneral.TryGetValue(docConst.Name, out docRefEnt))
							{
								if (this.m_included == null || this.m_included.ContainsKey(docRefEnt))
								{
									if (nSelect == 0)
									{
										writer.Write("\t(");
									}
									else
									{
										writer.WriteLine();
										writer.Write("\t,");
									}
									nSelect++;

									writer.Write(docConst.Name);
								}
							}
						}

						writer.WriteLine(");");

						writer.WriteLine("END_TYPE;");
						writer.WriteLine();
					}
				}

				// entities
				foreach (DocEntity docEntity in mapEntity.Values)
				{
					if (this.m_schema == null || this.m_schema.Entities.Contains(docEntity))
					{
						writer.Write("ENTITY ");
						writer.Write(docEntity.Name);

						if ((docEntity.IsAbstract))
						{
							writer.WriteLine();
							writer.Write(" ABSTRACT");
						}

						// build up list of subtypes from other schemas
						SortedList<string, DocEntity> subtypes = new SortedList<string, DocEntity>(this); // sort to match Visual Express
						foreach (DocEntity eachent in mapEntity.Values)
						{
							if (eachent.BaseDefinition != null && eachent.BaseDefinition.Equals(docEntity.Name))
							{
								subtypes.Add(eachent.Name, eachent);
							}
						}
						if (subtypes.Count > 0)
						{
							StringBuilder sb = new StringBuilder();

							// Capture all subtypes, not just those within schema
							int countsub = 0;
							foreach (string ds in subtypes.Keys)
							{
								DocEntity refent = subtypes[ds];
								if (this.m_included == null || this.m_included.ContainsKey(refent))
								{
									countsub++;

									if (sb.Length != 0)
									{
										sb.Append("\r\n    ,");
									}

									sb.Append(ds);
								}
							}

							if (!docEntity.IsAbstract)
							{
								writer.WriteLine();
							}

							if (countsub > 1 || this.m_oneof)
							{
								writer.Write(" SUPERTYPE OF (ONEOF\r\n    (" + sb.ToString() + "))");
							}
							else if (countsub == 1)
							{
								writer.Write(" SUPERTYPE OF (" + sb.ToString() + ")");
							}
						}

						if (docEntity.BaseDefinition != null)
						{
							writer.WriteLine();
							writer.Write(" SUBTYPE OF (");
							writer.Write(docEntity.BaseDefinition);
							writer.Write(")");
						}


						writer.WriteLine(";");

						// direct attributes
						bool hasinverse = false;
						bool hasderived = false;
						foreach (DocAttribute attr in docEntity.Attributes)
						{
							if (attr.Inverse == null && attr.Derived == null)
							{
								writer.Write("\t");
								writer.Write(attr.Name);
								writer.Write(" : ");

								if (attr.IsOptional)
								{
									writer.Write("OPTIONAL ");
								}

								WriteExpressAggregation(writer, attr);

								if (this.m_included == null || this.m_included.ContainsKey(attr))
								{
									writer.Write(attr.DefinedType);
								}
								else
								{
									writer.Write("IfcStrippedOptional");
								}
								writer.WriteLine(";");
							}
							else if (attr.Inverse != null && attr.Derived == null)
							{
								DocObject docref = null;
								if (mapGeneral.TryGetValue(attr.DefinedType, out docref))
								{
									if (this.m_included == null || this.m_included.ContainsKey(docref))
									{
										hasinverse = true;
									}
								}
							}
							else if (attr.Derived != null)
							{
								hasderived = true;
							}
						}

						// derived attributes
						if (hasderived)
						{
							writer.WriteLine(" DERIVE");

							foreach (DocAttribute attr in docEntity.Attributes)
							{
								if (attr.Derived != null)
								{
									// determine the superclass having the attribute                        
									DocEntity found = null;

									DocEntity super = docEntity;
									while (super != null && found == null && super.BaseDefinition != null)
									{
										super = mapEntity[super.BaseDefinition] as DocEntity;
										if (super != null)
										{
											foreach (DocAttribute docattr in super.Attributes)
											{
												if (docattr.Name.Equals(attr.Name))
												{
													// found class
													found = super;
													break;
												}
											}
										}
									}

									writer.Write("\t");
									if (found != null)
									{
										// overridden attribute
										writer.Write("SELF\\");
										writer.Write(found.Name);
										writer.Write(".");
									}

									writer.Write(attr.Name);
									writer.Write(" : ");

									WriteExpressAggregation(writer, attr);
									writer.Write(attr.DefinedType);

									writer.Write(" := ");
									writer.Write(attr.Derived);
									writer.WriteLine(";");
								}
							}

						}

						// inverse attributes
						if (hasinverse)
						{
							writer.WriteLine(" INVERSE");

							foreach (DocAttribute attr in docEntity.Attributes)
							{
								if (attr.Inverse != null && attr.Derived == null)
								{
									DocObject docref = null;
									if (mapGeneral.TryGetValue(attr.DefinedType, out docref))
									{
										if (this.m_included == null || this.m_included.ContainsKey(docref))
										{
											writer.Write("\t");
											writer.Write(attr.Name);
											writer.Write(" : ");

											WriteExpressAggregation(writer, attr);

											writer.Write(attr.DefinedType);
											writer.Write(" FOR ");
											writer.Write(attr.Inverse);
											writer.WriteLine(";");
										}
									}
								}
							}
						}

						// unique rules
						if (docEntity.UniqueRules.Count > 0)
						{
							writer.WriteLine(" UNIQUE");
							foreach (DocUniqueRule where in docEntity.UniqueRules)
							{
								writer.Write("\t");
								writer.Write(where.Name);
								writer.Write(" : ");
								foreach (DocUniqueRuleItem ruleitem in where.Items)
								{
									if (ruleitem != where.Items[0])
									{
										writer.Write(", ");
									}

									writer.Write(ruleitem.Name);
								}
								writer.WriteLine(";");
							}
						}

						// where rules
						if (docEntity.WhereRules.Count > 0)
						{
							writer.WriteLine(" WHERE");
							foreach (DocWhereRule where in docEntity.WhereRules)
							{
								writer.Write("\t");
								writer.Write(where.Name);
								writer.Write(" : ");
								writer.Write(MakeLongFormExpression(where.Expression, schemaid));
								writer.WriteLine(";");
							}
						}

						writer.WriteLine("END_ENTITY;");
						writer.WriteLine();
					}
				}

				// functions
				foreach (DocFunction docFunction in mapFunction.Values)
				{
					if (this.m_schema == null || this.m_schema.Functions.Contains(docFunction))
					{
						writer.Write("FUNCTION ");
						writer.WriteLine(docFunction.Name);
						writer.WriteLine(MakeLongFormExpression(docFunction.Expression, schemaid));
						writer.WriteLine("END_FUNCTION;");
						writer.WriteLine();
					}
				}

				// rules
				foreach (DocGlobalRule docRule in mapRule.Values)
				{
					if (this.m_schema == null || this.m_schema.GlobalRules.Contains(docRule))
					{
						writer.Write("RULE ");
						writer.Write(docRule.Name);
						writer.WriteLine(" FOR");
						writer.Write("\t(");
						writer.Write(docRule.ApplicableEntity);
						writer.WriteLine(");");

						writer.WriteLine(docRule.Expression);

						// where
						writer.WriteLine("    WHERE");
						foreach (DocWhereRule docWhere in docRule.WhereRules)
						{
							writer.Write("      ");
							writer.Write(docWhere.Name);
							writer.Write(" : ");
							writer.Write(MakeLongFormExpression(docWhere.Expression, schemaid));
							writer.WriteLine(";");
						}

						writer.WriteLine("END_RULE;");
						writer.WriteLine();
					}
				}

				writer.WriteLine("END_SCHEMA;");
			}
		}

		private static void WriteExpressAggregation(System.IO.StreamWriter writer, DocAttribute attr)
		{
			if (attr.AggregationType == 0)
				return;

			DocAttribute docAggregation = attr;
			while (docAggregation != null)
			{
				switch (docAggregation.AggregationType)
				{
					case 1:
						writer.Write("LIST ");
						break;

					case 2:
						writer.Write("ARRAY ");
						break;

					case 3:
						writer.Write("SET ");
						break;

					case 4:
						writer.Write("BAG ");
						break;
				}

				string lower = docAggregation.AggregationLower;
				if (String.IsNullOrEmpty(lower))
				{
					lower = "0";
				}

				if (docAggregation.AggregationUpper != null && docAggregation.AggregationUpper != "0")
				{
					writer.Write("[" + lower + ":" + docAggregation.AggregationUpper + "] OF ");
				}
				else if (docAggregation.AggregationLower != null)
				{
					writer.Write("[" + lower + ":?] OF ");
				}
				else
				{
					writer.Write("[0:?] OF ");
				}

				if (docAggregation.IsUnique)
				{
					// unique
					writer.Write("UNIQUE ");
				}

				// drill in
				docAggregation = docAggregation.AggregationAttribute;
			}
		}

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
	}
}
