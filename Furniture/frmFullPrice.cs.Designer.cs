namespace Furniture
{
    partial class frmFullPrice
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lblPrice = new System.Windows.Forms.Label();
            this.pBar = new System.Windows.Forms.ProgressBar();
            this.btnShowErrors = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.dgwErrors = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblTime = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgwErrors)).BeginInit();
            this.SuspendLayout();
            // 
            // lblPrice
            // 
            this.lblPrice.AutoSize = true;
            this.lblPrice.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPrice.Location = new System.Drawing.Point(12, 15);
            this.lblPrice.Name = "lblPrice";
            this.lblPrice.Size = new System.Drawing.Size(148, 24);
            this.lblPrice.TabIndex = 0;
            this.lblPrice.Text = "Вычисление...";
            // 
            // pBar
            // 
            this.pBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pBar.Location = new System.Drawing.Point(12, 45);
            this.pBar.Name = "pBar";
            this.pBar.Size = new System.Drawing.Size(560, 27);
            this.pBar.TabIndex = 1;
            // 
            // btnShowErrors
            // 
            this.btnShowErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnShowErrors.Location = new System.Drawing.Point(366, 86);
            this.btnShowErrors.Name = "btnShowErrors";
            this.btnShowErrors.Size = new System.Drawing.Size(125, 23);
            this.btnShowErrors.TabIndex = 2;
            this.btnShowErrors.Text = "Показать ошибки";
            this.btnShowErrors.UseVisualStyleBackColor = true;
            this.btnShowErrors.Visible = false;
            this.btnShowErrors.Click += new System.EventHandler(this.btnShowErrors_Click_1);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(497, 86);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "Закрыть";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // dgwErrors
            // 
            this.dgwErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgwErrors.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgwErrors.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgwErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgwErrors.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2});
            this.dgwErrors.Location = new System.Drawing.Point(-2, 127);
            this.dgwErrors.Name = "dgwErrors";
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgwErrors.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgwErrors.Size = new System.Drawing.Size(588, 0);
            this.dgwErrors.TabIndex = 4;
            // 
            // Column1
            // 
            this.Column1.DataPropertyName = "ComponentName";
            this.Column1.FillWeight = 60.9137F;
            this.Column1.HeaderText = "Компонент";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            // 
            // Column2
            // 
            this.Column2.DataPropertyName = "ErrorMessage";
            this.Column2.FillWeight = 139.0863F;
            this.Column2.HeaderText = "Ошибка";
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // lblTime
            // 
            this.lblTime.AutoSize = true;
            this.lblTime.Location = new System.Drawing.Point(13, 91);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(0, 13);
            this.lblTime.TabIndex = 5;
            // 
            // frmFullPrice
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 122);
            this.Controls.Add(this.lblTime);
            this.Controls.Add(this.dgwErrors);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnShowErrors);
            this.Controls.Add(this.pBar);
            this.Controls.Add(this.lblPrice);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmFullPrice";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Расчет цены проекта";
            this.Load += new System.EventHandler(this.frmFullPrice_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgwErrors)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblPrice;
        private System.Windows.Forms.ProgressBar pBar;
        private System.Windows.Forms.Button btnShowErrors;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.DataGridView dgwErrors;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
    }
}