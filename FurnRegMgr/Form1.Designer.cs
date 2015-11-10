namespace FurnRegMgr
{
    partial class FrmGenReg
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
            this.lblSerNum = new System.Windows.Forms.Label();
            this.txtSerNum = new System.Windows.Forms.TextBox();
            this.btnMakeRegCode = new System.Windows.Forms.Button();
            this.txtRegCode = new System.Windows.Forms.TextBox();
            this.lblRegCode = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblSerNum
            // 
            this.lblSerNum.Location = new System.Drawing.Point(16, 12);
            this.lblSerNum.Name = "lblSerNum";
            this.lblSerNum.Size = new System.Drawing.Size(296, 16);
            this.lblSerNum.TabIndex = 0;
            this.lblSerNum.Text = "Введите первые четыре группы серийного номера SW:";
            // 
            // txtSerNum
            // 
            this.txtSerNum.Location = new System.Drawing.Point(44, 36);
            this.txtSerNum.Name = "txtSerNum";
            this.txtSerNum.Size = new System.Drawing.Size(240, 20);
            this.txtSerNum.TabIndex = 1;
            // 
            // btnMakeRegCode
            // 
            this.btnMakeRegCode.Location = new System.Drawing.Point(80, 72);
            this.btnMakeRegCode.Name = "btnMakeRegCode";
            this.btnMakeRegCode.Size = new System.Drawing.Size(180, 32);
            this.btnMakeRegCode.TabIndex = 2;
            this.btnMakeRegCode.Text = "Создать регистрационный код";
            this.btnMakeRegCode.UseVisualStyleBackColor = true;
            this.btnMakeRegCode.Click += new System.EventHandler(this.BtnMakeRegCodeClick);
            // 
            // txtRegCode
            // 
            this.txtRegCode.Location = new System.Drawing.Point(44, 156);
            this.txtRegCode.Name = "txtRegCode";
            this.txtRegCode.ReadOnly = true;
            this.txtRegCode.Size = new System.Drawing.Size(240, 20);
            this.txtRegCode.TabIndex = 3;
            // 
            // lblRegCode
            // 
            this.lblRegCode.Location = new System.Drawing.Point(96, 132);
            this.lblRegCode.Name = "lblRegCode";
            this.lblRegCode.Size = new System.Drawing.Size(140, 16);
            this.lblRegCode.TabIndex = 4;
            this.lblRegCode.Text = "Регистрационный код:";
            // 
            // frmGenReg
            // 
            this.AcceptButton = this.btnMakeRegCode;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(328, 195);
            this.Controls.Add(this.lblRegCode);
            this.Controls.Add(this.txtRegCode);
            this.Controls.Add(this.btnMakeRegCode);
            this.Controls.Add(this.txtSerNum);
            this.Controls.Add(this.lblSerNum);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmGenReg";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Менеджер лицензий";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSerNum;
        private System.Windows.Forms.TextBox txtSerNum;
        private System.Windows.Forms.Button btnMakeRegCode;
        private System.Windows.Forms.TextBox txtRegCode;
        private System.Windows.Forms.Label lblRegCode;
    }
}

