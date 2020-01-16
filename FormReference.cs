// Name:        FormReference.cs
// Description: Dialog for building an IfcReference
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

using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class FormReference : Form
	{
		DocProject m_project;
		DocDefinition m_base;
		Dictionary<string, DocObject> m_map;

		public FormReference()
		{
			InitializeComponent();
		}

		public FormReference(DocProject docProject, DocDefinition docBase, Dictionary<string, DocObject> map, string value)
			: this()
		{
			this.m_project = docProject;
			this.m_base = docBase;
			this.m_map = map;

			// parse value
			CvtValuePath valuepath = CvtValuePath.Parse(value, map);
			LoadValuePath(valuepath);

			this.textBoxReference.ReadOnly = false;
		}

		public string ValuePath
		{
			get
			{
				return this.textBoxReference.Text;
			}
		}

		private void LoadValuePath(CvtValuePath valuepath)
		{
			this.textBoxReference.Text = String.Empty;
			this.listViewReference.Items.Clear();

			if (valuepath != null)
			{
				this.textBoxReference.Text = valuepath.ToString();
			}
			else
			{
				this.textBoxReference.Text = String.Empty;
			}

			while (valuepath != null)
			{
				ListViewItem lvi = new ListViewItem();
				lvi.Tag = valuepath;

				if (valuepath.Type != null)
				{
					lvi.Text = valuepath.Type.Name;
				}

				if (valuepath.Property != null)
				{
					lvi.SubItems.Add(valuepath.Property.Name);

					if (valuepath.Identifier != null)
					{
						lvi.SubItems.Add(valuepath.Identifier);
					}
				}

				this.listViewReference.Items.Add(lvi);

				valuepath = valuepath.InnerPath;
			}
		}

		private void buttonInsert_Click(object sender, EventArgs e)
		{
			DocDefinition docBase = this.m_base;
			DocDefinition docDefinition = null;


			// for now, clear it -- future: allow incremental replacement

			CvtValuePath valuepathouter = null;
			CvtValuePath valuepathinner = null;

			// keep building
			while (docBase != null)
			{
				using (FormSelectEntity formEntity = new FormSelectEntity(docBase, docDefinition, this.m_project, SelectDefinitionOptions.Entity | SelectDefinitionOptions.Type))
				{
					if (formEntity.ShowDialog(this) == System.Windows.Forms.DialogResult.OK && formEntity.SelectedEntity != null)
					{
						CvtValuePath valuepath = null;
						if (formEntity.SelectedEntity is DocEntity)
						{
							using (FormSelectAttribute formAttribute = new FormSelectAttribute((DocEntity)formEntity.SelectedEntity, this.m_project, null, false))
							{
								if (formAttribute.ShowDialog(this) == System.Windows.Forms.DialogResult.OK && formAttribute.SelectedAttribute != null)
								{
									string item = null;
									switch (formAttribute.SelectedAttribute.GetAggregation())
									{
										case DocAggregationEnum.SET:
											// if set collection, then qualify by name
											// future: more intelligent UI for picking property sets, properties
											using (FormSelectItem formItem = new FormSelectItem())
											{
												if (formItem.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
												{
													item = formItem.Item;
												}
											}
											break;

										case DocAggregationEnum.LIST:
											// if list collection, then qualify by index
											// future: more intelligent UI for picking list indices
											using (FormSelectItem formItem = new FormSelectItem())
											{
												if (formItem.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
												{
													item = formItem.Item;
												}
											}
											break;
									}

									// now add entry to listview
									valuepath = new CvtValuePath(formEntity.SelectedEntity, formAttribute.SelectedAttribute, item, null);

									if (valuepathinner != null)
									{
										valuepathinner.InnerPath = valuepath;
									}
									valuepathinner = valuepath;

									if (valuepathouter == null)
									{
										valuepathouter = valuepath;
									}

									// drill in
									if (this.m_map.ContainsKey(formAttribute.SelectedAttribute.DefinedType))
									{
										docBase = this.m_map[formAttribute.SelectedAttribute.DefinedType] as DocDefinition;
									}
								}
								else
								{
									break;
								}
							}

						}
						else if (formEntity.SelectedEntity is DocType)
						{
							valuepath = new CvtValuePath(formEntity.SelectedEntity, null, null, null);

							if (valuepathinner != null)
							{
								valuepathinner.InnerPath = valuepath;
							}
							valuepathinner = valuepath;

							if (valuepathouter == null)
							{
								valuepathouter = valuepath;
							}

							docBase = null;
						}

					}
					else
					{
						break;
					}

				}
			}

			this.LoadValuePath(valuepathouter);
		}

		private void buttonRemove_Click(object sender, EventArgs e)
		{
			this.LoadValuePath(null);
		}

		private void listViewReference_SelectedIndexChanged(object sender, EventArgs e)
		{
		}

		private void buttonProperty_Click(object sender, EventArgs e)
		{
			using (FormSelectProperty form = new FormSelectProperty(this.m_base as DocEntity, this.m_project, false))
			{
				if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
				{
					string value = form.GenerateValuePath();
					CvtValuePath valuepath = CvtValuePath.Parse(value, this.m_map);
					LoadValuePath(valuepath);
				}
			}
		}

		private void buttonQuantity_Click(object sender, EventArgs e)
		{
			using (FormSelectQuantity form = new FormSelectQuantity(this.m_base as DocEntity, this.m_project, false))
			{
				if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
				{
					string value = form.GenerateValuePath();
					CvtValuePath valuepath = CvtValuePath.Parse(value, this.m_map);
					LoadValuePath(valuepath);
				}
			}
		}
	}
}
