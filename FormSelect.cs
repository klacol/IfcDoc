// Name:        FormSelectGeneric.cs
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

namespace IfcDoc
{
	public partial class FormSelect : Form
	{

		public FormSelect()
		{
			InitializeComponent();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (this.listView.SelectedItems.Count == 1)
			{
				this.listView.SelectedItems[0].EnsureVisible();
			}
		}
	}
}
