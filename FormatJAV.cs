// Name:        FormatJAV.cs
// Description: Java Code Generator
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

using System.Runtime.Serialization.Json;

namespace IfcDoc.Format.JAV
{
	internal class FormatJAV : IDisposable,
		IFormatExtension
	{
		string m_filename;
		DocProject m_project;
		DocDefinition m_definition;

		/// <summary>
		/// Generates folder of definitions
		/// </summary>
		/// <param name="path"></param>
		public static void GenerateCode(DocProject project, string path)
		{
			foreach (DocSection docSection in project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocType docType in docSchema.Types)
					{
						using (FormatJAV format = new FormatJAV(path + @"\" + docSchema.Name + @"\" + docType.Name + ".java"))
						{
							format.Instance = project;
							format.Definition = docType;
							format.Save();
						}
					}

					foreach (DocEntity docType in docSchema.Entities)
					{
						using (FormatJAV format = new FormatJAV(path + @"\" + docSchema.Name + @"\" + docType.Name + ".java"))
						{
							format.Instance = project;
							format.Definition = docType;
							format.Save();
						}
					}
				}
			}
		}

		public FormatJAV()
		{
			this.m_filename = null;
		}

		public FormatJAV(string filename)
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

		public void Save()
		{
			string dirpath = System.IO.Path.GetDirectoryName(this.m_filename);
			if (!System.IO.Directory.Exists(this.m_filename))
			{
				System.IO.Directory.CreateDirectory(dirpath);
			}

			using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.m_filename))
			{
				writer.WriteLine("// This file was automatically generated from IFCDOC at www.buildingsmart-tech.org.");
				writer.WriteLine("// IFC content is copyright (C) 1996-2013 BuildingSMART International Ltd.");
				writer.WriteLine();

				if (this.m_definition != null)
				{
					writer.WriteLine("package buildingsmart.ifc");
					writer.WriteLine("{");

					if (this.m_definition is DocDefined)
					{
						// do nothing - don't make a separate type in Java, as that would require extra heap allocation 
					}
					else if (this.m_definition is DocSelect)
					{
						writer.WriteLine("\tpublic interface " + this.m_definition.Name);
						writer.WriteLine("\t{");
						writer.WriteLine("\t}");
					}
					else if (this.m_definition is DocEnumeration)
					{
						DocEnumeration docEnumumeration = (DocEnumeration)this.m_definition;

						writer.WriteLine("\tpublic enum " + this.m_definition.Name);
						writer.WriteLine("\t{");
						foreach (DocConstant docConstant in docEnumumeration.Constants)
						{
							writer.WriteLine("\t\t" + docConstant.Name + ",");
						}
						writer.WriteLine("\t}");
					}
					else if (this.m_definition is DocEntity)
					{
						DocEntity docEntity = (DocEntity)this.m_definition;

						string basedef = docEntity.BaseDefinition;
						if (String.IsNullOrEmpty(basedef))
						{
							basedef = "IfcBase";
						}

						writer.WriteLine("\tpublic class " + this.m_definition.Name + " extends " + basedef);
						writer.WriteLine("\t{");

						// fields
						foreach (DocAttribute docAttribute in docEntity.Attributes)
						{
							string deftype = docAttribute.DefinedType;

							// if defined type, use raw type (avoiding extra memory allocation)
							DocDefinition docDef = GetDefinition(deftype);
							if (docDef is DocDefined)
							{
								deftype = ((DocDefined)docDef).DefinedType;

								switch (deftype)
								{
									case "STRING":
										deftype = "string";
										break;

									case "INTEGER":
										deftype = "int";
										break;

									case "REAL":
										deftype = "double";
										break;

									case "BOOLEAN":
										deftype = "bool";
										break;

									case "LOGICAL":
										deftype = "int";
										break;

									case "BINARY":
										deftype = "byte[]";
										break;
								}
							}

							switch (docAttribute.GetAggregation())
							{
								case DocAggregationEnum.SET:
									writer.WriteLine("\t\tprivate " + deftype + "[] " + docAttribute.Name + ";");
									break;

								case DocAggregationEnum.LIST:
									writer.WriteLine("\t\tprivate " + deftype + "[] " + docAttribute.Name + ";");
									break;

								default:
									writer.WriteLine("\t\tprivate " + deftype + " " + docAttribute.Name + ";");
									break;
							}
						}

						writer.WriteLine("\t}");
					}

					writer.WriteLine("}");
				}
				else
				{

#if false // TBD
                    writer.WriteLine("package buildingsmart.ifc.properties");
                    writer.WriteLine("{");
                    writer.WriteLine();

                    Dictionary<string, string[]> mapEnums = new Dictionary<string, string[]>();

                    foreach (DocSection docSection in this.m_project.Sections)
                    {
                        foreach (DocSchema docSchema in docSection.Schemas)
                        {
                            foreach (DocPropertySet docPset in docSchema.PropertySets)
                            {
                                writer.WriteLine("    /// <summary>");
                                writer.WriteLine("    /// " + docPset.Documentation.Replace('\r', ' ').Replace('\n', ' '));
                                writer.WriteLine("    /// </summary>");

                                writer.WriteLine("    public class " + docPset.Name + " : Pset");
                                writer.WriteLine("    {");

                                foreach (DocProperty docProperty in docPset.Properties)
                                {
                                    writer.WriteLine("        /// <summary>");
                                    writer.WriteLine("        /// " + docProperty.Documentation.Replace('\r', ' ').Replace('\n', ' '));
                                    writer.WriteLine("        /// </summary>");

                                    switch (docProperty.PropertyType)
                                    {
                                        case DocPropertyTemplateTypeEnum.P_SINGLEVALUE:
                                            writer.WriteLine("        public " + docProperty.PrimaryDataType + " " + docProperty.Name + " { get { return this.GetValue<" + docProperty.PrimaryDataType + ">(\"" + docProperty.Name + "\"); } set { this.SetValue<" + docProperty.PrimaryDataType + ">(\"" + docProperty.Name + "\", value); } }");
                                            break;

                                        case DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE:
                                            // record enum for later
                                            {
                                                string[] parts = docProperty.SecondaryDataType.Split(':');
                                                if (parts.Length == 2)
                                                {
                                                    string typename = parts[0];
                                                    if (!mapEnums.ContainsKey(typename))
                                                    {
                                                        string[] enums = parts[1].Split(',');
                                                        mapEnums.Add(typename, enums);

                                                        writer.WriteLine("        public " + typename + " " + docProperty.Name + " { get { return this.GetValue<" + typename + ">(\"" + docProperty.Name + "\"); } set { this.SetValue<" + typename + ">(\"" + docProperty.Name + "\", value); } }");
                                                    }
                                                }
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

                    // enums
                    foreach (string strEnum in mapEnums.Keys)
                    {
                        string[] enums = mapEnums[strEnum];

                        writer.WriteLine("    /// <summary>");
                        writer.WriteLine("    /// </summary>");
                        writer.WriteLine("    public enum " + strEnum);
                        writer.WriteLine("    {");

                        int counter = 0;
                        foreach (string val in enums)
                        {
                            int num = 0;
                            string id = val.ToUpper().Trim('.').Replace('-', '_');
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
                            writer.WriteLine("        " + id + " = " + num + ",");
                        }

                        writer.WriteLine("    }");
                        writer.WriteLine();
                    }

                    writer.WriteLine("}");
#endif
				}
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion

		private DocDefinition GetDefinition(string def)
		{
			foreach (DocSection docSection in this.m_project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocType docType in docSchema.Types)
					{
						if (docType.Name.Equals(def))
						{
							return docType;
						}
					}

					foreach (DocEntity docType in docSchema.Entities)
					{
						if (docType.Name.Equals(def))
						{
							return docType;
						}
					}
				}
			}

			return null;
		}

		public string FormatEntity(DocEntity docEntity, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			string basedef = docEntity.BaseDefinition;
			if (String.IsNullOrEmpty(basedef))
			{
				basedef = "IfcBase";
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("public class " + docEntity.Name + " extends " + basedef);
			sb.AppendLine("{");

			// fields
			foreach (DocAttribute docAttribute in docEntity.Attributes)
			{
				string deftype = docAttribute.DefinedType;

				// if defined type, use raw type (avoiding extra memory allocation)
				DocObject docDef = null;
				if (deftype != null)
				{
					map.TryGetValue(deftype, out docDef);
				}

				if (docDef is DocDefined)
				{
					deftype = ((DocDefined)docDef).DefinedType;

					switch (deftype)
					{
						case "STRING":
							deftype = "string";
							break;

						case "INTEGER":
							deftype = "int";
							break;

						case "REAL":
							deftype = "double";
							break;

						case "BOOLEAN":
							deftype = "bool";
							break;

						case "LOGICAL":
							deftype = "int";
							break;

						case "BINARY":
							deftype = "byte[]";
							break;
					}
				}

				switch (docAttribute.GetAggregation())
				{
					case DocAggregationEnum.SET:
						sb.AppendLine("\tprivate " + deftype + "[] " + docAttribute.Name + ";");
						break;

					case DocAggregationEnum.LIST:
						sb.AppendLine("\tprivate " + deftype + "[] " + docAttribute.Name + ";");
						break;

					default:
						sb.AppendLine("\tprivate " + deftype + " " + docAttribute.Name + ";");
						break;
				}
			}

			sb.AppendLine("}");
			return sb.ToString();
		}

		public string FormatEnumeration(DocEnumeration docEnumeration, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("public enum " + docEnumeration.Name);
			sb.AppendLine("{");
			foreach (DocConstant docConstant in docEnumeration.Constants)
			{
				sb.AppendLine("\t" + docConstant.Name + ",");
			}
			sb.AppendLine("}");
			return sb.ToString();
		}

		public string FormatSelect(DocSelect docSelect, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			return "public interface " + docSelect.Name + "\r\n{\r\n}\r\n";
		}

		public string FormatDefined(DocDefined docDefined, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			// nothing -- java does not support structures
			return "/* " + docDefined.Name + " : " + docDefined.DefinedType + " (Java does not support structures, so usage of defined types are inline for efficiency.) */\r\n";
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
							sb.AppendLine();
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
							sb.AppendLine();
						}
					}
				}

			}

			return sb.ToString();
		}

		public string FormatData(DocPublication docPublication, DocExchangeDefinition docExchange, Dictionary<string, DocObject> map, Dictionary<long, SEntity> instances)
		{
			System.IO.MemoryStream stream = new System.IO.MemoryStream();
			if (instances.Count > 0)
			{
				SEntity rootproject = null;
				foreach (SEntity ent in instances.Values)
				{
					if (ent.GetType().Name.Equals("IfcProject"))
					{
						rootproject = ent;
						break;
					}
				}

				if (rootproject != null)
				{
					Type type = rootproject.GetType();

					DataContractJsonSerializer contract = new DataContractJsonSerializer(type);

					try
					{
						contract.WriteObject(stream, rootproject);
					}
					catch (Exception xx)
					{
						//...
						xx.ToString();
					}
				}
			}

			stream.Position = 0;
			System.IO.TextReader reader = new System.IO.StreamReader(stream);
			string content = reader.ReadToEnd();
			return content;
		}

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
	}
}
