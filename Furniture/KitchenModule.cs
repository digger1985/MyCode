using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using SolidWorks.Interop.sldworks;
using SwDocumentMgr;

namespace Furniture
{
    public class KitchenModule
    {
        const string plateright = "#swrfПравая";
        const string plateleft = "#swrfЛевая";
        public readonly ModelDoc2 RootModel;
        public readonly Component2 swRootComponent;
        public Measure measure;
        public readonly SwAddin swAddin;
        public readonly ModelDoc2 swModel;
        public readonly string rootName;

        public static event EventHandler KitchenModeChanged;
        public static void RaiseKitchenModeChangedEvent()
        {
            var handler = KitchenModeChanged;
            if (handler != null)
                handler(typeof(Cash), EventArgs.Empty);
        }

        public static event EventHandler KitchenModeAvailableChanged;
        public static void RaiseKitchenModeAvailableChangedEvent()
        {
            var handler = KitchenModeAvailableChanged;
            if (handler != null)
                handler(typeof(Cash), EventArgs.Empty);
        }

        public KitchenModule(ModelDoc2 _rootModel, Component2 _swRootComponent, SwAddin _swAddin, ModelDoc2 _swModel)
        {
            RootModel = _rootModel;
            swRootComponent = _swRootComponent;
            measure = RootModel.Extension.CreateMeasure();
            swAddin = _swAddin;
            swModel = _swModel;
            rootName = Path.GetFileNameWithoutExtension(RootModel.GetPathName());
        }
        public InfoForMate GetPointsMate(Component2 swComp2, Component2 mainComponent, bool isAnglePartOrig)
        {
            bool status = false;
            InfoForMate minDistance = new InfoForMate(double.MaxValue, null, null);
            //status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateleft, swComp2.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
            //status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateright, swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
            //measure.Calculate(null);

            List<string> goodPoints = new List<string>();
            for (int i = 1; i < 40; i++)
            {
                status = RootModel.Extension.SelectByID2(string.Format("Точка{0}@{1}@{2}", i.ToString(), mainComponent.Name, rootName), "DATUMPOINT", 0, 0, 0, false, 0, null, 0);
                if (!status)
                {
                    if (RootModel.Extension.SelectByID2(string.Format("Точка{0}@{1}@{2}", (i + 1).ToString(), mainComponent.Name, rootName), "DATUMPOINT", 0, 0, 0, false, 0, null, 0))
                        continue;
                    else
                        break;
                }
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                measure.Calculate(null);
                if (measure.IsIntersect || measure.NormalDistance < 0.00001)
                    goodPoints.Add(string.Format("Точка{0}@{1}@{2}", i.ToString(), mainComponent.Name, rootName));
            }
            minDistance = new InfoForMate(double.MaxValue, null, null);
            foreach (var point in goodPoints)
            {
                if (isAnglePartOrig)
                {
                    status = RootModel.Extension.SelectByID2(point, "DATUMPOINT", 0, 0, 0, false, 0, null, 0);
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя2", swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if (measure.Distance != -1)
                    {
                        if (measure.Distance < minDistance.distance)
                            minDistance = new InfoForMate(measure.Distance, mainComponent.FeatureByName(point.Split('@')[0]), swComp2.FeatureByName("#swrfЗадняя2"));
                    }
                }
                else
                {
                    status = RootModel.Extension.SelectByID2(point, "DATUMPOINT", 0, 0, 0, false, 0, null, 0);
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateleft, swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if (measure.Distance != -1)
                    {
                        if (measure.Distance < minDistance.distance)
                            minDistance = new InfoForMate(measure.Distance, mainComponent.FeatureByName(point.Split('@')[0]), swComp2.FeatureByName(plateleft));
                    }
                    RootModel.ClearSelection();
                    status = RootModel.Extension.SelectByID2(point, "DATUMPOINT", 0, 0, 0, false, 0, null, 0);
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateright, swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if (measure.Distance != -1)
                    {
                        if (measure.Distance < minDistance.distance)
                            minDistance = new InfoForMate(measure.Distance, mainComponent.FeatureByName(point.Split('@')[0]), swComp2.FeatureByName(plateright));
                    }
                }
            }
            return minDistance;
        }
        public InfoForMate GetSimilarTables(Component2 swComp2, bool isUpPart, bool isAnglePartOrig)
        {
            var swComponents = new LinkedList<Component2>();
            InfoForMate minDistance = new InfoForMate(double.MaxValue, null, null);
            string origplateright = plateright;
            string origplateleft = plateleft;
            if (isAnglePartOrig)
            {
                origplateright = "#swrfПраваяЗадняя";
                origplateleft = "#swrfЛеваяЗадняя";
            }
            if (swAddin.GetComponents(swRootComponent, swComponents, false, false))
            {
                double[] origBox = swComp2.GetBox(true, true);
                double origaverx = Math.Min(origBox[3], origBox[0]) + Math.Abs(origBox[3] - origBox[0]) / 2;
                double origaverz = Math.Min(origBox[5], origBox[2]) + Math.Abs(origBox[5] - origBox[2]) / 2;
                foreach (Component2 component in swComponents)
                {
                    bool isAnglePart, isDistUpPart, isTabletop, isKtExist;
                    var swCompModel = (ModelDoc2)component.GetModelDoc();
                    if (swCompModel == null)
                        continue;
                    isKtExist = GetTypeProperty(swCompModel.GetCustomInfoValue("", "KitchenType"), out isAnglePart, out isDistUpPart, out isTabletop);
                    if (!isKtExist)
                        continue;
                    if (isTabletop)
                        continue;
                    if (isDistUpPart != isUpPart)
                        continue;

                    double[] currentBox = component.GetBox(true, true);
                    double averx = Math.Min(currentBox[3], currentBox[0]) + Math.Abs(currentBox[3] - currentBox[0]) / 2;
                    double averz = Math.Min(currentBox[5], currentBox[2]) + Math.Abs(currentBox[5] - currentBox[2]) / 2;
                    double a = Math.Abs(averx - origaverx);
                    double b = Math.Abs(averz - origaverz);
                    double c = Math.Sqrt(a * a + b * b) * 1000;
                    if (c > 4000)
                        continue;
                    string compName = component.Name2;
                    if (compName.Contains("Замер") || compName == swComp2.Name)
                        continue;
                    bool bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", compName, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if (!(measure.IsIntersect && measure.IsParallel))
                    {
                        if (isAnglePart)
                        {
                            bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя2", compName, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                            bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                            measure.Calculate(null);
                            if (!(measure.IsIntersect && measure.IsParallel))
                                continue;

                        }
                        else
                            continue;
                    }
                    swModel.ClearSelection();
                    bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateright, compName, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", origplateleft, swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if (!measure.IsParallel && !isAnglePart)
                        continue;
                    if (minDistance.distance > measure.Distance && measure.IsParallel)
                    {
                        minDistance = new InfoForMate(measure.Distance, component.FeatureByName(plateright), swComp2.FeatureByName(origplateleft));
                        if (isAnglePart)
                        {
                            swModel.ClearSelection();
                            bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateright, compName, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                            bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", origplateright, swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                            measure.Calculate(null);
                            if (minDistance.distance > measure.Distance && measure.IsParallel)
                            {
                                minDistance = new InfoForMate(measure.Distance, component.FeatureByName(plateright), swComp2.FeatureByName(origplateright));
                            }
                        }
                    }
                    bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateleft, compName, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", origplateright, swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if (!measure.IsParallel)
                        continue;
                    if (minDistance.distance > measure.Distance)
                    {
                        minDistance = new InfoForMate(measure.Distance, component.FeatureByName(plateleft), swComp2.FeatureByName(origplateright));
                        if (isAnglePart)
                        {
                            swModel.ClearSelection();
                            bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateleft, compName, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                            bb = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", origplateleft, swComp2.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                            measure.Calculate(null);
                            if (minDistance.distance > measure.Distance && measure.IsParallel)
                            {
                                minDistance = new InfoForMate(measure.Distance, component.FeatureByName(plateleft), swComp2.FeatureByName(origplateleft));
                            }
                        }
                    }
                }
            }
            return minDistance;
        }
        
