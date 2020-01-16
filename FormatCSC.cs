// Name:        FormatCSC.cs
// Description: C# Code Generator
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.CSharp;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;
using IfcDoc.Schema.SVG;

namespace IfcDoc.Format.CSC
{
	internal class FormatCSC : IDisposable,
		IFormatExtension
	{
		string m_filename;
		DocProject m_project;
		DocSchema m_schema;
		DocDefinition m_definition;
		Dictionary<string, DocObject> m_map;

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

		/// <summary>
		/// Given a .NET type, resolves to EXPRESS primitive if applicable, or else named type
		/// </summary>
		/// <param name="typeField"></param>
		/// <returns></returns>
		public static string GetExpressType(Type typeField)
		{
			if (typeField == typeof(string))
			{
				return "STRING";
			}
			else if (typeField == typeof(long) ||
				typeField == typeof(int))
			{
				return "INTEGER";
			}
			else if (typeField == typeof(double) ||
				typeField == typeof(float))
			{
				return "REAL";
			}
			else if (typeField == typeof(decimal))
			{
				return "NUMBER";
			}
			else if (typeField == typeof(bool))
			{
				return "BOOLEAN";
			}
			else if (typeField == typeof(bool?))
			{
				return "LOGICAL";
			}
			else if (typeField == typeof(byte[]))
			{
				return "BINARY";
			}
			else
			{
				return typeField.Name;
			}
		}

		/// <summary>
		/// Returns the native .NET type to use for a given EXPRESS type.
		/// </summary>
		/// <param name="expresstype"></param>
		/// <returns></returns>
		public static Type GetNativeType(string expresstype)
		{
			switch (expresstype)
			{
				case "STRING":
					return typeof(string);

				case "INTEGER":
					return typeof(long);

				case "REAL":
					return typeof(double);

				case "NUMBER":
					return typeof(decimal);

				case "LOGICAL":
					return typeof(bool?);

				case "BOOLEAN":
					return typeof(bool);

				case "BINARY":
				case "BINARY (32)":
					return typeof(byte[]);

			}

			return null;
		}

		/// <summary>
		/// Converts any native types into .NET types
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		private static string FormatIdentifier(string identifier)
		{
			Type typeNative = GetNativeType(identifier);
			if (typeNative != null)
			{
				if (typeNative.IsGenericType && typeNative.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					return typeNative.GetGenericArguments()[0].Name + "?";
				}

				return typeNative.Name;
			}

			return identifier;
		}

		/// <summary>
		/// Generates folder of definitions
		/// </summary>
		/// <param name="path"></param>
		public static void GenerateCode(DocProject project, string path, Dictionary<string, DocObject> map, DocCodeEnum options)
		{
			string schemaid = project.GetSchemaIdentifier();

			// write assembly version info
			using (StreamWriter writerAssm = new StreamWriter(path + @"\AssemblyInfo.cs", false))
			{
				WriteResource(writerAssm, "IfcDoc.assemblyinfo.txt");

				writerAssm.WriteLine("[assembly: AssemblyTitle(\"buildingSMART." + schemaid.ToUpper() + "\")]");

				string version = project.GetSchemaVersion();
				if (!String.IsNullOrEmpty(version))
				{
					writerAssm.WriteLine("[assembly: AssemblyVersion(\"" + version + "\")]");
					writerAssm.WriteLine("[assembly: AssemblyFileVersion(\"" + version + "\")]");
				}
			}

			using (StreamWriter writerProj = new StreamWriter(path + @"\buildingSMART." + schemaid.ToUpper() + ".csproj", false))
			{
				WriteResource(writerProj, "IfcDoc.csproj1.txt");
				writerProj.WriteLine("    <Compile Include=\"AssemblyInfo.cs\" />");

				foreach (DocSection docSection in project.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						string pathSchema = path + @"\" + docSchema.Name;
						if (!Directory.Exists(pathSchema))
						{
							Directory.CreateDirectory(pathSchema);
						}

						string pathSchemaValidation = pathSchema + @"\validation";
						if (!Directory.Exists(pathSchemaValidation))
						{
							Directory.CreateDirectory(pathSchemaValidation);
						}


						// future: static functions and global rules for schema
#if false
                        using (FormatCSC format = new FormatCSC(pathSchema + @"\schema.cs"))
                        {
                            using (StreamWriter writer = new StreamWriter(format.m_filename))
                            {
                                format.WriteHeader(writer);

                                writer.WriteLine("namespace BuildingSmart.IFC." + docSchema.Name);
                                writer.WriteLine("{");

                                writer.WriteLine("\tinternal static class Schema");
                                writer.WriteLine("\t{");

                                writer.WriteLine("// Note: In a future version, functions and global rules will be included here.");

                                foreach (DocFunction docFunction in docSchema.Functions)
                                {
                                    //...
                                }

                                foreach (DocGlobalRule docRule in docSchema.GlobalRules)
                                {
                                    //...
                                }

                                writer.WriteLine("\t}");
                                writer.WriteLine("}");
                            }

                            writerProj.WriteLine("    <Compile Include=\"" + docSchema.Name + "\\schema.cs\" />");
                        }
#endif

						// html for schema
						if ((options & DocCodeEnum.Documentation) != 0)
						{
							string filehtml = docSchema.Name + @"\schema.htm";
							using (StreamWriter writerHtml = new StreamWriter(path + @"\" + filehtml, false, Encoding.UTF8))
							{
								writerHtml.Write(docSchema.Documentation);
							}
						}

						// diagram for schema
						if ((options & DocCodeEnum.Diagrams) != 0)
						{
							// use SVG format
							string filesvg = docSchema.Name + @"\schema.svg";
							using (SchemaSVG formatSVG = new SchemaSVG(path + @"\" + filesvg, docSchema, project, DiagramFormat.UML))
							{
								formatSVG.Save();
							}
						}

						foreach (DocType docType in docSchema.Types)
						{
							string file = docSchema.Name + @"\" + docType.Name + ".cs";
							using (FormatCSC format = new FormatCSC(path + @"\" + file))
							{
								format.Instance = project;
								format.Schema = docSchema;
								format.Definition = docType;
								format.Map = map;
								format.Save();
							}

							if ((options & DocCodeEnum.Documentation) != 0)
							{
								string filehtml = docSchema.Name + @"\" + docType.Name + ".htm";
								using (StreamWriter writerHtml = new StreamWriter(path + @"\" + filehtml, false, Encoding.UTF8))
								{
									List<DocumentationISO.ContentRef> listFig = new List<DocumentationISO.ContentRef>();
									List<DocumentationISO.ContentRef> listTab = new List<DocumentationISO.ContentRef>();
									string doc = DocumentationISO.UpdateNumbering(docType.Documentation, listFig, listTab, docType);
									writerHtml.Write(doc);
								}
							}

							if ((options & DocCodeEnum.Functions) != 0)
							{
								if (docType is DocDefined)
								{
									DocDefined docDef = (DocDefined)docType;
									foreach (DocWhereRule docWhere in docDef.WhereRules)
									{
										string filehtml = docSchema.Name + @"\" + docType.Name + @"-" + docWhere.Name + ".htm";
										string fileexpr = docSchema.Name + @"\" + docType.Name + @"-" + docWhere.Name + ".exp";
										using (StreamWriter writerHtml = new StreamWriter(path + @"\" + filehtml, false, Encoding.UTF8))
										{
											writerHtml.Write(docWhere.Documentation);
										}
										using (StreamWriter writerHtml = new StreamWriter(path + @"\" + fileexpr, false, Encoding.UTF8))
										{
											writerHtml.Write(docWhere.Expression);
										}
									}
								}
							}

							writerProj.WriteLine("    <Compile Include=\"" + file + "\" />");
						}

						foreach (DocEntity docType in docSchema.Entities)
						{
							string file = docSchema.Name + @"\" + docType.Name + ".cs";
							using (FormatCSC format = new FormatCSC(path + @"\" + file))
							{
								format.Instance = project;
								format.Schema = docSchema;
								format.Definition = docType;
								format.Map = map;
								format.Save();

								if ((options & DocCodeEnum.Documentation) != 0)
								{
									string filehtml = docSchema.Name + @"\" + docType.Name + ".htm";
									using (StreamWriter writerHtml = new StreamWriter(path + @"\" + filehtml, false, Encoding.UTF8))
									{
										List<DocumentationISO.ContentRef> listFig = new List<DocumentationISO.ContentRef>();
										List<DocumentationISO.ContentRef> listTab = new List<DocumentationISO.ContentRef>();
										string doc = DocumentationISO.UpdateNumbering(docType.Documentation, listFig, listTab, docType);
										writerHtml.Write(doc);
									}
								}
							}

							if ((options & DocCodeEnum.Functions) != 0)
							{
								foreach (DocWhereRule docWhere in docType.WhereRules)
								{
									string filehtml = docSchema.Name + @"\validation\" + docType.Name + @"-" + docWhere.Name + ".htm";
									string fileexpr = docSchema.Name + @"\validation\" + docType.Name + @"-" + docWhere.Name + ".exp";
									using (StreamWriter writerHtml = new StreamWriter(path + @"\" + filehtml, false, Encoding.UTF8))
									{
										writerHtml.Write(docWhere.Documentation);
									}
									using (StreamWriter writerHtml = new StreamWriter(path + @"\" + fileexpr, false, Encoding.UTF8))
									{
										writerHtml.Write(docWhere.Expression);
									}
								}
							}

							writerProj.WriteLine("    <Compile Include=\"" + file + "\" />");
						}

						if ((options & DocCodeEnum.Functions) != 0)
						{
							foreach (DocFunction docFunction in docSchema.Functions)
							{
								string filehtml = docSchema.Name + @"\validation\" + docFunction.Name + ".htm";
								string fileexpr = docSchema.Name + @"\validation\" + docFunction.Name + ".exp";
								using (StreamWriter writerHtml = new StreamWriter(path + @"\" + filehtml, false, Encoding.UTF8))
								{
									writerHtml.Write(docFunction.Documentation);
								}
								using (StreamWriter writer = new StreamWriter(path + @"\" + fileexpr, false, Encoding.UTF8))
								{
									writer.Write("FUNCTION ");
									writer.WriteLine(docFunction.Name);
									writer.WriteLine(docFunction.Expression);
									writer.WriteLine("END_FUNCTION;");
								}
							}
						}

						// global rules???....
						if ((options & DocCodeEnum.Rules) != 0)
						{
							foreach (DocGlobalRule docRule in docSchema.GlobalRules)
							{
								string filehtml = docSchema.Name + @"\validation\" + docRule.Name + ".htm";
								string fileexpr = docSchema.Name + @"\validation\" + docRule.Name + ".exp";
								using (StreamWriter writerHtml = new StreamWriter(path + @"\" + filehtml, false, Encoding.UTF8))
								{
									writerHtml.Write(docRule.Documentation);
								}
								using (StreamWriter writer = new StreamWriter(path + @"\" + fileexpr, false, Encoding.UTF8))
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
										writer.Write(docWhere.Expression);
										writer.WriteLine(";");
									}

									writer.WriteLine("END_RULE;");
									writer.WriteLine();
								}
							}
						}
					}
				}

				// save properties
#if false
                using (FormatCSC format = new FormatCSC(path + @"\pset.cs"))
                {
                    format.Instance = project;
                    format.Map = map;
                    format.Save();
                }
#endif
				WriteResource(writerProj, "IfcDoc.csproj2.txt");
			}
		}

		public FormatCSC()
		{
			this.m_filename = null;
		}

		public FormatCSC(string filename)
		{
			this.m_filename = filename;
		}

		public DocProject Instance
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

		public DocSchema Schema
		{
			get
			{
				return this.m_schema;
			}
			set
			{
				this.m_schema = value;
			}
		}

		/// <summary>
		/// Optional definition to save, or null for all definitions in project.
		/// </summary>
		public DocDefinition Definition
		{
			get
			{
				return this.m_definition;
			}
			set
			{
				this.m_definition = value;
			}
		}

		public Dictionary<string, DocObject> Map
		{
			get
			{
				return this.m_map;
			}
			set
			{
				this.m_map = value;
			}
		}

		private void WriteHeader(StreamWriter writer)
		{
			writer.WriteLine("// This file may be edited manually or auto-generated using IfcKit at www.buildingsmart-tech.org.");
			writer.WriteLine("// IFC content is copyright (C) 1996-2018 BuildingSMART International Ltd.");
			writer.WriteLine();

			writer.WriteLine("using System;");
			writer.WriteLine("using System.Collections.Generic;");
			writer.WriteLine("using System.ComponentModel;");
			writer.WriteLine("using System.ComponentModel.DataAnnotations;");
			writer.WriteLine("using System.ComponentModel.DataAnnotations.Schema;");
			writer.WriteLine("using System.Runtime.InteropServices;"); // GuidAttribute
			writer.WriteLine("using System.Runtime.Serialization;");
			writer.WriteLine("using System.Xml.Serialization;"); // directives for attributes, elements, value types, etc.
			writer.WriteLine();

			WriteIncludes(writer);
		}

		private void WriteIncludes(StreamWriter writer)
		{
			// new implementation: track dependencies 
			SortedList<string, DocSchema> listSchema = new SortedList<string, DocSchema>();
			if (this.m_definition is DocEntity)
			{
				DocEntity docEnt = (DocEntity)this.m_definition;
				while (docEnt != null)
				{
					if (docEnt.BaseDefinition != null)
					{
						DocDefinition docTarget = this.m_project.GetDefinition(docEnt.BaseDefinition);
						if (docTarget != null)
						{
							DocSchema docSchema = this.m_project.GetSchemaOfDefinition(docTarget);
							if (!listSchema.ContainsValue(docSchema))
							{
								listSchema.Add(docSchema.Name, docSchema);
							}
						}
					}

					// selects
					foreach (DocObject obj in this.m_map.Values)
					{
						if (obj is DocSelect)
						{
							DocSelect docSelect = (DocSelect)obj;
							foreach (DocSelectItem docItem in docSelect.Selects)
							{
								if (docItem.Name != null && docItem.Name.Equals(docEnt.Name))
								{
									DocSchema docSchema = this.m_project.GetSchemaOfDefinition(docSelect);
									if (!listSchema.ContainsValue(docSchema))
									{
										listSchema.Add(docSchema.Name, docSchema);
									}
								}
							}
						}
					}

					foreach (DocAttribute docAttr in docEnt.Attributes)
					{
						DocObject docRef = null;
						if (this.m_map.TryGetValue(docAttr.DefinedType, out docRef))
						{
							DocSchema docSchema = this.m_project.GetSchemaOfDefinition((DocDefinition)docRef);
							if (!listSchema.ContainsValue(docSchema))
							{
								listSchema.Add(docSchema.Name, docSchema);
							}
						}
					}

					// recurse
					if (docEnt.BaseDefinition != null)
					{
						docEnt = this.m_project.GetDefinition(docEnt.BaseDefinition) as DocEntity;
					}
					else
					{
						docEnt = null;
					}
				}
			}
			else if (this.m_definition is DocDefined)
			{
				DocDefined docDef = (DocDefined)this.m_definition;
				if (docDef.DefinedType != null)
				{
					DocDefinition docTarget = this.m_project.GetDefinition(docDef.DefinedType);
					if (docTarget != null)
					{
						DocSchema docSchema = this.m_project.GetSchemaOfDefinition(docTarget);
						if (!listSchema.ContainsValue(docSchema))
						{
							listSchema.Add(docSchema.Name, docSchema);
						}
					}
				}
			}

			// don't include our own schema
			if (this.m_schema != null && listSchema.ContainsKey(this.m_schema.Name))
			{
				listSchema.Remove(this.m_schema.Name);
			}

			foreach (DocSchema docSchemaRef in listSchema.Values)
			{
				writer.WriteLine("using BuildingSmart.IFC." + docSchemaRef.Name + ";");
			}
			writer.WriteLine();

#if false
            // schema references
            if (this.m_schema != null)
            {
                // alphabetize and avoid redundancies (due to errors from originating content)
                SortedList<string, DocSchema> listSchema = new SortedList<string, DocSchema>();
                foreach (DocSchemaRef docRef in this.m_schema.SchemaRefs)
                {
                    DocSchema docSchemaRef = this.m_project.GetSchema(docRef.Name); // get exact case (not uppercase)
                    if (docSchemaRef != null && !listSchema.ContainsKey(docSchemaRef.Name))
                    {
                        listSchema.Add(docSchemaRef.Name, docSchemaRef);
                    }
                }

                foreach (DocSchema docSchemaRef in listSchema.Values)
                {
                    writer.WriteLine("using BuildingSmart.IFC." + docSchemaRef.Name + ";");
                }

                writer.WriteLine();
            }
#endif
		}

		public void Save()
		{
			string dirpath = System.IO.Path.GetDirectoryName(this.m_filename);
			if (!System.IO.Directory.Exists(dirpath))
			{
				System.IO.Directory.CreateDirectory(dirpath);
			}

			using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.m_filename))
			{
				this.WriteHeader(writer);

				if (this.m_definition != null)
				{
					writer.Write("namespace BuildingSmart.IFC");
					if (this.m_schema != null)
					{
						writer.Write("." + this.m_schema.Name);
					}
					writer.WriteLine();
					writer.WriteLine("{");

					if (this.m_definition is DocDefined)
					{
						DocDefined docDefined = (DocDefined)this.m_definition;
						string text = this.Indent(this.FormatDefined(docDefined, this.m_map, null), 1);
						writer.WriteLine(text);
					}
					else if (this.m_definition is DocSelect)
					{
						DocSelect docSelect = (DocSelect)this.m_definition;
						string text = this.Indent(this.FormatSelect(docSelect, this.m_map, null), 1);
						writer.WriteLine(text);
					}
					else if (this.m_definition is DocEnumeration)
					{
						DocEnumeration docEnumeration = (DocEnumeration)this.m_definition;
						string text = this.Indent(this.FormatEnumeration(docEnumeration, this.m_map, null), 1);
						writer.WriteLine(text);
					}
					else if (this.m_definition is DocEntity)
					{
						DocEntity docEntity = (DocEntity)this.m_definition;
						string text = this.Indent(this.FormatEntity(docEntity, this.m_map, null), 1);
						writer.WriteLine(text);
					}

					writer.WriteLine("}");
				}
				else
				{

					writer.WriteLine("namespace BuildingSmart.IFC.Properties");
					writer.WriteLine("{");
					writer.WriteLine();

					foreach (DocPropertyEnumeration docEnum in this.m_project.PropertyEnumerations)
					{
						writer.WriteLine("    /// <summary>");
						writer.WriteLine("    /// </summary>");
						writer.WriteLine("    public enum " + docEnum.Name);
						writer.WriteLine("    {");

						int counter = 0;
						foreach (DocPropertyConstant docConst in docEnum.Constants)
						{
							int num = 0;
							string id = docConst.Name.ToUpper().Trim('.').Replace('-', '_');
							switch (id)
							{
								case "OTHER":
									num = -1;
									break;

								case "NOTKNOWN":
									num = -2;
									break;

								case "UNSET":
									num = 0;
									break;

								default:
									counter++;
									num = counter;
									break;
							}

							if (id[0] >= '0' && id[0] <= '9')
							{
								id = "_" + id; // avoid numbers
							}

							writer.WriteLine("        /// <summary></summary>");
							writer.WriteLine("        " + docConst.Name + " = " + num + ",");
						}

						writer.WriteLine("    }");
						writer.WriteLine();
					}

					foreach (DocSection docSection in this.m_project.Sections)
					{
						foreach (DocSchema docSchema in docSection.Schemas)
						{
							foreach (DocPropertySet docPset in docSchema.PropertySets)
							{
								writer.WriteLine("    /// <summary>");
								if (docPset.Documentation != null)
								{
									writer.WriteLine("    /// " + docPset.Documentation.Replace('\r', ' ').Replace('\n', ' '));
								}
								writer.WriteLine("    /// </summary>");

								writer.WriteLine("    public class " + docPset.Name + " : Pset");
								writer.WriteLine("    {");

								foreach (DocProperty docProperty in docPset.Properties)
								{
									writer.WriteLine("        /// <summary>");
									if (docProperty.Documentation != null)
									{
										writer.WriteLine("        /// " + docProperty.Documentation.Replace('\r', ' ').Replace('\n', ' '));
									}
									writer.WriteLine("        /// </summary>");

									switch (docProperty.PropertyType)
									{
										case DocPropertyTemplateTypeEnum.P_SINGLEVALUE:
											writer.WriteLine("        public " + docProperty.PrimaryDataType + " " + docProperty.Name + " { get { return this.GetValue<" + docProperty.PrimaryDataType + ">(\"" + docProperty.Name + "\"); } set { this.SetValue<" + docProperty.PrimaryDataType + ">(\"" + docProperty.Name + "\", value); } }");
											break;

										case DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE:
											{
												string typename = docProperty.Enumeration.Name;
												writer.WriteLine("        public " + typename + " " + docProperty.Name + " { get { return this.GetValue<" + typename + ">(\"" + docProperty.Name + "\"); } set { this.SetValue<" + typename + ">(\"" + docProperty.Name + "\", value); } }");
											}
											break;

										case DocPropertyTemplateTypeEnum.P_BOUNDEDVALUE:
											writer.WriteLine("        public PBound<" + docProperty.PrimaryDataType + "> " + docProperty.Name + " { get { return this.GetBound<" + docProperty.PrimaryDataType + ">(\"" + docProperty.Name + "\"); } }");
											break;

										case DocPropertyTemplateTypeEnum.P_LISTVALUE:
											break;

										case DocPropertyTemplateTypeEnum.P_TABLEVALUE:
											writer.WriteLine("        public PTable<" + docProperty.PrimaryDataType + ", " + docProperty.SecondaryDataType + "> " + docProperty.Name + " { get { return this.GetTable<" + docProperty.PrimaryDataType + ", " + docProperty.SecondaryDataType + ">(\"" + docProperty.Name + "\"); } }");
											break;

										case DocPropertyTemplateTypeEnum.P_REFERENCEVALUE:
											if (docProperty.PrimaryDataType.Equals("IfcTimeSeries"))
											{
												string datatype = docProperty.SecondaryDataType;
												if (String.IsNullOrEmpty(datatype))
												{
													datatype = "IfcReal";
												}
												writer.WriteLine("        public PTimeSeries<" + datatype + "> " + docProperty.Name + " { get { return this.GetTimeSeries<" + datatype + ">(\"" + docProperty.Name + "\"); } }");
											}
											// ... TBD
											break;

										case DocPropertyTemplateTypeEnum.COMPLEX:
											//... TBD
											break;
									}
								}

								writer.WriteLine("    }");
								writer.WriteLine();
							}

							
						}
					}
				}

				writer.Flush();
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion

		/// <summary>
		/// Inserts tabs for each line
		/// </summary>
		/// <param name="text"></param>
		/// <param name="level"></param>
		/// <returns></returns>
		private string Indent(string text, int level)
		{
			for (int i = 0; i < level; i++)
			{
				text = "\t" + text;
				text = text.Replace("\r\n", "\r\n\t");
			}

			return text;
		}

		/// <summary>
		/// Builds syntax for referencing select
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="docEntity"></param>
		/// <param name="map"></param>
		/// <param name="included"></param>
		/// <param name="hasentry">Whether entries already listed; if not, then colon is added</param>
		private void BuildSelectEntries(StringBuilder sb, DocDefinition docEntity, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included, bool hasentry)
		{
			SortedList<string, DocSelect> listSelects = new SortedList<string, DocSelect>();
			foreach (DocObject obj in map.Values)
			{
				if (obj is DocSelect)
				{
					DocSelect docSelect = (DocSelect)obj;
					foreach (DocSelectItem docItem in docSelect.Selects)
					{
						if (docItem.Name != null && docItem.Name.Equals(docEntity.Name) && !listSelects.ContainsKey(docSelect.Name))
						{
							// found it; add it
							listSelects.Add(docSelect.Name, docSelect);
						}
					}
				}
			}
			DocSchema entitySchema = m_project.GetSchemaOfDefinition(docEntity);

			foreach (DocSelect docSelect in listSelects.Values)
			{
				if (docSelect == listSelects.Values[0] && !hasentry)
				{
					sb.AppendLine(" :");
				}
				else
				{
					sb.AppendLine(",");
				}

				// fully qualify reference inline (rather than in usings) to differentiate C# select implementation from explicit schema references in data model
				DocSchema docSchema = this.m_project.GetSchemaOfDefinition(docSelect);
				if (docSchema == entitySchema)
					sb.Append("\t");
				else
				{
					sb.Append("\tBuildingSmart.IFC.");
					sb.Append(docSchema.Name);
					sb.Append(".");
				}
				sb.Append(docSelect.Name);
			}
		}

		private static void BuildAttributeList(DocEntity docEntity, Dictionary<string, DocObject> map, List<DocAttribute> listAttr)
		{
			// recurse upwards -- base first
			DocObject docBase = null;
			if (docEntity.BaseDefinition != null && map.TryGetValue(docEntity.BaseDefinition, out docBase) && docBase is DocEntity)
			{
				DocEntity docBaseEntity = (DocEntity)docBase;
				BuildAttributeList(docBaseEntity, map, listAttr);
			}

			foreach (DocAttribute docAttr in docEntity.Attributes)
			{
				if (docAttr.Inverse == null && docAttr.Derived == null && !docAttr.IsOptional)
				{
					listAttr.Add(docAttr);
				}
			}
		}
		private string formatAttributeName(DocAttribute docAttribute)
		{
			if(string.Compare(docAttribute.Name, "Operator", true) == 0)
				return "_operator";
			return Char.ToLowerInvariant(docAttribute.Name[0]) + docAttribute.Name.Substring(1);
		}
		public string FormatEntity(DocEntity docEntity, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();

			using (CSharpCodeProvider prov = new CSharpCodeProvider())
			{
				sb.AppendLine("[Guid(\"" + docEntity.Uuid.ToString() + "\")]");

				sb.Append("public ");
				if (docEntity.IsAbstract)
				{
					sb.Append("abstract ");
				}
				sb.Append("partial class " + docEntity.Name);

				bool hasentry = false;
				if (!String.IsNullOrEmpty(docEntity.BaseDefinition))
				{
					sb.Append(" : ");
					sb.Append(docEntity.BaseDefinition);
					hasentry = true;
				}

				// implement any selects
				BuildSelectEntries(sb, docEntity, map, included, hasentry);

				sb.AppendLine();
				sb.AppendLine("{");

				// fields
				int order = 0;
				StringBuilder sbFields = new StringBuilder();
				StringBuilder sbProperties = new StringBuilder();
				StringBuilder sbConstructor = new StringBuilder(); // constructor parameters
				StringBuilder sbAssignment = new StringBuilder(); // constructor assignment of fields
				StringBuilder sbElemConstructor = new StringBuilder(); // default constructor
				foreach (DocAttribute docAttribute in docEntity.Attributes)
				{
					string type = FormatIdentifier(docAttribute.DefinedType);

					DocObject docRef = null;
					if (docAttribute.DefinedType != null)
					{
						map.TryGetValue(docAttribute.DefinedType, out docRef);
					}

					if (docAttribute.Derived != null)
					{
						// export as "new" property that hides base
						switch (docAttribute.GetAggregation())
						{
							case DocAggregationEnum.SET:
								sbProperties.AppendLine("\tpublic new ISet<" + type + "> " + docAttribute.Name + " { get { return null; } }");
								break;

							case DocAggregationEnum.LIST:
								sbProperties.AppendLine("\tpublic new IList<" + type + "> " + docAttribute.Name + " { get { return null; } }");
								break;

							default:
								if (docRef is DocDefined)
								{
									sbProperties.AppendLine("\tpublic new " + type + " " + docAttribute.Name + " { get { return new " + type + "(); } }");
								}
								else
								{
									if (string.Compare(type, "Int64") == 0 || string.Compare(type, "Double") == 0)
										sbProperties.AppendLine("\tpublic new " + type + " " + docAttribute.Name + " { get { return 0; } }");
									else
										sbProperties.AppendLine("\tpublic new " + type + " " + docAttribute.Name + " { get { return null; } }");
								}
								break;
						}
						sbProperties.AppendLine();
						// future: generate C# code for EXPRESS
					}
					else
					{
						bool inscope = false;

						if (included != null)
						{
							included.TryGetValue(docAttribute, out inscope);
						}
						else
						{
							inscope = true;
						}


						if (docAttribute.Inverse == null)
						{
							// System.Runtime.Serialization -- used by Windows Communication Foundation formatters to indicate data serialization inclusion and order
							sbFields.AppendLine("\t[DataMember(Order = " + order + ")] ");
							order++;
						}
						else if (inscope)
						{
							// System.ComponentModel.DataAnnotations for capturing inverse properties -- EntityFramework navigation properties
							sbFields.AppendLine("\t[InverseProperty(\"" + docAttribute.Inverse + "\")] ");
						}

						// xml configuration
						if (docAttribute.AggregationAttribute == null && (docRef is DocDefined || docRef is DocEnumeration))
						{
							sbFields.AppendLine("\t[XmlAttribute]");
						}
						else
						{
							switch (docAttribute.XsdFormat)
							{
								case DocXsdFormatEnum.Attribute: // e.g. IfcRoot.OwnerHistory -- only attribute has tag; element data type does not
									sbFields.AppendLine("\t[XmlElement]");
									break;

								case DocXsdFormatEnum.Element: // attribute has tag and referenced object instance(s) have tags
									sbFields.AppendLine("\t[XmlElement(\"" + docAttribute.DefinedType + "\")]"); // same as .Element, but skip attribute name (NOT XmlAttribute)
									break;

								case DocXsdFormatEnum.Hidden:
									sbFields.AppendLine("\t[XmlIgnore]");
									break;
							}
						}

						if (docAttribute.Inverse == null || inscope)
						{
							// documentation
							if (!String.IsNullOrEmpty(docAttribute.Documentation))
							{
								sbFields.Append("\t[Description(\""); // keep descriptions on one line
								string encodedoc = docAttribute.Documentation.Replace("\\", "\\\\"); // backslashes used for notes that relate to EXPRESS syntax
								encodedoc = encodedoc.Replace("\"", "\\\""); // escape any quotes
								encodedoc = encodedoc.Replace("\r", " "); // remove any return characters
								encodedoc = encodedoc.Replace("\n", " "); // remove any return characters
								sbFields.Append(encodedoc);

								//prov.GenerateCodeFromExpression(new CodePrimitiveExpression(docAttribute.Documentation), new StringWriter(sbFields), null); //... do this directly to avoid line splitting...
								sbFields.AppendLine("\")]");
							}

							if (docAttribute.Inverse == null && !docAttribute.IsOptional)
							{
								sbFields.AppendLine("\t[Required()]");
							}

							if (docAttribute.IsUnique)
							{
								sbFields.AppendLine("\t[CustomValidation(typeof(" + docEntity.Name + "), \"Unique\")]"); // extent via partial class for implementation
																														 // MS Entity Framework 6.1 supports IndexAttribute for this purpose, however above is used for now to avoid additional dependency
							}

							string optional = "";
							if (docAttribute.IsOptional && (docRef == null || docRef is DocDefined || docRef is DocEnumeration))
							{
								optional = "?";
							}

							int lower = 0;
							if (docAttribute.AggregationLower != null && Int32.TryParse(docAttribute.AggregationLower, out lower) && lower != 0)
							{
								sbFields.AppendLine("\t[MinLength(" + lower + ")]");
							}

							int upper = 0;
							if (docAttribute.AggregationUpper != null && Int32.TryParse(docAttribute.AggregationUpper, out upper) && upper != 0)
							{
								sbFields.AppendLine("\t[MaxLength(" + upper + ")]");
							}

							//sb.AppendLine("[Guid(\"" + docAttribute.Uuid.ToString() + "\")]");
							switch (docAttribute.GetAggregation())
							{
								case DocAggregationEnum.SET:
									if (docAttribute.Inverse == null && !docAttribute.IsOptional)
									{
										sbAssignment.AppendLine("\t\tthis." + docAttribute.Name + " = new HashSet<" + type + ">(" + formatAttributeName(docAttribute) + ");");
									}
									else
									{
										sbAssignment.AppendLine("\t\tthis." + docAttribute.Name + " = new HashSet<" + type + ">();");
									}
									sbFields.AppendLine("\tpublic ISet<" + type + "> " + docAttribute.Name + " { get; protected set; }");
									//sbProperties.AppendLine("\tpublic ISet<" + type + "> " + docAttribute.Name + " { get { return this." + docAttribute.Name + "; } }");
									break;

								case DocAggregationEnum.LIST:
									if (docAttribute.Inverse == null && !docAttribute.IsOptional)
									{
										sbAssignment.AppendLine("\t\tthis." + docAttribute.Name + " = new List<" + type + ">(" + formatAttributeName(docAttribute) + ");");
									}
									else
									{
										sbAssignment.AppendLine("\t\tthis." + docAttribute.Name + " = new List<" + type + ">();");
									}
									sbFields.AppendLine("\tpublic IList<" + type + "> " + docAttribute.Name + " { get; protected set; }");
									//sbProperties.AppendLine("\tpublic IList<" + type + "> " + docAttribute.Name + " { get { return this." + docAttribute.Name + "; } }");
									break;

								case DocAggregationEnum.ARRAY:
									if (docAttribute.Inverse == null && !docAttribute.IsOptional)
									{
										sbAssignment.AppendLine("\t\tthis." + docAttribute.Name + " = " + formatAttributeName(docAttribute) + ";");
									}
									sbFields.AppendLine("\tpublic " + type + "[] " + docAttribute.Name + " { get; set; }");
									//sbProperties.AppendLine("\tpublic " + type + "[] " + docAttribute.Name + " { get { return this." + docAttribute.Name + "; } }");
									break;

								default:
									if (docAttribute.Inverse == null && !docAttribute.IsOptional)
									{
										sbAssignment.AppendLine("\t\tthis." + docAttribute.Name + " = " + formatAttributeName(docAttribute) + ";");
									}
									sbFields.AppendLine("\tpublic " + type + optional + " " + docAttribute.Name + " { get; set; }");
									//sbProperties.AppendLine("\tpublic " + type + optional + " " + docAttribute.Name + " { get { return this._" + docAttribute.Name + "; } set { this._" + docAttribute.Name + " = value;} }");
									break;
							}

							// helper constructors for x/y, x/y/z
							if (docAttribute.Inverse == null && docAttribute.GetAggregation() == DocAggregationEnum.LIST && upper == 3 && docRef is DocDefined)
							{
								DocDefined docDefined = (DocDefined)docRef;
								Type typePrim = GetNativeType(docDefined.DefinedType);
								if (typePrim != null)
								{
									string primtype = typePrim.Name;

									if (lower >= 1 && lower < upper)
									{
										sbElemConstructor.AppendLine("\tpublic " + docEntity.Name + "(" + primtype + " x, " + primtype + " y) : this(new " + type + "[]{ new " + type + "(x), new " + type + "(y)})");
										sbElemConstructor.AppendLine("\t{");
										sbElemConstructor.AppendLine("\t}");
										sbElemConstructor.AppendLine();
									}

									if (upper == 3)
									{
										sbElemConstructor.AppendLine("\tpublic " + docEntity.Name + "(" + primtype + " x, " + primtype + " y, " + primtype + " z) : this(new " + type + "[]{ new " + type + "(x), new " + type + "(y), new " + type + "(z)})");
										sbElemConstructor.AppendLine("\t{");
										sbElemConstructor.AppendLine("\t}");
										sbElemConstructor.AppendLine();
									}
								}
							}

							// todo: support special collections and properties that keep inverse properties in sync...
							sbFields.AppendLine();
							//sbProperties.AppendLine();
						}
					}
				}

				sb.Append(sbFields.ToString());
				sb.AppendLine();

				// constructors

#if false // no default constructors anymore
                // default constructor
                sb.AppendLine("\tpublic " + docEntity.Name + "()");
                sb.AppendLine("\t{");
                sb.AppendLine("\t}");
                sb.AppendLine();
#endif

				// parameters for base constructor
				List<DocAttribute> listAttr = new List<DocAttribute>();
				BuildAttributeList(docEntity, map, listAttr);

				List<DocAttribute> listBase = new List<DocAttribute>();
				if (docEntity.BaseDefinition != null)
				{
					DocEntity docBase = (DocEntity)map[docEntity.BaseDefinition];
					BuildAttributeList(docBase, map, listBase);
				}

				string constructorvisibility = "public";
				if (docEntity.IsAbstract)
				{
					constructorvisibility = "protected";
				}


				// helper constructor -- expand fixed lists into separate parameters -- e.g. IfcCartesianPoint(IfcLengthMeasure, IfcLengthMeasure, IfcLengthMeasure)
				sb.Append("\t" + constructorvisibility + " " + docEntity.Name + "(");
				foreach (DocAttribute docAttr in listAttr)
				{
					if (docAttr != listAttr[0])
					{
						sb.Append(", ");
					}

					string type = FormatIdentifier(docAttr.DefinedType);
					sb.Append(type);

					DocObject docRef = null;
					if (docAttr.DefinedType != null)
					{
						map.TryGetValue(docAttr.DefinedType, out docRef);
					}

					if (docAttr.GetAggregation() != DocAggregationEnum.NONE)
					{
						sb.Append("[]");
					}
					else if (docAttr.IsOptional && (docRef == null || docRef is DocDefined || docRef is DocEnumeration))
					{
						sb.Append("?");
					}

					sb.Append(" " + formatAttributeName(docAttr));
					
				}
				sb.AppendLine(sbConstructor.ToString() + ")");

				if (listBase.Count > 0)
				{
					sb.Append("\t\t: base(");
					foreach (DocAttribute docAttr in listBase)
					{
						if (docAttr != listBase[0])
						{
							sb.Append(", ");
						}

						sb.Append(formatAttributeName(docAttr));
					}

					sb.AppendLine(")");
				}

				sb.AppendLine("\t{");
				sb.Append(sbAssignment.ToString());
				sb.AppendLine("\t}");
				sb.AppendLine();


				// if only a single list attribute, then expand x, y, z (include IfcCartesianPoint, NOT IfcSurfaceReinforcementArea)
				if (sbElemConstructor.Length > 0 && order == 1 && listBase.Count == 0)
				{
					sb.AppendLine(sbElemConstructor.ToString());
				}

				sb.Append(sbProperties.ToString());
				sb.AppendLine();

				// where rules -- for now, captures EXPRESS syntax -- tbd: convert to C# syntax
#if false //... convert to C# native syntax... use external .exp files for now
                foreach (DocWhereRule docWhere in docEntity.WhereRules)
                {
                    sb.AppendLine("\tprivate bool " + docWhere.Name + "()");
                    sb.AppendLine("\t{");
                    sb.Append("\t\treturn Validate(");

                    prov.GenerateCodeFromExpression(new CodePrimitiveExpression(docWhere.Expression), new StringWriter(sb), null);

                    sb.AppendLine(");");
                    sb.AppendLine("\t}");
                }
#endif

				// sb.AppendLine();

				//sb.AppendLine("// Exchange properties");
#if false
            // then, model views within scope
            foreach(DocModelView docView in this.m_project.ModelViews)
            {
                if (included == null || included.ContainsKey(docView)) // todo: make option when generating to indicate which views...
                {
                    foreach(DocConceptRoot docRoot in docView.ConceptRoots)
                    {
                        if (docRoot.ApplicableEntity == docEntity)
                        {
                            foreach(DocTemplateUsage docConcept in docRoot.Concepts)
                            {
                                // sub-templates: traverse through

                                if (docConcept.Items.Count > 0)
                                {
                                    string template = docConcept.Definition.Name.Replace(" ", "_");

                                    // no sub-templates: use direct (e.g. ports)
                                    foreach (DocTemplateItem docItem in docConcept.Items)
                                    {
                                        string name = docItem.GetParameterValue("Name");

                                        if (docItem.Concepts.Count > 0)
                                        {
                                            // drill into each concept
                                            foreach (DocTemplateUsage docInner in docItem.Concepts)
                                            {
                                                foreach (DocTemplateItem docInnerItem in docInner.Items)
                                                {
                                                    string attr = docInnerItem.GetParameterValue("PropertyName"); // temphack
                                                    string type = docInnerItem.GetParameterValue("Value");

                                                    if (attr != null && type != null && attr.Length > 1)
                                                    {
                                                        sb.AppendLine("\tpublic " + type + " " + attr);
                                                        sb.AppendLine("\t{");
                                                        sb.AppendLine("\t\tget");
                                                        sb.AppendLine("\t\t{");
                                                        sb.AppendLine("\t\t\treturn Template." + template + ".GetValue(this, \"" + name + "\", \"" + attr + "\") as " + type + ";");
                                                        sb.AppendLine("\t\t}");
                                                        sb.AppendLine("\t\tset");
                                                        sb.AppendLine("\t\t{");
                                                        sb.AppendLine("\t\t\tTemplate." + template + ".SetValue(this, \"" + name +"\", \"" + attr + "\", value);");
                                                        sb.AppendLine("\t\t}");
                                                        sb.AppendLine("\t}"); 
                                                        sb.AppendLine();
                                                    }

                                                }
                                                
                                            }
                                        }
                                        else
                                        {
                                            string type = docItem.GetParameterValue("Value");
                                            if (type == null)
                                            {
                                                // find type according to template
                                                //docConcept.Definition.
                                            }

                                            if (name != null && type != null)
                                            {
                                                sb.AppendLine("\tpublic " + type + " " + name);
                                                sb.AppendLine("\t{");
                                                sb.AppendLine("\t\tget");
                                                sb.AppendLine("\t\t{");
                                                sb.AppendLine("\t\t\treturn Template." + template + ".GetValue(this, \"" + name + "\") as " + type + ";");
                                                sb.AppendLine("\t\t}");
                                                sb.AppendLine("\t\tset");
                                                sb.AppendLine("\t\t{");
                                                sb.AppendLine("\t\t\tTemplate." + template + ".SetValue(this, \"" + name + "\", value);");
                                                sb.AppendLine("\t\t}");
                                                sb.AppendLine("\t}"); 
                                                sb.AppendLine();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

#endif

				sb.AppendLine("}");
			}

			return sb.ToString();
		}

		public string FormatEnumeration(DocEnumeration docEnumeration, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();
			using (CSharpCodeProvider prov = new CSharpCodeProvider())
			{
				sb.AppendLine("[Guid(\"" + docEnumeration.Uuid.ToString() + "\")]");
				sb.AppendLine("public enum " + docEnumeration.Name);
				sb.AppendLine("{");
				int counter = 0;
				foreach (DocConstant docConstant in docEnumeration.Constants)
				{
					int val;

					if (docConstant.Name.Equals("NOTDEFINED"))
					{
						val = 0;
					}
					else if (docConstant.Name.Equals("USERDEFINED"))
					{
						val = -1;
					}
					else
					{
						counter++;
						val = counter;
					}

					// description
					if (!String.IsNullOrEmpty(docConstant.Documentation))
					{
						sb.Append("\t[Description(");
						prov.GenerateCodeFromExpression(new CodePrimitiveExpression(docConstant.Documentation), new StringWriter(sb), null);
						sb.AppendLine(")]");
					}

					sb.AppendLine("\t" + docConstant.Name + " = " + val + ",");
					sb.AppendLine();
				}
				sb.Append("}");
			}
			return sb.ToString();
		}

		public string FormatSelect(DocSelect docSelect, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("[Guid(\"" + docSelect.Uuid.ToString() + "\")]");
			sb.Append("public interface " + docSelect.Name);

			BuildSelectEntries(sb, docSelect, map, included, false);

			sb.AppendLine();
			sb.AppendLine("{");
			sb.AppendLine("}");

			return sb.ToString();
		}

		public string FormatDefined(DocDefined docDefined, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("[Guid(\"" + docDefined.Uuid.ToString() + "\")]");
			sb.Append("public partial struct " + docDefined.Name);

			// implement any selects
			BuildSelectEntries(sb, docDefined, map, included, false);

			sb.AppendLine();
			sb.AppendLine("{");

			sb.AppendLine("\t[XmlText]");
			if (docDefined.Length != 0)
			{
				sb.AppendLine("\t[MaxLength(" + docDefined.Length + ")]");
			}
			sb.AppendLine("\tpublic " + FormatIdentifier(docDefined.DefinedType) + " Value { get; private set; }");
			sb.AppendLine();

			// direct constructor for all types
			sb.AppendLine("\tpublic " + docDefined.Name + "(" + FormatIdentifier(docDefined.DefinedType) + " value) : this()");
			sb.AppendLine("\t{");
			sb.AppendLine("\t\tthis.Value = value;");
			sb.AppendLine("\t}");

			// helper constructor for types that wrap another defined type (e.g. IfcPositiveLengthMeasure(double value) instead of only IfcPositiveLengthMeasure(IfcLengthMeasure value)
			DocDefined docIndirect = this.m_project.GetDefinition(docDefined.DefinedType) as DocDefined;
			if (docIndirect != null)
			{
				sb.AppendLine();
				sb.AppendLine("\tpublic " + docDefined.Name + "(" + FormatIdentifier(docIndirect.DefinedType) + " value) : this()");
				sb.AppendLine("\t{");
				sb.AppendLine("\t\tthis.Value = new " + docDefined.DefinedType + "(value);");
				sb.AppendLine("\t}");

			}
			DocDefined definingPrimitive = docIndirect == null ? docDefined : docIndirect;
			bool stringPrimitive = (string.Compare(definingPrimitive.DefinedType, "string", true) == 0);
			DocPrimitive primitive = definingPrimitive.Definition as DocPrimitive;

			if(primitive != null)
			{
				sb.AppendLine();
				sb.AppendLine("\tpublic static implicit operator " + docDefined.Name + "(" + FormatIdentifier(definingPrimitive.DefinedType) + " value)");
				sb.AppendLine("\t{");
				if (stringPrimitive)
				{
					sb.AppendLine("\t\tif (value == null)");
					sb.AppendLine("\t\t\treturn null;");
					sb.AppendLine();
				}
				sb.AppendLine("\t\treturn new " + docDefined.Name + "(value);");
				sb.AppendLine("\t}");

				sb.AppendLine();
				sb.AppendLine("\tpublic static implicit operator " + FormatIdentifier(definingPrimitive.DefinedType)  + "(" + docDefined.Name + " value)");
				sb.AppendLine("\t{");
				if (stringPrimitive)
				{
					sb.AppendLine("\t\tif (value == null)");
					sb.AppendLine("\t\t\treturn null;");
					sb.AppendLine();
				}
				sb.AppendLine("\t\treturn value.Value;");
				sb.AppendLine("\t}");
			}
			sb.AppendLine();
			sb.AppendLine("\tpublic override string ToString()");
			sb.AppendLine("\t{");
			if(stringPrimitive)
				sb.AppendLine("\t\treturn (Value == null ? \"\" : Value.ToString());");
			else
				sb.AppendLine("\t\treturn Value.ToString();");
			sb.AppendLine("\t}");

			// end of type
			sb.AppendLine("}");

			return sb.ToString();
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
								string text = this.Indent(this.FormatDefined(docDefined, map, included), 1);
								sb.AppendLine(text);
							}
							else if (docType is DocSelect)
							{
								DocSelect docSelect = (DocSelect)docType;
								string text = this.Indent(this.FormatSelect(docSelect, map, included), 1);
								sb.AppendLine(text);
							}
							else if (docType is DocEnumeration)
							{
								DocEnumeration docEnumeration = (DocEnumeration)docType;
								string text = this.Indent(this.FormatEnumeration(docEnumeration, map, included), 1);
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
							string text = this.Indent(this.FormatEntity(docEntity, map, included), 1);
							sb.AppendLine(text);
						}
					}
				}

			}

			return sb.ToString();
		}

		public string FormatData(DocPublication docPublication, DocExchangeDefinition docExchange, Dictionary<string, DocObject> map, Dictionary<long, SEntity> instances)
		{
			//...???
			return null;
		}

		public static void GenerateExchange(DocProject docProject, DocModelView docView, string path, Dictionary<string, DocObject> map)
		{
			using (CSharpCodeProvider prov = new CSharpCodeProvider())
			{

				string schemaid = docView.Code;

				string pathSchema = path + @"\" + schemaid;
				if (!Directory.Exists(pathSchema))
				{
					Directory.CreateDirectory(pathSchema);
				}

				// write assembly version info
				using (StreamWriter writerAssm = new StreamWriter(pathSchema + @"\AssemblyInfo.cs", false, Encoding.UTF8))
				{
					WriteResource(writerAssm, "IfcDoc.assemblyinfo.txt");

					writerAssm.WriteLine("[assembly: AssemblyTitle(\"BuildingSmart.Exchange." + schemaid.ToUpper() + "\")]");

					string version = docView.Version;
					if (!String.IsNullOrEmpty(version))
					{
						writerAssm.WriteLine("[assembly: AssemblyVersion(\"" + version + "\")]");
						writerAssm.WriteLine("[assembly: AssemblyFileVersion(\"" + version + "\")]");
					}
				}

				string ifcschema = docProject.GetSchemaIdentifier();
				using (StreamWriter writerProj = new StreamWriter(pathSchema + @"\BuildingSmart.Exchange." + schemaid.ToUpper() + ".csproj", false, Encoding.UTF8))
				{
					WriteResource(writerProj, "IfcDoc.csproj1.txt");
					writerProj.WriteLine("  </ItemGroup>");
					writerProj.WriteLine("    <Reference Include=\"BuildingSmart.Exchange\" />");
					writerProj.WriteLine("    <Reference Include=\"BuildingSmart." + ifcschema + "\" />");
					writerProj.WriteLine("  <ItemGroup>");
					writerProj.WriteLine("    <Compile Include=\"AssemblyInfo.cs\" />");
					writerProj.WriteLine("    <Compile Include=\"" + schemaid + ".cs\" />");
					WriteResource(writerProj, "IfcDoc.csproj2.txt");
				}

				using (StreamWriter writerFile = new StreamWriter(pathSchema + @"\" + schemaid + ".cs", false, Encoding.UTF8))
				{
					writerFile.WriteLine("using System;");
					writerFile.WriteLine("using System.Collections.Generic;");
					writerFile.WriteLine("using System.ComponentModel;");
					writerFile.WriteLine("using BuildingSmart.Exchange;");
					writerFile.WriteLine();

					HashSet<DocSchema> setNamespaces = new HashSet<DocSchema>();

					StringBuilder sbCode = new StringBuilder();
					using (StringWriter writerCode = new StringWriter(sbCode))
					{

						writerCode.WriteLine();
						writerCode.WriteLine("namespace BuildingSmart.Exchange." + schemaid);
						writerCode.WriteLine("{");
						foreach (DocConceptRoot docConceptRoot in docView.ConceptRoots)
						{
							if (docConceptRoot.ApplicableEntity != null && !String.IsNullOrEmpty(docConceptRoot.Name))
							{
								DocSchema docSchema = docProject.GetSchemaOfDefinition(docConceptRoot.ApplicableEntity);
								if (!setNamespaces.Contains(docSchema))
								{
									setNamespaces.Add(docSchema);
								}

								string classname = docConceptRoot.Name.Replace(" ", "");

								// inherits from Concept, where helper functions are defined for getting/setting
								writerCode.WriteLine("  public class " + classname + " : Concept");
								writerCode.WriteLine("  {");

								// general constructor wraps a new object
								writerCode.WriteLine("    public " + classname + "() : base(new " + docConceptRoot.ApplicableEntity.Name + "())");
								writerCode.WriteLine("    {");
								writerCode.WriteLine("    }");
								writerCode.WriteLine();

								// parameterized constructor wraps existing object
								writerCode.WriteLine("    public " + classname + "(" + docConceptRoot.ApplicableEntity + " target) : base(target)");
								writerCode.WriteLine("    {");
								writerCode.WriteLine("    }");
								writerCode.WriteLine();

								foreach (DocTemplateUsage docConcept in docConceptRoot.Concepts)
								{
									if (docConcept.Definition != null && docConcept.Definition.Uuid == DocTemplateDefinition.guidTemplateMapping)
									{
										foreach (DocTemplateItem docItem in docConcept.Items)
										{
											string name = docItem.GetParameterValue("Name");
											string desc = docItem.Documentation;// GetParameterValue("Description");
											string refp = docItem.GetParameterValue("Reference");
											CvtValuePath vpath = CvtValuePath.Parse(refp, map);
											if (vpath != null)
											{
												string type = "object";
												string code = name.Replace(" ", "");
												string opt = "";
												bool vector = false;
												while (vpath.InnerPath != null)
												{
													if (vpath.Vector)
													{
														vector = true;
													}

													vpath = vpath.InnerPath;
												}

												if (vpath.Property != null)
												{
													type = vpath.Property.DefinedType;
												}
												else if (vpath.Type != null)
												{
													type = vpath.Type.Name;
												}

												DocDefinition docItemDef = docProject.GetDefinition(type);
												if (docItemDef != null)
												{
													DocSchema docItemSchema = docProject.GetSchemaOfDefinition(docItemDef);
													if (docItemSchema != null && !setNamespaces.Contains(docItemSchema))
													{
														setNamespaces.Add(docItemSchema);
													}

													if (docItemDef is DocDefined || docItemDef is DocEnumeration)
													{
														opt = "?";
													}
												}

												writerCode.WriteLine("    const string _" + code + " = @\"" + refp + "\";");

												// display name supports usage within property grids, headers when exporting to spreadsheets
												if (name != code)
												{
													writerCode.WriteLine("    [DisplayName(\"" + name + "\")]");
												}

												if (desc != null)
												{
													StringBuilder sb = new StringBuilder();
													prov.GenerateCodeFromExpression(new CodePrimitiveExpression(desc), new StringWriter(sb), null);
													writerCode.WriteLine("    [Description(" + sb.ToString() + ")]");
												}

												if (vector)
												{
													writerCode.WriteLine("    public IList<" + type + "> " + code + " { get { return this.GetValue(_" + code + ") as IList<" + type + ">; } }");
												}
												else
												{
													writerCode.WriteLine("    public " + type + opt + " " + code + " { get { return this.GetValue(_" + code + ") as " + type + "; } set { this.SetValue(_" + code + ", value); } }");
												}
												writerCode.WriteLine();
											}
										}
									}
								}

								writerCode.WriteLine("  }");
								writerCode.WriteLine();
							}
						}
						writerCode.WriteLine("}");


						// now write namespace includes
						foreach (DocSchema docSchema in setNamespaces)
						{
							writerFile.WriteLine("using BuildingSmart.IFC." + docSchema.Name + ";");
						}

						// write code
						writerFile.Write(sbCode.ToString());
					}

				}

			}
		}

	}
}
