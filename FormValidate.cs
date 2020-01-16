using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class FormValidate : Form
	{
		public FormValidate()
		{
			InitializeComponent();
		}

		public FormValidate(DocProject project, DocModelView docView, DocExchangeDefinition docExchange) : this()
		{
			this.textBoxPath.Text = Properties.Settings.Default.ValidateFile;
			this.checkBoxReport.Checked = Properties.Settings.Default.ValidateReport;

			foreach (DocModelView docEachView in project.ModelViews)
			{
				this.comboBoxView.Items.Add(docEachView);
			}

			if (docView == null && project.ModelViews.Count > 0)
			{
				docView = project.ModelViews[0];
			}
			this.comboBoxView.SelectedItem = docView;

			if (docExchange == null && docView != null && docView.Exchanges.Count > 0)
			{
				docExchange = docView.Exchanges[0];
			}
			this.comboBoxExchange.SelectedItem = docExchange;
		}

		public DocModelView SelectedView
		{
			get
			{
				return this.comboBoxView.SelectedItem as DocModelView;
			}
		}

		public DocExchangeDefinition SelectedExchange
		{
			get
			{
				return this.comboBoxExchange.SelectedItem as DocExchangeDefinition;
			}
		}

		private void comboBoxView_SelectedIndexChanged(object sender, EventArgs e)
		{
			DocModelView docView = this.comboBoxView.SelectedItem as DocModelView;
			if (docView == null)
				return;

			this.comboBoxExchange.Items.Clear();
			foreach (DocExchangeDefinition docExchange in docView.Exchanges)
			{
				this.comboBoxExchange.Items.Add(docExchange);
			}

		}

		private void buttonPath_Click(object sender, EventArgs e)
		{
			DialogResult res = this.openFileDialogIFC.ShowDialog();
			if (res == System.Windows.Forms.DialogResult.OK)
			{
				this.textBoxPath.Text = this.openFileDialogIFC.FileName;
				Properties.Settings.Default.ValidateFile = this.textBoxPath.Text;
			}
		}

		private void checkBoxReport_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.ValidateReport = this.checkBoxReport.Checked;
		}
	}
}