        public void GetComponentType(string filePath, out bool isAnglePart, out bool isUpPart, out bool isTabletop)
        {
            isAnglePart = false;
            isUpPart = false;
            isTabletop = false;
            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
            SwDmDocumentOpenError oe;
            var swDoc = swDocMgr.GetDocument(filePath,
                                         SwDmDocumentType.
                                             swDmDocumentUnknown
                                         , true, out oe);
            if (swDoc != null)
            {
                var names = (string[])swDoc.GetCustomPropertyNames();
                if (names != null && names.Contains("KitchenType"))
                {
                    SwDmCustomInfoType swDmCstInfoType;
                    string valueOfName = swDoc.GetCustomProperty("KitchenType", out swDmCstInfoType);
                    isAnglePart = valueOfName.ToLower().Contains("угловая");
                    isUpPart = valueOfName.ToLower().Contains("верхняя");
                    isTabletop = valueOfName.ToLower().Contains("столешница");
                }
                swDoc.CloseDoc();
            }
        }
        public bool GetTypeProperty(string propertyValue, out bool isAnglePart, out bool isUpPart, out bool isTabletop)
        {
            isAnglePart = false;
            isUpPart = false;
            isTabletop = false;
            if (string.IsNullOrEmpty(propertyValue))
                return false;
            isAnglePart = propertyValue.ToLower().Contains("угловая");
            isUpPart = propertyValue.ToLower().Contains("верхняя");
            isTabletop = propertyValue.ToLower().Contains("столешница");
            return true;

        }

