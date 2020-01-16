// Name:        FormMerge.cs
// Description: Dialog box for merging files.
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

// Portions of code within this file originated from DifferenceEngine on CodeProject.org (file containing referenced data definitions).

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using IfcDoc.Schema.DOC;

using DifferenceEngine;

namespace IfcDoc
{
	public partial class FormMerge : Form
	{
		bool m_ignore;
		Dictionary<Guid, DocObject> m_mapOriginal;

		public FormMerge()
		{
			InitializeComponent();

			m_ignore = false;
		}

		public FormMerge(Dictionary<Guid, DocObject> mapOriginal, DocProject docChange) : this()
		{
			this.m_mapOriginal = mapOriginal;

			// add nodes for everything, then delete ones that haven't changed or don't have any children

			// iterate and find changes in documentation
			foreach (DocPropertyEnumeration docChangePset in docChange.PropertyEnumerations)
			{
				DocObject docOriginalPset = null;
				if (mapOriginal.TryGetValue(docChangePset.Uuid, out docOriginalPset))
				{
					TreeNode tnPset = null;
					if (!String.Equals(docOriginalPset.Documentation, docChangePset.Documentation))
					{
						tnPset = this.AddNode(null, new ChangeInfo(docOriginalPset.Name, docOriginalPset, docChangePset));
					}

					foreach (DocPropertyConstant docChangeProp in docChangePset.Constants)
					{
						DocObject docOriginalProp = ((DocPropertyEnumeration)docOriginalPset).GetConstant(docChangeProp.Name);
						if (docOriginalProp != null)
						{
							TreeNode tnProperty = null;
							if (!String.Equals(docOriginalProp.Documentation, docChangeProp.Documentation))
							{
								if (tnPset == null)
								{
									tnPset = this.AddNode(null, new ChangeInfo(docOriginalPset.Name, docOriginalPset, docChangePset));
								}

								tnProperty = this.AddNode(tnPset, new ChangeInfo(docOriginalPset.Name + "." + docOriginalProp.Name, docOriginalProp, docChangeProp));
							}

							// localization
							foreach (DocLocalization docChangeLocal in docChangeProp.Localization)
							{
								DocLocalization docOriginalLocal = docOriginalProp.GetLocalization(docChangeLocal.Locale);
								if (docOriginalLocal != null)
								{
									if (!String.Equals(docOriginalLocal.Documentation, docChangeLocal.Documentation))
									{
										if (tnPset == null)
										{
											tnPset = this.AddNode(null, new ChangeInfo(docOriginalPset.Name, docOriginalPset, docChangePset));
										}

										if (tnProperty == null)
										{
											tnProperty = this.AddNode(tnPset, new ChangeInfo(docOriginalPset.Name + "." + docOriginalProp.Name, docOriginalProp, docChangeProp));
										}

										this.AddNode(tnProperty, new ChangeInfo(docChangeLocal.Locale, docOriginalLocal, docChangeLocal));
									}
								}
								else
								{
									if (tnPset == null)
									{
										tnPset = this.AddNode(null, new ChangeInfo(docOriginalPset.Name, docOriginalPset, docChangePset));
									}

									if (tnProperty == null)
									{
										tnProperty = this.AddNode(tnPset, new ChangeInfo(docOriginalPset.Name + "." + docOriginalProp.Name, docOriginalProp, docChangeProp));
									}

									// new localization
									this.AddNode(tnProperty, new ChangeInfo(docChangeLocal.Locale, docOriginalLocal, docChangeLocal));
								}
							}
						}
						else
						{
							if (tnPset == null)
							{
								tnPset = this.AddNode(null, new ChangeInfo(docOriginalPset.Name, docOriginalPset, docChangePset));
							}

							// NEW:
							this.AddNode(tnPset, new ChangeInfo(docChangePset.Name + "." + docChangeProp.Name, null, docChangeProp));
						}
					}
				}
				else
				{
					// NEW:
					this.AddNode(null, new ChangeInfo(docChangePset.Name, null, docChangePset));
				}

			}

			foreach (DocSection docChangeSection in docChange.Sections)
			{
				DocObject docOriginalSection = null;
				if (mapOriginal.TryGetValue(docChangeSection.Uuid, out docOriginalSection))
				{
					foreach (DocSchema docChangeSchema in docChangeSection.Schemas)
					{
						DocObject docOriginalSchema;
						if (mapOriginal.TryGetValue(docChangeSchema.Uuid, out docOriginalSchema))
						{
							// compare schemas

							TreeNode tnSchema = this.AddNode(null, new ChangeInfo(docOriginalSchema.Name, docOriginalSchema, docChangeSchema));

							foreach (DocType docChangeType in docChangeSchema.Types)
							{
								DocObject docOriginalType = null;
								if (mapOriginal.TryGetValue(docChangeType.Uuid, out docOriginalType))
								{
									TreeNode tnType = null;

									if (!String.Equals(docOriginalType.Documentation, docChangeType.Documentation))
									{
										tnType = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalType.Name, docOriginalType, docChangeType));
									}

									if (docChangeType is DocEnumeration)
									{
										DocEnumeration docChangeEnum = (DocEnumeration)docChangeType;
										foreach (DocConstant docChangeConst in docChangeEnum.Constants)
										{
											DocObject docOriginalConst = null;
											if (mapOriginal.TryGetValue(docChangeConst.Uuid, out docOriginalConst))
											{
												if (!String.Equals(docOriginalConst.Documentation, docChangeConst.Documentation))
												{
													if (tnType == null)
													{
														tnType = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalType.Name, docOriginalType, docChangeType));
													}
													this.AddNode(tnType, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalType.Name + "." + docOriginalConst.Name, docOriginalConst, docChangeConst));
												}
											}
											else
											{
												if (tnType == null)
												{
													tnType = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalType.Name, docOriginalType, docChangeType));
												}

												// NEW:
												this.AddNode(tnType, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalType.Name + "." + docOriginalConst.Name, null, docChangeConst));
											}
										}
									}
								}
							}

							foreach (DocEntity docChangeEntity in docChangeSchema.Entities)
							{
								DocObject docOriginalObj = null;
								if (mapOriginal.TryGetValue(docChangeEntity.Uuid, out docOriginalObj))
								{
									DocEntity docOriginalEntity = (DocEntity)docOriginalObj;
									TreeNode tnEntity = null;

									if (!String.Equals(docOriginalEntity.Documentation, docChangeEntity.Documentation))
									{
										tnEntity = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name, docOriginalEntity, docChangeEntity));
									}

									// add attributes
									foreach (DocAttribute docChangeAttr in docChangeEntity.Attributes)
									{
										DocObject docOriginalAttr = null;
										if (mapOriginal.TryGetValue(docChangeAttr.Uuid, out docOriginalAttr))
										{
											if (!String.Equals(docOriginalAttr.Name, docChangeAttr.Name) ||
												!String.Equals(docOriginalAttr.Documentation, docChangeAttr.Documentation))
											{
												if (tnEntity == null)
												{
													tnEntity = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name, docOriginalEntity, docChangeEntity));
												}
												this.AddNode(tnEntity, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name + "." + docOriginalAttr.Name, docOriginalAttr, docChangeAttr));
											}
										}
										else
										{
											if (tnEntity == null)
											{
												tnEntity = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name, docOriginalEntity, docChangeEntity));
											}

											// new attribute
											this.AddNode(tnEntity, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name + "." + docChangeAttr.Name, null, docChangeAttr));
										}
									}

									// remove attributes
									foreach (DocAttribute docOriginalAttr in docOriginalEntity.Attributes)
									{
										bool bFound = false;
										foreach (DocAttribute docChangeAttr in docChangeEntity.Attributes)
										{
											if (docOriginalAttr.Uuid.Equals(docChangeAttr.Uuid))
											{
												bFound = true;
												break;
											}
										}

										if (!bFound)
										{
											if (tnEntity == null)
											{
												tnEntity = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name, docOriginalEntity, docChangeEntity));
											}

											// delete attribute
											this.AddNode(tnEntity, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name + "." + docOriginalAttr.Name, docOriginalAttr, null));
										}
									}

									foreach (DocWhereRule docChangeWhere in docChangeEntity.WhereRules)
									{
										DocObject docOriginalWhere = null;
										if (mapOriginal.TryGetValue(docChangeWhere.Uuid, out docOriginalWhere))
										{
											DocWhereRule docOriginalWhereRule = (DocWhereRule)docOriginalWhere;
											if (!String.Equals(docOriginalWhere.Name, docChangeWhere.Name) ||
												!String.Equals(docOriginalWhere.Documentation, docChangeWhere.Documentation) ||
												!String.Equals(docOriginalWhereRule.Expression, docChangeWhere.Expression))
											{
												if (tnEntity == null)
												{
													tnEntity = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name, docOriginalEntity, docChangeEntity));
												}

												this.AddNode(tnEntity, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name + "." + docOriginalWhere.Name, docOriginalWhere, docChangeWhere));
											}
										}
										else
										{
											if (tnEntity == null)
											{
												tnEntity = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name, docOriginalEntity, docChangeEntity));
											}

											// new where rule
											this.AddNode(tnEntity, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name + "." + docChangeWhere.Name, null, docChangeWhere));
										}
									}

									foreach (DocUniqueRule docChangeAttr in docChangeEntity.UniqueRules)
									{
										DocObject docOriginalAttr = null;
										if (mapOriginal.TryGetValue(docChangeAttr.Uuid, out docOriginalAttr))
										{
											if (!String.Equals(docOriginalAttr.Documentation, docChangeAttr.Documentation))
											{
												if (tnEntity == null)
												{
													tnEntity = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name, docOriginalEntity, docChangeEntity));
												}

												this.AddNode(tnEntity, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalEntity.Name + "." + docOriginalAttr.Name, docOriginalAttr, docChangeAttr));
											}
										}
									}
								}

							}

							foreach (DocFunction docChangeFunction in docChangeSchema.Functions)
							{
								DocObject docOriginalFunc = null;
								if (mapOriginal.TryGetValue(docChangeFunction.Uuid, out docOriginalFunc))
								{
									TreeNode tnType = null;

									DocFunction docOriginalFunction = (DocFunction)docOriginalFunc;

									if (!String.Equals(docOriginalFunction.Name, docChangeFunction.Name) ||
										!String.Equals(docOriginalFunction.Documentation, docChangeFunction.Documentation) ||
										!String.Equals(docOriginalFunction.Expression, docChangeFunction.Expression))
									{
										tnType = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalFunction.Name, docOriginalFunction, docChangeFunction));
									}
								}
								else
								{
									// new attribute
									this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docChangeFunction.Name, null, docChangeFunction));
								}
							}

							foreach (DocPropertySet docChangePset in docChangeSchema.PropertySets)
							{
								DocObject docOriginalPset = null;
								if (mapOriginal.TryGetValue(docChangePset.Uuid, out docOriginalPset))
								{
									TreeNode tnPset = null;
									if (!String.Equals(docOriginalPset.Documentation, docChangePset.Documentation))
									{
										tnPset = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name, docOriginalPset, docChangePset));
									}

									foreach (DocProperty docChangeProp in docChangePset.Properties)
									{
										DocObject docOriginalProp = ((DocPropertySet)docOriginalPset).GetProperty(docChangeProp.Name);
										if (docOriginalProp != null)
										{
											TreeNode tnProperty = null;
											if (!String.Equals(docOriginalProp.Documentation, docChangeProp.Documentation))
											{
												if (tnPset == null)
												{
													tnPset = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name, docOriginalPset, docChangePset));
												}

												tnProperty = this.AddNode(tnPset, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name + "." + docOriginalProp.Name, docOriginalProp, docChangeProp));
											}

											// localization
											foreach (DocLocalization docChangeLocal in docChangeProp.Localization)
											{
												DocLocalization docOriginalLocal = docOriginalProp.GetLocalization(docChangeLocal.Locale);
												if (docOriginalLocal != null)
												{
													if (!String.Equals(docOriginalLocal.Documentation, docChangeLocal.Documentation))
													{
														if (tnPset == null)
														{
															tnPset = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name, docOriginalPset, docChangePset));
														}

														if (tnProperty == null)
														{
															tnProperty = this.AddNode(tnPset, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name + "." + docOriginalProp.Name, docOriginalProp, docChangeProp));
														}

														this.AddNode(tnProperty, new ChangeInfo(docChangeLocal.Locale, docOriginalLocal, docChangeLocal));
													}
												}
												else
												{
													if (tnPset == null)
													{
														tnPset = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name, docOriginalPset, docChangePset));
													}

													if (tnProperty == null)
													{
														tnProperty = this.AddNode(tnPset, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name + "." + docOriginalProp.Name, docOriginalProp, docChangeProp));
													}

													// new localization
													this.AddNode(tnProperty, new ChangeInfo(docChangeLocal.Locale, docOriginalLocal, docChangeLocal));
												}
											}
										}
										else
										{
											if (tnPset == null)
											{
												tnPset = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name, docOriginalPset, docChangePset));
											}

											// NEW:
											this.AddNode(tnPset, new ChangeInfo(docChangeSchema.Name + "." + docChangePset.Name + "." + docChangeProp.Name, null, docChangeProp));
										}
									}
								}
								else
								{
									// NEW:
									this.AddNode(tnSchema, new ChangeInfo(docChangeSchema.Name + "." + docChangePset.Name, null, docChangePset));
								}

							}

							foreach (DocQuantitySet docChangePset in docChangeSchema.QuantitySets)
							{
								DocObject docOriginalPset = null;
								if (mapOriginal.TryGetValue(docChangePset.Uuid, out docOriginalPset))
								{
									TreeNode tnQset = null;
									if (!String.Equals(docOriginalPset.Documentation, docChangePset.Documentation))
									{
										tnQset = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name, docOriginalPset, docChangePset));
									}

									foreach (DocQuantity docChangeProp in docChangePset.Quantities)
									{
										DocObject docOriginalProp = ((DocQuantitySet)docOriginalPset).GetQuantity(docChangeProp.Name);
										if (docOriginalProp != null)
										{
											if (!String.Equals(docOriginalProp.Documentation, docChangeProp.Documentation))
											{
												if (tnQset == null)
												{
													tnQset = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name, docOriginalPset, docChangePset));
												}
												this.AddNode(tnQset, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name + "." + docOriginalProp.Name, docOriginalProp, docChangeProp));
											}
										}
										else
										{
											if (tnQset == null)
											{
												tnQset = this.AddNode(tnSchema, new ChangeInfo(docOriginalSchema.Name + "." + docOriginalPset.Name, docOriginalPset, docChangePset));
											}

											// NEW:
											this.AddNode(tnQset, new ChangeInfo(docChangeSchema.Name + "." + docChangePset.Name + "." + docChangeProp.Name, null, docChangeProp));
										}
									}

								}
								else
								{
									// NEW:
									this.AddNode(tnSchema, new ChangeInfo(docChangeSchema.Name + "." + docChangePset.Name, null, docChangePset));
								}
							}

						}
					}
				}
			}

			if (this.treeView.Nodes.Count > 0)
			{
				this.treeView.SelectedNode = this.treeView.Nodes[0];
			}
		}

		private void SaveNode(TreeNode tn)
		{
			ChangeInfo info = (ChangeInfo)tn.Tag;
			if (tn.Checked)
			{
				if (info.Original == null)
				{
					// add new item
					if (info.Change is DocLocalization)
					{
						DocLocalization localChange = (DocLocalization)info.Change;

						ChangeInfo parentinfo = (ChangeInfo)tn.Parent.Tag;
						DocObject docObj = (DocObject)parentinfo.Original;
						docObj.RegisterLocalization(localChange.Locale, localChange.Name, localChange.Documentation);
					}
					else if (info.Change is DocAttribute)
					{
						DocAttribute localAttr = (DocAttribute)info.Change;

						ChangeInfo parentinfo = (ChangeInfo)tn.Parent.Tag;
						DocEntity docEntity = (DocEntity)parentinfo.Original;

						DocAttribute docAttr = (DocAttribute)localAttr.Clone();
						docEntity.Attributes.Add(docAttr);
					}
					else if (info.Change is DocWhereRule)
					{
						DocWhereRule localAttr = (DocWhereRule)info.Change;

						ChangeInfo parentinfo = (ChangeInfo)tn.Parent.Tag;
						DocEntity docEntity = (DocEntity)parentinfo.Original;

						DocWhereRule docAttr = (DocWhereRule)localAttr.Clone();
						docEntity.WhereRules.Add(docAttr);
					}
					else if (info.Change is DocFunction)
					{
						DocFunction localAttr = (DocFunction)info.Change;

						ChangeInfo parentinfo = (ChangeInfo)tn.Parent.Tag;
						DocSchema docSchema = (DocSchema)parentinfo.Original;

						DocFunction docAttr = (DocFunction)localAttr.Clone();
						docSchema.Functions.Add(docAttr);
					}
					else if (info.Change is DocConstant)
					{
						this.ToString();
					}
					else if (info.Change is DocProperty)
					{
						this.ToString();

						DocProperty localProp = (DocProperty)info.Change;

						ChangeInfo parentinfo = (ChangeInfo)tn.Parent.Tag;
						DocPropertySet docPset = (DocPropertySet)parentinfo.Original;

						DocProperty docProperty = (DocProperty)localProp.Clone();
						docPset.Properties.Add(docProperty);
					}
					else if (info.Change is DocPropertyConstant)
					{
						this.ToString();

						DocPropertyConstant localProp = (DocPropertyConstant)info.Change;

						ChangeInfo parentinfo = (ChangeInfo)tn.Parent.Tag;
						DocPropertyEnumeration docPset = (DocPropertyEnumeration)parentinfo.Original;

						DocPropertyEnumeration docEnumChange = (DocPropertyEnumeration)parentinfo.Change;
						int index = docEnumChange.Constants.IndexOf(localProp);

						DocPropertyConstant docProperty = (DocPropertyConstant)localProp.Clone();
						docPset.Constants.Insert(index, docProperty);
					}
					else
					{
						this.ToString();
					}
				}
				else if (info.Change == null)
				{
					// removal of definition
					if (info.Original is DocAttribute)
					{
						DocAttribute docAttr = (DocAttribute)info.Original;
						ChangeInfo parentinfo = (ChangeInfo)tn.Parent.Tag;
						DocEntity docEntity = (DocEntity)parentinfo.Original;

						docEntity.Attributes.Remove(docAttr);
						docAttr.Delete();
					}
					else
					{
						this.ToString();
					}
				}
				else
				{
					// change of documentation
					info.Original.Name = info.Change.Name;
					info.Original.Documentation = info.Change.Documentation;

					if (info.Original is DocWhereRule)
					{
						DocWhereRule whereOriginal = (DocWhereRule)info.Original;
						DocWhereRule whereChange = (DocWhereRule)info.Change;
						whereOriginal.Expression = whereChange.Expression;
					}
					else if (info.Original is DocFunction)
					{
						DocFunction whereOriginal = (DocFunction)info.Original;
						DocFunction whereChange = (DocFunction)info.Change;
						whereOriginal.Expression = whereChange.Expression;
					}
				}
			}

			foreach (TreeNode tnSub in tn.Nodes)
			{
				SaveNode(tnSub);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			if (this.DialogResult == System.Windows.Forms.DialogResult.OK)
			{
				foreach (TreeNode tn in this.treeView.Nodes)
				{
					SaveNode(tn);
				}
			}
		}

		private void radioButtonOriginal_Click(object sender, EventArgs e)
		{
			this.treeView.SelectedNode.Checked = false;
		}

		private void radioButtonChange_Click(object sender, EventArgs e)
		{
			this.treeView.SelectedNode.Checked = true;
		}

		private void UpdateNode(TreeNode tn)
		{
			ChangeInfo changeinfo = (ChangeInfo)tn.Tag;
			if (changeinfo.Change != null)
			{
				tn.Text = changeinfo.Change.Name;
			}
			else if (changeinfo.Original != null)
			{
				tn.Text = changeinfo.Original.Name;
			}

			// back color indicates change type
			if (changeinfo.Original == null)
			{
				// addition
				tn.BackColor = Color.Lime;
			}
			else if (changeinfo.Change == null)
			{
				// deletion
				tn.BackColor = Color.Red;
			}
			else if (changeinfo.Original.Documentation != changeinfo.Change.Documentation ||
				(changeinfo.Original.Documentation != null && !changeinfo.Original.Documentation.Equals(changeinfo.Change.Documentation)))
			{
				// update
				tn.BackColor = Color.Yellow;
			}
			else
			{
				tn.BackColor = Color.Empty;
			}

			// checkbox indicates accept or reject
			//tn.Checked = changeinfo.Accept;
		}

		private TreeNode AddNode(TreeNode tnParent, ChangeInfo changeinfo)
		{
			TreeNode tn = new TreeNode();
			tn.Tag = changeinfo;
			UpdateNode(tn);

			if (tnParent != null)
			{
				tnParent.Nodes.Add(tn);
			}
			else
			{
				this.treeView.Nodes.Add(tn);
			}

			return tn;
		}

		private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			this.lvSource.Items.Clear();
			this.lvDestination.Items.Clear();

			ChangeInfo info = (ChangeInfo)this.treeView.SelectedNode.Tag;

			DiffList_TextString source = new DiffList_TextString(info.Original != null ? info.Original.Documentation : String.Empty);
			DiffList_TextString destination = new DiffList_TextString(info.Change != null ? info.Change.Documentation : String.Empty);
			DiffEngine de = new DiffEngine();
			de.ProcessDiff(source, destination, DiffEngineLevel.SlowPerfect);
			ArrayList DiffLines = de.DiffReport();

			ListViewItem lviS;
			ListViewItem lviD;
			int cnt = 1;
			int i;

			foreach (DiffResultSpan drs in DiffLines)
			{
				switch (drs.Status)
				{
					case DiffResultSpanStatus.DeleteSource:
						for (i = 0; i < drs.Length; i++)
						{
							lviS = new ListViewItem(cnt.ToString("00000"));
							lviD = new ListViewItem(cnt.ToString("00000"));
							lviS.BackColor = Color.Red;
							lviS.SubItems.Add(((TextLine)source.GetByIndex(drs.SourceIndex + i)).Line);
							lviD.BackColor = Color.LightGray;
							lviD.SubItems.Add("");

							lvSource.Items.Add(lviS);
							lvDestination.Items.Add(lviD);
							cnt++;
						}

						break;
					case DiffResultSpanStatus.NoChange:
						for (i = 0; i < drs.Length; i++)
						{
							lviS = new ListViewItem(cnt.ToString("00000"));
							lviD = new ListViewItem(cnt.ToString("00000"));
							lviS.BackColor = Color.White;
							lviS.SubItems.Add(((TextLine)source.GetByIndex(drs.SourceIndex + i)).Line);
							lviD.BackColor = Color.White;
							lviD.SubItems.Add(((TextLine)destination.GetByIndex(drs.DestIndex + i)).Line);

							lvSource.Items.Add(lviS);
							lvDestination.Items.Add(lviD);
							cnt++;
						}

						break;
					case DiffResultSpanStatus.AddDestination:
						for (i = 0; i < drs.Length; i++)
						{
							lviS = new ListViewItem(cnt.ToString("00000"));
							lviD = new ListViewItem(cnt.ToString("00000"));
							lviS.BackColor = Color.LightGray;
							lviS.SubItems.Add("");
							lviD.BackColor = Color.LightGreen;
							lviD.SubItems.Add(((TextLine)destination.GetByIndex(drs.DestIndex + i)).Line);

							lvSource.Items.Add(lviS);
							lvDestination.Items.Add(lviD);
							cnt++;
						}

						break;
					case DiffResultSpanStatus.Replace:
						for (i = 0; i < drs.Length; i++)
						{
							lviS = new ListViewItem(cnt.ToString("00000"));
							lviD = new ListViewItem(cnt.ToString("00000"));
							lviS.BackColor = Color.Red;
							lviS.SubItems.Add(((TextLine)source.GetByIndex(drs.SourceIndex + i)).Line);
							lviD.BackColor = Color.LightGreen;
							lviD.SubItems.Add(((TextLine)destination.GetByIndex(drs.DestIndex + i)).Line);

							lvSource.Items.Add(lviS);
							lvDestination.Items.Add(lviD);
							cnt++;
						}

						break;
				}

			}


			this.radioButtonOriginal.Checked = !this.treeView.SelectedNode.Checked;
			this.radioButtonChange.Checked = this.treeView.SelectedNode.Checked;
		}

		private void treeView_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (m_ignore)
				return;

			m_ignore = true;

			this.radioButtonOriginal.Checked = !this.treeView.SelectedNode.Checked;
			this.radioButtonChange.Checked = this.treeView.SelectedNode.Checked;

			CheckNode(e.Node, e.Node.Checked);

			m_ignore = false;
		}

		/// <summary>
		/// Recurses through all tree nodes setting check state
		/// </summary>
		/// <param name="tn"></param>
		private void CheckNode(TreeNode tn, bool state)
		{
			tn.Checked = state;
			foreach (TreeNode tnSub in tn.Nodes)
			{
				CheckNode(tnSub, state);
			}
		}

		private void lvSource_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lvSource.SelectedItems.Count > 0)
			{
				ListViewItem lvi = lvDestination.Items[lvSource.SelectedItems[0].Index];
				lvi.Selected = true;
				lvi.EnsureVisible();
			}
		}

		private void lvDestination_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lvDestination.SelectedItems.Count > 0)
			{
				ListViewItem lvi = lvSource.Items[lvDestination.SelectedItems[0].Index];
				lvi.Selected = true;
				lvi.EnsureVisible();
			}
		}

		private void lvSource_Resize(object sender, EventArgs e)
		{
			if (lvSource.Width > 100)
			{
				lvSource.Columns[1].Width = -2;
			}
		}

		private void lvDestination_Resize(object sender, EventArgs e)
		{
			if (lvDestination.Width > 100)
			{
				lvDestination.Columns[1].Width = -2;
			}
		}
	}

	public class ChangeInfo
	{
		string m_caption;
		IDocumentation m_original;
		IDocumentation m_change;
		bool m_accept;

		public ChangeInfo(string caption, IDocumentation docOriginal, IDocumentation docChange)
		{
			m_caption = caption;
			m_original = docOriginal;
			m_change = docChange;
			m_accept = false;
		}

		public override string ToString()
		{
			string action = ": ";
			if (m_original == null)
			{
				action = "+ ";
			}
			else if (m_change == null)
			{
				action = "- ";
			}

			if (m_accept)
			{
				return action + m_caption + " [ACCEPT]";
			}
			else
			{
				return action + m_caption;
			}
		}

		public IDocumentation Original
		{
			get
			{
				return m_original;
			}
		}

		public IDocumentation Change
		{
			get
			{
				return m_change;
			}
		}

		/*
        public bool Accept
        {
            get
            {
                return this.m_accept;
            }
            set
            {
                this.m_accept = value;
            }
        }
         */
	}
}
