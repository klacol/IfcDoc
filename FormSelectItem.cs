// Name:        FormSelectItem.cs
// Description: Dialog box for selecting object within collection
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
	public partial class FormSelectItem : Form
	{
		public FormSelectItem()
		{
			InitializeComponent();
		}

		public string Item
		{
			get
			{
				return this.textBoxItem.Text;
			}
			set
			{
				this.textBoxItem.Text = value;
			}
		}
	}
}
