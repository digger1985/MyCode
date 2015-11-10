using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Furniture
{
    public partial class frmPurchaseExportMessageBox : Form
    {
        public frmPurchaseExportMessageBox()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (rdbtnAdd.Checked)
                this.DialogResult = DialogResult.Yes;
            if (rdbtnReplace.Checked)
                this.DialogResult = DialogResult.No;
            if (rdbtnCancel.Checked)
                this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
