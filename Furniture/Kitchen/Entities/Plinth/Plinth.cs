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
        public class Plinth : KitchenComponent
        {
            public Plinth(Component2 plinthComponent) : base(plinthComponent) { }

            public override string GetName()
            {
                return KitchenComponentTypes.GetName(KitchenComponentTypes.Type.Plinth);
            }

            public override KitchenComponentTypes.Type GetKitchenType()
            {
                return KitchenComponentTypes.Type.Plinth;
            }

            public override bool PositionProcess()
            {
                throw new NotImplementedException();
            }
        }
    }
}
