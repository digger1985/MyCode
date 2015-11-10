using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Furniture
{
    public static partial class Kitchen
    {
        #region Публичные методы
        /// <summary>
        /// Создает новую деталь и добавляет в неё дефолтный эскиз пола помещения ( не чищено )
        /// </summary>      
        public static int CreateFloorSketch(SwAddin swAddin)
        {            
            var swModel = (ModelDoc2)swAddin._iSwApp.NewDocument(swAddin._iSwApp.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplatePart), 0, 0, 0);
            int err = 0;
            swAddin._iSwApp.ActivateDoc2("Деталь3", false, ref err);

            swModel.Extension.SelectByID2("Сверху", "PLANE", 0, 0, 0, false, 0, null, 0);
            IFeatureManager featMan = swModel.FeatureManager;
            featMan.InsertRefPlane(4, 0, 4, 0, 0, 0);
            swModel.Extension.SelectByID2("Плоскость1", "PLANE", 0, 0, 0, false, 0, null, 0);
            swModel.SelectedFeatureProperties(0, 0, 0, 0, 0, 0, 0, true, false, "#swrfНижняя");

            swModel.SketchManager.InsertSketch(true);
            swModel.ClearSelection2(true);
            SketchSegment skSegment;
            skSegment = swModel.SketchManager.CreateLine(0, 0, 0, 0, -3, 0);
            skSegment = swModel.SketchManager.CreateLine(0, -3, 0, -5, -3, 0);
            swModel.Extension.SelectByID2("Line1", "SKETCHSEGMENT", 0, 0, 0, false, 0, null, 0);
            
            swModel.Extension.SelectByID2("Line2", "SKETCHSEGMENT", 0, 0, 0, false, 0, null, 0);
           
            swModel.SetPickMode();
            swModel.ClearSelection2(true);
            swModel.SketchManager.InsertSketch(true);
            swModel.ClearSelection2(true);
            swModel.ShowNamedView2("*Сверху", 5);
            swModel.ViewZoomtofit2();
            swModel.Extension.SelectByID2("Эскиз1", "SKETCH", 0, 0, 0, false, 0, null, 0);
            swModel.EditSketch();
            swModel.SetPickMode();

            return 0;
        }
        /// <summary>
        /// Вставляет в модель [проекта] деталь помещения и устанавливает дополнительные настройки для работы с ним
        /// </summary>
        /// <param name="roomModelPath">Путь в файлу детали помещения</param>
        /// <param name="orderModel">Модель [проекта], в которую вставляется деталь</param>
        /// <param name="swAddin"></param>
        public static bool InsertRoom(string roomModelPath, ModelDoc2 orderModel, SwAddin swAddin)
        {
            IAssemblyDoc orderAssembly = (IAssemblyDoc)orderModel;            
            string[] CompNames = new string[] { roomModelPath };
            double[] Transforms = new double[16];
            Transforms[0] = 1;
            Transforms[1] = 0;
            Transforms[2] = 0;
            Transforms[3] = 0;
            Transforms[4] = 1;
            Transforms[5] = 0;
            Transforms[6] = 0;
            Transforms[7] = 0;
            Transforms[8] = 1;
            Transforms[9] = 0;
            Transforms[10] = 0;
            Transforms[11] = 0;
            Transforms[12] = 1;
            Transforms[13] = 1;
            Transforms[14] = 1;
            Transforms[15] = 1;
            object[] addedComponents = orderAssembly.AddComponents((CompNames), (Transforms));
                        
            //фиксирование добавленного замера            
            orderModel.Save();
            if (addedComponents != null && addedComponents[0] != null)
            {
                ModelDoc2 swModel = swAddin._iSwApp.ActiveDoc;
                swModel.ClearSelection2(true);
                string compName = ((Component2)addedComponents[0]).Name;
                string rootName = Path.GetFileName(Path.GetDirectoryName(swModel.GetPathName()));
                bool status = swModel.Extension.SelectByID2(string.Format("{0}@{1}", compName, rootName), "COMPONENT", 0, 0, 0, false, 0, null, 0);
                if (status)
                    (swModel as IAssemblyDoc).FixComponent();

                swModel.ClearSelection2(true);
            }
            else
                return false;
            
            //переключение в изометрию
            ((ModelDoc2)swAddin.SwApp.ActiveDoc).ShowNamedView2("", (int)swStandardViews_e.swIsometricView);
            ((ModelDoc2)swAddin.SwApp.ActiveDoc).ViewZoomtofit2();

            //задание сцены - окклюзия           
            string scenePath = @"\scenes\02 studio scenes\99 ambient occlusion.p2s";
            ((ModelDoc2)swAddin.SwApp.ActiveDoc).Extension.InsertScene(scenePath);

            orderModel.Save();

            return true;
        }
        /// <summary>
        /// Создает помещение на основе готового и открытого для редактирования (активного) эскиза пола и сохраняет его 
        /// </summary>
        /// <param name="swAddin"></param>
        /// <param name="pathToSave">Путь к папке [заказа], в которую будет сохранена деталь помещения</param>
        /// <returns>Путь непосредственно к детали помещения</returns>
        public static string CreateAndSaveRoom(SwAddin swAddin, string pathToSave)
        {
            ModelDoc2 activeModel = swAddin._iSwApp.ActiveDoc;
            SketchManager sketchManager = activeModel.SketchManager;
            Sketch sketch = sketchManager.ActiveSketch;
            Feature feature = (Feature)sketch;
            string sketchName = feature.Name;

            #region Проверка правильности построения эскиза

            //нарисован ли замкнутый контур?
            bool isCircuit;
            SegmentsList sortedSegments;

            if (!CheckSketch(sketch, out isCircuit, out sortedSegments))
            {
                MessageBox.Show(sketchErrorMessage, "Ошибка построения эскиза!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }

            #endregion

            #region Создание стен

            //тонкостенная бобышка-стены на основе первичного эскиза
            int reverseThin = 1; //если контур не замкнутый, то по умолчанию стены генерятся внутри эскиза (реверсируем)
            if (isCircuit)
                reverseThin = 0; //если контур замкнутый, то по умолчанию стены генерятся снаружи эскиза (не реверсируем)
            activeModel.FeatureManager.FeatureExtrusionThin2(true, false, false, 0, 0, 2.8, 0.01, false, false, false, false, 0.01745329251994, 0.01745329251994, false, false, false, false, false, 0.02, 0.0, 0.0, reverseThin, 0, false, 0.005, true, true, 0, 0.0, true);

            #endregion

            #region Создание пола

            //копипастим эскиз
            activeModel.ClearSelection2(true);
            activeModel.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            activeModel.EditCopy();
            activeModel.Extension.SelectByID2("#swrfНижняя", "PLANE", 0, 0, 0, false, 0, null, 0);           
            activeModel.Paste();
            activeModel.ClearSelection2(true);

            //todo: получить имя вставленного эскиза
            string floorSketchName = "Эскиз2";
            activeModel.Extension.SelectByID2(floorSketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            activeModel.EditSketch();

            //если контур не замкнут, то вставляем ещё одну линию (замыкающую контур)
            if (!isCircuit)
            {
                SketchPoint startPoint = sortedSegments.EndPoint;
                SketchPoint endPoint = sortedSegments.StartPoint;
                sortedSegments.AddLast(activeModel.SketchManager.CreateLine(startPoint.X, startPoint.Y, startPoint.Z, endPoint.X, endPoint.Y, endPoint.Z));
                sortedSegments.EndPoint = endPoint;
            }

            //бобышка - пол 
            activeModel.FeatureManager.FeatureExtrusion(true, false, true, 0, 0, 0.001, 0.01, false, false, false, false, 0.01745329251994, 0.01745329251994, false, false, false, false, false, false, false);

            #endregion

            #region Установка точек

            LinkedListNode<SketchSegment> currentSketchSegment = sortedSegments.First;
            bool finish = false;
            do
            {
                LinkedListNode<SketchSegment> segment;
                if (currentSketchSegment == sortedSegments.First)
                    segment = sortedSegments.Last;
                else
                    segment = currentSketchSegment.Previous;
                
                activeModel.ClearSelection2(true);

                try
                {
                    if (currentSketchSegment == sortedSegments.Last && !isCircuit)
                        SelectSketchSegment(activeModel, floorSketchName, currentSketchSegment.Value, false);
                    else
                        SelectSketchSegment(activeModel, sketchName, currentSketchSegment.Value, false);

                    if (segment == sortedSegments.Last && !isCircuit)
                        SelectSketchSegment(activeModel, floorSketchName, segment.Value, true);
                    else
                        SelectSketchSegment(activeModel, sketchName, segment.Value, true);
                }
                catch { return ""; }

                activeModel.FeatureManager.InsertReferencePoint((int)swRefPointType_e.swRefPointIntersection, (int)swRefPointAlongCurveType_e.swRefPointAlongCurveDistance, 0.0, 1);

                if (currentSketchSegment != sortedSegments.Last)
                    currentSketchSegment = currentSketchSegment.Next;
                else
                    finish = true;
            }
            while (!finish);

            #endregion

            #region Добавление свойства Part_Name_spec

            //добавление свойства Part_Name_spec в деталь Замера
            CustomPropertyManager swCustPropMgr = activeModel.Extension.get_CustomPropertyManager("");
            swCustPropMgr.Add2("Part_Name_spec", (int)swCustomInfoType_e.swCustomInfoText, "Замер");

            #endregion

            #region Сохранение

            //номер созданного замера
            int index = 1;

            while (File.Exists(Path.Combine(pathToSave, string.Format("Замер{0}.SLDPRT", index))))
                index++;

            string path = Path.Combine(pathToSave, string.Format("Замер{0}.SLDPRT", index));
            activeModel.SaveAs3(path, 0, 2);
            swAddin.SwApp.CloseDoc(activeModel.GetTitle());

            #endregion

            return path;         
        }
        #endregion
        #region Приватные члены
        private static SketchPoint GetStartPoint(SketchSegment sketchSegment)
        {
            switch ((swSketchSegments_e)sketchSegment.GetType())
            {
                case swSketchSegments_e.swSketchLINE:
                    return ((SketchLine)sketchSegment).GetStartPoint2();
                case swSketchSegments_e.swSketchARC:
                    return ((SketchArc)sketchSegment).GetStartPoint2();
                case swSketchSegments_e.swSketchELLIPSE:
                    return ((SketchEllipse)sketchSegment).GetStartPoint2();
                case swSketchSegments_e.swSketchPARABOLA:
                    return ((SketchParabola)sketchSegment).GetStartPoint2();
                case swSketchSegments_e.swSketchSPLINE:
                      //(SketchPoint)((SketchSpline)sketchSegment).GetPoints2()
                    return null; //todo: че возвращать то?
                default:
                    throw new Exception("Тип не поддерживается"); //конкретизировать эксепшн
            }
        }
        private static SketchPoint GetEndPoint(SketchSegment sketchSegment)
        {
            switch ((swSketchSegments_e)sketchSegment.GetType())
            {
                case swSketchSegments_e.swSketchLINE:
                    return ((SketchLine)sketchSegment).GetEndPoint2();
                case swSketchSegments_e.swSketchARC:
                    return ((SketchArc)sketchSegment).GetEndPoint2();
                case swSketchSegments_e.swSketchELLIPSE:
                    return ((SketchEllipse)sketchSegment).GetEndPoint2();
                case swSketchSegments_e.swSketchPARABOLA:
                    return ((SketchParabola)sketchSegment).GetEndPoint2();
                case swSketchSegments_e.swSketchSPLINE:
                    return null; //todo: че возвращать то?
                default:
                    throw new Exception("Тип не поддерживается"); //конкретизировать эксепшн
            }
        }
        /// <summary>
        /// Проверяет корректность построенного эскиза пола
        /// </summary>
        /// <param name="sketch">Эскиз</param>
        /// <param name="isCircuit">Возвращает true, если эскиз замкнут, false иначе</param>
        /// <param name="sort">Возвращает сортированный список сегментов эскиза в порядке как нарисовано</param>
        /// <returns></returns>
        private static bool CheckSketch(Sketch sketch, out bool isCircuit, out SegmentsList sort)
        {
            //берем все линии текущего эскиза
            object[] sketchSegments = sketch.GetSketchSegments();
            //список всех сегментов эскиза
            List<object> unsort = sketchSegments.ToList();
            //список элементов (линиями, дугами и проч) эскиза в последовательности друг за другом (как нарисовано)
            sort = new SegmentsList();

            isCircuit = false;

            //проверка наличия пересечений (сквозных - в месте пересечения нет точки + когда на отрезке лежит начало/конец другого отрезка)            
            foreach (SketchSegment currentSegment in unsort)
            {
                Curve currentCurve = currentSegment.GetCurve();

                SketchPoint currStartPoint;
                SketchPoint currEndPoint;
                try
                {
                    currStartPoint = GetStartPoint(currentSegment);
                    currEndPoint = GetEndPoint(currentSegment);
                }
                catch { return false; }

                double[] currSegStart = { currStartPoint.X, currStartPoint.Y, currStartPoint.Z };
                double[] currSegEnd = { currEndPoint.X, currEndPoint.Y, currEndPoint.Z };
                foreach (SketchSegment segment in unsort)
                {
                    if (currentSegment != segment)
                    {
                        SketchPoint startPoint;
                        SketchPoint endPoint;
                        try
                        {
                            startPoint = GetStartPoint(segment);
                            endPoint = GetEndPoint(segment);
                        }
                        catch { return false; }

                        double[] segStart = { startPoint.X, startPoint.Y, startPoint.Z };
                        double[] segEnd = { endPoint.X, endPoint.Y, endPoint.Z };
                        Curve curve = segment.GetCurve();

                        dynamic intersectPoints = currentCurve.IntersectCurve(curve, currSegStart, currSegEnd, segStart, segEnd);
                        if (intersectPoints.GetType().Name == "DBNull")
                            continue;
                        int intersectCountPointsCount = ((double[])intersectPoints).Count() / 4;
                        for (int i = 0; i < intersectCountPointsCount; i++)
                        {
                            int index = i * 4;
                            if (index != 0) index--;

                            //эскиз должен быть нарисован на плоскости "Нижняя" так что Z=0                            
                            if ((Math.Round(currStartPoint.X, 10) == Math.Round(intersectPoints[index], 10) && Math.Round(currStartPoint.Y, 10) == Math.Round(intersectPoints[index + 1], 10)) ==
                                (Math.Round(currEndPoint.X, 10) == Math.Round(intersectPoints[index], 10) && Math.Round(currEndPoint.Y, 10) == Math.Round(intersectPoints[index + 1], 10)))
                                return false;
                        }
                    }
                }
            }

            //проверка действительно ли построена последовательность из сегментов

            //если этот флаг в тру, то это означает что дальше строить последовательность сегментов эскиза не надо
            //это происходит когда:
            //1. в sort построена последовательность и больше в неё добавить ничего нельзя
            //в таком случае либо список unsort пуст, и тогда все сегменты соединены (это хорошо)
            //либо в unsort что-то есть -> эскиз построен неверно (с разрывами)
            //2. найдена точка, из которой выходит более чем 2 сегмента (эскиз построен неверно)
            bool booool = false;
            do
            {
                //список сегментов, успешно помещенных в сортированную последовательность сегментов. (из unsort надо удалить)
                List<SketchSegment> removeList = new List<SketchSegment>();

                foreach (SketchSegment sketchSegment in unsort)
                {
                    if (sort.Count == 0)
                    {
                        sort.AddFirst(sketchSegment);

                        try
                        {
                            sort.StartPoint = GetStartPoint(sketchSegment);
                            sort.EndPoint = GetEndPoint(sketchSegment);
                        }
                        catch { return false; }

                        var type = sketchSegment.GetType();
                        removeList.Add(sketchSegment);
                    }
                    else
                    {
                        //эскиз должен быть нарисован на плоскости "Нижняя" так что Z=0

                        //считывание координат текущего сегмента из списка неотсортированных
                        SketchPoint segStartPoint = GetStartPoint(sketchSegment);
                        double x_segStartPoint = segStartPoint.X;
                        double y_segStartPoint = segStartPoint.Y;
                        SketchPoint segEndPoint = GetEndPoint(sketchSegment);
                        double x_segEndPoint = segEndPoint.X;
                        double y_segEndPoint = segEndPoint.Y;

                        //считывание первой и последней координат списка последовательно построенных сегментов (sort)                       
                        double x_startPoint = sort.StartPoint.X;
                        double y_startPoint = sort.StartPoint.Y;
                        double x_endPoint = sort.EndPoint.X;
                        double y_endPoint = sort.EndPoint.Y;

                        int i = 0;
                        bool startStart = x_segStartPoint == x_startPoint && y_segStartPoint == y_startPoint;
                        if (startStart) i++;
                        bool startEnd = x_segStartPoint == x_endPoint && y_segStartPoint == y_endPoint;
                        if (startEnd) i++;
                        bool endStart = x_segEndPoint == x_startPoint && y_segEndPoint == y_startPoint;
                        if (endStart) i++;
                        bool endEnd = x_segEndPoint == x_endPoint && y_segEndPoint == y_endPoint;
                        if (endEnd) i++;

                        if (i == 1) //прямая "прилеплена" с одной стороны - хорошо
                        {
                            if (startStart)
                            {
                                sort.AddFirst(sketchSegment);
                                sort.StartPoint = segEndPoint;
                            }
                            if (startEnd)
                            {
                                sort.AddLast(sketchSegment);
                                sort.EndPoint = segEndPoint;
                            }
                            if (endStart)
                            {
                                sort.AddFirst(sketchSegment);
                                sort.StartPoint = segStartPoint;
                            }
                            if (endEnd)
                            {
                                sort.AddLast(sketchSegment);
                                sort.EndPoint = segStartPoint;
                            }
                            removeList.Add(sketchSegment);
                        }
                        if (i == 2) //два совпадения (либо плохо, либо эскиз - замкнутый контур)
                        {
                            if ((startStart && endEnd) || (startEnd && endStart)) //действительно контур
                            {
                                sort.AddFirst(sketchSegment);
                                if (startStart && endEnd)
                                    sort.StartPoint = segEndPoint;
                                if (startEnd && endStart)
                                    sort.StartPoint = segStartPoint;

                                removeList.Add(sketchSegment);
                                break; //останавливаем перебор сегментом в unsort (контур теперь замкнут, теоретически там больше нет ничего) если есть - значит эскиз построен некорректно
                            }
                            else
                                return false;
                        }
                        if (i > 2)
                            return false;
                    }
                }

                if (removeList.Count == 0) //значит ничего не смогли засунуть в последовательность
                    return false;

                foreach (SketchSegment segToRemove in removeList)
                    unsort.Remove(segToRemove);

                if (unsort.Count == 0) //всё разместили в последовательность и всё ништяк
                    booool = true;

                if (sort.StartPoint == sort.EndPoint)
                {
                    if (unsort.Count == 0)
                    {
                        if (MessageBox.Show("Эскиз для построания помещения представляет собой замкнутый контур.\nПомещение будет включать в себя все стены.\nРекомендуется удалить одну из линий стены для удобства просмотра модели помещения. \n\nПродолжить построение?", "Построение помещения", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            return false;
                        else
                            isCircuit = true;
                    }
                    else
                        return false;
                }
            }
            while (!booool);
            return true;
        }
        private static bool SelectSketchSegment(ModelDoc2 model, string sketchName, SketchSegment segment, bool append)
        {
            int[] id = segment.GetID();
            switch ((swSketchSegments_e)segment.GetType())
            {
                case swSketchSegments_e.swSketchLINE:
                    return model.Extension.SelectByID2("Line" + id[1].ToString() + "@" + sketchName, "EXTSKETCHSEGMENT", 0, 0, 0, append, 0, null, 0);
                case swSketchSegments_e.swSketchARC:
                    return model.Extension.SelectByID2("Arc" + id[1].ToString() + "@" + sketchName, "EXTSKETCHSEGMENT", 0, 0, 0, append, 0, null, 0);
                case swSketchSegments_e.swSketchELLIPSE:
                    return model.Extension.SelectByID2("Ellipse" + id[1].ToString() + "@" + sketchName, "EXTSKETCHSEGMENT", 0, 0, 0, append, 0, null, 0);
                case swSketchSegments_e.swSketchPARABOLA:
                    return model.Extension.SelectByID2("Parabola" + id[1].ToString() + "@" + sketchName, "EXTSKETCHSEGMENT", 0, 0, 0, append, 0, null, 0);
                case swSketchSegments_e.swSketchSPLINE:
                    return model.Extension.SelectByID2("Spline" + id[1].ToString() + "@" + sketchName, "EXTSKETCHSEGMENT", 0, 0, 0, append, 0, null, 0);
                default:
                    throw new Exception("Тип не поддерживается"); //конкретизировать эксепшн
            }
        }        
        private static string sketchErrorMessage = "Некорректно построен эскиз. Эскиз должен представлять собой последовательность сегментов (замкнутую или незамкнутую) без пересечений и разрывов.";
        private class SegmentsList : LinkedList<SketchSegment>
        {
            public SketchPoint StartPoint { get; set; }
            public SketchPoint EndPoint { get; set; }
        }
        #endregion
    }
}
