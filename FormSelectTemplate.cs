// Name:        FormSelectTemplate.cs
// Description: Dialog box for selecting template
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

namespace IfcDoc
{
	public partial class FormSelectTemplate : Form
	{
		DocTemplateDefinition m_selection;
		DocProject m_project;
		DocEntity m_entity;

		Dictionary<string, DocObject> m_map;

		public FormSelectTemplate()
		{
			InitializeComponent();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="selection">Selected template.</param>
		/// <param name="project">Projet containing templates.</param>
		/// <param name="entity">The entity for which templates are filtered.</param>
		public FormSelectTemplate(DocTemplateDefinition selection, DocProject project, DocEntity entity)
			: this()
		{
			this.m_selection = selection;
			this.m_project = project;
			this.m_entity = entity;

			// build map
			this.m_map = new Dictionary<string, DocObject>();
			foreach (DocSection docSection in this.m_project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEntity in docSchema.Entities)
					{
						if (!this.m_map.ContainsKey(docEntity.Name))
						{
							m_map.Add(docEntity.Name, docEntity);
						}
					}

					foreach (DocType docType in docSchema.Types)
					{
						try
						{
							m_map.Add(docType.Name, docType);
						}
						catch
						{

						}
					}
				}
			}

			LoadTemplates(null, project.Templates);
		}

		private void LoadTemplates(TreeNode tnParent, List<DocTemplateDefinition> list)
		{
			foreach (DocTemplateDefinition docTemplate in list)
			{
#if true
				bool include = false;

				if (docTemplate.Name.Contains("Spatial"))
				{
					this.ToString();
				}

				// check for inheritance                
				DocObject docApplicableEntity = null;
				if (this.m_entity == null || String.IsNullOrEmpty(docTemplate.Type))
				{
					include = true;
				}
				else if (docTemplate.Type != null && m_map.TryGetValue(docTemplate.Type, out docApplicableEntity) && docApplicableEntity is DocEntity)
				{
					// check for inheritance
					DocEntity docBase = this.m_entity;
					while (docBase != null)
					{
						if (docBase == docApplicableEntity)
						{
							include = true;
							break;
						}

						if (docBase.BaseDefinition == null)
							break;

						DocObject docEach = null;
						if (this.m_map.TryGetValue(docBase.BaseDefinition, out docEach))
						{
							docBase = (DocEntity)docEach;
						}
						else
						{
							docBase = null;
						}
					}
				}

				if (include)
				{
					LoadTemplate(tnParent, docTemplate);
				}

				//else if (docTemplate.Type == null)
				//{
				// recurse
				//    LoadTemplates(tnParent, docTemplate.Templates);
				//}

#else
                LoadTemplate(null, docTemplate);
#endif
			}
		}

		private void LoadTemplate(TreeNode tnParent, DocTemplateDefinition template)
		{
			// add entity
			TreeNode tn = new TreeNode();
			tn.Tag = template;
			tn.Text = template.Name;

			if (tnParent != null)
			{
				tnParent.Nodes.Add(tn);
			}
			else
			{
				this.treeView.Nodes.Add(tn);
			}

			if (this.m_selection == template)
			{
				this.treeView.SelectedNode = tn;
			}

			this.LoadTemplates(tn, template.Templates);
			/*
            foreach (DocTemplateDefinition sub in template.Templates)
            {
                LoadTemplate(tn, sub);
            }*/
		}

		public DocTemplateDefinition SelectedTemplate
		{
			get
			{
				if (this.treeView.SelectedNode == null)
					return null;

				return this.treeView.SelectedNode.Tag as DocTemplateDefinition;
			}
		}

		private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			DocTemplateDefinition dtd = (DocTemplateDefinition)this.treeView.SelectedNode.Tag;
			this.ctlConcept.Project = this.m_project;
			this.ctlConcept.Template = dtd;
			this.ctlConcept.Enabled = true;
			SetContent(dtd);
		}

		private void ctlConcept_SelectionChanged(object sender, EventArgs e)
		{
			if (this.ctlConcept.CurrentAttribute != null)
			{
				SetContent(this.ctlConcept.CurrentAttribute);
			}
			else if (this.ctlConcept.Selection is DocModelRuleEntity)
			{
				DocModelRuleEntity dmr = (DocModelRuleEntity)this.ctlConcept.Selection;
				DocObject obj = null;
				this.m_map.TryGetValue(dmr.Name, out obj);
				this.SetContent(obj);
			}
			else
			{
				DocTemplateDefinition dtd = (DocTemplateDefinition)this.treeView.SelectedNode.Tag;
				this.SetContent(dtd);
			}
		}

		private void SetContent(DocObject obj)
		{
			string s = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\content\\ifc-styles.css";
			string header = "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><link rel=\"stylesheet\" type=\"text/css\" href=\"" + s + "\"></head><body>";
			string footer = "</body></html>";
			this.webBrowser.Navigate("about:blank");
			if (obj != null)
			{
				this.webBrowser.DocumentText = header + obj.Documentation + footer;
			}
			else
			{
				this.webBrowser.DocumentText = String.Empty;
			}
		}
	}
}
