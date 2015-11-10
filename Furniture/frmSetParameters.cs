using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swcommands;
using Environment = System.Environment;


namespace Furniture
{
    public partial class FrmSetParameters : Form
    {
        private readonly SwAddin _mSwAddin;
        private CheckBox _chkMeasure;
        private TabPage _tabDec;
        private readonly LinkedList<Label> _lblPrm = new LinkedList<Label>();
        private readonly LinkedList<Button> _btnPrm = new LinkedList<Button>();
        private readonly ModelDoc2 _swModel;
        private readonly AssemblyDoc _swAsmDoc;
        private readonly SelectionMgr _swSelMgr;
        private ModelDoc2 _swSelModel;
        private Component2 _swSelComp;
        internal bool IsNewSelectionDisabled;
        private bool _isMate;
        private readonly Dictionary<ComboBox, Dictionary<int, string>> _dictionary =
            new Dictionary<ComboBox, Dictionary<int, string>>();
        private readonly Dictionary<TabPage, int> _dictHeightPage = new Dictionary<TabPage, int>();
        private readonly Dictionary<ComboBox, string> _dictPathPict = new Dictionary<ComboBox, string>();
        private readonly Dictionary<DecorComponentsWithCombo, Dictionary<int, string>> _dictConfig =
            new Dictionary<DecorComponentsWithCombo, Dictionary<int, string>>();
        private bool _setNewListForComboBox = true;
        private readonly List<object> _objListChanged = new List<object>();
        private readonly List<object> _commonList = new List<object>();
        private readonly Dictionary<Button, TextBox> _butPlusTxt = new Dictionary<Button, TextBox>();
        private List<DimensionConfiguration> _dimensionConfig = new List<DimensionConfiguration>();
        private readonly Dictionary<TextBox, List<int>> _textBoxListForRedraw = new Dictionary<TextBox, List<int>>();
        private readonly Dictionary<int, TextBox> _numbAndTextBoxes = new Dictionary<int, TextBox>();
        private readonly Dictionary<ComboBox, List<int>> _comboBoxListForRedraw = new Dictionary<ComboBox, List<int>>();
        private readonly List<string> _namesOfColumnNameFromDimLimits = new List<string>();
        private int minimumconf = 0;
        private List<ref_object> refobj = null;
        private List<Control> controlsToHideWhenChangeMode = new List<Control>();
        private Dictionary<Control, int> packControlsWhenChangeMode = new Dictionary<Control, int>();
        private List<DimensionConfForList> listForDimensions = new List<DimensionConfForList>();

        public AssemblyDoc SwAsmDoc
        {
            get { return _swAsmDoc; }
        }

        public FrmSetParameters(SwAddin swAddin)
        {
            InitializeComponent();
            bitmap = Properties.Resources.Brush;
            tabMain.AutoSize = true;
            KeyDown += FormKeyDown;

            _mSwAddin = swAddin;
            _swModel = (ModelDoc2)_mSwAddin.SwApp.ActiveDoc;
            _swSelMgr = (SelectionMgr)_swModel.SelectionManager;

            _swAsmDoc = (AssemblyDoc)_swModel;
            _swAsmDoc.NewSelectionNotify += NewSelection;
            _swAsmDoc.AddItemNotify += AddNewItem;
            tabMain.Selected += TabMainTabSelected;
            tabMain.SelectedIndexChanged += TabMainSelectedIndexChanged;
            if (!Properties.Settings.Default.SetParentWindow)
                TopMost = true;
            Show();
            NewSelection();
        }

        private int AddNewItem(int entityType, string itemName)
        {
            if (entityType == (int)swNotifyEntityType_e.swNotifyFeature)
            {
                var swFeat = (Feature)_swAsmDoc.FeatureByName(itemName);
                if (swFeat != null)
                {
                    var swMate = (Mate2)swFeat.GetSpecificFeature2();
                    if (swMate != null)
                        _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                }
            }
            return 0;
        }

