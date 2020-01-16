using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;
using IfcDoc.Format.PNG;

namespace IfcDoc
{
	public partial class CtlProperties : UserControl
	{
		public CtlProperties()
		{
			InitializeComponent();
		}

		DocObject[] m_path;
		DocObject m_target;
		DocObject m_parent; // parent object, used for filtering for templates
		DocProject m_project;
		Dictionary<string, DocObject> m_map;
		bool m_loadreq; // suppress updates of requirements while loading
		bool m_editcon; // suppress updates of concepts while moving or deleting
		bool m_loadagg;
		bool m_loadall;
		object m_instance; // optional instance to highlight

		public event EventHandler Navigate;
		public event EventHandler RuleSelectionChanged;
		public event EventHandler RuleContentChanged;
		public event EventHandler SchemaChanged; // regen EXPRESS-G diagram

		//public FormProperties(DocObject docObject, DocObject docParent, DocProject docProject) : this()
		public void Init(DocObject[] path, DocProject docProject)
		{
			TabPage tabpageExist = this.tabControl.SelectedTab;

			this.tabControl.TabPages.Clear();
			this.panelIdentityIcon.Visible = false;

			this.m_path = path;
			if (this.m_path == null)
			{
				return;
			}

			this.m_loadall = true;
			try
			{

				this.m_target = path[path.Length - 1];
				if (path.Length > 1)
				{
					this.m_parent = path[path.Length - 2];
				}
				this.m_project = docProject;
				this.m_map = new Dictionary<string, DocObject>();

				DocObject docObject = this.m_target;

				this.toolStripButtonTranslationRemove.Enabled = false;

				// build map
				foreach (DocSection docSection in this.m_project.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						foreach (DocEntity docEntity in docSchema.Entities)
						{
							if (docEntity.Name != null && !this.m_map.ContainsKey(docEntity.Name))
							{
								this.m_map.Add(docEntity.Name, docEntity);
							}
						}

						foreach (DocType docType in docSchema.Types)
						{
							if (!this.m_map.ContainsKey(docType.Name))
							{
								this.m_map.Add(docType.Name, docType);
							}
						}
					}
				}

				// General pages applies to all definitions
				this.tabControl.TabPages.Add(this.tabPageGeneral);

				this.textBoxGeneralName.Enabled = false;
				this.textBoxGeneralName.Text = docObject.Name;
				this.textBoxGeneralDescription.Text = docObject.Documentation;

				this.listViewLocale.Items.Clear();
				foreach (DocLocalization docLocal in docObject.Localization)
				{
					ListViewItem lvi = new ListViewItem();
					lvi.Tag = docLocal;
					lvi.Text = docLocal.Locale;
					lvi.SubItems.Add(docLocal.Name);
					lvi.SubItems.Add(docLocal.Documentation);
					lvi.SubItems.Add(docLocal.URL);
					this.listViewLocale.Items.Add(lvi);
				}

				this.tabControl.TabPages.Add(this.tabPageIdentity);
				this.textBoxIdentityUuid.Text = docObject.Uuid.ToString();
				this.textBoxIdentityCode.Text = docObject.Code;
				this.textBoxIdentityVersion.Text = docObject.Version;
				this.comboBoxIdentityStatus.Text = docObject.Status;
				this.textBoxIdentityAuthor.Text = docObject.Author;
				this.textBoxIdentityOwner.Text = docObject.Owner;
				this.textBoxIdentityCopyright.Text = docObject.Copyright;

				if (docObject is DocModelView)
				{
					this.tabControl.TabPages.Add(this.tabPageView);

					DocModelView docView = (DocModelView)docObject;
					this.checkBoxViewIncludeAll.Checked = docView.IncludeAllDefinitions;
					this.textBoxViewRoot.Text = docView.RootEntity;

					if (docView.BaseView != null)
					{
						this.textBoxViewBase.Text = docView.BaseView;
						try
						{
							Guid guidView = new Guid(docView.BaseView);
							DocModelView docViewBase = this.m_project.GetView(guidView);
							if (docViewBase != null)
							{
								this.textBoxViewBase.Text = docViewBase.Name;
							}
						}
						catch
						{
						}
					}
					else
					{
						this.textBoxViewBase.Text = string.Empty;
					}

					this.textBoxViewXsdNamespace.Text = docView.XsdUri;
					if (docView.XsdFormats != null)
					{
						foreach (DocXsdFormat docFormat in docView.XsdFormats)
						{
							ListViewItem lvi = new ListViewItem();
							lvi.Tag = docFormat;
							lvi.Text = docFormat.Entity;
							lvi.SubItems.Add(docFormat.Attribute);
							lvi.SubItems.Add(docFormat.XsdFormat.ToString());
							lvi.SubItems.Add(docFormat.XsdTagless.ToString());

							this.listViewViewXsd.Items.Add(lvi);
						}
					}

					this.panelIdentityIcon.Visible = true;
					if (docView.Icon != null)
					{
						try
						{
							this.panelIcon.BackgroundImage = Image.FromStream(new System.IO.MemoryStream(docView.Icon));
						}
						catch
						{
						}
					}
					else
					{
						this.panelIcon.BackgroundImage = null;
					}

				}
				else if (docObject is DocExchangeDefinition)
				{
					this.tabControl.TabPages.Add(this.tabPageExchange);

					DocExchangeDefinition docExchange = (DocExchangeDefinition)docObject;
					this.checkBoxExchangeImport.Checked = ((docExchange.Applicability & DocExchangeApplicabilityEnum.Import) != 0);
					this.checkBoxExchangeExport.Checked = ((docExchange.Applicability & DocExchangeApplicabilityEnum.Export) != 0);

					this.panelIdentityIcon.Visible = true;
					if (docExchange.Icon != null)
					{
						try
						{
							this.panelIcon.BackgroundImage = Image.FromStream(new System.IO.MemoryStream(docExchange.Icon));
						}
						catch
						{
						}
					}
					else
					{
						this.panelIcon.BackgroundImage = null;
					}

					this.comboBoxExchangeClassProcess.Text = docExchange.ExchangeClass;
					this.comboBoxExchangeClassSender.Text = docExchange.SenderClass;
					this.comboBoxExchangeClassReceiver.Text = docExchange.ReceiverClass;
				}
				else if (docObject is DocTemplateDefinition)
				{
					this.tabControl.TabPages.Add(this.tabPageQuery);
					DocTemplateDefinition docTemplate = (DocTemplateDefinition)docObject;

					this.tabControl.TabPages.Add(this.tabPageConstraints);

					this.tabControl.TabPages.Add(this.tabPageUsage);
					this.listViewUsage.Items.Clear();

					// usage from other templates
					foreach (DocTemplateDefinition docTemp in this.m_project.Templates)
					{
						InitUsageFromTemplate(docTemp, docTemplate);
					}

					// usage from model views
					foreach (DocModelView docView in this.m_project.ModelViews)
					{
						foreach (DocConceptRoot docRoot in docView.ConceptRoots)
						{
							foreach (DocTemplateUsage docUsage in docRoot.Concepts)
							{
								if (docUsage.Definition == docTemplate)
								{
									DocObject[] usagepath = new DocObject[] { docRoot.ApplicableEntity, docRoot, docUsage };

									ListViewItem lvi = new ListViewItem();
									lvi.Tag = usagepath;
									lvi.Text = docView.Name;
									lvi.SubItems.Add(docRoot.ApplicableEntity.Name);
									this.listViewUsage.Items.Add(lvi);
								}
							}
						}
					}

					this.ctlRules.Project = this.m_project;
					this.ctlRules.BaseTemplate = this.m_parent as DocTemplateDefinition;
					this.ctlRules.Template = docTemplate;

					this.ctlOperators.Project = this.m_project;
					this.ctlOperators.Template = docTemplate;
					this.ctlOperators.Rule = null;
				}
				else if (docObject is DocConceptRoot)
				{
					DocConceptRoot docRoot = (DocConceptRoot)docObject;

					// V12: show template for item to allow one-off editing
					this.tabControl.TabPages.Add(this.tabPageQuery);
					this.ctlRules.Project = this.m_project;
					this.ctlRules.ConceptRoot = docRoot;
					this.ctlRules.Concept = null;
					this.ctlRules.BaseTemplate = null;
					this.ctlRules.Template = docRoot.ApplicableTemplate;

					this.tabControl.TabPages.Add(this.tabPageConstraints);
					this.ctlOperators.Project = this.m_project;
					this.ctlOperators.Template = docRoot.ApplicableTemplate;
					this.ctlOperators.Rule = null;


					this.tabControl.TabPages.Add(this.tabPageConcept); // note: while possible, not used
					this.tabControl.TabPages.Add(this.tabPageConceptRoot);

					this.ctlParameters.Project = this.m_project;
					this.ctlParameters.ConceptRoot = docRoot;
					this.ctlParameters.ConceptItem = this.ctlParameters.ConceptRoot;
					this.ctlParameters.ConceptLeaf = null;


					DocEntity docEntity = docRoot.ApplicableEntity;

					DocModelView docView = path[path.Length - 2] as DocModelView;
					DocModelView[] listViews = docProject.GetViewInheritance(docView); ;


					// find all inherited concepts
					List<DocTemplateDefinition> listTemplate = new List<DocTemplateDefinition>();
					Dictionary<DocTemplateDefinition, DocEntity> mapTemplate = new Dictionary<DocTemplateDefinition, DocEntity>();
					Dictionary<DocTemplateDefinition, DocModelView> mapView = new Dictionary<DocTemplateDefinition, DocModelView>();
					while (docEntity != null)
					{
						foreach (DocModelView docSuperView in listViews)
						{
							foreach (DocConceptRoot docRootEach in docSuperView.ConceptRoots)
							{
								if (docRootEach.ApplicableEntity == docEntity)
								{
									foreach (DocTemplateUsage docConcept in docRootEach.Concepts)
									{
										if (docConcept.Definition != null)
										{
											if (listTemplate.Contains(docConcept.Definition))
											{
												listTemplate.Remove(docConcept.Definition);
											}
											listTemplate.Insert(0, docConcept.Definition);
											mapTemplate[docConcept.Definition] = docEntity;
											mapView[docConcept.Definition] = docSuperView;
										}
									}
								}
							}
						}

						// recurse upwards
						docEntity = this.m_project.GetDefinition(docEntity.BaseDefinition) as DocEntity;
					}

					this.listViewConceptRoot.Items.Clear();
					foreach (DocTemplateDefinition dtd in listTemplate)
					{
						ListViewItem lvi = new ListViewItem();
						lvi.Tag = dtd;
						lvi.Text = dtd.Name;

						DocEntity docTemplateEntity = mapTemplate[dtd];
						lvi.SubItems.Add(docTemplateEntity.Name);

						DocModelView docTemplateView = mapView[dtd];
						lvi.SubItems.Add(docTemplateView.Name);

						// find local override if any
						lvi.ImageIndex = 3;
						foreach (DocTemplateUsage docConcept in docRoot.Concepts)
						{
							if (docConcept.Definition == dtd)
							{
								UpdateConceptInheritance(lvi, docConcept);
								break;
							}
						}

						this.listViewConceptRoot.Items.Add(lvi);
					}
				}
				else if (docObject is DocTemplateUsage)
				{
					DocTemplateUsage docUsage = (DocTemplateUsage)docObject;
					DocModelView docView = (DocModelView)this.m_path[this.m_path.Length - 3];
					DocConceptRoot docRoot = (DocConceptRoot)this.m_path[this.m_path.Length - 2];


					// V12: show template for item to allow one-off editing
					this.tabControl.TabPages.Add(this.tabPageQuery);
					this.ctlRules.Project = this.m_project;
					this.ctlRules.ConceptRoot = docRoot;
					this.ctlRules.Concept = docUsage;
					this.ctlRules.BaseTemplate = null;
					this.ctlRules.Template = docUsage.Definition;

					this.tabControl.TabPages.Add(this.tabPageConstraints);
					this.ctlOperators.Project = this.m_project;
					this.ctlOperators.Template = docUsage.Definition;
					this.ctlOperators.Rule = null;

					this.tabControl.TabPages.Add(this.tabPageConcept);
					this.tabControl.TabPages.Add(this.tabPageRequirements);

					this.ctlParameters.Project = this.m_project;
					this.ctlParameters.ConceptRoot = this.m_path[this.m_path.Length - 2] as DocConceptRoot;
					this.ctlParameters.ConceptItem = this.ctlParameters.ConceptRoot;
					this.ctlParameters.ConceptLeaf = docUsage;

					this.listViewExchange.Items.Clear();

					if (this.m_path.Length < 3)
						return; // should not occur -- View | Root | Concept; potentially multiple views nested

					// find the view
					if (docView == null)
						return;

					foreach (DocExchangeDefinition docExchange in docView.Exchanges)
					{
						DocExchangeRequirementEnum reqImport = DocExchangeRequirementEnum.NotRelevant;
						DocExchangeRequirementEnum reqExport = DocExchangeRequirementEnum.NotRelevant;

						// determine import/export support
						foreach (DocExchangeItem docExchangeItem in docUsage.Exchanges)
						{
							if (docExchangeItem.Exchange == docExchange)
							{
								if (docExchangeItem.Applicability == DocExchangeApplicabilityEnum.Import)
								{
									reqImport = docExchangeItem.Requirement;
								}
								else if (docExchangeItem.Applicability == DocExchangeApplicabilityEnum.Export)
								{
									reqExport = docExchangeItem.Requirement;
								}
							}
						}

						ListViewItem lvi = new ListViewItem();
						lvi.Tag = docExchange;
						lvi.Text = docExchange.Name;
						lvi.SubItems.Add(reqImport.ToString());
						lvi.SubItems.Add(reqExport.ToString());
						this.listViewExchange.Items.Add(lvi);
					}

					this.InitExchangeRequirements();
				}
				else if (docObject is DocSchema)
				{
					DocSchema docSchema = (DocSchema)docObject;
				}
				else if (docObject is DocEntity)
				{
					DocEntity docEntity = (DocEntity)docObject;

					this.tabControl.TabPages.Add(this.tabPageEntity);
					this.textBoxEntityBase.Text = docEntity.BaseDefinition;
					this.checkBoxEntityAbstract.Checked = docEntity.IsAbstract;
				}
				else if (docObject is DocDefined)
				{
					DocDefined docDefined = (DocDefined)docObject;

					this.textBoxAttributeType.Text = docDefined.DefinedType;

					this.tabControl.TabPages.Add(this.tabPageAttribute);

					this.labelAttributeInverse.Visible = false;
					this.textBoxAttributeInverse.Visible = false;
					this.buttonAttributeInverse.Visible = false;

					this.checkBoxAttributeOptional.Visible = false;
					this.checkBoxXsdTagless.Visible = false;
					this.labelAttributeXsdFormat.Visible = false;
					this.comboBoxAttributeXsdFormat.Visible = false;

					this.LoadAttributeCardinality();
				}
				else if (docObject is DocAttribute)
				{
					DocAttribute docAttribute = (DocAttribute)docObject;

					this.tabControl.TabPages.Add(this.tabPageAttribute);
					this.textBoxAttributeType.Text = docAttribute.DefinedType;
					this.textBoxAttributeInverse.Text = docAttribute.Inverse;
					this.textBoxAttributeDerived.Text = docAttribute.Derived;

					this.checkBoxAttributeOptional.Checked = docAttribute.IsOptional;
					if (docAttribute.XsdTagless != null)
					{
						if (docAttribute.XsdTagless == true)
						{
							this.checkBoxXsdTagless.CheckState = CheckState.Checked;
						}
						else
						{
							this.checkBoxXsdTagless.CheckState = CheckState.Unchecked;
						}
					}
					else
					{
						this.checkBoxXsdTagless.CheckState = CheckState.Indeterminate;
					}
					this.comboBoxAttributeXsdFormat.SelectedItem = docAttribute.XsdFormat.ToString();

					this.LoadAttributeCardinality();
				}
				else if (docObject is DocConstraint)
				{
					DocConstraint docConstraint = (DocConstraint)docObject;

					this.tabControl.TabPages.Add(this.tabPageExpression);
					this.textBoxExpression.Text = docConstraint.Expression;
				}
				else if (docObject is DocPropertySet)
				{
					this.tabControl.TabPages.Add(this.tabPagePropertySet);

					DocPropertySet docPset = (DocPropertySet)docObject;
					this.LoadApplicability();

					this.comboBoxPsetType.Text = docPset.PropertySetType;
				}
				else if (docObject is DocProperty)
				{
					this.tabControl.TabPages.Add(this.tabPageProperty);

					DocProperty docProp = (DocProperty)docObject;

					// backward compatibility:
					string secondary = docProp.SecondaryDataType;
					if (!String.IsNullOrEmpty(secondary))
					{
						string[] enumhost = secondary.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
						if (enumhost.Length == 2)
						{
							secondary = enumhost[0];
							docProp.SecondaryDataType = secondary; // update data underneath for future consistency
						}
					}

					this.comboBoxPropertyAccess.Text = docProp.AccessState.ToString();
					this.comboBoxPropertyType.Text = docProp.PropertyType.ToString();

					this.LoadPropertyType();
				}
				else if (docObject is DocQuantitySet)
				{
					this.tabControl.TabPages.Add(this.tabPagePropertySet);
					this.LoadApplicability();
					this.comboBoxPsetType.Enabled = false;
				}
				else if (docObject is DocQuantity)
				{
					this.tabControl.TabPages.Add(this.tabPageQuantity);

					DocQuantity docProp = (DocQuantity)docObject;
					this.comboBoxQuantityType.Text = docProp.QuantityType.ToString();
					this.comboBoxQuantityAccess.Text = docProp.AccessState.ToString();
				}
				else if (docObject is DocExample)
				{
					this.tabControl.TabPages.Add(this.tabPageExample); // 
					this.tabControl.TabPages.Add(this.tabPageViews); // applicable views
					this.tabControl.TabPages.Add(this.tabPagePropertySet); // applicable entities
					this.LoadApplicability();
					this.comboBoxPsetType.Enabled = false;
					this.buttonApplicabilityAddTemplate.Visible = true;

					DocExample docExample = (DocExample)docObject;
					if (docExample.File != null)
					{
						this.textBoxExample.Text = Encoding.ASCII.GetString(docExample.File);
						this.toolStripButtonExampleClear.Enabled = true;
						this.textBoxExample.ReadOnly = false;
						this.textBoxExample.Focus();
					}
					else if (docExample.Path != null)
					{
						this.textBoxExample.Text = docExample.Path;
						this.toolStripButtonExampleClear.Enabled = true;
						this.textBoxExample.ReadOnly = true;
						this.textBoxExample.Focus();
					}
					else
					{
						this.textBoxExample.Text = String.Empty;
						this.toolStripButtonExampleClear.Enabled = false;
						this.textBoxExample.ReadOnly = true;
					}

					this.LoadReferencedViews();
				}
				else if (docObject is DocPublication)
				{
					DocPublication docPublication = (DocPublication)docObject;

					this.tabControl.TabPages.Add(this.tabPagePublication);
					this.tabControl.TabPages.Add(this.tabPageViews);
					this.tabControl.TabPages.Add(this.tabPageFormats);
					this.LoadReferencedViews();

					this.textBoxHeader.Text = docPublication.Header;
					this.textBoxFooter.Text = docPublication.Footer;
					this.checkBoxPublishHideHistory.Checked = docPublication.HideHistory;
					this.checkBoxPublishISO.Checked = docPublication.ISO;
					this.checkBoxPublishUML.Checked = docPublication.UML;
					this.checkBoxPublishBSI.Checked = docPublication.ReportIssues;
					this.checkBoxPublishExchangeTables.Checked = docPublication.Exchanges;
					this.checkBoxPublishHtmlExamples.Checked = docPublication.HtmlExamples;

					this.listViewFormats.Items.Clear();
					foreach (DocFormat docFormat in docPublication.Formats)
					{
						if ((int)docFormat.FormatType == 0)
						{
							docFormat.FormatType = DocFormatSchemaEnum.OWL; // fixup rename that broke files
						}

						ListViewItem lvi = new ListViewItem();
						lvi.ImageIndex = 0;
						lvi.Tag = docFormat;
						lvi.Text = docFormat.FormatType.ToString();
						lvi.SubItems.Add(docFormat.FormatOptions.ToString());
						this.listViewFormats.Items.Add(lvi);
					}


					UpdateFormatOption();
				}
				else if (docObject is DocChangeAction)
				{
					this.tabControl.TabPages.Add(this.tabPageChange);
					DocChangeAction docChange = (DocChangeAction)docObject;
					this.toolStripButtonChangeSPF.Checked = docChange.ImpactSPF;
					this.toolStripButtonChangeXML.Checked = docChange.ImpactXML;

					switch (docChange.Action)
					{
						case DocChangeActionEnum.NOCHANGE:
							this.toolStripComboBoxChange.SelectedIndex = 0;
							break;

						case DocChangeActionEnum.ADDED:
							this.toolStripComboBoxChange.SelectedIndex = 1;
							break;

						case DocChangeActionEnum.DELETED:
							this.toolStripComboBoxChange.SelectedIndex = 2;
							break;

						case DocChangeActionEnum.MODIFIED:
							this.toolStripComboBoxChange.SelectedIndex = 3;
							break;

						case DocChangeActionEnum.MOVED:
							this.toolStripComboBoxChange.SelectedIndex = 4;
							break;

					}

					this.listViewChange.Items.Clear();
					foreach (DocChangeAspect docAspect in docChange.Aspects)
					{
						ListViewItem lvi = new ListViewItem();
						lvi.Text = docAspect.Aspect.ToString();
						lvi.SubItems.Add(docAspect.OldValue);
						lvi.SubItems.Add(docAspect.NewValue);
						this.listViewChange.Items.Add(lvi);
					}
				}

				if (tabpageExist != null && this.tabControl.TabPages.Contains(tabpageExist))
				{
					this.tabControl.SelectedTab = tabpageExist;
				}
			}
			finally
			{
				this.m_loadall = false;
			}
		}

