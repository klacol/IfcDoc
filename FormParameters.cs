using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class FormParameters : Form
	{
		DocModelRuleAttribute m_attr;

		public FormParameters()
		{
			InitializeComponent();
		}

		public DocProject Project
		{
			get
			{
				return this.ctlParameters.Project;
			}
			set
			{
				this.ctlParameters.Project = value;
			}
		}

		public DocConceptRoot ConceptRoot
		{
			get
			{
				return this.ctlParameters.ConceptRoot;
			}
			set
			{
				this.ctlParameters.ConceptRoot = value;
			}
		}

		public DocObject ConceptItem
		{
			get
			{
				return this.ctlParameters.ConceptItem;
			}
			set
			{
				this.ctlParameters.ConceptItem = value;
			}
		}

		public DocTemplateUsage ConceptLeaf
		{
			get
			{
				return this.ctlParameters.ConceptLeaf;
			}
			set
			{
				this.ctlParameters.ConceptLeaf = value;
			}
		}

		public object CurrentInstance
		{
			get
			{
				return this.ctlParameters.CurrentInstance;
			}
			set
			{
				this.ctlParameters.CurrentInstance = value;
			}
		}

		private void comboBoxTemplate_SelectedIndexChanged(object sender, EventArgs e)
		{
			DocModelRuleEntity sel = this.comboBoxTemplate.SelectedItem as DocModelRuleEntity;
			if (sel == null)
				return;

            if (sel.References.Count > 0)
            {
                DocTemplateDefinition docTemplateInner = sel.References[0];
                DocTemplateUsage docConceptInner = ((DocTemplateItem)this.ConceptItem).RegisterParameterConcept(this.ConceptAttr.Identification, docTemplateInner);
				List<DocTemplateUsage> docConcepts = ((DocTemplateItem)this.ConceptItem).RegisterParameterConcepts(docTemplateInner);
                this.ConceptLeaf = docConceptInner;
            }

			this.ctlParameters.ToolStripButtonVisibility();
        }

		public DocModelRuleAttribute ConceptAttr
		{
			get
			{
				return this.m_attr;
			}
			set
			{
				this.m_attr = value;

				this.comboBoxTemplate.Items.Clear();
				if (this.m_attr == null)
					return;

				foreach (DocModelRule rule in this.m_attr.Rules)
				{
					this.comboBoxTemplate.Items.Add(rule);
				}
				this.comboBoxTemplate.SelectedIndex = 0;

				/*
                if (dma.Rules.Count > 0 && dma.Rules[0] is DocModelRuleEntity)
                {
                    DocModelRuleEntity dme = (DocModelRuleEntity)dma.Rules[0];
                    if (dme.References.Count == 1)
                    {
                        docTemplateInner = dme.References[0];

                        if (dma.Rules.Count > 1)
                        {
                            // prompt user to select which template...
                        }
                    }
                }
                 */

			}
		}
	}
}
