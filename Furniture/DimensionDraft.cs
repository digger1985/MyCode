using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Microsoft.VisualBasic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SwDocumentMgr;
using View = SolidWorks.Interop.sldworks.View;

namespace Furniture
{
    public class DimensionDraft : IDisposable
    {
        private readonly ISldWorks _swApp;
        private readonly SwAddin _swAdd;
        private readonly List<string> _namesOfColumnNameFromDimLimits = new List<string>();
        private XmlDocument _cxml;
        private XmlNode _node;
        private int _z;
        private bool _createProgramm;
        public bool isValidXml;
        public DimensionDraft(SwAddin swAddin)
        {
            _swAdd = swAddin;
            _swApp = swAddin.SwApp;
        }

        public void AutoDimensionDrawing(bool many)
        {
            var swModel = (ModelDoc2)_swApp.ActiveDoc;
            if (Properties.Settings.Default.DeleteBeforeDim)
                _swAdd.DeleteSketchDemensions(many);
            if (swModel.GetUserPreferenceToggle((int)swUserPreferenceToggle_e.swViewSketchRelations))
                swModel.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swViewSketchRelations, false);
            AutoDimensionDrawing2(swModel, many);
        }

        private void ReplaceViews(object[] swViews,DrawingDoc swDrawing)
        {
            Sheet currentSheet = swDrawing.GetCurrentSheet();
            double width = 0, height = 0;
            currentSheet.GetSize(ref width, ref height);
            double halfx = width / 2;
            double halfy = height / 2;
            Dictionary<string, double[]> boxes = new Dictionary<string, double[]>();
            foreach (View t in swViews)
            {
                var swView = (View)t;
                const string expr = "^F[1-6]$";
                Match isMatch = Regex.Match(swView.Name, expr, RegexOptions.IgnoreCase);
                if (!isMatch.Success)
                    continue;
                double[] bBox = swView.GetOutline();
                boxes.Add(swView.Name, bBox);
            }
            if (!boxes.ContainsKey("F1"))
            {
                return;
            }
            foreach (View t in swViews)
            {
                var swView = (View)t;


                if (swView.GetName2().Contains("F2"))
                {
                    swView.Position = new double[] { boxes["F1"][0]/2 , 0 };//{ boxes["F1"][0] - ((boxes["F1"][2] - boxes["F1"][0]) / 2), 0 };//{ boxes["F1"][0]/2, halfy - Math.Abs(deltay) };//{ boxes["F1"][0] - ((boxes["F1"][2] - boxes["F1"][0]) / 2), halfy - Math.Abs(deltay) };
                }
                if (swView.GetName2().Contains("F3"))
                {
                    swView.Position = new double[] { boxes["F1"][2] + ((width - boxes["F1"][2]) / 2), 0 };//{ boxes["F1"][2] + ((boxes["F1"][2] - boxes["F1"][0]) / 2), 0 };
                }
                if (swView.GetName2().Contains("F4"))
                {
                    swView.Position = new double[] { 0, boxes["F1"][3] + ((height - boxes["F1"][3]) / 2) };//{ 0, boxes["F1"][3] + ((boxes["F1"][3] - boxes["F1"][1]) / 2) };//{ halfx, boxes["F1"][3] + ((height - boxes["F1"][3]) / 2) - Math.Abs(deltay) };//{ halfx, boxes["F1"][3] + ((boxes["F1"][3] - boxes["F1"][1]) / 2) - Math.Abs(deltay) };
                }
                if (swView.GetName2().Contains("F5"))
                {
                    swView.Position = new double[] { 0, boxes["F1"][1]/ 2 };//{ 0, boxes["F1"][1] - ((boxes["F1"][3] - boxes["F1"][1]) / 2) };//{ halfx, boxes["F1"][1] - ((boxes["F1"][1]) / 2) - Math.Abs(deltay) };//{ halfx, boxes["F1"][1] - ((boxes["F1"][3] - boxes["F1"][1]) / 2) - Math.Abs(deltay) };
                }
            }
        }
        private void AutoDimensionDrawing2(ModelDoc2 swModel, bool many )
        {
            bool del3List = false;
            var thrdList = new List<string>();
            int shi = 0;
            isValidXml = true;

            var swDrawing = (DrawingDoc)swModel;
            if (swModel.GetCustomInfoValue("", "AutoDim") == "No")
            {
                if (many)
                {
                    MessageBox.Show(@"Если хотите образмерить чертеж, смените No на Yes в поле 'AutoDim' свойств данного чертежа",
                        @"MrDoors", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                isValidXml = false;
            }
            if (swModel.GetCustomInfoValue("", "MakeCNCprog") == "Yes")
            {
                _createProgramm = true;
            }
            if (!isValidXml && !_createProgramm) // если не надо создавать программу и авто дим = но, то образмеривать просто не нужно.
                return;
            swModel.Extension.SetUserPreferenceDouble((int)swUserPreferenceDoubleValue_e.swDetailingDimToDimOffset,
                                                      (int)swUserPreferenceOption_e.swDetailingNoOptionSpecified, 0.006);
            swModel.Extension.SetUserPreferenceDouble((int)swUserPreferenceDoubleValue_e.swDetailingObjectToDimOffset,
                                                      (int)swUserPreferenceOption_e.swDetailingNoOptionSpecified, 0.010);
            
            Dictionary<string, bool> listSide;
            string targetModelPath = null;
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(_swAdd.RootModel.GetPathName()), "fpTime.txt")))
            {
                //MessageBox.Show("Образмеривание детали может пройти некорректно! Для корректного образмеривания детали необходимо произвести Окончательную обработку заказа.");
                throw new Exception("Образмеривание детали может пройти некорректно! Для корректного образмеривания детали необходимо произвести Окончательную обработку заказа.");
            }
            else
            {
                //просто взять Sketch Number
              
               string fnameWithoutExt = Path.GetFileNameWithoutExtension(swModel.GetPathName());
                fnameWithoutExt = fnameWithoutExt.Substring(fnameWithoutExt.Length - 4, 4);
                SwDMDocument8 swDoc = null;
                SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
                SwDmDocumentOpenError oe;
                SwDmCustomInfoType type;
                if (!(fnameWithoutExt[0] == '#' && (fnameWithoutExt[3] == 'P' || fnameWithoutExt[3] == 'p')))
                {
              
                    swDoc = (SwDMDocument8) swDocMgr.GetDocument(Path.ChangeExtension(swModel.GetPathName(), "SLDASM"),SwDmDocumentType.swDmDocumentAssembly, true, out oe);
                   
                    
                }
                else
                {
                    object brokenRefVar;
                    SwDMSearchOption src = swDocMgr.GetSearchOptionObject();

                    var swDocDraw = (SwDMDocument8)swDocMgr.GetDocument(swModel.GetPathName(), SwDmDocumentType.swDmDocumentDrawing, true, out oe);
                    var varRef = (object[])swDocDraw.GetAllExternalReferences2(src, out brokenRefVar);
                    swDocDraw.CloseDoc();
                    targetModelPath = (string)varRef[0];
                    swDoc = (SwDMDocument8)swDocMgr.GetDocument(targetModelPath, SwDmDocumentType.swDmDocumentAssembly, true, out oe);
                }
                if (swDoc != null)
                {
                    var prop = swDoc.GetCustomProperty("Sketch Number", out type);
                    if (string.IsNullOrEmpty(prop) || prop == "0")
                        throw new Exception("Образмеривание детали прервано, т.к. может пройти некорректно ! Для корректного образмеривания детали необходимо произвести Окончательную обработку заказа.");
                    swDoc.CloseDoc();
                }
                _swAdd.SetModelProperty(swModel, "WasMesure", string.Empty, swCustomInfoType_e.swCustomInfoYesOrNo, "Yes", true);
            }
            string pathXml = WriteXmlFile(swModel, isValidXml, targetModelPath);

            bool isNeededSheetNumber = PrepareDrawingDoc(swModel, out listSide);
            bool dimOnlyNew = false;
            if (swModel.GetCustomInfoValue("", "DimOnlyNew") == "Yes")
                dimOnlyNew = true;
            var vSheetNames = (string[])swDrawing.GetSheetNames();
            var rootNode = _node;
            bool atLeastOneF1View = false;


