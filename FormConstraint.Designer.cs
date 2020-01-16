namespace IfcDoc
{
	partial class FormConstraint
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormConstraint));
			this.comboBoxMetric = new System.Windows.Forms.ComboBox();
			this.comboBoxOperator = new System.Windows.Forms.ComboBox();
			this.textBoxBenchmark = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.textBoxExpression = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.comboBoxValue = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// comboBoxMetric
			// 
			resources.ApplyResources(this.comboBoxMetric, "comboBoxMetric");
			this.comboBoxMetric.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxMetric.FormattingEnabled = true;
			this.comboBoxMetric.Items.AddRange(new object[] {
			resources.GetString("comboBoxMetric.Items"),
			resources.GetString("comboBoxMetric.Items1"),
			resources.GetString("comboBoxMetric.Items2"),
			resources.GetString("comboBoxMetric.Items3")});
			this.comboBoxMetric.Name = "comboBoxMetric";
			this.comboBoxMetric.SelectedIndexChanged += new System.EventHandler(this.comboBoxMetric_SelectedIndexChanged);
			// 
			// comboBoxOperator
			// 
			resources.ApplyResources(this.comboBoxOperator, "comboBoxOperator");
			this.comboBoxOperator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxOperator.FormattingEnabled = true;
			this.comboBoxOperator.Items.AddRange(new object[] {
			resources.GetString("comboBoxOperator.Items"),
			resources.GetString("comboBoxOperator.Items1"),
			resources.GetString("comboBoxOperator.Items2"),
			resources.GetString("comboBoxOperator.Items3"),
			resources.GetString("comboBoxOperator.Items4"),
			resources.GetString("comboBoxOperator.Items5"),
			resources.GetString("comboBoxOperator.Items6")});
			this.comboBoxOperator.Name = "comboBoxOperator";
			this.comboBoxOperator.SelectedIndexChanged += new System.EventHandler(this.comboBoxOperator_SelectedIndexChanged);
			// 
			// textBoxBenchmark
			// 
			this.textBoxBenchmark.AcceptsReturn = true;
			resources.ApplyResources(this.textBoxBenchmark, "textBoxBenchmark");
			this.textBoxBenchmark.Name = "textBoxBenchmark";
			this.textBoxBenchmark.TextChanged += new System.EventHandler(this.textBoxBenchmark_TextChanged);
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// buttonOK
			// 
			resources.ApplyResources(this.buttonOK, "buttonOK");
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			resources.ApplyResources(this.buttonCancel, "buttonCancel");
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// textBoxExpression
			// 
			resources.ApplyResources(this.textBoxExpression, "textBoxExpression");
			this.textBoxExpression.Name = "textBoxExpression";
			this.textBoxExpression.ReadOnly = true;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// comboBoxValue
			// 
			resources.ApplyResources(this.comboBoxValue, "comboBoxValue");
			this.comboBoxValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxValue.FormattingEnabled = true;
			this.comboBoxValue.Name = "comboBoxValue";
			this.comboBoxValue.SelectedIndexChanged += new System.EventHandler(this.comboBoxValue_SelectedIndexChanged);
			// 
			// label5
			// 
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			// 
			// FormConstraint
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.label5);
			this.Controls.Add(this.comboBoxValue);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.textBoxExpression);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBoxBenchmark);
			this.Controls.Add(this.comboBoxOperator);
			this.Controls.Add(this.comboBoxMetric);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormConstraint";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox comboBoxMetric;
		private System.Windows.Forms.ComboBox comboBoxOperator;
		private System.Windows.Forms.TextBox textBoxBenchmark;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TextBox textBoxExpression;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox comboBoxValue;
		private System.Windows.Forms.Label label5;
	}
}