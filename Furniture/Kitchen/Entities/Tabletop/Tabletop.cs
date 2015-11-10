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
        public class Tabletop : KitchenComponent
        {
            public Tabletop(Component2 tabletopComponent) : base(tabletopComponent) { }

            public override string GetName()
            {
                return KitchenComponentTypes.GetName(KitchenComponentTypes.Type.Tabletop);
            }

            public override KitchenComponentTypes.Type GetKitchenType()
            {
                return KitchenComponentTypes.Type.Tabletop;
            }

            public override bool PositionProcess()
            {
                //получить плоскость, с которой сопряжена нижняя плоскость столешницы (подумать что за плоскость возвращается)
                //убедиться что плоскость принадлежит коробу
                //проверить тип короба
                    //если обычный                        
                        //проверить тип столешницы
                            //обычная
                                //ищем крайний правый и левый короба, присоединенные к текущему коробу
                                //устанавливаем длину и ширину столешницы
                                //сопряжаем зад столешницы с задней плоскостью короба
                                //сопряжаем, скажем, левую плоскость столешницы с левой плоскостью крайнего правого короба  
                            //угловая
                                //выдаем ошибку - угловую столешницу можно поместить только на угловой короб                            
                    //если угловой
                        //проверяем тип столешницы
                            //обычная
                                //ошибка - на угловой короб только угловую столешницу (или предупреждение?)
                            //угловая
                                //определяем крайний короб из набора коробов, присоединенного на плоскость спереди короба
                                //определяем крайний короб из набора коробов, присоединенного к плоскости (левая или правая?) короба
                                //устанавливаем длину и ширину столешницы
                                //цепляем оба зада к задам короба
                //???                                    
                //профит

                Func(this);
                return true;
            }
        }
    }
}