        internal int NewSelection()
        {
            if (tabMain.SelectedTab == tbpPos)
            {
                return 0;
            }

            pnlMode = null;
            //rbMode1 = null;
            //rbMode2 = null;
            packControlsWhenChangeMode = new Dictionary<Control, int>();
            controlsToHideWhenChangeMode = new List<Control>();

            int downPos = 0;
            if (_mSwAddin.OpenDrawingEnable() > 0)
            {
                SelectionMgr swSelMgr = _swModel.ISelectionManager;
                if (swSelMgr.GetSelectedObjectCount() == 1)
                {
                    var swTestSelComp = (Component2)swSelMgr.GetSelectedObjectsComponent2(1);
                    RefreshLabel(swTestSelComp);
                }
                //linkLabel1.Name = string.Empty;
                //linkLabel1.Text = @"ЭСКИЗ № н/о";
                //linkLabel1.Click += LinkLabel1Click;
            }
            else
            {
                linkLabel1.Name = string.Empty;
                linkLabel1.Text = string.Empty;
            }
            try
            {
                if (!IsNewSelectionDisabled && !((_chkMeasure != null) ? _chkMeasure.Checked : false))
                {
                    IsNewSelectionDisabled = true;
                    if (_swSelMgr.GetSelectedObjectCount() == 1)
                    {
                        var swTestSelComp = (Component2)_swSelMgr.GetSelectedObjectsComponent2(1);

                        Component2 swTecSelComp;
                        if (swTestSelComp != null && _swSelComp != null && _mSwAddin.GetParentLibraryComponent(swTestSelComp, out swTecSelComp))
                        {
                            ModelDoc2 spec;
                            Component2 testComp = _mSwAddin.GetMdbComponentTopLevel(swTecSelComp, out spec);
                            if (testComp != null)
                            {
                                ModelDoc2 testModel = testComp.IGetModelDoc();
                                if (testModel != null && _swSelModel == testModel)
                                {
                                    //RefreshLabel(swTestSelComp);
                                    IsNewSelectionDisabled = false;
                                    return 0;
                                }
                            }
                        }

                        #region Очистить все словари местные переменные

                        //закоменчено, т.к. этот код вызывает ошибку внутри солида и его закрытие.
                        //замена на похожие методы (ClearSelection2 и т.п.) не помогает 
                        /*/
                        if (!_isMate)
                        {                            
                            _swAsmDoc.NewSelectionNotify -= NewSelection;
                            _swModel.ClearSelection();
                            _swAsmDoc.NewSelectionNotify += NewSelection;
                        }
                        /*/
                        
                        _dictHeightPage.Clear();
                        _dictPathPict.Clear();
                        _dictionary.Clear();
                        _dictConfig.Clear();
                        _commonList.Clear();
                        _objListChanged.Clear();
                        _setNewListForComboBox = true;
                        _butPlusTxt.Clear();
                        _dimensionConfig.Clear();
                        _textBoxListForRedraw.Clear();
                        _numbAndTextBoxes.Clear();
                        _comboBoxListForRedraw.Clear();
                        _namesOfColumnNameFromDimLimits.Clear();
                        refobj = null;
                        #endregion

                        if (swTestSelComp != null)
                        {
                            if (_mSwAddin.GetParentLibraryComponent(swTestSelComp, out _swSelComp))
                            {
                                ModelDoc2 specModel;
                                _swSelComp = _mSwAddin.GetMdbComponentTopLevel(_swSelComp, out specModel);
                                _swSelModel = _swSelComp.IGetModelDoc();

                                #region ссылочный эскиз
                                //bool draft = (specModel.CustomInfo2["", "Required Draft"] == "Yes" ||
                                //    specModel.CustomInfo2[specModel.IGetActiveConfiguration().Name, "Required Draft"] == "Yes");

                                //if (draft)
                                //{
                                //    tabMain.Location = new Point(8, 70);
                                //    var dir = new DirectoryInfo(Path.GetDirectoryName(_mSwAddin.SwModel.GetPathName()));

                                //    var paths =
                                //        dir.GetFiles(
                                //            Path.GetFileNameWithoutExtension(specModel.GetPathName()) + ".SLDDRW",
                                //            SearchOption.AllDirectories);
                                //    string path=null;

                                //    if (paths.Any())
                                //        path = paths[0].FullName;
                                //    else
                                //    {
                                //        if (SwAddin.needWait)
                                //        {
                                //            path = Path.Combine(Path.GetDirectoryName(specModel.GetPathName()),
                                //                                Path.GetFileNameWithoutExtension(specModel.GetPathName()) +
                                //                                ".SLDDRW");
                                //            ThreadPool.QueueUserWorkItem(CheckDWRexistAfterCopy, path);
                                //        }
                                //    }

                                //    if (!string.IsNullOrEmpty(path))
                                //    {
                                //        linkLabel1.Name = path;
                                //        linkLabel1.Click += LinkLabel1Click;
                                //        string textInfo = specModel.CustomInfo2["", "Sketch Number"];
                                //        if (textInfo == "" || textInfo == "0" || textInfo == "Sketch Number")
                                //            linkLabel1.Text = @"ЭСКИЗ № н/о";
                                //        else
                                //            linkLabel1.Text = @"ЭСКИЗ № " + textInfo;
                                //    }
                                //    else
                                //        tabMain.Location = new Point(8, 40);
                                //}
                                //else
                                //{
                                //    tabMain.Location = new Point(8, 40);
                                //    linkLabel1.Text = "";
                                //}
                                #endregion

                                _lblPrm.Clear();
                                _btnPrm.Clear();

                                tbpParams.Controls.Clear();
                                tbpPos.Controls.Clear();

                                lblCompName.Text = _swSelComp.Name2;

                                OleDbConnection oleDb;

                                int i;
                                if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                                {
                                    using (oleDb)
                                    {
                                        #region Обработка файла *.mdb

                                        if (!tabMain.Controls.Contains(tbpParams))
                                        {
                                            tabMain.Controls.Remove(tbpPos);
                                            tabMain.Controls.Add(tbpParams);
                                            tabMain.Controls.Add(tbpPos);
                                        }

                                        if (tabMain.SelectedTab != tbpParams)
                                        {
                                            tabMain.SelectTab(tbpParams);
                                        }
                                        var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                                                                                 new object[] { null, null, null, "TABLE" });

                                        OleDbCommand cm;
                                        OleDbDataReader rd;
                                        listForDimensions = new List<DimensionConfForList>();

                                        #region Если есть objects

                                        if (oleSchem.Rows.Cast<DataRow>().Any(
                                            row => (string)row["TABLE_NAME"] == "objects"))
                                        {
                                            _chkMeasure = new CheckBox
                                            {
                                                Appearance = Appearance.Button,
                                                Location = new Point(133, 12),
                                                Name = @"chkMeasure",
                                                Size = new Size(80, 24),
                                                Text = @"Измерить",
                                                TextAlign = ContentAlignment.MiddleCenter,
                                                UseVisualStyleBackColor = true
                                            };
                                            tbpParams.Controls.Add(_chkMeasure);
                                            _chkMeasure.CheckedChanged += StartMeasure;

                                            i = 0;

                                            bool isNumber = false, isIdSlave = false, isAsmConfig = false, isFixedValues = false;

                                            #region Dimension Configuration

                                            if (
                                                oleSchem.Rows.Cast<DataRow>().Any(
                                                    row => (string)row["TABLE_NAME"] == "dimension_conf"))
                                            {
                                                cm = new OleDbCommand("SELECT * FROM dimension_conf ORDER BY id", oleDb);
                                                rd = cm.ExecuteReader();
                                                var outComps = new LinkedList<Component2>();
                                                if (_mSwAddin.GetComponents(
                                                    _swSelModel.IGetActiveConfiguration().IGetRootComponent2(),
                                                    outComps, true, false))
                                                {
                                                    _dimensionConfig = Decors.GetListComponentForDimension(_mSwAddin, rd,
                                                                                                           outComps);
                                                    _dimensionConfig.Sort((x, y) => x.Number.CompareTo(y.Number));
                                                    rd.Close();
                                                }
                                            }
                                            #endregion

                                            var thisDataSet = new DataSet();
                                            var testAdapter = new OleDbDataAdapter("SELECT * FROM objects", oleDb);
                                            testAdapter.Fill(thisDataSet, "objects");
                                            testAdapter.Dispose();
                                            bool captConfigBool = thisDataSet.Tables["objects"].Columns.Contains("captConf");
                                            foreach (var v in thisDataSet.Tables["objects"].Columns)
                                            {
                                                var vc = (DataColumn)v;
                                                if (vc.ColumnName == "number")
                                                    isNumber = true;
                                                if (vc.ColumnName == "idslave")
                                                {
                                                    if (vc.DataType.Name != "String")
                                                        MessageBox.Show(
                                                            @"Неверно указан тип данных в столбце 'ismaster'",
                                                            _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                                            MessageBoxIcon.Information);
                                                    isIdSlave = true;
                                                }
                                                if (vc.ColumnName == "mainasmconf" &&
                                                    _swSelModel.GetConfigurationCount() > 1)
                                                    isAsmConfig = true;
                                                if (vc.ColumnName == "fixedvalues")
                                                    isFixedValues = true;
                                            }
                                            thisDataSet.Clear();

                                            if (Properties.Settings.Default.CheckParamLimits &&
                                                oleSchem.Rows.Cast<DataRow>().Any(
                                                    row => (string)row["TABLE_NAME"] == "dimlimits"))
                                            {
                                                testAdapter = new OleDbDataAdapter("SELECT * FROM dimlimits", oleDb);
                                                testAdapter.Fill(thisDataSet, "dimlimits");
                                                testAdapter.Dispose();
                                                foreach (var v in thisDataSet.Tables["dimlimits"].Columns)
                                                {
                                                    var vc = (DataColumn)v;
                                                    _namesOfColumnNameFromDimLimits.Add(vc.ColumnName);
                                                }
                                                thisDataSet.Clear();
                                            }

                                            string currentConf = _swSelComp.ReferencedConfiguration;

                                            cm = isNumber
                                                     ? new OleDbCommand(
                                                           "SELECT * FROM objects WHERE number>0 ORDER BY number",
                                                           oleDb)
                                                     : new OleDbCommand("SELECT * FROM objects ORDER BY id", oleDb);
                                            rd = cm.ExecuteReader();

                                            #region Размеры

                                            #region Считывание данных из objects

                                            int k = 1;
                                            var dictWithDiscretValues = new Dictionary<string, List<int>>();

                                            while (rd.Read())
                                            {
                                                if (captConfigBool && rd["captConf"] != null && rd["captConf"].ToString() != "all" && rd["captConf"].ToString() != currentConf && !string.IsNullOrEmpty(rd["captConf"].ToString()))
                                                    continue;
                                                if (rd["caption"].ToString() == null || rd["caption"].ToString() == "" ||
                                                    rd["caption"].ToString().Trim() == "")
                                                    continue;

                                                #region Обработка поля mainasmconf

                                                if (isAsmConfig)
                                                {
                                                    var neededConf = rd["mainasmconf"].ToString();
                                                    bool isNeededConf = neededConf.Split('+').Select(v => v.Trim()).Any(
                                                        f => f == currentConf);
                                                    if (neededConf == "all")
                                                        isNeededConf = true;
                                                    if (!isNeededConf)
                                                        continue;
                                                }

                                                #endregion

                                                string strObjName = rd["name"].ToString();

                                                double strObjVal;
                                                if (_swSelModel.GetPathName().Contains("_SWLIB_BACKUP"))
                                                {
                                                    string pn = Path.GetFileNameWithoutExtension(_swSelModel.GetPathName());
                                                    string last3 = pn.Substring(pn.Length - 4, 4);
                                                    string[] arr = strObjName.Split('@');
                                                    if (arr.Length != 3)
                                                        throw new Exception("что-то не так");
                                                    arr[2] = Path.GetFileNameWithoutExtension(arr[2]) + last3 + Path.GetExtension(arr[2]);
                                                    strObjName = string.Format("{0}@{1}@{2}", arr[0], arr[1], arr[2]);
                                                }
                                                if (_mSwAddin.GetObjectValue(_swSelModel, strObjName, (int)rd["type"],
                                                                             out strObjVal))
                                                {
                                                    int val = GetCorrectIntValue(strObjVal);

                                                    int number = isNumber ? (int)rd["number"] : (int)rd["id"];
                                                    while (number != k && _dimensionConfig.Count != 0)
                                                    {
                                                        listForDimensions.AddRange(
                                                            from dimensionConfiguration in _dimensionConfig
                                                            where dimensionConfiguration.Number == k
                                                            select
                                                                new DimensionConfForList(dimensionConfiguration.Number,
                                                                                         dimensionConfiguration.Caption,
                                                                                         "", -1,
                                                                                         GetListIntFromString(
                                                                                             dimensionConfiguration.
                                                                                                 IdSlave),
                                                                                         dimensionConfiguration.
                                                                                             Component,
                                                                                         false,
                                                                                         dimensionConfiguration.Id));
                                                        //Logging.Log.Instance.Debug("listForDimensions1:" + listForDimensions.Count.ToString());
                                                        k++;
                                                    }

                                                    var listId = new List<int>();
                                                    try
                                                    {
                                                        if (isIdSlave)
                                                            listId = GetListIntFromString((string)rd["idslave"]);
                                                    }
                                                    catch
                                                    {
                                                        listId = null;
                                                    }
                                                    string labelName;
                                                    try
                                                    {
                                                        labelName = rd["caption"].ToString();
                                                    }
                                                    catch
                                                    {
                                                        string[] strObjNameParts = strObjName.Split('@');
                                                        labelName = strObjNameParts[0];
                                                    }

                                                    if (isFixedValues)
                                                    {
                                                        try
                                                        {
                                                            var arr = (string)rd["fixedvalues"];
                                                            string[] arrs = arr.Split(',');
                                                            var list = arrs.Select(s => Convert.ToInt32(s)).ToList();
                                                            if (!dictWithDiscretValues.ContainsKey(strObjName))
                                                                dictWithDiscretValues.Add(strObjName, list);
                                                        }
                                                        catch { }
                                                    }

                                                    listForDimensions.Add(new DimensionConfForList(number, labelName,
                                                                                                   strObjName, val,
                                                                                                   listId,
                                                                                                   null,
                                                                                                   (bool)rd["ismaster"],
                                                                                                   (int)rd["id"]));
                                                    //Logging.Log.Instance.Debug("listForDimensions2:" + listForDimensions.Count.ToString());
                                                    k++;
                                                }
                                            }
                                            rd.Close();

                                            #endregion

                                            listForDimensions.AddRange(
                                                _dimensionConfig.Where(x => x.Number >= k).Select(
                                                    b =>
                                                    new DimensionConfForList(b.Number, b.Caption, "", -1,
                                                                             GetListIntFromString(b.IdSlave),
                                                                             b.Component,
                                                                             false,
                                                                             b.Id)));
                                            listForDimensions.Sort((x, y) => x.Number.CompareTo(y.Number));
                                            //Logging.Log.Instance.Debug("listForDimensions3:" + listForDimensions.Count.ToString());
                                            foreach (var dimensionConfForList in listForDimensions)
                                            {
                                                var lblNew = new Label { Size = new Size(125, 24) };
                                                lblNew.Location = new Point(0, 48 + i * (lblNew.Size.Height + 6));
                                                lblNew.Name = "lblPrm" + i;
                                                lblNew.TextAlign = ContentAlignment.MiddleRight;
                                                lblNew.Text = dimensionConfForList.LabelName;
                                                lblNew.Tag = dimensionConfForList.StrObjName;
                                                tbpParams.Controls.Add(lblNew);
                                                _lblPrm.AddLast(lblNew);

                                                downPos = lblNew.Location.Y + lblNew.Size.Height;
                                                if (dimensionConfForList.StrObjName != "")
                                                {
                                                    #region TextBox с размерами

                                                    if (dictWithDiscretValues.ContainsKey(dimensionConfForList.StrObjName))
                                                    {
                                                        #region Если дискретные значения
                                                        var comboBoxWithDiscretValues = new ComboBox
                                                        {
                                                            Size = new Size(53, 24),
                                                            Location =
                                                                new Point(
                                                                lblNew.Location.X +
                                                                lblNew.Size.Width + 4,
                                                                lblNew.Location.Y),
                                                            Tag = dimensionConfForList.StrObjName,
                                                            TabIndex = i
                                                        };

                                                        int val = dimensionConfForList.ObjVal;

                                                        foreach (int vals in dictWithDiscretValues[dimensionConfForList.StrObjName])
                                                        {
                                                            int ordNumb = comboBoxWithDiscretValues.Items.Add(vals);
                                                            if (vals == val)
                                                                comboBoxWithDiscretValues.SelectedIndex = ordNumb;
                                                        }

                                                        tbpParams.Controls.Add(comboBoxWithDiscretValues);

                                                        _comboBoxListForRedraw.Add(comboBoxWithDiscretValues,
                                                                                   dimensionConfForList.IdSlave);
                                                        comboBoxWithDiscretValues.SelectedIndexChanged +=
                                                        ComboBoxWithDiscretValuesSelectedIndexChanged;
                                                        #endregion
                                                    }
                                                    else
                                                    {
                                                        #region Если обычные значения
                                                        int val = GetCorrectIntValue(dimensionConfForList.ObjVal);

                                                        var txtNew = new TextBox
                                                        {
                                                            Size = new Size(35, 24),
                                                            Location =
                                                                new Point(
                                                                lblNew.Location.X + lblNew.Size.Width +
                                                                4,
                                                                lblNew.Location.Y),
                                                            Name =
                                                                lblNew.Text + "@" +
                                                                dimensionConfForList.ObjVal,
                                                            Text = val.ToString(),
                                                            TextAlign = HorizontalAlignment.Right,
                                                            Tag = dimensionConfForList.StrObjName,
                                                            TabIndex = i
                                                        };

                                                        _numbAndTextBoxes.Add(dimensionConfForList.Id, txtNew);
                                                        if (!dimensionConfForList.IsGrey)
                                                            txtNew.ReadOnly = true;
                                                        else
                                                        {
                                                            _textBoxListForRedraw.Add(txtNew,
                                                                                      dimensionConfForList.IdSlave);
                                                            txtNew.KeyDown += TxtPrmKeyDown;
                                                            txtNew.TextChanged += TxtNewTextChanged;

                                                            var btnNew = new Button
                                                            {
                                                                ImageKey = @"Units1.ico",
                                                                ImageList = imageList1,
                                                                Size = new Size(24, 24),
                                                                Location =
                                                                    new Point(
                                                                    txtNew.Location.X +
                                                                    txtNew.Size.Width +
                                                                    4,
                                                                    lblNew.Location.Y),
                                                                Name =
                                                                    dimensionConfForList.ObjVal.
                                                                    ToString(),
                                                                Text = "",
                                                                Tag = txtNew.Name,
                                                                Enabled = false
                                                            };

                                                            if (!_butPlusTxt.ContainsKey(btnNew))
                                                                _butPlusTxt.Add(btnNew, txtNew);

                                                            tbpParams.Controls.Add(btnNew);
                                                            _btnPrm.AddLast(btnNew);

                                                            btnNew.Click += MeasureLength;

                                                            #region если есть таблица ref_objects  и в ней есть хоть одна строка с соотв-щем id
                                                            if (refobj == null)
                                                            {
                                                                refobj = new List<ref_object>();
                                                                if (oleSchem.Rows.Cast<DataRow>().Any(row => (string)row["TABLE_NAME"] == "ref_objects_axe"))
                                                                {

                                                                    cm = new OleDbCommand("SELECT * FROM ref_objects  LEFT JOIN ref_objects_axe  ON ref_objects_axe.id=ref_objects.objectsId", oleDb);
                                                                    rd = cm.ExecuteReader();

                                                                    while (rd.Read())
                                                                    {
                                                                        refobj.Add(new ref_object((string)rd["componentName"], (int)rd["objectsId"], (string)rd["axe"], (float)rd["correctionLeft_Up"], (float)rd["correctionRight_Down"]));
                                                                    }
                                                                    rd.Close();
                                                                }
                                                            }
                                                            ref_object[] currentrefs = refobj.Where(d => d.ObjectId == dimensionConfForList.Id).ToArray();
                                                            if (currentrefs.Length > 0)
                                                            {
                                                                //добавить кнопку
                                                                //выбрать из refobj только dimensionConfigForList.Id
                                                                var btnRef = new Button
                                                                {
                                                                    ImageKey = @"expand.gif",
                                                                    ImageList = imageList1,
                                                                    Size = new Size(24, 24),
                                                                    Location =
                                                                        new Point(
                                                                        txtNew.Location.X +
                                                                        txtNew.Size.Width + btnNew.Size.Width +
                                                                        8,
                                                                        lblNew.Location.Y),
                                                                    Name =
                                                                        dimensionConfForList.ObjVal.
                                                                        ToString(),
                                                                    Text = "",
                                                                    Tag = currentrefs,
                                                                    Enabled = true
                                                                };

                                                                tbpParams.Controls.Add(btnRef);
                                                                //_btnPrm.AddLast(btnNew);

                                                                btnRef.Click += ExpandBtn;
                                                                if (!_butPlusTxt.ContainsKey(btnRef))
                                                                    _butPlusTxt.Add(btnRef, txtNew);
                                                            }
                                                            #endregion
                                                        }
                                                        tbpParams.Controls.Add(txtNew);
                                                        _commonList.Add(txtNew);

                                                        #endregion
                                                    }

                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region ComboBox с конфигурациями

                                                    var cmp = dimensionConfForList.Component;
                                                    var comboBoxConfForDimTab = new ComboBox
                                                    {
                                                        Size = new Size(56, 24),
                                                        Location =
                                                            new Point(
                                                            lblNew.Location.X +
                                                            lblNew.Size.Width + 4,
                                                            lblNew.Location.Y),
                                                        Tag = cmp,
                                                        TabIndex = i
                                                    };

                                                    var confNames =
                                                        (string[])cmp.IGetModelDoc().GetConfigurationNames();
                                                    foreach (var confName in confNames)
                                                    {
                                                        int lonhName = confName.Length * 6;
                                                        if (comboBoxConfForDimTab.DropDownWidth < lonhName)
                                                            comboBoxConfForDimTab.DropDownWidth = lonhName;
                                                        comboBoxConfForDimTab.Items.Add(confName);
                                                    }
                                                    comboBoxConfForDimTab.SelectedItem = cmp.ReferencedConfiguration;
                                                    tbpParams.Controls.Add(comboBoxConfForDimTab);

                                                    _comboBoxListForRedraw.Add(comboBoxConfForDimTab,
                                                                               dimensionConfForList.IdSlave);

                                                    comboBoxConfForDimTab.SelectedIndexChanged +=
                                                        ComboBoxConfForDimTabSelectedIndexChanged;

                                                    #endregion
                                                }
                                                i++;
                                            }
                                            #region Добавить выбор конфигурации
                                            /*
                                            try
                                            {
                                                if (_swSelModel.GetConfigurationCount() > 1)
                                                {
                                                    var lblNew = new Label { Size = new Size(125, 24) };
                                                    lblNew.Location = new Point(0, 48 + i * (lblNew.Size.Height + 6));
                                                    lblNew.Name = "lblPrm" + i;
                                                    lblNew.TextAlign = ContentAlignment.MiddleRight;
                                                    lblNew.Text = "Конфигурации:";
                                                    tbpParams.Controls.Add(lblNew);
                                                    //lblNew.Tag = dimensionConfForList.StrObjName;
                                                    downPos = lblNew.Location.Y + lblNew.Size.Height;
                                                    string[] configurations = _swSelModel.GetConfigurationNames();
                                                    var comboBoxConfig = new ComboBox
                                                    {
                                                        Size = new Size(56, 24),
                                                        Location = new Point(lblNew.Location.X + lblNew.Size.Width + 4, lblNew.Location.Y),
                                                    };
                                                    string activeConf = _swSelComp.ReferencedConfiguration;//_swSelModel.IGetActiveConfiguration().Name;
                                                    foreach (var configuration in configurations)
                                                    {
                                                        int currentIndex = comboBoxConfig.Items.Add(configuration);
                                                        if (configuration == activeConf)
                                                            comboBoxConfig.SelectedIndex = currentIndex;
                                                    }
                                                    Size size = TextRenderer.MeasureText(activeConf, comboBoxConfig.Font);
                                                    int lonhName = size.Width; //nameOfConfiguration.Length * 5;
                                                    if (comboBoxConfig.DropDownWidth < lonhName)
                                                        comboBoxConfig.DropDownWidth = lonhName;
                                                    tbpParams.Controls.Add(comboBoxConfig);
                                                    comboBoxConfig.SelectedIndexChanged += ConfigurationChanged;
                                                }
                                            }
                                            catch (Exception e)
                                            { }
                                            */
                                            #endregion
                                            _dictHeightPage.Add(tbpParams, downPos);
                                            #endregion
                                        }

                                        #endregion
                                        #region Если есть comments
                                        if (oleSchem.Rows.Cast<DataRow>().Any(
                                           row => (string)row["TABLE_NAME"] == "comments"))
                                        {
                                            cm = new OleDbCommand("SELECT * FROM comments ORDER BY id", oleDb);
                                            rd = cm.ExecuteReader();
                                            if (rd.Read())
                                            {
                                                try
                                                {
                                                    CommentLayout.Visible = true;
                                                    WarningPB.Visible = (bool)rd["showSimbol"];
                                                    commentsTb.Text = (string)rd["comment"];

                                                    Size size = TextRenderer.MeasureText(commentsTb.Text, commentsTb.Font);

                                                    commentsTb.Height = (size.Width / (commentsTb.Width - 10)) * 30;


                                                }
                                                catch
                                                {
                                                    CommentLayout.Visible = false;
                                                }
                                            }
                                            rd.Close();


                                        }
                                        else
                                        {
                                            CommentLayout.Visible = false;
                                        }
                                        #endregion
                                        #region Decors

                                        if (!ReloadDecorTab(oleDb, oleSchem))
                                            return 1;
                                        #endregion

                                        oleDb.Close();

                                        #endregion
                                    }
                                }
                                else
                                    if (_tabDec != null)
                                        tabMain.Controls.Remove(_tabDec);

                                var swFeat = (Feature)_swSelModel.FirstFeature();
                                i = 0;

                                #region Position
                                var mates = _swSelComp.GetMates();
                                Dictionary<string, Mate2> existingMates = new Dictionary<string, Mate2>();
                                if (mates != null)
                                {
                                    foreach (var mate in mates)
                                    {
                                        if (mate is Mate2)
                                        {
                                            var spec = (Mate2)mate;
                                            int mec = spec.GetMateEntityCount();
                                            if (mec > 1)
                                            {
                                                for (int ik = 0; ik < mec; ik++)
                                                {
                                                    MateEntity2 me = spec.MateEntity(ik);
                                                    if (me.ReferenceComponent.Name.Contains(_swSelComp.Name))
                                                    {
                                                        Entity tt = me.Reference;
                                                        var tt2 = tt as Feature;
                                                        
                                                        if (tt is RefPlane && tt2 != null)
                                                        {

                                                            if (!existingMates.ContainsKey(tt2.Name))
                                                                existingMates.Add(tt2.Name, spec);
                                                        }

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                int downPosPosition = 0;
                                while (swFeat != null)
                                {
                                    if (swFeat.GetTypeName2() == "RefPlane")
                                    {
                                        string strPlaneName = swFeat.Name;

                                        if (strPlaneName.Length > 1)
                                        {
                                            if (strPlaneName.Substring(0, 1) == "#")
                                            {
                                                var btnNew = new Button { Size = new Size(120, 20) };
                                                btnNew.Location = new Point(62, 24 + i * (btnNew.Size.Height + 6));
                                                btnNew.Name = "btnPos" + i;
                                                btnNew.Text = strPlaneName.Substring(5);
                                                btnNew.Tag = strPlaneName;
                                                tbpPos.Controls.Add(btnNew);

                                                btnNew.Click += AddMate;

                                                downPosPosition = btnNew.Location.Y + btnNew.Size.Height;
                                                if (existingMates.ContainsKey(strPlaneName))
                                                {
                                                    //добавить кнопку для отвязки
                                                    var btnDeattach = new Button { Size = new Size(20, 20) };
                                                    btnDeattach.Location = new Point(62 + btnNew.Size.Width + 10, 24 + i * (btnNew.Size.Height + 6));
                                                    btnDeattach.Name = strPlaneName;
                                                    btnDeattach.Text = "X";
                                                    btnDeattach.Tag = existingMates[strPlaneName];
                                                    tbpPos.Controls.Add(btnDeattach);
                                                    btnDeattach.Click += DeleteMate;
                                                }
                                                i++;
                                            }
                                        }
                                    }
                                    swFeat = (Feature)swFeat.GetNextFeature();
                                }
                                _dictHeightPage.Add(tbpPos, downPosPosition);

                                #endregion

                                if (_lblPrm.Count == 0)
                                {
                                    tabMain.SelectTab(tbpPos);
                                    downPos = downPosPosition;
                                    SetSizeForTab(downPos);
                                    if (tabMain.Controls.Contains(tbpParams))
                                    {
                                        tabMain.Controls.Remove(tbpParams);
                                        //Logging.Log.Instance.Fatal("Вкладка на РПД не показана.Смотреть listForDimensions");
                                    }
                                }
                                if (rbMode1 != null && rbMode2 != null)
                                {
                                    if (controlsToHideWhenChangeMode == null || controlsToHideWhenChangeMode.Count == 0)
                                        pnlMode.Visible = false;
                                    if (Properties.Settings.Default.DefaultRPDView)
                                    {
                                        rbMode1.CheckedChanged += new EventHandler(ModeCheckedChanged);
                                        rbMode1.Checked = true;
                                    }
                                    else
                                    {
                                        rbMode2.Checked = true;
                                        rbMode1.CheckedChanged += new EventHandler(ModeCheckedChanged);
                                    }
                                }
                                SetSizeForTab(_dictHeightPage[tabMain.SelectedTab]);
                            }
                        }
                        ReloadAllSetParameters(swTestSelComp);
                    }
                    IsNewSelectionDisabled = false;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return 0;
        }

        private void ReloadAllSetParameters(Component2 swTestSelComp)
        {
            #region Очистить все словари местные переменные
            int downPos = 0;

            //закоменчено, т.к. этот код вызывает ошибку внутри солида и его закрытие.
            //замена на похожие методы (ClearSelection2 и т.п.) не помогает 
            /*/
            if (!_isMate)
            {                            
                _swAsmDoc.NewSelectionNotify -= NewSelection;
                _swModel.ClearSelection();
                _swAsmDoc.NewSelectionNotify += NewSelection;
            }
            /*/

            _dictHeightPage.Clear();
            _dictPathPict.Clear();
            _dictionary.Clear();
            _dictConfig.Clear();
            _commonList.Clear();
            _objListChanged.Clear();
            _setNewListForComboBox = true;
            _butPlusTxt.Clear();
            _dimensionConfig.Clear();
            _textBoxListForRedraw.Clear();
            _numbAndTextBoxes.Clear();
            _comboBoxListForRedraw.Clear();
            _namesOfColumnNameFromDimLimits.Clear();
            refobj = null;
            #endregion

            if (swTestSelComp != null)
            {
                if (_mSwAddin.GetParentLibraryComponent(swTestSelComp, out _swSelComp))
                {
                    ModelDoc2 specModel;
                    _swSelComp = _mSwAddin.GetMdbComponentTopLevel(_swSelComp, out specModel);
                    _swSelModel = _swSelComp.IGetModelDoc();

                    #region ссылочный эскиз
                    //bool draft = (specModel.CustomInfo2["", "Required Draft"] == "Yes" ||
                    //    specModel.CustomInfo2[specModel.IGetActiveConfiguration().Name, "Required Draft"] == "Yes");

                    //if (draft)
                    //{
                    //    tabMain.Location = new Point(8, 70);
                    //    var dir = new DirectoryInfo(Path.GetDirectoryName(_mSwAddin.SwModel.GetPathName()));

                    //    var paths =
                    //        dir.GetFiles(
                    //            Path.GetFileNameWithoutExtension(specModel.GetPathName()) + ".SLDDRW",
                    //            SearchOption.AllDirectories);
                    //    string path=null;

                    //    if (paths.Any())
                    //        path = paths[0].FullName;
                    //    else
                    //    {
                    //        if (SwAddin.needWait)
                    //        {
                    //            path = Path.Combine(Path.GetDirectoryName(specModel.GetPathName()),
                    //                                Path.GetFileNameWithoutExtension(specModel.GetPathName()) +
                    //                                ".SLDDRW");
                    //            ThreadPool.QueueUserWorkItem(CheckDWRexistAfterCopy, path);
                    //        }
                    //    }

                    //    if (!string.IsNullOrEmpty(path))
                    //    {
                    //        linkLabel1.Name = path;
                    //        linkLabel1.Click += LinkLabel1Click;
                    //        string textInfo = specModel.CustomInfo2["", "Sketch Number"];
                    //        if (textInfo == "" || textInfo == "0" || textInfo == "Sketch Number")
                    //            linkLabel1.Text = @"ЭСКИЗ № н/о";
                    //        else
                    //            linkLabel1.Text = @"ЭСКИЗ № " + textInfo;
                    //    }
                    //    else
                    //        tabMain.Location = new Point(8, 40);
                    //}
                    //else
                    //{
                    //    tabMain.Location = new Point(8, 40);
                    //    linkLabel1.Text = "";
                    //}
                    #endregion

                    _lblPrm.Clear();
                    _btnPrm.Clear();

                    tbpParams.Controls.Clear();
                    tbpPos.Controls.Clear();

                    lblCompName.Text = _swSelComp.Name2;

                    OleDbConnection oleDb;

                    int i;
                    if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                    {
                        using (oleDb)
                        {
                            #region Обработка файла *.mdb

                            if (!tabMain.Controls.Contains(tbpParams))
                            {
                                tabMain.Controls.Remove(tbpPos);
                                tabMain.Controls.Add(tbpParams);
                                tabMain.Controls.Add(tbpPos);
                            }
                            if (Properties.Settings.Default.KitchenModeOn)
                            {
                                tabMain.SelectTab(tbpPos);
                            }
                            else
                            {
                                if (tabMain.SelectedTab != tbpParams)
                                {
                                    tabMain.SelectTab(tbpParams);
                                }
                            }
                            var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                                                                     new object[] { null, null, null, "TABLE" });

                            OleDbCommand cm;
                            OleDbDataReader rd;
                            listForDimensions = new List<DimensionConfForList>();

                            #region Если есть objects

                            if (oleSchem.Rows.Cast<DataRow>().Any(
                                row => (string)row["TABLE_NAME"] == "objects"))
                            {
                                _chkMeasure = new CheckBox
                                {
                                    Appearance = Appearance.Button,
                                    Location = new Point(133, 12),
                                    Name = @"chkMeasure",
                                    Size = new Size(80, 24),
                                    Text = @"Измерить",
                                    TextAlign = ContentAlignment.MiddleCenter,
                                    UseVisualStyleBackColor = true
                                };
                                tbpParams.Controls.Add(_chkMeasure);
                                _chkMeasure.CheckedChanged += StartMeasure;

                                i = 0;

                                bool isNumber = false, isIdSlave = false, isAsmConfig = false, isFixedValues = false;

                                #region Dimension Configuration

                                if (
                                    oleSchem.Rows.Cast<DataRow>().Any(
                                        row => (string)row["TABLE_NAME"] == "dimension_conf"))
                                {
                                    cm = new OleDbCommand("SELECT * FROM dimension_conf ORDER BY id", oleDb);
                                    rd = cm.ExecuteReader();
                                    var outComps = new LinkedList<Component2>();
                                    if (_mSwAddin.GetComponents(
                                        _swSelModel.IGetActiveConfiguration().IGetRootComponent2(),
                                        outComps, true, false))
                                    {
                                        _dimensionConfig = Decors.GetListComponentForDimension(_mSwAddin, rd,
                                                                                               outComps);
                                        _dimensionConfig.Sort((x, y) => x.Number.CompareTo(y.Number));
                                        rd.Close();
                                    }
                                }
                                #endregion

                                var thisDataSet = new DataSet();
                                var testAdapter = new OleDbDataAdapter("SELECT * FROM objects", oleDb);
                                testAdapter.Fill(thisDataSet, "objects");
                                testAdapter.Dispose();
                                bool captConfigBool = thisDataSet.Tables["objects"].Columns.Contains("captConf");
                                foreach (var v in thisDataSet.Tables["objects"].Columns)
                                {
                                    var vc = (DataColumn)v;
                                    if (vc.ColumnName == "number")
                                        isNumber = true;
                                    if (vc.ColumnName == "idslave")
                                    {
                                        if (vc.DataType.Name != "String")
                                            MessageBox.Show(
                                                @"Неверно указан тип данных в столбце 'ismaster'",
                                                _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                                MessageBoxIcon.Information);
                                        isIdSlave = true;
                                    }
                                    if (vc.ColumnName == "mainasmconf" &&
                                        _swSelModel.GetConfigurationCount() > 1)
                                        isAsmConfig = true;
                                    if (vc.ColumnName == "fixedvalues")
                                        isFixedValues = true;
                                }
                                thisDataSet.Clear();

                                if (Properties.Settings.Default.CheckParamLimits &&
                                    oleSchem.Rows.Cast<DataRow>().Any(
                                        row => (string)row["TABLE_NAME"] == "dimlimits"))
                                {
                                    testAdapter = new OleDbDataAdapter("SELECT * FROM dimlimits", oleDb);
                                    testAdapter.Fill(thisDataSet, "dimlimits");
                                    testAdapter.Dispose();
                                    foreach (var v in thisDataSet.Tables["dimlimits"].Columns)
                                    {
                                        var vc = (DataColumn)v;
                                        _namesOfColumnNameFromDimLimits.Add(vc.ColumnName);
                                    }
                                    thisDataSet.Clear();
                                }

                                string currentConf = _swSelComp.ReferencedConfiguration;

                                cm = isNumber
                                         ? new OleDbCommand(
                                               "SELECT * FROM objects WHERE number>0 ORDER BY number",
                                               oleDb)
                                         : new OleDbCommand("SELECT * FROM objects ORDER BY id", oleDb);

                                rd = cm.ExecuteReader();

                                #region Размеры

                                #region Считывание данных из objects

                                int k = 1;
                                var dictWithDiscretValues = new Dictionary<string, List<int>>();

                                while (rd.Read())
                                {
                                    if (captConfigBool && rd["captConf"] != null && rd["captConf"].ToString() != "all" && rd["captConf"].ToString() != currentConf && !string.IsNullOrEmpty(rd["captConf"].ToString()))
                                        continue;
                                    if (rd["caption"].ToString() == null || rd["caption"].ToString() == "" ||
                                        rd["caption"].ToString().Trim() == "")
                                        continue;

                                    #region Обработка поля mainasmconf

                                    if (isAsmConfig)
                                    {
                                        var neededConf = rd["mainasmconf"].ToString();
                                        bool isNeededConf = neededConf.Split('+').Select(v => v.Trim()).Any(
                                            f => f == currentConf);
                                        if (neededConf == "all")
                                            isNeededConf = true;
                                        if (!isNeededConf)
                                            continue;
                                    }

                                    #endregion

                                    string strObjName = rd["name"].ToString();

                                    double strObjVal;
                                    if (_swSelModel.GetPathName().Contains("_SWLIB_BACKUP"))
                                    {
                                        string pn = Path.GetFileNameWithoutExtension(_swSelModel.GetPathName());
                                        string last3 = pn.Substring(pn.Length - 4, 4);
                                        string[] arr = strObjName.Split('@');
                                        if (arr.Length != 3)
                                            throw new Exception("что-то не так");
                                        arr[2] = Path.GetFileNameWithoutExtension(arr[2]) + last3 + Path.GetExtension(arr[2]);
                                        strObjName = string.Format("{0}@{1}@{2}", arr[0], arr[1], arr[2]);
                                    }
                                    if (_mSwAddin.GetObjectValue(_swSelModel, strObjName, (int)rd["type"],
                                                                 out strObjVal))
                                    {
                                        int val = GetCorrectIntValue(strObjVal);

                                        int number = isNumber ? (int)rd["number"] : (int)rd["id"];
                                        while (number != k && _dimensionConfig.Count != 0)
                                        {
                                            listForDimensions.AddRange(
                                                from dimensionConfiguration in _dimensionConfig
                                                where dimensionConfiguration.Number == k
                                                select
                                                    new DimensionConfForList(dimensionConfiguration.Number,
                                                                             dimensionConfiguration.Caption,
                                                                             "", -1,
                                                                             GetListIntFromString(
                                                                                 dimensionConfiguration.
                                                                                     IdSlave),
                                                                             dimensionConfiguration.
                                                                                 Component,
                                                                             false,
                                                                             dimensionConfiguration.Id));
                                            //Logging.Log.Instance.Debug("listForDimensions1:" + listForDimensions.Count.ToString());
                                            k++;
                                        }

                                        var listId = new List<int>();
                                        try
                                        {
                                            if (isIdSlave)
                                                listId = GetListIntFromString((string)rd["idslave"]);
                                        }
                                        catch
                                        {
                                            listId = null;
                                        }
                                        string labelName;
                                        try
                                        {
                                            labelName = rd["caption"].ToString();
                                        }
                                        catch
                                        {
                                            string[] strObjNameParts = strObjName.Split('@');
                                            labelName = strObjNameParts[0];
                                        }

                                        if (isFixedValues)
                                        {
                                            try
                                            {
                                                var arr = (string)rd["fixedvalues"];
                                                string[] arrs = arr.Split(',');
                                                var list = arrs.Select(s => Convert.ToInt32(s)).ToList();
                                                if (!dictWithDiscretValues.ContainsKey(strObjName))
                                                    dictWithDiscretValues.Add(strObjName, list);
                                            }
                                            catch { }
                                        }

                                        listForDimensions.Add(new DimensionConfForList(number, labelName,
                                                                                       strObjName, val,
                                                                                       listId,
                                                                                       null,
                                                                                       (bool)rd["ismaster"],
                                                                                       (int)rd["id"]));
                                        //Logging.Log.Instance.Debug("listForDimensions2:" + listForDimensions.Count.ToString());
                                        k++;
                                    }
                                }
                                rd.Close();

                                #endregion

                                listForDimensions.AddRange(
                                    _dimensionConfig.Where(x => x.Number >= k).Select(
                                        b =>
                                        new DimensionConfForList(b.Number, b.Caption, "", -1,
                                                                 GetListIntFromString(b.IdSlave),
                                                                 b.Component,
                                                                 false,
                                                                 b.Id)));
                                listForDimensions.Sort((x, y) => x.Number.CompareTo(y.Number));
                                //Logging.Log.Instance.Debug("listForDimensions3:" + listForDimensions.Count.ToString());
                                foreach (var dimensionConfForList in listForDimensions)
                                {
                                    var lblNew = new Label { Size = new Size(125, 24) };
                                    lblNew.Location = new Point(0, 48 + i * (lblNew.Size.Height + 6));
                                    lblNew.Name = "lblPrm" + i;
                                    lblNew.TextAlign = ContentAlignment.MiddleRight;
                                    lblNew.Text = dimensionConfForList.LabelName;
                                    lblNew.Tag = dimensionConfForList.StrObjName;
                                    tbpParams.Controls.Add(lblNew);
                                    _lblPrm.AddLast(lblNew);

                                    downPos = lblNew.Location.Y + lblNew.Size.Height;
                                    if (dimensionConfForList.StrObjName != "")
                                    {
                                        #region TextBox с размерами

                                        if (dictWithDiscretValues.ContainsKey(dimensionConfForList.StrObjName))
                                        {
                                            #region Если дискретные значения
                                            var comboBoxWithDiscretValues = new ComboBox
                                            {
                                                Size = new Size(53, 24),
                                                Location =
                                                    new Point(
                                                    lblNew.Location.X +
                                                    lblNew.Size.Width + 4,
                                                    lblNew.Location.Y),
                                                Tag = dimensionConfForList.StrObjName,
                                                TabIndex = i
                                            };

                                            int val = dimensionConfForList.ObjVal;

                                            foreach (int vals in dictWithDiscretValues[dimensionConfForList.StrObjName])
                                            {
                                                int ordNumb = comboBoxWithDiscretValues.Items.Add(vals);
                                                if (vals == val)
                                                    comboBoxWithDiscretValues.SelectedIndex = ordNumb;
                                            }

                                            tbpParams.Controls.Add(comboBoxWithDiscretValues);

                                            _comboBoxListForRedraw.Add(comboBoxWithDiscretValues,
                                                                       dimensionConfForList.IdSlave);
                                            comboBoxWithDiscretValues.SelectedIndexChanged +=
                                            ComboBoxWithDiscretValuesSelectedIndexChanged;
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Если обычные значения
                                            int val = GetCorrectIntValue(dimensionConfForList.ObjVal);

                                            var txtNew = new TextBox
                                            {
                                                Size = new Size(35, 24),
                                                Location =
                                                    new Point(
                                                    lblNew.Location.X + lblNew.Size.Width +
                                                    4,
                                                    lblNew.Location.Y),
                                                Name =
                                                    lblNew.Text + "@" +
                                                    dimensionConfForList.ObjVal,
                                                Text = val.ToString(),
                                                TextAlign = HorizontalAlignment.Right,
                                                Tag = dimensionConfForList.StrObjName,
                                                TabIndex = i
                                            };

                                            _numbAndTextBoxes.Add(dimensionConfForList.Id, txtNew);
                                            if (!dimensionConfForList.IsGrey)
                                                txtNew.ReadOnly = true;
                                            else
                                            {
                                                _textBoxListForRedraw.Add(txtNew,
                                                                          dimensionConfForList.IdSlave);
                                                txtNew.KeyDown += TxtPrmKeyDown;
                                                txtNew.TextChanged += TxtNewTextChanged;

                                                var btnNew = new Button
                                                {
                                                    ImageKey = @"Units1.ico",
                                                    ImageList = imageList1,
                                                    Size = new Size(24, 24),
                                                    Location =
                                                        new Point(
                                                        txtNew.Location.X +
                                                        txtNew.Size.Width +
                                                        4,
                                                        lblNew.Location.Y),
                                                    Name =
                                                        dimensionConfForList.ObjVal.
                                                        ToString(),
                                                    Text = "",
                                                    Tag = txtNew.Name,
                                                    Enabled = false
                                                };

                                                if (!_butPlusTxt.ContainsKey(btnNew))
                                                    _butPlusTxt.Add(btnNew, txtNew);

                                                tbpParams.Controls.Add(btnNew);
                                                _btnPrm.AddLast(btnNew);

                                                btnNew.Click += MeasureLength;

                                                #region если есть таблица ref_objects  и в ней есть хоть одна строка с соотв-щем id
                                                if (refobj == null)
                                                {
                                                    refobj = new List<ref_object>();
                                                    if (oleSchem.Rows.Cast<DataRow>().Any(row => (string)row["TABLE_NAME"] == "ref_objects_axe"))
                                                    {

                                                        cm = new OleDbCommand("SELECT * FROM ref_objects  LEFT JOIN ref_objects_axe  ON ref_objects_axe.id=ref_objects.objectsId", oleDb);
                                                        rd = cm.ExecuteReader();

                                                        while (rd.Read())
                                                        {
                                                            refobj.Add(new ref_object((string)rd["componentName"], (int)rd["objectsId"], (string)rd["axe"], (float)rd["correctionLeft_Up"], (float)rd["correctionRight_Down"]));
                                                        }
                                                        rd.Close();
                                                    }
                                                }
                                                ref_object[] currentrefs = refobj.Where(d => d.ObjectId == dimensionConfForList.Id).ToArray();
                                                if (currentrefs.Length > 0)
                                                {
                                                    //добавить кнопку
                                                    //выбрать из refobj только dimensionConfigForList.Id
                                                    var btnRef = new Button
                                                    {
                                                        ImageKey = @"expand.gif",
                                                        ImageList = imageList1,
                                                        Size = new Size(24, 24),
                                                        Location =
                                                            new Point(
                                                            txtNew.Location.X +
                                                            txtNew.Size.Width + btnNew.Size.Width +
                                                            8,
                                                            lblNew.Location.Y),
                                                        Name =
                                                            dimensionConfForList.ObjVal.
                                                            ToString(),
                                                        Text = "",
                                                        Tag = currentrefs,
                                                        Enabled = true
                                                    };

                                                    tbpParams.Controls.Add(btnRef);
                                                    //_btnPrm.AddLast(btnNew);

                                                    btnRef.Click += ExpandBtn;
                                                    if (!_butPlusTxt.ContainsKey(btnRef))
                                                        _butPlusTxt.Add(btnRef, txtNew);
                                                }
                                                #endregion
                                            }
                                            tbpParams.Controls.Add(txtNew);
                                            _commonList.Add(txtNew);

                                            #endregion
                                        }

                                        #endregion
                                    }
                                    else
                                    {
                                        #region ComboBox с конфигурациями

                                        var cmp = dimensionConfForList.Component;
                                        var comboBoxConfForDimTab = new ComboBox
                                        {
                                            Size = new Size(56, 24),
                                            Location =
                                                new Point(
                                                lblNew.Location.X +
                                                lblNew.Size.Width + 4,
                                                lblNew.Location.Y),
                                            Tag = cmp,
                                            TabIndex = i
                                        };

                                        var confNames =
                                            (string[])cmp.IGetModelDoc().GetConfigurationNames();
                                        foreach (var confName in confNames)
                                        {
                                            int lonhName = confName.Length * 6;
                                            if (comboBoxConfForDimTab.DropDownWidth < lonhName)
                                                comboBoxConfForDimTab.DropDownWidth = lonhName;
                                            comboBoxConfForDimTab.Items.Add(confName);
                                        }
                                        comboBoxConfForDimTab.SelectedItem = cmp.ReferencedConfiguration;
                                        tbpParams.Controls.Add(comboBoxConfForDimTab);

                                        _comboBoxListForRedraw.Add(comboBoxConfForDimTab,
                                                                   dimensionConfForList.IdSlave);

                                        comboBoxConfForDimTab.SelectedIndexChanged +=
                                            ComboBoxConfForDimTabSelectedIndexChanged;

                                        #endregion
                                    }
                                    i++;
                                }


                                #region Добавить выбор конфигурации
                                try
                                {
                                    if (_swSelModel.GetConfigurationCount() > 1)
                                    {
                                        var lblNew = new Label { Size = new Size(125, 24) };
                                        lblNew.Location = new Point(0, 48 + i * (lblNew.Size.Height + 6));
                                        lblNew.Name = "lblPrm" + i;
                                        lblNew.TextAlign = ContentAlignment.MiddleRight;
                                        lblNew.Text = "Конфигурации:";
                                        tbpParams.Controls.Add(lblNew);
                                        //lblNew.Tag = dimensionConfForList.StrObjName;
                                        downPos = lblNew.Location.Y + lblNew.Size.Height;
                                        string[] configurations = _swSelModel.GetConfigurationNames();
                                        var comboBoxConfig = new ComboBox
                                        {
                                            Size = new Size(56, 24),
                                            Location = new Point(lblNew.Location.X + lblNew.Size.Width + 4, lblNew.Location.Y),
                                        };
                                        string activeConf = _swSelComp.ReferencedConfiguration;//_swSelModel.IGetActiveConfiguration().Name;
                                        foreach (var configuration in configurations)
                                        {
                                            int currentIndex = comboBoxConfig.Items.Add(configuration);
                                            if (configuration == activeConf)
                                                comboBoxConfig.SelectedIndex = currentIndex;
                                        }
                                        Size size = TextRenderer.MeasureText(activeConf, comboBoxConfig.Font);
                                        int lonhName = size.Width; //nameOfConfiguration.Length * 5;
                                        if (comboBoxConfig.DropDownWidth < lonhName)
                                            comboBoxConfig.DropDownWidth = lonhName;
                                        tbpParams.Controls.Add(comboBoxConfig);
                                        comboBoxConfig.SelectedIndexChanged += ConfigurationChanged;
                                    }
                                }
                                catch 
                                { }

                                #endregion
                                #endregion
                                _dictHeightPage.Add(tbpParams, downPos);
                            }

                            #endregion
                            #region Если есть comments
                            if (oleSchem.Rows.Cast<DataRow>().Any(
                               row => (string)row["TABLE_NAME"] == "comments"))
                            {
                                cm = new OleDbCommand("SELECT * FROM comments ORDER BY id", oleDb);
                                rd = cm.ExecuteReader();
                                if (rd.Read())
                                {
                                    try
                                    {
                                        CommentLayout.Visible = true;
                                        WarningPB.Visible = (bool)rd["showSimbol"];
                                        commentsTb.Text = (string)rd["comment"];

                                        Size size = TextRenderer.MeasureText(commentsTb.Text, commentsTb.Font);

                                        commentsTb.Height = (size.Width / (commentsTb.Width - 10)) * 30;


                                    }
                                    catch
                                    {
                                        CommentLayout.Visible = false;
                                    }
                                }
                                rd.Close();


                            }
                            else
                            {
                                CommentLayout.Visible = false;
                            }
                            #endregion

                            #region Decors

                            if (!ReloadDecorTab(oleDb, oleSchem))
                                return;

                            #endregion

                            oleDb.Close();

                            #endregion
                        }
                    }
                    else
                        if (_tabDec != null)
                            tabMain.Controls.Remove(_tabDec);

                    var swFeat = (Feature)_swSelModel.FirstFeature();
                    i = 0;

                    #region Position

                    var mates = _swSelComp.GetMates();
                    Dictionary<string, Mate2> existingMates = new Dictionary<string, Mate2>();
                    if (mates != null)
                    {
                        foreach (var mate in mates)
                        {
                            if (mate is Mate2)
                            {
                                var spec = (Mate2)mate;
                                int mec = spec.GetMateEntityCount();
                                if (mec > 1)
                                {
                                    for (int ik = 0; ik < mec; ik++)
                                    {
                                        MateEntity2 me = spec.MateEntity(ik);
                                        if (me.ReferenceComponent.Name.Contains(_swSelComp.Name))
                                        {
                                            Entity tt = me.Reference;
                                            var tt2 = tt as Feature;
                                           
                                            if (tt is RefPlane && tt2 != null)
                                            {

                                                if (!existingMates.ContainsKey(tt2.Name))
                                                    existingMates.Add(tt2.Name, spec);
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }

                    int downPosPosition = 0;
                    int tabIndex = 0;
                    while (swFeat != null)
                    {
                        if (swFeat.GetTypeName2() == "RefPlane")
                        {
                            string strPlaneName = swFeat.Name;

                            if (strPlaneName.Length > 1)
                            {
                                if (strPlaneName.Substring(0, 1) == "#")
                                {
                                    var btnNew = new Button { Size = new Size(120, 20) };
                                    btnNew.Location = new Point(62, 24 + i * (btnNew.Size.Height + 6));
                                    //if (Properties.Settings.Default.KitchenModeOn)
                                    //    btnNew.Name = strPlaneName;//"btnPos" + i;
                                    //else
                                    btnNew.Name = "btnPos" + i;

                                    btnNew.Text = strPlaneName.Substring(5);
                                    btnNew.Tag = strPlaneName;
                                    btnNew.TabIndex = tabIndex;
                                    tabIndex++;
                                    tbpPos.Controls.Add(btnNew);
                                    btnNew.Click += AddMate;
                                    downPosPosition = btnNew.Location.Y + btnNew.Size.Height;
                                    if (existingMates.ContainsKey(strPlaneName))
                                    {
                                        //добавить кнопку для отвязки
                                        var btnDeattach = new Button { Size = new Size(20, 20) };
                                        btnDeattach.Location = new Point(62 + btnNew.Size.Width + 10, 24 + i * (btnNew.Size.Height + 6));
                                        btnDeattach.Name = strPlaneName;
                                        btnDeattach.Text = "X";
                                        btnDeattach.Tag = existingMates[strPlaneName];
                                        tbpPos.Controls.Add(btnDeattach);
                                        btnDeattach.Click += DeleteMate;
                                    }
                                    i++;

                                }
                            }
                        }
                        swFeat = (Feature)swFeat.GetNextFeature();
                    }
                    _dictHeightPage.Add(tbpPos, downPosPosition);

                    #endregion

                    if (_lblPrm.Count == 0)
                    {
                        tabMain.SelectTab(tbpPos);
                        downPos = downPosPosition;
                        SetSizeForTab(downPos);
                        if (tabMain.Controls.Contains(tbpParams))
                        {
                            tabMain.Controls.Remove(tbpParams);
                            //Logging.Log.Instance.Fatal("Вкладка на РПД не показана.Смотреть listForDimensions");
                        }
                    }
                    if (rbMode1 != null && rbMode2 != null)
                    {
                        if (controlsToHideWhenChangeMode == null || controlsToHideWhenChangeMode.Count == 0)
                            pnlMode.Visible = false;
                        if (Properties.Settings.Default.DefaultRPDView)
                        {
                            rbMode1.CheckedChanged += new EventHandler(ModeCheckedChanged);
                            rbMode1.Checked = true;
                        }
                        else
                        {
                            rbMode2.Checked = true;
                            rbMode1.CheckedChanged += new EventHandler(ModeCheckedChanged);
                        }
                    }
                    SetSizeForTab(_dictHeightPage[tabMain.SelectedTab]);
                }
            }
        }


        private bool ReloadDecorTab(OleDbConnection oleDb, DataTable oleSchem)
        {
            OleDbCommand cm;
            OleDbDataReader rd;
            int downPosDec = 0;
            if (oleSchem.Rows.Cast<DataRow>().Any(
                row => (string)row["TABLE_NAME"] == "decors"))
            {
                if (_tabDec != null)
                    tabMain.Controls.Remove(_tabDec);
                if (AddTabDecor() && _tabDec != null)
                {
                    cm = new OleDbCommand("SELECT * FROM decors ORDER BY Number ", oleDb);
                    rd = cm.ExecuteReader();

                    var outComps = new LinkedList<Component2>();

                    if (_mSwAddin.GetComponents(_swSelModel.IGetActiveConfiguration().IGetRootComponent2(), outComps, true, false))
                    //if (_mSwAddin.GetComponents(_swSelComp, outComps, true, false))
                    {
                        // чтение из первой табличке decors
                        var listElementsForForm = Decors.GetListComponentForDecors(
                            _mSwAddin, rd,
                            outComps);
                        rd.Close();

                        // чтение из второй таблички config_names 
                        var newList = new List<DecorsListL>();
                        foreach (var decorsList in listElementsForForm)
                        {
                            cm =
                                new OleDbCommand(
                                    "SELECT * FROM config_names WHERE id = " +
                                    decorsList.Number,
                                    oleDb);
                            rd = cm.ExecuteReader();
                            if (rd.Read())
                            {
                                if (rd["captConf"] as string != null)
                                {
                                    newList.Add(new DecorsListL(decorsList.Number,
                                                                decorsList.Component,
                                                                (string)rd["captConf"],
                                                                (string)rd["captDec"]));
                                }
                                else
                                {
                                    newList.Add(new DecorsListL(decorsList.Number,
                                                                decorsList.Component,
                                                                string.Empty, (string)rd["captDec"]));
                                }
                            }
                            rd.Close();
                        }

                        int dinamicDownPosition = 30;
                        int dinamicDownPositionPack = 0;
                        if (newList.Count == 0)
                        {
                            Logging.Log.Instance.Fatal("Возможна ошибка или описка в таблицах decors и config_names. Обратитесь к разработчикам библиотеки." + _swSelComp.Name);
                            throw new Exception("Возможна ошибка или описка в таблицах decors и config_names. Обратитесь к разработчикам библиотеки.");
                        }
                        else
                            minimumconf = newList.Min(x => x.Number);
                        int diff = 0;
                        foreach (var newL in newList)
                        {
                            bool isAtrName = false;
                            ComboBox cb = null;
                            string name = newL.LabelDecName;

                            var sizeForLabel = (int)(name.Length * 2.5);

                            if (sizeForLabel < 25)
                                sizeForLabel = 25;

                            #region Конфигурации

                            if (newL.LabelConfName != "")
                            {
                                if (
                                    !listForDimensions.Select(x => x.Component).Contains(
                                        newL.Component))
                                {
                                    string nameForConfigLabel = newL.LabelConfName;

                                    if (name.Length < nameForConfigLabel.Length)
                                        sizeForLabel = (int)(nameForConfigLabel.Length * 2.5);


                                    var labelForConfigName = new Label
                                    {
                                        Size =
                                            new Size(50,
                                                     sizeForLabel),
                                        Location = new Point(0, dinamicDownPosition),
                                        Text =
                                            nameForConfigLabel,
                                        TextAlign =
                                            ContentAlignment.
                                            MiddleLeft
                                    };
                                    diff = dinamicDownPosition - dinamicDownPositionPack;
                                    packControlsWhenChangeMode.Add(labelForConfigName, diff);
                                    var comboBoxConfig = new ComboBox
                                    {
                                        Location = new Point(
                                            labelForConfigName.
                                                Location
                                                .X +
                                            labelForConfigName.Size
                                                .
                                                Width,
                                            labelForConfigName.
                                                Location
                                                .Y),
                                        Size = new Size(50, 35),
                                        Name = nameForConfigLabel
                                    };
                                    packControlsWhenChangeMode.Add(comboBoxConfig, diff);
                                    var labelConfigSelected = new Label
                                    {
                                        Location = new Point(labelForConfigName.Location.X, labelForConfigName.Location.Y > 35 ? labelForConfigName.Location.Y - 35 : 0),
                                        Size = new Size(248, 25),
                                        Font = new Font("Tahoma", (float)7, FontStyle.Bold)
                                    };
                                    var seperateLine = new Label
                                    {
                                        Location = new Point(labelForConfigName.Location.X, labelForConfigName.Location.Y > 48 ? labelForConfigName.Location.Y - 48 : 0),
                                        Size = new Size(_tabDec.Size.Width, 20),
                                        Text = "_______________________________________________________",
                                        ForeColor = Color.DarkGray
                                    };
                                    seperateLine.SendToBack();
                                    comboBoxConfig.Tag = new KeyValuePair<Label, ComboBox>(labelConfigSelected, null);
                                    cb = comboBoxConfig;

                                    comboBoxConfig.KeyDown += ComboBoxConfigKeyDown;

                                    cm =
                                        new OleDbCommand(
                                            "SELECT * FROM decors_conf WHERE id = " +
                                            newL.Number +
                                            " AND Visible = true", oleDb);
                                    rd = cm.ExecuteReader();
                                    var strList = new Dictionary<int, string>();
                                    int l = 0;
                                    int numberCurrentConf = 0;
                                    var namesOfConfigurations = new List<string>();
                                    while (rd.Read())
                                    {
                                        string confName = rd["Configuration"].ToString();
                                        namesOfConfigurations.Add(confName);

                                    }
                                    rd.Close();

                                    namesOfConfigurations.Sort((x, y) => x.CompareTo(y));
                                    bool wasChangeConf = false;
                                    foreach (                                                   //нахождение нужной конфигурации и перезагрузка детали для этой конфигурации,
                                        var nameOfConfiguration in namesOfConfigurations)       //чтобы активная конфигурация в детали совпадала с активной конфигурацией в компоненте
                                    {
                                        Size size = TextRenderer.MeasureText(nameOfConfiguration, comboBoxConfig.Font);
                                        int lonhName = size.Width; //nameOfConfiguration.Length * 5;
                                        if (comboBoxConfig.DropDownWidth < lonhName)
                                            comboBoxConfig.DropDownWidth = lonhName;
                                        if (newL.Component.ReferencedConfiguration ==
                                            nameOfConfiguration)
                                        {
                                            numberCurrentConf = l;
                                            ChangeConfigurationForReferenceModel(
                                                newL.Component, nameOfConfiguration);
                                            wasChangeConf = true;
                                        }
                                        comboBoxConfig.Items.Add(nameOfConfiguration);
                                        strList.Add(l, nameOfConfiguration);
                                        l++;
                                    }
                                    if (!wasChangeConf)
                                    {
                                        string nameConf = namesOfConfigurations.First();
                                        numberCurrentConf = 0;                             // если у модели больше конфигураций, чем указано в mdb
                                        newL.Component.ReferencedConfiguration = nameConf;// то мы вибираем первую по алфавиту, указаную там
                                        ChangeConfigurationForReferenceModel(
                                            newL.Component, nameConf);
                                    }

                                    _dictConfig.Add(
                                        new DecorComponentsWithCombo(newL.Number,
                                                                     comboBoxConfig,
                                                                     newL.Component),
                                        strList);
                                    comboBoxConfig.SelectedIndex = numberCurrentConf;
                                    if (!string.IsNullOrEmpty(comboBoxConfig.SelectedItem.ToString()))
                                    {
                                        labelConfigSelected.Text = comboBoxConfig.Name + " : " +
                                            comboBoxConfig.SelectedItem.ToString();
                                    }
                                    _tabDec.Controls.Add(labelForConfigName);
                                    _tabDec.Controls.Add(comboBoxConfig);
                                    controlsToHideWhenChangeMode.Add(labelConfigSelected);
                                    _tabDec.Controls.Add(labelConfigSelected);
                                    controlsToHideWhenChangeMode.Add(seperateLine);
                                    _tabDec.Controls.Add(seperateLine);
                                    isAtrName = true;
                                    comboBoxConfig.SelectedIndexChanged +=
                                        ComboBoxConfigSelectedIndexChanged;
                                }
                                else
                                    MessageBox.Show(@"Одинаковые управляющие компоненты " +
                                                    newL.LabelConfName, _mSwAddin.MyTitle,
                                                    MessageBoxButtons.OK,
                                                    MessageBoxIcon.Exclamation);
                            }
                            else
                            {
                                if (dinamicDownPosition == 30)
                                    dinamicDownPosition = 0;
                                else
                                    dinamicDownPosition -= 40;

                            }
                            #endregion

                            var lablDecor = new Label
                            {
                                Size =
                                    new Size(50, sizeForLabel),
                                Location = new Point(110, dinamicDownPosition),

                                Text = name
                            };

                            packControlsWhenChangeMode.Add(lablDecor, diff);

                            dinamicDownPosition = dinamicDownPosition + sizeForLabel + 45;
                            dinamicDownPositionPack = dinamicDownPositionPack + sizeForLabel + 5;

                            var comboBoxDecor = new ComboBox
                            {
                                Location =
                                    new Point(
                                    lablDecor.Location.X +
                                    lablDecor.Size.Width,
                                    lablDecor.Location.Y),
                                Size = new Size(40, 35),
                                Name = name,
                                Tag = newL.Number
                            };
                            packControlsWhenChangeMode.Add(comboBoxDecor, diff);
                            if (isAtrName)
                                cb.Tag = new KeyValuePair<Label, ComboBox>(((KeyValuePair<Label, ComboBox>)cb.Tag).Key, comboBoxDecor);

                            #region Новая кнопка

                            Button btnAllMod = null;
                            Button btnFitDecor = null;
                            var mdoc = newL.Component.IGetModelDoc();
                            if (mdoc != null &&
                                mdoc.get_CustomInfo2("", "Accessories") != "Yes")
                            {
                                var toolTip1 = new ToolTip();
                                btnAllMod = new Button
                                {
                                    Location =
                                        new Point(
                                        comboBoxDecor.Location.X +
                                        comboBoxDecor.Size.Width + 5,
                                        comboBoxDecor.Location.Y),
                                    Tag = comboBoxDecor.Name,
                                    Name = newL.Number.ToString(),
                                    BackgroundImage = bitmap,
                                    BackgroundImageLayout = ImageLayout.Stretch
                                    //Image = bitmap
                                };
                                packControlsWhenChangeMode.Add(btnAllMod, diff);
                                btnAllMod.Size = new Size(21, 21);//new Size(30, btnAllMod.Size.Height);
                                var r = new Rectangle(btnAllMod.Location, btnAllMod.Size);
                                btnAllMod.DrawToBitmap(bitmap, r);
                                toolTip1.SetToolTip(btnAllMod,
                                                    "присвоить цвет всем деталям данного типа");
                                btnAllMod.Click += BtnAllModClick;
                                _tabDec.Controls.Add(btnAllMod);

                                var toolTip2 = new ToolTip();
                                Image image = imageList1.Images[3];
                                btnFitDecor = new Button
                                {
                                    Location =
                                        new Point(
                                        comboBoxDecor.Location.X +
                                        comboBoxDecor.Size.Width + 5 + 25,
                                        comboBoxDecor.Location.Y),
                                    Tag = comboBoxDecor.Name,
                                    Name = newL.Number.ToString() + "ex",
                                    BackgroundImage = image,
                                    BackgroundImageLayout = ImageLayout.Stretch,
                                    //ImageKey = @"expand.ico",
                                    ImageList = imageList1
                                };
                                packControlsWhenChangeMode.Add(btnFitDecor, diff);
                                toolTip2.SetToolTip(btnFitDecor, "растянуть декор по ширине и высоте");
                                btnFitDecor.Size = new Size(21, 21);//new Size(24, btnFitDecor.Size.Height);
                                btnFitDecor.Tag = comboBoxDecor.Name;
                                btnFitDecor.Click += BtnFitDecorClick;
                                _tabDec.Controls.Add(btnFitDecor);
                            }

                            #endregion

                            groupBox1.Location = new Point(groupBox1.Location.X,
                                                           lablDecor.Location.Y +
                                                           lablDecor.Size.Height + 5);

                            if (!packControlsWhenChangeMode.ContainsKey(groupBox1))
                                packControlsWhenChangeMode.Add(groupBox1, diff);
                            else
                                packControlsWhenChangeMode[groupBox1] = diff;

                            if (pnlMode == null)
                            {
                                pnlMode = new Panel()
                                {
                                    Location =
                                        new Point(5,
                                                  groupBox1.Location.Y +
                                                  groupBox1.Size.Height - 17),
                                    Size = new Size(_tabDec.Size.Width - 10, 37)
                                };

                                rbMode1 = new RadioButton()
                                {
                                    Location =
                                        new Point(10, 15),
                                    Text = "Сокращенный",
                                    Size = new Size(groupBox1.Size.Width / 2 + 40, 30)
                                };
                                rbMode2 = new RadioButton()
                                {
                                    Location =
                                        new Point(pnlMode.Size.Width / 2 + 25, rbMode1.Location.Y),
                                    Text = "Полный",
                                    Size = new Size(groupBox1.Size.Width / 2, 30)
                                };




                                pnlMode.Controls.Add(rbMode1);
                                pnlMode.Controls.Add(rbMode2);
                                _tabDec.Controls.Add(pnlMode);
                            }
                            else
                            {
                                pnlMode.Location = new Point(groupBox1.Location.X - 30,
                                                            groupBox1.Location.Y +
                                                            groupBox1.Size.Height - 17);
                                rbMode1.Location = new Point(10, 15);
                                rbMode2.Location = new Point(pnlMode.Size.Width / 2 + 25, rbMode1.Location.Y);
                            }
                            if (!packControlsWhenChangeMode.ContainsKey(pnlMode))
                                packControlsWhenChangeMode.Add(pnlMode, diff);
                            else
                                packControlsWhenChangeMode[pnlMode] = diff;

                            var list = new Dictionary<int, string>();

                            downPosDec = pnlMode.Location.Y +
                                        pnlMode.Size.Height - 12;

                            string decPathDef = Furniture.Helpers.LocalAccounts.decorPathResult;
                            string decPathFileNameWithoutExt = "";
                            cm = new OleDbCommand("SELECT * FROM decors_conf WHERE id = " +
                                                  newL.Number + " AND Visible = true", oleDb);
                            rd = cm.ExecuteReader();
                            string strConfForComponent = "";
                            if (rd.Read())
                                strConfForComponent = rd["Configuration"].ToString();
                            rd.Close();
                            rd = cm.ExecuteReader();

                            if (newL.Component.IGetModelDoc().GetConfigurationCount() > 1 &&
                                strConfForComponent != "" && strConfForComponent != "all")
                            {
                                while (rd.Read())
                                {
                                    if ((string)rd["Configuration"] == newL.Component.ReferencedConfiguration)
                                    {
                                        decPathFileNameWithoutExt = rd["Group"].ToString();
                                        break;
                                    }
                                }
                            }
                            else if (rd.Read())
                                decPathFileNameWithoutExt = rd["Group"].ToString();
                            rd.Close();

                            _tabDec.Controls.Add(comboBoxDecor);

                            if (decPathFileNameWithoutExt == "")
                            {
                                if (btnAllMod != null)
                                    btnAllMod.Visible = false;
                                comboBoxDecor.Visible = false;
                                comboBoxDecor.MouseCaptureChanged +=
                                    ComboBoxDecorMouseCaptureChanged;
                                comboBoxDecor.SelectedIndexChanged +=
                                    ComboBoxDecorSelectedIndexChanged;
                                comboBoxDecor.KeyDown += ComboBoxDecorKeyDown;
                                continue;
                            }
                            string path;
                            try
                            {
                                path =
                                    GetTextureFileFromRenderMaterial(
                                        newL.Component.IGetModelDoc());
                            }
                            catch
                            {
                                path = "";
                            }
                            var lastOleDb = oleDb;
                            oleDb.Close();

                            bool isNotUniqDecors = false;
                            if (path == "" &&
                                Properties.Settings.Default.SetDecorsFromFirstElement &&
                                Decors.MemoryForDecors != null &&
                                Decors.MemoryForDecors.ContainsKey(name))
                            {
                                path = Decors.MemoryForDecors[name];
                                isNotUniqDecors = true;
                            }

                            if (!OpenOleDecors(out oleDb))
                                return false;
                            int m = 0;
                            string[] outVal;
                            bool isDecor = false;
                            string selectStr;
                            if (GetDecorsPathArray(decPathFileNameWithoutExt, out outVal))
                            {
                                string bigSelectStr = "SELECT * FROM decordef WHERE ";

                                for (int l = 0; l < outVal.Count(); l++)
                                {
                                    if (l == 0)
                                        bigSelectStr = bigSelectStr + outVal[l] + " = true";
                                    else
                                        bigSelectStr = bigSelectStr + " OR " + outVal[l] +
                                                       " = true";
                                }
                                selectStr = bigSelectStr;
                            }
                            else
                                selectStr = "SELECT * FROM decordef WHERE " +
                                            decPathFileNameWithoutExt + " = true";
                            cm = new OleDbCommand(selectStr, oleDb);
                            rd = cm.ExecuteReader();

                            var strNameList = new List<string>();
                            while (rd.Read())
                            {
                                strNameList.Add(rd["FILEJPG"].ToString());
                            }
                            rd.Close();
                            oleDb.Close();
                            oleDb = lastOleDb;
                            oleDb.Open();
                            strNameList.Sort((x, y) => x.CompareTo(y));

                            bool isDefaultDecInOurColumn = false;
                            int maxLengthOfName = 2;
                            foreach (var fileName in strNameList)
                            {
                                string oneOfPath = decPathDef + fileName + ".jpg";
                                if (File.Exists(oneOfPath))
                                {
                                    if (fileName.Length > maxLengthOfName)
                                        maxLengthOfName = fileName.Length;
                                    list.Add(m, oneOfPath);
                                    comboBoxDecor.Items.Add(
                                        Path.GetFileNameWithoutExtension(oneOfPath));
                                    if (Path.GetFileNameWithoutExtension(path) == Path.GetFileNameWithoutExtension(oneOfPath))//(path == oneOfPath)
                                    {
                                        comboBoxDecor.SelectedIndex = m;
                                        if (!isNotUniqDecors)
                                            isDecor = true;
                                        else
                                            isDefaultDecInOurColumn = true;
                                        comboBoxDecor.MouseCaptureChanged +=
                                            ComboBoxDecorMouseCaptureChanged;
                                    }
                                    m++;
                                }
                                else
                                    MessageBox.Show(
                                        @"Файл" + Environment.NewLine + oneOfPath
                                        + Environment.NewLine + @"не существует!",
                                        _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                            }
                            if (maxLengthOfName > 2)
                            {
                                comboBoxDecor.Size = new Size(13 * maxLengthOfName,
                                                              comboBoxDecor.Size.Width);
                                if (btnAllMod != null)
                                    btnAllMod.Location =
                                        new Point(
                                            comboBoxDecor.Location.X +
                                            comboBoxDecor.Size.Width +
                                            5, comboBoxDecor.Location.Y);
                                if (btnFitDecor != null)
                                    btnFitDecor.Location =
                                        new Point(
                                            comboBoxDecor.Location.X +
                                            comboBoxDecor.Size.Width +
                                            5 + 25, comboBoxDecor.Location.Y);
                            }

                            _tabDec.Controls.Add(lablDecor);
                            _dictionary.Add(comboBoxDecor, list);

                            // добавление нового элемента в общий список элементов подверженных изменению
                            _commonList.Add(comboBoxDecor);

                            if (isDecor)
                            {
                                if (Properties.Settings.Default.CashModeOn)
                                {
                                    _objListChanged.Add(comboBoxDecor);
                                }
                                SetDecorPicture(comboBoxDecor, false, true);//Properties.Settings.Default.CashModeOn, true);
                            }
                            else
                            {
                                if (((isDefaultDecInOurColumn && isNotUniqDecors) || (Properties.Settings.Default.CashModeOn && _dictionary[comboBoxDecor].ContainsKey(comboBoxDecor.SelectedIndex))) &&
                                    !_objListChanged.Contains(comboBoxDecor))
                                {
                                    AddDict(comboBoxDecor,
                                            _dictionary[comboBoxDecor][
                                                comboBoxDecor.SelectedIndex]);
                                    _objListChanged.Add(comboBoxDecor);
                                }
                            }

                            comboBoxDecor.SelectedIndexChanged +=
                                ComboBoxDecorSelectedIndexChanged;
                            comboBoxDecor.KeyDown += ComboBoxDecorKeyDown;
                        }
                    }
                    _dictHeightPage.Add(_tabDec, downPosDec);
                }
            }
            else
            {
                if (_tabDec != null)
                    tabMain.Controls.Remove(_tabDec);
            }
            return true;
        }

        private void ModeCheckedChanged(object sender, EventArgs e)
        {
            if (pnlMode == null)
            {
                pnlMode = new Panel()
                {
                    Location =
                        new Point(5,
                                  groupBox1.Location.Y +
                                  groupBox1.Size.Height - 17),
                    Size = new Size(_tabDec.Size.Width - 10, 37)
                };
            }
            if (rbMode1.Checked)
            {
                foreach (var c in controlsToHideWhenChangeMode)
                {
                    c.Hide();
                }
                foreach (var c in packControlsWhenChangeMode)
                {
                    c.Key.Location = new Point(c.Key.Location.X, c.Key.Location.Y - c.Value);
                }
                var downPosDec = pnlMode.Location.Y + pnlMode.Size.Height - 12;
                if (!_dictHeightPage.ContainsKey(_tabDec))
                    _dictHeightPage.Add(_tabDec, downPosDec);
                else
                    _dictHeightPage[_tabDec] = downPosDec;

                SetSizeForTab(_dictHeightPage[tabMain.SelectedTab]);
            }
            else
            {
                foreach (var c in controlsToHideWhenChangeMode)
                {
                    c.Show();
                }
                foreach (var c in packControlsWhenChangeMode)
                {
                    c.Key.Location = new Point(c.Key.Location.X, c.Key.Location.Y + c.Value);
                }
                var downPosDec = pnlMode.Location.Y + pnlMode.Size.Height - 12;
                if (!_dictHeightPage.ContainsKey(_tabDec))
                    _dictHeightPage.Add(_tabDec, downPosDec);
                else
                    _dictHeightPage[_tabDec] = downPosDec;
                SetSizeForTab(_dictHeightPage[tabMain.SelectedTab]);
            }
        }


        private void ChangeConfigurationForReferenceModel(Component2 comp, string nameConfiguration)
        {
            int err = 0, wrn = 0;
            var mod =
                _mSwAddin.SwApp.OpenDoc6(
                    comp.IGetModelDoc().
                        GetPathName(),
                    (int)swDocumentTypes_e.swDocPART, 0, "",
                    ref err, ref wrn);
            if (mod != null)
            {
                mod.ShowConfiguration2(nameConfiguration);
                mod.Save();  
                _mSwAddin.SwApp.CloseDoc(mod.GetPathName());
            }
        }

        //private Component2 GetMdbComponentTopLevel(Component2 upComponent, out ModelDoc2 specModel)
        //{
        //    Component2 specComp = null;
        //    specModel = upComponent.IGetModelDoc();
        //    bool cycle = true;
        //    while (cycle)
        //    {
        //        Component2 specComp2 = upComponent;
        //        if (specComp != null && specComp == specComp2)
        //        {
        //            var comp = specComp.GetParent();
        //            if (comp != null)
        //                specComp2 = comp;
        //            else
        //                break;
        //        }
        //        specComp = specComp2;
        //        cycle = _mSwAddin.GetParentLibraryComponent(specComp, out specComp2);
        //        if (cycle)
        //        {
        //            upComponent = specComp2;
        //        }
        //    }
        //    return upComponent;
        //}

        private void CheckDWRexistAfterCopy(object o)
        {
            lock (SwAddin.workerLocker)
                Monitor.Wait(SwAddin.workerLocker);
            string path = o as string;
            if (string.IsNullOrEmpty(path))
                return;
            if (!File.Exists(path))
            {
                if (!linkLabel1.InvokeRequired)
                {
                    linkLabel1.Click -= LinkLabel1Click;
                    linkLabel1.Text = string.Empty;
                }
                else
                {
                    linkLabel1.Invoke(new EventHandler(delegate
                                                           {

                                                               linkLabel1.Click -= LinkLabel1Click;
                                                               linkLabel1.Text = string.Empty;
                                                           }));
                }
            }
        }

        private void RefreshLabel(Component2 swTestSelComp)
        {
            var comp = swTestSelComp;
            string textInfo = "";
            bool isDraft = false;
            do
            {
                var m = comp.IGetModelDoc();
                if (m != null)
                {
                    bool draft = (m.CustomInfo2["", "Required Draft"] == "Yes" ||
                                    m.CustomInfo2[m.IGetActiveConfiguration().Name, "Required Draft"] == "Yes");
                    if (draft)
                    {
                        var dir = new DirectoryInfo(Path.GetDirectoryName(_mSwAddin.SwModel.GetPathName()));
                        var paths =
                            dir.GetFiles(
                                Path.GetFileNameWithoutExtension(m.GetPathName()) + ".SLDDRW",
                                SearchOption.AllDirectories);

                        string path = null;

                        if (paths.Any())
                            path = paths[0].FullName;
                        else
                        {
                            if (SwAddin.needWait)
                            {
                                path = Path.Combine(Path.GetDirectoryName(m.GetPathName()),
                                                    Path.GetFileNameWithoutExtension(m.GetPathName()) +
                                                    ".SLDDRW");
                                ThreadPool.QueueUserWorkItem(CheckDWRexistAfterCopy, path);
                            }
                        }

                        if (!string.IsNullOrEmpty(path))
                        {
                            linkLabel1.Name = path;
                            linkLabel1.Click += LinkLabel1Click;
                            textInfo = m.CustomInfo2["", "Sketch Number"];
                            isDraft = true;
                            break;
                        }
                    }
                }
                comp = comp.GetParent();
            } while (comp != null);

            if (isDraft)
            {
                if (linkLabel1.Text == "")
                {
                    tabMain.Location = new Point(8, 70);
                    btnOK.Location = new Point(btnOK.Location.X, btnOK.Location.Y + 30);
                    btnCancel.Location = new Point(btnCancel.Location.X, btnCancel.Location.Y + 30);
                    Size = new Size(Size.Width, Size.Height + 30);
                }
                if (textInfo == "" || textInfo == "0" || textInfo == "Sketch Number")
                    linkLabel1.Text = @"ЭСКИЗ № н/о";
                else
                    linkLabel1.Text = @"ЭСКИЗ № " + textInfo;
            }
            else
            {
                if (linkLabel1.Text != "")
                {
                    tabMain.Location = new Point(8, 40);
                    btnOK.Location = new Point(btnOK.Location.X, btnOK.Location.Y - 30);
                    btnCancel.Location = new Point(btnCancel.Location.X, btnCancel.Location.Y - 30);
                    Size = new Size(Size.Width, Size.Height - 30);
                    linkLabel1.Text = "";
                }
            }
        }

        private static List<int> GetListIntFromString(string s)
        {
            var list = new List<int>();
            var strArr = s.Split(',');
            foreach (var s1 in strArr)
            {
                s = s1.Trim();
                try
                {
                    int i = Convert.ToInt32(s);
                    list.Add(i);
                }
                catch
                {
                    MessageBox.Show(@"Ошибка в записи данных в таблице 'objects' столбца 'idslave'", @"MrDoors",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            return list;
        }

        private static int GetCorrectIntValue(double strObjVal)
        {
            var one = (int)strObjVal;
            double two = strObjVal - one;
            if (two >= 0.5)
                return (one + 1);
            return one;
        }

        internal static bool OpenOleDecors(out OleDbConnection outOleDb)
        {
            outOleDb = null;
            string strDbName = "";
            foreach (var file in Directory.GetFiles(Furniture.Helpers.LocalAccounts.decorPathResult))
            {
                if (Path.GetFileName(file) == "decordef.mdb")
                {
                    strDbName = file;
                    break;
                }
            }

            if (strDbName != "")
            {
                int i = IntPtr.Size;
                outOleDb = i == 8
                               ? new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source = " + strDbName)
                               : new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;" + "data source = " + strDbName);
            }
            else
            {
                MessageBox.Show(@"Не найден файл базы данных декоров", @"MrDoors", MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return false;
            }
            outOleDb.Open();
            return true;
        }

        internal static bool GetDecorsPathArray(string valFromMdb, out string[] outVal)
        {
            bool ret = false;
            outVal = new string[] { };
            int i = 0;
            if (valFromMdb.Contains('+'))
            {
                string[] arrVal = valFromMdb.Split('+');

                outVal = new string[arrVal.Length];
                foreach (var s in arrVal)
                {
                    outVal[i] = s.TrimStart().TrimEnd();
                    i++;
                }
                ret = true;
            }
            return ret;
        }

        private void ChangeConfigurationComponent(ComboBox combConfig)
        {
            IsNewSelectionDisabled = true;
            int i = combConfig.SelectedIndex;
            int number = _dictConfig.Where(x => x.Key.Combo == combConfig).First().Key.Number;
            var nameConf = _dictConfig.Where(x => x.Key.Combo == combConfig).Select(x => x.Value.Where(y => y.Key == i).First()).First().Value;

            List<Component2> comps = Decors.GetConfigComponents(_mSwAddin, _swSelModel, number);
            foreach (Component2 component in comps)
            {
                if (component.GetTexture("") != null)
                {
                    string matName = GetTextureFileFromRenderMaterial(component.IGetModelDoc(), true);

                    for (int k = 1; k < 10; k++)
                    {
                        string val = component.IGetModelDoc().get_CustomInfo2("", "Color" + k);
                        if (Path.GetFileNameWithoutExtension(matName) == val)
                        {
                            component.IGetModelDoc().set_CustomInfo2("", "Color" + k, "Color" + k);
                            break;
                        }
                    }
                }

                bool b1 = component.Select(false);
                bool b2 = ((AssemblyDoc)_swSelModel).CompConfigProperties4(2, 0, true, true, nameConf, false);
                if (b1 && b2)
                {
                    _swSelModel.EditRebuild3();
                    int err = 0, wrn = 0;
                    var mod = _mSwAddin.SwApp.OpenDoc6(component.IGetModelDoc().GetPathName(),
                                                       (int)swDocumentTypes_e.swDocPART, 0, "", ref err, ref wrn);
                    if (mod != null)
                    {
                        mod.ShowConfiguration2(nameConf);
                        mod.Save(); 
                        _mSwAddin.SwApp.CloseDoc(mod.GetPathName());
                    }
                }

            }

            IsNewSelectionDisabled = false;

            OleDbConnection oleDb;
            if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
            {
                using (oleDb)
                {
                    _setNewListForComboBox = false;
                    var changedBox = ((KeyValuePair<Label, ComboBox>)combConfig.Tag).Value;//(ComboBox)combConfig.Tag;
                    if (changedBox.Items.Count == 1)
                        changedBox.SelectedIndex = -1;
                    try
                    {
                        changedBox.Items.RemoveAt(changedBox.SelectedIndex);
                    }
                    catch { }
                    changedBox.Items.Clear();

                    string decPathDef = Furniture.Helpers.LocalAccounts.decorPathResult;
                    string decPathFileNameWithoutExt = "";
                    var cm = new OleDbCommand("SELECT * FROM decors_conf WHERE id = " + number, oleDb);
                    var rd = cm.ExecuteReader();
                    while (rd.Read())
                    {
                        if ((string)rd["Configuration"] == nameConf)
                        {
                            decPathFileNameWithoutExt = rd["Group"].ToString();
                            break;
                        }
                    }
                    rd.Close();
                    oleDb.Close();

                    if (decPathFileNameWithoutExt == "")
                    {
                        if (changedBox.Visible)
                        {
                            changedBox.Visible = false;
                            var btn = _tabDec.Controls[number.ToString()] as Button;
                            if (btn != null)
                                btn.Visible = false;
                        }
                        if (_commonList.Contains(changedBox))
                            _commonList.Remove(changedBox);
                        changedBox.MouseCaptureChanged -= ComboBoxDecorMouseCaptureChanged;
                        _setNewListForComboBox = true;
                        return;
                    }
                    if (!changedBox.Visible)
                    {
                        changedBox.Visible = true;
                        var btn = _tabDec.Controls[number.ToString()] as Button;
                        if (btn != null)
                            btn.Visible = true;
                        if (!_commonList.Contains(changedBox))
                            _commonList.Add(changedBox);
                    }

                    int m = 0;
                    var list = new Dictionary<int, string>();

                    if (!OpenOleDecors(out oleDb))
                        return;

                    string[] outVal;
                    string selectStr;
                    if (GetDecorsPathArray(decPathFileNameWithoutExt, out outVal))
                    {
                        string bigSelectStr = "SELECT * FROM decordef WHERE ";

                        for (int l = 0; l < outVal.Count(); l++)
                        {
                            if (l == 0)
                                bigSelectStr = bigSelectStr + outVal[l] + " = true";
                            else
                                bigSelectStr = bigSelectStr + " OR " + outVal[l] + " = true";
                        }
                        selectStr = bigSelectStr;
                    }
                    else
                        selectStr = "SELECT * FROM decordef WHERE " + decPathFileNameWithoutExt + " = true";
                    cm = new OleDbCommand(selectStr, oleDb);
                    rd = cm.ExecuteReader();
                    var strName = new List<string>();
                    while (rd.Read())
                    {
                        strName.Add(rd["FILEJPG"].ToString());
                    }
                    rd.Close();
                    oleDb.Close();
                    strName.Sort((x, y) => x.CompareTo(y));

                    int maxLengthOfName = 2;
                    foreach (var name in strName)
                    {
                        string oneOfPath = decPathDef + name + ".jpg";
                        if (File.Exists(oneOfPath))
                        {
                            if (name.Length > maxLengthOfName)
                                maxLengthOfName = name.Length;

                            list.Add(m, oneOfPath);
                            changedBox.Items.Add(Path.GetFileNameWithoutExtension(oneOfPath));
                            m++;
                        }
                        else
                            MessageBox.Show(
                                @"Файл" + Environment.NewLine + oneOfPath
                                + Environment.NewLine + @"не существует!",
                                _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                    }
                    if (maxLengthOfName > 2)
                    {
                        changedBox.Size = new Size(maxLengthOfName * 13, changedBox.Size.Width);
                        var btn = _tabDec.Controls[number.ToString()] as Button;
                        if (btn != null)
                        {
                            btn.Location = new Point(changedBox.Location.X + changedBox.Size.Width + 5, btn.Location.Y);
                        }
                        var btn2 = _tabDec.Controls[number.ToString() + "ex"] as Button;
                        if (btn2 != null)
                        {
                            btn2.Location = new Point(changedBox.Location.X + changedBox.Size.Width + 25 + 5, btn.Location.Y);
                        }
                    }
                    else if (changedBox.Size.Width != 40)
                    {
                        changedBox.Size = new Size(40, 35);
                        var btn = _tabDec.Controls[number.ToString()] as Button;
                        if (btn != null)
                        {
                            btn.Location = new Point(changedBox.Location.X + changedBox.Size.Width + 5, btn.Location.Y);
                        }
                        var btn2 = _tabDec.Controls[number.ToString() + "ex"] as Button;
                        if (btn2 != null)
                        {
                            btn2.Location = new Point(changedBox.Location.X + changedBox.Size.Width + 25 + 5, btn.Location.Y);
                        }
                    }

                    _dictPathPict[changedBox] = "";
                    _dictionary[changedBox] = list;
                    pictureBox1.Image = null;
                    changedBox.MouseCaptureChanged -= ComboBoxDecorMouseCaptureChanged;
                    _setNewListForComboBox = true;
                }
            }
        }

        private bool AddTabDecor()
        {
            try
            {
                _tabDec = new TabPage();
                _tabDec.SuspendLayout();
                //tabMain.Controls.Add(_tabDec);
                int priceIndex = tabMain.TabPages.IndexOf(tbpPrice);
                tabMain.TabPages.Insert(priceIndex, _tabDec);
                _tabDec.Controls.Add(groupBox1);
                _tabDec.Location = new Point(4, 22);
                _tabDec.Name = "tabDec";
                _tabDec.Padding = new Padding(3);
                _tabDec.Size = new Size(230, 202);
                _tabDec.TabIndex = 2;
                _tabDec.Text = @"Декоры";
                _tabDec.UseVisualStyleBackColor = true;
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool SaveChanges(bool check, bool fit)
        {
            try
            {
                if (!this.InvokeRequired)
                    this.Enabled = false;
                else
                {
                    this.Invoke(new EventHandler(delegate
                    {

                        this.Enabled = false;
                    }));
                }
                if (check)
                {
                    foreach (var objUnit in _commonList)
                    {
                        if (objUnit.GetType() == typeof(TextBox))
                        {
                            var textBox = (TextBox)objUnit;
                            if (textBox.Text == "")
                            {
                                MessageBox.Show(
                                    @"В поле с именем " + textBox.Name.Split('@').First() + @" не указан размер!",
                                    _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                tabMain.SelectTab(tbpParams);
                                textBox.Focus();
                                return false;
                            }
                        }
                        else
                        {
                            var combBox = (ComboBox)objUnit;
                            if (combBox.Text == "")
                            {
                                MessageBox.Show(@"В поле с именем " + combBox.Name + @" не указано название цвета!",
                                                _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                tabMain.SelectTab(_tabDec);
                                combBox.Focus();
                                return false;
                            }
                        }
                    }
                }
                bool isGoodValue = false;
                OleDbConnection oleDb;
                var delList = new List<object>();
                if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                {
                    foreach (var objUnit in _objListChanged)
                    {
                        if (objUnit.GetType() == typeof(TextBox))
                        {
                            var txtBox = (TextBox)objUnit;
                            string tagOfTextBox = txtBox.Name;
                            if (txtBox.Text == tagOfTextBox.Split('@').Last()) continue;
                            isGoodValue = SavePartChanges(oleDb, txtBox);
                            if (!isGoodValue)
                            {
                                txtBox.Focus();
                                oleDb.Close();
                                return false;
                            }
                            delList.Add(txtBox);
                        }
                    }

                    #region Обработка таблицы dimlimits
                    if (delList.Count > 0 && _namesOfColumnNameFromDimLimits.Count > 0)
                    {
                        var objNames = _namesOfColumnNameFromDimLimits.Where(x => x.Contains("obj"));
                        var listId = new List<int>();
                        foreach (var objName in objNames)
                        {
                            int id = GetIdFromColumnName(objName);
                            if (!listId.Contains(id))
                                listId.Add(id);
                        }

                        var cm = new OleDbCommand("SELECT * FROM dimlimits", oleDb);
                        var rd = cm.ExecuteReader();
                        bool isNotNeededRanges = false;
                        int coincidentCount = 0;
                        var dB = new Dictionary<TextBox, Boolean>();

                        while (rd.Read())
                        {
                            var db = new Dictionary<TextBox, Boolean>();
                            foreach (var i in listId)
                            {
                                var mn = (int)rd["obj" + i + "min"];
                                var mx = (int)rd["obj" + i + "max"];
                                var txtBx = _numbAndTextBoxes[i];
                                int val = Convert.ToInt32(txtBx.Text);
                                db.Add(txtBx, (mn <= val) && (val <= mx));
                            }

                            if (db.Values.Aggregate(true, (current, b) => (b && current)))
                            {
                                isNotNeededRanges = false;
                                if (_namesOfColumnNameFromDimLimits.Contains("stdsketchnum"))
                                    _mSwAddin.SetModelProperty(_swSelModel, "stdsketchNum", "",
                                                               swCustomInfoType_e.swCustomInfoText,
                                                               rd["stdsketchnum"].ToString());
                                break;
                            }
                            int imm = db.Values.Where(x => x).Count();
                            if (imm >= coincidentCount)
                            {
                                coincidentCount = imm;
                                dB = db;
                            }
                            isNotNeededRanges = true;
                        }
                        rd.Close();

                        if (isNotNeededRanges)
                        {
                            string errText = @"Нет совпадающих значений для параметров ";
                            int i = 0;
                            var fff = dB.Where(x => x.Value == false);
                            foreach (var o in dB.Where(x => x.Value == false))
                            {
                                var t = o.Key;
                                string vl = t.Text;
                                t.Text = t.Name.Split('@').Last();
                                string tName = t.Name.Split('@').First();
                                if (i != fff.Count() - 1)
                                    errText = errText + " " + tName + " со значением " + vl + " и ";
                                else
                                    errText = errText + " " + tName + " со значением " + vl + "!";
                                i++;
                            }
                            MessageBox.Show(errText, _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            oleDb.Close();
                            return false;
                        }
                    }
                    #endregion

                    foreach (var o in delList)
                    {
                        SetValueForTxtBx((TextBox)o);
                        if (_objListChanged.Contains(o))
                            _objListChanged.Remove(o);
                    }
                    if (isGoodValue)
                    {
                        _swSelModel.EditRebuild3();
                        var swComps = new LinkedList<Component2>();
                        IsNewSelectionDisabled = true;
                        if (_mSwAddin.GetComponents(_swSelComp, swComps, false, false))
                        {
                            foreach (var component2 in swComps)
                            {
                                var swModel = component2.IGetModelDoc();
                                if ((swModel != null) && (_mSwAddin.GetModelDatabaseFileName(swModel) != ""))
                                {
                                    _mSwAddin.RecalculateModel(swModel);
                                }
                            }
                        }
                        _mSwAddin.RecalculateModel(_swSelModel);
                        IsNewSelectionDisabled = false;
                        _swModel.EditRebuild3();

                        #region Перестроение TextBox зависимых от других
                        foreach (var o in delList)
                        {
                            var txt = (TextBox)o;
                            if (_textBoxListForRedraw.ContainsKey(txt))
                                foreach (var num in _textBoxListForRedraw[txt])
                                {
                                    if (_numbAndTextBoxes.ContainsKey(num))
                                    {
                                        var cm = new OleDbCommand("SELECT * FROM objects WHERE id =" + num,
                                                                  oleDb);
                                        OleDbDataReader rd = cm.ExecuteReader();
                                        if (rd.Read())
                                        {
                                            string strObjName = rd["name"].ToString();
                                            double strObjVal;

                                            if (_mSwAddin.GetObjectValue(_swSelModel, strObjName, (int)rd["type"],
                                                                         out strObjVal))
                                            {
                                                var val = GetCorrectIntValue(strObjVal);
                                                _numbAndTextBoxes[num].Text = val.ToString();
                                            }
                                            rd.Close();
                                        }
                                    }
                                }
                        }
                        #endregion
                    }
                }
                oleDb.Close();
                delList.Clear();

                #region Установка декоров в измененных полях

                var dict = new Dictionary<ComboBox, bool>();
                foreach (var objUnit in _objListChanged)
                {
                    if (objUnit.GetType() == typeof(ComboBox))
                    {
                        var contr = (ComboBox)objUnit;

                        if (Properties.Settings.Default.SetDecorsFromFirstElement && Decors.MemoryForDecors != null &&
                            Decors.MemoryForDecors.ContainsKey(contr.Name) &&
                            Decors.MemoryForDecors[contr.Name] != _dictPathPict[contr])
                            if (MessageBox.Show(
                                @"Сменить выбранный декор по умолчанию с " +
                                Path.GetFileNameWithoutExtension(Decors.MemoryForDecors[contr.Name]) + @" на " +
                                Path.GetFileNameWithoutExtension(_dictPathPict[contr]) + @" для последующих деталей?",
                                _mSwAddin.MyTitle,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                Decors.MemoryForDecors[contr.Name] = _dictPathPict[contr];
                                SwAddin.SwitchToThisWindow(_mSwAddin._swHandle, false);
                            }
                            else
                            {
                                SwAddin.SwitchToThisWindow(_mSwAddin._swHandle, false);
                            }
                        var list = SetDecor(contr, fit);
                        //если есть св-во  ExtFanerFeats = Yes то записать в сво-ва цветов - цвета которые сделали для пласти
                        //в св-ва наличия - записать св-ва из БД
                        //если св-ва уже есть, то не переписывать их!

                        if (_swSelModel.GetCustomInfoValue("", "ExtFanerFeats") == "Yes")
                        {
                            if (minimumconf != 0 && contr.Tag as int? != null)
                            {
                                if (contr.Tag as int? == minimumconf)
                                {
                                    string colorName = Path.GetFileNameWithoutExtension(_dictPathPict[contr]);
                                    FrmEdge.SetDefault(oleDb, _swSelModel, colorName, _mSwAddin, _dictPathPict[contr]);//,_swSelComp);
                                }
                            }
                        }

                        if (list != null)
                        {
                            /*
                            foreach (var modelDoc2 in list)
                            {
                                var txt = modelDoc2.Extension.GetTexture("");
                                var rmArr = (object[]) modelDoc2.Extension.GetRenderMaterials();
                               if(txt != null && rmArr != null)
                               {
                                   var rm = (RenderMaterial) rmArr[0];
                                   if (rm.TextureFilename != txt.MaterialName)
                                   {
                                       _swSelModel.EditRebuild3();
                                       _swSelModel.Save();
                                   }
                                   //MessageBox.Show(modelDoc2.GetPathName() + Environment.NewLine + rm.TextureFilename + Environment.NewLine + txt.MaterialName);
                               }
                            }
                            
                            if ((from modelDoc2 in list
                                 let txt = modelDoc2.Extension.GetTexture("")
                                 let rmArr = (object[]) modelDoc2.Extension.GetRenderMaterials()
                                 where txt != null && rmArr != null
                                 let rm = (RenderMaterial) rmArr[0]
                                 where rm.TextureFilename != txt.MaterialName
                                 select txt).Any())
                            {
                                dict.Add(contr, true);
                            }
                             */
                            delList.Add(contr);
                        }
                    }
                }
                #endregion

                #region Удаление полей декоров из списка измененых полей
                foreach (var o in delList)
                {
                    if (_objListChanged.Contains(o))
                        _objListChanged.Remove(o);
                }
                #endregion

                foreach (var b in dict)
                {
                    if (b.Value)
                    {
                        MessageBox.Show(@"В поле с именем " + b.Key.Name + @" ошибка присвоения декора!");
                        tabMain.SelectTab(_tabDec);
                        b.Key.Focus();
                        return false;
                    }
                }
                _swSelModel.Save();  
            }
            catch { }
            finally
            {
                if (!this.InvokeRequired)
                    this.Enabled = true;
                else
                {
                    this.Invoke(new EventHandler(delegate
                    {

                        this.Enabled = true;
                    }));
                }
            }
            return true;
        }

        internal static int GetIdFromColumnName(string name)
        {
            string strRet = name.Substring(3, name.Length - 6);
            int ret = Convert.ToInt32(strRet);
            return ret;
        }

        private bool SavePartChanges(OleDbConnection oleDb, TextBox txtBox)
        {
            bool ret = true;
            string strTest = txtBox.Text;
            try
            {
                var cm = new OleDbCommand("SELECT " +
                                          _mSwAddin.CorrectDecimalSymbol(txtBox.Text, false, true) +
                                          " AS expr", oleDb);
                OleDbDataReader rd = cm.ExecuteReader();
                if (rd.Read())
                {
                    strTest = rd["expr"].ToString();
                    txtBox.Text = strTest;
                }
                rd.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
            }
            try
            {
                if (strTest.Contains(',') || strTest.Contains('.'))
                {
                    MessageBox.Show(@"Введите целое значение в поле " + txtBox.Name.Split('@').First() + @" !", _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation);
                    return false;
                }
                if (strTest.Length > 5)
                {
                    MessageBox.Show(@"Значение поля " + txtBox.Name.Split('@').First() + @" не должно превышать 5 символов!", _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation);
                    return false;
                }
                Convert.ToDouble(strTest);
            }
            catch
            {
                ret = false;
                MessageBox.Show(@"Введите числовое значение!", _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
            }
            return ret;
        }

        private void SetDecorPicture(ComboBox comb, bool setpicture = true, bool saveResult = false)
        {
            var pathFileDec = _dictionary[comb][comb.SelectedIndex];
            var number = (int)comb.Tag;
            AddDict(comb, pathFileDec);
            Component2 component = null;

            OleDbConnection oleDb, oleDbDecorDef;
            string colorProperty = "";
            if (!OpenOleDecors(out oleDbDecorDef))
                oleDbDecorDef = null;
            if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
            {
                var outComps = new LinkedList<Component2>();
                if (_mSwAddin.GetComponents(_swSelModel.IGetActiveConfiguration().IGetRootComponent2(),
                                            outComps, true, false))
                {
                    var cm = new OleDbCommand("SELECT * FROM decors WHERE Number = " + number, oleDb);
                    var rd = cm.ExecuteReader();

                    while (rd.Read())
                    {
                        component = (from component2 in outComps
                                     where !component2.IsSuppressed()
                                     let pathWithSuff = component2.GetPathName()
                                     let path =
                                         Path.GetFileNameWithoutExtension(
                                             _mSwAddin.GetModelNameWithoutSuffix(pathWithSuff))
                                     where path == (string)rd["Element"]
                                     select component2).FirstOrDefault();
                        colorProperty = rd["Color Property"].ToString();
                        if (component != null && colorProperty != "")
                            break;
                    }
                    rd.Close();
                    bool isColorChanged = false;
                    bool isNeedToShowMsg = false;
                    if (!setpicture)
                    {
                        string colorName = Path.GetFileNameWithoutExtension(pathFileDec);

                        isColorChanged = SetColorProperty(_swSelModel, colorProperty, oleDb, colorName, "Color Property", oleDbDecorDef);
                        if (isColorChanged && colorProperty.Length >= 6 && (colorProperty[5] == '1' || colorProperty[5] == '2' || colorProperty[5] == '3'))
                            isNeedToShowMsg = true;
                        rd = cm.ExecuteReader();
                        while (rd.Read())
                        {
                            foreach (var component2 in outComps)
                            {
                                if (!component2.IsSuppressed())
                                {
                                    if ((string)rd["Element"] ==
                                        Path.GetFileNameWithoutExtension(
                                            _mSwAddin.GetModelNameWithoutSuffix(
                                                component2.GetPathName())))
                                    {
                                        var mod = component2.IGetModelDoc();
                                        if (mod != null)
                                        {
                                            _mSwAddin.SetModelProperty(mod,
                                                                       (string)
                                                                       rd["Part Color Property"], "",
                                                                       swCustomInfoType_e.
                                                                           swCustomInfoText,
                                                                       colorName, true);
                                        }
                                    }
                                }
                            }
                        }
                        rd.Close();
                        if (saveResult && isColorChanged)
                        {
                            if (isNeedToShowMsg)
                                MessageBox.Show(@"В моделе " + Environment.NewLine + Path.GetFileName(_swSelModel.GetPathName()) + Environment.NewLine +
                                        @" свойство " + colorProperty + " не совпадает с декором нанесенным на модель." + Environment.NewLine + "Данная ошибка будет принудительно исправлена.", _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                            _swSelModel.Save();  
                        }
                    }
                }
                oleDb.Close();
            }
            if (setpicture)
            {
                Image image = Image.FromFile(pathFileDec);
                if (component != null && GetAngel(number, component))
                    image.RotateFlip(RotateFlipType.Rotate90FlipX);
                pictureBox1.Image = image;
                pictureBox1.Text = string.Empty;
                if (!OpenOleDecors(out oleDb))
                    pictureBox1.Text = string.Empty;
                else
                {

                    string[] restrictionValues = new string[3] { null, null, "decornames" };
                    DataTable schemaInformation = oleDb.GetSchema("Tables", restrictionValues);
                    if (schemaInformation.Rows.Count == 0)
                        pictureBox1.Text = string.Empty;
                    else
                    {
                        string selectStr = @"select * from decornames where FILEJPG = """ +
                                           Path.GetFileNameWithoutExtension(pathFileDec) + @"""";
                        var cm = new OleDbCommand(selectStr, oleDb);
                        var rd = cm.ExecuteReader();
                        while (rd.Read())
                        {
                            pictureBox1.Text = rd["DecorName"].ToString();
                            bool isDark = (bool)rd["IsDark"];
                            if (isDark)
                                pictureBox1.ForeColor = Color.Black;
                            else
                                pictureBox1.ForeColor = Color.White;

                        }
                        schemaInformation.Dispose();
                        rd.Close();
                    }

                    oleDb.Close();
                }
                //pictureBox1.Text = pathFileDec;

                // добавление комбобокса, который был изменен 
                if (!_objListChanged.Contains(comb))
                    _objListChanged.Add(comb);
            }
        }

        private void SetSizeForTab(int downPos)
        {
            tabMain.Size = new Size(tabMain.Size.Width, downPos + 40);
            btnOK.Location = new Point(btnOK.Location.X, tabMain.Location.Y + tabMain.Size.Height + 8);
            btnCancel.Location = new Point(btnCancel.Location.X, btnOK.Location.Y);
            Size = new Size(Size.Width, btnOK.Location.Y + btnOK.Size.Height + 40);
        }

        private IEnumerable<ModelDoc2> SetDecor(ComboBox comboBox, bool fit = false)
        {
            var retList = new List<ModelDoc2>();
            try
            {
                if (_dictPathPict[comboBox] == "") return null;
                var number = (int)comboBox.Tag;
                OleDbConnection oleDb;
                var componentList = new List<SetDecors>();
                var notUniqComponentList = new Dictionary<string, Component2>();
                bool done = false;
                string colorName = Path.GetFileNameWithoutExtension(_dictPathPict[comboBox]);
                if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                {
                    var outComps = new LinkedList<Component2>();
                    if (_mSwAddin.GetComponents(_swSelModel.IGetActiveConfiguration().IGetRootComponent2(),
                                                outComps, true, false))
                    {
                        var cm = new OleDbCommand("SELECT * FROM decors WHERE Number = " + number, oleDb);
                        var rd = cm.ExecuteReader();

                        while (rd.Read())
                        {
                            foreach (var component2 in outComps)
                            {
                                if (!component2.IsSuppressed())
                                {
                                    //if ((string)rd["Element"] ==Path.GetFileNameWithoutExtension(_mSwAddin.GetModelNameWithoutSuffix(component2.GetPathName())))
                                    if (Path.GetFileNameWithoutExtension(_mSwAddin.GetModelNameWithoutSuffix(component2.GetPathName())).Contains((string)rd["Element"]))
                                    {
                                        componentList.Add(new SetDecors(component2, (string)rd["Color Property"],
                                                                        (string)rd["Part Color Property"]));
                                        var mod = component2.IGetModelDoc();
                                        if (mod != null && mod.GetType() == (int)swDocumentTypes_e.swDocPART &&
                                            mod.get_CustomInfo2("", "Accessories") == "Yes")
                                        {
                                            if (!notUniqComponentList.ContainsKey(rd["Element"].ToString()))
                                            {
                                                notUniqComponentList.Add((string)rd["Element"], component2);
                                                done = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        rd.Close();
                    }
                    oleDb.Close();
                }
                if (done)
                {
                    #region Присвоение всем неуникальным деталям
                    //IEnumerable<ModelDoc2> listModels = Decors.ListMdb ?? Decors.GetAllModelsWithMdb(_mSwAddin, _swModel);
                    Decors.CheckListForDeletedModels(_mSwAddin);
                    List<ModelDoc2> listModels =
                        Decors.DictionaryListMdb[
                            _mSwAddin.GetXNameForAssembly(false,
                                                          Path.GetFileNameWithoutExtension(_swModel.GetPathName()))];
                    UserProgressBar pb;
                    _mSwAddin.SwApp.GetUserProgressBar(out pb);
                    int i = 0;
                    pb.Start(0, (notUniqComponentList.Keys.Count * listModels.Count),
                             "Проверка на наличие фурнитуры у деталей");
                    OleDbConnection oleDbDecorDef;
                    if (!OpenOleDecors(out oleDbDecorDef))
                        oleDbDecorDef = null;
                    foreach (var name in notUniqComponentList.Keys)
                    {
                        if (SetDecorsForEachDetail(notUniqComponentList[name], _dictPathPict[comboBox], number, fit))
                            foreach (var listModel in listModels)
                            {
                                var outComponents = new LinkedList<Component2>();
                                if (_mSwAddin.GetComponents(listModel.IGetActiveConfiguration().IGetRootComponent2(),
                                                            outComponents, true, false))
                                {
                                    if (outComponents.Select(x => Path.GetFileNameWithoutExtension(
                                        _mSwAddin.GetModelNameWithoutSuffix(x.GetPathName()))).Contains(name))
                                    {
                                        if (_mSwAddin.OpenModelDatabase(listModel, out oleDb))
                                        {
                                            var cm =
                                                new OleDbCommand("SELECT * FROM decors WHERE element = '" + name + "'",
                                                                 oleDb);
                                            var rd = cm.ExecuteReader();
                                            int strNumb = 0;
                                            if (rd.Read())
                                            {
                                                SetColorProperty(listModel, (string)rd["Color Property"], oleDb,
                                                                 colorName, "Color Property", oleDbDecorDef);
                                                SetColorProperty(notUniqComponentList[name].IGetModelDoc(),
                                                                 (string)rd["Part Color Property"], oleDb, colorName,
                                                                 "Part Color Property", oleDbDecorDef);
                                                strNumb = (int)rd["Number"];
                                                //listModel.Save();
                                                //notUniqComponentList[name].IGetModelDoc().Save();
                                                retList.Add(notUniqComponentList[name].IGetModelDoc());
                                            }
                                            rd.Close();
                                            if (strNumb != 0)
                                            {
                                                cm =
                                                    new OleDbCommand(
                                                        "SELECT * FROM decors WHERE Number = " + strNumb, oleDb);
                                                rd = cm.ExecuteReader();
                                                while (rd.Read())
                                                {
                                                    foreach (var component2 in outComponents)
                                                    {
                                                        if (!component2.IsSuppressed())
                                                        {
                                                            if ((string)rd["Element"] ==
                                                                Path.GetFileNameWithoutExtension(
                                                                    _mSwAddin.GetModelNameWithoutSuffix(
                                                                        component2.GetPathName())))
                                                            {
                                                                var mod = component2.IGetModelDoc();
                                                                if (mod != null)
                                                                {
                                                                    _mSwAddin.SetModelProperty(mod,
                                                                                               (string)
                                                                                               rd["Part Color Property"],
                                                                                               "",
                                                                                               swCustomInfoType_e.
                                                                                                   swCustomInfoText,
                                                                                               colorName);
                                                                    //mod.Save();
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                rd.Close();
                                            }

                                            oleDb.Close();
                                        }
                                    }
                                }
                                i++;
                                pb.UpdateProgress(i);
                            }
                    }
                    pb.End();
                    #endregion
                }
                else
                {
                    OleDbConnection oleDbDecorDef;
                    if (!OpenOleDecors(out oleDbDecorDef))
                        oleDbDecorDef = null;
                    foreach (var componentlist in componentList)
                    {
                        if (componentlist.Component != null)
                        {
                            if (Properties.Settings.Default.SetDecorsFromFirstElement && Decors.MemoryForDecors != null
                                && !Decors.MemoryForDecors.ContainsKey(comboBox.Name))
                                Decors.MemoryForDecors.Add(comboBox.Name, _dictPathPict[comboBox]);

                            SetDecorsForEachDetail(componentlist.Component, _dictPathPict[comboBox], number, fit);
                            SetColorProperty(_swSelModel, componentlist.ColorProperty, oleDb, colorName,
                                             "Color Property", oleDbDecorDef);
                            SetColorProperty(componentlist.Component.IGetModelDoc(), componentlist.PartColorProperty,
                                             oleDb, colorName, "Part Color Property", oleDbDecorDef);
                            retList.Add(componentlist.Component.IGetModelDoc());
                        }
                    }
                }
                _swModel.EditRebuild3();
            }
            catch { }
            return retList;
        }

        private bool SetColorProperty(ModelDoc2 inModel, string colorProp, OleDbConnection oleDb, string colorName,
            string column, OleDbConnection oleDbDecorDef)
        {
            bool isColorChanged = false;
            if (colorProp != "")
            {
                var names = (string[])inModel.GetCustomInfoNames2("");
                if (names.Contains(colorProp))
                {
                    isColorChanged = _mSwAddin.SetModelProperty(inModel, colorProp, "",
                                                                swCustomInfoType_e.swCustomInfoText,
                                                                colorName, true);
                    if (oleDbDecorDef != null)
                    {
                        string fullDecorName = string.Empty;
                        string[] restrictionValues = new string[3] { null, null, "decornames" };
                        DataTable schemaInformation = oleDbDecorDef.GetSchema("Tables", restrictionValues);
                        if (schemaInformation.Rows.Count == 0)
                            fullDecorName = string.Empty;
                        else
                        {
                            string selectStr = @"select * from decornames where FILEJPG = """ +
                                               colorName + @"""";
                            var cmDecorDef = new OleDbCommand(selectStr, oleDbDecorDef);
                            var rdDecorDef = cmDecorDef.ExecuteReader();
                            while (rdDecorDef.Read())
                            {
                                fullDecorName = rdDecorDef["DecorName"].ToString();
                            }
                            schemaInformation.Dispose();
                            cmDecorDef.Dispose();
                            rdDecorDef.Close();
                        }
                        int index;
                        if (int.TryParse(colorProp.Substring(colorProp.Length - 1, 1), out index))
                            _mSwAddin.SetModelProperty(inModel, "ColorName" + index.ToString(), "",
                                                       swCustomInfoType_e.swCustomInfoText,
                                                       fullDecorName, true);
                    }

                }
            }
            else
            {
                if (column == "Part Color Priority")
                    MessageBox.Show(@"В файле " + Environment.NewLine + oleDb.DataSource + Environment.NewLine +
                                @" в столбце 'Part Color Priority', Element: " +
                                Path.GetFileNameWithoutExtension(
                                    _mSwAddin.GetModelNameWithoutSuffix(inModel.GetPathName()))
                                + @" ошибка!", _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                else
                    MessageBox.Show(@"В файле " + Environment.NewLine + oleDb.DataSource + Environment.NewLine +
                                @" в столбце '" + column + @"' ошибка!", _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            return isColorChanged;
        }

        private bool SetDecorsForEachDetail(Component2 component, string pathPictName, int number, bool fit = false)
        {
            if (component.IGetModelDoc().GetType() == (int)swDocumentTypes_e.swDocPART)
            {
                double angel = GetAngel(number, component) ? 0 : 90;

                var m = component.IGetModelDoc();
                var rmArr = (object[])m.Extension.GetRenderMaterials();
                RenderMaterial rm = null;
                object swEnt;
                string fileName = pathPictName.Substring(0, pathPictName.Length - 4) + ".p2m";
                if (rmArr == null)
                {
                    component.Select(false);
                    swEnt = (Entity)_swModel.ISelectionManager.GetSelectedObject6(1, -1);
                    _swModel.ClearSelection();
                    //rm = m.Extension.CreateRenderMaterial(fileName);
                }
                else
                {
                    if (_swSelModel.GetCustomInfoValue("", "ExtFanerFeats") == "Yes")
                        swEnt = m;
                    else
                    {
                        rm = (RenderMaterial)rmArr[0];
                        var ent = (object[])rm.GetEntities();
                        swEnt = ent[0];
                    }
                }
                rm = m.Extension.CreateRenderMaterial(fileName);
                rm.AddEntity(swEnt);
                rm.RotationAngle = angel;
                rm.TextureFilename = pathPictName;
                rm.FileName = fileName;
                rm.FixedAspectRatio = false;
                rm.FitWidth = false;
                rm.FitHeight = false;
                rm.Width = 0.3;
                rm.Height = 0.3;
                if (fit)
                {
                    double width = 1;
                    double height = 1;
                    object b = component.GetBox(true, true);
                    if (b != null)
                    {
                        var boxs = (double[])b;
                        double xs1 = boxs[0];
                        double ys1 = boxs[1];
                        double zs1 = boxs[2];
                        double xs2 = boxs[3];
                        double ys2 = boxs[4];
                        double zs2 = boxs[5];
                        double x = Math.Abs(xs1 - xs2);
                        double z = Math.Abs(zs1 - zs2);
                        double y = Math.Abs(ys1 - ys2);
                        if (y < x && y < z)
                        {
                            width = z;
                            height = x;
                        }
                        else if (x < z && x < y)
                        {
                            width = y;
                            height = z;
                        }
                        else if (z < x && z < y)
                        {
                            width = y;
                            height = x;
                        }
                    }
                    rm.Width = width;
                    rm.Height = height;
                    rm.FitWidth = true;
                    rm.FitHeight = true;
                }
                var swConfig = (Configuration)m.GetConfigurationByName(component.ReferencedConfiguration);
                object displayStateNames = swConfig.GetDisplayStates();
                int e1, e2;
                int f;
                //var texture = m.Extension.CreateTexture(pathPictName, 1, 0, false);
                //component.RemoveTexture(string.Empty);
                //component.SetTexture(string.Empty, texture);
                m.Extension.AddRenderMaterial(rm, out f);
                m.Extension.AddDisplayStateSpecificRenderMaterial(rm, (int)swDisplayStateOpts_e.swSpecifyDisplayState,
                                                                  displayStateNames, out e1, out e2);
                if (fit)
                    m.Save2(true);
                return true;
            }
            return false;
        }

        private bool GetAngel(int number, Component2 component2)
        {
            bool vertical = false;
            OleDbConnection oleDb;
            if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
            {
                OleDbCommand cm = component2.IGetModelDoc().GetConfigurationCount() > 1
                                      ? new OleDbCommand(
                                            "SELECT * FROM decors_conf WHERE id = " + number + " AND Configuration = '" +
                                            component2.ReferencedConfiguration + "'", oleDb)
                                      : new OleDbCommand("SELECT * FROM decors_conf WHERE id = " + number, oleDb);
                var rd = cm.ExecuteReader();
                if (rd.Read())
                {
                    vertical = (bool)rd["Texture direction"];
                }
                rd.Close();
                oleDb.Close();
            }
            return vertical;
        }

        private void AddDict(ComboBox comb, string path)
        {
            if (!_dictPathPict.ContainsKey(comb))
                _dictPathPict.Add(comb, path);
            else
                _dictPathPict[comb] = path;
        }

        private void SetValueForTxtBx(TextBox textBox)
        {
            string strVal = textBox.Text;
            int val = Convert.ToInt32(strVal);
            if (val != -1)
            {
                _mSwAddin.SetObjectValue(_swSelModel, (string)textBox.Tag, 14, val);
                textBox.Name = textBox.Name.Split('@').First() + "@" + val;
            }
        }

        private string GetTextureFileFromRenderMaterial(ModelDoc2 model, bool remove = false)
        {
            string ret = "";
            var rmArr = (object[])model.Extension.GetRenderMaterials();
            string textureFn;
            RenderMaterial rm = null;
            if (rmArr == null)
            {
                var texture = model.Extension.GetTexture("");
                textureFn = texture != null ? texture.MaterialName : "";
            }
            else
            {
                rm = (RenderMaterial)rmArr[0];
                textureFn = rm.TextureFilename;
                if (textureFn.Contains("none.jpg"))
                    textureFn = string.Empty;
                var texture = model.Extension.GetTexture("");
                if (texture != null && File.Exists(texture.MaterialName))
                    textureFn = texture.MaterialName;
            }

            var b = Directory.GetFiles(Furniture.Helpers.LocalAccounts.decorPathResult)
                    .Any(x => Path.GetFileName(x) == Path.GetFileName(textureFn));
            if (b)
            {
                ret = textureFn;
                if (remove && rmArr != null)
                {
                    rm.TextureFilename = "";
                }
            }
            return ret;
        }

        private void RecalcSlaveIdComponents(ComboBox comBox)
        {
            OleDbConnection oleDb;

            if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
            {
                try
                {
                    if (_comboBoxListForRedraw.ContainsKey(comBox) && _comboBoxListForRedraw[comBox] != null)
                    {
                        foreach (var i in _comboBoxListForRedraw[comBox])
                        {
                            if (_numbAndTextBoxes.ContainsKey(i))
                            {
                                var cm = new OleDbCommand("SELECT * FROM objects WHERE id =" + i,
                                                          oleDb);
                                OleDbDataReader rd = cm.ExecuteReader();
                                if (rd.Read())
                                {
                                    string strObjName = rd["name"].ToString();
                                    double strObjVal;

                                    if (_mSwAddin.GetObjectValue(_swSelModel, strObjName, (int)rd["type"],
                                                                 out strObjVal))
                                    {
                                        var val = GetCorrectIntValue(strObjVal);
                                        _numbAndTextBoxes[i].Text = val.ToString();
                                    }
                                    rd.Close();
                                }

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    oleDb.Close();
                }

                IsNewSelectionDisabled = true;
                if (_mSwAddin.RecalculateModel(_swSelModel))
                    _swModel.EditRebuild3();
                IsNewSelectionDisabled = false;
            }
        }

        #region Event Handlers

        private void ComboBoxWithDiscretValuesSelectedIndexChanged(object sender, EventArgs e)
        {
            var cb = (ComboBox)sender;
            var val = (int)cb.SelectedItem;
            if (val != -1)
            {
                _mSwAddin.SetObjectValue(_swSelModel, (string)cb.Tag, 14, val);
            }
            _mSwAddin.RecalculateModel(_swSelModel);
            _swModel.EditRebuild3();
            RecalcSlaveIdComponents(cb);
        }
        private void ConfigurationChanged(object sender, EventArgs e)
        {

            var comBox = (ComboBox)sender;
            var nameConf = (string)comBox.SelectedItem;
            _swSelComp.ReferencedConfiguration = nameConf;
            var swModel = _mSwAddin.SwApp.IActiveDoc2;
            _swSelComp.Select(false);
            ((AssemblyDoc)swModel).CompConfigProperties4(2, 0, true, true, nameConf, false);

            ReloadAllSetParameters(_swSelComp);



            _mSwAddin.SwModel = (ModelDoc2)_mSwAddin.SwApp.ActiveDoc;
            OleDbConnection oleDb;

            if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
            {
                using (oleDb)
                {
                    var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                    
                    List<object> removeList = new List<object>();
                    foreach (var objUnit in _commonList)
                    {
                        if (objUnit.GetType() != typeof(TextBox))
                        {
                            var combBox = (ComboBox)objUnit;
                            if (combBox.Tag is int)
                                removeList.Add(objUnit);
                        }
                    }
                    foreach (var o in removeList)
                    {
                        _commonList.Remove(o);
                    }
                    ReloadDecorTab(oleDb, oleSchem);
                }
            }

        }
        private void ComboBoxConfForDimTabSelectedIndexChanged(object sender, EventArgs e)
        {
            var comBox = (ComboBox)sender;
            var component = (Component2)comBox.Tag;
            var nameConf = (string)comBox.SelectedItem;

            _swModel.ClearSelection2(true);

            SwAddin.IsEventsEnabled = false;
            ShowConfigurationDetails(component, _swSelModel, nameConf);
            RecalcSlaveIdComponents(comBox);
            OleDbConnection oleDb;
            if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
            {
                using (oleDb)
                {
                    List<object> objectsToDelete = new List<object>();
                    foreach (var objUnit in _commonList)
                    {
                        if (objUnit.GetType() == typeof(ComboBox))
                        {
                            objectsToDelete.Add(objUnit);
                        }
                    }
                    foreach (var o in objectsToDelete)
                    {
                        _commonList.Remove(o);
                    }
                    var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                    ReloadDecorTab(oleDb, oleSchem);
                }
            }
            SwAddin.IsEventsEnabled = true;
        }

        private void ShowConfigurationDetails(Component2 component, ModelDoc2 model, string nameConfiguration)
        {
            if (SwAddin.needWait)
            {
                ProgressBar.WaitTime.Instance.ShowWait();
                ProgressBar.WaitTime.Instance.SetLabel("Ожидание завершения предедущих операций.");
                lock (SwAddin.workerLocker)
                    Monitor.Wait(SwAddin.workerLocker);
                ProgressBar.WaitTime.Instance.HideWait();
            }
            if (component.Select(false) && ((AssemblyDoc)model).CompConfigProperties4(component.GetSuppression(), 0, true, true, nameConfiguration, false))
            {
                int err = 0, wrn = 0;
                var mod = _mSwAddin.SwApp.OpenDoc6(component.IGetModelDoc().GetPathName(),
                                                   component.IGetModelDoc().GetType(), 0, nameConfiguration, ref err, ref wrn);
                var dict = new Dictionary<Component2, ModelDoc2>();
                if (mod != null)
                {
                    mod.ShowConfiguration2(nameConfiguration);
                    if (mod.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
                    {
                        var outList = new LinkedList<Component2>();
                        if (_mSwAddin.GetComponents(mod.IGetActiveConfiguration().IGetRootComponent2(), outList, true,
                                                    false))
                        {
                            foreach (var component2 in outList)
                            {
                                if (mod.GetEquationMgr().GetCount() > 0)
                                    for (int i = 0; i < mod.GetEquationMgr().GetCount(); i++)
                                    {
                                        if (mod.GetEquationMgr().get_Equation(i).Contains(
                                            Path.GetFileNameWithoutExtension(component2.GetPathName())))
                                        {
                                            if (mod.GetEquationMgr().get_Suppression(i) != component2.IsSuppressed())
                                                mod.GetEquationMgr().set_Suppression(i, component2.IsSuppressed());
                                        }
                                    }
                            }
                            foreach (var component2 in outList)
                            {
                                var m = component2.IGetModelDoc();
                                if (m != null && m.GetConfigurationCount() > 1)
                                {
                                    if (dict.ContainsKey(component2))
                                        dict.Add(component2, component.IGetModelDoc());
                                }
                            }
                        }
                    }
                    mod.Save();  
                    _mSwAddin.SwApp.CloseDoc(mod.GetPathName());
                }
                foreach (var d in dict)
                    ShowConfigurationDetails(d.Key, d.Value, d.Key.ReferencedConfiguration);
            }
        }
        private void BtnFitDecorClick(object sender, EventArgs e)
        {
            _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
            var btn = (Button)sender;
            var comb = (ComboBox)_tabDec.Controls[(string)btn.Tag];
            if (!_objListChanged.Contains(comb))
                _objListChanged.Add(comb);
            SaveChanges(true, true);
        }

        private void BtnAllModClick(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var comBoxDecors = (ComboBox)_tabDec.Controls[(string)btn.Tag];
            if (!_dictPathPict.ContainsKey(comBoxDecors) || _dictPathPict[comBoxDecors] == "")
            {
                MessageBox.Show(@"Выберите декор!", _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                comBoxDecors.Focus();
                return;
            }

            bool extFanerFeats = _swSelModel.GetCustomInfoValue("", "ExtFanerFeats") == "Yes";


            string nameColor = Path.GetFileNameWithoutExtension(_dictPathPict[comBoxDecors]);
            OleDbConnection oleDb;
            try
            {
                if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                {
                    using (oleDb)
                    {
                        var cm = new OleDbCommand(
                            "SELECT * FROM decors_conf WHERE id = " + btn.Name + " AND Visible = true",
                            oleDb);
                        var rd = cm.ExecuteReader();
                        bool someConfiguration = false;
                        if (rd.Read())
                        {
                            if (rd["Configuration"].ToString().Trim() != "" && rd["Configuration"].ToString().Trim() != "all")
                            {
                                someConfiguration = true;
                            }
                        }
                        rd.Close();

                        var outComponents = new LinkedList<Component2>();
                        if (someConfiguration &&
                            _mSwAddin.GetComponents(_swSelModel.IGetActiveConfiguration().IGetRootComponent2(),
                            outComponents, true, false))
                        {
                            string nConf = "";
                            cm = new OleDbCommand("SELECT * FROM decors WHERE Number = " + btn.Name, oleDb);
                            rd = cm.ExecuteReader();
                            bool exit = false;
                            while (rd.Read())
                            {
                                foreach (var outComponent in outComponents)
                                {
                                    if (rd["Element"].ToString() ==
                                        Path.GetFileNameWithoutExtension(
                                            _mSwAddin.GetModelNameWithoutSuffix(outComponent.GetPathName())))
                                    {
                                        var md = outComponent.IGetModelDoc();
                                        if (md != null && md.GetType() == (int)swDocumentTypes_e.swDocPART)
                                        {
                                            nConf = outComponent.ReferencedConfiguration;
                                            exit = true;
                                            break;
                                        }
                                    }
                                }
                                if (exit)
                                    break;
                            }
                            rd.Close();
                            if (nConf != "")
                            {
                                cm =
                                    new OleDbCommand(
                                        "SELECT * FROM decors_conf WHERE id = " + btn.Name +
                                        " AND Visible = true AND Configuration = '" + nConf + "'", oleDb);
                            }
                        }
                        else
                            cm = new OleDbCommand(
                            "SELECT * FROM decors_conf WHERE id = " + btn.Name + " AND Visible = true",
                            oleDb);

                        rd = cm.ExecuteReader();
                        string group = "";
                        if (rd.Read())
                            group = (string)rd["Group"];
                        rd.Close();
                        oleDb.Close();
                        if (group != "")
                        {
                            string[] outVal;

                            string comGroup = "";
                            if (GetDecorsPathArray(group, out outVal))
                            {
                                if (OpenOleDecors(out oleDb))
                                {
                                    using (oleDb)
                                    {
                                        bool done = false;
                                        foreach (var s in outVal)
                                        {
                                            cm = new OleDbCommand("SELECT * FROM decordef WHERE " + s + "=true", oleDb);
                                            rd = cm.ExecuteReader();
                                            while (rd.Read())
                                            {
                                                if ((string)rd["FILEJPG"] == nameColor)
                                                {
                                                    comGroup = s;
                                                    done = true;
                                                    break;
                                                }
                                            }
                                            rd.Close();
                                            if (done)
                                                break;
                                        }
                                        oleDb.Close();
                                    }
                                }
                            }
                            else
                            {
                                comGroup = group;
                            }
                            Decors.CheckListForDeletedModels(_mSwAddin);

                            List<ModelDoc2> listMod =
                                Decors.DictionaryListMdb[
                                    _mSwAddin.GetXNameForAssembly(false,
                                                                  Path.GetFileNameWithoutExtension(
                                                                      _swModel.GetPathName()))];
                            UserProgressBar pb;
                            _mSwAddin.SwApp.GetUserProgressBar(out pb);
                            bool paint = false;

                            var outComps = new LinkedList<Component2>();
                            pb.Start(0, listMod.Count(), "Присвоение на каждую модель");
                            int i = 0;
                            foreach (var modelDoc2 in listMod)
                            {
                                if (modelDoc2 == _swSelModel) continue;
                                if (_mSwAddin.OpenModelDatabase(modelDoc2, out oleDb))
                                {
                                    cm =
                                        new OleDbCommand(
                                            "SELECT * FROM decors_conf WHERE id = " + btn.Name + " AND Visible = true",
                                            oleDb);
                                    rd = cm.ExecuteReader();
                                    bool someConfig = false;
                                    if (rd.Read())
                                    {
                                        if (rd["Configuration"].ToString().Trim() != "" &&
                                            rd["Configuration"].ToString().Trim() != "all")
                                        {
                                            someConfig = true;
                                        }
                                        else if (rd["Group"].ToString().Contains(comGroup))
                                        {
                                            paint = true;
                                        }
                                    }
                                    rd.Close();

                                    var component2S = new LinkedList<Component2>();
                                    if (someConfig &&
                                        _mSwAddin.GetComponents(
                                            modelDoc2.IGetActiveConfiguration().IGetRootComponent2(),
                                            component2S, true, false))
                                    {
                                        string nameConf = "";
                                        cm = new OleDbCommand("SELECT * FROM decors WHERE Number = " + btn.Name, oleDb);
                                        rd = cm.ExecuteReader();
                                        bool exit = false;
                                        while (rd.Read())
                                        {
                                            foreach (var component2 in component2S)
                                            {
                                                if ((string)rd["Element"] ==
                                                    Path.GetFileNameWithoutExtension(
                                                        _mSwAddin.GetModelNameWithoutSuffix(component2.GetPathName())))
                                                {
                                                    var modl = component2.IGetModelDoc();
                                                    if (modl != null &&
                                                        modl.GetType() == (int)swDocumentTypes_e.swDocPART)
                                                    {
                                                        nameConf = component2.ReferencedConfiguration;
                                                        exit = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (exit)
                                                break;
                                        }
                                        rd.Close();
                                        if (nameConf != "")
                                        {
                                            cm =
                                                new OleDbCommand(
                                                    "SELECT * FROM decors_conf WHERE id = " + btn.Name +
                                                    " AND Visible = true AND Configuration = '" + nameConf + "'",
                                                    oleDb);
                                            rd = cm.ExecuteReader();
                                            if (rd.Read())
                                            {
                                                if (rd["Group"].ToString().Contains(comGroup))
                                                {
                                                    paint = true;
                                                }
                                            }
                                            rd.Close();
                                        }
                                    }

                                    if (paint)
                                    {
                                        var componentList = new List<SetDecors>();
                                        if (_mSwAddin.GetComponents(
                                            modelDoc2.IGetActiveConfiguration().IGetRootComponent2(), outComps, true,
                                            false))
                                        {
                                            cm = new OleDbCommand("SELECT * FROM decors WHERE Number = " + btn.Name,
                                                                  oleDb);
                                            rd = cm.ExecuteReader();

                                            while (rd.Read())
                                            {
                                                if (!modelDoc2.GetPathName().Contains("_SWLIB_BACKUP"))
                                                {
                                                    componentList.AddRange(from component2 in outComps
                                                                           where !component2.IsSuppressed()
                                                                           where
                                                                               (string)rd["Element"] ==
                                                                               Path.GetFileNameWithoutExtension(
                                                                                   _mSwAddin.GetModelNameWithoutSuffix(
                                                                                       component2.GetPathName()))
                                                                           select
                                                                               new SetDecors(component2,
                                                                                             (string)
                                                                                             rd["Color Property"],
                                                                                             (string)
                                                                                             rd["Part Color Property"]));
                                                }
                                                else
                                                {
                                                    componentList.AddRange(from component2 in outComps
                                                                           where !component2.IsSuppressed()
                                                                           where
                                                                               (string)rd["Element"] == Path.GetFileNameWithoutExtension(_mSwAddin.GetModelNameWithoutSuffix(component2.GetPathName())).Substring(0, Path.GetFileNameWithoutExtension(_mSwAddin.GetModelNameWithoutSuffix(component2.GetPathName())).Length - 4)
                                                                           select
                                                                               new SetDecors(component2,
                                                                                             (string)
                                                                                             rd["Color Property"],
                                                                                             (string)
                                                                                             rd["Part Color Property"]));
                                                }
                                            }
                                            rd.Close();
                                            OleDbConnection oleDbDecorDef;
                                            if (!OpenOleDecors(out oleDbDecorDef))
                                                oleDbDecorDef = null;
                                            foreach (var componentlist in componentList)
                                            {
                                                if (componentlist.Component != null)
                                                {
                                                    SetDecorsForEachDetail(componentlist.Component,
                                                                           _dictPathPict[comBoxDecors],
                                                                           Convert.ToInt32(btn.Name));
                                                    SetColorProperty(modelDoc2, componentlist.ColorProperty, oleDb,
                                                                     nameColor,
                                                                     "Color Property", oleDbDecorDef);
                                                    SetColorProperty(componentlist.Component.IGetModelDoc(),
                                                                     componentlist.PartColorProperty,
                                                                     oleDb, nameColor, "Part Color Property", oleDbDecorDef);
                                                    if (extFanerFeats)
                                                    {
                                                        string colorName = Path.GetFileNameWithoutExtension(_dictPathPict[comBoxDecors]);
                                                        FrmEdge.SetDefault(oleDb, modelDoc2, colorName, _mSwAddin, _dictPathPict[comBoxDecors]);//, modelDoc2.ConfigurationManager.ActiveConfiguration.GetRootComponent3(true));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    oleDb.Close();
                                }
                                ++i;
                                pb.UpdateProgress(i);
                                paint = false;
                            }
                            pb.End();
                            _swModel.EditRebuild3();
                        }
                    }
                }
            }
            catch { }
        }

        private void TabMainTabSelected(object sender, TabControlEventArgs e)
        {
            if (IsNewSelectionDisabled) return;
            IsNewSelectionDisabled = true;
            if (_isMate)
                _isMate = false;
            _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
            IsNewSelectionDisabled = false;
        }

        private void ComboBoxDecorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                SelectNextControl((Control)sender, true, true, true, true);
            }
            if (e.KeyCode == Keys.Enter)
            {
                SaveChanges(false, false);
            }
        }

        private void ComboBoxDecorMouseCaptureChanged(object sender, EventArgs e)
        {
            var comb = (ComboBox)sender;
            if (comb.SelectedIndex == -1) return;
            SetDecorPicture(comb);
        }

        private void ComboBoxDecorSelectedIndexChanged(object sender, EventArgs e)
        {
            var comb = (ComboBox)sender;
            if (_setNewListForComboBox)
            {
                SetDecorPicture(comb);
                comb.MouseCaptureChanged += ComboBoxDecorMouseCaptureChanged;
            }
        }

        private void LinkLabel1Click(object sender, EventArgs e)
        {
            var label = (Label)sender;
            _mSwAddin.SwApp.OpenDoc(label.Name, (int)swDocumentTypes_e.swDocDRAWING);
            int errors = 0;
            var tmp = (ModelDoc2)_mSwAddin.SwApp.ActivateDoc2(label.Name, true, ref errors);
            var myModelView = (IModelView)tmp.GetFirstModelView();
            myModelView.FrameState = (int)swWindowState_e.swWindowMaximized;
        }

        private void StartMeasure(object sender, EventArgs e)
        {
            foreach (Button btnMeasure in _btnPrm)
            {
                btnMeasure.Enabled = _chkMeasure.Checked;
            }
        }


        private double GetMaxZ(double[] sBox, string axe)
        {
            switch (axe.ToLower())
            {
                case "z":
                    if (sBox[2] > sBox[5])
                        return sBox[2];
                    else
                        return sBox[5];

                case "x":
                    if (sBox[0] > sBox[3])
                        return sBox[0];
                    else
                        return sBox[3];

                case "y":
                    if (sBox[1] > sBox[4])
                        return sBox[1];
                    else
                        return sBox[4];
                default:
                    throw new Exception("В таблице неверно указана ось: " + axe);
            }
        }
        private double GetMinZ(double[] sBox, string axe)
        {
            switch (axe.ToLower())
            {
                case "z":
                    if (sBox[2] < sBox[5])
                        return sBox[2];
                    else
                        return sBox[5];
                case "x":
                    if (sBox[0] < sBox[3])
                        return sBox[0];
                    else
                        return sBox[3];

                case "y":
                    if (sBox[1] < sBox[4])
                        return sBox[1];
                    else
                        return sBox[4];
                default:
                    throw new Exception("В таблице неверно указана ось: " + axe);
            }
        }
        private double GetLengthByZR(double origZ, double destZ)
        {
            if (destZ > origZ)
                return double.MaxValue;
            return origZ - destZ;

        }
        private double GetLengthByZL(double origZ, double destZ)
        {
            if (destZ < origZ)
                return double.MaxValue;
            return destZ - origZ;

        }

        private Component2 CheckAllMates(ModelDoc2 model, Component2 originalComp, string[] linkedModels, double[] sBox, string axe, out bool isLeft)
        {
            int axeInt = 0;
            switch (axe.ToLower())
            {
                case "x":
                    axeInt = 3;
                    break;
                case "y":
                    axeInt = 4;
                    break;
                case "z":
                    axeInt = 5;
                    break;
            }
            if (axeInt == 0)
                throw new Exception("Не удалось растянуть деталь. В mdb ref_objects_axe не правильно указана ось (axe).");
            isLeft = false;
            //model.ClosestDistance(origPlane, nearestPlane, out point1, out point2);
            var swFeat = (Feature)model.FirstFeature();
            while (swFeat != null)
            {

                if (swFeat.GetTypeName2() == "MateGroup")
                {
                    var mate = swFeat.GetFirstSubFeature();
                    while (mate != null)
                    {
                        if (mate.GetTypeName() == "MateCoincident")
                        {

                            Mate2 spec = mate.GetSpecificFeature2();

                            if (spec.GetMateEntityCount() > 1)
                            {
                                var paramsArray = spec.MateEntity(0).EntityParams as double[];
                                double tst = 0;
                                if (paramsArray != null && paramsArray.Length > axeInt)
                                    tst = paramsArray[axeInt];
                                if (spec.MateEntity(0).ReferenceComponent.Name.Contains(originalComp.Name) && (tst == 1 || tst == -1))
                                {
                                    foreach (string modelName in linkedModels)
                                    {
                                        if (spec.MateEntity(1).ReferenceComponent.Name.Contains(modelName))
                                        {
                                            double avarageZ = (sBox[axeInt - 3] + sBox[axeInt]) / 2;
                                            double[] mateBox = spec.MateEntity(1).ReferenceComponent.GetBox(false, false);
                                            double mateAvarageZ = (mateBox[axeInt - 3] + mateBox[axeInt]) / 2;
                                            if (avarageZ > mateAvarageZ)
                                                isLeft = false;
                                            else
                                                isLeft = true;
                                            return spec.MateEntity(1).ReferenceComponent;
                                        }
                                    }
                                }
                                paramsArray = spec.MateEntity(1).EntityParams as double[];
                                tst = 0;
                                if (paramsArray != null && paramsArray.Length > axeInt)
                                    tst = paramsArray[axeInt];
                                if (spec.MateEntity(1).ReferenceComponent.Name.Contains(originalComp.Name) && (tst == 1 || tst == -1))
                                {
                                    foreach (string modelName in linkedModels)
                                    {
                                        if (spec.MateEntity(0).ReferenceComponent.Name.Contains(modelName))
                                        {
                                            double avarageZ = (sBox[axeInt - 3] + sBox[axeInt]) / 2;
                                            double[] mateBox = spec.MateEntity(0).ReferenceComponent.GetBox(false, false);
                                            double mateAvarageZ = (mateBox[axeInt - 3] + mateBox[axeInt]) / 2;
                                            if (avarageZ > mateAvarageZ)
                                                isLeft = false;
                                            else
                                                isLeft = true;
                                            return spec.MateEntity(0).ReferenceComponent;
                                        }
                                    }
                                }
                            }
                        }
                        mate = mate.GetNextSubFeature();
                    }
                }
                swFeat = (Feature)swFeat.GetNextFeature();
            }
            return null;
        }

        private void ExpandBtn(object sender, EventArgs e)
        {
            try
            {
                if (!this.InvokeRequired)
                    this.Enabled = false;
                else
                {
                    this.Invoke(new EventHandler(delegate
                    {
                        this.Enabled = false;
                    }));
                }
                SwAsmDoc.NewSelectionNotify -= NewSelection;
                Button btn = (Button)sender;
                //_mSwAddin.GetAllUniqueModels(_mSwAddin.RootModel, out allUniqModels);
                var btnDist = (Button)sender;
                TextBox txtDist = _butPlusTxt[btnDist];
                int originalLength;
                if (!int.TryParse(txtDist.Text, out originalLength))
                    return;
                ref_object[] refobj = (ref_object[])btn.Tag;

                string[] linkedModels = refobj.Select(d => d.ComponentName).ToArray();//new string[2] {"Стенка каркасная", "Панель каркасная"};
                string axe = refobj.First().Axe;
                double correctionValueLeft = refobj.First().CorrectionValueLeft;
                double correctionValueRight = refobj.First().CorrectionValueRight;
                KeyValuePair<double, Component2> nearestLeft = new KeyValuePair<double, Component2>(double.MaxValue,
                                                                                                    null);
                KeyValuePair<double, Component2> nearestRight = new KeyValuePair<double, Component2>(double.MaxValue,
                                                                                                     null);
                string path = string.Empty;
                var sBox = (double[])_swSelComp.GetBox(false, false);
                bool isLeft;
                Component2 mateComponent = CheckAllMates(_mSwAddin.RootModel, _swSelComp, linkedModels, sBox, axe,
                                                         out isLeft);
                if (mateComponent != null)
                {
                    if (isLeft)
                        nearestLeft = new KeyValuePair<double, Component2>(0, mateComponent);
                    else
                        nearestRight = new KeyValuePair<double, Component2>(0, mateComponent);
                }

                double originalMaxZ = GetMaxZ(sBox, axe);
                double originalMinZ = GetMinZ(sBox, axe);

                var swConfig = (Configuration)_mSwAddin.RootModel.GetActiveConfiguration();
                Component2 swRootComponent = null;
                if (swConfig != null)
                {
                    swRootComponent = (Component2)swConfig.GetRootComponent();
                }
                if (swRootComponent == null)
                    return;
                var swComponents = new LinkedList<Component2>();
                double leftDim = double.MaxValue, rightDim = double.MaxValue;
                if (nearestLeft.Key < leftDim)
                    leftDim = nearestLeft.Key;
                if (nearestRight.Key < rightDim)
                    rightDim = nearestRight.Key;
                double tmp = 0;

                if (_mSwAddin.GetComponents(swRootComponent, swComponents, false, false))
                {
                    foreach (var component in swComponents)
                    {
                        foreach (string lnkStr in linkedModels)
                        {
                            path = component.Name;
                            if (path.Contains(lnkStr)) // && path.Substring(path.Length - 6, 6).ToLower() == "sldasm")
                            {
                                //Считаем расстояние от "левой" стенки
                                tmp = GetLengthByZL(originalMaxZ, GetMinZ(component.GetBox(false, false), axe));
                                if (tmp < leftDim)
                                {
                                    leftDim = tmp;
                                    nearestLeft = new KeyValuePair<double, Component2>(leftDim, component);
                                }

                                //Считаем расстояние от правой стенки
                                tmp = GetLengthByZR(originalMinZ, GetMaxZ(component.GetBox(false, false), axe));
                                if (tmp < rightDim)
                                {
                                    rightDim = tmp;
                                    nearestRight = new KeyValuePair<double, Component2>(rightDim, component);
                                }

                            }
                        }
                    }
                    //привязать к ближайшему..
                    //сначала определим ближайшее

                    if (nearestLeft.Value == null || nearestRight.Value == null)
                        throw new Exception("Не определены размеры проема.");

                    if (mateComponent == null) // если нет сопряжения
                    {

                        KeyValuePair<double, Component2> nearestNearest = new KeyValuePair<double, Component2>();
                        Feature origPlane = null, nearestPlane = null;
                        string origPlaneName = null, nearestPlaneName = null;
                        if (nearestLeft.Key < nearestRight.Key)
                        {
                            nearestNearest = nearestLeft;
                            switch (axe.ToLower())
                            {
                                case "z":
                                    origPlaneName = "#swrfЛевая";
                                    nearestPlaneName = "#swrfПравая";
                                    break;
                                case "y":
                                    origPlaneName = "#swrfВерхняя";
                                    nearestPlaneName = "#swrfНижняя";
                                    break;
                                case "x":
                                    origPlaneName = "#swrfПередняя";
                                    nearestPlaneName = "#swrfЗадняя";
                                    break;
                            }
                        }
                        else
                        {
                            nearestNearest = nearestRight;
                            switch (axe.ToLower())
                            {
                                case "z":
                                    origPlaneName = "#swrfПравая";
                                    nearestPlaneName = "#swrfЛевая";
                                    break;
                                case "y":
                                    origPlaneName = "#swrfНижняя";
                                    nearestPlaneName = "#swrfВерхняя";
                                    break;
                                case "x":
                                    origPlaneName = "#swrfЗадняя";
                                    nearestPlaneName = "#swrfПередняя";
                                    break;
                            }
                        }
                        origPlane = _swSelComp.FeatureByName(origPlaneName);
                        nearestPlane = nearestNearest.Value.FeatureByName(nearestPlaneName);
                        _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                        //origPlane.Select(false);
                        //nearestPlane.Select(true);

                        if (origPlane != null && origPlane.Select(false) && nearestPlane != null &&
                            nearestPlane.Select(true))
                        {
                            _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_Mate, "");
                            _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmOK, "");
                        }
                        _swSelComp.Select(false);


                        mateComponent = nearestNearest.Value;
                        sBox = (double[])_swSelComp.GetBox(false, false);
                        originalMaxZ = GetMaxZ(sBox, axe);
                        originalMinZ = GetMinZ(sBox, axe);
                        _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                    }
                    //теперь просто раздвигаем...
                    double dist = 0;
                    double correctionValue = 0;
                    if (nearestLeft.Value == mateComponent)
                    {
                        if (nearestRight.Value == null)
                            throw new Exception("Не определены размеры проема.");
                        dist = GetLengthByZR(originalMinZ, GetMaxZ(nearestRight.Value.GetBox(false, false), axe));
                        correctionValue = correctionValueRight;
                    }
                    if (nearestRight.Value == mateComponent)
                    {
                        if (nearestLeft.Value == null)
                            throw new Exception("Не определены размеры проема.");
                        dist = GetLengthByZL(originalMaxZ, GetMinZ(nearestLeft.Value.GetBox(false, false), axe));
                        correctionValue = correctionValueLeft;
                    }
                    if (dist == 0)
                        return;
                    dist = dist * 1000 + correctionValue + originalLength;
                    if (dist >= 3000)
                        throw new Exception("Проем превышает максимально допустимое значение в 3000мм");
                    txtDist.Text = dist.ToString();

                    OleDbConnection oleDb;
                    if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                    {
                        if (SavePartChanges(oleDb, txtDist))
                        {
                            #region Обработка таблицы dimlimits

                            if (_namesOfColumnNameFromDimLimits.Count > 0)
                            {
                                var objNames = _namesOfColumnNameFromDimLimits.Where(x => x.Contains("obj"));
                                var listId = new List<int>();
                                foreach (var objName in objNames)
                                {
                                    int id = GetIdFromColumnName(objName);
                                    if (!listId.Contains(id))
                                        listId.Add(id);
                                }
                                int num = (from numbAndTextBox in _numbAndTextBoxes
                                           where numbAndTextBox.Value == txtDist
                                           select numbAndTextBox.Key).FirstOrDefault();
                                if (listId.Contains(num))
                                {
                                    bool isNeededRange = false;
                                    var cm = new OleDbCommand("SELECT * FROM dimlimits", oleDb);
                                    var rd = cm.ExecuteReader();
                                    var listWithNotRangedTxtBxs = new Dictionary<TextBox, TwoValue>();
                                    while (rd.Read())
                                    {
                                        var min = (int)rd["obj" + num + "min"];
                                        var max = (int)rd["obj" + num + "max"];
                                        if ((dist >= min) && (dist <= max))
                                        {
                                            isNeededRange = true;
                                            foreach (var i in listId)
                                            {
                                                var t = _numbAndTextBoxes[i];
                                                string strVal = t.Text;
                                                int vl = Convert.ToInt32(strVal);
                                                var mn = (int)rd["obj" + i + "min"];
                                                var mx = (int)rd["obj" + i + "max"];
                                                if ((mn > vl) || (mx < vl))
                                                {
                                                    listWithNotRangedTxtBxs.Add(t, new TwoValue(mn, mx));
                                                }
                                            }
                                            if (_namesOfColumnNameFromDimLimits.Contains("stdsketchnum"))
                                                _mSwAddin.SetModelProperty(_swSelModel, "stdsketchNum", "",
                                                                           swCustomInfoType_e.swCustomInfoText,
                                                                           rd["stdsketchnum"].ToString());
                                            break;
                                        }
                                    }
                                    rd.Close();
                                    if (!isNeededRange)
                                    {
                                        MessageBox.Show(
                                            @"Текущий размер не попадает ни под какой диапазон требуемых значений!Измените значение!",
                                            _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                        txtDist.Text = txtDist.Name.Split('@').Last();
                                        txtDist.Focus();
                                        return;
                                    }
                                    if (listWithNotRangedTxtBxs.Count > 0)
                                    {
                                        int i = 0;
                                        string errStr =
                                            "Устанавливая текущее значение необходимо изменить поля со следующими значениями в соответствии с диапазонами ";
                                        foreach (var textBox in listWithNotRangedTxtBxs.Keys)
                                        {
                                            textBox.Text = textBox.Name.Split('@').Last();
                                            string vl = textBox.Text;
                                            string tName = textBox.Name.Split('@').First();
                                            if (i != listWithNotRangedTxtBxs.Count - 1)
                                                errStr = errStr + " " + tName + " со значением " + vl + " в (" +
                                                         listWithNotRangedTxtBxs[textBox].First + "..." +
                                                         listWithNotRangedTxtBxs[textBox].Second + ") и ";
                                            else
                                                errStr = errStr + " " + tName + " со значением " + vl + " в (" +
                                                         listWithNotRangedTxtBxs[textBox].First + "..." +
                                                         listWithNotRangedTxtBxs[textBox].Second + ")!";
                                            i++;
                                        }
                                        MessageBox.Show(errStr, _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                                        MessageBoxIcon.Information);
                                    }
                                }
                            }

                            #endregion

                            SetValueForTxtBx(txtDist);

                            IsNewSelectionDisabled = true;
                            _mSwAddin.RecalculateModel(_swSelModel);
                            IsNewSelectionDisabled = false;
                            _swModel.EditRebuild3();
                            _swModel.GraphicsRedraw2();
                            if (_objListChanged.Contains(txtDist))
                                _objListChanged.Remove(txtDist);
                        }
                        oleDb.Close();
                    }
                }
                SwAsmDoc.NewSelectionNotify += NewSelection;
            }
            catch (Exception ex)
            {
                SwAsmDoc.NewSelectionNotify += NewSelection;
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (!this.InvokeRequired)
                    this.Enabled = true;
                else
                {
                    this.Invoke(new EventHandler(delegate
                    {

                        this.Enabled = true;
                    }));
                }
            }
        }
        private void MeasureLength(object sender, EventArgs e)
        {
            try
            {

                _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                _chkMeasure.Checked = false;
                Measure swMeasure = _swModel.Extension.CreateMeasure();

                swMeasure.Calculate(null);

                double dist = swMeasure.Length;

                if (dist == -1)
                    dist = swMeasure.Distance;

                if (dist != -1)
                {
                    var btnDist = (Button)sender;
                    TextBox txtDist = _butPlusTxt[btnDist];
                    dist = dist * 1000;
                    txtDist.Text = dist.ToString();

                    OleDbConnection oleDb;
                    if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                    {
                        if (SavePartChanges(oleDb, txtDist))
                        {
                            #region Обработка таблицы dimlimits
                            if (_namesOfColumnNameFromDimLimits.Count > 0)
                            {
                                var objNames = _namesOfColumnNameFromDimLimits.Where(x => x.Contains("obj"));
                                var listId = new List<int>();
                                foreach (var objName in objNames)
                                {
                                    int id = GetIdFromColumnName(objName);
                                    if (!listId.Contains(id))
                                        listId.Add(id);
                                }
                                int num = (from numbAndTextBox in _numbAndTextBoxes
                                           where numbAndTextBox.Value == txtDist
                                           select numbAndTextBox.Key).FirstOrDefault();
                                if (listId.Contains(num))
                                {
                                    bool isNeededRange = false;
                                    var cm = new OleDbCommand("SELECT * FROM dimlimits", oleDb);
                                    var rd = cm.ExecuteReader();
                                    var listWithNotRangedTxtBxs = new Dictionary<TextBox, TwoValue>();
                                    while (rd.Read())
                                    {
                                        var min = (int)rd["obj" + num + "min"];
                                        var max = (int)rd["obj" + num + "max"];
                                        if ((dist >= min) && (dist <= max))
                                        {
                                            isNeededRange = true;
                                            foreach (var i in listId)
                                            {
                                                var t = _numbAndTextBoxes[i];
                                                string strVal = t.Text;
                                                int vl = Convert.ToInt32(strVal);
                                                var mn = (int)rd["obj" + i + "min"];
                                                var mx = (int)rd["obj" + i + "max"];
                                                if ((mn > vl) || (mx < vl))
                                                {
                                                    listWithNotRangedTxtBxs.Add(t, new TwoValue(mn, mx));
                                                }
                                            }
                                            if (_namesOfColumnNameFromDimLimits.Contains("stdsketchnum"))
                                                _mSwAddin.SetModelProperty(_swSelModel, "stdsketchNum", "",
                                                                           swCustomInfoType_e.swCustomInfoText,
                                                                           rd["stdsketchnum"].ToString());
                                            break;
                                        }
                                    }
                                    rd.Close();
                                    if (!isNeededRange)
                                    {
                                        MessageBox.Show(
                                            @"Текущий размер не попадает ни под какой диапазон требуемых значений!Измените значение!",
                                            _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                        txtDist.Text = txtDist.Name.Split('@').Last();
                                        txtDist.Focus();
                                        return;
                                    }
                                    if (listWithNotRangedTxtBxs.Count > 0)
                                    {
                                        int i = 0;
                                        string errStr =
                                            "Устанавливая текущее значение необходимо изменить поля со следующими значениями в соответствии с диапазонами ";
                                        foreach (var textBox in listWithNotRangedTxtBxs.Keys)
                                        {
                                            textBox.Text = textBox.Name.Split('@').Last();
                                            string vl = textBox.Text;
                                            string tName = textBox.Name.Split('@').First();
                                            if (i != listWithNotRangedTxtBxs.Count - 1)
                                                errStr = errStr + " " + tName + " со значением " + vl + " в (" +
                                                         listWithNotRangedTxtBxs[textBox].First + "..." +
                                                         listWithNotRangedTxtBxs[textBox].Second + ") и ";
                                            else
                                                errStr = errStr + " " + tName + " со значением " + vl + " в (" +
                                                         listWithNotRangedTxtBxs[textBox].First + "..." +
                                                         listWithNotRangedTxtBxs[textBox].Second + ")!";
                                            i++;
                                        }
                                        MessageBox.Show(errStr, _mSwAddin.MyTitle, MessageBoxButtons.OK,
                                                        MessageBoxIcon.Information);
                                    }
                                }
                            }
                            #endregion

                            SetValueForTxtBx(txtDist);

                            IsNewSelectionDisabled = true;
                            _mSwAddin.RecalculateModel(_swSelModel);
                            IsNewSelectionDisabled = false;
                            _swModel.EditRebuild3();
                            _swModel.GraphicsRedraw2();
                            if (_objListChanged.Contains(txtDist))
                                _objListChanged.Remove(txtDist);
                        }
                        oleDb.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DeleteMate(object sender, EventArgs e)
        {
            var btnPressed = (Button)sender;
            var swMate = (Feature)btnPressed.Tag;
            //bool status =  _swSelModel.Extension.SelectByID2(swMate.Name, "MATE", 0, 0, 0, false, 0, null, 0);
            bool status = swMate.Select(false);
            if (status)
            {
                var swModel = _mSwAddin.SwApp.IActiveDoc2;
                swModel.EditDelete();
            }
            btnPressed.Visible = false;
            //swMate.dhf
        }

        private void AddMate(object sender, EventArgs e)
        {
            var btnPressed = (Button)sender;
            var swFeat = _swSelComp.FeatureByName((string)btnPressed.Tag);
            IsNewSelectionDisabled = true;
            _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");

            if (swFeat != null && swFeat.Select(false))
            {
                _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_Mate, "");
                _isMate = true;
            }
            IsNewSelectionDisabled = false;
        }

        private void TxtPrmKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                SelectNextControl((Control)sender, true, true, true, true);
            }

            if (e.KeyCode == Keys.Enter)
            {
                _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                if (SaveChanges(false, false))
                {
                    e.Handled = true;
                    SelectNextControl((Control)sender, true, true, true, true);
                }
            }

        }

        private void FormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
            }
        }

        private void ComboBoxConfigSelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox confComboBox = (ComboBox)sender;
            ChangeConfigurationComponent(confComboBox);
            Label selectedLabel = (Label)(((KeyValuePair<Label, ComboBox>)(confComboBox.Tag)).Key);
            selectedLabel.Text = confComboBox.Name + " : " + confComboBox.SelectedItem.ToString();
        }

        private void ComboBoxConfigKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                SelectNextControl((Control)sender, true, true, true, true);
            }
        }
        private void BtnOkClick(object sender, EventArgs e)
        {
            //Logging.Log.Instance.Debug("Ok Click");
            btnOK.Enabled = false;
            if (SwAddin.needWait)
            {
                ProgressBar.WaitTime.Instance.ShowWait();
                ProgressBar.WaitTime.Instance.SetLabel("Ожидание завершения предедущих операций.");
                lock (SwAddin.workerLocker)
                    Monitor.Wait(SwAddin.workerLocker);
                ProgressBar.WaitTime.Instance.HideWait();
                //Logging.Log.Instance.Debug("Ok Run");
                _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                if (SaveChanges(true, false))
                    Close();
            }
            else
            {
                //Logging.Log.Instance.Debug("Ok Run");
                _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                if (SaveChanges(true, false))
                    Close();
            }
            btnOK.Enabled = true;
        }

        private void BtnOkKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                SelectNextControl((Control)sender, true, true, true, true);
            }
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
            foreach (var objUnit in _commonList)
            {
                if (objUnit.GetType() == typeof(TextBox))
                {
                    var textBox = (TextBox)objUnit;
                    string oldVal = "";
                    foreach (var box in _butPlusTxt)
                    {
                        if (box.Value == textBox)
                            oldVal = box.Key.Name;
                    }

                    if (oldVal != "" && textBox.Text != oldVal)
                    {
                        textBox.Text = oldVal;
                        OleDbConnection oleDb;
                        if (_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                        {
                            if (SavePartChanges(oleDb, textBox))
                            {
                                SetValueForTxtBx(textBox);
                                var swComps = new LinkedList<Component2>();
                                IsNewSelectionDisabled = true;
                                if (_mSwAddin.GetComponents(_swSelComp, swComps, false, false))
                                {
                                    foreach (var component2 in swComps)
                                    {
                                        var swModel = component2.IGetModelDoc();
                                        if ((swModel != null) && (_mSwAddin.GetModelDatabaseFileName(swModel) != ""))
                                            _mSwAddin.RecalculateModel(swModel);
                                    }
                                }
                                _mSwAddin.RecalculateModel(_swSelModel);
                                IsNewSelectionDisabled = false;
                                _swModel.EditRebuild3();
                                _swModel.GraphicsRedraw2();
                            }
                        }
                    }
                }
            }
            Close();
        }

        private void BtnCancelKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                SelectNextControl((Control)sender, true, true, true, true);
            }
        }

        private void TabMainSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_dictHeightPage.Count != 0 && _dictHeightPage.ContainsKey(tabMain.SelectedTab))
            {
                SetSizeForTab(_dictHeightPage[tabMain.SelectedTab]);
            }

            if (_tabDec != null && tabMain.SelectedTab == _tabDec)
            {
                //Size = new Size(280, Size.Height);
                //tabMain.Size = new Size(260, tabMain.Size.Height);
                btnOK.Location = new Point(40, btnOK.Location.Y);
                btnCancel.Location = new Point(140, btnCancel.Location.Y);
            }
            else
            {
                if (tabMain.Size.Width == 260 && Size.Width == 280)
                {
                    Size = new Size(250, Size.Height);
                    tabMain.Size = new Size(230, tabMain.Size.Height);
                    btnOK.Location = new Point(25, btnOK.Location.Y);
                    btnCancel.Location = new Point(128, btnCancel.Location.Y);
                }
                if (tbpPrice != null && tabMain.SelectedTab == tbpPrice)
                {
                    string path = ((ModelDoc2)_mSwAddin.SwApp.ActiveDoc).GetPathName();

                    Repository.Instance.OrderNumber = Path.GetFileName(Path.GetDirectoryName(path));
                    SaveChanges(true, false);

                    try
                    {
                        List<string> errorsList = new List<string>();
                        decimal price = Repository.Instance.GetPriceForComponent(_swSelModel, errorsList);

                        if (errorsList.Count != 0)
                        {
                            lblPrice.Text = errorsList[0];
                        }
                        else
                        {
                            string ttl = string.Format("Цена: {0} р.", price);

                            if (ttl.First() == 'Ц' || ttl.First() == 'ц')
                                lblPrice.Font = new Font("Arial", 11, FontStyle.Bold);
                            else
                                lblPrice.Font = new Font("Arial", 8, FontStyle.Regular);

                            lblPrice.Text = ttl;
                        }
                    }
                    catch (Exception ex)
                    {
                        lblPrice.Text = ex.Message;
                    }
                }
            }
        }

        private void TxtNewTextChanged(object sender, EventArgs e)
        {
            var textBox = (TextBox)sender;
            if (!_objListChanged.Contains(textBox))
                _objListChanged.Add(textBox);
            double tt;

            //var currentCulture = System.Globalization.CultureInfo.InstalledUICulture;
            //var numberFormat = (System.Globalization.NumberFormatInfo)currentCulture.NumberFormat.Clone();
            //numberFormat.NumberDecimalSeparator = ".";
            //numberFormat.NumberDecimalDigits = 1;
            if (double.TryParse(textBox.Text, out tt))//System.Globalization.NumberStyles.Any ,numberFormat, out tt))
            {
                textBox.TextChanged -= TxtNewTextChanged;
                string tS = tt.ToString("0.0");
                if (tS.Last() == '0')
                    tS = tt.ToString("0");
                textBox.Text = tS;
                textBox.TextChanged += TxtNewTextChanged;
            }
            else
                return;
            if (_isMate)
            {
                IsNewSelectionDisabled = true;
                _mSwAddin.SwApp.RunCommand((int)swCommands_e.swCommands_PmCancel, "");
                _swModel.ClearSelection();
                _isMate = false;
                IsNewSelectionDisabled = false;
            }
        }
        #endregion

        private void commentsTBTextChanged(object sender, EventArgs e)
        {
            Size size = TextRenderer.MeasureText(commentsTb.Text, commentsTb.Font);

            commentsTb.Height = (size.Width / (commentsTb.Width - 10)) * 18 + 25;

        }
    }

    class DecorComponentsWithCombo
    {
        public int Number;
        public ComboBox Combo;
        public Component2 Component;

        public DecorComponentsWithCombo(int inNumber, ComboBox inCombo, Component2 inComponent)
        {
            Number = inNumber;
            Combo = inCombo;
            Component = inComponent;
        }
    }

    class SetDecors
    {
        public Component2 Component;
        public string ColorProperty;
        public string PartColorProperty;

        public SetDecors(Component2 inComponent, string inColorProperty, string inPartColorProperty)
        {
            Component = inComponent;
            ColorProperty = inColorProperty;
            PartColorProperty = inPartColorProperty;
        }

    }

    class DimensionConfForList
    {
        internal int Number;
        internal string LabelName;
        internal string StrObjName;
        internal int ObjVal;
        internal List<int> IdSlave;
        internal Component2 Component;
        internal bool IsGrey;
        internal int Id;

        internal DimensionConfForList(int number, string labelName, string strObjName, int objVal, List<int> idSlave,
            Component2 component, bool isGrey, int id)
        {
            Number = number;
            LabelName = labelName;
            StrObjName = strObjName;
            ObjVal = objVal;
            IdSlave = idSlave;
            Component = component;
            IsGrey = isGrey;
            Id = id;
        }
    }

    class TwoValue
    {
        public int First;
        public int Second;

        public TwoValue(int first, int second)
        {
            First = first;
            Second = second;
        }
    }
}