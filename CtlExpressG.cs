// Name:        CtlExpressG.cs
// Description: EXPRESS-G diagram editor
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2014 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using IfcDoc.Format.PNG;
using IfcDoc.Schema.DOC;
using IfcDoc.Schema;

namespace IfcDoc
{
	public partial class CtlExpressG : ScrollableControl
	{
		Image m_image;
		DocSchema m_schema;
		DocObject m_selection;
		DocObject m_highlight;
		DocLine m_lineselection;
		DocLine m_linehighlight;
		DocProject m_project;
		bool m_scrollselection;
		bool m_mousedown;
		ResizeHandle m_handle;
		Point m_ptDown;
		Point m_ptMove;
		Dictionary<DocObject, PointF> m_pointmap; // store previous coordinates of objects being moved to avoid round-off issues
		SizeF m_selectionsize; // size of selection when mouse down
		ToolMode m_toolmode;
		DiagramFormat m_diagramformat;
		List<DocDefinition> m_multiselect;

		public const int PageX = 600;
		public const int PageY = 888;
		public const float Factor = 0.375f;

		public event EventHandler SelectionChanged;
		public event EventHandler LinkOperation;
		public event EventHandler ExpandOperation; // user-double-clicks on selection to auto-expand attributes and subtypes

		public CtlExpressG()
		{
			InitializeComponent();

			this.DoubleBuffered = true;
			this.ResizeRedraw = true;
			this.AutoScrollMinSize = new Size(PageX, PageY);
			this.AutoScroll = true;
			this.HorizontalScroll.LargeChange = PageX;
			this.VerticalScroll.LargeChange = PageY;

			this.m_scrollselection = true;
			this.m_pointmap = new Dictionary<DocObject, PointF>();
			this.m_multiselect = new List<DocDefinition>();
		}

