using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using Environment = System.Environment;


namespace Furniture
{
    public partial class FrmEdge : Form
    {
        private readonly SwAddin _mSwAddin;
        private  ModelDoc2 _swModel;
        private SelectionMgr _swSelMgr;
        private Component2 _swSelComp;
        private readonly AssemblyDoc _swAsmDoc;
        private Feature feature11;
        private Feature feature12;
        private Feature feature21;
        private Feature feature22;
        private Feature axFeature11;
        private Feature axFeature12;
        private Feature axFeature21;
        private Feature axFeature22;
        private const string Edge11PropertyName = "Faner11";
        private const string Edge12PropertyName = "Faner12";
        private const string Edge21PropertyName = "Faner21";
        private const string Edge22PropertyName = "Faner22";
        private const string edge11PropertyColorName = "colorFaner11";
        private const string edge12PropertyColorName = "colorFaner12";
        private const string edge21PropertyColorName = "colorFaner21";
        private const string edge22PropertyColorName = "colorFaner22";
        [Flags]
        public enum FanersBools { Faner11 = 1, Faner12 = 2, Faner21 = 4, Faner22 = 8 }

        public FrmEdge(SwAddin swAddin)
        {
            InitializeComponent();
            _mSwAddin = swAddin;
            _swModel = (ModelDoc2)_mSwAddin.SwApp.ActiveDoc;
            _swSelMgr = (SelectionMgr)_swModel.SelectionManager;
            _swAsmDoc = (AssemblyDoc)_swModel;
            _swAsmDoc.NewSelectionNotify += NewSelection;

            pbEdge11.Image = null;
            pbEdge12.Image = null;
            pbEdge21.Image = null;
            pbEdge22.Image = null;
            NewSelection();
          

            Show();
        }

        internal int NewSelection()
        {
            SetDecors();
            RotateIfNeed();
            return 0;
        }

