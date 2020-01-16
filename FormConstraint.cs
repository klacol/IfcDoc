// Name:        FormCode.cs
// Description: Dialog box for editing constraints on rules
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
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
	public partial class FormConstraint : Form
	{
		DocType m_datatype;
		DocExpressType m_nativetype;

		static string[] s_operators = new string[7]
			{
				"=",
				"!=",
				">",
				">=",
				"<",
				"<=",
				"⊂",
			};

		public FormConstraint()
		{
			InitializeComponent();

			this.comboBoxMetric.SelectedIndex = 0;
			this.comboBoxOperator.SelectedIndex = 0;
		}

		public DocExpressType ExpressType
		{
			get
			{
				return this.m_nativetype;
			}
			set
			{
				this.m_nativetype = value;

				switch (this.m_nativetype)
				{
					case DocExpressType.BOOLEAN:

						this.comboBoxValue.Items.Add("FALSE");
						this.comboBoxValue.Items.Add("TRUE");
						this.comboBoxValue.Items.Add("NULL");
						this.comboBoxValue.Enabled = true;
						break;

					case DocExpressType.LOGICAL:
						this.comboBoxValue.Items.Add("FALSE");
						this.comboBoxValue.Items.Add("TRUE");
						this.comboBoxValue.Items.Add("UNKNOWN");
						this.comboBoxValue.Items.Add("NULL");
						this.comboBoxValue.Enabled = true;
						break;

					case DocExpressType.REAL:
					case DocExpressType.INTEGER:
					case DocExpressType.NUMBER:
					case DocExpressType.STRING:
					case DocExpressType.BINARY:
						this.comboBoxValue.Enabled = false;
						this.textBoxBenchmark.Enabled = true;
						break;
				}

			}
		}

		public DocType DataType
		{
			get
			{
				return this.m_datatype;
			}
			set
			{
				this.m_datatype = value;

				this.textBoxBenchmark.Enabled = true;
				this.comboBoxValue.Items.Clear();
				this.comboBoxValue.Enabled = false;
				if (this.m_datatype is DocEnumeration)
				{
					DocEnumeration docEnum = (DocEnumeration)this.m_datatype;
					foreach (DocConstant docCon in docEnum.Constants)
					{
						this.comboBoxValue.Items.Add(docCon);
					}
					this.comboBoxValue.Enabled = true;
					this.comboBoxValue.Visible = true;
					this.textBoxBenchmark.Enabled = false;
				}
				else if (this.m_datatype is DocDefined)
				{
					DocDefined docDef = (DocDefined)this.m_datatype;
					try
					{
						this.ExpressType = (DocExpressType)Enum.Parse(typeof(DocExpressType), docDef.DefinedType);
					}
					catch
					{

					}
				}
			}
		}

		public DocOpCode Metric
		{
			get
			{
				switch (this.comboBoxMetric.SelectedIndex)
				{
					case 0:
						return DocOpCode.NoOperation;

					case 1:
						return DocOpCode.LoadLength;

					case 2:
						return DocOpCode.IsInstance;

					case 3:
						return DocOpCode.IsUnique;
				}

				return DocOpCode.NoOperation;
			}
			set
			{
				switch (value)
				{
					case DocOpCode.NoOperation:
						this.comboBoxMetric.SelectedIndex = 0;
						break;

					case DocOpCode.LoadLength:
						this.comboBoxMetric.SelectedIndex = 1;
						break;

					case DocOpCode.IsInstance:
						this.comboBoxMetric.SelectedIndex = 2;
						break;

					case DocOpCode.IsUnique:
						this.comboBoxMetric.SelectedIndex = 3;
						break;
				}
			}
		}

		public DocOpCode Operation
		{
			get
			{
				switch (this.comboBoxOperator.SelectedIndex)
				{
					case 0:
						return DocOpCode.CompareEqual;

					case 1:
						return DocOpCode.CompareNotEqual;

					case 2:
						return DocOpCode.CompareGreaterThan;

					case 3:
						return DocOpCode.CompareGreaterThanOrEqual;

					case 4:
						return DocOpCode.CompareLessThan;

					case 5:
						return DocOpCode.CompareLessThanOrEqual;

					case 6:
						return DocOpCode.IsIncluded;
				}

				return DocOpCode.CompareEqual;
			}
			set
			{
				switch (value)
				{
					case DocOpCode.CompareEqual:
						this.comboBoxOperator.SelectedIndex = 0;
						break;
					case DocOpCode.CompareNotEqual:
						this.comboBoxOperator.SelectedIndex = 1;
						break;
					case DocOpCode.CompareGreaterThan:
						this.comboBoxOperator.SelectedIndex = 2;
						break;
					case DocOpCode.CompareGreaterThanOrEqual:
						this.comboBoxOperator.SelectedIndex = 3;
						break;
					case DocOpCode.CompareLessThan:
						this.comboBoxOperator.SelectedIndex = 4;
						break;
					case DocOpCode.CompareLessThanOrEqual:
						this.comboBoxOperator.SelectedIndex = 5;
						break;
					case DocOpCode.IsIncluded:
						this.comboBoxOperator.SelectedIndex = 6;
						break;
				}
			}
		}

		public string Literal
		{
			get
			{
				if (this.comboBoxValue.SelectedItem != null)
				{
					return this.comboBoxValue.Text;
				}

				if (String.IsNullOrEmpty(this.textBoxBenchmark.Text))
					return null;

				return this.textBoxBenchmark.Text;
			}
			set
			{
				this.textBoxBenchmark.Text = value;

				if (this.comboBoxValue.Items.Count > 0)
				{
					foreach (object o in this.comboBoxValue.Items)
					{
						if (o.ToString().Equals(value))
						{
							this.comboBoxValue.SelectedItem = o;
							break;
						}
					}
				}
			}
		}

		public string Expression
		{
			get
			{
				return this.textBoxExpression.Text;
			}
			set
			{
				this.textBoxExpression.Text = value;
#if false
                // parse it out
                if (value != null)
                {
                    int index = value.IndexOfAny(new char[] { '=', '!', '>', '<' });
                    if (index > 0)
                    {
                        string metric = value.Substring(0, index);
                        switch (metric)
                        {
                            case "Value":
                                this.comboBoxMetric.SelectedIndex = 0;
                                break;

                            case "Length":
                                this.comboBoxMetric.SelectedIndex = 1;
                                break;
                        }

                        string oper = value.Substring(index, 1);
                        string bench = value.Substring(index + 1);
                        if (value.Length > index + 1 && value[index + 1] == '=')
                        {
                            oper = value.Substring(index, 2);
                            bench = value.Substring(index + 2);
                        }
                        for (int i = 0; i < s_operators.Length; i++)
                        {
                            if (s_operators[i] == oper)
                            {
                                this.comboBoxOperator.SelectedIndex = i;
                                break;
                            }
                        }

                        this.textBoxBenchmark.Text = bench;
                    }                    
                }
#endif
			}
		}

		private void comboBoxMetric_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateExpression();
		}

		private void comboBoxOperator_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateExpression();
		}

		private void textBoxBenchmark_TextChanged(object sender, EventArgs e)
		{
			UpdateExpression();
		}

		private void UpdateExpression()
		{
			if (this.comboBoxOperator.SelectedIndex == -1)
				return;

			string value = this.textBoxBenchmark.Text;
			if (this.comboBoxValue.Visible)
			{
				value = this.comboBoxValue.Text;
			}
			if (String.IsNullOrEmpty(value))
			{
				value = "NULL";
			}
			else if (this.comboBoxMetric.SelectedIndex == 0)
			{
				value = "'" + value + "'";
			}

			switch (this.comboBoxMetric.SelectedIndex)
			{
				case 0: // value
					this.comboBoxOperator.Enabled = true;
					this.textBoxBenchmark.Enabled = !(this.m_datatype is DocEnumeration);
					this.textBoxExpression.Text = this.comboBoxMetric.Text + s_operators[this.comboBoxOperator.SelectedIndex] + value;
					break;

				case 1: // count
					this.comboBoxOperator.Enabled = true;
					this.textBoxBenchmark.Enabled = true;
					this.textBoxExpression.Text = this.comboBoxMetric.Text + s_operators[this.comboBoxOperator.SelectedIndex] + value;
					break;

				case 2: // type
					this.comboBoxOperator.Enabled = true;
					this.textBoxBenchmark.Enabled = true;
					this.textBoxExpression.Text = this.comboBoxMetric.Text + s_operators[this.comboBoxOperator.SelectedIndex] + value;
					break;

				case 3: // unique
					this.comboBoxOperator.Enabled = false;
					this.textBoxBenchmark.Enabled = false;
					this.textBoxBenchmark.Text = String.Empty;
					this.textBoxExpression.Text = "";
					break;
			}
		}

		private void comboBoxValue_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateExpression();
		}

	}
}
