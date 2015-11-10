using System;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Data.OleDb;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Furniture.ProgressBar;
using HookLibrary;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swpublished;
using SolidWorks.Interop.swconst;
using SolidWorksTools;
using SolidWorksTools.File;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using SwDocumentMgr;
using Environment = System.Environment;
using View = SolidWorks.Interop.sldworks.View;
using System.Globalization;
using System.Drawing;





namespace Furniture
{
    /// <summary>
    /// Summary description for Furniture.
    /// </summary>
    [Guid("58b5dd82-98b7-40a3-b285-dab574cdfc7f"), ComVisible(true)]
    [SwAddin(
        Description = "MrDoors addin",
        Title = "MrDoors",
        LoadAtStartup = true
        )]

    public class SwAddin : ISwAddin
    {
        
        #region Local Variables

        public static bool needWait = false;
        public static object workerLocker = new object();
        public static object workerLocker2 = new object();
        internal ISldWorks _iSwApp;
        private ICommandManager _iCmdMgr;
        private int _addinId;
        internal IntPtr _swHandle;
        public string MyTitle = "MrDoors";
        public ModelDoc2 SwModel;
        public ModelDoc2 RootModel;
        private FrmSetParameters _frmPrm;
        private FrmEdge _frmEdge;
        private ListHiddenComponent _hidComp;
        private readonly LinkedList<string> _allPaths = new LinkedList<string>();
        internal static bool IsEventsEnabled = true;
        private bool _isCheckedSaving;
        private List<Feature> _features = new List<Feature>();
        private LinkedList<Component2> _comps = new LinkedList<Component2>();
        private bool _neededReloadFirstLayerComponent;
        private bool _wasDocChange;
        internal static FrmOptions FrmOption;
        internal static FolderBrowserDialog BrowserDialog;
        private Dictionary<string, int> drawingDictionary;
        private KeyboardHook hook;
        public string currentPath;
        public Dictionary<string, int> DrawingDictionary
        {
            private set { drawingDictionary = value; }
            get { return drawingDictionary; }
        }


        #region Event Handler Variables

        private Hashtable _openDocs;
        private SldWorks _swEventPtr;

        #endregion

        #region Property Manager Variables

        private UserPmPage _ppage;

        #endregion

        [DllImport("user32.dll")]
        internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        internal static extern IntPtr SwitchToThisWindow(IntPtr hWnd, bool Restore);

        public ISldWorks SwApp
        {
            get { return _iSwApp; }
        }

        public ICommandManager CmdMgr
        {
            get { return _iCmdMgr; }
        }

        #endregion

        #region SolidWorks Registration

        [ComRegisterFunctionAttribute]
        public static void RegisterFunction(Type t)
        {
            #region Get Custom Attribute: SwAddinAttribute

            Type type = typeof(SwAddin);
            SwAddinAttribute sWattr =
                type.GetCustomAttributes(false).OfType<SwAddinAttribute>().Select(attr => attr).FirstOrDefault();

            #endregion

            Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
            Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

            string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID + "}";
            Microsoft.Win32.RegistryKey addinkey = hklm.CreateSubKey(keyname);
            addinkey.SetValue(null, 0);

            addinkey.SetValue("Description", sWattr.Description);
            addinkey.SetValue("Title", sWattr.Title);

            keyname = "Software\\SolidWorks\\AddInsStartup\\{" + t.GUID + "}";
            addinkey = hkcu.CreateSubKey(keyname);
            addinkey.SetValue(null, Convert.ToInt32(sWattr.LoadAtStartup), Microsoft.Win32.RegistryValueKind.DWord);
        }

        [ComUnregisterFunctionAttribute]
        public static void UnregisterFunction(Type t)
        {
            Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
            Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

            string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID + "}";
            hklm.DeleteSubKey(keyname);

            keyname = "Software\\SolidWorks\\AddInsStartup\\{" + t.GUID + "}";
            hkcu.DeleteSubKey(keyname);
        }

        
        #endregion

        #region ISwAddin Implementation

        public bool ConnectToSW(object thisSw, int cookie)
        {
            // Записать все настройки в xml config
            Furniture.Helpers.SaveLoadSettings.SaveAllProperties();

            bool ret;
            _iSwApp = (ISldWorks)thisSw;
            if (!Properties.Settings.Default.CashModeAvailable)
            {
                Properties.Settings.Default.CashModeOn = false;
                Properties.Settings.Default.Save();
            }
            if (!Properties.Settings.Default.KitchenModeAvailable)
            {
                Properties.Settings.Default.KitchenModeOn = false;
                Properties.Settings.Default.Save();
            }
            _addinId = cookie;
            _iSwApp.SetAddinCallbackInfo(0, this, _addinId);

            if (!CheckSolid())
            {
                string strReg = Interaction.InputBox("Введите регистрационный код:", MyTitle, "", 0, 0);
                if (strReg != "")
                {

                    try
                    {
                        // Запись ключа 
                        // SaveInRegEdit(strReg);
                        Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("SerialKey", strReg);

                        Properties.Settings.Default.AppRegCode = strReg;  // Старый вариант 
                        Properties.Settings.Default.Save();

                    }

                    catch (SecurityException ex)
                    {
                        Logging.Log.Instance.Fatal(ex, ex.Message.ToString() + "ConnectToSW");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Logging.Log.Instance.Fatal(ex, ex.Message.ToString() + "ConnectToSW");
                    }
                    finally
                    {
                        Properties.Settings.Default.AppRegCode = strReg;  // Старый вариант 
                        Properties.Settings.Default.Save();
                    }
                }
            }
            if (CheckSolid())
            {
                var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                #region закачать всякие недостающие файлы
                List<string> filesToDownload = new List<string>();
                filesToDownload.Add("Oracle.ManagedDataAccess.dll");
                filesToDownload.Add("log4net.dll");
                foreach (string file in filesToDownload)
                {
                    if (!File.Exists(Path.Combine(dirName, file)))
                    {
                        //скачать..
                        UpdaterFromFtp.DownloadFromFtp(file);
                    }
                }


                #endregion

                #region скачивание нового файла для обновления
                string newFileLocation;

                string lastFileLocation = dirName + "\\Furniture.dll";
                string updFullName = dirName + "\\update_furniture.exe";
                var ss = new SecureString();
                foreach (var ch in Properties.Settings.Default.AdmUsrPsw)
                {
                    ss.AppendChar(ch);
                }
                var psi = new ProcessStartInfo
                              {
                                  UseShellExecute = false,
                                  Domain = Environment.UserDomainName
                              };
                if (CheckIsSolidUserInAdmin())
                {
                    psi.UserName = Properties.Settings.Default.AdmUsrName;
                    psi.Password = ss;
                }
                if (!File.Exists(updFullName))
                {
                    string updTmpPath;
                    if (DownloadUpdFrn(out updTmpPath))
                    {
                        psi.FileName = updTmpPath;
                        psi.Arguments = Path.GetDirectoryName(updTmpPath) + "\\update_furniture.exe" + "@" +
                                        updFullName;
                        Process.Start(psi);
                    }
                }

                #endregion

                #region Обновление

                if (CheckUpdate(out newFileLocation))
                {
                    string arguments = newFileLocation + "@" + lastFileLocation;

                    if (File.Exists(updFullName))
                    {
                        if (!CheckIsSolidUserInAdmin())
                        {
                            psi = new ProcessStartInfo(updFullName, arguments);
                            psi.UseShellExecute = false;
                            psi.Domain = Environment.UserDomainName;
                        }
                        else
                        {
                            psi.FileName = updFullName;
                            psi.Arguments = arguments;
                        }
                        Process.Start(psi);
                        return false;
                    }
                }

                if (Properties.Settings.Default.CheckUpdateLib)
                    UpdatePatch();

                #endregion

                _iCmdMgr = _iSwApp.GetCommandManager(cookie);
                ChangeActiveDoc(false);

                _swEventPtr = (SldWorks)_iSwApp;
                _openDocs = new Hashtable();
                AttachEventHandlers();

                AddPmp();
                _swHandle = (IntPtr)((Frame)_iSwApp.Frame()).GetHWnd();
                WriteLogInfoOnServer();

                ret = true;

                hook = new KeyboardHook();
                hook.AddFilter(Keys.F, true, true, true);
                hook.AddFilter(Keys.Enter, false, false, false);
                hook.KeyPressed += new KeyboardHookEventHandler(hook_KeyPressed);
                hook.Install();
            }
            else
            {
                throw new Exception();
            }
            return ret;
        }


