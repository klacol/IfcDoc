using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class CtlOperators : UserControl
	{
		private DocProject m_project;
		private DocTemplateDefinition m_template;
		private DocModelRule m_modelrule; // relative rule for which to add constraints
		private object m_instance;
		private object[] m_population; // all instances for which rule is applicable, to check uniqueness

		public CtlOperators()
		{
			InitializeComponent();
		}

		public DocProject Project
		{
			get
			{
				return this.m_project;
			}
			set
			{
				this.m_project = value;
			}
		}

		public DocTemplateDefinition Template
		{
			get
			{
				return this.m_template;
			}
			set
			{
				this.m_template = value;

				this.LoadRules(null);
			}
		}

		public DocModelRule Rule
		{
			get
			{
				return this.m_modelrule;
			}
			set
			{
				this.m_modelrule = value;

				// update
				treeViewRules_AfterSelect(this, new TreeViewEventArgs(null));
			}
		}

		public object[] CurrentPopulation
		{
			get
			{
				return this.m_population;
			}
			set
			{
				this.m_population = value;
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

				foreach (TreeNode tn in this.treeViewRules.Nodes)
				{
					ValidateNode(tn);
				}
			}
		}

		public void DoInsert()
		{
			if (this.Template == null)
				return;

			DocModelRule docModelRule = this.Rule;
			if (docModelRule == null)
				return;

			if (!(this.Rule is DocModelRuleEntity))
				return;

			TreeNode tnSelect = this.treeViewRules.SelectedNode;

			DocOpLiteral oplit = new DocOpLiteral();
			oplit.Operation = DocOpCode.LoadString;
			oplit.Literal = null;

			DocOpStatement op = new DocOpStatement();
			op.Operation = DocOpCode.CompareEqual;
			op.Value = oplit;

			DocOpReference opref = new DocOpReference();
			opref.Operation = DocOpCode.NoOperation; // ldfld...
			opref.EntityRule = this.Rule as DocModelRuleEntity;
			op.Reference = opref;

			if (tnSelect != null)
			{
				DocOpExpression opSelect = tnSelect.Tag as DocOpExpression;
				if (tnSelect.Tag is DocModelRuleConstraint)
				{
					opSelect = ((DocModelRuleConstraint)tnSelect.Tag).Expression;

				}


				// convert existing node into new Logical operator

				DocOpLogical opLog = new DocOpLogical();
				opLog.Operation = DocOpCode.And;
				opLog.ExpressionA = opSelect;
				opLog.ExpressionB = op;

				if (tnSelect.Tag is DocModelRuleConstraint)
				{
					DocModelRuleConstraint dmr = (DocModelRuleConstraint)tnSelect.Tag;
					dmr.Expression = opLog;
				}
				else if (tnSelect.Parent != null)
				{
					DocOpLogical opParent = tnSelect.Parent.Tag as DocOpLogical;
					if (tnSelect.Parent.Tag is DocModelRuleConstraint)
					{
						opParent = ((DocModelRuleConstraint)tnSelect.Parent.Tag).Expression as DocOpLogical;
					}

					if (tnSelect.Parent.Nodes[0] == tnSelect)
					{
						opParent.ExpressionA = opLog;
					}
					else if (tnSelect.Parent.Nodes[1] == tnSelect)
					{
						opParent.ExpressionB = opLog;
					}
				}
				else if (tnSelect.Parent != null && tnSelect.Parent.Nodes[1] == tnSelect)
				{
					DocOpLogical opParent = (DocOpLogical)tnSelect.Parent.Tag;
					opParent.ExpressionB = opLog;
				}

				this.LoadRules(op);
			}
			else
			{
				// create new constraint
				DocModelRuleConstraint docCon = new DocModelRuleConstraint();
				docCon.Expression = op;
				docModelRule.Rules.Add(docCon);
				docCon.ParentRule = docModelRule;

				TreeNode tnCon = new TreeNode();
				tnCon.Tag = docCon;
				tnCon.Text = op.ToString(this.Template);
				tnCon.ImageIndex = 3;
				tnCon.SelectedImageIndex = tnCon.ImageIndex;
				this.treeViewRules.Nodes.Add(tnCon);

				this.treeViewRules.SelectedNode = tnCon;
			}

		}

		private void LoadRules(DocOp selection)
		{
			this.treeViewRules.BeginUpdate();
			this.treeViewRules.Nodes.Clear();
			if (this.m_template != null)
			{
				this.LoadRules(this.Template.Rules, null);
				this.treeViewRules.ExpandAll();
				this.treeViewRules_AfterSelect(this.treeViewRules, new TreeViewEventArgs(null));

				foreach (TreeNode tn in this.treeViewRules.Nodes)
				{
					SelectRule(tn, selection);
				}
			}
			this.treeViewRules.EndUpdate();

			// select nothing such that color is displayed for validation
			this.treeViewRules.SelectedNode = null;
		}

		private void LoadRules(List<DocModelRule> list, DocModelRule parentrule)
		{
			if (list == null)
				return;

			foreach (DocModelRule docRule in list)
			{
				if (docRule is DocModelRuleConstraint)
				{
					DocModelRuleConstraint rc = (DocModelRuleConstraint)docRule;
					TreeNode tn = new TreeNode();
					tn.Tag = docRule;
					tn.Text = docRule.Description;
					tn.ImageIndex = 4;
					this.treeViewRules.Nodes.Add(tn);

					// backward compatibility before V6.1 -- generate rule from syntax
					if (rc.Expression == null && docRule.Description != null)
					{
						DocOpReference reference = new DocOpReference();
						reference.EntityRule = parentrule as DocModelRuleEntity;

						DocOpLiteral literal = new DocOpLiteral();

						DocOpStatement statement = new DocOpStatement();
						statement.Reference = reference;
						statement.Value = literal;
						rc.Expression = statement;

						string value = docRule.Description;
						int index = value.IndexOfAny(new char[] { '=', '!', '>', '<' });
						if (index > 0)
						{
							string metric = value.Substring(0, index);
							switch (metric)
							{
								case "Value":
									///...
									break;

								case "Length":
									///...
									break;
							}

							string oper = value.Substring(index, 1);
							string bench = value.Substring(index + 1);
							if (value.Length > index + 1 && value[index + 1] == '=')
							{
								oper = value.Substring(index, 2);
								bench = value.Substring(index + 2);
							}

							switch (oper)
							{
								case "=":
									statement.Operation = DocOpCode.CompareEqual;
									break;

								case "!=":
									statement.Operation = DocOpCode.CompareNotEqual;
									break;

								case "<=":
									statement.Operation = DocOpCode.CompareLessThanOrEqual;
									break;

								case "<":
									statement.Operation = DocOpCode.CompareLessThan;
									break;

								case ">=":
									statement.Operation = DocOpCode.CompareGreaterThanOrEqual;
									break;

								case ">":
									statement.Operation = DocOpCode.CompareGreaterThan;
									break;
							}

							literal.Literal = bench;
						}
					}

					LoadOpExpression(tn, rc.Expression);
				}

				LoadRules(docRule.Rules, docRule);
			}
		}

		private void LoadOpExpression(TreeNode tn, DocOp op)
		{
			if (op == null)
				return;

			if (tn.Tag == null)
			{
				tn.Tag = op;
			}
			tn.Text = op.ToString(this.Template);
			tn.ImageIndex = 3;
			tn.SelectedImageIndex = tn.ImageIndex;

			if (op is DocOpLogical)
			{
				DocOpLogical oplog = (DocOpLogical)op;
				tn.ImageIndex = 4;
				tn.SelectedImageIndex = tn.ImageIndex;

				TreeNode tnA = new TreeNode();
				LoadOpExpression(tnA, oplog.ExpressionA);
				tn.Nodes.Add(tnA);

				TreeNode tnB = new TreeNode();
				LoadOpExpression(tnB, oplog.ExpressionB);
				tn.Nodes.Add(tnB);
			}
		}

		private void SelectRule(TreeNode tn, DocOp selection)
		{
			if (tn.Tag == selection)
			{
				this.treeViewRules.SelectedNode = tn;
				return;
			}
			else if (tn.Tag is DocModelRuleConstraint)
			{
				DocModelRuleConstraint dmc = (DocModelRuleConstraint)tn.Tag;
				if (dmc.Expression == selection)
				{
					this.treeViewRules.SelectedNode = tn;
				}
			}

			foreach (TreeNode ts in tn.Nodes)
			{
				SelectRule(ts, selection);
			}
		}

		private void treeViewRules_AfterSelect(object sender, TreeViewEventArgs e)
		{
			TreeNode tn = this.treeViewRules.SelectedNode;

			DocOp op = this.GetSelectedOp();

			if (this.Template == null)
			{
				foreach (ToolStripItem tsi in this.toolStripConceptRule.Items)
				{
					tsi.Enabled = false;

					if (tsi is ToolStripButton)
					{
						((ToolStripButton)tsi).Checked = false;
					}
				}
				return;
			}

			this.toolStripButtonRuleInsert.Enabled = (this.Rule is DocModelRuleEntity);
			this.toolStripButtonRuleRemove.Enabled = (tn != null);
			this.toolStripButtonRuleUpdate.Enabled = (op is DocOpStatement);
			this.toolStripButtonRuleRef.Enabled = (op is DocOpStatement && this.Rule != null);

			this.toolStripButtonRuleAnd.Enabled = (op is DocOpLogical);
			this.toolStripButtonRuleOr.Enabled = (op is DocOpLogical);
			this.toolStripButtonRuleXor.Enabled = (op is DocOpLogical);

			this.toolStripButtonRuleCeq.Enabled = (op is DocOpStatement);
			this.toolStripButtonRuleCne.Enabled = (op is DocOpStatement);
			this.toolStripButtonRuleCle.Enabled = (op is DocOpStatement);
			this.toolStripButtonRuleClt.Enabled = (op is DocOpStatement);
			this.toolStripButtonRuleCge.Enabled = (op is DocOpStatement);
			this.toolStripButtonRuleCgt.Enabled = (op is DocOpStatement);
			this.toolStripButtonRuleIncludes.Enabled = (op is DocOpStatement);

			this.toolStripButtonRuleAnd.Checked = (op != null && op.Operation == DocOpCode.And);
			this.toolStripButtonRuleOr.Checked = (op != null && op.Operation == DocOpCode.Or);
			this.toolStripButtonRuleXor.Checked = (op != null && op.Operation == DocOpCode.Xor);

			this.toolStripButtonRuleCeq.Checked = (op != null && op.Operation == DocOpCode.CompareEqual);
			this.toolStripButtonRuleCne.Checked = (op != null && op.Operation == DocOpCode.CompareNotEqual);
			this.toolStripButtonRuleCle.Checked = (op != null && op.Operation == DocOpCode.CompareLessThanOrEqual);
			this.toolStripButtonRuleClt.Checked = (op != null && op.Operation == DocOpCode.CompareLessThan);
			this.toolStripButtonRuleCge.Checked = (op != null && op.Operation == DocOpCode.CompareGreaterThanOrEqual);
			this.toolStripButtonRuleCgt.Checked = (op != null && op.Operation == DocOpCode.CompareGreaterThan);

			this.toolStripButtonRuleIncludes.Checked = (op != null && op.Operation == DocOpCode.IsIncluded);
		}

		private void treeViewRules_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			// launch literal editor
			toolStripButtonRuleUpdate_Click(null, EventArgs.Empty);
		}

		private void treeViewRules_MouseDown(object sender, MouseEventArgs e)
		{
			if (this.treeViewRules.HitTest(e.Location).Node == null)
			{
				this.treeViewRules.SelectedNode = null;
				treeViewRules_AfterSelect(this.treeViewRules, new TreeViewEventArgs(null));
			}
		}

		private void toolStripButtonRuleInsert_Click(object sender, EventArgs e)
		{
			this.DoInsert();
		}

		private void toolStripButtonRuleRemove_Click(object sender, EventArgs e)
		{
			TreeNode tn = this.treeViewRules.SelectedNode;
			if (tn == null)
				return;

			if (tn.Tag is DocModelRuleConstraint)
			{
				DocModelRuleConstraint docConstraint = (DocModelRuleConstraint)tn.Tag;
				docConstraint.Delete();
				this.RemoveRuleConstraint(this.Template.Rules, docConstraint);
				this.LoadRules(null);
			}
			else if (tn.Tag is DocOp)
			{
				// dissolve parent Logical operator, move other statement into parent

				DocOp op = (DocOp)tn.Tag;

				TreeNode tnParent = tn.Parent;
				DocOpLogical oplog = tnParent.Tag as DocOpLogical;
				if (tnParent.Tag is DocModelRuleConstraint)
				{
					DocModelRuleConstraint dmr = (DocModelRuleConstraint)tnParent.Tag;
					oplog = dmr.Expression as DocOpLogical;
				}

				TreeNode tnGrand = tnParent.Parent;

				DocOpExpression opOther = null;
				if (oplog.ExpressionA == op)
				{
					opOther = oplog.ExpressionB;
					oplog.ExpressionB = null; // prevent deletion
				}
				else if (oplog.ExpressionB == op)
				{
					opOther = oplog.ExpressionA;
					oplog.ExpressionA = null; // prevent deletion
				}

				if (tnParent.Tag is DocModelRuleConstraint)
				{
					DocModelRuleConstraint dmr = (DocModelRuleConstraint)tnParent.Tag;
					dmr.Expression = opOther;
				}
				else if (tnGrand != null)
				{
					DocOpLogical opGrand = tnGrand.Tag as DocOpLogical;
					if (tnGrand.Tag is DocModelRuleConstraint)
					{
						DocModelRuleConstraint dmr = (DocModelRuleConstraint)tnGrand.Tag;
						opGrand = dmr.Expression as DocOpLogical;
					}

					if (opGrand.ExpressionA == oplog)
					{
						opGrand.ExpressionA = opOther;
					}
					else if (opGrand.ExpressionB == oplog)
					{
						opGrand.ExpressionB = opOther;
					}
				}

				oplog.Delete();
				op.Delete();
				this.LoadRules(opOther);
			}
		}

		private void RemoveRuleConstraint(List<DocModelRule> list, DocModelRuleConstraint ruleCon)
		{
			if (list.Contains(ruleCon))
			{
				list.Remove(ruleCon);
			}

			foreach (DocModelRule sub in list)
			{
				if (sub.Rules != null)
				{
					RemoveRuleConstraint(sub.Rules, ruleCon);
				}
			}
		}

		private DocOp GetSelectedOp()
		{
			TreeNode tn = this.treeViewRules.SelectedNode;
			if (tn == null)
				return null;

			DocOp docop = tn.Tag as DocOp;
			if (tn.Tag is DocModelRuleConstraint)
			{
				DocModelRuleConstraint dmr = (DocModelRuleConstraint)tn.Tag;
				docop = dmr.Expression;
			}

			return docop;
		}


		private void toolStripButtonRuleUpdate_Click(object sender, EventArgs e)
		{
			DocOpStatement docop = GetSelectedOp() as DocOpStatement;
			if (docop == null)
				return;

			// enumeration|boolean|logical: pick from list
			// string|integer|real: freeform editor

			using (FormConstraint form = new FormConstraint())
			{
				form.DataType = this.m_project.GetDefinition(docop.Reference.EntityRule.Name) as DocType;
				if (form.DataType == null)
				{
					try
					{
						form.ExpressType = (DocExpressType)Enum.Parse(typeof(DocExpressType), docop.Reference.EntityRule.Name);
					}
					catch
					{

					}
				}

				form.Metric = docop.Metric;
				form.Operation = docop.Operation;

				if (docop.Value is DocOpLiteral)
				{
					form.Literal = ((DocOpLiteral)docop.Value).Literal;
				}
				if (form.ShowDialog(this) == DialogResult.OK)
				{
					docop.Operation = form.Operation;
					docop.Metric = form.Metric;

					if (!(docop.Value is DocOpLiteral))
					{
						docop.Value.Delete();
						docop.Value = new DocOpLiteral();
					}

					((DocOpLiteral)docop.Value).Literal = form.Literal;

					this.treeViewRules.SelectedNode.Text = docop.ToString(this.Template);
				}
			}
		}

		private void toolStripButtonRuleRef_Click(object sender, EventArgs e)
		{
			DocOp op = this.GetSelectedOp();
			if (op is DocOpStatement)
			{
				DocOpStatement statement = (DocOpStatement)op;
				if (this.Rule is DocModelRuleAttribute)
				{
					DocOpParameter param = new DocOpParameter();
					param.Operation = DocOpCode.LoadArgument;
					param.AttributeRule = (DocModelRuleAttribute)this.Rule;
					statement.Value = param;
				}
				else if (this.Rule is DocModelRuleEntity)
				{
					DocOpReference opref = new DocOpReference();
					opref.Operation = DocOpCode.LoadField;
					opref.EntityRule = (DocModelRuleEntity)this.Rule;
					statement.Value = opref;
				}

				this.treeViewRules.SelectedNode.Text = op.ToString(this.Template);
			}
		}

		private void toolStripButtonRuleAnd_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.And);
		}

		private void toolStripButtonRuleOr_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.Or);
		}

		private void toolStripButtonRuleXor_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.Xor);
		}

		private void toolStripButtonRuleCeq_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.CompareEqual);
		}

		private void toolStripButtonRuleCne_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.CompareNotEqual);
		}

		private void toolStripButtonRuleCgt_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.CompareGreaterThan);
		}

		private void toolStripButtonRuleCge_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.CompareGreaterThanOrEqual);
		}

		private void toolStripButtonRuleClt_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.CompareLessThan);
		}

		private void toolStripButtonRuleCle_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.CompareLessThanOrEqual);
		}

		private void SetSelectedRuleOperator(DocOpCode opcode)
		{
			TreeNode tn = this.treeViewRules.SelectedNode;
			DocOp docop = this.GetSelectedOp();

			docop.Operation = opcode;
			tn.Text = docop.ToString(this.Template);

			this.treeViewRules_AfterSelect(this.treeViewRules, new TreeViewEventArgs(this.treeViewRules.SelectedNode));
		}

		private void toolStripButtonRuleIncludes_Click(object sender, EventArgs e)
		{
			SetSelectedRuleOperator(DocOpCode.IsIncluded);
		}

		private void ValidateNode(TreeNode tn)
		{
			if (this.m_instance != null)
			{
				DocOp docOp = null;
				if (tn.Tag is DocModelRuleConstraint)
				{
					DocModelRuleConstraint con = (DocModelRuleConstraint)tn.Tag;
					docOp = con.Expression;
				}
				else if (tn.Tag is DocOp)
				{
					docOp = (DocOp)tn.Tag;
				}

				if (docOp != null)
				{
					Hashtable hashtable = new Hashtable();
					object oresult = docOp.Eval(this.m_instance, hashtable, this.m_template, null, null);

					// if hashtable contains a value, that means that entire population must be tested to determine uniqueness
					if (hashtable.Count > 0 && this.m_population != null)
					{
						// must evalulate all for uniqueness
						foreach (object other in this.m_population)
						{
							if (other == this.m_instance) // first instance will pass; following duplicate instances will fail
								break;

							// returning false means there's a duplicate (not unique).
							object otherresult = docOp.Eval(other, hashtable, this.m_template, null, null);
							if (otherresult is bool && !(bool)otherresult)
							{
								oresult = false;
								break;
							}
						}
					}

					if (oresult == null)
					{
						tn.BackColor = Color.Yellow;
					}
					else if (oresult is bool && (bool)oresult)
					{
						tn.BackColor = Color.Lime;
					}
					else if (oresult is bool && !(bool)oresult)
					{
						tn.BackColor = Color.Red;
					}
				}
			}
			else
			{
				tn.BackColor = Color.Empty;
			}

			// recurse
			foreach (TreeNode sub in tn.Nodes)
			{
				ValidateNode(sub);
			}
		}
	}
}
