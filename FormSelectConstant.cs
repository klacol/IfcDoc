// Name:        FormSelectConstant.cs
// Description: Dialog box for selecting enumeration
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class FormSelectConstant : Form
	{
		DocProject m_project;

		public FormSelectConstant()
		{
			InitializeComponent();
		}


		public FormSelectConstant(DocProject project, DocConstant selection) : this()
		{
			this.m_project = project;

			Dictionary<string, DocConstant> processed = new Dictionary<string, DocConstant>();
			
			foreach (DocConstant constant in project.Constants)
			{
				ListViewItem lvi = new ListViewItem();
				lvi.Tag = constant;
				lvi.Text = constant.Name +  (processed.ContainsKey(constant.Name) ? "_" + BuildingSmart.Utilities.Conversion.GlobalId.Format( constant.Uuid) : "");
				lvi.ImageIndex = 0;
				this.listView.Items.Add(lvi);

				if (selection == constant)
				{
					lvi.Selected = true;
				}
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (this.listView.SelectedItems.Count == 1)
			{
				this.listView.SelectedItems[0].EnsureVisible();
			}
		}

		public DocConstant Selection
		{
			get
			{
				if (this.listView.SelectedItems.Count == 1)
				{
					return this.listView.SelectedItems[0].Tag as DocConstant;
				}
				return null;
			}
		}
	}
}
