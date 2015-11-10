using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Furniture.Exceptions;
using Microsoft.VisualBasic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SwDocumentMgr;
using View = SolidWorks.Interop.sldworks.View;

namespace Furniture.FinalProcessing
{
    /// <summary>
    /// Класс для создания xml-программ для чертежей
    /// </summary>
    public static class XmlProgram
    {
        /// <summary>
        /// Структура для хранения параметров выреза под присадку
        /// </summary>
        private struct HoleParameters
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double Diameter { get; set; }
            public double Depth { get; set; }
        }

        /// <summary>
        /// Создает xml-программу для чертежа и сохраняет её
        /// </summary>
        /// <param name="drawingModel">Модель чертежа</param>
        /// <param name="rootModel">Модель компонента, для которого построен чертеж</param>
        /// <param name="targetFolderPath">Путь к папке, в которую нужно сохранить xml-программу</param>
        public static void CreateAndSave(ISldWorks app, ModelDoc2 drawingModel, string targetFolderPath, Furniture.SwAddin swAddin)
        {
            XmlDocument xmlProgramDocument = new XmlDocument();

            DrawingDoc drawing = (DrawingDoc)drawingModel;

            //if (!Directory.Exists(targetFolderPath))
            //Directory.CreateDirectory(targetFolderPath);

            string drawingPathName = Path.GetFileName(drawingModel.GetPathName());
            string drawingNameWithoutExt = Path.GetFileNameWithoutExtension(drawingModel.GetPathName());
            drawingNameWithoutExt = drawingNameWithoutExt.Substring(drawingNameWithoutExt.Length - 4, 4);

            string rootModelPathName = drawingModel.GetPathName().Replace("SLDDRW", "SLDASM");
            if (!File.Exists(rootModelPathName))
                throw new FileNotFoundException("Не найден файл модели, для которой построен чертеж. " + rootModelPathName);

            ModelDoc2 rootModel = (ModelDoc2)app.OpenDoc(rootModelPathName, (int)swDocumentTypes_e.swDocASSEMBLY);
            if (rootModel == null)
                throw new Exception("Не удалось открыть файл модели, для которой построен чертеж. " + rootModelPathName);

            string sketchNumber = rootModel.GetCustomInfoValue("", "Sketch Number");
            if (string.IsNullOrEmpty(sketchNumber))
                throw new ProperyNotFoundException("Значение свойства Sketch Number не найдено в " + rootModelPathName, rootModel);

            string orderNumber = rootModel.GetCustomInfoValue("", "Order Number");
            if (string.IsNullOrEmpty(orderNumber))
                throw new ProperyNotFoundException("Значение свойства Sketch Number не найдено в rootModelPathName", rootModel);

            string targetXmlFilePath = Path.Combine(targetFolderPath, orderNumber + "_" + sketchNumber + ".xml");

            if (File.Exists(targetXmlFilePath))
                File.Delete(targetXmlFilePath);

            XmlElement modelElement = xmlProgramDocument.CreateElement("Model");
            modelElement.SetAttribute("Name", drawingPathName);
            modelElement.SetAttribute("CNCValid", "True"); 
            modelElement.SetAttribute("vAddInn", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            modelElement.SetAttribute("vLib", Properties.Settings.Default.PatchVersion);
            XmlNode modelNode = xmlProgramDocument.AppendChild(modelElement);

            bool commentAttached = false;

            string[] sheetNames = (string[])drawing.GetSheetNames();
            //номер текущего листа
            int sheetNumber = 0;
            foreach (string sheetName in sheetNames)
            {


                DrawingDoc ddoc = drawingModel as DrawingDoc;
                bool b2 = ddoc.ActivateSheet(sheetName);


                XmlElement sheetElement = xmlProgramDocument.CreateElement("Sheet");
                sheetElement.SetAttribute("Name", sheetName);
                XmlNode sheetNode = modelNode.AppendChild(sheetElement);
                bool tableName = false;
                Sheet sheet = (Sheet)drawing.get_Sheet(sheetName);
                object[] views = (object[])sheet.GetViews(); //а не линкед лист тут, м?

                Dictionary<string, bool> listSide;
                bool isNeededSheetNumber = XmlProgram.PrepareDrawingDoc(drawingModel, out listSide, swAddin);

                bool side = sheetNumber == 1;
                if (isNeededSheetNumber && listSide.ContainsKey(sheetName.Substring(sheetName.Length - 1)))
                    side = listSide[sheetName.Substring(sheetName.Length - 1)];
                if (sheetName.ToUpper().Contains("FACE"))
                    side = true;
                if (sheetName.ToUpper().Contains("BACK"))
                    side = false;

                KeyValuePair<string, string> tableNameAttribute = new KeyValuePair<string, string>();

                if (views != null)
                {
                    List<string> slist = new List<string>();
                  
                    foreach (View view in views)
                    {
                        #region проход по видам

                        string name2 = view.GetName2();
                        bool b = ddoc.ActivateView(name2);
                        ModelDoc2 refModel = view.ReferencedDocument;

                        if (view.Name == "F1")
                        {
                            //если есть хотя бы один вид F1, то формируем comment
                            if (!commentAttached)
                            {
                                XmlElement tmpElem = xmlProgramDocument.CreateElement("Comment");
                                XmlProgram.CreateCommentElement(swAddin, refModel, view, tmpElem);
                                modelNode.PrependChild(tmpElem);
                                commentAttached = true;
                            }
                        }


                        double[] dsDim, deDim, endDim;
                        string dsName, deName;

                        //DimensionView.GetBlockPositions(app.IActiveDoc2, out dsDim, out deDim, out endDim, out dsName, out deName);
                        #region GetBlockPosition

                        DrawingDoc swDrawing = (DrawingDoc)app.IActiveDoc2;
                        var objSketchBlockDef = app.IActiveDoc2.SketchManager.GetSketchBlockDefinitions();
                        dsDim = null;
                        deDim = null;
                        endDim = null;
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

                                        if (swSketBlInst.Name.Contains("DS" + view.Name))
                                        {
                                            var point = swSketBlInst.InstancePosition;
                                            dsDim = (double[])point.ArrayData;
                                            dsName = swSketBlInst.Name;
                                        }
                                        if (swSketBlInst.Name.Contains("EndDimArea" + view.Name))
                                        {
                                            var point = swSketBlInst.InstancePosition;
                                            endDim = (double[])point.ArrayData;
                                        }
                                        if (swSketBlInst.Name.Contains("DE" + view.Name))
                                        {
                                            var point = swSketBlInst.InstancePosition;
                                            deDim = (double[])point.ArrayData;
                                            deName = swSketBlInst.Name;
                                        }
                                    }
                            }
                        }

                        #endregion


                        BlockPosition _side = BlockPositionExtension.FromBool(side);
                        double[] dimLineH, dimLineV;
                        bool? blockSize;
                        Ordinate vertex, vertexS;


                        #region AnalizViewCoefficient
                        //Common j = DimensionView.AnalizViewCoefficient(drawingModel, (double[])view.GetOutline(), ref _side, out dimLineH, out dimLineV, out blockSize, out vertex, out vertexS, dsDim, deDim, endDim, dsName, deName);

                        double x, y;
                        Annotation annX, annY;
                        BlockPosition sside = BlockPositionExtension.FromBool(side);
                        using (var a = new GetDimensions())
                        {
                            bool consider;

                            var tmpSide = GetDimensions.GetBlockPosition(drawingModel, out consider);//a.GetBlockPosition(swModel,out consider);
                            if (consider) // consider -принимать ли в расчет tmpSide
                                sside = tmpSide;
                            dimLineH = GetDimensionLineDirect(app, drawingModel, (double[])view.GetOutline(), false, sside, out x, out annX, out blockSize, out vertex, out vertexS, dsDim, deDim, endDim, dsName, deName);
                        }

                        //если криволинейка то почему-то слетает выделение вида. выделяем
                        b = ddoc.ActivateView(name2);

                        using (var a = new GetDimensions())
                        {
                            Ordinate vert, verts;
                            dimLineV = GetDimensionLineDirect(app, drawingModel, (double[])view.GetOutline(), true, sside, out y, out annY, out blockSize, out vert, out verts, dsDim, deDim, endDim, dsName, deName);
                            if (verts != null)
                                vertexS = verts;
                        }
                        Common j = new Common(new Coord(new CoordX(x, annX), new CoordY(y, annY)), null);


                        #endregion

                        //сначала надо получить данные детали  в id0

                        var gh = view.Position;

                        string digit = ""; //Y 

                        string materialName = refModel.get_CustomInfo2("", "Material");
                        if (materialName == "" && refModel.GetConfigurationCount() > 1)
                        {
                            string val, resolvedVal;
                            if (refModel.Extension.get_CustomPropertyManager(refModel.IGetActiveConfiguration().Name).Get4("Material", false, out val, out resolvedVal))
                                materialName = val;
                        }
                        Z = ParseMaterialName(materialName);
                        Regex regex = new Regex(@"\d+");
                        MatchCollection matchCollection = regex.Matches(materialName);
                        if (matchCollection.Count == 1)
                            digit = matchCollection[0].Value;
                        else
                            throw new Exception("");

                        //получить данные по вырезам

                        XmlElement viewElement = xmlProgramDocument.CreateElement("View");
                        viewElement.SetAttribute("Name", view.Name);
                        XmlNode viewNode = sheetNode.AppendChild(viewElement);

                        object[] entities = (object[])view.GetVisibleEntities(null, (int)swViewEntityType_e.swViewEntityType_Edge);

                        try
                        {
                            int i = 1;


                            List<Common> list = new System.Collections.Generic.List<Common> { j };
                            SelectData selectData = ((ModelDoc2)app.IActiveDoc2).ISelectionManager.CreateSelectData();

                            List<HoleParameters> holesParameters = new List<HoleParameters> { new HoleParameters { X = j.Coord.CooX.X, Y = j.Coord.CooY.Y, Z = Z } };

                            foreach (Entity entity in entities.Cast<Entity>())
                            {
                                #region проход по вырезам на виде


                              


                                XmlElement holeElement = xmlProgramDocument.CreateElement("id" + i.ToString());
                                i++;

                                
                                bool l = entity.Select4(false, selectData);

                                //todo: почему-то не выбирается иногда с первого раза. как-то надо изменить этот говнокод
                                DisplayDimension dd = (DisplayDimension)drawing.AddHoleCallout2(0, 0, 0);
                                if (dd == null)
                                    dd = (DisplayDimension)drawing.AddHoleCallout2(0, 0, 0);

                                if (dd != null)
                                {
                                    #region попытка считывать координаты

                                    double Diameter = 0;
                                    double Depth = 0;

                                    try
                                    {

                                        #region var fc = new FoundCoordinate(app, entity, vertex, dimLineH, dimLineV, _side, view, side)

                                        ModelDoc2 _swModel = app.IActiveDoc2;

                                        Annotation annH;
                                        DisplayDimension swDispDimH;

                                        annH = null;

                                        int i2 = 0;
                                        do
                                        {
                                            using (var a = new GetDimensions())
                                            {
                                                swDispDimH = a.GetDisplayDim(swDrawing, entity, _swModel.ISelectionManager.CreateSelectData());
                                            }
                                            i2++;
                                        }
                                        while (swDispDimH == null && i2 < 3);

                                        if (swDispDimH != null)
                                        {
                                            Dimension dimObjH = (Dimension)swDispDimH.GetDimension();
                                            string holeDepSuffix = swDispDimH.GetText((int)swDimensionTextParts_e.swDimensionTextPrefix);
                                            if (dimObjH != null)
                                            {
                                                double diam = Convert.ToDouble(Strings.Left(holeDepSuffix, 1) == "R"
                                                                                   ? (dimObjH.Value * 2).ToString("0.00")
                                                                                   : (dimObjH.Value).ToString("0.00"));

                                                var holeDepSuffix1 = swDispDimH.GetText((int)swDimensionTextParts_e.swDimensionTextSuffix);
                                                if (string.IsNullOrEmpty(holeDepSuffix1) && !string.IsNullOrEmpty(holeDepSuffix))
                                                {
                                                    var tt = holeDepSuffix.Split(new string[1] { "<HOLE-DEPTH>" }, 5, StringSplitOptions.RemoveEmptyEntries);
                                                    if (tt.Length >= 2)
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
                                                var diamAndDepth = SetManualDiametrAndDepth(diam, depth, type, app.IActiveDoc2);
                                                if (side || diamAndDepth.Y != 0)//if (_side.AsBoolean() || diamAndDepth.Y != 0) // насколько я понимаю, только этот if определяет, указывать ли сквознае отверстия
                                                {
                                                    //StrType = GetStringHoleType(diamAndDepth.X, diamAndDepth.Y);
                                                    //_holeType = DefinitionHolesType(diamAndDepth.X, diamAndDepth.Y);
                                                    annH = swDispDimH.IGetAnnotation();

                                                }
                                                else
                                                {
                                                    swDispDimH.IGetAnnotation().Select(true);
                                                    _swModel.DeleteSelection(true);
                                                }

                                                Diameter = diamAndDepth.X;
                                                if (diamAndDepth.Y != 0)
                                                    Depth = diamAndDepth.Y;
                                                else
                                                {
                                                    Depth = diam == 8.11 ? 22 : 0;
                                                }


                                            }

                                            if (((ModelDoc2)app.ActiveDoc).Extension.SelectByID2(swDispDimH.GetNameForSelection(), "DIMENSION", 0, 0, 0, false, 0, null, 0))
                                                ((ModelDoc2)app.ActiveDoc).EditDelete();
                                        }
                                        _swModel.ClearSelection();


                                        int HoleType = 5;

                                        if (Diameter == 8 && Depth == 0)
                                        {
                                            HoleType = 3;
                                        }
                                        if (Diameter == 5 && Depth == 12)
                                        {
                                            HoleType = 1;
                                        }
                                        if (Diameter == 5 && Depth == 0)
                                        {
                                            HoleType = 0;
                                        }
                                        if (Diameter == 8 && Depth != 0)
                                        {
                                            HoleType = 2;
                                        }
                                        if (Diameter == 8.11)
                                        {
                                            HoleType = 4;
                                        }


                                        if (HoleType < 5)
                                        {
                                            using (var a = new GetDimensions())
                                            {
                                                MathPoint instancePosition;
                                                if (_swModel.Extension.SelectByID2("Точка вставки/" + a.CreateBlock(app, HoleType, out instancePosition),
                                                                          "SKETCHPOINT", 0, 0, 0, true, 0, null, 0) && entity.Select(true))
                                                    _swModel.SketchAddConstraints("sgCONCENTRIC");
                                                _swModel.ClearSelection();
                                            }
                                        }

                                        double X = 0;
                                        //using (var c = new InsertDimension(app, entity, vertex, _swModel.ISelectionManager.CreateSelectData()))

                                        //X = c.FoundXcoord(dimLineH).X;

                                        bool flag = _swModel.Extension.SelectByID2("", "VERTEX", vertex.X, vertex.Y, 0, true, (int)swAutodimMark_e.swAutodimMarkOriginDatum, null, 0);
                                        if (flag && entity.Select4(true, _swModel.ISelectionManager.CreateSelectData()))
                                        {
                                            var swDispDim = (DisplayDimension)_swModel.AddHorizontalDimension2(0, 0, 0);
                                            if (swDispDim != null)
                                            {
                                                var obj = (Dimension)swDispDim.GetDimension();
                                                if (obj != null)
                                                {
                                                    var vector = (double[])obj.DimensionLineDirection.ArrayData;
                                                    X = Convert.ToDouble(obj.Value.ToString("0.00"));
                                                }
                                            }
                                            if (((ModelDoc2)app.ActiveDoc).Extension.SelectByID2(swDispDim.GetNameForSelection(), "DIMENSION", 0, 0, 0, false, 0, null, 0))
                                                ((ModelDoc2)app.ActiveDoc).EditDelete();
                                        }
                                        _swModel.ClearSelection();






                                        double Y = 0;
                                        //using (var c = new InsertDimension(app, entity, vertex, _swModel.ISelectionManager.CreateSelectData()))

                                        //Y = c.FoundYcoord(dimLineV).Y;



                                        flag = _swModel.Extension.SelectByID2("", "VERTEX", vertex.X, vertex.Y, 0, true, (int)swAutodimMark_e.swAutodimMarkOriginDatum, null, 0);
                                        if (flag && entity.Select4(true, _swModel.ISelectionManager.CreateSelectData()))
                                        {
                                            var swDispDim = (DisplayDimension)_swModel.AddVerticalDimension2(0, 0, 0);
                                            if (swDispDim != null)
                                            {
                                                var obj = (Dimension)swDispDim.GetDimension();
                                                if (obj != null)
                                                {
                                                    var vector = (double[])obj.DimensionLineDirection.ArrayData;

                                                    Y = Convert.ToDouble(obj.Value.ToString("0.00"));

                                                }
                                            }
                                            if (((ModelDoc2)app.ActiveDoc).Extension.SelectByID2(swDispDim.GetNameForSelection(), "DIMENSION", 0, 0, 0, false, 0, null, 0))
                                                ((ModelDoc2)app.ActiveDoc).EditDelete();
                                        }
                                        _swModel.ClearSelection();


                                        #endregion

                                        holeElement.SetAttribute("X", X.ToString());
                                        holeElement.SetAttribute("Y", Y.ToString());
                                        holeElement.SetAttribute("Diameter", Diameter.ToString());
                                        holeElement.SetAttribute("Depth", Depth.ToString());
                                        holesParameters.Add(new HoleParameters { X = X, Y = Y, Diameter = Diameter, Depth = Depth });
                                        list.Add(new Common(new Coord(new CoordX(X, null), new CoordY(Y, null)), entity));

                                    }
                                    catch { }

                                    #endregion

                                    if (((ModelDoc2)app.ActiveDoc).Extension.SelectByID2(dd.GetNameForSelection(), "DIMENSION", 0, 0, 0, false, 0, null, 0))
                                        ((ModelDoc2)app.ActiveDoc).EditDelete();

                                    //viewNode.AppendChild(holeElement);
                                }
                                entity.DeSelect();

                                #endregion
                            }

                            var scale = (double[])view.ScaleRatio;
                            double vScaleRat = scale[1];

                            XmlElement firstElement = xmlProgramDocument.CreateElement("id0");                          

                            var xl = holesParameters.Select(x2 => x2.X).ToList();
                            xl.Sort((x2, y2) => x2.CompareTo(y2));
                            double X2 = xl.Last();

                            var yl = holesParameters.Select(x2 => x2.Y).ToList();
                            yl.Sort((x2, y2) => x2.CompareTo(y2));
                            double Y2 = yl.Last();
                                                      
                            firstElement.SetAttribute("X", Math.Round(X2).ToString());
                            firstElement.SetAttribute("Y", Math.Round(Y2).ToString());
                            if (view.Name == "F1" || view.Name == "F6")
                                firstElement.SetAttribute("Z", Math.Round(Z).ToString());
                            viewNode.PrependChild(firstElement);

                            //сортируем список координат по возрастанию Y, для одинаковых Y сортируем по возрастанию X
                            holesParameters.Sort((a111, b111) =>
                                {                                   
                                    if (a111.Y == b111.Y)
                                    {
                                        if (a111.X == b111.X)
                                            return 0;
                                        else
                                        {
                                            if (a111.X > b111.X)
                                                return 1;
                                            else
                                                return -1;
                                        }
                                    }
                                    else 
                                    {
                                        if (a111.Y > b111.Y)
                                            return 1;
                                        else
                                            return -1;
                                    }                                   
                                });

                            int ii = 1;
                            foreach (var r in holesParameters)
                            {
                                if (r.Z != 0)
                                    break;
                                XmlElement holeElement = xmlProgramDocument.CreateElement("id" + ii.ToString());
                                holeElement.SetAttribute("X", Math.Round(r.X, 2).ToString());
                                holeElement.SetAttribute("Y", Math.Round(r.Y, 2).ToString());
                                holeElement.SetAttribute("Diameter", r.Diameter.ToString());
                                holeElement.SetAttribute("Depth", r.Depth.ToString());
                                viewNode.AppendChild(holeElement);
                                ii++;
                            }

                          
                                #region определение атрибута TableName
                                switch (sside)
                                {
                                    case BlockPosition.LeftTopToRightBottom:

                                        if (view.Name == "F1")
                                            tableNameAttribute = new KeyValuePair<string, string>("F1", "J");
                                        if (view.Name == "F6")
                                        {
                                            if (tableNameAttribute.Key != "F1") // F1 -  приоритетнее
                                                tableNameAttribute = new KeyValuePair<string, string>("F6", "B");
                                        }
                                        if (string.IsNullOrEmpty(tableNameAttribute.Key))
                                        {
                                            tableNameAttribute = new KeyValuePair<string, string>("none", "J");
                                        }
                                        break;
                                    case BlockPosition.RightTopToLeftBottom:
                                        if (view.Name == "F1")
                                            tableNameAttribute = new KeyValuePair<string, string>("F1", "B");
                                        if (view.Name == "F6")
                                        {
                                            if (tableNameAttribute.Key != "F1") // F1 -  приоритетнее
                                                tableNameAttribute = new KeyValuePair<string, string>("F6", "J");
                                        }
                                        if (string.IsNullOrEmpty(tableNameAttribute.Key))
                                        {
                                            tableNameAttribute = new KeyValuePair<string, string>("none", "B");
                                        }
                                        break;
                               
                                #endregion
                               
                            }

                        }
                        catch { }

                        #endregion
                    }
                   
                    sheetNode = sheetNode.ParentNode;
                }

                if (!tableName)
                {
                    if (!string.IsNullOrEmpty(tableNameAttribute.Value))
                        sheetElement.SetAttribute("TableName", tableNameAttribute.Value);
                    tableName = true;
                }

                sheetNumber++;
            }

