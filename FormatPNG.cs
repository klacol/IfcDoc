// Name:        FormatPNG.cs
// Description: Image generation
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

namespace IfcDoc.Format.PNG
{
	class FormatPNG
	{
		public const int CY = 12;  // height of each attribute
		public const int CX = 200; // width of each entity and gap
		public const int DX = 32;  // horizontal gap between each entity
		const int Border = 8;   // indent
		const double Factor = 0.375;// 0.375;

		/// <summary>
		/// Creates list of attributes for entity and supertypes.
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		public static void BuildAttributeList(DocEntity entity, List<DocAttribute> list, DocProject docProject)
		{
			DocObject docBase = null;
			if (entity.BaseDefinition != null)
			{
				docBase = docProject.GetDefinition(entity.BaseDefinition);
				if (docBase != null)
				{
					BuildAttributeList((DocEntity)docBase, list, docProject);
				}
			}

			foreach (DocAttribute docAttr in entity.Attributes)
			{
				if (!String.IsNullOrEmpty(docAttr.Derived))
				{
					// remove existing
					foreach (DocAttribute exist in list)
					{
						if (exist.Name.Equals(docAttr.Name))
						{
							list.Remove(exist);
							break;
						}
					}
				}
				else
				{
					list.Add(docAttr);
				}
			}
		}

		private static void DrawAttribute(
			Graphics g,
			int lane,
			List<int> lanes,
			DocEntity docEntity,
			DocModelView docView,
			DocModelRuleAttribute ruleAttribute,
			int offset,
			Dictionary<Rectangle, SEntity> layout,
			DocProject docProject,
			DocSchema docSchema,
			SEntity instance)
		{
			int x = lane * CX + FormatPNG.Border;
			int y = lanes[lane] + FormatPNG.Border;

			// find the index of the attribute
			List<DocAttribute> listAttr = new List<DocAttribute>();
			BuildAttributeList(docEntity, listAttr, docProject);

			int iAttr = -1;
			for (int i = 0; i < listAttr.Count; i++)
			{
				if (listAttr[i].Name.Equals(ruleAttribute.Name))
				{
					// found it
					iAttr = i;
					break;
				}
			}

			if (iAttr >= 0)
			{
				DocAttribute docAttr = listAttr[iAttr];

				object valueinstance = null;
				if (instance != null)
				{
					System.Reflection.FieldInfo field = instance.GetType().GetField(docAttr.Name);
					if (field != null)
					{
						valueinstance = field.GetValue(instance);
					}
				}

				// map it
				foreach (DocModelRule ruleEach in ruleAttribute.Rules)
				{
					if (ruleEach is DocModelRuleEntity)
					{
						DocModelRuleEntity ruleEntity = (DocModelRuleEntity)ruleEach;


						DocObject docObj = null;

						if (docSchema != null)
						{
							docObj = docSchema.GetDefinition(ruleEntity.Name);
							if (docObj is DocDefinitionRef)
								docObj = null;
						}

						if (docObj == null)
						{
							docObj = docProject.GetDefinition(ruleEntity.Name);
						}

						{
							int dest = FormatPNG.Border;
							if (lanes.Count > lane + 1)
							{
								dest = lanes[lane + 1] + FormatPNG.Border;
							}

							if (docObj is DocEntity)
							{
								DocEntity docEntityTarget = (DocEntity)docObj;

								// resolve inverse attribute                        
								List<DocAttribute> listTarget = new List<DocAttribute>();
								BuildAttributeList(docEntityTarget, listTarget, docProject);
								for (int i = 0; i < listTarget.Count; i++)
								{
									DocAttribute docAttrTarget = listTarget[i];
									if (docAttr.Inverse != null && docAttrTarget.Name.Equals(docAttr.Inverse))
									{
										// found it
										dest += CY * (i + 1);
										break;
									}
									else if (docAttrTarget.Inverse != null && docAttr.Name.Equals(docAttrTarget.Inverse))
									{
										//...also need to check for type compatibility

										bool found = false;
										DocEntity docTest = docEntity;
										while (docTest != null)
										{
											if (docTest.Name.Equals(docAttrTarget.DefinedType))
											{
												found = true;
												break;
											}

											if (docTest.BaseDefinition == null)
												break;

											DocObject docBase = docProject.GetDefinition(docTest.BaseDefinition) as DocEntity;
											if (docBase != null)
											{
												docTest = docBase as DocEntity;
											}
											else
											{
												break;
											}
										}

										// found it
										if (found)
										{
											dest += CY * (i + 1);
											break;
										}
									}
								}

								// draw the entity, recurse
								DrawEntity(g, lane + 1, lanes, docEntityTarget, docView, null, ruleEntity, layout, docProject, docSchema, valueinstance);
							}
							else
							{
								int targetY = lanes[lane + 1] + FormatPNG.Border;

								if (g != null)
								{
									Brush brush = Brushes.Black;

									if (instance != null)
									{
										if (valueinstance == null)
										{
											brush = Brushes.Red;
										}
										else if (valueinstance is System.Collections.IList)
										{
											brush = Brushes.Blue;
										}
										else
										{
											string typename = valueinstance.GetType().Name;
											if (typename == ruleEntity.Name)
											{
												brush = Brushes.Lime;
											}
											else
											{
												brush = Brushes.Red;
											}
										}
									}

									g.FillRectangle(brush, x + CX, targetY, CX - DX, CY);
									g.DrawRectangle(Pens.Black, x + CX, targetY, CX - DX, CY);
									using (Font font = new Font(FontFamily.GenericSansSerif, 8.0f))
									{
										string content = ruleEntity.Name;//docObj.Name;
										foreach (DocModelRule ruleConstraint in ruleEntity.Rules)
										{
											if (ruleConstraint is DocModelRuleConstraint)
											{
												DocModelRuleConstraint docConstraint = (DocModelRuleConstraint)ruleConstraint;

												// for now, we only support single direct rule -- future: recurse
												if (docConstraint.Expression is DocOpStatement)
												{
													DocOpStatement opStatement = (DocOpStatement)docConstraint.Expression;
													if (opStatement.Metric == DocOpCode.NoOperation &&
														opStatement.Value is DocOpLiteral)
													{
														//opStatement.Value
														DocOpLiteral opLiteral = (DocOpLiteral)opStatement.Value;
														content = opLiteral.Literal;
														using (StringFormat fmt = new StringFormat())
														{
															fmt.Alignment = StringAlignment.Far;
															g.DrawString(content, font, Brushes.White, new RectangleF(x + CX, targetY, CX - DX, CY), fmt);
														}
													}
												}
											}

#if false // legacy
                                            if (ruleConstraint.Description != null && ruleConstraint.Description.StartsWith("Value="))
                                            {
                                                content = ruleConstraint.Description.Substring(6);

                                                using (StringFormat fmt = new StringFormat())
                                                {
                                                    fmt.Alignment = StringAlignment.Far;
                                                    g.DrawString(content, font, Brushes.White, new RectangleF(x + CX, targetY, CX - DX, CY), fmt);
                                                }
                                            }
#endif
										}

										g.DrawString(ruleEntity.Name, font, Brushes.White, x + CX, targetY);

										if (ruleEntity.Identification == "Value")
										{
											// mark rule serving as default value
											g.FillEllipse(Brushes.Green, new Rectangle(x + CX - DX - CY, y, CY, CY));
										}
									}
								}

								// record rectangle
								if (layout != null)
								{
									layout.Add(new Rectangle(x + CX, targetY, CX - DX, CY), ruleEntity);
								}

								// increment lane offset for all lanes
								int minlane = targetY + CY * 2;
								int i = lane + 1;
								if (lanes[i] < minlane)
								{
									lanes[i] = minlane;
								}
							}

							// draw arrow
							if (g != null)
							{
								int x0 = x + CX - DX;
								int y0 = y + CY * (iAttr + 1) + CY / 2;
								int x1 = x + CX;
								int y1 = dest + CY / 2;
								int xM = x0 + DX / 2 - offset * 2;

								if (!String.IsNullOrEmpty(ruleAttribute.Identification))
								{
									// mark the attribute as using parameter
									//g.DrawRectangle(Pens.Blue, x, y + CY * (iAttr + 1), CX - DX, CY);
									using (Font font = new Font(FontFamily.GenericSansSerif, 8.0f, FontStyle.Regular))
									{
										using (StringFormat fmt = new StringFormat())
										{
											fmt.Alignment = StringAlignment.Near;
											g.DrawString(docAttr.Name, font, Brushes.Blue, x, y + CY * (iAttr + 1), fmt);
										}
									}
								}

								g.DrawLine(Pens.Black, x0, y0, xM, y0);
								g.DrawLine(Pens.Black, xM, y0, xM, y1);
								g.DrawLine(Pens.Black, xM, y1, x1, y1);
							}
						}
					}
				}

				if (g != null && ruleAttribute.Identification == "Name")
				{
					// mark rule serving as default name
					g.FillEllipse(Brushes.Blue, new Rectangle(x + CX - DX - CY, y + CY * (iAttr + 1), CY, CY));
				}

#if false
                string card = ruleAttribute.GetCardinalityExpression();
                if (g != null && card != null)
                {
                    card = card.Trim();
                    switch (docAttr.GetAggregation())
                    {
                        case DocAggregationEnum.SET:
                            card = "S" + card;
                            break;

                        case DocAggregationEnum.LIST:
                            card = "L" + card;
                            break;
                    }

                    int px = x + CX - DX;
                    int py = y + CY * (iAttr + 1);
                    g.FillRectangle(Brushes.White, px - CX / 5, py, CX / 5, CY);
                    using (Font font = new Font(FontFamily.GenericSansSerif, 8.0f, FontStyle.Regular))
                    {
                        using (StringFormat fmt = new StringFormat())
                        {
                            fmt.Alignment = StringAlignment.Far;
                            g.DrawString(card, font, Brushes.Blue, px, py, fmt);
                        }
                    }
                }
#endif
			}
		}

