using System;
using System.Windows.Forms;

namespace FurnRegMgr
{
    public partial class FrmGenReg : Form
    {
        public FrmGenReg()
        {
            InitializeComponent();
        }

        private void BtnMakeRegCodeClick(object sender, EventArgs e)
        {
            const ulong progId = 0x4ba5924e;

            try
            {
                string sWcodeReal = txtSerNum.Text;

                if (sWcodeReal.Length != 19)
                {
                    MessageBox.Show(@"¬ведите первые 16 символов серийного номера SW.", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    UInt64 sWcode = Convert.ToUInt64(sWcodeReal.Substring(0, 4) + sWcodeReal.Substring(5, 4)) *
                        Convert.ToUInt64(sWcodeReal.Substring(10, 4) + sWcodeReal.Substring(15, 4));
                    sWcode = sWcode ^ progId;
                    string code = sWcode.ToString("X");
                    txtRegCode.Text = code.Substring(code.Length - 8, 8);
                }
            }
            catch{}
        }
    }
}