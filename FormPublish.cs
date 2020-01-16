// Name:        FormPublish.cs
// Description: Dialog box for uploading content to server
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using IfcDoc.Schema.DOC;

using BuildingSmart.Utilities.Dictionary;

namespace IfcDoc
{
	public partial class FormPublish : Form
	{
		DocProject m_project;
		Exception m_exception;
		bool m_download;

		DataDictionary m_dictionary;
		Dictionary<string, string> m_contexts;

		Type m_typeProject; // when uploading, compiled type for project and assembly
		Queue<Type> m_queueTypes;
		int m_count; // total for progress counting
		Dictionary<Type, TreeNode> m_mapTree;

		public FormPublish()
		{
			InitializeComponent();

			this.checkBoxRemember.Checked = !String.IsNullOrEmpty(Properties.Settings.Default.Username);
			this.textBoxUsername.Text = Properties.Settings.Default.Username;
			this.textBoxPassword.Text = Properties.Settings.Default.Password;
		}

		public FormPublish(DocProject docProject, bool download) : this()
		{
			this.m_project = docProject;
			this.m_download = download;

			if (m_download)
			{
				this.Text = "Download";
			}
			else
			{
				this.Text = "Upload";
			}
		}

		public string Url
		{
			get
			{
				return this.textBoxUrl.Text;
			}
			set
			{
				this.textBoxUrl.Text = value;
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			this.textBoxUrl.Enabled = false;
			this.progressBar.Value = 0;
			this.buttonOK.Enabled = false;
			this.errorProvider.Clear();

			this.m_queueTypes = new Queue<Type>();
			this.m_mapTree = new Dictionary<Type, TreeNode>();
			foreach (TreeNode tnNamespace in this.treeViewContainer.Nodes)
			{
				foreach (TreeNode tnType in tnNamespace.Nodes)
				{
					if (tnType.Tag is Type && (tnType.Checked || tnNamespace.Checked) && tnType.BackColor != Color.Lime)
					{
						Type t = (Type)tnType.Tag;
						m_queueTypes.Enqueue(t);
						m_mapTree.Add(t, tnType);
					}
				}
			}

			this.m_count = this.m_queueTypes.Count;

			// start upload or download
			this.backgroundWorkerPublish.RunWorkerAsync();
		}

		private void backgroundWorkerPublish_DoWork(object sender, DoWorkEventArgs e)
		{
			// connect to server
			this.m_exception = null;
			try
			{
				if (this.m_download)
				{
					//DataDictionary.Download(this.m_views.ToArray());
				}
				else
				{
					if (m_queueTypes != null)
					{
						int i = 0;
						Type t;
						while (this.m_queueTypes.Count > 0)
						{
							t = this.m_queueTypes.Dequeue();

							for (int retry = 0; retry < 10; retry++)
							{
								try
								{
									this.m_dictionary.WriteType(t);
									break;
								}
								catch (WebException webex)
								{
									this.backgroundWorkerPublish.ReportProgress(0, webex);

									// wait 5 seconds, keep retrying
									System.Threading.Thread.Sleep(5000);
								}
							}

							i++;
							int prog = (int)((double)i * 100.0 / (double)this.m_count);
							this.backgroundWorkerPublish.ReportProgress(prog, t);

							if (e.Cancel)
								return;
						}
					}
				}
			}
			catch (Exception x)
			{
				this.m_exception = x;
			}
		}

		private void backgroundWorkerPublish_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			this.progressBar.Value = e.ProgressPercentage;

			if (e.UserState is Type)
			{
				Type t = (Type)e.UserState;

				TreeNode tn = null;
				if (m_mapTree.TryGetValue(t, out tn))
				{
					//tn.ForeColor = Color.Blue;
					tn.BackColor = Color.Lime;
					this.treeViewContainer.SelectedNode = tn;
					tn.EnsureVisible();
				}
			}
			else if (e.UserState is Exception)
			{
				this.errorProvider.SetError(this.labelError, this.m_exception.Message);
				// will attempt to keep going if web exception
			}
		}

		private void backgroundWorkerPublish_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (m_exception != null)
			{
				this.textBoxUrl.Enabled = true;
				this.buttonOK.Enabled = true;
				this.errorProvider.SetError(this.labelError, this.m_exception.Message);
			}
			else
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}

