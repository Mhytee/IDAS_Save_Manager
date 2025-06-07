using System.Windows.Forms;

namespace IDAS_Save_System
{
    partial class Form2
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
        public void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            lblSelect = new Label();
            comboSaves = new ComboBox();
            btnNoSave = new Button();
            btnSetPath = new Button();
            fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            btnLaunch = new Button();
            btnRename = new Button();
            btnDelete = new Button();
            btnDuplicate = new Button();
            DevName = new TextBox();
            label1 = new Label();
            picID6 = new PictureBox();
            picID8 = new PictureBox();
            picID7 = new PictureBox();
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            textBox3 = new TextBox();
            chkAutoNameOnly = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)fileSystemWatcher1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picID6).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picID8).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picID7).BeginInit();
            SuspendLayout();
            // 
            // lblSelect
            // 
            lblSelect.Anchor = AnchorStyles.Top;
            lblSelect.AutoSize = true;
            lblSelect.Font = new System.Drawing.Font("Unispace", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            lblSelect.Location = new System.Drawing.Point(81, 252);
            lblSelect.Name = "lblSelect";
            lblSelect.Size = new System.Drawing.Size(298, 29);
            lblSelect.TabIndex = 0;
            lblSelect.Text = "Select a save file:";
            lblSelect.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // comboSaves
            // 
            comboSaves.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboSaves.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSaves.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            comboSaves.FormattingEnabled = true;
            comboSaves.Location = new System.Drawing.Point(51, 284);
            comboSaves.MaxDropDownItems = 50;
            comboSaves.Name = "comboSaves";
            comboSaves.Size = new System.Drawing.Size(351, 23);
            comboSaves.TabIndex = 1;
            // 
            // btnNoSave
            // 
            btnNoSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnNoSave.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnNoSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnNoSave.Location = new System.Drawing.Point(12, 453);
            btnNoSave.Name = "btnNoSave";
            btnNoSave.Size = new System.Drawing.Size(430, 64);
            btnNoSave.TabIndex = 3;
            btnNoSave.Text = "Continue Without Save";
            btnNoSave.UseVisualStyleBackColor = true;
            btnNoSave.Click += btnNoSave_Click;
            // 
            // btnSetPath
            // 
            btnSetPath.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnSetPath.Dock = DockStyle.Top;
            btnSetPath.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btnSetPath.Location = new System.Drawing.Point(0, 0);
            btnSetPath.Margin = new Padding(0);
            btnSetPath.Name = "btnSetPath";
            btnSetPath.Size = new System.Drawing.Size(454, 23);
            btnSetPath.TabIndex = 4;
            btnSetPath.Text = "Set TeknoParrot Folder";
            btnSetPath.UseVisualStyleBackColor = true;
            btnSetPath.Click += btnSetPath_Click;
            // 
            // fileSystemWatcher1
            // 
            fileSystemWatcher1.EnableRaisingEvents = true;
            fileSystemWatcher1.SynchronizingObject = this;
            // 
            // btnLaunch
            // 
            btnLaunch.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnLaunch.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnLaunch.BackColor = System.Drawing.Color.SpringGreen;
            btnLaunch.FlatAppearance.BorderColor = System.Drawing.Color.SpringGreen;
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Font = new System.Drawing.Font("Unispace", 18F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            btnLaunch.Location = new System.Drawing.Point(12, 383);
            btnLaunch.Margin = new Padding(0);
            btnLaunch.Name = "btnLaunch";
            btnLaunch.Size = new System.Drawing.Size(430, 64);
            btnLaunch.TabIndex = 5;
            btnLaunch.Text = "Launch Game!";
            btnLaunch.UseVisualStyleBackColor = false;
            btnLaunch.Click += btnLaunch_Click;
            // 
            // btnRename
            // 
            btnRename.Anchor = AnchorStyles.Top;
            btnRename.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRename.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnRename.Location = new System.Drawing.Point(51, 313);
            btnRename.Name = "btnRename";
            btnRename.Size = new System.Drawing.Size(113, 37);
            btnRename.TabIndex = 6;
            btnRename.Text = "Rename";
            btnRename.UseVisualStyleBackColor = true;
            btnRename.Click += btnRename_Click;
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Top;
            btnDelete.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnDelete.BackColor = System.Drawing.Color.IndianRed;
            btnDelete.FlatAppearance.BorderColor = System.Drawing.Color.IndianRed;
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnDelete.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            btnDelete.Location = new System.Drawing.Point(289, 313);
            btnDelete.Margin = new Padding(0);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(113, 37);
            btnDelete.TabIndex = 7;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = false;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnDuplicate
            // 
            btnDuplicate.Anchor = AnchorStyles.Top;
            btnDuplicate.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnDuplicate.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnDuplicate.Location = new System.Drawing.Point(170, 313);
            btnDuplicate.Name = "btnDuplicate";
            btnDuplicate.Size = new System.Drawing.Size(113, 37);
            btnDuplicate.TabIndex = 8;
            btnDuplicate.Text = "Duplicate";
            btnDuplicate.UseVisualStyleBackColor = true;
            btnDuplicate.Click += btnDuplicate_Click;
            // 
            // DevName
            // 
            DevName.BackColor = System.Drawing.SystemColors.Control;
            DevName.BorderStyle = BorderStyle.None;
            DevName.Font = new System.Drawing.Font("Segoe UI", 7F);
            DevName.Location = new System.Drawing.Point(12, 26);
            DevName.Margin = new Padding(4, 3, 3, 3);
            DevName.Name = "DevName";
            DevName.Size = new System.Drawing.Size(454, 13);
            DevName.TabIndex = 9;
            DevName.Text = "Developed by Mhytee";
            DevName.TextChanged += DevName_TextChanged;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top;
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Unispace", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(131, 35);
            label1.Name = "label1";
            label1.Padding = new Padding(0, 0, 0, 3);
            label1.Size = new System.Drawing.Size(193, 32);
            label1.TabIndex = 10;
            label1.Text = "Game Select:";
            label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // picID6
            // 
            picID6.Anchor = AnchorStyles.Top;
            picID6.Image = (System.Drawing.Image)resources.GetObject("picID6.Image");
            picID6.Location = new System.Drawing.Point(51, 70);
            picID6.Name = "picID6";
            picID6.Size = new System.Drawing.Size(113, 149);
            picID6.SizeMode = PictureBoxSizeMode.StretchImage;
            picID6.TabIndex = 11;
            picID6.TabStop = false;
            // 
            // picID8
            // 
            picID8.Anchor = AnchorStyles.Top;
            picID8.Image = (System.Drawing.Image)resources.GetObject("picID8.Image");
            picID8.Location = new System.Drawing.Point(289, 70);
            picID8.Name = "picID8";
            picID8.Size = new System.Drawing.Size(113, 149);
            picID8.SizeMode = PictureBoxSizeMode.StretchImage;
            picID8.TabIndex = 12;
            picID8.TabStop = false;
            // 
            // picID7
            // 
            picID7.Anchor = AnchorStyles.Top;
            picID7.Image = (System.Drawing.Image)resources.GetObject("picID7.Image");
            picID7.Location = new System.Drawing.Point(170, 70);
            picID7.Name = "picID7";
            picID7.Size = new System.Drawing.Size(113, 149);
            picID7.SizeMode = PictureBoxSizeMode.StretchImage;
            picID7.TabIndex = 13;
            picID7.TabStop = false;
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Top;
            textBox1.BackColor = System.Drawing.SystemColors.ButtonFace;
            textBox1.BorderStyle = BorderStyle.None;
            textBox1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            textBox1.Location = new System.Drawing.Point(51, 225);
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.Size = new System.Drawing.Size(113, 16);
            textBox1.TabIndex = 14;
            textBox1.TabStop = false;
            textBox1.Text = "ID6: Double Aces";
            textBox1.TextAlign = HorizontalAlignment.Center;
            // 
            // textBox2
            // 
            textBox2.Anchor = AnchorStyles.Top;
            textBox2.BackColor = System.Drawing.SystemColors.ButtonFace;
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            textBox2.Location = new System.Drawing.Point(170, 225);
            textBox2.Name = "textBox2";
            textBox2.ReadOnly = true;
            textBox2.Size = new System.Drawing.Size(113, 16);
            textBox2.TabIndex = 15;
            textBox2.TabStop = false;
            textBox2.Text = "ID7: AAX";
            textBox2.TextAlign = HorizontalAlignment.Center;
            // 
            // textBox3
            // 
            textBox3.Anchor = AnchorStyles.Top;
            textBox3.BackColor = System.Drawing.SystemColors.ButtonFace;
            textBox3.BorderStyle = BorderStyle.None;
            textBox3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            textBox3.Location = new System.Drawing.Point(289, 225);
            textBox3.Name = "textBox3";
            textBox3.ReadOnly = true;
            textBox3.Size = new System.Drawing.Size(113, 16);
            textBox3.TabIndex = 16;
            textBox3.TabStop = false;
            textBox3.Text = "ID8: Infinity";
            textBox3.TextAlign = HorizontalAlignment.Center;
            // 
            // chkAutoNameOnly
            // 
            chkAutoNameOnly.AutoSize = true;
            chkAutoNameOnly.Font = new System.Drawing.Font("Segoe UI", 12F);
            chkAutoNameOnly.Location = new System.Drawing.Point(80, 355);
            chkAutoNameOnly.Name = "chkAutoNameOnly";
            chkAutoNameOnly.Size = new System.Drawing.Size(299, 25);
            chkAutoNameOnly.TabIndex = 19;
            chkAutoNameOnly.Text = "Always auto-name (skip name prompt)";
            chkAutoNameOnly.UseVisualStyleBackColor = true;
            chkAutoNameOnly.CheckedChanged += chkAutoNameOnly_CheckedChanged;
            // 
            // Form2
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(454, 529);
            Controls.Add(chkAutoNameOnly);
            Controls.Add(textBox3);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
            Controls.Add(picID7);
            Controls.Add(picID8);
            Controls.Add(picID6);
            Controls.Add(label1);
            Controls.Add(DevName);
            Controls.Add(btnDuplicate);
            Controls.Add(btnDelete);
            Controls.Add(btnRename);
            Controls.Add(btnLaunch);
            Controls.Add(btnSetPath);
            Controls.Add(btnNoSave);
            Controls.Add(comboSaves);
            Controls.Add(lblSelect);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(470, 501);
            Name = "Form2";
            Text = "IDAS Save Manager";
            Load += Form2_Load;
            ((System.ComponentModel.ISupportInitialize)fileSystemWatcher1).EndInit();
            ((System.ComponentModel.ISupportInitialize)picID6).EndInit();
            ((System.ComponentModel.ISupportInitialize)picID8).EndInit();
            ((System.ComponentModel.ISupportInitialize)picID7).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblSelect;
        private System.Windows.Forms.ComboBox comboSaves;
        private System.Windows.Forms.Button btnNoSave;
        private System.Windows.Forms.Button btnSetPath;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.Windows.Forms.Button btnLaunch;
        private System.Windows.Forms.Button btnDuplicate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.TextBox DevName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox picID6;
        private System.Windows.Forms.PictureBox picID7;
        private System.Windows.Forms.PictureBox picID8;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox1;
        private CheckBox chkAutoNameOnly;
    }
}