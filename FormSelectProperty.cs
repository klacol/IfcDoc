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
	public partial class FormSelectProperty : Form
	{
		DocEntity m_entity;
		protected DocProject m_project;
		string m_portname;

		Dictionary<string, DocProperty> m_sharedproperties;

		public FormSelectProperty()
		{
			InitializeComponent();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="docEntity">If entity specified, shows property sets and properties applicable to entity; if null, shows all property sets.</param>
		/// <param name="docProject">Required project.</param>
		/// <param name="multiselect">True to select multiple properties; False to select single property; Null to show properties that can be merged.</param>
		public FormSelectProperty(DocEntity docEntity, DocProject docProject, bool? multiselect) : this()
		{
			this.m_entity = docEntity;
			this.m_project = docProject;

			if (multiselect != null)
			{
				if (multiselect == true)
				{
					this.treeViewProperty.CheckBoxes = true;
					this.Text = "Include Property Sets";

					this.comboBoxPort.Enabled = false;
				}
				else if (multiselect == false)
				{
					// find applicable ports
					this.comboBoxPort.Items.Add("(object)");
					this.comboBoxPort.SelectedIndex = 0;

					if (docEntity != null)
					{
						foreach (DocModelView docView in docProject.ModelViews)
						{
							foreach (DocConceptRoot docRoot in docView.ConceptRoots)
							{
								if (docRoot.ApplicableEntity == docEntity)
								{
									foreach (DocTemplateUsage docConcept in docRoot.Concepts)
									{
										if (docConcept.Definition != null && docConcept.Definition.Uuid == DocTemplateDefinition.guidPortNesting)
										{
											foreach (DocTemplateItem docItem in docConcept.Items)
											{
												string name = docItem.GetParameterValue("Name");
												if (name != null && !this.comboBoxPort.Items.Contains(name))
												{
													this.comboBoxPort.Items.Add(name);
												}
											}
										}
									}
								}
							}
						}
					}
				}


				this.LoadPropertySets();
			}
			else if (multiselect == null)
			{
				this.Text = "Merge Duplicate Properties";
				this.treeViewProperty.CheckBoxes = true;
				this.comboBoxPort.Enabled = false;

				// build list of shared properties

				//Dictionary<string, DocProperty> duplicateProperties = new Dictionary<string, DocProperty>();
				this.m_sharedproperties = new Dictionary<string, DocProperty>();

				foreach (DocSection docSection in this.m_project.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						foreach (DocPropertySet docPset in docSchema.PropertySets)
						{
							// first, capture all unique properties... THEN, only shared properties
							if (!docPset.IsVisible())
							{
								foreach (DocProperty docProp in docPset.Properties)
								{
									if (!this.m_sharedproperties.ContainsKey(docProp.Name))
									{
										this.m_sharedproperties.Add(docProp.Name, docProp);
									}
									/*
								else if(!duplicateProperties.ContainsKey(docProp.Name))
								{
									duplicateProperties.Add(docProp.Name, docProp);
								}*/
								}
							}
						}
					}
				}
				//this.m_sharedproperties = duplicateProperties;

				// find all duplicate properties 
				foreach (DocSection docSection in this.m_project.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						foreach (DocPropertySet docPset in docSchema.PropertySets)
						{
							if (docPset.IsVisible())
							{
								TreeNode tnPset = null;

								foreach (DocProperty docProp in docPset.Properties)
								{
									DocProperty docExist = null;
									if (this.m_sharedproperties.TryGetValue(docProp.Name, out docExist) && docExist != docProp)
									{
										if (tnPset == null)
										{
											tnPset = new TreeNode();
											tnPset.Tag = docPset;
											tnPset.Text = docPset.Name;
											tnPset.ImageIndex = 0;
											tnPset.SelectedImageIndex = 0;
											tnPset.Checked = true;
											this.treeViewProperty.Nodes.Add(tnPset);
										}

										TreeNode tnProp = new TreeNode();
										tnProp.Tag = docProp;
										tnProp.Text = docProp.Name;
										tnProp.ImageIndex = 1;
										tnProp.SelectedImageIndex = 1;
										tnProp.Checked = true;
										tnPset.Nodes.Add(tnProp);
										tnPset.Expand();
									}

								}
							}
						}
					}
				}

			}

		}

		protected virtual void LoadPropertySets()
		{
			this.treeViewProperty.Nodes.Clear();

			DocEntity docEntity = this.m_entity;
			if (this.m_portname != null)
			{
				docEntity = this.m_project.GetDefinition("IfcDistributionPort") as DocEntity;
			}

			foreach (DocSection docSection in this.m_project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocPropertySet docPset in docSchema.PropertySets)
					{
						bool include = false;
						if (docEntity != null && docPset.ApplicableType != null)
						{
							string[] parts = docPset.ApplicableType.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
							foreach (string part in parts)
							{
								DocEntity docBase = docEntity;
								while (docBase != null)
								{
									if (part.Contains(docBase.Name))
									{
										include = true;
										break;
									}

									docBase = this.m_project.GetDefinition(docBase.BaseDefinition) as DocEntity;
								}
							}
						}

						if (this.m_entity == null || include)
						{
							TreeNode tnPset = new TreeNode();
							tnPset.Tag = docPset;
							tnPset.Text = docPset.Name;
							tnPset.ImageIndex = 0;
							tnPset.SelectedImageIndex = 0;
							this.treeViewProperty.Nodes.Add(tnPset);

							// only select psets if no entity defined
							if (this.m_entity != null && !this.treeViewProperty.CheckBoxes)
							{
								foreach (DocProperty docProp in docPset.Properties)
								{
									TreeNode tnProp = new TreeNode();
									tnProp.Tag = docProp;
									tnProp.Text = docProp.Name;
									tnProp.ImageIndex = 1;
									tnProp.SelectedImageIndex = 1;
									tnPset.Nodes.Add(tnProp);

									// also add min/max if bounded
									if (docProp.PropertyType == DocPropertyTemplateTypeEnum.P_BOUNDEDVALUE)
									{
										tnProp.Nodes.Add(new TreeNode("UpperBoundValue", 1, 1));
										tnProp.Nodes.Add(new TreeNode("LowerBoundValue", 1, 1));
									}
								}
							}
						}
					}
				}
			}

			this.treeViewProperty.ExpandAll();

		}

		private void treeViewProperty_AfterSelect(object sender, TreeViewEventArgs e)
		{
			DocObject docObj = this.treeViewProperty.SelectedNode.Tag as DocObject;
			if (docObj != null)
			{
				if (docObj is DocPropertySet)
				{
					this.textBoxType.Text = ((DocPropertySet)docObj).PropertySetType;
				}
				else if (docObj is DocProperty)
				{
					DocProperty docProp = (DocProperty)docObj;
					this.textBoxType.Text = docProp.PropertyType + ": " + docProp.PrimaryDataType + " / " + (docProp.Enumeration != null ? docProp.Enumeration.Name : docProp.SecondaryDataType);
				}

				this.textBoxDescription.Text = docObj.Documentation;
			}
			else if (this.treeViewProperty.SelectedNode.Parent != null && this.treeViewProperty.SelectedNode.Parent.Tag is DocProperty)
			{
				DocProperty docProp = (DocProperty)this.treeViewProperty.SelectedNode.Parent.Tag;
				this.textBoxType.Text = docProp.PropertyType + ": " + docProp.PrimaryDataType + " / " + docProp.SecondaryDataType;
				this.textBoxDescription.Text = docProp.Documentation;
			}

			this.buttonOK.Enabled = (this.treeViewProperty.CheckBoxes || this.SelectedProperty != null || (this.m_entity == null && this.SelectedPropertySet != null));
		}

		/// <summary>
		/// Distribution port within object, or NULL if main object
		/// </summary>
		public string SelectedPort
		{
			get
			{
				return this.m_portname;
			}
		}

		/// <summary>
		/// For bounded values: UpperBoundValue, LowerBoundValue, or null.
		/// </summary>
		public string SelectedQualifier
		{
			get
			{
				if (this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Tag == null)
				{
					return this.treeViewProperty.SelectedNode.Text;
				}
				else
				{
					return null;
				}
			}
		}

		public DocProperty SelectedProperty
		{
			get
			{
				if (this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Tag is DocProperty)
				{
					return (DocProperty)this.treeViewProperty.SelectedNode.Tag;
				}
				else if (this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Parent != null &&
					this.treeViewProperty.SelectedNode.Parent.Tag is DocProperty)
				{
					return (DocProperty)this.treeViewProperty.SelectedNode.Parent.Tag;
				}


				return null;
			}
		}

		public DocPropertySet SelectedPropertySet
		{
			get
			{
				if (this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Tag is DocProperty)
				{
					return (DocPropertySet)this.treeViewProperty.SelectedNode.Parent.Tag;
				}
				else if (this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Parent != null && this.treeViewProperty.SelectedNode.Parent.Tag is DocProperty)
				{
					return (DocPropertySet)this.treeViewProperty.SelectedNode.Parent.Parent.Tag;
				}
				else if (this.m_entity == null && this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Tag is DocPropertySet)
				{
					return (DocPropertySet)this.treeViewProperty.SelectedNode.Tag;
				}

				return null;
			}
		}

		public DocPropertySet[] IncludedPropertySets
		{
			get
			{
				List<DocPropertySet> list = new List<DocPropertySet>();
				foreach (TreeNode tn in this.treeViewProperty.Nodes)
				{
					if (tn.Checked)
					{
						list.Add((DocPropertySet)tn.Tag);
					}
				}

				return list.ToArray();
			}
		}

		public IList<DocProperty> IncludedProperties
		{
			get
			{
				List<DocProperty> list = new List<DocProperty>();
				foreach (TreeNode tn in this.treeViewProperty.Nodes)
				{
					if (tn.Checked)
					{
						foreach (TreeNode tnSub in tn.Nodes)
						{
							if (tnSub.Checked)
							{
								list.Add((DocProperty)tnSub.Tag);
							}
						}
					}
				}

				return list;
			}
		}

		public Dictionary<string, DocProperty> SharedProperties
		{
			get
			{
				return this.m_sharedproperties;
			}
		}

		private void comboBoxPort_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.comboBoxPort.SelectedIndex == 0)
			{
				this.m_portname = null;
			}
			else
			{
				this.m_portname = this.comboBoxPort.SelectedItem as string;
			}
			this.LoadPropertySets();
		}

		public string GenerateValuePath()
		{
			if (this.m_entity == null)
				return @"\";

			string portprefix = String.Empty;
			if (this.SelectedPort != null)
			{
				portprefix = @".IsNestedBy[]\IfcRelNests.RelatedObjects['" + this.SelectedPort + @"']\IfcDistributionPort";
			}

			if (this.SelectedPropertySet != null && this.SelectedPropertySet.PropertySetType == "PSET_PERFORMANCEDRIVEN")
			{
				portprefix += @".HasAssignments[]\IfcRelAssignsToControl.RelatingControl\IfcPerformanceHistory";
			}

			string value = @"\" + this.m_entity.Name + portprefix;

			if (this.SelectedProperty != null)
			{
				string valueprop = "NominalValue";
				string datatype = this.SelectedProperty.PrimaryDataType;
				switch (this.SelectedProperty.PropertyType)
				{
					case DocPropertyTemplateTypeEnum.P_BOUNDEDVALUE:
						if (this.SelectedQualifier != null)
						{
							valueprop = this.SelectedQualifier;
						}
						else
						{
							valueprop = "SetPointValue";
						}
						break;

					case DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE:
						valueprop = "EnumerationValues";
						break;

					case DocPropertyTemplateTypeEnum.P_LISTVALUE:
						valueprop = "ListValues";
						break;

					case DocPropertyTemplateTypeEnum.P_REFERENCEVALUE:
						valueprop = "PropertyReference";
						datatype = "IfcIrregularTimeSeries.Values[]\\IfcIrregularTimeSeriesValue.ListValues[]\\" + this.SelectedProperty.SecondaryDataType;
						break;

						// other property types are not supported
				}

				if (this.SelectedProperty.PropertyType == DocPropertyTemplateTypeEnum.COMPLEX)
				{
					value += @".IsDefinedBy['" + this.SelectedPropertySet +
						@"']\IfcRelDefinesByProperties.RelatingPropertyDefinition\IfcPropertySet.HasProperties['" + this.SelectedProperty +
						@"']\" + this.SelectedProperty.GetEntityName();
				}
				else
				{
					value += @".IsDefinedBy['" + this.SelectedPropertySet +
						@"']\IfcRelDefinesByProperties.RelatingPropertyDefinition\IfcPropertySet.HasProperties['" + this.SelectedProperty +
						@"']\" + this.SelectedProperty.GetEntityName() + @"." + valueprop + @"\" + datatype;
				}

				// special cases
				if (this.m_entity.Name.Equals("IfcMaterial"))
				{
					value =
						@"\IfcMaterial.HasProperties['" + this.SelectedPropertySet +
						@"']\IfcMaterialProperties.Properties['" + this.SelectedProperty +
						@"']\" + this.SelectedProperty.GetEntityName() + @"." + valueprop + @"\" + datatype;
				}
			}
			else
			{
				value += @".GlobalId\IfcGloballyUniqueId";
			}

			return value;
		}
	}

	public class FormSelectGlobalProperty : FormSelectProperty
	{
		public FormSelectGlobalProperty(DocProject docProject, bool? multiselect) : base(null, docProject, multiselect) { }

		protected override void LoadPropertySets()
		{
			this.treeViewProperty.Nodes.Clear();
			List<IGrouping<char, DocProperty>> groups = m_project.Properties.GroupBy(x => char.ToLower(x.Name[0])).ToList();
			foreach (IGrouping<char, DocProperty> group in groups)
			{
				TreeNode tnChar = new TreeNode();
				tnChar.Tag = group.Key;
				tnChar.Text = group.Key.ToString();
				tnChar.ImageIndex = 0;
				tnChar.SelectedImageIndex = 0;
				this.treeViewProperty.Nodes.Add(tnChar);

				foreach (DocProperty docProp in group)
				{
					TreeNode tnProp = new TreeNode();
					tnProp.Tag = docProp;
					tnProp.Text = docProp.Name;
					tnProp.ImageIndex = 1;
					tnProp.SelectedImageIndex = 1;
					tnChar.Nodes.Add(tnProp);
				}
			}
		}
	}
}
