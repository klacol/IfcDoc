// Name:        FormGenerate.cs
// Description: Dialog box for generating documentation based on MVD schema
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
	public partial class FormGenerate : Form
	{
		DocProject m_project;

		public FormGenerate()
		{
			InitializeComponent();

			this.textBoxPath.Text = Properties.Settings.Default.OutputPath;
			this.textBoxImagesDocumentation.Text = Properties.Settings.Default.InputPathGeneral;
			this.textBoxExternalConverter.Text = Properties.Settings.Default.ConverterPath;
			this.checkBoxSkip.Checked = Properties.Settings.Default.SkipDiagrams;
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			Properties.Settings.Default.OutputPath = this.textBoxPath.Text;
			Properties.Settings.Default.InputPathGeneral = this.textBoxImagesDocumentation.Text;
			Properties.Settings.Default.SkipDiagrams = this.checkBoxSkip.Checked;
			Properties.Settings.Default.Save();
		}

		private void buttonPath_Click(object sender, EventArgs e)
		{
			this.folderBrowserDialog.SelectedPath = this.textBoxPath.Text;
			DialogResult res = this.folderBrowserDialog.ShowDialog();
			if (res == DialogResult.OK)
			{
				this.textBoxPath.Text = this.folderBrowserDialog.SelectedPath;
			}
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

				this.checkedListBoxViews.Items.Clear();
				foreach (DocPublication docView in this.m_project.Publications)
				{
					this.checkedListBoxViews.Items.Add(docView);
				}
			}
		}

		public DocPublication[] Publications
		{
			get
			{
				List<DocPublication> list = new List<DocPublication>();
				for (int i = 0; i < checkedListBoxViews.Items.Count; i++)
				{
					bool check = this.checkedListBoxViews.GetItemChecked(i);
					if (check)
					{
						list.Add((DocPublication)this.checkedListBoxViews.Items[i]);
					}
				}

				if (list.Count > 0)
				{
					return list.ToArray();
				}

				return null;
			}
			set
			{
				if (value != null)
				{
					foreach (DocPublication view in value)
					{
						int index = this.checkedListBoxViews.Items.IndexOf(view);
						if (index >= 0)
						{
							this.checkedListBoxViews.SetItemChecked(index, true);
						}
					}
				}
			}
		}

		private void UpdateEnabled()
		{
			this.buttonOK.Enabled = (this.Publications != null && this.Publications.Length > 0);
		}

		private void checkedListBoxViews_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.UpdateEnabled();
		}

		private void checkedListBoxViews_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			this.UpdateEnabled();
			if (e.NewValue == CheckState.Checked)
			{
				this.buttonOK.Enabled = true;
			}
		}

		private void buttonImagesDocumentation_Click(object sender, EventArgs e)
		{
			this.folderBrowserDialog.SelectedPath = this.textBoxImagesDocumentation.Text;
			DialogResult res = this.folderBrowserDialog.ShowDialog();
			if (res == DialogResult.OK)
			{
				this.textBoxImagesDocumentation.Text = this.folderBrowserDialog.SelectedPath;
			}
		}

		private void checkBoxSkip_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.SkipDiagrams = this.checkBoxSkip.Checked;
		}

		private void buttonExternalConverter_Click(object sender, EventArgs e)
		{
			DialogResult res = this.openFileDialogConverter.ShowDialog(this);
			if (res == System.Windows.Forms.DialogResult.OK)
			{
				this.textBoxExternalConverter.Text = this.openFileDialogConverter.FileName;
				Properties.Settings.Default.ConverterPath = this.openFileDialogConverter.FileName;
			}
		}

		private void buttonExternalConverterClear_Click(object sender, EventArgs e)
		{
			this.textBoxExternalConverter.Text = String.Empty;
			Properties.Settings.Default.ConverterPath = String.Empty;
		}

	}
}
