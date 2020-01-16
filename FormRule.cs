// Name:        FormRule.cs
// Description: Dialog box for editing rule
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
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
	public partial class FormRule : Form
	{
		DocModelRule m_rule;
		DocProject m_project;
		DocTemplateDefinition m_template;

		public FormRule()
		{
			InitializeComponent();
		}

		public FormRule(DocModelRule rule, DocProject project, DocTemplateDefinition template)
			: this()
		{
			this.m_rule = rule;
			this.m_project = project;
			this.m_template = template;

			this.Text = this.m_rule.Name;
			this.textBoxIdentifier.Text = this.m_rule.Identification;
			this.textBoxIdentifier.Enabled = !String.IsNullOrEmpty(this.m_rule.Identification);
			if (String.IsNullOrEmpty(this.m_rule.Identification))
			{
				this.comboBoxUsage.SelectedIndex = 0;
			}
			else if (this.m_rule.Description != null && this.m_rule.Description.Equals("*"))
			{
				// convention indicating filter
				this.comboBoxUsage.SelectedIndex = 1;
			}
			else
			{
				// indicates parameter constraint
				this.comboBoxUsage.SelectedIndex = 2;
			}

			if (rule is DocModelRuleEntity)
			{
				this.textBoxPrefix.Enabled = true;
				this.textBoxPrefix.Text = ((DocModelRuleEntity)rule).Prefix;
			}


			if (this.m_rule.CardinalityMin == 0 && this.m_rule.CardinalityMax == 0)
			{
				this.comboBoxCardinality.SelectedIndex = 0;
			}
			else if (this.m_rule.CardinalityMin == -1 && this.m_rule.CardinalityMax == -1)
			{
				this.comboBoxCardinality.SelectedIndex = 1;
			}
			else if (this.m_rule.CardinalityMin == 0 && this.m_rule.CardinalityMax == 1)
			{
				this.comboBoxCardinality.SelectedIndex = 2;
			}
			else if (this.m_rule.CardinalityMin == 1 && this.m_rule.CardinalityMax == 1)
			{
				this.comboBoxCardinality.SelectedIndex = 3;
			}
			else if (this.m_rule.CardinalityMin == 1 && this.m_rule.CardinalityMax == -1)
			{
				this.comboBoxCardinality.SelectedIndex = 4;
			}
		}

		private void comboBoxCardinality_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (this.comboBoxCardinality.SelectedIndex)
			{
				case 0:
					this.m_rule.CardinalityMin = 0;
					this.m_rule.CardinalityMax = 0; // same as unitialized file
					break;

				case 1:
					this.m_rule.CardinalityMin = -1; // means 0:0
					this.m_rule.CardinalityMax = -1;
					break;

				case 2:
					this.m_rule.CardinalityMin = 0;
					this.m_rule.CardinalityMax = 1;
					break;

				case 3:
					this.m_rule.CardinalityMin = 1;
					this.m_rule.CardinalityMax = 1;
					break;

				case 4:
					this.m_rule.CardinalityMin = 1;
					this.m_rule.CardinalityMax = -1;
					break;
			}
		}

		private void comboBoxUsage_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.textBoxIdentifier.Enabled = (this.comboBoxUsage.SelectedIndex > 0);
			switch (this.comboBoxUsage.SelectedIndex)
			{
				case 0:
					this.textBoxIdentifier.Text = "";
					this.m_rule.Identification = null;
					this.m_rule.Description = null;
					break;

				case 1:
					this.m_rule.Description = "*";
					break;

				case 2:
					this.m_rule.Description = null;
					break;
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			// update
			if (this.m_rule.Identification != this.textBoxIdentifier.Text)
			{
				this.m_rule.RenameParameter(this.textBoxIdentifier.Text, this.m_project, this.m_template);
			}

			if (this.m_rule is DocModelRuleEntity)
			{
				((DocModelRuleEntity)this.m_rule).Prefix = this.textBoxPrefix.Text;
			}

			this.Close();
		}


	}
}
