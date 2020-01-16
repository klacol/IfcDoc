namespace IfcDoc
{
	partial class FormSelectView
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSelectView));
			this.labelDescription = new System.Windows.Forms.Label();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.treeView = new System.Windows.Forms.TreeView();
			this.comboBoxMVDXML = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// labelDescription
			// 
			resources.ApplyResources(this.labelDescription, "labelDescription");
			this.labelDescription.Name = "labelDescription";
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
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList.Images.SetKeyName(0, "DocModelView.bmp");
			// 
			// treeView
			// 
			resources.ApplyResources(this.treeView, "treeView");
			this.treeView.Name = "treeView";
			// 
			// comboBoxMVDXML
			// 
			resources.ApplyResources(this.comboBoxMVDXML, "comboBoxMVDXML");
			this.comboBoxMVDXML.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxMVDXML.FormattingEnabled = true;
			this.comboBoxMVDXML.Items.AddRange(new object[] {
			resources.GetString("comboBoxMVDXML.Items"),
			resources.GetString("comboBoxMVDXML.Items1")});
			this.comboBoxMVDXML.Name = "comboBoxMVDXML";
			// 
			// FormSelectView
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.comboBoxMVDXML);
			this.Controls.Add(this.treeView);
			this.Controls.Add(this.labelDescription);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormSelectView";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelDescription;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.ComboBox comboBoxMVDXML;
	}
}