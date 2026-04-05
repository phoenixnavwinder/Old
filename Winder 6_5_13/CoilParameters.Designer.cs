namespace WinderProject
{
    partial class frm_CoilParameters
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
            this.txtHubDiameter = new System.Windows.Forms.TextBox();
            this.txtFlangeWidth = new System.Windows.Forms.TextBox();
            this.txtLayers = new System.Windows.Forms.TextBox();
            this.txtBaseTurns = new System.Windows.Forms.TextBox();
            this.txtFiberDiameter = new System.Windows.Forms.TextBox();
            this.lblHubDiameter = new System.Windows.Forms.Label();
            this.lblFlangeWidth = new System.Windows.Forms.Label();
            this.lblLayers = new System.Windows.Forms.Label();
            this.lblBaseTurns = new System.Windows.Forms.Label();
            this.lblFiberDiameter = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtHubDiameter
            // 
            this.txtHubDiameter.Location = new System.Drawing.Point(150, 34);
            this.txtHubDiameter.Name = "txtHubDiameter";
            this.txtHubDiameter.Size = new System.Drawing.Size(100, 20);
            this.txtHubDiameter.TabIndex = 0;
            // 
            // txtFlangeWidth
            // 
            this.txtFlangeWidth.Location = new System.Drawing.Point(150, 60);
            this.txtFlangeWidth.Name = "txtFlangeWidth";
            this.txtFlangeWidth.Size = new System.Drawing.Size(100, 20);
            this.txtFlangeWidth.TabIndex = 1;
            // 
            // txtLayers
            // 
            this.txtLayers.Location = new System.Drawing.Point(150, 86);
            this.txtLayers.Name = "txtLayers";
            this.txtLayers.Size = new System.Drawing.Size(100, 20);
            this.txtLayers.TabIndex = 2;
            // 
            // txtBaseTurns
            // 
            this.txtBaseTurns.Location = new System.Drawing.Point(150, 112);
            this.txtBaseTurns.Name = "txtBaseTurns";
            this.txtBaseTurns.Size = new System.Drawing.Size(100, 20);
            this.txtBaseTurns.TabIndex = 3;
            // 
            // txtFiberDiameter
            // 
            this.txtFiberDiameter.Location = new System.Drawing.Point(150, 138);
            this.txtFiberDiameter.Name = "txtFiberDiameter";
            this.txtFiberDiameter.Size = new System.Drawing.Size(100, 20);
            this.txtFiberDiameter.TabIndex = 4;
            // 
            // lblHubDiameter
            // 
            this.lblHubDiameter.AutoSize = true;
            this.lblHubDiameter.Location = new System.Drawing.Point(29, 34);
            this.lblHubDiameter.Name = "lblHubDiameter";
            this.lblHubDiameter.Size = new System.Drawing.Size(115, 13);
            this.lblHubDiameter.TabIndex = 5;
            this.lblHubDiameter.Text = "Hub Diameter (inches):";
            // 
            // lblFlangeWidth
            // 
            this.lblFlangeWidth.AutoSize = true;
            this.lblFlangeWidth.Location = new System.Drawing.Point(29, 60);
            this.lblFlangeWidth.Name = "lblFlangeWidth";
            this.lblFlangeWidth.Size = new System.Drawing.Size(113, 13);
            this.lblFlangeWidth.TabIndex = 6;
            this.lblFlangeWidth.Text = "Flange Width (inches):";
            // 
            // lblLayers
            // 
            this.lblLayers.AutoSize = true;
            this.lblLayers.Location = new System.Drawing.Point(29, 86);
            this.lblLayers.Name = "lblLayers";
            this.lblLayers.Size = new System.Drawing.Size(41, 13);
            this.lblLayers.TabIndex = 7;
            this.lblLayers.Text = "Layers:";
            // 
            // lblBaseTurns
            // 
            this.lblBaseTurns.AutoSize = true;
            this.lblBaseTurns.Location = new System.Drawing.Point(29, 112);
            this.lblBaseTurns.Name = "lblBaseTurns";
            this.lblBaseTurns.Size = new System.Drawing.Size(64, 13);
            this.lblBaseTurns.TabIndex = 8;
            this.lblBaseTurns.Text = "Base Turns:";
            // 
            // lblFiberDiameter
            // 
            this.lblFiberDiameter.AutoSize = true;
            this.lblFiberDiameter.Location = new System.Drawing.Point(29, 138);
            this.lblFiberDiameter.Name = "lblFiberDiameter";
            this.lblFiberDiameter.Size = new System.Drawing.Size(118, 13);
            this.lblFiberDiameter.TabIndex = 9;
            this.lblFiberDiameter.Text = "Fiber Diameter (inches):";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(32, 187);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(218, 23);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // btnUndo
            // 
            this.btnUndo.Location = new System.Drawing.Point(32, 228);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(218, 23);
            this.btnUndo.TabIndex = 11;
            this.btnUndo.Text = "Undo Changes";
            this.btnUndo.UseVisualStyleBackColor = true;
            // 
            // frm_CoilParameters
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 274);
            this.Controls.Add(this.btnUndo);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblFiberDiameter);
            this.Controls.Add(this.lblBaseTurns);
            this.Controls.Add(this.lblLayers);
            this.Controls.Add(this.lblFlangeWidth);
            this.Controls.Add(this.lblHubDiameter);
            this.Controls.Add(this.txtFiberDiameter);
            this.Controls.Add(this.txtBaseTurns);
            this.Controls.Add(this.txtLayers);
            this.Controls.Add(this.txtFlangeWidth);
            this.Controls.Add(this.txtHubDiameter);
            this.Name = "frm_CoilParameters";
            this.Text = "Coil Parameters";
            this.Load += new System.EventHandler(this.frm_CoilParameters_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtHubDiameter;
        private System.Windows.Forms.TextBox txtFlangeWidth;
        private System.Windows.Forms.TextBox txtLayers;
        private System.Windows.Forms.TextBox txtBaseTurns;
        private System.Windows.Forms.TextBox txtFiberDiameter;
        private System.Windows.Forms.Label lblHubDiameter;
        private System.Windows.Forms.Label lblFlangeWidth;
        private System.Windows.Forms.Label lblLayers;
        private System.Windows.Forms.Label lblBaseTurns;
        private System.Windows.Forms.Label lblFiberDiameter;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnUndo;
    }
}