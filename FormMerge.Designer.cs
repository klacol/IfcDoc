namespace IfcDoc
{
	partial class FormMerge
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMerge));
			this.label3 = new System.Windows.Forms.Label();
			this.radioButtonOriginal = new System.Windows.Forms.RadioButton();
			this.radioButtonChange = new System.Windows.Forms.RadioButton();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.lvDestination = new System.Windows.Forms.ListView();
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.lvSource = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.treeView = new System.Windows.Forms.TreeView();
			this.label1 = new System.Windows.Forms.Label();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// radioButtonOriginal
			// 
			resources.ApplyResources(this.radioButtonOriginal, "radioButtonOriginal");
			this.radioButtonOriginal.Name = "radioButtonOriginal";
			this.radioButtonOriginal.TabStop = true;
			this.radioButtonOriginal.UseVisualStyleBackColor = true;
			this.radioButtonOriginal.Click += new System.EventHandler(this.radioButtonOriginal_Click);
			// 
			// radioButtonChange
			// 
			resources.ApplyResources(this.radioButtonChange, "radioButtonChange");
			this.radioButtonChange.Name = "radioButtonChange";
			this.radioButtonChange.TabStop = true;
			this.radioButtonChange.UseVisualStyleBackColor = true;
			this.radioButtonChange.Click += new System.EventHandler(this.radioButtonChange_Click);
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
			// tableLayoutPanel1
			// 
			resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
			this.tableLayoutPanel1.Controls.Add(this.lvDestination, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.radioButtonChange, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.radioButtonOriginal, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.lvSource, 0, 2);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			// 
			// lvDestination
			// 
			this.lvDestination.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader3,
			this.columnHeader4});
			resources.ApplyResources(this.lvDestination, "lvDestination");
			this.lvDestination.FullRowSelect = true;
			this.lvDestination.HideSelection = false;
			this.lvDestination.MultiSelect = false;
			this.lvDestination.Name = "lvDestination";
			this.lvDestination.UseCompatibleStateImageBehavior = false;
			this.lvDestination.View = System.Windows.Forms.View.Details;
			this.lvDestination.SelectedIndexChanged += new System.EventHandler(this.lvDestination_SelectedIndexChanged);
			this.lvDestination.Resize += new System.EventHandler(this.lvDestination_Resize);
			// 
			// columnHeader3
			// 
			resources.ApplyResources(this.columnHeader3, "columnHeader3");
			// 
			// columnHeader4
			// 
			resources.ApplyResources(this.columnHeader4, "columnHeader4");
			// 
			// lvSource
			// 
			this.lvSource.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader2});
			resources.ApplyResources(this.lvSource, "lvSource");
			this.lvSource.FullRowSelect = true;
			this.lvSource.HideSelection = false;
			this.lvSource.MultiSelect = false;
			this.lvSource.Name = "lvSource";
			this.lvSource.UseCompatibleStateImageBehavior = false;
			this.lvSource.View = System.Windows.Forms.View.Details;
			this.lvSource.SelectedIndexChanged += new System.EventHandler(this.lvSource_SelectedIndexChanged);
			this.lvSource.Resize += new System.EventHandler(this.lvSource_Resize);
			// 
			// columnHeader1
			// 
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			// 
			// columnHeader2
			// 
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			// 
			// treeView
			// 
			resources.ApplyResources(this.treeView, "treeView");
			this.treeView.CheckBoxes = true;
			this.treeView.Name = "treeView";
			this.treeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterCheck);
			this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// FormMerge
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.treeView);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.label3);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormMerge";
			this.ShowInTaskbar = false;
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.RadioButton radioButtonOriginal;
		private System.Windows.Forms.RadioButton radioButtonChange;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.ListView lvDestination;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.ColumnHeader columnHeader4;
		private System.Windows.Forms.ListView lvSource;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Label label1;
	}
}