		/// <summary>
		/// Draws entity and recurses.
		/// </summary>
		/// <param name="g">Graphics device.</param>
		/// <param name="lane">Horizontal lane for which to draw the entity.</param>
		/// <param name="lanes">List of lanes left-to-right.</param>
		/// <param name="docEntity">The entity to draw.</param>
		/// <param name="docView">The model view for which to draw the entity.</param>
		/// <param name="docTemplate">The template to draw.</param>
		/// <param name="docRule">Optional rule for recursing.</param>
		/// <param name="map">Map of definitions.</param>
		/// <param name="layout">Optional layout to receive rectangles for building image map</param>
		/// <param name="docProject">Required project.</param>
		/// <param name="instance">Optional instance where included or missing attributes are highlighted.</param>
		private static void DrawEntity(
			Graphics g,
			int lane,
			List<int> lanes,
			DocEntity docEntity,
			DocModelView docView,
			DocTemplateDefinition docTemplate,
			DocModelRuleEntity docRule,
			Dictionary<Rectangle, SEntity> layout,
			DocProject docProject,
			DocSchema docSchema,
			object instance)
		{
			List<DocAttribute> listAttr = new List<DocAttribute>();
			BuildAttributeList(docEntity, listAttr, docProject);

			while (lanes.Count < lane + 1)
			{
				int miny = 0;
				if (lanes.Count > lane)
				{
					miny = lanes[lane];
				}

				lanes.Add(miny);
			}

			int x = lane * CX + FormatPNG.Border;
			int y = lanes[lane] + FormatPNG.Border;

			if (g != null)
			{
				Brush brush = Brushes.Black;

				if (instance != null)
				{
					brush = Brushes.Red;

					if (instance is System.Collections.IList)
					{
						string typename = instance.GetType().Name;

						// keep going until matching instance
						System.Collections.IList list = (System.Collections.IList)instance;
						foreach (object member in list)
						{
							string membertypename = member.GetType().Name;
							DocEntity docType = docProject.GetDefinition(membertypename) as DocEntity;
							while (docType != null)
							{
								if (docType == docEntity)
								{
									brush = Brushes.Lime;
									instance = member;
									break;
								}

								docType = docProject.GetDefinition(docType.BaseDefinition) as DocEntity;
							}

							if (brush != Brushes.Red)
								break;
						}
					}
					else
					{
						string typename = instance.GetType().Name;
						DocEntity docType = docProject.GetDefinition(typename) as DocEntity;
						while (docType != null)
						{
							if (docType == docEntity)
							{
								brush = Brushes.Lime;
								break;
							}

							docType = docProject.GetDefinition(docType.BaseDefinition) as DocEntity;
						}
					}
				}
				else if (docEntity.IsDeprecated())
				{
					brush = Brushes.DarkRed;
				}
				else if (docEntity.IsAbstract)
				{
					brush = Brushes.Gray;
				}
				else
				{
					brush = Brushes.Black;
				}
				g.FillRectangle(brush, x, y, CX - DX, CY);
				g.DrawRectangle(Pens.Black, x, y, CX - DX, CY);
				using (Font font = new Font(FontFamily.GenericSansSerif, 8.0f, FontStyle.Bold))
				{
					g.DrawString(docEntity.Name, font, Brushes.White, x, y);
				}

				if (docRule != null && docRule.Identification == "Value")
				{
					// mark rule serving as default value
					g.FillEllipse(Brushes.Green, new Rectangle(x + CX - DX - CY, y, CY, CY));
				}

				g.DrawRectangle(Pens.Black, x, y + CY, CX - DX, CY * listAttr.Count);
				using (Font font = new Font(FontFamily.GenericSansSerif, 8.0f, FontStyle.Regular))
				{
					for (int iAttr = 0; iAttr < listAttr.Count; iAttr++)
					{
						DocAttribute docAttr = listAttr[iAttr];

						string display = docAttr.GetAggregationExpression();
						brush = Brushes.Black;

						if (docAttr.IsDeprecated())
						{
							brush = Brushes.DarkRed;
						}

						if (docAttr.Inverse != null)
						{
							brush = Brushes.Gray;
						}

						if (String.IsNullOrEmpty(display))
						{
							if (docAttr.IsOptional)
							{
								display = "[0:1]";
							}
							else
							{
								display = "[1:1]";
							}
						}

						g.DrawString(docAttr.Name, font, brush, x, y + CY * (iAttr + 1));
						using (StringFormat fmt = new StringFormat())
						{
							fmt.Alignment = StringAlignment.Far;
							g.DrawString(display, font, brush, new RectangleF(x, y + CY * (iAttr + 1), CX - DX, CY), fmt);
						}

					}
				}
			}

			// record rectangle
			if (layout != null)
			{
				try
				{
					layout.Add(new Rectangle(x, y, CX - DX, CY + CY * listAttr.Count), docRule);
				}
				catch
				{
					//...
				}
			}

