// Name:        FormSelectSchema.cs
// Description: Dialog box for selecting schema
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class FormSelectSchema : Form
	{
		DocProject m_project;

		public FormSelectSchema()
		{
			InitializeComponent();
		}

		public FormSelectSchema(DocProject project) : this()
		{
			this.m_project = project;

			foreach (DocSection docSection in project.Sections)
			{
				if (docSection.Schemas.Count > 0)
				{
					TreeNode tnSection = new TreeNode();
					tnSection.Tag = docSection;
					tnSection.Text = docSection.Name;
					tnSection.ImageIndex = 1;
					this.treeView.Nodes.Add(tnSection);

					foreach (DocSchema docSchema in docSection.Schemas)
					{
						TreeNode tnSchema = new TreeNode();
						tnSchema.Tag = docSchema;
						tnSchema.Text = docSchema.Name;
						tnSchema.ImageIndex = 0;
						tnSection.Nodes.Add(tnSchema);
					}

					tnSection.Expand();
				}
			}
		}

		public DocSchema Selection
		{
			get
			{
				if (this.treeView.SelectedNode != null && this.treeView.SelectedNode.Tag is DocSchema)
				{
					return (DocSchema)this.treeView.SelectedNode.Tag;
				}

				return null;
			}
		}
	}
}
