// Name:        CtlConcept.cs
// Description: Concept diagram editor
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using IfcDoc.Schema;
using IfcDoc.Format.PNG;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class CtlConcept : ScrollableControl
	{
		Image m_image;
		DocProject m_project;
		DocTemplateDefinition m_template;
		DocConceptRoot m_conceptroot;
		DocAttribute m_attribute;
		object m_selection;
		object m_highlight;
		int m_iSelection; // index of attribute within selection, or -1 if entity
		int m_iHighlight;
		Rectangle m_rcSelection;
		Rectangle m_rcHighlight;
		Dictionary<Rectangle, SEntity> m_hitmap;
		object m_instance; // optional instance to highlight

		public event EventHandler SelectionChanged;

		public CtlConcept()
		{
			InitializeComponent();

			this.DoubleBuffered = true;
			this.ResizeRedraw = true;
			this.AutoScroll = true;
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
				this.m_rcSelection = Rectangle.Empty;
				this.m_rcHighlight = Rectangle.Empty;
			}
		}

		public DocTemplateDefinition Template
		{
			get
			{
				return this.m_template;
			}
			set
			{
				if (this.m_template == value)
					return;

				this.m_template = value;
				this.m_hitmap = new Dictionary<Rectangle, SEntity>();
				Redraw();
			}
		}

		public DocConceptRoot ConceptRoot
		{
			get
			{
				return this.m_conceptroot;
			}
			set
			{
				this.m_conceptroot = value;
				this.m_hitmap = new Dictionary<Rectangle, SEntity>();
				this.Redraw();
			}
		}

		public object Selection
		{
			get
			{
				return this.m_selection;
			}
			set
			{
				if (this.m_selection == value)
					return;

				this.m_selection = value;
				this.m_attribute = null;

				// determine rectangle
				foreach (Rectangle rc in this.m_hitmap.Keys)
				{
					SEntity ent = this.m_hitmap[rc];
					DocModelRule mr = ent as DocModelRule;
					if (ent == value)
					{
						this.m_rcSelection = rc;
						this.m_iSelection = -1;
						break;
					}
					else if (value is DocModelRule && mr is DocModelRuleEntity && mr.Rules != null && mr.Rules.Contains((DocModelRule)value))
					{
						this.m_rcSelection = rc;
						this.m_iSelection = -1;

						DocEntity docEntity = this.m_project.GetDefinition(mr.Name) as DocEntity;
						if (docEntity != null)
						{
							List<DocAttribute> listAttr = new List<DocAttribute>();
							FormatPNG.BuildAttributeList(docEntity, listAttr, this.m_project);

							for (int i = 0; i < listAttr.Count; i++)
							{
								DocAttribute docAttr = listAttr[i];
								if (docAttr.Name.Equals(((DocModelRule)value).Name))
								{
									this.m_attribute = docAttr;
									this.m_iSelection = i;
									break;
								}
							}
						}
						break;
					}
					else if (mr == null && this.m_template != null && value is DocModelRule && this.m_template.Rules.Contains((DocModelRule)value))
					{
						this.m_rcSelection = rc;
						this.m_iSelection = -1;

						DocEntity docEntity = this.m_project.GetDefinition(this.m_template.Type) as DocEntity;
						List<DocAttribute> listAttr = new List<DocAttribute>();
						FormatPNG.BuildAttributeList(docEntity, listAttr, this.m_project);

						for (int i = 0; i < listAttr.Count; i++)
						{
							DocAttribute docAttr = listAttr[i];
							if (docAttr.Name.Equals(((DocModelRule)value).Name))
							{
								this.m_attribute = docAttr;
								this.m_iSelection = i;
								break;
							}
						}
					}
				}

				this.Invalidate();

				if (this.SelectionChanged != null)
				{
					this.SelectionChanged(this, EventArgs.Empty);
				}
			}
		}

		public DocAttribute CurrentAttribute
		{
			get
			{
				return this.m_attribute;
			}
		}

		public object CurrentInstance
		{
			get
			{
				return this.m_instance;
			}
			set
			{
				this.m_instance = value;
				this.Redraw();
			}
		}

		public void Redraw()
		{
			if (this.m_image != null)
			{
				this.m_image.Dispose();
				this.m_image = null;
			}

			this.m_rcSelection = Rectangle.Empty;
			this.m_rcHighlight = Rectangle.Empty;
			this.m_hitmap.Clear();
			if (this.m_template != null && this.m_project != null)
			{
				this.m_image = FormatPNG.CreateTemplateDiagram(this.m_template, this.m_hitmap, this.m_project, this.m_instance);
				if (this.m_image != null)
				{
					this.AutoScrollMinSize = new Size(this.m_image.Width, this.m_image.Height);
				}
			}
			else if (this.m_conceptroot != null && this.m_project != null && this.m_conceptroot.ApplicableEntity != null)
			{
				DocModelView docView = null;
				foreach (DocModelView eachView in this.m_project.ModelViews)
				{
					if (eachView.ConceptRoots.Contains(this.m_conceptroot))
					{
						docView = eachView;
						break;
					}
				}

				this.m_image = FormatPNG.CreateConceptDiagram(this.m_conceptroot.ApplicableEntity, docView, this.m_hitmap, this.m_project, this.m_instance);
				if (this.m_image != null)
				{
					this.AutoScrollMinSize = new Size(this.m_image.Width, this.m_image.Height);
				}
			}

			this.Invalidate();
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			Graphics g = pe.Graphics;

			if (this.m_image != null)
			{
				g.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);
				g.DrawImage(this.m_image, Point.Empty);

				if (this.m_rcSelection != Rectangle.Empty)
				{
					Rectangle rc = this.m_rcSelection;
					rc.Inflate(3, 3);
					rc.Width++;
					rc.Height++;
					using (Pen pen = new Pen(Color.Red, 6.0f))
					{
						g.DrawRectangle(pen, rc);
					}

					if (this.m_iSelection >= 0 && this.m_attribute != null)
					{
						Rectangle rcAttr = new Rectangle(m_rcSelection.Left, m_rcSelection.Top + (this.m_iSelection + 1) * FormatPNG.CY, FormatPNG.CX - FormatPNG.DX, FormatPNG.CY);
						g.FillRectangle(Brushes.Red, rcAttr);
						g.DrawString(this.m_attribute.Name, this.Font, Brushes.White, rcAttr);
					}
				}

				if (this.m_rcHighlight != Rectangle.Empty)
				{
					Rectangle rc = this.m_rcHighlight;
					rc.Inflate(2, 2);
					rc.Width++;
					rc.Height++;
					using (Pen pen = new Pen(Color.Orange, 4.0f))
					{
						g.DrawRectangle(pen, rc);
					}

					if (this.m_iHighlight >= 0)
					{
						Rectangle rcAttr = new Rectangle(m_rcHighlight.Left, m_rcHighlight.Top + (this.m_iHighlight + 1) * FormatPNG.CY, FormatPNG.CX - FormatPNG.DX, FormatPNG.CY);
						g.DrawRectangle(Pens.Orange, rcAttr);
					}
				}
			}

			if (this.m_template == null || String.IsNullOrEmpty(this.m_template.Type))
			{
				g.DrawString("Double-click to create mapping.", this.Font, Brushes.Black, 0.0f, 0.0f, StringFormat.GenericDefault);
			}

		}

		private SEntity Pick(Point pt, out int iAttr, out DocAttribute docAttribute, out Rectangle rc)
		{
			docAttribute = null;
			iAttr = -1;
			rc = new Rectangle();

			foreach (Rectangle rect in this.m_hitmap.Keys)
			{
				if (rect.Contains(pt))
				{
					rc = rect;
					iAttr = (pt.Y - rc.Top) / FormatPNG.CY - 1;

					SEntity sel = this.m_hitmap[rc];

					if (sel is DocTemplateDefinition)
					{
						return sel;
					}
					else
					{
						DocModelRuleEntity ruleEntity = sel as DocModelRuleEntity;

						DocEntity docEntity = null;
						if (ruleEntity != null)
						{
							DocObject docObjRef = null;

							if (this.m_template != null && !String.IsNullOrEmpty(this.m_template.Code))
							{
								foreach (DocSection docSection in this.m_project.Sections)
								{
									foreach (DocSchema docSchema in docSection.Schemas)
									{
										if (docSchema.Name.Equals(this.m_template.Code, StringComparison.OrdinalIgnoreCase))
										{
											docObjRef = docSchema.GetDefinition(ruleEntity.Name);
											break;
										}
									}
								}
							}

							if (docObjRef == null)
							{
								docObjRef = this.m_project.GetDefinition(ruleEntity.Name);
							}

							if (docObjRef is DocEntity)
							{
								docEntity = (DocEntity)docObjRef;
								List<DocAttribute> listAttr = new List<DocAttribute>();
								FormatPNG.BuildAttributeList(docEntity, listAttr, this.m_project);

								if (iAttr >= 0 && iAttr < listAttr.Count)
								{
									docAttribute = listAttr[iAttr];
									foreach (DocModelRule ruleAttr in ruleEntity.Rules)
									{
										if (ruleAttr is DocModelRuleAttribute && ruleAttr.Name.Equals(docAttribute.Name))
										{
											return ruleAttr;
										}
									}
								}
							}
						}
						else if (this.m_template != null)
						{
							docEntity = this.m_project.GetDefinition(this.m_template.Type) as DocEntity;
							List<DocAttribute> listAttr = new List<DocAttribute>();
							FormatPNG.BuildAttributeList(docEntity, listAttr, this.m_project);

							if (iAttr >= 0 && iAttr < listAttr.Count)
							{
								docAttribute = listAttr[iAttr];
								if (this.m_template.Rules != null)
								{
									foreach (DocModelRule ruleAttr in this.m_template.Rules)
									{
										if (ruleAttr is DocModelRuleAttribute && ruleAttr.Name.Equals(docAttribute.Name))
										{
											return ruleAttr;
										}
									}
								}
							}
						}
					}

					return sel;
				}
			}

			return null;
		}

		private void CtlConcept_MouseDown(object sender, MouseEventArgs e)
		{
			Point pt = e.Location;
			pt.X -= this.AutoScrollPosition.X;
			pt.Y -= this.AutoScrollPosition.Y;

			this.m_selection = Pick(pt, out this.m_iSelection, out this.m_attribute, out this.m_rcSelection);
			this.Invalidate();

			if (this.SelectionChanged != null)
			{
				this.SelectionChanged(this, EventArgs.Empty);
			}
		}

		private void CtlConcept_MouseMove(object sender, MouseEventArgs e)
		{
			Point pt = e.Location;
			pt.X -= this.AutoScrollPosition.X;
			pt.Y -= this.AutoScrollPosition.Y;

			DocAttribute attr;
			this.m_highlight = Pick(pt, out this.m_iHighlight, out attr, out this.m_rcHighlight);
			this.Invalidate();
		}

		private void CtlConcept_MouseUp(object sender, MouseEventArgs e)
		{

		}
	}
}