			SortedList<int, List<DocModelRuleAttribute>> mapAttribute = new SortedList<int, List<DocModelRuleAttribute>>();
			Dictionary<DocModelRuleAttribute, DocTemplateDefinition> mapTemplate = new Dictionary<DocModelRuleAttribute, DocTemplateDefinition>();
			if (docRule != null && docRule.Rules != null)
			{
				// map inner rules

				// sort
				foreach (DocModelRule rule in docRule.Rules)
				{
					if (rule is DocModelRuleAttribute)
					{
						DocModelRuleAttribute ruleAttribute = (DocModelRuleAttribute)rule;
						for (int i = 0; i < listAttr.Count; i++)
						{
							if (listAttr[i].Name.Equals(ruleAttribute.Name))
							{
								// found it
								if (!mapAttribute.ContainsKey(i))
								{
									mapAttribute.Add(i, new List<DocModelRuleAttribute>());
								}

								mapAttribute[i].Add(ruleAttribute);
								break;
							}
						}
					}
				}
			}
			else if (docTemplate != null)
			{
				if (docTemplate.Rules != null)
				{
					foreach (DocModelRuleAttribute ruleAttribute in docTemplate.Rules)
					{
						for (int i = 0; i < listAttr.Count; i++)
						{
							if (listAttr[i].Name != null && listAttr[i].Name.Equals(ruleAttribute.Name))
							{
								// found it
								//iAttr = i;
								if (!mapAttribute.ContainsKey(i))
								{
									mapAttribute.Add(i, new List<DocModelRuleAttribute>());
								}
								mapAttribute[i].Add(ruleAttribute);
								break;
							}
						}
					}
				}
			}
			else
			{
				// map each use definition at top-level              

				// build list of inherited views
				List<DocModelView> listViews = new List<DocModelView>();
				DocModelView docBaseView = docView;
				while (docBaseView != null)
				{
					listViews.Add(docBaseView);

					if (!String.IsNullOrEmpty(docBaseView.BaseView))
					{
						Guid guidBase = Guid.Parse(docBaseView.BaseView);
						if (guidBase != docBaseView.Uuid)
						{
							docBaseView = docProject.GetView(guidBase);
						}
						else
						{
							docBaseView = null;
						}
					}
					else
					{
						docBaseView = null;
					}
				}

				// build from inherited entities too

				List<DocTemplateDefinition> listTemplates = new List<DocTemplateDefinition>(); // keep track of templates so we don't repeat at supertypes
				List<DocTemplateDefinition> listSuppress = new List<DocTemplateDefinition>(); // list of templates that are suppressed

				DocEntity docEntitySuper = docEntity;
				while (docEntitySuper != null)
				{

					foreach (DocModelView docEachView in docProject.ModelViews)
					{
						if (docView == null || listViews.Contains(docEachView))
						{
							foreach (DocConceptRoot docRoot in docEachView.ConceptRoots)
							{
								if (docRoot.ApplicableEntity == docEntitySuper)
								{
									foreach (DocTemplateUsage docUsage in docRoot.Concepts)
									{
										if (docUsage.Definition != null && docUsage.Definition.Rules != null && !listTemplates.Contains(docUsage.Definition) && !listSuppress.Contains(docUsage.Definition))
										{
											if (docUsage.Suppress)
											{
												listSuppress.Add(docUsage.Definition);
											}
											else
											{

												listTemplates.Add(docUsage.Definition);

												foreach (DocModelRuleAttribute ruleAttribute in docUsage.Definition.Rules)
												{
													for (int i = 0; i < listAttr.Count; i++)
													{
														if (listAttr[i].Name.Equals(ruleAttribute.Name))
														{
															// found it                                
															if (!mapAttribute.ContainsKey(i))
															{
																mapAttribute.Add(i, new List<DocModelRuleAttribute>());
															}

															mapAttribute[i].Add(ruleAttribute);
															if (!mapTemplate.ContainsKey(ruleAttribute))
															{
																mapTemplate.Add(ruleAttribute, docUsage.Definition);
															}
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

					DocObject docTest = null;
					if (docEntitySuper.BaseDefinition != null)
					{
						docEntitySuper = docProject.GetDefinition(docEntitySuper.BaseDefinition) as DocEntity;
					}
					else
					{
						docEntitySuper = null;
					}
				}
			}

			int offset = -mapAttribute.Values.Count / 2;

			if (docRule != null)
			{
				offset -= docRule.References.Count / 2;

				foreach (DocTemplateDefinition dtdRef in docRule.References)
				{
					int targetY = lanes[lane + 1] + FormatPNG.Border;

					if (g != null)
					{
						// draw the template box
						g.FillRectangle(Brushes.Blue, x + CX, targetY, CX - DX, CY);
						g.DrawRectangle(Pens.Blue, x + CX, targetY, CX - DX, CY);
						using (Font font = new Font(FontFamily.GenericSansSerif, 8.0f))
						{
							g.DrawString(dtdRef.Name, font, Brushes.White, x + CX, targetY);
						}

						// draw the line connecting entity to the template
						int dest = FormatPNG.Border;
						if (lanes.Count > lane + 1)
						{
							dest = lanes[lane + 1] + FormatPNG.Border;
						}

						int x0 = x + CX - DX;
						int y0 = y + CY / 2;
						int x1 = x + CX;
						int y1 = dest + CY / 2;
						int xM = x0 + DX / 2 - offset * 2;

						g.DrawLine(Pens.Blue, x0, y0, xM, y0);
						g.DrawLine(Pens.Blue, xM, y0, xM, y1);
						g.DrawLine(Pens.Blue, xM, y1, x1, y1);

					}

					// record rectangle
					if (layout != null)
					{
						layout.Add(new Rectangle(x + CX, targetY, CX - DX, CY), dtdRef);
					}

					// increment lane offset for all lanes
					int tempminlane = targetY + CY * 2;
					int i = lane + 1;
					if (lanes[i] < tempminlane)
					{
						lanes[i] = tempminlane;
					}

					offset++;
				}
			}

			DocTemplateDefinition lastTemplate = null;
			foreach (List<DocModelRuleAttribute> listSort in mapAttribute.Values)
			{
				if (docRule == null && docTemplate == null)
				{
					// offset all lanes
					int maxlane = 0;
					for (int i = 1; i < lanes.Count; i++)
					{
						if (lanes[i] > maxlane)
						{
							maxlane = lanes[i];
						}
					}

					for (int i = 1; i < lanes.Count; i++)
					{
						lanes[i] = maxlane;
					}
				}

				foreach (DocModelRuleAttribute ruleAttributeSort in listSort)
				{
					// indicate each template
					DocTemplateDefinition eachTemplate = null;
					if (mapTemplate.TryGetValue(ruleAttributeSort, out eachTemplate))
					{
						// offset for use definition
						int minlan = 0;
						for (int i = 1; i < lanes.Count; i++)
						{
							if (eachTemplate != lastTemplate)
							{
								lanes[i] += CY * 2;
							}

							if (lanes[i] > minlan)
							{
								minlan = lanes[i];
							}
						}

						// verify this...
						for (int i = 1; i < lanes.Count; i++)
						{
							if (lanes[i] < minlan)
							{
								lanes[i] = minlan;
							}
						}

						if (g != null && eachTemplate != lastTemplate)
						{
							using (Font font = new Font(FontFamily.GenericSansSerif, 8.0f, FontStyle.Italic))
							{
								g.DrawString(eachTemplate.Name, font, Brushes.Gray, CX + FormatPNG.Border, lanes[1] - CY * 2 + FormatPNG.Border);
							}
							int lineY = lanes[1] - CY * 2 + FormatPNG.Border;
							g.DrawLine(Pens.Gray, CX + FormatPNG.Border, lineY, 1920, lineY);
						}

						lastTemplate = eachTemplate;
					}

					DrawAttribute(g, lane, lanes, docEntity, docView, ruleAttributeSort, offset, layout, docProject, docSchema, instance as SEntity);
				}
				offset++;
			}

			// increment lane offset
			int minlane = y + CY * (listAttr.Count + 2);
			if (lanes[lane] < minlane)
			{
				lanes[lane] = minlane;
			}
		}

		/// <summary>
		/// Creates a concept diagram for a particular entity, including all concepts at specified view and base view(s).
		/// </summary>
		/// <param name="docEntity"></param>
		/// <param name="docView"></param>
		/// <param name="map"></param>
		/// <param name="layout"></param>
		/// <param name="docProject"></param>
		/// <param name="instance"></param>
		/// <returns></returns>
		internal static Image CreateConceptDiagram(DocEntity docEntity, DocModelView docView, Dictionary<Rectangle, SEntity> layout, DocProject docProject, object instance)
		{
			DocSchema docSchema = docProject.GetSchemaOfDefinition(docEntity);

			layout.Clear();
			List<int> lanes = new List<int>(); // keep track of position offsets in each lane
			for (int i = 0; i < 16; i++)
			{
				lanes.Add(0);
			}

			// determine boundaries
			DrawEntity(null, 0, lanes, docEntity, docView, null, null, layout, docProject, docSchema, instance);
			Rectangle rcBounds = Rectangle.Empty;
			foreach (Rectangle rc in layout.Keys)
			{
				if (rc.Right > rcBounds.Width)
				{
					rcBounds.Width = rc.Right;
				}

				if (rc.Bottom > rcBounds.Bottom)
				{
					rcBounds.Height = rc.Bottom;
				}
			}
			rcBounds.Width += FormatPNG.Border;
			rcBounds.Height += FormatPNG.Border;

			Image image = new Bitmap(rcBounds.Width, rcBounds.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			using (Graphics g = Graphics.FromImage(image))
			{
				g.FillRectangle(Brushes.White, new Rectangle(0, 0, image.Width, image.Height));

				layout.Clear();
				lanes = new List<int>(); // keep track of position offsets in each lane
				for (int i = 0; i < 16; i++)
				{
					lanes.Add(0);
				}

				DrawEntity(g, 0, lanes, docEntity, docView, null, null, layout, docProject, docSchema, instance);

				g.DrawRectangle(Pens.Black, 0, 0, rcBounds.Width - 1, rcBounds.Height - 1);
			}

			return image;
		}

		/// <summary>
		/// Creates an instance diagram for a template.
		/// </summary>
		/// <param name="docTemplate"></param>
		/// <returns></returns>
		internal static Image CreateTemplateDiagram(DocTemplateDefinition docTemplate, Dictionary<Rectangle, SEntity> layout, DocProject docProject, object instance)
		{
			if (docTemplate.Type == null)
				return null;

			DocObject docTarget = docProject.GetDefinition(docTemplate.Type) as DocEntity;
			if (docTarget == null)
				return null;

			DocEntity docEntity = (DocEntity)docTarget;
			DocSchema docSchema = docProject.GetSchemaOfDefinition(docEntity);

			// first, determine bounds of drawing
			List<int> lanes = new List<int>(); // keep track of position offsets in each lane
			for (int i = 0; i < 16; i++)
			{
				lanes.Add(0);
			}
			layout.Clear();
			DrawEntity(null, 0, lanes, docEntity, null, docTemplate, null, layout, docProject, docSchema, instance);

			Rectangle rcBounds = new Rectangle();
			foreach (Rectangle rc in layout.Keys)
			{
				if (rc.Right > rcBounds.Right)
				{
					rcBounds.Width = rc.Right;
				}

				if (rc.Bottom > rcBounds.Bottom)
				{
					rcBounds.Height = rc.Bottom;
				}
			}
			rcBounds.Width += FormatPNG.Border;
			rcBounds.Height += FormatPNG.Border;

			Image image = new Bitmap(rcBounds.Width, rcBounds.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			using (Graphics g = Graphics.FromImage(image))
			{
				g.FillRectangle(Brushes.White, new Rectangle(0, 0, image.Width, image.Height));

				lanes = new List<int>(); // keep track of position offsets in each lane
				for (int i = 0; i < 16; i++)
				{
					lanes.Add(0);
				}
				layout.Clear();
				DrawEntity(g, 0, lanes, docEntity, null, docTemplate, null, layout, docProject, docSchema, instance);
				g.DrawRectangle(Pens.Black, 0, 0, rcBounds.Width - 1, rcBounds.Height - 1);
			}

			return image;
		}

		private static void DrawRoundedRectangle(Graphics g, Rectangle rc, int radius, Pen pen, Brush brush)
		{
			if (radius < 1)
				radius = 1;

			GraphicsPath path = new GraphicsPath();
			path.AddArc(rc.X, rc.Y, radius, radius, 180.0f, 90.0f);
			path.AddArc(rc.X + rc.Width - radius, rc.Y, radius, radius, 270.0f, 90.0f);
			path.AddArc(rc.X + rc.Width - radius, rc.Y + rc.Height - radius, radius, radius, 0.0f, 90.0f);
			path.AddArc(rc.X, rc.Y + rc.Height - radius, radius, radius, 90.0f, 90);
			path.CloseAllFigures();

			g.FillPath(brush, path);
			g.DrawPath(pen, path);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="g"></param>
		/// <param name="font"></param>
		/// <param name="fontBold"></param>
		/// <param name="fontBoldItalic"></param>
		/// <param name="sf"></param>
		/// <param name="sfLeft"></param>
		/// <param name="penDash"></param>
		/// <param name="docType"></param>
		/// <param name="docRectangle">Optional rectangle to use for a referenced definition</param>
		/// <param name="map"></param>
		/// <param name="format"></param>
		internal static void DrawSchemaDefinition(Graphics g,
			Font font, Font fontBold, Font fontBoldItalic,
			StringFormat sf, StringFormat sfLeft,
			Pen penDash,
			DocDefinition docType,
			DocRectangle docRectangle,
			DocProject docProject, DiagramFormat format)
		{
			Rectangle rc;
			if (docRectangle != null)
			{
				rc = new Rectangle(
					(int)(docRectangle.X * Factor),
					(int)(docRectangle.Y * Factor),
					(int)(docRectangle.Width * Factor),
					(int)(docRectangle.Height * Factor));
			}
			else if (docType.DiagramRectangle != null)
			{
				rc = new Rectangle(
					(int)(docType.DiagramRectangle.X * Factor),
					(int)(docType.DiagramRectangle.Y * Factor),
					(int)(docType.DiagramRectangle.Width * Factor),
					(int)(docType.DiagramRectangle.Height * Factor));
			}
			else
			{
				return;
			}

			if (docType is DocEntity)
			{
				string caption = docType.Name;

				DocEntity docEntity = (DocEntity)docType;

				if (format == DiagramFormat.ExpressG)
				{
					if (docEntity.WhereRules.Count > 0 || docEntity.UniqueRules.Count > 0)
					{
						caption = "*" + caption;
					}
					if (docEntity.IsAbstract)
					{
						caption = "(ABS)\r\n" + caption;
					}

					if (docType.IsDeprecated())
					{
						g.FillRectangle(Brushes.DarkRed, rc);
					}
					else
					{
						g.FillRectangle(Brushes.Yellow, rc);
					}
					g.DrawRectangle(Pens.Black, rc);
					g.DrawString(caption, fontBold, Brushes.Black, rc, sf);
				}
				else if (format == DiagramFormat.UML)
				{
					g.FillRectangle(Brushes.LightYellow, rc);
					g.DrawRectangle(Pens.Red, rc);

					Rectangle rcTop = rc;
					rcTop.Height = 16;

					if (docEntity.IsAbstract)
					{
						g.DrawString(caption, fontBoldItalic, Brushes.Black, rcTop, sf);
					}
					else
					{
						g.DrawString(caption, fontBold, Brushes.Black, rcTop, sf);
					}

					g.DrawLine(Pens.Black, rcTop.Left, rcTop.Bottom, rcTop.Right, rcTop.Bottom);

					// attributes of value types...
					int y = rcTop.Bottom;
					foreach (DocAttribute docAttr in docEntity.Attributes)
					{
						DocObject docAttrType = docProject.GetDefinition(docAttr.DefinedType);

						// include native types, enumerations, and defined types
						if (docAttrType == null || docAttrType is DocEnumeration || docAttrType is DocDefined)
						{
							Rectangle rcAttr = new Rectangle(rc.Left, y, rc.Width, 12);
							g.DrawString(docAttr.Name + ":" + docAttr.DefinedType, font, Brushes.Black, rcAttr, sfLeft);
							y += 12;
						}
					}
				}


				if (docRectangle == null)
				{
					foreach (DocAttribute docAttr in docEntity.Attributes)
					{
						bool include = true;
						if (format == DiagramFormat.UML)
						{
							DocObject docAttrType = docProject.GetDefinition(docAttr.DefinedType);
							include = (docAttrType is DocEntity || docAttrType is DocSelect);
						}

						if (include)
						{
							if (docAttr.DiagramLine != null)
							{
								Pen pen = new Pen(System.Drawing.Color.Black);

								if (docAttr.IsOptional)
								{
									pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
								}

								if (docAttr.IsDeprecated())
								{
									pen.Color = Color.DarkRed;
								}

								using (pen)
								{
									DrawLine(g, pen, docAttr.DiagramLine, format);
								}

							}

							if (docAttr.DiagramLabel != null && docAttr.DiagramLine != null)
							{
								if (format == DiagramFormat.ExpressG)
								{
									caption = docAttr.Name;
									if (!String.IsNullOrEmpty(docAttr.Derived))
									{
										caption = "(DER) " + caption;
									}
									else if (!String.IsNullOrEmpty(docAttr.Inverse))
									{
										caption = "(" + docAttr.DefinedType + "." + docAttr.Inverse + ")\r\n(INV) " + caption;
									}
									if (docAttr.GetAggregation() != DocAggregationEnum.NONE)
									{
										caption += " " + docAttr.GetAggregationExpression();
									}

									// determine X/Y based on midpoint of stated coordinate and target attribute
									double x = (docAttr.DiagramLabel.X + docAttr.DiagramLine[docAttr.DiagramLine.Count - 1].X) * 0.5 * Factor;
									double y = docAttr.DiagramLabel.Y * Factor;
									g.DrawString(caption, font, Brushes.Black, (int)x, (int)y, sf);
								}
								else if (format == DiagramFormat.UML)
								{
									double xHead = docAttr.DiagramLine[0].X * Factor;

									double x = docAttr.DiagramLine[docAttr.DiagramLine.Count - 1].X * Factor;
									double y = docAttr.DiagramLine[docAttr.DiagramLine.Count - 1].Y * Factor;

									StringFormat sfFar = new StringFormat();
									if (x > xHead)
									{
										sfFar.Alignment = StringAlignment.Far;
										x -= 8;
									}
									else
									{
										sfFar.Alignment = StringAlignment.Near;
										x += 8;
									}
									sfFar.LineAlignment = StringAlignment.Far;
									g.DrawString(docAttr.Name, font, Brushes.Black, (int)x, (int)y, sfFar);
									sfFar.LineAlignment = StringAlignment.Near;
									g.DrawString(docAttr.GetAggregationExpression(), font, Brushes.Black, (int)x, (int)y, sfFar);
								}
							}
						}

						foreach (DocLine docSub in docEntity.Tree)
						{
							DrawTree(g, docSub, Factor, Point.Empty, format);
						}
					}
				}
			}
			else if (docType is DocType && docType.DiagramRectangle != null)
			{
				if (format == DiagramFormat.ExpressG)
				{
					g.FillRectangle(Brushes.Lime, rc);
					g.DrawRectangle(penDash, rc);
					g.DrawString(docType.Name, font, Brushes.Black, rc, sf);

					if (docType is DocEnumeration)
					{
						g.DrawLine(penDash, rc.Right - 6, rc.Top, rc.Right - 6, rc.Bottom);
					}
					else if (docType is DocSelect)
					{
						g.DrawLine(penDash, rc.Left + 6, rc.Top, rc.Left + 6, rc.Bottom);
					}
					else if (docType is DocDefined)
					{
						DocDefined docItem = (DocDefined)docType;
						if (docItem.DiagramLine.Count > 0)
						{
							DrawLine(g, Pens.Black, docItem.DiagramLine, format);
						}
					}
				}
				else if (format == DiagramFormat.UML)
				{
					g.FillRectangle(Brushes.LightYellow, rc);
					g.DrawRectangle(Pens.Black, rc);

					Rectangle rcTop = rc;
					rcTop.Height = 8;

					Rectangle rcName = new Rectangle(rc.X, rc.Y + 8, rc.Width, 16);
					string typename = null;
					if (docType is DocEnumeration)
					{
						typename = "enumeration";
						g.DrawLine(Pens.Black, rcName.X, rcName.Bottom, rcName.Right, rcName.Bottom);
					}
					else if (docType is DocSelect)
					{
						typename = "interface";
					}
					else if (docType is DocDefined)
					{
						typename = "datatype";
					}

					g.DrawString(Char.ConvertFromUtf32(0xAB) + typename + Char.ConvertFromUtf32(0xBB), font, Brushes.Black, rcTop, sf);
					g.DrawString(docType.Name, font, Brushes.Black, rcName, sf);

					// members of enumeration...
				}


				if (docType is DocSelect)
				{
					DocSelect docSelect = (DocSelect)docType;

					if (docSelect.Tree != null)
					{
						foreach (DocLine docItem in docSelect.Tree)
						{
							if (docItem.Definition != null)
							{
								DrawLine(g, Pens.Black, docItem.DiagramLine, format);
							}
							else
							{
								// tree structure -- don't draw endcap
								for (int i = 0; i < docItem.DiagramLine.Count - 1; i++)
								{
									g.DrawLine(Pens.Black,
										new Point((int)(docItem.DiagramLine[i].X * Factor), (int)(docItem.DiagramLine[i].Y * Factor)),
										new Point((int)(docItem.DiagramLine[i + 1].X * Factor), (int)(docItem.DiagramLine[i + 1].Y * Factor)));
								}

								foreach (DocLine docItem2 in docItem.Tree)
								{
									// link parent if necessary (needed for imported vex diagrams)
									g.DrawLine(Pens.Black,
										new Point((int)(docItem.DiagramLine[docItem.DiagramLine.Count - 1].X * Factor), (int)(docItem.DiagramLine[docItem.DiagramLine.Count - 1].Y * Factor)),
										new Point((int)(docItem2.DiagramLine[0].X * Factor), (int)(docItem2.DiagramLine[0].Y * Factor)));

									DrawLine(g, Pens.Black, docItem2.DiagramLine, format);
								}
							}
						}
					}
				}
			}

		}

		internal static Image CreateSchemaDiagram(DocSchema docSchema, DocProject docProject, DiagramFormat format)
		{
			float pageX = (float)CtlExpressG.PageX;
			float pageY = (float)CtlExpressG.PageY;

			int cDiagrams = docSchema.UpdateDiagramPageNumbers();
			int cPagesY = docSchema.DiagramPagesVert;
			int cPagesX = docSchema.DiagramPagesHorz;
			if (cPagesX == 0 || cPagesY == 0)
			{
				// fallback if using earlier version without diagram info
				cPagesX = cDiagrams;
				cPagesY = 1;

				if (cPagesX == 0)
					cPagesX = 1;
			}

			int xTotal = cPagesX * (int)pageX;
			int yTotal = cPagesY * (int)pageY;
			Image image = new Bitmap(xTotal, yTotal, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			using (Graphics g = Graphics.FromImage(image))
			{
				Pen penDash = new Pen(System.Drawing.Color.Black);
				penDash.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
				using (penDash)
				{
					g.FillRectangle(Brushes.White, new Rectangle(0, 0, image.Width, image.Height));

					if (format == DiagramFormat.ExpressG)
					{
						for (float x = 0; x <= image.Width; x += pageX)
						{
							g.DrawLine(Pens.Green, new PointF(x - 1, 0.0f), new PointF(x - 1, (float)image.Height - 1.0f));
							g.DrawLine(Pens.Green, new PointF(x, 0.0f), new PointF(x, (float)image.Height - 1.0f));
						}
						for (float y = 0; y <= image.Height; y += pageY)
						{
							g.DrawLine(Pens.Green, new PointF(0.0f, y - 1), new PointF((float)image.Width - 1.0f, y - 1));
							g.DrawLine(Pens.Green, new PointF(0.0f, y), new PointF((float)image.Width - 1.0f, y));
						}
					}

					StringFormat sf = new StringFormat(StringFormat.GenericDefault);
					sf.Alignment = StringAlignment.Center;
					sf.LineAlignment = StringAlignment.Center;

					StringFormat sfLeft = new StringFormat(StringFormat.GenericDefault);
					sfLeft.Alignment = StringAlignment.Near;
					sfLeft.LineAlignment = StringAlignment.Near;
					sfLeft.FormatFlags = StringFormatFlags.NoWrap;

					using (Font font = new Font(FontFamily.GenericSansSerif, 7.0f))
					{
						using (Font fontBold = new Font(font, FontStyle.Bold))
						{
							using (Font fontBoldItalic = new Font(font, FontStyle.Bold | FontStyle.Italic))
							{
								foreach (DocType docType in docSchema.Types)
								{
									DrawSchemaDefinition(g, font, fontBold, fontBoldItalic, sf, sfLeft, penDash, docType, null, docProject, format);
								}

								foreach (DocEntity docType in docSchema.Entities)
								{
									DrawSchemaDefinition(g, font, fontBold, fontBoldItalic, sf, sfLeft, penDash, docType, null, docProject, format);
								}

								if (docSchema.PageTargets != null)
								{
									using (Pen penRound = new Pen(Color.Black))
									{
										penRound.StartCap = LineCap.Round;
										penRound.EndCap = LineCap.Round;

										foreach (DocPageTarget docTarget in docSchema.PageTargets)
										{
											bool include = true;
											if (format == DiagramFormat.UML)
											{
												DocObject docRef = docProject.GetDefinition(docTarget.Definition.Name);
												if (docRef != null)
												{
													include = (docRef is DocEntity || docRef is DocSelect);
												}
											}

											if (include)
											{


												int page = docSchema.GetDefinitionPageNumber(docTarget);
												int item = docSchema.GetPageTargetItemNumber(docTarget);
												string caption = page + "," + item;

												if (docTarget.DiagramRectangle != null && format == DiagramFormat.ExpressG)
												{
													Rectangle rc = new Rectangle(
														(int)(docTarget.DiagramRectangle.X * Factor),
														(int)(docTarget.DiagramRectangle.Y * Factor),
														(int)(docTarget.DiagramRectangle.Width * Factor),
														(int)(docTarget.DiagramRectangle.Height * Factor));

													DrawRoundedRectangle(g, rc, (int)(docTarget.DiagramRectangle.Height * Factor), penRound, Brushes.Silver);
													g.DrawString(caption, font, Brushes.Black, rc, sf);

													if (docTarget.DiagramLine != null)
													{
														using (Pen penBlue = new Pen(Color.Blue, 2.0f))
														{
															for (int i = 0; i < docTarget.DiagramLine.Count - 1; i++)
															{
																Point ptA = new Point((int)(docTarget.DiagramLine[i].X * Factor), (int)(docTarget.DiagramLine[i].Y * Factor));
																Point ptB = new Point((int)(docTarget.DiagramLine[i + 1].X * Factor), (int)(docTarget.DiagramLine[i + 1].Y * Factor));
																int cap = 0;
																if (i == docTarget.DiagramLine.Count - 2 && format == DiagramFormat.ExpressG)
																{
																	cap = DrawEndCap(g, ptA, ptB, format);
																	// adjust for cap size
																	if (ptB.X > ptA.X)
																	{
																		ptB.X -= cap;
																	}
																	else if (ptB.X < ptA.X)
																	{
																		ptB.X += cap;
																	}
																	if (ptB.Y > ptA.Y)
																	{
																		ptB.Y -= cap;
																	}
																	else if (ptB.Y < ptA.Y)
																	{
																		ptB.Y += cap;
																	}

																}

																g.DrawLine(penBlue, ptA, ptB);
															}
														}
													}
												}

												foreach (DocPageSource docSource in docTarget.Sources)
												{
													if (docSource.DiagramRectangle != null)
													{
														if (format == DiagramFormat.UML)
														{
															DrawSchemaDefinition(g, font, fontBold, fontBoldItalic, sf, sfLeft, penDash, docTarget.Definition, docSource.DiagramRectangle, docProject, format);
														}
														else
														{
															Rectangle rc = new Rectangle(
																(int)(docSource.DiagramRectangle.X * Factor),
																(int)(docSource.DiagramRectangle.Y * Factor),
																(int)(docSource.DiagramRectangle.Width * Factor),
																(int)(docSource.DiagramRectangle.Height * Factor));
															DrawRoundedRectangle(g, rc, (int)(docSource.DiagramRectangle.Height * Factor), penRound, Brushes.Silver);

															string name = docSource.Name;
															if (docTarget.Definition != null)
															{
																name = page + "," + item + " " + docTarget.Definition.Name;
															}

															g.DrawString(name, font, Brushes.Black, rc, sf);
														}
													}
												}
											}
										}
									}
								}

								if (docSchema.SchemaRefs != null)
								{
									foreach (DocSchemaRef docSchemaRef in docSchema.SchemaRefs)
									{
										foreach (DocDefinitionRef docDefRef in docSchemaRef.Definitions)
										{
											bool include = true;
											if (format == DiagramFormat.UML)
											{
												DocObject docRef = docProject.GetDefinition(docDefRef.Name);
												if (docRef != null)
												{
													include = (docRef is DocEntity || docRef is DocSelect);
												}
											}

											if (include && docDefRef.DiagramRectangle != null)
											{
												Rectangle rc = new Rectangle(
													(int)(docDefRef.DiagramRectangle.X * Factor),
													(int)(docDefRef.DiagramRectangle.Y * Factor),
													(int)(docDefRef.DiagramRectangle.Width * Factor),
													(int)(docDefRef.DiagramRectangle.Height * Factor));

												if (format == DiagramFormat.UML)
												{
													// embed referenced definition
													DocObject docObjRef = docProject.GetDefinition(docDefRef.Name);
													if (docObjRef is DocDefinition)
													{
														DrawSchemaDefinition(g, font, fontBold, fontBoldItalic, sf, sfLeft, penDash, (DocDefinition)docObjRef, docDefRef.DiagramRectangle, docProject, format);

														// UML only (not in original EXPRESS-G diagrams)
														foreach (DocAttributeRef docAttrRef in docDefRef.AttributeRefs)
														{
															//... also need to capture attribute name...
															DrawLine(g, Pens.Black, docAttrRef.DiagramLine, format);
														}
													}
												}
												else
												{
													Rectangle rcInner = rc;
													rcInner.Y = rc.Y + rc.Height / 3;
													rcInner.Height = rc.Height / 3;

													g.FillRectangle(Brushes.Silver, rc);
													g.DrawRectangle(penDash, rc);
													DrawRoundedRectangle(g, rcInner, 8, Pens.Black, Brushes.Silver);

													//rc.Y -= 6;
													rc.Height = 12;
													g.DrawString(docSchemaRef.Name.ToUpper(), font, Brushes.Black, rc, sf);

													//rc.Y += 12;
													rc.Y = rcInner.Y;
													g.DrawString(docDefRef.Name, font, Brushes.Black, rc, sf);
												}

												if (docDefRef.Tree != null)
												{
													foreach (DocLine docSub in docDefRef.Tree)
													{
														DrawTree(g, docSub, Factor, Point.Empty, format);
													}
												}
											}
										}
									}
								}

								if (docSchema.Comments != null && format == DiagramFormat.ExpressG)
								{
									using (Font fontItalic = new Font(font, FontStyle.Italic))
									{
										foreach (DocComment docComment in docSchema.Comments)
										{
											if (docComment.DiagramRectangle != null)
											{
												Rectangle rc = new Rectangle(
													(int)(docComment.DiagramRectangle.X * Factor),
													(int)(docComment.DiagramRectangle.Y * Factor),
													(int)(docComment.DiagramRectangle.Width * Factor),
													(int)(docComment.DiagramRectangle.Height * Factor));
												g.DrawString(docComment.Documentation, fontItalic, Brushes.Blue, rc, sf);
											}
										}
									}
								}

								if (docSchema.Primitives != null && format == DiagramFormat.ExpressG)
								{
									foreach (DocPrimitive docPrimitive in docSchema.Primitives)
									{
										if (docPrimitive.DiagramRectangle != null)
										{
											Rectangle rc = new Rectangle(
												(int)(docPrimitive.DiagramRectangle.X * Factor),
												(int)(docPrimitive.DiagramRectangle.Y * Factor),
												(int)(docPrimitive.DiagramRectangle.Width * Factor),
												(int)(docPrimitive.DiagramRectangle.Height * Factor));

											g.FillRectangle(Brushes.Lime, rc);
											g.DrawRectangle(Pens.Black, rc);
											g.DrawString(docPrimitive.Name, font, Brushes.Black, rc, sf);

											g.DrawLine(Pens.Black, rc.Right - 6, rc.Top, rc.Right - 6, rc.Bottom);
										}
									}
								}
							}
						}
					}
				}
			}

			return image;
		}

		private static void DrawLine(Graphics g, Pen pen, List<DocPoint> line, DiagramFormat format)
		{
			for (int i = 0; i < line.Count - 1; i++)
			{
				Point ptA = new Point((int)(line[i].X * Factor), (int)(line[i].Y * Factor));
				Point ptB = new Point((int)(line[i + 1].X * Factor), (int)(line[i + 1].Y * Factor));
				if (i == line.Count - 2)
				{
					int cap = DrawEndCap(g, ptA, ptB, format);

					// adjust for cap size
					if (ptB.X > ptA.X)
					{
						ptB.X -= cap;
					}
					else if (ptB.X < ptA.X)
					{
						ptB.X += cap;
					}
					if (ptB.Y > ptA.Y)
					{
						ptB.Y -= cap;
					}
					else if (ptB.Y < ptA.Y)
					{
						ptB.Y += cap;
					}
				}

				g.DrawLine(pen, ptA, ptB);
			}
		}

		private static int DrawEndCap(Graphics g, Point ptA, Point ptB, DiagramFormat format)
		{
			int rad = 4;
			int mul = 1;

			if (format == DiagramFormat.UML)
			{
				rad = 5;
				mul = 1;
			}

			Rectangle rc = new Rectangle(ptB.X - rad, ptB.Y - rad, rad * 2, rad * 2);
			if (ptB.X > ptA.X)
			{
				rc.X -= rad * mul;
				rc.Width = rc.Width * mul;
			}
			else if (ptB.X < ptA.X)
			{
				rc.X += rad * mul;
				rc.Width = rc.Width * mul;
			}
			else if (ptB.Y > ptA.Y)
			{
				rc.Y -= rad * mul;
				rc.Height = rc.Height * mul;
			}
			else if (ptB.Y < ptA.Y)
			{
				rc.Y += rad * mul;
				rc.Height = rc.Height * mul;
			}

			if (format == DiagramFormat.ExpressG)
			{
				// circle
				g.FillEllipse(Brushes.White, rc);
				g.DrawEllipse(Pens.Black, rc);
			}
			else if (format == DiagramFormat.UML)
			{
				// diamond
				g.DrawPolygon(Pens.Black, new Point[]
					{
						new Point(rc.Left, rc.Top + rc.Height/2),
						new Point(rc.Left + rc.Width/2, rc.Bottom),
						new Point(rc.Right, rc.Top + rc.Height / 2),
						new Point(rc.Left + rc.Width / 2, rc.Top),
						new Point(rc.Left, rc.Top + rc.Height/2)
					});

				// filled if composition...
			}

			return rad * 2;
		}

		private static void DrawTree(Graphics g, DocLine docSub, double factor, Point ptLast, DiagramFormat format)
		{
			Point ptNext = Point.Empty;
			if (docSub.DiagramLine != null)
			{
				float penwidth = 0.0f;
				if (format == DiagramFormat.ExpressG)
				{
					// draw circle at subtype

					penwidth = 3.0f;
				}
				else if (format == DiagramFormat.UML)
				{
					// draw arrow at supertype
					if (ptLast == Point.Empty && docSub.DiagramLine.Count >= 2)
					{
						using (Pen penAttr = new Pen(Color.Black))
						{
							//AdjustableArrowCap cap = new AdjustableArrowCap(6.0f, 6.0f, false);
							//cap.BaseCap = LineCap.ArrowAnchor;
							//penAttr.StartCap = LineCap.Custom;
							//penAttr.CustomStartCap = cap;
							//penAttr.StartCap = LineCap.Custom;

							// arrow side left
							g.DrawLine(penAttr,
								(float)(docSub.DiagramLine[0].X * factor),
								(float)(docSub.DiagramLine[0].Y * factor),
								(float)(docSub.DiagramLine[0].X * factor - 3.0f),
								(float)(docSub.DiagramLine[0].Y * factor + 6.0f));

							// arrow side right
							g.DrawLine(penAttr,
								(float)(docSub.DiagramLine[0].X * factor),
								(float)(docSub.DiagramLine[0].Y * factor),
								(float)(docSub.DiagramLine[0].X * factor + 3.0f),
								(float)(docSub.DiagramLine[0].Y * factor + 6.0f));

							// arrow base
							g.DrawLine(penAttr,
								(float)(docSub.DiagramLine[0].X * factor - 3.0f),
								(float)(docSub.DiagramLine[0].Y * factor + 6.0f),
								(float)(docSub.DiagramLine[0].X * factor + 3.0f),
								(float)(docSub.DiagramLine[0].Y * factor + 6.0f));


							// line
							g.DrawLine(penAttr,
								(float)(docSub.DiagramLine[0].X * factor),
								(float)(docSub.DiagramLine[0].Y * factor + 6.0f),
								(float)(docSub.DiagramLine[1].X * factor),
								(float)(docSub.DiagramLine[1].Y * factor));


						}
					}
				}

				using (Pen pen = new Pen(Color.Black, penwidth))
				{
					for (int i = 0; i < docSub.DiagramLine.Count - 1; i++)
					{
						Point ptA = new Point((int)(docSub.DiagramLine[i].X * factor), (int)(docSub.DiagramLine[i].Y * factor));
						Point ptB = new Point((int)(docSub.DiagramLine[i].X * factor), (int)(docSub.DiagramLine[i + 1].Y * factor));
						Point ptC = new Point((int)(docSub.DiagramLine[i + 1].X * factor), (int)(docSub.DiagramLine[i + 1].Y * factor));

						if (i == 0 && ptLast != Point.Empty)
						{
							g.DrawLine(pen, ptLast, ptA);
						}
						else if (i == 0 && format == DiagramFormat.UML)
						{
							// offset from arrow 
							ptA.Y += 6;
						}
						ptNext = ptC;

						g.DrawLine(pen, ptA, ptB);
						g.DrawLine(pen, ptB, ptC);

						if (docSub.Tree.Count == 0 && i == docSub.DiagramLine.Count - 2 && format == DiagramFormat.ExpressG)
						{
							DrawEndCap(g, ptB, ptC, format);
						}
					}
				}
			}

			foreach (DocLine docInner in docSub.Tree)
			{
				DrawTree(g, docInner, factor, ptNext, format);
			}
		}

		/// <summary>
		/// Creates an inheritance diagram filtered according to model views in scope.
		/// </summary>
		/// <param name="docProject">The project.</param>
		/// <param name="included">Map of included entities according to filtered model view(s).</param>
		/// <param name="docRoot">Root of hierarchy to draw.</param>
		/// <param name="docEntity">Target entity to highlight, if any.</param>
		/// <param name="font"></param>
		/// <param name="map"></param>
		/// <returns></returns>
		public static Image CreateInheritanceDiagram(DocProject docProject, Dictionary<DocObject, bool> included, DocEntity docRoot, DocEntity docEntity, Font font, Dictionary<Rectangle, DocEntity> map)
		{
			Rectangle rc = DrawHierarchy(null, new Point(DX, DX), docRoot, docEntity, docProject, included, font, null);
			if (rc.IsEmpty)
				return null;

			Image image = new Bitmap(rc.Width + CY, rc.Height + CY);
			using (Graphics g = Graphics.FromImage(image))
			{
				DrawHierarchy(g, new Point(CY / 2, CY / 2), docRoot, docEntity, docProject, included, font, map);
			}

			return image;
		}

		/// <summary>
		/// Create an inheritance diagram for a particular entity, its entire hierarchy of supertypes, and one level of subtypes, within scope.
		/// </summary>
		/// <param name="docEntity"></param>
		/// <param name="font"></param>
		/// <param name="map"></param>
		/// <returns></returns>
		public static Image CreateInheritanceDiagramForEntity(DocProject docProject, Dictionary<DocObject, bool> included, DocEntity docEntity, Font font, Dictionary<Rectangle, DocEntity> map)
		{
			// determine items within scope
			Dictionary<DocObject, bool> hierarchy = new Dictionary<DocObject, bool>();
			DocEntity docBase = docEntity;
			DocEntity docRoot = docBase;
			while (docBase != null)
			{
				docRoot = docBase;
				if (included == null || included.ContainsKey(docBase))
				{
					hierarchy.Add(docBase, true);
				}
				docBase = docProject.GetDefinition(docBase.BaseDefinition) as DocEntity;
			}

			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEnt in docSchema.Entities)
					{
						if (docEnt.BaseDefinition == docEntity.Name)
						{
							if (included == null || included.ContainsKey(docEnt))
							{
								hierarchy.Add(docEnt, true);
							}
						}
					}
				}
			}

			return CreateInheritanceDiagram(docProject, hierarchy, docRoot, docEntity, font, map);
		}

		/// <summary>
		/// Draws entity and subtypes recursively to image
		/// </summary>
		/// <param name="g">Graphics where to draw; if null, then just return rectangle</param>
		/// <param name="pt">Point where to draw</param>
		/// <param name="docEntity">Entity to draw</param>
		/// <returns>Bounding rectangle of what was drawn.</returns>
		private static Rectangle DrawHierarchy(Graphics g, Point pt, DocEntity docEntity, DocEntity docTarget, DocProject docProject, Dictionary<DocObject, bool> included, Font font, Dictionary<Rectangle, DocEntity> map)
		{
			if (docEntity == null)
				return Rectangle.Empty;

			Rectangle rc = new Rectangle(pt.X, pt.Y, CX, CY);

			Point ptSub = new Point(pt.X + CY, pt.Y + CY + CY);

			SortedList<string, DocEntity> subtypes = new SortedList<string, DocEntity>(); // sort to match Visual Express
			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEnt in docSchema.Entities)
					{
						if (included == null || included.ContainsKey(docEnt))
						{
							if (docEnt.BaseDefinition != null && docEnt.BaseDefinition.Equals(docEntity.Name))
							{
								if (subtypes.ContainsKey(docEnt.Name))
									System.Diagnostics.Debug.WriteLine("Duplicate subType Key " + docEnt.Name);
								else
									subtypes.Add(docEnt.Name, docEnt);
							}
						}
					}
				}
			}

			foreach (string s in subtypes.Keys)
			{
				DocEntity docEnt = subtypes[s];//docProject.GetDefinition(docSub.DefinedType) as DocEntity;
				if (docEnt != null)
				{
					bool vert = (docEnt.Status != "H");//hack

					Rectangle rcSub = DrawHierarchy(g, ptSub, docEnt, docTarget, docProject, included, font, map);

					if (g != null)
					{
						g.DrawLine(Pens.Black, rcSub.X - CY, pt.Y + CY, rcSub.X - CY, rcSub.Y + CY / 2);
						g.DrawLine(Pens.Black, rcSub.X - CY, rcSub.Y + CY / 2, rcSub.X, rcSub.Y + CY / 2);
					}

					if (vert)
					{
						ptSub.Y += rcSub.Height + CY;
					}
					else
					{
						ptSub.Y = pt.Y + CY + CY;
						ptSub.X += rcSub.Width + CY + CY + CY + CY + CY;
					}

					if (rc.Height < (rcSub.Y + rcSub.Height) - rc.Y)
					{
						rc.Height = (rcSub.Y + rcSub.Height) - rc.Y;
					}

					if (rc.Width < (rcSub.X + rcSub.Width) - rc.X + CY)
					{
						rc.Width = (rcSub.X + rcSub.Width) - rc.X + CY;
					}



				}
			}

			if (g != null)
			{
				Brush brush = Brushes.Black;

				if (docEntity.IsAbstract)
				{
					brush = Brushes.Gray;
				}

				if (docEntity == docTarget)
				{
					brush = Brushes.Blue;
				}

				if (docEntity.IsDeprecated())
				{
					brush = Brushes.DarkRed;
				}

				g.FillRectangle(brush, pt.X, pt.Y, rc.Width - CY, CY);
				g.DrawString(docEntity.Name, font, Brushes.White, pt);
				g.DrawRectangle(Pens.Black, pt.X, pt.Y, rc.Width - CY, CY);
			}

			if (map != null && docEntity != docTarget)
			{
				Rectangle rcKey = new Rectangle(pt.X, pt.Y, rc.Width - CY, CY);
				if (!map.ContainsKey(rcKey))
				{
					map.Add(rcKey, docEntity);
				}
			}


			return rc;
		}
	}


}
