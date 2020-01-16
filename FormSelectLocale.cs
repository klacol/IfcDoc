// Name:        FormSelectLocale.cs
// Description: Dialog for selecting a language for documentation translation.
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

using System.Globalization;

namespace IfcDoc
{
	public partial class FormSelectLocale : Form
	{
		public FormSelectLocale()
		{
			InitializeComponent();

			CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
			CultureInfo[] specific = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
			foreach (CultureInfo item in cultures)
			{
				TreeNode lvi = new TreeNode();
				lvi.Tag = item;
				lvi.Text = item.Name + " - " + item.DisplayName;
				this.treeViewLocale.Nodes.Add(lvi);

				foreach (CultureInfo spec in specific)
				{
					if (spec.Parent.Equals(item))
					{
						TreeNode tnSpec = new TreeNode();
						tnSpec.Tag = spec;
						tnSpec.Text = spec.Name + " - " + spec.DisplayName;
						lvi.Nodes.Add(tnSpec);
					}
				}
			}
		}

		public CultureInfo SelectedLocale
		{
			get
			{
				if (this.treeViewLocale.SelectedNode == null)
					return null;

				return this.treeViewLocale.SelectedNode.Tag as CultureInfo;
			}
		}
	}
}