        static void SaveInRegEdit(string value)
        {
            var password = new SecureString();
            password.AppendChar('Q');
            password.AppendChar('p');
            password.AppendChar('6');
            password.AppendChar('7');
            password.AppendChar('y');
            password.AppendChar('S');
            password.AppendChar('h');


            string commands = @"REG ADD HKLM\SOFTWARE\addinSW /v SerialKey /t reg_sz /d " + value;
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = string.Format(@"/c" + commands),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    UserName = "Solid",
                    Password = password,
                    Domain = "mrdoors",
                }
            })
            {
                process.Start();
                process.CloseMainWindow();
                process.WaitForExit();

            }



        }



        private void hook_KeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (e.KeyStroke.Alt == false && e.KeyStroke.Ctrl == false && e.KeyStroke.Shift == false && e.KeyStroke.KeyCode == Keys.Enter)
            {
                if (_frmEdge != null)
                {
                    _frmEdge.SaveSettings();
                    SwitchToThisWindow(_swHandle, false);
                    _frmEdge.Activate();
                }
                else
                    return;

            }
            else if (e.KeyStroke.Alt == true && e.KeyStroke.Ctrl == true && e.KeyStroke.Shift == true && e.KeyStroke.KeyCode == Keys.F)
            {
                hook.KeyPressed -= new KeyboardHookEventHandler(hook_KeyPressed);
                hook.Uninstall();
                hook.Dispose();
                hook = null;
                var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string lastFileLocation = dirName + "\\Furniture.dll";
                _iSwApp.UnloadAddIn(lastFileLocation); //(@"C:\Program Files\SolidWorks-Russia\MrDoors\Furniture.dll");
            }

        }
        private bool CheckIsSolidUserInAdmin()
        {
            try
            {
                var builtinAdminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

                var ctx = new PrincipalContext(ContextType.Machine);

                GroupPrincipal group = GroupPrincipal.FindByIdentity(ctx, builtinAdminSid.Value);

                return group.Members.Any(p => p.Name.Contains("Solid"));
            }
            catch
            {
                return true;
            }
        }

        public bool DisconnectFromSW()
        {
            RemoveSetParametersForm();
            //RemoveCommandMgr();
            RemovePopupMenu();
            RemoveMenu();
            RemovePmp();
            DetachEventHandlers();

            _iSwApp = null;
            GC.Collect();
            return true;
        }

        #endregion

        #region UI Methods

        public void AddCommandMgr()
        {
            var iBmp = new BitmapHandler();
            const string title = "MrDoors";
            const string toolTip = "MrDoors addin";


            var docTypes = new[]
                               {
                                   (int) swDocumentTypes_e.swDocASSEMBLY,
                                   (int) swDocumentTypes_e.swDocDRAWING,
                                   (int) swDocumentTypes_e.swDocPART
                               };

            Assembly thisAssembly = Assembly.GetAssembly(GetType());

            ICommandGroup cmdGroup = _iCmdMgr.CreateCommandGroup(1, title, toolTip, "", -1);
            cmdGroup.LargeIconList = iBmp.CreateFileFromResourceBitmap("Furniture.ToolbarLarge.bmp", thisAssembly);
            cmdGroup.SmallIconList = iBmp.CreateFileFromResourceBitmap("Furniture.ToolbarSmall.bmp", thisAssembly);
            cmdGroup.LargeMainIcon = iBmp.CreateFileFromResourceBitmap("Furniture.MainIconLarge.bmp", thisAssembly);
            cmdGroup.SmallMainIcon = iBmp.CreateFileFromResourceBitmap("Furniture.MainIconSmall.bmp", thisAssembly);

            int cmdIndex00 = cmdGroup.AddCommandItem("MrDoors РПД", -1, "", "", -1, "ShowSetParameters",
                                                     "ShowSetParametersEnable", 0);
            int cmdIndex0 = cmdGroup.AddCommandItem("Скопировать выбранные компоненты", -1, "", "", 0,
                                                    "SaveAsComponents", "SaveAsComponentsEnable", 0);
            int cmdIndex1 = cmdGroup.AddCommandItem("Пересчитать конфигурацию деталей", -1, "", "", 2,
                                                    "RecalculateRanges", "RecalculateRangesEnable", 0);
            int cmdIndex7 = cmdGroup.AddCommandItem("Пересоздать отверстия для детали(не пакетно)", -1, "", "", 2, "CutOffDetail", "SaveAsComponentsEnable", 0);

            int cmdIndex2 = cmdGroup.AddCommandItem("Создать отверстия под фурнитуру", -1, "", "", -1, "CutOff",
                                                    "CutOffEnable", 0);
            int cmdIndex3 = cmdGroup.AddCommandItem("Копировать чертежи", -1, "", "", -1, "CopyDrawings",
                                                    "CopyDrawingsEnable", 0);
            int cmdIndex4 = cmdGroup.AddCommandItem("", -1, "", "", -1, "ShowOptions", "", 0);
            int cmdIndex5 = cmdGroup.AddCommandItem("Настройки", -1, "", "", 0, "ShowOptions", "", 0);
            int cmdIndex6 = cmdGroup.AddCommandItem("Открыть все чертежи", -1, "", "", -1, "OpenDrawings",
                                                    "OpenDrawingsEnable", 0);



            cmdGroup.HasToolbar = true;
            cmdGroup.HasMenu = true;
            cmdGroup.Activate();

            bool bResult;

            foreach (int type in docTypes)
            {
                ICommandTab cmdTab = _iCmdMgr.GetCommandTab(type, title);

                if (cmdTab == null)
                {
                    cmdTab = _iCmdMgr.AddCommandTab(type, title);

                    CommandTabBox cmdBox = cmdTab.AddCommandTabBox();

                    var cmdIDs = new int[6];
                    var textType = new int[6];

                    cmdIDs[0] = cmdGroup.get_CommandID(cmdIndex0);
                    System.Diagnostics.Debug.Print(cmdGroup.get_CommandID(cmdIndex0).ToString());
                    textType[0] = (int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextHorizontal;

                    cmdIDs[1] = cmdGroup.get_CommandID(cmdIndex1);
                    System.Diagnostics.Debug.Print(cmdGroup.get_CommandID(cmdIndex1).ToString());
                    textType[1] = (int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextHorizontal;

                    cmdIDs[2] = cmdGroup.get_CommandID(cmdIndex2);
                    System.Diagnostics.Debug.Print(cmdGroup.get_CommandID(cmdIndex2).ToString());
                    textType[2] = (int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextHorizontal;

                    cmdIDs[3] = cmdGroup.get_CommandID(cmdIndex3);
                    System.Diagnostics.Debug.Print(cmdGroup.get_CommandID(cmdIndex3).ToString());
                    textType[3] = (int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextHorizontal;

                    cmdIDs[4] = cmdGroup.ToolbarId;
                    System.Diagnostics.Debug.Print(cmdIDs[4].ToString());
                    textType[4] = (int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextHorizontal |
                                  (int)swCommandTabButtonFlyoutStyle_e.swCommandTabButton_ActionFlyout;

                    cmdIDs[5] = cmdGroup.get_CommandID(cmdIndex6);
                    System.Diagnostics.Debug.Print(cmdGroup.get_CommandID(cmdIndex6).ToString());
                    textType[5] = (int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextHorizontal;

                    bResult = cmdBox.AddCommands(cmdIDs, textType);

                    CommandTabBox cmdBox1 = cmdTab.AddCommandTabBox();
                    cmdIDs = new int[1];
                    textType = new int[1];

                    cmdIDs[0] = cmdGroup.ToolbarId;
                    textType[0] = (int)swCommandTabButtonTextDisplay_e.swCommandTabButton_TextBelow |
                                  (int)swCommandTabButtonFlyoutStyle_e.swCommandTabButton_ActionFlyout;

                    bResult = cmdBox1.AddCommands(cmdIDs, textType);

                    cmdTab.AddSeparator(cmdBox1, cmdGroup.ToolbarId);
                }
            }
            thisAssembly = null;
            iBmp.Dispose();
        }

        public void RemoveCommandMgr()
        {
            _iCmdMgr.RemoveCommandGroup(1);
        }

        public void ChangeActiveDoc(bool isDestroy)
        {
            RemoveSetParametersForm();

            string tmp = _iSwApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swFileLocationsDesignLibrary);
            if ((tmp.ToLower().Contains("_swlib_backup") && !Properties.Settings.Default.CashModeOn) || (!tmp.ToLower().Contains("_swlib_backup") && Properties.Settings.Default.CashModeOn))
                ChangeCashMode();
            SwModel = (ModelDoc2)_iSwApp.ActiveDoc;
            bool isProceedMenuChanging = true;

            if (isDestroy)
            {
                while (SwModel != null)
                {
                    SwModel = (ModelDoc2)SwModel.GetNext();
                    if (SwModel != null)
                        if (SwModel.Visible)
                        {
                            isProceedMenuChanging = false;
                            break;
                        }
                }
            }
            if (isProceedMenuChanging)
            {
                var docType = (int)swDocumentTypes_e.swDocNONE;
                if (SwModel != null)
                    docType = SwModel.GetType(); //установка типа документа

                RemoveMenu();

                _iSwApp.AddMenu(docType, MyTitle, docType == (int)swDocumentTypes_e.swDocNONE ? 3 : 5);

                if (docType == (int)swDocumentTypes_e.swDocASSEMBLY)
                {
                    _iSwApp.AddMenuItem2(docType, _addinId, "MrDoors РПД@" + MyTitle, -1, "ShowSetParameters",
                                         "ShowSetParametersEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "@" + MyTitle, -1, "ShowOptions", "", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Расчет цены проекта@" + MyTitle, -1, "GetProjectPrice", "GetProjectPriceEnable", "");

                    _iSwApp.AddMenuItem2(docType, _addinId, "Экспорт в Покупки@" + MyTitle, -1, "PurchaseExport", "PurchaseExportEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "@" + MyTitle, -1, "ShowOptions", "", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Скопировать выбранные компоненты@" + MyTitle, -1,
                                         "SaveAsComponents", "SaveAsComponentsEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Скопировать проект в другой заказ@" + MyTitle, -1,
                                                    "CopyProjectFormShow", "CopyProjectFormShowEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Оторвать все@" + MyTitle, -1,
                                                    "CopyToLocal", "CopyToLocalEnable", "");

                    _iSwApp.AddMenuItem2(docType, _addinId, "Пересчитать конфигурацию деталей@" + MyTitle, -1,
                                      "RecalculateRanges", "RecalculateRangesEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Пересоздать отверстия для детали(не пакетно)@" + MyTitle, -1,
                                         "CutOffDetail", "SaveAsComponentsEnable", "");

                    if (Properties.Settings.Default.KitchenModeOn)
                        _iSwApp.AddMenuItem2(docType, _addinId, "Переключить режим кухни. Сейчас режим кухни: ВКЛ@" + MyTitle, -1,
                                                  "ChangeKitchenMode", "ChangeKitchenModeEnable", "");
                    else
                        _iSwApp.AddMenuItem2(docType, _addinId, "Переключить режим кухни. Сейчас режим кухни: ВЫКЛ@" + MyTitle, -1,
                                                  "ChangeKitchenMode", "ChangeKitchenModeEnable", "");

                    if (Properties.Settings.Default.CashModeOn)
                        _iSwApp.AddMenuItem2(docType, _addinId, "Переключить режим кэша. Сейчас режим кэша: ВКЛ@" + MyTitle, -1,
                                                  "ChangeCashMode", "ChangeCashModeEnable", "");
                    else
                        _iSwApp.AddMenuItem2(docType, _addinId, "Переключить режим кэша. Сейчас режим кэша: ВЫКЛ@" + MyTitle, -1,
                                                  "ChangeCashMode", "ChangeCashModeEnable", "");

                    _iSwApp.AddMenuItem2(docType, _addinId, "Создать замер@" + MyTitle, -1,
                                                 "CreateMetering", "CreateMeteringEnable", "");

                    _iSwApp.AddMenuItem2(docType, _addinId, "Создать отверстия и вырезы@" + MyTitle, -1, "CutOff",
                                         "CutOffEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Проверить на пересечение фурнитуры@" + MyTitle, -1,
                                         "CheckCutOff", "CheckCutOffEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Удалить все созданные отверстия@" + MyTitle, -1,
                                         "RemoveCavity", "RemoveCavityEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Удалить ошибочные отверстия@" + MyTitle, -1,
                                         "RemoveErrorCavity", "RemoveErrorCavityEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Вернуть цвета после изменения прозрачности@" + MyTitle, -1,
                                         "FixColor", "FixColorEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Заменить выбранные компоненты@" + MyTitle, -1, "ReplaceComponents", "ReplaceComponentsEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "@" + MyTitle, -1, "ShowOptions", "", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Копировать чертежи@" + MyTitle, -1, "CopyDrawings",
                                         "CopyDrawingsEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Открыть все чертежи@" + MyTitle, -1, "OpenDrawings",
                                         "OpenDrawingsEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Окончательная обработка заказа@" + MyTitle, -1,
                                         "StartFinalProcessing", "FinalProcessingEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "@" + MyTitle, -1, "ShowOptions", "", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Образмерить все чертежи@" + MyTitle, -1, "DimensionAll",
                     "DimensionAllEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "@" + MyTitle, -1, "ShowOptions", "", "");
                }

                if (docType == (int)swDocumentTypes_e.swDocDRAWING)
                {
                    //_iSwApp.AddMenuItem2(docType, _addinId, "Автомасштабирование чертежа@" + MyTitle, -1, "AutoScaleDrawing", "AutoScaleDrawingEnable", "");
                    //_iSwApp.AddMenuItem2(docType, _addinId, "Xml@" + MyTitle, -1, "Xml", "XmlEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Нанести условные обозначения отверстий@" + MyTitle, -1,
                                         "AddHolesSymbols", "AddHolesSymbolsEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "@" + MyTitle, -1, "ShowOptions", "", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Образмеривание чертежа@" + MyTitle, -1,
                                         "AutoDimensionDrawing", "AutoDimensionDrawingEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Удалить размеры и блоки@" + MyTitle, -1,
                                         "DeleteSketchDemensions", "DeleteSketchDemensionsEnable", "");

                }
                if (docType == (int)swDocumentTypes_e.swDocNONE)
                {
                    _iSwApp.AddMenuItem2(docType, _addinId, "Автовосстановление@" + MyTitle, -1, "AutoRecovery", "AutoRecoveryEnable", "");
                    _iSwApp.AddMenuItem2(docType, _addinId, "Создать кэш. Долгая операция.@" + MyTitle, -1, "CreateCash", "CreateCashEnable", "");
                    //_iSwApp.AddMenuItem2(docType, _addinId, "Утилитка для проверки создания кэша.@" + MyTitle, -1,"CheckAccessories", "CheckAccessoriesEnable", "");
                }
                if (docType == (int)swDocumentTypes_e.swDocPART && Properties.Settings.Default.KitchenModeOn)
                {
                    _iSwApp.AddMenuItem2(docType, _addinId, "Сгенерировать помещение@" + MyTitle, -1, "GeneratePlane", "GeneratePlaneEnable", "");
                }
                _iSwApp.AddMenuItem2(docType, _addinId, "Настройки@" + MyTitle, -1, "ShowOptions", "", "");
                //if (Properties.Settings.Default.CheckUpdateLib)
                //{
                //    _iSwApp.AddMenuItem2(docType, _addinId, "@" + MyTitle, -1, "ShowOptions", "", "");
                //    _iSwApp.AddMenuItem2(docType, _addinId, "Обновление библиотеки@" + MyTitle, -1, "UpdateLib",
                //                                "", "");
                //}
            }
        }
        public void AutoRecovery()
        {

            string recoveryFolder = SwApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swAutoSaveDirectory);
            var files = Directory.GetFiles(recoveryFolder, "*.*", SearchOption.TopDirectoryOnly);//(@"C:\Users\kraev\AppData\Local\TempSW Резервный каталог\swxauto", "*.sldasm", SearchOption.TopDirectoryOnly);
            string copyToDirectory = null;
            string fileToOpen = null;
            SwDMApplication swDocMgr = GetSwDmApp();
            SwDmDocumentOpenError oe;
            if (files.Length > 0)
            {
                string pattern = @"\d\d\d\d\d\d-\d\d\d\d-\d\d\d\d";
                Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                foreach (string f in files)
                {
                    string fileName = Path.GetFileName(f);
                    if (rgx.Matches(fileName).Count > 0 && !fileName.Contains("#"))
                    {
                        var swDoc = swDocMgr.GetDocument(f, SwDmDocumentType.swDmDocumentUnknown, true,
                                                             out oe);
                        string[] names = swDoc.GetCustomPropertyNames();
                        SwDmCustomInfoType cit = SwDmCustomInfoType.swDmCustomInfoText;

                        string asmUnit = swDoc.GetCustomProperty("AsmUnit", out cit);
                        if (string.IsNullOrEmpty(asmUnit))
                            throw new Exception("Не удалось получить свойство AsmUnit из файла " + f);
                        copyToDirectory = Path.GetDirectoryName(asmUnit);
                        fileToOpen = f;
                    }

                }
            }
            if (copyToDirectory == null)
            {
                MessageBox.Show("Не удалось получить папку назначения для автовосстановления. ",
                                "Ошибка автовосстановления!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (MessageBox.Show("Вы действительно хотите произвести автовосстановление в папку: " + copyToDirectory, MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;
            //открыть этот файл...
            _iSwApp.CloseAllDocuments(true);
            int warnings = 0;
            int errors = 0;
            string newFileToOpen = null;
            SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
            var swDoc2 = swDocMgr.GetDocument(fileToOpen, SwDmDocumentType.swDmDocumentAssembly, true, out oe);
            string[] allTopRef = swDoc2.GetAllExternalReferences(src);
            foreach (string f in files)
            {
                if (f.Length < 26)
                    continue;
                string fileTofind = Path.GetFileName(f).Substring(25, Path.GetFileName(f).Length - 25);
                string[] foundFiles = Directory.GetFiles(copyToDirectory, fileTofind, SearchOption.AllDirectories);
                if (foundFiles.Length > 1)
                    continue;
                if (foundFiles.Length == 0)
                {
                    //вот тут будет сложная логика. Надо понять куда эту фигню положить..
                    string currFName = Path.GetFileName(f);
                    bool isInRef = false;
                    foreach (var refer in allTopRef)
                    {
                        string pathCopyTo;
                        if (refer.Contains(currFName))
                        {
                            //есть в общих референсах, значит копировать в \Детали
                            pathCopyTo = Path.Combine(copyToDirectory, "Детали");
                            if (!Directory.Exists(pathCopyTo))
                                Directory.CreateDirectory(pathCopyTo);
                            File.Copy(f, Path.Combine(pathCopyTo, currFName));
                            isInRef = true;
                            break;
                        }
                    }
                    if (isInRef)
                        continue;
                    //взять Accessories и Auxiliary
                    var swDoc = swDocMgr.GetDocument(f, SwDmDocumentType.swDmDocumentUnknown, true, out oe);
                    string[] names = swDoc.GetCustomPropertyNames();
                    SwDmCustomInfoType cit = SwDmCustomInfoType.swDmCustomInfoText;
                    string auxiliary = swDoc.GetCustomProperty("Auxiliary", out cit);
                    string accessories = swDoc.GetCustomProperty("Accessories", out cit);
                    if (string.IsNullOrEmpty(auxiliary))
                        auxiliary = "No";
                    if (string.IsNullOrEmpty(accessories))
                        accessories = "No";
                    bool aux = (auxiliary == "Yes");
                    bool accessor = (accessories == "Yes");
                    if (!aux && accessor)
                    {
                        string pathCopyTo = Path.Combine(copyToDirectory, "Фурнитура");
                        if (!Directory.Exists(pathCopyTo))
                            Directory.CreateDirectory(pathCopyTo);
                        File.Copy(f, Path.Combine(pathCopyTo, currFName));
                        continue;
                    }
                    if (aux && accessor)
                    {
                        string pathCopyTo = Path.Combine(copyToDirectory, "Фурнитура");
                        pathCopyTo = Path.Combine(pathCopyTo, "Модели фурнитуры");
                        if (!Directory.Exists(pathCopyTo))
                            Directory.CreateDirectory(pathCopyTo);
                        File.Copy(f, Path.Combine(pathCopyTo, currFName));
                        continue;
                    }
                    if (!aux && !accessor)
                    {
                        string pathCopyTo = Path.Combine(copyToDirectory, "Детали");
                        pathCopyTo = Path.Combine(pathCopyTo, "Детали для сборок");
                        if (!Directory.Exists(pathCopyTo))
                            Directory.CreateDirectory(pathCopyTo);
                        File.Copy(f, Path.Combine(pathCopyTo, currFName));
                        continue;
                    }
                    if (aux && !accessor)
                    {
                        string pathCopyTo = Path.Combine(copyToDirectory, "Детали");
                        pathCopyTo = Path.Combine(pathCopyTo, "Вспомогательные детали");
                        if (!Directory.Exists(pathCopyTo))
                            Directory.CreateDirectory(pathCopyTo);
                        File.Copy(f, Path.Combine(pathCopyTo, currFName));
                        continue;
                    }
                }

                //File.Delete(foundFiles[0]);
                //File.Move(f, foundFiles[0]);
                File.Copy(f, foundFiles[0], true);

                if (f == fileToOpen)
                    newFileToOpen = foundFiles[0];
            }
            if (newFileToOpen == null)
                throw new Exception("Не удалось найти файл сборки для открытия после успешного автовосстановления.");
            DetachEventHandlers();
            var swModelDoc = (ModelDoc2)_iSwApp.OpenDoc6(newFileToOpen, (int)swDocumentTypes_e.swDocASSEMBLY,
                                                    (int)swOpenDocOptions_e.swOpenDocOptions_LoadModel, "", errors,
                                                    warnings);
            AttachEventHandlers();


        }
        public int AutoRecoveryEnable()
        {
            string recoveryFolder = SwApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swAutoSaveDirectory);
            var files = Directory.GetFiles(recoveryFolder, "*.sldasm", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
                return 1;
            else
                return 0;
        }
        public int CopyToLocal()
        {
            try
            {
                if (RootModel == null)
                    RootModel = SwModel;

                var startTime = DateTime.Now;
                frmCopyProject.RecopyHeare(this, _iSwApp, RootModel);
                TimeSpan time = DateTime.Now - startTime;
                MessageBox.Show(
                         @"Отрыв завершен за " + time.Minutes + @" минут " + time.Seconds + @" секунд",
                         MyTitle, MessageBoxButtons.OK,
                         MessageBoxIcon.Information);

                bool goFinalProcessing = (MessageBox.Show(
                    @"Для корректного завершения операции копирования проекта в другой заказ, необходимо произвести операцию ""Окончательная обработка"". Произвести ее сейчас?",
                    "Произвести окончательную обработку сейчас?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                                      DialogResult.Yes);
                if (goFinalProcessing)
                {
                    FinalProcessing(false);
                }

                //ChangeCashMode();
            }
            catch (Exception e)
            {
                Logging.Log.Instance.Fatal(e, "Ошибка при отрыве из кэша.");
            }
            return 0;
        }
        public int CopyToLocalEnable()
        {
            if (Properties.Settings.Default.CashModeOn)
                return 1;
            else
                return 0;
        }
        private void RemoveSetParametersForm()
        {
            if (_frmPrm != null)
                _frmPrm.Close();
        }

        private void RemoveMenu()
        {
            bool bRet = _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocPART, MyTitle, "");
            bRet = _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocASSEMBLY, MyTitle, "");
            bRet = _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocDRAWING, MyTitle, "");
            bRet = _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocNONE, MyTitle, "");
        }

        public void RemovePopupMenu()
        {
            if (SwModel != null)
            {
                _iSwApp.RemoveMenuPopupItem2(SwModel.GetType(), _addinId,
                                             (int)swSelectType_e.swSelEVERYTHING, MyTitle + " - MrDoors РПД",
                                             "ShowSetParameters", "ShowSetParametersEnable", "", "");
                _iSwApp.RemoveMenuPopupItem2(SwModel.GetType(), _addinId,
                                             (int)swSelectType_e.swSelEVERYTHING,
                                             MyTitle + " - Заменить выбранные компоненты", "ReplaceComponents", "ReplaceComponentsEnable", "",
                                             "");
                _iSwApp.RemoveMenuPopupItem2(SwModel.GetType(), _addinId,
                           (int)swSelectType_e.swSelEVERYTHING, MyTitle + " - Открыть чертеж детали", "OpenDrawing", "OpenDrawingEnable", "", "");
                _iSwApp.RemoveMenuPopupItem2(SwModel.GetType(), _addinId,
                           (int)swSelectType_e.swSelEVERYTHING, MyTitle + " - Пересоздать отверстия для детали(не пакетно)", "CutOffDetail", "SaveAsComponentsEnable", "", "");
                _iSwApp.RemoveMenuPopupItem2((int)SwModel.GetType(), _addinId,
                                             (int)swSelectType_e.swSelEVERYTHING, MyTitle + " - Отделка кромки",
                                             "EdgeProcessing", "EdgeProcessingEnable", "", "");
                _iSwApp.RemoveMenuPopupItem2((int)SwModel.GetType(), _addinId,
                                             (int)swSelectType_e.swSelEVERYTHING, MyTitle + " - Удалить кромку",
                                             "DeleteEdge", "DeleteEdgeEnable", "", "");
            }
        }

        public Boolean AddPmp()
        {
            _ppage = new UserPmPage(this);
            return true;
        }

        public Boolean RemovePmp()
        {
            _ppage = null;
            return true;
        }

        #endregion

        #region UI Callbacks

        public void CreateCube()
        {
            string partTemplate =
                _iSwApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplatePart);
            var modDoc =
                (IModelDoc2)_iSwApp.NewDocument(partTemplate, (int)swDwgPaperSizes_e.swDwgPaperA2size, 0.0, 0.0);

            modDoc.InsertSketch2(true);
            modDoc.SketchRectangle(0, 0, 0, .1, .1, .1, false);

            IFeatureManager featMan = modDoc.FeatureManager;
            featMan.FeatureExtrusion(true,
                                     false, false,
                                     (int)swEndConditions_e.swEndCondBlind, (int)swEndConditions_e.swEndCondBlind,
                                     0.1, 0.0,
                                     false, false,
                                     false, false,
                                     0.0, 0.0,
                                     false, false,
                                     false, false,
                                     true,
                                     false, false);
        }

        public void ShowPmp()
        {
            if (_ppage != null)
                _ppage.Show();
        }

        public int EnablePmp()
        {
            if (_iSwApp.ActiveDoc != null)
                return 1;
            return 0;
        }

        #endregion

        #region Event Methods

        public bool AttachEventHandlers()
        {
            AttachSwEvents();
            var modDoc = (ModelDoc2)_iSwApp.GetFirstDocument();

            while (modDoc != null)
            {
                if (!_openDocs.Contains(modDoc))
                {
                    AttachModelDocEventHandler(modDoc);
                }
                modDoc = (ModelDoc2)modDoc.GetNext();
            }
            return true;
        }

        private void AttachSwEvents()
        {
            try
            {
                _swEventPtr.ActiveDocChangeNotify += OnDocChange;
                _swEventPtr.DocumentLoadNotify2 += OnDocLoad;
                _swEventPtr.DestroyNotify += CloseDocument;
                //_swEventPtr.FileNewNotify2 += OnFileNew;
                //_swEventPtr.ActiveModelDocChangeNotify += OnModelChange;
                //_swEventPtr.ActiveDocChangeNotify += OnModelChange;
                _swEventPtr.CommandCloseNotify += OnCommandCloseNotify;
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

        private void DetachSwEvents()
        {
            try
            {
                _swEventPtr.ActiveDocChangeNotify -= OnDocChange;
                _swEventPtr.DocumentLoadNotify2 -= OnDocLoad;
                //_swEventPtr.FileNewNotify2 -= OnFileNew;
                //_swEventPtr.ActiveModelDocChangeNotify -= OnModelChange;
                //_swEventPtr.ActiveDocChangeNotify -= OnModelChange;
                _swEventPtr.CommandCloseNotify -= OnCommandCloseNotify;
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

        public bool AttachModelDocEventHandler(ModelDoc2 modDoc)
        {
            if (modDoc == null)
                return false;

            DocumentEventHandler docHandler;

            if (!_openDocs.Contains(modDoc))
            {
                switch (modDoc.GetType())
                {
                    case (int)swDocumentTypes_e.swDocPART:
                        {
                            docHandler = new PartEventHandler(modDoc, this);
                            break;
                        }
                    case (int)swDocumentTypes_e.swDocASSEMBLY:
                        {
                            docHandler = new AssemblyEventHandler(modDoc, this);
                            break;
                        }
                    case (int)swDocumentTypes_e.swDocDRAWING:
                        {
                            docHandler = new DrawingEventHandler(modDoc, this);
                            break;
                        }
                    default:
                        {
                            return false;
                        }
                }
                docHandler.AttachEventHandlers();
                _openDocs.Add(modDoc, docHandler);
            }
            return true;
        }

        public bool DetachModelEventHandler(ModelDoc2 modDoc)
        {
            if (modDoc.Visible)
                ChangeActiveDoc(true);
            _openDocs.Remove(modDoc);
            return true;
        }

        public bool DetachEventHandlers()
        {
            DetachSwEvents();

            try
            {
                DocumentEventHandler docHandler;
                int numKeys = _openDocs.Count;
                var keys = new Object[numKeys];
                _openDocs.Keys.CopyTo(keys, 0);
                foreach (ModelDoc2 key in keys)
                {
                    docHandler = (DocumentEventHandler)_openDocs[key];
                    docHandler.DetachEventHandlers();
                    docHandler = null;
                }
            }
            catch
            {
            }
            GC.Collect();
            return true;
        }

        #endregion

        #region Event Handlers

        public int CloseDocument()
        {
            //тут проверка на оторванность
            if (!CheckForTearOff2())
                MessageBox.Show(@"Внимание! Нельзя вести одновременно 2 и более проектов в режиме КЭШ! Выход из проекта в этом режиме может привести к появлению ошибок(а именно: если зайти во 2-ой проект и сделать там ""Оторвать все"" то ссылки на детали в 1-ом проекте удалятся!!!). Необходимо снова войти впроект и выполнить ""Оторвать все""", "Внимание!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return -1;
        }

        public int OnDocChange()
        {
            ModeChanged(null, null);

            Cash.CashModeChanged -= new EventHandler(ModeChanged);
            Cash.CashModeChanged += new EventHandler(ModeChanged);

            KitchenModule.KitchenModeChanged -= new EventHandler(ModeChanged);
            KitchenModule.KitchenModeChanged += new EventHandler(ModeChanged);


            ChangeActiveDoc(false);
            string newXname = GetXNameForAssembly(false, Path.GetFileNameWithoutExtension(SwModel.GetPathName()));
            currentPath = SwModel.GetPathName();
            if (newXname != "")
            {
                if (!Decors.DictionaryListMdb.ContainsKey(newXname))
                {
                    _wasDocChange = true;
                    if (_neededReloadFirstLayerComponent)
                        ReloadFirstLayerComponent();
                    string fileName;
                    var list = new List<ModelDoc2>();
                    if (ExistingFileDictionaryWithMdb(out fileName))
                    {
                        var strArr = File.ReadAllLines(fileName);
                        LinkedList<ModelDoc2> modList;
                        string rootDirPath = GetRootFolder(SwModel);
                        if (GetAllUniqueModels(SwModel, out modList))
                        {
                            foreach (var s in strArr)
                            {
                                if (!s.Contains("_SWLIB_BACKUP"))
                                    list.AddRange(modList.Where(modelDoc2 => modelDoc2.GetPathName() == rootDirPath + s));
                                else
                                    list.AddRange(modList.Where(modelDoc2 => modelDoc2.GetPathName() == s));
                            }
                        }
                    }
                    else
                    {
                        list = Decors.GetAllModelsWithMdb(this, SwModel);
                        CreateRelativePathAndWriteToFile(fileName, list.Select(x => x.GetPathName()).ToArray());
                    }

                    Decors.DictionaryListMdb.Add(newXname, list);
                    Decors.MemoryForDecors.Clear();
                }

                _isCheckedSaving = true;
                RootModel = SwModel;
            }
            else
                _isCheckedSaving = false;
            return 0;
        }

        public int OnDocLoad(string docTitle, string docPath)
        {
            var modDoc = (ModelDoc2)_iSwApp.GetFirstDocument();

            if (IsEventsEnabled)
            {
                string[] spl = docPath.Split('\\');
                if (spl.Length > 2 && !docPath.ToUpper().Contains("ДЕТАЛИ") && !docPath.ToUpper().Contains("ФУРНИТУРА"))
                {
                    string[] externals = linksToCash.CheckForExternal();
                    if (externals.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(Environment.NewLine);
                        bool showMwssage = false;
                        foreach (string external in externals)
                        {
                            string[] spl2 = external.Split('\\');
                            if (spl2.Length < 2)
                                continue;
                            if (spl[1] != spl2[1] && spl[1].ToUpper() != "_SWLIB_" && spl[1].ToUpper() != "_SWLIB_BACKUP")
                            {
                                showMwssage = true;
                                sb.Append(external);
                                sb.Append(Environment.NewLine);
                            }
                            else
                            {
                                showMwssage = false;
                                break;
                            }
                        }
                        if (showMwssage)
                        {
                            if (Properties.Settings.Default.CashModeOn)
                            {
                                _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocASSEMBLY, "Переключить режим кэша. Сейчас режим кэша: ВКЛ@" + MyTitle, "ChangeCashMode");
                                _iSwApp.AddMenuItem2((int)swDocumentTypes_e.swDocASSEMBLY, _addinId, "Переключить режим кэша. Сейчас режим кэша: ВЫКЛ@" + MyTitle, 8, // позиция 8 необходимо менять.. или что-то делать с этим хардкодом
                                                                      "ChangeCashMode", "ChangeCashModeEnable", "");

                                string tmp = _iSwApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swFileLocationsDesignLibrary);
                                _iSwApp.SetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swFileLocationsDesignLibrary, tmp.ToLower().Replace("_swlib_backup", "_swlib_").ToUpper());
                                _iSwApp.RefreshTaskpaneContent();
                                Properties.Settings.Default.CashModeOn = false;
                            }
                            StringBuilder sb2 = new StringBuilder();
                            sb2.Append("В следующих сборках есть неоторванные детали которые ссылаются на _SWLIB_BACKUP: ");
                            sb2.Append(sb.ToString());
                            sb2.Append(Environment.NewLine);
                            sb2.Append(@"Сделайте в этих заказах опцию ""Оторвать все""");
                            sb2.Append(Environment.NewLine);
                            sb2.Append("Иначе вы не сможете зайти в режим кэша.");
                            sb2.Append(Environment.NewLine);
                            sb2.Append("Продолжить?");
                            MessageBox.Show(sb2.ToString(), @"MrDoors");
                        }

                    }
                }



                if (modDoc != null)
                {
                    if (modDoc.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
                    {
                        try //  getting 'System.NullReferenceException' here
                        {
                            ((AssemblyDoc)(modDoc)).DestroyNotify += CloseDocument;
                        }
                        catch (Exception e)
                        {
                            // Let the programmer know what went wrong.
                            string msg = "PartEventHandler:AttachEventHandlers() DestroyNotify exception:" + e.Message;
                            System.Diagnostics.Debug.WriteLine(msg);
                        }
                    }
                    string name = GetXNameForAssembly(false, Path.GetFileNameWithoutExtension(modDoc.GetPathName()));
                    if (name != "")
                    {
                        if (Decors.DictionaryListMdb.ContainsKey(name))
                            Decors.DictionaryListMdb.Remove(name);
                    }
                    if (_iSwApp.IActiveDoc2 != null && (
                                                           Path.GetFileNameWithoutExtension(
                                                               _iSwApp.IActiveDoc2.GetPathName()) == docTitle ||
                                                           Path.GetFileName(_iSwApp.IActiveDoc2.GetPathName()) ==
                                                           docTitle))
                        _neededReloadFirstLayerComponent = true;
                }
            }

            while (modDoc != null)
            {
                if (modDoc.GetTitle() == docTitle)
                {
                    if (!_openDocs.Contains(modDoc))
                    {
                        AttachModelDocEventHandler(modDoc);
                    }
                }
                modDoc = (ModelDoc2)modDoc.GetNext();
            }

            if (_wasDocChange && _neededReloadFirstLayerComponent)
                ReloadFirstLayerComponent();
            return 0;
        }

        public int OnFileNew(object newDoc, int docType, string templateName)
        {
            return 0;
        }

        private TaskpaneView indicator;
        private ListBox infoList = new ListBox();
        private object lockObject = new object();
        public void ModeChanged(object sender, EventArgs e)
        {
            if (IsEventsEnabled)
            {
                IsEventsEnabled = false;
                if (Properties.Settings.Default.CashModeAvailable && Properties.Settings.Default.KitchenModeAvailable)
                {
                    if (indicator != null)
                    {
                        indicator.DeleteView();
                        indicator = null;
                    }

                    Bitmap bitmap;
                    string bitmapPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\SWDocuments"; //путь к Документам
                    if (!Directory.Exists(bitmapPath + "\\"))
                        Directory.CreateDirectory(bitmapPath);

                    bitmapPath += "\\indicator.ind";

                    if (Properties.Settings.Default.CashModeOn && Properties.Settings.Default.KitchenModeOn)
                        bitmap = Furniture.Properties.Resources.GreenIndicator;
                    else
                        bitmap = Furniture.Properties.Resources.RedIndicator;

                    bitmap.Save(Path.GetFullPath(bitmapPath));

                    infoList.Items.Clear();
                    string status = Properties.Settings.Default.CashModeOn ? "вкл" : "выкл";
                    infoList.Items.Add("Режим КЭШ: " + status);
                    status = Properties.Settings.Default.KitchenModeOn ? "вкл" : "выкл";
                    infoList.Items.Add("Режим кухонь: " + status);

                    if (File.Exists(bitmapPath))
                    {
                        indicator = _iSwApp.CreateTaskpaneView2(bitmapPath, "Состояние");
                        indicator.DisplayWindowFromHandle(infoList.Handle.ToInt32());
                    }

                    SwApp.ActivateTaskPane((int)swTaskPaneTab_e.swDesignLibrary);
                }
                IsEventsEnabled = true;
            }
        }

        public void UpdateIndicator()
        {
            if (IsEventsEnabled)
            {
                IsEventsEnabled = false;
                if (!Properties.Settings.Default.CashModeAvailable)
                    if (Properties.Settings.Default.CashModeOn)
                        ChangeCashMode();

                if (!Properties.Settings.Default.KitchenModeAvailable)
                    if (Properties.Settings.Default.KitchenModeOn)
                        ChangeKitchenMode();

                if (!(Properties.Settings.Default.CashModeAvailable && Properties.Settings.Default.KitchenModeAvailable))
                {
                    if (indicator != null)
                    {
                        indicator.DeleteView();
                        indicator = null;
                    }
                }
                else
                {
                    IsEventsEnabled = true;
                    ModeChanged(null, null);
                    IsEventsEnabled = false;
                }

                IsEventsEnabled = true;
            }
        }

        public int OnModelChange()
        {
            return 0;
        }

        private LinkedList<string> _addingComponentNames = new LinkedList<string>();


        public int GeneratePlane()
        {
            IsEventsEnabled = false;
            try
            {

                string createdRoomPath = Kitchen.CreateAndSaveRoom(this, Path.GetDirectoryName(RootModel.GetPathName()));
                if (!string.IsNullOrEmpty(createdRoomPath))
                    Kitchen.InsertRoom(createdRoomPath, RootModel, this);
            }
            catch { }
            IsEventsEnabled = true;
            return 0;
        }
        public int CreateMetering()
        {
            IsEventsEnabled = false;
            try
            {
                RootModel = _iSwApp.ActiveDoc;
                Kitchen.CreateFloorSketch(this);
            }
            catch { }
            IsEventsEnabled = true;
            return 0;
        }

        public int GeneratePlaneEnable()
        {
            return Properties.Settings.Default.KitchenModeOn ? 1 : 0;
        }
        public int CreateMeteringEnable()
        {
            return Properties.Settings.Default.KitchenModeOn ? 1 : 0;
        }

        public int ChangeKitchenMode()
        {
            if (!Properties.Settings.Default.KitchenModeOn)
            {
                _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocASSEMBLY,
                                   "Переключить режим кухни. Сейчас режим кухни: ВЫКЛ@" + MyTitle, "ChangeKitchenMode");
                _iSwApp.AddMenuItem2((int)swDocumentTypes_e.swDocASSEMBLY, _addinId, "Переключить режим кухни. Сейчас режим кухни: ВКЛ@" + MyTitle, 7, // позиция 8 необходимо менять.. или что-то делать с этим хардкодом
                                                      "ChangeKitchenMode", "ChangeKitchenModeEnable", "");
            }
            else
            {

                _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocASSEMBLY,
                                    "Переключить режим кухни. Сейчас режим кухни: ВКЛ@" + MyTitle, "ChangeKitchenMode");
                _iSwApp.AddMenuItem2((int)swDocumentTypes_e.swDocASSEMBLY, _addinId, "Переключить режим кухни. Сейчас режим кухни: ВЫКЛ@" + MyTitle, 7, // позиция 8 необходимо менять.. или что-то делать с этим хардкодом
                                                      "ChangeKitchenMode", "ChangeKitchenModeEnable", "");
            }
            Properties.Settings.Default.KitchenModeOn = !Properties.Settings.Default.KitchenModeOn;
            Properties.Settings.Default.Save();
            Component2 mainComponent;
            GetSymilarComponentByName(SwModel, "Замер", out mainComponent);
            if (mainComponent == null)
                return 1;
            return 1;
        }

        public int ChangeCashMode()
        {
            if (!Properties.Settings.Default.CashModeOn)
            {
                if (currentPath == null)
                    return 0;
                string[] spl = currentPath.Split('\\');
                if (spl.Length > 2 && !currentPath.ToUpper().Contains("ДЕТАЛИ") && !currentPath.ToUpper().Contains("ФУРНИТУРА"))
                {

                    string[] externals = linksToCash.CheckForExternal();
                    if (externals.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(Environment.NewLine);
                        bool showMwssage = false;
                        foreach (string external in externals)
                        {
                            string[] spl2 = external.Split('\\');
                            if (spl2.Length < 2)
                                continue;
                            if (spl[1] != spl2[1] && spl[1].ToUpper() != "_SWLIB_" && spl[1].ToUpper() != "_SWLIB_BACKUP")
                            {
                                showMwssage = true;
                                sb.Append(external);
                                sb.Append(Environment.NewLine);
                            }
                            else
                            {
                                showMwssage = false;
                                break;
                            }
                        }
                        if (showMwssage)
                        {
                            StringBuilder sb2 = new StringBuilder();
                            sb2.Append(
                                "В следующих сборках есть неоторванные детали которые ссылаются на _SWLIB_BACKUP: ");
                            sb2.Append(sb.ToString());
                            sb2.Append(Environment.NewLine);
                            sb2.Append(@"Сделайте в этих заказах опцию ""Оторвать все""");
                            sb2.Append(Environment.NewLine);
                            sb2.Append("Иначе вы не сможете зайти в режим кэша.");
                            sb2.Append(Environment.NewLine);
                            sb2.Append("Продолжить?");
                            MessageBox.Show(sb2.ToString(), @"MrDoors");
                            return 0;
                        }

                    }
                }

                _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocASSEMBLY,
                                   "Переключить режим кэша. Сейчас режим кэша: ВЫКЛ@" + MyTitle, "ChangeCashMode");
                _iSwApp.AddMenuItem2((int)swDocumentTypes_e.swDocASSEMBLY, _addinId, "Переключить режим кэша. Сейчас режим кэша: ВКЛ@" + MyTitle, 8, // позиция 8 необходимо менять.. или что-то делать с этим хардкодом
                                                      "ChangeCashMode", "ChangeCashModeEnable", "");
                string tmp = _iSwApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swFileLocationsDesignLibrary);
                tmp = tmp.ToLower().Replace("_swlib_backup", "_swlib_").ToUpper();
                _iSwApp.SetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swFileLocationsDesignLibrary, tmp.ToLower().Replace("_swlib_", "_swlib_backup").ToUpper());
                _iSwApp.RefreshTaskpaneContent();
            }
            else
            {
                _iSwApp.RemoveMenu((int)swDocumentTypes_e.swDocASSEMBLY,
                                   "Переключить режим кэша. Сейчас режим кэша: ВКЛ@" + MyTitle, "ChangeCashMode");
                _iSwApp.AddMenuItem2((int)swDocumentTypes_e.swDocASSEMBLY, _addinId, "Переключить режим кэша. Сейчас режим кэша: ВЫКЛ@" + MyTitle, 8, // позиция 8 необходимо менять.. или что-то делать с этим хардкодом
                                                      "ChangeCashMode", "ChangeCashModeEnable", "");

                string tmp = _iSwApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swFileLocationsDesignLibrary);
                _iSwApp.SetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swFileLocationsDesignLibrary, tmp.ToLower().Replace("_swlib_backup", "_swlib_").ToUpper());
                _iSwApp.RefreshTaskpaneContent();
            }
            Properties.Settings.Default.CashModeOn = !Properties.Settings.Default.CashModeOn;
            Properties.Settings.Default.Save();
            return 0;
        }
        public int ChangeCashModeEnable()
        {
            return Properties.Settings.Default.CashModeAvailable ? 1 : 0;
        }
        public int ChangeKitchenModeEnable()
        {
            return Properties.Settings.Default.KitchenModeAvailable ? 1 : 0;
        }
        public int CreateCash()
        {

            Cash.Create();
            return 0;
        }
        public int CreateCashEnable()
        {
            return Properties.Settings.Default.CashModeAvailable ? 1 : 0;
        }
        public int CheckAccessories()
        {
            Cash.CheckAccessories();
            return 0;
        }
        public int CheckAccessoriesEnable()
        {
            return 1;
        }

        private static string GetOldPath(string newPath)
        {
            string retStr = newPath.Replace("_SWLIB_BACKUP", "_SWLIB_");
            string oldPathOnly = Path.GetDirectoryName(retStr);
            string pathWithoutExt = Path.GetFileNameWithoutExtension(retStr);
            retStr = pathWithoutExt.Substring(0, pathWithoutExt.Length - 4);
            retStr = string.Format("{0}{1}", retStr, Path.GetExtension(newPath));//Path.ChangeExtension(retStr, Path.GetExtension(newPath));
            return Path.Combine(oldPathOnly, retStr);
        }
        private void RefreshTaskpaneContent(object o)
        {
            Thread.Sleep(1000);
            lock (workerLocker)
            {
                _iSwApp.RefreshTaskpaneContent();
            }
            if (Properties.Settings.Default.ImmediatelyDetachModel)
            {
                SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
            }
        }
        private void CopyToCash(object o)
        {
            Thread.Sleep(2000);

            try
            {
                if (!(o is KeyValuePair<string, string>))
                    return;
                var oo = ((KeyValuePair<string, string>)(o));
                lock (workerLocker)
                {
                    Cash.CopyToCash(oo.Key, oo.Value);
                }
                _iSwApp.RefreshTaskpaneContent();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Logging.Log.Instance.Fatal(e, "Ошибка в CopyToCash");
            }
        }
        public static bool IfNextExist(string filePath, out string newFilePath)
        {
            newFilePath = string.Empty;
            bool result = false;
            string oldNumber =
                    Path.GetFileNameWithoutExtension(filePath).Substring(
                        Path.GetFileNameWithoutExtension(filePath).Length - 3, 2);
            int newNumber = 0;
            string newNumberString = string.Empty;
            if (int.TryParse(oldNumber, out newNumber))
                newNumber += 1;
            if (newNumber == 0)
                return false;
            if (newNumber >= 99)
                newNumberString = "01";
            if (newNumber >= 10 && newNumber < 99)
                newNumberString = newNumber.ToString();
            else
                newNumberString = "0" + newNumber.ToString();

            newFilePath = Path.Combine(Path.GetDirectoryName(filePath),
                                                Path.GetFileNameWithoutExtension(filePath).Substring(0,
                                                                                                    Path.
                                                                                                        GetFileNameWithoutExtension
                                                                                                        (filePath)
                                                                                                        .Length -
                                                                                                    3) +
                                                newNumberString + "P" + ".SLDASM");

            if (File.Exists(newFilePath))
                result = true;
            return result;
        }
        private string GetNextUnExistNumber(string[] varRef, string filePath)
        {
            bool isExist = true;
            string newNumberString;
            string currentFile = GetNextNumberCash(filePath, out newNumberString);
            while (isExist)
            {
                isExist = false;
                foreach (string o in varRef)
                {
                    if (o.ToUpper() == currentFile.ToUpper())
                    {
                        isExist = true;
                        break;
                    }
                }
                if (isExist)
                {
                    currentFile = GetNextNumberCash(currentFile, out newNumberString);
                    break;
                }
            }
            return currentFile;
        }
        private bool CheckForTearOff2()
        {
            bool isTearOff = true;
            if (string.IsNullOrEmpty(currentPath))
                return isTearOff;
            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
            SwDmDocumentOpenError oe;
            SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
            object brokenRefVar;

            var swDoc = (SwDMDocument8)swDocMgr.GetDocument(currentPath,
                                                     SwDmDocumentType.swDmDocumentAssembly,
                                                     true, out oe);
            if (swDoc != null)
            {
                var varRef = (string[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                if (varRef != null && varRef.Length != 0)
                {
                    foreach (string o in varRef)
                    {
                        if (o.ToUpper().Contains("_SWLIB_BACKUP"))
                        {
                            isTearOff = false;
                            currentPath = string.Empty;
                            break;
                        }
                    }
                }
                else
                {
                    swDoc.CloseDoc();
                }
                swDoc.CloseDoc();
            }
            return isTearOff;
        }
        private bool CheckForTearOff()
        {
            bool isTearOff = true;
            var comps = (object[])((AssemblyDoc)SwModel).GetComponents(false);
            if (comps != null)
            {
                foreach (var ocomp in comps)
                {
                    var comp2 = (Component2)ocomp;
                    var mmodel = comp2.GetModelDoc2() as ModelDoc2;
                    if (mmodel != null)
                    {
                        if (mmodel.GetPathName().ToUpper().Contains("_SWLIB_BACKUP"))
                        {
                            isTearOff = false;

                            break;
                        }
                    }
                }
            }
            return isTearOff;


        }
        private bool CheckIfAlreadyExist(string filePath, out string nextFile)
        {
            bool isAlreadyExist = false;
            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
            SwDmDocumentOpenError oe;
            SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
            object brokenRefVar;
            nextFile = string.Empty;
            var swDoc = (SwDMDocument8)swDocMgr.GetDocument(SwModel.GetPathName(),
                                                     SwDmDocumentType.swDmDocumentAssembly,
                                                     true, out oe);
            if (swDoc != null)
            {
                var varRef = (string[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                if (varRef != null && varRef.Length != 0)
                {
                    foreach (string o in varRef)
                    {
                        if (o.ToUpper() == filePath.ToUpper())
                        {
                            isAlreadyExist = true;
                            nextFile = GetNextUnExistNumber(varRef, filePath);
                            break;
                        }
                    }
                }
                else
                {
                    swDoc.CloseDoc();
                }
                swDoc.CloseDoc();
            }
            return isAlreadyExist;
        }
        private string GetNextNumberCash(string filePath, out string newNumberString)
        {
            string oldNumber =
                       Path.GetFileNameWithoutExtension(filePath).Substring(
                           Path.GetFileNameWithoutExtension(filePath).Length - 3, 2);
            int newNumber = 0;

            if (int.TryParse(oldNumber, out newNumber))
                newNumber += 1;
            else
            {
                //это может быть аксесуар
                SwDMApplication swDocMgr = GetSwDmApp();
                SwDmDocumentOpenError oe;
                var swDoc = swDocMgr.GetDocument(filePath, SwDmDocumentType.swDmDocumentAssembly, true, out oe);
                if (swDoc == null)
                {
                    Logging.Log.Instance.Debug("Не удалось сделать GetDocument в " + filePath);
                    newNumberString = "00";
                    return filePath;
                }
                SwDmCustomInfoType type;
                bool isAccessory = false;
                try
                {
                    string[] tmp = swDoc.GetCustomPropertyNames();
                    if (tmp != null && tmp.Contains("Accessories"))
                        isAccessory = swDoc.GetCustomProperty("Accessories", out type) == "Yes";
                    else
                        isAccessory = false;
                }
                catch (Exception)
                {
                    Logging.Log.Instance.Debug("Ошибка при попытке обратится к св-ву Accessories. Деталь: " + swDoc.FullName);
                    newNumberString = "00";
                    return filePath;
                }
                if (isAccessory)
                {
                    newNumberString = "00";
                    return filePath;
                }

            }
            if (newNumber == 0)
                Cash.CopyToCash(GetOldPath(filePath), "01");
            if (newNumber >= 100)
                newNumberString = "01";
            if (newNumber >= 10 && newNumber < 100)
                newNumberString = newNumber.ToString();
            else
                newNumberString = "0" + newNumber.ToString();

            string newFilePath = Path.Combine(Path.GetDirectoryName(filePath),
                                              Path.GetFileNameWithoutExtension(filePath).Substring(0,
                                                                                                   Path.
                                                                                                       GetFileNameWithoutExtension
                                                                                                       (filePath)
                                                                                                       .Length -
                                                                                                   3) +
                                              newNumberString + "P" + ".SLDASM");
            return newFilePath;
        }

        public void AddMate(ModelDoc2 swModel, Feature plane1, Feature plane2, bool flipped = false)
        {
            plane1.Select(false);
            plane2.Select(true);
            int longstatus;
            int align = flipped ? 0 : 1;
            var mate = ((AssemblyDoc)swModel).AddMate3(0, align, false, 8.060884073998, 0, 0, 0.001, 0.001, 0, 0.5235987755983, 0.5235987755983, false, out longstatus);
            //swModel.ClearSelection2(true);
            //swModel.EditRebuild3();
        }

        public int OnAddNewItem(int entityType, string itemName)
        {
            if (IsEventsEnabled)
            {
                IsEventsEnabled = false;
                try
                {
                    Component2 addedComponent;
                    if (!GetComponentByName(SwModel, itemName, false, out addedComponent))
                    {
                        MessageBox.Show("");
                        IsEventsEnabled = true;
                        return 0;
                    }
                    string addedComponentPath = addedComponent.GetPathName();
                    ModelDoc2 addedModel = addedComponent.GetModelDoc2();

                    if (Properties.Settings.Default.CashModeOn)
                    {
                        #region КЭШ вкл

                        bool saveAlready = !_isCheckedSaving;
                        if (!_isCheckedSaving)
                        {
                            _isCheckedSaving = CheckSaving(SwApp.IActiveDoc2.GetPathName());
                            RootModel = SwModel;
                        }
                        if (RootModel == null)
                            RootModel = SwModel;

                        bool isAlreadyExist = false;
                        string newFilePath = string.Empty;
                        string newNumberString = string.Empty;
                        Logging.Log.Instance.Debug("0");
                        if (entityType == (int)swNotifyEntityType_e.swNotifyComponent) //тип компонента
                        {
                            //Тут проверить нет ли в сборке уже такой детали                            
                            if (!saveAlready)
                                isAlreadyExist = CheckIfAlreadyExist(addedComponentPath, out newFilePath);
                            else
                                isAlreadyExist = false;

                            if (Path.GetFileNameWithoutExtension(addedComponentPath).Last() == 'P')
                                File.SetAttributes(addedComponentPath, FileAttributes.Hidden);

                            Decors.AddModelToList(this, addedComponent.GetModelDoc2());

                            ////проверить есть ли следующий
                            if (!isAlreadyExist)
                                newFilePath = GetNextNumberCash(addedComponentPath, out newNumberString);

                            if ((addedComponentPath.ToUpper().Contains("_SWLIB_") || addedComponentPath.ToUpper().Contains("_SWLIB_BACKUP")) && !isAlreadyExist)
                                _addingComponentNames.AddLast(itemName);

                            if (!File.Exists(newFilePath) && !isAlreadyExist)
                                ThreadPool.QueueUserWorkItem(CopyToCash, new KeyValuePair<string, string>(GetOldPath(addedComponent.GetPathName()), newNumberString));
                            else
                            {
                                if (!isAlreadyExist)
                                    File.SetAttributes(newFilePath, FileAttributes.Normal);
                            }

                            if (addedComponent.Select(false) && !isAlreadyExist)
                                ThreadPool.QueueUserWorkItem(CopyDrawings2Thread, null);

                            if (addedModel != null)
                            {
                                Decors.AddModelToList(this, addedModel);
                                string fileName;
                                if (ExistingFileDictionaryWithMdb(out fileName))
                                {
                                    var strArr = File.ReadAllLines(fileName).ToList();
                                    strArr.Add(addedComponent.GetPathName());
                                    File.SetAttributes(fileName, FileAttributes.Normal);
                                    File.WriteAllLines(fileName, strArr);
                                    File.SetAttributes(fileName, FileAttributes.Hidden);
                                }
                            }
                        }

                        if (_frmPrm != null)
                            _frmPrm.Close();
                        SwitchToThisWindow(_swHandle, true);

                        #endregion
                    }
                    else
                    {
                        #region КЭШ выкл

                        if (_frmPrm != null)
                            _frmPrm.Close();

                        if (!_isCheckedSaving)
                        {
                            _isCheckedSaving = CheckSaving(SwApp.IActiveDoc2.GetPathName());
                            RootModel = SwModel;
                        }

                        #region Проверка на актуальность имени модели

                        var nameFromModel = Path.GetFileNameWithoutExtension(SwModel.GetPathName());
                        var nameFromDirectory = GetOrderName(SwModel);
                        if (nameFromModel != nameFromDirectory)
                        {
                            SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                            addedComponent.Select(false);
                            SwModel.DeleteSelection(false);
                            MessageBox.Show(@"Разные имена папки заказа и файла!Измените имена!", MyTitle,
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                            IsEventsEnabled = true;
                            return 0;
                        }

                        string nameForAssembly = GetXNameForAssembly(false, Path.GetFileNameWithoutExtension(SwModel.GetPathName()));
                        if (!Decors.DictionaryListMdb.ContainsKey(nameForAssembly))
                        {
                            Decors.DictionaryListMdb.Add(nameForAssembly, new List<ModelDoc2>());
                            string fileName;
                            if (!ExistingFileDictionaryWithMdb(out fileName))
                            {
                                File.Create(fileName).Close();
                                File.SetAttributes(fileName, FileAttributes.Hidden);
                            }
                        }

                        LinkedList<Component2> outComponents = new LinkedList<Component2>();
                        try
                        {
                            if (GetComponents(SwModel.IGetActiveConfiguration().IGetRootComponent2(), outComponents, false, false))
                            {
                                Component2 comp = outComponents.First(x => x.Name.Contains("#"));
                                string name = comp.Name.Substring(comp.Name.IndexOf("#") + 1,
                                                                  comp.Name.Substring(comp.Name.IndexOf("#")).IndexOf(
                                                                      "-") -
                                                                  1);
                                UInt64 number = Convert.ToUInt64(name, 16);
                                name = number.ToString();
                                string orderNumber = "";
                                var orderNumberArrays = GetOrderName(SwModel).Split('-');
                                orderNumber = orderNumberArrays.Aggregate(orderNumber,
                                                                          (current, orderNumberArray) =>
                                                                          current + orderNumberArray);
                                int i = 0;
                                while (name != orderNumber && i < 255)
                                {
                                    name = "0" + name;
                                    i++;
                                }

                                bool isFailed = (i == 255);

                                if (isFailed)
                                {
                                    string nativeNumber = name;
                                    if (name.Length > 14)
                                        name = name.Substring(name.Length - 14);
                                    if (name.Length == 14)
                                        nativeNumber = name.Substring(0, 6) + "-" + name.Substring(6, 4) + "-" +
                                                       name.Substring(10);

                                    SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                                    addedComponent.Select(false);
                                    SwModel.DeleteSelection(false);
                                    var mmb = new MyMessageBox();
                                    mmb.Show(
                                            @"Ошибка! Т.к. имя основого файла/папки
                                            заказа было изменено,дальнейший
                                            отрыв деталей в сборку невозможен!
                                            Восстановите предыдущее имя файла папки!",
                                            "Корректное имя: ",
                                        nativeNumber, "OK", "MrDoors");
                                    IsEventsEnabled = true;
                                    return 0;
                                }
                            }
                        }
                        catch { }

                        #endregion

                        try
                        {
                            if (entityType == (int)swNotifyEntityType_e.swNotifyComponent) //тип компонента
                            {
                                if (!addedComponent.Name2.Contains("^")) //проверка на встроенный в сборку компонент
                                {
                                    var swCompModel = (ModelDoc2)addedComponent.GetModelDoc();
                                    if (swCompModel != null)
                                    {
                                        //только если модель из библиотеки или сборки
                                        if (swCompModel.GetPathName().Contains(Furniture.Helpers.LocalAccounts.modelPathResult))
                                        {
                                            Component2 swLibComp;
                                            if (GetParentLibraryComponent(addedComponent, out swLibComp))
                                                _addingComponentNames.AddLast(itemName);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logging.Log.Instance.Fatal(e.Message + "OnAddNewItem()");
                            MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }

                        #endregion
                    }

                    if (Properties.Settings.Default.KitchenModeOn)
                    {
                        #region Обработка режима кухни

                        CustomPropertyManager propertyManager = addedModel.Extension.get_CustomPropertyManager(string.Empty);
                        string kitchenTypeValue = string.Empty;
                        string resolvedKitchenTypeValue = string.Empty;
                        propertyManager.Get4("KitchenType", false, out kitchenTypeValue, out resolvedKitchenTypeValue);

                        if (!string.IsNullOrEmpty(kitchenTypeValue))
                        {
                            ModelDoc2 swModel = SwApp.IActiveDoc2;
                            swModel.ClearSelection2(true);
                            Component2 mainComponent = null;
                            var swConfig = (Configuration)swModel.GetActiveConfiguration();
                            Component2 swRootComponent;
                            if (swConfig != null)
                                swRootComponent = (Component2)swConfig.GetRootComponent();
                            else
                                return 1;
                            KitchenModule km = new KitchenModule(RootModel, swRootComponent, this, swModel);
                            var measure = km.measure;
                            string rootName = km.rootName;
                            GetSymilarComponentByName(SwModel, "Замер", out mainComponent);
                            if (mainComponent == null)
                                return 1;

                            bool isAnglePart = itemName.ToLower().Contains("угловая");
                            bool isUpPart = itemName.ToLower().Contains("верхняя");
                            bool isTabletop = false;
                            km.GetComponentType(addedComponentPath, out isAnglePart, out isUpPart, out isTabletop);

                            if (swConfig != null && mainComponent != null)
                            {
                                Feature plane1, plane2;

                                if (isTabletop)
                                    goto endMate;

                                plane1 = mainComponent.FeatureByName("#swrfНижняя");
                                plane2 = addedComponent.FeatureByName("Нижняя");
                                AddMate(swModel, plane1, plane2);

                                var minDistance = km.GetSimilarTables(addedComponent, isUpPart, isAnglePart);

                                if (minDistance.planeDist != null && minDistance.planeSource != null)
                                    AddMate(swModel, minDistance.planeDist, minDistance.planeSource);
                                else
                                {
                                    minDistance = km.GetPointsMate(addedComponent, mainComponent, isAnglePart);
                                    if (minDistance.planeDist != null && minDistance.planeSource != null)
                                        AddMate(swModel, minDistance.planeDist, minDistance.planeSource);
                                }

                            endMate:
                                addedComponent.Select(false);
                            }
                            else
                                MessageBox.Show("Не удалось найти сборку с именем Замер!!");
                        }
                        #endregion
                    }

                    //отрыв без подтверждения если поставлена галочка и режим КЭШ выключен
                    if (Properties.Settings.Default.ImmediatelyDetachModel && !Properties.Settings.Default.CashModeOn)
                    {
                        IsEventsEnabled = true;
                        SwApp.RunCommand((int)swCommands_e.swCommands_PmOK, "");
                        IsEventsEnabled = false;
                    }
                }
                catch { }
                IsEventsEnabled = true;
            }
            return 0;
        }

        public class SuffixCopyPassClass
        {
            public LinkedList<CopiedFileNames> filesNames;
            public Component2 swAddedComp;
            public bool isAsinc;
        }

        private void AddSuffix(object o)
        {
            bool isAsync = ((SuffixCopyPassClass)(o)).isAsinc;
            var swAddedComp = ((SuffixCopyPassClass)(o)).swAddedComp;
            var filesNames = ((SuffixCopyPassClass)(o)).filesNames;
            AddSuffix(filesNames, swAddedComp, isAsync);
        }
        private void AddSuffix(LinkedList<CopiedFileNames> filesNames, Component2 swAddedComp, bool isAsync)
        {

            if (isAsync)
                needWait = true;

            var thisModel = (ModelDoc2)swAddedComp.GetModelDoc();

            if (thisModel != null)
            {
                thisModel.ReloadOrReplace(false, thisModel.GetPathName(), true);
                if (isAsync)
                {
                    lock (workerLocker2)
                        Monitor.PulseAll(workerLocker2);
                }

                Decors.AddModelToList(this, thisModel);
                string fileName;
                if (ExistingFileDictionaryWithMdb(out fileName))
                {
                    var strArr = File.ReadAllLines(fileName).ToList();
                    strArr.Add(thisModel.GetPathName().Substring(GetRootFolder(SwModel).Length));
                    File.SetAttributes(fileName, FileAttributes.Normal);
                    File.WriteAllLines(fileName, strArr);
                    File.SetAttributes(fileName, FileAttributes.Hidden);
                }
                #region Добавление суффикса в свойства

                try
                {
                    if (filesNames != null)
                    {
                        var dictNames = new Dictionary<string, string>();
                        foreach (var copiedFileNamese in filesNames)
                        {
                            if (
                                !dictNames.ContainsKey(Path.GetFileName(copiedFileNamese.OldName)))
                                dictNames.Add(Path.GetFileName(copiedFileNamese.OldName),
                                              Path.GetFileName(copiedFileNamese.NewName));
                        }

                        SwDMApplication swDocMgr = GetSwDmApp();


                        foreach (var copiedFileNamese in filesNames)
                        {
                            SwDmDocumentOpenError oe;

                            var swDoc = swDocMgr.GetDocument(copiedFileNamese.NewName,
                                                             SwDmDocumentType.
                                                                 swDmDocumentUnknown
                                                             , true, out oe);
                            if (swDoc != null)
                            {
                                var names = (string[])swDoc.GetCustomPropertyNames();
                                foreach (var name in names)
                                {
                                    SwDmCustomInfoType swDmCstInfoType;
                                    string valueOfName = swDoc.GetCustomProperty(name, out swDmCstInfoType);
                                    string lowValOfName = valueOfName.ToLower();

                                    if (lowValOfName.Contains("@") &&
                                        !lowValOfName.Contains("#") &&
                                        (lowValOfName.Contains(".sld")))
                                    {
                                        foreach (var dictName in dictNames)
                                        {
                                            string lowDictName = dictName.Key.ToLower();
                                            if (lowValOfName.Contains(lowDictName))
                                            {
                                                string oldModName =
                                                    valueOfName.Substring(
                                                        valueOfName.IndexOf(
                                                            dictName.Key.Split('.').First()),
                                                        dictName.Key.Length);

                                                string newStrVal =
                                                    valueOfName.Replace(oldModName,
                                                                        dictName.Value);
                                                ModelDoc2 outMod;

                                                if (GetModelByName(thisModel,
                                                                   Path.GetFileName(
                                                                       swDoc.FullName), true,
                                                                   out outMod))
                                                {
                                                    SetModelProperty(outMod, name, "",
                                                                     swCustomInfoType_e.
                                                                         swCustomInfoText,
                                                                     newStrVal);
                                                    outMod.Save();
                                                }
                                            }
                                        }
                                    }
                                }
                                swDoc.CloseDoc();
                            }
                        }
                    }
                }
                catch
                {
                }

                #endregion

                if (Properties.Settings.Default.AutoSaveDrawings)
                {

                    if (Properties.Settings.Default.ShowRPDBefore)
                    {
                        _frmPrm.SwAsmDoc.NewSelectionNotify -= _frmPrm.NewSelection;
                        bool select = swAddedComp.Select(false);
                        _frmPrm.SwAsmDoc.NewSelectionNotify += _frmPrm.NewSelection;
                        if (select)
                            CopyDrawings2(true, true, null);
                    }
                    else
                    {
                        if (swAddedComp.Select(false))
                            CopyDrawings2(true, true, null);
                    }

                }
                //thisModel.ReloadOrReplace(false, thisModel.GetPathName(), true);
                RebuildEquation(swAddedComp);

                if (Properties.Settings.Default.AutoRecalculateOnAdd)
                {
                    try
                    {
                        RecalculateModel(thisModel);
                    }
                    catch
                    {
                    }
                }

                // добавили, что бы данная модель попала в список ссылок при отрыве и ее чертеж не удалился
            }
            if (Properties.Settings.Default.AutoCutOff) CutOff();
            if (Properties.Settings.Default.AutoRecalculateOnAdd) SwModel.EditRebuild3();

            needWait = false;
            //Logging.Log.Instance.TraceStop(startTime, "Stop");
            if (isAsync)
            {
                lock (workerLocker)
                    Monitor.PulseAll(workerLocker);
            }
        }
        public int OnCommandCloseNotify(int command, int reason)
        {
            if (IsEventsEnabled)
            {
                IsEventsEnabled = false;
                try
                {
                    Component2 swAddedComp = null;

                    if (Properties.Settings.Default.CashModeOn)
                    {
                        #region КЭШ вкл

                        if (_addingComponentNames.Count > 0)
                        {
                            IsEventsEnabled = false;
                            _iSwApp.RefreshTaskpaneContent();

                            if (Properties.Settings.Default.AutoSaveComponents)
                            {
                                foreach (string compName in _addingComponentNames)
                                {
                                    GetComponentByName(SwModel, compName, false, out swAddedComp);
                                }
                            }
                            //записать
                            if (RootModel == null)
                                RootModel = SwModel;

                            int par1 = 0, par2 = 0;
                            RootModel.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, par1, par2);

                            if (par1 > 0)
                            {
                                MessageBox.Show("При сохранении добавленного компонента произошли ошибки.");
                                IsEventsEnabled = true;
                                return 0;
                            }

                            linksToCash.Save(RootModel.GetPathName());



                            _addingComponentNames.Clear();


                        }
                        else
                            return 0;

                        #endregion
                    }
                    else
                    {
                        #region КЭШ выкл

                        if (_addingComponentNames.Count > 0)
                        {
                            try
                            {
                                if (Properties.Settings.Default.AutoSaveComponents)
                                {
                                    foreach (string compName in _addingComponentNames)
                                    {
                                        if (GetComponentByName(SwModel, compName, false, out swAddedComp))
                                        {
                                            LinkedList<CopiedFileNames> filesNames = null;


                                            if (swAddedComp.Select(false))
                                            {
                                                SaveAsComponents(out filesNames);
                                            }
                                            if (Properties.Settings.Default.ShowRPDBefore)
                                            {
                                                lock (workerLocker)
                                                {
                                                    ThreadPool.QueueUserWorkItem(AddSuffix,
                                                                                 new SuffixCopyPassClass()
                                                                                 {
                                                                                     filesNames = filesNames,
                                                                                     swAddedComp = swAddedComp,
                                                                                     isAsinc = true
                                                                                 });
                                                }
                                                lock (workerLocker2)
                                                    Monitor.Wait(workerLocker2);

                                            }
                                            else
                                            {
                                                AddSuffix(filesNames, swAddedComp, false);

                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Logging.Log.Instance.Fatal(e.Message + "OnCommandCloseNotify()");
                                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            }
                            _addingComponentNames = new LinkedList<string>();
                        }

                        #endregion
                    }

                    if (Properties.Settings.Default.KitchenModeOn)
                    {
                        #region Обработка режима кухни

                        CustomPropertyManager propertyManager = ((ModelDoc2)swAddedComp.GetModelDoc2()).Extension.get_CustomPropertyManager(string.Empty);
                        string kitchenTypeValue = string.Empty;
                        string resolvedKitchenTypeValue = string.Empty;
                        propertyManager.Get4("KitchenType", false, out kitchenTypeValue, out resolvedKitchenTypeValue);

                        if (!string.IsNullOrEmpty(kitchenTypeValue))
                        {
                            var swModel = SwApp.IActiveDoc2;
                            var swConfig = (Configuration)swModel.GetActiveConfiguration();
                            Component2 swRootComponent = null;
                            if (swConfig != null)
                                swRootComponent = (Component2)swConfig.GetRootComponent();
                            KitchenModule km = new KitchenModule(RootModel, swRootComponent, this, swModel);
                            bool isAnglePart, isUpPart, isTabletop;
                            km.GetComponentType(swAddedComp.GetPathName(), out isAnglePart, out isUpPart, out isTabletop);
                            if (isTabletop)
                                km.TableTopProcess(swAddedComp);
                        }

                        #endregion
                    }

                    if (Properties.Settings.Default.AutoShowSetParameters && swAddedComp != null)
                    {
                        if (swAddedComp.Select(false))
                            ShowSetParameters();
                    }

                    RebuildEquation(swAddedComp);
                }
                catch { }
                IsEventsEnabled = true;
            }
            return 0;
        }

        public int OnNewSelection()
        {
            if (IsEventsEnabled)
            {
                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;
                RemovePopupMenu();

                if (swModel != null)
                {
                    var swSelMgr = (SelectionMgr)swModel.SelectionManager;

                    if (swSelMgr.GetSelectedObjectCount() > 0)
                    {
                        var swSelComp = (Component2)swSelMgr.GetSelectedObjectsComponent2(1);
                        if (swSelComp != null)
                        {
                            Component2 swDbComp;
                            if (GetParentLibraryComponent(swSelComp, out swDbComp))
                            {
                                _iSwApp.AddMenuPopupItem2(swModel.GetType(), _addinId,
                                                          (int)swSelectType_e.swSelEVERYTHING,
                                                          MyTitle + " - MrDoors РПД", "ShowSetParameters",
                                                          "ShowSetParametersEnable", "", "");
                                _iSwApp.AddMenuPopupItem2(swModel.GetType(), _addinId,
                                                        (int)swSelectType_e.swSelEVERYTHING,
                                                        MyTitle + " - Открыть чертеж детали",
                                                        "OpenDrawing", "OpenDrawingEnable", "", "");
                                _iSwApp.AddMenuPopupItem2((int)SwModel.GetType(), _addinId,
                                                          (int)swSelectType_e.swSelEVERYTHING,
                                                          MyTitle + " - Отделка кромки", "EdgePro" +
                                                                                         "cessing",
                                                          "EdgeProcessingEnable", "", "");
                                _iSwApp.AddMenuPopupItem2(swModel.GetType(), _addinId,
                                                          (int)swSelectType_e.swSelEVERYTHING,
                                                          MyTitle + " - Заменить выбранные компоненты",
                                                          "ReplaceComponents", "ReplaceComponentsEnable", "", "");
                                _iSwApp.AddMenuPopupItem2(SwModel.GetType(), _addinId,
                                                        (int)swSelectType_e.swSelEVERYTHING,
                                                        MyTitle + " - Пересоздать отверстия для детали(не пакетно)",
                                                        "CutOffDetail", "SaveAsComponentsEnable", "", "");
                            }
                        }
                        //if (swSelMgr.GetSelectedObjectType2(1) == (int) swSelectType_e.swSelFACES)
                        //{
                        //    var swSelFace = (Face2) swSelMgr.GetSelectedObject(1);
                        //    var swSelBody = swSelFace.IGetBody();

                        //    if (!AddDeleteBodyMenu(swSelBody))
                        //    {
                        //        _iSwApp.AddMenuPopupItem2((int)SwModel.GetType(), _addinId,
                        //                                  (int) swSelectType_e.swSelEVERYTHING,
                        //                                  MyTitle + " - Отделка кромки", "EdgeProcessing",
                        //                                  "EdgeProcessingEnable", "", "");
                        //    }
                        //}
                        if (swSelMgr.GetSelectedObjectType2(1) == (int)swSelectType_e.swSelSOLIDBODIES)
                        {
                            var swSelBody = (Body2)swSelMgr.GetSelectedObject(1);
                            AddDeleteBodyMenu(swSelBody);
                        }
                    }
                }
            }
            return 0;
        }
        public int ReplaceComponentsEnable()
        {
            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            LinkedList<Component2> selComps;
            return GetSelectedComponents(out selComps) ? 1 : 0;
        }
        public int OpenDrawingEnable()
        {
            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            SelectionMgr swSelMgr = SwModel.ISelectionManager;
            Component2 _swSelComp;
            if (swSelMgr.GetSelectedObjectCount() == 1)
            {
                var swTestSelComp = (Component2)swSelMgr.GetSelectedObjectsComponent2(1);

                if (GetParentLibraryComponent(swTestSelComp, out _swSelComp))
                {
                    do
                    {
                        ModelDoc2 specModel = swTestSelComp.IGetModelDoc();
                        bool draft = (specModel.CustomInfo2["", "Required Draft"] == "Yes" ||
                                      specModel.CustomInfo2[
                                          specModel.IGetActiveConfiguration().Name, "Required Draft"] == "Yes");

                        if (draft)
                        {
                            return 1;
                        }
                        swTestSelComp = swTestSelComp.GetParent();
                    } while (swTestSelComp != null);
                }
            }
            return 0;
        }

        public Component2 GetMdbComponentTopLevel(Component2 upComponent, out ModelDoc2 specModel)
        {
            Component2 specComp = null;
            specModel = upComponent.IGetModelDoc();
            bool cycle = true;
            while (cycle)
            {
                Component2 specComp2 = upComponent;
                if (specComp != null && specComp == specComp2)
                {
                    var comp = specComp.GetParent();
                    if (comp != null)
                        specComp2 = comp;
                    else
                        break;
                }
                specComp = specComp2;
                cycle = GetParentLibraryComponent(specComp, out specComp2);
                if (cycle)
                {
                    upComponent = specComp2;
                }
            }
            return upComponent;
        }

        public void OpenDrawing(bool minimize = false)
        {
            SelectionMgr swSelMgr = SwModel.ISelectionManager;
            Component2 _swSelComp;
            //ModelDoc2 _swSelModel;
            string path1 = null;
            if (swSelMgr.GetSelectedObjectCount() == 1)
            {
                var swTestSelComp = (Component2)swSelMgr.GetSelectedObjectsComponent2(1);
                bool draft = false;
                if (GetParentLibraryComponent(swTestSelComp, out _swSelComp))
                {

                    while (swTestSelComp != null && (!draft || swTestSelComp != null))
                    {
                        ModelDoc2 specModel = swTestSelComp.IGetModelDoc();
                        draft = (specModel.CustomInfo2["", "Required Draft"] == "Yes" ||
                                 specModel.CustomInfo2[
                                     specModel.IGetActiveConfiguration().Name, "Required Draft"] == "Yes");
                        if (draft)
                        {
                            DirectoryInfo dir;
                            dir = new DirectoryInfo(Path.GetDirectoryName(SwModel.GetPathName()));

                            var paths =
                                dir.GetFiles(
                                    Path.GetFileNameWithoutExtension(specModel.GetPathName()) + ".SLDDRW",
                                    SearchOption.AllDirectories);


                            if (paths.Any())
                                path1 = paths[0].FullName;
                            else
                            {

                                path1 = Path.Combine(Path.GetDirectoryName(specModel.GetPathName()),
                                                     Path.GetFileNameWithoutExtension(specModel.GetPathName()) +
                                                     ".SLDDRW");

                            }
                        }
                        swTestSelComp = swTestSelComp.GetParent();

                    }
                }

            }
            else
            {
                return;
            }

            if (string.IsNullOrEmpty(path1))
                return;

            var path = Path.Combine(Path.GetDirectoryName(path1),
                                                            Path.GetFileNameWithoutExtension(path1) +
                                                            ".SLDDRW");

            SwApp.OpenDoc(path, (int)swDocumentTypes_e.swDocDRAWING);
            int errors = 0;
            var tmp = (ModelDoc2)SwApp.ActivateDoc2(path, true, ref errors);
            if (tmp != null)
            {
                var myModelView = (IModelView)tmp.GetFirstModelView();
                if (minimize)
                    myModelView.FrameState = (int)swWindowState_e.swWindowMinimized;
                else
                    myModelView.FrameState = (int)swWindowState_e.swWindowMaximized;
            }
        }

        private bool AddDeleteBodyMenu(Body2 swSelBody)
        {
            bool ret = false;

            if (swSelBody != null)
            {
                if (swSelBody.Name.Length > 9)
                {
                    if (swSelBody.Name.Substring(0, 9).ToLower() == "#swrfedge")
                    {
                        bool bRet = _iSwApp.AddMenuPopupItem2((int)swDocumentTypes_e.swDocPART, _addinId,
                                                              (int)swSelectType_e.swSelEVERYTHING,
                                                              MyTitle + " - Удалить кромку", "DeleteEdge", "", "", "");
                        ret = true;
                    }
                }
            }
            return ret;
        }

        public int OnComponentStateChange(object componentModel, string compName, short oldCompState, short newCompState)
        {
            //_isRegenDisabled = true;
            return 0;
        }

        public int OnRegenPost()
        {
            //_isRegenDisabled = false;
            return 0;
        }

        #endregion

        public void ShowOptions()
        {
            FrmOption = new FrmOptions(this, _iSwApp, _addinId);
            FrmOption.ShowDialog();
        }

        #region MrDoors RPD

        public int ShowSetParametersEnable()
        {
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void ShowSetParameters()
        {
            if (_frmPrm == null)
            {
                _frmPrm = new FrmSetParameters(this);
                _frmPrm.Disposed += FrmPrmDisposed;
                _frmPrm.FormClosing += FrmPrmFormClosing;
                if (Properties.Settings.Default.SetParentWindow)
                    SetParent(_frmPrm.Handle, _swHandle); // делаем SW парентом формы, что бы она сворачивалсь
            }
        }

        private void FrmPrmFormClosing(object sender, FormClosingEventArgs e)
        {
            SetParent(_frmPrm.Handle, GetDesktopWindow());
        }

        private void FrmPrmDisposed(object sender, EventArgs e)
        {
            _frmPrm = null;
        }
        private void FrmEdgeFormClosing(object sender, FormClosingEventArgs e)
        {
            SetParent(_frmEdge.Handle, GetDesktopWindow());
        }
        private void FrmEdgeDisposed(object sender, EventArgs e)
        {
            _frmEdge = null;
        }
        #endregion

        #region Delete notused models

        public int DeleteNotUsedModelsEnable()
        {
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void DeleteNotUsedModels()
        {
            MessageBox.Show(@"Не работает!");
        }

        #endregion

        #region Deleting edge

        public int EdgeProcessingEnable()
        {
            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            SelectionMgr swSelMgr = SwModel.ISelectionManager;
            Component2 _swSelComp;
            if (swSelMgr.GetSelectedObjectCount() == 1)
            {
                var swTestSelComp = (Component2)swSelMgr.GetSelectedObjectsComponent2(1);

                if (GetParentLibraryComponent(swTestSelComp, out _swSelComp))
                {
                    do
                    {
                        ModelDoc2 specModel = swTestSelComp.IGetModelDoc();
                        //так тупо написано, потому что вычисления свойства гораздо тяжелее чем if (bool)
                        bool fanerExist = false;
                        if (!string.IsNullOrEmpty(specModel.CustomInfo2["", "ExtFanerFeats"]) && specModel.CustomInfo2["", "ExtFanerFeats"] == "Yes")
                            fanerExist = true;
                        if (!fanerExist && !string.IsNullOrEmpty(specModel.CustomInfo2[specModel.IGetActiveConfiguration().Name, "ExtFanerFeats"]) && specModel.CustomInfo2[specModel.IGetActiveConfiguration().Name, "ExtFanerFeats"] == "Yes")
                            fanerExist = true;
                        if (fanerExist)
                        {
                            return 1;
                        }
                        swTestSelComp = swTestSelComp.GetParent();
                    } while (swTestSelComp != null);
                }
            }
            return 0;
        }

        public void EdgeProcessing()
        {
            if (_frmEdge == null)
            {
                IsEventsEnabled = false;
                try
                {
                    _frmEdge = new FrmEdge(this);
                    _frmEdge.Disposed += FrmEdgeDisposed;
                    _frmEdge.FormClosing += FrmEdgeFormClosing;
                    SetParent(_frmEdge.Handle, _swHandle); // делаем SW парентом формы, что бы она сворачивалсь
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }

                IsEventsEnabled = true;
            }
        }

        public void DeleteEdge()
        {
            IsEventsEnabled = false;

            try
            {
                if (MessageBox.Show(@"Удалить кромку?", MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                    DialogResult.Yes)
                {
                    var swSelMgr = (SelectionMgr)SwModel.SelectionManager;

                    Body2 swSelBody = null;

                    if (swSelMgr.GetSelectedObjectType2(1) == (int)swSelectType_e.swSelFACES)
                    {
                        //нужно брать body, т.к. этот face не даст правильный feature
                        var swSelFace = (Face2)swSelMgr.GetSelectedObject(1);
                        swSelBody = (Body2)swSelFace.GetBody();
                    }

                    if (swSelMgr.GetSelectedObjectType2(1) == (int)swSelectType_e.swSelSOLIDBODIES)
                    {
                        swSelBody = (Body2)swSelMgr.GetSelectedObject(1);
                    }


                    if (swSelBody != null)
                    {
                        string strBodyName = swSelBody.Name;
                        Face2 swFace = swSelBody.IGetFirstFace();
                        Feature swFeature = swFace.IGetFeature();

                        //string featname = swFeature.Name;
                        //featname = swFeature.GetTypeName2();

                        if (swFeature.GetTypeName2() == "SewRefSurface")
                        {
                            Feature swMoveFeat;
                            if (GetFeatureByName(SwModel, strBodyName + "_move", out swMoveFeat))
                            {
                                swMoveFeat.Select(false);
                                SwModel.DeleteSelection(false);
                            }

                            swFeature.Select(false);
                        }
                        else
                        {
                            var parFeatArr = (object[])swFeature.GetParents();
                            var swParFeature = (Feature)parFeatArr[0];
                            parFeatArr = (object[])swParFeature.GetParents();
                            swParFeature = (Feature)parFeatArr[0];
                            swParFeature.Select(false);
                        }

                        SwModel.DeleteSelection(false);

                        //фаску нужно удалять после тела, т.к. ее удаление влияет на объект swSelBody
                        Feature swChamFeat;
                        if (GetFeatureByName(SwModel, strBodyName + "_chamfer", out swChamFeat))
                        {
                            swChamFeat.Select(false);
                            SwModel.DeleteSelection(false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            IsEventsEnabled = true;
        }
        public int DeleteEdgeEnable()
        {
            if (Properties.Settings.Default.CashModeOn)
                return 0;
            else
                return 1;

        }
        #endregion
        #region CopyProject
        public void CopyProjectFormShow()
        {
            if (!CheckForTearOff())
            {
                MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
                return;
            }
            var model = SwModel;
            if (RootModel != null)
                model = RootModel;
            var FrmCopyProject = new frmCopyProject(this, _iSwApp, model, model.GetPathName());
            FrmCopyProject.ShowDialog();
        }
        public int CopyProjectFormShowEnable()
        {
            if (Properties.Settings.Default.CashModeOn)
                return 0;
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }
        #endregion
        #region Fix Separate Errors
        public void FixSeperateErrors()
        {
            WaitTime.Instance.SetLabel("Исправление некорректного отрыва деталей...");
            WaitTime.Instance.ShowWait();
            SwModel.Save2(true);
            try
            {
                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;

                _allPaths.Clear();

                // проверка на оторванность
                string suffix = GetXNameForAssembly(false, Path.GetFileNameWithoutExtension(SwModel.GetPathName()));
                var oComps = (object[])((AssemblyDoc)swModel).GetComponents(true);
                if (oComps != null)
                {
                    List<string> topLvlCompPaths = new List<string>();
                    foreach (var oComp in oComps)
                    {
                        var comp = (Component2)oComp;
                        topLvlCompPaths.Add(comp.GetPathName());
                    }
                    string rootName = swModel.GetPathName();
                    if (RootModel != null)
                        rootName = RootModel.GetPathName();//GetRootFolder(swModel);
                    _iSwApp.CloseAllDocuments(true);


                    CheckRefRecurcy(topLvlCompPaths.ToArray(), suffix);

                    int warnings = 0;
                    int errors = 0;
                    var swModelDoc = (ModelDoc2)_iSwApp.OpenDoc6(rootName, (int)swDocumentTypes_e.swDocASSEMBLY,
                                                                  (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "",
                                                                  errors,
                                                                  warnings);
                    swModelDoc.EditRebuild3();
                    AttachModelDocEventHandler(swModelDoc);

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                WaitTime.Instance.HideWait();
            }
        }
        private void CheckRefRecurcy(string[] paths, string suffix)
        {
            SwDmDocumentOpenError oe;
            SwDMApplication swDocMgr = GetSwDmApp();

            SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
            src.SearchFilters = 255;
            object brokenRefVar;

            foreach (var compPath in paths)
            {
                SwDMDocument8 swDoc = null;
                if (compPath.ToLower().Contains("sldasm"))
                    swDoc = (SwDMDocument8)swDocMgr.GetDocument(compPath,
                                                                SwDmDocumentType.swDmDocumentAssembly,
                                                                false, out oe);
                if (swDoc != null)
                {

                    var varRef = (string[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                    if (varRef != null && varRef.Length != 0)
                    {
                        foreach (string o in varRef)
                        {
                            if (!o.Contains(suffix))
                            {
                                string destFileName = AddStringSuffix(o, Path.GetDirectoryName(compPath), suffix);
                                if (!File.Exists(destFileName))
                                {
                                    File.Copy(o, destFileName);
                                    File.SetAttributes(destFileName, FileAttributes.Normal);
                                }
                                swDoc.ReplaceReference(o, destFileName);
                                swDoc.Save();
                            }
                            var varRef1 = new string[1] { o };
                            CheckRefRecurcy(varRef1, suffix);
                        }
                    }
                    else
                    {
                        swDoc.CloseDoc();
                        return;
                    }
                    swDoc.CloseDoc();
                }
            }
        }
        //public int FixSeperateErrorsEnable()
        //{
        //    return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        //}

        private string AddStringSuffix(string filePath, string rootFolder, string suffix)
        {
            string s = string.Empty;
            if (string.IsNullOrEmpty(suffix))
                s = GetXNameForAssembly();
            else
                s = suffix;
            string lastDirectory = rootFolder.Split(Path.DirectorySeparatorChar).LastOrDefault();
            if (!string.IsNullOrEmpty(lastDirectory))
            {
                if (Path.GetExtension(filePath).ToLower() == ".sldprt")
                {
                    if (lastDirectory.ToLower() == "фурнитура")
                    {
                        rootFolder = Path.Combine(rootFolder, "Модели фурнитуры");
                        if (!Directory.Exists(rootFolder))
                            Directory.CreateDirectory(rootFolder);
                    }
                    if (lastDirectory.ToLower() == "детали")
                    {
                        rootFolder = Path.Combine(rootFolder, "Детали для сборок");
                        if (!Directory.Exists(rootFolder))
                            Directory.CreateDirectory(rootFolder);
                    }
                }
            }
            return Path.Combine(rootFolder, Path.GetFileNameWithoutExtension(filePath) + " #" + s + Path.GetExtension(filePath));
        }
        #endregion
        #region Final Processing

        public int FinalProcessingEnable()
        {
            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }
        public static bool GetDeleteXmlFiles(string rootModelPath, out List<string> modelsToDelete, out List<string> allModels)
        {
            modelsToDelete = new List<string>();
            allModels = new List<string>();
            string delDir = Path.Combine(Path.GetDirectoryName(rootModelPath), "Программы");
            if (Directory.Exists(delDir))
            {
                if (Directory.GetFiles(delDir, "*.xml", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    //теперь тут проверку игоря..
                    #region проверка типа как у Игоря, проверяю по датам файлов что время последней окончат. обработки >= времени всех sldprt и sldasm кроме главной!
                    //считываем время последней окончательной обработки
                    string _openFile = rootModelPath;
                    bool copyXmlFiles = false;
                    string fpFileName = Path.Combine(Path.GetDirectoryName(_openFile), "fpTime.txt");
                    if (File.Exists(fpFileName))
                    {
                        var sr = File.OpenText(fpFileName);
                        string line = sr.ReadLine();
                        sr.Close();
                        DateTime fpTime;
                        var culture = CultureInfo.CreateSpecificCulture("ru-RU");
                        var styles = DateTimeStyles.AssumeLocal;
                        if (DateTime.TryParse(line, culture, styles, out fpTime))
                        {
                            var files =
                                 Directory.GetFiles(Path.GetDirectoryName(_openFile), "*.SLDASM",
                                                   SearchOption.AllDirectories);
                            //.Union(Directory.GetFiles(Path.GetDirectoryName(_openFile), "*.SLDPRT",
                            //                          SearchOption.AllDirectories));
                            bool cicleBreak = false;
                            var currXml = new XmlDocument();
                            foreach (var file in files)
                            {
                                if (File.Exists(file) && !(Path.GetFileName(file).First() == '~') && Path.GetFileName(file) != Path.GetFileName(_openFile))
                                {
                                    if (File.GetLastWriteTime(file) > fpTime.AddSeconds(1))//добавляем секунду
                                    {
                                        cicleBreak = true;
                                        modelsToDelete.Add(Path.GetFileNameWithoutExtension(file));
                                    }
                                    allModels.Add(Path.GetFileNameWithoutExtension(file));
                                }
                            }
                            copyXmlFiles = !cicleBreak;
                        }

                    }
                    return !copyXmlFiles;

                    #endregion

                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        private void MakeXmlUnvalid(List<string> deletedFiles)
        {
            foreach (var file in deletedFiles)
            {
                if (File.Exists(file))
                {
                    //добавить ко всем файлам что они не валидны
                    var currXml = new XmlDocument();
                    currXml.Load(file);
                    if (currXml.DocumentElement.GetAttribute("CNCValid") != false.ToString())
                    {
                        currXml.DocumentElement.SetAttribute("CNCValid", false.ToString());
                        currXml.Save(file);
                    }
                }
            }
        }

        public void DeleteXmlFiles(bool deleteUnActualModels)
        {
            List<string> modelsToDelete, allModels, deletedModels = new List<string>(), deletedFiles = new List<string>();
            bool deleteXml = GetDeleteXmlFiles(SwModel.GetPathName(), out modelsToDelete, out allModels);

            string delDir = Path.Combine(Path.GetDirectoryName(SwModel.GetPathName()), "Программы");
            if (Directory.Exists(delDir))
            {
                foreach (var file in Directory.GetFiles(delDir, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    //прежде чем удалить этот файл 1) вытащить ModelName из xml 2) Если он есть в modelsToDelete = > удалить
                    var currXml = new XmlDocument();
                    currXml.Load(file);
                    if (currXml.ChildNodes.Count == 0)
                    {
                        File.Delete(file);
                        continue;
                    }
                    var modelName = Path.GetFileNameWithoutExtension(currXml.ChildNodes[0].Attributes["Name"].Value);
                    if (modelsToDelete.Contains(modelName) && deleteUnActualModels || (currXml.DocumentElement.GetAttribute("CNCValid") == null || currXml.DocumentElement.GetAttribute("CNCValid") == false.ToString()))
                    {
                        //File.Delete(file);
                        deletedFiles.Add(file);
                        deletedModels.Add(modelName);
                    }
                    else
                    {
                        if (!allModels.Contains(modelName) && !(modelName.Substring(modelName.Length - 4, 4)[0] == '#' && (modelName.Substring(modelName.Length - 4, 4)[3] == 'P' || modelName.Substring(modelName.Length - 4, 4)[3] == 'p')))
                        {
                            File.Delete(file);
                            deletedModels.Add(modelName);
                        }
                    }
                }
            }
            if (!deleteUnActualModels)
                return;
            //теперь для тех файлов которые были удалены перегенерить программы
            if (Properties.Settings.Default.CreateProgramsOnFF && deletedModels.Count > 0)
            {
                var vOpenDocs = SwApp.GetDocuments();
                List<IModelView> modelViewsMinimized = new List<IModelView>();
                foreach (var vOpenDoc in vOpenDocs)
                {
                    ModelDoc2 tmpModel = vOpenDoc;
                    if (tmpModel.GetType() == (int)swDocumentTypes_e.swDocDRAWING)
                        SwApp.CloseDoc(tmpModel.GetTitle());
                    else if (tmpModel.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
                    {
                        var myModelView1 = (IModelView)tmpModel.GetFirstModelView();
                        if (myModelView1 != null)
                        {
                            myModelView1.FrameState = (int)swWindowState_e.swWindowMinimized;
                            modelViewsMinimized.Add(myModelView1);
                        }
                    }
                }
                if (deletedFiles.Count > 2)
                {
                    string text = "Ряд моделей в сборке были изменены после образмеривания чертежей на них. Это привело к удалению программ для данных моделей. Необходимо пересоздать указанные программы. Это займет дополнительное время при окончательной обработке заказа. Пересоздать программы?";
                    if (MessageBox.Show(text, MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                    {
                        //Logging.Log.Instance.SendMail("Перестроение чертежей. Количество перестраеваемых:" + deletedFiles.Count + " . Ответ пользователя - НЕТ");
                        MakeXmlUnvalid(deletedFiles);
                        return;
                    }
                    else
                    {
                        //Logging.Log.Instance.SendMail("Перестроение чертежей. Количество перестраеваемых:" + deletedFiles.Count + " . Ответ пользователя - ДА");
                        MakeXmlUnvalid(deletedFiles);
                    }

                }

                foreach (var deletedModel in deletedModels)
                {

                    //для начала просто переобразмерить...
                    using (var dd = new DimensionDraft(this))
                    {
                        //выделить деталь...
                        object[] oComps = null;
                        var tmp = SwModel as AssemblyDoc;
                        if (tmp != null)
                            oComps = tmp.GetComponents(true) as object[];
                        else
                        {
                            //Достать AssemblyDoc
                            continue;
                        }

                        if (oComps != null)
                        {
                            foreach (Component2 oComp in oComps)
                            {

                                if (oComp.Name.Contains(deletedModel))
                                {
                                    try
                                    {
                                        if (oComp.Select(false))
                                        {

                                            //открыть чертеж.
                                            OpenDrawing(true);

                                            //образмерить
                                            dd.AutoDimensionDrawing(false);
                                            //CloseAllExceptMainAssembly();
                                        }
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        Logging.Log.Instance.Fatal(ex, "Ошибка при перестроении программы..");
                                        continue;
                                    }
                                    finally
                                    {
                                        CloseAllExceptMainAssembly();
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (var modelView in modelViewsMinimized)
                {
                    modelView.FrameState = (int)swWindowState_e.swWindowMaximized;

                }
            }
        }
        private void CloseAllExceptMainAssembly()
        {
            var vOpenDocs = SwApp.GetDocuments();

            foreach (var vOpenDoc in vOpenDocs)
            {
                ModelDoc2 tmpModel = vOpenDoc;
                if (tmpModel.GetType() == (int)swDocumentTypes_e.swDocDRAWING)
                    SwApp.CloseDoc(tmpModel.GetTitle());

            }
        }

        public void StartFinalProcessing(bool askIfNoDraw = true)
        {
            var startTime = DateTime.Now;
            FinalProcessing(askIfNoDraw);
            TimeSpan time = DateTime.Now - startTime;
            MessageBox.Show(
                     @"Операция завершена за " + time.Minutes + @" минут " + time.Seconds + @" секунд",
                     MyTitle, MessageBoxButtons.OK,
                     MessageBoxIcon.Information);
        }

        string DrwPathResult = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DrwPath") == string.Empty
                                       ? Properties.Settings.Default.DrwPath : Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DrwPath");
        public void FinalProcessing(bool askIfNoDraw = true)
        {
            const string strErr = "Ошибка при окончательной обработке заказа!\n";

            try
            {
                if (Properties.Settings.Default.CashModeOn)
                {
                    bool cashComponentsExist = false;
                    var components = (RootModel as AssemblyDoc).GetComponents(false);
                    foreach (var comp in components)
                    {
                        Regex regex = new Regex(@"#\d\dP");
                        Match match = regex.Match(((Component2)comp).Name);
                        if (match.Success)
                            cashComponentsExist = true;
                    }

                    if (cashComponentsExist)
                    {
                        if (MessageBox.Show("В сборке имеются неоторванные детали. Для корректной работы рекомендуется выполнить отрыв. \n\nОторвать компоненты от библиотеки?", "MrDoors", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            if (RootModel == null)
                                RootModel = SwModel;
                            frmCopyProject.RecopyHeare(this, _iSwApp, RootModel);
                        }
                    }
                }
                ProgressBar.WaitTime.Instance.SetLabel("Идет окончательная обработка. Подождите...");
                ProgressBar.WaitTime.Instance.ShowWait();

                if (RootModel == null)
                    RootModel = SwModel;


                DeleteXmlFiles(askIfNoDraw);//true);

                RecalculateRanges2(true);

                //в головную сборку добавить сведения о SP солида, версии аддина, версии библиотеки.

                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;

                string versionSp = string.Empty;
                try
                {
                    var subfolders = Registry.Users.GetSubKeyNames();

                    foreach (var subfolder in subfolders)
                    {
                        if (subfolder.Contains("S-1-5-21"))
                        {
                            var curfolder = Registry.Users.OpenSubKey(subfolder);
                            var subsubfolder = curfolder.GetSubKeyNames();
                            foreach (var ss in subsubfolder)
                            {
                                if (ss.ToLower() == "software")
                                {
                                    curfolder =
                                        Registry.Users.OpenSubKey(subfolder + @"\" + ss +
                                                                  @"\SolidWorks\SolidWorks 2011\General\Last Run SolidWorks");
                                    if (curfolder != null)
                                    {
                                        versionSp = curfolder.GetValue("SOLIDWORKS_SP_STRING").ToString();
                                        break;
                                    }

                                }
                            }
                            if (!string.IsNullOrEmpty(versionSp))
                                break;
                        }
                    }
                }
                catch (Exception)
                { }
                if (!string.IsNullOrEmpty(versionSp))
                    SetModelProperty(swModel, "vSP", string.Empty, swCustomInfoType_e.swCustomInfoText, versionSp, true);
                SetModelProperty(swModel, "vAddInn", string.Empty, swCustomInfoType_e.swCustomInfoText, Assembly.GetExecutingAssembly().GetName().Version.ToString(), true);
                SetModelProperty(swModel, "vLib", string.Empty, swCustomInfoType_e.swCustomInfoText, Properties.Settings.Default.PatchVersion, true);
                string orderName = Path.GetFileName(Path.GetDirectoryName(swModel.GetPathName()));
                LinkedList<ModelDoc2> allModels;
                var allFindModelsPaths = new LinkedList<string>();

                _allPaths.Clear();
                var comps = (object[])((AssemblyDoc)swModel).GetComponents(false);
                if (comps != null)
                {
                    foreach (var ocomp in comps)
                    {
                        var comp2 = (Component2)ocomp;
                        var mmodel = comp2.GetModelDoc2() as ModelDoc2;
                        SwDMApplication swDocMgr = GetSwDmApp();
                        SwDmDocumentOpenError oe;
                        if (mmodel != null && mmodel.GetType() == (int)swDocumentTypes_e.swDocPART)
                        {
                            var swDoc = swDocMgr.GetDocument(mmodel.GetPathName(), SwDmDocumentType.swDmDocumentUnknown,
                                                             true, out oe);
                            if (swDoc != null)
                            {
                                if (swDoc.ConfigurationManager.GetConfigurationCount() > 1)
                                {
                                    comp2.Select(false);
                                    int inf = 0;
                                    ((AssemblyDoc)swModel).EditPart2(true, true, ref inf);
                                }
                            }
                        }
                    }
                }

                swModel.ClearSelection();
                ((AssemblyDoc)swModel).EditAssembly();
                swModel.EditRebuild3();

                // проверка на оторванность
                string suffix = GetXNameForAssembly(false, Path.GetFileNameWithoutExtension(SwModel.GetPathName()));
                var oComps = (object[])((AssemblyDoc)swModel).GetComponents(true);
                if (oComps != null)
                {
                    foreach (var oComp in oComps)
                    {
                        var comp = (Component2)oComp;
                        bool notModel;
                        if (CheckSuffixModel(comp, suffix, out notModel))
                        {
                            var model = comp.IGetModelDoc();
                            string name = Path.GetFileName(model != null ? model.GetPathName() : comp.GetPathName());
                            string texterr;
                            if (notModel)
                            {
                                texterr = strErr + Environment.NewLine + @"Имя детали " + name +
                                          @" не может быть корректно обработана программой," +
                                          Environment.NewLine +
                                          @" т.к. ее файл НЕ находится в основной папке заказа: " +
                                          Path.GetFileName(GetRootFolder(swModel)) +
                                          Environment.NewLine +
                                          @"Для устранения ошибки перенесите файл данной детали в основную папку заказа." +
                                          Environment.NewLine +
                                          @"Продолжить окончательную обработку?";
                            }
                            else
                            {
                                if (name.Contains(suffix))
                                {
                                    texterr = strErr + Environment.NewLine + @"Деталь " + name +
                                              @" некорректно оторвана" +
                                              Environment.NewLine + "Попытаться исправить ошибку отрыва?" + Environment.NewLine
                                              + "После этого необходимо будет повторить окончательную обработку заказа.";
                                    if (MessageBox.Show(texterr, MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                                        return;
                                    else
                                    {
                                        FixSeperateErrors();
                                        return;
                                    }
                                }
                                else
                                {
                                    texterr = strErr + Environment.NewLine + @"Деталь " + name + @" некорректно оторвана" +
                                              Environment.NewLine +
                                              @" от библиотеки и не будет импортирована в программу ""Покупки""!" +
                                              Environment.NewLine +
                                              @" Необходимо удалить данную деталь из заказа и добавить заново!" +
                                              Environment.NewLine +
                                              @"Продолжить окончательную обработку?";
                                }
                            }
                            if (MessageBox.Show(texterr, MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Error) ==
                                DialogResult.No)
                                return;
                        }
                    }
                }

                if (GetAllUniqueModels(swModel, out allModels))
                {

                    var listModelsWithArticul = new List<ModelDoc2>();
                    #region Unused models deleting

                    try
                    {
                        //для освобождения Солидом удаленных моделей необходимо перезагрузить сборку
                        int err = 0;
                        int wrn = 0;
                        if (swModel.Save3(
                            (int)
                            (swSaveAsOptions_e.swSaveAsOptions_Silent |
                             swSaveAsOptions_e.swSaveAsOptions_SaveReferenced), ref err, ref wrn))
                        {
                            string pathswModel = swModel.GetPathName();
                            swModel.ReloadOrReplace(false, pathswModel, true);
                            _allPaths.Clear();
                            if (GetAllPathNames(swModel.GetPathName()))
                            {
                                DeleteUnusedModels(GetRootFolder(swModel), _allPaths);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(strErr + @"Удаление неиспользуемых моделей.\n" +
                                        e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    #endregion

                    DeleteXmlFiles(false);

                    //сразу срубать все fpTime.txt
                    string newDictFileName1 = Path.Combine(Path.GetDirectoryName(SwModel.GetPathName()), "fpTime.txt");//  SwModel.GetPathName().Substring(0,  SwModel.GetPathName().LastIndexOf("\\") + 1) + "fpTime.txt");
                    File.Delete(newDictFileName1);

                    #region Drawings numering

                    drawingDictionary = new Dictionary<string, int>();
                    var moddrwnum = new List<ModelDrawingNumber>();

                    try
                    {
                        foreach (var modelDoc2 in allModels)
                        {
                            string modName;
                            try
                            {
                                modName = modelDoc2.GetPathName();
                            }
                            catch
                            {
                                continue;
                            }
                            string newModName;
                            string tt = Path.GetFileNameWithoutExtension(modName);
                            if (tt.Substring(tt.Length - 4, 4)[0] == '#' && (tt.Substring(tt.Length - 4, 4)[3] == 'P' || tt.Substring(tt.Length - 4, 4)[3] == 'p'))
                            {
                                string dir = Path.Combine(Path.GetDirectoryName(SwModel.GetPathName()), "ЧЕРТЕЖИ");
                                newModName = Path.Combine(dir, tt + ".SLDDRW");
                            }
                            else
                                newModName = Path.GetDirectoryName(modName) + "\\" + Path.GetFileNameWithoutExtension(modName) + ".SLDDRW";
                            if (File.Exists(newModName))
                            {
                                DateTime time;
                                if (!DateTime.TryParse(modelDoc2.get_CustomInfo2("", "CreationTime"), out time))
                                {
                                    time = File.GetCreationTime(newModName);
                                    SetModelProperty(modelDoc2, "CreationTime", "", swCustomInfoType_e.swCustomInfoText, time.ToString(), true);
                                }

                                moddrwnum.Add(new ModelDrawingNumber(newModName, modelDoc2, time));
                            }
                            else
                            {
                                if (modelDoc2.get_CustomInfo2("", "Required Draft") == "Yes")
                                {
                                    if (askIfNoDraw)
                                    {
                                        MessageBox.Show(
                                            "Для детали " + modName +
                                            " не был скопирован чертеж при отрыве от библиотеки. Чертеж будет скопирован, а окончательная обработка - продолжена. После окончательной обработки проверьте чертеж дополнительно.",
                                            "Ошибка при копировании чертежа.", MessageBoxButtons.OK);
                                    }
                                    CopyDrawing(modelDoc2);
                                    if (File.Exists(newModName))
                                    {
                                        var time = File.GetCreationTime(newModName);
                                        SetModelProperty(modelDoc2, "CreationTime", "", swCustomInfoType_e.swCustomInfoText, time.ToString(), true);
                                        moddrwnum.Add(new ModelDrawingNumber(newModName, modelDoc2, time));
                                    }
                                }

                            }
                        }

                        moddrwnum.Sort((x, y) => x.Time.CompareTo(y.Time));
                        for (int i = 0; i < moddrwnum.Count; i++)
                        {
                            string drwnum = (i + 1).ToString();
                            var moddrw = moddrwnum[i];
                            #region переименовывания xml

                            //string fieldValue=moddrw.Model.GetCustomInfoValue("", "Sketch Number");
                            //    if (fieldValue != drwnum)
                            //    {
                            //         SetModelProperty(moddrw.Model, "Sketch Number", "", swCustomInfoType_e.swCustomInfoText,
                            //                 drwnum);
                            //        //если свойство было перезаписано, то попытаться найти этот xml, и переименовать его в соответствии с этим SketchNumber
                            //        //сначала попробуем найти xml со старым номером
                            //         string delDir = Path.Combine(Path.GetDirectoryName(SwModel.GetPathName()), "Программы");

                            //        string sketchNumber = fieldValue;
                            //        string orderNumber = moddrw.Model.GetCustomInfoValue("", "Order Number");
                            //        string ret=null;
                            //        if (!(string.IsNullOrEmpty(sketchNumber) || string.IsNullOrEmpty(orderNumber)))
                            //            ret = Path.Combine(delDir, orderNumber + "_" + sketchNumber + ".xml");
                            //        if (ret!=null && File.Exists(ret))
                            //        {
                            //            //если файл существует, проверить , нужная ли в нем модель..
                            //            var currXml = new XmlDocument();
                            //            currXml.Load(ret);
                            //            var modelName = Path.GetFileNameWithoutExtension(currXml.ChildNodes[0].Attributes["Name"].Value);
                            //            if (moddrw.Model.GetPathName().Contains(modelName))
                            //            {
                            //                //в нем нужная модель, переименовываем
                            //                File.Move(ret,ret.Replace("_"+fieldValue,"_"+drwnum));
                            //            }
                            //            else
                            //            {
                            //                //удаляем...
                            //                File.Delete(ret);
                            //            }

                            //        }
                            //         foreach (var file in Directory.GetFiles(delDir, "*.xml", SearchOption.TopDirectoryOnly))
                            //         {
                            //             //прежде чем удалить этот файл 1) вытащить ModelName из xml 2) Если он есть в modelsToDelete = > удалить
                            //             var currXml = new XmlDocument();
                            //             currXml.Load(file);
                            //             var modelName =
                            //                 Path.GetFileNameWithoutExtension(
                            //                     currXml.ChildNodes[0].Attributes["Name"].Value);
                            //         }
                            //    }
                            #endregion
                            SetModelProperty(moddrw.Model, "Sketch Number", "", swCustomInfoType_e.swCustomInfoText, drwnum);
                            if (!drawingDictionary.ContainsKey(Path.GetFileName(moddrw.DrwModel)))
                                drawingDictionary.Add(Path.GetFileName(moddrw.DrwModel), i + 1);
                        }
                        //переименовать xml в соответствии с новыми номерами.
                        string delDir = Path.Combine(Path.GetDirectoryName(SwModel.GetPathName()), "Программы");
                        if (Directory.Exists(delDir))
                        {
                            foreach (var file in Directory.GetFiles(delDir, "*.xml", SearchOption.TopDirectoryOnly).Where(f => f.ToLower().EndsWith("xml")).ToArray())
                            {
                                if (File.Exists(file + "tmp"))
                                    File.Delete(file + "tmp");
                                File.Move(file, file + "tmp");
                            }
                            foreach (var file in Directory.GetFiles(delDir, "*.xmltmp", SearchOption.TopDirectoryOnly))
                            {
                                var currXml = new XmlDocument();
                                currXml.Load(file);
                                var modelName = Path.GetFileNameWithoutExtension(currXml.ChildNodes[0].Attributes["Name"].Value);

                                if (drawingDictionary.ContainsKey(modelName + ".SLDDRW"))
                                {
                                    string firstSubstr = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                                    string newPath = Path.Combine(Path.GetDirectoryName(file), firstSubstr);
                                    if (!File.Exists(newPath + "_" + drawingDictionary[modelName + ".SLDDRW"] + ".xml"))
                                        File.Move(file, newPath + "_" + drawingDictionary[modelName + ".SLDDRW"] + ".xml");//file.Substring(0, file.Length - 8) + drawingDictionary[modelName + ".SLDDRW"] + ".xml");

                                }
                            }
                            foreach (var file in Directory.GetFiles(delDir, "*.xmltmp", SearchOption.TopDirectoryOnly))
                            {
                                File.Delete(file);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(strErr + @"Нумерация чертежей.\n" +
                                        e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }


                    #endregion

                    LinkedList<Component2> swComponents;
                    if (SetAsmUnit(swModel, out swComponents))
                    {
                        foreach (ModelDoc2 model in allModels)
                        {

                            try
                            {
                                #region Quantity property setting

                                ModelDoc2 model1 = model;

                                int modelCnt = swComponents.Select(comp => (ModelDoc2)comp.GetModelDoc()).Where(
                                    compModel =>
                                    compModel != null).Count(
                                        compModel => model1.GetPathName() == compModel.GetPathName());

                                allFindModelsPaths.AddLast(model.GetPathName());

                                SetModelProperty(model, "Quantity", "", swCustomInfoType_e.swCustomInfoText,
                                                 modelCnt.ToString());

                                #endregion

                                SetModelProperty(model, "Order Number", "", swCustomInfoType_e.swCustomInfoText,
                                                 orderName);

                                SetModelProperty(model, "Designer", "", swCustomInfoType_e.swCustomInfoText,
                                                 Properties.Settings.Default.Designer);

                                var namesOfProperties = (string[])model.GetCustomInfoNames();
                                foreach (var namesOfProperty in namesOfProperties)
                                {
                                    if (namesOfProperty.Contains("Color") &&
                                        model.get_CustomInfo2("", namesOfProperty) == namesOfProperty)
                                        SetModelProperty(model, namesOfProperty, "", swCustomInfoType_e.swCustomInfoText,
                                                         "");
                                }
                                #region Sizes properties setting

                                try
                                {
                                    double strObjVal;
                                    OleDbConnection oleDb;
                                    if (OpenModelDatabase(model, out oleDb))
                                    {
                                        var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                                                                                 new object[] { null, null, null, "TABLE" });
                                        if (
                                            oleSchem.Rows.Cast<DataRow>().Any(
                                                row => (string)row["TABLE_NAME"] == "objects"))
                                        {
                                            var command = new OleDbCommand("SELECT * FROM objects ORDER BY id", oleDb);
                                            OleDbDataReader rd = command.ExecuteReader();
                                            while (rd.Read())
                                            {
                                                string strObjName = rd["name"].ToString();
                                                for (int i = 0; i < rd.FieldCount; i++)
                                                //проверка наличия поля TSize в таблицах
                                                {
                                                    if (rd.GetName(i) == "TSize")
                                                    {
                                                        if (GetObjectValue(model, strObjName, (int)rd["type"],
                                                                           out strObjVal))
                                                        {
                                                            string strObjTSize = rd["TSize"].ToString();
                                                            SetModelProperty(model, strObjTSize, "",
                                                                             swCustomInfoType_e.swCustomInfoText,
                                                                             strObjVal.ToString());
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        oleDb.Close();
                                    }
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show(strErr + @"Добавление свойств сборки.\n" +
                                                    e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                }

                                #endregion

                                #region Fibers Directions

                                string compDet = model.IGetActiveConfiguration().Name;
                                bool vertic = (model.get_CustomInfo2(compDet, "FibDir") == "V");
                                bool gorizont = (model.get_CustomInfo2(compDet, "FibDir") == "H");

                                if (vertic || gorizont)
                                {
                                    var dir1 = new DirectoryInfo(GetRootFolder(swModel));
                                    FileInfo[] file =
                                        dir1.GetFiles(Path.GetFileNameWithoutExtension(model.GetPathName()) +
                                                      ".SLDDRW", SearchOption.AllDirectories);


                                    if (file.Length != 0)
                                    {
                                        int errDrw1 = 0;
                                        int wrnDrw1 = 0;
                                        var modelDraw1 = SwApp.OpenDoc6(file[0].FullName,
                                                                        (int)swDocumentTypes_e.swDocDRAWING,
                                                                        (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                                                                        "",
                                                                        ref errDrw1, ref wrnDrw1);
                                        var mpoint =
                                            (MathPoint)(SwApp.IGetMathUtility()).CreatePoint(new[] { 0.5, 0.1, 0 });
                                        double d;
                                        string path;
                                        if (gorizont)
                                        {
                                            d = 1.571;
                                            path = DrwPathResult + @"Блоки\H.SLDBLK";
                                            modelDraw1.SketchManager.MakeSketchBlockFromFile(mpoint, path, false, 0.05,
                                                                                             d);
                                        }
                                        else
                                        {
                                            d = 0;
                                            path = DrwPathResult + @"Блоки\V.SLDBLK";
                                            modelDraw1.SketchManager.MakeSketchBlockFromFile(mpoint, path, false, 0.05,
                                                                                             d);
                                        }
                                        modelDraw1.Save3(
                                            (int)
                                            (swSaveAsOptions_e.swSaveAsOptions_Silent |
                                             swSaveAsOptions_e.swSaveAsOptions_SaveReferenced), ref errDrw1, ref wrnDrw1);

                                        SwApp.CloseDoc(modelDraw1.GetPathName());
                                    }

                                }

                                #endregion
                                if (CheckPropForArticul(model))
                                    listModelsWithArticul.Add(model);
                            }
                            catch
                            {
                                continue;
                            }
                        }


                        #region Quantity = 0

                        try
                        {
                            _allPaths.AddLast(swModel.GetPathName());
                            if (GetAllPathNames(swModel.GetPathName())) // заполняем AllPaths
                            {

                                foreach (string modelPath in _allPaths)
                                {
                                    if (allFindModelsPaths.Find(modelPath) == null) // если модели нет в доступных
                                    {
                                        //тогда для модели ModelPath обнуляем Quantity, все что не используется
                                        SwDmDocumentOpenError oe;
                                        SwDMApplication swDocMgr = GetSwDmApp();

                                        var swDoc = (SwDMDocument)swDocMgr.GetDocument(modelPath,
                                                                                        SwDmDocumentType.
                                                                                            swDmDocumentUnknown,
                                                                                        false,
                                                                                        out oe);
                                        if (oe == SwDmDocumentOpenError.swDmDocumentOpenErrorNone)
                                        {
                                            swDoc.AddCustomProperty("Quantity",
                                                                    SwDmCustomInfoType.swDmCustomInfoText,
                                                                    "0");
                                            swDoc.SetCustomProperty("Quantity", "0");
                                            swDoc.Save();
                                            swDoc.CloseDoc();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(strErr + e.Message, MyTitle, MessageBoxButtons.OK,
                                            MessageBoxIcon.Exclamation);
                        }

                        #endregion
                    }

                    #region Конвертация в DWG И PDF

                    if (Properties.Settings.Default.ConvertToDwgPdf)
                    {
                        var dir = new DirectoryInfo(GetRootFolder(SwApp.IActiveDoc2));
                        int errors = 0, warnings = 0;
                        foreach (var mdn in moddrwnum)
                        {
                            try
                            {
                                var mod = _iSwApp.OpenDoc6(mdn.DrwModel, (int)swDocumentTypes_e.swDocDRAWING,
                                                           (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "",
                                                           ref errors,
                                                           ref warnings);
                                string numb = mdn.Model.get_CustomInfo2("", "Sketch Number");
                                if (numb == "")
                                {
                                    _iSwApp.CloseDoc(mdn.DrwModel);
                                    continue;
                                }

                                string nameWithoutExt = dir.Name + "_" + numb;


                                string path = dir.FullName + "\\ЧЕРТЕЖИ_DWG";
                                if (!Directory.Exists(path))
                                    Directory.CreateDirectory(path);

                                string newDwgName = path + "\\" + nameWithoutExt + ".DWG";
                                if (File.Exists(newDwgName))
                                    File.Delete(newDwgName);
                                mod.SaveAs(newDwgName);

                                path = dir.FullName + "\\ЧЕРТЕЖИ_PDF";
                                if (!Directory.Exists(path))
                                    Directory.CreateDirectory(path);
                                var swExportPDFData =
                                    (ExportPdfData)
                                    SwApp.GetExportFileData((int)swExportDataFileType_e.swExportPdfData);
                                var drawDoc = (DrawingDoc)mod;
                                var oSheets = (string[])drawDoc.GetSheetNames();
                                var objs = new object[oSheets.Count() - 1];
                                for (int i = 0; i < oSheets.Count(); i++)
                                {
                                    drawDoc.ActivateSheet(oSheets[i]);
                                    var sheet = drawDoc.IGetCurrentSheet();
                                    if (i != 0)
                                    {
                                        objs[i - 1] = sheet;

                                        var swViews = (object[])sheet.GetViews();
                                        if (swViews != null)
                                        {
                                            foreach (var t in swViews)
                                            {
                                                var swView = (View)t;
                                                if (!swView.Name.Contains("Чертежный вид") ||
                                                    swView.Name.ToLower().Contains("const") ||
                                                    swView.Type == (int)swDrawingViewTypes_e.swDrawingDetailView)
                                                    continue;
                                                swView.SetDisplayMode3(false, (int)swDisplayMode_e.swHIDDEN_GREYED,
                                                                       false, false);
                                            }
                                        }
                                    }
                                }

                                DispatchWrapper[] dispWrapArr = ObjectArrayToDispatchWrapperArray((objs.ToArray()));
                                swExportPDFData.SetSheets(
                                    (int)swExportDataSheetsToExport_e.swExportData_ExportSpecifiedSheets,
                                    (dispWrapArr));
                                int err = 0, wrn = 0;

                                string newPdfName = path + "\\" + nameWithoutExt + ".pdf";
                                if (File.Exists(newPdfName))
                                    File.Delete(newPdfName);

                                mod.Extension.SaveAs(newPdfName,
                                                     (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                                                     (int)swSaveAsOptions_e.swSaveAsOptions_Silent, swExportPDFData,
                                                     ref err, ref wrn);
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(
                                    @"Ошибка конвертации чертежа " + Path.GetFileNameWithoutExtension(mdn.DrwModel) +
                                    Environment.NewLine + e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            finally
                            {
                                _iSwApp.CloseDoc(mdn.DrwModel);
                            }
                        }
                    }

                    #endregion

                    int error = 0;
                    foreach (var model in listModelsWithArticul)
                    {
                        #region Edge (кромки) изменение свойств при НЕ наличии кромок.
                        string currentConfigName = string.Empty;//model.IGetActiveConfiguration().Name;
                        var actConf = model.IGetActiveConfiguration();
                        string currentConfigName2 = string.Empty;
                        if (actConf != null)
                            currentConfigName2 = actConf.Name;
                        bool hasExtFanerFeats = false;
                        if ((!string.IsNullOrEmpty(model.CustomInfo2["", "ExtFanerFeats"]) && model.CustomInfo2["", "ExtFanerFeats"] == "Yes"))
                            hasExtFanerFeats = true;
                        if (hasExtFanerFeats)
                            FrmEdge.Actualization(model, this);
                        else if (!(!string.IsNullOrEmpty(model.CustomInfo2["", "DoNotRewColorInFP"]) && model.CustomInfo2["", "DoNotRewColorInFP"] == "Yes"))
                        {
                            //сначала актуализировать св-ва FanerXX исходя из того есть ли тела и не подавлены ли они
                            //FrmEdge.Actualization(model,this);
                            //Есть ли хоть одно св-во FanerXX
                            string config11 = string.Empty, config12 = string.Empty, config21 = string.Empty, config22 = string.Empty;
                            string color1 = model.GetCustomInfoValue(currentConfigName, "Color1") as string;
                            bool faner11Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName, "Faner11") as string);
                            if (!faner11Exist && !string.IsNullOrEmpty(currentConfigName2))
                            {
                                faner11Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName2, "Faner11") as string);
                                if (faner11Exist)
                                    config11 = currentConfigName2;
                            }
                            else
                                config11 = currentConfigName;
                            bool faner12Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName, "Faner12") as string);
                            if (!faner12Exist && !string.IsNullOrEmpty(currentConfigName2))
                            {
                                faner12Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName2, "Faner12") as string);
                                if (faner12Exist)
                                    config12 = currentConfigName2;
                            }
                            else
                                config12 = currentConfigName;
                            bool faner21Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName, "Faner21") as string);
                            if (!faner21Exist && !string.IsNullOrEmpty(currentConfigName2))
                            {
                                faner21Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName2, "Faner21") as string);
                                if (faner21Exist)
                                    config21 = currentConfigName2;
                            }
                            else
                                config21 = currentConfigName;
                            bool faner22Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName, "Faner22") as string);
                            if (!faner22Exist && !string.IsNullOrEmpty(currentConfigName2))
                            {
                                faner22Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName2, "Faner22") as string);
                                if (faner22Exist)
                                    config22 = currentConfigName2;
                            }
                            else
                                config22 = currentConfigName;
                            if (!(faner11Exist || faner12Exist || faner21Exist || faner22Exist))
                            {
                                continue;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(color1))
                                {
                                    MessageBox.Show("Ошибка при обработки декоров кромок модели! В модели " + model.GetPathName() + " нет свойства Color1 которое указывает декор детали. Назначте в РПД декор данной детали повторно.");
                                    Logging.Log.Instance.Fatal("Ошибка при обработки декоров кромок модели: " + model.GetPathName());
                                    continue;
                                }
                            }
                            if (faner11Exist)
                                if (model.GetCustomInfoValue(config11, "Faner11") as string != "Нет")
                                {
                                    //faner11Exist = !string.IsNullOrEmpty(model.GetCustomInfoValue(currentConfigName, "colorFaner11") as string);
                                    //if (!faner11Exist)
                                    SetModelProperty(model, "colorFaner11", config11, swCustomInfoType_e.swCustomInfoText, color1.Substring(0, 2), true);
                                }
                            if (faner12Exist)
                                if (model.GetCustomInfoValue(config12, "Faner12") as string != "Нет")
                                {
                                    SetModelProperty(model, "colorFaner12", config12, swCustomInfoType_e.swCustomInfoText, color1.Substring(0, 2), true);
                                }
                            if (faner21Exist)
                                if (model.GetCustomInfoValue(config21, "Faner21") as string != "Нет")
                                {
                                    SetModelProperty(model, "colorFaner21", config21, swCustomInfoType_e.swCustomInfoText, color1.Substring(0, 2), true);
                                }
                            if (faner22Exist)
                                if (model.GetCustomInfoValue(config22, "Faner22") as string != "Нет")
                                {
                                    SetModelProperty(model, "colorFaner22", config22, swCustomInfoType_e.swCustomInfoText, color1.Substring(0, 2), true);
                                }
                        }
                        #endregion
                    }

                    #region Saving
                    try
                    {
                        int err2 = 0;
                        int wrn2 = 0;
                        if (swModel.Save3(
                            (int)
                            (swSaveAsOptions_e.swSaveAsOptions_Silent |
                             swSaveAsOptions_e.swSaveAsOptions_SaveReferenced), ref err2, ref wrn2))
                            swModel.SetSaveFlag();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(strErr + @"Ошибка сохранения." + Environment.NewLine +
                                        e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    #endregion

                    #region Проверка на эквивалентность свойств документа в менеджере и солиде
                    foreach (var model in listModelsWithArticul)
                    {
                        SwDMApplication swDocMgr = GetSwDmApp();
                        SwDmCustomInfoType typ;
                        SwDmDocumentOpenError oe;
                        try
                        {
                            var swDoc = swDocMgr.GetDocument(model.GetPathName(), SwDmDocumentType.swDmDocumentUnknown, true, out oe);


                            if (swDoc != null)
                            {
                                var comPropnames = (string[])swDoc.GetCustomPropertyNames();
                                string linkto;
                                if (comPropnames != null)
                                    foreach (var comPropname in comPropnames)
                                    {
                                        string val = ((ISwDMDocument5)swDoc).GetCustomPropertyValues(comPropname,
                                                                                                      out typ,
                                                                                                      out linkto);

                                        if (val != model.GetCustomInfoValue("", comPropname))
                                        {
                                            if (!askIfNoDraw)
                                            {
                                                string texterr =
                                                    "Окончательная обработка будет прервана, т.к. обнаружена внутренняя ошибка SW. Сохранитесь, перезапустите SW и снова сделайте окончательную обработку.";
                                                MessageBox.Show(texterr);
                                                return;
                                            }
                                            var m =
                                                    (ModelDoc2)SwApp.ActivateDoc2(model.GetPathName(), true, ref error);

                                            m.Save();

                                            SwApp.CloseDoc(m.GetPathName());


                                        }
                                    }
                                if (swDoc.ConfigurationManager.GetConfigurationCount() > 1)
                                {
                                    var nameConf = swDoc.ConfigurationManager.GetActiveConfigurationName();

                                    var conf =
                                        (SwDMConfiguration5)
                                        swDoc.ConfigurationManager.GetConfigurationByName(nameConf);
                                    var confPropnames = (string[])conf.GetCustomPropertyNames();

                                    if (confPropnames != null)
                                        foreach (var name in confPropnames)
                                        {

                                            string val = conf.GetCustomPropertyValues(name, out typ, out linkto);

                                            if (val != model.GetCustomInfoValue(nameConf, name))
                                            {
                                                if (!askIfNoDraw)
                                                {
                                                    string texterr = "Окончательная обработка будет прервана, т.к. обнаружена внутренняя ошибка SW. Сохранитесь, перезапустите SW и снова сделайте окончательную обработку.";
                                                    MessageBox.Show(texterr);
                                                    return;
                                                }

                                                var m =
                                                    (ModelDoc2)
                                                    SwApp.ActivateDoc2(model.GetPathName(), true, ref error);

                                                m.Save();

                                                SwApp.CloseDoc(m.GetPathName());


                                            }
                                        }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    #endregion

                    #region генерация программ

                    //if (Properties.Settings.Default.CreateProgramForLathe)
                    //    foreach (var dD in drawingDictionary)
                    //    {
                    //        string orderNumber = GetOrderName(swModel);
                    //        var pathProgramm = Path.GetDirectoryName(swModel.GetPathName()) + @"\Программы\" + dD.Key +
                    //                           ".xml";
                    //        string newPath = Path.GetDirectoryName(swModel.GetPathName()) + @"\Программы\" + orderNumber +
                    //                         "_" +
                    //                         dD.Value + ".xml";

                    //        if (!File.Exists(pathProgramm))
                    //        {
                    //        }
                    //        else
                    //        {
                    //            if (File.Exists(newPath))
                    //                File.Delete(newPath);
                    //            new FileInfo(pathProgramm).MoveTo(newPath);
                    //        }
                    //    }

                    #endregion

                    #region Загрузка на сервер

                    //try
                    //{
                    //    string pathagree = GetRootFolder(swModel);
                    //    string numagree = Path.GetFileName(pathagree);

                    //    byte[] bytes =
                    //        Encoding.Default.GetBytes("NUMAGREE=" + numagree + "&PATHAGREE=" + pathagree +
                    //                                  "&STATE=GETITAGREEPATH");
                    //    string content = SendDataOnServer(bytes);

                    //    content = content.Substring(0, content.Length - 1);

                    //    if (content.ToLower() != "error")
                    //    {
                    //        bytes = Encoding.Default.GetBytes("ITAGREEPATH=" + content + "&STATE=FILEDATACLEAR");
                    //        SendDataOnServer(bytes);
                    //    }
                    //    foreach (var model in listModelsWithArticul)
                    //    {
                    //        string path = Path.GetDirectoryName(model.GetPathName()) + "\\";
                    //        string name = Path.GetFileName(model.GetPathName());
                    //        string typeOfDoc = model.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY
                    //                               ? "swDocASSEMBLY"
                    //                               : "swDocPART";
                    //        string uploadString = "STATE=FILEDATAADD&ITAGREEPATH=" + content + "&IDFILETYPE=" +
                    //                              typeOfDoc + "&FILEPATH=" + path + "&FILENAME=" + name;
                    //        var nameConfig = model.IGetActiveConfiguration().Name;
                    //        var namesComProp = (string[])model.GetCustomInfoNames2("");
                    //        foreach (var nameComProp in namesComProp)
                    //        {
                    //            try
                    //            {
                    //                var swType = model.GetCustomInfoType3("", nameComProp);
                    //                string val = model.GetCustomInfoValue("", nameComProp);
                    //                if (swType == (int)swCustomInfoType_e.swCustomInfoYesOrNo)
                    //                {
                    //                    val = val == "Yes" ? "T" : "F";
                    //                }
                    //                bytes =
                    //                    Encoding.Default.GetBytes("Деталь: " + Path.GetFileName(model.GetPathName()) + " Конфигурация: " + " Свойство: " + nameComProp + " Значение: " + val);//uploadString + "&IDFILEATTRIBUTE=" + nameComProp +"&ATTRIBUTEVALUE=" + val);

                    //                SendDataOnServer(bytes);
                    //            }
                    //            catch
                    //            {
                    //                continue;
                    //            }
                    //        }
                    //        var namesConfProp = model.GetCustomInfoNames2(nameConfig);
                    //        foreach (var nameConfProp in namesConfProp)
                    //        {
                    //            try
                    //            {
                    //                var swType =
                    //                    model.Extension.get_CustomPropertyManager(nameConfig).GetType2(nameConfProp);
                    //                string val, resVal;
                    //                if (model.Extension.get_CustomPropertyManager(nameConfig).Get4(nameConfProp, true,
                    //                                                                               out val, out resVal))
                    //                {
                    //                    if (swType == (int)swCustomInfoType_e.swCustomInfoYesOrNo)
                    //                    {
                    //                        val = val == "Yes" ? "T" : "F";
                    //                    }
                    //                    bytes =
                    //                        Encoding.Default.GetBytes("Деталь: " + Path.GetFileName(model.GetPathName()) + " Конфигурация: " + nameConfig + " Свойство: " + nameConfProp + " Значение: " + val);//uploadString + "&IDFILEATTRIBUTE=" + nameConfProp +"&ATTRIBUTEVALUE=" + val);
                    //                    SendDataOnServer(bytes);
                    //                }
                    //            }
                    //            catch
                    //            {
                    //                continue;
                    //            }
                    //        }
                    //    }
                    //}
                    //catch
                    //{
                    //}

                    #endregion

                }



                string s = SwModel.GetPathName();
                string directoryPath = s.Substring(0, s.LastIndexOf("\\") + 1);
                string newDictFileName = directoryPath + "fpTime.txt";
                var sw = File.CreateText(newDictFileName);
                var culture = CultureInfo.CreateSpecificCulture("ru-RU");
                sw.WriteLine(DateTime.Now.AddSeconds(3).ToString(culture));//Добавляем 3 сек потому что Игорь не может получить точную дату изменения файла.
                sw.Close();
                int er = 0;
                var tmp = (ModelDoc2)SwApp.ActivateDoc2(s, true, ref er);
                if (tmp != null)
                {
                    var myModelView = (IModelView)tmp.GetFirstModelView();
                    myModelView.FrameState = (int)swWindowState_e.swWindowMaximized;

                }

            }
            catch (Exception e)
            {

                MessageBox.Show(strErr + e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                //для всех чертежей в папке Детали (вкл. вложенные) вытащить те модели для которых нужен xml
                ProgressBar.WaitTime.Instance.HideWait();
                try
                {
                    SwDMApplication swDocMgr = GetSwDmApp();
                    if (SwModel == null)
                        SwModel = (ModelDoc2)_iSwApp.ActiveDoc;
                    string partDir = Path.Combine(Path.GetDirectoryName(SwModel.GetPathName()), "Детали");
                    string xmlDir = Path.Combine(Path.GetDirectoryName(SwModel.GetPathName()), "Программы");
                    SwDmDocumentOpenError oe;
                    SwDmCustomInfoType swDm;
                    List<string> drawThatNeedXml = new List<string>();
                    List<string> xmlThatExist = new List<string>();
                    List<string> resultList = new List<string>();
                    if (Directory.Exists(partDir))
                    {
                        foreach (var file in Directory.GetFiles(partDir, "*.slddrw", SearchOption.AllDirectories))
                        {

                            //вытащить свойство MakeCNCprog и было ли образмеренно!
                            var swDoc1 =
                                (SwDMDocument)
                                swDocMgr.GetDocument(file, SwDmDocumentType.swDmDocumentDrawing, true, out oe);
                            if (swDoc1 != null)
                            {
                                var names = (string[])swDoc1.GetCustomPropertyNames();
                                if (names != null && names.Contains("MakeCNCprog") && swDoc1.GetCustomProperty("MakeCNCprog", out swDm) == "Yes" && names.Contains("WasMesure") && swDoc1.GetCustomProperty("WasMesure", out swDm) == "Yes")
                                {
                                    drawThatNeedXml.Add(Path.GetFileNameWithoutExtension(file));
                                }
                                swDoc1.CloseDoc();
                            }


                        }
                    }

                    if (Directory.Exists(xmlDir))
                    {
                        foreach (var file in Directory.GetFiles(xmlDir, "*.xml", SearchOption.TopDirectoryOnly))
                        {
                            var currXml = new XmlDocument();
                            currXml.Load(file);
                            if (currXml != null && currXml.ChildNodes.Count == 0)
                            {
                                File.Delete(file);
                                continue;
                            }
                            if (currXml != null && currXml.ChildNodes[0] != null && currXml.ChildNodes[0].Attributes["Name"] != null && currXml.ChildNodes[0].Attributes["Name"].Value != null)
                            {
                                var modelName =
                                    Path.GetFileNameWithoutExtension(currXml.ChildNodes[0].Attributes["Name"].Value);
                                xmlThatExist.Add(modelName);
                            }
                        }
                    }
                    foreach (var draw in drawThatNeedXml)
                    {
                        if (xmlThatExist.Contains(draw))
                            continue;
                        resultList.Add(draw);
                    }

                    if (resultList.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        string orderNumber = Path.GetFileNameWithoutExtension(SwModel.GetPathName());
                        foreach (var s in resultList)
                        {
                            sb.Append(s);
                            sb.Append(Environment.NewLine);
                        }
                        Logging.Log.Instance.SendMail(sb.ToString(), "no xml error", orderNumber);
                        //frmShowList frmShow = new frmShowList(resultList, "Внимание!", "Следующие детали должны иметь xml в папке \"Программы\":", "Если xml на них не будет,то импорт в \"Покупки\" может пройти неудачно. Для того, чтобы получить xml для этих чертежей", " надо произвести их образмеривание. Если и в этом случае xml не сформировался, обратитесь в методический отдел.");
                        //frmShow.Show();
                    }
                }
                catch (Exception e)
                {
                    Logging.Log.Instance.Fatal(e, "ошибка при отправке письма при обнаружении отсутствующих xml");
                }
            }
        }

        private bool CheckPropForArticul(ModelDoc2 model)
        {
            bool ret = false;
            var commonPropNames = (string[])model.GetCustomInfoNames();
            foreach (var commonPropName in commonPropNames)
            {
                if (commonPropName.ToLower() == "articul" && model.GetCustomInfoValue("", commonPropName) != "")
                    ret = true;
            }
            if (model.GetConfigurationCount() >= 1)
            {
                string confName = model.IGetActiveConfiguration().Name;
                var confPropNames = (string[])model.GetCustomInfoNames2(confName);
                foreach (var confPropName in confPropNames)
                {
                    if (confPropName.ToLower() == "articul" && model.GetCustomInfoValue(confName, confPropName) != "")
                        ret = true;
                }
            }
            return ret;
        }

        private bool CheckSuffixModel(Component2 comp, string suffix, out bool notModel)
        {
            notModel = false;
            try
            {
                string tmp = Path.GetFileNameWithoutExtension(comp.GetPathName());
                if (tmp.Substring(tmp.Length - 4, 4)[0] == '#' && (tmp.Substring(tmp.Length - 4, 4)[3] == 'P' || tmp.Substring(tmp.Length - 4, 4)[3] == 'p'))
                {
                    return false;
                }
                var model = comp.IGetModelDoc();
                if (model != null)
                {
                    if (Properties.Settings.Default.CashModeOn && model.get_CustomInfo2("", "Accessories") == "Yes")
                        return false;
                    if (!Path.GetFileNameWithoutExtension(model.GetPathName()).Contains(suffix))
                    {
                        if (model.GetConfigurationCount() == 1 && model.get_CustomInfo2("", "Articul") != "")
                            return true;

                        var configNames = (string[])model.GetConfigurationNames();
                        if (configNames.Any(configName => model.get_CustomInfo2(configName, "Articul") != ""))
                            return true;

                        var outComps = new LinkedList<Component2>();
                        bool val;
                        if (GetComponents(comp, outComps, false, false) &&
                            outComps.Any(component2 => CheckSuffixModel(component2, suffix, out val)))
                            return true;
                    }
                    else
                    {
                        if (model.GetType() != (int)swDocumentTypes_e.swDocASSEMBLY)
                            return false;

                        var comps = (object[])((AssemblyDoc)model).GetComponents(false);
                        return
                            (from Component2 c in comps select Path.GetFileNameWithoutExtension(c.GetPathName())).
                                Any(
                                    name => !name.Contains(suffix));

                    }
                }
                else
                {
                    SwDmDocumentOpenError oe;

                    if (!Path.GetFileNameWithoutExtension(comp.GetPathName()).Contains(suffix))
                    {
                        SwDMApplication swDocMgr = GetSwDmApp();
                        var swDoc = (SwDMDocument)swDocMgr.GetDocument(comp.GetPathName(),
                                                                        SwDmDocumentType.swDmDocumentAssembly, true,
                                                                        out oe);
                        if (swDoc != null)
                        {
                            SwDmCustomInfoType swDm;
                            if (swDoc.ConfigurationManager.GetConfigurationCount() == 1)
                            {
                                var names = (string[])swDoc.GetCustomPropertyNames();
                                if (names.Contains("Articul") && swDoc.GetCustomProperty("Articul", out swDm) != "")
                                    return true;
                            }
                            else
                            {
                                var names = (string[])swDoc.ConfigurationManager.GetConfigurationNames();
                                if ((from name in names
                                     select (SwDMConfiguration)swDoc.ConfigurationManager.GetConfigurationByName(name)
                                         into conf
                                         let nms = (string[])conf.GetCustomPropertyNames()
                                         where nms.Contains("Articul") && conf.GetCustomProperty("Articul", out swDm) != ""
                                         select conf).Any())
                                    return true;
                            }
                        }
                        else
                        {
                            notModel = true;
                            return true;
                        }
                    }
                    else
                    {
                        var outList = new LinkedList<Component2>();
                        if (GetComponents(comp, outList, true, false))
                        {
                            return
                                (from Component2 c in outList select Path.GetFileNameWithoutExtension(c.GetPathName())).
                                    Any(
                                        name => !name.Contains(suffix));
                        }
                    }
                }
            }
            catch
            {
                notModel = true;
                return true;
            }
            return false;
        }

        private static DispatchWrapper[] ObjectArrayToDispatchWrapperArray(object[] objects)
        {
            int arraySize = objects.GetUpperBound(0);

            var d = new DispatchWrapper[arraySize + 1];

            for (int arrayIndex = 0; arrayIndex <= arraySize; arrayIndex++)
            {
                d[arrayIndex] = new DispatchWrapper(objects[arrayIndex]);
            }
            return d;
        }

        private void DeleteUnusedModels(string strFolder, IEnumerable<string> allPaths)
        {
            string[] modelNames = Directory.GetFiles(strFolder, "*.SLD???");
            foreach (string t in modelNames)
            {
                if ((File.GetAttributes(t) & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    string modExt = Path.GetExtension(t).ToLower();
                    if (modExt == ".sldasm" || modExt == ".sldprt" || modExt == ".slddrw")
                    {
                        bool isDrawing = (modExt == ".slddrw");
                        string nameWoExt = Path.GetFileNameWithoutExtension(t).ToLower();
                        string t1 = t;
                        bool isUsedFile =
                            allPaths.Any(
                                pathOfCom =>
                                (isDrawing && (Path.GetFileNameWithoutExtension(pathOfCom).ToLower() == nameWoExt)) ||
                                (!isDrawing && (pathOfCom.ToLower() == t1.ToLower())));
                        if (!isUsedFile)
                        {
                            try
                            {
                                if (t.Contains(Path.GetFileNameWithoutExtension(SwModel.GetPathName()) + ".SLDDRW"))
                                    continue;
                                File.Delete(t);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            string[] subFolders = Directory.GetDirectories(strFolder);
            foreach (string t in subFolders)
            {
                DeleteUnusedModels(t, allPaths);
            }
        }

        private bool GetAllPathNames(string mainPath)
        {
            try
            {
                SwDmDocumentOpenError oe;
                object brokenRefVar;
                SwDMApplication swDocMgr = GetSwDmApp();

                var swDoc =
                    (SwDMDocument8)swDocMgr.GetDocument(mainPath, SwDmDocumentType.swDmDocumentUnknown, true, out oe);
                SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
                var varRef = (string[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                if (varRef == null) return true;

                foreach (string names in varRef)
                {
                    if (File.Exists(names))
                    {
                        string names1 = names;
                        bool isAlredyAdd = _allPaths.Any(i => i == names1);
                        if (!isAlredyAdd)
                        {
                            _allPaths.AddLast(names);
                            GetAllPathNames(names);
                        }
                    }
                }
                swDoc.CloseDoc();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

        }

        #endregion

        #region Add Holes

        private bool GetSelectedView(out View swView)
        {
            bool ret = false;
            swView = null;

            SelectionMgr swSelMgr = SwModel.ISelectionManager;
            if (swSelMgr.GetSelectedObjectCount() == 1)
            {
                if (swSelMgr.GetSelectedObjectType3(1, 0) == (int)swSelectType_e.swSelDRAWINGVIEWS)
                {
                    swView = (View)swSelMgr.GetSelectedObject6(1, 0);
                    ret = true;
                }
            }
            return ret;
        }

        public int AddHolesSymbolsEnable()
        {
            View swView;
            return GetSelectedView(out swView) ? 1 : 0;
        }

        public void AddHolesSymbols()
        {
            try
            {
                View swView;
                if (GetSelectedView(out swView))
                {
                    SwModel.ClearSelection();

                    var swBDefs = (object[])SwModel.SketchManager.GetSketchBlockDefinitions();
                    if (swBDefs != null)
                    {
                        foreach (object t in swBDefs)
                        {
                            var swSbDef = (SketchBlockDefinition)t;
                            var swBInsts = (object[])swSbDef.GetInstances();
                            if (swBInsts != null)
                            {
                                foreach (object t1 in swBInsts)
                                {
                                    var swSbInst = (SketchBlockInstance)t1;
                                    if (((Feature)swSbInst.GetSketch()).Name == ((Feature)swView.GetSketch()).Name)
                                    {
                                        swSbInst.Select(true, null);
                                    }
                                }
                            }
                        }
                    }

                    SwModel.DeleteSelection(false);

                    var drwHoles = new LinkedList<DrawingHole>();

                    object plinfoobj;
                    var edges = (object[])swView.GetPolylines7(0, out plinfoobj);
                    var plinfo = (double[])plinfoobj;
                    int plinfoidx = 0;

                    foreach (object t in edges)
                    {
                        Curve crv = null;
                        Face2 swFace = null;
                        Surface swSurface = null;

                        try
                        {
                            var swEdge = (Edge)t;
                            crv = swEdge.IGetCurve();

                            var faces = (object[])swEdge.GetTwoAdjacentFaces2();
                            swFace = (Face2)faces[0];

                            swSurface = (Surface)swFace.GetSurface();
                            if (!swSurface.IsCylinder())
                            {
                                swFace = (Face2)faces[1];
                                swSurface = (Surface)swFace.GetSurface();
                            }
                        }
                        catch
                        {
                            var swSEdge = (SilhouetteEdge)t;
                            if (swSEdge != null)
                            {
                                crv = swSEdge.GetCurve();
                                swFace = swSEdge.GetFace();
                                swSurface = (Surface)swFace.GetSurface();
                            }
                        }
                        if (swFace != null)
                        {
                            if (crv.IsCircle())
                            {
                                var curveprms = (double[])crv.CircleParams;
                                double holeradius = curveprms[6];

                                if (swSurface.IsCylinder())
                                {
                                    var swFeature = (Feature)swFace.GetFeature();
                                    if (swFeature.GetTypeName2().ToLower() == "cut" ||
                                        swFeature.GetTypeName2().ToLower() == "ice")
                                    {
                                        var swExtrData = (ExtrudeFeatureData2)swFeature.GetDefinition();
                                        double holedepth = swExtrData.GetDepth(true);
                                        var newhole = new DrawingHole(crv, holeradius, holedepth,
                                                                      plinfo[plinfoidx + 2], plinfo[plinfoidx + 3],
                                                                      plinfo[plinfoidx + 4]);
                                        drwHoles.AddLast(newhole);
                                    }
                                }
                            }
                        }
                        plinfoidx += 2 + (int)plinfo[plinfoidx + 1] + 7 +
                                     (int)plinfo[plinfoidx + 2 + (int)plinfo[plinfoidx + 1] + 6] * 3;
                    }
                    if (drwHoles.Count > 0)
                    {
                        var drwHolesArr = new DrawingHole[drwHoles.Count];

                        int holeidx = 0;
                        foreach (DrawingHole drwhole in drwHoles)
                        {
                            drwHolesArr[holeidx] = drwhole;
                            holeidx++;
                        }
                        string holeFileName = DrwPathResult + "holes.txt";
                        var drwHoleBlocks = new LinkedList<DrawingHoleBlock>();

                        if (File.Exists(holeFileName))
                        {
                            var reader = new StreamReader(holeFileName, Encoding.GetEncoding(1251));
                            string line;

                            while ((line = reader.ReadLine()) != null)
                            {
                                string[] linearr = line.Split('\t');

                                try
                                {
                                    drwHoleBlocks.AddLast(
                                        new DrawingHoleBlock(DrwPathResult + linearr[2],
                                                             Convert.ToDouble(linearr[0]) / 2000,
                                                             Convert.ToDouble(linearr[1]) / 1000));
                                }
                                catch
                                {
                                }
                            }
                            reader.Close();
                        }

                        var drwHoleBlocksArr = new DrawingHoleBlock[drwHoleBlocks.Count];
                        drwHoleBlocks.CopyTo(drwHoleBlocksArr, 0);

                        string[] blocknames = Directory.GetFiles(DrwPathResult, "*.SLDBLK");
                        int blockidx = 0;

                        for (int i = 0; i < drwHolesArr.Length; i++)
                        {
                            if (!drwHolesArr[i].IsProcessed)
                            {
                                string blockname = "";

                                foreach (DrawingHoleBlock t in drwHoleBlocksArr)
                                {
                                    if (t.Radius == drwHolesArr[i].Radius &&
                                        t.Depth == drwHolesArr[i].Depth)
                                    {
                                        blockname = t.BlockFileName;
                                        break;
                                    }
                                }
                                if (blockname == "")
                                {
                                    while (blocknames.Length > blockidx)
                                    {
                                        blockname = blocknames[blockidx];
                                        blockidx++;

                                        string blockname1 = blockname;
                                        if (drwHoleBlocksArr.Any(t => t.BlockFileName.ToLower() == blockname1.ToLower()))
                                        {
                                            blockname = "";
                                        }
                                        if (blockname != "")
                                            break;
                                    }
                                }
                                ProcessHole(swView, drwHolesArr[i], blockname);

                                for (int j = i + 1; j < drwHolesArr.Length; j++)
                                {
                                    if (!drwHolesArr[j].IsProcessed && drwHolesArr[i].Radius == drwHolesArr[j].Radius &&
                                        drwHolesArr[i].Depth == drwHolesArr[j].Depth)
                                    {
                                        ProcessHole(swView, drwHolesArr[j], blockname);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void ProcessHole(View swView, DrawingHole drwhole, string blkname)
        {
            var mpoint = (MathPoint)(_iSwApp.IGetMathUtility()).CreatePoint(
                new[] { drwhole.Cx, drwhole.Cy, drwhole.Cz });

            if (blkname != "")
            {
                SketchBlockDefinition swSbDef = SwModel.SketchManager.MakeSketchBlockFromFile(mpoint, blkname, false, 1,
                                                                                              0);

                if (swSbDef == null)
                {
                    var swBDefs = (object[])SwModel.SketchManager.GetSketchBlockDefinitions();
                    if (swBDefs != null)
                    {
                        foreach (var swTestSbDef in
                            swBDefs.Cast<SketchBlockDefinition>().Where(swTestSbDef => swTestSbDef.FileName == blkname))
                        {
                            swSbDef = swTestSbDef;
                            break;
                        }
                    }
                }

                if (swSbDef != null)
                {
                    var swBInsts = (object[])swSbDef.GetInstances();
                    if (swBInsts != null)
                    {
                        foreach (object t in swBInsts)
                        {
                            var swSbInst = (SketchBlockInstance)t;
                            if (((Feature)swSbInst.GetSketch()).Name == ((Feature)swView.GetSketch()).Name)
                            {
                                swSbInst.Scale2 = drwhole.Radius * 2 * swView.ScaleDecimal * 1000;
                            }
                        }
                    }
                }
            }
            drwhole.IsProcessed = true;
        }

        #endregion

        #region Auto Scale for Drawing

        public int AutoScaleDrawingEnable()
        {
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void AutoScaleDrawing()
        {
            try
            {
                var swDraw = (DrawingDoc)SwModel;
                var swSheet = (Sheet)swDraw.GetCurrentSheet();
                var swViews = (object[])swSheet.GetViews();

                double shWidth = 0, shHeight = 0;

                if (CheckDrawingForIntersection(ref swViews))
                {
                    foreach (object t in swViews)
                    {
                        var swView = (View)t;

                        var xf1 = (double[])swView.GetOutline();
                        var pos = (double[])swView.Position;

                        bool isleft = xf1[0] < shWidth - xf1[2];

                        pos[0] = isleft ? (xf1[2] - xf1[0]) / 2 + 0.02 : shWidth - (xf1[2] - xf1[0]) / 2 - 0.02;
                        pos[1] = xf1[1] < shHeight - xf1[3]
                                     ? (xf1[3] - xf1[1]) / 2 + (isleft ? 0.02 : 0.06)
                                     : shHeight - (xf1[3] - xf1[1]) / 2 - 0.02;
                        swView.Position = pos;
                    }
                }

                bool isIntersection;
                int scaleChangingCnt = 0;

                do
                {
                    isIntersection = CheckDrawingForIntersection(ref swViews);

                    if (isIntersection)
                    {
                        foreach (object t in swViews)
                        {
                            var swView = (View)t;
                            if (swView.Name.Substring(0, 1) != "*")
                            {
                                swView.ScaleDecimal = swView.ScaleDecimal * 0.9;
                            }
                        }
                    }
                    scaleChangingCnt++;
                } while (isIntersection && scaleChangingCnt < 10);

                SwModel.GraphicsRedraw2();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static bool CheckDrawingForIntersection(ref object[] swViews)
        {
            bool ret = false;

            for (int i = 0; i < swViews.Length; i++)
            {
                var swView = (View)swViews[i];
                if (swView.Name.Substring(0, 1) != "*")
                {
                    var xf1 = (double[])swView.GetOutline();

                    xf1[0] -= 0.01;
                    xf1[1] -= 0.01;
                    xf1[2] += 0.01;
                    xf1[3] += 0.01;

                    for (int j = 0; j < swViews.Length; j++)
                    {
                        if (j != i)
                        {
                            var swView2 = (View)swViews[j];
                            if (swView2.Name.Substring(0, 1) != "*")
                            {
                                var xf2 = (double[])swView2.GetOutline();

                                xf2[0] -= 0.01;
                                xf2[1] -= 0.01;
                                xf2[2] += 0.01;
                                xf2[3] += 0.01;

                                if (((xf1[0] >= xf2[0] && xf1[0] <= xf2[2]) || (xf1[2] >= xf2[0] && xf1[2] <= xf2[2]) ||
                                     (xf2[0] >= xf1[0] && xf2[0] <= xf1[2]) || (xf2[2] >= xf1[0] && xf2[2] <= xf1[2])) &&
                                    ((xf1[1] >= xf2[1] && xf1[1] <= xf2[3]) || (xf1[3] >= xf2[1] && xf1[3] <= xf2[3]) ||
                                     (xf2[1] >= xf1[1] && xf2[1] <= xf1[3]) || (xf2[3] >= xf1[1] && xf2[3] <= xf1[3])))
                                {
                                    ret = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (ret)
                    break;
            }
            return ret;
        }

        #endregion

        #region AutoDimension and RemoveAll

        public int AutoDimensionDrawingEnable()
        {
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void AutoDimensionDrawing()
        {
            IsEventsEnabled = false;

            using (var dimDraft = new DimensionDraft(this))
            {
                try
                {
                    dimDraft.AutoDimensionDrawing(true);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    throw;
                }
            }

            IsEventsEnabled = true;
            return;
        }

        public int DimensionAllEnable()
        {
            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void DimensionAll()
        {
            //if (!CheckForTearOff())
            //{
            //    MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
            //    return;
            //}
            IsEventsEnabled = false;
            var dir = new DirectoryInfo(GetRootFolder(SwApp.IActiveDoc2));
            if (Properties.Settings.Default.CashModeOn)
            {
                LinkedList<ModelDoc2> models;
                GetAllUniqueModels(SwModel, out models);
                LinkedList<string> modelPaths = new LinkedList<string>();
                foreach (var modelDoc2 in models)
                {
                    modelPaths.AddLast(Path.GetFileNameWithoutExtension(modelDoc2.GetPathName()));
                }
                List<FileInfo> drawToDelete = new List<FileInfo>();
                foreach (FileInfo file in dir.GetFiles("*.SLDDRW", SearchOption.AllDirectories))
                {
                    if (!modelPaths.Contains(Path.GetFileNameWithoutExtension(file.Name)))
                        drawToDelete.Add(file);
                }
                foreach (var fileInfo in drawToDelete)
                {
                    fileInfo.Delete();
                }
            }
            var allNeedleFiles = dir.GetFiles("*.SLDDRW", SearchOption.AllDirectories).Where(
                file => Path.GetFileNameWithoutExtension(file.FullName) != dir.Name);
            int count = allNeedleFiles.Count();
            UserProgressBar pb;
            SwApp.GetUserProgressBar(out pb);
            pb.Start(0, count, "Образмеривание");
            int i = 0;
            foreach (var swModel in allNeedleFiles.Select(
                file => (ModelDoc2)SwApp.OpenDoc(file.FullName, (int)swDocumentTypes_e.swDocDRAWING)).Where(
                    swModel => swModel != null))
            {
                using (var dimDraft = new DimensionDraft(this))
                {
                    try
                    {
                        dimDraft.AutoDimensionDrawing(false);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        break;
                    }
                    if (dimDraft.isValidXml)
                        swModel.Save();
                }
                SwApp.CloseDoc(swModel.GetPathName());
                i++;
                pb.UpdateProgress(i);
            }
            pb.End();

            if (MessageBox.Show(@"Образмеривание завершено! Открыть все чертежи?", MyTitle, MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                OpenDrawings();
                IsEventsEnabled = true;
            }
            else
            {
                IsEventsEnabled = true;
                SwApp.ActivateDoc(SwModel.GetPathName());
                return;
            }
            return;
        }

        public int DeleteSketchDemensionsEnable()
        {
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }
        public void DeleteSketchDemensions()
        {
            DeleteSketchDemensions(true);
        }
        public void DeleteSketchDemensions(bool many)
        {
            var swModel = SwApp.IActiveDoc2;
            //if (swModel.GetCustomInfoValue("", "AutoDim") == "No")
            //{
            //    if (many)
            //        MessageBox.Show(@"Удаление размеров невозможно!Если Вы все же хотите сделать это, смените No на Yes в поле 'AutoDim' свойств данного чертежа",MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}
            var swDrawing = (DrawingDoc)swModel;
            var swSelData = swModel.ISelectionManager.CreateSelectData();
            var objSheetNames = (object[])swDrawing.GetSheetNames();
            foreach (var swSheetName in objSheetNames.Cast<string>())
            {
                swDrawing.ActivateSheet(swSheetName);
                var swSheet = (Sheet)swDrawing.GetCurrentSheet();
                var swViews = (object[])swSheet.GetViews();
                if (swViews != null)
                {
                    foreach (var swView in swViews.Cast<View>())
                    {
                        const string expr = "^F[1-6]$";

                        Match isMatch = Regex.Match(swView.Name, expr, RegexOptions.IgnoreCase);
                        if ((!(isMatch.Success || swView.Name.Contains("Чертежный вид"))) ||
                            (swView.Name.ToLower().Contains("const")) ||
                            (swView.Type == (int)swDrawingViewTypes_e.swDrawingDetailView)) continue;
                        swDrawing.ActivateView(swView.Name);
                        var sketch = swView.IGetSketch();
                        var objSketchSeg = sketch.GetSketchSegments();
                        if (objSketchSeg != null)
                        {
                            var objSketSegs = (object[])objSketchSeg;
                            foreach (var objSketSeg in objSketSegs)
                            {
                                var swSketSeg = (SketchSegment)objSketSeg;
                                swSketSeg.Select(true);
                            }
                            swModel.DeleteSelection(false);
                        }
                        object objDimName = null;
                        try
                        {
                            objDimName = swView.GetDimensionIds4();
                        }
                        catch (Exception)
                        {
                        }



                        var swSelMgr = swModel.ISelectionManager;


                        if (objDimName != null)
                        {
                            var objDimNames = (object[])objDimName;
                            int i = 0;
                            while (true)
                            {
                                try
                                {
                                    var swDimName = (string)objDimNames[i];
                                    swModel.Extension.SelectByID2(swDimName, "DIMENSION", 0, 0, 0, false, 0, null, 0);
                                    var displayDimension = swSelMgr.GetSelectedObject6(1, 0) as DisplayDimension;
                                    if (displayDimension.IGetAnnotation().Color != 4194304)
                                    {
                                        try
                                        {
                                            swModel.DeleteSelection(false);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                    i++;
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        }
                        swModel.ClearSelection2(true);
                        var objSketchBlockDef = swModel.SketchManager.GetSketchBlockDefinitions();
                        bool done = false;
                        if (objSketchBlockDef != null)
                        {
                            var objSketBlDefs = (object[])objSketchBlockDef;
                            foreach (var objSketBlDef in objSketBlDefs)
                            {
                                var swSketBlDef = (SketchBlockDefinition)objSketBlDef;
                                var objSketBlInsts = (object[])swSketBlDef.GetInstances();
                                if (objSketBlInsts != null)
                                    foreach (var objSketBlInst in objSketBlInsts)
                                    {
                                        done = true;
                                        var swSketBlInst = (SketchBlockInstance)objSketBlInst;

                                        if (!swSketBlInst.Name.Contains("Напр. волокон") &&
                                            !swSketBlInst.Name.Contains("База_станка") &&
                                            !swSketBlInst.Name.Contains("DS") && !swSketBlInst.Name.Contains("DE") && !swSketBlInst.Name.Contains("EndDimArea") && !swSketBlInst.Name.Contains("_b64"))
                                            swSketBlInst.Select(true, swSelData);
                                    }
                            }
                        }
                        if (done)
                            swModel.DeleteSelection(false);
                    }
                }
            }
            swModel.EditRebuild3();
            return;
        }

        #endregion

        #region Calculating Project Price

        public int GetProjectPriceEnable()
        {
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void GetProjectPrice()
        {
            //проверка доступа к БД
            Exception exception = Repository.Instance.ConnectionCheck();
            if (exception != null)
            {
                MessageBox.Show(exception.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                frmFullPrice ffp = new frmFullPrice((ModelDoc2)_iSwApp.ActiveDoc);
                ffp.ShowDialog();
            }
        }

        #endregion

        #region PurchaseExport

        public int PurchaseExportEnable()
        {
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void PurchaseExport()
        {
            frmPurchaseExport fpe = new frmPurchaseExport(_iSwApp);
            fpe.ShowDialog();
        }

        public int XmlEnable()
        {
            return 1;
        }

        public void Xml()
        {
            //Furniture.FinalProcessing.XmlProgram.CreateAndSave(_iSwApp, ((ModelDoc2)_iSwApp.ActiveDoc), "");
        }

        #endregion


        public int FixColorEnable()
        {
            if (Properties.Settings.Default.CashModeOn)
                return 0;
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void FixColor()
        {
            if (!CheckForTearOff())
            {
                MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
                return;
            }
            string filePtwoM =
                new DirectoryInfo(Furniture.Helpers.LocalAccounts.decorPathResult).GetFiles("*.p2m").First().FullName;
            var assembly = (AssemblyDoc)SwModel;
            var oComps = assembly.GetComponents(true);
            LinkedList<ModelDoc2> outModels;
            if (GetAllUniqueLibraryModels(SwModel, out outModels))
            {
                foreach (var oComp in oComps)
                {
                    var comp = (Component2)oComp;
                    FixColorForEachComponent(SwModel, comp, filePtwoM, outModels);
                }
                SwModel.ForceRebuild3(true);
            }
        }

        private void FixColorForEachComponent(ModelDoc2 model, Component2 component, string file,
                                              LinkedList<ModelDoc2> models)
        {
            try
            {
                var swConfig = model.IGetActiveConfiguration();
                object displayStateNames = swConfig.GetDisplayStates();
                var dispNames = (string[])displayStateNames;

                var mod = component.IGetModelDoc();
                if (mod != null && mod.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY && models.Contains(mod))
                {
                    if (component.SetTextureByDisplayState(dispNames[0], mod.Extension.CreateTexture(file, 1, 0, false)))
                        component.RemoveTextureByDisplayState(dispNames[0]);

                    var assembly = (AssemblyDoc)mod;
                    var oComps = assembly.GetComponents(true);
                    foreach (var oComp in oComps)
                    {
                        var comp = (Component2)oComp;
                        FixColorForEachComponent(mod, comp, file, models);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public int DeleteNonlinkedAnnotationsEnable()
        {
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void DeleteNonlinkedAnnotations()
        {
            MessageBox.Show(@"Пунк меню, который пока не работает!");
        }

        public int RecalculateRangesEnable()
        {
            if (Properties.Settings.Default.CashModeOn)
                return 0;
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void RecalculateRanges()
        {
            if (!CheckForTearOff())
            {
                MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
                return;
            }
            RecalculateRanges2(true);
        }

        public void RecalculateRanges2(bool isRebuild)
        {
            IsEventsEnabled = false;
            var swModel = (ModelDoc2)_iSwApp.ActiveDoc;

            try
            {
                UserProgressBar pb;
                SwApp.GetUserProgressBar(out pb);
                LinkedList<ModelDoc2> allUniqueModels;
                if (GetAllUniqueLibraryModels(swModel, out allUniqueModels))
                {
                    int i = 0;
                    pb.Start(0, allUniqueModels.Count, "Пересчет моделей");
                    foreach (ModelDoc2 mdoc in allUniqueModels)
                    {
                        pb.UpdateTitle("Пересчет модели " + Path.GetFileNameWithoutExtension(mdoc.GetPathName()));
                        RecalculateModel(mdoc);
                        i++;
                        pb.UpdateProgress(i);
                    }
                    if (isRebuild)
                        swModel.EditRebuild3();
                }
                pb.End();
            }
            catch (Exception e)
            {
                MessageBox.Show(@"Ошибка при работе программы!" + Environment.NewLine + e.Message, MyTitle,
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            IsEventsEnabled = true;
        }

        public bool RecalculateModel(ModelDoc2 inModel)
        {
            bool ret = false;

            try
            {
                OleDbConnection oleDb;

                if (OpenModelDatabase(inModel, out oleDb))
                {
                    var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                                                             new object[] { null, null, null, "TABLE" });
                    if (oleSchem.Rows.Cast<DataRow>().Any(row => (string)row["TABLE_NAME"] == "objects"))
                    {
                        var cm = new OleDbCommand("SELECT source FROM objects GROUP BY source", oleDb);
                        OleDbDataReader rdSource = cm.ExecuteReader();

                        double strObjVal;
                        while (rdSource.Read())
                        {
                            if (rdSource["source"].ToString() != "")
                            {
                                cm = new OleDbCommand("SELECT * FROM objects WHERE ismaster=true " +
                                                      "AND source='" + rdSource["source"] + "' ORDER BY id", oleDb);
                                OleDbDataReader rdMaster = cm.ExecuteReader();

                                string strWhere = "";
                                string strOrder = "";

                                while (rdMaster.Read())
                                {
                                    string strObjName = rdMaster["name"].ToString();

                                    if (GetObjectValue(inModel, strObjName, (int)rdMaster["type"], out strObjVal))
                                    {
                                        if (strWhere != "") strWhere = strWhere + " AND ";
                                        strWhere = strWhere + "obj" + rdMaster["id"] + "<=" +
                                                   CorrectDecimalSymbol(strObjVal.ToString(), false, true);

                                        if (strOrder != "") strOrder = strOrder + ", ";
                                        strOrder = strOrder + "obj" + rdMaster["id"];
                                    }
                                }
                                rdMaster.Close();

                                if (strWhere != "") strWhere = " WHERE " + strWhere;
                                if (strOrder != "") strOrder = " ORDER BY " + strOrder + " DESC ";

                                cm = new OleDbCommand("SELECT * FROM " + rdSource["source"] + strWhere + strOrder, oleDb);
                                OleDbDataReader rdData = cm.ExecuteReader();

                                if (rdData.Read())
                                {
                                    for (int i = 0; i < rdData.FieldCount; i++)
                                    {
                                        string strFieldName = rdData.GetName(i);

                                        cm = new OleDbCommand("SELECT * FROM objects " + "WHERE id=" +
                                                              Strings.Right(strFieldName, strFieldName.Length - 3) +
                                                              " AND ismaster=false", oleDb);
                                        OleDbDataReader rdSlave = cm.ExecuteReader();

                                        if (rdSlave.Read())
                                        {
                                            double val = 0;
                                            switch ((int)rdSlave["type"])
                                            {
                                                case 14:
                                                    val = (double)rdData[i];
                                                    break;

                                                case 20:
                                                case 22:
                                                    val = (bool)rdData[i] ? 1 : 0;
                                                    break;
                                            }
                                            // bool? noArtIfSuppressed = rdSlave["noArtIfSuppressed"] as bool?;
                                            bool noArtIfSuppressed = false;

                                            try
                                            {
                                                if (rdSlave["noArtIfSuppressed"] as bool? != null)
                                                {
                                                    noArtIfSuppressed = (bool)rdSlave["noArtIfSuppressed"];
                                                }
                                            }
                                            catch (Exception)
                                            { }

                                            //MessageBox.Show(Path.GetFileNameWithoutExtension(inModel.GetPathName()) + " " + rdSlave["name"] + " " + val);
                                            SetObjectValue(inModel, rdSlave["name"].ToString(), (int)rdSlave["type"], val, (bool)noArtIfSuppressed);
                                        }
                                        rdSlave.Close();
                                    }
                                }
                                rdData.Close();

                            }
                        }
                        rdSource.Close();

                        if (oleSchem.Rows.Cast<DataRow>().Any(row => (string)row["TABLE_NAME"] == "equations"))
                        {
                            cm = new OleDbCommand("SELECT * FROM equations ORDER BY id", oleDb);
                            OleDbDataReader rdEqu = cm.ExecuteReader();

                            while (rdEqu.Read())
                            {
                                if (GetObjectValue(inModel, rdEqu["master"].ToString(), 14, out strObjVal))
                                {
                                    SetObjectValue(inModel, rdEqu["slave"].ToString(), 14, strObjVal);
                                }
                            }
                            rdEqu.Close();
                        }
                    }
                    oleDb.Close();

                    //Принудительное погашение сопряжения с ошибкой
                    Feature swSubFeature;
                    var swFeature = (Feature)inModel.FirstFeature();

                    bool isFeatureFound = false;

                    while (swFeature != null)
                    {
                        if (swFeature.GetTypeName2() == "MateGroup")
                        {
                            isFeatureFound = true;

                            swSubFeature = (Feature)swFeature.GetFirstSubFeature();
                            while (swSubFeature != null)
                            {
                                if (swSubFeature.GetErrorCode() != (int)swFeatureError_e.swFeatureErrorNone)
                                {
                                    swSubFeature.IsSuppressed2((int)swInConfigurationOpts_e.swThisConfiguration, null);
                                    swSubFeature.SetSuppression((int)swFeatureSuppressionAction_e.swSuppressFeature);
                                }
                                swSubFeature = (Feature)swSubFeature.GetNextSubFeature();
                            }
                        }
                        if (isFeatureFound) break;
                        swFeature = (Feature)swFeature.GetNextFeature();
                    }
                    ret = true;
                }

                _features.Clear();
                _comps.Clear();
            }

            catch (Exception e)
            {
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return ret;
        }

        public bool GetObjectValue(ModelDoc2 inModel, string inName, int inType, out double outVal)
        {
            object swObj;
            Dimension swDim;
            Feature swFeature;
            Component2 swComp;
            bool ret = false;

            outVal = 0;

            if (GetObjectByName(inModel, inName, inType, out swObj))
            {
                switch (inType)
                {
                    case 14:
                        swDim = (Dimension)swObj;
                        outVal = swDim.GetSystemValue2("") * 1000;
                        break;

                    case 22:
                        swFeature = (Feature)swObj;
                        outVal = (swFeature.IsSuppressed() ? 0 : 1);
                        break;

                    case 20:
                        swComp = (Component2)swObj;
                        outVal = (swComp.IsSuppressed() ? 0 : 1);
                        break;
                }
                ret = true;
            }
            else if (inType == (int)swSelectType_e.swSelUNSUPPORTED)
            {

                int status = 0;
                outVal = inModel.Extension.GetMassProperties(0, ref status)[5];
                if (status == (int)swMassPropertiesStatus_e.swMassPropertiesStatus_OK)
                    ret = true;
                else
                    ret = false;

            }
            return ret;
        }

        public bool SetObjectValue(ModelDoc2 inModel, string inName, int inType, double inVal, bool noArtIfSuppressed = false)
        {
            object swObj;
            Dimension swDim;
            Feature swFeature;
            Component2 swComp;
            bool ret = false;

            if (GetObjectByName(inModel, inName, inType, out swObj))
            {
                double dTestVal;
                switch (inType)
                {
                    case 14:
                        swDim = (Dimension)swObj;
                        dTestVal = swDim.GetSystemValue2("") * 1000;

                        if (dTestVal != inVal)
                        {
                            ret = (swDim.SetSystemValue2(inVal / 1000,
                                                         (int)
                                                         swSetValueInConfiguration_e.swSetValue_InAllConfigurations) ==
                                   (int)swSetValueReturnStatus_e.swSetValue_Successful);
                        }
                        else
                            ret = true;
                        break;

                    case 22:
                        swFeature = (Feature)swObj;
                        dTestVal = (swFeature.IsSuppressed() ? 0 : 1);

                        if (dTestVal != inVal)
                        {
                            //ret = swFeature.SetSuppression(inVal == 0 ? (int) swFeatureSuppressionAction_e.swSuppressFeature : (int)swFeatureSuppressionAction_e.swUnSuppressFeature);
                            ret =
                                swFeature.SetSuppression2(
                                    inVal == 0
                                        ? (int)swFeatureSuppressionAction_e.swSuppressFeature
                                        : (int)swFeatureSuppressionAction_e.swUnSuppressFeature,
                                    (int)swInConfigurationOpts_e.swAllConfiguration, inModel.GetConfigurationNames());

                            #region Ошибка текстуры

                            Component2 newComp = null;

                            var config = inModel.IGetActiveConfiguration();
                            if (config != null)
                            {
                                swComp = config.IGetRootComponent2();
                                if (swComp != null)
                                {
                                    var outComps = new LinkedList<Component2>();
                                    if (GetComponents(swComp, outComps, false, false))
                                    {
                                        foreach (var component2 in outComps)
                                        {
                                            var mod = component2.IGetModelDoc();
                                            if (mod != null)
                                            {
                                                var texture = component2.GetTexture("");
                                                if (mod.GetType() == (int)swDocumentTypes_e.swDocPART &&
                                                    texture != null)
                                                {
                                                    newComp = component2;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (newComp != null)
                            {
                                inModel.Save();
                                if (newComp.Select(false))
                                    ((AssemblyDoc)inModel).ComponentReload();
                            }

                            #endregion
                        }
                        else
                            ret = true;
                        break;

                    case 20:
                        swComp = (Component2)swObj;
                        try
                        {
                            if ((swComp.IsSuppressed() ? 0 : 1) == inVal)
                            {
                                ret = true;
                            }
                            else
                            {
                                int k = inVal == 0
                                            ? (int)swComponentSuppressionState_e.swComponentSuppressed
                                            : (int)swComponentSuppressionState_e.swComponentFullyResolved;

                                bool isCavity = false;
                                if (inVal == 0)
                                {
                                    var outC = new LinkedList<Component2>();
                                    if (GetComponents(swComp, outC, true, false))
                                    {
                                        isCavity =
                                            outC.Select(component2 => component2.IGetModelDoc()).Any(
                                                m => m != null && m.get_CustomInfo2("", "swrfIsCut") == "Yes");
                                    }
                                }
                                int tmp = k;
                                if (k == (int)swComponentSuppressionState_e.swComponentFullyResolved)
                                    tmp = swComp.SetSuppression2(k);
                                if (noArtIfSuppressed)
                                {
                                    //если надо гасить, то гасить после..
                                    //если надо зажигать- то зажигать до..
                                    ModelDoc2 outModel;

                                    if (GetModelByName(inModel, inName, false, out outModel, true))
                                    {
                                        if (k == (int)swComponentSuppressionState_e.swComponentSuppressed)
                                        {
                                            //удаляем артикул
                                            RenameCustomProperty(outModel, string.Empty, "Articul", "Noarticul");
                                        }
                                        else if (k == (int)swComponentSuppressionState_e.swComponentFullyResolved)
                                        {
                                            //пишем артикул
                                            RenameCustomProperty(outModel, string.Empty, "Noarticul", "Articul");
                                        }
                                    }
                                }
                                if (k == (int)swComponentSuppressionState_e.swComponentSuppressed)
                                    tmp = swComp.SetSuppression2(k);
                                k = tmp;

                                #region Погашение уравнений

                                var equMrg = inModel.GetEquationMgr();
                                if (equMrg != null)
                                {
                                    var outList = new LinkedList<Component2>();
                                    if (GetComponents(inModel.IGetActiveConfiguration().IGetRootComponent2(), outList,
                                                      true,
                                                      false))
                                    {
                                        bool notSuppComp =
                                            outList.All(
                                                component2 =>
                                                !swComp.IsSuppressed() ||
                                                swComp.GetPathName() != component2.GetPathName() ||
                                                component2.IsSuppressed());
                                        for (int i = 0; i < inModel.GetEquationMgr().GetCount(); i++)
                                        {
                                            if (equMrg.Equation[i].Contains(
                                                Path.GetFileNameWithoutExtension(swComp.GetPathName())))
                                            {
                                                if ((equMrg.get_Suppression(i) ? 0 : 1) != inVal)
                                                {
                                                    equMrg.set_Suppression(i, inVal == 0 && notSuppComp);
                                                }
                                            }
                                        }
                                    }
                                }

                                #endregion

                                #region Погашение отверстий

                                if (isCavity)
                                {
                                    LinkedList<Component2> swComps;
                                    List<Feature> list;
                                    if (_features.Count == 0 || _comps.Count == 0)
                                        list = GetAllCavitiesFeatures(out swComps);
                                    else
                                    {
                                        list = _features;
                                        swComps = _comps;
                                    }
                                    string nameMod = Path.GetFileNameWithoutExtension(inModel.GetPathName());

                                    var delList = (from component2 in swComps
                                                   where
                                                       component2.Name.Contains(nameMod) &&
                                                       component2.Name.Contains(swComp.Name)
                                                   from feature in list
                                                   where
                                                       component2.Name ==
                                                       GetSpecialNameFromFeature(feature.IGetFirstSubFeature().Name).
                                                           Split('/').First() + "/" +
                                                       GetSpecialNameFromFeature(feature.IGetFirstSubFeature().Name).
                                                           Split('/').ToArray()[1]
                                                   select feature).ToList();
                                    SwModel.ClearSelection();
                                    foreach (var feature in delList)
                                    {
                                        feature.Select(true);
                                    }
                                    SwModel.DeleteSelection(false);
                                }

                                #endregion

                                ret = (k == (int)swSuppressionError_e.swSuppressionChangeOk);
                            }
                        }
                        catch
                        {
                        }
                        break;
                }
            }
            return ret;
        }
        private void RenameCustomProperty(ModelDoc2 model, string conf, string from, string to)
        {
            string val1, val2;
            model.Extension.get_CustomPropertyManager("").Get2(from, out val1, out val2);
            string value = val1;//model.GetCustomInfoValue(conf, from);
            if (!string.IsNullOrEmpty(value))
            {
                model.DeleteCustomInfo2(conf, from);
                model.AddCustomInfo3(conf, to, (int)swCustomInfoType_e.swCustomInfoText, value);
                model.Save();
            }

        }

        private List<Feature> GetAllCavitiesFeatures(out LinkedList<Component2> outComponents)
        {
            var list = new List<Feature>();
            var outComps = new LinkedList<Component2>();
            if (GetComponents(SwModel.IGetActiveConfiguration().IGetRootComponent2(), outComps, true, false))
            {
                foreach (var outComponent in outComps)
                {
                    var m = outComponent.IGetModelDoc();
                    if (m != null && m.GetType() == (int)swDocumentTypes_e.swDocPART)
                    {
                        var feature = outComponent.FirstFeature();
                        while (feature != null)
                        {
                            if (feature.GetTypeName() == "Cavity")
                            {
                                list.Add(feature);
                            }
                            feature = feature.IGetNextFeature();
                        }
                    }
                }
            }
            outComponents = outComps;
            _features = list;
            _comps = outComps;
            return list;
        }

        private bool GetObjectByName(ModelDoc2 inModel, string inName, int inType, out object outObj)
        {
            ModelDoc2 swWorkModel;
            bool isModelFound;
            bool ret = false;

            outObj = null;
            string[] strNameArr = inName.Split('@');

            if (strNameArr.Length > 1)
            {
                isModelFound = GetModelByName(inModel, strNameArr[inType == 14 ? 2 : 1], false, out swWorkModel);

            }
            else
            {
                swWorkModel = inModel;
                isModelFound = true;
            }
            if (isModelFound)
            {
                Feature swFeature;
                switch (inType)
                {
                    case 14:
                        if (GetFeatureByName(swWorkModel, strNameArr[1], out swFeature))
                        {
                            Dimension swDim;
                            if (GetDimByName(swFeature, strNameArr[0], out swDim))
                            {
                                outObj = swDim;
                                ret = true;
                            }
                        }
                        break;

                    case 22:
                        if (GetFeatureByName(swWorkModel, strNameArr[0], out swFeature))
                        {
                            outObj = swFeature;
                            ret = true;
                        }
                        break;

                    case 20:
                        Component2 swComp;
                        if (GetComponentByName(swWorkModel, strNameArr[0], true, out swComp))
                        {
                            outObj = swComp;
                            ret = true;
                        }
                        break;
                }
            }
            return ret;
        }

        internal bool GetModelByName(ModelDoc2 inModel, string inName, bool suff, out ModelDoc2 outModel, bool special = false)
        {
            LinkedList<ModelDoc2> allUniqueModels;
            string strModelName;
            bool ret = false;

            outModel = null;

            if (GetAllUniqueModels(inModel, out allUniqueModels))
            {
                foreach (ModelDoc2 mdoc in allUniqueModels)
                {
                    strModelName = mdoc.GetPathName();
                    if (!suff)
                        strModelName = GetModelNameWithoutSuffix(strModelName);
                    strModelName = Path.GetFileName(strModelName);

                    if (strModelName.Contains("_SWLIB_BACKUP"))
                    {
                        char tmp = Path.GetFileNameWithoutExtension(strModelName).Last();
                        if (tmp == 'P' || tmp == 'p')
                            strModelName = Path.GetFileNameWithoutExtension(strModelName).Substring(0, Path.GetFileNameWithoutExtension(strModelName).Length - 4) + Path.GetExtension(strModelName);
                    }
                    if (inName.Contains("_SWLIB_BACKUP"))
                    {
                        char tmp2 = Path.GetFileNameWithoutExtension(inName).Last();
                        if (tmp2 == 'P' || tmp2 == 'p')
                            inName = Path.GetFileNameWithoutExtension(inName).Substring(0, Path.GetFileNameWithoutExtension(inName).Length - 4) + Path.GetExtension(inName);
                    }

                    if (strModelName.ToLower() == inName.ToLower() || Path.GetFileNameWithoutExtension(strModelName).ToLower() == inName.ToLower() || (special && inName.Split('-').Contains(Path.GetFileNameWithoutExtension(strModelName))))
                    {
                        outModel = mdoc;
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }

        private bool GetSymilarComponentByName(ModelDoc2 inModel, string inName, out Component2 outComponent)
        {
            var swComponents = new LinkedList<Component2>();
            bool ret = false;

            outComponent = null;

            var swConfig = (Configuration)inModel.GetActiveConfiguration();

            if (swConfig != null)
            {
                var swRootComponent = (Component2)swConfig.GetRootComponent();
                if (GetComponents(swRootComponent, swComponents, false, false))
                {
                    foreach (Component2 comp in swComponents)
                    {
                        string compName = comp.Name2;
                        if (compName.ToLower().Contains(inName.ToLower()))
                        {
                            outComponent = comp;
                            ret = true;
                            break;
                        }
                    }
                }
            }
            return ret;
        }

        private bool GetComponentByName(ModelDoc2 inModel, string inName, bool isDropSuffix, out Component2 outComponent, bool isGetSubComponents)
        {
            var swComponents = new LinkedList<Component2>();
            bool ret = false;

            outComponent = null;

            var swConfig = (Configuration)inModel.GetActiveConfiguration();

            if (swConfig != null)
            {
                var swRootComponent = (Component2)swConfig.GetRootComponent();
                if (GetComponents(swRootComponent, swComponents, isGetSubComponents, false))
                {
                    foreach (Component2 comp in swComponents)
                    {
                        string compName = isDropSuffix ? GetComponentNameWithoutSuffix(comp.Name2) : comp.Name2;
                        if (compName.ToLower() == inName.ToLower())
                        {
                            outComponent = comp;
                            ret = true;
                            break;
                        }
                    }
                }
            }
            return ret;
        }
        internal bool GetComponentByName(ModelDoc2 inModel, string inName, bool isDropSuffix, out Component2 outComponent)
        {
            return GetComponentByName(inModel, inName, isDropSuffix, out outComponent, true);
        }

        internal static bool GetFeatureByName(ModelDoc2 inModel, string inName, out Feature outFeature)
        {
            Feature swSubFeature;
            bool ret = false;

            outFeature = (Feature)inModel.FirstFeature();

            while (outFeature != null)
            {
                if (outFeature.Name == inName)
                {
                    ret = true;
                    break;
                }
                swSubFeature = (Feature)outFeature.GetFirstSubFeature();

                while (swSubFeature != null)
                {
                    if (swSubFeature.Name.ToLower() == inName.ToLower())
                    {
                        ret = true;
                        break;
                    }
                    swSubFeature = (Feature)swSubFeature.GetNextSubFeature();
                }
                if (ret) break;

                outFeature = (Feature)outFeature.GetNextFeature();
            }
            return ret;
        }

        private static bool GetDimByName(Feature inFeature, string inName, out Dimension outDim)
        {
            bool ret = false;

            outDim = null;

            var swDispDim = (DisplayDimension)inFeature.GetFirstDisplayDimension();

            while (swDispDim != null)
            {
                outDim = (Dimension)swDispDim.GetDimension();

                if (outDim.Name.ToLower() == inName.ToLower())
                {
                    ret = true;
                    break;
                }
                swDispDim = (DisplayDimension)inFeature.GetNextDisplayDimension(swDispDim);
            }
            return ret;
        }

        private bool GetSelectedComponents(out LinkedList<Component2> selComps)
        {
            bool ret = false;

            selComps = new LinkedList<Component2>();

            SelectionMgr swSelMgr = SwModel.ISelectionManager;
            int selCnt = swSelMgr.GetSelectedObjectCount();

            for (int i = 1; i <= selCnt; i++)
            {
                var swComp = swSelMgr.GetSelectedObjectsComponent3(i, 0);
                var swCompModel = (ModelDoc2)swComp.GetModelDoc();

                if (swCompModel != null)
                {
                    selComps.AddLast(swComp);
                    ret = true;
                }
            }
            return ret;
        }

        public void ReplaceComponents()
        {
            //if (!CheckForTearOff())
            //{
            //    MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
            //    return;
            //}
            LinkedList<Component2> selComps;

            IsEventsEnabled = false;

            try
            {
                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;
                if (GetSelectedComponents(out selComps))
                {
                    var selCompsArr = new Component2[selComps.Count];
                    selComps.CopyTo(selCompsArr, 0);

                    for (int i = 0; i < selCompsArr.Length; i++)
                    {
                        Component2 swDbComp;
                        if (GetParentLibraryComponent(selCompsArr[i], out swDbComp))
                        {
                            selCompsArr[i] = swDbComp;
                        }
                    }
                    swModel.ClearSelection();

                    var frmRplComp = new FrmReplaceComponents(this, selCompsArr);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            IsEventsEnabled = true;
        }

        public int SaveAsComponentsEnable()
        {
            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            LinkedList<Component2> selComps;
            return GetSelectedComponents(out selComps) ? 1 : 0;
        }

        public void SaveAsComponents(out LinkedList<CopiedFileNames> filesNames)
        {

            filesNames = new LinkedList<CopiedFileNames>();

            bool tmp = IsEventsEnabled;
            IsEventsEnabled = false;
            try
            {
                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;
                LinkedList<Component2> selComps;
                if (GetSelectedComponents(out selComps))
                {
                    foreach (Component2 comp in selComps)
                    {
                        SaveAsComponent(swModel, comp, true, filesNames, 1);

                        comp.Select(false);
                        ((AssemblyDoc)swModel).EditAssembly();
                        swModel.ClearSelection();
                        ((AssemblyDoc)swModel).EditAssembly();

                    }
                }
            }
            catch (Exception e)
            {
                Logging.Log.Instance.Fatal(e.Message + "SaveAsComponents()");
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            IsEventsEnabled = tmp;
        }

        private void SaveAsComponent(ModelDoc2 swModel, Component2 inComp, bool isFirstLevel, LinkedList<CopiedFileNames> filesNames, int k)//k - показатель вложенности для логгирования
        {
            string offset = string.Empty;
            //for (int i = 0; i < k; i++)
            //    offset += "   ";
            try
            {
                string strSubCompOldFileNameFromComponent = inComp.GetPathName();
                if (strSubCompOldFileNameFromComponent == "")
                {
                    var swCompModel = (ModelDoc2)inComp.GetModelDoc();
                    if (swCompModel != null)
                        strSubCompOldFileNameFromComponent = swCompModel.GetPathName();
                    else
                        return;
                }

                bool isModelAlreadyReplaced =
                    filesNames.Any(oldfile => oldfile.OldName == strSubCompOldFileNameFromComponent);

                if (!isModelAlreadyReplaced)
                {
                    ModelDoc2 swCompModel;
                    string strSubCompOldFileNameFromModel;
                    if (GetComponentModel(swModel, inComp, out swCompModel, out strSubCompOldFileNameFromModel))
                    {


                        bool isAuxiliary = (swCompModel.get_CustomInfo2("", "Auxiliary") == "Yes");
                        bool isAccessory = (swCompModel.get_CustomInfo2("", "Accessories") == "Yes");

                        string strSubCompNewFileName;
                        if (GetComponentNewFileName(swModel, isAuxiliary, isAccessory,
                                                    isFirstLevel, strSubCompOldFileNameFromModel,
                                                    out strSubCompNewFileName))
                        {


                            bool isCopyError = false;

                            try
                            {
                                if (!File.Exists(strSubCompNewFileName)) //фурнитура
                                {
                                    File.Copy(strSubCompOldFileNameFromModel, strSubCompNewFileName, true);

                                }
                            }
                            catch (Exception)
                            {
                                MessageBox.Show(@"Не удалось сохранить файл " + strSubCompNewFileName,
                                                MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                isCopyError = true;
                            }
                            if (!isCopyError)
                            {
                                filesNames.AddLast(new CopiedFileNames(strSubCompOldFileNameFromComponent,
                                                                       strSubCompNewFileName));
                                File.SetAttributes(strSubCompNewFileName, FileAttributes.Normal);

                                var subComps = new LinkedList<Component2>();
                                if (GetComponents(inComp, subComps, false, false))
                                {
                                    foreach (Component2 subcmp in subComps)
                                    {
                                        SaveAsComponent(swModel, subcmp, false, filesNames, k + 1);
                                    }

                                }

                                SwDmDocumentOpenError oe;


                                SwDMApplication swDocMgr = GetSwDmApp();
                                var swDoc = (SwDMDocument8)swDocMgr.GetDocument(strSubCompNewFileName,
                                                                                 SwDmDocumentType.swDmDocumentAssembly,
                                                                                 false, out oe);


                                if (swDoc != null)
                                {
                                    SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
                                    src.SearchFilters = 255;

                                    object brokenRefVar;
                                    var varRef = (object[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);

                                    if (varRef != null)
                                    {
                                        foreach (object t in varRef)
                                        {
                                            var strRef = (string)t;
                                            string strRefFileName = Path.GetFileName(strRef);
                                            string strNewRef = "";
                                            foreach (CopiedFileNames oldfile in filesNames)
                                            {
                                                if (Path.GetFileName(oldfile.OldName).ToLower() == strRefFileName.ToLower())
                                                {
                                                    strNewRef = oldfile.NewName;
                                                    break;
                                                }
                                            }
                                            if (strNewRef != "")
                                                swDoc.ReplaceReference(strRef, strNewRef);
                                        }
                                    }
                                    swDoc.Save();
                                    swDoc.CloseDoc();
                                }

                                if (isFirstLevel)
                                {
                                    var outList1 = new LinkedList<Component2>();

                                    if (inComp.Select(false))
                                        ((AssemblyDoc)swModel).ReplaceComponents(strSubCompNewFileName, "", false, true);
                                    if (inComp.Select(false))
                                        ((AssemblyDoc)swModel).ComponentReload();

                                    if (GetComponents(inComp, outList1, true, false))
                                    {
                                        foreach (var component in outList1)
                                        {
                                            var model = component.IGetModelDoc();
                                            if (model != null && model.GetConfigurationCount() > 1)
                                            {
                                                int err = 0, wrn = 0;
                                                var mod = SwApp.OpenDoc6(model.GetPathName(),
                                                                         (int)swDocumentTypes_e.swDocPART,
                                                                         0, "", ref err, ref wrn);
                                                if (mod != null)
                                                {

                                                    mod.ShowConfiguration2(component.ReferencedConfiguration);
                                                    mod.Save();
                                                    SwApp.CloseDoc(mod.GetPathName());

                                                }
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Log.Instance.Fatal(e.Message + "SaveAsComponent()");
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return;
        }

        private bool GetComponentModel(ModelDoc2 swModel, Component2 inComp, out ModelDoc2 swCompModel, out string modelFileName)
        {
            int i = 0;
            modelFileName = "";
            do
            {
                var compState = (swComponentSuppressionState_e)inComp.GetSuppression();

                if (compState.ToString() == "swComponentSuppressed")
                    inComp.SetSuppression2((int)swComponentSuppressionState_e.swComponentFullyResolved);

                swCompModel = (ModelDoc2)inComp.GetModelDoc();

                if (swCompModel == null)
                {
                    string newModelPath = CheckIsCompInOurLib(inComp.GetPathName());
                    if (inComp.Select(false))
                    {
                        ((AssemblyDoc)swModel).ReplaceComponents(newModelPath, "", true, true);
                        //((AssemblyDoc) swModel).ComponentReload();
                        swCompModel = inComp.IGetModelDoc();
                        //((AssemblyDoc) swModel).OpenCompFile();
                        //При автоматическом отрывании компонента OpenCompFile не работает!
                        if (((ModelDoc2)_iSwApp.ActiveDoc).GetPathName() != swModel.GetPathName())
                            swCompModel = (ModelDoc2)_iSwApp.ActiveDoc;
                    }
                }
                i++;
            } while (swCompModel == null && i < 10);

            if (swCompModel != null)
                modelFileName = swCompModel.GetPathName();
            else
            {
                MessageBox.Show(@"Компонент " + inComp.Name + @"не оторвался!", MyTitle, MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                return false;
            }
            return true;
        }

        private bool GetComponentNewFileName(ModelDoc2 swModel, bool isAuxiliary,
            bool isAccessory, bool isFirstLevel, string inOldFileName, out string outNewFileName)
        {
            string subFolderName;
            bool ret = false;
            outNewFileName = "";

            string rootFolder = GetRootFolder(swModel);

            if (isAccessory)
            {
                subFolderName = "ФУРНИТУРА";
                if (isAuxiliary) subFolderName = subFolderName + "\\Модели фурнитуры";
            }
            else
            {
                subFolderName = "ДЕТАЛИ";
                //if (isAuxiliary) subFolderName = subFolderName + "\\Вспомогательные детали";
                //else
                //{
                //    if (!isFirstLevel)
                //        subFolderName = subFolderName + "\\Детали для сборок";
                //}
                if (!isFirstLevel)
                    subFolderName = subFolderName + "\\Детали для сборок";
            }
            string modelFolder = rootFolder + "\\" + subFolderName;

            if (!Directory.Exists(modelFolder))
                Directory.CreateDirectory(modelFolder);

            if (Directory.Exists(modelFolder))
            {
                string strOldCompFileName = Path.GetFileName(inOldFileName);
                string strNewFileName;

                string strOldCompName = GetModelNameWithoutSuffix(inOldFileName);
                strOldCompName = modelFolder + "\\" + Path.GetFileName(strOldCompName);

                int lngCopyIdx = 1;
                do
                {
                    outNewFileName = Strings.Left(strOldCompName, strOldCompName.Length - 7) + " #" + GetXNameForAssembly() +
                        "-" + lngCopyIdx + Strings.Right(strOldCompName, 7);
                    lngCopyIdx++;
                    strNewFileName = Path.GetFileName(outNewFileName);
                }
                while ((Directory.GetFiles(rootFolder, strNewFileName, SearchOption.AllDirectories).Count() > 0 && !isAccessory) || strOldCompFileName == strNewFileName);

                ret = true;
            }
            return ret;
        }

        #region Copy Drawing
        public int CopyDrawingsEnable()
        {

            if (Properties.Settings.Default.CashModeOn)
                return 0;
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void CopyDrawings()
        {
            //if (!CheckForTearOff())
            //{
            //    MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
            //    return;
            //}
            CopyDrawings2(false, true, null);
        }
        private void CopyDrawings2Thread(object o)
        {
            CopyDrawings2(true, false, null);//(swComp2.GetModelDoc2());
        }
        public void CopyDrawings2(bool isSilent, bool askIfExist, string whereToSearchDirectory)
        {
            var allUniqueModels = new LinkedList<ModelDoc2>();
            const string strErrorNoCompSel = "Выберите хотя бы один компонент!";
            try
            {
                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;
                var swSelMgr = (SelectionMgr)swModel.SelectionManager;

                int selCnt = swSelMgr.GetSelectedObjectCount();

                if (selCnt < 1)
                {
                    MessageBox.Show(strErrorNoCompSel, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    for (int i = 1; i <= selCnt; i++)
                    {
                        if (swSelMgr.GetSelectedObjectType3(i, 0) == (int)swSelectType_e.swSelCOMPONENTS)
                        {
                            var swComp = (Component2)swSelMgr.GetSelectedObject6(i, 0);

                            if (swComp.Name.Split('/').Count() == 2)
                                GetComponentByName(swModel, swComp.Name.Split('/').First(), false, out swComp);

                            var swCompModel = (ModelDoc2)swComp.GetModelDoc();
                            if (swCompModel != null)
                            {
                                LinkedList<ModelDoc2> selCompModels;

                                if (GetAllUniqueModels(swCompModel, out selCompModels))
                                {
                                    foreach (ModelDoc2 selmdoc in selCompModels)
                                    {
                                        ModelDoc2 selmdoc1 = selmdoc;

                                        bool isModelAlreadyAdded = allUniqueModels.Any(allmdoc => allmdoc.GetPathName() == selmdoc1.GetPathName());

                                        if (!isModelAlreadyAdded)
                                        {
                                            allUniqueModels.AddLast(selmdoc);

                                        }
                                    }
                                }
                            }
                        }
                    }
                    int copiedDrwNum = 0;
                    string notHaveDrawing = "Чертежи не найдены для:\n";
                    const string strHaveCopied = "Чертежи скопированы.";
                    bool isNotHaveDrawing = false;
                    foreach (ModelDoc2 mdoc in allUniqueModels)
                    {
                        if ((mdoc.GetType() == (int)swDocumentTypes_e.swDocPART) || (mdoc.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY))
                        {
                            if (mdoc.get_CustomInfo2("", "Required Draft") == "Yes")
                            {
                                if (CopyDrawing(mdoc, askIfExist, whereToSearchDirectory))
                                {
                                    copiedDrwNum++;
                                }
                                else
                                {
                                    notHaveDrawing += (Path.GetFileName(mdoc.GetPathName()) + "\n");
                                    isNotHaveDrawing = true;
                                }
                            }
                            else
                            {
                                if (mdoc.get_CustomInfo2("", "Required Draft") == "No") continue;
                                if (mdoc.get_CustomInfo2(mdoc.IGetActiveConfiguration().Name, "Required Draft") == "Yes")
                                {
                                    if (CopyDrawing(mdoc, askIfExist, whereToSearchDirectory))
                                    {
                                        copiedDrwNum++;
                                    }
                                    else
                                    {
                                        notHaveDrawing += (Path.GetFileName(mdoc.GetPathName()) + "\n");
                                        isNotHaveDrawing = true;
                                    }
                                }
                            }
                        }
                    }
                    if (!isSilent)
                    {
                        if (copiedDrwNum > 0)
                        {
                            if (isNotHaveDrawing)
                            {
                                MessageBox.Show(strHaveCopied + @"\n" + notHaveDrawing, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show(strHaveCopied, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show(isNotHaveDrawing ? notHaveDrawing : @"Заказ не содержит ссылки на чертежи",
                                            MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        public bool CopyDrawing(ModelDoc2 inModel)
        {
            return CopyDrawing(inModel, true, null);
        }
        private bool CopyDrawing(ModelDoc2 inModel, bool askIfExist, string whereToSearchDirectory)
        {
            bool ret = false;

            try
            {
                string strModelName = inModel.GetPathName();
                if (strModelName != "")
                {
                    string strDrwNewName; // это куда копируем...
                    string strDrwName = string.Empty;
                    var dir = new DirectoryInfo(DrwPathResult);
                    if (strModelName.ToUpper().Contains("_SWLIB_BACKUP"))
                    {
                        strDrwNewName = Path.GetDirectoryName(RootModel.GetPathName()) + "\\ЧЕРТЕЖИ\\" + Path.GetFileNameWithoutExtension(strModelName) + ".SLDDRW";

                        string modelNameWithoutExtension = Path.GetFileNameWithoutExtension(strModelName);
                        strDrwName = modelNameWithoutExtension.Substring(0, modelNameWithoutExtension.Length - 4);
                        foreach (FileInfo file in dir.GetFiles(strDrwName + ".SLDDRW", SearchOption.AllDirectories))
                        {
                            strDrwName = file.FullName;
                            break;
                        }
                        if (!Directory.Exists(Path.GetDirectoryName(RootModel.GetPathName()) + "\\ЧЕРТЕЖИ"))
                            Directory.CreateDirectory(Path.GetDirectoryName(RootModel.GetPathName()) + "\\ЧЕРТЕЖИ");
                    }
                    else
                    {
                        strDrwNewName = Path.GetDirectoryName(strModelName) + "\\" +
                                        Path.GetFileNameWithoutExtension(strModelName) + ".SLDDRW";
                        //string strDrwNewMyName = GetRootFolder(inRootModel) + "\\ЧЕРТЕЖИ " + GetOrderName(inRootModel) + "\\" + Path.GetFileName(strDrwNewName);
                        strDrwName = GetModelNameWithoutSuffix(strModelName);
                        if (!string.IsNullOrEmpty(whereToSearchDirectory))
                        {
                            string[] postfixArr = Path.GetFileNameWithoutExtension(strModelName).Split('-');
                            string postfix = string.Empty;
                            if (postfixArr.Length > 1)
                                postfix = "*" + postfixArr.Last() + ".SLDDRW";
                            else
                                postfix = "*.SLDDRW";
                            dir = new DirectoryInfo(whereToSearchDirectory);
                            foreach (
                                FileInfo file in
                                    dir.GetFiles(Path.GetFileNameWithoutExtension(strDrwName) + postfix,
                                                 SearchOption.AllDirectories))
                            {
                                strDrwName = file.FullName;
                                break;
                            }
                        }
                        else
                        {
                            foreach (
                                FileInfo file in
                                    dir.GetFiles(Path.GetFileNameWithoutExtension(strDrwName) + ".SLDDRW",
                                                 SearchOption.AllDirectories))
                            {
                                strDrwName = file.FullName;
                                break;
                            }
                        }
                        if (!File.Exists(strDrwName) && !string.IsNullOrEmpty(whereToSearchDirectory))
                        {
                            // копируем из библиотеки
                            strDrwName = GetModelNameWithoutSuffix(strModelName);
                            dir = new DirectoryInfo(DrwPathResult);
                            foreach (
                                FileInfo file in
                                    dir.GetFiles(Path.GetFileNameWithoutExtension(strDrwName) + ".SLDDRW",
                                                 SearchOption.AllDirectories))
                            {
                                strDrwName = file.FullName;
                                break;
                            }
                        }
                    }
                    if (File.Exists(strDrwName))
                    {
                        bool isOverwriteExistingFile = true;

                        if (File.Exists(strDrwNewName))
                        {
                            if (askIfExist)
                                isOverwriteExistingFile = (MessageBox.Show(@"Файл " + strDrwNewName + @" существует. Вы хотите перезаписать его?",
                                    MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
                            else
                                isOverwriteExistingFile = false;

                        }
                        if (isOverwriteExistingFile)
                        {
                            File.Copy(strDrwName, strDrwNewName, true);
                            File.SetAttributes(strDrwNewName, FileAttributes.Normal);

                            SwDmDocumentOpenError oe;
                            object brokenRefVar;

                            SwDMApplication swDocMgr = GetSwDmApp();
                            var swDoc = (SwDMDocument8)swDocMgr.GetDocument(strDrwNewName,
                                SwDmDocumentType.swDmDocumentDrawing, false, out oe);
                            SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
                            var varRef = (object[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                            if (varRef.Count() > 1)
                            {
                                foreach (var o in varRef)
                                {
                                    ModelDoc2 mod;
                                    if (GetModelByName(inModel, (new FileInfo((string)o)).Name, false, out mod))
                                    {
                                        swDoc.ReplaceReference((string)o, mod.GetPathName());
                                    }
                                    else
                                    {
                                        MessageBox.Show("Модель потеряла связь с чертежом! Изменения вносимые в модель на чертеже отражаться не будут.");
                                    }
                                }
                            }
                            else
                                swDoc.ReplaceReference((string)varRef[0], strModelName);
                            SwDmDocumentSaveError lngSaveRes = swDoc.Save();
                            swDoc.CloseDoc();
                            ret = true;
                        }
                        // сюда вынесли ret = true, т.е. файл чертежа все равно существует, просто не заменили
                        else ret = true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return ret;
        }
        #endregion

        #region Open Drawings
        public int OpenDrawingsEnable()
        {
            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            return (_iSwApp.ActiveDoc != null) ? 1 : 0;
        }

        public void OpenDrawings()
        {
            //if (!CheckForTearOff())
            //{
            //    MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
            //    return;
            //}
            var dir = new DirectoryInfo(GetRootFolder(SwModel));
            if (Properties.Settings.Default.CashModeOn)
            {
                LinkedList<ModelDoc2> models;
                GetAllUniqueModels(SwModel, out models);
                LinkedList<string> modelPaths = new LinkedList<string>();
                foreach (var modelDoc2 in models)
                {
                    modelPaths.AddLast(Path.GetFileNameWithoutExtension(modelDoc2.GetPathName()));
                }
                List<FileInfo> drawToDelete = new List<FileInfo>();
                foreach (FileInfo file in dir.GetFiles("*.SLDDRW", SearchOption.AllDirectories))
                {
                    if (modelPaths.Contains(Path.GetFileNameWithoutExtension(file.Name)))
                        _iSwApp.OpenDoc(file.FullName, (int)swDocumentTypes_e.swDocDRAWING);
                    else
                        drawToDelete.Add(file);
                }
                foreach (var fileInfo in drawToDelete)
                {
                    fileInfo.Delete();
                }
            }
            else
            {
                foreach (FileInfo file in dir.GetFiles("*.SLDDRW", SearchOption.AllDirectories))
                {
                    if (CheckAvailableMemory())
                        _iSwApp.OpenDoc(file.FullName, (int)swDocumentTypes_e.swDocDRAWING);
                    else
                    {
                        MessageBox.Show(@"Памяти не хватает на открытие всех чертежей!", MyTitle, MessageBoxButtons.OK,
                                        MessageBoxIcon.Exclamation);
                        break;
                    }

                }
            }
        }

        private bool CheckAvailableMemory()
        {
            var ci = new ComputerInfo();

            var avm = (double)ci.AvailablePhysicalMemory;
            var tvm = (double)ci.TotalPhysicalMemory;
            double rez = (avm / tvm);
            if (rez < 0.1)
                return false;
            return true;
        }

        #endregion

        #region Cut off segments
        public int CutOffEnable()
        {

            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            int ret = 0;
            if (_iSwApp.ActiveDoc != null)
                if (((ModelDoc2)_iSwApp.ActiveDoc).GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
                    ret = 1;
            return ret;
        }
        private bool CheckComponentsIntersection(Component2 component1, Component2 component2)
        {
            object b = component1.GetBox(true, true);
            if (b != null)
            {
                var boxs = (double[])b;
                double xs1 = boxs[0];
                double ys1 = boxs[1];
                double zs1 = boxs[2];
                double xs2 = boxs[3];
                double ys2 = boxs[4];
                double zs2 = boxs[5];
                b = component2.GetBox(true, true);
                if (b != null)
                {
                    var boxf = (double[])b;
                    double xf1 = boxf[0];
                    double yf1 = boxf[1];
                    double zf1 = boxf[2];
                    double xf2 = boxf[3];
                    double yf2 = boxf[4];
                    double zf2 = boxf[5];
                    if (((xf1 >= xs1 - 0.00000001 && xf1 - 0.00000001 <= xs2) || (xs1 >= xf1 - 0.00000001 && xs1 - 0.00000001 <= xf2)) &&
                        ((yf1 >= ys1 - 0.00000001 && yf1 - 0.00000001 <= ys2) || (ys1 >= yf1 - 0.00000001 && ys1 - 0.00000001 <= yf2)) &&
                        ((zf1 >= zs1 - 0.00000001 && zf1 - 0.00000001 <= zs2) || (zs1 >= zf1 - 0.00000001 && zs1 - 0.00000001 <= zf2)))
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }
        private bool CheckIfParent(Component2 parent, Component2 child)
        {
            if (parent.Name.Split('/').First() == child.Name.Split('/').First())
                return true;
            else
                return false;

        }
        private void CheckBoxIntersection(List<Component2> cutList, Component2 key, ref Dictionary<string, Component2> detalsToShow)//Dictionary<Component2, List<Component2>> dictionary,Component2 key,Component2 cut,ref Dictionary<string,Component2> detalsToShow ,bool showAll)
        {
            foreach (var cut in cutList)
            {
                foreach (var cut2 in cutList)
                {
                    if (cut == cut2)
                        continue;
                    if (CheckComponentsIntersection(cut, cut2) && !CheckIfParent(key, cut) && !CheckIfParent(key, cut2))
                    {
                        if (!detalsToShow.ContainsKey(key.Name))
                            detalsToShow.Add(key.Name, key);
                    }
                }
            }
        }
        private bool CheckComponentsIntersection2(Component2 cut, Feature feat, Component2 parentControl)
        {
            object boxb1 = cut.GetBox(true, true);
            object boxb2 = new object();
            object boxb3 = parentControl.GetBox(true, true);


            //int count=0;
            //var faces = feat.GetFaces();
            //double[] faceBox = null;
            //foreach (var f in faces)
            //{
            //    faceBox = (((Face)(f)).GetBody() as Body).GetBodyBox();
            //    //if ((boxb2 as double[]) == null)
            //    //    boxb2 = new double[6];

            //}
            //if (faceBox != null)
            //    boxb2 = faceBox;
            //else
            feat.GetBox(ref boxb2);

            if ((boxb2 as double[]) != null && boxb1 != null && boxb3 != null)
            {
                var box1 = (double[])boxb1;
                double xs1 = box1[0];
                double ys1 = box1[1];
                double zs1 = box1[2];
                double xs2 = box1[3];
                double ys2 = box1[4];
                double zs2 = box1[5];
                var box3 = (double[])boxb3;
                var box2 = (double[])boxb2;
                double xf1 = box2[0];
                double yf1 = box2[1];
                double zf1 = box2[2] + box3[2];
                double xf2 = box2[3];
                double yf2 = box2[4];
                double zf2 = box2[5] + box3[5];
                if ((((xf1 > xs1 && xf1 < xs2) || (xf1 > xs2 && xf1 < xs1)) &&
                     ((yf1 > ys1 && yf1 < ys2) || (yf1 > ys2 && yf1 < ys1)) &&
                      ((zf1 > zs1 && zf1 < zs2) || (zf1 > zs2 && zf1 < zs1))) ||
                    (((xf2 > xs1 && xf2 < xs2) || (xf2 > xs2 && xf2 < xs1)) &&
                     ((yf2 > ys1 && yf2 < ys2) || (yf2 > ys2 && yf2 < ys1)) &&
                      ((zf2 > zs1 && zf2 < zs2) || (zf2 > zs2 && zf2 < zs1))) ||
                    (((xf2 > xs1 && xf2 < xs2) || (xf2 > xs2 && xf2 < xs1)) &&
                     ((yf1 > ys1 && yf1 < ys2) || (yf1 > ys2 && yf1 < ys1)) &&
                      ((zf1 > zs1 && zf1 < zs2) || (zf1 > zs2 && zf1 < zs1))) ||
                    (((xf2 > xs1 && xf2 < xs2) || (xf2 > xs2 && xf2 < xs1)) &&
                     ((yf1 > ys1 && yf1 < ys2) || (yf1 > ys2 && yf1 < ys1)) &&
                      ((zf2 > zs1 && zf2 < zs2) || (zf2 > zs2 && zf2 < zs1))) ||
                    (((xf2 > xs1 && xf2 < xs2) || (xf2 > xs2 && xf2 < xs1)) &&
                     ((yf2 > ys1 && yf2 < ys2) || (yf2 > ys2 && yf2 < ys1)) &&
                      ((zf1 > zs1 && zf1 < zs2) || (zf1 > zs2 && zf1 < zs1))) ||
                    (((xf1 > xs1 && xf1 < xs2) || (xf1 > xs2 && xf1 < xs1)) &&
                     ((yf2 > ys1 && yf2 < ys2) || (yf2 > ys2 && yf2 < ys1)) &&
                      ((zf1 > zs1 && zf1 < zs2) || (zf1 > zs2 && zf1 < zs1))) ||
                    (((xf1 > xs1 && xf1 < xs2) || (xf1 > xs2 && xf1 < xs1)) &&
                     ((yf1 > ys1 && yf1 < ys2) || (yf1 > ys2 && yf1 < ys1)) &&
                      ((zf2 > zs1 && zf2 < zs2) || (zf2 > zs2 && zf2 < zs1))) ||
                    (((xf1 > xs1 && xf1 < xs2) || (xf1 > xs2 && xf1 < xs1)) &&
                     ((yf2 > ys1 && yf2 < ys2) || (yf2 > ys2 && yf2 < ys1)) &&
                      ((zf2 > zs1 && zf2 < zs2) || (zf2 > zs2 && zf2 < zs1)))
                    )
                    return true;
                else
                    return false;

            }
            return false;
        }

        private void CheckBoxIntersectionWithCavities(List<Component2> cutList, Component2 key)
        {
            //взять все фичи определенного типа из key
            var swFeat = key.FirstFeature();
            while (swFeat != null)
            {
                if (swFeat.GetTypeName2() == "ICE")
                {
                    foreach (var cut in cutList)
                    {
                        if (CheckComponentsIntersection2(cut, swFeat, key))
                        {
                            //cut.Select(false);
                            swFeat.Select(true);
                            MessageBox.Show("Пересечение! Деталь: " + key.GetPathName() + " фича:" + swFeat.Name, "Пересечение!", MessageBoxButtons.OK);
                            return;
                        }
                    }
                }
                swFeat = swFeat.IGetNextFeature();
            }

        }
        public void CutOffDetail()
        {
            //if (!CheckForTearOff())
            //{
            //    MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
            //    return;
            //}
            SelectionMgr swSelMgr = SwModel.ISelectionManager;
            int lngInfo = 0;
            const string cavFeatPrefix = "#MrDoorsCavity";

            if (swSelMgr.GetSelectedObjectCount() == 1)
            {
                var selComp = (Component2)swSelMgr.GetSelectedObjectsComponent2(1).GetParent();
                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;
                IsEventsEnabled = false;

                swModel.ClearSelection2(true);

                var swConfig = (Configuration)swModel.GetActiveConfiguration();
                if (swConfig != null)
                {
                    var swRootComponent = selComp;
                    DeleteCavities(swRootComponent);
                    swModel.EditRebuild3();
                }
                IsEventsEnabled = true;




                var swAsm = (AssemblyDoc)swModel;
                LinkedList<Component2> shelfComponents;
                LinkedList<Component2> cutComponents;
                ModelDoc2 swEditModel;
                Feature swLastFeature;
                var dictionary = new Dictionary<Component2, List<Component2>>();
                var detalsToShow = new Dictionary<string, Component2>();
                UserProgressBar pb;
                SwApp.GetUserProgressBar(out pb);
                int countPb = 0;
                bool del;
                if (GetCutComponents(swModel, out cutComponents, out shelfComponents, out del))
                {
                    pb.Start(0, (shelfComponents.Count * cutComponents.Count),
                             "Проверка на пересечение фурнитуры с деталями");
                    int c = 0;
                    foreach (Component2 shelfcomp in shelfComponents)
                    {
                        string[] names = shelfcomp.Name.Split('/');
                        bool findMatch = false;
                        foreach (var name in names)
                        {
                            if (name == selComp.Name)
                            {
                                findMatch = true;
                                break;
                            }
                        }
                        if (!findMatch)
                            continue;

                        List<Feature> list;
                        bool done = false;
                        var checkedCutComps = new List<Component2>();
                        if (IsCavities(shelfcomp, out list))
                        {
                            GetListCutComponents(swModel, list, out checkedCutComps);
                            done = true;
                        }
                        var sBox = (double[])shelfcomp.GetBox(false, false);

                        foreach (var cutcomp in cutComponents)
                        {
                            if (done && checkedCutComps.Contains(cutcomp)) continue;
                            var cBox = cutcomp.GetBox(false, false) as double[];
                            if (cBox == null)
                                continue;
                            double x1 = sBox[0];
                            double y1 = sBox[1];
                            double z1 = sBox[2];
                            double x2 = sBox[3];
                            double y2 = sBox[4];
                            double z2 = sBox[5];
                            double x3 = cBox[0];
                            double y3 = cBox[1];
                            double z3 = cBox[2];
                            double x4 = cBox[3];
                            double y4 = cBox[4];
                            double z4 = cBox[5];

                            if (CompareDouble(x1, x2, x3, x4) && CompareDouble(y1, y2, y3, y4) && CompareDouble(z1, z2, z3, z4))
                            {
                                var listCut = new List<Component2>();
                                if (dictionary.ContainsKey(shelfcomp))
                                {
                                    dictionary[shelfcomp].Add(cutcomp);
                                }
                                else
                                {
                                    listCut.Add(cutcomp);
                                    dictionary.Add(shelfcomp, listCut);
                                }
                                countPb++;
                            }
                            c++;
                            pb.UpdateProgress(c);
                        }
                    }
                    pb.End();
                }
                int i = 0;
                pb.Start(0, countPb, "Вычитание отверстий");
                Stopwatch stopwatch = new Stopwatch();
                string timeToFinish = string.Empty;
                foreach (var key in dictionary.Keys)
                {
                    if (key.Select2(false, 0) && swAsm.EditPart2(true, true, ref lngInfo) ==
                        (int)swEditPartCommandStatus_e.swEditPartSuccessful)
                    {
                        string[] names = key.Name.Split('/');
                        bool findMatch = false;
                        foreach (var name in names)
                        {
                            if (name == selComp.Name)
                            {
                                findMatch = true;
                                break;
                            }
                        }
                        if (!findMatch)
                            continue;

                        foreach (var cut in dictionary[key])
                        {
                            stopwatch.Restart();
                            cut.Select(false);
                            //CheckBoxIntersection(dictionary, key, cut, ref detalsToShow);
                            swAsm.InsertCavity4(0, 0, 0, true, 1, -1);
                            swEditModel = (ModelDoc2)key.GetModelDoc();
                            swLastFeature = (Feature)swEditModel.FeatureByPositionReverse(0);

                            if (swLastFeature.GetTypeName2() == "Cavity" && !swLastFeature.Name.Contains(cavFeatPrefix))
                            {
                                var swPreLastFeature = (Feature)swEditModel.FeatureByPositionReverse(1);
                                if (swPreLastFeature != null && swLastFeature.GetTypeName2() == "Cavity" &&
                                    swLastFeature.Name.Contains(cavFeatPrefix))
                                {
                                    int numb = Convert.ToInt32(swPreLastFeature.Name.Substring(21)) + 1;
                                    swLastFeature.Name = cavFeatPrefix + "Полость" + numb;
                                }
                                else
                                    swLastFeature.Name = cavFeatPrefix + swLastFeature.Name;
                            }
                            i++;
                            stopwatch.Stop();
                            TimeSpan tmp1 = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds * (countPb - i));
                            timeToFinish = tmp1.Hours + ":" + tmp1.Minutes + ":" + tmp1.Seconds;
                            pb.UpdateTitle("Вычитание отверстия " + i + " из " + countPb + " осталось: " + timeToFinish);
                            pb.UpdateProgress(i);
                        }
                    }
                }
                pb.End();
                swModel.ClearSelection();
                swAsm.EditAssembly();
                swModel.EditRebuild3();

            }
            else
            {
                return;
            }
        }

        public void CutOff()
        {
            IsEventsEnabled = false;
            var startTime = DateTime.Now;
            //if (!CheckForTearOff())
            //{
            //    MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
            //    return;
            //}
            try
            {
                UserProgressBar pb;
                SwApp.GetUserProgressBar(out pb);
                int lngInfo = 0;
                int countPb = 0;
                var dictionary = new Dictionary<Component2, List<Component2>>();
                const string cavFeatPrefix = "#MrDoorsCavity";
                LinkedList<Component2> cutComponents;
                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;
                var swAsm = (AssemblyDoc)swModel;

                swModel.ClearSelection2(true);
                swModel.EditRebuild3();

                #region Перезагрузка компонент, которые были погашены при отрыве

                var swConfig = (Configuration)swModel.GetActiveConfiguration();
                if (swConfig != null)
                {
                    var swRootComponent = (Component2)swConfig.GetRootComponent();
                    var swComponents = new LinkedList<Component2>();
                    if (GetComponents(swRootComponent, swComponents, true, false))
                    {
                        int q = 0;
                        pb.Start(0, swComponents.Count, "Перезагрузка компонент, которые были погашены при отрыве");
                        foreach (var swComponent in swComponents)
                        {
                            if (swComponent.IsSuppressed())
                            {
                                ModelDoc2 mod = swComponent.IGetModelDoc();
                                if (mod == null && GetModelByName(swModel, Path.GetFileName(swComponent.GetPathName()), true, out mod))
                                {
                                    if (mod.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY &&
                                        mod.get_CustomInfo2("", "Accessories") == "Yes")
                                    {
                                        swComponent.SetSuppression2(2);
                                        swModel.EditRebuild3();
                                        swComponent.SetSuppression2(0);
                                        break;
                                    }
                                }
                            }
                            q++;
                            pb.UpdateProgress(q);
                        }
                        swModel.ForceRebuild3(false);
                        pb.End();
                    }
                }

                #endregion

                LinkedList<Component2> shelfComponents;

                bool del;
                if (GetCutComponents(swModel, out cutComponents, out shelfComponents, out del))
                {
                    pb.Start(0, (shelfComponents.Count * cutComponents.Count),
                             "Проверка на пересечение фурнитуры с деталями");
                    int c = 0;
                    foreach (Component2 shelfcomp in shelfComponents)
                    {
                        List<Feature> list;
                        bool done = false;
                        var checkedCutComps = new List<Component2>();
                        if (IsCavities(shelfcomp, out list))
                        {
                            GetListCutComponents(swModel, list, out checkedCutComps);
                            done = true;
                        }
                        var sBox = (double[])shelfcomp.GetBox(false, false);

                        foreach (var cutcomp in cutComponents)
                        {
                            if (done && checkedCutComps.Contains(cutcomp)) continue;
                            double[] cBox = cutcomp.GetBox(false, false) as double[];
                            if (cBox == null)
                                continue;
                            double x1 = sBox[0];
                            double y1 = sBox[1];
                            double z1 = sBox[2];
                            double x2 = sBox[3];
                            double y2 = sBox[4];
                            double z2 = sBox[5];
                            double x3 = cBox[0];
                            double y3 = cBox[1];
                            double z3 = cBox[2];
                            double x4 = cBox[3];
                            double y4 = cBox[4];
                            double z4 = cBox[5];

                            if (CompareDouble(x1, x2, x3, x4) && CompareDouble(y1, y2, y3, y4) && CompareDouble(z1, z2, z3, z4))
                            {
                                var listCut = new List<Component2>();
                                if (dictionary.ContainsKey(shelfcomp))
                                {
                                    dictionary[shelfcomp].Add(cutcomp);
                                }
                                else
                                {
                                    listCut.Add(cutcomp);
                                    dictionary.Add(shelfcomp, listCut);
                                }
                                countPb++;
                            }
                            c++;
                            pb.UpdateProgress(c);
                        }
                    }
                    pb.End();
                }

                int i = 0;
                if (Properties.Settings.Default.CreatePacketHoles)
                    pb.Start(0, dictionary.Count, "Вычитание отверстий");
                else
                    pb.Start(0, countPb, "Вычитание отверстий");

                Stopwatch stopwatch = new Stopwatch();
                string timeToFinish = string.Empty;
                var detalsToShow = new Dictionary<string, Component2>();
                ModelDoc2 swEditModel;
                Feature swLastFeature;
                foreach (var key in dictionary.Keys)
                {
                    if (key.Select2(false, 0) && swAsm.EditPart2(true, true, ref lngInfo) ==
                        (int)swEditPartCommandStatus_e.swEditPartSuccessful)
                    {

                        if (Properties.Settings.Default.CreatePacketHoles)
                        {
                            stopwatch.Restart();
                            swModel.ClearSelection();
                            foreach (var cut in dictionary[key])
                            {
                                cut.Select(true);
                                //CheckBoxIntersection(dictionary, key, cut, ref detalsToShow,false);
                            }
                            CheckBoxIntersection(dictionary[key], key, ref detalsToShow);
                            if (Properties.Settings.Default.CheckCavitiesCross)
                                CheckBoxIntersectionWithCavities(dictionary[key], key);
                            swAsm.InsertCavity4(0, 0, 0, true, 1, -1);
                            swEditModel = (ModelDoc2)key.GetModelDoc();
                            swLastFeature = (Feature)swEditModel.FeatureByPositionReverse(0);
                            if (swLastFeature.GetTypeName2() == "Cavity" && !swLastFeature.Name.Contains(cavFeatPrefix))
                            {
                                var swPreLastFeature = (Feature)swEditModel.FeatureByPositionReverse(1);
                                if (swPreLastFeature != null && swLastFeature.GetTypeName2() == "Cavity" &&
                                    swLastFeature.Name.Contains(cavFeatPrefix))
                                {
                                    int numb = Convert.ToInt32(swPreLastFeature.Name.Substring(21)) + 1;
                                    swLastFeature.Name = cavFeatPrefix + "Полость" + numb;
                                }
                                else
                                    swLastFeature.Name = cavFeatPrefix + swLastFeature.Name;
                            }
                            i++;
                            stopwatch.Stop();
                            TimeSpan tmp = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds * (dictionary.Count - i));
                            timeToFinish = tmp.Hours + ":" + tmp.Minutes + ":" + tmp.Seconds;
                            pb.UpdateTitle("Деталь " + i + " из " + dictionary.Count + " осталось: " + timeToFinish);
                            pb.UpdateProgress(i);
                        }
                        else
                        {
                            foreach (var cut in dictionary[key])
                            {
                                stopwatch.Restart();
                                cut.Select(false);
                                //CheckBoxIntersection(dictionary, key, cut, ref detalsToShow);
                                swAsm.InsertCavity4(0, 0, 0, true, 1, -1);
                                swEditModel = (ModelDoc2)key.GetModelDoc();
                                swLastFeature = (Feature)swEditModel.FeatureByPositionReverse(0);

                                if (swLastFeature.GetTypeName2() == "Cavity" && !swLastFeature.Name.Contains(cavFeatPrefix))
                                {
                                    var swPreLastFeature = (Feature)swEditModel.FeatureByPositionReverse(1);
                                    if (swPreLastFeature != null && swLastFeature.GetTypeName2() == "Cavity" &&
                                        swLastFeature.Name.Contains(cavFeatPrefix))
                                    {
                                        int numb = Convert.ToInt32(swPreLastFeature.Name.Substring(21)) + 1;
                                        swLastFeature.Name = cavFeatPrefix + "Полость" + numb;
                                    }
                                    else
                                        swLastFeature.Name = cavFeatPrefix + swLastFeature.Name;
                                }
                                i++;
                                stopwatch.Stop();
                                TimeSpan tmp1 = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds * (countPb - i));
                                timeToFinish = tmp1.Hours + ":" + tmp1.Minutes + ":" + tmp1.Seconds;
                                pb.UpdateTitle("Вычитание отверстия " + i + " из " + countPb + " осталось: " + timeToFinish);
                                pb.UpdateProgress(i);
                            }
                        }
                    }
                    else
                        swModel.ClearSelection();
                }
                //endMark:
                pb.End();
                swModel.ClearSelection();
                swAsm.EditAssembly();
                swModel.EditRebuild3();

                var sTime = DateTime.Now - startTime;
                if (detalsToShow.Count == 0)
                {
                    MessageBox.Show(
                        @"Вычитание отверстий завершено за " + sTime.Minutes + @" минут " + sTime.Seconds + @" секунд",
                        MyTitle, MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    //проверить, вырезались ли отверстия на тех деталях, где есть подозрения на то что они могут не вырезаться.
                    List<string> toDelete = new List<string>();
                    foreach (var component in detalsToShow)
                    {
                        swEditModel = (ModelDoc2)component.Value.GetModelDoc();
                        var features = swEditModel.FeatureManager.GetFeatures(true);
                        foreach (var feature in features)
                        {
                            if (feature.Name.Contains(cavFeatPrefix))//(swLastFeature.GetTypeName2() == "Cavity" && swLastFeature.Name.Contains(cavFeatPrefix))
                            {
                                toDelete.Add(component.Key);
                            }
                        }

                    }
                    foreach (var str in toDelete)
                    {
                        if (detalsToShow.ContainsKey(str))
                            detalsToShow.Remove(str);
                    }
                    if (detalsToShow.Count != 0)
                    {
                        frmShowList frmShow = new frmShowList(detalsToShow.Keys.ToList(), false);
                        frmShow.Show();
                    }
                    else
                    {
                        MessageBox.Show(
                        @"Вычитание отверстий завершено за " + sTime.Minutes + @" минут " + sTime.Seconds + @" секунд",
                        MyTitle, MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    }
                }
                if (Properties.Settings.Default.CheckCavitiesCross &&
                    MessageBox.Show(@"Проверить вырезанные отверстия на пересечение?", MyTitle,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    CheckCutOff2(false);
            }
            catch
            {
                MessageBox.Show(@"Ошибка при вычитании отверстий", MyTitle, MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            IsEventsEnabled = true;
        }

        private static bool CompareDouble(double p1, double p2, double p3, double p4)
        {
            double x;
            if (p1 < p3)
            {
                x = p3 - p2;
                if (x < 0)
                    x = -x;
                if (x < 0.0000001 && x > 0)
                    return false;
                if (p3 < p2)
                    return true;
            }
            else
            {
                x = p1 - p4;
                if (x < 0)
                    x = -x;
                if (x < 0.0000001 && x > 0)
                    return false;
                if (p1 < p4)
                    return true;
            }
            return false;
        }

        private bool GetCutComponents(ModelDoc2 swModel,
            out LinkedList<Component2> outCutComponents, out LinkedList<Component2> outShelfComponents, out bool delete)
        {
            delete = false;
            var swComponents = new LinkedList<Component2>();
            ModelDoc2 swCompModel;

            outCutComponents = new LinkedList<Component2>();
            outCutComponents.Clear();

            outShelfComponents = new LinkedList<Component2>();
            outShelfComponents.Clear();

            var swConfig = (Configuration)swModel.GetActiveConfiguration();
            if (swConfig != null)
            {
                var swRootComponent = (Component2)swConfig.GetRootComponent();

                if (GetComponents(swRootComponent, swComponents, true, false))
                {
                    foreach (Component2 comp in swComponents)
                    {
                        swCompModel = (ModelDoc2)comp.GetModelDoc();
                        if (swCompModel != null)
                        {
                            if (swCompModel.get_CustomInfo2("", "swrfIsCut") == "Yes")
                            {
                                outCutComponents.AddLast(comp);
                            }
                            if (swCompModel.get_CustomInfo2("", "swrfIsShelf") == "Yes")
                            {
                                if (swCompModel.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
                                    swCompModel.DeleteCustomInfo2("", "swrfIsShelf");
                                else
                                {
                                    if (comp.GetTexture("") != null)
                                        swCompModel.Save();
                                    outShelfComponents.AddLast(comp);
                                }

                                #region Удаление старых ненужных отверстий
                                var swFeat = comp.FirstFeature();
                                while (swFeat != null)
                                {
                                    if (swFeat.GetTypeName2() == "Cavity")
                                    {
                                        if (swFeat.Name.Contains("#swrf"))
                                        {
                                            swFeat.Select(true);
                                            delete = true;
                                        }
                                    }
                                    swFeat = swFeat.IGetNextFeature();
                                }
                                if (delete)
                                {
                                    swModel.DeleteSelection(true);
                                    swModel.ClearSelection2(true);
                                    GC.Collect();
                                }
                                #endregion
                            }
                        }
                    }
                }
            }
            return true;
        }

        private void GetListCutComponents(ModelDoc2 swModel, IEnumerable<Feature> inListCavities, out List<Component2> checkedComponents)
        {
            checkedComponents = new List<Component2>();
            foreach (var inListCavity in inListCavities)
            {
                Component2 swComp;
                if (GetComponentByName(swModel, GetSpecialNameFromFeature(inListCavity.IGetFirstSubFeature().Name),
                                   false, out swComp, true))
                {
                    var mod = swComp.IGetModelDoc();
                    if (mod != null)
                    {
                        if (mod.get_CustomInfo2("", "swrfIsCut") == "Yes")
                            checkedComponents.Add(swComp);
                        else
                        {
                            var outComps = new LinkedList<Component2>();
                            if (GetComponents(swComp, outComps, true, false))
                            {
                                checkedComponents.AddRange(from component2 in outComps
                                                           let m = component2.IGetModelDoc()
                                                           where m.get_CustomInfo2("", "swrfIsCut") == "Yes"
                                                           select component2);
                            }
                        }
                    }
                }
                else
                {
                    if (GetComponentByName(swModel, GetSpecialNameFromFeature2(inListCavity.IGetFirstSubFeature().Name),
                                   false, out swComp, true))
                    {
                        var mod = swComp.IGetModelDoc();
                        if (mod != null && mod.get_CustomInfo2("", "swrfIsCut") == "Yes")
                            checkedComponents.Add(swComp);
                    }
                }
            }
        }
        //private static bool IsCavitiesNew(Component2 comp, out List<Feature> listCavities)
        //{
        //    bool ret = false;
        //    listCavities = new List<Feature>();
        //    var swFeat = comp.FirstFeature();
        //    while (swFeat != null)
        //    {

        //        if (swFeat.GetTypeName2() == "Cavity")
        //        {
        //            var swFeats = swFeat.GetChildren();
        //            var a = swFeat.IGetFirstSubFeature();
        //            var b = swFeat.GetAffectedFaces();
        //            var c = swFeat.GetBody();
        //            var d = swFeat.GetParents();
        //            var e = swFeat.IGetNextSubFeature();
        //            if (swFeats == null)
        //            {
        //                swFeat = swFeat.IGetNextFeature();
        //                continue;
        //            }
        //            foreach (var feat in swFeats)
        //            {
        //                listCavities.Add(feat);
        //                ret = true;
        //            }
        //        }
        //        swFeat = swFeat.IGetNextFeature();
        //    }
        //    return ret;
        //}
        private static bool IsCavities(Component2 comp, out List<Feature> listCavities)
        {
            bool ret = false;
            listCavities = new List<Feature>();
            var swFeat = comp.FirstFeature();
            while (swFeat != null)
            {
                if (swFeat.GetTypeName2() == "Cavity")
                {
                    listCavities.Add(swFeat);
                    ret = true;
                }
                swFeat = swFeat.IGetNextFeature();
            }
            return ret;
        }

        private static string GetSpecialNameFromFeature(string bigName)
        {
            return (bigName.Split('@').ToArray()[2].Split('/').First() + "-" +
                                                        bigName.Split('@').First().Split('<').Last().Split('>').
                                                            First() + "/" +
                                                        bigName.Split('@').ToArray()[1].Split('/').Last().Split('<')
                                                            .First() + "-" +
                                                        bigName.Split('@').ToArray()[1].Split('/').Last().Split('<')
                                                            .Last().Split('>').First() + "/" +
                                                        bigName.Split('@').ToArray()[2].Split('/').Last().Split('<')
                                                            .First() + "-" +
                                                        bigName.Split('@').ToArray()[2].Split('/').Last().Split('<')
                                                            .Last().Split('>').First());
        }

        private static string GetSpecialNameFromFeature2(string bigName)
        {
            return (bigName.Split('@').First().Split('<').First() + "-" +
                    bigName.Split('@').First().Split('<').Last().Split('>').First() + "/" +
                    bigName.Split('@').ToArray()[1].Split('/').Last().Split('<').First() + "-" +
                    bigName.Split('@').ToArray()[1].Split('/').Last().Split('<').Last().Split('>').First());
        }

        private bool CheckCrossBoxs(ModelDoc2 swModel, IEnumerable<Feature> inListCavities)
        {
            foreach (var inListCavity in inListCavities)
            {
                object b = null;
                if (inListCavity.GetBox(ref b))
                {
                    var boxs = (double[])b;
                    double xs1 = boxs[0];
                    double ys1 = boxs[1];
                    double zs1 = boxs[2];
                    double xs2 = boxs[3];
                    double ys2 = boxs[4];
                    double zs2 = boxs[5];
                    foreach (var listCavity in inListCavities)
                    {
                        if (GetParentCompNamFromFeature(inListCavity) != GetParentCompNamFromFeature(listCavity))
                        {
                            if (listCavity.GetBox(ref b))
                            {
                                var boxf = (double[])b;
                                double xf1 = boxf[0];
                                double yf1 = boxf[1];
                                double zf1 = boxf[2];
                                double xf2 = boxf[3];
                                double yf2 = boxf[4];
                                double zf2 = boxf[5];

                                if ((((xf1 > xs1 && xf1 < xs2) || (xs1 > xf1 && xs1 < xf2)) &&
                                     ((yf1 > ys1 && yf1 < ys2) || (ys1 > yf1 && ys1 < yf2)) &&
                                     ((zf1 > zs1 && zf1 < zs2) || (zs1 > zf1 && zs1 < zf2))) ||
                                    (((xf1 >= xs1 && xf1 <= xs2) || (xs1 >= xf1 && xs1 <= xf2)) &&
                                     ((yf1 > ys1 && yf1 < ys2) || (ys1 > yf1 && ys1 < yf2)) &&
                                     ((zf1 > zs1 && zf1 < zs2) || (zs1 > zf1 && zs1 < zf2))) ||
                                    (((xf1 > xs1 && xf1 < xs2) || (xs1 > xf1 && xs1 < xf2)) &&
                                     ((yf1 >= ys1 && yf1 <= ys2) || (ys1 >= yf1 && ys1 <= yf2)) &&
                                     ((zf1 > zs1 && zf1 < zs2) || (zs1 > zf1 && zs1 < zf2))) ||
                                    (((xf1 > xs1 && xf1 < xs2) || (xs1 > xf1 && xs1 < xf2)) &&
                                     ((yf1 > ys1 && yf1 < ys2) || (ys1 > yf1 && ys1 < yf2)) &&
                                     ((zf1 >= zs1 && zf1 <= zs2) || (zs1 >= zf1 && zs1 <= zf2))))
                                {
                                    string compName1, compName2;
                                    List<Component2> listHide;
                                    if (HideCompAndShowCross(swModel, inListCavity, listCavity, out compName1,
                                                             out compName2, out listHide))
                                    {
                                        ShowListHiddenComponent(listHide, compName1, compName2);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        private bool HideCompAndShowCross(ModelDoc2 swModel, Feature f1, Feature f2, out string comp1Name, out string comp2Name,
            out List<Component2> list)
        {
            return HideCompAndShowCross(swModel, GetParentCompNamFromFeature(f1), GetParentCompNamFromFeature(f2), out comp1Name, out comp2Name, out list);
        }
        private bool HideCompAndShowCross(ModelDoc2 swModel, string f1, string f2, out string comp1Name, out string comp2Name,
            out List<Component2> list)
        {
            bool ret = false;
            Component2 swComp1, swComp2;
            list = new List<Component2>();
            comp1Name = " ";
            comp2Name = " ";
            if (GetComponentByName(swModel, f1, false, out swComp1) &&
                GetComponentByName(swModel, f2, false, out swComp2))
            {
                var swComponents = new LinkedList<Component2>();
                comp1Name = swComp1.Name;
                comp2Name = swComp2.Name;
                if (comp1Name != comp2Name)
                {
                    var swConfig = (Configuration)swModel.GetActiveConfiguration();
                    if (swConfig != null)
                    {
                        var swRootComponent = (Component2)swConfig.GetRootComponent();

                        if (GetComponents(swRootComponent,
                                          swComponents, false, false))
                        {
                            foreach (var component in swComponents)
                            {
                                if ((component.Name != swComp1.Name) && (component.Name != swComp2.Name) &&
                                    (component.Visible == 1))
                                {
                                    component.Select(false);
                                    swModel.HideComponent2();
                                    swModel.ClearSelection();
                                    list.Add(component);
                                    ret = true;
                                }
                            }
                            return ret;
                        }
                    }
                }
            }
            return false;
        }

        private static string GetParentCompNamFromFeature(Feature feature)
        {
            return (feature.IGetFirstSubFeature().Name.Split('@').First().Split('<').First() + "-" +
                    feature.IGetFirstSubFeature().Name.Split('@').First().Split('<').Last().Split('>')
                        .First());
        }
        #endregion

        #region CheckCut
        public int CheckCutOffEnable()
        {
            if (Properties.Settings.Default.CashModeOn)
                return 0;
            if (Properties.Settings.Default.CreatePacketHoles)
                return 0;
            else
            {

                int ret = 0;
                if (_iSwApp.ActiveDoc != null)
                    if (((ModelDoc2)_iSwApp.ActiveDoc).GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
                        ret = 1;
                return ret;
            }
        }

        public void CheckCutOff()
        {
            if (!CheckForTearOff())
            {
                MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
                return;
            }
            if (!Properties.Settings.Default.CreatePacketHoles)
                CheckCutOff2(true);
            //else
            //{
            //    CheckCutOff3();
            //}
        }
        //private void CheckCutOff3()
        //{
        //    Stopwatch stopwatch = new Stopwatch();
        //    string timeToFinish = string.Empty;
        //    var detalsToShow = new Dictionary<string, Component2>();
        //    var swModel = (ModelDoc2)_iSwApp.ActiveDoc;
        //    LinkedList<Component2> cutComponents;
        //    LinkedList<Component2> shelfComponents;
        //    var dictionary = new Dictionary<Component2, List<Component2>>();
        //    UserProgressBar pb;
        //    var swAsm = (AssemblyDoc)swModel;
        //    int countPb = 0;
        //    int lngInfo = 0;
        //    const string cavFeatPrefix = "#MrDoorsCavity";
        //    SwApp.GetUserProgressBar(out pb);
        //    bool del;
        //    if (GetCutComponents(swModel, out cutComponents, out shelfComponents, out del))
        //    {
        //        pb.Start(0, (shelfComponents.Count * cutComponents.Count),
        //                 "Проверка на пересечение фурнитуры с деталями");

        //        int c = 0;
        //        foreach (Component2 shelfcomp in shelfComponents)
        //        {
        //            List<Feature> list;
        //            bool done = false;
        //            var checkedCutComps = new List<Component2>();
        //            if (IsCavities(shelfcomp, out list))
        //            {
        //                GetListCutComponents(swModel, list, out checkedCutComps);
        //                done = true;
        //            }
        //            var sBox = (double[])shelfcomp.GetBox(false, false);

        //            foreach (var cutcomp in cutComponents)
        //            {
        //                if (done && checkedCutComps.Contains(cutcomp)) continue;
        //                var cBox = (double[])cutcomp.GetBox(false, false);
        //                double x1 = sBox[0];
        //                double y1 = sBox[1];
        //                double z1 = sBox[2];
        //                double x2 = sBox[3];
        //                double y2 = sBox[4];
        //                double z2 = sBox[5];
        //                double x3 = cBox[0];
        //                double y3 = cBox[1];
        //                double z3 = cBox[2];
        //                double x4 = cBox[3];
        //                double y4 = cBox[4];
        //                double z4 = cBox[5];

        //                if (CompareDouble(x1, x2, x3, x4) && CompareDouble(y1, y2, y3, y4) && CompareDouble(z1, z2, z3, z4))
        //                {
        //                    var listCut = new List<Component2>();
        //                    if (dictionary.ContainsKey(shelfcomp))
        //                    {
        //                        dictionary[shelfcomp].Add(cutcomp);
        //                    }
        //                    else
        //                    {
        //                        listCut.Add(cutcomp);
        //                        dictionary.Add(shelfcomp, listCut);
        //                    }
        //                    countPb++;
        //                }
        //                c++;
        //                pb.UpdateProgress(c);
        //            }
        //        }
        //        pb.End();
        //    }
        //    int i = 0;
        //    pb.Start(0, dictionary.Count, "Проверка отверстий на пересечение.");
        //    foreach (var key in dictionary.Keys)
        //    {
        //        if (key.Select2(false, 0) && swAsm.EditPart2(true, true, ref lngInfo) ==
        //            (int)swEditPartCommandStatus_e.swEditPartSuccessful)
        //        {

        //            foreach (var cut in dictionary[key])
        //            {
        //                stopwatch.Restart();
        //                cut.Select(false);
        //                CheckBoxIntersection(dictionary, key, cut, ref detalsToShow, true);
        //                i++;
        //                stopwatch.Stop();

        //            }
        //        }
        //        else
        //            swModel.ClearSelection();
        //        TimeSpan tmp1 = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds * (countPb - i));
        //        timeToFinish = tmp1.Hours + ":" + tmp1.Minutes + ":" + tmp1.Seconds;
        //        pb.UpdateTitle("Проверка отверстий " + i + " из " + countPb + " осталось: " + timeToFinish);
        //        pb.UpdateProgress(i);
        //    }
        //    //endMark:
        //    pb.End();
        //    swModel.ClearSelection();
        //    swAsm.EditAssembly();
        //    swModel.EditRebuild3();

        //    //проверить, вырезались ли отверстия на тех деталях, где есть подозрения на то что они могут не вырезаться.

        //    if (detalsToShow.Count != 0)
        //    {
        //        frmShowList frmShow = new frmShowList(detalsToShow.Keys.ToList(), true);
        //        frmShow.Show();
        //    }
        //    else
        //    {
        //        MessageBox.Show(@"Проверка не выявила пересечений фурнитуры.",
        //        MyTitle, MessageBoxButtons.OK,
        //        MessageBoxIcon.Information);
        //    }
        //}
        private void CheckCutOff2(bool check)
        {
            if (check && (MessageBox.Show(@"Вырезать отверстия перед проверкой на пересечение?",
                MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
                CutOff();
            else
            {
                var swModel = (ModelDoc2)_iSwApp.ActiveDoc;
                swModel.EditRebuild3();

                bool onePartBoolForMsg = false, secPartBoolForMsg = false, delete;
                LinkedList<Component2> cutComp, shelfComp;
                if (GetCutComponents(swModel, out cutComp, out shelfComp, out delete))
                {
                    foreach (var comp in shelfComp)
                    {
                        List<Feature> list;
                        onePartBoolForMsg = delete;
                        secPartBoolForMsg = true;

                        if (IsCavities(comp, out list))
                        {
                            secPartBoolForMsg = false;
                            if (CheckCrossBoxs(swModel, list))
                                return;
                        }
                    }
                }
                if (onePartBoolForMsg && secPartBoolForMsg &&
                    (MessageBox.Show(
                        @"Все отверстия были удалены вследствие старого формата!Вырезать отверстия заново?",
                        MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
                    CutOff();
                else
                    MessageBox.Show(@"Фурнитура данной сборки пересечений друг с другом не имеет!", MyTitle,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region Показать скрытые компоненты
        public void ShowListHiddenComponent(List<Component2> inComponent, string name1, string name2)
        {
            if (_hidComp == null)
            {
                _hidComp = new ListHiddenComponent(this, inComponent, name1, name2);
                _hidComp.Disposed += ShowListHiddenComponentDispose;
                SetParent(_hidComp.Handle, _swHandle);
            }
        }

        public void ShowListHiddenComponentDispose(object sender, EventArgs e)
        {
            _hidComp = null;
        }
        #endregion

        public int RemoveCavityEnable()
        {
            //if (Properties.Settings.Default.CashModeOn)
            //    return 0;
            if (_iSwApp.ActiveDoc != null)
                return 1;
            return 0;
        }

        public void RemoveCavity()
        {
            //if (!CheckForTearOff())
            //{
            //    MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
            //    return;
            //}
            IsEventsEnabled = false;
            var swModel = SwApp.IActiveDoc2;

            swModel.ClearSelection2(true);

            var swConfig = (Configuration)swModel.GetActiveConfiguration();
            if (swConfig != null)
            {
                var swRootComponent = (Component2)swConfig.GetRootComponent();
                DeleteCavities(swRootComponent);
                swModel.EditRebuild3();
            }
            MessageBox.Show(@"Удаление отверстий завершено!", MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            IsEventsEnabled = true;
        }

        private static void DeleteCavities(Component2 swComp)
        {
            var childComps = (object[])swComp.GetChildren();
            foreach (var oChildComp in childComps)
            {
                var childComp = (Component2)oChildComp;
                var swCompModel = (ModelDoc2)childComp.GetModelDoc();
                if (swCompModel != null)
                {
                    var swModExt = swCompModel.Extension;
                    Feature swFeat = swCompModel.FirstFeature();
                    int i = 0;
                    while (swFeat != null)
                    {
                        if (swFeat.GetTypeName2() == "Cavity")
                        {
                            swFeat.Select2(true, i);
                            i++;
                        }
                        swFeat = swFeat.IGetNextFeature();
                    }
                    swModExt.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
                    DeleteCavities(childComp);
                }
            }
        }

        public int RemoveErrorCavityEnable()
        {
            if (Properties.Settings.Default.CashModeOn)
                return 0;
            if (_iSwApp.ActiveDoc != null)
                return 1;
            return 0;
        }

        public void RemoveErrorCavity()
        {
            if (!CheckForTearOff())
            {
                MessageBox.Show(@"Сборка не оторвана. Перейдите в режим кэша и нажмите ""Оторвать все""");
                return;
            }
            LinkedList<Component2> swComps;
            var list = GetAllCavitiesFeatures(out swComps);
            SwModel.EditRebuild3();
            SwModel.ClearSelection();
            foreach (var feature in list)
            {
                if (feature.GetErrorCode() != (int)swFeatureError_e.swFeatureErrorNone)
                    feature.Select(true);
            }
            SwModel.DeleteSelection(false);
        }

        public bool OpenModelDatabase(ModelDoc2 inModel, out OleDbConnection outDb)
        {
            bool ret = false;
            outDb = null;

            string strDbName = GetModelDatabaseFileName(inModel);
            if (strDbName != "")
            {
                int i = IntPtr.Size;
                outDb = i == 8
                            ? new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source = " + strDbName)
                            : new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;" + "data source = " + strDbName);

                try
                {
                    outDb.Open();
                    ret = true;
                }
                catch { }
            }
            return ret;
        }

        public string GetModelDatabaseFileName(ModelDoc2 inModel)
        {
            string ret = "";
            string strDbName = inModel.GetPathName();

            if (strDbName != "")
            {
                if (strDbName.Contains("_SWLIB_BACKUP")) // если берем из кэша, то не учитываем последние 3 символа перед расширением
                {
                    string fileName = Path.GetFileNameWithoutExtension(strDbName);
                    fileName = string.Format("{0}{1}", fileName.Substring(0, fileName.Length - 4), Path.GetExtension(strDbName));
                    strDbName = Path.Combine(Path.GetDirectoryName(strDbName), fileName);
                }
                strDbName = GetModelNameWithoutSuffix(strDbName);
                if (Path.GetExtension(strDbName).ToLower() == ".slddrw")
                    strDbName = Path.GetFileNameWithoutExtension(strDbName) + ".sldasm";

                string DBPathResult = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DBPath") == string.Empty ? Properties.Settings.Default.DBPath : Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DBPath");

                var dir = new DirectoryInfo(DBPathResult);
                //var dir = new DirectoryInfo(Properties.Settings.Default.DBPath); // старый вариант

                foreach (FileInfo file in dir.GetFiles(Path.GetFileName(strDbName) + ".mdb", SearchOption.AllDirectories))
                {
                    strDbName = file.FullName;
                    ret = strDbName;
                    break;
                }
            }
            return ret;
        }

        public bool GetParentLibraryComponent(Component2 swInComp, out Component2 swOutComp)
        {
            Component2 swComp = swInComp;
            swOutComp = null;

            do
            {
                var swCompModel = (ModelDoc2)swComp.GetModelDoc();

                if (swCompModel != null)
                {
                    string strDbName = GetModelDatabaseFileName(swCompModel);
                    if (strDbName != "")
                    {
                        swOutComp = swComp;
                        break;
                    }

                    var swFeat = (Feature)swCompModel.FirstFeature();

                    while (swFeat != null)
                    {
                        if (swFeat.GetTypeName2() == "RefPlane")
                        {
                            string strPlaneName = swFeat.Name;

                            if (strPlaneName.Length > 5)
                            {
                                if (strPlaneName.Substring(0, 1) == "#")
                                {
                                    swOutComp = swComp;
                                    break;
                                }
                            }
                        }
                        swFeat = (Feature)swFeat.GetNextFeature();
                    }
                    if (swOutComp != null)
                        break;
                }
                swComp = swComp.GetParent();
            }
            while (swComp != null);

            return (swOutComp != null);
        }

        public bool GetAllUniqueLibraryModels(ModelDoc2 inModel, out LinkedList<ModelDoc2> outModels)
        {
            bool ret = false;

            outModels = new LinkedList<ModelDoc2>();

            try
            {
                LinkedList<ModelDoc2> allUniqueModels;
                if (GetAllUniqueModels(SwModel, out allUniqueModels))
                {
                    foreach (ModelDoc2 mdoc in allUniqueModels)
                    {
                        if (GetModelDatabaseFileName(mdoc) != "")
                            outModels.AddLast(mdoc);
                    }
                }
                ret = true;
            }
            catch { }
            return ret;
        }

        public bool GetAllUniqueModels(ModelDoc2 inModel, out LinkedList<ModelDoc2> outModels)
        {
            Configuration swConfig;
            var swComponents = new LinkedList<Component2>();
            bool ret = false;

            outModels = new LinkedList<ModelDoc2>();
            try
            {
                outModels.AddLast(inModel);
                swConfig = (Configuration)inModel.GetActiveConfiguration();

                if (swConfig != null)
                {
                    var swRootComponent = (Component2)swConfig.GetRootComponent();

                    if (GetComponents(swRootComponent, swComponents, true, false))
                    {
                        foreach (Component2 comp in swComponents)
                        {
                            var swCompModel = (ModelDoc2)comp.GetModelDoc();

                            if (swCompModel != null)
                            {
                                ModelDoc2 model = swCompModel;
                                bool isModelAlreadyAdded = outModels.Any(mdoc => mdoc.GetPathName() == model.GetPathName());

                                if (!isModelAlreadyAdded)
                                    outModels.AddLast(swCompModel);
                            }
                        }
                    }
                }
                ret = true;
            }
            catch { }
            return ret;
        }

        public bool GetComponents(Component2 inParseComponent, LinkedList<Component2> outComponents,
            bool isGetSubComponents, bool isRecursive)
        {
            bool ret = false;

            try
            {
                if (!isRecursive)
                    outComponents.Clear();

                if (inParseComponent != null)
                {
                    var subComponents = (object[])inParseComponent.GetChildren();

                    // GetChildren иногда не возвращает сабкомпоненты. Приходится дублировать GetComponentsом
                    if (subComponents != null && subComponents.Length == 0)
                    {
                        var swCompModel = (ModelDoc2)inParseComponent.GetModelDoc();
                        var tt = ((AssemblyDoc)swCompModel).GetComponents(true);
                        if (tt.Length != 0)
                            subComponents = tt;
                    }

                    if (subComponents != null)
                        foreach (Component2 subSwComp in subComponents)
                        {
                            outComponents.AddLast(subSwComp);

                            if (isGetSubComponents)
                                GetComponents(subSwComp, outComponents, true, true);
                        }
                }
                ret = true;
            }
            catch { }
            return ret;
        }

        public bool SetAsmUnit(ModelDoc2 model, out LinkedList<Component2> swComps)
        {
            bool ret = false;
            Configuration swConfig;
            swComps = new LinkedList<Component2>();

            try
            {
                SetModelProperty(model, "AsmUnit", "", swCustomInfoType_e.swCustomInfoText, model.GetPathName());
                swConfig = (Configuration)model.GetActiveConfiguration();
                if (swConfig != null)
                {
                    var swRootComponent = (Component2)swConfig.GetRootComponent();
                    if (GetComponents(swRootComponent, swComps, true, false))
                    {
                        foreach (var component2 in swComps)
                        {
                            var mod = component2.IGetModelDoc();
                            if (mod != null)
                            {
                                bool isIndependent = (!string.IsNullOrEmpty(mod.GetCustomInfoValue("", "IsIndependent") as string) && mod.GetCustomInfoValue("", "IsIndependent") == "Yes");
                                if (isIndependent)
                                {
                                    SetModelProperty(mod, "AsmUnit", "", swCustomInfoType_e.swCustomInfoText, RootModel.GetPathName());
                                }
                                else
                                {
                                    var swParentComp = component2.GetParent();
                                    string val = swParentComp == null ? model.GetPathName() : swParentComp.GetPathName();
                                    SetModelProperty(mod, "AsmUnit", "", swCustomInfoType_e.swCustomInfoText, val);
                                }
                            }
                        }
                        ret = true;
                    }
                }
            }
            catch { }
            return ret;
        }

        internal string GetModelNameWithoutSuffix(string inModelName)
        {
            int lngSuffPos = Strings.InStr(inModelName, " #", CompareMethod.Text);

            if (lngSuffPos != 0)
                inModelName = Strings.Left(inModelName, lngSuffPos - 1) + Strings.Right(inModelName, 7);
            return inModelName;
        }

        private static string GetComponentNameWithoutSuffix(string inModelName)
        {
            //InStrRev, т.к. в случае подкомпонента в имени будет несколько суффиксов
            string split = inModelName.Split('/').FirstOrDefault();
            string[] tmp = split.Split('-');

            if (split != null && tmp.Length > 1)
            {
                string tt;
                if (!tmp[0].Contains('#'))
                {
                    tt = tmp[0] + "-" + tmp[1];
                }
                else
                    tt = tmp.FirstOrDefault();
                if (tt != null && (tt.Last() == 'P' || tt.Last() == 'p'))
                {
                    string retName = tt.Substring(0, tt.IndexOf('#')) + "-" + split.Split('-')[1];
                    return retName;
                }

            }
            int lngSuffPos = Strings.InStrRev(inModelName, " #", -1, CompareMethod.Text);
            int lngCompNumPos = Strings.InStrRev(inModelName, "-", -1, CompareMethod.Text);

            if (lngSuffPos != 0 && lngCompNumPos != 0)
            {
                string s1 = Strings.Left(inModelName, lngSuffPos - 1);
                string s2 = Strings.Right(inModelName, inModelName.Length - lngCompNumPos + 1);
                inModelName = s1 + s2;
            }
            return inModelName;

        }

        private static string GetRootFolder(ModelDoc2 inRootModel)
        {
            return Path.GetDirectoryName(inRootModel.GetPathName());
        }

        internal static string GetOrderName(ModelDoc2 inRootModel)
        {
            return Path.GetFileName(GetRootFolder(inRootModel));
        }

        internal static SwDMApplication GetSwDmApp()
        {
            var swDocMgrClsFactory = new SwDMClassFactory();
            return swDocMgrClsFactory.GetApplication("7DBAB8BFA71BAA4AD7A88E1ABC12D7437FE07A00F33DAAB0");
        }

        private static bool CheckSolid()
        {
            const ulong progId = 0x4ba5924e;
            bool ret = false;

            try
            {
                var sWcodeReal = (string)Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\SolidWorks\\Security", "Serial Number", "");
                UInt64 sWcode = Convert.ToUInt64(sWcodeReal.Substring(0, 4) + sWcodeReal.Substring(5, 4)) *
                    Convert.ToUInt64(sWcodeReal.Substring(10, 4) + sWcodeReal.Substring(15, 4));
                sWcode = sWcode ^ progId;
                string code = sWcode.ToString("X");

                try
                {

                    //RegistryKey readKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\addinSW");  // читаем ветку реестра
                    //string readKey = Properties.Settings.Default.AppRegCode;

                    string readKey = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("SerialKey");

                    if (readKey != string.Empty)
                    {
                        //string loadKey = (string)readKey.GetValue("SerialKey");
                        ret = (readKey == code.Substring(code.Length - 8, 8));  // Properties.Settings.Default.AppRegCode Старый вариант сохранение в свойство User

                    }
                    else
                    {
                        // Запись ключа 
                        //SaveInRegEdit(Properties.Settings.Default.AppRegCode);
                        Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("SerialKey", Properties.Settings.Default.AppRegCode);
                        ret = (Properties.Settings.Default.AppRegCode == code.Substring(code.Length - 8, 8));
                    }
                }
                catch (Exception ex)
                {
                    ret = (Properties.Settings.Default.AppRegCode == code.Substring(code.Length - 8, 8));
                    Logging.Log.Instance.Fatal(ex, ex.Message.ToString() + "CheckSolid()");

                }

            }
            catch { }

            return ret;
        }


        private bool CheckUpdate(out string newFileLocation)
        {
            newFileLocation = "";
            SwApp.Visible = true;
            var oldVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            long oldVer = GetNumberFromFileVersion(oldVersion);
            try
            {
                string newName;
                if (Directory.Exists("D:\\"))
                {
                    try
                    {
                        if (!Directory.Exists("D:\\MrDoors_Solid_Update"))
                            Directory.CreateDirectory("D:\\MrDoors_Solid_Update");
                        newName = "D:\\MrDoors_Solid_Update\\Furniture.dll";
                    }
                    catch (Exception)
                    {
                        if (!Directory.Exists("C:\\MrDoors_Solid_Update"))
                            Directory.CreateDirectory("C:\\MrDoors_Solid_Update");
                        newName = "C:\\MrDoors_Solid_Update\\Furniture.dll";
                    }
                }
                else
                {
                    if (!Directory.Exists("C:\\MrDoors_Solid_Update"))
                        Directory.CreateDirectory("C:\\MrDoors_Solid_Update");
                    newName = "C:\\MrDoors_Solid_Update\\Furniture.dll";
                }
                string targetTxtFile = Properties.Settings.Default.UpdateToTest ? "versiontest.txt" : "version.txt";
                //var reqFtp = (FtpWebRequest)WebRequest.Create(Properties.Settings.Default.FurnFtpPath + targetTxtFile);
                var reqFtp = (FtpWebRequest)WebRequest.Create(Furniture.Helpers.FtpAccess.resultFtp + targetTxtFile);
                reqFtp.Credentials = new NetworkCredential(Properties.Settings.Default.FurnFtpName,
                                                           Properties.Settings.Default.FurnFtpPass);
                reqFtp.Method = WebRequestMethods.Ftp.DownloadFile;
                var response = reqFtp.GetResponse();
                var stream = response.GetResponseStream();
                var reader = new StreamReader(stream);
                string newVersion = reader.ReadLine();
                int majorNew, majorOld;
                bool forceUpdate = false;
                if (int.TryParse(newVersion.Split('.')[1], out majorNew) && int.TryParse(oldVersion.Split('.')[1], out majorOld))
                {
                    if (majorNew > majorOld)
                        forceUpdate = true;
                }

                if (GetNumberFromFileVersion(newVersion) != oldVer)
                {
                    if (!forceUpdate)
                    {
                        if (MessageBox.Show(@"Доступна новая версия программы. Обновить?", @"MrDoors", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            return DownloadDll(stream, reqFtp, newVersion, newName, out newFileLocation);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Доступна новая версия программы. Программа будет принудительно обновлена.", @"MrDoors!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return DownloadDll(stream, reqFtp, newVersion, newName, out newFileLocation);
                    }
                }
                stream.Close();
                reqFtp.Abort();
            }
            catch
            {
                MessageBox.Show("Функция обновления временно недоступна.");
            }
            return false;
        }
        private bool DownloadDll(Stream stream, FtpWebRequest reqFtp, string newVersion, string newName, out string newFileLocation)
        {
            if (Directory.Exists(Furniture.Helpers.LocalAccounts.modelPathResult.Replace("_SWLIB_", "_SWLIB_BACKUP")))
            {
                string warnMessage =
                    @"ОБРАЩАЕМ ВАШЕ ВНИМАНИЕ ! В режиме работы КЭШ редактирование свойств деталей сборочных единиц, отдельных деталей и фурнитуры допускается ТОЛЬКО через меню РПД ! В случае ручного редактирования свойств (меню Файл\Свойства…)возможны критические ошибки в их программной обработке,приводящие к неверному изготовлению деталей на производстве и ошибочной комплектации заказов фурнитурой.";
                MessageBox.Show(warnMessage, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            stream.Close();
            reqFtp.Abort();
            //long size = GetSizeForFurnitureDll(Properties.Settings.Default.FurnFtpPath + newVersion);
            long size = GetSizeForFurnitureDll(Furniture.Helpers.FtpAccess.resultFtp + newVersion);
            UserProgressBar pb;
            SwApp.GetUserProgressBar(out pb);
            FileStream fileStream = null;
            try
            {
                fileStream = new FileInfo(newName).Create();
            }
            catch (Exception)
            {
                MessageBox.Show("Не возможно создать файл " + newName + "Возможно из-за того что там уже есть такой ридонли.");
            }

            var wc = new WebClient { Credentials = new NetworkCredential(Properties.Settings.Default.FurnFtpName, Properties.Settings.Default.FurnFtpPass) };
            var str =
                //wc.OpenRead(Properties.Settings.Default.FurnFtpPath + newVersion + "/Furniture.dll");
                wc.OpenRead(Furniture.Helpers.FtpAccess.resultFtp + newVersion + "/Furniture.dll");
            const int bufferSize = 1024;
            var z = (int)(size / bufferSize);
            var buffer = new byte[bufferSize];
            int readCount = str.Read(buffer, 0, bufferSize);
            pb.Start(0, z, "Скачивание программы");
            int i = 0;
            while (readCount > 0)
            {
                fileStream.Write(buffer, 0, readCount);
                readCount = str.Read(buffer, 0, bufferSize);
                pb.UpdateProgress(i);
                i++;
            }
            pb.End();
            fileStream.Close();
            wc.Dispose();
            newFileLocation = newName;
            return true;
        }
        private long GetSizeForFurnitureDll(string uri)
        {
            long s = 0;
            var reqFtp = WebRequest.Create(uri);
            reqFtp.Credentials = new NetworkCredential(Properties.Settings.Default.FurnFtpName,
                                                       Properties.Settings.Default.FurnFtpPass);
            reqFtp.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            WebResponse response;
            try
            {
                response = reqFtp.GetResponse();
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message);
                reqFtp.Abort();
                return 0;
            }
            var stream = response.GetResponseStream();
            var reader = new StreamReader(stream);
            bool isHtmlFormat = false;
            string g;
            bool err = true;
            do
            {
                try
                {
                    g = reader.ReadLine();
                    if (g == "<HTML>")
                    {
                        isHtmlFormat = true;
                        break;
                    }
                    if (g.Contains("Furniture.dll"))
                    {
                        var f = g.Substring(30).TrimStart();
                        var size = f.Split(' ').First();
                        s = Convert.ToInt64(size);
                    }
                }
                catch { err = false; }
            } while (err);

            if (isHtmlFormat)
            {
                err = true;
                do
                {
                    try
                    {
                        g = reader.ReadLine();
                        var proSize = g.Substring(20, g.IndexOf("<A") - 20).Trim();
                        var arr = proSize.Split(',');
                        string size = arr.Aggregate("", (current, a) => current + a);
                        s = Convert.ToInt64(size);
                    }
                    catch { err = false; }
                } while (err);
            }

            reqFtp.Abort();
            return s;
        }

        private static void UpdatePatchAfterQuestion(Dictionary<string, long> dict, long lS, string lN)
        {
            var listNameOfPatches = dict.Keys.ToList();
            listNameOfPatches.Sort((x, y) => UpdatingLib.GetDate(x).CompareTo(UpdatingLib.GetDate(y)));
            string lastPatch = listNameOfPatches.Last();
            long sizePatches = dict.Sum(l => l.Value);
            if ((UpdatingLib.GetDate(lN) > UpdatingLib.GetDate(lastPatch)) ||
                (UpdatingLib.GetDate(lN) == UpdatingLib.GetDate(lastPatch) && lS <= sizePatches))
            {
                var f = new UpdatingLib("L");
                f.Updating();
            }
            else
            {
                var f = new UpdatingLib("P");
                f.Updating();
            }
        }

        private void UpdatePatch()
        {
            long lS;
            string lN;
            bool isCritical = false;
            var dict = UpdatingLib.DefineSizeOfFiles(out lS, out lN, out isCritical);
            if (dict.Count > 0)
            {
                if (isCritical)
                {
                    if (MessageBox.Show(
                        @"Доступно новое КРИТИЧЕСКОЕ обновление библиотеки." + Environment.NewLine +
                        @"ВАЖНО!!! ВСЕ ВАШИ СОБСТВЕННЫЕ НАРАБОТКИ В ПАПКЕ " + Furniture.Helpers.LocalAccounts.modelPathResult +
                        @" БУДУТ УДАЛЕНЫ!" + Environment.NewLine +
                        @"ПОЖАЛУЙСТА СОХРАНИТЕ ВСЕ СВОИ ДОПОЛНЕНИЯ К БИБЛИОТЕКЕ В ОТДЕЛЬНОЙ ПАПКЕ!", @"MrDoors",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        UpdatePatchAfterQuestion(dict, lS, lN);

                    }
                }
                else
                {
                    if (MessageBox.Show(
                        @"Доступно новое обновление библиотеки. Обновить?" + Environment.NewLine +
                        @"ВАЖНО!!! ВСЕ ВАШИ СОБСТВЕННЫЕ НАРАБОТКИ В ПАПКЕ " + Furniture.Helpers.LocalAccounts.modelPathResult +
                        @" БУДУТ УДАЛЕНЫ!" + Environment.NewLine +
                        @"ПОЖАЛУЙСТА СОХРАНИТЕ ВСЕ СВОИ ДОПОЛНЕНИЯ К БИБЛИОТЕКЕ В ОТДЕЛЬНОЙ ПАПКЕ!", @"MrDoors",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var listNameOfPatches = dict.Keys.ToList();
                        listNameOfPatches.Sort((x, y) => UpdatingLib.GetDate(x).CompareTo(UpdatingLib.GetDate(y)));
                        string lastPatch = listNameOfPatches.Last();
                        long sizePatches = dict.Sum(l => l.Value);
                        //if ((UpdatingLib.GetDate(lN) > UpdatingLib.GetDate(lastPatch)) ||
                        //    (UpdatingLib.GetDate(lN) == UpdatingLib.GetDate(lastPatch) && lS <= sizePatches))
                        if ((UpdatingLib.GetDate(lN) > UpdatingLib.GetDate(lastPatch)) && (lS <= sizePatches))
                        {
                            var f = new UpdatingLib("L");
                            f.Updating();
                        }
                        else
                        {
                            var f = new UpdatingLib("P");
                            f.Updating();
                        }
                    }
                    else
                        return;
                }
            }
            else
                if (lS > 0 &&
                MessageBox.Show(@"Доступно новое обновление библиотеки. Обновить?" + Environment.NewLine +
                    @"ВАЖНО!!! ВСЕ ВАШИ СОБСТВЕННЫЕ НАРАБОТКИ В ПАПКЕ " + Furniture.Helpers.LocalAccounts.modelPathResult +
                    @" БУДУТ УДАЛЕНЫ!" + Environment.NewLine +
                    @"ПОЖАЛУЙСТА СОХРАНИТЕ ВСЕ СВОИ ДОПОЛНЕНИЯ К БИБЛИОТЕКЕ В ОТДЕЛЬНОЙ ПАПКЕ!", @"MrDoors", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var f = new UpdatingLib("L");
                    f.Updating();
                }
        }

        private static long GetNumberFromFileVersion(string name)
        {
            long number = 0;
            try
            {
                do
                {
                    int start = name.IndexOf('.');
                    name = name.Remove(start, 1);
                } while (name.Contains('.'));
                number = Convert.ToInt64(name);
            }
            catch { }
            return number;
        }

        public bool SetModelProperty(ModelDoc2 model, string fieldName, string cfgName, swCustomInfoType_e fieldType, string val, bool checkFirst = false)
        {
            bool isPropertyChanged = false;
            string fieldValue = string.Empty;
            if (checkFirst)
            {
                fieldValue = model.GetCustomInfoValue(cfgName, fieldName);
                if (fieldValue == val)
                    return isPropertyChanged;
            }
            if (!string.IsNullOrEmpty(fieldValue.Trim()) && fieldValue != "-" && !fieldValue.Contains("Color"))
                isPropertyChanged = true;
            model.AddCustomInfo3(cfgName, fieldName, (int)fieldType, val);
            model.set_CustomInfo2(cfgName, fieldName, val);
            return isPropertyChanged;
        }

        public string CorrectDecimalSymbol(string inValue, bool blToSystem, bool blToDot)
        {
            string ret;
            const double dbl = 0.1;

            if ((dbl.ToString().Substring(2, 1) == "." && blToSystem) || (blToDot && !blToSystem))
            {
                ret = inValue.Replace(",", ".");
            }
            else
            {
                ret = inValue.Replace(".", ",");
            }
            return ret;
        }

        private string CheckIsCompInOurLib(string swModelPath)
        {
            if (!File.Exists(swModelPath))
            {
                foreach (var file in Directory.GetFiles(Furniture.Helpers.LocalAccounts.modelPathResult,
                    GetModelNameWithoutSuffix(Path.GetFileName(swModelPath)), SearchOption.AllDirectories))
                {
                    if (file != " ")
                        return file;
                }
            }
            return swModelPath;
        }

        internal string GetXNameForAssembly(bool check = true, string nameDir = "")
        {
            string number = check ? GetOrderName(SwModel) : nameDir;
            string orderNumber = "";
            try
            {
                var orderNumberArrays = number.Split('-');
                orderNumber = orderNumberArrays.Aggregate(orderNumber, (current, orderNumberArray) => current + orderNumberArray);
                ulong codeName = Convert.ToUInt64(orderNumber);
                orderNumber = codeName.ToString("X");
            }
            catch
            {
                if (check)
                    MessageBox.Show(@"В имени заказа должны присутствовать только цифры и символ '-' !", @"MrDoors",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                orderNumber = "";
            }
            return orderNumber;
        }

        private bool CheckSaving(string defaultPath)
        {
            bool ret = false;
            if (defaultPath == "")
            {
                MessageBox.Show(@"Сборка не сохранена! Будет сохранена в принудительном порядке!", MyTitle,
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                SaveAssembly(SelectDirectoryForSaving());
                ret = true;
            }
            else
            {
                if (defaultPath.Length > 140)
                {
                    string oldPath = defaultPath;
                    while (defaultPath.Length > 140)
                    {
                        MessageBox.Show(
                            @"Выбранный Вами путь слишком велик, сборка будет сохранена по другому пути!
                     Пожалуйста,выберите новую папку.", MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        defaultPath = SelectDirectoryForSaving();
                    }
                    SaveAssembly(defaultPath);
                    File.Delete(oldPath);
                }
                else
                {

                    if (GetXNameForAssembly(false, defaultPath.Split('\\').Last().Split('.').First()) == "")
                    {
                        MessageBox.Show(@"В имени заказа должны присутствовать только цифры и символ '-' !", MyTitle,
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SaveAssembly(SelectDirectoryForSaving());
                        File.Delete(defaultPath);
                    }
                    string nameFile = defaultPath.Split('\\').Last();
                    string nameFileWithoutExt = nameFile.Split('.').First();
                    string nameDir = defaultPath.Split('\\').ToArray()[defaultPath.Split('\\').Count() - 2];
                    if (nameFileWithoutExt != nameDir)
                    {
                        int len = Strings.InStr(defaultPath, nameFile, CompareMethod.Text);
                        string newDir = Strings.Left(defaultPath, len - 1) + nameFileWithoutExt;
                        string newPath = newDir + "\\" + nameFile;
                        if (!Directory.Exists(newDir))
                            Directory.CreateDirectory(newDir);
                        SaveAssembly(newPath);
                        File.Delete(defaultPath);
                    }
                }
            }
            return ret;
        }

        private void SaveAssembly(string defaultPath)
        {
            SwApp.IActiveDoc2.SaveAs(defaultPath);
            SwApp.IActiveDoc2.EditRebuild3();
        }

        private string SelectDirectoryForSaving()
        {
            IsEventsEnabled = false;
            BrowserDialog = new FolderBrowserDialog { Description = @"Выберите папку с номером заказа" };
            do
            {
                if (BrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string path = BrowserDialog.SelectedPath;
                    string nameFolder = path.Split('\\').Last();
                    if (GetXNameForAssembly(false, nameFolder) == "")
                    {
                        MessageBox.Show(@"В имени заказа должны присутствовать только цифры и символ '-' !", @"MrDoors",
                                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        continue;
                    }
                    if (CheckTopDirectorysForIncludingAssemblies(path))
                    {
                        string newFileName = path + "\\" + nameFolder + ".SLDASM";
                        if (File.Exists(newFileName))
                        {
                            MessageBox.Show(@"Выберете другую папку, данная папка уже содержит сборку", MyTitle,
                                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                            continue;
                        }
                        if (newFileName.Length > 140)
                        {
                            MessageBox.Show(
                                @"Выбранный Вами путь слишком велик, сборка будет сохранена по другому пути!
                                Пожалуйста,выберите новую папку.",
                                MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            continue;
                        }
                        IsEventsEnabled = true;
                        return newFileName;
                    }
                    MessageBox.Show(
                        @"Выберите другую папку,так как Вы производите сохранение в одну из
                      подпапок  папки,содержащих сборку",
                        MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }
            } while (hook != null);
            return string.Empty;
        }

        private static bool CheckTopDirectorysForIncludingAssemblies(string defaultPath)
        {
            try
            {
                do
                {
                    var files = Directory.GetFiles(defaultPath, "*.SLDASM", SearchOption.TopDirectoryOnly);
                    if (files.Count() > 0 && !(files.Count() == 1 && files[0].Contains('$')))
                        return false;
                    defaultPath = defaultPath.Remove(defaultPath.LastIndexOf('\\'));
                } while (defaultPath.Contains('\\'));
            }
            catch { }
            return true;
        }

        public int UpdateLib()
        {
            if (MessageBox.Show(@"Обновить библиотеку?", @"MrDoors", MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var f = new UpdatingLib("L");
                SetParent(f.Handle, _swHandle);
                f.Updating();
            }
            return 1;
        }

        private static bool DownloadUpdFrn(out string path)
        {
            bool ret = true;
            path = "";
            try
            {
                path = Directory.Exists(@"D:\") ? @"D:\download_update_furniture.exe" : @"C:\download_update_furniture.exe";
                string pathUpd = Directory.Exists(@"D:\") ? @"D:\update_furniture.exe" : @"C:\update_furniture.exe";
                if (File.Exists(path) && File.Exists(pathUpd))
                    return true;
                var wc = new WebClient { Credentials = new NetworkCredential("solidk", "KSolid") };
                var fileStream = new FileInfo(path).Create();

                //var str = wc.OpenRead("ftp://194.84.146.5/download_update_furniture.exe");
                var str = wc.OpenRead(Furniture.Helpers.FtpAccess.resultFtp + "download_update_furniture.exe");

                const int bufferSize = 1024;
                var buffer = new byte[bufferSize];
                int readCount = str.Read(buffer, 0, bufferSize);
                while (readCount > 0)
                {
                    fileStream.Write(buffer, 0, readCount);
                    readCount = str.Read(buffer, 0, bufferSize);
                }
                str.Close();
                fileStream.Close();
                fileStream = new FileInfo(pathUpd).Create();
                //str = wc.OpenRead("ftp://194.84.146.5/update_furniture.exe");
                str = wc.OpenRead(Furniture.Helpers.FtpAccess.resultFtp + "update_furniture.exe");

                readCount = str.Read(buffer, 0, bufferSize);
                while (readCount > 0)
                {
                    fileStream.Write(buffer, 0, readCount);
                    readCount = str.Read(buffer, 0, bufferSize);
                }
                str.Close();
                fileStream.Close();
                wc.Dispose();
            }
            catch
            {
                ret = false;
            }
            return ret;
        }

        private void ReloadFirstLayerComponent()
        {
            IsEventsEnabled = false;

            var outComps = new LinkedList<Component2>();
            if (GetComponents(SwModel.IGetActiveConfiguration().IGetRootComponent2(), outComps,
                              false, false) && outComps.Count > 0)
            {
                if (Properties.Settings.Default.ReloadComponents)
                {
                    foreach (var component2 in outComps)
                    {
                        if (component2.Select(false))
                            ((AssemblyDoc)SwModel).ComponentReload();
                    }
                    MessageBox.Show(@"Перезагрузка была проведена успешно!", MyTitle, MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
                _neededReloadFirstLayerComponent = false;
            }
            IsEventsEnabled = true;
            _wasDocChange = false;
        }

        private bool ExistingFileDictionaryWithMdb(out string newDictFileName)
        {
            string s = SwModel.GetPathName();
            string directoryPath = s.Substring(0, s.LastIndexOf("\\") + 1);
            newDictFileName = directoryPath + GetOrderName(SwModel) + "_dictionary.txt";
            return File.Exists(newDictFileName);
        }

        private void CreateRelativePathAndWriteToFile(string fileName, params string[] paths)
        {
            string dirName = GetRootFolder(SwModel);
            var list = from path in paths select path.Substring(dirName.Length);
            File.WriteAllLines(fileName, list);
            File.SetAttributes(fileName, FileAttributes.Hidden);
        }

        private void RebuildEquation(Component2 component)
        {
            ModelDoc2 model = component.IGetModelDoc();
            if (model != null && model.GetConfigurationCount() > 1 &&
                                            model.GetEquationMgr().GetCount() > 0)
            {
                var compList = new LinkedList<Component2>();
                if (GetComponents(component, compList, true, false))
                {
                    foreach (var component2 in compList)
                    {
                        int eqCount = model.GetEquationMgr().GetCount();
                        for (int i = 0; i <= eqCount; i++)
                        {
                            var name = component2.Name;
                            string eq = model.GetEquationMgr().get_Equation(i);
                            if (eq.Contains(Path.GetFileNameWithoutExtension(component2.GetPathName())))
                            {
                                if (component2.IsSuppressed())
                                {
                                    name = component2.Name;
                                    if (compList.Where(x =>
                                                       x.GetPathName() ==
                                                       component2.GetPathName() &&
                                                       x.IsSuppressed() == false).Count() >
                                        0)
                                        model.GetEquationMgr().Suppression[i] = false;
                                    else
                                        model.GetEquationMgr().Suppression[i] = true;
                                }
                                else
                                    model.GetEquationMgr().Suppression[i] = false;
                            }
                            else
                            {
                                string fn = Path.GetFileNameWithoutExtension(component2.GetPathName());

                                if (Properties.Settings.Default.CashModeOn && fn.Length > 5 && eq.Length > 5 && fn.ToUpper().Last() == 'P' && eq.Contains(fn.Substring(0, fn.Length - 4)))
                                {
                                    //поменять это уравнение
                                    string orig = fn.Substring(0, fn.Length - 4);
                                    string whatToReplace = eq.Substring(eq.IndexOf(orig) + orig.Length, 4);
                                    string replaceWith = fn.Substring(fn.Length - 4, 4);
                                    string newEq = eq.Replace(whatToReplace, replaceWith);
                                    model.GetEquationMgr().set_Equation(i, newEq);
                                }
                            }
                        }
                        if (model.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
                            RebuildEquation(component2);
                    }
                }
                model.ForceRebuild3(true);
            }
        }

        private void WriteLogInfoOnServer()
        {
            try
            {
                string relVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string terminal = Environment.MachineName;
                string vUsrName = Environment.UserName;
                byte[] bytes = Encoding.ASCII.GetBytes("relnum=" + relVersion + "&terminal=" + terminal + "&username=" + vUsrName + "&state=EDIT");
                SendDataOnServer(bytes);
            }
            catch { }
        }

        private string SendDataOnServer(byte[] data)
        {
            ////Logging.Log.Instance.Debug(Encoding.Default.GetString(data));
            //string ret;
            //try
            //{
            //    const string url = "https://www.scentre.ru/pls/webguest/!WEB.PK_SW.ShowForm?";
            //    ServicePointManager.CertificatePolicy = new MyPolicy();
            //    var request = (HttpWebRequest)WebRequest.Create(url);
            //    request.Method = "POST";
            //    request.KeepAlive = false;
            //    request.ProtocolVersion = HttpVersion.Version10;
            //    request.Proxy = null;
            //    request.Credentials = new NetworkCredential("swrguest", "sg4829");

            //    Stream reqStream = request.GetRequestStream();f
            //    reqStream.Write(data, 0, data.Length);
            //    reqStream.Close();

            //    var response = (HttpWebResponse)request.GetResponse();
            //    ret = new StreamReader(response.GetResponseStream()).ReadToEnd();
            //}
            //catch
            //{
            //    ret = "";
            //}
            //return ret;
            return Encoding.ASCII.GetString(data);
        }
    }

    #region Classes

    class MyPolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
        {
            return true;
        }
    }


    public class CopiedFileNames
    {
        public string OldName = "";
        public string NewName = "";

        public CopiedFileNames(string inOldName, string inNewName)
        {
            OldName = inOldName;
            NewName = inNewName;
        }
    }


    class ModelTexture
    {
        public string TextureName;
        public int SortOrder;

        public ModelTexture(string inTextureName, int inSortOrder)
        {
            TextureName = inTextureName;
            SortOrder = inSortOrder;

            string[] nameArr = TextureName.Split('\\');
            if (nameArr.Length > 1)
                TextureName = nameArr[nameArr.Length - 1];
        }
    }


    class ModelDrawingNumber : IComparable
    {
        public string DrwModel;
        public ModelDoc2 Model;
        public DateTime Time;

        public ModelDrawingNumber(string drwModel, ModelDoc2 model, DateTime time)
        {
            DrwModel = drwModel;
            Model = model;
            Time = time;
        }

        public int CompareTo(object obj)
        {
            return Time.CompareTo(((ModelDrawingNumber)obj).Time);
        }
    }


    class ModelTextureComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((ModelTexture)x).SortOrder - ((ModelTexture)y).SortOrder;
        }
    }


    class DrawingHole
    {
        public Curve SwCurve;
        public double Radius;
        public double Depth;
        public double Cx, Cy, Cz;
        public bool IsProcessed;

        public DrawingHole(Curve inCurve, double inRadius, double inDepth, double inCx, double inCy, double inCz)
        {
            SwCurve = inCurve;
            Radius = Math.Round(inRadius, 4);
            Depth = Math.Round(inDepth, 4);

            Cx = inCx;
            Cy = inCy;
            Cz = inCz;
        }
    }


    class DrawingHoleBlock
    {
        public string BlockFileName;
        public double Radius;
        public double Depth;

        public DrawingHoleBlock(string inBlockFileName, double inRadius, double inDepth)
        {
            BlockFileName = inBlockFileName;
            Radius = Math.Round(inRadius, 4);
            Depth = Math.Round(inDepth, 4);
        }
    }
    #endregion
}
