using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class FormSelectExchange : Form
	{
		public FormSelectExchange()
		{
			InitializeComponent();
		}

		public FormSelectExchange(DocModelView docView)
			: this()
		{
			foreach (DocExchangeDefinition docExchange in docView.Exchanges)
			{
				ListViewItem lvi = new ListViewItem();
				lvi.Tag = docExchange;
				lvi.Text = docExchange.Name;
				lvi.ImageIndex = 0;
				this.listView.Items.Add(lvi);
			}
		}

		public DocExchangeDefinition Selection
		{
			get
			{
				if (this.listView.SelectedItems.Count == 1)
				{
					return (DocExchangeDefinition)this.listView.SelectedItems[0].Tag;
				}

				return null;
			}
		}

		private void listView_ItemActivate(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
