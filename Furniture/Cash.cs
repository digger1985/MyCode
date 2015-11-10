using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Ionic.Zip;
using Microsoft.VisualBasic.FileIO;
using SwDocumentMgr;
using SearchOption = System.IO.SearchOption;

namespace Furniture
{
    public class Cash
    {
        public static event EventHandler CashModeChanged;
        public static void RaiseCashModeChangedEvent()
        {
            var handler = CashModeChanged;
            if (handler != null)
                handler(typeof(Cash), EventArgs.Empty);
        }

        public static event EventHandler CashModeAvailableChanged;
        public static void RaiseCashModeAvailableChangedEvent()
        {
            var handler = CashModeAvailableChanged;
            if (handler != null)
                handler(typeof(Cash), EventArgs.Empty);
        }

        const string warnMessage = @"ОБРАЩАЕМ ВАШЕ ВНИМАНИЕ ! В режиме работы КЭШ редактирование свойств деталей сборочных единиц, отдельных деталей и фурнитуры допускается ТОЛЬКО через меню РПД ! В случае ручного редактирования свойств (меню Файл\Свойства…)возможны критические ошибки в их программной обработке,приводящие к неверному изготовлению деталей на производстве и ошибочной комплектации заказов фурнитурой.";
        public static void CheckAccessories()
        {
            string[] files = Directory.GetFiles(Furniture.Helpers.LocalAccounts.modelPathResult.Replace("_SWLIB_", "_SWLIB_BACKUP"), "*.SLDASM", SearchOption.AllDirectories);
            SwDmDocumentOpenError oe;
            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();

            SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
           
            ProgressBar.WaitTime.Instance.SetLabel("Проверяется cash...");
            ProgressBar.WaitTime.Instance.ShowWait();
            foreach (var file in files)
            {
                try
                {

                    SwDMDocument8 swDoc = (SwDMDocument8)swDocMgr.GetDocument(file,
                                                                 SwDmDocumentType.swDmDocumentAssembly,
                                                                 true, out oe);
                    string[] references = swDoc.GetAllExternalReferences(src);
                    foreach (string reference in references)
                    {
                        if (!File.Exists(reference))
                            Logging.Log.Instance.Debug("Файл: " + file + "ссылка: " + reference);
                    }
                    swDoc.CloseDoc();
                    //bool isAccessory = ( swDoc.GetCustomProperty("Accessories",out type) == "Yes");
                    //if (isAccessory)
                    //    ;//Logging.Log.Instance.Debug("Файл аксесуара: "+file);

                }
                catch (Exception)
                {
                }
            }
            ProgressBar.WaitTime.Instance.HideWait();

        }
        public static void ActualizaAllCash()
        {
            string[] notHiddenDirectories = GetNotHiddenDirectories(Furniture.Helpers.LocalAccounts.modelPathResult);
            string[] filesInCurrentDir = new string[0];

            SwDmDocumentOpenError oe = new SwDmDocumentOpenError();
            SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
            SwDMSearchOption src = swDocMgr.GetSearchOptionObject(); ;
            SwDmCustomInfoType type = new SwDmCustomInfoType();

            foreach (var notHiddenDirectory in notHiddenDirectories)
            {
                string rFileName = notHiddenDirectory.Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_", "_swlib_backup");
                filesInCurrentDir = Directory.GetFiles(rFileName, "*.SLDASM");
                foreach (var file in filesInCurrentDir)
                {
                    rFileName = file;
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(rFileName);
                    if (rFileName.ToLower().Contains("_swlib_backup") && (fileNameWithoutExtension.Last() == 'P' || fileNameWithoutExtension.Last() == 'p') && Path.GetExtension(rFileName) == ".SLDASM" && !rFileName.ToLower().Contains("вспомогательн"))
                    {

                        int rr;
                        if (int.TryParse(fileNameWithoutExtension.Substring(fileNameWithoutExtension.Length - 3, 2), out rr))
                        {
                            //if (rr != 1)
                            //{
                            //    string newFilePath = Path.Combine(Path.GetDirectoryName(rFileName),
                            //                                      Path.GetFileNameWithoutExtension(rFileName).Substring(
                            //                                          0,
                            //                                          Path.GetFileNameWithoutExtension(rFileName).Length - 4) +
                            //                                      "#01P.SLDASM"); // + Path.GetExtension(fileName));
                            //    if (File.Exists(newFilePath))
                            //        File.SetAttributes(newFilePath, FileAttributes.Normal);
                            //    File.SetAttributes(file, FileAttributes.Hidden);
                            //}

                            if ((File.GetAttributes(file) & FileAttributes.Hidden) == FileAttributes.Hidden)
                            {
                                DeleteIfNonAccessory(file, src, swDocMgr, oe, type);

                            }
                        }
                    }
                }

            }
        }
        private static void DeleteIfNonAccessory(string file, SwDMSearchOption src, SwDMApplication swDocMgr, SwDmDocumentOpenError oe, SwDmCustomInfoType type)
        {
            //удалить все связанные, кроме аксесуаров..
            if (!File.Exists(file))
                return;
            if (!file.ToLower().Contains("_swlib_backup") || Path.GetFileNameWithoutExtension(file).First() == '~')
                return;
            var swDoc = swDocMgr.GetDocument(file, SwDmDocumentType.swDmDocumentAssembly, true, out oe);
            if (swDoc == null)
            {
                Logging.Log.Instance.Debug("Не удалось сделать GetDocument в " + file);
                return;
            }
            bool isAccessory;
            try
            {
                string[] tmp = swDoc.GetCustomPropertyNames();
                if (tmp != null && tmp.Contains("Accessories"))
                    isAccessory = swDoc.GetCustomProperty("Accessories", out type) == "Yes";
                else
                    isAccessory = false;
            }
            catch (Exception)
            {
                Logging.Log.Instance.Debug("Ошибка при попытке обратится к св-ву Accessories. Деталь: " +
                                            swDoc.FullName);
                swDoc.CloseDoc();
                return;
            }
            if (isAccessory) // если это аксесуар, то ничего не делаем.
            {
                swDoc.CloseDoc();
                return;
            }
            if (Path.GetExtension(file) == ".SLDASM")
            {
                string[] extReferences = swDoc.GetAllExternalReferences(src);
                swDoc.CloseDoc();
                foreach (var extReference in extReferences)
                {
                    DeleteIfNonAccessory(extReference, src, swDocMgr, oe, type);
                }
            }
            else
                swDoc.CloseDoc();

            if (Path.GetExtension(file) == ".SLDASM" || Path.GetExtension(file) == ".SLDPRT")
                File.Delete(file);
        }
        public static void Create()
        {
            MessageBox.Show(warnMessage, "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            TimeSpan ts = new TimeSpan();
            var startTime = DateTime.Now;
            ProgressBar.WaitTime.Instance.SetLabel("Создается cash...");
            ProgressBar.WaitTime.Instance.ShowWait();

            CreateDirectoryWithBackup(Path.Combine(Path.GetPathRoot(Furniture.Helpers.LocalAccounts.modelPathResult), "_SWLIB_BACKUP"));
            //крепежную фурнитуру- просто скопипастить
            //Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(Path.Combine(Properties.Settings.Default.ModelPath, "Крепежная фурнитура"), "D:\\_SWLIB_BACKUP\\Крепежная фурнитура");
            //сделать карту папок как в SWLIB

            CreateDirectoryMap();
            //выявить все папки, которые не Hidden
            string[] notHiddenDirectories = GetNotHiddenDirectories(Furniture.Helpers.LocalAccounts.modelPathResult);

            //скопировать  "Фурнитура для каркасов" например эту D:\_SWLIB_\DAY SYSTEM\Изделия\ТВ_МОДУЛЬ\Фурнитура для каркасов и вообще другие фурнитуры..

            //FileSystem.CopyDirectory(Path.Combine(Properties.Settings.Default.ModelPath, @"DAY SYSTEM\Изделия\ТВ_МОДУЛЬ\Фурнитура для каркасов"), @"D:\_SWLIB_BACKUP\DAY SYSTEM\Изделия\ТВ_МОДУЛЬ\Фурнитура для каркасов");
            //notHiddenDirectories = new string[2] { @"D:\_SWLIB_\CLASSICS\Горизонтальные панели", @"D:\_SWLIB_\CLASSICS\Экспресс-шкафы" };// @"D:\_SWLIB_\DAY SYSTEM\Фасады\лифты", @"D:\_SWLIB_\ШКАФЫ-КУПЕ\Ящики\Ручки для FUTUR", @"D:\_SWLIB_\CLASSICS\Горизонтальные панели\" };
            //теперь возьмем к примеру D:\_SWLIB_\ШКАФЫ-КУПЕ\Каркасы ШКАФОВ-КУПЕ и попытаемся скопировать из нее хотябы по одной копии
            string[] filesInCurrentDir = new string[0];

            int i = 1;
            var swDocMgr = SwAddin.GetSwDmApp();
            var src = swDocMgr.GetSearchOptionObject();
            foreach (var notHiddenDirectory in notHiddenDirectories)
            {
                //Logging.Log.Instance.Debug("Создание кэш директории:" + notHiddenDirectory);
                filesInCurrentDir = Directory.GetFiles(notHiddenDirectory, "*.SLDASM");
                foreach (var file in filesInCurrentDir)
                {
                    try
                    {
                        CopyToCash(file, "01", false, src, swDocMgr);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    catch (Exception e)
                    {
                        Logging.Log.Instance.Debug("Ошибка при создании кэш: " + e.Message);
                    }
                }
                i++;
            }
            var finishTime = DateTime.Now;
            ts = finishTime - startTime;
            ProgressBar.WaitTime.Instance.HideWait();
            MessageBox.Show("Создание кэша закончено за: " + ts.ToString());
        }
        private static string[] GetNotHiddenDirectories(string sourceDirectory)
        {
            string[] directoriesNotToUse = new string[] { "Декоры", "Базы данных", "Чертежи", "Крепежная фурнитура" };
            string[] directories = Directory.GetDirectories(Furniture.Helpers.LocalAccounts.modelPathResult, "*", SearchOption.AllDirectories);
            List<string> result = new List<string>(directories.Length);
            foreach (var directory in directories)
            {
                if (FileSystem.GetDirectoryInfo(directory).Attributes == (FileAttributes.Hidden | FileAttributes.Directory)) //&& directory.Contains("Вспомог"))
                    continue;
                bool needToContinue = false;
                foreach (var notToUse in directoriesNotToUse)
                {
                    if (directory.ToUpper().Contains(notToUse.ToUpper()))
                    {
                        needToContinue = true;
                        break;
                    }
                }
                if (needToContinue)
                    continue;
                result.Add(directory);
            }
            return result.ToArray();
        }
        private static void CreateDirectoryMap()
        {
            string[] directories = Directory.GetDirectories(Furniture.Helpers.LocalAccounts.modelPathResult, "*", SearchOption.AllDirectories);
            string tmp;
            foreach (var directory in directories)
            {
                tmp = directory.Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_", "_swlib_backup");
                Directory.CreateDirectory(tmp);
                if ((File.GetAttributes(directory) & FileAttributes.Hidden) == FileAttributes.Hidden)
                    File.SetAttributes(tmp, FileAttributes.Hidden);
            }
        }
        public static void CopySldPrtOnUpdate(string sourcePath)
        {
            //тут надо заменить все файлы, отличающиеся только индексом на этот файл
            if (!sourcePath.Contains("_SWLIB_") && !sourcePath.Contains("_SWLIB_BACKUP") || !File.Exists(sourcePath)) //|| sourcePath.Contains(Path.Combine(Properties.Settings.Default.ModelPath, "Крепежная фурнитура")))
            {
                Logging.Log.Instance.Debug("НЕ КОПИРУЕМ В КЭШ! " + sourcePath);
                return;
            }
            string destPath = sourcePath.Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_", "_swlib_backup");
            if (!Directory.Exists(Path.GetDirectoryName(destPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            var files = Directory.GetFiles(Path.GetDirectoryName(destPath)).Where(f => f.ToUpper().Contains(Path.GetFileNameWithoutExtension(sourcePath).ToUpper()) && f.First() != '~');
            //if (files.Count() > 0)
            //{
            foreach (var file in files)
            {
                File.Copy(sourcePath, file, true);
            }
            //}
            //else
            //{
            //    //скопировать 1-й экземпляр.. ну ваще такого не может быть
            //    File.Copy(sourcePath,)
            //}
        }
        public static string CopyToCash(string sourcePath, string idCopyTo, bool isAccessory = false, SwDMSearchOption src = null, SwDMApplication swDocMgr = null, Dictionary<string, string> deleteDict = null)
        {
            if (!sourcePath.Contains("_SWLIB_") && !sourcePath.Contains("_SWLIB_BACKUP") || !File.Exists(sourcePath)) //|| sourcePath.Contains(Path.Combine(Properties.Settings.Default.ModelPath, "Крепежная фурнитура")))
            {
                Logging.Log.Instance.Debug("НЕ КОПИРУЕМ В КЭШ! " + sourcePath);
                return "01";
            }
            //Logging.Log.Instance.Debug("Копирование в кэш:" + sourcePath);
            if (string.IsNullOrEmpty(idCopyTo) || idCopyTo == "99")
            {
                //тут надо найти idCopyTo
                string destPath = sourcePath.Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_", "_swlib_backup");
                string destFolderPath = Path.GetDirectoryName(destPath);
                string sourceFolderPath = Path.GetDirectoryName(sourcePath);
                if (!Directory.Exists(destFolderPath))
                {
                    Directory.CreateDirectory(destFolderPath);
                    if (Directory.Exists(sourceFolderPath))
                    {
                        if ((File.GetAttributes(sourceFolderPath) & FileAttributes.Hidden) == FileAttributes.Hidden)
                            File.SetAttributes(destFolderPath, FileAttributes.Hidden);
                    }
                }
                var files = Directory.GetFiles(Path.GetDirectoryName(destPath)).Where(f => f.ToUpper().Contains(Path.GetFileNameWithoutExtension(sourcePath).ToUpper()) && f.First() != '~' && (Path.GetFileName(f).Length == Path.GetFileName(sourcePath).Length + 4 || Path.GetFileName(f).Length == Path.GetFileName(sourcePath).Length + 5));
                idCopyTo = files.Max(f => f.Substring(f.Length - 10, 2));
                int newNumber;
                if (int.TryParse(idCopyTo, out newNumber))
                {
                    newNumber = newNumber + 1;
                    if (newNumber >= 99)
                        idCopyTo = "01";
                    else
                    {
                        if (newNumber >= 10 && newNumber < 99)
                            idCopyTo = newNumber.ToString();
                        else
                            idCopyTo = "0" + newNumber.ToString();
                    }
                }
                else
                {
                    //throw new Exception("Ошибка при копировании в кэш. Обратитесь к разработчикам.");
                    idCopyTo = "01";
                }
            }
            File.SetAttributes(sourcePath, FileAttributes.Normal);
            if (swDocMgr == null)
                swDocMgr = SwAddin.GetSwDmApp();
            SwDmDocumentOpenError oe;
            if (src == null)
                src = swDocMgr.GetSearchOptionObject();

            var swDoc = swDocMgr.GetDocument(sourcePath, SwDmDocumentType.swDmDocumentAssembly, true, out oe);
            if (swDoc == null)
            {
                Logging.Log.Instance.Debug("Не удалось сделать GetDocument в " + sourcePath);
                return idCopyTo;
            }
            SwDmCustomInfoType type;
            if (!isAccessory)
            {
                try
                {
                    string[] tmp = swDoc.GetCustomPropertyNames();
                    if (tmp!=null && tmp.Contains("Accessories"))
                        isAccessory = swDoc.GetCustomProperty("Accessories", out type) == "Yes";
                    else
                        isAccessory = false;
                }
                catch (Exception)
                {
                    Logging.Log.Instance.Debug("Ошибка при попытке обратится к св-ву Accessories. Деталь: " + swDoc.FullName);
                }
            }

            string[] extReferences = swDoc.GetAllExternalReferences(src);

            File.SetAttributes(sourcePath, FileAttributes.ReadOnly);
            string newPath;
            if (!isAccessory)
            {
                newPath =Path.Combine(Path.GetDirectoryName(sourcePath).Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_","_swlib_backup"),Path.GetFileNameWithoutExtension(sourcePath) +"#"+ idCopyTo + "P" + Path.GetExtension(sourcePath));
                if (deleteDict != null && deleteDict.ContainsKey(sourcePath))
                    newPath = deleteDict[sourcePath];
            }
            else
            {
                if (!File.Exists(sourcePath) || sourcePath.ToUpper().Contains("_SWLIB_BACKUP"))
                    return idCopyTo;
                if (!File.Exists(sourcePath.ToUpper().Replace("_SWLIB_", "_SWLIB_BACKUP")))
                {
                    newPath = sourcePath.Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_", "_swlib_backup");
                    if (deleteDict != null && deleteDict.ContainsKey(sourcePath))
                        newPath = deleteDict[sourcePath];
                    if (!Directory.Exists(Path.GetDirectoryName(newPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                    File.Copy(sourcePath,newPath, false);
                    File.SetAttributes(newPath, FileAttributes.Normal);
                    if (extReferences != null)
                    {
                        foreach (string reference in extReferences)
                        {
                            CopyToCash(reference, idCopyTo, true, src, swDocMgr, deleteDict);
                        }
                    }
                }
                return idCopyTo; //newPath = sourcePath.Replace("_SWLIB_", "_SWLIB_BACKUP");
            }
            if (!File.Exists(newPath))
            {
                //создать путь если надо
                if (!Directory.Exists(Path.GetDirectoryName(newPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Copy(sourcePath, newPath);
            }
            string firstLvlId = idCopyTo;
            File.SetAttributes(newPath, FileAttributes.Normal);
            swDoc.CloseDoc();
            swDoc = swDocMgr.GetDocument(newPath, SwDmDocumentType.swDmDocumentAssembly, false, out oe);
            if (swDoc == null)
                return idCopyTo;
            string[] extReferences2 = swDoc.GetAllExternalReferences(src);
            if (extReferences2 == null)
                return idCopyTo;
            //тут какая то фигня что полученные референсы могут иметь не свсем правильные значения
            string[] tmpExtReference = new string[extReferences2.Length];
            bool isChenge = false;
            for (int j = 0; j < extReferences2.Length;j++ )
            {
                if (!File.Exists(extReferences2[j]))
                {
                    isChenge = true;
                    tmpExtReference[j] = extReferences[j];
                    continue;
                }

                string[] spl = extReferences2[j].Split('\\');
                if (spl.Length < 2)
                    continue;
                if (spl[1] != "_SWLIB_" && spl[1] != "_SWLIB_BACKUP" && spl[1].Contains("_SWLIB_")) //&& spl[1].Length>15)
                {
                    StringBuilder sb = new StringBuilder();
                    isChenge = true;
                    for (int i = 0; i < spl.Length; i++)
                    {
                        if (i != 1)
                            sb.Append(spl[i]);
                        else
                            sb.Append("_SWLIB_");
                        if (i < spl.Length-1)
                            sb.Append(Path.DirectorySeparatorChar);
                    }
                    tmpExtReference[j] = sb.ToString();
                }
            }
            if (isChenge)
            {
                for (int i = 0; i < tmpExtReference.Length; i++)
                {
                    if (tmpExtReference[i] != null)
                        swDoc.ReplaceReference(extReferences2[i], tmpExtReference[i]);
                    else
                        tmpExtReference[i] = extReferences2[i];
                }
                swDoc.Save();
                extReferences2 = swDoc.GetAllExternalReferences(src);
            }
            //конец обработки той фигни
            for (int i = 0; i < extReferences.Length; i++)
            {
                string extReference = extReferences[i];
                //проверить правильная ли ссылка
                if (Path.GetExtension(extReferences2[i]).ToLower() == ".sldasm" && Path.GetExtension(sourcePath).ToLower() == ".sldprt") //&& !extReferences2[i].ToUpper().Contains("_SWLIB_"))
                {
                    //Logging.Log.Instance.Debug("циклическая ссылка :" + extReferences2[i] + "в моделе: " + sourcePath);
                    //так быть не должно! от этого все проблемы!
                    continue;
                }
                if (!File.Exists(extReference))
                {
                    Logging.Log.Instance.Debug("Не правильная ссылка в " + sourcePath + " сама ссылка: " + extReference);
                    continue;
                }
                if (extReference.ToUpper().Contains("_SWLIB_BACKUP"))
                    continue;
                var swDocR = swDocMgr.GetDocument(extReference, SwDmDocumentType.swDmDocumentAssembly, true, out oe);
                if (swDocR == null)
                {
                    Logging.Log.Instance.Debug("Не удалось сделать GetDocument в " + sourcePath);
                    return idCopyTo;
                }
                //if (!isAccessory)
                //{
                    try
                    {
                        string[] tmp = swDocR.GetCustomPropertyNames();
                        if (tmp != null && tmp.Contains("Accessories"))
                            isAccessory = swDocR.GetCustomProperty("Accessories", out type) == "Yes";
                        else
                            isAccessory = false;
                    }
                    catch (Exception)
                    {
                        Logging.Log.Instance.Debug("Ошибка при попытке обратится к св-ву Accessories. Деталь: " + swDoc.FullName);
                    }
                //}
               
                if (isAccessory)
                {
                    if (!File.Exists(extReferences2[i]) || extReferences2[i].ToUpper().Contains("_SWLIB_BACKUP") && Path.GetExtension(extReferences2[i]).ToUpper() == ".SLDPRT")
                    {
                        continue;
                    }
                    //string newPath2;
                    //if (!extReferences2[i].Contains("_SWLIB_BACKUP"))
                    //    newPath2 = extReferences2[i].Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_", "_swlib_backup");
                    //else
                    //    newPath2 = extReferences2[i];
                    //if (!File.Exists(newPath2))//(!File.Exists(extReferences2[i].ToUpper().Replace("_SWLIB_", "_SWLIB_BACKUP")))
                    //{
                    //    if (deleteDict != null && deleteDict.ContainsKey(sourcePath))
                    //        newPath2 = deleteDict[sourcePath];
                    //    if (!Directory.Exists(Path.GetDirectoryName(newPath2)))
                    //        Directory.CreateDirectory(Path.GetDirectoryName(newPath2));
                    //    File.Copy(extReferences2[i], newPath2, false);
                    //    File.SetAttributes(newPath2, FileAttributes.Normal);
                    //}
                    //swDoc.ReplaceReference(extReferences2[i], newPath2);
                    //if (Path.GetExtension(extReferences2[i]) == ".SLDASM")
                    //{
                    //    var swDoc2 = swDocMgr.GetDocument(newPath2, SwDmDocumentType.swDmDocumentAssembly, false, out oe);
                    //    if (swDoc2 == null)
                    //        continue;
                    //    string[] extReferences3 = swDoc2.GetAllExternalReferences(src);
                    //    if (extReferences3 == null)
                    //        return idCopyTo;
                    //    foreach (var reference in extReferences3)
                    //    {
                    //        if (!File.Exists(reference) || reference.ToUpper().Contains("_SWLIB_BACKUP"))
                    //            continue;
                    //        string ttt = reference.Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_","_swlib_backup");
                    //        if (!Directory.Exists(Path.GetDirectoryName(ttt)))
                    //            Directory.CreateDirectory(Path.GetDirectoryName(ttt));
                    //        if (!File.Exists(ttt))
                    //            File.Copy(reference,ttt , false);
                    //        File.SetAttributes(ttt, FileAttributes.Normal);
                    //        swDoc2.ReplaceReference(reference, ttt);
                    //    }
                    //    swDoc2.Save();
                    //}
                    //continue;
                }
                //if (Path.GetExtension(extReference).ToUpper() == ".SLDASM")
                //{
                    idCopyTo = CopyToCash(extReference, null, isAccessory, src, swDocMgr);
                //}

                string destPath = extReference.Replace("_SWLIB_", "_SWLIB_BACKUP").Replace("_swlib_", "_swlib_backup");


                if (!isAccessory)
                {
                    //CheckHidden(extReference, sourcePath);
                    destPath = Path.Combine(Path.GetDirectoryName(destPath),Path.GetFileNameWithoutExtension(destPath) +"#"+ idCopyTo + "P" +Path.GetExtension(destPath));
                    if (deleteDict != null && deleteDict.ContainsKey(destPath))
                        destPath = deleteDict[sourcePath];
                }

                if (!File.Exists(destPath))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(destPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    File.Copy(extReference, destPath);
                    File.SetAttributes(destPath, FileAttributes.Normal);
                    File.SetAttributes(destPath, FileAttributes.Hidden);
                }
                swDoc.ReplaceReference(extReferences2[i], destPath);
                //Logging.Log.Instance.Debug("ReplaceReference " + swDoc.FullName + "from " + extReferences2[i] + "to " + destPath);
                swDocR.CloseDoc();
            }
            //переписать свойства!
            try
            {
                string[] tmp = swDoc.GetCustomPropertyNames();
                string[] relatedFiles=null;
                if (tmp != null)
                {
                    foreach (string name in tmp)
                    {
                        SwDmCustomInfoType swDmCstInfoType;
                        string valueOfName = swDoc.GetCustomProperty(name, out swDmCstInfoType);
                        string lowValOfName = valueOfName.ToLower();

                        if (lowValOfName.Contains("@") && !lowValOfName.Contains("#") && (lowValOfName.Contains(".sld")))
                        {
                            var search = valueOfName.Split('.').First().Split('@').Last();
                            if (relatedFiles == null)
                                relatedFiles = swDoc.GetAllExternalReferences(src);
                            foreach (var file in relatedFiles)
                            {
                                if (Path.GetFileNameWithoutExtension(file).Contains(search))
                                {
                                    string newValue = valueOfName.Replace(search, Path.GetFileNameWithoutExtension(file));
                                    swDoc.SetCustomProperty(name, newValue);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Logging.Log.Instance.Debug("Ошибка при попытке обратится к св-ву. Деталь: " + swDoc.FullName);
            }
             

            swDoc.Save();
            //Logging.Log.Instance.Debug("SAVE "+swDoc.FullName);
            swDoc.CloseDoc();
            return firstLvlId;//idCopyTo;
        }
        private static void CheckHidden(string extReference, string sourcePath)
        {
            if (!File.Exists(sourcePath) || (File.GetAttributes(sourcePath) & FileAttributes.Hidden) == FileAttributes.Hidden)
                return;
            if (!File.Exists(extReference) || (File.GetAttributes(extReference) & FileAttributes.Hidden) == FileAttributes.Hidden)
                return;
            string currDir = Path.GetDirectoryName(sourcePath);
            DirectoryInfo di;
            while (currDir != Furniture.Helpers.LocalAccounts.modelPathResult && currDir != Path.GetDirectoryName(Furniture.Helpers.LocalAccounts.modelPathResult))
            {
                di = new DirectoryInfo(currDir);
                if (di.Attributes.HasFlag(FileAttributes.Hidden))
                    return;
                currDir = Path.GetDirectoryName(currDir);
            }
            currDir = Path.GetDirectoryName(extReference);
            while (currDir != Furniture.Helpers.LocalAccounts.modelPathResult && currDir != Path.GetDirectoryName(Furniture.Helpers.LocalAccounts.modelPathResult))
            {
                di = new DirectoryInfo(currDir);
                if (di.Attributes.HasFlag(FileAttributes.Hidden))
                    return;
                currDir = Path.GetDirectoryName(currDir);
            }
            Logging.Log.Instance.Debug("Список!!!!!!! : " + sourcePath + " " + extReference);

        }
        private static void CreateDirectoryWithBackup(string workFolder)
        {
            if (Directory.Exists(workFolder))
            {
                //архивируем все содержимое, сохраняем, потом удаляем 
                //string fileZipTo = "c:\\Temp\\swlib_backup" + DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString() + ".zip";
                //using (ZipFile zip = new ZipFile(Encoding.GetEncoding(866)))
                //{
                //    var ziopEntry = zip.AddDirectory(workFolder);
                //    zip.Save(fileZipTo);
                //}
                //сначала снимаем все права readonly
                var dirInfo = FileSystem.GetDirectoryInfo(workFolder);
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                }
                //Удаляем
                FileSystem.DeleteDirectory(workFolder, DeleteDirectoryOption.DeleteAllContents);
                //создаем
                dirInfo = Directory.CreateDirectory(workFolder);
                dirInfo.Attributes = FileAttributes.Normal;
                //копируем забэкапленый файл
                //File.Move(fileZipTo, workFolder + "\\swlib_backup" + DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString() + ".zip");
            }
            else
            {
                //создаем эту папку
                Directory.CreateDirectory(workFolder);

            }
        }
        internal static void ChangeSimilarFromCash(string sourceFilePath)
        {
            string tmpFile = sourceFilePath.ToUpper().Replace("_SWLIB_", "_SWLIB_BACKUP");
            string directoryToSearch = Path.GetDirectoryName(tmpFile);
            string[] filesToDelete = Directory.GetFiles(directoryToSearch);//, Path.GetFileNameWithoutExtension(tmpFile));
            foreach (var fileToChange in filesToDelete)
            {
                if (File.Exists(fileToChange) && fileToChange.ToUpper().Contains(Path.GetFileNameWithoutExtension(tmpFile).ToUpper()))
                {
                    File.Move(sourceFilePath, fileToChange);
                    //SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
                    //SwDmDocumentOpenError oe;
                    //if (Path.GetExtension(fileToChange).ToLower() == ".sldasm")
                    //{
                    //    var swDoc = swDocMgr.GetDocument(fileToChange, SwDmDocumentType.swDmDocumentAssembly, true,out oe);

                    //}
                    Logging.Log.Instance.Debug(string.Format("Файл {0} заменен в  кэше!", fileToChange));
                }
            }
        }
        internal static void DeleteSimilarFromCashOrig(string sourceFilePath, ref Dictionary<string, string> deleteDict)
        {
            string tmpFile = sourceFilePath.ToUpper().Replace("_SWLIB_", "_SWLIB_BACKUP");
            string directoryToSearch = Path.GetDirectoryName(tmpFile);
            if (!Directory.Exists(directoryToSearch))
                return;
            string[] filesToDelete = Directory.GetFiles(directoryToSearch);//, Path.GetFileNameWithoutExtension(tmpFile));
            foreach (var fileToDelete in filesToDelete)
            {
                if (File.Exists(fileToDelete) && fileToDelete.ToUpper().Contains(Path.GetFileNameWithoutExtension(tmpFile).ToUpper()) && Path.GetExtension(fileToDelete).ToUpper() == Path.GetExtension(tmpFile).ToUpper())
                {
                    File.Delete(fileToDelete);
                    string fname = Path.GetFileNameWithoutExtension(fileToDelete);
                    if (fname.Length > 4)
                    {
                        string suf = fname.Substring(fname.Length - 3, 3);
                        if ((suf.ToUpper() == "01P" || suf[2] != 'P') && !deleteDict.ContainsKey(sourceFilePath))
                            deleteDict.Add(sourceFilePath, fileToDelete);
                    }
                    Logging.Log.Instance.Debug(string.Format("Файл {0} удален из кэша!", fileToDelete));
                }
            }
        }
    }
}
