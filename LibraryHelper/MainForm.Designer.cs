namespace LibraryHelper
{
    partial class MainForm
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.configNameAdd = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbPropertyName = new System.Windows.Forms.TextBox();
            this.cbType = new System.Windows.Forms.ComboBox();
            this.cbYesNo = new System.Windows.Forms.ComboBox();
            this.btnAddToAddList = new System.Windows.Forms.Button();
            this.propertiesGridViewAdd = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tbPropertyValue = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbPropNameToDelete = new System.Windows.Forms.TextBox();
            this.btnAddToDeleteList = new System.Windows.Forms.Button();
            this.dgvPropertiesToDelete = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.tbNewPropName = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tbOldPropName = new System.Windows.Forms.TextBox();
            this.btnAddToListToChange = new System.Windows.Forms.Button();
            this.dgvPropertiesToChange = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnStart = new System.Windows.Forms.Button();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.tbLogFileName = new System.Windows.Forms.TextBox();
            this.btnSaveLog = new System.Windows.Forms.Button();
            this.lblFileCount = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.lblInfo = new System.Windows.Forms.Label();
            this.dgvInfo = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbPath = new System.Windows.Forms.TextBox();
            this.btnPath = new System.Windows.Forms.Button();
            this.groupBox11 = new System.Windows.Forms.GroupBox();
            this.cbDrw = new System.Windows.Forms.CheckBox();
            this.cbPart = new System.Windows.Forms.CheckBox();
            this.cbAsm = new System.Windows.Forms.CheckBox();
            this.cbHidden = new System.Windows.Forms.CheckBox();
            this.cbSubfolder = new System.Windows.Forms.CheckBox();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.cbHiddenFolder = new System.Windows.Forms.CheckBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.propertiesGridViewAdd)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPropertiesToDelete)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.groupBox9.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPropertiesToChange)).BeginInit();
            this.groupBox8.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvInfo)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox11.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(6, 116);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(502, 384);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl1_Selecting);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox3);
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(494, 358);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Добавление свойств";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.configNameAdd);
            this.groupBox3.Location = new System.Drawing.Point(9, 272);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(476, 83);
            this.groupBox3.TabIndex = 17;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Введите конфигурацию или оставьте поле пустым";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Конфигурация:";
            // 
            // configNameAdd
            // 
            this.configNameAdd.Location = new System.Drawing.Point(9, 44);
            this.configNameAdd.Name = "configNameAdd";
            this.configNameAdd.Size = new System.Drawing.Size(461, 20);
            this.configNameAdd.TabIndex = 13;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.tbPropertyName);
            this.groupBox2.Controls.Add(this.cbType);
            this.groupBox2.Controls.Add(this.cbYesNo);
            this.groupBox2.Controls.Add(this.btnAddToAddList);
            this.groupBox2.Controls.Add(this.propertiesGridViewAdd);
            this.groupBox2.Controls.Add(this.tbPropertyValue);
            this.groupBox2.Location = new System.Drawing.Point(9, 9);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(476, 257);
            this.groupBox2.TabIndex = 16;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Заполните список свойствами, которые будут добавлены";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Введите свойства:";
            // 
            // tbPropertyName
            // 
            this.tbPropertyName.Location = new System.Drawing.Point(34, 61);
            this.tbPropertyName.Name = "tbPropertyName";
            this.tbPropertyName.Size = new System.Drawing.Size(143, 20);
            this.tbPropertyName.TabIndex = 7;
            // 
            // cbType
            // 
            this.cbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType.FormattingEnabled = true;
            this.cbType.Items.AddRange(new object[] {
            "Текст",
            "Дата",
            "Номер",
            "Да или нет"});
            this.cbType.Location = new System.Drawing.Point(183, 61);
            this.cbType.Name = "cbType";
            this.cbType.Size = new System.Drawing.Size(140, 21);
            this.cbType.TabIndex = 11;
            this.cbType.SelectedIndexChanged += new System.EventHandler(this.cbType_SelectedIndexChanged);
            // 
            // cbYesNo
            // 
            this.cbYesNo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbYesNo.FormattingEnabled = true;
            this.cbYesNo.Items.AddRange(new object[] {
            "Yes",
            "No"});
            this.cbYesNo.Location = new System.Drawing.Point(329, 61);
            this.cbYesNo.Name = "cbYesNo";
            this.cbYesNo.Size = new System.Drawing.Size(140, 21);
            this.cbYesNo.TabIndex = 12;
            this.cbYesNo.Visible = false;
            // 
            // btnAddToAddList
            // 
            this.btnAddToAddList.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAddToAddList.Location = new System.Drawing.Point(351, 88);
            this.btnAddToAddList.Name = "btnAddToAddList";
            this.btnAddToAddList.Size = new System.Drawing.Size(118, 23);
            this.btnAddToAddList.TabIndex = 10;
            this.btnAddToAddList.Text = "Добавить в список";
            this.btnAddToAddList.UseVisualStyleBackColor = true;
            this.btnAddToAddList.Click += new System.EventHandler(this.btnAddProperty_Click);
            // 
            // propertiesGridViewAdd
            // 
            this.propertiesGridViewAdd.AllowUserToAddRows = false;
            this.propertiesGridViewAdd.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.propertiesGridViewAdd.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.propertiesGridViewAdd.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.propertiesGridViewAdd.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3});
            this.propertiesGridViewAdd.Location = new System.Drawing.Point(6, 117);
            this.propertiesGridViewAdd.Name = "propertiesGridViewAdd";
            this.propertiesGridViewAdd.ReadOnly = true;
            this.propertiesGridViewAdd.Size = new System.Drawing.Size(464, 134);
            this.propertiesGridViewAdd.TabIndex = 0;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "Имя свойства";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            // 
            // Column2
            // 
            this.Column2.HeaderText = "Тип";
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // Column3
            // 
            this.Column3.HeaderText = "Значение";
            this.Column3.Name = "Column3";
            this.Column3.ReadOnly = true;
            // 
            // tbPropertyValue
            // 
            this.tbPropertyValue.Location = new System.Drawing.Point(329, 61);
            this.tbPropertyValue.Name = "tbPropertyValue";
            this.tbPropertyValue.Size = new System.Drawing.Size(141, 20);
            this.tbPropertyValue.TabIndex = 9;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox5);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(494, 358);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Удаление свойств";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.label5);
            this.groupBox5.Controls.Add(this.tbPropNameToDelete);
            this.groupBox5.Controls.Add(this.btnAddToDeleteList);
            this.groupBox5.Controls.Add(this.dgvPropertiesToDelete);
            this.groupBox5.Location = new System.Drawing.Point(9, 9);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(476, 346);
            this.groupBox5.TabIndex = 20;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Заполните список свойствами, которые будут удалены";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 26);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(102, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Введите свойства:";
            // 
            // tbPropNameToDelete
            // 
            this.tbPropNameToDelete.Location = new System.Drawing.Point(50, 42);
            this.tbPropNameToDelete.Name = "tbPropNameToDelete";
            this.tbPropNameToDelete.Size = new System.Drawing.Size(296, 20);
            this.tbPropNameToDelete.TabIndex = 7;
            // 
            // btnAddToDeleteList
            // 
            this.btnAddToDeleteList.Location = new System.Drawing.Point(352, 40);
            this.btnAddToDeleteList.Name = "btnAddToDeleteList";
            this.btnAddToDeleteList.Size = new System.Drawing.Size(118, 23);
            this.btnAddToDeleteList.TabIndex = 10;
            this.btnAddToDeleteList.Text = "Добавить в список";
            this.btnAddToDeleteList.UseVisualStyleBackColor = true;
            this.btnAddToDeleteList.Click += new System.EventHandler(this.btnAddToDeleteList_Click);
            // 
            // dgvPropertiesToDelete
            // 
            this.dgvPropertiesToDelete.AllowUserToAddRows = false;
            this.dgvPropertiesToDelete.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPropertiesToDelete.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.dgvPropertiesToDelete.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPropertiesToDelete.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1});
            this.dgvPropertiesToDelete.Location = new System.Drawing.Point(6, 80);
            this.dgvPropertiesToDelete.Name = "dgvPropertiesToDelete";
            this.dgvPropertiesToDelete.ReadOnly = true;
            this.dgvPropertiesToDelete.Size = new System.Drawing.Size(464, 260);
            this.dgvPropertiesToDelete.TabIndex = 0;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "Имя свойства";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.groupBox9);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(494, 358);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Замена имен свойств";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.tbNewPropName);
            this.groupBox9.Controls.Add(this.label11);
            this.groupBox9.Controls.Add(this.tbOldPropName);
            this.groupBox9.Controls.Add(this.btnAddToListToChange);
            this.groupBox9.Controls.Add(this.dgvPropertiesToChange);
            this.groupBox9.Location = new System.Drawing.Point(9, 9);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(476, 346);
            this.groupBox9.TabIndex = 25;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "Заполните список свойствами, которые будут заменены";
            // 
            // tbNewPropName
            // 
            this.tbNewPropName.Location = new System.Drawing.Point(255, 42);
            this.tbNewPropName.Name = "tbNewPropName";
            this.tbNewPropName.Size = new System.Drawing.Size(211, 20);
            this.tbNewPropName.TabIndex = 11;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 26);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(102, 13);
            this.label11.TabIndex = 1;
            this.label11.Text = "Введите свойства:";
            // 
            // tbOldPropName
            // 
            this.tbOldPropName.Location = new System.Drawing.Point(50, 42);
            this.tbOldPropName.Name = "tbOldPropName";
            this.tbOldPropName.Size = new System.Drawing.Size(199, 20);
            this.tbOldPropName.TabIndex = 7;
            // 
            // btnAddToListToChange
            // 
            this.btnAddToListToChange.Location = new System.Drawing.Point(352, 76);
            this.btnAddToListToChange.Name = "btnAddToListToChange";
            this.btnAddToListToChange.Size = new System.Drawing.Size(118, 23);
            this.btnAddToListToChange.TabIndex = 10;
            this.btnAddToListToChange.Text = "Добавить в список";
            this.btnAddToListToChange.UseVisualStyleBackColor = true;
            this.btnAddToListToChange.Click += new System.EventHandler(this.btnAddToListToChange_Click);
            // 
            // dgvPropertiesToChange
            // 
            this.dgvPropertiesToChange.AllowUserToAddRows = false;
            this.dgvPropertiesToChange.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPropertiesToChange.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            this.dgvPropertiesToChange.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPropertiesToChange.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn4,
            this.Column5});
            this.dgvPropertiesToChange.Location = new System.Drawing.Point(6, 107);
            this.dgvPropertiesToChange.Name = "dgvPropertiesToChange";
            this.dgvPropertiesToChange.ReadOnly = true;
            this.dgvPropertiesToChange.Size = new System.Drawing.Size(464, 233);
            this.dgvPropertiesToChange.TabIndex = 0;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "Старое имя свойства";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            // 
            // Column5
            // 
            this.Column5.HeaderText = "Новое имя свойства";
            this.Column5.Name = "Column5";
            this.Column5.ReadOnly = true;
            // 
            // btnStart
            // 
            this.btnStart.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnStart.Location = new System.Drawing.Point(6, 506);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(502, 32);
            this.btnStart.TabIndex = 2;
            this.btnStart.Text = "Начать";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.tbLogFileName);
            this.groupBox8.Controls.Add(this.btnSaveLog);
            this.groupBox8.Controls.Add(this.lblFileCount);
            this.groupBox8.Controls.Add(this.label9);
            this.groupBox8.Controls.Add(this.lblInfo);
            this.groupBox8.Controls.Add(this.dgvInfo);
            this.groupBox8.Location = new System.Drawing.Point(514, 131);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(476, 407);
            this.groupBox8.TabIndex = 22;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "Отчет";
            // 
            // tbLogFileName
            // 
            this.tbLogFileName.Location = new System.Drawing.Point(146, 376);
            this.tbLogFileName.Name = "tbLogFileName";
            this.tbLogFileName.Size = new System.Drawing.Size(159, 20);
            this.tbLogFileName.TabIndex = 24;
            this.tbLogFileName.Text = "Log";
            // 
            // btnSaveLog
            // 
            this.btnSaveLog.Enabled = false;
            this.btnSaveLog.Location = new System.Drawing.Point(311, 374);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new System.Drawing.Size(159, 23);
            this.btnSaveLog.TabIndex = 23;
            this.btnSaveLog.Text = "Сохранить в файл";
            this.btnSaveLog.UseVisualStyleBackColor = true;
            this.btnSaveLog.Click += new System.EventHandler(this.btnSaveLog_Click);
            // 
            // lblFileCount
            // 
            this.lblFileCount.AutoSize = true;
            this.lblFileCount.Location = new System.Drawing.Point(124, 59);
            this.lblFileCount.Name = "lblFileCount";
            this.lblFileCount.Size = new System.Drawing.Size(13, 13);
            this.lblFileCount.TabIndex = 22;
            this.lblFileCount.Text = "0";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 58);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(112, 13);
            this.label9.TabIndex = 21;
            this.label9.Text = "Обработано файлов:";
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(8, 32);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(0, 13);
            this.lblInfo.TabIndex = 19;
            // 
            // dgvInfo
            // 
            this.dgvInfo.AllowUserToAddRows = false;
            this.dgvInfo.AllowUserToDeleteRows = false;
            this.dgvInfo.AllowUserToOrderColumns = true;
            this.dgvInfo.AllowUserToResizeColumns = false;
            this.dgvInfo.AllowUserToResizeRows = false;
            this.dgvInfo.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvInfo.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvInfo.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn2});
            this.dgvInfo.Location = new System.Drawing.Point(6, 86);
            this.dgvInfo.Name = "dgvInfo";
            this.dgvInfo.ReadOnly = true;
            this.dgvInfo.Size = new System.Drawing.Size(464, 283);
            this.dgvInfo.TabIndex = 20;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "Info";
            this.dataGridViewTextBoxColumn2.HeaderText = "Событие";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.tbPath);
            this.groupBox1.Controls.Add(this.btnPath);
            this.groupBox1.Location = new System.Drawing.Point(19, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(476, 104);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Задайте папку с файлами";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Путь:";
            // 
            // tbPath
            // 
            this.tbPath.Enabled = false;
            this.tbPath.Location = new System.Drawing.Point(6, 38);
            this.tbPath.Name = "tbPath";
            this.tbPath.Size = new System.Drawing.Size(463, 20);
            this.tbPath.TabIndex = 3;
            // 
            // btnPath
            // 
            this.btnPath.Location = new System.Drawing.Point(395, 61);
            this.btnPath.Name = "btnPath";
            this.btnPath.Size = new System.Drawing.Size(75, 23);
            this.btnPath.TabIndex = 4;
            this.btnPath.Text = "Выбрать";
            this.btnPath.UseVisualStyleBackColor = true;
            this.btnPath.Click += new System.EventHandler(this.btnPath_Click_1);
            // 
            // groupBox11
            // 
            this.groupBox11.Controls.Add(this.cbDrw);
            this.groupBox11.Controls.Add(this.cbPart);
            this.groupBox11.Controls.Add(this.cbAsm);
            this.groupBox11.Location = new System.Drawing.Point(514, 6);
            this.groupBox11.Name = "groupBox11";
            this.groupBox11.Size = new System.Drawing.Size(181, 104);
            this.groupBox11.TabIndex = 8;
            this.groupBox11.TabStop = false;
            this.groupBox11.Text = "Обрабатывать:";
            // 
            // cbDrw
            // 
            this.cbDrw.AutoSize = true;
            this.cbDrw.Location = new System.Drawing.Point(19, 73);
            this.cbDrw.Name = "cbDrw";
            this.cbDrw.Size = new System.Drawing.Size(71, 17);
            this.cbDrw.TabIndex = 2;
            this.cbDrw.Text = "Чертежи";
            this.cbDrw.UseVisualStyleBackColor = true;
            // 
            // cbPart
            // 
            this.cbPart.AutoSize = true;
            this.cbPart.Location = new System.Drawing.Point(19, 49);
            this.cbPart.Name = "cbPart";
            this.cbPart.Size = new System.Drawing.Size(64, 17);
            this.cbPart.TabIndex = 1;
            this.cbPart.Text = "Детали";
            this.cbPart.UseVisualStyleBackColor = true;
            // 
            // cbAsm
            // 
            this.cbAsm.AutoSize = true;
            this.cbAsm.Checked = true;
            this.cbAsm.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAsm.Location = new System.Drawing.Point(19, 26);
            this.cbAsm.Name = "cbAsm";
            this.cbAsm.Size = new System.Drawing.Size(63, 17);
            this.cbAsm.TabIndex = 0;
            this.cbAsm.Text = "Сборки";
            this.cbAsm.UseVisualStyleBackColor = true;
            // 
            // cbHidden
            // 
            this.cbHidden.AutoSize = true;
            this.cbHidden.Location = new System.Drawing.Point(721, 79);
            this.cbHidden.Name = "cbHidden";
            this.cbHidden.Size = new System.Drawing.Size(185, 17);
            this.cbHidden.TabIndex = 7;
            this.cbHidden.Text = "Обрабатывать скрытые файлы";
            this.cbHidden.UseVisualStyleBackColor = true;
            // 
            // cbSubfolder
            // 
            this.cbSubfolder.AutoSize = true;
            this.cbSubfolder.Checked = true;
            this.cbSubfolder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSubfolder.Location = new System.Drawing.Point(721, 32);
            this.cbSubfolder.Name = "cbSubfolder";
            this.cbSubfolder.Size = new System.Drawing.Size(151, 17);
            this.cbSubfolder.TabIndex = 6;
            this.cbSubfolder.Text = "Обрабатывать подпапки";
            this.cbSubfolder.UseVisualStyleBackColor = true;
            this.cbSubfolder.CheckedChanged += new System.EventHandler(this.cbSubfolder_CheckedChanged);
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabPage4);
            this.tabControl2.Location = new System.Drawing.Point(12, 12);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(1022, 574);
            this.tabControl2.TabIndex = 23;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.cbHiddenFolder);
            this.tabPage4.Controls.Add(this.groupBox1);
            this.tabPage4.Controls.Add(this.cbHidden);
            this.tabPage4.Controls.Add(this.tabControl1);
            this.tabPage4.Controls.Add(this.groupBox11);
            this.tabPage4.Controls.Add(this.btnStart);
            this.tabPage4.Controls.Add(this.cbSubfolder);
            this.tabPage4.Controls.Add(this.groupBox8);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(1014, 548);
            this.tabPage4.TabIndex = 0;
            this.tabPage4.Text = "Управление свойствами";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // cbHiddenFolder
            // 
            this.cbHiddenFolder.AutoSize = true;
            this.cbHiddenFolder.Location = new System.Drawing.Point(721, 55);
            this.cbHiddenFolder.Name = "cbHiddenFolder";
            this.cbHiddenFolder.Size = new System.Drawing.Size(265, 17);
            this.cbHiddenFolder.TabIndex = 23;
            this.cbHiddenFolder.Text = "Обрабатывать содержимое скрытых подпапок";
            this.cbHiddenFolder.UseVisualStyleBackColor = true;
            this.cbHiddenFolder.CheckedChanged += new System.EventHandler(this.cbHiddenFolder_CheckedChanged);
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1044, 600);
            this.Controls.Add(this.tabControl2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LibraryHelper";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.propertiesGridViewAdd)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPropertiesToDelete)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPropertiesToChange)).EndInit();
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvInfo)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox11.ResumeLayout(false);
            this.groupBox11.PerformLayout();
            this.tabControl2.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView propertiesGridViewAdd;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnAddToAddList;
        private System.Windows.Forms.TextBox tbPropertyValue;
        private System.Windows.Forms.TextBox tbPropertyName;
        private System.Windows.Forms.ComboBox cbType;
        private System.Windows.Forms.ComboBox cbYesNo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox configNameAdd;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbPropNameToDelete;
        private System.Windows.Forms.Button btnAddToDeleteList;
        private System.Windows.Forms.DataGridView dgvPropertiesToDelete;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.GroupBox groupBox8;
        private System.Windows.Forms.Label lblFileCount;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.DataGridView dgvInfo;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.GroupBox groupBox9;
        private System.Windows.Forms.TextBox tbNewPropName;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox tbOldPropName;
        private System.Windows.Forms.Button btnAddToListToChange;
        private System.Windows.Forms.DataGridView dgvPropertiesToChange;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column5;
        private System.Windows.Forms.Button btnSaveLog;
        private System.Windows.Forms.TextBox tbLogFileName;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox11;
        private System.Windows.Forms.CheckBox cbDrw;
        private System.Windows.Forms.CheckBox cbPart;
        private System.Windows.Forms.CheckBox cbAsm;
        private System.Windows.Forms.CheckBox cbHidden;
        private System.Windows.Forms.CheckBox cbSubfolder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbPath;
        private System.Windows.Forms.Button btnPath;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.CheckBox cbHiddenFolder;
    }
}

