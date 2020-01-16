using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class FormValidateMappings : Form
	{
		DocProject m_project;
		DocModelView m_view;
		Dictionary<string, DocObject> m_mapEntity;
		Dictionary<long, object> m_instances;

		public FormValidateMappings()
		{
			InitializeComponent();
		}

		public FormValidateMappings(DocProject docProject, DocModelView docView, Dictionary<string, DocObject> mapEntity, Dictionary<long, object> instances) : this()
		{
			this.m_project = docProject;
			this.m_view = docView;
			this.m_mapEntity = mapEntity;
			this.m_instances = instances;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			this.tabControl.TabPages.Clear();

			foreach (DocConceptRoot docRoot in this.m_view.ConceptRoots)
			{
				foreach (DocTemplateUsage docConcept in docRoot.Concepts)
				{
					if (docConcept.Definition != null && docConcept.Definition.Uuid == DocTemplateDefinition.guidTemplateMapping)
					{
						TabPage tabPage = new TabPage();
						tabPage.Text = docRoot.Name;
						tabPage.Tag = docConcept;
						this.tabControl.TabPages.Add(tabPage);

						ListView listView = new ListView();
						tabPage.Controls.Add(listView);
						listView.Dock = DockStyle.Fill;
						listView.View = View.Details;
						listView.FullRowSelect = true;

						ColumnHeader colHeaderIndex = new ColumnHeader();
						colHeaderIndex.Text = "Index";
						colHeaderIndex.Width = 100;
						listView.Columns.Add(colHeaderIndex);

						List<CvtValuePath> colmaps = new List<CvtValuePath>();

						foreach (DocTemplateItem docItem in docConcept.Items)
						{
							string expr = docItem.GetParameterValue("Reference");
							CvtValuePath valpath = CvtValuePath.Parse(expr, this.m_mapEntity);
							colmaps.Add(valpath);

							ColumnHeader columnHeader = new ColumnHeader();
							columnHeader.Text = docItem.GetParameterValue("Name");
							columnHeader.Tag = docItem;
							columnHeader.Width = 100;
							listView.Columns.Add(columnHeader);
						}

						// load instance data
						foreach (object instance in this.m_instances.Values)
						{
							string eachname = instance.GetType().Name;
							if (docRoot.ApplicableEntity != null && docRoot.ApplicableEntity.IsInstanceOfType(instance))
							{
								ListViewItem lvi = new ListViewItem();
								lvi.Text = (listView.Items.Count + 1).ToString();
								listView.Items.Add(lvi);

								int iCol = 0;
								foreach (DocTemplateItem docItem in docConcept.Items)
								{
									CvtValuePath valpath = colmaps[iCol];
									iCol++;

									object value = valpath.GetValue((SEntity)instance, null);

									if (value == instance)
									{
										value = e.GetType().Name;
									}
									else if (value is SEntity)
									{
										// use name
										FieldInfo fieldValue = value.GetType().GetField("Name");
										if (fieldValue != null)
										{
											value = fieldValue.GetValue(value);
										}
									}
									else if (value is System.Collections.IList)
									{
										System.Collections.IList list = (System.Collections.IList)value;
										StringBuilder sbList = new StringBuilder();
										foreach (object elem in list)
										{
											FieldInfo fieldName = elem.GetType().GetField("Name");
											if (fieldName != null)
											{
												object elemname = fieldName.GetValue(elem);
												if (elemname != null)
												{
													FieldInfo fieldValue = elemname.GetType().GetField("Value");
													if (fieldValue != null)
													{
														object elemval = fieldValue.GetValue(elemname);
														sbList.Append(elemval.ToString());
													}
												}
											}
											sbList.Append("; <br/>");
										}
										value = sbList.ToString();
									}
									else if (value is Type)
									{
										value = ((Type)value).Name;
									}

#if false
                                    if (!String.IsNullOrEmpty(format))
                                    {
                                        if (format.Equals("Required") && value == null)
                                        {
                                            includerow = false;
                                        }
                                    }
#endif
									string text = String.Empty;
									if (value != null)
									{
										FieldInfo fieldValue = value.GetType().GetField("Value");
										if (fieldValue != null)
										{
											value = fieldValue.GetValue(value);
										}

#if false
                                        if (format != null && format.Equals("True") && (value == null || !value.ToString().Equals("True")))
                                        {
                                            includerow = false;
                                        }
#endif
										if (value is Double)
										{
											text = ((Double)value).ToString("N3");
										}
										else if (value is List<Int64>)
										{
											// latitude or longitude
											List<Int64> intlist = (List<Int64>)value;
											if (intlist.Count >= 3)
											{
												text = intlist[0] + "° " + intlist[1] + "' " + intlist[2] + "\"";
											}
										}
										else if (value != null)
										{
											text = value.ToString();
										}
									}

									lvi.SubItems.Add(text);
								}
							}

						}
					}
				}
			}

		}
	}
}