		private void comboBoxProtocol_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (this.comboBoxProtocol.SelectedIndex)
			{
				case 0:
					this.textBoxUrl.Text = DataDictionary.test_bsdd_buildingsmart_org;//"http://test.bsdd.buildingsmart.org/";
					break;

				case 1:
					this.textBoxUrl.Text = DataDictionary.bsdd_buildingsmart_org;//"http://bsdd.buildingsmart.org/";
					break;
			}
		}

		private void FormPublish_Load(object sender, EventArgs e)
		{
			this.comboBoxProtocol.SelectedIndex = 0;
		}

		private void buttonLogin_Click(object sender, EventArgs e)
		{
			this.buttonLogin.Enabled = false;
			this.comboBoxProtocol.Enabled = false;
			this.textBoxUsername.Enabled = false;
			this.textBoxPassword.Enabled = false;

			try
			{
				this.progressBar.Visible = true;
				this.progressBar.Style = ProgressBarStyle.Marquee;

				this.m_dictionary = DataDictionary.Connect(this.textBoxUrl.Text, this.textBoxUsername.Text, this.textBoxPassword.Text);

				this.comboBoxContext.Items.Clear();
				this.m_contexts = this.m_dictionary.ReadContexts(!this.m_download);
				if (this.m_contexts != null)
				{
					foreach (string key in this.m_contexts.Keys)
					{
						string val = this.m_contexts[key];
						this.comboBoxContext.Items.Add(val);
					}
					if (this.comboBoxContext.Items.Count > 0)
					{
						this.comboBoxContext.SelectedIndex = 0;
						this.comboBoxContext.Enabled = true;
						this.textBoxNamespace.Enabled = true;
						this.buttonSearch.Enabled = (this.m_download);
					}
				}
				else
				{
					this.errorProvider.SetError(this.labelError, "The account provided does not have any write access.");
				}


				if (this.m_download)
				{
					Requery();
				}
				else
				{
					// generate types
					this.m_typeProject = Compiler.CompileProject(this.m_project, true);
					if (this.m_typeProject != null)
					{
						// get the root type -- only write types that derive from the semantic base class (IfcRoot)
						Type typeBase = m_typeProject;
						while (typeBase.BaseType != null)
						{
							typeBase = typeBase.BaseType;
						}

						// publish types
						SortedDictionary<string, SortedDictionary<string, Type>> mapNamespace = new SortedDictionary<string, SortedDictionary<string, Type>>();

						Type[] types = m_typeProject.Assembly.GetTypes();

						this.progressBar.Style = ProgressBarStyle.Continuous;

						foreach (Type t in types)
						{
							if (t.IsClass)
							{
								SortedDictionary<string, Type> listNS = null;
								if (!mapNamespace.TryGetValue(t.Namespace, out listNS))
								{
									listNS = new SortedDictionary<string, Type>();
									mapNamespace.Add(t.Namespace, listNS);
								}
								listNS.Add(t.Name, t);
							}
						}

						foreach (string ns in mapNamespace.Keys)
						{
							TreeNode tn = new TreeNode();
							tn.Tag = ns;
							tn.Text = ns;
							tn.ImageIndex = 24;
							tn.SelectedImageIndex = 24;
							this.treeViewContainer.Nodes.Add(tn);

							SortedDictionary<string, Type> listNS = mapNamespace[ns];
							foreach (string typename in listNS.Keys)
							{
								Type t = listNS[typename];
								TreeNode tnType = new TreeNode();
								tnType.Tag = t;
								tnType.Text = t.Name;
								tnType.ImageIndex = 5;
								tnType.SelectedImageIndex = 5;

								tn.Nodes.Add(tnType);
							}
						}
					}
					else
					{
						this.errorProvider.SetError(this.labelError, "No data has been defined that can be uploaded.");
					}
				}
			}
			catch (Exception xx)
			{
				this.progressBar.Style = ProgressBarStyle.Continuous;
				this.progressBar.Visible = false;
				this.errorProvider.SetError(this.labelError, xx.Message);
			}


			this.buttonOK.Enabled = true;
		}


		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			if (this.checkBoxRemember.Checked)
			{
				Properties.Settings.Default.Username = this.textBoxUsername.Text;
				Properties.Settings.Default.Password = this.textBoxPassword.Text;
			}
			else
			{
				Properties.Settings.Default.Username = String.Empty;
				Properties.Settings.Default.Password = String.Empty;
			}
			Properties.Settings.Default.Save();
		}

		private void treeViewContainer_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			if (!this.m_download)
				return;

			//IfdConcept ifdConcept = e.Node.Tag as IfdConcept;
			if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag == null)
			{
				// load child concepts
				try
				{
					if (!this.backgroundWorkerConcepts.IsBusy)
					{
						this.backgroundWorkerConcepts.RunWorkerAsync(e.Node);
					}
					else
					{
						e.Cancel = true;
					}
				}
				catch
				{
					e.Cancel = true;
				}
			}
		}

		private void backgroundWorkerConcepts_DoWork(object sender, DoWorkEventArgs e)
		{
			if (this.m_dictionary == null)
				return;

			TreeNode tn = (TreeNode)e.Argument;
			Dictionary<string, object> items = this.m_dictionary.ReadNamespace(tn.Text);
			tn.Nodes[0].Tag = items;

			e.Result = tn;
		}

		private void backgroundWorkerConcepts_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			TreeNode tn = (TreeNode)e.Result;

			object[] list = tn.Nodes[0].Tag as object[];
			if (list == null)
				return;

			tn.Nodes.Clear();

			if (list == null)
				return;

			foreach (object conc in list)
			{
				TreeNode tnConc = new TreeNode();
				tnConc.Tag = conc;

				if (conc is Type)
				{
					Type t = (Type)conc;
					tnConc.Tag = t;
					tnConc.Text = t.Name;
					tnConc.ImageIndex = 5;

					foreach (PropertyInfo prop in t.GetProperties())
					{
						TreeNode tnProp = new TreeNode();
						tnProp.Tag = prop;
						tnProp.Text = tnProp.Name;
						tnProp.ImageIndex = 9;
					}
				}
				else if (conc is string)
				{
					tnConc.Text = conc.ToString();
					tnConc.ImageIndex = 24;

					TreeNode tnSub = new TreeNode();
					tnSub.Text = "Loading...";
					tnConc.Nodes.Add(tnSub);
				}

#if false
                IfdConceptTypeEnum ifdConceptType = IfdConceptTypeEnum.UNDEFINED;
                Enum.TryParse<IfdConceptTypeEnum>(conc.conceptType, out ifdConceptType);

                switch (ifdConceptType)
                {
                    case IfdConceptTypeEnum.BAG:
                        tnConc.ImageIndex = 24;
                        break;

                    case IfdConceptTypeEnum.NEST:
                        tnConc.ImageIndex = 10;
                        break;

                    case IfdConceptTypeEnum.PROPERTY:
                        tnConc.ImageIndex = 9;
                        break;

                    case IfdConceptTypeEnum.SUBJECT:
                        tnConc.ImageIndex = 5; // DocEntity
                        break;

                    case IfdConceptTypeEnum.MEASURE:
                        tnConc.ImageIndex = 4;
                        break;

                    case IfdConceptTypeEnum.ACTOR:
                        tnConc.ImageIndex = 8;
                        break;

                    case IfdConceptTypeEnum.DOCUMENT:
                        tnConc.ImageIndex = 20;
                        break;

                    case IfdConceptTypeEnum.CLASSIFICATION:
                        tnConc.ImageIndex = 1;
                        break;

                    default:
                        break;
                }
#endif
				tnConc.SelectedImageIndex = tnConc.ImageIndex;

				tn.Nodes.Add(tnConc);
			}

			//tn.Text += " (" + list.Count + ")";
		}

		private void Requery()
		{
			if (this.m_download)
			{
				this.progressBar.Style = ProgressBarStyle.Continuous;

				this.treeViewContainer.Nodes.Clear();

				Dictionary<string, object> objs = this.m_dictionary.ReadNamespace(this.textBoxNamespace.Text);
				if (objs == null)
					return;

				foreach (object o in objs.Values)
				{
					if (o is string)
					{
						TreeNode tnRoot = new TreeNode();
						tnRoot.Text = o.ToString();
						tnRoot.ImageIndex = 24;
						tnRoot.SelectedImageIndex = 24;
						this.treeViewContainer.Nodes.Add(tnRoot);

						Dictionary<string, object> subs = this.m_dictionary.ReadNamespace(tnRoot.Text);
						if (subs != null)
						{
							foreach (object s in subs.Values)
							{
								if (s is Type)
								{
									Type t = (Type)s;
									TreeNode tnSub = new TreeNode();
									tnSub.Tag = s;
									tnSub.Text = t.Name;
									tnSub.ImageIndex = 5;
									tnSub.SelectedImageIndex = 5;
									tnRoot.Nodes.Add(tnSub);
								}
								else if (s is string)
								{
									TreeNode tnSub = new TreeNode();
									tnSub.Tag = null;// s;
									tnSub.Text = (string)s;
									tnSub.ImageIndex = 24;
									tnSub.SelectedImageIndex = 24;
									tnRoot.Nodes.Add(tnSub);

									TreeNode tnExp = new TreeNode();
									tnExp.Text = "Loading...";
									tnSub.Nodes.Add(tnExp);
								}
							}
						}

						//TreeNode tnSub = new TreeNode();
						//tnSub.Text = "Loading...";
						//tnRoot.Nodes.Add(tnSub);

						//tnRoot.Expand(); // was trigger event notification to load
					}
				}
			}
		}

		private void comboBoxContext_SelectedIndexChanged(object sender, EventArgs e)
		{
			string item = this.comboBoxContext.SelectedItem as string;
			foreach (string key in this.m_contexts.Keys)
			{
				string val = this.m_contexts[key];
				if (item.Equals(val))
				{
					// found the context -- now set it and requery...
					this.m_dictionary.SetActiveContext(key);

					// requery
					Requery();
				}
			}
		}

		private void textBoxNamespace_TextChanged(object sender, EventArgs e)
		{
		}

		private void buttonSearch_Click(object sender, EventArgs e)
		{
			// requery
			Requery();
		}

		private void textBoxNamespace_Validated(object sender, EventArgs e)
		{
			// requery
			Requery();
		}
	}
}
