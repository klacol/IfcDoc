namespace IfcDoc
{
	partial class FormValidate
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
			this.groupBoxViews = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxExchange = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.comboBoxView = new System.Windows.Forms.ComboBox();
			this.tabPageValidate = new System.Windows.Forms.TabPage();
			this.groupBoxLocation = new System.Windows.Forms.GroupBox();
			this.checkBoxReport = new System.Windows.Forms.CheckBox();
			this.buttonPath = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxPath = new System.Windows.Forms.TextBox();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.openFileDialogIFC = new System.Windows.Forms.OpenFileDialog();
			this.groupBoxViews.SuspendLayout();
			this.tabPageValidate.SuspendLayout();
			this.groupBoxLocation.SuspendLayout();
			this.tabControl.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBoxViews
			// 
			this.groupBoxViews.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxViews.Controls.Add(this.label3);
			this.groupBoxViews.Controls.Add(this.comboBoxExchange);
			this.groupBoxViews.Controls.Add(this.label2);
			this.groupBoxViews.Controls.Add(this.comboBoxView);
			this.groupBoxViews.Location = new System.Drawing.Point(6, 106);
			this.groupBoxViews.Name = "groupBoxViews";
			this.groupBoxViews.Size = new System.Drawing.Size(580, 251);
			this.groupBoxViews.TabIndex = 1;
			this.groupBoxViews.TabStop = false;
			this.groupBoxViews.Text = "Checking";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 65);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(58, 13);
			this.label3.TabIndex = 3;
			this.label3.Text = "Exchange:";
			// 
			// comboBoxExchange
			// 
			this.comboBoxExchange.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxExchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxExchange.FormattingEnabled = true;
			this.comboBoxExchange.Location = new System.Drawing.Point(9, 81);
			this.comboBoxExchange.Name = "comboBoxExchange";
			this.comboBoxExchange.Size = new System.Drawing.Size(565, 21);
			this.comboBoxExchange.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(5, 19);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(65, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Model View:";
			// 
			// comboBoxView
			// 
			this.comboBoxView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBoxView.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxView.FormattingEnabled = true;
			this.comboBoxView.Location = new System.Drawing.Point(9, 35);
			this.comboBoxView.Name = "comboBoxView";
			this.comboBoxView.Size = new System.Drawing.Size(565, 21);
			this.comboBoxView.TabIndex = 0;
			this.comboBoxView.SelectedIndexChanged += new System.EventHandler(this.comboBoxView_SelectedIndexChanged);
			// 
			// tabPageValidate
			// 
			this.tabPageValidate.Controls.Add(this.groupBoxViews);
			this.tabPageValidate.Controls.Add(this.groupBoxLocation);
			this.tabPageValidate.Location = new System.Drawing.Point(4, 22);
			this.tabPageValidate.Name = "tabPageValidate";
			this.tabPageValidate.Padding = new System.Windows.Forms.Padding(3);
			this.tabPageValidate.Size = new System.Drawing.Size(592, 363);
			this.tabPageValidate.TabIndex = 0;
			this.tabPageValidate.Text = "Validate";
			this.tabPageValidate.UseVisualStyleBackColor = true;
			// 
			// groupBoxLocation
			// 
			this.groupBoxLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBoxLocation.Controls.Add(this.checkBoxReport);
			this.groupBoxLocation.Controls.Add(this.buttonPath);
			this.groupBoxLocation.Controls.Add(this.label1);
			this.groupBoxLocation.Controls.Add(this.textBoxPath);
			this.groupBoxLocation.Location = new System.Drawing.Point(6, 6);
			this.groupBoxLocation.Name = "groupBoxLocation";
			this.groupBoxLocation.Size = new System.Drawing.Size(580, 94);
			this.groupBoxLocation.TabIndex = 0;
			this.groupBoxLocation.TabStop = false;
			this.groupBoxLocation.Text = "File";
			// 
			// checkBoxReport
			// 
			this.checkBoxReport.AutoSize = true;
			this.checkBoxReport.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.checkBoxReport.Location = new System.Drawing.Point(9, 65);
			this.checkBoxReport.Name = "checkBoxReport";
			this.checkBoxReport.Size = new System.Drawing.Size(197, 17);
			this.checkBoxReport.TabIndex = 3;
			this.checkBoxReport.Text = "Generate HTML report alongside file";
			this.checkBoxReport.UseVisualStyleBackColor = true;
			this.checkBoxReport.CheckedChanged += new System.EventHandler(this.checkBoxReport_CheckedChanged);
			// 
			// buttonPath
			// 
			this.buttonPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonPath.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.buttonPath.Location = new System.Drawing.Point(499, 36);
			this.buttonPath.Name = "buttonPath";
			this.buttonPath.Size = new System.Drawing.Size(75, 23);
			this.buttonPath.TabIndex = 2;
			this.buttonPath.Text = "Browse...";
			this.buttonPath.UseVisualStyleBackColor = true;
			this.buttonPath.Click += new System.EventHandler(this.buttonPath_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.label1.Location = new System.Drawing.Point(6, 21);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(94, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "IFC file to validate:";
			// 
			// textBoxPath
			// 
			this.textBoxPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxPath.Location = new System.Drawing.Point(9, 38);
			this.textBoxPath.Name = "textBoxPath";
			this.textBoxPath.ReadOnly = true;
			this.textBoxPath.Size = new System.Drawing.Size(484, 20);
			this.textBoxPath.TabIndex = 1;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.buttonCancel.Location = new System.Drawing.Point(537, 406);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 5;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.buttonOK.Location = new System.Drawing.Point(456, 406);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 4;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tabPageValidate);
			this.tabControl.Location = new System.Drawing.Point(12, 11);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(600, 389);
			this.tabControl.TabIndex = 6;
			// 
			// openFileDialogIFC
			// 
			this.openFileDialogIFC.FileName = "openFileDialog";
			this.openFileDialogIFC.Filter = "IFC Files (*.ifc)|*.ifc";
			this.openFileDialogIFC.Title = "Validate IFC File";
			// 
			// FormValidate
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(624, 441);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.tabControl);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormValidate";
			this.ShowInTaskbar = false;
			this.Text = "Validate";
			this.groupBoxViews.ResumeLayout(false);
			this.groupBoxViews.PerformLayout();
			this.tabPageValidate.ResumeLayout(false);
			this.groupBoxLocation.ResumeLayout(false);
			this.groupBoxLocation.PerformLayout();
			this.tabControl.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBoxViews;
		private System.Windows.Forms.TabPage tabPageValidate;
		private System.Windows.Forms.GroupBox groupBoxLocation;
		private System.Windows.Forms.CheckBox checkBoxReport;
		private System.Windows.Forms.Button buttonPath;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxPath;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboBoxExchange;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox comboBoxView;
		private System.Windows.Forms.OpenFileDialog openFileDialogIFC;

	}
}