        private void SetDecors()
        {
            var swTestSelComp = (Component2)_swSelMgr.GetSelectedObjectsComponent2(1);
            if (swTestSelComp == null || string.IsNullOrEmpty(swTestSelComp.Name))
                return;
            lbMainNameLabel.Text = swTestSelComp.Name.Split('/').First();
            Component2 swTecSelComp;
            _mSwAddin.GetParentLibraryComponent(swTestSelComp, out swTecSelComp);
            OleDbConnection oleDb;
            ModelDoc2 specModel;
            _swSelComp = _mSwAddin.GetMdbComponentTopLevel(swTecSelComp, out specModel);
            var _swSelModel = _swSelComp.IGetModelDoc();
            _swModel = _swSelModel;
            if (!_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                return;
            var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,new object[] { null, null, null, "TABLE" });

            if (!oleSchem.Rows.Cast<DataRow>().Any(row => (string)row["TABLE_NAME"] == "faners"))
            {
                //Logging.Log.Instance.Fatal("Нет таблицы faners, хотя есть свойство ExFanerFeats! Невозможно задать кромки. " + _swSelComp.Name);
                return;
            }
            OleDbCommand cm;
            OleDbDataReader rd;
            var outComps = new LinkedList<Component2>();

            string decPathDef = Furniture.Helpers.LocalAccounts.decorPathResult;
            cm = new OleDbCommand("SELECT * FROM faners ORDER BY FanerName ", oleDb);
            rd = cm.ExecuteReader();
            List <Faner> faners= new List<Faner>();
            while (rd.Read())
            {
                if (rd["FanerType"] as string !=null)
                {
                    faners.Add(new Faner((string)rd["FanerName"], (string)rd["FanerType"], (string)rd["DecorGroup"]));
                }
                else 
                {
                    faners.Add(new Faner((string)rd["FanerName"], string.Empty, (string)rd["DecorGroup"]));
                }
            } 
            rd.Close();
            foreach (Faner faner in faners)
            {
                switch(faner.FanerName.Substring(faner.FanerName.Length - 2, 2))
                {
                    case "11":
                        feature11 = FindEdge(_swSelComp, faner.FanerName);
                        axFeature11 = FindEdge(_swSelComp, faner.AxFanerName);
                        SetValuesForGroup(faner, gbEdge11, cbExist11, cbColor11, feature11,  Edge11PropertyName, edge11PropertyColorName);
                        break;
                    case "12":
                        feature12 = FindEdge(_swSelComp, faner.FanerName);
                        axFeature12 = FindEdge(_swSelComp, faner.AxFanerName);
                        SetValuesForGroup(faner, gbEdge12, cbExist12, cbColor12, feature12,  Edge12PropertyName, edge12PropertyColorName);
                        break;
                    case "21":
                        feature21 = FindEdge(_swSelComp, faner.FanerName);
                        axFeature21 = FindEdge(_swSelComp, faner.AxFanerName);
                        SetValuesForGroup(faner, gbEdge21, cbExist21, cbColor21, feature21,  Edge21PropertyName, edge21PropertyColorName);
                        break;
                    case "22":
                        feature22 = FindEdge(_swSelComp, faner.FanerName);
                        axFeature22 = FindEdge(_swSelComp, faner.AxFanerName);
                        SetValuesForGroup(faner, gbEdge22, cbExist22, cbColor22, feature22,  Edge22PropertyName, edge22PropertyColorName);
                        break;
                    default:
                        break;
                }
            }
            return;
        }
        private void RotateIfNeed()
        {
            OleDbCommand cm;
            OleDbDataReader rd;
            OleDbConnection oleDb;
            if (!_mSwAddin.OpenModelDatabase(_swModel, out oleDb))
                return;
            cm = new OleDbCommand("SELECT * FROM decors_conf ORDER BY id ", oleDb);
            rd = cm.ExecuteReader();
            rd.Read();
            bool? textureDirection = rd["Texture direction"] as bool?;
            if (textureDirection == null || textureDirection == false)
            {
                ApplyTriangles(false);
                return;
            }
            //если мы сдесь, значит надо поворачивать
            Point gbEdge11Location = gbEdge11.Location;
            Point pbEdge11Location = pbEdge11.Location;
            Size sz11 = new Size(pbEdge11.Width, pbEdge11.Height);

            gbEdge11.Location = gbEdge22.Location;
            pbEdge11.Location = pbEdge22.Location;
            pbEdge11.Size = pbEdge22.Size;

            gbEdge22.Location = gbEdge12.Location;
            pbEdge22.Location = pbEdge12.Location;
            pbEdge22.Size = pbEdge12.Size;

            gbEdge12.Location = gbEdge21.Location;
            pbEdge12.Location = pbEdge21.Location;
            pbEdge12.Size = pbEdge21.Size;

            gbEdge21.Location = gbEdge11Location;
            pbEdge21.Location = pbEdge11Location;
            pbEdge21.Size = sz11;

            pbEdgeMain.Image = Properties.Resources.VerticalFiberDirection;
            ApplyTriangles(true);
        }
        private void SetValuesForGroup(Faner faner,GroupBox gb,ComboBox cb,ComboBox cbColor,Feature feature,string propertyName,string colorPropertyName)
        {
                gb.Enabled = true;
                
                //lb.Text = faner.FanerName;
                cb.Items.Clear();
                cb.Items.Add("Нет");
                cb.Items.Add("H");
                //if (feature.IsSuppressed())
                //{
                //    cb.SelectedIndex = 0;
                //    _mSwAddin.SetModelProperty(_swModel, propertyName, "", swCustomInfoType_e.swCustomInfoText, string.Empty, true);//"Нет", true);
                //}
                //else
                //{
                    //if (!string.IsNullOrEmpty(faner.DefaultFanerType))
                    //{
                    //    if (faner.DefaultFanerType == "H")
                    //    {
                    //        cb.SelectedIndex = 1;
                    //    }
                    //    else
                    //    {
                    //        if (!string.IsNullOrEmpty(faner.DefaultFanerType))
                    //        {
                    //            cb.Items.Add(faner.DefaultFanerType);
                    //            cb.SelectedIndex = 2;
                    //        }
                    //    }
                    //}

                var fanerType = _swModel.GetCustomInfoValue("", propertyName);
                if (!string.IsNullOrEmpty(fanerType))
                {
                    //выбрать то св-во которое указано в fanerType
                    int i = 0;
                    foreach (string item in cb.Items)
                    {
                        if (item == fanerType)
                            cb.SelectedIndex = i;
                        i++;
                    }
                }
                else // если такого св-ва нет, то пишу его из дефолтного значения БД
                {
                    //switch (faner.DefaultFanerType)
                    //{
                    //    case null:
                    //    case "":
                    //    case "Нет":
                    cb.SelectedIndex = 0;
                    _mSwAddin.SetModelProperty(_swModel, propertyName, "", swCustomInfoType_e.swCustomInfoText, string.Empty, true);//"Нет", true);
                    //        throw new Exception("В БД неправильно указан тип кромки. (таблица faners)");
                    //        break;
                    //    case "H":
                    //        cb.SelectedIndex = 1;
                    //        _mSwAddin.SetModelProperty(_swModel, propertyName, "", swCustomInfoType_e.swCustomInfoText, "H", true);
                    //        break;
                    //}
                }
                //}


            cbColor.Items.Clear();
                foreach (string decorGroup in faner.DecorGroup)
                {
                    cbColor.Items.Add(decorGroup);
                }
                var colorName = _swModel.GetCustomInfoValue("", colorPropertyName);
                if (!string.IsNullOrEmpty(colorName))
                {
                    //выбрать то св-во которое указано в fanerType
                    int i = 0;
                    foreach (string item in cbColor.Items)
                    {
                        if (item == colorName)
                            cbColor.SelectedIndex = i;
                        i++;
                    }
                }
        }
        private static Feature FindEdge(Component2 inComponent, string featureName)
        {
            var childComps = (object[])inComponent.GetChildren();
            foreach (var oChildComp in childComps)
            {
                var childComp = (Component2)oChildComp;
                var swCompModel = (ModelDoc2)childComp.GetModelDoc();
                if (swCompModel != null)
                {
                    Feature swFeat = swCompModel.FirstFeature();
                    while (swFeat != null)
                    {
                        if (swFeat.Name == featureName) //"OffsetRefSurface" swFeat.GetTypeName2() == "ICE"  &&
                        {
                            return swFeat;
                        }
                        swFeat = swFeat.IGetNextFeature();
                    }
                    FindEdge(childComp, featureName);
                }
            }
            return null;
        }
        private void SaveSettings(ComboBox cb,ComboBox cbColor,Feature feature,Feature axfaner,string existPropName,string colorPropName)
        {
            //для каждой группы написать такую обработку.
            if (cb.Text == "Нет")
            {
                // гасим
                if (!axfaner.IsSuppressed())
                    axfaner.SetSuppression(0);
                //if (featureNone.IsSuppressed())
                //    featureNone.SetSuppression(2);
                string colorPath = GetTextureFilePath("none");
                SetDecors(_swModel, feature, colorPath);
                _swModel.DeleteCustomInfo2(string.Empty, existPropName);
                _swModel.DeleteCustomInfo2(string.Empty, colorPropName);
                //_mSwAddin.SetModelProperty(_swModel, existPropName, "", swCustomInfoType_e.swCustomInfoText, string.Empty, true);//"Нет", true);
                //_mSwAddin.SetModelProperty(_swModel, colorPropName, "", swCustomInfoType_e.swCustomInfoText, string.Empty, true);//"Нет", true);
                return;
            }
            else if (cb.Text != "Нет")
            {
                //зажигаем (если погашена)
                if (axfaner.IsSuppressed())
                    axfaner.SetSuppression(2);
                //if (!featureNone.IsSuppressed())
                //    featureNone.SetSuppression(0);
                _mSwAddin.SetModelProperty(_swModel, existPropName, "", swCustomInfoType_e.swCustomInfoText, "H", true);
            }
            if (!string.IsNullOrEmpty(cbColor.Text) && !axfaner.IsSuppressed())
            {
                string colorPath = GetTextureFilePath(cbColor.Text);
                SetDecors(_swModel,feature, colorPath);
                _mSwAddin.SetModelProperty(_swModel, colorPropName, "", swCustomInfoType_e.swCustomInfoText, cbColor.Text, true);
            }
        }
        public void SaveSettings()
        {
            if (gbEdge11.Enabled)
            {
                SaveSettings(cbExist11, cbColor11, feature11,axFeature11, Edge11PropertyName, edge11PropertyColorName);
            }
            if (gbEdge12.Enabled)
            {
                SaveSettings(cbExist12, cbColor12, feature12, axFeature12, Edge12PropertyName, edge12PropertyColorName);
            }
            if (gbEdge21.Enabled)
            {
                SaveSettings(cbExist21, cbColor21, feature21,axFeature21,Edge21PropertyName, edge21PropertyColorName);
            }
            if (gbEdge22.Enabled)
            {
                SaveSettings(cbExist22, cbColor22, feature22, axFeature22, Edge22PropertyName, edge22PropertyColorName);
            }
            _swModel.EditRebuild3();
            _swModel.Save2(false);
        }
        private void BtnOkClick(object sender, EventArgs e)
        {
            SaveSettings();
            Close();
        }
        private static string GetTextureFilePath(string cbText)
        {
            return Path.Combine(Furniture.Helpers.LocalAccounts.decorPathResult, cbText + ".jpg");
        }
        private static void SetDecors(ModelDoc2 mainModel,Feature edge,string textureFilePath)
        {
            //if (textureFilePath.Contains("none"))
            //    textureFilePath = textureFilePath.Replace("jpg", "p2m");
            var texture = mainModel.Extension.CreateTexture(textureFilePath, 20, 0, false);
            edge.RemoveTexture(true, string.Empty);
            edge.SetTexture(true, string.Empty, texture);

        }
        private void ApplyTriangles(bool isRotate)
        {
            PictureBox[] pbArray = new PictureBox[4] { pbEdge11, pbEdge22, pbEdge12, pbEdge21 };
            if (isRotate)
                pbArray = new PictureBox[4] { pbEdge21, pbEdge11, pbEdge22, pbEdge12 };

            GraphicsPath edge;
            
            for (int i=0;i<4;i++)
            {
                edge = new GraphicsPath();
                switch (i)
                {
                    case 0:
                        edge.AddLines(new Point[]
                                    {
                                        new Point(pbArray[i].Width/2, 0), new Point(pbArray[i].Width, pbArray[i].Height),
                                        new Point(0, pbArray[i].Height),new Point(pbArray[i].Width/2, 0)
                                    });
                        pbArray[i].Region = new Region(edge);
                        break;
                    case 1:
                        edge.AddLines(new Point[]
                                    {
                                        new Point(0,pbArray[i].Height/2), new Point(pbArray[i].Width, 0),
                                        new Point(pbArray[i].Width, pbArray[i].Height),new Point(0,pbArray[i].Height/2)
                                    });
                        pbArray[i].Region = new Region(edge);
                        break;
                    case 2:
                        edge.AddLines(new Point[]
                                    {
                                        new Point(pbArray[i].Width/2,pbArray[i].Height), new Point(0, 0),
                                        new Point(pbArray[i].Width, 0),new Point(pbArray[i].Width/2,pbArray[i].Height)
                                    });
                        pbArray[i].Region = new Region(edge);
                        break;
                    case 3:
                        edge.AddLines(new Point[]
                                    {
                                        new Point(pbArray[i].Width,pbArray[i].Height/2), new Point(0, pbArray[i].Height),
                                        new Point(0, 0),new Point(pbArray[i].Width,pbArray[i].Height/2)
                                    });
                        pbArray[i].Region = new Region(edge);
                        break;
                    default:
                        throw new Exception("Неправильный цикл!");
                }
               
            }

            
        }
        private void cbColorIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox) sender;
            Image image;
            if (useForAll.Checked)
            {
                if (!string.IsNullOrEmpty(cb.Text) && cb.Enabled)
                {
                    image = Image.FromFile(GetTextureFilePath(cb.Text));
                }
                else
                {
                    image = null;
                }
              
                int selIndex = cb.SelectedIndex;
                cbColor11.SelectedIndexChanged -= cbColorIndexChanged;
                cbColor12.SelectedIndexChanged -= cbColorIndexChanged;
                cbColor21.SelectedIndexChanged -= cbColorIndexChanged;
                cbColor22.SelectedIndexChanged -= cbColorIndexChanged;
                if (gbEdge11.Enabled && cbExist11.Text!="Нет")
                {
                    pbEdge11.Image = image;
                    if (cb != cbColor11)
                        cbColor11.SelectedIndex = selIndex;
                }
                if (gbEdge12.Enabled && cbExist12.Text != "Нет")
                {
                    pbEdge12.Image = image;
                    if (cb != cbColor12)
                        cbColor12.SelectedIndex = selIndex;
                }
                if (gbEdge21.Enabled && cbExist21.Text != "Нет")
                {
                    pbEdge21.Image = image;
                    if (cb != cbColor21)
                        cbColor21.SelectedIndex = selIndex;
                }
                if (gbEdge22.Enabled && cbExist22.Text != "Нет")
                {
                    pbEdge22.Image = image;
                    if (cb != cbColor22)
                        cbColor22.SelectedIndex = selIndex;
                }
                cbColor11.SelectedIndexChanged += cbColorIndexChanged;
                cbColor12.SelectedIndexChanged += cbColorIndexChanged;
                cbColor21.SelectedIndexChanged += cbColorIndexChanged;
                cbColor22.SelectedIndexChanged += cbColorIndexChanged;
                return;
            }
            PictureBox pb=null;
            switch (cb.Name)
            {
                case "cbColor11":
                    pb = pbEdge11;
                    break;
                case "cbColor12":
                    pb = pbEdge12;
                    break;
                case "cbColor21":
                    pb = pbEdge21;
                    break;
                case "cbColor22":
                    pb = pbEdge22;
                    break;
                default:
                    return;
            }
            if (!string.IsNullOrEmpty(cb.Text) && cb.Enabled)
            {
                image = Image.FromFile(GetTextureFilePath(cb.Text));
                pb.Image = image;
            }
            else
            {
                pb.Image = null;
            }
        }
        public static string GetCommentFromProperties(string faner11, string faner12, string faner21, string faner22, double angle, SwAddin _mSwAddin, ModelDoc2 model)
        {
            bool fan11exist = !string.IsNullOrEmpty(faner11) && faner11 != "Нет";
            bool fan12exist = !string.IsNullOrEmpty(faner12) && faner12 != "Нет";
            bool fan21exist = !string.IsNullOrEmpty(faner21) && faner21 != "Нет";
            bool fan22exist = !string.IsNullOrEmpty(faner22) && faner22 != "Нет";
            FanersBools fb = new FanersBools();
            if (fan11exist)
                fb = fb | FanersBools.Faner11;
            if (fan12exist)
                fb = fb | FanersBools.Faner12;
            if (fan21exist)
                fb = fb | FanersBools.Faner21;
            if (fan22exist)
                fb = fb | FanersBools.Faner22;
            #region comment from fb and angle
            if (Math.Abs(angle) < 0.00001)
            {
                switch (fb)
                {
                    case FanersBools.Faner11:
                        return "KROMKOY K SEBE";
                    case FanersBools.Faner12:
                        return "KROMKOY OT SEBYA";
                    case FanersBools.Faner21:
                        return "KROMKOY OT UPORA";
                    case FanersBools.Faner22:
                        return "KROMKOY K UPORU";
                    case FanersBools.Faner21 | FanersBools.Faner22:
                    case FanersBools.Faner12 | FanersBools.Faner11:
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        OleDbCommand cm;
                        OleDbDataReader rd;
                        OleDbConnection oleDb;
                        if (!_mSwAddin.OpenModelDatabase(model, out oleDb))
                            return "Error DB Access!";
                        cm = new OleDbCommand("SELECT * FROM decors_conf ORDER BY id ", oleDb);
                        rd = cm.ExecuteReader();
                        rd.Read();
                        bool? textureDirection = rd["Texture direction"] as bool?;
                        string retresult;
                        if (textureDirection == null || textureDirection == false)
                            retresult= "VOLOKNA GORIZONTALNO";
                        else
                            retresult= "VOLOKNA VERTIKALNO";
                        if (faner11 == "N")
                        {
                            retresult = retresult + ", GOLOY K SEBE";
                        }
                        return retresult;
                    case FanersBools.Faner11 | FanersBools.Faner22:
                        return "KROMKOY K UPORU & K SEBE";
                    case FanersBools.Faner11 | FanersBools.Faner21:
                        return "KROMKOY OT UPORA & K SEBE";
                    case FanersBools.Faner12 | FanersBools.Faner22:
                        return "KROMKOY K UPORU & OT SEBYA";
                    case FanersBools.Faner21 | FanersBools.Faner12:
                        return "KROMKOY OT UPORA & OT SEBYA";
                    case FanersBools.Faner11 | FanersBools.Faner22 | FanersBools.Faner12:
                        return "GOLOY OT UPORA";
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21:
                        return "GOLOY K UPORU";
                    case FanersBools.Faner11 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY OT SEBYA";
                    case FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY K SEBE";
                    default:
                        return fb.ToString();

                }
            }
            else if (Math.Abs(angle + 90) < 0.00001 || Math.Abs(angle - 270) < 0.00001)//
            {
                switch (fb)
                {
                    case FanersBools.Faner11:
                        return "KROMKOY OT UPORA";
                    case FanersBools.Faner12:
                        return "KROMKOY K UPORU";
                    case FanersBools.Faner21:
                        return "KROMKOY OT SEBYA";
                    case FanersBools.Faner22:
                        return "KROMKOY K SEBE";
                    case FanersBools.Faner21 | FanersBools.Faner22:
                    case FanersBools.Faner12 | FanersBools.Faner11:
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        OleDbCommand cm;
                        OleDbDataReader rd;
                        OleDbConnection oleDb;
                        if (!_mSwAddin.OpenModelDatabase(model, out oleDb))
                            return "Error DB Access!";
                        cm = new OleDbCommand("SELECT * FROM decors_conf ORDER BY id ", oleDb);
                        rd = cm.ExecuteReader();
                        rd.Read();
                        bool? textureDirection = rd["Texture direction"] as bool?;
                        string retresult;
                        if (textureDirection == null || textureDirection == false)
                            retresult= "VOLOKNA VERTIKALNO";
                        else
                            retresult= "VOLOKNA GORIZONTALNO";
                         if (faner11 == "N")
                        {
                            retresult = retresult + ", GOLOY K SEBE";
                        }
                        return retresult;
                    case FanersBools.Faner11 | FanersBools.Faner22:
                        return "KROMKOY OT UPORA & K SEBE";
                    case FanersBools.Faner11 | FanersBools.Faner21:
                        return "KROMKOY OT UPORA & OT SEBYA";
                    case FanersBools.Faner12 | FanersBools.Faner22:
                        return "KROMKOY K UPORU & K SEBE";
                    case FanersBools.Faner21 | FanersBools.Faner12:
                        return "KROMKOY K UPORU & OT SEBYA";
                    case FanersBools.Faner11 | FanersBools.Faner22 | FanersBools.Faner12:
                        return "GOLOY OT SEBYA";
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21:
                        return "GOLOY K SEBE";
                    case FanersBools.Faner11 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY K UPORU";
                    case FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY OT UPORA";
                    default:
                        return fb.ToString();

                }
            }
            else if (Math.Abs(angle - 180) < 0.00001 || Math.Abs(angle + 180) < 0.00001)
            {
                switch (fb)
                {
                    case FanersBools.Faner11:
                        return "KROMKOY OT SEBYA";
                    case FanersBools.Faner12:
                        return "KROMKOY K SEBE";
                    case FanersBools.Faner21:
                        return "KROMKOY K UPORU";
                    case FanersBools.Faner22:
                        return "KROMKOY OT UPORA";
                    case FanersBools.Faner21 | FanersBools.Faner22:
                    case FanersBools.Faner12 | FanersBools.Faner11:
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        OleDbCommand cm;
                        OleDbDataReader rd;
                        OleDbConnection oleDb;
                        if (!_mSwAddin.OpenModelDatabase(model, out oleDb))
                            return "Error DB Access!";
                        cm = new OleDbCommand("SELECT * FROM decors_conf ORDER BY id ", oleDb);
                        rd = cm.ExecuteReader();
                        rd.Read();
                        bool? textureDirection = rd["Texture direction"] as bool?;
                        string retresult;
                        if (textureDirection == null || textureDirection == false)
                            retresult= "VOLOKNA GORIZONTALNO";
                        else
                            retresult= "VOLOKNA VERTIKALNO";
                        if (faner11 == "N")
                        {
                            retresult = retresult + ", GOLOY K SEBE";
                        }
                        return retresult;
                    case FanersBools.Faner11 | FanersBools.Faner22:
                        return "KROMKOY OT UPORA & OT SEBYA";
                    case FanersBools.Faner11 | FanersBools.Faner21:
                        return "KROMKOY K UPORU & OT SEBYA";
                    case FanersBools.Faner12 | FanersBools.Faner22:
                        return "KROMKOY OT UPORA & K SEBE";
                    case FanersBools.Faner21 | FanersBools.Faner12:
                        return "KROMKOY K UPORU & K SEBE";
                    case FanersBools.Faner11 | FanersBools.Faner22 | FanersBools.Faner12:
                        return "GOLOY K UPORU";
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21:
                        return "GOLOY OT UPORA";
                    case FanersBools.Faner11 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY K SEBE";
                    case FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY OT SEBYA";
                    default:
                        return fb.ToString();

                }
            }
            else if (Math.Abs(angle - 90) < 0.00001)
            {
                switch (fb)
                {
                    case FanersBools.Faner11:
                        return "KROMKOY K UPORU";
                    case FanersBools.Faner12:
                        return "KROMKOY OT UPORA";
                    case FanersBools.Faner21:
                        return "KROMKOY K SEBE";
                    case FanersBools.Faner22:
                        return "KROMKOY OT SEBYA";
                    case FanersBools.Faner21 | FanersBools.Faner22:
                    case FanersBools.Faner12 | FanersBools.Faner11:
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        OleDbCommand cm;
                        OleDbDataReader rd;
                        OleDbConnection oleDb;
                        if (!_mSwAddin.OpenModelDatabase(model, out oleDb))
                            return "Error DB Access!";
                        cm = new OleDbCommand("SELECT * FROM decors_conf ORDER BY id ", oleDb);
                        rd = cm.ExecuteReader();
                        rd.Read();
                        bool? textureDirection = rd["Texture direction"] as bool?;
                        string retresult;
                        if (textureDirection == null || textureDirection == false)
                            retresult= "VOLOKNA VERTIKALNO";
                        else
                            retresult= "VOLOKNA GORIZONTALNO";
                            if (faner11 == "N")
                        {
                            retresult = retresult + ", GOLOY K SEBE";
                        }
                        return retresult;
                    case FanersBools.Faner11 | FanersBools.Faner22:
                        return "KROMKOY K UPORU & OT SEBYA";
                    case FanersBools.Faner11 | FanersBools.Faner21:
                        return "KROMKOY K UPORU & K SEBE";
                    case FanersBools.Faner12 | FanersBools.Faner22:
                        return "KROMKOY OT UPORA & OT SEBYA";
                    case FanersBools.Faner21 | FanersBools.Faner12:
                        return "KROMKOY OT UPORA & K SEBE";
                    case FanersBools.Faner11 | FanersBools.Faner22 | FanersBools.Faner12:
                        return "GOLOY K SEBE";
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21:
                        return "GOLOY OT SEBYA";
                    case FanersBools.Faner11 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY OT UPORA";
                    case FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY K UPORU";
                    default:
                        return fb.ToString();

                }
            }
            else
                return "angle ne kraten 90";
            #endregion
        }
        public static string GetComment(ModelDoc2 model, double angle, SwAddin _mSwAddin)
        {
            //Считать по тому, подавлены они физически или нет. Если Есть св-во Faner11 а тела нет или подавлено то записать в свойство пустую строку
            //если наоборот Тело есть а Faner11 -нет или пустое, то записать туда H или DefaultValue
            Component2 _swSelectedComponent =model.ConfigurationManager.ActiveConfiguration.GetRootComponent3(true);
            OleDbCommand cm;
            OleDbDataReader rd;
            OleDbConnection oleDb;
            if (!_mSwAddin.OpenModelDatabase(model, out oleDb))
                return "Error DB Access!";
            cm = new OleDbCommand("SELECT * FROM faners ORDER BY FanerName ", oleDb);
            rd = cm.ExecuteReader();
            List<Faner> faners = new List<Faner>();
            while (rd.Read())
            {
                if (rd["FanerType"] as string != null)
                {
                    faners.Add(new Faner((string)rd["FanerName"], (string)rd["FanerType"], (string)rd["DecorGroup"]));
                }
                else
                {
                    faners.Add(new Faner((string)rd["FanerName"], string.Empty, (string)rd["DecorGroup"]));
                }
            }
            rd.Close();
            FanersBools fb = new FanersBools();
            Feature feature;
            foreach (var faner in faners)
            {
                switch (faner.FanerName.Substring(faner.FanerName.Length - 2, 2))
                {
                    case "11":
                         feature = FindEdge(_swSelectedComponent, faner.AxFanerName);
                            if (feature != null)
                            {
                                if (!feature.IsSuppressed())
                                    fb = fb | FanersBools.Faner11;
                            }
                        break;
                    case "12":
                        feature = FindEdge(_swSelectedComponent, faner.AxFanerName);
                            if (feature != null)
                            {
                                if (!feature.IsSuppressed())
                                    fb = fb | FanersBools.Faner12;
                            }
                        break;
                    case "21":
                        feature = FindEdge(_swSelectedComponent, faner.AxFanerName);
                            if (feature != null)
                            {
                                if (!feature.IsSuppressed())
                                    fb = fb | FanersBools.Faner21;
                            }
                        break;
                    case "22":
                        feature = FindEdge(_swSelectedComponent, faner.AxFanerName);
                            if (feature != null)
                            {
                                if (!feature.IsSuppressed())
                                    fb = fb | FanersBools.Faner22;
                            }
                        break;
                }
            }
            if (angle == 0)
            {
                switch (fb)
                {
                    case FanersBools.Faner11:
                        return "KROMKOY K SEBE";
                    case FanersBools.Faner12:
                        return "KROMKOY OT SEBYA";
                    case FanersBools.Faner21:
                        return "KROMKOY OT UPORA";
                    case FanersBools.Faner22:
                        return "KROMKOY K UPORU";
                    case FanersBools.Faner21 | FanersBools.Faner22:
                    case FanersBools.Faner12 | FanersBools.Faner11:
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        cm = new OleDbCommand("SELECT * FROM decors_conf ORDER BY id ", oleDb);
                        rd = cm.ExecuteReader();
                        rd.Read();
                        bool? textureDirection = rd["Texture direction"] as bool?;
                        if (textureDirection == null || textureDirection == false)
                            return "VOLOKNA GORIZONTALNO";
                        else
                            return "VOLOKNA VERTIKALNO";


                    case FanersBools.Faner11 | FanersBools.Faner22:
                        return "KROMKOY K UPORU & K SEBE";
                    case FanersBools.Faner11 | FanersBools.Faner21:
                        return "KROMKOY OT UPORA & K SEBE";
                    case FanersBools.Faner12 | FanersBools.Faner22:
                        return "KROMKOY K UPORU & OT SEBYA";
                    case FanersBools.Faner21 | FanersBools.Faner12:
                        return "KROMKOY OT UPORA & OT SEBYA";
                    case FanersBools.Faner11 | FanersBools.Faner22 | FanersBools.Faner12:
                        return "GOLOY OT UPORA";
                    case FanersBools.Faner11 | FanersBools.Faner12 | FanersBools.Faner21:
                        return "GOLOY K UPORU";
                    case FanersBools.Faner11 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY OT SEBYA";
                    case FanersBools.Faner12 | FanersBools.Faner21 | FanersBools.Faner22:
                        return "GOLOY K SEBE";
                    default:
                        return fb.ToString();

                }
            }
            else
                return "angle!=0";
        }
        public static void SetDefault(OleDbConnection oleDb, ModelDoc2 _swSelModel, string colorName, SwAddin _mSwAddin,string fullFileNameColor)
        {

            Component2 _swSelectedComponent =_swSelModel.ConfigurationManager.ActiveConfiguration.GetRootComponent3(true);
            OleDbCommand cm;
            OleDbDataReader rd;
            if (!_mSwAddin.OpenModelDatabase(_swSelModel, out oleDb))
                return;
            cm = new OleDbCommand("SELECT * FROM faners ORDER BY FanerName ", oleDb);
            rd = cm.ExecuteReader();
            List<Faner> faners = new List<Faner>();
            while (rd.Read())
            {
                if (rd["FanerType"] as string != null)
                {
                    faners.Add(new Faner((string)rd["FanerName"], (string)rd["FanerType"], (string)rd["DecorGroup"]));
                }
                else
                {
                    faners.Add(new Faner((string)rd["FanerName"], string.Empty, (string)rd["DecorGroup"]));
                }
            }
            rd.Close();
            string value = string.Empty;
            Feature feature,axfeature;
            foreach (var faner in faners)
            {
                string suffix = faner.FanerName.Substring(faner.FanerName.Length - 2, 2);
                            //красим кромку
                feature = FindEdge(_swSelectedComponent, faner.FanerName);
                axfeature = FindEdge(_swSelectedComponent, faner.AxFanerName);
                if (feature != null && axfeature!=null)
                {
                    if (!feature.IsSuppressed() && !axfeature.IsSuppressed())
                    {
                        SetDecors(_swSelModel, feature, fullFileNameColor);
                        _mSwAddin.SetModelProperty(_swSelModel, "Faner"+suffix, "",swCustomInfoType_e.swCustomInfoText,faner.DefaultFanerType, true);
                        _mSwAddin.SetModelProperty(_swSelModel, "colorFaner"+suffix, "",swCustomInfoType_e.swCustomInfoText, colorName, true);
                    }
                }
            }
        }

        public static void Actualization(ModelDoc2 model, SwAddin _mSwAddin)
        {
            Component2 _swSelectedComponent = model.ConfigurationManager.ActiveConfiguration.GetRootComponent3(true);
            OleDbCommand cm;
            OleDbDataReader rd;
            OleDbConnection oleDb;
            if (!_mSwAddin.OpenModelDatabase(model, out oleDb))
                return;
            cm = new OleDbCommand("SELECT * FROM faners ORDER BY FanerName ", oleDb);
            rd = cm.ExecuteReader();
            List<Faner> faners = new List<Faner>();
            while (rd.Read())
            {
                if (rd["FanerType"] as string != null)
                {
                    faners.Add(new Faner((string)rd["FanerName"], (string)rd["FanerType"], (string)rd["DecorGroup"]));
                }
                else
                {
                    faners.Add(new Faner((string)rd["FanerName"], string.Empty, (string)rd["DecorGroup"]));
                }
            }
            rd.Close();
            Feature feature;
            foreach (var faner in faners)
            {
                string suffix = faner.FanerName.Substring(faner.FanerName.Length - 2, 2);

                feature = FindEdge(_swSelectedComponent, faner.AxFanerName);
                if (feature != null)
                {
                    if (feature.IsSuppressed())
                    {
                        _mSwAddin.SetModelProperty(model, "Faner" + suffix, "", swCustomInfoType_e.swCustomInfoText, string.Empty, true);
                        _mSwAddin.SetModelProperty(model, "colorFaner" + suffix, "", swCustomInfoType_e.swCustomInfoText, string.Empty, true);
                    }
                    else
                    {
                        string fieldValue = model.GetCustomInfoValue(string.Empty, "Faner" + suffix);
                        if (fieldValue == string.Empty)
                        {
                            string msgText = "Для данной детали: " + model.GetPathName() + " кромка Faner" + suffix + " в модели не соответствует свойствам файла сборки. Данная кромка не будет импортирована в программу Покупки! Чтобы исправить эту ошибку используйте опцию \"MrDoors - Отделка кромки\"";
                            MessageBox.Show(msgText, @"MrDoors", MessageBoxButtons.OK);
                        }
                    }
                }
            }
        }

        private void ExistChanged(object sender, EventArgs e)
        {
            ComboBox send = (ComboBox) sender;
           
            switch (send.Name)
            {
                case "cbExist11":
                    if (send.Text != "Нет")
                    {
                        cbColor11.Enabled = true;
                        cbColorIndexChanged(cbColor11, null);
                        pbEdge11.Enabled = true;
                        return;
                    }
                    else
                    {
                        pbEdge11.Image = null;
                        cbColor11.Enabled = false;
                    }
                    break;
                case "cbExist12":
                    if (send.Text != "Нет")
                    {
                        cbColor12.Enabled = true;
                        cbColorIndexChanged(cbColor12, null);
                        pbEdge12.Enabled = true;
                        return;
                    }
                    else
                    {
                        pbEdge12.Image = null;
                        cbColor12.Enabled = false;
                    }
                    break;
                case "cbExist21":
                    if (send.Text != "Нет")
                    {
                        cbColor21.Enabled = true;
                        cbColorIndexChanged(cbColor21, null);
                        pbEdge21.Enabled = true;
                        return;
                    }
                    else
                    {
                        pbEdge21.Image = null;
                        cbColor21.Enabled = false;
                    }
                    break;
                case "cbExist22":
                    if (send.Text != "Нет")
                    {
                        cbColor22.Enabled = true;
                        cbColorIndexChanged(cbColor22, null);
                        pbEdge22.Enabled = true;
                        return;
                    }
                    else
                    {
                        pbEdge22.Image = null;
                        cbColor22.Enabled = false;
                    }
                    break;
            }
        }
    }

}