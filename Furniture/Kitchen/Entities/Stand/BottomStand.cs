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

namespace Furniture
{
    public static partial class Kitchen
    {
        public class BottomStand : Stand
        {
            public BottomStand(Component2 bottomStandComponent) : base(bottomStandComponent) { }

            public override string GetName()
            {
                return KitchenComponentTypes.GetName(KitchenComponentTypes.Type.BottomStand);
            }

            public override KitchenComponentTypes.Type GetKitchenType()
            {
                return KitchenComponentTypes.Type.BottomStand;
            }

            public override bool PositionProcess()
            {
                Func(this);
                return true;
                //получить сопряженную с задом плоскость
                //найти другие зады нижних баз, сопряженные с этой плоскостью
                    //если нет
                        //получить точки на этой плоскости
                        //определить к какой точке база расположена ближе
                        //переместить к точке
                    //если есть
                        //получить все базы на полу, которые ни с чем не сопряжены слева/справа и не стоят в углу
                        //если больше одной -> найти ближайшую
                        //определить какая из её плоскостей свободна
                //прицепить к полу
                //???
                //профит
            }

            public Component2 GetNeighborStand2(SwAddin SwAddin, Planes.Type plane)
            {
                Component2 currentStand = null;
                string currentStandName;
                dynamic currentStandMates;

                string neighborStandPlane = "";
                switch (plane)
                {
                    case Planes.Type.Left:
                        neighborStandPlane = Planes.GetName(Planes.Type.Right);
                        break;
                    case Planes.Type.Right:
                        neighborStandPlane = Planes.GetName(Planes.Type.Left);
                        break;
                }

                bool searchFinished = false;
                Measure measure = SwAddin.RootModel.Extension.CreateMeasure();
                bool findComp;
                string projectName = Path.GetFileNameWithoutExtension(SwAddin.RootModel.GetPathName());
                //а если его просто нет?
                while (!searchFinished)
                {
                    findComp = false;
                    if (currentStand == null)
                    {
                        currentStandMates = this.Component.GetMates();
                        currentStandName = this.Component.Name;
                    }
                    else
                    {
                        currentStandMates = currentStand.GetMates();
                        currentStandName = currentStand.Name;
                    }

                    foreach (var mate in currentStandMates)
                    {
                        if (mate is Mate2)
                        {
                            Mate2 spec = (Mate2)mate;
                            int mec = spec.GetMateEntityCount();
                            if (mec > 1)
                            {
                                for (int ik = 0; ik < mec; ik++)
                                {
                                    MateEntity2 me = spec.MateEntity(ik);
                                    string name = me.ReferenceComponent.Name;
                                    if (!name.Contains(currentStandName))
                                    {
                                        Component2 mateStand = me.ReferenceComponent;
                                        string filePath = mateStand.GetPathName();
                                        SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
                                        SwDmDocumentOpenError oe;
                                        var swDoc = swDocMgr.GetDocument(filePath, SwDmDocumentType.swDmDocumentUnknown, true, out oe);
                                        SwDmCustomInfoType swDmCstInfoType;
                                        string valueOfName = "";
                                        //todo: говнокод детектед
                                        try
                                        {
                                            valueOfName = swDoc.GetCustomProperty("KitchenType", out swDmCstInfoType);
                                        }
                                        catch { }
                                        if (valueOfName.ToLower().Contains("тумба"))
                                        {
                                            SwAddin.RootModel.ClearSelection();
                                            SwAddin.RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", Planes.GetName(plane), currentStand.Name, projectName), "PLANE", 0, 0, 0, false, 0, null, 0);
                                            SwAddin.RootModel.Extension.SelectByID2(string.Format("{0}@{1}@{2}", neighborStandPlane, mateStand.Name, projectName), "PLANE", 0, 0, 0, true, 0, null, 0);
                                            measure.Calculate(null);

                                            if (measure.IsIntersect && measure.IsParallel)
                                            {
                                                currentStand = mateStand;
                                                findComp = true;
                                            }
                                            SwAddin.RootModel.ClearSelection();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    searchFinished = !findComp;
                }
                return currentStand;
            }  
        }
    }
}
