// Name:        FormSelectView.cs
// Description: Dialog box for selecting model view
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
	public partial class FormSelectView : Form
	{
		DocProject m_project;

		public FormSelectView()
		{
			InitializeComponent();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="project">The project.</param>
		public FormSelectView(DocProject project, string description)
			: this()
		{
			this.m_project = project;

			if (description != null)
			{
				this.labelDescription.Text = description;
			}

			foreach (DocModelView docView in this.m_project.ModelViews)
			{
				FillTree(null, docView);
			}

			this.treeView.ExpandAll();
		}

		private void FillTree(TreeNode tnParent, DocModelView docView)
		{
			TreeNode tn = new TreeNode();
			tn.Tag = docView;
			tn.Text = docView.Name;
			tn.ImageIndex = 0;

			if (tnParent != null)
			{
				tnParent.Nodes.Add(tn);
			}
			else
			{
				this.treeView.Nodes.Add(tn);
			}

			// recurse
			foreach (DocModelView docSub in docView.ModelViews)
			{
				FillTree(tn, docSub);
			}
		}

		public string VersionMVDXML
		{
			get
			{
				switch (this.comboBoxMVDXML.SelectedIndex)
				{
					case 0:
						return IfcDoc.Schema.MVD.mvdXML.NamespaceV11;

					case 1:
						return IfcDoc.Schema.MVD.mvdXML.NamespaceV12;
				}

				return null;
			}
			set
			{
				if (value != null)
				{
					this.comboBoxMVDXML.Visible = true;
					switch (value)
					{
						case IfcDoc.Schema.MVD.mvdXML.NamespaceV11:
							this.comboBoxMVDXML.SelectedIndex = 0;
							break;

						case IfcDoc.Schema.MVD.mvdXML.NamespaceV12:
							this.comboBoxMVDXML.SelectedIndex = 1;
							break;
					}
				}
				else
				{
					this.comboBoxMVDXML.Visible = false;
				}
			}
		}

		public DocModelView[] Selection
		{
			get
			{
				DocModelView[] sel = null;
				if (this.treeView.SelectedNode != null)
				{
					sel = new DocModelView[1];
					sel[0] = (DocModelView)this.treeView.SelectedNode.Tag;
				}

				return sel;
			}
			set
			{
				//...
			}
		}
	}
}
