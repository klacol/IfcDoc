namespace IfcDoc
{
	partial class FormRule
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormRule));
			this.label3 = new System.Windows.Forms.Label();
			this.textBoxIdentifier = new System.Windows.Forms.TextBox();
			this.comboBoxCardinality = new System.Windows.Forms.ComboBox();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.comboBoxUsage = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxPrefix = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// textBoxIdentifier
			// 
			resources.ApplyResources(this.textBoxIdentifier, "textBoxIdentifier");
			this.textBoxIdentifier.Name = "textBoxIdentifier";
			// 
			// comboBoxCardinality
			// 
			resources.ApplyResources(this.comboBoxCardinality, "comboBoxCardinality");
			this.comboBoxCardinality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxCardinality.FormattingEnabled = true;
			this.comboBoxCardinality.Items.AddRange(new object[] {
			resources.GetString("comboBoxCardinality.Items"),
			resources.GetString("comboBoxCardinality.Items1"),
			resources.GetString("comboBoxCardinality.Items2"),
			resources.GetString("comboBoxCardinality.Items3"),
			resources.GetString("comboBoxCardinality.Items4")});
			this.comboBoxCardinality.Name = "comboBoxCardinality";
			this.comboBoxCardinality.SelectedIndexChanged += new System.EventHandler(this.comboBoxCardinality_SelectedIndexChanged);
			// 
			// buttonCancel
			// 
			resources.ApplyResources(this.buttonCancel, "buttonCancel");
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOK
			// 
			resources.ApplyResources(this.buttonOK, "buttonOK");
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// comboBoxUsage
			// 
			resources.ApplyResources(this.comboBoxUsage, "comboBoxUsage");
			this.comboBoxUsage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxUsage.FormattingEnabled = true;
			this.comboBoxUsage.Items.AddRange(new object[] {
			resources.GetString("comboBoxUsage.Items"),
			resources.GetString("comboBoxUsage.Items1"),
			resources.GetString("comboBoxUsage.Items2")});
			this.comboBoxUsage.Name = "comboBoxUsage";
			this.comboBoxUsage.SelectedIndexChanged += new System.EventHandler(this.comboBoxUsage_SelectedIndexChanged);
			// 
			// label5
			// 
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// textBoxPrefix
			// 
			resources.ApplyResources(this.textBoxPrefix, "textBoxPrefix");
			this.textBoxPrefix.Name = "textBoxPrefix";
			// 
			// FormRule
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBoxPrefix);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.comboBoxUsage);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.comboBoxCardinality);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.textBoxIdentifier);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormRule";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textBoxIdentifier;
		private System.Windows.Forms.ComboBox comboBoxCardinality;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.ComboBox comboBoxUsage;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxPrefix;
	}
}