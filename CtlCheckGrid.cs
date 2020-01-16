// Name:        CtlCheckGrid.cs
// Description: Checkbox grid
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

using IfcDoc;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class CtlCheckGrid : ScrollableControl
	{
		ICheckGrid m_datasource;
		ToolMode m_toolmode;
		object m_selection;

		const int CX = 12;
		const int CY = 12;

		const int SX = 200;
		const int SY = 200;

		public CtlCheckGrid()
		{
			InitializeComponent();

			this.DoubleBuffered = true;
			this.ResizeRedraw = true;
			this.AutoScroll = true;
		}

		public ICheckGrid CheckGridSource
		{
			get
			{
				return this.m_datasource;
			}
			set
			{
				this.m_datasource = value;
				if (this.m_datasource != null)
				{
					this.AutoScrollMinSize = new Size(SX + CX * this.m_datasource.GetColumnCount(), SY + CY * this.m_datasource.GetRowCount());
				}
				this.Invalidate();
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
				this.m_selection = value;
				if (this.SelectionChanged != null)
				{
					this.SelectionChanged(this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler SelectionChanged;

		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);

			if (this.m_datasource == null)
				return;

			Graphics g = pe.Graphics;
			g.TranslateTransform(this.AutoScrollPosition.X, this.AutoScrollPosition.Y);

			// draw column header
			for (int iCol = 0; iCol < this.m_datasource.GetColumnCount(); iCol++)
			{
				object col = this.m_datasource.GetColumn(iCol);
				g.DrawRectangle(Pens.Black, SX + iCol * CX, 0, CY, this.AutoScrollMinSize.Height);
				g.DrawString(col.ToString(), this.Font, Brushes.Black, SX + iCol * CX - 2, 0, new System.Drawing.StringFormat(StringFormatFlags.DirectionVertical));
			}

			for (int iRow = 0; iRow < this.m_datasource.GetRowCount(); iRow++)
			{
				object row = this.m_datasource.GetRow(iRow);

				// draw row header
				g.DrawRectangle(Pens.Black, 0, SY + iRow * CY, this.AutoScrollMinSize.Width, CY);
				g.DrawString(row.ToString(), this.Font, Brushes.Black, 0, SY + iRow * CY);

				for (int iCol = 0; iCol < this.m_datasource.GetColumnCount(); iCol++)
				{
					object col = this.m_datasource.GetColumn(iCol);

					// draw cell
					CellValue val = this.m_datasource.GetCell(iRow, iCol);
					Brush brush = Brushes.Gray; // incompatible
					switch (val)
					{
						case CellValue.None:
							brush = Brushes.LightGray;
							break;

						case CellValue.Mandatory:
							brush = Brushes.LightGreen;
							break;

						case CellValue.Recommended:
							brush = Brushes.Blue;
							break;

						case CellValue.NotRelevant:
							brush = Brushes.White;
							break;

						case CellValue.NotRecommended:
							brush = Brushes.Yellow;
							break;

						case CellValue.Excluded:
							brush = Brushes.Red;
							break;
					}
					g.FillRectangle(brush, new Rectangle(SX + iCol * CX + 1, SY + iRow * CY + 1, CX - 1, CY - 1));

#if false
                    if (val != CellValue.Unavailable)
                    {
                        g.FillRectangle(Brushes.White, new Rectangle(SX + iCol * CX + 1, SY + iRow * CY + 1, CX-1, CY-1));
                        if (val == CellValue.Optional || val == CellValue.Required)
                        {
                            g.DrawLine(Pens.Black, SX + iCol * CX, SY + (iRow + 1) * CY, SX + (iCol + 1) * CX, SY + iRow * CY);
                        }
                        if (val == CellValue.Required)
                        {
                            g.DrawLine(Pens.Black, SX + iCol * CX, SY + iRow * CY, SX + (iCol + 1) * CX, SY + (iRow + 1) * CY);
                        }
                    }
                    else
                    {
                        g.FillRectangle(Brushes.DarkGray, new Rectangle(SX + iCol * CX + 1, SY + iRow * CY + 1, CX - 1, CY - 1));
                    }
#endif
				}
			}
		}

		private object Pick(int x, int y)
		{
			if (this.m_datasource == null)
				return null;

			int iCol = (x - SX) / CX;
			int iRow = (y - SY) / CY;

			if (x < SX && iRow >= 0 && iRow < this.m_datasource.GetRowCount())
			{
				// clicking on row
				return this.m_datasource.GetRow(iRow);
			}
			else if (y < SY && iCol >= 0 && iCol < this.m_datasource.GetColumnCount())
			{
				// clicking on column
				return this.m_datasource.GetColumn(iCol);
			}
			else if (iCol >= 0 && iRow >= 0 && iCol < this.m_datasource.GetColumnCount() && iRow < this.m_datasource.GetRowCount())
			{
				return this.m_datasource.GetObject(iRow, iCol);
			}

			return null;
		}

		private void CtlCheckGrid_MouseClick(object sender, MouseEventArgs e)
		{
			if (this.m_datasource == null)
				return;

			Point pt = e.Location;
			pt.X -= this.AutoScrollPosition.X;
			pt.Y -= this.AutoScrollPosition.Y;

			if (this.Mode == IfcDoc.ToolMode.Select)
			{
				this.Selection = this.Pick(pt.X, pt.Y);
				return;
			}

			int iCol = (pt.X - SX) / CX;
			int iRow = (pt.Y - SY) / CY;
			if (iCol < 0 || iRow < 0)
				return;

			CellValue oldval = this.m_datasource.GetCell(iRow, iCol);
			if (oldval == CellValue.Unavailable)
				return;
			CellValue newval = oldval;
			switch (oldval)
			{

				case CellValue.None:
					newval = CellValue.Recommended;
					break;

				case CellValue.Recommended:
					newval = CellValue.Mandatory;
					break;

				case CellValue.Mandatory:
					newval = CellValue.None;
					break;

				default:
					newval = CellValue.Mandatory;
					break;
			}
			this.m_datasource.SetCell(iRow, iCol, newval);

			this.Invalidate(false);
		}

		private void CtlCheckGrid_MouseMove(object sender, MouseEventArgs e)
		{
			if (this.m_datasource == null)
				return;

			Point pt = e.Location;
			pt.X -= this.AutoScrollPosition.X;
			pt.Y -= this.AutoScrollPosition.Y;

			switch (this.Mode)
			{
				case ToolMode.Select:
					{
						object link = this.Pick(pt.X, pt.Y);
						if (link != null)
						{
							this.Cursor = Cursors.Hand;
						}
						else
						{
							this.Cursor = Cursors.Default;
						}
					}
					break;

				case IfcDoc.ToolMode.Move:
				case IfcDoc.ToolMode.Link:
					{
						int iCol = (pt.X - SX) / CX;
						int iRow = (pt.Y - SY) / CY;
						if (iCol >= 0 && iCol < this.m_datasource.GetColumnCount() &&
							iRow >= 0 && iRow < this.m_datasource.GetRowCount())
						{
							CellValue cell = this.m_datasource.GetCell(iRow, iCol);
							if (cell != CellValue.Unavailable)
							{
								this.Cursor = Cursors.UpArrow;
							}
							else
							{
								this.Cursor = Cursors.No;
							}
						}
						else
						{
							this.Cursor = Cursors.Default;
						}
					}
					break;
			}
		}
	}


	public interface ICheckGrid
	{
		/// <summary>
		/// Returns object for column, where ToString() is shown as text; NULL if no more columns.
		/// </summary>
		/// <param name="col"></param>
		/// <returns></returns>
		object GetColumn(int col);

		/// <summary>
		/// Returns object for row, where ToString() is shown as text; NULL if no more rows.
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		object GetRow(int row);

		/// <summary>
		/// Returns object at cell.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="col"></param>
		/// <returns></returns>
		object GetObject(int row, int col);

		/// <summary>
		/// Returns value for cell.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="col"></param>
		/// <returns></returns>
		CellValue GetCell(int row, int col);

		/// <summary>
		/// Sets value for cell.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="col"></param>
		/// <param name="val"></param>
		void SetCell(int row, int col, CellValue val);

		int GetColumnCount();

		int GetRowCount();
	}

	public enum CellValue
	{
		/// <summary>
		/// Black -- entry is not possible (not compatible)
		/// </summary>
		Unavailable = -1,

		/// <summary>
		/// Grey -- entry is possible but not defined
		/// </summary>
		None = 0,

		/// <summary>
		/// Green indicator
		/// </summary>
		Mandatory = 1,

		/// <summary>
		/// Blue indicator
		/// </summary>
		Recommended = 2,

		/// <summary>
		/// White indicator - entry defined
		/// </summary>
		NotRelevant = 3,

		/// <summary>
		/// Yellow indicator
		/// </summary>
		NotRecommended = 4,

		/// <summary>
		/// Red indicator
		/// </summary>
		Excluded = 5,
	}

	/// <summary>
	/// Shows entities for each row, exchanges for each column
	/// </summary>
	public class CheckGridConcept : ICheckGrid
	{
		DocTemplateDefinition m_template;
		DocModelView m_view;
		DocProject m_project;

		public CheckGridConcept(DocTemplateDefinition docTemplate, DocModelView docView, DocProject docProject)
		{
			this.m_template = docTemplate;
			this.m_view = docView;
			this.m_project = docProject;
		}

		public int GetColumnCount()
		{
			if (this.m_view == null)
				return 0;

			return this.m_view.Exchanges.Count;
		}

		public int GetRowCount()
		{
			if (this.m_view == null)
				return 0;

			return this.m_view.ConceptRoots.Count;
		}

		public object GetColumn(int col)
		{
			if (col >= 0 && col < this.m_view.Exchanges.Count)
			{
				return this.m_view.Exchanges[col];
			}

			return null;
		}

		public object GetRow(int row)
		{
			if (row >= 0 && row < this.m_view.ConceptRoots.Count)
			{
				return this.m_view.ConceptRoots[row];
			}

			return null;
		}

		public object GetObject(int row, int col)
		{
			DocConceptRoot docRoot = this.m_view.ConceptRoots[row];
			DocExchangeDefinition docExchange = this.m_view.Exchanges[col];

			foreach (DocTemplateUsage docUsage in docRoot.Concepts)
			{
				if (docUsage.Definition == this.m_template)
				{
					foreach (DocExchangeItem docEx in docUsage.Exchanges)
					{
						if (docEx.Exchange == docExchange && docEx.Applicability == DocExchangeApplicabilityEnum.Export)
						{
							return docEx;
						}
					}

					return null;
				}
			}

			return null;
		}

		public CellValue GetCell(int row, int col)
		{
			DocConceptRoot docRoot = this.m_view.ConceptRoots[row];
			DocExchangeDefinition docExchange = this.m_view.Exchanges[col];

			foreach (DocTemplateUsage docUsage in docRoot.Concepts)
			{
				if (docUsage.Definition == this.m_template)
				{
					foreach (DocExchangeItem docEx in docUsage.Exchanges)
					{
						if (docEx.Exchange == docExchange && docEx.Applicability == DocExchangeApplicabilityEnum.Export)
						{
							switch (docEx.Requirement)
							{
								case DocExchangeRequirementEnum.Mandatory:
									return CellValue.Mandatory;

								case DocExchangeRequirementEnum.Optional:
									return CellValue.Recommended;

								case DocExchangeRequirementEnum.NotRelevant:
									return CellValue.NotRelevant;

								case DocExchangeRequirementEnum.NotRecommended:
									return CellValue.NotRecommended;

								case DocExchangeRequirementEnum.Excluded:
									return CellValue.Excluded;
							}
						}
					}

					return CellValue.NotRelevant;
				}
			}

			return CellValue.None;
		}

		public void SetCell(int row, int col, CellValue val)
		{
			DocConceptRoot docRoot = this.m_view.ConceptRoots[row];
			DocExchangeDefinition docExchange = this.m_view.Exchanges[col];

			DocTemplateUsage docUsage = null;
			foreach (DocTemplateUsage eachUsage in docRoot.Concepts)
			{
				if (eachUsage.Definition == this.m_template)
				{
					docUsage = eachUsage;
					break;
				}
			}

			if (docUsage == null)
			{
				docUsage = new DocTemplateUsage();
				docRoot.Concepts.Add(docUsage);
				docUsage.Definition = this.m_template;
			}

			DocExchangeRequirementEnum req = DocExchangeRequirementEnum.NotRelevant;
			switch (val)
			{
				case CellValue.Mandatory:
					req = DocExchangeRequirementEnum.Mandatory;
					break;

				case CellValue.Recommended:
					req = DocExchangeRequirementEnum.Optional;
					break;
			}
			docUsage.RegisterExchange(docExchange, req);
		}

	}

	/// <summary>
	/// Shows concepts for each row, exchanges for each column
	/// </summary>
	public class CheckGridEntity : ICheckGrid
	{
		DocConceptRoot m_root;
		DocModelView m_view;
		DocProject m_project;
		List<DocTemplateDefinition> m_listTemplate;

		public CheckGridEntity(DocConceptRoot docRoot, DocModelView docView, DocProject docProject)
		{
			this.m_root = docRoot;
			this.m_view = docView;
			this.m_project = docProject;
			this.m_listTemplate = new List<DocTemplateDefinition>();

			List<DocTemplateDefinition> listTemplate = docProject.GetTemplateList();

			//... filter out templates to only those that apply to entity...

			foreach (DocTemplateDefinition docTemplate in listTemplate)
			{
				if (docTemplate.Rules != null && docTemplate.Rules.Count > 0) // don't include abstract/organizational templates
				{
					bool include = false;

					// check for inheritance                
					DocObject docApplicableEntity = docProject.GetDefinition(docTemplate.Type) as DocEntity;

					if (docTemplate.Type != null && docApplicableEntity is DocEntity)
					{
						// check for inheritance
						DocEntity docBase = docRoot.ApplicableEntity;
						while (docBase != null)
						{
							if (docBase == docApplicableEntity)
							{
								include = true;
								break;
							}

							if (docBase.BaseDefinition == null)
								break;

							docBase = docProject.GetDefinition(docBase.BaseDefinition) as DocEntity;
						}
					}

					if (include)
					{
						this.m_listTemplate.Add(docTemplate);
					}
				}
			}

		}

		public int GetColumnCount()
		{
			if (this.m_view != null)
			{
				return this.m_view.Exchanges.Count;
			}

			return 0;
		}

		public int GetRowCount()
		{
			return this.m_listTemplate.Count;
		}

		public object GetColumn(int col)
		{
			if (col >= 0 && col < this.m_view.Exchanges.Count)
			{
				return this.m_view.Exchanges[col];
			}

			return null;
		}

		public object GetRow(int row)
		{
			if (row >= 0 && row < this.m_listTemplate.Count)
			{
				return this.m_listTemplate[row];
			}

			return null;
		}

		public object GetObject(int row, int col)
		{
			if (row < 0 || row >= this.m_listTemplate.Count)
				return null;

			if (col < 0 || col >= this.m_view.Exchanges.Count)
				return null;

			DocTemplateDefinition docTemplate = this.m_listTemplate[row];
			DocExchangeDefinition docExchange = this.m_view.Exchanges[col];

			foreach (DocTemplateUsage docUsage in this.m_root.Concepts)
			{
				if (docUsage.Definition == docTemplate)
				{
					foreach (DocExchangeItem docItem in docUsage.Exchanges)
					{
						if (docItem.Exchange == docExchange && docItem.Applicability == DocExchangeApplicabilityEnum.Export)
						{
							return docItem;
						}
					}
				}
			}

			return CellValue.None;
		}

		public CellValue GetCell(int row, int col)
		{
			if (row < 0 || row >= this.m_listTemplate.Count)
				return CellValue.Unavailable;

			if (col < 0 || col >= this.m_view.Exchanges.Count)
				return CellValue.Unavailable;

			DocTemplateDefinition docTemplate = this.m_listTemplate[row];
			DocExchangeDefinition docExchange = this.m_view.Exchanges[col];

			foreach (DocTemplateUsage docUsage in this.m_root.Concepts)
			{
				if (docUsage.Definition == docTemplate)
				{
					foreach (DocExchangeItem docItem in docUsage.Exchanges)
					{
						if (docItem.Exchange == docExchange && docItem.Applicability == DocExchangeApplicabilityEnum.Export)
						{
							switch (docItem.Requirement)
							{
								case DocExchangeRequirementEnum.Mandatory:
									return CellValue.Mandatory;

								case DocExchangeRequirementEnum.Optional:
									return CellValue.Recommended;

								default:
									return CellValue.None;
							}
						}
					}
				}
			}

			return CellValue.None;
		}

		public void SetCell(int row, int col, CellValue val)
		{
			DocTemplateDefinition docTemplate = this.m_listTemplate[row];
			DocExchangeDefinition docExchange = this.m_view.Exchanges[col];

			DocTemplateUsage docUsage = null;
			foreach (DocTemplateUsage eachUsage in this.m_root.Concepts)
			{
				if (eachUsage.Definition == docTemplate)
				{
					docUsage = eachUsage;
					break;
				}
			}

			if (docUsage == null)
			{
				docUsage = new DocTemplateUsage();
				this.m_root.Concepts.Add(docUsage);
				docUsage.Definition = docTemplate;
			}

			DocExchangeRequirementEnum req = DocExchangeRequirementEnum.NotRelevant;
			switch (val)
			{
				case CellValue.Mandatory:
					req = DocExchangeRequirementEnum.Mandatory;
					break;

				case CellValue.Recommended:
					req = DocExchangeRequirementEnum.Optional;
					break;
			}
			docUsage.RegisterExchange(docExchange, req);
		}
	}

	/// <summary>
	/// Shows entities for each row, concepts for each column
	/// </summary>
	public class CheckGridExchange : ICheckGrid
	{
		DocProject m_project;
		DocModelView m_view;
		DocExchangeDefinition m_exchange;

		List<DocTemplateDefinition> m_listTemplate;

		public CheckGridExchange(DocExchangeDefinition docExchange, DocModelView docView, DocProject docProject)
		{
			this.m_project = docProject;
			this.m_view = docView;
			this.m_exchange = docExchange;
			this.m_listTemplate = docProject.GetTemplateList();

			// filter out template list to only those that are currently used
			for (int i = this.m_listTemplate.Count - 1; i >= 0; i--)
			{
				bool used = false;
				foreach (DocConceptRoot docRoot in this.m_view.ConceptRoots)
				{
					foreach (DocTemplateUsage docUsage in docRoot.Concepts)
					{
						if (docUsage.Definition == this.m_listTemplate[i])
						{
							used = true;
							break;
						}
					}
				}

				if (!used)
				{
					this.m_listTemplate.RemoveAt(i);
				}
			}
		}

		public int GetColumnCount()
		{
			return this.m_listTemplate.Count;
		}

		public int GetRowCount()
		{
			return this.m_view.ConceptRoots.Count;
		}

		public object GetColumn(int col)
		{
			if (col >= 0 && col < this.m_listTemplate.Count)
			{
				return this.m_listTemplate[col];
			}

			return null;
		}

		public object GetRow(int row)
		{
			if (row >= 0 && row < this.m_view.ConceptRoots.Count)
			{
				return this.m_view.ConceptRoots[row];
			}

			return null;
		}

		public object GetObject(int row, int col)
		{
			if (col >= this.m_listTemplate.Count || row >= this.m_view.ConceptRoots.Count)
				return null;

			DocTemplateDefinition docTemplate = this.m_listTemplate[col];
			DocConceptRoot docRoot = this.m_view.ConceptRoots[row];

			foreach (DocTemplateUsage docUsage in docRoot.Concepts)
			{
				if (docUsage.Definition == docTemplate)
				{
					foreach (DocExchangeItem docExchangeItem in docUsage.Exchanges)
					{
						if (docExchangeItem.Exchange == this.m_exchange && docExchangeItem.Applicability == DocExchangeApplicabilityEnum.Export)
						{
							return docUsage;
						}
					}
				}
			}

			return null;
		}

		public CellValue GetCell(int row, int col)
		{
			if (col >= this.m_listTemplate.Count || row >= this.m_view.ConceptRoots.Count)
				return CellValue.Unavailable;

			DocTemplateDefinition docTemplate = this.m_listTemplate[col];
			DocConceptRoot docRoot = this.m_view.ConceptRoots[row];

			// return Unavailable if template is incompatible with root
			bool applicable = false;
			DocDefinition docDef = this.m_project.GetDefinition(docTemplate.Type);
			DocEntity docEnt = docRoot.ApplicableEntity;
			while (docEnt != null)
			{
				if (docEnt == docDef)
				{
					applicable = true;
					break;
				}

				docEnt = this.m_project.GetDefinition(docEnt.BaseDefinition) as DocEntity;
			}

			if (!applicable)
			{
				return CellValue.Unavailable;
			}

			foreach (DocTemplateUsage docUsage in docRoot.Concepts)
			{
				if (docUsage.Definition == docTemplate)
				{
					foreach (DocExchangeItem docExchangeItem in docUsage.Exchanges)
					{
						if (docExchangeItem.Exchange == this.m_exchange && docExchangeItem.Applicability == DocExchangeApplicabilityEnum.Export)
						{
							switch (docExchangeItem.Requirement)
							{
								case DocExchangeRequirementEnum.Mandatory:
									return CellValue.Mandatory;

								case DocExchangeRequirementEnum.Optional:
									return CellValue.Recommended;

								case DocExchangeRequirementEnum.NotRelevant:
									return CellValue.NotRelevant;

								case DocExchangeRequirementEnum.NotRecommended:
									return CellValue.NotRecommended;

								case DocExchangeRequirementEnum.Excluded:
									return CellValue.Excluded;
							}
						}
					}

					return CellValue.NotRelevant;
				}
			}

			return CellValue.None;
		}

		public void SetCell(int row, int col, CellValue val)
		{
			DocTemplateDefinition docTemplate = this.m_listTemplate[col];
			DocConceptRoot docRoot = this.m_view.ConceptRoots[row];

			DocTemplateUsage docConcept = null;
			foreach (DocTemplateUsage docUsage in docRoot.Concepts)
			{
				if (docUsage.Definition == docTemplate)
				{
					docConcept = docUsage;
					break;
				}
			}

			if (docConcept == null)
			{
				docConcept = new DocTemplateUsage();
				docRoot.Concepts.Add(docConcept);
				docConcept.Definition = docTemplate;
			}

			DocExchangeRequirementEnum req = DocExchangeRequirementEnum.NotRelevant;
			switch (val)
			{
				case CellValue.Mandatory:
					req = DocExchangeRequirementEnum.Mandatory;
					break;

				case CellValue.Recommended:
					req = DocExchangeRequirementEnum.Optional;
					break;
			}
			docConcept.RegisterExchange(this.m_exchange, req);
		}
	}
}
