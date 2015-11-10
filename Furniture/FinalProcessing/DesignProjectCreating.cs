using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Furniture
{
    public static class DesignProject
    {
        public static string GetCustomPropertyValue(ModelDoc2 model, string propertyName)
        {
            CustomPropertyManager propertyManager = model.Extension.get_CustomPropertyManager(string.Empty);
            string value = string.Empty;
            string resolvedValue = string.Empty;
            propertyManager.Get4(propertyName, false, out value, out resolvedValue);
            return resolvedValue;
        }

        public struct OrderComponentData
        {
            public string Article { get; set; }
            public string Name { get; set; }
            public string Width { get; set; }
            public string Depth { get; set; }
            public string Height { get; set; }
            public string Color1 { get; set; }
            public string Color2 { get; set; }
            public string Color3 { get; set; }
            public string Color4 { get; set; }
            public string Color5 { get; set; }
            public string Color6 { get; set; }
            public string Color7 { get; set; }
            public string Count { get; set; }
        }

        public static void Create(ISldWorks app, ModelDoc2 mainModel)
        {
            string drawingTemplate = app.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateDrawing);           
            ModelDoc2 designProject = app.NewDocument(drawingTemplate, (int)swDwgPaperSizes_e.swDwgPaperBsize, 0, 0);
            DrawingDoc designProjectDrawing = (DrawingDoc)designProject;
            int errors = 0;
            app.ActivateDoc2(designProject.GetTitle(), false, ref errors);
            designProjectDrawing.SetupSheet5("", (int)swDwgPaperSizes_e.swDwgPapersUserDefined, (int)swDwgTemplates_e.swDwgTemplateCustom, 1, 100, false, "a4 - iso.slddrt", 0.297, 0.21, "", true); 
            var h = designProjectDrawing.CreateDrawViewFromModelView3(mainModel.GetPathName(), "*Изометрия", 0.1, 0.2, 0);

            List<OrderComponentData> orderData = new List<OrderComponentData>();

            object[] allComponents = ((AssemblyDoc)mainModel).GetComponents(true);
            foreach (Component2 component in allComponents)
            {
                ModelDoc2 currentModel = component.GetModelDoc2();
                string ip = GetCustomPropertyValue(currentModel, "IsProduct");
                if (ip == "Yes")
                {
                    orderData.Add(
                        new OrderComponentData() 
                        {
                            Article = GetCustomPropertyValue(currentModel, "Articul"),
                            Name = GetCustomPropertyValue(currentModel, "Part_Name_spec"),
                            Width = GetCustomPropertyValue(currentModel, "Size1_spec"),
                            Depth = GetCustomPropertyValue(currentModel, "Size2_spec"),
                            Height = GetCustomPropertyValue(currentModel, "Size3_spec"),
                            Color1 = GetCustomPropertyValue(currentModel, "Color1"),
                            Color2 = GetCustomPropertyValue(currentModel, "Color2"),
                            Color3 = GetCustomPropertyValue(currentModel, "Color3"),
                            Color4 = GetCustomPropertyValue(currentModel, "Color4"),
                            Color5 = GetCustomPropertyValue(currentModel, "Color5"),
                            Color6 = GetCustomPropertyValue(currentModel, "Color6"),
                            Color7 = GetCustomPropertyValue(currentModel, "Color7")
                        });
                }
            }

            TableAnnotation table = designProjectDrawing.InsertTableAnnotation(0.01, 0.15, (int)swBOMConfigurationAnchorType_e.swBOMConfigurationAnchor_TopLeft, orderData.Count + 1, 14);
            table.BorderLineWeight = 0;
            table.GridLineWeight = 0;

            #region Заполнение заголовков колонок таблицы
          
            table.set_Text(0, 0, "Поз."); 
            table.set_Text(0, 1, "Артикул");
            table.set_Text(0, 2, "Наименование изделия");
            table.set_Text(0, 3, "Ширина");
            table.set_Text(0, 4, "Глубина");
            table.set_Text(0, 5, "Высота");
            table.set_Text(0, 6, "Цвет 1");
            table.set_Text(0, 7, "Цвет 2");
            table.set_Text(0, 8, "Цвет 3");
            table.set_Text(0, 9, "Цвет 4");
            table.set_Text(0, 10, "Цвет 5");
            table.set_Text(0, 11, "Цвет 6");
            table.set_Text(0, 12, "Цвет 7");
            table.set_Text(0, 13, "Кол-во");

            #endregion

            int i = 1;
            foreach (OrderComponentData productData in orderData)
            {
                table.set_Text(i, 0, i.ToString());
                table.set_Text(i, 1, productData.Article);
                table.set_Text(i, 2, productData.Name);
                table.set_Text(i, 3, productData.Width);
                table.set_Text(i, 4, productData.Depth);
                table.set_Text(i, 5, productData.Height);
                table.set_Text(i, 6, productData.Color1);
                table.set_Text(i, 7, productData.Color2);
                table.set_Text(i, 8, productData.Color3);
                table.set_Text(i, 9, productData.Color4);
                table.set_Text(i, 10, productData.Color5);
                table.set_Text(i, 11, productData.Color6);
                table.set_Text(i, 12, productData.Color7);
                i++; 
            }
            
            table.SetColumnWidth(0, 0.013, (int)swTableRowColSizeChangeBehavior_e.swTableRowColChange_TableSizeCanChange);
            table.SetColumnWidth(1, 0.025, (int)swTableRowColSizeChangeBehavior_e.swTableRowColChange_TableSizeCanChange);
            table.SetColumnWidth(2, 0.07, (int)swTableRowColSizeChangeBehavior_e.swTableRowColChange_TableSizeCanChange);
            
            for (int j = 3; j < 14; j++)
            {
                table.SetColumnWidth(j, 0.02, (int)swTableRowColSizeChangeBehavior_e.swTableRowColChange_TableSizeCanChange);
            }





            designProject.ViewZoomtofit2();
        }
    }
}
