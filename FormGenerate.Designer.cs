namespace IfcDoc
{
	partial class FormGenerate
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormGenerate));
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxPath = new System.Windows.Forms.TextBox();
            this.buttonPath = new System.Windows.Forms.Button();
            this.checkBoxSkip = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.checkedListBoxViews = new System.Windows.Forms.CheckedListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonImagesDocumentation = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxImagesDocumentation = new System.Windows.Forms.TextBox();
            this.buttonExternalConverter = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxExternalConverter = new System.Windows.Forms.TextBox();
            this.openFileDialogConverter = new System.Windows.Forms.OpenFileDialog();
            this.buttonExternalConverterClear = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // textBoxPath
            // 
            resources.ApplyResources(this.textBoxPath, "textBoxPath");
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.ReadOnly = true;
            // 
            // buttonPath
            // 
            resources.ApplyResources(this.buttonPath, "buttonPath");
            this.buttonPath.Name = "buttonPath";
            this.buttonPath.UseVisualStyleBackColor = true;
            this.buttonPath.Click += new System.EventHandler(this.buttonPath_Click);
            // 
            // checkBoxSkip
            // 
            resources.ApplyResources(this.checkBoxSkip, "checkBoxSkip");
            this.checkBoxSkip.Name = "checkBoxSkip";
            this.checkBoxSkip.UseVisualStyleBackColor = true;
            this.checkBoxSkip.CheckedChanged += new System.EventHandler(this.checkBoxSkip_CheckedChanged);
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
            // checkedListBoxViews
            // 
            resources.ApplyResources(this.checkedListBoxViews, "checkedListBoxViews");
            this.checkedListBoxViews.FormattingEnabled = true;
            this.checkedListBoxViews.Name = "checkedListBoxViews";
            this.checkedListBoxViews.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxViews_ItemCheck);
            this.checkedListBoxViews.SelectedIndexChanged += new System.EventHandler(this.checkedListBoxViews_SelectedIndexChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // buttonImagesDocumentation
            // 
            resources.ApplyResources(this.buttonImagesDocumentation, "buttonImagesDocumentation");
            this.buttonImagesDocumentation.Name = "buttonImagesDocumentation";
            this.buttonImagesDocumentation.UseVisualStyleBackColor = true;
            this.buttonImagesDocumentation.Click += new System.EventHandler(this.buttonImagesDocumentation_Click);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // textBoxImagesDocumentation
            // 
            resources.ApplyResources(this.textBoxImagesDocumentation, "textBoxImagesDocumentation");
            this.textBoxImagesDocumentation.Name = "textBoxImagesDocumentation";
            this.textBoxImagesDocumentation.ReadOnly = true;
            // 
            // buttonExternalConverter
            // 
            resources.ApplyResources(this.buttonExternalConverter, "buttonExternalConverter");
            this.buttonExternalConverter.Name = "buttonExternalConverter";
            this.buttonExternalConverter.UseVisualStyleBackColor = true;
            this.buttonExternalConverter.Click += new System.EventHandler(this.buttonExternalConverter_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // textBoxExternalConverter
            // 
            resources.ApplyResources(this.textBoxExternalConverter, "textBoxExternalConverter");
            this.textBoxExternalConverter.Name = "textBoxExternalConverter";
            this.textBoxExternalConverter.ReadOnly = true;
            // 
            // openFileDialogConverter
            // 
            this.openFileDialogConverter.DefaultExt = "exe";
            resources.ApplyResources(this.openFileDialogConverter, "openFileDialogConverter");
            // 
            // buttonExternalConverterClear
            // 
            resources.ApplyResources(this.buttonExternalConverterClear, "buttonExternalConverterClear");
            this.buttonExternalConverterClear.Name = "buttonExternalConverterClear";
            this.buttonExternalConverterClear.UseVisualStyleBackColor = true;
            this.buttonExternalConverterClear.Click += new System.EventHandler(this.buttonExternalConverterClear_Click);
            // 
            // FormGenerate
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.buttonCancel;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.buttonExternalConverterClear);
            this.Controls.Add(this.buttonExternalConverter);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxExternalConverter);
            this.Controls.Add(this.buttonImagesDocumentation);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxImagesDocumentation);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.checkedListBoxViews);
            this.Controls.Add(this.checkBoxSkip);
            this.Controls.Add(this.buttonPath);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textBoxPath);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormGenerate";
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxPath;
		private System.Windows.Forms.Button buttonPath;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.CheckedListBox checkedListBoxViews;
		private System.Windows.Forms.CheckBox checkBoxSkip;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button buttonImagesDocumentation;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textBoxImagesDocumentation;
		private System.Windows.Forms.Button buttonExternalConverter;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox textBoxExternalConverter;
		private System.Windows.Forms.OpenFileDialog openFileDialogConverter;
		private System.Windows.Forms.Button buttonExternalConverterClear;
	}
}