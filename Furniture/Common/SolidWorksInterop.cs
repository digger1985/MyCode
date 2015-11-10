using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolidWorks.Interop.sldworks;

namespace Furniture
{
    public static class SolidWorksInterop
    {
        /// <summary>
        /// Выполняет метод для компонентов сборки (подсборки и детали)
        /// </summary>
        /// <param name="assemblyDoc">Сборка</param>
        /// <param name="actionForPart">Метод для детали</param>
        /// <param name="actionForAssembly">Метод для подсборок</param>
        public static void DoSmthForEachComponent(AssemblyDoc assemblyDoc, Action<ModelDoc2> actionForPart, Action<ModelDoc2> actionForAssembly)
        {
            object[] components = (object[])assemblyDoc.GetComponents(true);

            if (components.Length == 0)
                return;

            foreach (var item in components)
            {
                Component2 component = (Component2)item;
                ModelDoc2 model = component.IGetModelDoc();
                if (model != null)
                {
                    AssemblyDoc ad = model as AssemblyDoc;
                    if (ad != null)
                    {
                        actionForAssembly(model);
                        DoSmthForEachComponent(ad, actionForPart, actionForAssembly);
                        continue;
                    }

                    PartDoc pd = model as PartDoc;
                    if (pd != null)
                        actionForPart(model);
                }
            }
        }      
        /// <summary>
        /// Выполняет метод для компонентов сборки (подсборки и детали)
        /// </summary>
        /// <param name="assemblyDoc">Сборка</param>
        /// <param name="actionForPart">Метод для компонентов</param>
        public static void DoSmthForEachComponent(AssemblyDoc assemblyDoc, Action<ModelDoc2> action)
        {
            DoSmthForEachComponent(assemblyDoc, action, action);
        }
        /// <summary>
        /// Выполняет метод для каждой детали в сборке
        /// </summary>
        /// <param name="assemblyDoc">Сборка</param>
        /// <param name="actionForPart">Метод для детали</param>
        public static void DoSmthForEachPart(AssemblyDoc assemblyDoc, Action<ModelDoc2> actionForPart)
        {
            DoSmthForEachComponent(assemblyDoc, actionForPart, new Action<ModelDoc2>((ad) => { }));
        }
    }
}