		private void InitUsageFromTemplateRule(DocTemplateDefinition docTemp, DocTemplateDefinition docSource, DocModelRule docRule)
		{
			if (docRule is DocModelRuleEntity)
			{
				DocModelRuleEntity docRuleEntity = (DocModelRuleEntity)docRule;
				if (docRuleEntity.References.Contains(docSource))
				{
					DocObject[] usagepath = new DocObject[] { docTemp };

					ListViewItem lvi = new ListViewItem();
					lvi.Tag = usagepath;
					lvi.Text = "[Template]";
					lvi.SubItems.Add(docTemp.Name);
					this.listViewUsage.Items.Add(lvi);
				}
			}

			// recurse
			foreach (DocModelRule docInner in docRule.Rules)
			{
				InitUsageFromTemplateRule(docTemp, docSource, docInner);
			}
		}

		private void InitUsageFromTemplate(DocTemplateDefinition docTemp, DocTemplateDefinition docSource)
		{
			foreach (DocModelRule docRule in docTemp.Rules)
			{
				InitUsageFromTemplateRule(docTemp, docSource, docRule);
			}

			// recurse
			foreach (DocTemplateDefinition docSub in docTemp.Templates)
			{
				InitUsageFromTemplate(docSub, docSource);
			}
		}

		public object[] CurrentPopulation
		{
			get
			{
				return this.ctlOperators.CurrentPopulation;
			}
			set
			{
				this.ctlOperators.CurrentPopulation = value;
			}
		}

		public object CurrentInstance
		{
			get
			{
				return this.m_instance;
			}
			set
			{
				this.m_instance = value;
				this.ctlOperators.CurrentInstance = value;
				this.ctlParameters.CurrentInstance = value;
				this.ctlRules.CurrentInstance = value;
			}
		}

