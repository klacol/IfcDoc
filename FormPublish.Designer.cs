namespace IfcDoc
{
	partial class FormPublish
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPublish));
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.textBoxUrl = new System.Windows.Forms.TextBox();
			this.labelServer = new System.Windows.Forms.Label();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.labelSummary = new System.Windows.Forms.Label();
			this.backgroundWorkerPublish = new System.ComponentModel.BackgroundWorker();
			this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this.labelError = new System.Windows.Forms.Label();
			this.comboBoxProtocol = new System.Windows.Forms.ComboBox();
			this.buttonLogin = new System.Windows.Forms.Button();
			this.labelUsername = new System.Windows.Forms.Label();
			this.textBoxUsername = new System.Windows.Forms.TextBox();
			this.textBoxPassword = new System.Windows.Forms.TextBox();
			this.labelPassword = new System.Windows.Forms.Label();
			this.backgroundWorkerContexts = new System.ComponentModel.BackgroundWorker();
			this.checkBoxRemember = new System.Windows.Forms.CheckBox();
			this.textBoxWarning = new System.Windows.Forms.TextBox();
			this.treeViewContainer = new System.Windows.Forms.TreeView();
			this.imageListIcon = new System.Windows.Forms.ImageList(this.components);
			this.backgroundWorkerConcepts = new System.ComponentModel.BackgroundWorker();
			this.comboBoxContext = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxNamespace = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.buttonSearch = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// progressBar
			// 
			resources.ApplyResources(this.progressBar, "progressBar");
			this.progressBar.Name = "progressBar";
			// 
			// textBoxUrl
			// 
			resources.ApplyResources(this.textBoxUrl, "textBoxUrl");
			this.textBoxUrl.Name = "textBoxUrl";
			this.textBoxUrl.ReadOnly = true;
			// 
			// labelServer
			// 
			resources.ApplyResources(this.labelServer, "labelServer");
			this.labelServer.Name = "labelServer";
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
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// labelSummary
			// 
			resources.ApplyResources(this.labelSummary, "labelSummary");
			this.labelSummary.Name = "labelSummary";
			// 
			// backgroundWorkerPublish
			// 
			this.backgroundWorkerPublish.WorkerReportsProgress = true;
			this.backgroundWorkerPublish.WorkerSupportsCancellation = true;
			this.backgroundWorkerPublish.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerPublish_DoWork);
			this.backgroundWorkerPublish.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorkerPublish_ProgressChanged);
			this.backgroundWorkerPublish.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerPublish_RunWorkerCompleted);
			// 
			// errorProvider
			// 
			this.errorProvider.ContainerControl = this;
			// 
			// labelError
			// 
			resources.ApplyResources(this.labelError, "labelError");
			this.labelError.Name = "labelError";
			// 
			// comboBoxProtocol
			// 
			resources.ApplyResources(this.comboBoxProtocol, "comboBoxProtocol");
			this.comboBoxProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxProtocol.FormattingEnabled = true;
			this.comboBoxProtocol.Items.AddRange(new object[] {
			resources.GetString("comboBoxProtocol.Items"),
			resources.GetString("comboBoxProtocol.Items1")});
			this.comboBoxProtocol.Name = "comboBoxProtocol";
			this.comboBoxProtocol.SelectedIndexChanged += new System.EventHandler(this.comboBoxProtocol_SelectedIndexChanged);
			// 
			// buttonLogin
			// 
			resources.ApplyResources(this.buttonLogin, "buttonLogin");
			this.buttonLogin.Name = "buttonLogin";
			this.buttonLogin.UseVisualStyleBackColor = true;
			this.buttonLogin.Click += new System.EventHandler(this.buttonLogin_Click);
			// 
			// labelUsername
			// 
			resources.ApplyResources(this.labelUsername, "labelUsername");
			this.labelUsername.Name = "labelUsername";
			// 
			// textBoxUsername
			// 
			resources.ApplyResources(this.textBoxUsername, "textBoxUsername");
			this.textBoxUsername.Name = "textBoxUsername";
			// 
			// textBoxPassword
			// 
			resources.ApplyResources(this.textBoxPassword, "textBoxPassword");
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.UseSystemPasswordChar = true;
			// 
			// labelPassword
			// 
			resources.ApplyResources(this.labelPassword, "labelPassword");
			this.labelPassword.Name = "labelPassword";
			// 
			// backgroundWorkerContexts
			// 
			this.backgroundWorkerContexts.WorkerReportsProgress = true;
			this.backgroundWorkerContexts.WorkerSupportsCancellation = true;
			// 
			// checkBoxRemember
			// 
			resources.ApplyResources(this.checkBoxRemember, "checkBoxRemember");
			this.checkBoxRemember.Name = "checkBoxRemember";
			this.checkBoxRemember.UseVisualStyleBackColor = true;
			// 
			// textBoxWarning
			// 
			this.textBoxWarning.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.textBoxWarning, "textBoxWarning");
			this.textBoxWarning.ForeColor = System.Drawing.Color.Black;
			this.textBoxWarning.Name = "textBoxWarning";
			this.textBoxWarning.ReadOnly = true;
			// 
			// treeViewContainer
			// 
			resources.ApplyResources(this.treeViewContainer, "treeViewContainer");
			this.treeViewContainer.CheckBoxes = true;
			this.treeViewContainer.ImageList = this.imageListIcon;
			this.treeViewContainer.Name = "treeViewContainer";
			this.treeViewContainer.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewContainer_BeforeExpand);
			// 
			// imageListIcon
			// 
			this.imageListIcon.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListIcon.ImageStream")));
			this.imageListIcon.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageListIcon.Images.SetKeyName(0, "Folder.bmp");
			this.imageListIcon.Images.SetKeyName(1, "DocSection.bmp");
			this.imageListIcon.Images.SetKeyName(2, "DocAttribute.bmp");
			this.imageListIcon.Images.SetKeyName(3, "DocConstant.bmp");
			this.imageListIcon.Images.SetKeyName(4, "DocDefinedType.bmp");
			this.imageListIcon.Images.SetKeyName(5, "DocEntity.bmp");
			this.imageListIcon.Images.SetKeyName(6, "DocEnumeration.bmp");
			this.imageListIcon.Images.SetKeyName(7, "DocFunction.bmp");
			this.imageListIcon.Images.SetKeyName(8, "DocGlobalRule.bmp");
			this.imageListIcon.Images.SetKeyName(9, "DocProperty.bmp");
			this.imageListIcon.Images.SetKeyName(10, "DocPropertySet.bmp");
			this.imageListIcon.Images.SetKeyName(11, "DocQuantity.bmp");
			this.imageListIcon.Images.SetKeyName(12, "DocQuantitySet.bmp");
			this.imageListIcon.Images.SetKeyName(13, "DocSchema.bmp");
			this.imageListIcon.Images.SetKeyName(14, "DocSection.bmp");
			this.imageListIcon.Images.SetKeyName(15, "DocSelect.bmp");
			this.imageListIcon.Images.SetKeyName(16, "DocTemplateItem.bmp");
			this.imageListIcon.Images.SetKeyName(17, "DocConceptLeaf.png");
			this.imageListIcon.Images.SetKeyName(18, "DocUniqueRule.bmp");
			this.imageListIcon.Images.SetKeyName(19, "DocWhereRule.bmp");
			this.imageListIcon.Images.SetKeyName(20, "DocAnnex.bmp");
			this.imageListIcon.Images.SetKeyName(21, "DocEnumeration-Format.bmp");
			this.imageListIcon.Images.SetKeyName(22, "DocField-Inverse.bmp");
			this.imageListIcon.Images.SetKeyName(23, "DocTemplateDefinition.png");
			this.imageListIcon.Images.SetKeyName(24, "DocModelView.png");
			this.imageListIcon.Images.SetKeyName(25, "DocExchangeDefinition.png");
			this.imageListIcon.Images.SetKeyName(26, "DocExchangeItem.bmp");
			this.imageListIcon.Images.SetKeyName(27, "DocRuleAttribute.bmp");
			this.imageListIcon.Images.SetKeyName(28, "DocRuleEntity.bmp");
			this.imageListIcon.Images.SetKeyName(29, "DocChangeSet.bmp");
			this.imageListIcon.Images.SetKeyName(30, "DocConceptRoot.png");
			this.imageListIcon.Images.SetKeyName(31, "DocExample.png");
			this.imageListIcon.Images.SetKeyName(32, "DocEnumeration.bmp");
			this.imageListIcon.Images.SetKeyName(33, "DocConstant.bmp");
			this.imageListIcon.Images.SetKeyName(34, "Microsoft.VisualStudio.Blend.Html.Intrinsic.Mark.16x16.png");
			this.imageListIcon.Images.SetKeyName(35, "Microsoft.VisualStudio.Blend.Html.Intrinsic.Th.16x16.png");
			this.imageListIcon.Images.SetKeyName(36, "Microsoft.VisualStudio.Blend.Html.Intrinsic.Base.16x16.png");
			this.imageListIcon.Images.SetKeyName(37, "Microsoft.VisualStudio.Blend.Html.Intrinsic.Head.16x16.png");
			this.imageListIcon.Images.SetKeyName(38, "Microsoft.VS.Behavior.16x16.png");
			this.imageListIcon.Images.SetKeyName(39, "Microsoft.VS.Action.16x16.png");
			this.imageListIcon.Images.SetKeyName(40, "DocTerm.png");
			this.imageListIcon.Images.SetKeyName(41, "DocAbbreviation.png");
			this.imageListIcon.Images.SetKeyName(42, "Microsoft.VisualStudio.Blend.Html.Intrinsic.Div.Contenteditable.16x16.png");
			this.imageListIcon.Images.SetKeyName(43, "Microsoft.VisualStudio.Blend.Html.Intrinsic.Aside.16x16.png");
			this.imageListIcon.Images.SetKeyName(44, "ni0016-16.png");
			this.imageListIcon.Images.SetKeyName(45, "ni0017-16.png");
			this.imageListIcon.Images.SetKeyName(46, "ni0019-16.png");
			// 
			// backgroundWorkerConcepts
			// 
			this.backgroundWorkerConcepts.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerConcepts_DoWork);
			this.backgroundWorkerConcepts.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerConcepts_RunWorkerCompleted);
			// 
			// comboBoxContext
			// 
			resources.ApplyResources(this.comboBoxContext, "comboBoxContext");
			this.comboBoxContext.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxContext.FormattingEnabled = true;
			this.comboBoxContext.Items.AddRange(new object[] {
			resources.GetString("comboBoxContext.Items"),
			resources.GetString("comboBoxContext.Items1")});
			this.comboBoxContext.Name = "comboBoxContext";
			this.comboBoxContext.Sorted = true;
			this.comboBoxContext.SelectedIndexChanged += new System.EventHandler(this.comboBoxContext_SelectedIndexChanged);
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// textBoxNamespace
			// 
			resources.ApplyResources(this.textBoxNamespace, "textBoxNamespace");
			this.textBoxNamespace.Name = "textBoxNamespace";
			this.textBoxNamespace.TextChanged += new System.EventHandler(this.textBoxNamespace_TextChanged);
			this.textBoxNamespace.Validated += new System.EventHandler(this.textBoxNamespace_Validated);
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// buttonSearch
			// 
			resources.ApplyResources(this.buttonSearch, "buttonSearch");
			this.buttonSearch.Name = "buttonSearch";
			this.buttonSearch.UseVisualStyleBackColor = true;
			this.buttonSearch.Click += new System.EventHandler(this.buttonSearch_Click);
			// 
			// FormPublish
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.buttonSearch);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.textBoxNamespace);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBoxContext);
			this.Controls.Add(this.treeViewContainer);
			this.Controls.Add(this.checkBoxRemember);
			this.Controls.Add(this.labelPassword);
			this.Controls.Add(this.textBoxPassword);
			this.Controls.Add(this.textBoxUsername);
			this.Controls.Add(this.labelUsername);
			this.Controls.Add(this.buttonLogin);
			this.Controls.Add(this.comboBoxProtocol);
			this.Controls.Add(this.labelError);
			this.Controls.Add(this.labelSummary);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.textBoxUrl);
			this.Controls.Add(this.labelServer);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.textBoxWarning);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormPublish";
			this.ShowInTaskbar = false;
			this.Load += new System.EventHandler(this.FormPublish_Load);
			((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.TextBox textBoxUrl;
		private System.Windows.Forms.Label labelServer;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label labelSummary;
		private System.ComponentModel.BackgroundWorker backgroundWorkerPublish;
		private System.Windows.Forms.ErrorProvider errorProvider;
		private System.Windows.Forms.Label labelError;
		private System.Windows.Forms.ComboBox comboBoxProtocol;
		private System.Windows.Forms.Button buttonLogin;
		private System.Windows.Forms.Label labelPassword;
		private System.Windows.Forms.TextBox textBoxPassword;
		private System.Windows.Forms.TextBox textBoxUsername;
		private System.Windows.Forms.Label labelUsername;
		private System.ComponentModel.BackgroundWorker backgroundWorkerContexts;
		private System.Windows.Forms.CheckBox checkBoxRemember;
		private System.Windows.Forms.TextBox textBoxWarning;
		private System.Windows.Forms.TreeView treeViewContainer;
		private System.ComponentModel.BackgroundWorker backgroundWorkerConcepts;
		private System.Windows.Forms.ImageList imageListIcon;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxContext;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxNamespace;
		private System.Windows.Forms.Button buttonSearch;
	}
}