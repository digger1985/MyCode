using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Furniture.ProgressBar;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SwDocumentMgr;

namespace Furniture
{
    public partial class frmCopyProject : Form
    {
        private readonly SwAddin _swAdd;
         private readonly ISldWorks _iswApp;
        private readonly string _openFile;
        private readonly ModelDoc2 _swModelDoc;

        public frmCopyProject(SwAddin swAdd,ISldWorks iswApp,ModelDoc2 rootModel,string openFile)
        {
            _swAdd = swAdd;
            _iswApp = iswApp;
            _swModelDoc = rootModel;
            _openFile = openFile;
            InitializeComponent();
            WarningLabel.Text = string.Empty;
        }

        private static void DeleteXmlFiles(string tempDir,string[] allFiles,string _openFile)
        {
                var modelsToDelete = new List<string>();
                string fpFileName = Path.Combine(tempDir, "fpTime.txt");
                bool cicleBreak = false;
                if (File.Exists(fpFileName))
                {
                    var sr = File.OpenText(fpFileName);
                    string line = sr.ReadLine();
                    sr.Close();
                    DateTime fpTime;
                    var culture = CultureInfo.CreateSpecificCulture("ru-RU");
                    var styles = DateTimeStyles.AssumeLocal;
                    if (DateTime.TryParse(line, culture, styles, out fpTime))
                    {

                        foreach (var file in allFiles)
                        {
                            if (File.Exists(file) && !(Path.GetFileName(file).First() == '~') &&
                                Path.GetFileName(file) != Path.GetFileName(_openFile) && Path.GetExtension(file).ToUpper()==".SLDASM")
                            {
                                if (File.GetLastWriteTime(file) > fpTime.AddSeconds(1)) //добавляем секунду
                                {
                                    cicleBreak = true;
                                    modelsToDelete.Add(Path.GetFileNameWithoutExtension(file));
                                }
                            }
                        }
                    }
                }
            if (cicleBreak)
            {
                string delDir = Path.Combine(tempDir, "Программы");
                if (Directory.Exists(delDir))
                {
                    foreach (var file in Directory.GetFiles(delDir, "*.xml", SearchOption.TopDirectoryOnly))
                    {
                        //прежде чем удалить этот файл 1) вытащить ModelName из xml 2) Если он есть в modelsToDelete = > удалить
                        var currXml = new XmlDocument();
                        currXml.Load(file);
                        if (currXml.ChildNodes.Count == 0)
                        {
                            File.Delete(file);
                            continue;
                        }
                        var modelName = Path.GetFileNameWithoutExtension(currXml.ChildNodes[0].Attributes["Name"].Value);
                        if (modelsToDelete.Contains(modelName))
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
        }

        public static void RecopyHeare(SwAddin swAddin, ISldWorks swApp,ModelDoc2 swModelDoc)
        {    
            try 
            {
            SwAddin.IsEventsEnabled = false;            
            swModelDoc.Save();            
            ModelDocExtension swModelDocExt = default(ModelDocExtension);
            PackAndGo swPackAndGo = default(PackAndGo);
            Dictionary<string,string> filesToHideAndShow = new Dictionary<string, string>();
            WaitTime.Instance.ShowWait();
            WaitTime.Instance.SetLabel("Отрыв сборки от библиотеки.");
            int warnings = 0;
            int errors = 0;
            string _openFile = swModelDoc.GetPathName();
         
            swAddin.currentPath = string.Empty;
            swApp.CloseAllDocuments(true);
            if (!Directory.Exists("C:\\Temp"))
                Directory.CreateDirectory("C:\\Temp");
            string tempDir = Path.Combine("C:\\Temp", Path.GetFileNameWithoutExtension(_openFile));
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);
            string fileToOpenTemp = Path.Combine(tempDir, Path.GetFileName(_openFile));
            //File.Copy(_openFile, fileToOpenTemp);
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(Path.GetDirectoryName(_openFile), tempDir);
            if (!File.Exists(fileToOpenTemp))
            {
                throw new Exception("Ошибка. Не найден файл: " + fileToOpenTemp);
            }
            swAddin.DetachEventHandlers();
            swModelDoc = (ModelDoc2)swApp.OpenDoc6(fileToOpenTemp, (int)swDocumentTypes_e.swDocASSEMBLY,
                                                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", errors,
                                                    warnings);
            swAddin.AttachEventHandlers();
            try
            {
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(_openFile)))
                {
                    File.Delete(file);
                }
                foreach (var directory in Directory.GetDirectories(Path.GetDirectoryName(_openFile)))
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(directory, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    //Directory.Delete(directory, true);
                }
            }
            catch (Exception e)
            {
                Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory( tempDir,Path.GetDirectoryName(_openFile),true);
                swAddin.DetachEventHandlers();
                swApp.CloseAllDocuments(true);
                swModelDoc = (ModelDoc2)swApp.OpenDoc6(_openFile, (int)swDocumentTypes_e.swDocASSEMBLY,
                                                        (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", errors,
                                                        warnings);
                swAddin.AttachEventHandlers();
                MessageBox.Show(@"Из-за ошибки: """ + e.Message + @""" не удалось оторвать проект. Перезапустите SW и попробуйте еще раз.");
                return;
            }

            //File.Delete(_openFile);
            swModelDocExt = (ModelDocExtension)swModelDoc.Extension;

            swPackAndGo = (PackAndGo)swModelDocExt.GetPackAndGo();

            //swModelDoc = (ModelDoc2)swApp.OpenDoc6(openFile, (int)swDocumentTypes_e.swDocASSEMBLY, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);
            //swModelDocExt = (ModelDocExtension)swModelDoc.Extension;

            int namesCount = swPackAndGo.GetDocumentNamesCount();

            object fileNames;
            object[] pgFileNames = new object[namesCount - 1];
            bool status = swPackAndGo.GetDocumentNames(out fileNames);
            string[] newFileNames = new string[namesCount];
            string[] rFileNames1 = fileNames as string[];
            if (rFileNames1 == null)
                throw new Exception("Не удалось оторвать заказ.");
            string[] rFileNames = new string[rFileNames1.Length];

            for (int ii = 0; ii < rFileNames1.Length;ii++ )
            {
                var tt1 = Microsoft.VisualBasic.FileSystem.Dir(rFileNames1[ii]);
                if (tt1==null)
                    tt1 = Microsoft.VisualBasic.FileSystem.Dir(rFileNames1[ii], Microsoft.VisualBasic.FileAttribute.Hidden);
                rFileNames[ii] = Path.Combine(Path.GetDirectoryName(rFileNames1[ii]),tt1);
            }
            //удалить неактуальные программы..
            DeleteXmlFiles(tempDir, rFileNames,_openFile);
            //bool isAuxiliary = (swCompModel.get_CustomInfo2("", "Auxiliary") == "Yes");
            //bool isAccessory = (swCompModel.get_CustomInfo2("", "Accessories") == "Yes");

            //string strSubCompNewFileName;
            //if (GetComponentNewFileName(swModel, isAuxiliary, isAccessory,
            //                            isFirstLevel, strSubCompOldFileNameFromModel,
            //                            out strSubCompNewFileName))
            //{
            //}


            string xName = swAddin.GetXNameForAssembly(false, Path.GetFileNameWithoutExtension(_openFile));
            string furnDir = Path.Combine(Path.GetDirectoryName(_openFile), "ФУРНИТУРА");
            string furnHelpDir = Path.Combine(Path.GetDirectoryName(_openFile), "ФУРНИТУРА", "Модели фурнитуры");
            string partDir = Path.Combine(Path.GetDirectoryName(_openFile), "ДЕТАЛИ");
            string partDirHelp = Path.Combine(Path.GetDirectoryName(_openFile), "ДЕТАЛИ", "Вспомогательные детали");
            string partForAssDir = Path.Combine(Path.GetDirectoryName(_openFile), "ДЕТАЛИ", "Детали для сборок");

            //DSOFile.OleDocumentProperties m_oDocument = null;
            //m_oDocument = new DSOFile.OleDocumentPropertiesClass();
            for (int i = 0; i<namesCount;i++)
            {

                //вот пример как выглядит массив элементов в fileNames. Переделать их в newFileNames и все...
        //                [0]	"c:\\temp\\2222\\2222.sldasm"	string
        //[1]	"d:\\_swlib_backup\\крепежная фурнитура\\вспомогательные\\3090_штифт для эксцентрика пластикового 20 мм.sldprt"	string
        //[2]	"d:\\_swlib_backup\\крепежная фурнитура\\вспомогательные\\3093_эксцентрик пластиковый для дсп 16 мм 20 мм.sldprt"	string
        //[3]	"d:\\_swlib_backup\\крепежная фурнитура\\вспомогательные\\отв для арт. 3090.sldprt"	string
        //[4]	"d:\\_swlib_backup\\крепежная фурнитура\\3093+3090_(эксцентрик пластиковый 20 мм для дсп 16 мм+штифт для эксцентрика пластикового 20 мм).sldasm"	string
        //[5]	"d:\\_swlib_backup\\шкафы-купе\\каркасные детали\\дсп 16 мм\\вспомогательные\\8504_панель вкладная 000a01p.sldprt"	string
        //[6]	"d:\\_swlib_backup\\шкафы-купе\\каркасные детали\\дсп 16 мм\\8504f_панель вкладная 000a01p.sldasm"	string

                //в rFileNames[i] вот что: 
                //d:\_swlib_backup\шкафы-купе\каркасы шкафов-купе\вспомогательные\детали для сборок\Панель боковая правая_K02214P.SLDPRT
                //если содержит _swlib_backup и на конце 2 цифры + "P" = заменяем эти 2 цифры на 01 и помещаем оба файлика в список
                //потом пробегусь по списку. 01 - убрать скрытие, для старого - скрыть


                string fileName = Path.GetFileName(rFileNames[i]);
                string tmprFileName = rFileNames[i];
                if (!Directory.Exists(partDir))
                    Directory.CreateDirectory(partDir);
                if (fileName.ToLower().Contains(Path.GetFileName(_openFile.ToLower())))
                {
                    newFileNames[i] = _openFile;
                    continue;
                }
                if (rFileNames[i].ToLower().Contains(tempDir.ToLower()))
                {
                    newFileNames[i] = rFileNames[i].Replace(tempDir.ToLower(), Path.GetDirectoryName(_openFile));
                    continue;
                }
                string idCopyTo = rFileNames[i].Substring(rFileNames[i].Length - 10, 2);
                  int newNumber;
                char lastChar = Path.GetFileNameWithoutExtension(rFileNames[i]).Last();
                if ((lastChar != 'p' && lastChar!='P') || !int.TryParse(idCopyTo, out newNumber))//(rFileNames[i].ToLower().Contains("крепежная фурнитура") || rFileNames[i].ToLower().Contains("фурнитура для каркасов"))
                {
                    if (!Directory.Exists(furnDir))
                        Directory.CreateDirectory(furnDir);


                    if (rFileNames[i].ToLower().Contains("вспомогательные"))
                    {
                        //2222//ФУРНИТУРА//Модели фурнитуры
                        if (!Directory.Exists(furnHelpDir))
                            Directory.CreateDirectory(furnHelpDir);
                        newFileNames[i] = Path.Combine(furnHelpDir, Path.GetFileNameWithoutExtension(rFileNames[i]) + " #" + xName+"-1" + Path.GetExtension(rFileNames[i]));
                        continue;
                    }
                    else
                    {
                        //2222//ФУРНИТУРА//
                        newFileNames[i] = Path.Combine(furnDir, Path.GetFileNameWithoutExtension(rFileNames[i]) + " #" + xName +"-1"+ Path.GetExtension(rFileNames[i]));
                        continue;
                    }
                }
                else
                {
                    tmprFileName = GetFileNameWithoutSuff(rFileNames[i]);
                }
                string fnext = Path.GetFileNameWithoutExtension(tmprFileName);
                string[] arr = fnext.Split('-').ToArray();
                if (arr.Length != 2)
                {
                    //учитывать только последний "-"
                    string[] tmp = new string[2];
                    for (int j = 0; j < arr.Length-1; j++)
                    {
                        tmp[0] = string.Format("{0}-{1}", tmp[0], arr[j]);
                    }
                    tmp[0] = tmp[0].Remove(0, 1);
                    tmp[1] = arr.Last();
                    arr = tmp;
                }
                string newName = arr[0] + " #" + xName+"-" + arr[1] + Path.GetExtension(tmprFileName);
                if (tmprFileName.ToLower().Contains("вспомогательные"))
                {
                    //if (true)//(tmprFileName.ToLower().Contains("детали для сборок"))//|| Path.GetExtension(tmprFileName).ToLower()==".sldprt" )
                    //{
                        if (!Directory.Exists(partForAssDir))
                            Directory.CreateDirectory(partForAssDir);
                        newFileNames[i] = Path.Combine(partForAssDir, newName);
                        continue;
                    //}
                    ////2222\ДЕТАЛИ\Вспомогательные детали
                    //if (!Directory.Exists(partDirHelp))
                    //    Directory.CreateDirectory(partDirHelp);

                    //newFileNames[i] = Path.Combine(partDirHelp, newName);
                    //continue;
                }
                newFileNames[i] = Path.Combine(partDir, newName);
            }
            BStrWrapper[] pgSetFileNames;
            //тут надо немного поправить newFileNames так, чтобы не было одинаковых..
            string[] tmpFileNames = new string[newFileNames.Length];
            int jj = 0;
            foreach (var tmpFileName in newFileNames)
            {
                int same = tmpFileNames.Count(f => f!=null ? f.ToLower() == tmpFileName.ToLower() : false);
                if (same > 0 && !tmpFileName.Contains("фурнитура") && !tmpFileName.Contains("ФУРНИТУРА"))
                {
                    string tmp = Path.GetFileNameWithoutExtension(tmpFileName).Split('-').LastOrDefault();
                    if (string.IsNullOrEmpty(tmp))
                        continue;
                    int tmpNumb;
                    if (!int.TryParse(tmp, out tmpNumb))
                        continue;

                    int index = Path.GetFileNameWithoutExtension(tmpFileName).IndexOf('-');
                    string substr = Path.GetFileNameWithoutExtension(tmpFileName).Substring(0, index)+"-"+(tmpNumb+1).ToString() + Path.GetExtension(tmpFileName);
                    string res = Path.Combine(Path.GetDirectoryName(tmpFileName), substr);
                next:
                    same = tmpFileNames.Count(f => f != null ? f.ToLower() == res.ToLower() : false);
                    if (same > 0)
                    {
                        tmp = Path.GetFileNameWithoutExtension(res).Split('-').LastOrDefault();
                        if (string.IsNullOrEmpty(tmp))
                            continue;
                        if (!int.TryParse(tmp, out tmpNumb))
                            continue;
                        index = Path.GetFileNameWithoutExtension(res).IndexOf('-');
                        substr = Path.GetFileNameWithoutExtension(res).Substring(0, index) + "-" + (tmpNumb + 1).ToString() + Path.GetExtension(tmpFileName);
                        res = Path.Combine(Path.GetDirectoryName(res), substr);
                        goto next;
                    }
                    tmpFileNames[jj] = res;
                }
                else
                {
                    tmpFileNames[jj] = tmpFileName;
                }
                jj++;
            }
            newFileNames = tmpFileNames;
            pgSetFileNames = ObjectArrayToBStrWrapperArray(newFileNames);
            //var documentsToRemove = ObjectArrayToBStrWrapperArray(newFileNames.Where(x => x == "C:\\temp").ToArray());
            //swPackAndGo.RemoveExternalDocuments(documentsToRemove);
            status = swPackAndGo.SetDocumentSaveToNames(pgSetFileNames);

            var statuses = (int[])swModelDocExt.SavePackAndGo(swPackAndGo);
            //перед открытием создать файлик dictionary
            string dictFile= Path.Combine(Path.GetDirectoryName(_openFile), Path.GetFileNameWithoutExtension(_openFile) + "_dictionary.txt");
            if (File.Exists(dictFile))
                File.Delete(dictFile);
            using (File.CreateText(dictFile)) { }; //File.Create(dictFile);
            List<string> strArr = new List<string>();
            foreach (string file in Directory.GetFiles(partDir, "*.SLDASM", SearchOption.TopDirectoryOnly))
            {
                strArr.Add(file);
            }
            File.SetAttributes(dictFile, FileAttributes.Normal);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(dictFile, true))
            {
                foreach (var line in strArr)
                {
                    file.WriteLine(line);
                }
                file.Close();
            }
            //File.WriteAllLines(dictFile, strArr);
            File.SetAttributes(dictFile, FileAttributes.Hidden);

            //тут проверить что все оторвалось. Если что, поправить ссылки
            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
            SwDmDocumentOpenError oe;
            SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
            object brokenRefVar;
            string[] files = Directory.GetFiles(Path.GetDirectoryName(_openFile),"*.*",SearchOption.AllDirectories);
            foreach (string fileName in files)
            {
                SwDMDocument8 swDoc = null;
                if (fileName.ToLower().Contains("sldasm"))
                    swDoc = (SwDMDocument8) swDocMgr.GetDocument(fileName,
                                                                 SwDmDocumentType.swDmDocumentAssembly,
                                                                 false, out oe);
                if (swDoc != null)
                {

                    var varRef = (string[]) swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                    if (varRef != null && varRef.Length != 0)
                    {
                        foreach (string o in varRef)
                        {
                            if (o.ToUpper().Contains("_SWLIB_BACKUP"))
                            {
                                string tt = Path.GetFileNameWithoutExtension(o);
                                string newRef = null;
                                foreach (var fn in files)
                                {
                                    if (fn.Contains(tt))
                                    {
                                        newRef = fn;
                                        break;
                                    }
                                }
                                if (newRef != null)
                                {
                                    swDoc.ReplaceReference(o, newRef);
                                    swDoc.Save();
                                }

                            }
                        }
                    }
                    else
                    {
                        swDoc.CloseDoc();
                        continue;
                    }
                    swDoc.CloseDoc();
                }
            }
            swApp.CloseAllDocuments(true);
            if (!File.Exists(_openFile))
                throw new Exception("Ошибка. Не найден файл: " + _openFile);
            foreach (var file in newFileNames)
            {
                File.SetAttributes(file,FileAttributes.Normal);
            }
            //Logging.Log.Instance.Debug("SetAttributes ");
            SwAddin.IsEventsEnabled = true;
            swModelDoc = (ModelDoc2)swApp.OpenDoc6(_openFile, (int)swDocumentTypes_e.swDocASSEMBLY,
                                                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", errors,
                                                    warnings);
            SwAddin.IsEventsEnabled = false;
            //Logging.Log.Instance.Debug("OpenDoc6");
            swModelDoc.EditRebuild3();
            swAddin.AttachModelDocEventHandler(swModelDoc);
            //теперь тут пробежатся по всем файлам в filesToHideAndShow. key -хайдить , value - показывать
            //foreach (var files in filesToHideAndShow)
            //{
            //    if (File.Exists(files.Key) && File.Exists(files.Value))
            //    {
            //        File.SetAttributes(files.Value, FileAttributes.Normal);
            //        File.SetAttributes(files.Key, FileAttributes.Hidden);
            //        string nextFile,sourceFile = files.Key;
            //        while (SwAddin.IfNextExist(sourceFile, out nextFile))
            //        {
            //            sourceFile = nextFile;
            //            File.SetAttributes(nextFile, FileAttributes.Hidden);
            //        }
            //    }
            //}
            SwDmCustomInfoType swDmCstInfoType;
            if (Directory.Exists(furnDir))
            {
                foreach (string path in Directory.GetFiles(furnDir, "*.sldasm", SearchOption.TopDirectoryOnly))
                {
                    if (!File.Exists(path))
                        continue;
                    var swDoc = swDocMgr.GetDocument(path, SwDmDocumentType.swDmDocumentAssembly, true, out oe);
                    if (Path.GetFileNameWithoutExtension(path).First() == '~' || swDoc == null)
                        continue;
                    string valueOfName = swDoc.GetCustomProperty("Accessories", out swDmCstInfoType);
                    swDoc.CloseDoc();
                    if (valueOfName != null && (valueOfName == "No" || valueOfName == "no"))
                    {
                        File.Delete(path);
                    }
                }
            }
            var oComps = (object[])((AssemblyDoc)swModelDoc).GetComponents(true);
            if (oComps != null)
            {
                
                foreach (var oComp in oComps)
                {
                    var comp = (Component2)oComp;
                    var model = (ModelDoc2)comp.GetModelDoc();
                    if (model != null)
                    {
                        File.SetAttributes(model.GetPathName(), FileAttributes.Normal);
                        var swDoc = swDocMgr.GetDocument(model.GetPathName(), SwDmDocumentType.swDmDocumentUnknown, true, out oe);
                        if (swDoc != null)
                        {
                            var names = (string[])swDoc.GetCustomPropertyNames();
                            try
                            {
                                foreach (var name in names)
                                {
                                    
                                    string valueOfName = swDoc.GetCustomProperty(name, out swDmCstInfoType);
                                    string lowValOfName = valueOfName.ToLower();

                                    if (lowValOfName.Contains("@") &&
                                        lowValOfName.Contains("#") &&
                                        (lowValOfName.Contains(".sld")))
                                    {
                                        var split = valueOfName.Split('.');
                                        string tmp = split.First();
                                        string ext = split.Last();
                                        if (lowValOfName.Contains("#" + xName) || tmp.ToUpper().Last()!='P')
                                            continue;
                                        string sid = tmp.Substring(tmp.Length - 3, 2);
                                        int id;
                                        if (!int.TryParse(sid,out id))
                                            continue;
                                        tmp = tmp.Substring(0, tmp.Length - 4);
                                        tmp = tmp +" #" +xName+"-"+id +"."+ext;

                                        swAddin.SetModelProperty(model, name, "",
                                                                swCustomInfoType_e.
                                                                    swCustomInfoText,
                                                                tmp);
                                        model.Save(); 

                                        //swDoc.SetCustomProperty(name, tmp);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                Logging.Log.Instance.Debug("Ошибка при попытке обратится к св-ву. Деталь: " + swDoc.FullName);
                            }
                            finally
                            {
                                swDoc.CloseDoc();
                            }
                        }

                    }
                }
            }


            Cash.ActualizaAllCash();
            SwAddin.IsEventsEnabled = true;
            linksToCash.Remove(_openFile);
            //скопировать fpTime.txt
            string fpTime = Path.Combine(tempDir, "fpTime.txt");
            if (File.Exists(fpTime))
                File.Copy(fpTime, Path.Combine(Path.GetDirectoryName(_openFile), "fpTime.txt"));
            CopyDrawings(rFileNames, newFileNames, tempDir, _openFile);
            CopyProgramms(rFileNames, newFileNames,tempDir, _openFile);
            
            
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }
            finally 
            {
                WaitTime.Instance.HideWait();

            }
                                   
        }
        private static void CopyProgramms(string[] sorceFnames,string[] destFnames,string tmpDir, string openFileName)
        {
            var tmpProgDir = Path.Combine(tmpDir, "Программы");
            if (Directory.Exists(tmpProgDir))
            {
                var xmlFiles = Directory.GetFiles(tmpProgDir, "*.xml",
                                                     SearchOption.TopDirectoryOnly);
                if (xmlFiles.Length != 0)
                {
                    var progPath = Path.Combine(Path.GetDirectoryName(openFileName), "Программы");
                    if (!Directory.Exists(progPath))
                    {
                        Directory.CreateDirectory(progPath);
                    }
                    string currentSearchFile;
                    foreach (var xmlFile in xmlFiles)
                    {

                        var copyToName = Path.GetFileName(xmlFile);
                        string copyToXmlFile = Path.Combine(progPath, copyToName);

                        File.Copy(xmlFile, copyToXmlFile);
                        var currXml = new XmlDocument();
                        currXml.Load(copyToXmlFile);
                        
                        if (!string.IsNullOrEmpty(currXml.DocumentElement.Attributes[0].Value))
                        {
                            currentSearchFile =Path.GetFileNameWithoutExtension(currXml.DocumentElement.Attributes[0].Value) +
                                ".SLDASM";
                            int index;
                            if (Contains(sorceFnames, currentSearchFile, out index))
                            {
                                currXml.DocumentElement.Attributes[0].Value =
                                    currXml.DocumentElement.Attributes[0].Value.Replace(Path.GetFileNameWithoutExtension(sorceFnames[index]),Path.GetFileNameWithoutExtension(destFnames[index]));
                                currXml.Save(copyToXmlFile);
                            }
                        }
                    }
                }
            }
        }
        private static void CopyDrawings(string[] sorceFnames,string[] destFnames,string tmpDir,string openFileName)
        {
            string sourceDir = Path.GetDirectoryName(openFileName);
            var allAsm = Directory.GetFiles(sourceDir, "*.SLDASM", SearchOption.AllDirectories);
            int index;
            SwDmDocumentOpenError oe;
            object brokenRefVar;

            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
            foreach (var drw in Directory.GetFiles(tmpDir,"*.SLDDRW",SearchOption.AllDirectories))
            {
                if (drw.ToUpper().Contains("ЧЕРТЕЖИ"))
                    continue;
                //найти аналог-ый файл
                if (Contains(allAsm, Path.GetFileNameWithoutExtension(drw)+".SLDASM", out index))
                {
                    string whereToCopy = Path.Combine(Path.GetDirectoryName(allAsm[index]), Path.GetFileNameWithoutExtension(allAsm[index]) + ".SLDDRW");
                    File.Copy(drw, whereToCopy);

                    File.SetAttributes(whereToCopy, FileAttributes.Normal);

                    var swDoc = (SwDMDocument8)swDocMgr.GetDocument(whereToCopy, SwDmDocumentType.swDmDocumentDrawing, false, out oe);
                    SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
                    var varRef = (object[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                    if (varRef.Count() == 1)
                    {
                        swDoc.ReplaceReference((string)varRef[0], allAsm[index]);
                    }
                    SwDmDocumentSaveError lngSaveRes = swDoc.Save();
                    swDoc.CloseDoc();
                }
            }
            var tmpDrawDir = Path.Combine(tmpDir, "ЧЕРТЕЖИ");
            string currentSearchFile;
            Dictionary<string,string> filesToCopy = new Dictionary<string, string>();
            if (Directory.Exists(tmpDrawDir))
            {
                foreach (string drawingFile in Directory.GetFiles(tmpDrawDir,"*.SLDDRW",SearchOption.TopDirectoryOnly))
                {
                    currentSearchFile = Path.GetFileNameWithoutExtension(drawingFile)+ ".SLDASM";
              
                    if (Contains(sorceFnames,currentSearchFile,out index))
                    {
                        //int index = Array.IndexOf(sorceFnames, currentSearchFile);
                        string whereToCopy =Path.Combine(Path.GetDirectoryName(destFnames[index]), Path.GetFileNameWithoutExtension(destFnames[index]) + ".SLDDRW");
                        filesToCopy.Add(drawingFile, whereToCopy);
                    }
                }
                foreach (var item in filesToCopy)
                {
                    File.Copy(item.Key,item.Value);
                    //поменять ссылку из чертежа...
                    File.SetAttributes(item.Value, FileAttributes.Normal);


                    var swDoc = (SwDMDocument8)swDocMgr.GetDocument(item.Value, SwDmDocumentType.swDmDocumentDrawing, false, out oe);
                    SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
                    var varRef = (object[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                    if (varRef.Count() == 1)
                    {
                        swDoc.ReplaceReference((string)varRef[0], Path.Combine(Path.GetDirectoryName(item.Value),Path.GetFileNameWithoutExtension(item.Value)+".SLDASM"));
                    }
                    SwDmDocumentSaveError lngSaveRes = swDoc.Save();
                    swDoc.CloseDoc();

                }
            }
        }
        private static bool Contains(string[] sourceFnames,string currentSearchFile,out int index)
        {
            bool result = false;
            index = -1;
            for (int i =0;i<sourceFnames.Length;i++)
            {
                if (Path.GetFileName(sourceFnames[i]) == currentSearchFile)
                {
                    index = i;
                    result = true;
                    break;
                }
            }
            return result;
        }
        private static string GetFileNameWithoutSuff(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Last().ToString().ToLower() != "p")
                return path;
            int newNumber;
            if (int.TryParse(fileName.Substring(fileName.Length - 3, 2), out newNumber))
            {
                string result = Path.Combine(Path.GetDirectoryName(path),fileName.Substring(0, fileName.Length - 4)+"-"+newNumber.ToString() + Path.GetExtension(path));
                return result;
            }
            else
                return path;
        }


        private void btOk_Click(object sender, EventArgs e)
        {
         

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            WaitTime.Instance.ShowWait();
            WaitTime.Instance.SetLabel("Копирование проекта в другой заказ...");
            try
            {
                #region проверка типа как у Игоря, проверяю по датам файлов что время последней окончат. обработки >= времени всех sldprt и sldasm
                //считываем время последней окончательной обработки

                List<string> xmlFilesToDelete, allModels;
                bool copyXmlFiles = !SwAddin.GetDeleteXmlFiles(_openFile,out xmlFilesToDelete,out allModels);//false;

                #endregion
                string copyToNumber = string.Empty;
                string myPath = tbPath.Text;
                copyToNumber = myPath.Split(Path.DirectorySeparatorChar).LastOrDefault();
                string openFile = _openFile;
                if (!Directory.Exists(myPath) || string.IsNullOrEmpty(copyToNumber))
                {
                    MessageBox.Show("Папка, куда копировать - не создана!");
                    stopwatch.Stop();
                    return;
                }

                bool status = false;
                int warnings = 0;
                int errors = 0;
                int[] statuses = null;
                string myFileName = null;

                string copyFromNumber = Path.GetFileNameWithoutExtension(openFile);

                string copyFromHex = GetXNameForAssembly(copyFromNumber);
                string copyToHex = GetXNameForAssembly(copyToNumber);
                var match = Regex.Match(copyToNumber, @"\d\d\d\d\d\d-\d\d\d\d-\d\d\d\d");
                if (string.IsNullOrEmpty(copyFromHex) || string.IsNullOrEmpty(copyToHex) || !match.Success)
                {
                    MessageBox.Show("Папка, куда копировать - не соответствует маске: ХХХХХХ-ХХХХ-ХХХХ, где Х - цифра.");
                    stopwatch.Stop();
                    return;
                }
                if (Directory.GetFiles(myPath).Length != 0)
                {
                    if (MessageBox.Show(@"Папка назначения не пуста. Перед копированием заказа находящееся в этой папке будет удалено!! Продолжить?", "Папка назначения не пуста!", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                    {
                        stopwatch.Stop();
                        return;
                    }
                }
                ModelDoc2 swModelDoc = default(ModelDoc2);
                ModelDocExtension swModelDocExt = default(ModelDocExtension);
                PackAndGo swPackAndGo = default(PackAndGo);


                ISldWorks swApp = _iswApp;

                swModelDoc = _swModelDoc;


                //проверить, содержит ли путь к openFile только ASCII. Если не только ASCII, то выкинуть ошибку и ничего не делать.
                bool deleteFromTmp = false;
                foreach (char c in _openFile)
                {
                    if ((int)c > 127)
                    {
                        //скопировать все в temp директорию.., закрыть все, открыть из temp Директории, скопировать оттуда.
                        swApp.CloseAllDocuments(true);
                        if (!Directory.Exists("C:\\Temp"))
                            Directory.CreateDirectory("C:\\Temp");
                        string tempDir = Path.Combine("C:\\Temp", Path.GetFileNameWithoutExtension(_openFile));
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                        Directory.CreateDirectory(tempDir);

                        Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(Path.GetDirectoryName(_openFile), tempDir);

                        string fileToOpenTemp = Path.Combine(tempDir, Path.GetFileName(_openFile));

                        if (!File.Exists(fileToOpenTemp))
                        {
                            throw new Exception("Ошибка. Не найден файл: " + fileToOpenTemp);
                        }
                        _swAdd.DetachEventHandlers();
                        swModelDoc = (ModelDoc2)swApp.OpenDoc6(fileToOpenTemp, (int)swDocumentTypes_e.swDocASSEMBLY,
                                                                (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", errors,
                                                                warnings);
                        _swAdd.AttachEventHandlers();
                        openFile = fileToOpenTemp;
                        deleteFromTmp = true;
                        break;
                        //MessageBox.Show(
                        //    "Путь к проекту, который вы хотите скопировать должен содержать только английские буквы! Путь:" +
                        //    _openFile);
                        //return;
                    }
                }


                swModelDocExt = (ModelDocExtension) swModelDoc.Extension;

                swPackAndGo = (PackAndGo) swModelDocExt.GetPackAndGo();

                //swModelDoc = (ModelDoc2)swApp.OpenDoc6(openFile, (int)swDocumentTypes_e.swDocASSEMBLY, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);
                //swModelDocExt = (ModelDocExtension)swModelDoc.Extension;

                int namesCount = swPackAndGo.GetDocumentNamesCount();

                object fileNames;
                object[] pgFileNames = new object[namesCount - 1];
                status = swPackAndGo.GetDocumentNames(out fileNames);
                pgFileNames = (object[]) fileNames;
                string oldPath = Path.GetDirectoryName(openFile).ToUpper();
                string[] newFileNames = new string[namesCount];
                string currentPath;
                int j = 0;

                string[] allDrwFilesInOldDirectory1 = Directory.GetFiles(oldPath, "*.SLDDRW",
                                                                         SearchOption.AllDirectories);
                List<string> allDrwFilesInOldDirectory = new List<string>(allDrwFilesInOldDirectory1.Length);
                foreach (var file in allDrwFilesInOldDirectory1)
                {
                    allDrwFilesInOldDirectory.Add(Path.GetFileNameWithoutExtension(file).ToLower());
                }
                string[] allPrtAndAsmFilesInOldDirectory1 = Directory.GetFiles(oldPath, "*.SLDASM",
                                                                               SearchOption.AllDirectories);
                string[] allPrtAndAsmFilesInOldDirectory2 = Directory.GetFiles(oldPath, "*.SLDPRT",
                                                                               SearchOption.AllDirectories);
                //var allPrtAndAsmFilesInOldDirectory3 = allPrtAndAsmFilesInOldDirectory1.Union(allPrtAndAsmFilesInOldDirectory2);
                Dictionary<string, string> allPrtAndAsmFilesInOldDirectory = new Dictionary<string, string>();
                foreach (var file in allPrtAndAsmFilesInOldDirectory1)
                {
                    if (allPrtAndAsmFilesInOldDirectory.ContainsKey(Path.GetFileName(file)))
                        continue;
                    else
                    {
                        allPrtAndAsmFilesInOldDirectory.Add(Path.GetFileName(file).ToLower(), file);
                    }
                }
                foreach (var file in allPrtAndAsmFilesInOldDirectory2)
                {
                    if (allPrtAndAsmFilesInOldDirectory.ContainsKey(Path.GetFileName(file)))
                        continue;
                    else
                    {
                        allPrtAndAsmFilesInOldDirectory.Add(Path.GetFileName(file).ToLower(), file);
                    }
                }



                for (int i = 0; i <= namesCount - 1; i++)
                {
                    if (!(((string) pgFileNames[i]).ToUpper().Contains(oldPath)))
                        continue;
                    if (allPrtAndAsmFilesInOldDirectory.ContainsKey(Path.GetFileName((string) pgFileNames[i])))
                    {
                        //подставить обычное имя.
                        myFileName =
                            Path.GetFileName(allPrtAndAsmFilesInOldDirectory[Path.GetFileName((string) pgFileNames[i])]);
                    }
                    else
                        throw new Exception(
                            "Необрабатоваемое исключение при копировании проекта: в словаре не найден элемент. Обратитесь к разработчику Addin-а");

                    currentPath = Path.GetDirectoryName((string) pgFileNames[i]).ToUpper().Replace(oldPath, myPath);


                    if (allDrwFilesInOldDirectory.Contains(Path.GetFileNameWithoutExtension(myFileName).ToLower()))
                    {
                        //скопировать соответствующий чертеж с соответствующими изменениями..
                        string fileToCopyTo = string.Empty;
                        string fileToCopyFrom =
                            Directory.GetFiles(oldPath,
                                               Path.GetFileNameWithoutExtension((string) pgFileNames[i]) + ".SLDDRW",
                                               SearchOption.AllDirectories).FirstOrDefault();
                        if (fileToCopyFrom != null)
                        {
                            //тут поменять путь и частично имя файла
                            if (fileToCopyFrom.Contains(copyFromHex))
                                fileToCopyTo = fileToCopyFrom.Replace(copyFromHex, copyToHex);
                            if (fileToCopyFrom.Contains(copyFromHex.ToLower()))
                                fileToCopyTo = fileToCopyFrom.Replace(copyFromHex.ToLower(), copyToHex);

                            fileToCopyTo = Path.Combine(currentPath, Path.GetFileName(fileToCopyTo));
                            //создать destination path если надо    
                            //Запомнить, а не копировать, тк не все пути созданы
                            //drwCopy.Add(new KeyValuePair<string, string>(fileToCopyFrom, fileToCopyTo));
                        }
                    }

                    if (myFileName.Contains(copyFromNumber))
                        myFileName = myFileName.Replace(copyFromNumber, copyToNumber);
                    if (myFileName.Contains(copyFromHex))
                        myFileName = myFileName.Replace(copyFromHex, copyToHex);
                    if (myFileName.Contains(copyFromHex.ToLower()))
                        myFileName = myFileName.Replace(copyFromHex.ToLower(), copyToHex);
                    myFileName = Path.Combine(currentPath, myFileName);
                    //myFileName = myPath + myFileName;
                    newFileNames[i] = (string) myFileName;
                    j = j + 1;
                }

                BStrWrapper[] pgSetFileNames;
                pgSetFileNames = ObjectArrayToBStrWrapperArray(newFileNames);
                status = swPackAndGo.SetDocumentSaveToNames(pgSetFileNames);
                swPackAndGo.IncludeDrawings = true;
                object getFileNames;
                object getDocumentStatus;
                string[] pgGetFileNames = new string[namesCount - 1];

                status = swPackAndGo.GetDocumentSaveToNames(out getFileNames, out getDocumentStatus);
                pgGetFileNames = (string[]) getFileNames;
                var getDocumentStatusInt = (int[]) (getDocumentStatus);
                int tt = 0;
                foreach (var i in getDocumentStatusInt)
                {
                    if (i == 1)
                    {
                        MessageBox.Show("Не удалось копировать проект. Деталь " + pgGetFileNames[tt] + " создана в контексте основной сборки, что недопустимо. Создайте указанную деталь в отдельном файле, затем добавьте ее в основную сборку.", "Не удалось копировать проект.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    tt++;
                }

                // Pack and Go

                statuses = (int[]) swModelDocExt.SavePackAndGo(swPackAndGo);
                
                string fileToOpen = Path.Combine(myPath, copyToNumber + ".SLDASM");
                swApp.CloseAllDocuments(true);
                if (!File.Exists(fileToOpen))
                    throw new Exception("Ошибка. Не найден файл: "+fileToOpen);
                swModelDoc = (ModelDoc2) swApp.OpenDoc6(fileToOpen, (int) swDocumentTypes_e.swDocASSEMBLY,
                                                        (int) swOpenDocOptions_e.swOpenDocOptions_Silent, "", errors,
                                                        warnings);
                swModelDoc.EditRebuild3();
                _swAdd.AttachModelDocEventHandler(swModelDoc);

                string newDictFileName = Path.Combine(Path.GetDirectoryName(fileToOpen), "fpTime.txt");
                var sw = File.CreateText(newDictFileName);
                var culture = CultureInfo.CreateSpecificCulture("ru-RU");
                sw.WriteLine(DateTime.Now.AddSeconds(3).ToString(culture));//Добавляем 3 сек потому что Игорь не может получить точную дату изменения файла.
                sw.Close();

                //тут пробежатся по всем моделям и сделать им "копировать чертежи"
                var oComps = (object[])((AssemblyDoc)swModelDoc).GetComponents(true);
                if (oComps != null)
                {
                    SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
                    SwDmDocumentOpenError oe;
                    foreach (var oComp in oComps)
                    {
                        var comp = (Component2)oComp;
                        var model = (ModelDoc2)comp.GetModelDoc();
                        if (model!=null)
                        {
                            var swDoc = swDocMgr.GetDocument(model.GetPathName(), SwDmDocumentType.swDmDocumentUnknown, true, out oe);

                            if (swDoc != null)
                            {
                                var names = (string[])swDoc.GetCustomPropertyNames();
                                foreach (var name in names)
                                {
                                    SwDmCustomInfoType swDmCstInfoType;
                                    string valueOfName = swDoc.GetCustomProperty(name, out swDmCstInfoType);
                                    string lowValOfName = valueOfName.ToLower();

                                    if (lowValOfName.Contains("@") &&
                                        lowValOfName.Contains("#") &&
                                        (lowValOfName.Contains(".sld")))
                                    {
                                        if (lowValOfName.Contains(copyFromHex.ToLower()))
                                        {
                                            string newStrVal = valueOfName.Replace(copyFromHex, copyToHex);

                                             _swAdd.SetModelProperty(model, name, "",
                                                                 swCustomInfoType_e.
                                                                     swCustomInfoText,
                                                                 newStrVal);
                                                model.Save();  
                                        }
                                    }
                                }
                                swDoc.CloseDoc();
                            }

                        }
                        if (comp.Select(false))
                            _swAdd.CopyDrawings2(true, false, Path.GetDirectoryName(openFile));
                    }
                }
                //скопировать программы (*.xml)
                //if (copyXmlFiles)
                //{
                    var xmlFiles = Directory.GetFiles(Path.GetDirectoryName(openFile), "*.xml",
                                                      SearchOption.AllDirectories);
                    int xopyedXmlFiles = 0;
                    if (xmlFiles.Length != 0)
                    {
                        var progPath = Path.Combine(myPath, "Программы");
                        if (!Directory.Exists(progPath))
                        {
                            Directory.CreateDirectory(progPath);
                        }
                        foreach (var xmlFile in xmlFiles)
                        {

                            var copyToName = Path.GetFileName(xmlFile);
                            if (copyToName.Contains(copyFromHex))
                                copyToName = copyToName.Replace(copyFromHex, copyToHex);
                            if (copyToName.Contains(copyFromNumber))
                                copyToName = copyToName.Replace(copyFromNumber, copyToNumber);
                            string copyToXmlFile = Path.Combine(progPath, copyToName);

                            File.Copy(xmlFile, copyToXmlFile);
                            xopyedXmlFiles = xopyedXmlFiles + 1;
                            var currXml = new XmlDocument();
                            currXml.Load(copyToXmlFile);
                            if (xmlFilesToDelete.Contains(Path.GetFileNameWithoutExtension(currXml.DocumentElement.Attributes[0].Value)) || !allModels.Contains(Path.GetFileNameWithoutExtension(currXml.DocumentElement.Attributes[0].Value)))
                            {
                                File.Delete(copyToXmlFile);
                                continue;
                            }
                            currXml.DocumentElement.Attributes[0].Value =
                                currXml.DocumentElement.Attributes[0].Value.Replace(copyFromHex, copyToHex);
                            currXml.Save(copyToXmlFile);
                        }
                    }
                //}
                //удалить из tmp  если надо
                if (deleteFromTmp)
                {
                    string tempDir = Path.Combine("C:\\Temp", Path.GetFileNameWithoutExtension(openFile));
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                if (xopyedXmlFiles>0)
                {
                    WaitTime.Instance.HideWait();
                    stopwatch.Stop();
                    string totalTimeElapsed1 = string.Format("{0} часов {1} минут {2} секунд", stopwatch.Elapsed.Hours, stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds);

                    MessageBox.Show("Время копирования проекта: " + totalTimeElapsed1 + @".  Для корректного завершения операции копирования проекта в другой заказ, ,будет произведена операция ""Окончательная обработка"".");
                    _swAdd.FinalProcessing();
                    return;
                }
                    WaitTime.Instance.HideWait();
                    stopwatch.Stop();

                    string totalTimeElapsed = string.Format("{0} часов {1} минут {2} секунд", stopwatch.Elapsed.Hours,
                                                            stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds);

                    bool goFinalProcessing = (MessageBox.Show(
                        "Время копирования проекта: " + totalTimeElapsed +
                        @".  Для корректного завершения операции копирования проекта в другой заказ, необходимо произвести операцию ""Окончательная обработка"". Произвести ее сейчас?",
                        "Произвести окончательную обработку сейчас?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                                              DialogResult.Yes);
                    if (goFinalProcessing)
                    {

                        _swAdd.FinalProcessing();
                    }
            }
            catch (Exception exc)
            {
                throw exc;
            }
            finally
            {
                WaitTime.Instance.HideWait();
                stopwatch.Stop();
                Close();
            }
        }

        private static string GetXNameForAssembly(string number)
        {
            string orderNumber = "";
            try
            {
                var orderNumberArrays = number.Split('-');
                orderNumber = orderNumberArrays.Aggregate(orderNumber, (current, orderNumberArray) => current + orderNumberArray);
                ulong codeName = Convert.ToUInt64(orderNumber);
                orderNumber = codeName.ToString("X");
            }
            catch
            {
                orderNumber = "";
            }
            return orderNumber;
        }
        private static BStrWrapper[] ObjectArrayToBStrWrapperArray(object[] SwObjects)
        {
            int arraySize;
            arraySize = SwObjects.GetUpperBound(0);
            BStrWrapper[] dispwrap = new BStrWrapper[arraySize + 1];
            int arrayIndex;

            for (arrayIndex = 0; arrayIndex < arraySize + 1; arrayIndex++)
            {
                dispwrap[arrayIndex] = new BStrWrapper((string)(SwObjects[arrayIndex]));
            }

            return dispwrap;

        }
        private void btGetPath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                tbPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }
    }
}
