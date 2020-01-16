// Name:        FormSelectAttribute.cs
// Description: Dialog for selecting an attribute of an entity as part of a template rule
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2011 BuildingSmart International Ltd.
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
	public partial class FormSelectAttribute : Form
	{
		public FormSelectAttribute()
		{
			InitializeComponent();
		}

		public FormSelectAttribute(DocEntity entity, DocProject project, string selection, bool freeform)
			: this()
		{
			this.textBoxAttributeName.Enabled = freeform;
			this.textBoxAttributeName.Text = selection;

			this.LoadEntity(entity, project, selection);
		}

		private void LoadEntity(DocEntity entity, DocProject project, string selection)
		{
			if (entity == null)
				return;

			// recurse to base
			if (entity.BaseDefinition != null)
			{
				DocEntity docBase = project.GetDefinition(entity.BaseDefinition) as DocEntity;
				LoadEntity(docBase, project, selection);
			}

			// load attributes
			foreach (DocAttribute docAttr in entity.Attributes)
			{
				// if attribute is derived, dont add, but remove existing
				if (!String.IsNullOrEmpty(docAttr.Derived))
				{
					foreach (ListViewItem lvi in this.listView.Items)
					{
						if (lvi.Text.Equals(docAttr.Name))
						{
							lvi.Remove();
							break;
						}
					}
				}
				else
				{
					ListViewItem lvi = new ListViewItem();
					lvi.Tag = docAttr;
					lvi.Text = docAttr.Name;
					lvi.SubItems.Add(docAttr.DefinedType); // INVERSE / SET / LIST / OPTIONAL...
					this.listView.Items.Add(lvi);

					if (selection != null && lvi.Text.Equals(selection))
					{
						lvi.Selected = true;
					}
				}
			}
		}

		/// <summary>
		/// Selected attribute if defined on entity, or null if none or custom attribute
		/// </summary>
		public DocAttribute SelectedAttribute
		{
			get
			{
				if (this.listView.SelectedItems.Count == 1)
				{
					return this.listView.SelectedItems[0].Tag as DocAttribute;
				}

				return null;
			}
		}

		/// <summary>
		/// Name of selected attribute (or custom attribute)
		/// </summary>
		public string Selection
		{
			get
			{
				return this.textBoxAttributeName.Text;
			}
		}


		private void listView_ItemActivate(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void listView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.listView.SelectedItems.Count == 1)
			{
				this.textBoxAttributeName.Text = this.listView.SelectedItems[0].Text;
			}
		}

		private void textBoxAttributeName_Validated(object sender, EventArgs e)
		{
			this.listView.SelectedItems.Clear();
		}
	}
}
