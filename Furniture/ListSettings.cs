using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Furniture
{
    public partial class ListSettings : Form
    {
        public ListSettings(IEnumerable<string> list)
        {
            InitializeComponent();
            Show();
            int i = 0;
            int length = 0;
            foreach (var str in list)
            {
                listBox1.Items.Add(str);
                if (str.Length * 6 > length)
                    length = str.Length*6;
                i++;
            }
            listBox1.Size = i>10 ? new Size(length, 150) : new Size(length,i*15);
            Size = new Size(length + 20,Size.Height);
            button1.Location = new Point((Size.Width - button1.Width)/2,button1.Location.Y);
        }

        private void Button1Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