		private void LoadApplicability()
		{
			DocVariableSet dvs = (DocVariableSet)this.m_target;

			this.listViewPsetApplicability.Items.Clear();

			if (!String.IsNullOrEmpty(dvs.ApplicableType))
			{
				string[] parts = dvs.ApplicableType.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string part in parts)
				{
					string[] sub = part.Split('/');

					ListViewItem lvi = new ListViewItem();
					lvi.Tag = part;
					lvi.Text = sub[0];

					if (sub.Length > 1)
					{
						lvi.SubItems.Add(sub[1]);
					}

					this.listViewPsetApplicability.Items.Add(lvi);
				}
			}

			// templates
			if (dvs is DocExample)
			{
				DocExample dex = (DocExample)dvs;
				if (dex.ApplicableTemplates != null)
				{
					foreach (DocTemplateDefinition dtd in dex.ApplicableTemplates)
					{
						ListViewItem lvi = new ListViewItem();
						lvi.Tag = dtd;
						lvi.Text = dtd.Name;

						this.listViewPsetApplicability.Items.Add(lvi);
					}
				}
			}
		}

		private DocAttribute FindAttribute(DocEntity entity, string name)
		{
			foreach (DocAttribute eachattr in entity.Attributes)
			{
				if (eachattr.Name.Equals(name))
					return eachattr;
			}

			// recurse
			if (entity.BaseDefinition != null)
			{
				DocEntity basetype = (DocEntity)this.m_map[entity.BaseDefinition];
				return FindAttribute(basetype, name);
			}

			return null; // not found
		}

		private void UpdateTreeRule(TreeNode tnRule)
		{
			DocModelRule docRule = (DocModelRule)tnRule.Tag;
			tnRule.Text = docRule.Name;

			DocTemplateDefinition docTemplateParent = this.m_parent as DocTemplateDefinition;
			if (docTemplateParent != null)
			{
				DocModelRule[] objpath = docTemplateParent.GetRulePath(tnRule.FullPath);
				if (objpath != null && objpath[objpath.Length - 1] != null)
				{
					tnRule.ForeColor = Color.Gray;
				}
			}

			string tooltip = docRule.Name;
			// decorative text doesn't allow treeview path to work -- use tooltip in UI now instead
			//tooltip += docRule.GetCardinalityExpression();
			if (!String.IsNullOrEmpty(docRule.Identification))
			{
				tooltip += " <" + docRule.Identification + ">";
				tnRule.BackColor = Color.LightBlue; // mark parameter
			}
			else
			{
				tnRule.BackColor = Color.Empty;
			}
			tnRule.ToolTipText = tooltip;
		}

		private void listViewLocale_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.listViewLocale.SelectedItems.Count > 1)
			{
				this.textBoxGeneralName.Enabled = true;
				this.textBoxGeneralName.Text = "";
				this.textBoxGeneralDescription.Text = "";
				this.comboBoxLocaleCategory.SelectedIndex = -1;
				this.textBoxLocaleURL.Text = "";
			}
			if (this.listViewLocale.SelectedItems.Count == 1)
			{
				DocLocalization docLocal = (DocLocalization)this.listViewLocale.SelectedItems[0].Tag;
				this.textBoxGeneralName.Enabled = true;
				this.textBoxGeneralName.Text = docLocal.Name;
				this.textBoxGeneralDescription.Text = docLocal.Documentation;
				this.comboBoxLocaleCategory.SelectedIndex = (int)docLocal.Category;
				this.textBoxLocaleURL.Text = docLocal.URL;

				this.comboBoxLocaleCategory.Enabled = true;
				this.textBoxLocaleURL.Enabled = true;
			}
			else
			{
				this.textBoxGeneralName.Enabled = false;
				this.textBoxGeneralName.Text = this.m_target.Name;
				this.textBoxGeneralDescription.Text = this.m_target.Documentation;

				this.comboBoxLocaleCategory.Enabled = false;
				this.textBoxLocaleURL.Enabled = false;
				this.comboBoxLocaleCategory.SelectedIndex = -1;
				this.textBoxLocaleURL.Text = "";
			}

			this.toolStripButtonTranslationRemove.Enabled = (this.listViewLocale.SelectedItems.Count > 0);
		}

		private void textBoxGeneralName_TextChanged(object sender, EventArgs e)
		{
			if (this.m_loadall)
				return;

			if (this.listViewLocale.SelectedItems.Count > 0)
			{
				foreach (ListViewItem lvi in this.listViewLocale.SelectedItems)
				{
					DocLocalization docLocal = (DocLocalization)lvi.Tag;
					docLocal.Name = this.textBoxGeneralName.Text;

					lvi.SubItems[1].Text = this.textBoxGeneralName.Text;
				}
			}
			else
			{
				//this.m_target.Name = this.textBoxGeneralName.Text;
				//this.Text = this.m_target.Name;
			}
		}

		private void textBoxGeneralDescription_TextChanged(object sender, EventArgs e)
		{
			if (this.m_loadall)
				return;

			if (this.listViewLocale.SelectedItems.Count > 0)
			{
				foreach (ListViewItem lvi in this.listViewLocale.SelectedItems)
				{
					DocLocalization docLocal = (DocLocalization)lvi.Tag;
					docLocal.Documentation = this.textBoxGeneralDescription.Text;

					lvi.SubItems[2].Text = this.textBoxGeneralDescription.Text;
				}
			}
			else
			{
				this.m_target.Documentation = this.textBoxGeneralDescription.Text;
			}
		}

		private void buttonPsetEntity_Click(object sender, EventArgs e)
		{
			DocObject target = null;
			DocEntity entity = null;

			// get selected entity
			if (this.m_target is DocVariableSet)
			{
				DocVariableSet docTemplate = (DocVariableSet)this.m_target;
				if (docTemplate.ApplicableType != null && m_map.TryGetValue(docTemplate.ApplicableType, out target))
				{
					entity = (DocEntity)target;
				}

				using (FormSelectEntity form = new FormSelectEntity(null, entity, this.m_project, SelectDefinitionOptions.Entity | SelectDefinitionOptions.Predefined))
				{
					DialogResult res = form.ShowDialog(this);
					if (res == DialogResult.OK && form.SelectedEntity != null)
					{
						if (String.IsNullOrEmpty(docTemplate.ApplicableType))
						{
							docTemplate.ApplicableType = form.SelectedEntity.Name;
						}
						else
						{
							docTemplate.ApplicableType += "," + form.SelectedEntity.Name;
						}

						// append predefined type, if any
						if (form.SelectedConstant != null)
						{
							docTemplate.ApplicableType += "/" + form.SelectedConstant.Name;
						}

						this.LoadApplicability();
					}
				}
			}
		}

		private void comboBoxPsetType_SelectedIndexChanged(object sender, EventArgs e)
		{
			DocPropertySet docPset = (DocPropertySet)this.m_target;
			docPset.PropertySetType = this.comboBoxPsetType.Text;

			if (this.SchemaChanged != null)
			{
				// update icon in tree
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void comboBoxPsetType_TextUpdate(object sender, EventArgs e)
		{
			DocPropertySet docPset = (DocPropertySet)this.m_target;
			docPset.PropertySetType = this.comboBoxPsetType.Text;
		}

		private void LoadPropertyType()
		{
			DocProperty docProperty = (DocProperty)this.m_target;
			this.textBoxPropertyDataPrimary.Text = docProperty.PrimaryDataType;
			this.textBoxPropertyDataSecondary.Text = docProperty.SecondaryDataType;
			this.textBoxPropertyEnumeration.Text = docProperty.Enumeration == null ? "" : docProperty.Enumeration.Name;

			switch (docProperty.PropertyType)
			{
				case DocPropertyTemplateTypeEnum.P_SINGLEVALUE:
					this.buttonPropertyDataPrimary.Enabled = true;
					this.buttonPropertyDataSecondary.Enabled = false;
					this.buttonPropertyEnumeration.Enabled = false;
					break;

				case DocPropertyTemplateTypeEnum.P_BOUNDEDVALUE:
					this.buttonPropertyDataPrimary.Enabled = true;
					this.buttonPropertyDataSecondary.Enabled = false;
					this.buttonPropertyEnumeration.Enabled = false;
					break;

				case DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE:
					this.buttonPropertyDataPrimary.Enabled = false; // fixed to IfcLabel
					this.buttonPropertyDataSecondary.Enabled = false;
					this.buttonPropertyEnumeration.Enabled = true;
					break;

				case DocPropertyTemplateTypeEnum.P_LISTVALUE:
					this.buttonPropertyDataPrimary.Enabled = true;
					this.buttonPropertyDataSecondary.Enabled = false;
					this.buttonPropertyEnumeration.Enabled = false;
					break;

				case DocPropertyTemplateTypeEnum.P_TABLEVALUE:
					this.buttonPropertyDataPrimary.Enabled = true;
					this.buttonPropertyDataSecondary.Enabled = true;
					this.buttonPropertyEnumeration.Enabled = false;
					break;

				case DocPropertyTemplateTypeEnum.P_REFERENCEVALUE:
					this.buttonPropertyDataPrimary.Enabled = true;
					this.buttonPropertyDataSecondary.Enabled = true;
					this.buttonPropertyEnumeration.Enabled = false;
					break;

				case DocPropertyTemplateTypeEnum.COMPLEX:
					this.buttonPropertyDataPrimary.Enabled = false;
					this.buttonPropertyDataSecondary.Enabled = true;
					this.buttonPropertyEnumeration.Enabled = false;
					break;
			}
		}

		private void comboBoxPropertyType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.m_loadall)
				return;

			DocProperty docProperty = (DocProperty)this.m_target;

			// if old value was reference, then must reset to label
			if (docProperty.PropertyType == DocPropertyTemplateTypeEnum.P_REFERENCEVALUE ||
				String.IsNullOrEmpty(docProperty.PrimaryDataType))
			{
				docProperty.PrimaryDataType = "IfcLabel";
			}

			try
			{
				docProperty.PropertyType = (DocPropertyTemplateTypeEnum)Enum.Parse(typeof(DocPropertyTemplateTypeEnum), this.comboBoxPropertyType.SelectedItem.ToString());
			}
			catch
			{
				docProperty.PropertyType = DocPropertyTemplateTypeEnum.COMPLEX;
			}

			switch (docProperty.PropertyType)
			{
				case DocPropertyTemplateTypeEnum.P_SINGLEVALUE:
					docProperty.SecondaryDataType = String.Empty;
					docProperty.Enumeration = null;
					break;

				case DocPropertyTemplateTypeEnum.P_BOUNDEDVALUE:
					docProperty.SecondaryDataType = String.Empty;
					docProperty.Enumeration = null;
					break;

				case DocPropertyTemplateTypeEnum.P_LISTVALUE:
					docProperty.SecondaryDataType = String.Empty;
					docProperty.Enumeration = null;
					break;

				case DocPropertyTemplateTypeEnum.P_TABLEVALUE:
					if (String.IsNullOrEmpty(docProperty.SecondaryDataType))
					{
						docProperty.SecondaryDataType = "IfcReal";
					}
					docProperty.Enumeration = null;
					break;

				case DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE: // add/remove enum values
					docProperty.PrimaryDataType = "IfcLabel";
					docProperty.SecondaryDataType = String.Empty; 
					break;

				case DocPropertyTemplateTypeEnum.P_REFERENCEVALUE:
					docProperty.PrimaryDataType = "IfcTimeSeries";
					docProperty.SecondaryDataType = "IfcReal";
					docProperty.Enumeration = null;
					break;

				case DocPropertyTemplateTypeEnum.COMPLEX:
					docProperty.PrimaryDataType = String.Empty;
					docProperty.SecondaryDataType = String.Empty;
					docProperty.Enumeration = null;
					break;
			}

			// update
			this.LoadPropertyType();
		}

		private void buttonPropertyData_Click(object sender, EventArgs e)
		{
			DocProperty docTemplate = (DocProperty)this.m_target;

			string basetypename = "IfcValue";
			switch (docTemplate.PropertyType)
			{
				case DocPropertyTemplateTypeEnum.P_REFERENCEVALUE:
					basetypename = "IfcObjectReferenceSelect";
					break;
			}

			DocObject docobj = null;
			DocDefinition docEntity = null;
			if (this.m_map.TryGetValue(basetypename, out docobj))
			{
				docEntity = (DocDefinition)docobj;
			}

			// get selected entity
			DocObject target = null;
			DocDefinition entity = null;
			if (docTemplate.PrimaryDataType != null && m_map.TryGetValue(docTemplate.PrimaryDataType, out target))
			{
				entity = (DocDefinition)target;
			}

			using (FormSelectEntity form = new FormSelectEntity(docEntity, entity, this.m_project, SelectDefinitionOptions.Entity | SelectDefinitionOptions.Type))
			{
				DialogResult res = form.ShowDialog(this);
				if (res == DialogResult.OK && form.SelectedEntity != null)
				{
					docTemplate.PrimaryDataType = form.SelectedEntity.Name;
					this.textBoxPropertyDataPrimary.Text = docTemplate.PrimaryDataType;
				}
			}
		}
		private void buttonPropertyEnumeration_Click(object sender, EventArgs e)
		{
			DocProperty docTemplate = (DocProperty)this.m_target;
			DocPropertyEnumeration docEnum = docTemplate.Enumeration;

			if (docTemplate.PropertyType == DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE)
			{
				// browse for property enumeration
				using (FormSelectGeneric<DocPropertyEnumeration> form = new FormSelectGeneric<DocPropertyEnumeration>(m_project, docEnum, m_project.PropertyEnumerations))
				{
					if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
					{
						if (form.Selection != null)
						{
							docTemplate.PrimaryDataType = "IfcLabel";
							docTemplate.Enumeration = form.Selection;
							this.textBoxPropertyEnumeration.Text = form.Selection.Name;
						}
						else
						{
							docTemplate.PrimaryDataType = "IfcLabel";
							docTemplate.Enumeration = null;
							this.textBoxPropertyEnumeration.Text = "";
						}
						this.textBoxPropertyDataPrimary.Text = docTemplate.PrimaryDataType;
					}
				}
				return;
			}
		}
		private void buttonPropertyDataSecondary_Click(object sender, EventArgs e)
		{
			DocProperty docTemplate = (DocProperty)this.m_target;
			if (docTemplate.PropertyType == DocPropertyTemplateTypeEnum.COMPLEX)
			{
				using (FormSelectProperty form = new FormSelectProperty(null, this.m_project, false))
				{
					if (form.ShowDialog(this) == DialogResult.OK)
					{
						foreach (DocProperty docExistProp in docTemplate.Elements)
						{
							docExistProp.Delete();
						}
						docTemplate.Elements.Clear();
						docTemplate.SecondaryDataType = null;
						if (form.SelectedPropertySet != null)
						{
							docTemplate.SecondaryDataType = form.SelectedPropertySet.Name;
							this.textBoxPropertyDataSecondary.Text = docTemplate.SecondaryDataType;
							foreach (DocProperty docProp in form.SelectedPropertySet.Properties)
							{
								DocProperty docPropClone = (DocProperty)docProp.Clone();
								docPropClone.Uuid = Guid.NewGuid();
								docTemplate.Elements.Add(docPropClone);
							}
							//... reload tree...
						}
					}
				}
				return;
			}

			string basetypename = "IfcValue";
			DocObject docobj = null;
			DocDefinition docEntity = null;
			if (this.m_map.TryGetValue(basetypename, out docobj))
			{
				docEntity = (DocDefinition)docobj;
			}

			// get selected entity
			DocObject target = null;
			DocDefinition entity = null;
			if (docTemplate.PrimaryDataType != null && m_map.TryGetValue(docTemplate.PrimaryDataType, out target))
			{
				entity = (DocDefinition)target;
			}

			using (FormSelectEntity form = new FormSelectEntity(docEntity, entity, this.m_project, SelectDefinitionOptions.Entity | SelectDefinitionOptions.Type))
			{
				DialogResult res = form.ShowDialog(this);
				if (res == DialogResult.OK && form.SelectedEntity != null)
				{
					docTemplate.SecondaryDataType = form.SelectedEntity.Name;
					this.textBoxPropertyDataSecondary.Text = docTemplate.SecondaryDataType;
				}
			}
		}
		private void textBoxIdentityCode_TextChanged(object sender, EventArgs e)
		{
			this.m_target.Code = this.textBoxIdentityCode.Text;
		}

		private void textBoxIdentityVersion_TextChanged(object sender, EventArgs e)
		{
			this.m_target.Version = this.textBoxIdentityVersion.Text;
		}

		private void comboBoxIdentityStatus_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.m_target.Status = this.comboBoxIdentityStatus.Text;

			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void comboBoxIdentityStatus_TextChanged(object sender, EventArgs e)
		{
			this.m_target.Status = this.comboBoxIdentityStatus.Text;
		}

		private void textBoxIdentityAuthor_TextChanged(object sender, EventArgs e)
		{
			this.m_target.Author = this.textBoxIdentityAuthor.Text;
		}

		private void textBoxIdentityOwner_TextChanged(object sender, EventArgs e)
		{
			this.m_target.Owner = this.textBoxIdentityOwner.Text;
		}

		private void textBoxIdentityCopyright_TextChanged(object sender, EventArgs e)
		{
			this.m_target.Copyright = this.textBoxIdentityCopyright.Text;
		}

		private void comboBoxLocaleCategory_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!this.comboBoxLocaleCategory.Enabled)
				return;

			if (String.IsNullOrEmpty(this.comboBoxLocaleCategory.Text))
				return;

			if (this.listViewLocale.SelectedItems.Count > 0)
			{
				foreach (ListViewItem lvi in this.listViewLocale.SelectedItems)
				{
					DocLocalization docLocal = (DocLocalization)lvi.Tag;
					docLocal.Category = (DocCategoryEnum)Enum.Parse(typeof(DocCategoryEnum), this.comboBoxLocaleCategory.Text);
				}
			}
		}

		private void textBoxLocaleURL_TextChanged(object sender, EventArgs e)
		{
			if (!this.textBoxLocaleURL.Enabled)
				return;

			if (this.listViewLocale.SelectedItems.Count > 0)
			{
				foreach (ListViewItem lvi in this.listViewLocale.SelectedItems)
				{
					DocLocalization docLocal = (DocLocalization)lvi.Tag;
					docLocal.URL = this.textBoxLocaleURL.Text;
				}
			}
		}

		private void buttonAttributeType_Click(object sender, EventArgs e)
		{
			DocDefinition selection = null;
			if (this.m_target is DocAttribute)
			{
				DocAttribute docAttr = (DocAttribute)this.m_target;
				selection = this.m_project.GetDefinition(docAttr.DefinedType);
			}

			using (FormSelectEntity form = new FormSelectEntity(null, selection, this.m_project, SelectDefinitionOptions.Entity | SelectDefinitionOptions.Type))
			{
				if (form.ShowDialog(this) == DialogResult.OK && form.SelectedEntity != null)
				{
					// find existing type or reference type in schema
					DocSchema docSchema = (DocSchema)this.m_path[1];
					DocDefinition docDef = docSchema.GetDefinition(form.SelectedEntity.Name);
					if (docDef == null)
					{
						// generate link to schema
						foreach (DocSection docSection in this.m_project.Sections)
						{
							foreach (DocSchema docOtherSchema in docSection.Schemas)
							{
								docDef = docOtherSchema.GetDefinition(form.SelectedEntity.Name);
								if (docDef is DocType || docDef is DocEntity)
								{
									DocSchemaRef docSchemaReference = null;
									foreach (DocSchemaRef docSchemaRef in docSchema.SchemaRefs)
									{
										if (String.Equals(docSchemaRef.Name, docOtherSchema.Name, StringComparison.OrdinalIgnoreCase))
										{
											docSchemaReference = docSchemaRef;
											break;
										}
									}

									if (docSchemaReference == null)
									{
										docSchemaReference = new DocSchemaRef();
										docSchemaReference.Name = docOtherSchema.Name.ToUpper();
										docSchema.SchemaRefs.Add(docSchemaReference);
									}

									docDef = new DocDefinitionRef();
									docDef.Name = form.SelectedEntity.Name;
									docSchemaReference.Definitions.Add((DocDefinitionRef)docDef);

									break;
								}
							}

							if (docDef != null)
								break;
						}
					}

					if (this.m_target is DocAttribute)
					{
						DocAttribute docAttr = (DocAttribute)this.m_target;

						if (docAttr.Definition != null && docAttr.Definition.DiagramRectangle != null)
						{
							// find page target, make page reference
							if (docDef is DocDefinitionRef)
							{
								DocDefinitionRef ddr = (DocDefinitionRef)docDef;

								// find existing page target
								foreach (DocPageTarget docPageTarget in docSchema.PageTargets)
								{
									if (docPageTarget.Definition == ddr)
									{
										// found it -- make page source
										DocPageSource docPageSource = new DocPageSource();
										docPageTarget.Sources.Add(docPageSource);
										docDef = docPageSource;
										break;
									}
								}
							}

							if (docDef.DiagramRectangle == null)
							{
								docDef.DiagramRectangle = new DocRectangle();
								docDef.DiagramNumber = docAttr.Definition.DiagramNumber;
								docDef.DiagramRectangle.X = docAttr.Definition.DiagramRectangle.X;
								docDef.DiagramRectangle.Y = docAttr.Definition.DiagramRectangle.Y;
								docDef.DiagramRectangle.Width = docAttr.Definition.DiagramRectangle.Width;
								docDef.DiagramRectangle.Height = docAttr.Definition.DiagramRectangle.Height;
							}
						}

						docAttr.Definition = docDef;
						docAttr.DefinedType = form.SelectedEntity.Name;
						this.textBoxAttributeType.Text = docAttr.DefinedType;
					}
					else if (this.m_target is DocDefined)
					{
						DocDefined docDefined = (DocDefined)this.m_target;
						if (docDefined.Definition.DiagramRectangle != null)
						{
							docDef.DiagramRectangle = new DocRectangle();
							docDef.DiagramNumber = docDefined.Definition.DiagramNumber;
							docDef.DiagramRectangle.X = docDefined.Definition.DiagramRectangle.X;
							docDef.DiagramRectangle.Y = docDefined.Definition.DiagramRectangle.Y;
							docDef.DiagramRectangle.Width = docDefined.Definition.DiagramRectangle.Width;
							docDef.DiagramRectangle.Height = docDefined.Definition.DiagramRectangle.Height;
						}

						docDefined.Definition = docDef;
						docDefined.DefinedType = form.SelectedEntity.Name;
						this.textBoxAttributeType.Text = docDefined.DefinedType;
					}

					if (this.SchemaChanged != null)
					{
						this.SchemaChanged(this, EventArgs.Empty);
					}

				}
			}
		}

		private void checkBoxAttributeOptional_CheckedChanged(object sender, EventArgs e)
		{
			DocAttribute docAttr = (DocAttribute)this.m_target;
			if (this.checkBoxAttributeOptional.Checked)
			{
				docAttr.AttributeFlags |= 1;
			}
			else
			{
				docAttr.AttributeFlags &= ~1;
			}

			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void checkBoxEntityAbstract_CheckedChanged(object sender, EventArgs e)
		{
			DocEntity docAttr = (DocEntity)this.m_target;
			if (this.checkBoxEntityAbstract.Checked)
			{
				docAttr.EntityFlags &= ~0x20;
			}
			else
			{
				docAttr.EntityFlags |= 0x20;
			}

			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void textBoxExpression_TextChanged(object sender, EventArgs e)
		{
			DocConstraint docConstraint = (DocConstraint)this.m_target;
			docConstraint.Expression = this.textBoxExpression.Text;
		}

		private void comboBoxQuantityType_SelectedIndexChanged(object sender, EventArgs e)
		{
			DocQuantity docProperty = (DocQuantity)this.m_target;
			try
			{
				docProperty.QuantityType = (DocQuantityTemplateTypeEnum)Enum.Parse(typeof(DocQuantityTemplateTypeEnum), this.comboBoxQuantityType.SelectedItem.ToString());
			}
			catch
			{
				docProperty.QuantityType = DocQuantityTemplateTypeEnum.Q_COUNT;
			}
		}

		private void buttonViewBase_Click(object sender, EventArgs e)
		{
			using (FormSelectView form = new FormSelectView(this.m_project, null))
			{
				DialogResult res = form.ShowDialog();
				if (res == DialogResult.OK)
				{
					DocModelView docView = (DocModelView)this.m_target;

					if (form.Selection != null && form.Selection.Length == 1)
					{
						this.textBoxViewBase.Text = form.Selection[0].Name;
						docView.BaseView = form.Selection[0].Uuid.ToString();
					}
					else
					{
						this.textBoxViewBase.Text = String.Empty;
						docView.BaseView = null;
					}
				}
			}
		}

		private void buttonPsetApplicabilityDelete_Click(object sender, EventArgs e)
		{
			List<DocTemplateDefinition> listTemplate = null;
			if (this.m_target is DocExample)
			{
				DocExample docEx = (DocExample)this.m_target;
				listTemplate = (List<DocTemplateDefinition>)docEx.ApplicableTemplates;
				listTemplate.Clear();
			}

			// build new string
			StringBuilder sb = new StringBuilder();
			foreach (ListViewItem lvi in this.listViewPsetApplicability.Items)
			{
				if (!lvi.Selected)
				{
					if (lvi.Tag is string)
					{
						if (sb.Length > 0)
						{
							sb.Append(",");
						}

						sb.Append(lvi.Tag as string);
					}
					else if (lvi.Tag is DocTemplateDefinition)
					{
						if (listTemplate != null)
						{
							listTemplate.Add((DocTemplateDefinition)lvi.Tag);
						}
					}
				}
			}

			DocVariableSet dvs = (DocVariableSet)this.m_target;
			if (sb.Length > 0)
			{
				dvs.ApplicableType = sb.ToString();
			}
			else
			{
				dvs.ApplicableType = null;
			}

			this.LoadApplicability();
		}

		private void listViewPsetApplicability_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.buttonPsetApplicabilityDelete.Enabled = (this.listViewPsetApplicability.SelectedItems.Count > 0);
		}

		private void checkBoxExchangeImport_CheckedChanged(object sender, EventArgs e)
		{
			DocExchangeDefinition docExchange = (DocExchangeDefinition)this.m_target;
			if (this.checkBoxExchangeImport.Checked)
			{
				docExchange.Applicability |= DocExchangeApplicabilityEnum.Import;
			}
			else
			{
				docExchange.Applicability &= ~DocExchangeApplicabilityEnum.Import;
			}
		}

		private void checkBoxExchangeExport_CheckedChanged(object sender, EventArgs e)
		{
			DocExchangeDefinition docExchange = (DocExchangeDefinition)this.m_target;
			if (this.checkBoxExchangeImport.Checked)
			{
				docExchange.Applicability |= DocExchangeApplicabilityEnum.Export;
			}
			else
			{
				docExchange.Applicability &= ~DocExchangeApplicabilityEnum.Export;
			}
		}

		private void buttonExchangeIconChange_Click(object sender, EventArgs e)
		{
			DialogResult res = this.openFileDialogIcon.ShowDialog();
			if (res != System.Windows.Forms.DialogResult.OK)
				return;

			byte[] icon = null;
			try
			{
				using (System.IO.FileStream filestream = new System.IO.FileStream(this.openFileDialogIcon.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
				{
					icon = new byte[filestream.Length];
					filestream.Read(icon, 0, icon.Length);
				}

				this.panelIcon.BackgroundImage = Image.FromStream(new System.IO.MemoryStream(icon));

				if (this.m_target is DocExchangeDefinition)
				{
					((DocExchangeDefinition)this.m_target).Icon = icon;
				}
				else if (this.m_target is DocModelView)
				{
					((DocModelView)this.m_target).Icon = icon;
				}
			}
			catch (Exception x)
			{
				MessageBox.Show(this, x.Message, "Error", MessageBoxButtons.OK);
			}
		}

		private void buttonExchangeIconClear_Click(object sender, EventArgs e)
		{
			if (this.m_target is DocExchangeDefinition)
			{
				((DocExchangeDefinition)this.m_target).Icon = null;
			}
			else if (this.m_target is DocModelView)
			{
				((DocModelView)this.m_target).Icon = null;
			}

			this.panelIcon.BackgroundImage = null;
		}

		private void buttonApplicabilityAddTemplate_Click(object sender, EventArgs e)
		{
			using (FormSelectTemplate form = new FormSelectTemplate(null, this.m_project, null))
			{
				if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK && form.SelectedTemplate != null)
				{
					DocExample docExample = (DocExample)this.m_target;
					docExample.ApplicableTemplates.Add(form.SelectedTemplate);

					this.LoadApplicability();
				}
			}
		}

		private void comboBoxAttributeXsdFormat_SelectedIndexChanged(object sender, EventArgs e)
		{
			DocAttribute docAttr = (DocAttribute)this.m_target;
			if (this.comboBoxAttributeXsdFormat.SelectedItem != null)
			{
				DocXsdFormatEnum formatnew = (DocXsdFormatEnum)Enum.Parse(typeof(DocXsdFormatEnum), this.comboBoxAttributeXsdFormat.SelectedItem as string, true);
				if (docAttr.XsdFormat != formatnew)
				{
					docAttr.XsdFormat = formatnew;
				}
			}
		}

		private void checkBoxXsdTagless_CheckedChanged(object sender, EventArgs e)
		{
			DocAttribute docAttr = (DocAttribute)this.m_target;
			switch (this.checkBoxXsdTagless.CheckState)
			{
				case CheckState.Checked:
					docAttr.XsdTagless = true;
					break;

				case CheckState.Unchecked:
					docAttr.XsdTagless = false;
					break;

				case CheckState.Indeterminate:
					docAttr.XsdTagless = null;
					break;
			}
		}

		private void buttonUsageEdit_Click(object sender, EventArgs e)
		{
			if (this.Navigate != null)
			{
				this.Navigate(this, EventArgs.Empty);
			}

			/*
            using (FormProperties form = new FormProperties(path, this.m_project))
            {
                form.ShowDialog(this);
            }*/
		}

		private void buttonUsageMigrate_Click(object sender, EventArgs e)
		{
			DocTemplateDefinition docTemplate = (DocTemplateDefinition)this.m_target;
			DocEntity docEntity = (DocEntity)this.m_map[docTemplate.Type];

			using (FormSelectTemplate form = new FormSelectTemplate(docTemplate, this.m_project, null))
			{
				DialogResult res = form.ShowDialog(this);
				if (res == System.Windows.Forms.DialogResult.OK && form.SelectedTemplate != null && form.SelectedTemplate != docTemplate)
				{
					while (this.listViewUsage.SelectedItems.Count > 0)
					{
						ListViewItem lvi = this.listViewUsage.SelectedItems[0];
						DocObject[] path = (DocObject[])lvi.Tag;
						if (path.Length == 3)
						{
							DocTemplateUsage usage = (DocTemplateUsage)path[2];
							usage.Definition = form.SelectedTemplate;

							lvi.Remove();
						}
						else
						{
							return;
						}
					}
				}
			}
		}

		private void listViewUsage_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.toolStripButtonUsageNavigate.Enabled = (this.listViewUsage.SelectedItems.Count == 1);

			bool migrate = false;
			if (this.listViewUsage.SelectedItems.Count > 0)
			{
				migrate = true;
				foreach (ListViewItem lvi in this.listViewUsage.SelectedItems)
				{
					if (lvi.Text.Equals("[Template]"))
					{
						migrate = false;
					}
				}
			}
			this.toolStripButtonUsageMigrate.Enabled = migrate;
		}

		private void comboBoxExchangeClassProcess_Validated(object sender, EventArgs e)
		{
			DocExchangeDefinition docExchange = (DocExchangeDefinition)this.m_target;
			docExchange.ExchangeClass = this.comboBoxExchangeClassProcess.Text;
		}

		private void comboBoxExchangeClassSender_Validated(object sender, EventArgs e)
		{
			DocExchangeDefinition docExchange = (DocExchangeDefinition)this.m_target;
			docExchange.SenderClass = this.comboBoxExchangeClassSender.Text;
		}

		private void comboBoxExchangeClassReceiver_Validated(object sender, EventArgs e)
		{
			DocExchangeDefinition docExchange = (DocExchangeDefinition)this.m_target;
			docExchange.ReceiverClass = this.comboBoxExchangeClassReceiver.Text;
		}

		private void buttonAttributeInverse_Click(object sender, EventArgs e)
		{
			DocAttribute docAttr = (DocAttribute)this.m_target;
			DocObject docEntity = null;
			if (this.m_map.TryGetValue(docAttr.DefinedType, out docEntity) && docEntity is DocEntity)
			{
				using (FormSelectAttribute form = new FormSelectAttribute((DocEntity)docEntity, this.m_project, null, false))
				{
					if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
					{
						if (form.SelectedAttribute != null)
						{
							docAttr.Inverse = form.SelectedAttribute.Name;
							this.textBoxAttributeInverse.Text = docAttr.Inverse;
						}
						else
						{
							docAttr.Inverse = null;
							this.textBoxAttributeInverse.Text = String.Empty;
						}
					}
				}
			}
		}

		private void listViewAttributeCardinality_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.buttonAttributeAggregationRemove.Enabled = (this.listViewAttributeCardinality.Items.Count > 1);

			DocAttribute docAttr = this.GetAttributeAggregation();

			this.m_loadagg = true;
			this.comboBoxAttributeAggregation.SelectedIndex = docAttr.AggregationType;
			this.textBoxAttributeAggregationMin.Text = docAttr.AggregationLower;
			this.textBoxAttributeAggregationMax.Text = docAttr.AggregationUpper;
			this.m_loadagg = false;
		}

		private DocAttribute GetAttributeAggregation()
		{
			DocAttribute docAttr = null;
			if (this.m_target is DocAttribute)
			{
				docAttr = (DocAttribute)this.m_target;
			}
			else if (this.m_target is DocDefined)
			{
				docAttr = ((DocDefined)this.m_target).Aggregation;
			}

			if (this.listViewAttributeCardinality.SelectedItems.Count == 1)
			{
				docAttr = (DocAttribute)this.listViewAttributeCardinality.SelectedItems[0].Tag;
			}
			return docAttr;
		}

		private void LoadAttributeCardinality()
		{
			DocAttribute docAttr = null;
			if (this.m_target is DocAttribute)
			{
				docAttr = (DocAttribute)this.m_target;
			}
			else if (this.m_target is DocDefined)
			{
				docAttr = ((DocDefined)this.m_target).Aggregation;
			}

			this.m_loadagg = true;
			this.listViewAttributeCardinality.Items.Clear();
			while (docAttr != null)
			{
				ListViewItem lvi = new ListViewItem();
				lvi.Tag = docAttr;
				lvi.Text = docAttr.GetAggregation().ToString();
				lvi.SubItems.Add(docAttr.AggregationLower);
				lvi.SubItems.Add(docAttr.AggregationUpper);
				this.listViewAttributeCardinality.Items.Add(lvi);

				if (docAttr.IsUnique)
				{
					this.checkBoxAttributeUnique.Checked = true;
				}
				else
				{
					this.checkBoxAttributeUnique.Checked = false;
				}

                docAttr = docAttr.AggregationAttribute;
            }
            this.m_loadagg = false;
        }

		private void buttonAttributeAggregationInsert_Click(object sender, EventArgs e)
		{
			DocAttribute docAttr = null;
			if (this.m_target is DocAttribute)
			{
				docAttr = (DocAttribute)this.m_target;
				while (docAttr != null && docAttr.AggregationAttribute != null)
				{
					docAttr = docAttr.AggregationAttribute;
				}

				docAttr.AggregationAttribute = new DocAttribute();
			}
			else if (this.m_target is DocDefined)
			{
				docAttr = ((DocDefined)this.m_target).Aggregation;
				while (docAttr != null && docAttr.AggregationAttribute != null)
				{
					docAttr = docAttr.AggregationAttribute;
				}

				if (docAttr != null)
				{
					docAttr.AggregationAttribute = new DocAttribute();
				}
				else
				{
					((DocDefined)this.m_target).Aggregation = new DocAttribute();
				}
			}

			this.LoadAttributeCardinality();
			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void buttonAttributeAggregationRemove_Click(object sender, EventArgs e)
		{
			DocAttribute docAttr = (DocAttribute)this.m_target;
			while (docAttr.AggregationAttribute != null && docAttr.AggregationAttribute.AggregationAttribute != null)
			{
				docAttr = docAttr.AggregationAttribute;
			}

			docAttr.AggregationAttribute.Delete();
			docAttr.AggregationAttribute = null;

			this.LoadAttributeCardinality();
			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void comboBoxAttributeAggregation_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.m_loadagg)
				return;

			DocAttribute docAttr = this.GetAttributeAggregation();
			if (docAttr != null)
			{
				docAttr.AggregationType = this.comboBoxAttributeAggregation.SelectedIndex;
				this.LoadAttributeCardinality();
			}
			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void textBoxAttributeAggregationMin_TextChanged(object sender, EventArgs e)
		{
			if (this.m_loadagg)
				return;

			DocAttribute docAttr = this.GetAttributeAggregation();
			docAttr.AggregationLower = this.textBoxAttributeAggregationMin.Text;
			this.LoadAttributeCardinality();
			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void textBoxAttributeAggregationMax_TextChanged(object sender, EventArgs e)
		{
			if (this.m_loadagg)
				return;

			DocAttribute docAttr = this.GetAttributeAggregation();
			docAttr.AggregationUpper = this.textBoxAttributeAggregationMax.Text;
			this.LoadAttributeCardinality();
			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}


		private void buttonViewXsdAttribute_Click(object sender, EventArgs e)
		{
			using (FormSelectEntity form = new FormSelectEntity(null, null, this.m_project, SelectDefinitionOptions.Entity))
			{
				if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK && form.SelectedEntity is DocEntity)
				{
					using (FormSelectAttribute formAttr = new FormSelectAttribute((DocEntity)form.SelectedEntity, this.m_project, null, false))
					{
						if (formAttr.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
						{
							DocXsdFormat docFormat = new DocXsdFormat();
							docFormat.Entity = form.SelectedEntity.Name;
							docFormat.Attribute = formAttr.SelectedAttribute.Name;

							DocModelView docView = (DocModelView)this.m_target;
							docView.XsdFormats.Add(docFormat);

							ListViewItem lvi = new ListViewItem();
							lvi.Tag = docFormat;
							lvi.Text = docFormat.Entity;
							lvi.SubItems.Add(docFormat.Attribute);
							lvi.SubItems.Add(docFormat.XsdFormat.ToString());
							lvi.SubItems.Add(docFormat.XsdTagless.ToString());

							this.listViewViewXsd.Items.Add(lvi);

							lvi.Selected = true;
						}
					}
				}
			}

		}

		private void buttonViewXsdDelete_Click(object sender, EventArgs e)
		{
			this.listViewViewXsd.BeginUpdate();
			for (int i = this.listViewViewXsd.SelectedItems.Count - 1; i >= 0; i--)
			{
				DocXsdFormat docFormat = (DocXsdFormat)this.listViewViewXsd.SelectedItems[i].Tag;
				DocModelView docView = (DocModelView)this.m_target;
				docView.XsdFormats.Remove(docFormat);
				docFormat.Delete();

				this.listViewViewXsd.Items.RemoveAt(this.listViewViewXsd.SelectedIndices[i]);
			}
			this.listViewViewXsd.EndUpdate();
		}

		private void comboBoxViewXsd_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.listViewViewXsd.SelectedItems.Count != 1)
				return;

			DocXsdFormat docFormat = (DocXsdFormat)this.listViewViewXsd.SelectedItems[0].Tag;
			docFormat.XsdFormat = (DocXsdFormatEnum)Enum.Parse(typeof(DocXsdFormatEnum), this.comboBoxViewXsd.SelectedItem as string);
			this.listViewViewXsd.SelectedItems[0].SubItems[2].Text = docFormat.XsdFormat.ToString();
		}

		private void checkBoxViewXsdTagless_CheckedChanged(object sender, EventArgs e)
		{
			DocXsdFormat docFormat = (DocXsdFormat)this.listViewViewXsd.SelectedItems[0].Tag;
			switch (this.checkBoxViewXsdTagless.CheckState)
			{
				case CheckState.Checked:
					docFormat.XsdTagless = true;
					break;

				case CheckState.Unchecked:
					docFormat.XsdTagless = false;
					break;

				case CheckState.Indeterminate:
					docFormat.XsdTagless = null;
					break;
			}

			if (docFormat.XsdTagless != null)
			{
				this.listViewViewXsd.SelectedItems[0].SubItems[3].Text = docFormat.XsdTagless.ToString();
			}
			else
			{
				this.listViewViewXsd.SelectedItems[0].SubItems[3].Text = String.Empty;
			}
		}

		private void listViewViewXsd_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.buttonViewXsdDelete.Enabled = (this.listViewViewXsd.SelectedItems.Count >= 1);

			if (this.listViewViewXsd.SelectedItems.Count != 1)
			{
				this.comboBoxViewXsd.Enabled = false;
				this.comboBoxViewXsd.SelectedIndex = 0;
				this.checkBoxViewXsdTagless.Enabled = false;
				this.checkBoxViewXsdTagless.Checked = false;
				return;
			}

			this.comboBoxViewXsd.Enabled = true;
			this.checkBoxXsdTagless.Enabled = true;

			DocXsdFormat docFormat = (DocXsdFormat)this.listViewViewXsd.SelectedItems[0].Tag;
			this.comboBoxViewXsd.SelectedIndex = (int)docFormat.XsdFormat;
			if (docFormat.XsdTagless != null)
			{
				if (docFormat.XsdTagless == true)
				{
					this.checkBoxViewXsdTagless.CheckState = CheckState.Checked;
				}
				else
				{
					this.checkBoxViewXsdTagless.CheckState = CheckState.Unchecked;
				}
			}
			else
			{
				this.checkBoxViewXsdTagless.CheckState = CheckState.Indeterminate;
			}
		}

		private void buttonExampleLoad_Click(object sender, EventArgs e)
		{
			if (this.openFileDialogExample.ShowDialog() == DialogResult.OK)
			{
				DocExample docExample = (DocExample)this.m_target;
				try
				{
					using (System.IO.FileStream fs = new System.IO.FileStream(this.openFileDialogExample.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
					{
						if (fs.Length < Int32.MaxValue)
						{
							docExample.Path = null;
							docExample.File = new byte[fs.Length];
							fs.Read(docExample.File, 0, (int)fs.Length);
							this.textBoxExample.Text = Encoding.ASCII.GetString(docExample.File);
						}
						else
						{
							MessageBox.Show("File is too large. Example files must be 2 GB or less, and recommended to be far less than that for documentation usability.");
						}
					}
				}
				catch (Exception xx)
				{
					MessageBox.Show(xx.Message);
				}
			}
		}

		private void buttonExampleClear_Click(object sender, EventArgs e)
		{
			DocExample docExample = (DocExample)this.m_target;
			docExample.File = null;
			docExample.Path = null;
			this.textBoxExample.Text = String.Empty;
		}

		private void buttonEntityBase_Click(object sender, EventArgs e)
		{
			DocEntity docEntity = (DocEntity)this.m_target;
			DocObject docBase = null;
			if (docEntity.BaseDefinition != null)
			{
				this.m_map.TryGetValue(docEntity.BaseDefinition, out docBase);
			}

			using (FormSelectEntity form = new FormSelectEntity(null, (DocEntity)docBase, this.m_project, SelectDefinitionOptions.Entity))
			{
				DialogResult res = form.ShowDialog(this);
				if (res == DialogResult.OK && form.SelectedEntity != null)
				{
					//todo: circular inheritance check...

					docEntity.BaseDefinition = form.SelectedEntity.Name;
					this.textBoxEntityBase.Text = docEntity.BaseDefinition;

					// find existing type or reference type in schema
					DocSchema docSchema = (DocSchema)this.m_path[1];

					// 1. Existing entity in schema
					// 2. Existing reference in schema
					// 3. Make new reference to other schema

					DocDefinition docDef = docSchema.GetDefinition(form.SelectedEntity.Name);
					if (docDef == null)
					{
						// generate link to schema
						foreach (DocSection docSection in this.m_project.Sections)
						{
							foreach (DocSchema docOtherSchema in docSection.Schemas)
							{
								docDef = docOtherSchema.GetDefinition(form.SelectedEntity.Name);
								if (docDef is DocEntity)
								{
									DocEntity docEntityBase = (DocEntity)docDef;
									DocSchemaRef docSchemaReference = null;
									foreach (DocSchemaRef docSchemaRef in docSchema.SchemaRefs)
									{
										if (String.Equals(docSchemaRef.Name, docOtherSchema.Name, StringComparison.OrdinalIgnoreCase))
										{
											docSchemaReference = docSchemaRef;
											break;
										}
									}

									if (docSchemaReference == null)
									{
										docSchemaReference = new DocSchemaRef();
										docSchemaReference.Name = docOtherSchema.Name.ToUpper();
										docSchema.SchemaRefs.Add(docSchemaReference);
									}

									DocDefinitionRef docDefRef = new DocDefinitionRef();
									docDef = docDefRef;
									docDef.Name = form.SelectedEntity.Name;
									docSchemaReference.Definitions.Add((DocDefinitionRef)docDef);

									docDef.DiagramRectangle = new DocRectangle();
									docDef.DiagramNumber = docEntity.DiagramNumber;
									docDef.DiagramRectangle.X = docEntity.DiagramRectangle.X;
									docDef.DiagramRectangle.Y = docEntity.DiagramRectangle.Y - 200;
									docDef.DiagramRectangle.Width = docEntity.DiagramRectangle.Width;
									docDef.DiagramRectangle.Height = 100;

								}

								break;

							}

							if (docDef != null)
								break;
						}
					}

					if (docDef != null && docDef.DiagramRectangle != null)
					{
						// find page target, make page reference
						if (docDef is DocDefinitionRef)
						{
							DocDefinitionRef ddr = (DocDefinitionRef)docDef;

							// find existing page target
							foreach (DocPageTarget docPageTarget in docSchema.PageTargets)
							{
								if (docPageTarget.Definition == ddr)
								{
									// found it -- make page source
									DocPageSource docPageSource = new DocPageSource();
									docPageTarget.Sources.Add(docPageSource);
									docDef = docPageSource;
									break;
								}
							}
						}

						if (docDef.DiagramRectangle == null)
						{
							docDef.DiagramRectangle = new DocRectangle();
							docDef.DiagramNumber = docEntity.DiagramNumber;
							docDef.DiagramRectangle.X = docEntity.DiagramRectangle.X;
							docDef.DiagramRectangle.Y = docEntity.DiagramRectangle.Y - 100;
							docDef.DiagramRectangle.Width = docEntity.DiagramRectangle.Width;
							docDef.DiagramRectangle.Height = docEntity.DiagramRectangle.Height;
						}

						DocLine docLine = new DocLine();
						CtlExpressG.LayoutLine(docDef, docEntity, docLine.DiagramLine);
						docLine.Definition = docEntity;

						IDocTreeHost treeHost = docDef as IDocTreeHost;
						if(treeHost != null && treeHost.Tree != null)
						{
							treeHost.Tree.Add(docLine);
						}
					}

					if (this.SchemaChanged != null)
					{
						this.SchemaChanged(this, EventArgs.Empty);
					}
				}

			}

		}

		private void buttonEntityBaseClear_Click(object sender, EventArgs e)
		{
			DocEntity docEntity = (DocEntity)this.m_target;

			if (docEntity.BaseDefinition != null)
			{
				DocObject docBase = null;
				this.m_map.TryGetValue(docEntity.BaseDefinition, out docBase);
				if (docBase is DocEntity)
				{
					DocEntity docEntBase = (DocEntity)docBase;

					foreach (DocSubtype docSub in docEntBase.Subtypes)
					{
						if (docSub.DefinedType == docEntity.Name)
						{
							docSub.Delete();
							docEntBase.Subtypes.Remove(docSub);
							break;
						}
					}


					foreach (DocLine docLine in docEntBase.Tree)
					{
						foreach (DocLine docLineSub in docLine.Tree)
						{
							if (docLineSub.Definition == docEntity)
							{
								docLineSub.Delete();
								docLine.Tree.Remove(docLineSub);
								break;
							}
						}

						if (docLine.Definition == docEntity)
						{
							docLine.Delete();
							docEntBase.Tree.Remove(docLine);
							break;
						}
					}
				}

				DocSchema docSchema = this.m_project.GetSchemaOfDefinition(docEntity);

				foreach (DocSchemaRef docSchemaRef in docSchema.SchemaRefs)
				{
					foreach (DocDefinitionRef docEntBase in docSchemaRef.Definitions)
					{
						if (docEntBase.Tree != null)
						{
							foreach (DocLine docLine in docEntBase.Tree)
							{
								foreach (DocLine docLineSub in docLine.Tree)
								{
									if (docLineSub.Definition == docEntity)
									{
										docLineSub.Delete();
										docLine.Tree.Remove(docLineSub);
										break;
									}
								}


								if (docLine.Definition == docEntity)
								{
									docLine.Delete();
									docEntBase.Tree.Remove(docLine);
									break;
								}
							}
						}
					
					}
				}

				docEntity.BaseDefinition = null;
			}

			this.textBoxEntityBase.Text = String.Empty;
			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		public void DoInsert(ToolMode toolmode)
		{
			this.ctlRules.DoInsert();
		}

		public object SelectedRule
		{
			get
			{
				return this.ctlRules.Selection;
			}
			set
			{
				this.ctlRules.Selection = value;
				this.ctlOperators.Rule = value as DocModelRule;
			}
		}

		public DocAttribute SelectedAttribute
		{
			get
			{
				return this.ctlRules.Attribute;
			}
			set
			{
				this.ctlRules.Attribute = value;
			}
		}

		public DocObject[] SelectedUsage
		{
			get
			{
				if (this.listViewUsage.SelectedItems.Count == 0)
					return null;

				DocObject[] path = (DocObject[])this.listViewUsage.SelectedItems[0].Tag;
				return path;
			}
		}

		private void ctlRules_SelectionChanged(object sender, EventArgs e)
		{
			if (this.RuleSelectionChanged != null)
			{
				this.RuleSelectionChanged(this, e);
			}
		}

		private void ctlRules_ContentChanged(object sender, EventArgs e)
		{
			this.ctlOperators.Project = this.m_project;
			this.ctlOperators.Template = this.ctlRules.Template;
			this.ctlOperators.Rule = null;

			if (this.RuleContentChanged != null)
			{
				this.RuleContentChanged(this, e);
			}
		}

		private void toolStripButtonTranslationInsert_Click(object sender, EventArgs e)
		{
			using (FormSelectLocale form = new FormSelectLocale())
			{
				DialogResult res = form.ShowDialog(this);
				if (res == DialogResult.OK && form.SelectedLocale != null)
				{
					DocLocalization docLocal = new DocLocalization();
					docLocal.Locale = form.SelectedLocale.Name;
					this.m_target.Localization.Add(docLocal);

					ListViewItem lvi = new ListViewItem();
					lvi.Tag = docLocal;
					lvi.Text = docLocal.Locale;
					lvi.SubItems.Add("");
					lvi.SubItems.Add("");
					this.listViewLocale.Items.Add(lvi);

					this.listViewLocale.SelectedItems.Clear();
					lvi.Selected = true;
				}
			}
		}

		private void toolStripButtonTranslationRemove_Click(object sender, EventArgs e)
		{
			for (int i = this.listViewLocale.SelectedItems.Count - 1; i >= 0; i--)
			{
				ListViewItem lvi = this.listViewLocale.SelectedItems[i];
				DocLocalization docLocal = (DocLocalization)lvi.Tag;
				this.m_target.Localization.Remove(docLocal);
				docLocal.Delete();

				lvi.Remove();
			}
		}

		private void tabPageGeneral_Click(object sender, EventArgs e)
		{

		}

		private void UpdateConceptInheritance(ListViewItem lvi, DocTemplateUsage docConcept)
		{
			if (docConcept == null)
			{
				lvi.ImageIndex = 3;
			}
			else if (docConcept.Suppress)
			{
				lvi.ImageIndex = 2;
			}
			else if (docConcept.Override)
			{
				lvi.ImageIndex = 1;
			}
			else
			{
				lvi.ImageIndex = 0;
			}
		}

		private void SetConceptInheritance(bool isOverride, bool isSuppress)
		{
			DocConceptRoot docRoot = (DocConceptRoot)this.m_target;
			foreach (ListViewItem lvi in this.listViewConceptRoot.SelectedItems)
			{
				DocTemplateDefinition dtd = (DocTemplateDefinition)lvi.Tag;
				DocTemplateUsage docConcept = null;
				foreach (DocTemplateUsage docConceptEach in docRoot.Concepts)
				{
					if (docConceptEach.Definition == dtd)
					{
						docConcept = docConceptEach;
						break;
					}
				}

				if (docConcept == null)
				{
					docConcept = new DocTemplateUsage();
					docConcept.Definition = dtd;
					docRoot.Concepts.Add(docConcept);

					//... update main tree view...
				}

				docConcept.Override = isOverride;
				docConcept.Suppress = isSuppress;

				UpdateConceptInheritance(lvi, docConcept);
			}

		}

		private void toolStripMenuItemModeInherit_Click(object sender, EventArgs e)
		{
			SetConceptInheritance(false, false);
		}

		private void toolStripMenuItemModeOverride_Click(object sender, EventArgs e)
		{
			SetConceptInheritance(true, false);
		}

		private void toolStripMenuItemModeSuppress_Click(object sender, EventArgs e)
		{
			SetConceptInheritance(false, true);
		}

		private void listViewConceptRoot_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.toolStripMenuItemModeInherit.Enabled = true;
			this.toolStripMenuItemModeOverride.Enabled = true;
			this.toolStripMenuItemModeSuppress.Enabled = true;
		}

		private void listViewUsage_ItemActivate(object sender, EventArgs e)
		{
			buttonUsageEdit_Click(this.toolStripButtonUsageNavigate, EventArgs.Empty);
		}

		private void checkBoxAttributeUnique_CheckedChanged(object sender, EventArgs e)
		{
			if (this.m_loadagg)
				return;

			DocAttribute docAttr = this.GetAttributeAggregation();
			if (this.checkBoxAttributeUnique.Checked)
			{
				docAttr.AggregationFlag = 2;
			}
			else
			{
				docAttr.AggregationFlag = 0;
			}
			this.LoadAttributeCardinality();
		}

		private void textBoxAttributeDerived_TextChanged(object sender, EventArgs e)
		{
			if (this.m_loadall)
				return;

			DocAttribute docAttr = (DocAttribute)this.m_target;
			if (!String.IsNullOrEmpty(this.textBoxAttributeDerived.Text))
			{
				docAttr.Derived = this.textBoxAttributeDerived.Text;
			}
			else
			{
				docAttr.Derived = null;
			}
		}

		private void checkBoxViewIncludeAll_CheckedChanged(object sender, EventArgs e)
		{
			DocModelView docView = (DocModelView)this.m_target;
			docView.IncludeAllDefinitions = this.checkBoxViewIncludeAll.Checked;
		}

		private void buttonViewEntity_Click(object sender, EventArgs e)
		{
			DocModelView docView = (DocModelView)this.m_target;
			using (FormSelectEntity form = new FormSelectEntity(null, null, this.m_project, SelectDefinitionOptions.Entity))
			{
				if (form.ShowDialog(this) == DialogResult.OK)
				{
					if (form.SelectedEntity != null)
					{
						docView.RootEntity = form.SelectedEntity.Name;
					}
					else
					{
						docView.RootEntity = null;
					}
				}
			}

			this.textBoxViewRoot.Text = docView.RootEntity;
		}

		private void textBoxViewXsdNamespace_TextChanged(object sender, EventArgs e)
		{
			DocModelView docView = (DocModelView)this.m_target;
			docView.XsdUri = this.textBoxViewXsdNamespace.Text;
		}

		private void toolStripButtonReqImportMandatory_Click(object sender, EventArgs e)
		{
			this.ApplyExchangeRequirement(this.toolStripButtonReqImportMandatory, DocExchangeApplicabilityEnum.Import, DocExchangeRequirementEnum.Mandatory);
		}

		private void toolStripButtonReqImportRecommended_Click(object sender, EventArgs e)
		{
			this.ApplyExchangeRequirement(this.toolStripButtonReqImportRecommended, DocExchangeApplicabilityEnum.Import, DocExchangeRequirementEnum.Optional);
		}

		private void toolStripButtonReqImportNotRecommended_Click(object sender, EventArgs e)
		{
			this.ApplyExchangeRequirement(this.toolStripButtonReqImportNotRecommended, DocExchangeApplicabilityEnum.Import, DocExchangeRequirementEnum.NotRecommended);
		}

		private void toolStripButtonReqImportExcluded_Click(object sender, EventArgs e)
		{
			this.ApplyExchangeRequirement(this.toolStripButtonReqImportExcluded, DocExchangeApplicabilityEnum.Import, DocExchangeRequirementEnum.Excluded);
		}

		private void toolStripButtonReqExportExcluded_Click(object sender, EventArgs e)
		{
			this.ApplyExchangeRequirement(this.toolStripButtonReqExportExcluded, DocExchangeApplicabilityEnum.Export, DocExchangeRequirementEnum.Excluded);
		}

		private void toolStripButtonReqExportNotRecommended_Click(object sender, EventArgs e)
		{
			this.ApplyExchangeRequirement(this.toolStripButtonReqExportNotRecommended, DocExchangeApplicabilityEnum.Export, DocExchangeRequirementEnum.NotRecommended);
		}

		private void toolStripButtonReqExportRecommended_Click(object sender, EventArgs e)
		{
			this.ApplyExchangeRequirement(this.toolStripButtonReqExportRecommended, DocExchangeApplicabilityEnum.Export, DocExchangeRequirementEnum.Optional);
		}

		private void toolStripButtonReqExportMandatory_Click(object sender, EventArgs e)
		{
			this.ApplyExchangeRequirement(this.toolStripButtonReqExportMandatory, DocExchangeApplicabilityEnum.Export, DocExchangeRequirementEnum.Mandatory);
		}

		private void listViewExchange_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.InitExchangeRequirements();
		}

		private void InitExchangeRequirements()
		{
			if (this.listViewExchange.SelectedItems.Count == 0)
			{
				// disable all
				this.toolStripButtonReqImportMandatory.Enabled = false;
				this.toolStripButtonReqImportRecommended.Enabled = false;
				this.toolStripButtonReqImportNotRecommended.Enabled = false;
				this.toolStripButtonReqImportExcluded.Enabled = false;
				this.toolStripButtonReqExportExcluded.Enabled = false;
				this.toolStripButtonReqExportNotRecommended.Enabled = false;
				this.toolStripButtonReqExportRecommended.Enabled = false;
				this.toolStripButtonReqExportMandatory.Enabled = false;
				return;
			}

			this.toolStripButtonReqImportMandatory.Enabled = true;
			this.toolStripButtonReqImportRecommended.Enabled = true;
			this.toolStripButtonReqImportNotRecommended.Enabled = true;
			this.toolStripButtonReqImportExcluded.Enabled = true;
			this.toolStripButtonReqExportExcluded.Enabled = true;
			this.toolStripButtonReqExportNotRecommended.Enabled = true;
			this.toolStripButtonReqExportRecommended.Enabled = true;
			this.toolStripButtonReqExportMandatory.Enabled = true;

			LoadExchangeRequirement(this.toolStripButtonReqImportMandatory, DocExchangeApplicabilityEnum.Import, DocExchangeRequirementEnum.Mandatory);
			LoadExchangeRequirement(this.toolStripButtonReqImportRecommended, DocExchangeApplicabilityEnum.Import, DocExchangeRequirementEnum.Optional);
			LoadExchangeRequirement(this.toolStripButtonReqImportNotRecommended, DocExchangeApplicabilityEnum.Import, DocExchangeRequirementEnum.NotRecommended);
			LoadExchangeRequirement(this.toolStripButtonReqImportExcluded, DocExchangeApplicabilityEnum.Import, DocExchangeRequirementEnum.Excluded);
			LoadExchangeRequirement(this.toolStripButtonReqExportExcluded, DocExchangeApplicabilityEnum.Export, DocExchangeRequirementEnum.Excluded);
			LoadExchangeRequirement(this.toolStripButtonReqExportNotRecommended, DocExchangeApplicabilityEnum.Export, DocExchangeRequirementEnum.NotRecommended);
			LoadExchangeRequirement(this.toolStripButtonReqExportRecommended, DocExchangeApplicabilityEnum.Export, DocExchangeRequirementEnum.Optional);
			LoadExchangeRequirement(this.toolStripButtonReqExportMandatory, DocExchangeApplicabilityEnum.Export, DocExchangeRequirementEnum.Mandatory);
		}

		private void LoadExchangeRequirement(ToolStripButton button, DocExchangeApplicabilityEnum applicability, DocExchangeRequirementEnum requirement)
		{
			DocTemplateUsage docUsage = (DocTemplateUsage)this.m_target;

			bool? common = null; // the common value
			bool varies = false; // whether value varies among objects

			foreach (ListViewItem lvi in this.listViewExchange.SelectedItems)
			{
				DocExchangeDefinition docDef = (DocExchangeDefinition)lvi.Tag;

				// find exchange on usage
				foreach (DocExchangeItem docItem in docUsage.Exchanges)
				{
					if (docItem.Exchange == docDef && docItem.Applicability == applicability)
					{
						bool eachval = (docItem.Requirement == requirement);
						if (common == null)
						{
							common = eachval;
						}
						else if (common != eachval)
						{
							varies = true;
						}
					}
				}
			}

			this.m_loadreq = true;
			button.Checked = (common == true && !varies);
			this.m_loadreq = false;
		}

		private void ApplyExchangeRequirement(ToolStripButton button, DocExchangeApplicabilityEnum applicability, DocExchangeRequirementEnum requirement)
		{
			if (m_loadreq)
				return;

			// if already checked, then reset
			if (button.Checked)
			{
				requirement = DocExchangeRequirementEnum.NotRelevant;
			}

			// commit changes

			DocTemplateUsage docUsage = (DocTemplateUsage)this.m_target;

			foreach (ListViewItem lvi in this.listViewExchange.SelectedItems)
			{
				DocExchangeDefinition docExchange = (DocExchangeDefinition)lvi.Tag;

				// find existing  
				bool exists = false;
				foreach (DocExchangeItem docItem in docUsage.Exchanges)
				{
					if (docItem.Exchange == docExchange && docItem.Applicability == applicability)
					{
						// found it
						if (requirement == DocExchangeRequirementEnum.NotRelevant)
						{
							// delete item (reduce size)
							docUsage.Exchanges.Remove(docItem);
							docItem.Delete();
						}
						else
						{
							// update item
							docItem.Requirement = requirement;
						}
						exists = true;
						break; // perf, and collection may have been modified
					}
				}

				if (!exists)
				{
					DocExchangeItem docItem = new DocExchangeItem();
					docItem.Exchange = docExchange;
					docItem.Applicability = applicability;
					docItem.Requirement = requirement;
					docUsage.Exchanges.Add(docItem);
				}

				// update list
				if (applicability == DocExchangeApplicabilityEnum.Import)
				{
					lvi.SubItems[1].Text = requirement.ToString();
				}
				else if (applicability == DocExchangeApplicabilityEnum.Export)
				{
					lvi.SubItems[2].Text = requirement.ToString();
				}

				// force update of buttons
				listViewExchange_SelectedIndexChanged(this, EventArgs.Empty);
			}

		}

		private void LoadReferencedViews()
		{
			List<DocModelView> list = null;
			if (this.m_target is DocPublication)
			{
				list = ((DocPublication)this.m_target).Views;
			}
			else if (this.m_target is DocExample)
			{
				list = ((DocExample)this.m_target).Views;
			}

			this.listViewViews.Items.Clear();
			foreach (DocModelView docView in list)
			{
				ListViewItem lvi = new ListViewItem();
				lvi.Tag = docView;
				lvi.Text = docView.Name;
				this.listViewViews.Items.Add(lvi);
			}
		}

		private void toolStripButtonViewInsert_Click(object sender, EventArgs e)
		{
			List<DocModelView> list = null;
			if (this.m_target is DocPublication)
			{
				list = ((DocPublication)this.m_target).Views;
			}
			else if (this.m_target is DocExample)
			{
				list = ((DocExample)this.m_target).Views;
			}

			using (FormSelectView form = new FormSelectView(this.m_project, "Select the view(s) to include."))
			{
				if (form.ShowDialog(this) == DialogResult.OK)
				{
					foreach (DocModelView docView in form.Selection)
					{
						if (!list.Contains(docView))
						{
							list.Add(docView);
						}
					}

					this.LoadReferencedViews();
				}
			}

		}

		private void toolStripButtonViewRemove_Click(object sender, EventArgs e)
		{
			List<DocModelView> list = null;
			if (this.m_target is DocPublication)
			{
				list = ((DocPublication)this.m_target).Views;
			}
			else if (this.m_target is DocExample)
			{
				list = ((DocExample)this.m_target).Views;
			}

			for (int i = this.listViewViews.SelectedIndices.Count - 1; i >= 0; i--)
			{
				list.RemoveAt(this.listViewViews.SelectedIndices[i]);
			}

			this.LoadReferencedViews();
		}

		private void listViewViews_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.toolStripButtonViewRemove.Enabled = (this.listViewViews.SelectedIndices.Count > 0);
		}

		private void toolStripButtonFormatSchema_Click(object sender, EventArgs e)
		{
			SetFormatOption(DocFormatOptionEnum.Schema);
		}

		private void toolStripButtonFormatExamples_Click(object sender, EventArgs e)
		{
			SetFormatOption(DocFormatOptionEnum.Examples);
		}

		private void toolStripButtonFormatMarkup_Click(object sender, EventArgs e)
		{
			SetFormatOption(DocFormatOptionEnum.Markup);
		}

		private void SetFormatOption(DocFormatOptionEnum option)
		{
			DocFormat docFormat = (DocFormat)this.listViewFormats.SelectedItems[0].Tag;
			docFormat.FormatOptions = option;

			this.listViewFormats.SelectedItems[0].SubItems[1].Text = option.ToString();
			UpdateFormatOption();
		}

		private void listViewFormats_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateFormatOption();
		}

		private void UpdateFormatOption()
		{
			if (this.listViewFormats.SelectedItems.Count == 1)
			{
				this.toolStripButtonFormatSchema.Enabled = true;
				this.toolStripButtonFormatExamples.Enabled = true;
				this.toolStripButtonFormatMarkup.Enabled = true;

				ListViewItem lvi = (ListViewItem)this.listViewFormats.SelectedItems[0];
				DocFormat docFormat = (DocFormat)lvi.Tag;
				this.toolStripButtonFormatNone.Checked = (docFormat.FormatOptions == DocFormatOptionEnum.None);
				this.toolStripButtonFormatSchema.Checked = (docFormat.FormatOptions == DocFormatOptionEnum.Schema);
				this.toolStripButtonFormatExamples.Checked = (docFormat.FormatOptions == DocFormatOptionEnum.Examples);
				this.toolStripButtonFormatMarkup.Checked = (docFormat.FormatOptions == DocFormatOptionEnum.Markup);
				lvi.SubItems[1].Text = docFormat.FormatOptions.ToString();
			}
			else
			{
				this.toolStripButtonFormatSchema.Enabled = false;
				this.toolStripButtonFormatExamples.Enabled = false;
				this.toolStripButtonFormatMarkup.Enabled = false;
			}

		}

		private void toolStripButtonFormatNone_Click(object sender, EventArgs e)
		{
			SetFormatOption(DocFormatOptionEnum.None);
		}

		private void checkBoxPublishISO_CheckedChanged(object sender, EventArgs e)
		{
			DocPublication docPub = (DocPublication)this.m_target;
			docPub.ISO = this.checkBoxPublishISO.Checked;
		}

		private void checkBoxPublishHideHistory_CheckedChanged(object sender, EventArgs e)
		{
			DocPublication docPub = (DocPublication)this.m_target;
			docPub.HideHistory = this.checkBoxPublishHideHistory.Checked;
		}

		private void checkBoxPublishUML_CheckedChanged(object sender, EventArgs e)
		{
			DocPublication docPub = (DocPublication)this.m_target;
			docPub.UML = this.checkBoxPublishUML.Checked;
		}

		private void textBoxHeader_TextChanged(object sender, EventArgs e)
		{
			DocPublication docPub = (DocPublication)this.m_target;
			docPub.Header = textBoxHeader.Text;
		}

		private void textBoxFooter_TextChanged(object sender, EventArgs e)
		{
			DocPublication docPub = (DocPublication)this.m_target;
			docPub.Footer = textBoxFooter.Text;
		}

		private void checkBoxPublishExchangeTables_CheckedChanged(object sender, EventArgs e)
		{
			DocPublication docPub = (DocPublication)this.m_target;
			docPub.Exchanges = checkBoxPublishExchangeTables.Checked;
		}

		private void textBoxExample_TextChanged(object sender, EventArgs e)
		{
			if (this.m_loadall)
				return;

			DocExample docExample = (DocExample)this.m_target;
			docExample.File = Encoding.ASCII.GetBytes(this.textBoxExample.Text);
		}

		private void comboBoxPropertyAccess_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.m_loadall)
				return;

			DocProperty docProperty = (DocProperty)this.m_target;
			docProperty.AccessState = (DocStateEnum)this.comboBoxPropertyAccess.SelectedIndex;

			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void comboBoxQuantityAccess_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.m_loadall)
				return;

			DocQuantity docProperty = (DocQuantity)this.m_target;
			docProperty.AccessState = (DocStateEnum)this.comboBoxPropertyAccess.SelectedIndex;

			if (this.SchemaChanged != null)
			{
				this.SchemaChanged(this, EventArgs.Empty);
			}
		}

		private void checkBoxPublishHtmlExamples_CheckedChanged(object sender, EventArgs e)
		{
			DocPublication docPub = (DocPublication)this.m_target;
			docPub.HtmlExamples = checkBoxPublishHtmlExamples.Checked;
		}

		private void toolStripButtonExampleLink_Click(object sender, EventArgs e)
		{
			if (this.openFileDialogExample.ShowDialog() == DialogResult.OK)
			{
				DocExample docExample = (DocExample)this.m_target;
				docExample.Path = this.openFileDialogExample.FileName;
				docExample.File = null;
			}
		}

		private void checkBoxPublishBSI_CheckedChanged(object sender, EventArgs e)
		{
			DocPublication docPub = (DocPublication)this.m_target;
			docPub.ReportIssues = this.checkBoxPublishBSI.Checked;
		}

		private void toolStripButtonRootEntity_Click(object sender, EventArgs e)
		{
			DocConceptRoot docConceptRoot = (DocConceptRoot)this.m_target;
			using (FormSelectEntity form = new FormSelectEntity(null, docConceptRoot.ApplicableEntity, this.m_project, SelectDefinitionOptions.Entity))
			{
				if (form.ShowDialog(this) == DialogResult.OK && form.SelectedEntity != null)
				{
					docConceptRoot.ApplicableEntity = form.SelectedEntity as DocEntity;

					if (this.SchemaChanged != null)
					{
						this.SchemaChanged(this, EventArgs.Empty);
					}
				}
			}
		}

		
	}
}
