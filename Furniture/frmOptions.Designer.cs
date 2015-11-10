using System.Reflection;

namespace Furniture
{
    partial class FrmOptions
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpDBPath = new System.Windows.Forms.GroupBox();
            this.txtDBPath = new System.Windows.Forms.TextBox();
            this.btnDBPath = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.tbUpdateBibl = new System.Windows.Forms.TabControl();
            this.tbpCommon = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cbConnectSectionName = new System.Windows.Forms.ComboBox();
            this.tbOraDbPassword = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tbOraDbLogin = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btnConnectIniPath = new System.Windows.Forms.Button();
            this.tbConnectIniPath = new System.Windows.Forms.TextBox();
            this.grpDecors = new System.Windows.Forms.GroupBox();
            this.txtDecorPath = new System.Windows.Forms.TextBox();
            this.btnDecorPath = new System.Windows.Forms.Button();
            this.txtDesigner = new System.Windows.Forms.TextBox();
            this.lblDesigner = new System.Windows.Forms.Label();
            this.grpModelPath = new System.Windows.Forms.GroupBox();
            this.txtModelPath = new System.Windows.Forms.TextBox();
            this.btnModelPath = new System.Windows.Forms.Button();
            this.grpDrwPath = new System.Windows.Forms.GroupBox();
            this.txtDrwPath = new System.Windows.Forms.TextBox();
            this.btnDrwPath = new System.Windows.Forms.Button();
            this.tbpAutoSave = new System.Windows.Forms.TabPage();
            this.chckSetParent = new System.Windows.Forms.CheckBox();
            this.chckDefaultRPDView = new System.Windows.Forms.CheckBox();
            this.chbDeleteDrawIfStandart = new System.Windows.Forms.CheckBox();
            this.chbViewBeforeDim = new System.Windows.Forms.CheckBox();
            this.deleteBeforeDim = new System.Windows.Forms.CheckBox();
            this.chckBxCreatePacketHoles = new System.Windows.Forms.CheckBox();
            this.chckBxAutoArrangeDimension = new System.Windows.Forms.CheckBox();
            this.chkShowRPDBeforeEnd = new System.Windows.Forms.CheckBox();
            this.chckBxAutoScale = new System.Windows.Forms.CheckBox();
            this.chckBxReloadComps = new System.Windows.Forms.CheckBox();
            this.chckBxDetachModel = new System.Windows.Forms.CheckBox();
            this.chckBxConvertDwgPdf = new System.Windows.Forms.CheckBox();
            this.chBxPrmLmts = new System.Windows.Forms.CheckBox();
            this.chBxCvtCrss = new System.Windows.Forms.CheckBox();
            this.chkColorNotUniq = new System.Windows.Forms.CheckBox();
            this.chkAutoRecalculateOnAdd = new System.Windows.Forms.CheckBox();
            this.chkAutoCutOff = new System.Windows.Forms.CheckBox();
            this.chkAutoShowSetParameters = new System.Windows.Forms.CheckBox();
            this.chkAutoSaveDrawings = new System.Windows.Forms.CheckBox();
            this.chkAutoSaveComponents = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.grpBxDefOpt = new System.Windows.Forms.GroupBox();
            this.btnDefOpt = new System.Windows.Forms.Button();
            this.btnAdvanced = new System.Windows.Forms.Button();
            this.grpBxUpdateLib = new System.Windows.Forms.GroupBox();
            this.btnIpdateLib = new System.Windows.Forms.Button();
            this.chckBxUpdateLib = new System.Windows.Forms.CheckBox();
            this.lblFtpPath = new System.Windows.Forms.Label();
            this.lblPatchVersion = new System.Windows.Forms.Label();
            this.tbpMacros = new System.Windows.Forms.TabPage();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.MacroPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ModulName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProcName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.IsDraw = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.chkMcrsAuto = new System.Windows.Forms.CheckBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.grpDBPath.SuspendLayout();
            this.tbUpdateBibl.SuspendLayout();
            this.tbpCommon.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.grpDecors.SuspendLayout();
            this.grpModelPath.SuspendLayout();
            this.grpDrwPath.SuspendLayout();
            this.tbpAutoSave.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.grpBxDefOpt.SuspendLayout();
            this.grpBxUpdateLib.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpDBPath
            // 
            this.grpDBPath.Controls.Add(this.txtDBPath);
            this.grpDBPath.Controls.Add(this.btnDBPath);
            this.grpDBPath.Location = new System.Drawing.Point(8, 12);
            this.grpDBPath.Name = "grpDBPath";
            this.grpDBPath.Size = new System.Drawing.Size(504, 52);
            this.grpDBPath.TabIndex = 0;
            this.grpDBPath.TabStop = false;
            this.grpDBPath.Text = "Путь к базам данных:";
            // 
            // txtDBPath
            // 
            this.txtDBPath.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.txtDBPath.Location = new System.Drawing.Point(8, 24);
            this.txtDBPath.Name = "txtDBPath";
            this.txtDBPath.ReadOnly = true;
            this.txtDBPath.Size = new System.Drawing.Size(448, 21);
            this.txtDBPath.TabIndex = 1;
            // 
            // btnDBPath
            // 
            this.btnDBPath.Location = new System.Drawing.Point(464, 24);
            this.btnDBPath.Name = "btnDBPath";
            this.btnDBPath.Size = new System.Drawing.Size(32, 20);
            this.btnDBPath.TabIndex = 0;
            this.btnDBPath.Text = "...";
            this.btnDBPath.UseVisualStyleBackColor = true;
            this.btnDBPath.Click += new System.EventHandler(this.BtnDbPathClick);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(386, 568);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 24);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOkClick);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(472, 568);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 24);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
            // 
            // tbUpdateBibl
            // 
            this.tbUpdateBibl.Controls.Add(this.tbpCommon);
            this.tbUpdateBibl.Controls.Add(this.tbpAutoSave);
            this.tbUpdateBibl.Controls.Add(this.tabPage1);
            this.tbUpdateBibl.Location = new System.Drawing.Point(12, 12);
            this.tbUpdateBibl.Name = "tbUpdateBibl";
            this.tbUpdateBibl.SelectedIndex = 0;
            this.tbUpdateBibl.Size = new System.Drawing.Size(544, 483);
            this.tbUpdateBibl.TabIndex = 3;
            // 
            // tbpCommon
            // 
            this.tbpCommon.Controls.Add(this.groupBox1);
            this.tbpCommon.Controls.Add(this.grpDecors);
            this.tbpCommon.Controls.Add(this.txtDesigner);
            this.tbpCommon.Controls.Add(this.lblDesigner);
            this.tbpCommon.Controls.Add(this.grpModelPath);
            this.tbpCommon.Controls.Add(this.grpDrwPath);
            this.tbpCommon.Controls.Add(this.grpDBPath);
            this.tbpCommon.Location = new System.Drawing.Point(4, 22);
            this.tbpCommon.Name = "tbpCommon";
            this.tbpCommon.Padding = new System.Windows.Forms.Padding(3);
            this.tbpCommon.Size = new System.Drawing.Size(536, 457);
            this.tbpCommon.TabIndex = 0;
            this.tbpCommon.Text = "Основные";
            this.tbpCommon.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.cbConnectSectionName);
            this.groupBox1.Controls.Add(this.tbOraDbPassword);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.tbOraDbLogin);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.btnConnectIniPath);
            this.groupBox1.Controls.Add(this.tbConnectIniPath);
            this.groupBox1.Location = new System.Drawing.Point(9, 313);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(503, 129);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Настройки для связи с базой данных";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 102);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(39, 13);
            this.label9.TabIndex = 9;
            this.label9.Text = "Server";
            // 
            // cbConnectSectionName
            // 
            this.cbConnectSectionName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbConnectSectionName.FormattingEnabled = true;
            this.cbConnectSectionName.Location = new System.Drawing.Point(72, 99);
            this.cbConnectSectionName.Name = "cbConnectSectionName";
            this.cbConnectSectionName.Size = new System.Drawing.Size(114, 21);
            this.cbConnectSectionName.TabIndex = 8;
            // 
            // tbOraDbPassword
            // 
            this.tbOraDbPassword.Location = new System.Drawing.Point(72, 72);
            this.tbOraDbPassword.Name = "tbOraDbPassword";
            this.tbOraDbPassword.PasswordChar = '*';
            this.tbOraDbPassword.Size = new System.Drawing.Size(114, 21);
            this.tbOraDbPassword.TabIndex = 7;
            this.tbOraDbPassword.TextChanged += new System.EventHandler(this.DbConnectChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 80);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 13);
            this.label8.TabIndex = 6;
            this.label8.Text = "Password";
            // 
            // tbOraDbLogin
            // 
            this.tbOraDbLogin.Location = new System.Drawing.Point(72, 46);
            this.tbOraDbLogin.Name = "tbOraDbLogin";
            this.tbOraDbLogin.Size = new System.Drawing.Size(114, 21);
            this.tbOraDbLogin.TabIndex = 5;
            this.tbOraDbLogin.TextChanged += new System.EventHandler(this.DbConnectChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 54);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(59, 13);
            this.label7.TabIndex = 4;
            this.label7.Text = "User Name";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 25);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(135, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "Путь к файлу connect.ini:";
            // 
            // btnConnectIniPath
            // 
            this.btnConnectIniPath.Location = new System.Drawing.Point(461, 21);
            this.btnConnectIniPath.Name = "btnConnectIniPath";
            this.btnConnectIniPath.Size = new System.Drawing.Size(32, 20);
            this.btnConnectIniPath.TabIndex = 2;
            this.btnConnectIniPath.Text = "...";
            this.btnConnectIniPath.UseVisualStyleBackColor = true;
            this.btnConnectIniPath.Click += new System.EventHandler(this.btnConnectIniPath_Click);
            // 
            // tbConnectIniPath
            // 
            this.tbConnectIniPath.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.tbConnectIniPath.Location = new System.Drawing.Point(147, 20);
            this.tbConnectIniPath.Name = "tbConnectIniPath";
            this.tbConnectIniPath.ReadOnly = true;
            this.tbConnectIniPath.Size = new System.Drawing.Size(308, 21);
            this.tbConnectIniPath.TabIndex = 2;
            // 
            // grpDecors
            // 
            this.grpDecors.Controls.Add(this.txtDecorPath);
            this.grpDecors.Controls.Add(this.btnDecorPath);
            this.grpDecors.Location = new System.Drawing.Point(8, 219);
            this.grpDecors.Name = "grpDecors";
            this.grpDecors.Size = new System.Drawing.Size(504, 52);
            this.grpDecors.TabIndex = 5;
            this.grpDecors.TabStop = false;
            this.grpDecors.Text = "Путь к декорам:";
            // 
            // txtDecorPath
            // 
            this.txtDecorPath.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.txtDecorPath.Location = new System.Drawing.Point(8, 24);
            this.txtDecorPath.Name = "txtDecorPath";
            this.txtDecorPath.ReadOnly = true;
            this.txtDecorPath.Size = new System.Drawing.Size(448, 21);
            this.txtDecorPath.TabIndex = 1;
            // 
            // btnDecorPath
            // 
            this.btnDecorPath.Location = new System.Drawing.Point(464, 24);
            this.btnDecorPath.Name = "btnDecorPath";
            this.btnDecorPath.Size = new System.Drawing.Size(32, 20);
            this.btnDecorPath.TabIndex = 0;
            this.btnDecorPath.Text = "...";
            this.btnDecorPath.UseVisualStyleBackColor = true;
            this.btnDecorPath.Click += new System.EventHandler(this.BtnDecorPathClick);
            // 
            // txtDesigner
            // 
            this.txtDesigner.Location = new System.Drawing.Point(148, 280);
            this.txtDesigner.Name = "txtDesigner";
            this.txtDesigner.Size = new System.Drawing.Size(200, 21);
            this.txtDesigner.TabIndex = 4;
            // 
            // lblDesigner
            // 
            this.lblDesigner.Location = new System.Drawing.Point(6, 280);
            this.lblDesigner.Name = "lblDesigner";
            this.lblDesigner.Size = new System.Drawing.Size(136, 20);
            this.lblDesigner.TabIndex = 3;
            this.lblDesigner.Text = "Фамилия конструктора:";
            // 
            // grpModelPath
            // 
            this.grpModelPath.Controls.Add(this.txtModelPath);
            this.grpModelPath.Controls.Add(this.btnModelPath);
            this.grpModelPath.Location = new System.Drawing.Point(8, 144);
            this.grpModelPath.Name = "grpModelPath";
            this.grpModelPath.Size = new System.Drawing.Size(504, 52);
            this.grpModelPath.TabIndex = 2;
            this.grpModelPath.TabStop = false;
            this.grpModelPath.Text = "Путь к моделям:";
            // 
            // txtModelPath
            // 
            this.txtModelPath.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.txtModelPath.Location = new System.Drawing.Point(8, 24);
            this.txtModelPath.Name = "txtModelPath";
            this.txtModelPath.ReadOnly = true;
            this.txtModelPath.Size = new System.Drawing.Size(448, 21);
            this.txtModelPath.TabIndex = 1;
            // 
            // btnModelPath
            // 
            this.btnModelPath.Location = new System.Drawing.Point(464, 24);
            this.btnModelPath.Name = "btnModelPath";
            this.btnModelPath.Size = new System.Drawing.Size(32, 20);
            this.btnModelPath.TabIndex = 0;
            this.btnModelPath.Text = "...";
            this.btnModelPath.UseVisualStyleBackColor = true;
            this.btnModelPath.Click += new System.EventHandler(this.BtnModelPathClick);
            // 
            // grpDrwPath
            // 
            this.grpDrwPath.Controls.Add(this.txtDrwPath);
            this.grpDrwPath.Controls.Add(this.btnDrwPath);
            this.grpDrwPath.Location = new System.Drawing.Point(8, 76);
            this.grpDrwPath.Name = "grpDrwPath";
            this.grpDrwPath.Size = new System.Drawing.Size(504, 52);
            this.grpDrwPath.TabIndex = 1;
            this.grpDrwPath.TabStop = false;
            this.grpDrwPath.Text = "Путь к чертежам:";
            // 
            // txtDrwPath
            // 
            this.txtDrwPath.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.txtDrwPath.Location = new System.Drawing.Point(8, 24);
            this.txtDrwPath.Name = "txtDrwPath";
            this.txtDrwPath.ReadOnly = true;
            this.txtDrwPath.Size = new System.Drawing.Size(448, 21);
            this.txtDrwPath.TabIndex = 1;
            // 
            // btnDrwPath
            // 
            this.btnDrwPath.Location = new System.Drawing.Point(464, 24);
            this.btnDrwPath.Name = "btnDrwPath";
            this.btnDrwPath.Size = new System.Drawing.Size(32, 20);
            this.btnDrwPath.TabIndex = 0;
            this.btnDrwPath.Text = "...";
            this.btnDrwPath.UseVisualStyleBackColor = true;
            this.btnDrwPath.Click += new System.EventHandler(this.BtnDrwPathClick);
            // 
            // tbpAutoSave
            // 
            this.tbpAutoSave.Controls.Add(this.chckSetParent);
            this.tbpAutoSave.Controls.Add(this.chckDefaultRPDView);
            this.tbpAutoSave.Controls.Add(this.chbDeleteDrawIfStandart);
            this.tbpAutoSave.Controls.Add(this.chbViewBeforeDim);
            this.tbpAutoSave.Controls.Add(this.deleteBeforeDim);
            this.tbpAutoSave.Controls.Add(this.chckBxCreatePacketHoles);
            this.tbpAutoSave.Controls.Add(this.chckBxAutoArrangeDimension);
            this.tbpAutoSave.Controls.Add(this.chkShowRPDBeforeEnd);
            this.tbpAutoSave.Controls.Add(this.chckBxAutoScale);
            this.tbpAutoSave.Controls.Add(this.chckBxReloadComps);
            this.tbpAutoSave.Controls.Add(this.chckBxDetachModel);
            this.tbpAutoSave.Controls.Add(this.chckBxConvertDwgPdf);
            this.tbpAutoSave.Controls.Add(this.chBxPrmLmts);
            this.tbpAutoSave.Controls.Add(this.chBxCvtCrss);
            this.tbpAutoSave.Controls.Add(this.chkColorNotUniq);
            this.tbpAutoSave.Controls.Add(this.chkAutoRecalculateOnAdd);
            this.tbpAutoSave.Controls.Add(this.chkAutoCutOff);
            this.tbpAutoSave.Controls.Add(this.chkAutoShowSetParameters);
            this.tbpAutoSave.Controls.Add(this.chkAutoSaveDrawings);
            this.tbpAutoSave.Controls.Add(this.chkAutoSaveComponents);
            this.tbpAutoSave.Controls.Add(this.label5);
            this.tbpAutoSave.Controls.Add(this.label4);
            this.tbpAutoSave.Location = new System.Drawing.Point(4, 22);
            this.tbpAutoSave.Name = "tbpAutoSave";
            this.tbpAutoSave.Padding = new System.Windows.Forms.Padding(3);
            this.tbpAutoSave.Size = new System.Drawing.Size(536, 457);
            this.tbpAutoSave.TabIndex = 1;
            this.tbpAutoSave.Text = "Дополнительные";
            this.tbpAutoSave.UseVisualStyleBackColor = true;
            // 
            // chckSetParent
            // 
            this.chckSetParent.AutoSize = true;
            this.chckSetParent.Location = new System.Drawing.Point(12, 82);
            this.chckSetParent.Name = "chckSetParent";
            this.chckSetParent.Size = new System.Drawing.Size(188, 17);
            this.chckSetParent.TabIndex = 23;
            this.chckSetParent.Text = "Сворачивать РПД вместе с SW";
            this.chckSetParent.UseVisualStyleBackColor = true;
            // 
            // chckDefaultRPDView
            // 
            this.chckDefaultRPDView.Location = new System.Drawing.Point(12, 105);
            this.chckDefaultRPDView.Name = "chckDefaultRPDView";
            this.chckDefaultRPDView.Size = new System.Drawing.Size(352, 16);
            this.chckDefaultRPDView.TabIndex = 22;
            this.chckDefaultRPDView.Text = "Показывать РПД в сокращенном виде";
            this.chckDefaultRPDView.UseVisualStyleBackColor = true;
            // 
            // chbDeleteDrawIfStandart
            // 
            this.chbDeleteDrawIfStandart.AutoSize = true;
            this.chbDeleteDrawIfStandart.Enabled = false;
            this.chbDeleteDrawIfStandart.Location = new System.Drawing.Point(12, 381);
            this.chbDeleteDrawIfStandart.Name = "chbDeleteDrawIfStandart";
            this.chbDeleteDrawIfStandart.Size = new System.Drawing.Size(367, 17);
            this.chbDeleteDrawIfStandart.TabIndex = 21;
            this.chbDeleteDrawIfStandart.Text = "Удалять чертеж при совпадении типологии детали со стандартной";
            this.chbDeleteDrawIfStandart.UseVisualStyleBackColor = true;
            // 
            // chbViewBeforeDim
            // 
            this.chbViewBeforeDim.AutoSize = true;
            this.chbViewBeforeDim.Location = new System.Drawing.Point(12, 312);
            this.chbViewBeforeDim.Name = "chbViewBeforeDim";
            this.chbViewBeforeDim.Size = new System.Drawing.Size(311, 17);
            this.chbViewBeforeDim.TabIndex = 18;
            this.chbViewBeforeDim.Text = "Устранять пересечения видов перед образмериванием";
            this.chbViewBeforeDim.UseVisualStyleBackColor = true;
            // 
            // deleteBeforeDim
            // 
            this.deleteBeforeDim.AutoSize = true;
            this.deleteBeforeDim.Location = new System.Drawing.Point(12, 289);
            this.deleteBeforeDim.Name = "deleteBeforeDim";
            this.deleteBeforeDim.Size = new System.Drawing.Size(368, 17);
            this.deleteBeforeDim.TabIndex = 17;
            this.deleteBeforeDim.Text = "Удалять существующие размера и блоки перед образмериванием";
            this.deleteBeforeDim.UseVisualStyleBackColor = true;
            // 
            // chckBxCreatePacketHoles
            // 
            this.chckBxCreatePacketHoles.AutoSize = true;
            this.chckBxCreatePacketHoles.Location = new System.Drawing.Point(12, 217);
            this.chckBxCreatePacketHoles.Name = "chckBxCreatePacketHoles";
            this.chckBxCreatePacketHoles.Size = new System.Drawing.Size(175, 17);
            this.chckBxCreatePacketHoles.TabIndex = 16;
            this.chckBxCreatePacketHoles.Text = "Вырезать отверстия пакетно";
            this.chckBxCreatePacketHoles.UseVisualStyleBackColor = true;
            this.chckBxCreatePacketHoles.CheckedChanged += new System.EventHandler(this.PacketHolesCheckedChanged);
            // 
            // chckBxAutoArrangeDimension
            // 
            this.chckBxAutoArrangeDimension.AutoSize = true;
            this.chckBxAutoArrangeDimension.Location = new System.Drawing.Point(12, 358);
            this.chckBxAutoArrangeDimension.Name = "chckBxAutoArrangeDimension";
            this.chckBxAutoArrangeDimension.Size = new System.Drawing.Size(347, 17);
            this.chckBxAutoArrangeDimension.TabIndex = 15;
            this.chckBxAutoArrangeDimension.Text = "Дополнительно выравнивать размеры после образмеривания";
            this.chckBxAutoArrangeDimension.UseVisualStyleBackColor = true;
            // 
            // chkShowRPDBeforeEnd
            // 
            this.chkShowRPDBeforeEnd.AutoSize = true;
            this.chkShowRPDBeforeEnd.Location = new System.Drawing.Point(12, 82);
            this.chkShowRPDBeforeEnd.Name = "chkShowRPDBeforeEnd";
            this.chkShowRPDBeforeEnd.Size = new System.Drawing.Size(371, 17);
            this.chkShowRPDBeforeEnd.TabIndex = 14;
            this.chkShowRPDBeforeEnd.Text = "Показывать окно РПД не дожидаясь отрыва детали от библиотеки";
            this.chkShowRPDBeforeEnd.UseVisualStyleBackColor = true;
            this.chkShowRPDBeforeEnd.Visible = false;
            // 
            // chckBxAutoScale
            // 
            this.chckBxAutoScale.AutoSize = true;
            this.chckBxAutoScale.Location = new System.Drawing.Point(12, 335);
            this.chckBxAutoScale.Name = "chckBxAutoScale";
            this.chckBxAutoScale.Size = new System.Drawing.Size(274, 17);
            this.chckBxAutoScale.TabIndex = 12;
            this.chckBxAutoScale.Text = "Масштабировать чертеж после образмеривания";
            this.chckBxAutoScale.UseVisualStyleBackColor = true;
            // 
            // chckBxReloadComps
            // 
            this.chckBxReloadComps.AutoSize = true;
            this.chckBxReloadComps.Location = new System.Drawing.Point(12, 240);
            this.chckBxReloadComps.Name = "chckBxReloadComps";
            this.chckBxReloadComps.Size = new System.Drawing.Size(357, 17);
            this.chckBxReloadComps.TabIndex = 11;
            this.chckBxReloadComps.Text = "Перегружать компоненты верхнего уровня сборки при открытии";
            this.chckBxReloadComps.UseVisualStyleBackColor = true;
            // 
            // chckBxDetachModel
            // 
            this.chckBxDetachModel.AutoSize = true;
            this.chckBxDetachModel.Location = new System.Drawing.Point(12, 127);
            this.chckBxDetachModel.Name = "chckBxDetachModel";
            this.chckBxDetachModel.Size = new System.Drawing.Size(217, 17);
            this.chckBxDetachModel.TabIndex = 10;
            this.chckBxDetachModel.Text = "Отрывать детали без подтверждения";
            this.chckBxDetachModel.UseVisualStyleBackColor = true;
            // 
            // chckBxConvertDwgPdf
            // 
            this.chckBxConvertDwgPdf.AutoSize = true;
            this.chckBxConvertDwgPdf.Enabled = false;
            this.chckBxConvertDwgPdf.Location = new System.Drawing.Point(12, 407);
            this.chckBxConvertDwgPdf.Name = "chckBxConvertDwgPdf";
            this.chckBxConvertDwgPdf.Size = new System.Drawing.Size(463, 17);
            this.chckBxConvertDwgPdf.TabIndex = 9;
            this.chckBxConvertDwgPdf.Text = "Конвертировать чертежи в формат DWG и PDF при окончательной обработке заказа";
            this.chckBxConvertDwgPdf.UseVisualStyleBackColor = true;
            // 
            // chBxPrmLmts
            // 
            this.chBxPrmLmts.AutoSize = true;
            this.chBxPrmLmts.Location = new System.Drawing.Point(12, 263);
            this.chBxPrmLmts.Name = "chBxPrmLmts";
            this.chBxPrmLmts.Size = new System.Drawing.Size(212, 17);
            this.chBxPrmLmts.TabIndex = 8;
            this.chBxPrmLmts.Text = "Проверять ограничения параметров";
            this.chBxPrmLmts.UseVisualStyleBackColor = true;
            // 
            // chBxCvtCrss
            // 
            this.chBxCvtCrss.AutoSize = true;
            this.chBxCvtCrss.Location = new System.Drawing.Point(12, 194);
            this.chBxCvtCrss.Name = "chBxCvtCrss";
            this.chBxCvtCrss.Size = new System.Drawing.Size(354, 17);
            this.chBxCvtCrss.TabIndex = 7;
            this.chBxCvtCrss.Text = "Предлагать проверку на пересечение при вырезании отверстий";
            this.chBxCvtCrss.UseVisualStyleBackColor = true;
            // 
            // chkColorNotUniq
            // 
            this.chkColorNotUniq.Location = new System.Drawing.Point(12, 172);
            this.chkColorNotUniq.Name = "chkColorNotUniq";
            this.chkColorNotUniq.Size = new System.Drawing.Size(492, 16);
            this.chkColorNotUniq.TabIndex = 6;
            this.chkColorNotUniq.Text = "Использовать выбранные декоры по умолчанию при отрыве";
            this.chkColorNotUniq.UseVisualStyleBackColor = true;
            // 
            // chkAutoRecalculateOnAdd
            // 
            this.chkAutoRecalculateOnAdd.Location = new System.Drawing.Point(12, 150);
            this.chkAutoRecalculateOnAdd.Name = "chkAutoRecalculateOnAdd";
            this.chkAutoRecalculateOnAdd.Size = new System.Drawing.Size(492, 16);
            this.chkAutoRecalculateOnAdd.TabIndex = 5;
            this.chkAutoRecalculateOnAdd.Text = "Пересчитывать при отрыве";
            this.chkAutoRecalculateOnAdd.UseVisualStyleBackColor = true;
            // 
            // chkAutoCutOff
            // 
            this.chkAutoCutOff.Enabled = false;
            this.chkAutoCutOff.Location = new System.Drawing.Point(222, 105);
            this.chkAutoCutOff.Name = "chkAutoCutOff";
            this.chkAutoCutOff.Size = new System.Drawing.Size(308, 16);
            this.chkAutoCutOff.TabIndex = 4;
            this.chkAutoCutOff.Text = "Вырезать отверстия под фурнитуру при отрыве";
            this.chkAutoCutOff.UseVisualStyleBackColor = true;
            this.chkAutoCutOff.Visible = false;
            // 
            // chkAutoShowSetParameters
            // 
            this.chkAutoShowSetParameters.Location = new System.Drawing.Point(12, 60);
            this.chkAutoShowSetParameters.Name = "chkAutoShowSetParameters";
            this.chkAutoShowSetParameters.Size = new System.Drawing.Size(295, 16);
            this.chkAutoShowSetParameters.TabIndex = 3;
            this.chkAutoShowSetParameters.Text = "Выдавать окно РПД при отрыве";
            this.chkAutoShowSetParameters.UseVisualStyleBackColor = true;
            // 
            // chkAutoSaveDrawings
            // 
            this.chkAutoSaveDrawings.Location = new System.Drawing.Point(12, 38);
            this.chkAutoSaveDrawings.Name = "chkAutoSaveDrawings";
            this.chkAutoSaveDrawings.Size = new System.Drawing.Size(492, 16);
            this.chkAutoSaveDrawings.TabIndex = 2;
            this.chkAutoSaveDrawings.Text = "Отрывать чертёж от библиотеки";
            this.chkAutoSaveDrawings.UseVisualStyleBackColor = true;
            // 
            // chkAutoSaveComponents
            // 
            this.chkAutoSaveComponents.Location = new System.Drawing.Point(12, 16);
            this.chkAutoSaveComponents.Name = "chkAutoSaveComponents";
            this.chkAutoSaveComponents.Size = new System.Drawing.Size(492, 16);
            this.chkAutoSaveComponents.TabIndex = 0;
            this.chkAutoSaveComponents.Text = "Отрывать модель от библиотеки";
            this.chkAutoSaveComponents.UseVisualStyleBackColor = true;
            this.chkAutoSaveComponents.CheckedChanged += new System.EventHandler(this.ChkAutoSaveComponentsCheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 272);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(523, 13);
            this.label5.TabIndex = 20;
            this.label5.Text = "_________________________________________________________________________________" +
                "_____";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 389);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(523, 13);
            this.label4.TabIndex = 19;
            this.label4.Text = "_________________________________________________________________________________" +
                "_____";
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.grpBxDefOpt);
            this.tabPage1.Controls.Add(this.btnAdvanced);
            this.tabPage1.Controls.Add(this.grpBxUpdateLib);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(536, 457);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "Сервисные";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // grpBxDefOpt
            // 
            this.grpBxDefOpt.Controls.Add(this.btnDefOpt);
            this.grpBxDefOpt.Location = new System.Drawing.Point(21, 192);
            this.grpBxDefOpt.Name = "grpBxDefOpt";
            this.grpBxDefOpt.Size = new System.Drawing.Size(483, 49);
            this.grpBxDefOpt.TabIndex = 5;
            this.grpBxDefOpt.TabStop = false;
            this.grpBxDefOpt.Text = "Установка начальных настроек SolidWorks";
            // 
            // btnDefOpt
            // 
            this.btnDefOpt.Location = new System.Drawing.Point(6, 20);
            this.btnDefOpt.Name = "btnDefOpt";
            this.btnDefOpt.Size = new System.Drawing.Size(231, 23);
            this.btnDefOpt.TabIndex = 0;
            this.btnDefOpt.Text = "Произвести начальные настройки";
            this.btnDefOpt.UseVisualStyleBackColor = true;
            this.btnDefOpt.Click += new System.EventHandler(this.BtnDefOptClick);
            // 
            // btnAdvanced
            // 
            this.btnAdvanced.Location = new System.Drawing.Point(27, 274);
            this.btnAdvanced.Name = "btnAdvanced";
            this.btnAdvanced.Size = new System.Drawing.Size(231, 22);
            this.btnAdvanced.TabIndex = 2;
            this.btnAdvanced.Text = "Администраторские настройки";
            this.btnAdvanced.UseVisualStyleBackColor = true;
            this.btnAdvanced.Click += new System.EventHandler(this.BtnAdvancedClick);
            // 
            // grpBxUpdateLib
            // 
            this.grpBxUpdateLib.Controls.Add(this.btnIpdateLib);
            this.grpBxUpdateLib.Controls.Add(this.chckBxUpdateLib);
            this.grpBxUpdateLib.Controls.Add(this.lblFtpPath);
            this.grpBxUpdateLib.Controls.Add(this.lblPatchVersion);
            this.grpBxUpdateLib.Location = new System.Drawing.Point(21, 28);
            this.grpBxUpdateLib.Name = "grpBxUpdateLib";
            this.grpBxUpdateLib.Size = new System.Drawing.Size(483, 140);
            this.grpBxUpdateLib.TabIndex = 4;
            this.grpBxUpdateLib.TabStop = false;
            this.grpBxUpdateLib.Text = "Обновление библиотеки";
            // 
            // btnIpdateLib
            // 
            this.btnIpdateLib.Location = new System.Drawing.Point(6, 111);
            this.btnIpdateLib.Name = "btnIpdateLib";
            this.btnIpdateLib.Size = new System.Drawing.Size(231, 23);
            this.btnIpdateLib.TabIndex = 4;
            this.btnIpdateLib.Text = "Принудительное обновление библиотеки";
            this.btnIpdateLib.UseVisualStyleBackColor = true;
            this.btnIpdateLib.Click += new System.EventHandler(this.btnIpdateLib_Click);
            // 
            // chckBxUpdateLib
            // 
            this.chckBxUpdateLib.AutoSize = true;
            this.chckBxUpdateLib.Location = new System.Drawing.Point(6, 20);
            this.chckBxUpdateLib.Name = "chckBxUpdateLib";
            this.chckBxUpdateLib.Size = new System.Drawing.Size(276, 17);
            this.chckBxUpdateLib.TabIndex = 3;
            this.chckBxUpdateLib.Text = "Предлагать обновление библиотеки при запуске";
            this.chckBxUpdateLib.UseVisualStyleBackColor = true;
            this.chckBxUpdateLib.CheckedChanged += new System.EventHandler(this.chckBxUpdateLib_CheckedChanged);
            // 
            // lblFtpPath
            // 
            this.lblFtpPath.AutoSize = true;
            this.lblFtpPath.Location = new System.Drawing.Point(3, 77);
            this.lblFtpPath.Name = "lblFtpPath";
            this.lblFtpPath.Size = new System.Drawing.Size(202, 13);
            this.lblFtpPath.TabIndex = 1;
            this.lblFtpPath.Text = "Путь локального сервера библиотек :";
            // 
            // lblPatchVersion
            // 
            this.lblPatchVersion.AutoSize = true;
            this.lblPatchVersion.Location = new System.Drawing.Point(3, 47);
            this.lblPatchVersion.Name = "lblPatchVersion";
            this.lblPatchVersion.Size = new System.Drawing.Size(124, 13);
            this.lblPatchVersion.TabIndex = 0;
            this.lblPatchVersion.Text = "Версия библиотеки от:";
            // 
            // tbpMacros
            // 
            this.tbpMacros.Location = new System.Drawing.Point(0, 0);
            this.tbpMacros.Name = "tbpMacros";
            this.tbpMacros.Size = new System.Drawing.Size(200, 100);
            this.tbpMacros.TabIndex = 0;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(0, 0);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 0;
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(0, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 0;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(0, 0);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 0;
            // 
            // MacroPath
            // 
            this.MacroPath.Name = "MacroPath";
            // 
            // ModulName
            // 
            this.ModulName.HeaderText = "Имя модуля";
            this.ModulName.Name = "ModulName";
            // 
            // ProcName
            // 
            this.ProcName.HeaderText = "Имя процедуры";
            this.ProcName.Name = "ProcName";
            // 
            // IsDraw
            // 
            this.IsDraw.HeaderText = "Применить к чертежу";
            this.IsDraw.Name = "IsDraw";
            this.IsDraw.Width = 120;
            // 
            // chkMcrsAuto
            // 
            this.chkMcrsAuto.Location = new System.Drawing.Point(0, 0);
            this.chkMcrsAuto.Name = "chkMcrsAuto";
            this.chkMcrsAuto.Size = new System.Drawing.Size(104, 24);
            this.chkMcrsAuto.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 510);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "10.0.0.0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 498);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 13);
            this.label2.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "label3";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 526);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(368, 61);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // FrmOptions
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(568, 602);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbUpdateBibl);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Настройки";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmOptions_FormClosed);
            this.Load += new System.EventHandler(this.FrmOptionsLoad);
            this.grpDBPath.ResumeLayout(false);
            this.grpDBPath.PerformLayout();
            this.tbUpdateBibl.ResumeLayout(false);
            this.tbpCommon.ResumeLayout(false);
            this.tbpCommon.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.grpDecors.ResumeLayout(false);
            this.grpDecors.PerformLayout();
            this.grpModelPath.ResumeLayout(false);
            this.grpModelPath.PerformLayout();
            this.grpDrwPath.ResumeLayout(false);
            this.grpDrwPath.PerformLayout();
            this.tbpAutoSave.ResumeLayout(false);
            this.tbpAutoSave.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.grpBxDefOpt.ResumeLayout(false);
            this.grpBxUpdateLib.ResumeLayout(false);
            this.grpBxUpdateLib.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.GroupBox grpDBPath;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDBPath;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TextBox txtDBPath;
        private System.Windows.Forms.TabControl tbUpdateBibl;
        private System.Windows.Forms.TabPage tbpCommon;
        private System.Windows.Forms.GroupBox grpDrwPath;
        private System.Windows.Forms.TextBox txtDrwPath;
        private System.Windows.Forms.Button btnDrwPath;
        private System.Windows.Forms.TabPage tbpAutoSave;
        private System.Windows.Forms.CheckBox chkAutoSaveComponents;
        private System.Windows.Forms.CheckBox chkAutoCutOff;
        private System.Windows.Forms.CheckBox chkAutoShowSetParameters;
        private System.Windows.Forms.CheckBox chkAutoSaveDrawings;
        private System.Windows.Forms.GroupBox grpModelPath;
        private System.Windows.Forms.TextBox txtModelPath;
        private System.Windows.Forms.Button btnModelPath;
        private System.Windows.Forms.TextBox txtDesigner;
        private System.Windows.Forms.Label lblDesigner;
        private System.Windows.Forms.TabPage tbpMacros;
        private System.Windows.Forms.CheckBox chkMcrsAuto;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.DataGridViewTextBoxColumn MacroPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn ModulName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProcName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsDraw;
        private System.Windows.Forms.CheckBox chkAutoRecalculateOnAdd;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox grpDecors;
        private System.Windows.Forms.TextBox txtDecorPath;
        private System.Windows.Forms.Button btnDecorPath;
        private System.Windows.Forms.CheckBox chkColorNotUniq;
        private System.Windows.Forms.CheckBox chBxPrmLmts;
        private System.Windows.Forms.CheckBox chBxCvtCrss;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button btnAdvanced;
        private System.Windows.Forms.Label lblFtpPath;
        private System.Windows.Forms.Label lblPatchVersion;
        private System.Windows.Forms.CheckBox chckBxUpdateLib;
        private System.Windows.Forms.CheckBox chckBxConvertDwgPdf;
        private System.Windows.Forms.GroupBox grpBxDefOpt;
        private System.Windows.Forms.Button btnDefOpt;
        private System.Windows.Forms.GroupBox grpBxUpdateLib;
        private System.Windows.Forms.CheckBox chckBxDetachModel;
        private System.Windows.Forms.CheckBox chckBxReloadComps;
        private System.Windows.Forms.CheckBox chckBxAutoScale;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnIpdateLib;
        private System.Windows.Forms.CheckBox chkShowRPDBeforeEnd;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckBox chckBxAutoArrangeDimension;
        private System.Windows.Forms.CheckBox chckBxCreatePacketHoles;
        private System.Windows.Forms.CheckBox deleteBeforeDim;
        private System.Windows.Forms.CheckBox chbViewBeforeDim;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox chbDeleteDrawIfStandart;
        private System.Windows.Forms.CheckBox chckDefaultRPDView;
        private System.Windows.Forms.CheckBox chckSetParent;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnConnectIniPath;
        private System.Windows.Forms.TextBox tbConnectIniPath;
        private System.Windows.Forms.TextBox tbOraDbPassword;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbOraDbLogin;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cbConnectSectionName;
        //private System.Windows.Forms.CheckBox chBxScaleWhenDimen;
    }
}