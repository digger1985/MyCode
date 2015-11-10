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
    public partial class frmShowList : Form
    {
        public frmShowList(List<string> wrongPartsList,bool showAll)
        {
            InitializeComponent();
            if (showAll)
            {
                label3.Text = "Имеет пересечения с фурнитурой других деталей.";
                label6.Text = string.Empty;
            }
            foreach (var partName in wrongPartsList)
            {
                lbListOfItems.Items.Add(partName);
            }
        }
        private void btnOk_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
