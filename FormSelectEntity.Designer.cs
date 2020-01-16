namespace IfcDoc
{
	partial class FormSelectEntity
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSelectEntity));
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.treeViewInheritance = new System.Windows.Forms.TreeView();
			this.comboBoxPredefined = new System.Windows.Forms.ComboBox();
			this.labelPredefined = new System.Windows.Forms.Label();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.treeViewAlphabetical = new System.Windows.Forms.TreeView();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
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
			// treeViewInheritance
			// 
			resources.ApplyResources(this.treeViewInheritance, "treeViewInheritance");
			this.treeViewInheritance.HideSelection = false;
			this.treeViewInheritance.Name = "treeViewInheritance";
			this.treeViewInheritance.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewInheritance_AfterSelect);
			// 
			// comboBoxPredefined
			// 
			resources.ApplyResources(this.comboBoxPredefined, "comboBoxPredefined");
			this.comboBoxPredefined.FormattingEnabled = true;
			this.comboBoxPredefined.Name = "comboBoxPredefined";
			// 
			// labelPredefined
			// 
			resources.ApplyResources(this.labelPredefined, "labelPredefined");
			this.labelPredefined.Name = "labelPredefined";
			// 
			// tabControl1
			// 
			resources.ApplyResources(this.tabControl1, "tabControl1");
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.treeViewInheritance);
			resources.ApplyResources(this.tabPage1, "tabPage1");
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.treeViewAlphabetical);
			resources.ApplyResources(this.tabPage2, "tabPage2");
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// treeViewAlphabetical
			// 
			resources.ApplyResources(this.treeViewAlphabetical, "treeViewAlphabetical");
			this.treeViewAlphabetical.HideSelection = false;
			this.treeViewAlphabetical.Name = "treeViewAlphabetical";
			this.treeViewAlphabetical.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewAlphabetical_AfterSelect);
			// 
			// FormSelectEntity
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.tabControl1);
			this.Controls.Add(this.labelPredefined);
			this.Controls.Add(this.comboBoxPredefined);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormSelectEntity";
			this.ShowInTaskbar = false;
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.TreeView treeViewInheritance;
		private System.Windows.Forms.ComboBox comboBoxPredefined;
		private System.Windows.Forms.Label labelPredefined;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TreeView treeViewAlphabetical;
	}
}