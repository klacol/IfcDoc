namespace IfcDoc
{
	partial class FormSelectTemplate
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSelectTemplate));
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.treeView = new System.Windows.Forms.TreeView();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			this.splitContainerPreview = new System.Windows.Forms.SplitContainer();
			this.ctlConcept = new IfcDoc.CtlConcept();
			this.webBrowser = new System.Windows.Forms.WebBrowser();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerPreview)).BeginInit();
			this.splitContainerPreview.Panel1.SuspendLayout();
			this.splitContainerPreview.Panel2.SuspendLayout();
			this.splitContainerPreview.SuspendLayout();
			this.SuspendLayout();
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
			// treeView
			// 
			this.treeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.treeView, "treeView");
			this.treeView.HideSelection = false;
			this.treeView.ImageList = this.imageList;
			this.treeView.Name = "treeView";
			this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList.Images.SetKeyName(0, "DocTemplateDefinition.png");
			// 
			// splitContainer
			// 
			resources.ApplyResources(this.splitContainer, "splitContainer");
			this.splitContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.splitContainer.Name = "splitContainer";
			// 
			// splitContainer.Panel1
			// 
			this.splitContainer.Panel1.Controls.Add(this.treeView);
			// 
			// splitContainer.Panel2
			// 
			this.splitContainer.Panel2.Controls.Add(this.splitContainerPreview);
			// 
			// splitContainerPreview
			// 
			resources.ApplyResources(this.splitContainerPreview, "splitContainerPreview");
			this.splitContainerPreview.Name = "splitContainerPreview";
			// 
			// splitContainerPreview.Panel1
			// 
			this.splitContainerPreview.Panel1.Controls.Add(this.ctlConcept);
			// 
			// splitContainerPreview.Panel2
			// 
			this.splitContainerPreview.Panel2.Controls.Add(this.webBrowser);
			// 
			// ctlConcept
			// 
			resources.ApplyResources(this.ctlConcept, "ctlConcept");
			this.ctlConcept.ConceptRoot = null;
			this.ctlConcept.CurrentInstance = null;
			this.ctlConcept.Name = "ctlConcept";
			this.ctlConcept.Project = null;
			this.ctlConcept.Selection = null;
			this.ctlConcept.Template = null;
			this.ctlConcept.SelectionChanged += new System.EventHandler(this.ctlConcept_SelectionChanged);
			// 
			// webBrowser
			// 
			resources.ApplyResources(this.webBrowser, "webBrowser");
			this.webBrowser.Name = "webBrowser";
			// 
			// FormSelectTemplate
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.splitContainer);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormSelectTemplate";
			this.ShowInTaskbar = false;
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
			this.splitContainer.ResumeLayout(false);
			this.splitContainerPreview.Panel1.ResumeLayout(false);
			this.splitContainerPreview.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainerPreview)).EndInit();
			this.splitContainerPreview.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.SplitContainer splitContainer;
		private CtlConcept ctlConcept;
		private System.Windows.Forms.SplitContainer splitContainerPreview;
		private System.Windows.Forms.WebBrowser webBrowser;
	}
}