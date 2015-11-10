namespace Furniture
{
    partial class frmPurchaseExportMessageBox
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rdbtnCancel = new System.Windows.Forms.RadioButton();
            this.rdbtnReplace = new System.Windows.Forms.RadioButton();
            this.rdbtnAdd = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rdbtnCancel);
            this.groupBox1.Controls.Add(this.rdbtnReplace);
            this.groupBox1.Controls.Add(this.rdbtnAdd);
            this.groupBox1.Location = new System.Drawing.Point(15, 25);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(279, 99);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // rdbtnCancel
            // 
            this.rdbtnCancel.AutoSize = true;
            this.rdbtnCancel.Location = new System.Drawing.Point(17, 65);
            this.rdbtnCancel.Name = "rdbtnCancel";
            this.rdbtnCancel.Size = new System.Drawing.Size(120, 17);
            this.rdbtnCancel.TabIndex = 2;
            this.rdbtnCancel.TabStop = true;
            this.rdbtnCancel.Text = "Не импортировать";
            this.rdbtnCancel.UseVisualStyleBackColor = true;
            // 
            // rdbtnReplace
            // 
            this.rdbtnReplace.AutoSize = true;
            this.rdbtnReplace.Location = new System.Drawing.Point(17, 19);
            this.rdbtnReplace.Name = "rdbtnReplace";
            this.rdbtnReplace.Size = new System.Drawing.Size(232, 17);
            this.rdbtnReplace.TabIndex = 0;
            this.rdbtnReplace.TabStop = true;
            this.rdbtnReplace.Text = "Заменить содержимое текущего заказа";
            this.rdbtnReplace.UseVisualStyleBackColor = true;
            // 
            // rdbtnAdd
            // 
            this.rdbtnAdd.AutoSize = true;
            this.rdbtnAdd.Location = new System.Drawing.Point(17, 42);
            this.rdbtnAdd.Name = "rdbtnAdd";
            this.rdbtnAdd.Size = new System.Drawing.Size(248, 17);
            this.rdbtnAdd.TabIndex = 1;
            this.rdbtnAdd.TabStop = true;
            this.rdbtnAdd.Text = "Добавить к содержимому текущего заказа";
            this.rdbtnAdd.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(199, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Заказ не пустой! Выберите действие:";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(181, 130);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(113, 23);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "Применить";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // frmPurchaseExportMessageBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(310, 159);
            this.ControlBox = false;
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmPurchaseExportMessageBox";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rdbtnCancel;
        private System.Windows.Forms.RadioButton rdbtnReplace;
        private System.Windows.Forms.RadioButton rdbtnAdd;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOK;
    }
}