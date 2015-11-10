using System.Drawing;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swcommands;



namespace Furniture
{
    partial class FrmSetParameters
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
            _swAsmDoc.AddItemNotify -= new DAssemblyDocEvents_AddItemNotifyEventHandler(this.AddNewItem);

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSetParameters));
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tbpParams = new System.Windows.Forms.TabPage();
            this.tbpPos = new System.Windows.Forms.TabPage();
            this.tbpPrice = new System.Windows.Forms.TabPage();
            this.lblPrice = new System.Windows.Forms.Label();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.lblCompName = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.CommentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.WarningPB = new System.Windows.Forms.PictureBox();
            this.commentsTb = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tabMain.SuspendLayout();
            this.tbpPrice.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.CommentLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.WarningPB)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tbpParams);
            this.tabMain.Controls.Add(this.tbpPos);
            this.tabMain.Controls.Add(this.tbpPrice);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(6, 96);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(272, 153);
            this.tabMain.TabIndex = 0;
            // 
            // tbpParams
            // 
            this.tbpParams.Location = new System.Drawing.Point(4, 22);
            this.tbpParams.Name = "tbpParams";
            this.tbpParams.Padding = new System.Windows.Forms.Padding(3);
            this.tbpParams.Size = new System.Drawing.Size(264, 127);
            this.tbpParams.TabIndex = 0;
            this.tbpParams.Text = "Размеры";
            this.tbpParams.UseVisualStyleBackColor = true;
            // 
            // tbpPos
            // 
            this.tbpPos.Location = new System.Drawing.Point(4, 22);
            this.tbpPos.Margin = new System.Windows.Forms.Padding(0);
            this.tbpPos.Name = "tbpPos";
            this.tbpPos.Size = new System.Drawing.Size(264, 127);
            this.tbpPos.TabIndex = 1;
            this.tbpPos.Text = "Позиционирование";
            this.tbpPos.UseVisualStyleBackColor = true;
            // 
            // tbpPrice
            // 
            this.tbpPrice.Controls.Add(this.lblPrice);
            this.tbpPrice.Location = new System.Drawing.Point(4, 22);
            this.tbpPrice.Name = "tbpPrice";
            this.tbpPrice.Size = new System.Drawing.Size(264, 127);
            this.tbpPrice.TabIndex = 2;
            this.tbpPrice.Text = "Цена";
            this.tbpPrice.UseVisualStyleBackColor = true;
            // 
            // lblPrice
            // 
            this.lblPrice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPrice.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPrice.Location = new System.Drawing.Point(0, 0);
            this.lblPrice.Name = "lblPrice";
            this.lblPrice.Size = new System.Drawing.Size(264, 127);
            this.lblPrice.TabIndex = 0;
            this.lblPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Units1.ico");
            this.imageList1.Images.SetKeyName(1, "Стрелка.png");
            this.imageList1.Images.SetKeyName(2, "expand.gif");
            this.imageList1.Images.SetKeyName(3, "Стрелка2.gif");
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.Location = new System.Drawing.Point(60, 31);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(128, 99);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Декор";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(5, 15);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(118, 79);
            this.pictureBox1.TabIndex = 0;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(105, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 24);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
            this.btnCancel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BtnCancelKeyDown);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(5, 6);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(90, 24);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOkClick);
            this.btnOK.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BtnOkKeyDown);
            // 
            // lblCompName
            // 
            this.lblCompName.Location = new System.Drawing.Point(6, 3);
            this.lblCompName.Name = "lblCompName";
            this.lblCompName.Size = new System.Drawing.Size(200, 28);
            this.lblCompName.TabIndex = 5;
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(6, 80);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(0, 13);
            this.linkLabel1.TabIndex = 6;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.CommentLayout, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.lblCompName, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.linkLabel1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tabMain, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(284, 314);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // CommentLayout
            // 
            this.CommentLayout.AutoSize = true;
            this.CommentLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CommentLayout.ColumnCount = 2;
            this.CommentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.CommentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.CommentLayout.Controls.Add(this.WarningPB, 0, 0);
            this.CommentLayout.Controls.Add(this.commentsTb, 1, 0);
            this.CommentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CommentLayout.Location = new System.Drawing.Point(6, 34);
            this.CommentLayout.Name = "CommentLayout";
            this.CommentLayout.RowCount = 1;
            this.CommentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.CommentLayout.Size = new System.Drawing.Size(272, 43);
            this.CommentLayout.TabIndex = 8;
            // 
            // WarningPB
            // 
            this.WarningPB.Image = global::Furniture.Properties.Resources.warning;
            this.WarningPB.Location = new System.Drawing.Point(3, 3);
            this.WarningPB.Name = "WarningPB";
            this.WarningPB.Size = new System.Drawing.Size(35, 37);
            this.WarningPB.TabIndex = 9;
            this.WarningPB.TabStop = false;
            this.WarningPB.Visible = false;
            // 
            // commentsTb
            // 
            this.commentsTb.AcceptsReturn = true;
            this.commentsTb.BackColor = System.Drawing.SystemColors.Control;
            this.commentsTb.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.commentsTb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.commentsTb.Location = new System.Drawing.Point(44, 3);
            this.commentsTb.Multiline = true;
            this.commentsTb.Name = "commentsTb";
            this.commentsTb.ReadOnly = true;
            this.commentsTb.Size = new System.Drawing.Size(225, 37);
            this.commentsTb.TabIndex = 10;
            this.commentsTb.TextChanged += new System.EventHandler(this.commentsTBTextChanged);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Controls.Add(this.btnOK, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnCancel, 1, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(42, 255);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(200, 36);
            this.tableLayoutPanel2.TabIndex = 8;
            // 
            // FrmSetParameters
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(284, 314);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Location = new System.Drawing.Point(10, 35);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSetParameters";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "MrDoors РПД";
            this.TopMost = true;
            this.tabMain.ResumeLayout(false);
            this.tbpPrice.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.CommentLayout.ResumeLayout(false);
            this.CommentLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.WarningPB)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Bitmap bitmap;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tbpParams;
        private System.Windows.Forms.TabPage tbpPos;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lblCompName;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Panel pnlMode;
        private System.Windows.Forms.RadioButton rbMode1;
        private System.Windows.Forms.RadioButton rbMode2;
        private System.Windows.Forms.Label pictureBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel CommentLayout;
        private System.Windows.Forms.PictureBox WarningPB;
        private System.Windows.Forms.TextBox commentsTb;
        private System.Windows.Forms.TabPage tbpPrice;
        private System.Windows.Forms.Label lblPrice;
    }
}