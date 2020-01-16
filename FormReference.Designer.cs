namespace IfcDoc
{
	partial class FormReference
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormReference));
			this.listViewReference = new System.Windows.Forms.ListView();
			this.columnHeaderEntity = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderAttribute = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderItem = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.buttonInsert = new System.Windows.Forms.Button();
			this.buttonRemove = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxReference = new System.Windows.Forms.TextBox();
			this.buttonProperty = new System.Windows.Forms.Button();
			this.buttonQuantity = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// listViewReference
			// 
			resources.ApplyResources(this.listViewReference, "listViewReference");
			this.listViewReference.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeaderEntity,
			this.columnHeaderAttribute,
			this.columnHeaderItem});
			this.listViewReference.FullRowSelect = true;
			this.listViewReference.Name = "listViewReference";
			this.listViewReference.UseCompatibleStateImageBehavior = false;
			this.listViewReference.View = System.Windows.Forms.View.Details;
			this.listViewReference.SelectedIndexChanged += new System.EventHandler(this.listViewReference_SelectedIndexChanged);
			// 
			// columnHeaderEntity
			// 
			resources.ApplyResources(this.columnHeaderEntity, "columnHeaderEntity");
			// 
			// columnHeaderAttribute
			// 
			resources.ApplyResources(this.columnHeaderAttribute, "columnHeaderAttribute");
			// 
			// columnHeaderItem
			// 
			resources.ApplyResources(this.columnHeaderItem, "columnHeaderItem");
			// 
			// buttonInsert
			// 
			resources.ApplyResources(this.buttonInsert, "buttonInsert");
			this.buttonInsert.Name = "buttonInsert";
			this.buttonInsert.UseVisualStyleBackColor = true;
			this.buttonInsert.Click += new System.EventHandler(this.buttonInsert_Click);
			// 
			// buttonRemove
			// 
			resources.ApplyResources(this.buttonRemove, "buttonRemove");
			this.buttonRemove.Name = "buttonRemove";
			this.buttonRemove.UseVisualStyleBackColor = true;
			this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
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
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// textBoxReference
			// 
			resources.ApplyResources(this.textBoxReference, "textBoxReference");
			this.textBoxReference.Name = "textBoxReference";
			this.textBoxReference.ReadOnly = true;
			// 
			// buttonProperty
			// 
			resources.ApplyResources(this.buttonProperty, "buttonProperty");
			this.buttonProperty.Name = "buttonProperty";
			this.buttonProperty.UseVisualStyleBackColor = true;
			this.buttonProperty.Click += new System.EventHandler(this.buttonProperty_Click);
			// 
			// buttonQuantity
			// 
			resources.ApplyResources(this.buttonQuantity, "buttonQuantity");
			this.buttonQuantity.Name = "buttonQuantity";
			this.buttonQuantity.UseVisualStyleBackColor = true;
			this.buttonQuantity.Click += new System.EventHandler(this.buttonQuantity_Click);
			// 
			// FormReference
			// 
			this.AcceptButton = this.buttonOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.buttonQuantity);
			this.Controls.Add(this.buttonProperty);
			this.Controls.Add(this.textBoxReference);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonRemove);
			this.Controls.Add(this.buttonInsert);
			this.Controls.Add(this.listViewReference);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormReference";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView listViewReference;
		private System.Windows.Forms.ColumnHeader columnHeaderEntity;
		private System.Windows.Forms.ColumnHeader columnHeaderAttribute;
		private System.Windows.Forms.ColumnHeader columnHeaderItem;
		private System.Windows.Forms.Button buttonInsert;
		private System.Windows.Forms.Button buttonRemove;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxReference;
		private System.Windows.Forms.Button buttonProperty;
		private System.Windows.Forms.Button buttonQuantity;
	}
}