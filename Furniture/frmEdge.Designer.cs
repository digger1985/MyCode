using SolidWorks.Interop.sldworks;

namespace Furniture
{
    partial class FrmEdge
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
            _swAsmDoc.NewSelectionNotify -= new DAssemblyDocEvents_NewSelectionNotifyEventHandler(this.NewSelection);
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
            this.btnOK = new System.Windows.Forms.Button();
            this.pbEdgeMain = new System.Windows.Forms.PictureBox();
            this.cbExist21 = new System.Windows.Forms.ComboBox();
            this.cbExist11 = new System.Windows.Forms.ComboBox();
            this.cbExist22 = new System.Windows.Forms.ComboBox();
            this.cbExist12 = new System.Windows.Forms.ComboBox();
            this.cbColor11 = new System.Windows.Forms.ComboBox();
            this.cbColor21 = new System.Windows.Forms.ComboBox();
            this.cbColor22 = new System.Windows.Forms.ComboBox();
            this.cbColor12 = new System.Windows.Forms.ComboBox();
            this.gbEdge11 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pbEdge11 = new System.Windows.Forms.PictureBox();
            this.gbEdge22 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.gbEdge12 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.gbEdge21 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.pbEdge21 = new System.Windows.Forms.PictureBox();
            this.pbEdge22 = new System.Windows.Forms.PictureBox();
            this.pbEdge12 = new System.Windows.Forms.PictureBox();
            this.useForAll = new System.Windows.Forms.CheckBox();
            this.lbMainNameLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbEdgeMain)).BeginInit();
            this.gbEdge11.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbEdge11)).BeginInit();
            this.gbEdge22.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.gbEdge12.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.gbEdge21.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbEdge21)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbEdge22)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbEdge12)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(163, 324);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 24);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOkClick);
            // 
            // pbEdgeMain
            // 
            this.pbEdgeMain.Image = global::Furniture.Properties.Resources.HorizontalFiberDirection;
            this.pbEdgeMain.Location = new System.Drawing.Point(128, 101);
            this.pbEdgeMain.Name = "pbEdgeMain";
            this.pbEdgeMain.Size = new System.Drawing.Size(160, 159);
            this.pbEdgeMain.TabIndex = 7;
            this.pbEdgeMain.TabStop = false;
            // 
            // cbExist21
            // 
            this.cbExist21.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbExist21.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbExist21.FormattingEnabled = true;
            this.cbExist21.Location = new System.Drawing.Point(3, 6);
            this.cbExist21.Name = "cbExist21";
            this.cbExist21.Size = new System.Drawing.Size(45, 21);
            this.cbExist21.TabIndex = 12;
            this.cbExist21.SelectedIndexChanged += new System.EventHandler(this.ExistChanged);
            // 
            // cbExist11
            // 
            this.cbExist11.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbExist11.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbExist11.FormattingEnabled = true;
            this.cbExist11.Location = new System.Drawing.Point(3, 6);
            this.cbExist11.Name = "cbExist11";
            this.cbExist11.Size = new System.Drawing.Size(45, 21);
            this.cbExist11.TabIndex = 13;
            this.cbExist11.SelectedIndexChanged += new System.EventHandler(this.ExistChanged);
            // 
            // cbExist22
            // 
            this.cbExist22.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbExist22.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbExist22.FormattingEnabled = true;
            this.cbExist22.Location = new System.Drawing.Point(3, 6);
            this.cbExist22.Name = "cbExist22";
            this.cbExist22.Size = new System.Drawing.Size(45, 21);
            this.cbExist22.TabIndex = 14;
            this.cbExist22.SelectedIndexChanged += new System.EventHandler(this.ExistChanged);
            // 
            // cbExist12
            // 
            this.cbExist12.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbExist12.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbExist12.FormattingEnabled = true;
            this.cbExist12.Location = new System.Drawing.Point(3, 6);
            this.cbExist12.Name = "cbExist12";
            this.cbExist12.Size = new System.Drawing.Size(45, 21);
            this.cbExist12.TabIndex = 15;
            this.cbExist12.SelectedIndexChanged += new System.EventHandler(this.ExistChanged);
            // 
            // cbColor11
            // 
            this.cbColor11.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cbColor11.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbColor11.FormattingEnabled = true;
            this.cbColor11.Location = new System.Drawing.Point(58, 6);
            this.cbColor11.Name = "cbColor11";
            this.cbColor11.Size = new System.Drawing.Size(44, 21);
            this.cbColor11.TabIndex = 16;
            this.cbColor11.SelectedIndexChanged += new System.EventHandler(this.cbColorIndexChanged);
            // 
            // cbColor21
            // 
            this.cbColor21.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cbColor21.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbColor21.FormattingEnabled = true;
            this.cbColor21.Location = new System.Drawing.Point(58, 6);
            this.cbColor21.Name = "cbColor21";
            this.cbColor21.Size = new System.Drawing.Size(44, 21);
            this.cbColor21.TabIndex = 17;
            this.cbColor21.SelectedIndexChanged += new System.EventHandler(this.cbColorIndexChanged);
            // 
            // cbColor22
            // 
            this.cbColor22.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cbColor22.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbColor22.FormattingEnabled = true;
            this.cbColor22.Location = new System.Drawing.Point(58, 6);
            this.cbColor22.Name = "cbColor22";
            this.cbColor22.Size = new System.Drawing.Size(44, 21);
            this.cbColor22.TabIndex = 18;
            this.cbColor22.SelectedIndexChanged += new System.EventHandler(this.cbColorIndexChanged);
            // 
            // cbColor12
            // 
            this.cbColor12.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cbColor12.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbColor12.FormattingEnabled = true;
            this.cbColor12.Location = new System.Drawing.Point(58, 6);
            this.cbColor12.Name = "cbColor12";
            this.cbColor12.Size = new System.Drawing.Size(44, 21);
            this.cbColor12.TabIndex = 19;
            this.cbColor12.SelectedIndexChanged += new System.EventHandler(this.cbColorIndexChanged);
            // 
            // gbEdge11
            // 
            this.gbEdge11.Controls.Add(this.tableLayoutPanel1);
            this.gbEdge11.Enabled = false;
            this.gbEdge11.Location = new System.Drawing.Point(148, 266);
            this.gbEdge11.Name = "gbEdge11";
            this.gbEdge11.Size = new System.Drawing.Size(111, 52);
            this.gbEdge11.TabIndex = 20;
            this.gbEdge11.TabStop = false;
            this.gbEdge11.Text = "Кромка 11";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.cbExist11, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.cbColor11, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(105, 33);
            this.tableLayoutPanel1.TabIndex = 28;
            // 
            // pbEdge11
            // 
            this.pbEdge11.Location = new System.Drawing.Point(180, 235);
            this.pbEdge11.Name = "pbEdge11";
            this.pbEdge11.Size = new System.Drawing.Size(50, 25);
            this.pbEdge11.TabIndex = 24;
            this.pbEdge11.TabStop = false;
            // 
            // gbEdge22
            // 
            this.gbEdge22.Controls.Add(this.tableLayoutPanel2);
            this.gbEdge22.Enabled = false;
            this.gbEdge22.Location = new System.Drawing.Point(294, 155);
            this.gbEdge22.Name = "gbEdge22";
            this.gbEdge22.Size = new System.Drawing.Size(111, 52);
            this.gbEdge22.TabIndex = 21;
            this.gbEdge22.TabStop = false;
            this.gbEdge22.Text = "Кромка 22";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.cbColor22, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.cbExist22, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(105, 33);
            this.tableLayoutPanel2.TabIndex = 28;
            // 
            // gbEdge12
            // 
            this.gbEdge12.Controls.Add(this.tableLayoutPanel3);
            this.gbEdge12.Enabled = false;
            this.gbEdge12.Location = new System.Drawing.Point(151, 44);
            this.gbEdge12.Name = "gbEdge12";
            this.gbEdge12.Size = new System.Drawing.Size(111, 52);
            this.gbEdge12.TabIndex = 22;
            this.gbEdge12.TabStop = false;
            this.gbEdge12.Text = "Кромка 12";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.cbColor12, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.cbExist12, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(105, 33);
            this.tableLayoutPanel3.TabIndex = 28;
            // 
            // gbEdge21
            // 
            this.gbEdge21.Controls.Add(this.tableLayoutPanel4);
            this.gbEdge21.Enabled = false;
            this.gbEdge21.Location = new System.Drawing.Point(11, 152);
            this.gbEdge21.Name = "gbEdge21";
            this.gbEdge21.Size = new System.Drawing.Size(111, 52);
            this.gbEdge21.TabIndex = 23;
            this.gbEdge21.TabStop = false;
            this.gbEdge21.Text = "Кромка 21";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Controls.Add(this.cbExist21, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.cbColor21, 1, 0);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 1;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(105, 33);
            this.tableLayoutPanel4.TabIndex = 28;
            // 
            // pbEdge21
            // 
            this.pbEdge21.Location = new System.Drawing.Point(128, 155);
            this.pbEdge21.Name = "pbEdge21";
            this.pbEdge21.Size = new System.Drawing.Size(25, 50);
            this.pbEdge21.TabIndex = 18;
            this.pbEdge21.TabStop = false;
            // 
            // pbEdge22
            // 
            this.pbEdge22.Location = new System.Drawing.Point(260, 155);
            this.pbEdge22.Name = "pbEdge22";
            this.pbEdge22.Size = new System.Drawing.Size(25, 50);
            this.pbEdge22.TabIndex = 25;
            this.pbEdge22.TabStop = false;
            // 
            // pbEdge12
            // 
            this.pbEdge12.Location = new System.Drawing.Point(180, 101);
            this.pbEdge12.Name = "pbEdge12";
            this.pbEdge12.Size = new System.Drawing.Size(50, 25);
            this.pbEdge12.TabIndex = 26;
            this.pbEdge12.TabStop = false;
            // 
            // useForAll
            // 
            this.useForAll.AutoSize = true;
            this.useForAll.Location = new System.Drawing.Point(18, 53);
            this.useForAll.Name = "useForAll";
            this.useForAll.Size = new System.Drawing.Size(113, 43);
            this.useForAll.TabIndex = 27;
            this.useForAll.Text = "Изменять декор \r\nвсех кромок\r\nодновременно";
            this.useForAll.UseVisualStyleBackColor = true;
            // 
            // lbMainNameLabel
            // 
            this.lbMainNameLabel.AutoSize = true;
            this.lbMainNameLabel.Location = new System.Drawing.Point(18, 13);
            this.lbMainNameLabel.Name = "lbMainNameLabel";
            this.lbMainNameLabel.Size = new System.Drawing.Size(0, 13);
            this.lbMainNameLabel.TabIndex = 28;
            // 
            // FrmEdge
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(408, 357);
            this.Controls.Add(this.lbMainNameLabel);
            this.Controls.Add(this.useForAll);
            this.Controls.Add(this.pbEdge12);
            this.Controls.Add(this.pbEdge22);
            this.Controls.Add(this.pbEdge21);
            this.Controls.Add(this.pbEdge11);
            this.Controls.Add(this.gbEdge21);
            this.Controls.Add(this.gbEdge12);
            this.Controls.Add(this.gbEdge22);
            this.Controls.Add(this.gbEdge11);
            this.Controls.Add(this.pbEdgeMain);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmEdge";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "MrDoors -  Отделка кромки";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.pbEdgeMain)).EndInit();
            this.gbEdge11.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbEdge11)).EndInit();
            this.gbEdge22.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.gbEdge12.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.gbEdge21.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbEdge21)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbEdge22)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbEdge12)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.PictureBox pbEdgeMain;
        private System.Windows.Forms.ComboBox cbExist21;
        private System.Windows.Forms.ComboBox cbExist11;
        private System.Windows.Forms.ComboBox cbExist22;
        private System.Windows.Forms.ComboBox cbExist12;
        private System.Windows.Forms.ComboBox cbColor11;
        private System.Windows.Forms.ComboBox cbColor21;
        private System.Windows.Forms.ComboBox cbColor22;
        private System.Windows.Forms.ComboBox cbColor12;
        private System.Windows.Forms.GroupBox gbEdge11;
        private System.Windows.Forms.GroupBox gbEdge22;
        private System.Windows.Forms.GroupBox gbEdge12;
        private System.Windows.Forms.GroupBox gbEdge21;
        private System.Windows.Forms.PictureBox pbEdge11;
        private System.Windows.Forms.PictureBox pbEdge21;
        private System.Windows.Forms.PictureBox pbEdge22;
        private System.Windows.Forms.PictureBox pbEdge12;
        private System.Windows.Forms.CheckBox useForAll;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label lbMainNameLabel;
    }
}