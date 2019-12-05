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
	public partial class FormSelectPropertyConstant : Form
	{
		DocProject m_project;

		public FormSelectPropertyConstant()
		{
			InitializeComponent();
		}

		public FormSelectPropertyConstant(DocProject project, DocPropertyConstant selection) : this()
		{
			this.m_project = project;

			Dictionary<string, DocPropertyConstant> processed = new Dictionary<string, DocPropertyConstant>();

			foreach (DocPropertyConstant constant in project.PropertyConstants)
			{
				ListViewItem lvi = new ListViewItem();
				lvi.Tag = constant;
				lvi.Text = constant.Name + (processed.ContainsKey(constant.Name) ? "_" + BuildingSmart.Utilities.Conversion.GlobalId.Format( constant.Uuid) : "");
				processed[constant.Name] = constant;
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

		public DocPropertyConstant Selection
		{
			get
			{
				if (this.listView.SelectedItems.Count == 1)
				{
					return this.listView.SelectedItems[0].Tag as DocPropertyConstant;
				}
				return null;
			}
		}
	}
}