            foreach (var vSheetName in vSheetNames)
            {
                bool iftherewasAhole = false;
                XmlElement element = null;
                XmlElement sheetNode = null;
                KeyValuePair<string,string> tableNameAttribute = new KeyValuePair<string, string>();
                if (_createProgramm && !string.IsNullOrEmpty(pathXml))
                {
                    element = _cxml.CreateElement("Sheet");
                    element.SetAttribute("Name", vSheetName);
                    _node = rootNode.AppendChild(element);
                    sheetNode = element;
                }

                double vScale = 0;
                var type = new List<string>();
                var listSize = new List<SizeForDim>();
                swDrawing.ActivateSheet(vSheetName);
                swModel.Extension.SelectByID2(vSheetName, "SHEET", 0, 0, 0, true,
                                              (int)swSelectionMarkAction_e.swSelectionMarkAppend, null, 0);
                swModel.ViewZoomToSelection();
                swModel.ClearSelection();
                var swSheet = (Sheet)swDrawing.GetCurrentSheet();
                var swViews = (object[])swSheet.GetViews();
                bool side = shi == 1;
                if (isNeededSheetNumber && listSide.ContainsKey(vSheetName.Substring(vSheetName.Length - 1)))
                    side = listSide[vSheetName.Substring(vSheetName.Length - 1)];
                if (vSheetName.ToUpper().Contains("FACE"))
                    side = true;
                if (vSheetName.ToUpper().Contains("BACK"))
                    side = false;

                if (swViews != null)
                {
                    var rootViewElement = _node;
                    if (Properties.Settings.Default.ViewsBeforeDimen)
                        ReplaceViews(swViews, swDrawing);
                    foreach (var t in swViews)
                    {
                        #region Образмеривание вида
                        var swView = (View) t;
                        swModel.ClearSelection2(true);

                        const string expr = "^F[1-6]$";

                        Match isMatch = Regex.Match(swView.Name, expr, RegexOptions.IgnoreCase);

                        if (_createProgramm && swView.Name.ToLower().Contains("const"))
                        {

                            //Logging.Log.Instance.Fatal(@"На данный чертеж программа не будет создана! " + swModel.GetPathName() + "swView.Name = " + swView.Name);
                            //MessageBox.Show(@"На данный чертеж программа не будет создана!", @"MrDoors",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                        }

                        if (!(isMatch.Success || swView.Name.Contains("Чертежный вид")) ||
                            swView.Name.ToLower().Contains("const") ||
                            swView.Type == (int)swDrawingViewTypes_e.swDrawingDetailView) continue;


                        if (_createProgramm && !isMatch.Success)
                        {
                            _createProgramm = false;
                            //Logging.Log.Instance.Fatal(@"На данный чертеж программа не будет создана! " + swModel.GetPathName() + "swView.Name = " + swView.Name);
                            //MessageBox.Show(@"На данный чертеж программа не будет создана!", @"MrDoors",
                            //                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }

                        swDrawing.ActivateView(swView.GetName2());
                        swView.UseSheetScale = 0;

                        var list = HideUnusedComponents(swView, dimOnlyNew);

                        try
                        {
                            vScale = ((double[]) swView.ScaleRatio)[1];
                            swView.SetDisplayMode3(false, (int) swDisplayMode_e.swFACETED_HIDDEN, true, true);

                            #region Процесс образмеривания



                            using (var d = new DimensionView(_swApp, BlockPositionExtension.FromBool(side), dimOnlyNew))
                            {

                                d.DimView(side);
                                if (!iftherewasAhole)
                                    iftherewasAhole = d.IsHole;
                                if (!isNeededSheetNumber && !side && !iftherewasAhole)
                                    del3List = true;
                                else
                                {
                                    if (isNeededSheetNumber && !iftherewasAhole)
                                    {
                                        del3List = true;
                                        thrdList.Add(vSheetName);
                                    }
                                }
                                foreach (var tp in d.List)
                                {
                                    if (!type.Contains(tp))
                                        type.Add(tp);
                                }
                                listSize.Add(d.AddSize);

                                #region Запись данных в xml файл

                                if (element != null)
                                {
                                    element = _cxml.CreateElement("View");
                                    element.SetAttribute("Name", swView.Name);
                                    if (swView.Name == "F1")
                                    {
                                        atLeastOneF1View = true;

                                            SwDmDocumentOpenError oe;
                                            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
                                            var swDoc = (SwDMDocument8)swDocMgr.GetDocument(Path.ChangeExtension(swModel.GetPathName(), "SLDASM"),
                                                                                             SwDmDocumentType.swDmDocumentAssembly, true, out oe);
                                            if (swDoc != null)
                                            {
                                                var cit = new SwDmCustomInfoType();
                                                string[] names = swDoc.GetCustomPropertyNames();
                                                string extFeats = null,faner11 = null,faner12 = null,faner21 = null,faner22 = null;

                                                if (names.Contains("ExtFanerFeats"))
                                                    extFeats = swDoc.GetCustomProperty("ExtFanerFeats", out cit);
                                                double angle = 57.29577951308232*swView.Angle; //(180/П)
                                                if (Math.Abs(angle) < 0.000001 || Math.Abs(angle + 90) < 0.000001 || Math.Abs(angle - 270) < 0.000001 || Math.Abs(angle - 180) < 0.000001 || Math.Abs(angle - 90) < 0.000001) //!string.IsNullOrEmpty(extFeats) && extFeats == "Yes" &&
                                                {
                                                    if (names.Contains("Faner11"))
                                                        faner11 = swDoc.GetCustomProperty("Faner11", out cit);
                                                    if (names.Contains("Faner12"))
                                                        faner12 = swDoc.GetCustomProperty("Faner12", out cit);
                                                    if (names.Contains("Faner21"))
                                                        faner21 = swDoc.GetCustomProperty("Faner21", out cit);
                                                    if (names.Contains("Faner22"))
                                                        faner22 = swDoc.GetCustomProperty("Faner22", out cit);

                                                    var tmpElem = _cxml.CreateElement("Comment");
                                                    string comment = FrmEdge.GetCommentFromProperties(faner11,faner12,faner21,faner22, angle, _swAdd,swModel);
                                                    double angle2 = angle + 90;
                                                    double angle4 = angle + 180;
                                                    double angle3 = angle + 270;
                                                    if (angle2 > 270)
                                                        angle2 = angle2%360;
                                                    if (angle3 > 270)
                                                        angle3 = angle3 % 360;
                                                    if (angle4 > 270)
                                                        angle4 = angle4 % 360;
                                                    string comment2 = FrmEdge.GetCommentFromProperties(faner11, faner12, faner21, faner22, angle2, _swAdd, swModel);
                                                    string comment3 = FrmEdge.GetCommentFromProperties(faner11, faner12, faner21, faner22, angle3, _swAdd, swModel);
                                                    string comment4 = FrmEdge.GetCommentFromProperties(faner11, faner12, faner21, faner22, angle4, _swAdd, swModel);
                                                    tmpElem.SetAttribute("Rot270",comment3);
                                                    tmpElem.SetAttribute("Rot180",comment4);
                                                    tmpElem.SetAttribute("Rot90", comment2);
                                                    tmpElem.SetAttribute("Rot0", comment);
                                                    rootNode.PrependChild(tmpElem);
                                                }

                                            }

                                        //swDoc.CloseDoc();

                                        //int warnings = 0;
                                        //int errors = 0;
                                        //var swModelDoc = _swApp.OpenDoc6(Path.ChangeExtension(swModel.GetPathName(), "SLDASM"), (int)swDocumentTypes_e.swDocASSEMBLY,
                                        //                  (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", errors,
                                        //                  warnings);
                                        //if (!string.IsNullOrEmpty(swModelDoc.CustomInfo2["", "ExtFanerFeats"]) && swModelDoc.CustomInfo2["", "ExtFanerFeats"] == "Yes")
                                        //{
                                        //    var tmpElem = _cxml.CreateElement("Comment");
                                        //    string comment = FrmEdge.GetComment(swModelDoc, swView.Angle, _swAdd);
                                        //    tmpElem.SetAttribute("Rot270", string.Empty);
                                        //    tmpElem.SetAttribute("Rot90", string.Empty);
                                        //    tmpElem.SetAttribute("Rot0", comment);
                                        //    rootNode.PrependChild(tmpElem);
                                        //}


                                    }
                                    if (_node != null)
                                    {
                                        _node = rootViewElement.AppendChild(element);
                                        switch (d.Side)
                                        {
                                            case BlockPosition.LeftTopToRightBottom:
                                                
                                                if (swView.Name == "F1")
                                                    tableNameAttribute = new KeyValuePair<string, string>("F1","J");
                                                if (swView.Name == "F6")
                                                {
                                                    if (tableNameAttribute.Key!="F1") // F1 -  приоритетнее
                                                        tableNameAttribute = new KeyValuePair<string, string>("F6", "B");
                                                }
                                                if (string.IsNullOrEmpty(tableNameAttribute.Key))
                                                {
                                                    tableNameAttribute = new KeyValuePair<string, string>("none","J");
                                                }
                                                break;
                                            case BlockPosition.RightTopToLeftBottom:
                                                if (swView.Name == "F1")
                                                    tableNameAttribute = new KeyValuePair<string, string>("F1","B");
                                                if (swView.Name == "F6")
                                                {
                                                    if (tableNameAttribute.Key!="F1") // F1 -  приоритетнее
                                                        tableNameAttribute = new KeyValuePair<string, string>("F6", "J");
                                                }
                                                if (string.IsNullOrEmpty(tableNameAttribute.Key))
                                                {
                                                    tableNameAttribute = new KeyValuePair<string, string>("none", "B");
                                                }
                                                break;
                                            case BlockPosition.LeftBottomToRightTop:
                                            case BlockPosition.RigthBottomToLeftTop:
                                                //MessageBox.Show(
                                                //    "В этом чертеже начало координат находится внизу. Программа может быть создана некорректно!",
                                                //    @"MrDoors", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                                // element = _cxml.CreateElement("Table");
                                                // element.SetAttribute("Name", "Не удалось определить наименование стола!");
                                                //_node=_node.AppendChild(element);
                                                break;

                                        }
                                    }

                                    int i = 0;
                                    string id = "id" + i;
                                    element = _cxml.CreateElement(id);

                                    element.SetAttribute("X", Math.Round(d.X).ToString(CultureInfo.CreateSpecificCulture("ru-RU")));
                                    element.SetAttribute("Y", Math.Round(d.Y).ToString(CultureInfo.CreateSpecificCulture("ru-RU")));
                                    if (swView.Name == "F1" || swView.Name == "F6")
                                        element.SetAttribute("Z", _z.ToString(CultureInfo.CreateSpecificCulture("ru-RU")));
                                    if (_node != null)
                                        _node.AppendChild(element);

                                    foreach (var ls in d.ListSize)
                                    {
                                        i++;
                                        id = "id" + i;
                                        element = _cxml.CreateElement(id);



                                        element.SetAttribute("X", ls.X.ToString(CultureInfo.CreateSpecificCulture("ru-RU")));
                                        element.SetAttribute("Y",  ls.Y.ToString(CultureInfo.CreateSpecificCulture("ru-RU")));
                                        element.SetAttribute("Diameter",  ls.Diameter.ToString(CultureInfo.CreateSpecificCulture("ru-RU")));
                                        element.SetAttribute("Depth",ls.Depth.ToString(CultureInfo.CreateSpecificCulture("ru-RU")));

                                        if (_node != null)
                                            _node.AppendChild(element);
                                    }
                                    _node = _node.ParentNode;
                                }

                                #endregion
                            }

                            #endregion

                            swView.SetDisplayMode3(false, (int) swDisplayMode_e.swFACETED_HIDDEN_GREYED, true, true);

                            swDrawing.ActivateSheet(vSheetName);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(@"Ошибка при образмеривании! " + e.Message, @"MrDoors",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            if (dimOnlyNew)
                                ShowHiddenComponents(list);
                        }

                        #endregion
                    }
                }
                if (type.Count != 0)
                    LegendMaker(swModel, swDrawing, type, vScale);
                if (!string.IsNullOrEmpty(tableNameAttribute.Value))
                    sheetNode.SetAttribute("TableName", tableNameAttribute.Value);
                if (Properties.Settings.Default.ScaleWhenDimen && !del3List && shi != 0)
                    AutoScaleSheet(listSize, swSheet, vScale, side);
                shi++;
                if(_node!=null)
                    _node = _node.ParentNode;
                AutoArrangeDimentions(vSheetName,dimOnlyNew);
            }
            swModel.EditRebuild3();
            if (shi == 3)
            {
                if (del3List)
                {
                    if (isNeededSheetNumber && thrdList.Count > 0)
                        foreach (var sh in thrdList)
                            swModel.Extension.SelectByID2(sh, "SHEET", 0, 0, 0, true, 0, null, 0);
                    else
                    {
                     if (!swModel.Extension.SelectByID2("Лист3", "SHEET", 0, 0, 0, false, 0, null, 0))
                     {
                         swModel.Extension.SelectByID2("Back3", "SHEET", 0, 0, 0, false, 0, null, 0);
                     }
                    }
                    swModel.DeleteSelection(true);
                }
                else
                    SheetNumering(swModel, swDrawing);
            }
            swModel.ForceRebuild3(false);
            bool writeXml = true;
            if (atLeastOneF1View)
            {
                try
                {
                    writeXml=SomeLogicChanges(swModel);
                }
                catch(Exception e)
                {
                    if (swModel.GetPathName() != null)
                        Logging.Log.Instance.Fatal(e, "Ошибка при применении логики к XML. " + swModel.GetPathName());
                    else
                        Logging.Log.Instance.Fatal(e, "Ошибка при применении логики к XML. ");
                        
                }
                if (writeXml)
                    StopWriteXml(pathXml);
            }

            return;
        }
        private void DrawRenumering()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            List<ModelDrawingNumber> list = new List<ModelDrawingNumber>();
            //if (<DrawRenumering>o__SiteContainerb.<>p__Sitec == null)
            //{
            //    <DrawRenumering>o__SiteContainerb.<>p__Sitec = CallSite<Func<CallSite, object, ModelDoc2>>.Create(Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(ModelDoc2), typeof(DimensionDraft)));
            //}
            //ModelDoc2 inModel = <DrawRenumering>o__SiteContainerb.<>p__Sitec.Target(<DrawRenumering>o__SiteContainerb.<>p__Sitec, this._swApp.ActiveDoc);
            ModelDoc2 inModel = _swApp.ActiveDoc;
            try
            {
                LinkedList<ModelDoc2> list2;
                if (this._swAdd.GetAllUniqueModels(inModel, out list2))
                {
                    foreach (ModelDoc2 doc2 in list2)
                    {
                        string pathName;
                        DateTime creationTime;
                        try
                        {
                            pathName = doc2.GetPathName();
                        }
                        catch
                        {
                            continue;
                        }
                        string str2 = Path.GetDirectoryName(pathName) + @"\" + Path.GetFileNameWithoutExtension(pathName) + ".SLDDRW";
                        if (File.Exists(str2))
                        {
                            if (!DateTime.TryParse(doc2.get_CustomInfo2("", "CreationTime"), out creationTime))
                            {
                                creationTime = File.GetCreationTime(str2);
                                this._swAdd.SetModelProperty(doc2, "CreationTime", "", swCustomInfoType_e.swCustomInfoText, creationTime.ToString(), true);
                            }
                            list.Add(new ModelDrawingNumber(str2, doc2, creationTime));
                        }
                        else if (doc2.get_CustomInfo2("", "Required Draft") == "Yes")
                        {
                            MessageBox.Show("Для детали " + pathName + " не был скопирован чертеж при отрыве от библиотеки. Чертеж будет скопирован повторно.", "Ошибка при копировании чертежа.", MessageBoxButtons.OK);
                            this._swAdd.CopyDrawing(doc2);
                            if (File.Exists(str2))
                            {
                                creationTime = File.GetCreationTime(str2);
                                this._swAdd.SetModelProperty(doc2, "CreationTime", "", swCustomInfoType_e.swCustomInfoText, creationTime.ToString(), true);
                                list.Add(new ModelDrawingNumber(str2, doc2, creationTime));
                            }
                        }
                    }
                    list.Sort(delegate (ModelDrawingNumber x, ModelDrawingNumber y) {
                        return x.Time.CompareTo(y.Time);
                    });
                    for (int i = 0; i < list.Count; i++)
                    {
                        string val = (i + 1).ToString();
                        ModelDrawingNumber number = list[i];
                        this._swAdd.SetModelProperty(number.Model, "Sketch Number", "", swCustomInfoType_e.swCustomInfoText, val, false);
                        if (!dictionary.ContainsKey(Path.GetFileName(number.DrwModel)))
                        {
                            dictionary.Add(Path.GetFileName(number.DrwModel), i + 1);
                        }
                    }
                    string path = Path.Combine(Path.GetDirectoryName(this._swAdd.SwModel.GetPathName()), "Программы");
                    if (Directory.Exists(path))
                    {
                        foreach (string str5 in Directory.GetFiles(path, "*.xml", SearchOption.TopDirectoryOnly))
                        {
                            File.Move(str5, str5 + "tmp");
                        }
                        foreach (string str5 in Directory.GetFiles(path, "*.xmltmp", SearchOption.TopDirectoryOnly))
                        {
                            XmlDocument document = new XmlDocument();
                            document.Load(str5);
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(document.ChildNodes[0].Attributes["Name"].Value);
                            if (dictionary.ContainsKey(fileNameWithoutExtension + ".SLDDRW"))
                            {
                                File.Move(str5, str5.Substring(0, str5.Length - 8) + dictionary[fileNameWithoutExtension + ".SLDDRW"] + ".xml");
                            }
                        }
                        foreach (string str5 in Directory.GetFiles(path, "*.xmltmp", SearchOption.TopDirectoryOnly))
                        {
                            File.Delete(str5);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("Ошибка при перенумерации чертежей, после удаления одного из них(8504F_Панель вкладная 00AA)\nНумерация чертежей.\\n" + exception.Message, this._swAdd.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        private bool SomeLogicChanges(ModelDoc2 swModel)
        {
            XmlNodeList views = null;
            XmlNode F1View = null, F6View = null;
            XmlNodeList list = null;
            if (this._cxml.ChildNodes[0].Attributes["Name"].Value.Contains("8504F_Панель вкладная 00AA") && Properties.Settings.Default.DeleteDraftIfStandart)
            {
                foreach (XmlNode node3 in this._cxml.ChildNodes[0].ChildNodes)
                {
                    list = node3.SelectNodes("View");
                    if (list.Count > 0)
                    {
                        foreach (XmlNode node4 in list)
                        {
                            if (node4.Attributes["Name"].Value == "F1")
                            {
                                double result = 0.0;
                                double num2 = 0.0;
                                if ((double.TryParse(node4.ChildNodes[0].Attributes["Y"].Value, out result) && double.TryParse(node4.ChildNodes[1].Attributes["Y"].Value, out num2)) && ((((result >= 140.0) && (result < 601.0)) && (result == (num2 + 50.0))) || (((result > 69.0) && (result < 140.0)) && (result == (num2 * 2.0)))))
                                {
                                    bool flag2 = true;
                                    if (MessageBox.Show("Данная деталь совпадает со стандртной типологией, на которую существует программа на обработку, поэтому чертеж на нее является излишним.Удалить чертеж на данную деталь ?", "MrDoors", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                                    {
                                        break;
                                    }
                                    try
                                    {
                                        string pathName = swModel.GetPathName();
                                        string path = string.Format("{0}{1}", pathName.Substring(0, pathName.Length - 6), "SLDASM");
                                        if (File.Exists(path))
                                        {
                                            int errors = 0;
                                            int warnings = 0;
                                            ModelDoc2 model = this._swApp.OpenDoc6(path, 2, 1, "", ref errors, ref warnings);
                                            this._swAdd.SetModelProperty(model, "Required Draft", "", swCustomInfoType_e.swCustomInfoText, "No", true);
                                            model.Save2(true);
                                        }
                                        if (File.Exists(pathName))
                                        {
                                            this._swApp.CloseDoc(pathName);
                                            File.Delete(pathName);
                                        }
                                        this.DrawRenumering();
                                        flag2 = false;
                                    }
                                    catch (Exception)
                                    {
                                    }
                                    return flag2;
                                }
                            }
                        }
                    }
                }
            }
            List<XmlNode> allViews = new List<XmlNode>();
            foreach (XmlNode sheet in _cxml.ChildNodes[0].ChildNodes)
            {
                views = sheet.SelectNodes("View");
                if (views.Count > 0)
                {
                    foreach (XmlNode view in views)
                    {
                        allViews.Add(view);
                        if (view.Attributes["Name"].Value == "F1")
                            F1View = view;
                        if (view.Attributes["Name"].Value == "F6")
                            F6View = view;
                    }
                }
            }

            if (F1View != null)
            {
                double currentDepth = 0;
                try
                {
                    double.TryParse(F1View.ChildNodes[0].Attributes["Z"].Value, out currentDepth);
                }
                catch
                { }
                foreach (XmlNode node in F1View.ChildNodes)
                {
                    string X = node.Attributes["X"].Value;
                    string Y = node.Attributes["Y"].Value;
                    //string xPath = string.Format("*[@X={0} and @Y={1}]", X, Y);
                    List<XmlNode> correlateNodes = new List<XmlNode>();
                    foreach (XmlNode child in F1View.ChildNodes)
                    {
                        if (child.Attributes["Depth"] == null)
                            continue;
                        if (child.Attributes["X"].Value == X && child.Attributes["Y"].Value == Y)
                            correlateNodes.Add(child);
                    }
                    //var correlateNodes = F1View.SelectNodes(xPath);
                    XmlNode sequentiallyNode = null;
                    if (correlateNodes.Count > 1)
                    {
                        //тут мы определили что есть минимум 2 присадки с одинаковыми координатами
                        //проверить что хотябы одна из них сквозная
                        foreach (XmlNode corrNode in correlateNodes)
                        {
                            if (corrNode.Attributes["Depth"].Value == "0")
                            {
                                sequentiallyNode = corrNode;
                                break;
                            }
                        }
                        if (sequentiallyNode == null)
                            goto second; // есть более одной присадки с совпадающими координатами, но ниодна из них не сквозная. Выходим из цикла
                        if (F6View == null) // если нет F6 то ее придется создать
                            F6View = CreateF("F6", _cxml, F1View);
                        MoveToOppositePlane("F6", sequentiallyNode, F1View, F6View, _cxml);
                    }
                second:
                    if (currentDepth == 0 || node.Name == "id0" || F6View == null)
                        continue;
                    correlateNodes = new List<XmlNode>();
                    foreach (XmlNode child in F6View.ChildNodes)
                    {
                        if (child.Attributes["X"].Value == X && child.Attributes["Y"].Value == Y)
                            correlateNodes.Add(child);
                    }
                    //correlateNodes = F6View.SelectNodes(xPath);
                    XmlNode unSequentiallyNode = null;
                    if (node.Attributes["Depth"].Value == "0")
                        sequentiallyNode = node;
                    else
                        unSequentiallyNode = node;
                    if (correlateNodes.Count > 0)
                    {
                        foreach (XmlNode corrNode in correlateNodes)
                        {
                            if (corrNode.Attributes["Depth"].Value == "0")
                                sequentiallyNode = corrNode;
                            else
                                unSequentiallyNode = corrNode;
                        }
                        if (sequentiallyNode == null)
                            continue; // есть более одной присадки с совпадающими координатами, но ниодна из них не сквозная. Выходим из цикла
                        double unSequentiallyNodeDepth;
                        if (currentDepth != 0 && unSequentiallyNode!=null && double.TryParse(unSequentiallyNode.Attributes["Depth"].Value, out unSequentiallyNodeDepth))
                        {
                            sequentiallyNode.Attributes["Depth"].Value = ((int)(currentDepth - unSequentiallyNodeDepth + 5)).ToString();
                        }
                    }
                }
            }
            if (F6View != null)
            {
                double currentDepth = 0;
                try
                {
                    double.TryParse(F6View.ChildNodes[0].Attributes["Z"].Value, out currentDepth);
                }
                catch
                { }
                foreach (XmlNode node in F6View.ChildNodes)
                {
                    string X = node.Attributes["X"].Value;
                    string Y = node.Attributes["Y"].Value;
                    //string xPath = string.Format("*[@X={0} and @Y={1}]", X, Y);
                    List<XmlNode> correlateNodes = new List<XmlNode>();
                    foreach (XmlNode child in F6View.ChildNodes)
                    {
                        if (child.Attributes["Depth"] == null)
                            continue;
                        if (child.Attributes["X"].Value == X && child.Attributes["Y"].Value == Y)
                            correlateNodes.Add(child);
                    }
                    //var correlateNodes = F6View.SelectNodes(xPath);
                    XmlNode sequentiallyNode = null;
                    if (correlateNodes.Count > 1)
                    {
                        //тут мы определили что есть минимум 2 присадки с одинаковыми координатами
                        //проверить что хотябы одна из них сквозная

                        foreach (XmlNode corrNode in correlateNodes)
                        {
                            if (corrNode.Attributes["Depth"].Value == "0")
                            {
                                sequentiallyNode = corrNode;
                                continue;
                            }
                        }
                        if (sequentiallyNode == null)
                            goto third; // есть более одной присадки с совпадающими координатами, но ниодна из них не сквозная. Выходим из цикла
                        if (F1View == null) // если нет F6 то ее придется создать
                            F1View = CreateF("F1", _cxml, F6View);
                        MoveToOppositePlane("F1", sequentiallyNode, F1View, F6View, _cxml);

                    }
                third:
                    if (currentDepth == 0 || node.Name == "id0" || F1View == null)
                        continue;
                    correlateNodes = new List<XmlNode>();
                    foreach (XmlNode child in F1View.ChildNodes)
                    {
                        if (child.Attributes["X"].Value == X && child.Attributes["Y"].Value == Y)
                            correlateNodes.Add(child);
                    }
                    //correlateNodes = F1View.SelectNodes(xPath);
                    XmlNode unSequentiallyNode = null;
                    if (node.Attributes["Depth"].Value == "0")
                        sequentiallyNode = node;
                    else
                        unSequentiallyNode = node;
                    if (correlateNodes.Count > 0)
                    {
                        foreach (XmlNode corrNode in correlateNodes)
                        {
                            if (corrNode.Attributes["Depth"].Value == "0")
                                sequentiallyNode = corrNode;
                            else
                                unSequentiallyNode = corrNode;
                        }
                        if (sequentiallyNode == null)
                            continue; // есть более одной присадки с совпадающими координатами, но ниодна из них не сквозная. Выходим из цикла
                        double unSequentiallyNodeDepth;
                        if (currentDepth != 0 && unSequentiallyNode!=null && double.TryParse(unSequentiallyNode.Attributes["Depth"].Value, out unSequentiallyNodeDepth))
                        {
                            sequentiallyNode.Attributes["Depth"].Value = ((int)(currentDepth - unSequentiallyNodeDepth + 5)).ToString();
                        }
                    }
                }
            }
            foreach (XmlNode view in allViews)
            {
                if (view != null && view.ChildNodes.Count > 1)
                {
                    XmlNode newView;
                    List<Tuple<double, double, XmlNode>> nodeList = new List<Tuple<double, double, XmlNode>>();
                    newView = view.CloneNode(false);
                    foreach (XmlNode node in view.ChildNodes)
                    {
                        nodeList.Add(new Tuple<double, double, XmlNode>(double.Parse(node.Attributes["Y"].Value),
                                                                        double.Parse(node.Attributes["X"].Value), node));
                    }
                    if (newView != null && newView.Attributes != null && view != null && view.Attributes != null && view.Attributes["Name"]!=null)
                    {
                        view.RemoveAll();
                        foreach (XmlAttribute attribute in newView.Attributes)
                        {
                            XmlAttribute xKey = _cxml.CreateAttribute(attribute.Name);
                            xKey.Value = attribute.Value;
                            view.Attributes.Append(xKey);
                        }
                        if (view.Attributes["Name"].Value == "F1" || view.Attributes["Name"].Value == "F6")
                            nodeList =nodeList.OrderBy(x => x.Item3.Name != "id0").ThenBy(x => x.Item1).ThenBy(x => x.Item2).ToList();
                        else
                            nodeList =nodeList.OrderBy(x => x.Item3.Name != "id0").ThenBy(x => x.Item2).ThenBy(x => x.Item1).ToList();
                        int i = 0;
                        foreach (var tuple in nodeList)
                        {
                            //tuple.Item3.InnerXml = tuple.Item3.InnerXml.Replace(tuple.Item3.Name, string.Format("id{0}", i));
                            XmlNode newNode = _cxml.CreateElement(string.Format("id{0}", i));
                            if (tuple.Item3 != null && tuple.Item3.Attributes != null && newNode.Attributes!=null)
                            {
                                foreach (XmlAttribute attribute in tuple.Item3.Attributes)
                                {
                                    XmlAttribute xKey = _cxml.CreateAttribute(attribute.Name);
                                    xKey.Value = attribute.Value;
                                    newNode.Attributes.Append(xKey);
                                }
                            }
                            i++;
                            view.AppendChild(newNode);
                        }
                    }
                }
            }
            return true;
        }
        private static XmlNode CreateF(string planeToCreate, XmlDocument currXml, XmlNode oppositeView)
        {
            var newNode = currXml.CreateElement("View");
            newNode.SetAttribute("Name", planeToCreate);
            var result = oppositeView.ParentNode.AppendChild(newNode);
            var sizes = currXml.CreateElement("id0");
            XmlNode sizeNode = null;
            sizeNode = oppositeView.ChildNodes[0];


            if (sizeNode != null)
            {
                foreach (XmlAttribute atribute in sizeNode.Attributes)
                {
                    sizes.SetAttribute(atribute.Name, atribute.Value);
                }

                result.AppendChild(sizes);
            }
            return result;
        }
        private static void MoveToOppositePlane(string moveToPlane, XmlNode node, XmlNode F1View, XmlNode F6View, XmlDocument currXml)
        {
            if (moveToPlane == "F6")
            {
                F1View.RemoveChild(node);
                var tmpNode = currXml.CreateElement("id" + F6View.ChildNodes.Count.ToString());

                foreach (XmlAttribute attribute in node.Attributes)
                {
                    tmpNode.SetAttribute(attribute.Name, attribute.Value);
                }
                F6View.AppendChild(tmpNode);
            }
            if (moveToPlane == "F1")
            {
                F6View.RemoveChild(node);
                var tmpNode = currXml.CreateElement("id" + F1View.ChildNodes.Count.ToString());

                foreach (XmlAttribute attribute in node.Attributes)
                {
                    tmpNode.SetAttribute(attribute.Name, attribute.Value);
                }
                F1View.AppendChild(tmpNode);
            }
        }

        private void AutoArrangeDimentions(string swSheetName,bool dimOnlyNew)
        {
            if (!dimOnlyNew)
                return;
            if (!Properties.Settings.Default.AutoArrangeDimension)
                return;
            var swModel = _swApp.IActiveDoc2;

            var swDrawing = (DrawingDoc)swModel;
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
                        }
                        object objDimName;
                        try
                        {
                            objDimName  = swView.GetDimensionIds4();
                        }
                        catch (Exception)
                        {
                            return;
                        }
                        
                        if (objDimName != null)
                        {
                            var objDimNames = (object[])objDimName;

                            for (int i = 0; i <= objDimNames.Length;i++ )
                                {
                                    try
                                    {
                                        var swDimName = (string)objDimNames[i];
                                        swModel.Extension.SelectByID2(swDimName, "DIMENSION", 0, 0, 0, true, i, null, 0);
                                        i++;
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                }
                        }
                        swModel.Extension.SetUserPreferenceDouble((int)swUserPreferenceDoubleValue_e.swDetailingObjectToDimOffset, 0, 0.002);
                        swModel.Extension.SetUserPreferenceDouble((int)swUserPreferenceDoubleValue_e.swDetailingDimToDimOffset, 0, 0.001);
                        _swAdd.SwApp.RunCommand((int)swCommands_e.swCommands_AutoArrangeDimension, "");

                    }
                }
            swModel.ForceRebuild3(false);

        }

        private string WriteXmlFile(ModelDoc2 swModel, bool isValidXml, string targetModelPath = null)
        {
            string ret = "";
            if (_createProgramm)
            {
                var path = Path.GetDirectoryName(_swAdd.RootModel.GetPathName());
                path = path + @"\Программы\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string modelName = Path.GetFileName(swModel.GetPathName());

                ret = path + modelName + ".xml";

                string fnameWithoutExt = Path.GetFileNameWithoutExtension(swModel.GetPathName());
                fnameWithoutExt = fnameWithoutExt.Substring(fnameWithoutExt.Length - 4, 4);
                string underlyingModelFileName;
                if (fnameWithoutExt[0] == '#' && (fnameWithoutExt[3] == 'P' || fnameWithoutExt[3] == 'p'))
                {
                    underlyingModelFileName = targetModelPath;//@"D:\_SWLIB_BACKUP\ШКАФЫ-КУПЕ\Каркасные детали\ДСП 16 мм\8504F_Панель вкладная 000A#17P.SLDASM";
                }
                else
                    underlyingModelFileName = swModel.GetPathName().Replace("SLDDRW", "SLDASM");
                ModelDoc2 underlyingModel =(ModelDoc2) _swApp.OpenDoc(underlyingModelFileName, (int) swDocumentTypes_e.swDocASSEMBLY);

                string sketchNumber = underlyingModel.GetCustomInfoValue("", "Sketch Number");
                string orderNumber = underlyingModel.GetCustomInfoValue("", "Order Number");
                _swApp.CloseDoc(underlyingModelFileName);
                if (!(string.IsNullOrEmpty(sketchNumber) || string.IsNullOrEmpty(orderNumber)))
                    ret = Path.Combine(path, orderNumber + "_" + sketchNumber + ".xml");
                    
                if (File.Exists(ret))
                {
                    File.Delete(ret);
                }
                try
                {
                    _cxml = new XmlDocument();
                    var element = _cxml.CreateElement("Model");
                    //modelName = Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.UTF8.GetBytes(modelName)));
                    element.SetAttribute("Name", modelName);
                    element.SetAttribute("CNCValid", isValidXml.ToString());
                    element.SetAttribute("vAddInn", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    element.SetAttribute("vLib", Properties.Settings.Default.PatchVersion);
                    _node = _cxml.AppendChild(element);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(@"Ошибка: " + ex.Message);
                }
            }
            return ret;
        }

        private void StopWriteXml(string path)
        {
            if (path == "")
                return;
            if (_createProgramm)
                _cxml.Save(path);
        }

        private static void AutoScaleSheet(IEnumerable<SizeForDim> sizeForDimL, Sheet swSheet, double scale, bool side)
        {
            double xAdding = 0, yAdding = 0, x = 0, y = 0;
            View baseView = null;
            var dictAlign = new Dictionary<View, int>();

            if (sizeForDimL.Count() > 1)
                foreach (var sizeForDim in sizeForDimL)
                {
                    int align = sizeForDim.View.GetAlignment();
                    if (align == 1)
                        baseView = sizeForDim.View;
                    sizeForDim.View.RemoveAlignment();
                }

            var listPosition = sizeForDimL.Select(a => a.View.Position);
            var xPos = listPosition.Select(a => a[0]).Distinct();
            var yPos = listPosition.Select(a => a[1]).Distinct();
            var listX = new List<SizeForDim>();
            var listY = new List<SizeForDim>();

            #region Нахождение максимального y

            foreach (var xPo in xPos)
            {
                var maxY = new Max();
                var list = new List<SizeForDim>();
                foreach (var sizeForDim in sizeForDimL)
                {
                    var pos = (double[])sizeForDim.View.Position;
                    if (pos[0] == xPo)
                    {
                        maxY.Adding = maxY.Adding + sizeForDim.YDown + sizeForDim.YUp;
                        maxY.Main = maxY.Main + sizeForDim.Y;
                        list.Add(sizeForDim);
                    }
                }
                if (maxY.Adding + maxY.Main > yAdding + y)
                {
                    yAdding = maxY.Adding;
                    y = maxY.Main;
                    listY = list;
                }
            }

            #endregion

            #region Нахождение максимального x

            foreach (var yPo in yPos)
            {
                var maxX = new Max();
                var list = new List<SizeForDim>();
                foreach (var sizeForDim in sizeForDimL)
                {
                    var pos = (double[])sizeForDim.View.Position;
                    if (pos[1] == yPo)
                    {
                        maxX.Adding = maxX.Adding + sizeForDim.XLeft + sizeForDim.XRight;
                        maxX.Main = maxX.Main + sizeForDim.X;
                        list.Add(sizeForDim);
                    }
                }
                if (maxX.Adding + maxX.Main > xAdding + x)
                {
                    xAdding = maxX.Adding;
                    x = maxX.Main;
                    listX = list;
                }
            }

            #endregion

            double kX = (0.272 - xAdding) / x;
            double kY = ((side ? 0.145 : 0.165) - yAdding) / y;
            double j = (kX < kY ? kX : kY);
            var p = new double[3];
            listX.Sort((a, b) => a.View.Position[0].CompareTo(b.View.Position[0]));
            var arrX = listX.ToArray();
            double leftSide = 0.02;
            for (int k = 0; k < listX.Count; k++)
            {
                double delta = arrX[k].View.Position[0] - (arrX[k].Bound[2] + arrX[k].Bound[0]) / 2;
                if (delta != 0)
                    delta = delta * j;
                double add = 0;
                for (int d = k + 1; d < listX.Count; d++)
                {
                    add = add + (kX * arrX[d].XLeft) + (kX * arrX[d].X) + arrX[d].XRight;
                }
                p[0] = leftSide + arrX[k].XLeft + (kX * arrX[k].X) / 2 + delta;
                leftSide = 0.292 - add;
                p[1] = arrX[k].View.Position[1];
                p[2] = 0;
                object oPos = p;
                arrX[k].View.Position = oPos;
                if (arrX[k].View != baseView && !dictAlign.ContainsKey(arrX[k].View))
                    dictAlign.Add(arrX[k].View, (int)swAlignViewTypes_e.swAlignViewHorizontalCenter);
            }

            listY.Sort((a, b) => a.View.Position[1].CompareTo(b.View.Position[1]));
            var arrY = listY.ToArray();
            double downSide = side ? 0.06 : 0.025;
            for (int k = 0; k < listY.Count; k++)
            {
                double delta = arrY[k].View.Position[1] - (arrY[k].Bound[1] + arrY[k].Bound[3]) / 2;
                if (delta != 0)
                    delta = delta * j;
                double add = 0;
                for (int d = k + 1; d < listY.Count; d++)
                {
                    add = add + (kY * arrY[d].YDown) + (kY * arrY[d].Y) + arrY[d].YUp;
                }
                p[0] = arrY[k].View.Position[0];
                p[1] = downSide + arrY[k].YDown + (kY * arrY[k].Y) / 2 + delta;
                p[2] = 0;
                object oPos = p;
                arrY[k].View.Position = oPos;
                downSide = (side ? 0.205 : 0.17) - add;
                if (arrY[k].View != baseView && !dictAlign.ContainsKey(arrY[k].View))
                    dictAlign.Add(arrY[k].View, (int)swAlignViewTypes_e.swAlignViewVerticalCenter);
            }
            if (baseView != null)
            {
                swSheet.SetScale(1, (scale / j), false, false);
                foreach (var sizeForDim in sizeForDimL)
                {
                    if (sizeForDim.View != baseView && dictAlign.ContainsKey(sizeForDim.View))
                        sizeForDim.View.AlignWithView(dictAlign[sizeForDim.View], baseView);
                }
            }
            else
                swSheet.SetScale(1, (scale / (j * 0.95)), false, false);
        }

        private static void ShowHiddenComponents(IEnumerable<Feature> list)
        {
            foreach (var feature in list)
            {
                feature.SetSuppression(2);
            }
        }
        private static void AddTextBlock(ModelDoc2 swModel, DrawingDoc swDraw, string txt, double tX, double tY, double tZ, double tHeight, double tAngle)
        {
            AddTextBlock(swModel, swDraw, txt, tX, tY, tZ, tHeight, tAngle, null,false);
        }

        private static void AddTextBlock(ModelDoc2 swModel, DrawingDoc swDraw, string txt, double tX, double tY, double tZ, double tHeight, double tAngle,MathPoint mp,bool needToExplode)
        {
            swModel.SetAddToDB(true);
            swDraw.ICreateText2(txt, tX, tY, tZ, tHeight, tAngle);
            swModel.Extension.SelectByID2("", "NOTE", tX, tY, tZ, true, 0, null, 0);
            //if (!needToExplode)
                swModel.SketchManager.MakeSketchBlockFromSelected(mp);
            swModel.SetAddToDB(false);
            swModel.ClearUndoList();
            
        }

        private void LegendMaker(ModelDoc2 swModel, DrawingDoc swDraw, IEnumerable<string> type, double vScaleRatio)
        {
            foreach (var t in type)
            {
                using (var a = new GetDimensions())
                {
                    MathPoint instancePosition;
                    if (t.Contains("8h") && t.StartsWith("8h"))
                    {
                        a.CreateBlock(_swApp, 2, out instancePosition, 1.45 / 15 * vScaleRatio, 0.134 / 15 * vScaleRatio);
                        AddTextBlock(swModel, swDraw, "- отв." + "<MOD-DIAM>" + "8 несквозные", 0.1, 0.011, 0, 0.0025, 0, instancePosition,true);
                        
                    }
                    if (t == "5h12")
                    {
                        a.CreateBlock(_swApp, 1, out instancePosition, 0.36 / 15 * vScaleRatio, 0.134 / 15 * vScaleRatio);
                        AddTextBlock(swModel, swDraw, "- отв." + "<MOD-DIAM>" + "5h12", 0.027, 0.011, 0, 0.0025, 0, instancePosition,true);
                    }
                    if (t == "8")
                    {
                        a.CreateBlock(_swApp, 3, out instancePosition, 2.09 / 15 * vScaleRatio, 0.134 / 15 * vScaleRatio);
                        AddTextBlock(swModel, swDraw, "- отв." + "<MOD-DIAM>" + "8 сквозные", 0.142, 0.011, 0, 0.0025, 0, instancePosition,true);
                    }
                    if (t == "5")
                    {
                        a.CreateBlock(_swApp, 0, out instancePosition, 0.85 / 15 * vScaleRatio, 0.134 / 15 * vScaleRatio);
                        AddTextBlock(swModel, swDraw, "- отв." + "<MOD-DIAM>" + "5" + " сквозные", 0.06, 0.011, 0,
                                     0.0025, 0, instancePosition,true);
                       
                    }
                    if (t.Contains("8.11"))
                    {
                        a.CreateBlock(_swApp, 4, out instancePosition, 0.36 / 15 * vScaleRatio, 0.224 / 15 * vScaleRatio);
                        AddTextBlock(swModel, swDraw, "- отв." + "<MOD-DIAM>" + "8h22", 0.027, 0.017, 0, 0.0025, 0, instancePosition,true);
                    }
                }
            }
        }

        private static void SheetNumering(ModelDoc2 swModel, DrawingDoc swDraw)
        {
            int shi = 0;

            var swSheetNames = (string[])swDraw.GetSheetNames();
            foreach (var swSheetName in swSheetNames)
            {
                if (shi == 1)
                {
                    swDraw.ActivateSheet(swSheetName);
                    AddTextBlock(swModel, swDraw, "Лист 1 из 2", 0.26, 0.2, 0, 0.004, 0);
                }
                else
                {
                    if (shi == 2)
                    {
                        swDraw.ActivateSheet(swSheetName);
                        AddTextBlock(swModel, swDraw, "Лист 2 из 2", 0.26, 0.2, 0, 0.004, 0);
                    }
                }
                shi++;
            }
        }

        private IEnumerable<Feature> HideUnusedComponents(View swView, bool dimOnlyNew)
        {
            var features = new List<Feature>();
            
            try
            {
                var comp = swView.RootDrawingComponent;
                if (comp.GetChildrenCount() == 0)
                {
                    if (dimOnlyNew)
                    {
                        var rootComp = _swAdd.RootModel.IGetActiveConfiguration().IGetRootComponent2();
                        if (rootComp != null)
                        {
                            var outComponents = new LinkedList<Component2>();
                            if (_swAdd.GetComponents(rootComp, outComponents, true, false))
                            {
                                Component2 neededComp = (from outComponent in outComponents
                                                         let mod = outComponent.IGetModelDoc()
                                                         where mod != null
                                                         let name = Path.GetFileNameWithoutExtension(mod.GetPathName())
                                                         let drName = comp.Name.Substring(0, comp.Name.LastIndexOf('-'))
                                                         where name == drName
                                                         select outComponent).FirstOrDefault();

                                if (neededComp != null)
                                {
                                    var mod = neededComp.IGetModelDoc();
                                    var fm = mod.FeatureManager;
                                    var oswFeat = (object[])fm.GetFeatures(true);
                                    foreach (var ofeature in oswFeat)
                                    {
                                        var feature = (Feature)ofeature;
                                        string name = feature.GetTypeName2();
                                        if (name == "ICE" || name == "HoleWzd")
                                        {
                                            if (!feature.IsSuppressed())
                                            {
                                                feature.SetSuppression(0);
                                                features.Add(feature);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                    foreach (var oSwComp in (object[])comp.GetChildren())
                    {
                        var swComp = (DrawingComponent) oSwComp;
                        if (swComp.Visible)
                        {
                            var mod = swComp.Component.IGetModelDoc();

                            if (mod != null)
                            {

                                    if (dimOnlyNew)
                                    {
                                        var fm = mod.FeatureManager;
                                        var oswFeat = (object[]) fm.GetFeatures(true);
                                        foreach (var ofeature in oswFeat)
                                        {
                                            var feature = (Feature) ofeature;
                                            string name1 = feature.GetTypeName2();
                                            if (name1 == "ICE" || name1 == "HoleWzd")
                                            {
                                                if (!feature.IsSuppressed())
                                                {
                                                    feature.SetSuppression(0);
                                                    features.Add(feature);
                                                }
                                            }
                                        }
                                    }
                                if (mod.CustomInfo2["", "Accessories"] == "Yes" || mod.CustomInfo2["","HideOnDraft"]=="Yes")
                                    swComp.Visible = false;
                            }
                        }
                    }
            }
            catch { }
           
            return features;
        }

        private bool IsNeededColumnInDbFile(OleDbConnection oleDb, out bool isPropName, out bool isStdSketchNum, out bool isSheetNames)
        {
            bool ret = false;
            var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                                                                             new object[] { null, null, null, "TABLE" });
            isPropName = false;
            isStdSketchNum = false;
            isSheetNames = false;
            if (oleSchem.Rows.Cast<DataRow>().Any(
                                    row => (string)row["TABLE_NAME"] == "dimlimits") && oleSchem.Rows.Cast<DataRow>().Any(
                                    row => (string)row["TABLE_NAME"] == "objects"))
            {
                var thisDataSet = new DataSet();
                var testAdapter = new OleDbDataAdapter("SELECT * FROM objects", oleDb);
                testAdapter.Fill(thisDataSet, "objects");
                testAdapter.Dispose();
                foreach (var v in thisDataSet.Tables["objects"].Columns)
                {
                    var vc = (DataColumn)v;
                    if (vc.ColumnName == "propnames")
                        isPropName = true;
                }
                thisDataSet.Clear();
                testAdapter = new OleDbDataAdapter("SELECT * FROM dimlimits", oleDb);
                testAdapter.Fill(thisDataSet, "dimlimits");
                testAdapter.Dispose();
                foreach (var v in thisDataSet.Tables["dimlimits"].Columns)
                {
                    var vc = (DataColumn)v;
                    _namesOfColumnNameFromDimLimits.Add(vc.ColumnName);
                    if (vc.ColumnName == "stdsketchnum")
                        isStdSketchNum = true;
                    if (vc.ColumnName == "sheetnames")
                        isSheetNames = true;
                }
                thisDataSet.Clear();
                ret = true;
            }
            return ret;
        }

        private bool PrepareDrawingDoc(ModelDoc2 swModel, out Dictionary<string, bool> list)
        {
            list = new Dictionary<string, bool>();
            bool ret = false;
            //LinkedList<ModelDoc2> outModels;
            // if (_swAdd.GetAllUniqueModels(_swAdd.RootModel, out outModels))
            //{
            /*
                ModelDoc2 swAsmModel = null;
                SwDmDocumentOpenError oe;
                SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
                var swDoc = (SwDMDocument8)swDocMgr.GetDocument(swModel.GetPathName(),
                    SwDmDocumentType.swDmDocumentDrawing, true, out oe);
                if (swDoc != null)
                {
                    SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
                    object brokenRefVar;
                    var varRef = (object[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                    var name = (string)varRef[0];
                    swAsmModel = outModels.FirstOrDefault(modelDoc2 => modelDoc2.GetPathName() == name);
                }
            */
            ModelDoc2 swAsmModel = GetCurrentAsmModel(swModel);

            OleDbConnection oleDb;
            if (swAsmModel != null)
            {
                if (_createProgramm)
                {
                    var matName = swAsmModel.get_CustomInfo2("", "Material");
                    if (matName == "" && swAsmModel.GetConfigurationCount() > 1)
                    {
                        string val, resolvedVal;
                        if (
                            swAsmModel.Extension.get_CustomPropertyManager(swAsmModel.IGetActiveConfiguration().Name).
                                Get4(
                                    "Material", false, out val, out resolvedVal))
                            matName = val;
                    }
                    _z = ParseMaterialName(matName);
                }

                if (_swAdd.OpenModelDatabase(swAsmModel, out oleDb))
                {
                    bool isPropName, isStdSketchNum, isSheetNames;
                    if (IsNeededColumnInDbFile(oleDb, out isPropName, out isStdSketchNum, out isSheetNames))
                    {
                        var objNames = _namesOfColumnNameFromDimLimits.Where(x => x.Contains("obj"));
                        var listId = new List<int>();
                        foreach (var objName in objNames)
                        {
                            int id = FrmSetParameters.GetIdFromColumnName(objName);
                            if (!listId.Contains(id))
                                listId.Add(id);
                        }
                        var dictIdSize = new Dictionary<int, double>();
                        var cm = new OleDbCommand("SELECT * FROM objects", oleDb);
                        var rd = cm.ExecuteReader();
                        while (rd.Read())
                        {
                            if (listId.Contains((int) rd["id"]))
                            {
                                double val;
                                if (_swAdd.GetObjectValue(swAsmModel, (string) rd["name"], 14, out val) &&
                                    !dictIdSize.ContainsKey((int) rd["id"]))
                                    dictIdSize.Add((int) rd["id"], val);
                            }
                        }
                        rd.Close();
                        cm = new OleDbCommand("SELECT * FROM dimlimits", oleDb);
                        rd = cm.ExecuteReader();
                        while (rd.Read())
                        {
                            var lB = (from i in listId
                                      let mn = (int) rd["obj" + i + "min"]
                                      let mx = (int) rd["obj" + i + "max"]
                                      select (mn <= dictIdSize[i]) && (dictIdSize[i] <= mx)).ToList();
                            if (lB.Aggregate(true, (current, b) => (b && current)))
                            {
                                if (isSheetNames)
                                {
                                    var needSheetsNumb = (string) rd["sheetnames"];
                                    swModel.ClearSelection();
                                    foreach (var strNum in needSheetsNumb.Split(','))
                                    {
                                        string strNm = strNum.Trim();
                                        string num = strNm.Substring(0, strNm.Length - 1);
                                        string side = strNm.Substring(strNm.Length - 1);
                                        list.Add(num, side.ToLower() == "l");
                                    }
                                    var swDrw = (DrawingDoc) swModel;
                                    var sheetnames = (string[]) swDrw.GetSheetNames();

                                    foreach (var sheetname in sheetnames)
                                    {
                                        if (sheetname.Contains("1") ||
                                            list.Keys.Contains(sheetname.Substring(sheetname.Length - 1)))
                                            continue;
                                        swModel.Extension.SelectByID2(sheetname, "SHEET", 0, 0, 0, true, 0, null, 0);
                                    }
                                    swModel.DeleteSelection(true);
                                    ret = true;
                                }
                                break;
                            }
                        }
                        rd.Close();
                    }
                    oleDb.Close();
                }
            }
            return ret;
        }

        private int ParseMaterialName(string matName)
        {
            try
            {
                var arr = matName.ToCharArray();
                string digit = "";
                foreach (var ch in arr)
                {
                    try
                    {
                        Convert.ToInt32(ch.ToString());
                        digit += ch.ToString();
                    }
                    catch
                    {
                        continue;
                    }
                }
                return Convert.ToInt32(digit);
            }
            catch (Exception)
            {
                MessageBox.Show(@"Ошибка чтения свойства детали!", @"MrDoors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
        }

        private ModelDoc2 GetCurrentAsmModel(ModelDoc2 swModel)
        {
            ModelDoc2 swAsmModel = null;
            LinkedList<ModelDoc2> outModels;
            if (_swAdd.GetAllUniqueModels(_swAdd.RootModel, out outModels))
            {
                SwDmDocumentOpenError oe;
                SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
                var swDoc = (SwDMDocument8)swDocMgr.GetDocument(swModel.GetPathName(),
                                                                 SwDmDocumentType.swDmDocumentDrawing, true, out oe);
                if (swDoc != null)
                {
                    SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
                    object brokenRefVar;
                    var varRef = (object[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                    var name = (string)varRef[0];
                    swAsmModel = outModels.FirstOrDefault(modelDoc2 => modelDoc2.GetPathName().ToLower() == name.ToLower());
                    swDoc.CloseDoc();
                }
            }
            return swAsmModel;
        }

        public void Dispose()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }
    }
    public static class BlockPositionExtension
    {
        //public static BlockPosition blockPosition = BlockPosition.LeftTopToRightBottom;
        public static BlockPosition Not(this BlockPosition block)
        {
            switch (block)
            {
                case BlockPosition.LeftTopToRightBottom:
                    return BlockPosition.RightTopToLeftBottom;
                case BlockPosition.LeftBottomToRightTop:
                    return BlockPosition.RigthBottomToLeftTop;
                case BlockPosition.RightTopToLeftBottom:
                    return BlockPosition.LeftTopToRightBottom;
                case BlockPosition.RigthBottomToLeftTop:
                    return BlockPosition.LeftBottomToRightTop;
            }
            return BlockPosition.LeftTopToRightBottom;
        }
        public static bool AsBoolean(this BlockPosition block)
        {
            switch (block)
            {
                case BlockPosition.LeftTopToRightBottom:
                    return true;
                case BlockPosition.LeftBottomToRightTop:
                    return true;
                case BlockPosition.RightTopToLeftBottom:
                    return false;
                case BlockPosition.RigthBottomToLeftTop:
                    return false;
            }
            return true;
        }
        public static BlockPosition FromBool(bool side)
        {
            if (side)
                return BlockPosition.LeftTopToRightBottom;
            else
                return BlockPosition.RightTopToLeftBottom;
                
        }

    }
    public enum BlockPosition
    {
        LeftTopToRightBottom,
        LeftBottomToRightTop,
        RightTopToLeftBottom,
        RigthBottomToLeftTop
        
    }
    class DimensionView : IDisposable
    {
        private readonly ISldWorks _swApp;
        private readonly ModelDoc2 _swModel;
        private BlockPosition _side;
        private readonly List<string> _typeList = new List<string>();
        private bool _isHole;
        private readonly bool _dimOnlyNew;

            
        private SizeForDim _addSize = new SizeForDim(0, 0, 0, 0, 0, 0, null, null);
        public BlockPosition Side
        {
            get { return _side; }
        }
        public DimensionView(ISldWorks swApp, BlockPosition side, bool dimOnlyNew)
        {
            _swApp = swApp;
            _swModel = swApp.IActiveDoc2;
            _side = side;
            _dimOnlyNew = dimOnlyNew;
            ListSize = new List<CoordAndDepth>();
        }

        public List<string> List
        {
            get
            {
                return _typeList;
            }
        }

        public SizeForDim AddSize
        {
            get
            {
                return _addSize;
            }
        }

        public bool IsHole
        {
            get
            {
                return _isHole;
            }
        }

        public List<CoordAndDepth> ListSize { get; private set; }

        public double X { get; private set; }

        public double Y { get; private set; }

        public void DimView(bool doThrowHoles)
        {
            Ordinate vertex, vertexS;
            var swModel = (ModelDoc2) _swApp.ActiveDoc;
            var swDrawing = (DrawingDoc) swModel;
            var drView = swDrawing.IActiveDrawingView;
            var boundBox = (double[]) drView.GetOutline();
            var scale = (double[]) drView.ScaleRatio;
            double vScaleRat = scale[1];
            double[] dimLineH, dimLineV;

            bool? blockSize;

            double[] dsDim, deDim, endDim;
            string dsName, deName;
            GetBlockPositions(_swModel, out dsDim, out deDim, out endDim, out dsName, out deName);
            double notMoreThenY = double.MaxValue, notMoreThenX = double.MaxValue;
            if (dsDim != null && endDim != null)
            {
                notMoreThenY = Math.Abs((dsDim[1] - endDim[1]) * 1000);
                notMoreThenX = Math.Abs((dsDim[0] - endDim[0]) * 1000);
            }
            
            var origin = AnalizViewCoefficient(swModel, boundBox, ref _side, out dimLineH, out dimLineV,out blockSize,out vertex,out vertexS,dsDim,deDim,endDim,dsName,deName);

            
            if (_side == BlockPosition.LeftTopToRightBottom || _side == BlockPosition.LeftBottomToRightTop || _side == BlockPosition.RigthBottomToLeftTop)
                vertexS = vertex;
            
            var list = new List<Common> {origin};

            var vEdges = (object[]) drView.GetVisibleEntities(null, (int) swViewEntityType_e.swViewEntityType_Edge);
            var dictionaryHoles = new Dictionary<string, CoordH>();
            var holesCount = new Dictionary<string, int>();

            #region Добавление новых образмериваемых объектов в список

            foreach (var ent in vEdges.Cast<Entity>())
            {
                using (var fc = new FoundCoordinate(_swApp, ent, vertex, dimLineH, dimLineV, _side, drView, doThrowHoles))
                {
                    if (fc.Coordinate!=null)
                        if (fc.Coordinate.CooX.X > notMoreThenX || fc.Coordinate.CooY.Y > notMoreThenY)
                        {
                            fc.Coordinate.CooX.AnnX.Select(false);
                            fc.Coordinate.CooY.AnnY.Select(true);
                            fc.CoordH.AnnH.Select(true);
                            swModel.DeleteSelection(true);
                            continue;
                        }
                      
                    if (fc.Coordinate != null)
                        list.Add(new Common(fc.Coordinate, ent));

                    if (fc.CoordH != null)
                    {
                        _isHole = true;
                        double diam;
                        double depth = 0;
                        if (fc.StrType.Contains("h"))
                        {
                            try
                            {
                                string definedValue = fc.StrType;
                                diam = Convert.ToDouble(definedValue.Substring(0, definedValue.IndexOf("h")));
                                depth = Convert.ToDouble(definedValue.Substring(definedValue.IndexOf("h") + 1));
                            }
                            catch
                            {
                                string definedValue = fc.StrType;
                                definedValue = definedValue.Contains('.')
                                                          ? definedValue.Replace('.', ',') : definedValue;
                                diam = Convert.ToDouble(definedValue.Substring(0, definedValue.IndexOf("h")));
                                depth = Convert.ToDouble(definedValue.Substring(definedValue.IndexOf("h") + 1));
                            }
                        }
                        else
                        {
                            try
                            {
                                string definedValue = fc.StrType;
                                diam = Convert.ToDouble(definedValue);
                            }
                            catch
                            {
                                string definedValue = fc.StrType;
                                if (definedValue == " накернить")
                                    diam = 0;
                                else
                                {
                                    definedValue = definedValue.Contains('.')
                                                       ? definedValue.Replace('.', ',')
                                                       : definedValue;
                                    diam = Convert.ToDouble(definedValue);
                                }
                            }
                        }

                        ListSize.Add(new CoordAndDepth(fc.CoordH.X, fc.CoordH.Y, diam, depth));

                        if (dictionaryHoles.ContainsKey(fc.StrType))
                        {
                            if (dictionaryHoles[fc.StrType].X <= fc.CoordH.X &&
                                dictionaryHoles[fc.StrType].Y <= fc.CoordH.Y)
                            {
                                dictionaryHoles[fc.StrType].AnnH.Select(true);
                                swModel.DeleteSelection(true);
                                dictionaryHoles[fc.StrType] = fc.CoordH;
                            }
                            else
                            {
                                fc.CoordH.AnnH.Select(true);
                                swModel.DeleteSelection(true);
                            }
                            holesCount[fc.StrType]++;
                        }
                        else
                        {
                            dictionaryHoles.Add(fc.StrType, fc.CoordH);
                            holesCount.Add(fc.StrType, 1);
                        }
                    }
                }
                swModel.ClearUndoList();
            }

            #endregion

            Dictionary<double, Annotation> cooX;
            Dictionary<double, Annotation> cooY;
            List<Common2> lineList;

            GetDictionaryAndDeleteAnn(list, out cooX, out cooY, out lineList);

            var xl = list.Select(x => x.Coord.CooX.X).ToList();
            xl.Sort((x, y) => x.CompareTo(y));
            X = xl.Last();
            double xSize = xl.Last()/(vScaleRat*1000);

            var yl = list.Select(x => x.Coord.CooY.Y).ToList();
            yl.Sort((x, y) => x.CompareTo(y));
            Y = yl.Last();
            double ySize = yl.Last()/(vScaleRat*1000);

            if (_side != BlockPosition.RightTopToLeftBottom)
            {
                SetPosition(cooX, vertex, vScaleRat, true, _side);
                SetPosition(cooY, vertexS, vScaleRat, false, _side);
            }
            else
            {
                SetPosition(cooX, vertex, vScaleRat, true, _side);
                SetPosition(cooY, vertex, vScaleRat, false, _side);
            }

            if (_dimOnlyNew)
                DeleteOrigin(origin);

            InsertLines(swModel, lineList);

            int i = 0;
            double xPositionOfHoleAnn = 0;
            foreach (var dictionaryHole in dictionaryHoles)
            {
                if (dictionaryHole.Value.Y > notMoreThenY || dictionaryHole.Value.X > notMoreThenX)
                {
                    dictionaryHole.Value.DispDim.IGetAnnotation().Select(false);
                    swModel.DeleteSelection(true);
                    continue;
                }
                double d = SetPositionHole(dictionaryHole.Value.AnnH, dictionaryHole.Value.DispDim, vertex,
                                           dictionaryHole.Value.X,
                                           vScaleRat, boundBox, holesCount[dictionaryHole.Key], dictionaryHole.Key, i,
                                           _side.AsBoolean());
                if (d > xPositionOfHoleAnn)
                    xPositionOfHoleAnn = d;
                if (AnalizeType(dictionaryHole.Key))
                    _typeList.Add(dictionaryHole.Key);
                i++;
            }
            if (xPositionOfHoleAnn < ((boundBox[0] + boundBox[2] + xSize)/2))
                xPositionOfHoleAnn = 0;
            else
                xPositionOfHoleAnn = xPositionOfHoleAnn - ((boundBox[0] + boundBox[2] + xSize)/2);

            double addXLeft = 0, addYUp = 0, addXRight = 0, addYDown = 0;

            var oNotes = (object[]) drView.GetNotes();
            if (oNotes != null)
            {
                foreach (var oNote in oNotes)
                {
                    var note = (Note) oNote;
                    var pos = (double[]) note.IGetAnnotation().GetPosition();
                    double x, y;
                    if (note.Angle == 0)
                    {
                        x = pos[0];
                        if (pos[1] < (boundBox[1] + boundBox[3])/2)
                        {
                            y = boundBox[1] - 0.004*i;
                            addYDown = 0.004;
                        }
                        else
                        {
                            y = boundBox[3] + 0.004*(cooX.Count + 1);
                            addYUp = 0.004;
                        }
                    }
                    else
                    {
                        y = pos[1];
                        if (pos[0] < (boundBox[0] + boundBox[2])/2)
                        {
                            x = boundBox[0] - (cooY.Count + 1)*0.004;
                            addXLeft = 0.004;
                        }
                        else
                        {
                            x = boundBox[2] + xPositionOfHoleAnn + 0.004;
                            addXRight = 0.004;
                        }
                    }
                    note.IGetAnnotation().SetPosition(x, y, 0);
                }
            }

            _addSize = new SizeForDim((cooY.Count*0.004 + ((boundBox[2] - boundBox[0] - xSize)/2) + addXLeft),
                                      (cooX.Count*0.004 + ((boundBox[3] - boundBox[1] - ySize)/2) + addYUp),
                                      xPositionOfHoleAnn + addXRight,
                                      (((boundBox[3] - boundBox[1] - ySize)/2) + i*0.004 + addYDown),
                                      xSize, ySize, boundBox, drView);
        }
        public static void GetBlockPositions(ModelDoc2 swModel,out double[] dsDoubles,out double[] deDoubles,out double[] endDimDoubles,out string dsName,out string deName)
        {
            var swDrawing = (DrawingDoc)swModel;
            var drView = swDrawing.IActiveDrawingView;
            var objSketchBlockDef = swModel.SketchManager.GetSketchBlockDefinitions();
            dsDoubles = null;
            deDoubles = null;
            endDimDoubles = null;
            dsName = null;
            deName = null;
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
                            var swSketBlInst = (SketchBlockInstance)objSketBlInst;

                            if (swSketBlInst.Name.Contains("DS" + drView.Name))
                            {
                                var point = swSketBlInst.InstancePosition;
                                dsDoubles= (double[])point.ArrayData;
                                dsName = swSketBlInst.Name;
                            }
                            if (swSketBlInst.Name.Contains("EndDimArea"+drView.Name))
                            {
                                var point = swSketBlInst.InstancePosition;
                                endDimDoubles = (double[])point.ArrayData;
                            }
                            if (swSketBlInst.Name.Contains("DE" + drView.Name))
                            {
                                var point = swSketBlInst.InstancePosition;
                                deDoubles = (double[])point.ArrayData;
                                deName = swSketBlInst.Name;
                            }
                        }
                }
            }
        }

        private void DeleteOrigin(Common origin)
        {
            _swModel.ClearSelection2(true);
            do
            {
                origin.Coord.CooX.AnnX.Select(true);
            } while (!_swModel.DeleteSelection(true));

            do
            {
                origin.Coord.CooY.AnnY.Select(true);
            } while (!_swModel.DeleteSelection(true));
            _swModel.ClearSelection2(true);
        }

        private static bool AnalizeType(string type)
        {
            if (type == "5h12")
                return true;
            if (type == "8")
                return true;
            if (type == "5")
                return true;
            if (type.Contains("8.11"))
                return true;
            if (type.Contains("8h"))
                return true;
            return false;
        }

        private static void InsertLines(ModelDoc2 swModel, List<Common2> list)
        {
            var xDist = list.Select(x => x.Ordinate.X).Distinct();
            list.Sort((x, y) => (x.Ordinate.Y.CompareTo(y.Ordinate.Y)));
            foreach (var d in xDist)
            {
                double d1 = d;
                var l = list.Where(x => x.Ordinate.X == d1).ToArray();
                for (int i = 0; i < l.Count() - 1; i++)
                {
                    if (l[i].Ordinate.Y != l[i + 1].Ordinate.Y)
                        DrawDottedLine(swModel, l[i].Entity, l[i + 1].Entity);
                }
            }
            var yDist = list.Select(x => x.Ordinate.Y).Distinct();
            list.Sort((x, y) => (x.Ordinate.X.CompareTo(y.Ordinate.X)));
            foreach (var d in yDist)
            {
                double d1 = d;
                var l = list.Where(x => x.Ordinate.Y == d1).ToArray();
                for (int i = 0; i < l.Count() - 1; i++)
                {
                    if (l[i].Ordinate.X != l[i + 1].Ordinate.X)
                        DrawDottedLine(swModel, l[i].Entity, l[i + 1].Entity);
                }
            }
        }

        private static double SetPositionHole(Annotation annotation, DisplayDimension displayDimension,
            Ordinate vertex, double x, double vScale, double[] boundBox, int numbers, string type, int posNumb, bool side)
        {
            string newstring;
            if (type.Contains("8.11h"))
                newstring = Convert.ToString(numbers + " отв. " + "<MOD-DIAM>" + "8" +
                                             type.Substring(type.IndexOf('h')));
            else
            {
                if (type.Contains("7,33"))
                    newstring = Convert.ToString(numbers + " отв. по схеме A25SK");
                else
                    newstring = Convert.ToString(numbers + " отв. " + "<MOD-DIAM>" + type);
                if (type.Contains("накернить"))
                    newstring = Convert.ToString(numbers + " отв. накернить");
            }
            displayDimension.SetText((int)swDimensionTextParts_e.swDimensionTextAll, newstring);
            if (side)
                annotation.SetPosition(vertex.X + x / 1000 / vScale, boundBox[1] - 0.004 * posNumb, 0);
            else
                annotation.SetPosition(vertex.X - x / 1000 / vScale, boundBox[1] - 0.004 * posNumb, 0);

            int textCount = newstring.Length;
            return (((double[])annotation.GetPosition())[0] + textCount * 0.001);
        }

        private void GetDictionaryAndDeleteAnn(IEnumerable<Common> list, out Dictionary<double, Annotation> dictX,
            out Dictionary<double, Annotation> dictY, out List<Common2> dropLineList)
        {
            var allAnnX = new List<Annotation>();
            var coordinateX = new Dictionary<double, Annotation>();
            var dopY = new Dictionary<double, double>();
            var allAnnY = new List<Annotation>();
            var coordinateY = new Dictionary<double, Annotation>();
            var dopX = new Dictionary<double, double>();
            dropLineList = new List<Common2>();
            foreach (var coord in list)
            {
                if (coord.Coord.CooX != null && coord.Coord.CooY != null)
                {
                    allAnnX.Add(coord.Coord.CooX.AnnX);
                    if (!coordinateX.ContainsKey(coord.Coord.CooX.X))
                    {
                        coordinateX.Add(coord.Coord.CooX.X, coord.Coord.CooX.AnnX);
                        dopY.Add(coord.Coord.CooX.X, coord.Coord.CooY.Y);
                    }
                    else if (coord.Coord.CooY.Y < dopY[coord.Coord.CooX.X])
                    {
                        dopY[coord.Coord.CooX.X] = coord.Coord.CooY.Y;
                        coordinateX[coord.Coord.CooX.X] = coord.Coord.CooX.AnnX;
                    }
                    allAnnY.Add(coord.Coord.CooY.AnnY);
                    if (!coordinateY.ContainsKey(coord.Coord.CooY.Y))
                    {
                        coordinateY.Add(coord.Coord.CooY.Y, coord.Coord.CooY.AnnY);
                        dopX.Add(coord.Coord.CooY.Y, coord.Coord.CooX.X);
                    }
                    else if (( coord.Coord.CooX.X < dopX[coord.Coord.CooY.Y] && _side.AsBoolean()) ||
                        ((coord.Coord.CooX.X) > dopX[coord.Coord.CooY.Y] && !_side.AsBoolean()))
                    {
                        dopX[coord.Coord.CooY.Y] =coord.Coord.CooX.X;
                        coordinateY[coord.Coord.CooY.Y] = coord.Coord.CooY.AnnY;
                    }
                    dropLineList.Add(new Common2(new Ordinate(coord.Coord.CooX.X, coord.Coord.CooY.Y), coord.Entity));
                }
            }
            foreach (var annotation in allAnnX)
            {
                if (!coordinateX.ContainsValue(annotation))
                {
                    do
                    {
                        annotation.Select(true);
                    } while (!_swModel.DeleteSelection(true));
                }
            }
            foreach (var annotation in allAnnY)
            {
                if (!coordinateY.ContainsValue(annotation))
                {
                    do
                    {
                        annotation.Select(true);
                    } while (!_swModel.DeleteSelection(true));
                }
            }
            dictX = coordinateX;
            dictY = coordinateY;
        }

        private static void SetPosition(Dictionary<double, Annotation> dictionary, Ordinate vertex, double vScale, bool xOrY, BlockPosition side)
        {
            
            var x = dictionary.OrderBy(z => z.Key);
            int k = 1;
            foreach (var keyValuePair in x)
            {
                if (keyValuePair.Value != null)
                {
                    switch (side)
                    {
                        case BlockPosition.LeftTopToRightBottom:
                            {
                                if (xOrY)
                                {
                                    if (keyValuePair.Key > 100)
                                        keyValuePair.Value.SetPosition(vertex.X + keyValuePair.Key / 2000 / vScale,
                                                                   vertex.Y + 0.004 * k, 0);
                                    else
                                        keyValuePair.Value.SetPosition(vertex.X + keyValuePair.Key / 1000 / vScale + 0.006,
                                                                   vertex.Y + 0.004 * k, 0);
                                }
                                else
                                {
                                    if (keyValuePair.Key > 100)
                                        keyValuePair.Value.SetPosition(vertex.X - 0.004 * k,
                                                                   vertex.Y - keyValuePair.Key / 2000 / vScale, 0);
                                    else
                                        keyValuePair.Value.SetPosition(vertex.X - 0.004 * k,
                                                                   vertex.Y - keyValuePair.Key / 1000 / vScale - 0.006, 0);
                                }
                            }
                        break;
                        case BlockPosition.RightTopToLeftBottom:
                            {
                                if (xOrY)
                                {
                                    if (keyValuePair.Key > 100)
                                        keyValuePair.Value.SetPosition(vertex.X - keyValuePair.Key / 2000 / vScale,
                                                                   vertex.Y + 0.004 * k, 0);
                                    else
                                        keyValuePair.Value.SetPosition(vertex.X - keyValuePair.Key / 1000 / vScale - 0.006,
                                                                   vertex.Y + 0.004 * k, 0);
                                }
                                else
                                {
                                    var tt1 = vertex.Y - keyValuePair.Key/2000/vScale;
                                    var tt2 = vertex.Y - keyValuePair.Key/1000/vScale - 0.006;

                                    if (keyValuePair.Key > 100)
                                        keyValuePair.Value.SetPosition(vertex.X + 0.004 * k,
                                                                   tt1, 0);
                                    else
                                        keyValuePair.Value.SetPosition(vertex.X + 0.004 * k,
                                                                   tt2, 0);
                                }
                            }
                        break;
                        case BlockPosition.LeftBottomToRightTop:
                            {
                                if (xOrY)
                                {
                                    if (keyValuePair.Key > 100)
                                        keyValuePair.Value.SetPosition(vertex.X + keyValuePair.Key / 2000 / vScale,
                                                                   vertex.Y - 0.004 * k, 0);
                                    else
                                        keyValuePair.Value.SetPosition(vertex.X + keyValuePair.Key / 1000 / vScale + 0.006,
                                                                   vertex.Y - 0.004 * k, 0);
                                }
                                else
                                {
                                    if (keyValuePair.Key > 100)
                                        keyValuePair.Value.SetPosition(vertex.X - 0.004 * k,
                                                                   vertex.Y + keyValuePair.Key / 2000 / vScale, 0);
                                    else
                                        keyValuePair.Value.SetPosition(vertex.X - 0.004 * k,
                                                                   vertex.Y + keyValuePair.Key / 1000 / vScale - 0.006, 0);
                                }
                            }
                            break;
                        case BlockPosition.RigthBottomToLeftTop:
                            {
                                if (xOrY)
                                {
                                    if (keyValuePair.Key > 100)
                                        keyValuePair.Value.SetPosition(vertex.X - keyValuePair.Key / 2000 / vScale,
                                                                   vertex.Y - 0.004 * k, 0);
                                    else
                                        keyValuePair.Value.SetPosition(vertex.X - keyValuePair.Key / 1000 / vScale + 0.006,
                                                                   vertex.Y - 0.004 * k, 0);
                                }
                                else
                                {
                                    if (keyValuePair.Key > 100)
                                        keyValuePair.Value.SetPosition(vertex.X + 0.004 * k,
                                                                   vertex.Y + keyValuePair.Key / 2000 / vScale, 0);
                                    else
                                        keyValuePair.Value.SetPosition(vertex.X + 0.004 * k,
                                                                   vertex.Y + keyValuePair.Key / 1000 / vScale - 0.006, 0);
                                }
                            }
                            break;
                    }
                    k++;
                }
            }
        }

        private static Common AnalizViewCoefficient(ModelDoc2 swModel, double[] boundBox,ref BlockPosition side, out double[] dimLinDirH, out double[] dimLinDirV, out bool? blockSize,out Ordinate vertex,out Ordinate vertexS,double[] dsDim,double[] deDim,double[] endDim,string dsName,string deName)
        {
            double x, y;
            Annotation annX, annY;
            using (var a = new GetDimensions())
            {
                bool consider;

                var tmpSide = GetDimensions.GetBlockPosition(swModel, out consider);//a.GetBlockPosition(swModel,out consider);
                if (consider) // consider -принимать ли в расчет tmpSide
                    side = tmpSide;
                dimLinDirH = a.GetDimensionLineDirect(swModel, boundBox, false, side, out x, out annX, out blockSize,out vertex,out vertexS,dsDim,deDim,endDim,dsName,deName);
            }
            using (var a = new GetDimensions())
            {
                Ordinate vert, verts;
                dimLinDirV = a.GetDimensionLineDirect(swModel, boundBox, true, side, out y, out annY, out blockSize, out vert, out verts, dsDim, deDim, endDim, dsName, deName);
                if (verts != null)
                    vertexS = verts;
            }
            return new Common(new Coord(new CoordX(x, annX), new CoordY(y, annY)), null);
        }

        private static void DrawDottedLine(ModelDoc2 swModel, Entity ent1, Entity ent2)
        {
            swModel.SetAddToDB(true);
            if (ent1 != null && ent2 != null)
            {
                swModel.SketchManager.CreateCenterLine(1, 1, 0, 1.1, 1.1, 0);
                var objPoints = (object[])swModel.SketchManager.ActiveSketch.GetSketchPoints();
                SketchPoint swPoint1 = null, swPoint2 = null;
                foreach (var objPoint in objPoints)
                {
                    var swPoint = (SketchPoint)objPoint;
                    double x1 = 1 - swPoint.X;
                    double x2 = 1.1 - swPoint.X;
                    double y1 = 1 - swPoint.Y;
                    double y2 = 1.1 - swPoint.Y;
                    if (x1 < 0)
                        x1 = -x1;
                    if (x2 < 0)
                        x2 = -x2;
                    if (y1 < 0)
                        y1 = -y1;
                    if (y2 < 0)
                        y2 = -y2;
                    if (x1 < 0.0001 && y1 < 0.0001)
                    {
                        swPoint1 = swPoint;
                    }
                    if (x2 < 0.0001 && y2 < 0.0001)
                    {
                        swPoint2 = swPoint;
                    }
                }
                if (swPoint1 != null && swPoint2 != null)
                {
                    if (swPoint1.Select(false) && ent1.Select(true))
                    {
                        swModel.SketchAddConstraints("sgCONCENTRIC");
                    }
                    if (swPoint2.Select(false) && ent2.Select(true))
                    {
                        swModel.SketchAddConstraints("sgCONCENTRIC");
                    }
                }
            }
            swModel.SetAddToDB(false);
            swModel.GraphicsRedraw2();
            swModel.ClearSelection();
            //swModel.ClearUndoList();
        }

        public void Dispose()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }
    }

    class FoundCoordinate : IDisposable
    {
        private readonly Coord _coordinate;
        private readonly ISldWorks _swApp;
        private readonly ModelDoc2 _swModel;
        private readonly Entity _ent;
        private readonly Ordinate _vertex;
        private readonly SelectData _swSelData;
        private View _swView;
        private readonly bool _doThroughHoles;
        private readonly BlockPosition _side;
        private int _holeType;

        public string StrType { get; private set; }

        public CoordH CoordH { get; private set; }

        public Coord Coordinate
        {
            get
            {
                return (_coordinate);
            }
        }

        public FoundCoordinate(ISldWorks swApp, Entity ent, Ordinate vertex, double[] dimLineH, double[] dimLineV, BlockPosition side,View swView,bool doThroughHoles)
        {
            _swApp = swApp;
            _swModel = swApp.IActiveDoc2;
            _side = side;
            _ent = ent;
            _vertex = vertex;
            _doThroughHoles = doThroughHoles;
            _swSelData = _swModel.ISelectionManager.CreateSelectData();
            _swView = swView;
            _coordinate = FindCoord(dimLineH, dimLineV);
        }

        private Coord FindCoord(double[] dimLineH, double[] dimLineV)
        {
            CoordX coordX;
            CoordY coordY;
            Annotation annH;
            DisplayDimension swDispDimH;
            if (FoundParamsOfCuvites(out annH, out swDispDimH))
            {
                if (_holeType < 5)
                {
                    InsertBlock();
                }
                using (var c = new InsertDimension(_swApp, _ent, _vertex, _swSelData))
                {
                    coordX = c.FoundXcoord(dimLineH);
                }
                using (var c = new InsertDimension(_swApp, _ent, _vertex, _swSelData))
                {
                    coordY = c.FoundYcoord(dimLineV);
                }
                CoordH = new CoordH(coordX.X, coordY.Y, annH, swDispDimH);
                return new Coord(coordX, coordY);
            }
            return null;
        }

        private bool FoundParamsOfCuvites(out Annotation annH, out DisplayDimension swDispDimH)
        {
            bool ret = false;
            annH = null;
            var swDrawing = (DrawingDoc)_swModel;
            int i = 0;
            do
            {
                using (var a = new GetDimensions())
                {
                    swDispDimH = a.GetDisplayDim(swDrawing, _ent, _swSelData);
                }
                i++;
            } while (swDispDimH == null && i < 3);
            if (swDispDimH != null)
            {
                var dimObjH = (Dimension)swDispDimH.GetDimension();
                string holeDepSuffix = swDispDimH.GetText((int)swDimensionTextParts_e.swDimensionTextPrefix);
                if (dimObjH != null)
                {
                    double diam = Convert.ToDouble(Strings.Left(holeDepSuffix, 1) == "R"
                                                       ? (dimObjH.Value * 2).ToString("0.00")
                                                       : (dimObjH.Value).ToString("0.00"));

                    var holeDepSuffix1 = swDispDimH.GetText((int)swDimensionTextParts_e.swDimensionTextSuffix);
                    if (string.IsNullOrEmpty(holeDepSuffix1) && !string.IsNullOrEmpty(holeDepSuffix))
                    {
                        var tt =holeDepSuffix.Split(new string[1]{"<HOLE-DEPTH>"},5,StringSplitOptions.RemoveEmptyEntries);
                        if (tt.Length>=2)
                            holeDepSuffix1 = "<HOLE-DEPTH> " + tt[1]; 
                    }
                    double depth;
                    try
                    {
                        string holeDepSuffixAbridgement = Strings.Right(holeDepSuffix1,
                                                                        holeDepSuffix1.Length - 12);
                        depth = Convert.ToDouble(holeDepSuffixAbridgement.Replace('.', ','));
                    }
                    catch
                    {
                        try
                        {
                            string holeDepSuffixAbridgement = Strings.Right(holeDepSuffix1,
                                                                            holeDepSuffix1.Length - 12);
                            depth = Convert.ToDouble(holeDepSuffixAbridgement);
                        }
                        catch
                        {
                            depth = 0;
                        }
                    }
                    int drType = 7;
                    var swView = swDrawing.IActiveDrawingView;
                    if (swView != null)
                        drType = swView.Type;
                    bool type = drType == 4;
                    var diamAndDepth = SetManualDiametrAndDepth(diam, depth, type);
                    if (_doThroughHoles || diamAndDepth.Y != 0)//if (_side.AsBoolean() || diamAndDepth.Y != 0) // насколько я понимаю, только этот if определяет, указывать ли сквознае отверстия
                    {
                            StrType = GetStringHoleType(diamAndDepth.X, diamAndDepth.Y);
                        _holeType = DefinitionHolesType(diamAndDepth.X, diamAndDepth.Y);
                        annH = swDispDimH.IGetAnnotation();
                        ret = true;
                    }
                    else
                    {
                        swDispDimH.IGetAnnotation().Select(true);
                        _swModel.DeleteSelection(true);
                    }
                }
            }
            _swModel.ClearSelection();
            return ret;
        }

        private void InsertBlock()
        {
            using (var a = new GetDimensions())
            {
                MathPoint instancePosition;
                if (_swModel.Extension.SelectByID2("Точка вставки/" + a.CreateBlock(_swApp, _holeType, out instancePosition),
                                          "SKETCHPOINT", 0, 0, 0, true, 0, null, 0) && _ent.Select(true))
                    _swModel.SketchAddConstraints("sgCONCENTRIC");
                _swModel.ClearSelection();
            }
        }

        private static string GetStringHoleType(double diam, double depth)
        {
            string ret;
            if (diam == 5 && depth == 2)
                return " накернить";
            if (depth != 0)
            {
                ret = Convert.ToString(diam + "h" + depth);
            }
            else
            {
                ret = diam == 8.11 ? "8.11h22" : Convert.ToString(diam);
            }
            return ret;
        }

        private Ordinate SetManualDiametrAndDepth(double diam, double depth, bool type)
        {
            var diamAndDepth = new Ordinate(diam, depth);
            if (type)
            {
                if (diam == 8 && depth == 0)
                {
                    diamAndDepth.X = 8;
                    diamAndDepth.Y = GetDepthFor(8);
                    return diamAndDepth;
                }
                if (diam == 11 && depth == 0)
                {
                    diamAndDepth.Y = 32;
                    return diamAndDepth;
                }
            }
            else
            {
                if (diam == 8 && (depth == 12.5 || depth == 13))
                {
                    diamAndDepth.X = 8;
                    diamAndDepth.Y = 12;
                    return diamAndDepth;
                }
                if ((diam == 5) && ((depth == 6.1) || (depth == 1.6) || (depth == 12.5) || (depth == 13)))
                {
                    diamAndDepth.X = 5;
                    diamAndDepth.Y = 12;
                    return diamAndDepth;
                }

                if (diam == 20 && depth == 0)
                {
                    diamAndDepth.X = 20;
                    diamAndDepth.Y = GetDepthFor(20);
                    return diamAndDepth;
                }
                if (diam == 15.11)
                {
                    diamAndDepth.X = 8;
                    return diamAndDepth;
                }
                if (diam == 25 && depth == 0)
                {
                    diamAndDepth.Y = 27;
                    return diamAndDepth;
                }
                if (diam ==57 && depth == 0)
                {
                    depth = 16;
                    diamAndDepth.X = 57;
                    diamAndDepth.Y = 16;
                }

            }
            return diamAndDepth;
        }

        private static int DefinitionHolesType(double diametr, double depth)
        {
            if (diametr == 8 && depth == 0)
            {
                return 3;
            }
            if (diametr == 5 && depth == 12)
            {
                return 1;
            }
            if (diametr == 5 && depth == 0)
            {
                return 0;
            }
            if (diametr == 8 && depth != 0)
            {
                return 2;
            }
            if (diametr == 8.11)
            {
                return 4;
            }
            return 5;
        }

        private double GetDepthFor(int d)
        {
            var sldDraw = (DrawingDoc)_swModel;
            var sldView = sldDraw.IGetFirstView();
            var swNote = (Note)sldView.GetFirstNote();
            _swModel.ClearSelection2((true));
            while (swNote != null)
            {
                var swAnn = (Annotation)swNote.GetAnnotation();
                swAnn.Select2(true, 0);
                switch (d)
                {
                    case 20:
                        if (Strings.InStr(swNote.GetText(), "16 мм", CompareMethod.Text) != 0)
                            return 12.5;
                        if (Strings.InStr(swNote.GetText(), "19 мм", CompareMethod.Text) != 0)
                            return 14.5;
                        if (Strings.InStr(swNote.GetText(), "25 мм", CompareMethod.Text) != 0)
                            return 14.5;
                        break;
                    case 8:
                        if (Strings.InStr(swNote.GetText(), "16 мм", CompareMethod.Text) != 0)
                            return 22.0;
                        if (Strings.InStr(swNote.GetText(), "19 мм", CompareMethod.Text) != 0)
                            return 22.0;
                        if (Strings.InStr(swNote.GetText(), "25 мм", CompareMethod.Text) != 0)
                            return 22.0;
                        if (Strings.InStr(swNote.GetText(), "38 мм", CompareMethod.Text) != 0)
                            return 32.0;
                        if (Strings.InStr(swNote.GetText(), "32 мм", CompareMethod.Text) != 0)
                            return 30.0;
                        break;
                }
                swNote = (Note)swNote.GetNext();
            }
            _swModel.ClearSelection2((true));
            return 0;
        }

        public void Dispose()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }
    }

    class InsertDimension : IDisposable
    {
        private readonly ModelDoc2 _swModel;
        private readonly Entity _ent;
        private readonly Ordinate _vertex;
        private readonly SelectData _swSelData;
        public InsertDimension(ISldWorks swApp, Entity entity, Ordinate vertex, SelectData swSelData)
        {
            _swModel = swApp.IActiveDoc2;
            _ent = entity;
            _vertex = vertex;
            _swSelData = swSelData;
        }

        public CoordX FoundXcoord(double[] dimLine)
        {
            Annotation annX = null;
            double x = 0;
            if (SelectVertexDatum() && _ent.Select4(true, _swSelData))
            {
                var swDispDim = (DisplayDimension)_swModel.AddHorizontalDimension2(0, 0, 0);
                if (swDispDim != null)
                {
                    var obj = (Dimension)swDispDim.GetDimension();
                    if (obj != null)
                    {
                        var vector = (double[])obj.DimensionLineDirection.ArrayData;
                        //if (IsParallel(dimLine, vector))
                        //{
                            x = Convert.ToDouble(obj.Value.ToString("0.00"));
                            annX = swDispDim.IGetAnnotation();
                        //}
                        //else
                        //{
                        //    _swModel.EditDelete();
                        //}
                    }
                }
            }
            _swModel.ClearSelection();
            return new CoordX(x, annX);
        }

        private bool SelectVertexDatum()
        {
            return _swModel.Extension.SelectByID2("", "VERTEX", _vertex.X, _vertex.Y, 0, true,
                                          (int)swAutodimMark_e.swAutodimMarkOriginDatum, null, 0);
        }

        public CoordY FoundYcoord(double[] dimLine)
        {
            Annotation annY = null;
            double y = 0;
            if (SelectVertexDatum() && _ent.Select4(true, _swSelData))
            {
                var swDispDim = (DisplayDimension)_swModel.AddVerticalDimension2(0, 0, 0);
                if (swDispDim != null)
                {
                    var obj = (Dimension)swDispDim.GetDimension();
                    if (obj != null)
                    {
                        var vector = (double[])obj.DimensionLineDirection.ArrayData;
                        //if (IsParallel(dimLine, vector))
                        //{
                            y = Convert.ToDouble(obj.Value.ToString("0.00"));
                            annY = swDispDim.IGetAnnotation();
                        //}
                        //else
                        //{
                        //    _swModel.EditDelete();
                        //}
                    }
                }
            }
            _swModel.ClearSelection();
            return new CoordY(y, annY);
        }


        private static bool IsParallel(double[] horEtalon, double[] dimLineParams)
        {
            return true;
            //if (((dimLineParams[0].ToString("0.000") == horEtalon[0].ToString("0.000")) ||
            //    (dimLineParams[0].ToString("0.000") == (-horEtalon[0]).ToString("0.000"))) &&
            //    ((dimLineParams[1].ToString("0.000") == horEtalon[1].ToString("0.000")) ||
            //    (dimLineParams[1].ToString("0.000") == (-horEtalon[1]).ToString("0.000"))) &&
            //    ((dimLineParams[2].ToString("0.000") == horEtalon[2].ToString("0.000")) ||
            //    (dimLineParams[2].ToString("0.000") == (-horEtalon[2]).ToString("0.000"))))
            //    return true;
            //return false;
        }

        public void Dispose()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }

    }
    
    class GetDimensions : IDisposable
    {

        public Ordinate SelectVertexDatumUp(ModelDoc2 swModel, double[] boundBox, BlockPosition left, bool clear = false)
        {
            var sizeX = boundBox[2] - boundBox[0];
            if (sizeX < 0)
                sizeX = -sizeX;
            var sizeY = boundBox[3] - boundBox[1];
            if (sizeY < 0)
                sizeY = -sizeY;
            var etalon = sizeX > sizeY ? sizeY : sizeX;
            double step = etalon / 40;


            var ordinate = new Ordinate(0, 0);
            if (left.AsBoolean())
            {
                for (int iX = 0; iX <= 30; iX++)
                {
                    for (int iY = 0; iY <= 30; iY++)
                    {
                        var boolstatus = swModel.Extension.SelectByID2("", "VERTEX", boundBox[0] + step * iX,
                                                                       boundBox[3] - step * iY, 0.0, true,
                                                                       (int)
                                                                       swAutodimMark_e.swAutodimMarkOriginDatum,
                                                                       null, 0);
                        if (boolstatus)
                        {
                            ordinate.X = boundBox[0] + step * iX;
                            ordinate.Y = boundBox[3] - step * iY;
                            if (clear)
                                swModel.ClearSelection();
                            return ordinate;
                        }
                    }
                }
            }
            else
            {
                for (int iX = 0; iX <= 30; iX++)
                {
                    for (int iY = 0; iY <= 30; iY++)
                    {
                        var boolstatus = swModel.Extension.SelectByID2("", "VERTEX", boundBox[2] - step * iX,
                                                                       boundBox[3] - step * iY, 0.0, true,
                                                                       (int)
                                                                       swAutodimMark_e.swAutodimMarkOriginDatum,
                                                                       null, 0);
                        if (boolstatus)
                        {
                            ordinate.X = boundBox[2] - step * iX;
                            ordinate.Y = boundBox[3] - step * iY;
                            if (clear)
                                swModel.ClearSelection();
                            return ordinate;
                        }
                    }
                }
            }
            if (clear)
                swModel.ClearSelection();
            return ordinate;
        }

        public void SelectVertexDatumDown(ModelDoc2 swModel, double[] boundBox, BlockPosition left)
        {
            var sizeX = boundBox[2] - boundBox[0];
            if (sizeX < 0)
                sizeX = -sizeX;
            var sizeY = boundBox[3] - boundBox[1];
            if (sizeY < 0)
                sizeY = -sizeY;
            var etalon = sizeX > sizeY ? sizeY : sizeX;
            double step = etalon / 40;

            if (left.AsBoolean())
            {
                for (int iX = 0; iX <= 30; iX++)
                {
                    for (int iY = 0; iY <= 30; iY++)
                    {
                        var boolstatus = swModel.Extension.SelectByID2("", "VERTEX", boundBox[0] + step * iX,
                                                                       boundBox[1] + step * iY, 0.0, true,
                                                                       (int)
                                                                       swAutodimMark_e.swAutodimMarkOriginDatum,
                                                                       null, 0);
                        if (boolstatus)
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                for (int iX = 0; iX <= 30; iX++)
                {
                    for (int iY = 0; iY <= 30; iY++)
                    {
                        var boolstatus = swModel.Extension.SelectByID2("", "VERTEX", boundBox[2] - step * iX,
                                                                       boundBox[1] + step * iY, 0.0, true,
                                                                       (int)
                                                                       swAutodimMark_e.swAutodimMarkOriginDatum,
                                                                       null, 0);
                        if (boolstatus)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public double[] GetDimensionLineDirect(ModelDoc2 swModel, double[] boundBox, bool vertic,BlockPosition side, out double d, out Annotation annotation,out bool? blockSize,out Ordinate vertex,out Ordinate vertexS,  double[] dsDim,double[] deDim,double[] endDim,string dsName,string deName)
        {
            blockSize = null;
            var swDrawing = (DrawingDoc) swModel;
            var drView = swDrawing.IActiveDrawingView;
            var scale = (double[]) drView.ScaleRatio;
            double vScaleRat = scale[1];
            double[] doubles = null;
            d = 0;
            annotation = null;
            
            vertex = GetVertexFromBlock(swModel, drView, vScaleRat,true);
            if(!(vertex!=null && SelectVertexDatum(swModel, vertex)))
                vertex = SelectVertexDatumUp(swModel, boundBox, side);

            if (dsDim != null && deDim != null)
            {
                vertexS = GetVertexFromBlock(swModel, drView, vScaleRat, false);
                //swModel.ClearSelection2(true);

                swDrawing.ActivateView(drView.Name);
                bool tmp = swModel.Extension.SelectByID2(@"Точка вставки/" + dsName, "SKETCHPOINT", 0, 0, 0, false, 0, null, 0);
                tmp = swModel.Extension.SelectByID2(@"Точка вставки/" + deName, "SKETCHPOINT", 0, 0, 0, true, 0, null, 0);
                DisplayDimension dim1;
                if (vertic)
                     dim1 = swModel.AddVerticalDimension2(0, 0, 0);
                else
                     dim1 = swModel.AddHorizontalDimension2(0, 0, 0);

                dim1.GetDimension().DrivenState = (int)swDimensionDrivenState_e.swDimensionDriven;
                d = dim1.GetDimension().Value;
                if (vertic)
                {
                    doubles = new double[3] { 1, 0, 0 };
                }
                else
                {
                    doubles = new double[3] {0, 1, 0};
                }
                annotation = dim1.GetAnnotation();
            }
            else
            {


                if (vertic)
                {
                    vertexS = GetVertexFromBlock(swModel, drView, vScaleRat, false);
                    if (!(vertexS != null && SelectVertexDatum(swModel, vertexS)))
                        SelectVertexDatumDown(swModel, boundBox, side);
                    else if (vertex != null)
                        blockSize = vertex.X < vertexS.X;

                    DisplayDimension dim = null;

                    dim = swModel.IAddVerticalDimension2(0, 0, 0);



                    if (dim != null)
                    {
                        d = dim.IGetDimension().Value;
                        doubles = (double[]) dim.IGetDimension().DimensionLineDirection.ArrayData;
                        if (blockSize != null)
                        {
                            vertexS.Y = vertexS.Y + (d/1000)/vScaleRat;
                        }

                        annotation = dim.IGetAnnotation();
                    }
                }
                else
                {
                    vertexS = GetVertexFromBlock(swModel, drView, vScaleRat, false);
                    if (!(vertexS != null && SelectVertexDatum(swModel, vertexS)))
                    {
                        vertexS = SelectVertexDatumUp(swModel, boundBox, side.Not());
                    }
                    else if (vertex != null)
                        blockSize = vertex.X < vertexS.X;


                    var dim = swModel.AddHorizontalDimension2(0, 0, 0);

                    if (dim != null)
                    {
                        d = dim.GetDimension().Value;
                        doubles = (double[]) dim.GetDimension().DimensionLineDirection.ArrayData;
                        annotation = dim.GetAnnotation();

                    }
                }
            }
            swModel.ClearSelection();
            return doubles;
        }

        public DisplayDimension GetDisplayDim(DrawingDoc swDrawing, Entity ent, SelectData swSelData)
        {
            ent.Select4(true, swSelData);
            return (DisplayDimension)swDrawing.AddHoleCallout2(0, 0, 0);
        }

        public string CreateBlock(ISldWorks swApp, int type, out MathPoint instancePosition, double x = 0, double y = 0 )
        {
            var objPoint1 = new double[3];
            objPoint1[0] = 0;
            objPoint1[1] = 0;
            objPoint1[2] = 0;
            var swMathUtil1 = swApp.IGetMathUtility();
            instancePosition = (MathPoint)swMathUtil1.CreatePoint(objPoint1);

            int i = 0;
            switch (type)
            {
                case 0:
                    i = 4;
                    break;
                case 1:
                    i = 6;
                    break;
                case 2:
                    i = 2;
                    break;
                case 3:
                    i = 3;
                    break;
                case 4:
                    i = 6;
                    break;
            }
            double blScale = 0.05;
            var swModel = (ModelDoc2)swApp.ActiveDoc;
            var swSkSeg = new ISketchSegment[i];
            swModel.SetAddToDB(true);
            switch (type)
            {
                case 0: // 5 through
                    swSkSeg[0] = (SketchSegment)swModel.CreateLine2(-0.04, 0.0, 0.0, 0.0, 0.04, 0.0);
                    swSkSeg[1] = (SketchSegment)swModel.CreateLine2(0.0, 0.04, 0.0, 0.04, 0.0, 0.0);
                    swSkSeg[2] = (SketchSegment)swModel.CreateLine2(0.04, 0.0, 0.0, 0.0, -0.04, 0.0);
                    swSkSeg[3] = (SketchSegment)swModel.CreateLine2(0.0, -0.04, 0.0, -0.04, 0.0, 0.0);
                    break;
                case 1: // 5 H 12
                    swSkSeg[0] = (SketchSegment)swModel.CreateCircle2(0.0, 0.0, 0.0, 0.003, 0.0, 0.0);
                    swSkSeg[2] = (SketchSegment)swModel.CreateCircle2(0.0, 0.0, 0.0, 0.0045, 0.0, 0.0);
                    swSkSeg[3] = (SketchSegment)swModel.CreateCircle2(0.0, 0.0, 0.0, 0.006, 0.0, 0.0);
                    swSkSeg[4] = (SketchSegment)swModel.CreateCircle2(0.0, 0.0, 0.0, 0.0075, 0.0, 0.0);
                    swSkSeg[5] = (SketchSegment)swModel.CreateCircle2(0.0, 0.0, 0.0, 0.009, 0.0, 0.0);
                    blScale = 0.075;
                    break;
                case 2: // 8
                    swSkSeg[0] = (SketchSegment)swModel.CreateLine2(-0.04, -0.04, 0.0, 0.04, 0.04, 0.0);
                    swSkSeg[1] = (SketchSegment)swModel.CreateLine2(-0.04, 0.04, 0.0, 0.04, -0.04, 0.0);
                    blScale = 0.06;
                    break;
                case 3: //8 through
                    swSkSeg[0] = (SketchSegment)swModel.CreateLine2(-0.04, -0.0293, 0.0, 0.0, 0.04, 0.0);
                    swSkSeg[1] = (SketchSegment)swModel.CreateLine2(0.0, 0.04, 0.0, 0.04, -0.0293, 0.0);
                    swSkSeg[2] = (SketchSegment)swModel.CreateLine2(0.04, -0.0293, 0.0, -0.04, -0.0293, 0.0);
                    break;
                case 4: //811
                    swSkSeg[0] = (SketchSegment)swModel.CreateLine2(-0.04, -0.04, 0.0, -0.04, 0.04, 0.0);
                    swSkSeg[1] = (SketchSegment)swModel.CreateLine2(-0.04, 0.04, 0.0, 0.04, 0.04, 0.0);
                    swSkSeg[2] = (SketchSegment)swModel.CreateLine2(0.04, 0.04, 0.0, 0.04, -0.04, 0.0);
                    swSkSeg[3] = (SketchSegment)swModel.CreateLine2(0.04, -0.04, 0.0, -0.04, -0.04, 0.0);
                    swSkSeg[4] = (SketchSegment)swModel.CreateLine2(-0.04, -0.04, 0.0, 0.04, 0.04, 0.0);
                    swSkSeg[5] = (SketchSegment)swModel.CreateLine2(-0.04, 0.04, 0.0, 0.04, -0.04, 0.0);
                    blScale = 0.03;
                    break;
            }
            object vSkSeg = swSkSeg;
            swModel.Extension.MultiSelect(vSkSeg, true, null);
            SketchBlockDefinition swSketchBlockDef = swModel.SketchManager.MakeSketchBlockFromSelected(null);
            var swInst = swSketchBlockDef.IGetInstances(1);
            swInst.Scale = blScale;
            if (x != 0 && y != 0)
            {
                var objPoint = new double[3];
                objPoint[0] = x+0.01;
                objPoint[1] = y;
                objPoint[2] = 0;
                var swMathUtil = swApp.IGetMathUtility();
                var swMathPoint = (MathPoint)swMathUtil.CreatePoint(objPoint);
                swInst.InstancePosition = swMathPoint;
                instancePosition = swMathPoint;
            }
            swModel.SetAddToDB(false);
            swModel.GraphicsRedraw2();
            swModel.ClearSelection();
            return swInst.Name;
        }

        public static BlockPosition GetBlockPosition(ModelDoc2 swModel, out bool consider)
        {
            consider = true;
            var swDrawing = (DrawingDoc)swModel;
            var drView = swDrawing.IActiveDrawingView;
            var objSketchBlockDef = swModel.SketchManager.GetSketchBlockDefinitions();
            double[] dimsDS = new double[0], dimsDE = new double[0];
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
                                var swSketBlInst = (SketchBlockInstance)objSketBlInst;

                                if (swSketBlInst.Name.Contains("DS" + drView.Name))
                                {
                                    //dsSketchBlockInstance = swSketBlInst;
                                    var point = swSketBlInst.InstancePosition;
                                    dimsDS = (double[])point.ArrayData;

                                }
                                if (swSketBlInst.Name.Contains("DE" + drView.Name))
                                {
                                    //deSketchBlockInstance = swSketBlInst;
                                    var point = swSketBlInst.InstancePosition;
                                    dimsDE = (double[])point.ArrayData;
                                }
                            }
                }
            }
            if (dimsDS.Length < 2 || dimsDE.Length < 2)
            {
                consider = false;
                return BlockPosition.LeftTopToRightBottom; // дефолтная ориентация
            }
            if (dimsDS[0] < dimsDE[0] && dimsDS[1] > dimsDE[1])
                return BlockPosition.LeftTopToRightBottom;
            if (dimsDS[0] < dimsDE[0] && dimsDS[1] < dimsDE[1])
                return BlockPosition.LeftBottomToRightTop;
            if (dimsDS[0] > dimsDE[0] && dimsDS[1] < dimsDE[1])
                return BlockPosition.RigthBottomToLeftTop;
            if (dimsDS[0] > dimsDE[0] && dimsDS[1] > dimsDE[1])
                return BlockPosition.RightTopToLeftBottom;
            consider = false;
            return BlockPosition.LeftTopToRightBottom; // дефолтная ориентация
        }
        public Ordinate GetVertexFromBlock(ModelDoc2 swModel, View drView, double vScaleRat, bool up)
        {
            Ordinate vertex = null;
            var objSketchBlockDef = swModel.SketchManager.GetSketchBlockDefinitions();
            if (objSketchBlockDef != null)
            {
                var objSketBlDefs = (object[]) objSketchBlockDef;
                foreach (var objSketBlDef in objSketBlDefs)
                {
                    var swSketBlDef = (SketchBlockDefinition) objSketBlDef;

                    //if ((Path.GetFileNameWithoutExtension(swSketBlDef.FileName) == ("DS" + drView.Name) && up) ||
                       //(Path.GetFileNameWithoutExtension(swSketBlDef.FileName) == ("DE" + drView.Name) && !up))
                    {
                        var objSketBlInsts = (object[]) swSketBlDef.GetInstances();
                        if (objSketBlInsts != null)
                            foreach (var objSketBlInst in objSketBlInsts)
                            {
                                var swSketBlInst = (SketchBlockInstance) objSketBlInst;
                                
                                if ((swSketBlInst.Name.Contains("DS" + drView.Name) && up) ||
                                    (swSketBlInst.Name.Contains("DE" + drView.Name) && !up))
                                {
                                    var pos = (double[]) drView.Position;
                                    var point = swSketBlInst.InstancePosition;
                                    var dims = (double[]) point.ArrayData;
                                    double y = dims[1]/vScaleRat;
                                    double x = dims[0]/vScaleRat;
                                    x = pos[0] + x;
                                    y = pos[1] + y;
                                    vertex = new Ordinate(x, y);
                                }
                            }
                    }

                }
            }
            return vertex;
        }

        public bool SelectVertexDatum (ModelDoc2 swModel,Ordinate vertex)
        {
            return swModel.Extension.SelectByID2("", "VERTEX", vertex.X, vertex.Y, 0, true,
                                          (int)swAutodimMark_e.swAutodimMarkOriginDatum, null, 0);
        }

        public void Dispose()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }
    }

    class Coord
    {
        public CoordX CooX;
        public CoordY CooY;

        public Coord(CoordX inCooX, CoordY inCooY)
        {
            CooX = inCooX;
            CooY = inCooY;
        }
    }

    class CoordX
    {
        public double X;
        public Annotation AnnX;

        public CoordX(double inX, Annotation inAnnX)
        {
            X = inX;
            AnnX = inAnnX;
        }
    }

    class CoordY
    {
        public double Y;
        public Annotation AnnY;

        public CoordY(double inY, Annotation inAnnY)
        {
            Y = inY;
            AnnY = inAnnY;
        }
    }

    class CoordH
    {
        public double X;
        public double Y;
        public Annotation AnnH;
        public DisplayDimension DispDim;

        public CoordH(double inX, double inY, Annotation inAnnH, DisplayDimension inDispDim)
        {
            X = inX;
            Y = inY;
            AnnH = inAnnH;
            DispDim = inDispDim;
        }
    }

    public class Ordinate
    {
        public double X;
        public double Y;

        public Ordinate(double inX, double inY)
        {
            X = inX;
            Y = inY;
        }
    }

    class Common
    {
        public Coord Coord;
        public Entity Entity;

        public Common(Coord inCoord, Entity inEntity)
        {
            Coord = inCoord;
            Entity = inEntity;
        }
    }

    class Common2
    {
        public Ordinate Ordinate;
        public Entity Entity;

        public Common2(Ordinate inOrdinate, Entity inEntity)
        {
            Ordinate = inOrdinate;
            Entity = inEntity;
        }
    }

    class SizeForDim
    {
        public double XLeft;
        public double YUp;
        public double XRight;
        public double YDown;
        public double X;
        public double Y;
        public double[] Bound;
        public View View;

        public SizeForDim(double xLeft, double yUp, double xRight, double yDown, double x, double y, double[] bound, View view)
        {
            XLeft = xLeft;
            YUp = yUp;
            XRight = xRight;
            YDown = yDown;
            X = x;
            Y = y;
            Bound = bound;
            View = view;
        }
    }

    struct Max
    {
        public double Adding;
        public double Main;
    }

    class CoordAndDepth
    {
        public double X;
        public double Y;
        public double Diameter;
        public double Depth;

        public CoordAndDepth(double x, double y, double diameter, double depth)
        {
            X = x;
            Y = y;
            Diameter = diameter;
            Depth = depth;
        }
    }
}
