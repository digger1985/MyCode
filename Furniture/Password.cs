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
    public partial class Password : Form
    {
        private string _passwrd;

        public Password()
        {
            InitializeComponent();
            Show();
            textBox1.KeyDown += TextBox1KeyDown;
        }

        void TextBox1KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                _passwrd = textBox1.Text;
                Close();
            }
        }

        internal string Passwrd
        {
            get
            {
                return _passwrd;
            } 
            set
            {
                _passwrd = value;
            }
        }
    }
}
