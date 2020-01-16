namespace IfcDoc
{
	partial class FormProperties
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormProperties));
			this.buttonClose = new System.Windows.Forms.Button();
			this.ctlProperties = new IfcDoc.CtlProperties();
			this.SuspendLayout();
			// 
			// buttonClose
			// 
			resources.ApplyResources(this.buttonClose, "buttonClose");
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.UseVisualStyleBackColor = true;
			// 
			// ctlProperties
			// 
			resources.ApplyResources(this.ctlProperties, "ctlProperties");
			this.ctlProperties.CurrentInstance = null;
			this.ctlProperties.CurrentPopulation = null;
			this.ctlProperties.Name = "ctlProperties";
			this.ctlProperties.SelectedAttribute = null;
			this.ctlProperties.SelectedRule = null;
			this.ctlProperties.Load += new System.EventHandler(this.ctlProperties_Load);
			// 
			// FormProperties
			// 
			this.AcceptButton = this.buttonClose;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonClose;
			this.Controls.Add(this.ctlProperties);
			this.Controls.Add(this.buttonClose);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormProperties";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button buttonClose;
		private CtlProperties ctlProperties;
	}
}