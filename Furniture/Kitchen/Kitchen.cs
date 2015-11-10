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
        public static void Func(Kitchen.KitchenComponent kComp)
        {
            //допустим это короб
            if (kComp.GetKitchenType() == KitchenComponentTypes.Type.BottomStand)
            {
                Mate2[] mates = kComp.Component.GetMates();


            }

            if (kComp.GetKitchenType() == KitchenComponentTypes.Type.Tabletop)
            {
                object[] mates2 = kComp.Component.GetMates();
                foreach (var tempMate in mates2)
                {
                    Mate2 mate = (Mate2)tempMate;

                      //var v = mate.

                    int entCount = mate.GetMateEntityCount();
                    if (entCount > 1)
                    {
                        for (int ik = 0; ik < entCount; ik++)
                        {
                            MateEntity2 mateEntity = mate.MateEntity(ik);
                            Component2 refComponent = mateEntity.ReferenceComponent;
                            if (refComponent.Name != kComp.Component.Name)
                            {
                                List<string> planes = new List<string>();

                                foreach (Planes.Type planeType in Planes.AllPlanes)
                                {
                                    Feature plane = refComponent.FeatureByName(Planes.GetName(planeType));
                                    if (plane != null)
                                    {
                                        //определить она ли сопряжена
                                    }
 
                                }
                                
                                
                                /*/
                                var fg = refComponent.FeatureByName("dsds");

                                Feature feature = refComponent.FirstFeature();
                                while (feature != null)
                                {
                                    Planes.Type planeType = Planes.GetType(feature.Name);
                                    if (planeType != Planes.Type.Undefined)
                                        planes.Add(feature.Name);
                                    feature = feature.GetNextFeature();
                                }
                                /*/

                            }
                        }
                    }

                }



            }
        }



    }
}
