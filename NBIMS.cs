// Name:        NBIMS.cs
// Description: Dialog box for generating documentation output required of NBIMS V3 format.
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;
using IfcDoc.Schema.MVD;
using IfcDoc.Format.HTM;
using IfcDoc.Format.EXP;
using IfcDoc.Format.XSD;
using IfcDoc.Format.XML;
using IfcDoc.Format.SPF;

namespace IfcDoc
{
	public class NBIMS
	{
		private static string GenerateTemplateImage(DocTemplateDefinition docTemplate, Dictionary<string, DocObject> mapEntity, DocProject project, string path)
		{
			int cx = 0;
			int cy = 0;

			System.Drawing.Image image = IfcDoc.Format.PNG.FormatPNG.CreateTemplateDiagram(docTemplate, mapEntity, new Dictionary<System.Drawing.Rectangle, SEntity>(), project, null);

			if (image != null)
			{
				using (image)
				{
					cx = image.Width;
					cy = image.Height;
					string filepath = path + "\\" + docTemplate.Name.ToLower().Replace(' ', '-') + ".png";
					image.Save(filepath, System.Drawing.Imaging.ImageFormat.Png);

					cx = cx / 2;
					cy = cy / 2;
				}
			}

			return "<br/><img src=\"" + docTemplate.Name.ToLower().Replace(' ', '-') + ".png\" width=\"" + cx + "\" height=\"" + cy + "\"/>";
		}

		private static void WriteConceptTemplates(DocProject project, FormatHTM format, bool descriptions, bool all, string path, Dictionary<string, DocObject> mapEntity, Dictionary<DocObject, bool> included)
		{
			foreach (DocTemplateDefinition docTemplate in project.Templates)
			{
				if (all || included.ContainsKey(docTemplate))
				{
					int count = 1;
					if (docTemplate.Templates != null)
					{
						foreach (DocTemplateDefinition doc2 in docTemplate.Templates)
						{
							if (all || included.ContainsKey(doc2))
							{
								count++;

								if (doc2.Templates != null)
								{
									foreach (DocTemplateDefinition doc3 in doc2.Templates)
									{
										if (all || included.ContainsKey(doc3))
										{
											count++;
										}
									}
								}
							}
						}
					}

					string img = "";
					if (docTemplate.Rules != null && docTemplate.Rules.Count > 0)
					{
						img = GenerateTemplateImage(docTemplate, mapEntity, project, path);

						//img = "<br/><img src=\"" + docTemplate.Name.ToLower().Replace(' ', '-') + ".png\" width=\"" + cx + "\" height=\"" + cy + "\"/>";
					}
					format.WriteLine("<tr><td rowspan=\"" + count + "\">" + docTemplate.Name + "</td><td></td><td></td>");
					if (descriptions)
					{
						format.Write("<td>" + docTemplate.Documentation + img + "</td>");
					}
					format.WriteLine("</tr>");

					if (docTemplate.Templates != null)
					{
						foreach (DocTemplateDefinition doc2 in docTemplate.Templates)
						{
							if (all || included.ContainsKey(doc2))
							{
								int count2 = 1;
								if (doc2.Templates != null)
								{
									foreach (DocTemplateDefinition doc3 in doc2.Templates)
									{
										if (all || included.ContainsKey(doc3))
										{
											count2++;
										}
									}
								}

								img = "";
								if (doc2.Rules != null && doc2.Rules.Count > 0)
								{
									img = GenerateTemplateImage(doc2, mapEntity, project, path);

									//img = "<br/><img src=\"" + doc2.Name.ToLower().Replace(' ', '-') + ".png\"/>";
								}
								format.WriteLine("<tr><td rowspan=\"" + count2 + "\">" + doc2.Name + "</td><td></td>");
								if (descriptions)
								{
									format.WriteLine("<td>" + doc2.Documentation + img + "</td>");
								}
								format.Write("</tr>");

								if (doc2.Templates != null)
								{
									foreach (DocTemplateDefinition doc3 in doc2.Templates)
									{
										if (all || included.ContainsKey(doc3))
										{
											img = "";
											if (doc3.Rules != null && doc3.Rules.Count > 0)
											{
												img = GenerateTemplateImage(doc3, mapEntity, project, path);

												//img = "<br/><img src=\"" + doc3.Name.ToLower().Replace(' ', '-') + ".png\"/>";
											}
											format.WriteLine("<tr><td>" + doc3.Name + "</td>");
											if (descriptions)
											{
												format.WriteLine("<td>" + doc3.Documentation + img + "</td>");
											}
											format.WriteLine("</tr>");
										}
									}
								}
							}
						}
					}
				}
			}
		}

