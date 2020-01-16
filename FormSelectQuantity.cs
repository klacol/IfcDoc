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
	public partial class FormSelectQuantity : Form
	{
		DocEntity m_entity;
		DocProject m_project;

		public FormSelectQuantity()
		{
			InitializeComponent();
		}

		public FormSelectQuantity(DocEntity docEntity, DocProject docProject, bool multiselect)
			: this()
		{
			this.m_entity = docEntity;
			this.m_project = docProject;

			if (multiselect)
			{
				this.treeViewProperty.CheckBoxes = true;
				this.Text = "Include Quantity Sets";
			}

			foreach (DocSection docSection in this.m_project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocQuantitySet docPset in docSchema.QuantitySets)
					{
						bool include = false;
						if (docPset.ApplicableType != null)
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

							if (!this.treeViewProperty.CheckBoxes)
							{
								foreach (DocQuantity docProp in docPset.Quantities)
								{
									TreeNode tnProp = new TreeNode();
									tnProp.Tag = docProp;
									tnProp.Text = docProp.Name;
									tnProp.ImageIndex = 1;
									tnProp.SelectedImageIndex = 1;
									tnPset.Nodes.Add(tnProp);
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
				if (docObj is DocQuantitySet)
				{
					this.textBoxType.Text = "QSET_OCCURRENCEDRIVEN";
				}
				else if (docObj is DocQuantity)
				{
					DocQuantity docProp = (DocQuantity)docObj;
					this.textBoxType.Text = docProp.QuantityType.ToString();
				}

				this.textBoxDescription.Text = docObj.Documentation;
			}

			this.buttonOK.Enabled = (this.treeViewProperty.CheckBoxes || (this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Tag is DocQuantity));
		}

		public DocQuantity SelectedQuantity
		{
			get
			{
				if (this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Tag is DocQuantity)
				{
					return (DocQuantity)this.treeViewProperty.SelectedNode.Tag;
				}

				return null;
			}
		}

		public DocQuantitySet SelectedQuantitySet
		{
			get
			{
				if (this.treeViewProperty.SelectedNode != null && this.treeViewProperty.SelectedNode.Tag is DocQuantity)
				{
					return (DocQuantitySet)this.treeViewProperty.SelectedNode.Parent.Tag;
				}

				return null;
			}
		}

		public DocQuantitySet[] IncludedQuantitySets
		{
			get
			{
				List<DocQuantitySet> list = new List<DocQuantitySet>();
				foreach (TreeNode tn in this.treeViewProperty.Nodes)
				{
					if (tn.Checked)
					{
						list.Add((DocQuantitySet)tn.Tag);
					}
				}

				return list.ToArray();
			}
		}


		public string GenerateValuePath()
		{
			if (this.m_entity == null)
				return @"\\";

			string suffix = null;
			switch (this.SelectedQuantity.QuantityType)
			{
				case DocQuantityTemplateTypeEnum.Q_AREA:
					suffix = @"IfcQuantityArea.AreaValue\IfcAreaMeasure";
					break;

				case DocQuantityTemplateTypeEnum.Q_COUNT:
					suffix = @"IfcQuantityCount.CountValue\IfcInteger";
					break;

				case DocQuantityTemplateTypeEnum.Q_LENGTH:
					suffix = @"IfcQuantityLength.LengthValue\IfcLengthMeasure";
					break;

				case DocQuantityTemplateTypeEnum.Q_TIME:
					suffix = @"IfcQuantityTime.TimeValue\IfcTimeMeasure";
					break;

				case DocQuantityTemplateTypeEnum.Q_VOLUME:
					suffix = @"IfcQuantityVolume.VolumeValue\IfcVolumeMeasure";
					break;

				case DocQuantityTemplateTypeEnum.Q_WEIGHT:
					suffix = @"IfcQuantityWeight.WeightValue\IfcWeightMeasure";
					break;
			}

			string value = @"\" + this.m_entity.Name + @".IsDefinedBy['" + this.SelectedQuantitySet +
				@"']\IfcRelDefinesByProperties.RelatingPropertyDefinition\IfcElementQuantity.Quantities['" + this.SelectedQuantity +
				@"']\" + suffix;

			return value;
		}
	}
}