            xmlProgramDocument.Save(@"D:\xmlка.xml");
        }

        /// <summary>
        /// Создает xml-программу для чертежа
        /// </summary>
        /// <param name="drawingDoc">Модель чертежа</param>
        /// <param name="rootModel">Модель компонента, для которого построен чертеж</param>       
        /// <returns>Документ xml-программы</returns>
        public static XmlDocument Create(DrawingDoc drawingDoc, ModelDoc2 rootModel)
        {
            throw new NotImplementedException();
        }

        //причесать
        private static XmlElement CreateCommentElement(SwAddin swAddin, ModelDoc2 refModel, View view, XmlElement tmpElem)
        {
            SwDmDocumentOpenError oe;
            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
            SwDMDocument8 swDoc = (SwDMDocument8)swDocMgr.GetDocument(Path.ChangeExtension(refModel.GetPathName(), "SLDASM"), SwDmDocumentType.swDmDocumentAssembly, true, out oe);

            if (swDoc != null)
            {
                string[] propertyNames = swDoc.GetCustomPropertyNames();
                string faner11 = null, faner12 = null, faner21 = null, faner22 = null;
                double angle = 57.29577951308232 * view.Angle; //(180/П)

                if (Math.Abs(angle) < 0.000001 || Math.Abs(angle + 90) < 0.000001 || Math.Abs(angle - 270) < 0.000001 || Math.Abs(angle - 180) < 0.000001 || Math.Abs(angle - 90) < 0.000001) //!string.IsNullOrEmpty(extFeats) && extFeats == "Yes" &&
                {
                    SwDmCustomInfoType cit = new SwDmCustomInfoType();

                    if (propertyNames.Contains("Faner11"))
                        faner11 = swDoc.GetCustomProperty("Faner11", out cit);
                    if (propertyNames.Contains("Faner12"))
                        faner12 = swDoc.GetCustomProperty("Faner12", out cit);
                    if (propertyNames.Contains("Faner21"))
                        faner21 = swDoc.GetCustomProperty("Faner21", out cit);
                    if (propertyNames.Contains("Faner22"))
                        faner22 = swDoc.GetCustomProperty("Faner22", out cit);


                    string comment = XmlProgram.GetCommentFromProperties(faner11, faner12, faner21, faner22, angle, swAddin, refModel);
                    double angle2 = angle + 90;
                    double angle4 = angle + 180;
                    double angle3 = angle + 270;
                    if (angle2 > 270)
                        angle2 = angle2 % 360;
                    if (angle3 > 270)
                        angle3 = angle3 % 360;
                    if (angle4 > 270)
                        angle4 = angle4 % 360;
                    string comment2 = XmlProgram.GetCommentFromProperties(faner11, faner12, faner21, faner22, angle2, swAddin, refModel);
                    string comment3 = XmlProgram.GetCommentFromProperties(faner11, faner12, faner21, faner22, angle3, swAddin, refModel);
                    string comment4 = XmlProgram.GetCommentFromProperties(faner11, faner12, faner21, faner22, angle4, swAddin, refModel);
                    tmpElem.SetAttribute("Rot270", comment3);
                    tmpElem.SetAttribute("Rot180", comment4);
                    tmpElem.SetAttribute("Rot90", comment2);
                    tmpElem.SetAttribute("Rot0", comment);
                }
            }
            return null;
        }
        private static double Z = 0;
        //потом разберемся
        private static string GetCommentFromProperties(string faner11, string faner12, string faner21, string faner22, double angle, SwAddin _mSwAddin, ModelDoc2 model)
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
                            retresult = "VOLOKNA GORIZONTALNO";
                        else
                            retresult = "VOLOKNA VERTIKALNO";
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
                            retresult = "VOLOKNA VERTIKALNO";
                        else
                            retresult = "VOLOKNA GORIZONTALNO";
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
                            retresult = "VOLOKNA GORIZONTALNO";
                        else
                            retresult = "VOLOKNA VERTIKALNO";
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
                            retresult = "VOLOKNA VERTIKALNO";
                        else
                            retresult = "VOLOKNA GORIZONTALNO";
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
        [Flags]
        private enum FanersBools { Faner11 = 1, Faner12 = 2, Faner21 = 4, Faner22 = 8 }
        private static bool PrepareDrawingDoc(ModelDoc2 swModel, out Dictionary<string, bool> list, SwAddin _swAdd)
        {
            list = new Dictionary<string, bool>();
            bool ret = false;

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


            OleDbConnection oleDb;
            if (swAsmModel != null)
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
                Z = ParseMaterialName(matName); ;

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
                            if (listId.Contains((int)rd["id"]))
                            {
                                double val;
                                if (_swAdd.GetObjectValue(swAsmModel, (string)rd["name"], 14, out val) &&
                                    !dictIdSize.ContainsKey((int)rd["id"]))
                                    dictIdSize.Add((int)rd["id"], val);
                            }
                        }
                        rd.Close();
                        cm = new OleDbCommand("SELECT * FROM dimlimits", oleDb);
                        rd = cm.ExecuteReader();
                        while (rd.Read())
                        {
                            var lB = (from i in listId
                                      let mn = (int)rd["obj" + i + "min"]
                                      let mx = (int)rd["obj" + i + "max"]
                                      select (mn <= dictIdSize[i]) && (dictIdSize[i] <= mx)).ToList();
                            if (lB.Aggregate(true, (current, b) => (b && current)))
                            {
                                if (isSheetNames)
                                {
                                    var needSheetsNumb = (string)rd["sheetnames"];
                                    swModel.ClearSelection();
                                    foreach (var strNum in needSheetsNumb.Split(','))
                                    {
                                        string strNm = strNum.Trim();
                                        string num = strNm.Substring(0, strNm.Length - 1);
                                        string side = strNm.Substring(strNm.Length - 1);
                                        list.Add(num, side.ToLower() == "l");
                                    }
                                    var swDrw = (DrawingDoc)swModel;
                                    var sheetnames = (string[])swDrw.GetSheetNames();

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
        private static bool IsNeededColumnInDbFile(OleDbConnection oleDb, out bool isPropName, out bool isStdSketchNum, out bool isSheetNames)
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
        private static readonly List<string> _namesOfColumnNameFromDimLimits = new List<string>();
        private static int ParseMaterialName(string matName)
        {
            int rezult = 0;
            Regex regex = new Regex(@"\d+");
            Match match = regex.Match(matName);
            if (match.Success)
                int.TryParse(match.Value, out rezult);
            else
                MessageBox.Show(@"Ошибка чтения свойства детали!", @"MrDoors", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return rezult;
        }
        private static Ordinate SetManualDiametrAndDepth(double diam, double depth, bool type, ModelDoc2 swmodel)
        {
            var diamAndDepth = new Ordinate(diam, depth);
            if (type)
            {
                if (diam == 8 && depth == 0)
                {
                    diamAndDepth.X = 8;
                    diamAndDepth.Y = GetDepthFor(8, swmodel);
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
                    diamAndDepth.Y = GetDepthFor(20, swmodel);
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
                if (diam == 57 && depth == 0)
                {
                    depth = 16;
                    diamAndDepth.X = 57;
                    diamAndDepth.Y = 16;
                }

            }
            return diamAndDepth;
        }
        private static double GetDepthFor(int d, ModelDoc2 _swModel)
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
        public static double[] GetDimensionLineDirect(ISldWorks app, ModelDoc2 swModel, double[] boundBox, bool vertic, BlockPosition side, out double d, out Annotation annotation, out bool? blockSize, out Ordinate vertex, out Ordinate vertexS, double[] dsDim, double[] deDim, double[] endDim, string dsName, string deName)
        {
            blockSize = null;
            var swDrawing = (DrawingDoc)swModel;
            var drView = swDrawing.IActiveDrawingView;
            var scale = (double[])drView.ScaleRatio; //todo: тут валится криволинейка
            double vScaleRat = scale[1];
            double[] doubles = null;
            d = 0;
            annotation = null;

            using (var a = new GetDimensions())
            {
                vertex = a.GetVertexFromBlock(swModel, drView, vScaleRat, true);
                if (!(vertex != null && a.SelectVertexDatum(swModel, vertex)))
                    vertex = a.SelectVertexDatumUp(swModel, boundBox, side);

                if (dsDim != null && deDim != null)
                {
                    vertexS = a.GetVertexFromBlock(swModel, drView, vScaleRat, false);
                    
                    swDrawing.ActivateView(drView.Name);
                    bool tmp = swModel.Extension.SelectByID2(@"Точка вставки/" + dsName, "SKETCHPOINT", 0, 0, 0, false, 0, null, 0);
                    tmp = swModel.Extension.SelectByID2(@"Точка вставки/" + deName, "SKETCHPOINT", 0, 0, 0, true, 0, null, 0);
                    DisplayDimension dim1;
                    if (vertic)
                        dim1 = swModel.AddVerticalDimension2(0, 0, 0);
                    else
                        dim1 = swModel.AddHorizontalDimension2(0, 0, 0);

                    var d22 = dim1.GetDimension();
                    d22.DrivenState = (int)swDimensionDrivenState_e.swDimensionDriven;
                    d = d22.Value;
                    if (vertic)
                    {
                        doubles = new double[3] { 1, 0, 0 };
                    }
                    else
                    {
                        doubles = new double[3] { 0, 1, 0 };
                    }
                    annotation = dim1.GetAnnotation();
                    if (((ModelDoc2)app.ActiveDoc).Extension.SelectByID2(dim1.GetNameForSelection(), "DIMENSION", 0, 0, 0, false, 0, null, 0))
                        ((ModelDoc2)app.ActiveDoc).EditDelete();
                }
                else
                {


                    if (vertic)
                    {

                        vertexS = a.GetVertexFromBlock(swModel, drView, vScaleRat, false);
                        if (!(vertexS != null && a.SelectVertexDatum(swModel, vertexS)))
                            a.SelectVertexDatumDown(swModel, boundBox, side);
                        else if (vertex != null)
                            blockSize = vertex.X < vertexS.X;


                        DisplayDimension dim = null;

                        dim = swModel.IAddVerticalDimension2(0, 0, 0);



                        if (dim != null)
                        {
                            var d22 = dim.IGetDimension();
                            d = d22.Value;
                            doubles = (double[])d22.DimensionLineDirection.ArrayData;
                            if (blockSize != null)
                            {
                                vertexS.Y = vertexS.Y + (d / 1000) / vScaleRat;
                            }

                            annotation = dim.IGetAnnotation();
                        }
                    }
                    else
                    {

                        vertexS = a.GetVertexFromBlock(swModel, drView, vScaleRat, false);
                        if (!(vertexS != null && a.SelectVertexDatum(swModel, vertexS)))
                        {
                            vertexS = a.SelectVertexDatumUp(swModel, boundBox, side.Not());
                        }

                        else if (vertex != null)
                            blockSize = vertex.X < vertexS.X;


                        var dim = swModel.AddHorizontalDimension2(0, 0, 0);

                        if (dim != null)
                        {
                            var d22 = dim.GetDimension();
                            d = d22.Value;
                            doubles = (double[])d22.DimensionLineDirection.ArrayData;
                            annotation = dim.GetAnnotation();

                            try
                            {
                                if (((ModelDoc2)app.ActiveDoc).Extension.SelectByID2(dim.GetNameForSelection(), "DIMENSION", 0, 0, 0, false, 0, null, 0))
                                    ((ModelDoc2)app.ActiveDoc).EditDelete();
                            }
                            catch { }

                        }

                    }

                }




            }
            swModel.ClearSelection();
            return doubles;
        }
        public class FuncComparer<T> : IComparer<T>
        {
            private readonly Func<T, T, int> func;
            public FuncComparer(Func<T, T, int> comparerFunc)
            {
                this.func = comparerFunc;
            }

            public int Compare(T x, T y)
            {
                return this.func(x, y);
            }
        }
    }
}
