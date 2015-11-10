using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SldWorks;
using SwConst;

namespace LibraryHelper
{
    public struct ErrorInfo
    {
        public string FileName;
        public string Error;
    }

    public struct CustomProperty
    {
        public string Name;
        public swCustomInfoType_e PropertyType;
        public string Value;
    }

    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            cbType.SelectedIndex = 0;
            cbYesNo.SelectedIndex = 0;
            propertiesGridViewAdd.Rows.Clear();
        }

        /// <summary>
        /// Выполняется ли сейчас какое-либо действие
        /// </summary>
        private bool process = false;

        #region Управление свойствами

        private List<CustomProperty> propToAdd;
        private List<string> propToDel;
        private List<KeyValuePair<string, string>> propToChange;

        #region Заполнение списков

        private void btnAddProperty_Click(object sender, EventArgs e)
        {
            if (cbType.SelectedIndex == 3)
            {
                if (string.IsNullOrEmpty(tbPropertyName.Text))
                    MessageBox.Show("Введите имя свойства!");
                else
                    propertiesGridViewAdd.Rows.Add(tbPropertyName.Text, (string)cbType.SelectedItem, cbYesNo.SelectedItem);
            }
            else
            {
                if (string.IsNullOrEmpty(tbPropertyName.Text) || string.IsNullOrEmpty(tbPropertyValue.Text))
                    MessageBox.Show("Введите имя свойства и его значение.");
                else
                    propertiesGridViewAdd.Rows.Add(tbPropertyName.Text, (string)cbType.SelectedItem, tbPropertyValue.Text);
            }

            tbPropertyName.Clear();
            tbPropertyValue.Clear();
        }
        private void btnAddToDeleteList_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbPropNameToDelete.Text))
                MessageBox.Show("Не задано имя свойства.");
            else
            {
                dgvPropertiesToDelete.Rows.Add(tbPropNameToDelete.Text);
                tbPropNameToDelete.Clear();
            }
        }
        private void btnAddToListToChange_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbOldPropName.Text) || string.IsNullOrEmpty(tbNewPropName.Text))
            {
                MessageBox.Show("Введите старое и новое название свойства.");
            }
            else
            {
                dgvPropertiesToChange.Rows.Add(tbOldPropName.Text, tbNewPropName.Text);
                tbOldPropName.Clear();
                tbNewPropName.Clear();
            }
        }

        #endregion

        /// <summary>
        /// Метод обработки, выполняющийся в фоновом потоке
        /// </summary>
        private void worker()
        {
            int errorsCount = 0;
            int fileProcCount = 0;

            this.BeginInvoke(new Action(() =>
            {
                lblInfo.Text = "Запуск Solid Works...";
            }));

            object obj = Activator.CreateInstance(Type.GetTypeFromProgID(("SldWorks.Application")));
            if (obj != null)
            {
                this.BeginInvoke(new Action(() =>
                {
                    using (DataGridViewRow row = new DataGridViewRow())
                    {
                        row.CreateCells(dgvInfo);
                        row.Cells[0].Value = "Запущен Solid Works";
                        row.Cells[0].Style.ForeColor = Color.Green;
                        dgvInfo.Rows.Add(row);
                    }
                }));

                SldWorks.SldWorks App = obj as SldWorks.SldWorks;

                DirectoryInfo rootDir = new DirectoryInfo(tbPath.Text);
                List<FileInfo> files = new List<FileInfo>();
                FileInfo[] rootFiles = rootDir.GetFiles();
                files.AddRange(rootFiles);

                if (cbSubfolder.Checked)
                {
                    DirectoryInfo[] subFolders = rootDir.GetDirectories("*.*", SearchOption.AllDirectories);
                    foreach (DirectoryInfo di in subFolders)
                    {
                        if (di.Attributes.HasFlag(FileAttributes.Hidden))
                        {
                            if (cbHiddenFolder.Checked)
                            {
                                FileInfo[] filesInfo = di.GetFiles();
                                files.AddRange(filesInfo);
                            }
                        }
                        else
                        {
                            FileInfo[] filesInfo = di.GetFiles();
                            files.AddRange(filesInfo);
                        }
                    }
                }

                foreach (FileInfo fileInfo in files)
                {
                    string filePath = fileInfo.FullName;
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    try
                    {
                        if (!cbHidden.Checked)
                        {
                            if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                            {
                                fileProcCount++;
                                this.BeginInvoke(new Action(() =>
                                {
                                    using (DataGridViewRow row = new DataGridViewRow())
                                    {
                                        row.CreateCells(dgvInfo);
                                        row.Cells[0].Value = "Файл пропущен. (Скрытый) " + filePath;
                                        row.Cells[0].Style.ForeColor = Color.Gray;
                                        dgvInfo.Rows.Add(row);
                                        lblFileCount.Text = fileProcCount.ToString() + " из " + files.Count().ToString();
                                    }
                                }));
                                continue;
                            }
                        }

                        if (fileName[0] == '~')
                        {
                            fileProcCount++;
                            this.BeginInvoke(new Action(() =>
                            {
                                using (DataGridViewRow row = new DataGridViewRow())
                                {
                                    row.CreateCells(dgvInfo);
                                    row.Cells[0].Value = "Файл пропущен. (Файл автосохранения) " + filePath;
                                    row.Cells[0].Style.ForeColor = Color.Gray;
                                    dgvInfo.Rows.Add(row);
                                }
                                lblFileCount.Text = fileProcCount.ToString() + " из " + files.Count().ToString();
                            }));
                            continue;
                        }

                        swDocumentTypes_e docType = getDocType(Path.GetExtension(filePath));

                        if (skipFile(docType))
                        {
                            fileProcCount++;
                            this.BeginInvoke(new Action(() =>
                            {
                                using (DataGridViewRow row = new DataGridViewRow())
                                {
                                    row.CreateCells(dgvInfo);
                                    row.Cells[0].Value = "Файл пропущен. (" + getDocTypeName(docType) + ") " + filePath;
                                    row.Cells[0].Style.ForeColor = Color.Gray;
                                    dgvInfo.Rows.Add(row);
                                    lblFileCount.Text = fileProcCount.ToString() + " из " + files.Count().ToString();
                                }
                            }));
                            continue;
                        }

                        if (docType == swDocumentTypes_e.swDocNONE)
                        {
                            fileProcCount++;

                            this.BeginInvoke(new Action(() =>
                            {
                                using (DataGridViewRow row = new DataGridViewRow())
                                {
                                    row.CreateCells(dgvInfo);
                                    row.Cells[0].Value = "Файл пропущен. " + filePath;
                                    row.Cells[0].Style.ForeColor = Color.Gray;
                                    dgvInfo.Rows.Add(row);
                                }
                                lblFileCount.Text = fileProcCount.ToString() + " из " + files.Count().ToString();
                            }));
                            continue;
                        }

                        //todo: начало обработки
                        int i = 0;
                        this.BeginInvoke(new Action(() =>
                           {
                               i = tabControl1.SelectedIndex;
                           }));

                        if (i == 0)
                            addProperties(filePath, App);
                        if (i == 1)
                            deleteProperties(filePath, App);
                        if (i == 2)
                            changeProperties(filePath, App);

                        //todo: конец обработки                       

                    }
                    catch (Exception e)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            using (DataGridViewRow row = new DataGridViewRow())
                            {
                                row.CreateCells(dgvInfo);
                                row.Cells[0].Value = "Ошибка: " + e.Message + " При обработки файла " + fileName;
                                row.Cells[0].Style.ForeColor = Color.Red;
                                dgvInfo.Rows.Add(row);
                            }
                        }));
                        errorsCount++;
                    }

                    fileProcCount++;
                    this.BeginInvoke(new Action(() =>
                    {
                        lblFileCount.Text = fileProcCount.ToString() + " из " + files.Count().ToString();
                    }));
                }

                #region Закрыть SW
                try
                {
                    App.ExitApp();
                    this.BeginInvoke(new Action(() =>
                    {
                        using (DataGridViewRow row = new DataGridViewRow())
                        {
                            row.CreateCells(dgvInfo);
                            row.Cells[0].Value = "Solid Works закрыт";
                            row.Cells[0].Style.ForeColor = Color.Green;
                            dgvInfo.Rows.Add(row);
                        }
                    }));
                }
                catch
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        using (DataGridViewRow row = new DataGridViewRow())
                        {
                            row.CreateCells(dgvInfo);
                            row.Cells[0].Value = "Не удалось закрыть Solid Works";
                            row.Cells[0].Style.ForeColor = Color.Red;
                            dgvInfo.Rows.Add(row);
                        }
                    }));
                }
                #endregion

                this.BeginInvoke(new Action(() =>
                {
                    process = false;
                    btnSaveLog.Enabled = true;
                    btnStart.Enabled = true;
                    groupBox1.Enabled = true;
                    groupBox11.Enabled = true;

                    cbHidden.Enabled = true;
                    cbHiddenFolder.Enabled = true;
                    cbSubfolder.Enabled = true;
                    tabControl1.Enabled = true;

                    lblInfo.Text = "Готово";
                    dgvPropertiesToChange.Rows.Clear();
                    dgvPropertiesToDelete.Rows.Clear();
                    propertiesGridViewAdd.Rows.Clear();

                }));
            }
            else
            {
                using (DataGridViewRow row = new DataGridViewRow())
                {
                    row.CreateCells(dgvInfo);
                    row.Cells[0].Value = "Не удалось запустить Solid Works";
                    row.Cells[0].Style.ForeColor = Color.Red;
                    dgvInfo.Rows.Add(row);
                }

                process = false;
                btnSaveLog.Enabled = true;
                btnStart.Enabled = true;
                groupBox1.Enabled = true;
                groupBox11.Enabled = true;

                cbHidden.Enabled = true;
                cbHiddenFolder.Enabled = true;
                cbSubfolder.Enabled = true;
                tabControl1.Enabled = true;

                lblInfo.Text = "";
                dgvPropertiesToChange.Rows.Clear();
                dgvPropertiesToDelete.Rows.Clear();
                propertiesGridViewAdd.Rows.Clear();
            }

        }

        private void addProperties(string filePath, SldWorks.SldWorks App)
        {
            this.BeginInvoke(new Action(() =>
            {
                lblInfo.Text = "Открытие файла " + filePath + "...";
            }));

            FileInfo fileInfo = new FileInfo(filePath);
            bool readOnly = false;
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                readOnly = true;
                File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
            }

            swFileLoadError_e errors = 0;
            swFileLoadWarning_e warnings = 0;
            swDocumentTypes_e docType = getDocType(Path.GetExtension(filePath));
            App.OpenDoc6(filePath, (int)docType, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, configNameAdd.Text, (int)errors, (int)warnings);
            ModelDoc2 document = (ModelDoc2)App.ActiveDoc;
            CustomPropertyManager swCustPropMgr = document.Extension.get_CustomPropertyManager("");

            this.BeginInvoke(new Action(() =>
            {
                lblInfo.Text = "Запись свойств в файл " + filePath + "...";
            }));

            string[] customPropertiesNames = swCustPropMgr.GetNames();

            //todo: читать прямо из датагрида
            foreach (CustomProperty customProperty in propToAdd)
            {
                int result;
                int goodResult;

                if (customPropertiesNames != null && customPropertiesNames.Contains(customProperty.Name))
                {
                    goodResult = 0;
                    result = swCustPropMgr.Set(customProperty.Name, customProperty.Value);                }
                else
                {
                    goodResult = 1;
                    result = swCustPropMgr.Add2(customProperty.Name, (int)customProperty.PropertyType, customProperty.Value);
                }

                if (result != goodResult)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        using (DataGridViewRow row = new DataGridViewRow())
                        {
                            row.CreateCells(dgvInfo);
                            row.Cells[0].Value = "Не удалось добавить свойство " + customProperty.Name + " в файл " + filePath;
                            row.Cells[0].Style.ForeColor = Color.Red;
                            dgvInfo.Rows.Add(row);
                        }
                    }));
                }
                else
                {
                    if (!document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, (int)errors, (int)warnings))
                        this.BeginInvoke(new Action(() =>
                        {
                            using (DataGridViewRow row = new DataGridViewRow())
                            {
                                row.CreateCells(dgvInfo);
                                row.Cells[0].Value = "Свойство " + customProperty.Name + " не записано. (Сохранение файла не удалось) Файл: " + filePath;
                                row.Cells[0].Style.ForeColor = Color.Red;
                                dgvInfo.Rows.Add(row);
                            }
                        }));
                    else
                        this.BeginInvoke(new Action(() =>
                        {
                            using (DataGridViewRow row = new DataGridViewRow())
                            {
                                row.CreateCells(dgvInfo);
                                row.Cells[0].Value = "Свойство " + customProperty.Name + " успешно записано в файл " + filePath;
                                row.Cells[0].Style.ForeColor = Color.Green;
                                dgvInfo.Rows.Add(row);
                            }
                        }));
                }


            }
            App.QuitDoc(document.GetTitle());

            if (readOnly)
                File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);
        }
        private void changeProperties(string filePath, SldWorks.SldWorks App)
        {
            this.BeginInvoke(new Action(() =>
            {
                lblInfo.Text = "Открытие файла " + filePath + "...";
            }));

            FileInfo fileInfo = new FileInfo(filePath);
            bool readOnly = false;
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                readOnly = true;
                File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
            }

            swDocumentTypes_e docType = getDocType(Path.GetExtension(filePath));
            swFileLoadError_e errors = 0;
            swFileLoadWarning_e warnings = 0;
            ModelDoc2 document1 = App.OpenDoc6(filePath, (int)docType, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, configNameAdd.Text, (int)errors, (int)warnings);
            ModelDoc2 document = (ModelDoc2)App.ActiveDoc;
            CustomPropertyManager swCustPropMgr = document.Extension.get_CustomPropertyManager("");

            this.BeginInvoke(new Action(() =>
            {
                lblInfo.Text = "Замена свойств в файле " + filePath + "...";
            }));

            string[] names = swCustPropMgr.GetNames();

            //todo: читать прямо из датагрида
            foreach (KeyValuePair<string, string> keyValue in propToChange)
            {
                int result = 0;
                int goodResult = 1;

                if (names != null && !names.Contains(keyValue.Key))
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        using (DataGridViewRow row = new DataGridViewRow())
                        {
                            row.CreateCells(dgvInfo);
                            row.Cells[0].Value = "Свойство " + keyValue.Key + " не содержится в файле " + filePath;
                            row.Cells[0].Style.ForeColor = Color.Red;
                            dgvInfo.Rows.Add(row);
                        }
                    }));
                }
                else
                {
                    string value = swCustPropMgr.Get(keyValue.Key);
                    int type = swCustPropMgr.GetType2(keyValue.Key);
                    if (swCustPropMgr.Delete(keyValue.Key) == 1)
                        result = swCustPropMgr.Add2(keyValue.Value, type, value);

                    if (result != goodResult)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            using (DataGridViewRow row = new DataGridViewRow())
                            {
                                row.CreateCells(dgvInfo);
                                row.Cells[0].Value = "Не удалось заменить имя свойства " + keyValue.Key + " на " + keyValue.Value + "  в файле " + filePath;
                                row.Cells[0].Style.ForeColor = Color.Red;
                                dgvInfo.Rows.Add(row);
                            }
                        }));
                    }
                    else
                    {
                        if (!document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, (int)errors, (int)warnings))
                            this.BeginInvoke(new Action(() =>
                            {
                                using (DataGridViewRow row = new DataGridViewRow())
                                {
                                    row.CreateCells(dgvInfo);
                                    row.Cells[0].Value = "Не удалось заменить имя свойства " + keyValue.Key + " на " + keyValue.Value + " (Сохранение не удалось) в файле " + filePath;
                                    row.Cells[0].Style.ForeColor = Color.Red;
                                    dgvInfo.Rows.Add(row);
                                }
                            }));
                        else
                            this.BeginInvoke(new Action(() =>
                            {
                                using (DataGridViewRow row = new DataGridViewRow())
                                {
                                    row.CreateCells(dgvInfo);
                                    row.Cells[0].Value = "Замена имени свойства " + keyValue.Key + " на " + keyValue.Value + " произведена успешно в файле " + filePath;
                                    row.Cells[0].Style.ForeColor = Color.Green;
                                    dgvInfo.Rows.Add(row);
                                }
                            }));
                    }
                }
            }
            App.QuitDoc(document.GetTitle());

            if (readOnly)
                File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);
        }
        private void deleteProperties(string filePath, SldWorks.SldWorks App)
        {
            this.BeginInvoke(new Action(() =>
            {
                lblInfo.Text = "Открытие файла " + filePath + "...";
            }));

            FileInfo fileInfo = new FileInfo(filePath);
            bool readOnly = false;
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                readOnly = true;
                File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
            }

            swFileLoadError_e errors = 0;
            swFileLoadWarning_e warnings = 0;
            swDocumentTypes_e docType = getDocType(Path.GetExtension(filePath));
            App.OpenDoc6(filePath, (int)docType, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, configNameAdd.Text, (int)errors, (int)warnings);
            ModelDoc2 document = (ModelDoc2)App.ActiveDoc;
            CustomPropertyManager swCustPropMgr = document.Extension.get_CustomPropertyManager("");

            this.BeginInvoke(new Action(() =>
            {
                lblInfo.Text = "Удаление свойств в файле " + filePath + "...";
            }));

            string[] names = swCustPropMgr.GetNames();

            //todo: читать прямо из датагрида
            foreach (string property in propToDel)
            {
                int result;
                int goodResult = 1;

                bool propExists = false;
                if (names != null)
                    if (names.Contains(property))
                        propExists = true;

                if (!propExists)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        using (DataGridViewRow row = new DataGridViewRow())
                        {
                            row.CreateCells(dgvInfo);
                            row.Cells[0].Value = "Свойство " + property + " отсутствует в файле " + filePath;
                            row.Cells[0].Style.ForeColor = Color.Red;
                            dgvInfo.Rows.Add(row);
                        }
                    }));
                }
                else
                {
                    result = swCustPropMgr.Delete(property);
                    if (result != goodResult)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            using (DataGridViewRow row = new DataGridViewRow())
                            {
                                row.CreateCells(dgvInfo);
                                row.Cells[0].Value = "Не удалось удалить свойство " + property + " в файле " + filePath;
                                row.Cells[0].Style.ForeColor = Color.Red;
                                dgvInfo.Rows.Add(row);
                            }
                        }));
                    }
                    else
                    {
                        if (document.Save3((int)swSaveAsOptions_e.swSaveAsOptions_OverrideSaveEmodel, (int)errors, (int)warnings))
                            this.BeginInvoke(new Action(() =>
                            {
                                using (DataGridViewRow row = new DataGridViewRow())
                                {
                                    row.CreateCells(dgvInfo);
                                    row.Cells[0].Value = "Свойство " + property + " успешно удалено в файле " + filePath;
                                    row.Cells[0].Style.ForeColor = Color.Green;
                                    dgvInfo.Rows.Add(row);
                                }
                            }));
                        else
                            this.BeginInvoke(new Action(() =>
                            {
                                using (DataGridViewRow row = new DataGridViewRow())
                                {
                                    row.CreateCells(dgvInfo);
                                    row.Cells[0].Value = "Не удалось удалить свойство " + property + " (Ошибка при сохранении) в файле " + filePath;
                                    row.Cells[0].Style.ForeColor = Color.Red;
                                    dgvInfo.Rows.Add(row);
                                }
                            }));
                    }
                }
            }
            App.QuitDoc(document.GetTitle());

            if (readOnly)
                File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);
        }
        
        /// <summary>
        /// Обработка нажатия кнопки "Начать"
        /// </summary>       
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbPath.Text))
                MessageBox.Show("Не задан путь!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            else
            {
                //todo: if (dgvPropertiesToDelete.Rows.Count == 0 )
                //MessageBox.Show("В списке на обработку не задано ни одно свойство.");
                //else
                {
                    process = true;
                    btnSaveLog.Enabled = false;
                    btnStart.Enabled = false;
                    groupBox1.Enabled = false;
                    groupBox11.Enabled = false;
                    cbHidden.Enabled = false;
                    cbHiddenFolder.Enabled = false;
                    cbSubfolder.Enabled = false;
                    tabControl1.Enabled = false;

                    lblInfo.Text = "";
                    lblFileCount.Text = "0";
                    dgvInfo.Rows.Clear();

                    propToChange = new List<KeyValuePair<string, string>>();
                    foreach (DataGridViewRow r in dgvPropertiesToChange.Rows)
                    {
                        KeyValuePair<string, string> propNames = new KeyValuePair<string, string>((string)r.Cells[0].Value, (string)r.Cells[1].Value);
                        propToChange.Add(propNames);
                    }

                    propToDel = new List<string>();
                    foreach (DataGridViewRow r in dgvPropertiesToDelete.Rows)
                    {
                        propToDel.Add((string)r.Cells[0].Value);
                    }

                    propToAdd = new List<CustomProperty>();
                    foreach (DataGridViewRow r in propertiesGridViewAdd.Rows)
                    {
                        propToAdd.Add(new CustomProperty()
                        {
                            Name = (string)r.Cells[0].Value,
                            PropertyType = getCustomInfoType((string)r.Cells[1].Value),
                            Value = (string)r.Cells[2].Value
                        });
                    }

                    Thread workerThread = new Thread(worker);
                    workerThread.Start();
                }
            }
        }

        private swCustomInfoType_e getCustomInfoType(string stringType)
        {
            switch (stringType)
            {
                case "Текст":
                    return swCustomInfoType_e.swCustomInfoText;
                case "Дата":
                    return swCustomInfoType_e.swCustomInfoDate;
                case "Номер":
                    return swCustomInfoType_e.swCustomInfoNumber;
                case "Да или нет":
                    return swCustomInfoType_e.swCustomInfoYesOrNo;
                default:
                    return swCustomInfoType_e.swCustomInfoUnknown;
            }
        }
        private swDocumentTypes_e getDocType(string extension)
        {
            switch (extension)
            {
                case ".SLDASM":
                    return swDocumentTypes_e.swDocASSEMBLY;
                case ".SLDPRT":
                    return swDocumentTypes_e.swDocPART;
                case ".SLDDRW":
                    return swDocumentTypes_e.swDocDRAWING;
                default:
                    return swDocumentTypes_e.swDocNONE;

            }
        }
        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbType.SelectedIndex == 3)
            {
                tbPropertyValue.Visible = false;
                cbYesNo.Visible = true;
            }
            else
            {
                tbPropertyValue.Visible = true;
                cbYesNo.Visible = false;
            }
        }

        /// <summary>
        /// Пропускать ли файл при обработке
        /// </summary>        
        private bool skipFile(swDocumentTypes_e type)
        {
            switch (type)
            {
                case swDocumentTypes_e.swDocASSEMBLY:
                    return (!cbAsm.Checked);
                case swDocumentTypes_e.swDocPART:
                    return (!cbPart.Checked);
                case swDocumentTypes_e.swDocDRAWING:
                    return (!cbDrw.Checked);
                default:
                    return true;
            }
        }
        /// <summary>
        /// Возвращает название типа документа
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>"Сборка", "Деталь", "Чертеж" или ""</returns>
        private string getDocTypeName(swDocumentTypes_e type)
        {
            switch (type)
            {
                case swDocumentTypes_e.swDocASSEMBLY:
                    return "Сборка";
                case swDocumentTypes_e.swDocPART:
                    return "Деталь";
                case swDocumentTypes_e.swDocDRAWING:
                    return "Чертеж";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// Сохранение лога
        /// </summary>
        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            string path;
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowDialog();
                path = fbd.SelectedPath;
            }

            if (!string.IsNullOrEmpty(path))
            {
                List<string> logStrings = new List<string>();
                foreach (DataGridViewRow row in dgvInfo.Rows)
                {
                    logStrings.Add((string)row.Cells[0].Value);
                }

                path += @"\" + tbLogFileName.Text + ".txt";
                try
                {
                    System.IO.File.WriteAllLines(path, logStrings.ToArray());
                    MessageBox.Show("Лог сохранен.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        #region Обработка интерфейса

        private void btnPath_Click_1(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowDialog();
                tbPath.Text = fbd.SelectedPath;
            }
        }
        private void cbHiddenFolder_CheckedChanged(object sender, EventArgs e)
        {
            if (cbHiddenFolder.Checked)
                cbSubfolder.Checked = true;
        }
        private void cbSubfolder_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbSubfolder.Checked)
                cbHiddenFolder.Checked = false;
        }
        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (process)
                e.Cancel = true;
        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        #endregion

        #endregion
    }
}
