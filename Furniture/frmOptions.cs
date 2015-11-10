using System;
using System.IO;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;


namespace Furniture
{
    public partial class FrmOptions : Form
    {
        private Options _frmOpt;
        private readonly SwAddin _swAdd;
        private readonly ISldWorks _iswApp;
        private readonly int _cook;
        private bool _isNeededRefresh;

        public FrmOptions(SwAddin swAdd, ISldWorks iswApp, int cook)
        {
            _swAdd = swAdd;
            _iswApp = iswApp;
            _cook = cook;

            InitializeComponent();
            Closing += FrmOptionsClosing;
            FormClosed += FrmOptionsClosed;
        }

        void FrmOptionsClosed(object sender, EventArgs e)
        {
            if (_isNeededRefresh)
            {
                _swAdd.DisconnectFromSW();
                _swAdd.ConnectToSW(_iswApp, _cook);
            }
        }

        private void FrmOptionsClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_frmOpt != null)
            {
                if (_frmOpt.TestState)
                {
                    _isNeededRefresh = true;
                }
                _frmOpt.Close();
                _frmOpt = null;
            }
        }

        private void BtnDbPathClick(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = @"Выберите папку с базами данных";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDBPath.Text = folderBrowserDialog1.SelectedPath + @"\";
            }
        }

        private void BtnDrwPathClick(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = @"Выберите папку с чертежами";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDrwPath.Text = folderBrowserDialog1.SelectedPath + @"\";
            }
        }

        private void BtnModelPathClick(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = @"Выберите папку с библиотечными моделями";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtModelPath.Text = folderBrowserDialog1.SelectedPath + @"\";
            }
        }

        private void BtnDecorPathClick(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = @"Выберите папку с декорами";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDecorPath.Text = folderBrowserDialog1.SelectedPath + @"\";
            }
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnOkClick(object sender, EventArgs e)
        {
            if (chckBxUpdateLib.Checked != Properties.Settings.Default.CheckUpdateLib)
                _isNeededRefresh = true;

            // запись настроек в xml furniture.dll.config
            Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("DBPath", txtDBPath.Text);
            Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("DrwPath", txtDrwPath.Text);
            Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("ModelPath", txtModelPath.Text);
            Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("DecorPath", txtDecorPath.Text);
            Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("ConnectIniPath", tbConnectIniPath.Text);
            Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("ConnectParameterName", (String)cbConnectSectionName.SelectedItem);
            Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("OraDbLogin", tbOraDbLogin.Text);
            Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("OraDbPassword", tbOraDbPassword.Text);


            // запись настроек в свойства
            Properties.Settings.Default.DBPath = txtDBPath.Text;
            Properties.Settings.Default.DrwPath = txtDrwPath.Text;
            Properties.Settings.Default.ModelPath = txtModelPath.Text;
            Properties.Settings.Default.Designer = txtDesigner.Text;
            Properties.Settings.Default.DecorPath = txtDecorPath.Text;
            Properties.Settings.Default.AutoSaveComponents = chkAutoSaveComponents.Checked;
            Properties.Settings.Default.AutoSaveDrawings = chkAutoSaveDrawings.Checked;
            Properties.Settings.Default.AutoShowSetParameters = chkAutoShowSetParameters.Checked;
            Properties.Settings.Default.AutoCutOff = chkAutoCutOff.Checked;
            Properties.Settings.Default.AutoRecalculateOnAdd = chkAutoRecalculateOnAdd.Checked;
            Properties.Settings.Default.SetDecorsFromFirstElement = chkColorNotUniq.Checked;
            Properties.Settings.Default.ScaleWhenDimen = chckBxAutoScale.Checked;
            Properties.Settings.Default.CheckCavitiesCross = chBxCvtCrss.Checked;
            Properties.Settings.Default.CheckParamLimits = chBxPrmLmts.Checked;
            Properties.Settings.Default.CheckUpdateLib = chckBxUpdateLib.Checked;
            Properties.Settings.Default.ConvertToDwgPdf = chckBxConvertDwgPdf.Checked;
            Properties.Settings.Default.ImmediatelyDetachModel = chckBxDetachModel.Checked;
            Properties.Settings.Default.DefaultRPDView = chckDefaultRPDView.Checked;
            Properties.Settings.Default.ReloadComponents = chckBxReloadComps.Checked;
            Properties.Settings.Default.ShowRPDBefore = chkShowRPDBeforeEnd.Checked;
            Properties.Settings.Default.AutoArrangeDimension = chckBxAutoArrangeDimension.Checked;
            Properties.Settings.Default.CreatePacketHoles = chckBxCreatePacketHoles.Checked;
            Properties.Settings.Default.DeleteBeforeDim = deleteBeforeDim.Checked;
            Properties.Settings.Default.ViewsBeforeDimen = chbViewBeforeDim.Checked;
            Properties.Settings.Default.DeleteDraftIfStandart = chbDeleteDrawIfStandart.Checked;
            Properties.Settings.Default.SetParentWindow = chckSetParent.Checked;
            Properties.Settings.Default.ConnectIniPath = tbConnectIniPath.Text;
            Properties.Settings.Default.ConnectParameterName = (String)cbConnectSectionName.SelectedItem;
            Properties.Settings.Default.OraDbLogin = tbOraDbLogin.Text;
            Properties.Settings.Default.OraDbPassword = tbOraDbPassword.Text;
            Properties.Settings.Default.Save();
        }

        string ftpResult = Furniture.Helpers.FtpAccess.resultFtp;

        private void FrmOptionsLoad(object sender, EventArgs e)
        {
            label1.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            CreateTextForLabelTest();

            if (Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DBPath") == string.Empty)
            {
                txtDBPath.Text = Properties.Settings.Default.DBPath;
                Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("DBPath", Properties.Settings.Default.DBPath);
            }
            else txtDBPath.Text = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DBPath");

            if (Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DrwPath") == string.Empty)
            {
                txtDrwPath.Text = Properties.Settings.Default.DrwPath;
                Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("DrwPath", Properties.Settings.Default.DrwPath);
            }
            else txtDrwPath.Text = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DrwPath");

            if (Furniture.Helpers.SaveLoadSettings.ReadAppSettings("ModelPath") == string.Empty)
            {
                txtModelPath.Text = Properties.Settings.Default.ModelPath;
                Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("ModelPath", Properties.Settings.Default.ModelPath);
            }
            else txtModelPath.Text = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("ModelPath");

            if (Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DecorPath") == string.Empty)
            {
                txtDecorPath.Text = Properties.Settings.Default.DecorPath;
                Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("DecorPath", Properties.Settings.Default.DecorPath);
            }
            else txtDecorPath.Text = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DecorPath");

            txtDesigner.Text = Properties.Settings.Default.Designer;
            //lblFtpPath.Text += @" " + Properties.Settings.Default.FtpPath;
            lblFtpPath.Text += @" " + ftpResult;

            lblPatchVersion.Text += @" " + Properties.Settings.Default.PatchVersion;

            chkAutoSaveComponents.Checked = Properties.Settings.Default.AutoSaveComponents;
            chkAutoSaveDrawings.Checked = Properties.Settings.Default.AutoSaveDrawings;
            chkAutoShowSetParameters.Checked = Properties.Settings.Default.AutoShowSetParameters;
            chkAutoCutOff.Checked = Properties.Settings.Default.AutoCutOff;
            chBxCvtCrss.Checked = Properties.Settings.Default.CheckCavitiesCross;
            chBxPrmLmts.Checked = Properties.Settings.Default.CheckParamLimits;
            chckBxUpdateLib.Checked = Properties.Settings.Default.CheckUpdateLib;

            btnIpdateLib.Enabled = Properties.Settings.Default.CheckUpdateLib;

            chckBxConvertDwgPdf.Checked = Properties.Settings.Default.ConvertToDwgPdf;
            if (Properties.Settings.Default.CashModeOn)
            {
                Properties.Settings.Default.ImmediatelyDetachModel = false;
                chckBxDetachModel.Checked = Properties.Settings.Default.ImmediatelyDetachModel;
                chckBxDetachModel.Enabled = false;
            }
            else
            {
                chckBxDetachModel.Checked = Properties.Settings.Default.ImmediatelyDetachModel;
                chckBxDetachModel.Enabled = true;
            }

            chckDefaultRPDView.Checked = Properties.Settings.Default.DefaultRPDView;
            chckBxReloadComps.Checked = Properties.Settings.Default.ReloadComponents;
            chckBxAutoScale.Checked = Properties.Settings.Default.ScaleWhenDimen;

            deleteBeforeDim.Checked = Properties.Settings.Default.DeleteBeforeDim;
            chbViewBeforeDim.Checked = Properties.Settings.Default.ViewsBeforeDimen;
            chbDeleteDrawIfStandart.Checked = Properties.Settings.Default.DeleteDraftIfStandart;

            chkAutoRecalculateOnAdd.Checked = Properties.Settings.Default.AutoRecalculateOnAdd;
            chkColorNotUniq.Checked = Properties.Settings.Default.SetDecorsFromFirstElement;
            chkShowRPDBeforeEnd.Checked = Properties.Settings.Default.ShowRPDBefore;
            chckBxAutoArrangeDimension.Checked = Properties.Settings.Default.AutoArrangeDimension;
            chckSetParent.Checked = Properties.Settings.Default.SetParentWindow;
            chckBxCreatePacketHoles.CheckedChanged -= PacketHolesCheckedChanged;
            chckBxCreatePacketHoles.Checked = Properties.Settings.Default.CreatePacketHoles;
            chckBxCreatePacketHoles.CheckedChanged += PacketHolesCheckedChanged;

            if (Furniture.Helpers.SaveLoadSettings.ReadAppSettings("ConnectIniPath") == string.Empty)
            {
                tbConnectIniPath.Text = Properties.Settings.Default.ConnectIniPath;
                //Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("ConnectIniPath", Properties.Settings.Default.ConnectIniPath);
            }
            else tbConnectIniPath.Text = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("ConnectIniPath");


            if (!String.IsNullOrEmpty(Furniture.Helpers.LocalAccounts.connectIniPath) && File.Exists(Furniture.Helpers.LocalAccounts.connectIniPath))
            {
                IniParser parser = new IniParser(Furniture.Helpers.LocalAccounts.connectIniPath);
                String[] connectParameters = parser.EnumSection("CONNECT");
                cbConnectSectionName.Items.Clear();
                cbConnectSectionName.Items.AddRange(connectParameters);
                cbConnectSectionName.SelectedItem = Properties.Settings.Default.ConnectParameterName;
            }

            tbOraDbLogin.Text = Properties.Settings.Default.OraDbLogin;
            tbOraDbPassword.Text = Properties.Settings.Default.OraDbPassword;
            ChkAutoSaveComponentsCheckedChanged(chkAutoSaveComponents, null);
        }

        private void ChkAutoSaveComponentsCheckedChanged(object sender, EventArgs e)
        {
            chkAutoSaveDrawings.Enabled = chkAutoSaveComponents.Checked;
            chkAutoShowSetParameters.Enabled = chkAutoSaveComponents.Checked;
            //chkAutoCutOff.Enabled = chkAutoSaveComponents.Checked;
            chkAutoRecalculateOnAdd.Enabled = chkAutoSaveComponents.Checked;
        }

        private void BtnAdvancedClick(object sender, EventArgs e)
        {
            var pass = new Password();
            pass.Closed += PassClosed;
        }

        private void PassClosed(object sender, EventArgs e)
        {
            var pass = (Password)sender;

            //var pc = new PrincipalContext(ContextType.Domain,"MRDOORS.RU");
            //if (pc.ValidateCredentials("Solid", pass.Passwrd))
            if (pass.Passwrd == Properties.Settings.Default.AdmUsrPsw)
            {
                _frmOpt = new Options();
                _frmOpt.Closing += _frmOpt_Closing;
            }
        }

        private void _frmOpt_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CreateTextForLabelTest();
            _swAdd.UpdateIndicator();
        }

        private void BtnDefOptClick(object sender, EventArgs e)
        {
            if (MessageBox.Show(@"Произвести настройку?", _swAdd.MyTitle, MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.No) return;

            chckBxUpdateLib.Checked = true;

            var list = new List<string>();

            #region Настройки конвертации в DWG

            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swDxfOutputNoScale, 0);
            list.Add("Файл->Сохранить как->Параметры экспорта->DXF/DWG->Масштаб 1:1 : выключено");
            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swDxfVersion,
                                                  (int)swDxfFormat_e.swDxfFormat_R2000);
            list.Add("Файл->Сохранить как->Параметры экспорта->DXF/DWG->Версия: R2000-2002");
            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swDxfOutputFonts, 1);
            list.Add("Файл->Сохранить как->Параметры экспорта->DXF/DWG->Шрифты : TrueType");
            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swDxfOutputLineStyles, 0);
            list.Add("Файл->Сохранить как->Параметры экспорта->DXF/DWG->Типы линой : Стандартные стили AutoCAD");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDxfMapping, false);
            list.Add("Файл->Сохранить как->Параметры экспорта->DXF/DWG->Перевод SolidWorks в DXF/DWG : выключено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDxfEndPointMerge, false);
            list.Add("Файл->Сохранить как->Параметры экспорта->DXF/DWG->Соединение конечных точек : выключено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDxfExportSplinesAsSplines, true);
            list.Add(
                "Файл->Сохранить как->Параметры экспорта->DXF/DWG->Параметры экспорта сплайнов : Экспортировать все сплайны в качестве сплайнов");
            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swDxfMultiSheetOption,
                                                  (int)swDxfMultisheet_e.swDxfMultiSheet);
            list.Add(
                "Файл->Сохранить как->Параметры экспорта->DXF/DWG->Несколько чертежных листов : Экспортировать все листы в один файл");

            #endregion

            #region Настройки конвертации в PDF

            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swPDFExportInColor, false);
            list.Add("Файл->Сохранить как->Параметры экспорта->PDF->Экспортировать PDF в цвете : выключено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swPDFExportEmbedFonts, true);
            list.Add("Файл->Сохранить как->Параметры экспорта->PDF->Встроить шрифты : включено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swPDFExportHighQuality, false);
            list.Add("Файл->Сохранить как->Параметры экспорта->PDF->Линии высокого качества : выключено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swPDFExportPrintHeaderFooter, false);
            list.Add("Файл->Сохранить как->Параметры экспорта->PDF->Печать колонтитулов : выключено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swPDFExportUseCurrentPrintLineWeights, true);
            list.Add("Файл->Сохранить как->Параметры экспорта->PDF->Использовать указанную толщину линии принтера (Файл, Печать, Толщина линии) : включено");
            #endregion

            #region Основные настройки
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swExtRefUpdateCompNames, true);
            list.Add(
                "Настройки пользователя->Внешние ссылки->Сборки->Обновить имена компонентов, когда документы заменяются : включено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swUseFolderSearchRules, true);
            list.Add("Настройки пользователя->Внешние ссылки->Поиск внешних ссылок в списке папок документа : включено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swExternalReferencesDisable, false);
            list.Add("Настройки пользователя->Внешние ссылки->Не создавать внешние ссылки для модели : выключено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swLargeAsmModeEnabled, false);
            list.Add("Настройки пользователя->Сборки->Большие сборки->");
            list.Add("Использовать режим большой сборки, чтобы улучшить производительность при работе со сборкой, количество компонентов которой превышает это число : выключено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swAutoLoadPartsLightweight, false);
            list.Add(
                "Настройки пользователя->Качество изображения->Сборки->Автоматически загрузить детали как сокращенные : выключено");
            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swPerformanceAlwaysResolveSubassemblies, true);
            list.Add(
                "Настройки пользователя->Качество изображения->Сборки->Всегда решать узлы сборки : включено");
            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swCheckForOutOfDateLightweightComponents, (int)swCheckOutOfDate_e.swCheckOutOfDate_AlwaysResolve);
            list.Add(

                "Настройки пользователя->Качество изображения->Сборки->проверить существование старых сокращенных компонентов : всегда решать");
            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swResolveLightweight,
                                                  (int)swPromptAlwaysNever_e.swResponseAlways);
            list.Add("Настройки пользователя->Качество изображения->Сборки->Решить сокращенные детали : всегда");
            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swPerformanceAssemRebuildOnLoad,
                                                  (int)swPromptAlwaysNever_e.swResponseAlways);
            list.Add("Настройки пользователя->Качество изображения->Сборки->перестроить сборку при запуске : всегда");

            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swTransparencyHighQualityDynamic, false);
            list.Add("Настройки пользователя->Качество изображения->Прозрачность->Высокое качество для динамического вида : выключено");

            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swLevelOfDetail, 20);
            list.Add("Настройки пользователя->Качество изображения->Уровень детализации : Меньше (быстрее)");

            _iswApp.SetUserPreferenceDoubleValue((int)swUserPreferenceDoubleValue_e.swMateAnimationSpeed, 0);
            list.Add("Настройки пользователя->Качество изображения->Сборки->Скорость анимации : выключено");

            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swPerformancePreviewDuringOpen, false);
            list.Add("Настройки пользователя->Качество изображения-> Нет предварительного просмотра во время открытия : включено");

            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swFeatureManagerEnsureVisible, false);
            list.Add("Настройки пользователя->Качество изображения->Скроллинг выбранного элемента вида: выключено");

            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swAutoSaveEnable, true);
            list.Add("Настройки пользователя->Резервные копии->Сохранять инфо автовосстановления: включено");

            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swAutoSaveInterval, 5);
            list.Add("Настройки пользователя->Резервные копии->Сохранять инфо автовосстановления каждые: 5 минут");

            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSaveReminderEnable, true);
            list.Add("Настройки пользователя->Резервные копии->Отобразить напоминание, если документ не был сохранен: включено");

            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSaveReminderInterval, 4);
            list.Add("Настройки пользователя->Резервные копии->Отобразить напоминание, если документ не был сохранен каждые: 4 минуты");

            _iswApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swLoadExternalReferences, (int)swLoadExternalReferences_e.swLoadExternalReferences_None);
            list.Add("Настройки пользователя->Внешние ссылки->Запуск документов с ссылкой: Нет");

            string assemblyPath = @"C:\ProgramData\SolidWorks\SolidWorks 2011\templates\Сборка.asmdot";
            string partPath = @"C:\ProgramData\SolidWorks\SolidWorks 2011\templates\Деталь.prtdot";
            string drawPath = @"C:\ProgramData\SolidWorks\SolidWorks 2011\templates\Чертеж.drwdot";



            if (!File.Exists(assemblyPath) || !File.Exists(partPath) || !File.Exists(drawPath))
            {
                File.Delete(assemblyPath);
                File.Delete(partPath);
                File.Delete(drawPath);

                ModelDoc2 part = _swAdd._iSwApp.NewPart();
                _swAdd._iSwApp.CloseDoc(Path.GetFileNameWithoutExtension(part.GetPathName()));
            }

            if (File.Exists(assemblyPath))
            {
                if (_iswApp.SetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly, assemblyPath))
                    list.Add(@"Шаблоны документов->Сборка: " + assemblyPath);
                else
                    list.Add(@"Не удалось установить шаблон сборки: " + assemblyPath);
            }
            else
                list.Add(@"Не найден файл шаблона сборки.");

            if (File.Exists(partPath))
            {
                if (_iswApp.SetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplatePart, partPath))
                    list.Add(@"Шаблоны документов->Деталь: " + partPath);
                else
                    list.Add(@"Не удалось установить шаблон детали: " + partPath);
            }
            else
                list.Add(@"Не найден файл шаблона детали.");

            if (File.Exists(drawPath))
            {
                if (_iswApp.SetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateDrawing, drawPath))
                    list.Add(@"Шаблоны документов->Чертеж: " + drawPath);
                else
                    list.Add(@"Не удалось установить шаблон чертежа: " + drawPath);
            }
            else
                list.Add(@"Не найден файл шаблона чертежа.");

            IModelDoc2 tt = _iswApp.ActiveDoc;
            if (tt != null)
            {
                tt.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDisplaySketches, false);
                list.Add("Вид->Эскизы : не отображать.");
            }
            if (Furniture.Helpers.LocalAccounts.modelPathResult == @"D:\_SWLIB_")
                txtModelPath.Text = @"D:\_SWLIB_\";
            if (Furniture.Helpers.LocalAccounts.modelPathResult == @"C:\_SWLIB_")
                txtModelPath.Text = @"C:\_SWLIB_\";
            txtDesigner.Text = UserPrincipal.Current.DisplayName;
            //Properties.Settings.Default.FtpPath = @"ftp://194.84.146.5/solidlibupdate";
            Properties.Settings.Default.FtpPath = ftpResult + @"solidlibupdate";
            Properties.Settings.Default.NameFtpUserForLibUpdate = "ftpsolidRO";
            //Properties.Settings.Default.FurnFtpPath = @"ftp://194.84.146.5/";
            Properties.Settings.Default.FurnFtpPath = ftpResult;
            Properties.Settings.Default.FurnFtpName = @"solidk";
            #region выставляем флажки во вкладка "Дополнительные"

            Properties.Settings.Default.AutoSaveComponents = true;
            Properties.Settings.Default.AutoSaveDrawings = true;
            Properties.Settings.Default.AutoShowSetParameters = true;
            Properties.Settings.Default.AutoCutOff = false;
            Properties.Settings.Default.AutoRecalculateOnAdd = false;
            Properties.Settings.Default.SetDecorsFromFirstElement = true;
            Properties.Settings.Default.ScaleWhenDimen = false;
            Properties.Settings.Default.CheckCavitiesCross = false;
            Properties.Settings.Default.CheckParamLimits = false;
            Properties.Settings.Default.ConvertToDwgPdf = false;
            Properties.Settings.Default.ImmediatelyDetachModel = false;
            Properties.Settings.Default.ReloadComponents = false;
            Properties.Settings.Default.ShowRPDBefore = false;
            Properties.Settings.Default.AutoArrangeDimension = false;
            Properties.Settings.Default.DeleteBeforeDim = false;
            Properties.Settings.Default.DeleteDraftIfStandart = false;
            Properties.Settings.Default.SetParentWindow = false;
            Properties.Settings.Default.Save();

            chkAutoSaveComponents.Checked = Properties.Settings.Default.AutoSaveComponents;
            chkAutoSaveDrawings.Checked = Properties.Settings.Default.AutoSaveDrawings;
            chkAutoShowSetParameters.Checked = Properties.Settings.Default.AutoShowSetParameters;
            chkAutoCutOff.Checked = Properties.Settings.Default.AutoCutOff;
            chBxCvtCrss.Checked = Properties.Settings.Default.CheckCavitiesCross;
            chBxPrmLmts.Checked = Properties.Settings.Default.CheckParamLimits;
            chckBxUpdateLib.Checked = Properties.Settings.Default.CheckUpdateLib;

            btnIpdateLib.Enabled = Properties.Settings.Default.CheckUpdateLib;

            chckBxConvertDwgPdf.Checked = Properties.Settings.Default.ConvertToDwgPdf;
            chckBxDetachModel.Checked = Properties.Settings.Default.ImmediatelyDetachModel;
            chckDefaultRPDView.Checked = Properties.Settings.Default.DefaultRPDView;
            chckBxReloadComps.Checked = Properties.Settings.Default.ReloadComponents;
            chckBxAutoScale.Checked = Properties.Settings.Default.ScaleWhenDimen;

            chkAutoRecalculateOnAdd.Checked = Properties.Settings.Default.AutoRecalculateOnAdd;
            chkColorNotUniq.Checked = Properties.Settings.Default.SetDecorsFromFirstElement;
            chkShowRPDBeforeEnd.Checked = Properties.Settings.Default.ShowRPDBefore;
            chckBxAutoArrangeDimension.Checked = Properties.Settings.Default.AutoArrangeDimension;
            deleteBeforeDim.Checked = Properties.Settings.Default.DeleteBeforeDim;
            chbDeleteDrawIfStandart.Checked = Properties.Settings.Default.DeleteDraftIfStandart;
            ChkAutoSaveComponentsCheckedChanged(chkAutoSaveComponents, null);
            #endregion
            #endregion

            #region меню View

            _iswApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swViewShowAnnotationLinkErrors, true);
            #endregion

            #region Изменение горячих клавиш
            const string RegPath = @"Software\SolidWorks\SolidWorks 2011\Customization";

            RegistryKey hkClsid = null;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    hkClsid = Registry.CurrentUser.OpenSubKey(RegPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
                    if (hkClsid != null)
                    {
                        hkClsid = hkClsid.OpenSubKey(@"tPlate" + i + @"\Custom Accelerators\AddArray", RegistryKeyPermissionCheck.ReadWriteSubTree);
                        if (hkClsid.OpenSubKey(@"3_13") != null)
                            hkClsid.DeleteSubKey(@"3_13", false);

                        hkClsid = Registry.CurrentUser.OpenSubKey(RegPath,
                                                                  RegistryKeyPermissionCheck.ReadWriteSubTree);
                        hkClsid = hkClsid.OpenSubKey(@"tPlate" + i + @"\Custom Accelerators\SubArray",
                                                     RegistryKeyPermissionCheck.ReadWriteSubTree);
                        var isExist = hkClsid.OpenSubKey(@"3_13");
                        if (isExist == null)
                        {
                            hkClsid = hkClsid.CreateSubKey(@"3_13", RegistryKeyPermissionCheck.ReadWriteSubTree);
                            hkClsid.SetValue("cmd", 38524, RegistryValueKind.DWord);
                            hkClsid.SetValue("fVirt", 3, RegistryValueKind.DWord);
                            hkClsid.SetValue("key", 13, RegistryValueKind.DWord);

                        }
                    }
                }

            }
            catch
            {
            }
            #endregion

            #region изменение округления единиц измерения в шаблоне
            string pathToTemplate = _iswApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
            if (!string.IsNullOrEmpty(pathToTemplate))
            {
                int warnings = 0;
                int errors = 0;
                ModelDoc2 template = _iswApp.OpenDoc6(pathToTemplate, (int)swDocumentTypes_e.swDocASSEMBLY,
                                                      (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", errors,
                                                      warnings);
                if (template != null)
                {
                    _iswApp.ActivateDoc2(pathToTemplate, true, ref errors);
                    template.SetUserPreferenceIntegerValue(
                        (int)swUserPreferenceIntegerValue_e.swUnitsLinearDecimalPlaces,
                        1);
                    template.Save();
                    _iswApp.CloseDoc(pathToTemplate);
                }
            }

            #endregion


            new ListSettings(list);
        }

        private void CreateTextForLabelTest()
        {
            if (Properties.Settings.Default.UpdateToTest)
            {
                label2.Text =
                    @"Обновление происходит до тестовой версии.";
                label3.Text = @"Для изменения обновления на рабочии версии: Сервисные - Администраторские настройки - 
ввести пароль пользователя 'Solid' и снять галочку с 'обновлять до тестовой версии'";
            }
            else
            {
                label2.Text = "";
                label3.Text = "";
            }
            Application.DoEvents();
        }

        private void btnIpdateLib_Click(object sender, EventArgs e)
        {
            var f = new UpdatingLib("L");
            f.Updating(true);
        }

        private void chckBxUpdateLib_CheckedChanged(object sender, EventArgs e)
        {
            //Properties.Settings.Default.CheckUpdateLib = ((CheckBox) sender).Checked;
            //btnIpdateLib.Enabled = Properties.Settings.Default.CheckUpdateLib;
            btnIpdateLib.Enabled = ((CheckBox)sender).Checked;
            //Properties.Settings.Default.Save();
        }

        private void PacketHolesCheckedChanged(object sender, EventArgs e)
        {
            if (chckBxCreatePacketHoles.Checked)
                MessageBox.Show("Опция вырезать отверстия пакетно - существенно ускоряет быстродействие при вычитании отверстий! Отверстия будут вычитаться единым объектом для каждой детали. Это изменит работу опции \"Проверить пересечение фурнитуры\". Кроме того удалять отверстия поодному можно будет пересоздавая отверстия для детали на которой они будут удалены с помощью опции \"Пересоздать отверстия для детали\".");
        }

        private void btnConnectIniPath_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Connect file (*.ini) | *.ini";
            openFileDialog.InitialDirectory = @"C:\";
            openFileDialog.Title = @"Выберите путь к файлу connect.ini";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                tbConnectIniPath.Text = openFileDialog.FileName;

                IniParser parser = new IniParser(tbConnectIniPath.Text);
                String[] connectParameters = parser.EnumSection("CONNECT");
                cbConnectSectionName.Items.Clear();
                cbConnectSectionName.Items.AddRange(connectParameters);
                cbConnectSectionName.SelectedIndex = 0;
            }

            Repository.Flush();
        }

        private void DbConnectChanged(object sender, EventArgs e)
        {
            Repository.Flush();

        }

        private void FrmOptions_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
    }
}