using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Furniture.ProgressBar
{
    public class WaitTime
    {
        private static readonly WaitTime instance = new WaitTime();
        public static WaitTime Instance
        {
            get { return instance; }
        }
        private WaitTime() { }

        Thread th = null;
        Form fn = new Form();
        System.Windows.Forms.ProgressBar pg = new System.Windows.Forms.ProgressBar();
        Label label = new Label();
        TableLayoutPanel table = new TableLayoutPanel();
        private delegate void SetTextCallback(string text);

        private string labelText = "Подождите пожалуйста окончания операции отрыва модели от библиотеки ";

        private bool isAlreadyRun = false;
        private void RunWait()
        {
            //fn.Close();
            //fn = new Form();
            isAlreadyRun = true;
            label.Width = 255;
            label.Dock = DockStyle.Fill;

            label.Text = labelText;
            table.ColumnCount = 1;
            table.RowCount = 2;
            fn.Controls.Add(table);
            table.Dock = DockStyle.Fill;
            pg.Dock = DockStyle.Fill;
            pg.Location = new System.Drawing.Point(12, 12);
            pg.Name = "progressBar";
            pg.Size = new System.Drawing.Size(260, 23);
            pg.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            pg.TabIndex = 0;
            fn.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            fn.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            fn.ClientSize = new System.Drawing.Size(284, 90);
            table.Controls.Add(pg, 0, 0);
            table.Controls.Add(label, 0, 1);
            //fn.Controls.Add(this.pg);
            fn.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            fn.MaximizeBox = false;
            fn.MinimizeBox = false;
            fn.Name = "dialog";
            fn.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            fn.Text = "Пожалуйста, подождите...";
            fn.TopMost = true;
            fn.ResumeLayout(false);
            try
            {
                fn.ShowDialog();
            }
            catch(Exception)
            {}
        }
        public void SetLabel(string _labelText)
        {
            try
            {
                labelText = _labelText;

                //if (!label.InvokeRequired)
                //    label.Text = labelText;
                //else
                //    label.Invoke(new Action(delegate { label.Text = labelText; }));
                //Application.DoEvents();
            }
            catch (Exception)
            {
            }
           

        }
        public void ShowWait()
        {
            if (isAlreadyRun)
                return;
            th = new Thread(new ThreadStart(RunWait));
            th.Start();
        }

        public void HideWait()
        {

            if (!fn.InvokeRequired)
                fn.Close();
            else
                fn.Invoke(new EventHandler(delegate{fn.Close();}));
                
            //th.Abort();
            isAlreadyRun = false;
        }

    }
}