		public void Redraw()
		{
			if (this.m_image != null)
			{
				this.m_image.Dispose();
				this.m_image = null;
			}

			this.Invalidate();
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

		public DocSchema Schema
		{
			get
			{
				return this.m_schema;
			}
			set
			{
				this.m_schema = value;
				this.m_selection = null;
				this.m_highlight = null;

				this.Redraw();
			}
		}

		public DocObject Highlight
		{
			get
			{
				return this.m_highlight;
			}
		}

		public DocObject Selection
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

				// auto-scroll to page
				if (this.m_scrollselection && !this.m_mousedown)
				{
					DocDefinition docDef = null;
					if (this.m_selection is DocDefinition)
					{
						docDef = (DocDefinition)this.m_selection;
					}
					else if (this.m_selection is DocAttribute)
					{
						// find the enclosing definition
						DocAttribute attr = (DocAttribute)this.m_selection;
						foreach (DocEntity docEnt in this.m_schema.Entities)
						{
							if (docEnt.Attributes.Contains(attr))
							{
								docDef = docEnt;
								break;
							}
						}
					}

					if (docDef != null)
					{
						if (docDef.DiagramNumber != 0 && this.m_schema.DiagramPagesHorz != 0)
						{
							int pageY = (docDef.DiagramNumber - 1) / this.m_schema.DiagramPagesHorz;
							int pageX = (docDef.DiagramNumber - 1) % this.m_schema.DiagramPagesHorz;

							// also scroll vertically if not visible
							int incY = 0;
							if (docDef.DiagramRectangle.Y > this.ClientSize.Height)
							{
								incY = (int)(docDef.DiagramRectangle.Y * CtlExpressG.Factor) - (pageY * PageY);
							}

							this.AutoScrollPosition = new Point(pageX * PageX, pageY * PageY + incY);
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

		public List<DocDefinition> Multiselection
		{
			get
			{
				return this.m_multiselect;
			}
		}

		public Point Marker
		{
			get
			{
				return this.m_ptDown;
			}
			set
			{
				this.m_ptDown = value;
			}
		}

		/// <summary>
		/// Indicates whether view will be automatically scrolled when the selection changes.
		/// </summary>
		public bool ScrollToSelection
		{
			get
			{
				return this.m_scrollselection;
			}
			set
			{
				this.m_scrollselection = value;
			}
		}

		public ToolMode Mode
		{
			get
			{
				return this.m_toolmode;
			}
			set
			{
				this.m_toolmode = value;
				this.Invalidate();
			}
		}

		public DiagramFormat Format
		{
			get
			{
				return this.m_diagramformat;
			}
			set
			{
				this.m_diagramformat = value;
				this.Redraw();
			}
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			if (this.m_image == null && this.m_schema != null && this.m_project != null)
			{
				this.m_image = FormatPNG.CreateSchemaDiagram(this.m_schema, this.m_project, this.m_diagramformat);
				this.AutoScrollMinSize = new Size(this.m_image.Width, this.m_image.Height);
			}

			if (this.m_image != null)
			{
				Graphics g = pe.Graphics;
				g.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);
				g.DrawImage(this.m_image, Point.Empty);

				using (Pen pen = new Pen(Color.Maroon, 5.0f))
				{
					foreach (DocDefinition docDef in this.m_multiselect)
					{
						DrawObjectBorder(g, docDef, pen);
					}
				}

				if (this.m_selection != null)
				{
					using (Pen pen = new Pen(Color.Red, 5.0f))
					{
						if (this.m_lineselection != null)
						{
							DrawObjectBorder(g, this.m_lineselection, pen);
						}
						else
						{
							DrawObjectBorder(g, this.m_selection, pen);
						}
					}
				}

				if (this.m_highlight != null)
				{
					using (Pen pen = new Pen(Color.Orange, 3.0f))
					{
						if (this.m_linehighlight != null)
						{
							DrawObjectBorder(g, this.m_linehighlight, pen);
						}
						else
						{
							DrawObjectBorder(g, this.m_highlight, pen);
						}
					}
				}

				g.TranslateTransform(-this.AutoScrollPosition.X, -this.AutoScrollPosition.Y);

				// draw link line
				if (this.m_mousedown && this.m_toolmode == ToolMode.Link)
				{
					g.DrawLine(Pens.Red, this.m_ptDown, this.m_ptMove);
				}

				if (this.m_mousedown && this.m_toolmode == ToolMode.Move && this.m_selection == null)
				{
					// draw selection box
					Rectangle rc = CreateNormalizedRectangle(this.m_ptDown, this.m_ptMove);
					g.DrawRectangle(Pens.Red, rc);
				}
				else if (this.m_selection == null && this.m_toolmode == ToolMode.Move)
				{
					// draw insertion point
					int rad = 16;
					g.DrawLine(Pens.Red, this.m_ptDown.X - rad, this.m_ptDown.Y, this.m_ptDown.X + rad, this.m_ptDown.Y);
					g.DrawLine(Pens.Red, this.m_ptDown.X, this.m_ptDown.Y - rad, this.m_ptDown.X, this.m_ptDown.Y + rad);
				}
			}
		}

		private static Rectangle CreateNormalizedRectangle(Point p1, Point p2)
		{
			Rectangle rc = new Rectangle();
			if (p1.X < p2.X)
			{
				rc.X = p1.X;
				rc.Width = p2.X - p1.X;
			}
			else
			{
				rc.X = p2.X;
				rc.Width = p1.X - p2.X;
			}
			if (p1.Y < p2.Y)
			{
				rc.Y = p1.Y;
				rc.Height = p2.Y - p1.Y;
			}
			else
			{
				rc.Y = p2.Y;
				rc.Height = p1.Y - p2.Y;
			}
			return rc;
		}

		private void DrawObjectBorder(Graphics g, SEntity obj, Pen pen)
		{
			if (obj is DocDefinition && ((DocDefinition)obj).DiagramRectangle != null)
			{
				DocRectangle docRect = ((DocDefinition)obj).DiagramRectangle;

				Rectangle rc = new Rectangle(
					(int)(docRect.X * Factor),
					(int)(docRect.Y * Factor),
					(int)(docRect.Width * Factor),
					(int)(docRect.Height * Factor));
				rc.Inflate(-2, -2);

				g.DrawRectangle(pen, rc);
			}
			else if (obj is DocAttribute)
			{
				DocAttribute docAttr = (DocAttribute)obj;
				if (docAttr.DiagramLine != null)
				{
					for (int i = 0; i < docAttr.DiagramLine.Count - 1; i++)
					{
						g.DrawLine(pen,
							new Point((int)(docAttr.DiagramLine[i].X * Factor), (int)(docAttr.DiagramLine[i].Y * Factor)),
							new Point((int)(docAttr.DiagramLine[i + 1].X * Factor), (int)(docAttr.DiagramLine[i + 1].Y * Factor)));
					}
				}
			}
			else if (obj is DocLine)
			{
				// tree point
				DocLine docLine = (DocLine)obj;
				DocPoint docPoint = docLine.DiagramLine[docLine.DiagramLine.Count - 1];
				Rectangle rc = new Rectangle((int)(docPoint.X * Factor), (int)(docPoint.Y * Factor), 0, 0);
				rc.Inflate(6, 6);
				g.DrawEllipse(pen, rc);
			}
		}

		private void CtlExpressG_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != System.Windows.Forms.MouseButtons.Left)
				return;

			bool multi = ((System.Windows.Forms.Control.ModifierKeys & Keys.Control) == Keys.Control);

			this.m_mousedown = true;
			this.m_ptDown = e.Location;

			this.Selection = this.Pick(e.Location, out this.m_lineselection, out this.m_handle);
			if (this.Selection == null && !multi)
			{
				this.m_multiselect.Clear();
				this.m_pointmap.Clear();
			}

			UpdateCursor(this.m_handle);

			if (this.m_selection is DocDefinition)
			{
				DocDefinition docDef = (DocDefinition)this.Selection;
				if (!m_pointmap.ContainsKey(docDef))
				{
					m_pointmap.Add(docDef, new PointF((float)docDef.DiagramRectangle.X, (float)docDef.DiagramRectangle.Y));
				}
				m_selectionsize = new SizeF((float)docDef.DiagramRectangle.Width, (float)docDef.DiagramRectangle.Height);
			}

			if (multi && this.Selection is DocDefinition)
			{
				DocDefinition docDef = (DocDefinition)this.Selection;
				if (!this.m_multiselect.Contains(docDef))
				{
					this.m_multiselect.Add(docDef);
				}
			}

			this.Invalidate();
		}

		/// <summary>
		/// Updates any connected geometry to match definition
		/// </summary>
		/// <param name="docDef"></param>
		public void LayoutDefinition(DocDefinition selection)
		{
			// then traverse all dependencies on selection
			if (selection is DocEntity)
			{
				DocEntity docEntity = (DocEntity)selection;
				foreach (DocAttribute docAttr in docEntity.Attributes)
				{
					LayoutLine(docEntity, docAttr.Definition, docAttr.DiagramLine);
					if (docAttr.DiagramLabel != null)
					{
						if (docAttr.DiagramLine[2].Y > docAttr.DiagramLine[0].Y)
						{
							docAttr.DiagramLabel.X = docAttr.DiagramLine[0].X;
							docAttr.DiagramLabel.Y = docAttr.DiagramLine[0].Y + 20.0;
						}
						else
						{
							docAttr.DiagramLabel.X = docAttr.DiagramLine[0].X;
							docAttr.DiagramLabel.Y = docAttr.DiagramLine[0].Y - 20.0;
						}
					}
				}
				foreach (DocLine docLine in docEntity.Tree)
				{
					LayoutTree(docEntity, docLine);
				}
			}
			else if (selection is DocDefinitionRef)
			{
				DocDefinitionRef docRef = (DocDefinitionRef)selection;

				foreach (DocAttributeRef docAttr in docRef.AttributeRefs)
				{
					LayoutLine(docRef, docAttr.DefinitionRef, docAttr.DiagramLine);

#if fals3
                    if (docAttr.DiagramLabel != null)
                    {
                        if (docAttr.DiagramLine[2].Y > docAttr.DiagramLine[0].Y)
                        {
                            docAttr.DiagramLabel.X = docAttr.DiagramLine[0].X;
                            docAttr.DiagramLabel.Y = docAttr.DiagramLine[0].Y + 20.0;
                        }
                        else
                        {
                            docAttr.DiagramLabel.X = docAttr.DiagramLine[0].X;
                            docAttr.DiagramLabel.Y = docAttr.DiagramLine[0].Y - 20.0;
                        }
                    }
#endif
				}
				if (docRef.Tree != null)
				{
					foreach (DocLine docLine in docRef.Tree)
					{
						LayoutTree(docRef, docLine);
					}
				}
			}
			else if (selection is DocDefined)
			{
				DocDefined docDef = (DocDefined)selection;
				LayoutLine(docDef, docDef.Definition, docDef.DiagramLine);
			}
			else if (selection is DocSelect)
			{
				DocSelect docSelect = (DocSelect)selection;
				foreach (DocLine docLine in docSelect.Tree)
				{
					LayoutTree(docSelect, docLine);
				}
			}
			else if (selection is DocPageTarget)
			{
				DocPageTarget docTarget = (DocPageTarget)selection;
				if (docTarget.Definition != null)
				{
					LayoutLine(docTarget.Definition, docTarget, docTarget.DiagramLine);
				}
				docTarget.DiagramLine.Reverse();
			}

			foreach (DocPageTarget docPageTarget in this.m_schema.PageTargets)
			{
				if (docPageTarget.Definition == selection)
				{
					LayoutLine(docPageTarget.Definition, docPageTarget, docPageTarget.DiagramLine);
					docPageTarget.DiagramLine.Reverse();
				}
			}

			foreach (DocSchemaRef docSchemaRef in this.m_schema.SchemaRefs)
			{
				foreach (DocDefinitionRef docDefRef in docSchemaRef.Definitions)
				{
					if (docDefRef.Tree != null)
					{
						foreach (DocLine docLine in docDefRef.Tree)
						{
							if (docLine.Definition == selection)
							{
								LayoutLine(docDefRef, docLine.Definition, docLine.DiagramLine);
							}
							else
							{
								foreach (DocLine docSub in docLine.Tree)
								{
									if (docSub.Definition == selection)
									{
										LayoutNode(docLine, docSub);
									}
								}
							}
						}
					}

					foreach (DocAttributeRef docAttRef in docDefRef.AttributeRefs)
					{
						if (docAttRef.DefinitionRef == selection)
						{
							LayoutLine(docDefRef, docAttRef.DefinitionRef, docAttRef.DiagramLine);
						}
					}
				}

			}

			foreach (DocEntity docEntity in this.m_schema.Entities)
			{
				// if there multile attributes refer to the same definition, then space evenly
				int count = 0;
				foreach (DocAttribute docAttr in docEntity.Attributes)
				{
					if (docAttr.Definition == selection)
					{
						count++;
					}
				}

				int each = 0;
				foreach (DocAttribute docAttr in docEntity.Attributes)
				{
					if (docAttr.Definition == selection)
					{
						LayoutLine(docEntity, docAttr.Definition, docAttr.DiagramLine);
						if (count > 1 && docAttr.DiagramLine.Count == 3)
						{
							each++;
							docAttr.DiagramLine[0].Y = selection.DiagramRectangle.Y + (selection.DiagramRectangle.Height * (double)each / (double)(count + 1));
							docAttr.DiagramLine[1].Y = selection.DiagramRectangle.Y + (selection.DiagramRectangle.Height * (double)each / (double)(count + 1));
							docAttr.DiagramLine[2].Y = selection.DiagramRectangle.Y + (selection.DiagramRectangle.Height * (double)each / (double)(count + 1));
						}

						if (docAttr.DiagramLabel == null)
						{
							docAttr.DiagramLabel = new DocRectangle();
						}

						if (docAttr.DiagramLine[2].Y > docAttr.DiagramLine[0].Y)
						{
							docAttr.DiagramLabel.X = docAttr.DiagramLine[0].X;
							docAttr.DiagramLabel.Y = docAttr.DiagramLine[0].Y + 20.0;
						}
						else
						{
							docAttr.DiagramLabel.X = docAttr.DiagramLine[0].X;
							docAttr.DiagramLabel.Y = docAttr.DiagramLine[0].Y - 20.0;
						}
					}
				}

				foreach (DocLine docLine in docEntity.Tree)
				{
					// workaround to fix-up broken data (due to bug in previous import from Visual Express -- no longer occurs with new files)
#if false
                    if (docLine.Definition == null && selection is DocEntity && ((DocEntity)selection).BaseDefinition == docEntity.Name)
                    {
                        docLine.Definition = selection;
                        System.Diagnostics.Debug.WriteLine("CtlExpressG::LayoutDefinition -- fixed up null reference to subtype");
                    }
#endif
					if (docLine.Definition == selection)
					{
						LayoutLine(docEntity, docLine.Definition, docLine.DiagramLine);
					}
					else
					{
						foreach (DocLine docSub in docLine.Tree)
						{
							if (docSub.Definition == selection)
							{
								LayoutNode(docLine, docSub);
							}
						}
					}
				}
			}

			foreach (DocType docType in this.m_schema.Types)
			{
				if (docType is DocDefined)
				{
					DocDefined docDef = (DocDefined)docType;
					if (docDef.Definition == selection)
					{
						LayoutLine(docDef, docDef.Definition, docDef.DiagramLine);
					}
				}
				else if (docType is DocSelect)
				{
					DocSelect docSel = (DocSelect)docType;
					foreach (DocLine docLine in docSel.Tree)
					{
						if (docLine.Definition == selection)
						{
							LayoutLine(docSel, selection, docLine.DiagramLine);
						}

						foreach (DocLine docSub in docLine.Tree)
						{
							if (docSub.Definition == selection)
							{
								LayoutNode(docLine, docSub);
							}
						}
					}
				}
			}

			// recalculate page extents and resize/repaginate if necessary
			double cx = 0.0;
			double cy = 0.0;
			foreach (DocType docDef in this.m_schema.Types)
			{
				ExpandExtents(docDef.DiagramRectangle, ref cx, ref cy);
			}
			foreach (DocEntity docDef in this.m_schema.Entities)
			{
				ExpandExtents(docDef.DiagramRectangle, ref cx, ref cy);
			}
			foreach (DocPrimitive docDef in this.m_schema.Primitives)
			{
				ExpandExtents(docDef.DiagramRectangle, ref cx, ref cy);
			}
			foreach (DocPageTarget docDef in this.m_schema.PageTargets)
			{
				ExpandExtents(docDef.DiagramRectangle, ref cx, ref cy);
				foreach (DocPageSource docSource in docDef.Sources)
				{
					ExpandExtents(docSource.DiagramRectangle, ref cx, ref cy);
				}
			}
			foreach (DocSchemaRef docRef in this.m_schema.SchemaRefs)
			{
				foreach (DocDefinitionRef docDef in docRef.Definitions)
				{
					ExpandExtents(docDef.DiagramRectangle, ref cx, ref cy);
				}
			}
			foreach (DocComment docDef in this.m_schema.Comments)
			{
				ExpandExtents(docDef.DiagramRectangle, ref cx, ref cy);
			}
			this.m_schema.DiagramPagesHorz = 1 + (int)Math.Floor(cx * Factor / PageX);
			this.m_schema.DiagramPagesVert = 1 + (int)Math.Floor(cy * Factor / PageY);

			int px = (int)(selection.DiagramRectangle.X * Factor / CtlExpressG.PageX);
			int py = (int)(selection.DiagramRectangle.Y * Factor / CtlExpressG.PageY);
			int page = 1 + py * this.m_schema.DiagramPagesHorz + px;
			selection.DiagramNumber = page;
		}

		private void MoveObject(DocDefinition docSelection, float dx, float dy)
		{
			if (this.m_pointmap.ContainsKey(docSelection))
			{
				PointF ptSelection = this.m_pointmap[docSelection];

				if ((this.m_handle & ResizeHandle.North) != 0)
				{
					double yTail = docSelection.DiagramRectangle.Y + docSelection.DiagramRectangle.Height;
					docSelection.DiagramRectangle.Y = ptSelection.Y + dy / Factor;
					docSelection.DiagramRectangle.Height = yTail - docSelection.DiagramRectangle.Y;
				}
				else if ((this.m_handle & ResizeHandle.South) != 0)
				{
					docSelection.DiagramRectangle.Height = m_selectionsize.Height + dy / Factor;
				}

				if ((this.m_handle & ResizeHandle.West) != 0)
				{
					double xTail = docSelection.DiagramRectangle.X + docSelection.DiagramRectangle.Width;
					docSelection.DiagramRectangle.X = ptSelection.X + dx / Factor;
					docSelection.DiagramRectangle.Width = xTail - docSelection.DiagramRectangle.X;
				}
				else if ((this.m_handle & ResizeHandle.East) != 0)
				{
					docSelection.DiagramRectangle.Width = m_selectionsize.Width + dx / Factor;
				}

				if (this.m_handle == ResizeHandle.Move)
				{
					docSelection.DiagramRectangle.X = ptSelection.X + dx / Factor;
					docSelection.DiagramRectangle.Y = ptSelection.Y + dy / Factor;

					if (docSelection.DiagramRectangle.X < 0)
					{
						docSelection.DiagramRectangle.X = 0;
					}
					if (docSelection.DiagramRectangle.Y < 0)
					{
						docSelection.DiagramRectangle.Y = 0;
					}
				}
				else
				{
					if (docSelection.DiagramRectangle.Width < 64)
					{
						docSelection.DiagramRectangle.Width = 64;
					}
					if (docSelection.DiagramRectangle.Height < 64)
					{
						docSelection.DiagramRectangle.Height = 64;
					}
				}

				LayoutDefinition(docSelection);
			}
		}

		private void CtlExpressG_MouseMove(object sender, MouseEventArgs e)
		{
			this.m_ptMove = e.Location;

			if (this.m_mousedown && this.m_toolmode == ToolMode.Move)
			{
				UpdateCursor(this.m_handle);

				Point ptLocation = e.Location;
				if (ptLocation.X < 0)
					ptLocation.X = 0;
				if (ptLocation.Y < 0)
					ptLocation.Y = 0;

				if (this.m_lineselection != null && this.m_selection is DocDefinition)
				{
					// moving tree node
					DocPoint docPoint = m_lineselection.DiagramLine[m_lineselection.DiagramLine.Count - 1];
					docPoint.X = (ptLocation.X - this.AutoScrollPosition.X) / Factor;
					docPoint.Y = (ptLocation.Y - this.AutoScrollPosition.Y) / Factor;

					// layout the owning element
					LayoutDefinition((DocDefinition)this.m_selection);

					// layout lines to all subtypes
					foreach (DocLine docSub in this.m_lineselection.Tree)
					{
						LayoutNode(this.m_lineselection, docSub);
					}

					this.Redraw();
				}
				else if (this.m_selection is DocDefinition)
				{
					float dx = (float)(ptLocation.X - this.m_ptDown.X);
					float dy = (float)(ptLocation.Y - this.m_ptDown.Y);


					// move or resize the object...
					DocDefinition docSelection = (DocDefinition)this.m_selection;
					MoveObject(docSelection, dx, dy);
					foreach (DocDefinition docDef in this.m_multiselect)
					{
						if (docDef != docSelection)
						{
							MoveObject(docDef, dx, dy);
						}
					}
					this.Redraw();
				}
				else if (this.m_selection == null)
				{
					// draw box and highlight multiple within region
					this.m_multiselect.Clear();
					Rectangle rc = CreateNormalizedRectangle(this.m_ptDown, this.m_ptMove);
					foreach (DocEntity docEntity in this.m_schema.Entities)
					{
						SelectWithinRectangle(docEntity, rc);
					}
					foreach (DocType docType in this.m_schema.Types)
					{
						SelectWithinRectangle(docType, rc);
					}
					foreach (DocPrimitive docType in this.m_schema.Primitives)
					{
						SelectWithinRectangle(docType, rc);
					}
					foreach (DocPageTarget docTarget in this.m_schema.PageTargets)
					{
						SelectWithinRectangle(docTarget, rc);
						foreach (DocPageSource docSource in docTarget.Sources)
						{
							SelectWithinRectangle(docSource, rc);
						}
					}
					foreach (DocSchemaRef docSchemaRef in this.m_schema.SchemaRefs)
					{
						foreach (DocDefinitionRef docRef in docSchemaRef.Definitions)
						{
							SelectWithinRectangle(docRef, rc);
						}
					}
					// don't select comments

					this.Invalidate();
				}
			}
			else
			{
				ResizeHandle handle = ResizeHandle.None;
				DocObject highlight = this.Pick(e.Location, out this.m_linehighlight, out handle);
				if (this.m_highlight != highlight)
				{
					this.m_highlight = highlight;
					this.Invalidate();
				}

				if (this.m_mousedown && this.m_toolmode == ToolMode.Link)
				{
					this.Invalidate();
				}

				UpdateCursor(handle);
			}
		}

		private void SelectWithinRectangle(DocDefinition def, Rectangle rc)
		{
			if (def.DiagramRectangle == null)
				return;

			Rectangle rcObject = new Rectangle(
				(int)(def.DiagramRectangle.X * Factor) + this.AutoScrollPosition.X,
				(int)(def.DiagramRectangle.Y * Factor) + this.AutoScrollPosition.Y,
				(int)(def.DiagramRectangle.Width * Factor),
				(int)(def.DiagramRectangle.Height * Factor));

			if (rc.IntersectsWith(rcObject))
			{
				this.m_multiselect.Add(def);
				if (!this.m_pointmap.ContainsKey(def))
				{
					m_pointmap.Add(def, new PointF((float)def.DiagramRectangle.X, (float)def.DiagramRectangle.Y));
				}
			}
		}

		private void UpdateCursor(ResizeHandle handle)
		{
			switch (this.m_toolmode)
			{
				case ToolMode.Select:
					this.Cursor = Cursors.Default;
					break;

				case ToolMode.Move:
					switch (handle)
					{
						case ResizeHandle.North | ResizeHandle.East:
						case ResizeHandle.South | ResizeHandle.West:
							this.Cursor = Cursors.SizeNESW;
							break;

						case ResizeHandle.North | ResizeHandle.West:
						case ResizeHandle.South | ResizeHandle.East:
							this.Cursor = Cursors.SizeNWSE;
							break;

						case ResizeHandle.West:
						case ResizeHandle.East:
							this.Cursor = Cursors.SizeWE;
							break;

						case ResizeHandle.North:
						case ResizeHandle.South:
							this.Cursor = Cursors.SizeNS;
							break;

						case ResizeHandle.Move:
							this.Cursor = Cursors.SizeAll;
							break;

						default:
							this.Cursor = Cursors.Default;
							break;
					}
					break;

				case ToolMode.Link:
					switch (handle)
					{
						case ResizeHandle.Move: // body -- attribute
							this.Cursor = Cursors.Cross;
							break;

						case ResizeHandle.None:
							this.Cursor = Cursors.No;
							break;

						default: // along edge -- inheritance
							this.Cursor = Cursors.UpArrow;
							break;
					}
					break;
			}
		}

		private static void ExpandExtents(DocRectangle docRect, ref double cx, ref double cy)
		{
			if (docRect == null)
				return;

			if (docRect.X + docRect.Width > cx)
				cx = docRect.X + docRect.Width;

			if (docRect.Y + docRect.Height > cy)
				cy = docRect.Y + docRect.Height;
		}

		/// <summary>
		/// Updates layout of node end of tree, adjusting tree segment as necessary
		/// </summary>
		/// <param name="docLine"></param>
		/// <param name="docDef"></param>
		public static void LayoutNode(DocLine docTree, DocLine docLine)
		{
			DocDefinition docDef = docLine.Definition;

			if (docLine.DiagramLine.Count >= 2)
			{
				while (docLine.DiagramLine.Count < 3)
				{
					docLine.DiagramLine.Insert(1, new DocPoint());
				}

				// shortcut: make up a rectangle for the line -- don't use constructor as that will allocate as object to be serialized within current project
				DocRectangle fakeRectangle = (DocRectangle)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DocRectangle));
				fakeRectangle.X = docTree.DiagramLine[docTree.DiagramLine.Count - 1].X;
				fakeRectangle.Y = docTree.DiagramLine[docTree.DiagramLine.Count - 1].Y;
				fakeRectangle.Width = 0;
				fakeRectangle.Height = 0;

				LayoutLine(fakeRectangle, docDef.DiagramRectangle, docLine.DiagramLine[0], docLine.DiagramLine[1], docLine.DiagramLine[2]);
			}
		}

