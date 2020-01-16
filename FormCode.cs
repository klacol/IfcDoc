// Name:        FormCode.cs
// Description: Dialog box for generating source code based on MVD schema
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

namespace IfcDoc
{
	public partial class FormCode : Form
	{
		public FormCode()
		{
			InitializeComponent();
		}

		private void buttonPath_Click(object sender, EventArgs e)
		{
			DialogResult res = this.folderBrowserDialog.ShowDialog();
			if (res == DialogResult.OK)
			{
				this.textBoxPath.Text = this.folderBrowserDialog.SelectedPath;
			}
		}

		private void FormCode_Load(object sender, EventArgs e)
		{
			this.comboBoxLanguage.SelectedIndex = 0;
		}

		public string Path
		{
			get
			{
				return this.textBoxPath.Text;
			}
			set
			{
				this.textBoxPath.Text = value;
			}
		}

		public string Language
		{
			get
			{
				return this.comboBoxLanguage.Text;
			}
			set
			{
				this.comboBoxLanguage.SelectedValue = value;
			}
		}


	}
}
