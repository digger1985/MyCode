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
    public partial class MyMessageBox : Form
    {
        public MyMessageBox()
        {
            InitializeComponent();
        }

        public void Show(string labelText,string neededLabelForTextBox,string textBoxText,string btnText,string caption)
        {
            Text = caption;
            label1.Text = labelText;
            textBox1.Text = textBoxText;
            button1.Text = btnText;
            label2.Text = neededLabelForTextBox;
            Show();
        }

        private void Button1Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
