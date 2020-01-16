namespace BuildingSmart.Utilities.Validation
{
	partial class FormValidator
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormValidator));
			this.treeView = new System.Windows.Forms.TreeView();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.listView = new System.Windows.Forms.ListView();
			this.splitContainerMain = new System.Windows.Forms.SplitContainer();
			this.textBoxHelp = new System.Windows.Forms.TextBox();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
			this.toolStripStatusLabelNone = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabelPass = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabelPartial = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabelFail = new System.Windows.Forms.ToolStripStatusLabel();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
			this.splitContainerMain.Panel1.SuspendLayout();
			this.splitContainerMain.Panel2.SuspendLayout();
			this.splitContainerMain.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// treeView
			// 
			this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView.ImageIndex = 0;
			this.treeView.ImageList = this.imageList;
			this.treeView.Location = new System.Drawing.Point(0, 0);
			this.treeView.Name = "treeView";
			this.treeView.SelectedImageIndex = 0;
			this.treeView.Size = new System.Drawing.Size(225, 370);
			this.treeView.TabIndex = 0;
			this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList.Images.SetKeyName(0, "ni0007-16.png");
			this.imageList.Images.SetKeyName(1, "ni0008-16.png");
			this.imageList.Images.SetKeyName(2, "ni0009-16.png");
			this.imageList.Images.SetKeyName(3, "ni0010-16.png");
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.treeView);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.listView);
			this.splitContainer1.Size = new System.Drawing.Size(675, 370);
			this.splitContainer1.SplitterDistance = 225;
			this.splitContainer1.TabIndex = 1;
			// 
			// listView
			// 
			this.listView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView.Location = new System.Drawing.Point(0, 0);
			this.listView.Name = "listView";
			this.listView.Size = new System.Drawing.Size(446, 370);
			this.listView.SmallImageList = this.imageList;
			this.listView.TabIndex = 0;
			this.listView.UseCompatibleStateImageBehavior = false;
			this.listView.View = System.Windows.Forms.View.Details;
			// 
			// splitContainerMain
			// 
			this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
			this.splitContainerMain.Name = "splitContainerMain";
			this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainerMain.Panel1
			// 
			this.splitContainerMain.Panel1.Controls.Add(this.splitContainer1);
			// 
			// splitContainerMain.Panel2
			// 
			this.splitContainerMain.Panel2.Controls.Add(this.textBoxHelp);
			this.splitContainerMain.Panel2.Controls.Add(this.statusStrip);
			this.splitContainerMain.Size = new System.Drawing.Size(675, 453);
			this.splitContainerMain.SplitterDistance = 370;
			this.splitContainerMain.TabIndex = 2;
			// 
			// textBoxHelp
			// 
			this.textBoxHelp.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxHelp.Location = new System.Drawing.Point(0, 0);
			this.textBoxHelp.Multiline = true;
			this.textBoxHelp.Name = "textBoxHelp";
			this.textBoxHelp.ReadOnly = true;
			this.textBoxHelp.Size = new System.Drawing.Size(675, 57);
			this.textBoxHelp.TabIndex = 0;
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripStatusLabelNone,
			this.toolStripStatusLabelPass,
			this.toolStripStatusLabelPartial,
			this.toolStripStatusLabelFail,
			this.toolStripStatusLabel1,
			this.toolStripProgressBar});
			this.statusStrip.Location = new System.Drawing.Point(0, 57);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(675, 22);
			this.statusStrip.TabIndex = 1;
			this.statusStrip.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(281, 17);
			this.toolStripStatusLabel1.Spring = true;
			this.toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// toolStripProgressBar
			// 
			this.toolStripProgressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripProgressBar.Name = "toolStripProgressBar";
			this.toolStripProgressBar.Size = new System.Drawing.Size(100, 16);
			// 
			// backgroundWorker
			// 
			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
			this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
			this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
			// 
			// toolStripStatusLabelNone
			// 
			this.toolStripStatusLabelNone.Image = ((System.Drawing.Image)(resources.GetObject("toolStripStatusLabelNone.Image")));
			this.toolStripStatusLabelNone.Name = "toolStripStatusLabelNone";
			this.toolStripStatusLabelNone.Size = new System.Drawing.Size(83, 17);
			this.toolStripStatusLabelNone.Text = "No Data (0)";
			// 
			// toolStripStatusLabelPass
			// 
			this.toolStripStatusLabelPass.Image = ((System.Drawing.Image)(resources.GetObject("toolStripStatusLabelPass.Image")));
			this.toolStripStatusLabelPass.Name = "toolStripStatusLabelPass";
			this.toolStripStatusLabelPass.Size = new System.Drawing.Size(63, 17);
			this.toolStripStatusLabelPass.Text = "Pass (0)";
			// 
			// toolStripStatusLabelPartial
			// 
			this.toolStripStatusLabelPartial.Image = ((System.Drawing.Image)(resources.GetObject("toolStripStatusLabelPartial.Image")));
			this.toolStripStatusLabelPartial.Name = "toolStripStatusLabelPartial";
			this.toolStripStatusLabelPartial.Size = new System.Drawing.Size(73, 17);
			this.toolStripStatusLabelPartial.Text = "Partial (0)";
			// 
			// toolStripStatusLabelFail
			// 
			this.toolStripStatusLabelFail.Image = ((System.Drawing.Image)(resources.GetObject("toolStripStatusLabelFail.Image")));
			this.toolStripStatusLabelFail.Name = "toolStripStatusLabelFail";
			this.toolStripStatusLabelFail.Size = new System.Drawing.Size(58, 17);
			this.toolStripStatusLabelFail.Text = "Fail (0)";
			// 
			// FormValidator
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(675, 453);
			this.Controls.Add(this.splitContainerMain);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormValidator";
			this.ShowInTaskbar = false;
			this.Text = "Validation";
			this.Load += new System.EventHandler(this.FormValidator_Load);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainerMain.Panel1.ResumeLayout(false);
			this.splitContainerMain.Panel2.ResumeLayout(false);
			this.splitContainerMain.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
			this.splitContainerMain.ResumeLayout(false);
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.SplitContainer splitContainerMain;
		private System.Windows.Forms.TextBox textBoxHelp;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar;
		private System.ComponentModel.BackgroundWorker backgroundWorker;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelNone;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelPass;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelPartial;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelFail;

	}
}