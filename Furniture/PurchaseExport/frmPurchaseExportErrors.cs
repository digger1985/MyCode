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
    public partial class frmPurchaseExportErrors : Form
    {
        public frmPurchaseExportErrors(List<string[]> errors)
        {
            InitializeComponent();
            List<object> errorsList = new List<object>();
            foreach (string[] error in errors)
            {
                errorsList.Add(
                    new 
                    { 
                        FilePath = error[0],
                        FileName = error[1],
                        ErrMsg = error[2]
                    }
                    );
            }
            dataGridView1.DataSource = errorsList;
        }

        private void frmPurchaseExportErrors_Load(object sender, EventArgs e)
        {

        }        
    }
}
