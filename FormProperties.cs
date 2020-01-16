// Name:        FormProperties.cs
// Description: Dialog for editing documentation, templates, concepts, and exchanges.
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
using IfcDoc.Format.PNG;

namespace IfcDoc
{
	public partial class FormProperties : Form
	{
		public FormProperties()
		{
			InitializeComponent();
		}

		//public FormProperties(DocObject docObject, DocObject docParent, DocProject docProject) : this()
		public FormProperties(DocObject[] path, DocProject docProject)
			: this()
		{
			this.ctlProperties.Init(path, docProject);
		}

		private void ctlProperties_Load(object sender, EventArgs e)
		{

		}

	}
}
