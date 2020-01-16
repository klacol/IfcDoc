namespace IfcDoc
{
	partial class FormSaveFolder
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
			this.label1 = new System.Windows.Forms.Label();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.checkBoxSchemas = new System.Windows.Forms.CheckBox();
			this.checkBoxExchanges = new System.Windows.Forms.CheckBox();
			this.checkBoxExamples = new System.Windows.Forms.CheckBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonFolder = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.checkBoxLocalization = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(668, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "This will save content to a folder structure, which can then be loaded back (usin" +
	"g Open Folder), and synchronized and compared on GitHub:";
			// 
			// linkLabel1
			// 
			this.linkLabel1.AutoSize = true;
			this.linkLabel1.Location = new System.Drawing.Point(13, 36);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new System.Drawing.Size(300, 13);
			this.linkLabel1.TabIndex = 1;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "https://github.com/BuildingSMART/IfcDoc/tree/master/IfcKit";
			// 
			// folderBrowserDialog
			// 
			this.folderBrowserDialog.Description = "Select Folder";
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(616, 326);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(697, 326);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// checkBoxSchemas
			// 
			this.checkBoxSchemas.AutoSize = true;
			this.checkBoxSchemas.Checked = true;
			this.checkBoxSchemas.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxSchemas.Location = new System.Drawing.Point(16, 142);
			this.checkBoxSchemas.Name = "checkBoxSchemas";
			this.checkBoxSchemas.Size = new System.Drawing.Size(622, 17);
			this.checkBoxSchemas.TabIndex = 4;
			this.checkBoxSchemas.Text = "Schemas: definitions saved as C#, validation rules saved as Express, documentatio" +
	"n saved as HTML, diagrams saved as SVG";
			this.checkBoxSchemas.UseVisualStyleBackColor = true;
			this.checkBoxSchemas.CheckedChanged += new System.EventHandler(this.checkBoxSchemas_CheckedChanged);
			// 
			// checkBoxExchanges
			// 
			this.checkBoxExchanges.AutoSize = true;
			this.checkBoxExchanges.Checked = true;
			this.checkBoxExchanges.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxExchanges.Location = new System.Drawing.Point(16, 176);
			this.checkBoxExchanges.Name = "checkBoxExchanges";
			this.checkBoxExchanges.Size = new System.Drawing.Size(303, 17);
			this.checkBoxExchanges.TabIndex = 5;
			this.checkBoxExchanges.Text = "Exchanges: model views and templates saved as mvdXML";
			this.checkBoxExchanges.UseVisualStyleBackColor = true;
			this.checkBoxExchanges.CheckedChanged += new System.EventHandler(this.checkBoxExchanges_CheckedChanged);
			// 
			// checkBoxExamples
			// 
			this.checkBoxExamples.AutoSize = true;
			this.checkBoxExamples.Location = new System.Drawing.Point(16, 211);
			this.checkBoxExamples.Name = "checkBoxExamples";
			this.checkBoxExamples.Size = new System.Drawing.Size(341, 17);
			this.checkBoxExamples.TabIndex = 6;
			this.checkBoxExamples.Text = "Examples: examples saved as IFC, documentation saved as HTML";
			this.checkBoxExamples.UseVisualStyleBackColor = true;
			this.checkBoxExamples.CheckedChanged += new System.EventHandler(this.checkBoxExamples_CheckedChanged);
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(16, 94);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(675, 20);
			this.textBox1.TabIndex = 7;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(13, 78);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(220, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Local folder path (this will be the IfcKit folder):";
			// 
			// buttonFolder
			// 
			this.buttonFolder.Location = new System.Drawing.Point(697, 94);
			this.buttonFolder.Name = "buttonFolder";
			this.buttonFolder.Size = new System.Drawing.Size(75, 23);
			this.buttonFolder.TabIndex = 9;
			this.buttonFolder.Text = "Browse...";
			this.buttonFolder.UseVisualStyleBackColor = true;
			this.buttonFolder.Click += new System.EventHandler(this.buttonFolder_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(13, 331);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(578, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "NOTE: This functionality is in beta, and does not capture all state yet; .ifcdoc " +
	"files are still needed if round-tripping all data.";
			// 
			// checkBoxLocalization
			// 
			this.checkBoxLocalization.AutoSize = true;
			this.checkBoxLocalization.Location = new System.Drawing.Point(16, 244);
			this.checkBoxLocalization.Name = "checkBoxLocalization";
			this.checkBoxLocalization.Size = new System.Drawing.Size(290, 17);
			this.checkBoxLocalization.TabIndex = 11;
			this.checkBoxLocalization.Text = "Localization: translations saved as tab-delimited text files";
			this.checkBoxLocalization.UseVisualStyleBackColor = true;
			this.checkBoxLocalization.CheckedChanged += new System.EventHandler(this.checkBoxLocalization_CheckedChanged);
			// 
			// FormSaveFolder
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(784, 361);
			this.Controls.Add(this.checkBoxLocalization);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.buttonFolder);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.checkBoxExamples);
			this.Controls.Add(this.checkBoxExchanges);
			this.Controls.Add(this.checkBoxSchemas);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormSaveFolder";
			this.ShowInTaskbar = false;
			this.Text = "Save To Folder";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.CheckBox checkBoxSchemas;
		private System.Windows.Forms.CheckBox checkBoxExchanges;
		private System.Windows.Forms.CheckBox checkBoxExamples;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonFolder;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckBox checkBoxLocalization;
	}
}