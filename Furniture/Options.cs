using System;
using System.IO;
using System.Windows.Forms;

namespace Furniture
{
    public partial class Options : Form
    {
        internal bool TestState { get; set; }
        internal bool LogOn { get; set; }
        internal bool FatalEmailOn { get; set; }

        public Options()
        {
            InitializeComponent();

            DefaultSettings();
            LocalLibUpdatePath.Text = "...";
            Show();
            checkBox1.CheckedChanged += CheckBox1CheckedChanged;
            checkBox2.CheckedChanged += checkBox2_CheckedChanged;
            checkBox3.CheckedChanged += checkBox3_CheckedChanged;
            checkBox4.CheckedChanged += checkBox4_CheckedChanged;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            TestState = true;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            txtBxAdmUsrPsw.PasswordChar = (char)(checkBox3.Checked ? 0 : '*');
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            txtBxScrFtpUpdtFrn.PasswordChar = (char)(checkBox2.Checked ? 0 : '*');
        }

        private void CheckBox1CheckedChanged(object sender, EventArgs e)
        {
            txtBxFtpSecure.PasswordChar = (char)(checkBox1.Checked ? 0 : '*');
        }
        string ftpResult = Furniture.Helpers.FtpAccess.resultFtp;
        private void DefaultSettings()
        {
            txtBxFtpSecure.PasswordChar = (char)(checkBox1.Checked ? 0 : '*');
            txtBxAdmUsrPsw.PasswordChar = (char)(checkBox3.Checked ? 0 : '*');
            txtBxScrFtpUpdtFrn.PasswordChar = (char)(checkBox2.Checked ? 0 : '*');
            //txtBxFtpPath.Text = Properties.Settings.Default.FtpPath;
            txtBxFtpPath.Text = ftpResult + "solidlibupdate";
            txtBxPchVer.Text = Properties.Settings.Default.PatchVersion;
            txtBxFtpName.Text = Properties.Settings.Default.NameFtpUserForLibUpdate;
            txtBxFtpSecure.Text = Properties.Settings.Default.SecureFtpUser;
            txtBxAdmUsrName.Text = Properties.Settings.Default.AdmUsrName;
            txtBxAdmUsrPsw.Text = Properties.Settings.Default.AdmUsrPsw;
            //txtBxFtpUpdtFrn.Text = Properties.Settings.Default.FurnFtpPath;
            txtBxFtpUpdtFrn.Text = ftpResult;
            txtBxNameFtpUpdtFrn.Text = Properties.Settings.Default.FurnFtpName;
            txtBxScrFtpUpdtFrn.Text = Properties.Settings.Default.FurnFtpPass;
            checkBox4.Checked = Properties.Settings.Default.UpdateToTest;
            checkBoxLogOn.Checked = Properties.Settings.Default.LoggingOn;
            checkBoxFatalMailOn.Checked = Properties.Settings.Default.FatalMailOn;
            cbCreatePrograms.Checked = Properties.Settings.Default.CreateProgramsOnFF;
            chbKitchenOn.Checked = Properties.Settings.Default.KitchenModeAvailable;
            chbCashOn.Checked = Properties.Settings.Default.CashModeAvailable;
        }


        private void Button1Click(object sender, EventArgs e)
        {
            //Properties.Settings.Default.FurnFtpPath = txtBxFtpUpdtFrn.Text;
            ftpResult = txtBxFtpUpdtFrn.Text;
            Properties.Settings.Default.FurnFtpName = txtBxNameFtpUpdtFrn.Text;
            Properties.Settings.Default.FurnFtpPass = txtBxScrFtpUpdtFrn.Text;
            Properties.Settings.Default.PatchVersion = txtBxPchVer.Text;
            Properties.Settings.Default.NameFtpUserForLibUpdate = txtBxFtpName.Text;
            Properties.Settings.Default.SecureFtpUser = txtBxFtpSecure.Text;
            Properties.Settings.Default.FtpPath = txtBxFtpPath.Text;
            Properties.Settings.Default.AdmUsrPsw = txtBxAdmUsrPsw.Text;
            Properties.Settings.Default.AdmUsrName = txtBxAdmUsrName.Text;
            Properties.Settings.Default.UpdateToTest = checkBox4.Checked;
            Properties.Settings.Default.LoggingOn = checkBoxLogOn.Checked;
            Properties.Settings.Default.FatalMailOn = checkBoxFatalMailOn.Checked;
            Properties.Settings.Default.CreateProgramsOnFF = cbCreatePrograms.Checked;
            Properties.Settings.Default.CashModeAvailable = chbCashOn.Checked;
            Properties.Settings.Default.KitchenModeAvailable = chbKitchenOn.Checked;
            Properties.Settings.Default.Save();
            Close();
        }

        private void Button2Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LocalLibUpdatePath_Click(object sender, EventArgs e)
        {
            if (LocalLibUpdatePath.Text == "...")
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LocalLibUpdatePath.Text = "Обновить";
                    PathTb.Text = openFileDialog.FileName;
                }
            }
            else
            {
                string tmp = PathTb.Text;
                Close();
                var ul = new UpdatingLib("L");
                ul.UpdateLibFromLocalFile(new FileInfo(tmp));


            }
        }

        private void Options_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
    }
}
