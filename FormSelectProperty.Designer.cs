namespace IfcDoc
{
	partial class FormSelectProperty
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSelectProperty));
			this.treeViewProperty = new System.Windows.Forms.TreeView();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.comboBoxPort = new System.Windows.Forms.ComboBox();
			this.textBoxDescription = new System.Windows.Forms.TextBox();
			this.textBoxType = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// treeViewProperty
			// 
			this.treeViewProperty.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.treeViewProperty.ImageIndex = 0;
			this.treeViewProperty.ImageList = this.imageList;
			this.treeViewProperty.Location = new System.Drawing.Point(17, 48);
			this.treeViewProperty.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.treeViewProperty.Name = "treeViewProperty";
			this.treeViewProperty.SelectedImageIndex = 0;
			this.treeViewProperty.Size = new System.Drawing.Size(333, 440);
			this.treeViewProperty.TabIndex = 1;
			this.treeViewProperty.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewProperty_AfterSelect);
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList.Images.SetKeyName(0, "DocPropertySet.bmp");
			this.imageList.Images.SetKeyName(1, "DocProperty.bmp");
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.buttonCancel.Location = new System.Drawing.Point(929, 500);
			this.buttonCancel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(100, 28);
			this.buttonCancel.TabIndex = 5;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.buttonOK.Location = new System.Drawing.Point(821, 500);
			this.buttonOK.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(100, 28);
			this.buttonOK.TabIndex = 4;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// comboBoxPort
			// 
			this.comboBoxPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxPort.FormattingEnabled = true;
			this.comboBoxPort.Location = new System.Drawing.Point(17, 15);
			this.comboBoxPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.comboBoxPort.Name = "comboBoxPort";
			this.comboBoxPort.Size = new System.Drawing.Size(333, 24);
			this.comboBoxPort.TabIndex = 0;
			this.comboBoxPort.SelectedIndexChanged += new System.EventHandler(this.comboBoxPort_SelectedIndexChanged);
			// 
			// textBoxDescription
			// 
			this.textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxDescription.Location = new System.Drawing.Point(360, 48);
			this.textBoxDescription.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.textBoxDescription.Multiline = true;
			this.textBoxDescription.Name = "textBoxDescription";
			this.textBoxDescription.ReadOnly = true;
			this.textBoxDescription.Size = new System.Drawing.Size(668, 440);
			this.textBoxDescription.TabIndex = 3;
			// 
			// textBoxType
			// 
			this.textBoxType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxType.Location = new System.Drawing.Point(360, 15);
			this.textBoxType.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.textBoxType.Name = "textBoxType";
			this.textBoxType.ReadOnly = true;
			this.textBoxType.Size = new System.Drawing.Size(668, 22);
			this.textBoxType.TabIndex = 2;
			// 
			// FormSelectProperty
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(1045, 543);
			this.Controls.Add(this.textBoxType);
			this.Controls.Add(this.textBoxDescription);
			this.Controls.Add(this.comboBoxPort);
			this.Controls.Add(this.treeViewProperty);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormSelectProperty";
			this.Text = "Select Property";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.ComboBox comboBoxPort;
		private System.Windows.Forms.TextBox textBoxDescription;
		private System.Windows.Forms.TextBox textBoxType;
		protected System.Windows.Forms.TreeView treeViewProperty;
	}
}