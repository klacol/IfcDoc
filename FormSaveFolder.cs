using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IfcDoc
{
	public partial class FormSaveFolder : Form
	{
		static bool s_schemas = true;
		static bool s_exchanges = true;
		static bool s_examples = false;
		static bool s_localize = false;

		public FormSaveFolder()
		{
			InitializeComponent();

			this.checkBoxSchemas.Checked = s_schemas;
			this.checkBoxExchanges.Checked = s_exchanges;
			this.checkBoxExamples.Checked = s_examples;
			this.checkBoxLocalization.Checked = s_localize;
		}

		public string SelectedPath
		{
			get
			{
				return this.textBox1.Text;
			}
			set
			{
				this.textBox1.Text = value;
			}
		}

		public FolderStorageOptions Options
		{
			get
			{
				FolderStorageOptions options = FolderStorageOptions.None;
				if (this.checkBoxSchemas.Checked)
					options |= FolderStorageOptions.Schemas;

				if (this.checkBoxExchanges.Checked)
					options |= FolderStorageOptions.Exchanges;

				if (this.checkBoxExamples.Checked)
					options |= FolderStorageOptions.Examples;

				if (this.checkBoxLocalization.Checked)
					options |= FolderStorageOptions.Examples;

				return options;
			}
		}

		private void buttonFolder_Click(object sender, EventArgs e)
		{
			this.folderBrowserDialog.SelectedPath = this.textBox1.Text;
			DialogResult res = this.folderBrowserDialog.ShowDialog();
			if (res == System.Windows.Forms.DialogResult.OK)
			{
				this.textBox1.Text = this.folderBrowserDialog.SelectedPath;
			}
		}

		private void checkBoxSchemas_CheckedChanged(object sender, EventArgs e)
		{
			s_schemas = this.checkBoxSchemas.Checked;
		}

		private void checkBoxExchanges_CheckedChanged(object sender, EventArgs e)
		{
			s_exchanges = this.checkBoxExchanges.Checked;
		}

		private void checkBoxExamples_CheckedChanged(object sender, EventArgs e)
		{
			s_examples = this.checkBoxExamples.Checked;
		}

		private void checkBoxLocalization_CheckedChanged(object sender, EventArgs e)
		{
			s_localize = this.checkBoxLocalization.Checked;
		}
	}
}