		public static void Export(DocProject project, DocModelView docView, string path, Dictionary<string, DocObject> mapEntity, Dictionary<string, string> mapSchema)
		{
			Dictionary<DocObject, bool> included = docView.Filter(project);

			const string HEADERCELL = "<th style=\"-webkit-transform:rotate(90deg); writing-mode:tb-rl; -moz-transform:rotate(90deg); -o-transform: rotate(90deg); white-space:nowrap; display:blocking; ms-filter: \"progid:DXImageTransform.Microsoft.BasicImage(rotation=0.083)\"; >";

			// get list of entities in order
			List<DocEntity> sortEntity = project.GetEntityList();

			// build list of templates in use (to preserve sort order)
			Dictionary<DocTemplateDefinition, List<DocConceptRoot>> mapTemplates = new Dictionary<DocTemplateDefinition, List<DocConceptRoot>>();

			// double-loop not optimal, but lists aren't that large
			foreach (DocEntity docSortEnt in sortEntity)
			{
				foreach (DocConceptRoot docRoot in docView.ConceptRoots)
				{
					if (docRoot.ApplicableEntity == docSortEnt)
					{
						foreach (DocTemplateUsage docConcept in docRoot.Concepts)
						{
							if (!mapTemplates.ContainsKey(docConcept.Definition))
							{
								mapTemplates.Add(docConcept.Definition, new List<DocConceptRoot>());
							}
							mapTemplates[docConcept.Definition].Add(docRoot);
						}
					}
				}
			}

			// build list of concept roots sorted by entity order
			List<DocConceptRoot> sortConceptRoot = new List<DocConceptRoot>();
			foreach (DocEntity docSortEntity in sortEntity)
			{
				foreach (DocConceptRoot docRoot in docView.ConceptRoots)
				{
					if (docRoot.ApplicableEntity == docSortEntity)
					{
						sortConceptRoot.Add(docRoot);
						break;
					}
				}
			}


			using (FormatEXP formatEXP = new FormatEXP(path + @"\" + docView.Code + ".exp"))
			{
				formatEXP.Instance = project;
				formatEXP.ModelViews = new DocModelView[] { docView };
				formatEXP.Save();
			}

			using (FormatXSD formatXSD = new FormatXSD(path + @"\" + docView.Code + ".xsd"))
			{
				formatXSD.Project = project;
				formatXSD.ModelViews = new DocModelView[] { docView };
				formatXSD.Save();
			}

			using (FormatSPF format = new FormatSPF(path + @"\" + docView.Code + ".ifc", Schema.IFC.SchemaIfc.Types, new Dictionary<long, SEntity>()))
			{
				format.InitHeaders(docView.Code, project.GetSchemaIdentifier());
				Schema.IFC.IfcProject ifcProject = new IfcDoc.Schema.IFC.IfcProject();
				Program.ExportIfc(ifcProject, project, included);
				format.Save();
			}

			using (FormatXML format = new FormatXML(path + @"\" + docView.Code + ".mvdxml", typeof(mvdXML), mvdXML.DefaultNamespace))
			{
				mvdXML mvd = new mvdXML();
				Program.ExportMvd(mvd, project, mapEntity, included);
				format.Instance = mvd;
				format.Save();
			}


			using (FormatHTM format = new FormatHTM(path + @"\" + docView.Code + ".htm", mapEntity, new Dictionary<string, string>(), included))
			{
				format.WriteHeader(docView.Name, 0, null);

				// 1 Scope
				// 1.1 Business Case Description
				// 1.2 Participants and Stakeholders

				// 2 Normative References

				// 3 Terms and definitions

				// 4 Symbols and abbreviated terms

				// 5 Business processes
				// 5.1 Process models provided
				// 5.2 Representative process models
				// 5.3 Process models formatting

				format.WriteLine("<h1>6 Exchange requirements</h1>");
				format.WriteLine("<h2>6.1 Exchange requirements legibility</h2>");
				format.WriteLine("<h3>6.1.1 Exchange requirements list</h3>");
				format.WriteLine("<p>Each exchange is listed by name as follows.</p>");
				Dictionary<string, string> mapExchangeClass = new Dictionary<string, string>();
				format.WriteLine("<ul>");
				foreach (DocExchangeDefinition docExchange in docView.Exchanges)
				{
					if (docExchange.ExchangeClass != null && !mapExchangeClass.ContainsKey(docExchange.ExchangeClass) && !String.IsNullOrEmpty(docExchange.ExchangeClass))
					{
						mapExchangeClass.Add(docExchange.ExchangeClass, docExchange.ExchangeClass);
					}

					format.WriteLine("<li>" + docExchange.Name + "</li>");
				}
				format.WriteLine("</ul>");

				format.WriteLine("<h3>6.1.2 Exchange requirement classification list</h3>");
				format.WriteLine("<p>Each phase classification used by this model view is listed by Omniclass notation and title as follows.</p>");
				format.WriteLine("<ul>");
				foreach (string key in mapExchangeClass.Keys)
				{
					format.WriteLine("<li>" + key + "</li>");
				}
				format.WriteLine("</ul>");

				format.WriteLine("<h3>6.1.3 Exchange requirement coverage analysis</h3>");
				format.WriteLine("<p>Each exchange is listed by name and corresponding classifications for the process undertaken, the sender of the information, and the receiver of the information.</p>");
				format.WriteLine("<table class=\"gridtable\">");
				format.WriteLine("<tr><th>Exchange</th><th>Process</th><th>Sender</th><th>Receiver</th></tr>");
				foreach (DocExchangeDefinition docExchange in docView.Exchanges)
				{
					format.WriteLine("<tr><td>" + docExchange.Name + "</td><td>" + docExchange.ExchangeClass + "</td><td>" + docExchange.SenderClass + "</td><td>" + docExchange.ReceiverClass);
				}
				format.WriteLine("</table>");

				format.WriteLine("<h2>6.2 Exchange requirements detail</h2>");
				format.WriteLine("<h3>6.2.1 Exchange requirements definition</h3>");
				format.WriteLine("<p>Each exchange is listed by name and a description of the information contained.</p>");
				foreach (DocExchangeDefinition docExchange in docView.Exchanges)
				{
					format.WriteLine("<h4>" + docExchange.Name + "</h4>");
					format.WriteLine(docExchange.Documentation);
				}

				format.WriteLine("<h3>6.2.2 Business rule list</h3>");
				format.WriteLine("<p>Each exchange consists of a set of entity data definitions with usage defined according to business rule concepts. " +
					"An entity describes an object class having one or more attributes, where each attribute may refer to values, collections, or references to other objects. " +
					"A concept describes usage of object classes, where allowable values and object types are indicated for specific attributes." +
					"Each heading that follows refers to an exchange, where each table row corresponds to an entity, each table column corresponds to an exchange, and each cell indicates whether the concept is used for the entity within the exchange.</p>");
				foreach (DocExchangeDefinition docExchange in docView.Exchanges)
				{
					format.WriteLine("<h4>" + docExchange.Name + "</h4>");

					List<DocTemplateDefinition> listTemplate = project.GetTemplateList();
					List<DocTemplateDefinition> usedTemplate = new List<DocTemplateDefinition>();

					foreach (DocConceptRoot docRoot in docView.ConceptRoots)
					{
						foreach (DocTemplateUsage docConcept in docRoot.Concepts)
						{
							if (!usedTemplate.Contains(docConcept.Definition))
							{
								foreach (DocExchangeItem docExchangeItem in docConcept.Exchanges)
								{
									if (docExchangeItem.Exchange == docExchange &&
										(docExchangeItem.Requirement == DocExchangeRequirementEnum.Mandatory || docExchangeItem.Requirement == DocExchangeRequirementEnum.Optional))
									{
										usedTemplate.Add(docConcept.Definition);
										break;
									}
								}
							}
						}
					}

					for (int i = listTemplate.Count - 1; i >= 0; i--)
					{
						if (!usedTemplate.Contains(listTemplate[i]))
						{
							listTemplate.RemoveAt(i);
						}
					}

					format.WriteLine("<table class=\"gridtable\">");
					format.Write("<tr>");
					format.Write("<th>Entity</th>");
					for (int i = 0; i < listTemplate.Count; i++)
					{
						format.Write(HEADERCELL);
						format.Write(listTemplate[i].Name);
						format.Write("</th>");
					}
					format.WriteLine("</tr>");

					foreach (DocConceptRoot docRoot in sortConceptRoot)
					{
						DocExchangeRequirementEnum[] reqs = new DocExchangeRequirementEnum[listTemplate.Count];
						bool include = false;
						foreach (DocTemplateUsage docConcept in docRoot.Concepts)
						{
							foreach (DocExchangeItem docExchangeItem in docConcept.Exchanges)
							{
								if (docExchangeItem.Exchange == docExchange &&
									(docExchangeItem.Requirement == DocExchangeRequirementEnum.Mandatory || docExchangeItem.Requirement == DocExchangeRequirementEnum.Optional))
								{
									int index = listTemplate.IndexOf(docConcept.Definition);
									reqs[index] = docExchangeItem.Requirement;
									include = true;
									break;
								}
							}
						}

						if (include)
						{
							format.Write("<tr>");
							format.Write("<td>" + docRoot.ApplicableEntity.Name + "</td>");
							for (int i = 0; i < reqs.Length; i++)
							{
								format.Write("<td>");
								switch (reqs[i])
								{
									case DocExchangeRequirementEnum.Mandatory:
										format.Write("R");
										break;

									case DocExchangeRequirementEnum.Optional:
										format.Write("O");
										break;
								}
								format.Write("</td>");
							}
							format.WriteLine("</tr>");
						}
					}

					format.WriteLine("</table>");
				}

				format.WriteLine("<h3>6.2.3 Business rule definition</h3>");
				format.WriteLine("<p>Business rule definitions are all defined as re-usable templates as indicated in Clause 7.3.5.</p>");

				format.WriteLine("<h2>6.3 Exchange requirements reusability</h2>");
				format.WriteLine("<p>Names and classifications of exchanges are intended to be consistent across other model views where applicable, " +
					"while the content of a particular exchange is intended to be unique.</p>");

				format.WriteLine("<h3>6.3.1 Related business process list</h3>");
				format.WriteLine("<p>Business processes within this model view are correlated with those used in other model views as follows.</p>");

				// Rows: Business processes; Columns: Views; Cells: mark if applicable
				format.WriteLine("<table class=\"gridtable\">");
				format.Write("<tr><th>Process</th>");
				foreach (DocModelView docEachView in project.ModelViews)
				{
					format.Write(HEADERCELL + docEachView.Name + "</th>");
				}
				format.WriteLine("</tr>");
				foreach (string proc in mapExchangeClass.Keys)
				{
					format.Write("<tr><td>" + proc + "</td>");
					foreach (DocModelView docEachView in project.ModelViews)
					{
						bool yes = false;
						foreach (DocExchangeDefinition docEachExchange in docEachView.Exchanges)
						{
							if (!String.IsNullOrEmpty(docEachExchange.ExchangeClass) && docEachExchange.ExchangeClass.Equals(proc))
							{
								yes = true;
								break;
							}
						}

						format.Write("<td>");
						if (yes)
						{
							format.Write("X");
						}
						format.Write("</td>");
					}
					format.WriteLine("</tr>");
				}
				format.WriteLine("</table>");

				format.WriteLine("<h3>6.3.2 Related exchange requirement list</h3>");
				format.WriteLine("<p>Exchange requirements within this model view are correlated with those used in other model views as follows.</p>");
				// Rows: Exchanges; Columns: Views; Cells: Mark if applicable
				format.WriteLine("<table class=\"gridtable\">");
				format.Write("<tr><th>Exchange</th>");
				foreach (DocModelView docEachView in project.ModelViews)
				{
					format.Write(HEADERCELL + docEachView.Name + "</th>");
				}
				format.WriteLine("</tr>");
				foreach (DocExchangeDefinition docExchange in docView.Exchanges)
				{
					format.Write("<tr><td>" + docExchange.Name + "</td>");
					foreach (DocModelView docEachView in project.ModelViews)
					{
						bool yes = false;
						foreach (DocExchangeDefinition docEachExchange in docEachView.Exchanges)
						{
							if (!String.IsNullOrEmpty(docEachExchange.Name) && docEachExchange.Name.Equals(docExchange.Name))
							{
								yes = true;
								break;
							}
						}

						format.Write("<td>");
						if (yes)
						{
							format.Write("X");
						}
						format.Write("</td>");
					}
					format.WriteLine("</tr>");
				}
				format.WriteLine("</table>");

				format.WriteLine("<h3>6.3.3 Related exchange requirement reuse analysis</h3>");
				format.WriteLine("<p>Exchange requirements across other model views are correlated as follows.</p>");

				// Rows: Exchange names; Columns: Views; Cells: Mark if applicable
				List<string> listExchangeNames = new List<string>();
				foreach (DocModelView docEachView in project.ModelViews)
				{
					foreach (DocExchangeDefinition docEachExchange in docEachView.Exchanges)
					{
						if (!listExchangeNames.Contains(docEachExchange.Name))
						{
							listExchangeNames.Add(docEachExchange.Name);
						}
					}
				}
				format.WriteLine("<table class=\"gridtable\">");
				format.Write("<tr><th>Exchange</th>");
				foreach (DocModelView docEachView in project.ModelViews)
				{
					format.Write(HEADERCELL + docEachView.Name + "</th>");
				}
				format.WriteLine("</tr>");
				foreach (string exchangename in listExchangeNames)
				{
					format.Write("<tr><td>" + exchangename + "</td>");
					foreach (DocModelView docEachView in project.ModelViews)
					{
						bool yes = false;
						foreach (DocExchangeDefinition docEachExchange in docEachView.Exchanges)
						{
							if (!String.IsNullOrEmpty(docEachExchange.Name) && docEachExchange.Name.Equals(exchangename))
							{
								yes = true;
								break;
							}
						}

						format.Write("<td>");
						if (yes)
						{
							format.Write("X");
						}
						format.Write("</td>");
					}
					format.WriteLine("</tr>");
				}
				format.WriteLine("</table>");


				format.WriteLine("<h1>7 Model view definition</h1>");
				format.WriteLine("<h2>7.1 Data Definition</h2>");
				format.WriteLine("<h3>7.1.1 Data definitions list</h3>");
				format.WriteLine("<p>Each entity data definition is listed by schema and entity name as follows.</p>");
				format.WriteLine("<ul>");
				string lastschema = null;
				foreach (DocConceptRoot docRoot in sortConceptRoot)
				{
					string schema = mapSchema[docRoot.ApplicableEntity.Name];
					if (schema != lastschema)
					{
						// close out last
						if (docRoot != sortConceptRoot[0])
						{
							format.WriteLine("</ul></li>");
						}

						// open next
						format.WriteLine("<li>" + schema + "<ul>");

						lastschema = schema;
					}

					format.WriteLine("<li>" + docRoot.ApplicableEntity.Name + "</li>");
				}
				format.WriteLine("</ul></ul>");

				format.WriteLine("<h3>7.1.2 Data definitions</h3>");
				format.WriteLine("<p>Each entity data definition is described within subsections as follows, with electronic representations provided in EXPRESS and XSD formats.</p>");
				FormatXSD formatXSD = new FormatXSD(null);
				foreach (DocConceptRoot docRoot in sortConceptRoot)
				{
					string xsd = formatXSD.FormatEntity(docRoot.ApplicableEntity, mapEntity, included);

					format.WriteLine("<h4>" + docRoot.ApplicableEntity.Name + "</h4>");
					format.WriteLine(docRoot.ApplicableEntity.Documentation);
					format.WriteLine("<br/>");

					format.WriteExpressEntitySpecification(docRoot.ApplicableEntity, true, null);
					format.WriteLine("<br/>");
					format.WriteFormatted(xsd);
				}

				format.WriteLine("<h3>7.1.3 Data definition reference schema list</h3>");
				format.WriteLine("<p>Each referenced schema is listed by standards body notation and official title.</p>");
				format.WriteLine("<table class=\"gridtable\">");
				format.WriteLine("<tr><th>Reference</th><th>Description</th></tr>");
				format.WriteLine("<tr><td>ISO 16739:2013</td><td>Industry Foundation Classes (IFC) for data sharing in the construction and facilities management industries</td></tr>");
				format.WriteLine("</table>");

				// 7.2 Concept definitions
				format.WriteLine("<h2>7.2 Concept definitions</h2>");
				format.WriteLine("<h3>7.2.1 Concept list</h3>");
				format.WriteLine("<p>Each concept is listed by entity name and concept template within the following table. " +
					"Each row corresponds to an entity, each column corresponds to a concept template, and each cell indicates usage of a concept template for an entity.</p>");
				format.WriteLine("<table class=\"gridtable\">");
				format.Write("<tr><th>Entity</th>");
				foreach (DocTemplateDefinition docTemplate in mapTemplates.Keys)
				{
					format.Write(HEADERCELL + docTemplate.Name + "</th>");
				}
				format.WriteLine("</tr>");


				foreach (DocConceptRoot docRoot in sortConceptRoot)
				{
					if (docRoot.Concepts.Count > 0)
					{
						format.Write("<tr><td>" + docRoot.ApplicableEntity.Name + "</td>");

						foreach (DocTemplateDefinition docTemplate in mapTemplates.Keys)
						{
							format.Write("<td>");
							foreach (DocTemplateUsage docConcept in docRoot.Concepts)
							{
								if (docConcept.Definition == docTemplate)
								{
									format.Write("X");
								}
							}
							format.Write("</td>");
						}

						format.WriteLine("</tr>");
					}
				}
				format.WriteLine("</table>");

				string pathImages = path + "\\" + docView.Code;
				System.IO.Directory.CreateDirectory(pathImages);

				format.WriteLine("<h3>7.2.2 Concept definitions</h3>");
				format.WriteLine("<p>Each entity is described within subsections, with diagrams indicating the graph of attributes and objects representing the combination of all concepts applied to instances of the entity. " +
					"Each block in the diagram represents an entity, where the entity name is shown at the top of the block with background in black.  " +
					"Each attribute within the entity is shown in order, where black is used to indicate a direct attribute and grey is used to indicate an inverse attribute. " +
					"Notation to the right of each attribute indicates aggregation, where S indicates a SET (unordered unique objects) and L indicates a LIST (ordered objects), " +
					"the first number in brackets indicates the minimum count, and the second number in brackets indicates the maximum count or “?” for unlimited. " +
					"Lines connecting blocks indicates attributes that point to objects of other data definitions.</p>");
				foreach (DocConceptRoot docRoot in sortConceptRoot)
				{
					format.WriteLine("<h4>" + docRoot.ApplicableEntity.Name + "</h4>");

					string img = "";
					if (docView.Code != null)
					{
						int cx = 0;
						int cy = 0;
						try
						{
							using (System.Drawing.Image image = IfcDoc.Format.PNG.FormatPNG.CreateConceptDiagram(docRoot.ApplicableEntity, docView, mapEntity, new Dictionary<System.Drawing.Rectangle, SEntity>(), project, null))
							{
								cx = image.Width;
								cy = image.Height;
								string filepath = path + "\\" + docView.Code.ToLower() + "\\" + docRoot.ApplicableEntity.Name.ToLower() + ".png";
								image.Save(filepath, System.Drawing.Imaging.ImageFormat.Png);
							}
						}
						catch
						{
						}

						// shrink for access from MS Word
						cx = cx / 2;
						cy = cy / 2;

						img = "<br/><img src=\"" + docView.Code.ToLower() + "/" + docRoot.ApplicableEntity.Name.ToLower() + ".png\" width=\"" + cx + "\" height=\"" + cy + "\"/>";
					}

					format.WriteLine(docRoot.Documentation);
					format.WriteLine(img);
					foreach (DocTemplateUsage docConcept in docRoot.Concepts)
					{
						if (!String.IsNullOrEmpty(docConcept.Documentation))
						{
							format.WriteLine("<h5>" + docConcept.Definition.Name + "</h5>");
							format.WriteLine(docConcept.Documentation);
						}
					}
				}
				format.WriteLine("</table>");

				format.WriteLine("<h3>7.2.3 Concept attributes list</h3>");
				format.WriteLine("<p>Concepts may be defined that use parameters to indicate applicable values. " +
					"For example, plumbing objects may make use of ports to enable connectivity to other objects for distribution of water, " +
					"and a specific entity such as a hot water heater may have specific ports such as “ColdWaterIn” and “HotWaterOut”. " +
					"Defining attributes at concepts enables re-use of concepts where the data structures are the same, but applicable values may differ." +
					"Each concept is shown in a subsection as follows, with rows correspoding to entities and rule instances, columns corresponding to template parameters, and cells corresponding to values applied to rules.</p>");

				// then format each template
				foreach (DocTemplateDefinition docTemplate in mapTemplates.Keys)
				{
					string[] parameters = docTemplate.GetParameterNames();

					if (parameters.Length > 0)
					{
						format.WriteLine("<h4>" + docTemplate.Name + "</h4>");
						format.WriteLine("<table class=\"gridtable\">");
						format.Write("<tr><th>Entity</th>");

						foreach (string parm in parameters)
						{
							format.Write("<th>" + parm + "</th>");
						}

						format.WriteLine("</tr>");

						List<DocConceptRoot> listRoots = mapTemplates[docTemplate];
						foreach (DocConceptRoot docRoot in listRoots)
						{
							foreach (DocTemplateUsage docConcept in docRoot.Concepts)
							{
								if (docConcept.Definition == docTemplate)
								{
									if (docConcept.Items.Count == 0)
									{
										format.WriteLine("<tr><td>" + docRoot.ApplicableEntity.Name + "</td></tr>");
									}
									else
									{
										foreach (DocTemplateItem docItem in docConcept.Items)
										{
											if (docItem == docConcept.Items[0])
											{
												format.Write("<tr><td rowspan=\"" + docConcept.Items.Count + "\">" + docRoot.ApplicableEntity.Name + "</td>");
											}
											else
											{
												format.Write("<tr>");
											}

											foreach (string parm in parameters)
											{
												string val = docItem.GetParameterValue(parm);
												format.Write("<td>" + val + "</td>");
											}

											format.WriteLine("</tr>");
										}
									}
								}
							}
						}

						format.WriteLine("</table>");
					}
				}

				format.WriteLine("<h3>7.2.4 Concept relationship description</h3>");
				format.WriteLine("<p>Concepts may inherit from other concepts such that more generic rules may be defined at a higher level and more specific rules at a lower level.  " +
					"For example, geometry may be defined for a distribution segment (e.g. ducts, pipes, cables) that indicate permitted use of an extruded area solid (IfcExtrudedAreaSolid) " +
					"which defines a 2D cross section extruded along a 3D linear segment. " +
					"Such rule may be further refined for ducts to indicate that the cross-sections are further restricted to shapes such as hollow rectangles (IfcRectangleHollowProfileDef) " +
					"or hollow circles (IfcCircleHollowProfileDef)." +
					"Concepts are shown in a hierarchy as follows where inner concepts inherit from outer concepts.</p>");
				WriteTemplateList(format, project.Templates, included);

				format.WriteLine("<h3>7.2.5 Concept requirements applicability</h3>");
				format.WriteLine("<p>Each entity is shown in subsections as follows, with rows corresponding to concepts, columns corresponding to exchanges, " +
					"and cells indicating requirements where 'R' indicates required and 'O' indicates optional.</p>");
				foreach (DocConceptRoot docRoot in sortConceptRoot)
				{
					if (docRoot.Concepts.Count > 0)
					{
						format.WriteLine("<h4>" + docRoot.ApplicableEntity.Name + "</h4>");

						format.WriteLine("<table class=\"gridtable\">");
						format.Write("<tr><th>Concept</th>");
						for (int i = 0; i < docView.Exchanges.Count; i++)
						{
							format.Write(HEADERCELL);
							format.Write(docView.Exchanges[i].Name);
							format.Write("</th>");
						}
						format.WriteLine("</tr>");

						DocExchangeRequirementEnum[] reqs = new DocExchangeRequirementEnum[docView.Exchanges.Count];

						foreach (DocTemplateUsage docConcept in docRoot.Concepts)
						{
							StringBuilder sbRequired = new StringBuilder();
							StringBuilder sbOptional = new StringBuilder();
							foreach (DocExchangeItem docExchangeItem in docConcept.Exchanges)
							{
								int index = docView.Exchanges.IndexOf(docExchangeItem.Exchange);
								reqs[index] = docExchangeItem.Requirement;
							}

							format.Write("<tr><td>" + docConcept.Definition.Name + "</td>");
							for (int i = 0; i < docView.Exchanges.Count; i++)
							{
								format.Write("<td>");
								switch (reqs[i])
								{
									case DocExchangeRequirementEnum.Mandatory:
										format.Write("R");
										break;

									case DocExchangeRequirementEnum.Optional:
										format.Write("O");
										break;
								}
								format.Write("</td>");
							}
							format.WriteLine("</tr>");
						}

						format.WriteLine("</table>");
					}
				}


				format.WriteLine("<h2>7.3 Concept reusability</h2>");
				format.WriteLine("<h3>7.3.1 Concept list</h3>");
				format.WriteLine("<p>Each concept used within this model view is listed as follows.</p>");

				format.WriteLine("<ul>");
				foreach (DocTemplateDefinition docTemplate in mapTemplates.Keys)
				{
					format.WriteLine("<li>" + docTemplate.Name + "</li>");
				}

				format.WriteLine("</ul>");

				format.WriteLine("<h3>7.3.2 Related existing concept list</h3>");
				format.WriteLine("<p>In the following table, each row corresponds to a concept used within this model view, " +
					"each column corresponds to another model view, and each cell indicates usage of the concept within the corresponding model view.</p>");
				format.WriteLine("<table class=\"gridtable\">");
				format.Write("<tr><th>Concept</th>");
				foreach (DocModelView docEachView in project.ModelViews)
				{
					format.Write(HEADERCELL + docEachView.Name + "</th>");
				}
				format.WriteLine("</tr>");

				foreach (DocTemplateDefinition docTemplate in mapTemplates.Keys)
				{
					format.Write("<tr><td>" + docTemplate.Name + "</td>");

					foreach (DocModelView docEachView in project.ModelViews)
					{
						bool yes = false;
						foreach (DocConceptRoot docEachRoot in docEachView.ConceptRoots)
						{
							foreach (DocTemplateUsage docEachUsage in docEachRoot.Concepts)
							{
								if (docEachUsage.Definition == docTemplate)
								{
									yes = true;
									break;
								}
							}
						}

						if (yes)
						{
							format.Write("<td>X</td>");
						}
						else
						{
							format.Write("<td></td>");
						}
					}

					format.WriteLine("</tr>");
				}
				format.WriteLine("</table>");

				format.WriteLine("<h3>7.3.4 Concept business rule list</h3>");
				format.WriteLine("<p>Each concept template is defined in a subsection as follows, with rows corresponding to each business rule. " +
					"The <i>Reference</i> column identifies the path to the entity and attribute. " +
					"The <i>Cardinality</i> column indicates whether the number of permitted instances is restricted differently than the underlying schema, using [N:M] notation where N indicates the minimum number of instances, M indicates the maximum number of instances, where '?' indicates unbounded. " +
					"The <i>Parameter</i> column indicates the name of a substitutable parameter, if applicable, defined at each usage of the business rule.<p>");
				foreach (DocTemplateDefinition docTemplate in mapTemplates.Keys)
				{
					format.WriteLine("<h4>" + docTemplate.Name + "</h4>");

					format.WriteLine("<table class=\"gridtable\">");
					format.WriteLine("<tr><th>Reference</th><th>Cardinality</th><th>Parameter</th></tr>");

					if (docTemplate.Rules != null)
					{
						foreach (DocModelRule docRule in docTemplate.Rules)
						{
							WriteModelRule(format, docRule, "\\" + docTemplate.Type + "." + docRule.Name);
						}
					}

					format.WriteLine("</table>");
				}

				format.WriteLine("<h3>7.3.5 Concept business rule description</h3>");
				format.WriteLine("<p>Each concept template is described in a subsection as follows, with diagrams indicating usage of attributes and entities reflecting defined business rules.</p>");
				foreach (DocTemplateDefinition docTemplate in mapTemplates.Keys)
				{
					format.WriteLine("<h4>" + docTemplate.Name + "</h4>");

					format.WriteLine(docTemplate.Documentation);

					string img = "";
					if (docTemplate.Rules != null && docTemplate.Rules.Count > 0)
					{
						img = GenerateTemplateImage(docTemplate, mapEntity, project, path);
						format.WriteLine(img);
					}
				}

				format.WriteLine("<h2>7.4 Implementation</h2>");
				format.WriteLine("<h3>7.4.1 MVD Schema Listing</h3>");
				format.WriteLine("The schema encapsulating the data definitions for this model view is published in multiple representations. " +
"<p>An MVDXML file defines the referenced entities and rules for this model view. This file may be used to validate instance data (in IFC-SPF or IFC-XML files), filter instance data to include entities and attributes within scope of this model view, or generate sub-schemas (including the EXP and XSD representations). " +
"<p>An EXP file represents the schema in EXPRESS format (ISO 10303-11) which adapts the referenced Industry Foundation Classes schema (ISO 16739) by including a subset of data definitions and a subset of attributes within each data definition. The EXP file may be used by software development tools for generating programming languages schemas (e.g. C++, C#, Java), database definitions (e.g. SQL DDL), and data transport schema definitions (e.g. XSD). " +
"<p>An XSD file represents the schema in XML Data Definition Language (XSD) which adapts the referenced subset of data definitions. The XSD file may be used by software development tools (e.g. Eclipse, Microsoft Visual Studio) to validate XML files and generate language-specific classes. " +
"<p>An IFC file represents the dynamic portions of the schema in the form of property sets within an SPF (ISO 10303-21) instance file.</p>" +
"<p>The rationale for publishing multiple representations is to provide the richest level of integration for different implementations; " +
"while XSD is often used in defining web standards replacing document-based exchanges (e.g. invoices), it lacks data model information needed for type safety, data integrity, indexing, and optimization; " +
"all of which may be derived from the EXPRESS representation. " +
"</p>");
				format.WriteLine("<table class=\"gridtable\">");
				format.WriteLine("<tr><th>File</th><th>Format</th></tr>");
				format.WriteLine("<tr><td>" + docView.Code + ".exp</td><td>EXPRESS schema definition</td></tr>");
				format.WriteLine("<tr><td>" + docView.Code + ".xsd</td><td>XML schema definition (XSD)</td></tr>");
				format.WriteLine("<tr><td>" + docView.Code + ".mvdxml</td><td>MVDXML schema transform</td></tr>");
				format.WriteLine("<tr><td>" + docView.Code + ".ifc</td><td>IFC dynamic schema definition</td></tr>");
				format.WriteLine("</table>");

				format.WriteLine("<h3>7.4.2 MVD Format Description</h3>");

				format.WriteLine("<p>Implementations of this model view may publish instance data in various formats. " +
					"Such format indicates the data encoding and does not necessarily imply that data may only be exchanged using physical files on computers; " +
					"formats may be transmitted over the Internet as the “presentation layer” (OSI Layer 6) of any API. " +
					"As the IFC data model supports both full and partial data models where all objects can be tagged to indicate merge directives (Create/Update/Delete using IfcOwnerHistory.ChangeAction), " +
					"data may be transmitted in whole or in part, such as indicating only data changes.</p>");
				format.WriteLine("<p>As other OSI layers are already standardized, a full web API may be defined by referencing each layer as follows:</p>");
				format.WriteLine("<table class=\"gridtable\">");
				format.WriteLine("<tr><th>OSI Layer</th><th>OSI Layer Name</th><th>Protocol</th><th>Description</th></tr>");
				format.WriteLine("<tr><td>7</td><td>Application</td><td>WebDav</td><td>Defines valid operations such as GET, PUT, POST, DELETE, MKCOL, LOCK, UNLOCK</td></tr>");
				format.WriteLine("<tr><td>6</td><td>Presentation</td><td>IFC-SPF/IFC-XML</td><td>Defines data encoding</td></tr>");
				format.WriteLine("<tr><td>5</td><td>Session</td><td>HTTP/HTTPS</td><td>Defines establishment of sessions, compression, authentication, requests, responses, and errors</td></tr>");
				format.WriteLine("<tr><td>4</td><td>Transport</td><td>TCP</td><td>Defines message delivery</td></tr>");
				format.WriteLine("<tr><td>3</td><td>Network</td><td>IP</td><td>Defines network paths across multiple nodes</td></tr>");
				format.WriteLine("<tr><td>2</td><td>Data Link</td><td>MAC</td><td>Defines data frame communications between two nodes</td></tr>");
				format.WriteLine("<tr><td>1</td><td>Physical</td><td>(undefined)</td><td>Defines physical connectivity</td></tr>");
				format.WriteLine("</table>");

				format.WriteLine("<p>Each supported format is listed by name, with Extension indicating the default file extension to use on applicable platforms (e.g. Windows), MIME type for indicating the HTTP header when transmitting over the Internet, and Reference standard indicating the presentation layer encoding format.</p>");

				format.WriteLine("<table class=\"gridtable\">");
				format.WriteLine("<tr><th>Format</th><th>Extension</th><th>MIME</th><th>Reference</th></tr>");
				format.WriteLine("<tr><td>IFC-SPF</td><td>.ifc</td><td>application/step</td><td>ISO 10303-21</td></tr>");
				format.WriteLine("<tr><td>IFC-XML</td><td>.ifcxml</td><td>application/xml</td><td>ISO 10303-28</td></tr>");
				format.WriteLine("</table>");

				format.WriteLine("<p>IFC-SPF (ISO 10303-21) is a text format optimized to carry data with complex relationships, supporting human readability yet more compact representation (typically around 10% of size of equivalent XML).</p>");

				format.WriteLine("<p>IFC-HDF (ISO 10303-26) is a binary file format encapsulating data in a compact, indexable encoding optimized for quick retrieval and minimal memory usage. ");
				format.WriteLine("<blockquote class=\"note\">NOTE&nbsp; As this file type is not yet widely implemented, it is not officially part of this model view, however implementations may prefer such format for internal use.</blockquote>");

				format.WriteLine("<p>IFC-XML (ISO 10303-28) is a hierarchical markup format with wide support from software development tools and platforms, supporting greater human readability at the expense of larger representation.  </p>");
				format.WriteLine("<blockquote class=\"note\">NOTE&nbsp; As typical buildings contain millions of elements with graphs of relationships resulting in gigabytes of data, " +
"XML is not yet suitable for representing complete buildings from a pragmatic standpoint of data size, transmission cost, and loading time. " +
"However, using derived formats along with MVDXML to filter data sets may enable more efficient exchanges to take place.</blockquote>");

				format.WriteLine("<p>IFC-ZIP (ISO 21320-1) is a compressed file format encapsulating one of the above formats to minimize data size. ");
				format.WriteLine("<blockquote class=\"note\">NOTE&nbsp; As this model view is primarily intended for web-based exchange, zip compression may be selected by other means according to the client and server; therefore, the IFC-ZIP format is not officially part of this model view.</blockquote>");

				format.WriteLine("<h3>7.4.3 MVD Dynamic Schema Analysis</h3>");
				format.WriteLine("<p>Portions of data definitions are defined dynamically, to allow software applications to support extensible definitions while minimizing implementation overhead. " +
					"Each property set is shown within a subsection as follows, with rows corresponding to properties. See <i>IfcPropertySet</i> for usage information.</p>");
				foreach (DocSection docSection in project.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						foreach (DocPropertySet docPset in docSchema.PropertySets)
						{
							if (included == null || included.ContainsKey(docPset))
							{
								format.WriteLine("<h4>" + docPset.Name + "</h4>");
								format.WriteLine("<table class=\"gridtable\">");
								format.WriteLine("<tr><th>Property</th><th>Property Type</th><th>Data Type</th><th>Description</th></tr>");

								foreach (DocProperty docProp in docPset.Properties)
								{
									string datatype = docProp.PrimaryDataType;
									if (!String.IsNullOrEmpty(docProp.SecondaryDataType))
									{
										datatype += "/" + docProp.SecondaryDataType;
									}

									format.WriteLine("<tr><td>" + docProp.Name + "</td><td>" + docProp.PropertyType + "</td><td>" + datatype + "</td><td>" + docProp.Documentation + "</td></tr>");
								}

								format.WriteLine("</table>");
							}
						}
					}
				}

				format.WriteLine("<h3>7.4.4 Non-Applicable Entity Exclusion Analysis</h3>");
				format.WriteLine("<p>The referenced IFC schema is shown in the following table, with each row corresponding to a schema namespace, with data definitions listed within, where bold items indicate definitions within scope of this Model View Definition.</p>");
				format.WriteLine("<table class=\"gridtable\">");
				format.WriteLine("<tr><th>Namespace</th><th>Definitions</th><th>Usage</th></tr>");
				foreach (DocSection docSection in project.Sections)
				{
					if (docSection.Schemas.Count > 0)
					{
						foreach (DocSchema docSchema in docSection.Schemas)
						{
							format.Write("<tr><td>" + docSchema.Name + "</td><td>");

							int min = 0;
							int max = 0;

							foreach (DocEntity docEntity in docSchema.Entities)
							{
								max++;
								if (included == null || included.ContainsKey(docEntity))
								{
									min++;
									format.Write("<b>");
								}
								format.Write(docEntity.Name);
								format.Write("; ");
								if (included == null || included.ContainsKey(docEntity))
								{
									format.Write("</b>");
								}
							}

							foreach (DocType docType in docSchema.Types)
							{
								max++;
								if (included == null || included.ContainsKey(docType))
								{
									min++;
									format.Write("<b>");
								}
								format.Write(docType.Name);
								format.Write("; ");
								if (included == null || included.ContainsKey(docType))
								{
									format.Write("</b>");
								}
							}

							format.Write("</td>");

							format.Write("<td>" + min + "/" + max + " (" + (100.0 * (double)min / (double)max).ToString("N0") + "%)</td>");

							format.WriteLine("</tr>");

						}
					}
				}
				format.WriteLine("</table>");

				// 8 Conformance testing procedures

				// 9 Implementation resources

				// 10 Revision Plans

				format.WriteFooter("");
			}

		}

		private static void WriteModelRule(FormatHTM format, DocModelRule docRule, string path)
		{
			format.Write("<tr><td>" + path + "</td><td>" + /*docRule.GetCardinalityExpression() + */"</td><td>" + docRule.Identification + "</td></tr>");

			if (docRule.Rules != null)
			{
				foreach (DocModelRule docSub in docRule.Rules)
				{
					if (docSub is DocModelRuleAttribute)
					{
						WriteModelRule(format, docSub, path + "." + docSub.Name);
					}
					else if (docSub is DocModelRuleEntity)
					{
						WriteModelRule(format, docSub, path + "\\" + docSub.Name);
					}
				}
			}
		}

		private static void WriteTemplateList(FormatHTM format, List<DocTemplateDefinition> list, Dictionary<DocObject, bool> included)
		{
			format.WriteLine("<ul>");
			foreach (DocTemplateDefinition docTemplate in list)
			{
				if (included == null || included.ContainsKey(docTemplate))
				{
					format.WriteLine("<li>" + docTemplate.Name); ;

					if (docTemplate.Templates != null && docTemplate.Templates.Count > 0)
					{
						WriteTemplateList(format, docTemplate.Templates, included);
					}

					format.WriteLine("</li>");
				}
			}
			format.WriteLine("</ul>");
		}
	}
}
