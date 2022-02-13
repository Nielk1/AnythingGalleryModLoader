
namespace AnythingGalleryModManager
{
    partial class Form1
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
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnInstallLoader = new System.Windows.Forms.Button();
            this.btnUninstallLoader = new System.Windows.Forms.Button();
            this.clbMods = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(45, 13);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Loading";
            // 
            // btnInstallLoader
            // 
            this.btnInstallLoader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInstallLoader.Enabled = false;
            this.btnInstallLoader.Location = new System.Drawing.Point(12, 49);
            this.btnInstallLoader.Name = "btnInstallLoader";
            this.btnInstallLoader.Size = new System.Drawing.Size(670, 23);
            this.btnInstallLoader.TabIndex = 1;
            this.btnInstallLoader.Text = "Install Mod Loader";
            this.btnInstallLoader.UseVisualStyleBackColor = true;
            this.btnInstallLoader.Click += new System.EventHandler(this.btnInstallLoader_Click);
            // 
            // btnUninstallLoader
            // 
            this.btnUninstallLoader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUninstallLoader.Enabled = false;
            this.btnUninstallLoader.Location = new System.Drawing.Point(12, 78);
            this.btnUninstallLoader.Name = "btnUninstallLoader";
            this.btnUninstallLoader.Size = new System.Drawing.Size(670, 23);
            this.btnUninstallLoader.TabIndex = 2;
            this.btnUninstallLoader.Text = "Remove Mod Loader and Mods";
            this.btnUninstallLoader.UseVisualStyleBackColor = true;
            this.btnUninstallLoader.Click += new System.EventHandler(this.btnUninstallLoader_Click);
            // 
            // clbMods
            // 
            this.clbMods.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.clbMods.Enabled = false;
            this.clbMods.FormattingEnabled = true;
            this.clbMods.Location = new System.Drawing.Point(15, 107);
            this.clbMods.Name = "clbMods";
            this.clbMods.Size = new System.Drawing.Size(667, 184);
            this.clbMods.TabIndex = 3;
            this.clbMods.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbMods_ItemCheck);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 302);
            this.Controls.Add(this.clbMods);
            this.Controls.Add(this.btnUninstallLoader);
            this.Controls.Add(this.btnInstallLoader);
            this.Controls.Add(this.lblStatus);
            this.Name = "Form1";
            this.Text = "The Anything Gallery Mod Manager";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnInstallLoader;
        private System.Windows.Forms.Button btnUninstallLoader;
        private System.Windows.Forms.CheckedListBox clbMods;
    }
}

