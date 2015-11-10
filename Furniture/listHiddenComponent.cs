using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using Environment = System.Environment;

namespace Furniture
{
    public partial class ListHiddenComponent : Form
    {
        private readonly SwAddin _mSwAddin;
        private readonly List<Component2> _hidComp;
        private bool _done = true;

        public ListHiddenComponent(SwAddin swAddin, List<Component2> inListComponent,string name1,string name2)
        {
            _mSwAddin = swAddin;
            _hidComp = inListComponent;
            InitializeComponent();
            Show();
            EditLabel(name1, name2);
            button1.Click +=Button1Click;
        }

        private void EditLabel(string name1, string name2)
        {
            int size1 = name1.Length;
            int size2 = name2.Length + 3;
            label1.Text = @"Компоненты " + Environment.NewLine + name1 + Environment.NewLine + @" и " + name2 +
                Environment.NewLine + @" имеют пересекающуюся фурнитуру!";
            label1.Width = size1 > size2 ? (size1 + 20): (size2 + 20);
            button1.Location = new Point(label1.Location.X,label1.Location.Y + label1.Height + 10);
            button1.Width = label1.Width;
            ActiveForm.Width = label1.Width + 50;
            ActiveForm.Height = label1.Height + button1.Height + 70;
        }

        private void Button1Click(object sender, EventArgs e)
        {
            if (_done)
            {
                Close();
                ShowComponents();
                _mSwAddin.CutOff();
            }
            _done = false;
        }

        private void ShowComponents()
        {
            foreach (var component2 in _hidComp)
            {
                component2.Visible = 1;
            }
        }
    }
}