        public void TableTopProcess(Component2 swAddedComp)
        {
            var mates = swAddedComp.GetMates();
            Component2 firstComponent = null;
            var swAddedCompModel = (ModelDoc2)swAddedComp.GetModelDoc();
            bool? isLeft = null;
            if (swAddedCompModel.GetCustomInfoValue("", "KitchenType").Contains("левая"))
                isLeft = true;
            else if (swAddedCompModel.GetCustomInfoValue("", "KitchenType").Contains("правая"))
                isLeft = false;
            if (isLeft == null)
                return;
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
                                if (me.ReferenceComponent.Name.Contains(swAddedComp.Name))
                                {
                                    string firstComp = spec.MateEntity(0).ReferenceComponent.Name.Split('/')[0];
                                    swAddin.GetComponentByName(RootModel, firstComp, false, out firstComponent);
                                    break;

                                }
                            }
                        }
                    }
                }
            }
            if (firstComponent != null)
            {
                //привязать...
                swAddin.AddMate(RootModel, firstComponent.FeatureByName("#swrfЗадняя"), swAddedComp.FeatureByName("#swrfЗадняя"), true);
            }

            bool status;
            var swComponents = new LinkedList<Component2>();

            if (swAddin.GetComponents(swRootComponent, swComponents, false, false))
            {

                double[] origBox = swAddedComp.GetBox(true, true);
                double origaverx = Math.Min(origBox[3], origBox[0]) +
                                    Math.Abs(origBox[3] - origBox[0]) / 2;
                double origaverz = Math.Min(origBox[5], origBox[2]) +
                                    Math.Abs(origBox[5] - origBox[2]) / 2;

                var swCompModel = (ModelDoc2)firstComponent.GetModelDoc();
                bool isAnglePartFirst = false, isUpPartfirst = false, isTabletopFirst = false;
                if (swCompModel != null)
                    GetTypeProperty(swCompModel.GetCustomInfoValue("", "KitchenType"), out isAnglePartFirst, out isUpPartfirst, out isTabletopFirst);
                if (isTabletopFirst || isUpPartfirst)
                    return;
                string tmp = (bool)isLeft ? "#swrfЛевая" : "#swrfПравая";
                if (isAnglePartFirst)
                    swAddin.AddMate(RootModel, firstComponent.FeatureByName("#swrfЗадняя2"), swAddedComp.FeatureByName(tmp), true);
                else
                    swAddin.AddMate(RootModel, firstComponent.FeatureByName(tmp), swAddedComp.FeatureByName(tmp), true);

                InfoForMate maxDistance = FindMinTopTable(swComponents, swAddedComp, isLeft);
                InfoForMate maxDistance3 = FindMaxPlate(swComponents, swAddedComp, firstComponent, origaverx, origaverz, isAnglePartFirst, isLeft);

                if (maxDistance.planeDist != null && maxDistance.planeSource != null && maxDistance3.distance > maxDistance.distance)//&& maxDistance3.planeSource.Name == maxDistance.planeSource.Name)
                {
                    swModel.ClearSelection();
                    InfoForMate maxDistance2 = new InfoForMate(double.MinValue, null, null);
                    string tt = isAnglePartFirst ? "#swrfЗадняя2" : plateleft;
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", tt, firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    maxDistance.planeDist.Select(true);
                    measure.Calculate(null);
                    if (measure.IsParallel && maxDistance2.distance < measure.Distance)
                        maxDistance2 = new InfoForMate(measure.Distance, firstComponent.FeatureByName(tt), null);
                    swModel.ClearSelection();
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateright, firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    maxDistance.planeDist.Select(true);
                    measure.Calculate(null);
                    if (measure.IsParallel && maxDistance2.distance < measure.Distance)
                        maxDistance2 = new InfoForMate(measure.Distance, firstComponent.FeatureByName(plateright), null);
                    //if (maxDistance2.planeDist!=null)
                    //    swAddin.AddMate(swModel, maxDistance2.planeDist, maxDistance.planeSource, true);//distToTopTable = new InfoForMate(maxDistance.distance, maxDistance2.planeDist, maxDistance.planeSource);//
                    swModel.ClearSelection();
                    maxDistance.planeDist.Select(false);
                    maxDistance2.planeDist.Select(true);
                    measure.Calculate(null);
                    maxDistance.distance = measure.Distance;
                }
                else
                {

                    maxDistance = maxDistance3;//maxDistance = FindMaxPlate(swComponents, swAddedComp, firstComponent, origaverx, origaverz,isAnglePartFirst);
                    //if (maxDistance.planeDist != null && maxDistance.planeSource != null)
                    //{
                    //    if (maxDistance.planeSource.Name != "#swrfЗадняя2")
                    //        swAddin.AddMate(swModel, maxDistance.planeSource, swAddedComp.FeatureByName(maxDistance.planeSource.Name), true);
                    //    else
                    //    {
                    //        maxDistance.planeSource.Select(false);
                    //        swAddedComp.FeatureByName(plateleft).Select(true);
                    //        measure.Calculate(null);
                    //        double distanceleft = measure.Distance;
                    //        maxDistance.planeSource.Select(false);
                    //        swAddedComp.FeatureByName(plateright).Select(true);
                    //        measure.Calculate(null);
                    //        double distanceright = measure.Distance; 
                    //        if (distanceleft<distanceright)
                    //            swAddin.AddMate(swModel, maxDistance.planeSource, swAddedComp.FeatureByName(plateleft), true);
                    //        else
                    //            swAddin.AddMate(swModel, maxDistance.planeSource, swAddedComp.FeatureByName(plateright), true);
                    //    }

                    //}
                }
                double distance2;
                swModel.ClearSelection();
                if (!isAnglePartFirst)
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "Передняя", firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                else
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateleft, firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", firstComponent.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                measure.Calculate(null);
                distance2 = measure.Distance * 1000;
                //поменять размер..
                var curModel = swAddedComp.GetModelDoc2();
                bool isNumber = false;
                OleDbConnection oleDb;
                OleDbDataReader rd;
                
                List<string> strObjNames = new List<string>();
                string filePath = swAddedComp.GetPathName();
                if (swAddin.OpenModelDatabase(curModel, out oleDb))
                {
                    using (oleDb)
                    {
                        OleDbCommand cm;
                        cm = isNumber
                                    ? new OleDbCommand(
                                        "SELECT * FROM objects WHERE number>0 ORDER BY number",
                                        oleDb)
                                    : new OleDbCommand("SELECT * FROM objects ORDER BY id", oleDb);
                        rd = cm.ExecuteReader();
                        while (rd.Read())
                        {
                            if (rd["caption"].ToString() == null || rd["caption"].ToString() == "" ||
                            rd["caption"].ToString().Trim() == "")
                                continue;
                            string strObjName = rd["name"].ToString();

                            if (filePath.Contains("_SWLIB_BACKUP"))
                            {
                                string pn = Path.GetFileNameWithoutExtension(filePath);
                                string last3 = pn.Substring(pn.Length - 4, 4);
                                string[] arr = strObjName.Split('@');
                                if (arr.Length != 3)
                                    throw new Exception("что-то не так");
                                arr[2] = Path.GetFileNameWithoutExtension(arr[2]) + last3 + Path.GetExtension(arr[2]);
                                strObjName = string.Format("{0}@{1}@{2}", arr[0], arr[1], arr[2]);
                               
                            }                             
                            strObjNames.Add(strObjName);
                        }
                    }
                }
                swAddin.SetObjectValue(curModel, strObjNames[0], 14, maxDistance.distance * 1000);
                swAddin.SetObjectValue(curModel, strObjNames[1], 14, distance2);
            }
        }
        private InfoForMate FindMinTopTable(LinkedList<Component2> swComponents, Component2 swAddedComp, bool? isLeft)
        {
            bool status;
            InfoForMate minDistance = new InfoForMate(double.MaxValue, null, null);
            InfoForMate currentDistance1 = null, currentDistance2 = null;
            foreach (Component2 component in swComponents)
            {
                bool isAnglePart, isUpPart, isTabletop, isKtExist;
                var swCompModel = (ModelDoc2)component.GetModelDoc();
                if (swCompModel == null)
                    continue;
                isKtExist = GetTypeProperty(swCompModel.GetCustomInfoValue("", "KitchenType"), out isAnglePart, out isUpPart, out isTabletop);
                if (!isKtExist)
                    continue;
                if (isUpPart || !isTabletop)
                    continue;
                if (component.Name == swAddedComp.Name)
                    continue;
                swModel.ClearSelection();
                //пересекает ли эта столешница столешницу к которой мы хотим прикрепится.
                bool isIntersect = false;
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", swAddedComp.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateleft, component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                measure.Calculate(null);
                isIntersect = (measure.NormalDistance <= 0.002 && measure.NormalDistance != -1) || measure.IsIntersect;
                if (!isIntersect)
                {
                    swModel.ClearSelection();
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", swAddedComp.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateright, component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    isIntersect = (measure.NormalDistance <= 0.002 && measure.NormalDistance != -1) || measure.IsIntersect;
                }
                if (!isIntersect)
                    continue;
                swModel.ClearSelection();

                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateleft, swAddedComp.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfПередняя", component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                measure.Calculate(null);
                if (minDistance.distance > measure.Distance && measure.Distance != -1 && measure.IsParallel)
                    currentDistance1 = new InfoForMate(measure.Distance, component.FeatureByName("#swrfПередняя"), swAddedComp.FeatureByName(plateright));
                swModel.ClearSelection();
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateright, swAddedComp.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfПередняя", component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                measure.Calculate(null);
                if (minDistance.distance > measure.Distance && measure.Distance != -1 && measure.IsParallel)
                    currentDistance2 = new InfoForMate(measure.Distance, component.FeatureByName("#swrfПередняя"), swAddedComp.FeatureByName(plateleft));

                if (currentDistance1 != null && currentDistance2 != null)
                {
                    if ((bool)isLeft && currentDistance1.distance > currentDistance2.distance)
                        minDistance = currentDistance1;
                    else if (!(bool)isLeft && currentDistance1.distance < currentDistance2.distance)
                        minDistance = currentDistance2;
                }
                else
                {
                    if (currentDistance1 != null)
                        minDistance = currentDistance1;
                    if (currentDistance2 != null)
                        minDistance = currentDistance2;
                }

            }
            return minDistance;
        }
        private InfoForMate FindMaxPlate(LinkedList<Component2> swComponents, Component2 swAddedComp, Component2 firstComponent, double origaverx, double origaverz, bool isAnglePartFirst, bool? isLeft)
        {
            InfoForMate maxDistance = new InfoForMate(double.MinValue, null, null);
            bool status;
            string firstPlateLeft = plateleft;
            string firstPlateRight = plateright;
            if (isAnglePartFirst)
            {
                firstPlateLeft = "#swrfЗадняя2";
                firstPlateRight = "#swrfЗадняя2";
            }
            InfoForMate currentDistance1 = null, currentDistance2 = null;
            bool isAnglePart, isUpPart, isTabletop, isKtExist;
            foreach (Component2 component in swComponents)
            {
                currentDistance1 = null;
                currentDistance2 = null;
                var swCompModel = (ModelDoc2)component.GetModelDoc();
                if (swCompModel == null)
                    continue;
                isKtExist = GetTypeProperty(swCompModel.GetCustomInfoValue("", "KitchenType"), out isAnglePart, out isUpPart, out isTabletop);
                if (!isKtExist)
                    continue;
                if (isUpPart || isTabletop)
                    continue;
                if (component.Name == swAddedComp.Name)
                    continue;
                //if (component.Name == firstComponent.Name)
                //    continue;
                swModel.ClearSelection();
                //сначала проверка на удаленность
                /*double[] currentBox = component.GetBox(true, true);
                double averx = Math.Min(currentBox[3], currentBox[0]) +
                                Math.Abs(currentBox[3] - currentBox[0]) / 2;
                double averz = Math.Min(currentBox[5], currentBox[2]) +
                                Math.Abs(currentBox[5] - currentBox[2]) / 2;
                double a = Math.Abs(averx - origaverx);
                double b = Math.Abs(averz - origaverz);
                double c = Math.Sqrt(a * a + b * b) * 1000;
                if (c > 4000)
                    continue;*/
                swModel.ClearSelection();
                //тут проверить что она у той же стенки

                string swrfBackAngle = "#swrfЗадняя2";
                if (component.Name != firstComponent.Name)
                {
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if ((!measure.IsIntersect || !measure.IsParallel) && !isAnglePart)
                        continue;
                    if (isAnglePart && (!measure.IsParallel || !measure.IsIntersect))
                    {
                        swModel.ClearSelection();
                        status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", "#swrfЗадняя", firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                        status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", swrfBackAngle, component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                        measure.Calculate(null);
                        if (!measure.IsIntersect || !measure.IsParallel)
                            continue;
                        else
                            swrfBackAngle = "#swrfЗадняя";
                    }
                }
                swModel.ClearSelection();
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", firstPlateLeft, firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateright, component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                measure.Calculate(null);
                if (!measure.IsParallel && !isAnglePart)
                    continue;
                if (isAnglePart) //&& !measure.IsParallel)
                {
                    swModel.ClearSelection();
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", firstPlateLeft, firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", swrfBackAngle, component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if (measure.Distance != -1 && measure.IsParallel)
                        currentDistance1 = new InfoForMate(measure.Distance, component.FeatureByName(swrfBackAngle), firstComponent.FeatureByName(firstPlateLeft));
                }
                else
                {
                    //if (measure.Distance != -1)
                    currentDistance1 = new InfoForMate(measure.Distance, component.FeatureByName(plateright),
                                                  firstComponent.FeatureByName(firstPlateLeft));
                }
                swModel.ClearSelection();
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", firstPlateRight, firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", plateleft, component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                measure.Calculate(null);
                if (!measure.IsParallel && !isAnglePart)
                    continue;
                if (isAnglePart) //&& !measure.IsParallel)
                {
                    swModel.ClearSelection();
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", firstPlateRight, firstComponent.Name, rootName), "PLANE", 0, 0, 0, false, 0, null, 0);
                    status = RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", swrfBackAngle, component.Name, rootName), "PLANE", 0, 0, 0, true, 0, null, 0);
                    measure.Calculate(null);
                    if (measure.Distance != -1 && measure.IsParallel)
                        currentDistance2 = new InfoForMate(measure.Distance, component.FeatureByName(swrfBackAngle),
                                                      firstComponent.FeatureByName(firstPlateRight));
                }
                else
                {
                    //if (measure.Distance != -1)
                    currentDistance2 = new InfoForMate(measure.Distance, component.FeatureByName(plateleft),
                                                       firstComponent.FeatureByName(firstPlateRight));
                }
                if (component.Name == firstComponent.Name)
                {
                    if ((bool)isLeft)
                    {
                        if (currentDistance1 != null && maxDistance.distance < currentDistance1.distance)
                            maxDistance = currentDistance1;
                    }
                    else
                    {
                        if (currentDistance1 != null && maxDistance.distance < currentDistance2.distance)
                            maxDistance = currentDistance2;
                    }

                }
                else
                {
                    if (currentDistance1 != null && currentDistance2 != null)
                    {
                        if ((bool)isLeft && currentDistance1.distance >= currentDistance2.distance && maxDistance.distance < currentDistance1.distance)
                            maxDistance = currentDistance1;
                        else if (!(bool)isLeft && currentDistance1.distance <= currentDistance2.distance && maxDistance.distance < currentDistance2.distance)
                            maxDistance = currentDistance2;
                    }
                    else
                    {
                        if (currentDistance1 != null && maxDistance.distance < currentDistance1.distance)
                            maxDistance = currentDistance1;
                        if (currentDistance2 != null && maxDistance.distance < currentDistance2.distance)
                            maxDistance = currentDistance2;
                    }
                }
            }
            return maxDistance;
        }
    }


    public class InfoForMate
    {
        public Feature planeDist;
        public Feature planeSource;
        public double distance;
        public InfoForMate(double _distance, Feature _planeDist, Feature _planeSource)
        {
            distance = _distance;
            planeDist = _planeDist;
            planeSource = _planeSource;
        }
        //public string planeNameDist;
        //public string planeNameSource;
    }
    internal class LineProp
    {
        public string lineName;
        public SketchPoint startPoint;
        public SketchPoint endPoint;
        public double length
        {
            get
            {
                return Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));
            }
        }
        public LineProp(string _lineName, SketchPoint _startPoint, SketchPoint _endPoint)
        {
            lineName = _lineName;
            startPoint = _startPoint;
            endPoint = _endPoint;
        }
    }
    internal class CoordNeightboors
    {
        public SketchPoint point;
        public LineProp neiOne;
        public LineProp neiTwo;       
        public CoordNeightboors(SketchPoint _point, LineProp _neiOne, LineProp _neiTwo)
        {
            point = _point;
            neiOne = _neiOne;
            neiTwo = _neiTwo;
        }
    }
    internal class TwoPoints
    {
        public IFeature point1;
        public IFeature point2;       
        public TwoPoints(IFeature _point1, IFeature _point2)
        {
            point1 = _point1;
            point2 = _point2;
        }
    }
}