		/// <summary>
		/// Updates layout of parent end of tree
		/// </summary>
		/// <param name="defA"></param>
		/// <param name="docLine"></param>
		public static void LayoutTree(DocDefinition defA, DocLine docLine)
		{
			if (docLine.Definition != null)
			{
				LayoutLine(defA, docLine.Definition, docLine.DiagramLine);
			}
			else if (docLine.DiagramLine.Count >= 2)
			{
				while (docLine.DiagramLine.Count < 3)
				{
					docLine.DiagramLine.Insert(1, new DocPoint());
				}

				// shortcut: make up a rectangle for the line -- don't use constructor as that will allocate as object to be serialized within current project
				DocRectangle fakeRectangle = (DocRectangle)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DocRectangle));
				fakeRectangle.X = docLine.DiagramLine[docLine.DiagramLine.Count - 1].X;
				fakeRectangle.Y = docLine.DiagramLine[docLine.DiagramLine.Count - 1].Y;
				fakeRectangle.Width = 0;
				fakeRectangle.Height = 0;

				LayoutLine(defA.DiagramRectangle, fakeRectangle, docLine.DiagramLine[0], docLine.DiagramLine[1], docLine.DiagramLine[2]);
			}
		}

		public static void LayoutLine(DocDefinition defA, DocDefinition defB, List<DocPoint> list)
		{
			if (defA == null)
				return;

			if (list == null)
				return;

			while (list.Count > 3)
			{
				list[list.Count - 1].Delete();
				list.RemoveAt(list.Count - 1);
			}
			while (list.Count < 3)
			{
				list.Add(new DocPoint());
			}
			DocPoint ptA = list[0];
			DocPoint ptM = list[1];
			DocPoint ptB = list[2];

			DocRectangle rB = null;
			if (defB != null)
			{
				rB = defB.DiagramRectangle;
			}

			LayoutLine(defA.DiagramRectangle, rB, ptA, ptM, ptB);
		}

		/// <summary>
		/// Lays out points for connectors using straight segments where possible, falling back on elbows if below
		/// </summary>
		/// <param name="rcA">The source rectangle</param>
		/// <param name="rcB">The target rectangle - optional; if null then only the first point is resized</param>
		/// <param name="ptA">The source point, which is positioned along the perimeter of the source rectangle.</param>
		/// <param name="ptM">The elbow point, which may be the same as the source point (if none), or different if an elbow is needed.</param>
		/// <param name="ptB">The target point, which is positioned at the midpoint of one of the four edges of the target rectangle.</param>
		private static void LayoutLine(DocRectangle rcA, DocRectangle rcB, DocPoint ptA, DocPoint ptM, DocPoint ptB)
		{
			if (rcA == null || rcB == null)
				return;

			if (rcB.X > rcA.X + rcA.Width)
			{
				// to right
				ptB.X = rcB.X;
				ptB.Y = rcB.Y + rcB.Height / 2;
				ptA.X = rcA.X + rcA.Width;
				ptA.Y = ptB.Y;
				ptM.X = ptA.X;
				ptM.Y = ptA.Y;
				if (ptA.Y > rcA.Y + rcA.Height)
				{
					// below
					ptA.Y = rcA.Y + rcA.Height;
				}
				else if (ptA.Y < rcA.Y)
				{
					// above
					ptA.Y = rcA.Y;
				}
			}
			else if (rcB.X + rcB.Width < rcA.X)
			{
				// to left 
				ptB.X = rcB.X + rcB.Width;
				ptB.Y = rcB.Y + rcB.Height / 2;
				ptA.X = rcA.X;
				ptA.Y = ptB.Y;
				ptM.X = ptA.X;
				ptM.Y = ptA.Y;
				if (ptA.Y > rcA.Y + rcA.Height)
				{
					// below
					ptA.Y = rcA.Y + rcA.Height;
				}
				else if (ptA.Y < rcA.Y)
				{
					// above
					ptA.Y = rcA.Y;
				}
			}
			else if (rcB.Y > rcA.Y + rcA.Height)
			{
				// to down 
				ptB.X = rcB.X + rcB.Width / 2;
				ptB.Y = rcB.Y;
				ptA.X = ptB.X;
				ptA.Y = rcA.Y + rcA.Height;
				ptM.X = ptA.X;
				ptM.Y = ptA.Y;
				if (ptA.X > rcA.X + rcA.Width)
				{
					// right
					ptA.X = rcA.X + rcA.Width;
				}
				else if (ptA.X < rcA.X)
				{
					// left
					ptA.X = rcA.X;
				}
			}
			else if (rcB.Y + rcB.Height < rcA.Y)
			{
				// to up
				ptB.X = rcB.X + rcB.Width / 2;
				ptB.Y = rcB.Y + rcB.Height;
				ptA.X = ptB.X;
				ptA.Y = rcA.Y;
				ptM.X = ptA.X;
				ptM.Y = ptA.Y;
				if (ptA.X > rcA.X + rcA.Width)
				{
					// right
					ptA.X = rcA.X + rcA.Width;
				}
				else if (ptA.X < rcA.X)
				{
					// left
					ptA.X = rcA.X;
				}
			}
		}

		private void CtlExpressG_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button != System.Windows.Forms.MouseButtons.Left)
				return;

			this.m_mousedown = false;

			if (this.m_selection == null && this.m_toolmode == ToolMode.Move && this.m_multiselect.Count > 0)
			{
				this.m_selection = this.m_multiselect[0];

				if (this.SelectionChanged != null)
				{
					this.SelectionChanged(this, EventArgs.Empty);
				}
			}

			if (this.m_selection != null && this.m_highlight != null && this.m_toolmode == ToolMode.Link)
			{
				if (this.LinkOperation != null)
				{
					// special case for linking entities for subtypes
					EventArgs args = EventArgs.Empty;
					if (this.Cursor == Cursors.UpArrow && (this.m_selection is DocEntity || this.m_selection is DocDefinitionRef) && this.m_highlight is DocEntity)
					{
						//... also need to determine if referenced type is an entity
						args = null;
					}
					this.LinkOperation(this, args);
				}

				this.Redraw();
			}

			this.m_pointmap.Clear();
			foreach (DocDefinition docDef in this.m_multiselect)
			{
				this.m_pointmap.Add(docDef, new PointF((float)docDef.DiagramRectangle.X, (float)docDef.DiagramRectangle.Y));
			}
			this.Invalidate();
		}

		private static bool HitTest(DocRectangle docRect, Point pt, out ResizeHandle handle)
		{
			handle = ResizeHandle.None;

			if (docRect == null)
				return false;

			Rectangle rc = new Rectangle(
				(int)(docRect.X * Factor),
				(int)(docRect.Y * Factor),
				(int)(docRect.Width * Factor),
				(int)(docRect.Height * Factor));

			bool contains = rc.Contains(pt);
			if (contains)
			{
				int W = pt.X - rc.Left;
				int N = pt.Y - rc.Top;
				int S = rc.Bottom - pt.Y;
				int E = rc.Right - pt.X;

				int rad = 5;
				if (E < rad)
				{
					handle |= ResizeHandle.East;
				}
				else if (W < rad)
				{
					handle |= ResizeHandle.West;
				}
				if (S < rad)
				{
					handle |= ResizeHandle.South;
				}
				else if (N < rad)
				{
					handle |= ResizeHandle.North;
				}

				if (handle == ResizeHandle.None)
					handle = ResizeHandle.Move;
			}
			return contains;
		}

		/// <summary>
		/// Finds object at absolute point (regardless of scroll position or page)
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		private DocObject Pick(Point pt, out DocLine line, out ResizeHandle handle)
		{
			line = null;
			handle = ResizeHandle.None;

			if (this.m_schema == null)
				return null;

			pt.X -= this.AutoScrollPosition.X;
			pt.Y -= this.AutoScrollPosition.Y;

			PointF ptFloat = new PointF(pt.X, pt.Y);

			// pick in reverse order
			for (int iType = this.m_schema.Entities.Count - 1; iType >= 0; iType--)
			{
				DocEntity docType = this.m_schema.Entities[iType];

				if (HitTest(docType.DiagramRectangle, pt, out handle))
					return docType;

				foreach (DocAttribute docAttr in docType.Attributes)
				{
					if (docAttr.DiagramLine != null)
					{
						for (int i = 0; i < docAttr.DiagramLine.Count - 1; i++)
						{
							PointF ptA = new PointF((float)(docAttr.DiagramLine[i].X * Factor), (float)docAttr.DiagramLine[i].Y * Factor);
							PointF ptB = new PointF((float)(docAttr.DiagramLine[i + 1].X * Factor), (float)docAttr.DiagramLine[i + 1].Y * Factor);

							PointF ptClosest = new PointF();
							double distance = FindDistanceToSegment(ptFloat, ptA, ptB, out ptClosest);
							if (distance < 3.0)
							{
								return docAttr;
							}
						}
					}
				}

				if (docType.Tree != null)
				{
					foreach (DocLine docLine in docType.Tree)
					{
						if (docLine.DiagramLine.Count > 0)
						{
							DocPoint docPoint = docLine.DiagramLine[docLine.DiagramLine.Count - 1];
							PointF ptA = new PointF((float)(docPoint.X * Factor), (float)docPoint.Y * Factor);
							if (Math.Abs(ptFloat.X - ptA.X) < 4 && Math.Abs(ptFloat.Y - ptA.Y) <= 5)
							{
								line = docLine;
								handle = ResizeHandle.Move;
								return docType;
							}
						}
					}
				}
			}

			for (int iType = this.m_schema.Types.Count - 1; iType >= 0; iType--)
			{
				DocType docType = this.m_schema.Types[iType];

				if (HitTest(docType.DiagramRectangle, pt, out handle))
					return docType;

				if (docType is DocSelect)
				{
					DocSelect docSel = (DocSelect)docType;
					foreach (DocLine docLine in docSel.Tree)
					{
						DocPoint docPoint = docLine.DiagramLine[docLine.DiagramLine.Count - 1];
						PointF ptA = new PointF((float)(docPoint.X * Factor), (float)docPoint.Y * Factor);
						if (Math.Abs(ptFloat.X - ptA.X) < 4 && Math.Abs(ptFloat.Y - ptA.Y) <= 5)
						{
							handle = ResizeHandle.Move;
							line = docLine;
							return docType;
						}
					}
				}
			}

			foreach (DocComment docType in this.m_schema.Comments)
			{
				if (HitTest(docType.DiagramRectangle, pt, out handle))
					return docType;
			}

			foreach (DocPageTarget docType in this.m_schema.PageTargets)
			{
				if (HitTest(docType.DiagramRectangle, pt, out handle))
					return docType;

				foreach (DocPageSource docSource in docType.Sources)
				{
					if (HitTest(docSource.DiagramRectangle, pt, out handle))
						return docSource;
				}
			}

			foreach (DocPrimitive docType in this.m_schema.Primitives)
			{
				if (HitTest(docType.DiagramRectangle, pt, out handle))
					return docType;
			}

			foreach (DocSchemaRef docSchemaRef in this.m_schema.SchemaRefs)
			{
				foreach (DocDefinitionRef docType in docSchemaRef.Definitions)
				{
					if (HitTest(docType.DiagramRectangle, pt, out handle))
						return docType;
					if (docType.Tree != null)
					{
						foreach (DocLine docLine in docType.Tree)
						{
							DocPoint docPoint = docLine.DiagramLine[docLine.DiagramLine.Count - 1];
							PointF ptA = new PointF((float)(docPoint.X * Factor), (float)docPoint.Y * Factor);
							if (Math.Abs(ptFloat.X - ptA.X) < 4 && Math.Abs(ptFloat.Y - ptA.Y) <= 5)
							{
								handle = ResizeHandle.Move;
								line = docLine;
								return docType;
							}
						}
					}
				}
			}

			return null;
		}

		// Calculate the distance between
		// point pt and the segment p1 --> p2.
		private double FindDistanceToSegment(PointF pt, PointF p1, PointF p2, out PointF closest)
		{
			float dx = p2.X - p1.X;
			float dy = p2.Y - p1.Y;
			if ((dx == 0) && (dy == 0))
			{
				// It's a point not a line segment.        
				closest = p1;
				dx = pt.X - p1.X;
				dy = pt.Y - p1.Y;
				return Math.Sqrt(dx * dx + dy * dy);
			}
			// Calculate the t that minimizes the distance.    
			float t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) / (dx * dx + dy * dy);

			// See if this represents one of the segment's    // end points or a point in the middle.    
			if (t < 0)
			{
				closest = new PointF(p1.X, p1.Y);
				dx = pt.X - p1.X; dy = pt.Y - p1.Y;
			}
			else if (t > 1)
			{
				closest = new PointF(p2.X, p2.Y);
				dx = pt.X - p2.X;
				dy = pt.Y - p2.Y;
			}
			else
			{
				closest = new PointF(p1.X + t * dx, p1.Y + t * dy);
				dx = pt.X - closest.X;
				dy = pt.Y - closest.Y;
			}
			return Math.Sqrt(dx * dx + dy * dy);
		}

		private void CtlExpressG_DragEnter(object sender, DragEventArgs e)
		{
			DocEntity docEnt = e.Data.GetData(typeof(DocEntity)) as DocEntity;
			if (docEnt != null)
			{
				e.Effect = DragDropEffects.Move;
			}
		}

		private void CtlExpressG_DragOver(object sender, DragEventArgs e)
		{
			Point clientPoint = this.PointToClient(new Point(e.X, e.Y));
			double X = (clientPoint.X - this.AutoScrollPosition.X) / Factor;
			double Y = (clientPoint.Y - this.AutoScrollPosition.Y) / Factor;

			DocDefinition docDef = e.Data.GetData(typeof(DocEntity)) as DocEntity;
			if (docDef == null)
			{
				docDef = e.Data.GetData(typeof(DocType)) as DocType;
			}

			if (docDef != null)
			{
				e.Effect = DragDropEffects.Move;

				if (docDef.DiagramRectangle == null)
				{
					docDef.DiagramRectangle = new DocRectangle();
				}

				docDef.DiagramRectangle.X = X;
				docDef.DiagramRectangle.Y = Y;

				if (docDef.DiagramRectangle.Width <= 0.0 || docDef.DiagramRectangle.Height <= 0.0)
				{
					docDef.DiagramRectangle.Width = 400.0;
					docDef.DiagramRectangle.Height = 100.0;// +50.0 * docEnt.Attributes.Count;
				}

				this.Redraw();
			}
		}

		private void CtlExpressG_DragDrop(object sender, DragEventArgs e)
		{
			DocEntity docEnt = e.Data.GetData(typeof(DocEntity)) as DocEntity;
			if (docEnt != null)
			{
				e.Effect = DragDropEffects.Move;
			}

		}

		private void CtlExpressG_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			// auto-expand all attributes and subclasses
			if (this.Selection is DocDefinitionRef)
			{
				if (this.ExpandOperation != null)
				{
					this.ExpandOperation(this, EventArgs.Empty);
				}
			}
		}

	}

	[Flags]
	public enum ResizeHandle
	{
		None = 0,

		North = 1,
		West = 2,
		South = 4,
		East = 8,

		Move = 16,
	}

	public enum ToolMode
	{
		Select = 0,
		Move = 1,
		Link = 2,
	}

}
