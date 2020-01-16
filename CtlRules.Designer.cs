namespace IfcDoc
{
    partial class CtlRules
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CtlRules));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonTemplateInsert = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonTemplateRemove = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonTemplateUpdate = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRuleRef = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonMoveUp = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonMoveDown = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSetDefault = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonProperty = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonQuantity = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonTemplate = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabelTemplate = new System.Windows.Forms.ToolStripLabel();
            this.treeViewTemplate = new System.Windows.Forms.TreeView();
            this.imageListRules = new System.Windows.Forms.ImageList(this.components);
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonTemplateInsert,
            this.toolStripButtonTemplateRemove,
            this.toolStripButtonTemplateUpdate,
            this.toolStripButtonRuleRef,
            this.toolStripSeparator1,
            this.toolStripButtonMoveUp,
            this.toolStripButtonMoveDown,
            this.toolStripButtonSetDefault,
            this.toolStripButtonProperty,
            this.toolStripButtonQuantity,
            this.toolStripButtonTemplate,
            this.toolStripSeparator2,
            this.toolStripLabelTemplate});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1067, 31);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonTemplateInsert
            // 
            this.toolStripButtonTemplateInsert.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonTemplateInsert.Enabled = false;
            this.toolStripButtonTemplateInsert.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonTemplateInsert.Image")));
            this.toolStripButtonTemplateInsert.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonTemplateInsert.Name = "toolStripButtonTemplateInsert";
            this.toolStripButtonTemplateInsert.Size = new System.Drawing.Size(29, 28);
            this.toolStripButtonTemplateInsert.Text = "Insert";
            this.toolStripButtonTemplateInsert.ToolTipText = "Inserts an entity reference or attribute reference.";
            this.toolStripButtonTemplateInsert.Click += new System.EventHandler(this.toolStripButtonTemplateInsert_Click);
            // 
            // toolStripButtonTemplateRemove
            // 
            this.toolStripButtonTemplateRemove.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonTemplateRemove.Enabled = false;
            this.toolStripButtonTemplateRemove.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonTemplateRemove.Image")));
            this.toolStripButtonTemplateRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonTemplateRemove.Name = "toolStripButtonTemplateRemove";
            this.toolStripButtonTemplateRemove.Size = new System.Drawing.Size(29, 28);
            this.toolStripButtonTemplateRemove.Text = "Remove";
            this.toolStripButtonTemplateRemove.ToolTipText = "Removes the selected reference";
            this.toolStripButtonTemplateRemove.Click += new System.EventHandler(this.toolStripButtonTemplateRemove_Click);
            // 
            // toolStripButtonTemplateUpdate
            // 
            this.toolStripButtonTemplateUpdate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonTemplateUpdate.Enabled = false;
            this.toolStripButtonTemplateUpdate.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonTemplateUpdate.Image")));
            this.toolStripButtonTemplateUpdate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonTemplateUpdate.Name = "toolStripButtonTemplateUpdate";
            this.toolStripButtonTemplateUpdate.Size = new System.Drawing.Size(29, 28);
            this.toolStripButtonTemplateUpdate.Text = "Update";
            this.toolStripButtonTemplateUpdate.ToolTipText = "Edits the selected reference";
            this.toolStripButtonTemplateUpdate.Click += new System.EventHandler(this.toolStripButtonTemplateUpdate_Click);
            // 
            // toolStripButtonRuleRef
            // 
            this.toolStripButtonRuleRef.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRuleRef.Enabled = false;
            this.toolStripButtonRuleRef.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRuleRef.Image")));
            this.toolStripButtonRuleRef.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRuleRef.Name = "toolStripButtonRuleRef";
            this.toolStripButtonRuleRef.Size = new System.Drawing.Size(29, 28);
            this.toolStripButtonRuleRef.Text = "Reference";
            this.toolStripButtonRuleRef.ToolTipText = "Links another concept template for the selected reference.";
            this.toolStripButtonRuleRef.Click += new System.EventHandler(this.toolStripButtonRuleRef_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // toolStripButtonMoveUp
            // 
            this.toolStripButtonMoveUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonMoveUp.Enabled = false;
            this.toolStripButtonMoveUp.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonMoveUp.Image")));
            this.toolStripButtonMoveUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonMoveUp.Name = "toolStripButtonMoveUp";
            this.toolStripButtonMoveUp.Size = new System.Drawing.Size(29, 28);
            this.toolStripButtonMoveUp.Text = "Move Up";
            this.toolStripButtonMoveUp.Click += new System.EventHandler(this.toolStripButtonMoveUp_Click);
            // 
            // toolStripButtonMoveDown
            // 
            this.toolStripButtonMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonMoveDown.Enabled = false;
            this.toolStripButtonMoveDown.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonMoveDown.Image")));
            this.toolStripButtonMoveDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonMoveDown.Name = "toolStripButtonMoveDown";
            this.toolStripButtonMoveDown.Size = new System.Drawing.Size(29, 28);
            this.toolStripButtonMoveDown.Text = "Move Down";
            this.toolStripButtonMoveDown.Click += new System.EventHandler(this.toolStripButtonMoveDown_Click);
            // 
            // toolStripButtonSetDefault
            // 
            this.toolStripButtonSetDefault.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSetDefault.Enabled = false;
            this.toolStripButtonSetDefault.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSetDefault.Image")));
            this.toolStripButtonSetDefault.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSetDefault.Name = "toolStripButtonSetDefault";
            this.toolStripButtonSetDefault.Size = new System.Drawing.Size(29, 28);
            this.toolStripButtonSetDefault.Text = "toolStripButton1";
            this.toolStripButtonSetDefault.ToolTipText = "Mark as default, indicating value will be used in documentation, validation repor" +
    "ts, and generated APIs";
            this.toolStripButtonSetDefault.Click += new System.EventHandler(this.toolStripButtonSetDefault_Click);
            // 
            // toolStripButtonProperty
            // 
            this.toolStripButtonProperty.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonProperty.Image")));
            this.toolStripButtonProperty.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonProperty.Name = "toolStripButtonProperty";
            this.toolStripButtonProperty.Size = new System.Drawing.Size(98, 28);
            this.toolStripButtonProperty.Text = "Property...";
            this.toolStripButtonProperty.ToolTipText = "Maps to an existing property definition.";
            this.toolStripButtonProperty.Click += new System.EventHandler(this.toolStripButtonProperty_Click);
            // 
            // toolStripButtonQuantity
            // 
            this.toolStripButtonQuantity.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonQuantity.Image")));
            this.toolStripButtonQuantity.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonQuantity.Name = "toolStripButtonQuantity";
            this.toolStripButtonQuantity.Size = new System.Drawing.Size(98, 28);
            this.toolStripButtonQuantity.Text = "Quantity...";
            this.toolStripButtonQuantity.ToolTipText = "Maps to an existing quantity definition.";
            this.toolStripButtonQuantity.Click += new System.EventHandler(this.toolStripButtonQuantity_Click);
            // 
            // toolStripButtonTemplate
            // 
            this.toolStripButtonTemplate.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonTemplate.Image")));
            this.toolStripButtonTemplate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonTemplate.Name = "toolStripButtonTemplate";
            this.toolStripButtonTemplate.Size = new System.Drawing.Size(104, 28);
            this.toolStripButtonTemplate.Text = "Template...";
            this.toolStripButtonTemplate.ToolTipText = "Maps to an existing concept template.";
            this.toolStripButtonTemplate.Click += new System.EventHandler(this.toolStripButtonTemplate_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 31);
            // 
            // toolStripLabelTemplate
            // 
            this.toolStripLabelTemplate.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabelTemplate.Name = "toolStripLabelTemplate";
            this.toolStripLabelTemplate.Size = new System.Drawing.Size(0, 28);
            // 
            // treeViewTemplate
            // 
            this.treeViewTemplate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewTemplate.FullRowSelect = true;
            this.treeViewTemplate.HideSelection = false;
            this.treeViewTemplate.ImageIndex = 0;
            this.treeViewTemplate.ImageList = this.imageListRules;
            this.treeViewTemplate.Location = new System.Drawing.Point(0, 31);
            this.treeViewTemplate.Margin = new System.Windows.Forms.Padding(4);
            this.treeViewTemplate.Name = "treeViewTemplate";
            this.treeViewTemplate.SelectedImageIndex = 0;
            this.treeViewTemplate.Size = new System.Drawing.Size(1067, 461);
            this.treeViewTemplate.TabIndex = 3;
            this.treeViewTemplate.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewTemplate_AfterSelect);
            // 
            // imageListRules
            // 
            this.imageListRules.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListRules.ImageStream")));
            this.imageListRules.TransparentColor = System.Drawing.Color.Fuchsia;
            this.imageListRules.Images.SetKeyName(0, "DocEntity.bmp");
            this.imageListRules.Images.SetKeyName(1, "DocAttribute.bmp");
            this.imageListRules.Images.SetKeyName(2, "DocQuantity.bmp");
            this.imageListRules.Images.SetKeyName(3, "DocTemplateDefinition.bmp");
            // 
            // CtlRules
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.treeViewTemplate);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "CtlRules";
            this.Size = new System.Drawing.Size(1067, 492);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonTemplateInsert;
        private System.Windows.Forms.ToolStripButton toolStripButtonTemplateRemove;
        private System.Windows.Forms.ToolStripButton toolStripButtonTemplateUpdate;
        private System.Windows.Forms.TreeView treeViewTemplate;
        private System.Windows.Forms.ToolStripButton toolStripButtonMoveUp;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButtonMoveDown;
        private System.Windows.Forms.ImageList imageListRules;
        private System.Windows.Forms.ToolStripButton toolStripButtonRuleRef;
        private System.Windows.Forms.ToolStripButton toolStripButtonSetDefault;
        private System.Windows.Forms.ToolStripButton toolStripButtonProperty;
        private System.Windows.Forms.ToolStripButton toolStripButtonQuantity;
        private System.Windows.Forms.ToolStripButton toolStripButtonTemplate;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripLabel toolStripLabelTemplate;
    }
}
