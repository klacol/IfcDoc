using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using IfcDoc.Schema.DOC;
using IfcDoc.Format.PNG;

namespace IfcDoc
{
	public partial class CtlInheritance : ScrollableControl
	{
		private DocProject m_project;
		private DocModelView m_modelview;
		private DocEntity m_entity;
		private DocEntity m_highlight;
		private DocEntity m_selection;
		private ToolMode m_mode;
		private Image m_image;
		private Dictionary<Rectangle, DocEntity> m_hitmap;
		private Rectangle m_rcHighlight;
		private Rectangle m_rcSelection;

		public CtlInheritance()
		{
			InitializeComponent();

			this.DoubleBuffered = true;
			this.ResizeRedraw = true;
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

		public DocModelView ModelView
		{
			get
			{
				return this.m_modelview;
			}
			set
			{
				this.m_modelview = value;
			}
		}

		public DocEntity Entity
		{
			get
			{
				return this.m_entity;
			}
			set
			{
				this.m_entity = value;
				this.m_selection = null;
				this.m_highlight = null;
				this.Render();
			}
		}

		public ToolMode Mode
		{
			get
			{
				return this.m_mode;
			}
			set
			{
				this.m_mode = value;
			}
		}

		public DocEntity Selection
		{
			get
			{
				return this.m_selection;
			}
		}

		public event EventHandler SelectionChanged;

		public void Render()
		{
			if (this.m_image != null)
			{
				this.m_image.Dispose();
				this.m_image = null;
			}

			Dictionary<DocObject, bool> included = null;
			if (this.m_modelview != null)
			{
				included = new Dictionary<DocObject, bool>();
				this.m_project.RegisterObjectsInScope(this.m_modelview, included);
			}

			this.m_hitmap = new Dictionary<Rectangle, DocEntity>();
			this.m_image = FormatPNG.CreateInheritanceDiagram(this.m_project, included, this.m_entity, null, this.Font, this.m_hitmap);
			if (this.m_image != null)
			{
				this.AutoScrollMinSize = new Size(this.m_image.Width, this.m_image.Height);
			}
			this.Invalidate();
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			if (this.m_image == null)
				return;

			Graphics g = pe.Graphics;
			g.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);
			g.DrawImage(this.m_image, Point.Empty);

			if (this.m_highlight != null)
			{
				g.DrawRectangle(Pens.Red, this.m_rcHighlight);
			}
		}

		private DocEntity Pick(Point pt, out Rectangle rectangle)
		{
			rectangle = Rectangle.Empty;
			pt.X -= this.AutoScrollPosition.X;
			pt.Y -= this.AutoScrollPosition.Y;

			foreach (Rectangle rc in this.m_hitmap.Keys)
			{
				if (rc.Contains(pt))
				{
					rectangle = rc;
					DocEntity docEnt = this.m_hitmap[rc];
					return docEnt;
				}
			}

			return null;
		}

		private void CtlInheritance_MouseDown(object sender, MouseEventArgs e)
		{
			this.m_selection = this.Pick(e.Location, out this.m_rcSelection);
			switch (this.m_mode)
			{
				case ToolMode.Select:
					if (this.SelectionChanged != null)
					{
						this.SelectionChanged(this, EventArgs.Empty);
					}
					break;

				case ToolMode.Move:
					if (this.m_selection != null)
					{
						this.m_selection._InheritanceDiagramFlag = !this.m_selection._InheritanceDiagramFlag;
						this.m_selection.Status = (this.m_selection._InheritanceDiagramFlag ? "H" : String.Empty);
						this.Render();
					}
					break;
			}
		}

		private void CtlInheritance_MouseMove(object sender, MouseEventArgs e)
		{
			this.m_highlight = this.Pick(e.Location, out this.m_rcHighlight);
			switch (this.m_mode)
			{
				case ToolMode.Select:
					if (m_highlight != null)
					{
						this.Cursor = Cursors.Hand;
					}
					else
					{
						this.Cursor = Cursors.Default;
					}
					break;

				case ToolMode.Move:
					if (m_highlight != null)
					{
						this.Cursor = Cursors.UpArrow;
					}
					else
					{
						this.Cursor = Cursors.Default;
					}
					break;
			}

			this.Invalidate();
		}

		private void CtlInheritance_MouseUp(object sender, MouseEventArgs e)
		{

		}
	}
}
