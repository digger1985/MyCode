using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Text;
using Ionic.Zip;
using Ionic.Zlib;

namespace Furniture
{
    public partial class UpdatingLib : Form
    {
        private readonly List<string> _logErr = new List<string>();
        private DirectoryInfo _tempDir;
        private const string ErrStr = "Ошибка обновления библиотеки!";
        private DirectoryInfo _tempForLastFiles;
        private static DirectoryInfo _mainDir;
        private bool _wasUpdate;
        private bool _isError;
        private string _attr;
        private bool _justClose=false;
        private readonly Dictionary<string, string> _dict = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _dictReverse = new Dictionary<string, string>();
        private static bool _forcedUpdateLib;
        private List<string> _cashFilesToDelete;
        private List<string> _cashFilesToCopy;

        public UpdatingLib(string attr)
        {
            InitializeComponent();
            _attr = attr;
            Closing += Form1_Closing;
            Show();
        }

        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_justClose)
                return;
            if (_isError)
            {
                progressBar1.Maximum = _dictReverse.Count;
                progressBar1.Value = progressBar1.Maximum;

                foreach (var dReverse in _dictReverse)
                {
                    if (File.Exists(dReverse.Key))
                        File.Delete(dReverse.Key);
                    File.Copy(dReverse.Value, dReverse.Key);
                    --progressBar1.Value;
                }
            }

            if (_tempForLastFiles != null && Directory.Exists(_tempForLastFiles.FullName))
                _tempForLastFiles.Delete(true);
            if (_tempDir != null && Directory.Exists(_tempDir.FullName))
            {
                try
                {
                    _tempDir.Delete(true);
                }
                catch(Exception)
                {
                    
                }
            }

            _mainDir.Attributes = FileAttributes.Normal;
            if (Furniture.Helpers.LocalAccounts.modelPathResult == @"D:\_SWLIB_")
            {                
                Properties.Settings.Default.ModelPath = @"D:\_SWLIB_\";
                Properties.Settings.Default.Save();

                Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("ModelPath", Properties.Settings.Default.ModelPath);
            }
            if (Furniture.Helpers.LocalAccounts.modelPathResult == @"C:\_SWLIB_")
            {
                Properties.Settings.Default.ModelPath = @"C:\_SWLIB_\";
                Properties.Settings.Default.Save();

                Furniture.Helpers.SaveLoadSettings.AddOrUpdateAppSettings("ModelPath", Properties.Settings.Default.ModelPath);
            }
            string fl = Furniture.Helpers.LocalAccounts.modelPathResult + "LogUpd.txt";
            if (File.Exists(fl))
                new FileInfo(fl).Attributes = FileAttributes.Normal;
            if (_logErr.Count > 0)
                File.WriteAllLines(fl, _logErr);//Logging.Log.Instance.Info(_logErr);

            foreach (var file in _mainDir.GetFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.ReadOnly;
            }
            
            _mainDir.Attributes = FileAttributes.ReadOnly;

            Dispose();

            if (_wasUpdate)
                if(MessageBox.Show(@"Библиотека обновлена!Текущая версия от " + Properties.Settings.Default.PatchVersion + Environment.NewLine + @"Открыть лог обновления?",
                                    @"MrDoors", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                    Process.Start(Logging.Log.Instance.FilePath);
        }

        public void Updating(bool forced = false)
        {
            try
            {
                string[] externals= linksToCash.CheckForExternal();
                if (externals.Length>0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Environment.NewLine);
                    foreach (var external in externals)
                    {
                        sb.Append(external);
                        sb.Append(Environment.NewLine);
                    }
                    StringBuilder sb2 = new StringBuilder();
                    sb2.Append("В следующих сборках есть неоторванные детали: ");
                    sb2.Append(sb.ToString());
                    sb2.Append(Environment.NewLine);
                    sb2.Append(@"Сделайте в этих заказах опцию ""Оторвать все"", иначе обновление может испортить элементы заказа.");
                    sb2.Append(Environment.NewLine);
                    sb2.Append("Если эти заказы вам не нужны, можете просто удалить их.");
                    sb2.Append(Environment.NewLine);
                    sb2.Append("Прервать обновление?");
                    if (MessageBox.Show( sb2.ToString()
                    , @"MrDoors", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _justClose = true;
                        Close();
                        return;
                    }
                }
                if (Properties.Settings.Default.FtpPath == "")
                {
                    DefineNeededNasAddress();
                }

                _isError = true;
                _mainDir = Directory.CreateDirectory(Furniture.Helpers.LocalAccounts.modelPathResult);
                // поскольку нерадивые админы стерли на ftp все до 1 августа 2012 то сделать проверку что если библиотека раньше этого то делать 
                
                #region if < Date(1 августа), то принудительное обновление
                if (_attr != "L")
                {
                    DateTime oldLibVersion;
                    if (DateTime.TryParse(Properties.Settings.Default.PatchVersion, out oldLibVersion))
                    {
                        if (oldLibVersion < new DateTime(2012, 8, 2))
                            _attr = "L";
                    }
                }

                #endregion
                if (_attr == "L")
                {
                    _forcedUpdateLib = forced;
                    if (!WorkWithLibrary())
                    {
                        _wasUpdate = false;
                        _isError = false;
                        Close();
                        return;
                    }
                }
                else
                {
                    long lSize;
                    string library;
                    bool isCritical;
                    var newPatches = DefineSizeOfFiles(out lSize, out library,out isCritical);
                    if (newPatches == null)
                    {
                        _wasUpdate = false;
                        return;
                    }
                    var pSize = newPatches.Select(x => x.Value).Aggregate<long, long>(0, (current, l) => current + l);
                    if (lSize != 0)
                    {
                        if (lSize < pSize)
                        {
                            _wasUpdate = WorkWithLibrary();
                            _isError = false;
                            Close();
                            return;
                        }
                    }
                    WorkWithPatch(newPatches);
                }
                _isError = false;
                _wasUpdate = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    @"Ошибка обновления библиотеки! Не удалось загрузить последнюю версию библиотеки, текущая версия " +
                    Properties.Settings.Default.PatchVersion +" Текст ошибки:"+ e.Message, @"MrDoors", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.Log.Instance.Fatal(e, @"Ошибка обновления библиотеки! Не удалось загрузить последнюю версию библиотеки, текущая версия " +Properties.Settings.Default.PatchVersion);
                foreach(var file in Directory.GetFiles(_mainDir.Root + "MrDoors_Solid_Update\\"))
                {
                    if(file.Contains(".zip") || file.Contains(".del"))
                    {
                        File.Delete(file);
                    }
                }
            }
            Close();
        }

        private static void DefineNeededNasAddress()
        {
            var listIp = new List<string>();
            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
            foreach (IPInterfaceProperties ipip in nis.Select(ni => ni.GetIPProperties()))
            {
                listIp.AddRange(ipip.UnicastAddresses.Select(networkInterface => networkInterface.Address.ToString()));
            }
            var dict = new Dictionary<string, string>();
            //var reqFtp = (FtpWebRequest)WebRequest.Create("ftp://194.84.146.5/FtpPath.txt");
            var reqFtp = (FtpWebRequest)WebRequest.Create(Furniture.Helpers.FtpAccess.resultFtp +"FtpPath.txt");
            reqFtp.Credentials = new NetworkCredential("solidk", "KSolid");
            reqFtp.Method = WebRequestMethods.Ftp.DownloadFile;
            var response = reqFtp.GetResponse();
            var stream = response.GetResponseStream();
            var reader = new StreamReader(stream);
            bool err = true;
            do
            {
                try
                {
                    string rd = reader.ReadLine();
                    var r = rd.Split(' ');
                    dict.Add(r.First(), r.Last());
                }
                catch
                {
                    err = false;
                }
            } while (err);

            stream.Close();
            reqFtp.Abort();
            string neededpath = "";

            foreach (var ip in listIp)
            {
                foreach (var key in dict.Keys)
                {
                    if (ip == key)
                        neededpath = dict[key];
                }
            }
            if (neededpath == "" && dict.ContainsKey("other"))
                neededpath = dict["other"];
            Properties.Settings.Default.FtpPath = neededpath;
            Properties.Settings.Default.Save();

        }   
        private static void setAttributesNormal(DirectoryInfo dir)
        {
            foreach (DirectoryInfo subDirPath in dir.GetDirectories())
            {
                subDirPath.Attributes = FileAttributes.Normal;
                if ((subDirPath.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    subDirPath.Attributes = subDirPath.Attributes | FileAttributes.Hidden;
                setAttributesNormal(subDirPath);

            }
            foreach (FileInfo filePath in dir.GetFiles())
            {
                var file = filePath;
                file.Attributes = FileAttributes.Normal;
            }
        }
        private bool WorkWithLibrary()
        {
            var newLib = DownloadLibrary();
            if (newLib == null) return false;

            _logErr.Add("Загрузка библиотеки от " + GetDate(newLib.Name));

            _tempDir = Directory.CreateDirectory(GetNewTempDirName());
            _tempForLastFiles = Directory.CreateDirectory(GetNewTempDirName());
            UnZipPatch(newLib.FullName);
            
            var mainFiles = _mainDir.GetFiles("*", SearchOption.AllDirectories).Where(x => x.IsReadOnly = true);
            foreach (var file in mainFiles)
            {
                file.IsReadOnly = false;
            }
            _mainDir.Attributes = FileAttributes.Normal;
            setAttributesNormal(_mainDir);
            if (_mainDir.Exists)
                _mainDir.Delete(true);

            _logErr.Add("Удаление старой библиотеки от " + Properties.Settings.Default.PatchVersion);

            label1.Text = @"Копирование";
            Application.DoEvents();

            var tempFiles = _tempDir.GetFiles("*", SearchOption.AllDirectories);
            foreach (var tempFile in tempFiles.Where(x => x.IsReadOnly = true))
            {
                tempFile.IsReadOnly = false;
            }
            _mainDir.Create();
            progressBar1.Value = 1;
            progressBar1.Maximum = tempFiles.Count();
            foreach (var file in tempFiles)
            {
                label2.Text = file.Name;
                label3.Text = @"в " + _mainDir.FullName;
                Application.DoEvents();
                string newFileName = _mainDir.FullName + file.FullName.Substring(_tempDir.FullName.Length);

                var listDirs = MakeDirInPath(newFileName);
                foreach (var directoryInfo in GetAllDirectoriesFromPath(file.FullName))
                {
                    foreach (var listDir in listDirs)
                    {
                        if (listDir.Name == directoryInfo.Name)
                            listDir.Attributes = directoryInfo.Attributes;
                    }
                }
                if (file.Name.First() != '~' && !file.Name.Contains("LogUpd.txt"))
                {
                    File.Copy(file.FullName, newFileName);
                    _logErr.Add("Запись файла " + newFileName);
                }

                progressBar1.Increment(1);
            }
            label1.Text = @"Удаление файлов с ""~""";
            foreach (var tildaFile in _mainDir.GetFiles("~*.*", SearchOption.AllDirectories))
            {
                tildaFile.Attributes = FileAttributes.Normal;
                _logErr.Add("Удаление файла с тильдой: " + tildaFile.FullName);
                tildaFile.Delete();
            }
            label1.Text = @"Удаление пустых директорий";
            processDirectory(_mainDir.FullName,_logErr);
            if (newLib.Exists)
            {
                newLib.Delete();
                _logErr.Add("Удаление пустой директории: " + newLib.FullName);
            }
            Properties.Settings.Default.PatchVersion = GetDate(newLib.Name).ToString();
            Properties.Settings.Default.Save();
            return true;
        }
        private static void processDirectory(string startLocation,List<string> _logErr,bool useLogErrAndDelete=true )
        {
            try
            {

            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                processDirectory(directory, _logErr, useLogErrAndDelete);
                if (Directory.GetFiles(directory,"*.*",SearchOption.AllDirectories).Length == 0)//  && Directory.GetDirectories(directory).Length == 0)
                {
                    File.SetAttributes(directory,FileAttributes.Normal);
                    if (useLogErrAndDelete)
                    {
                        Directory.Delete(directory, true);
                        _logErr.Add("Удаление директории:" + directory);
                    }
                }
            }

            }
            catch (Exception)
            {
            }
        }

        private void UpdateCash()
        {
            progressBar1.Minimum = 1;
            progressBar1.Value = 1;
            progressBar1.Maximum = _cashFilesToDelete.Count + _cashFilesToCopy.Count;
            Dictionary<string,string> deleteDict = new Dictionary<string, string>();
            foreach (var delFile in _cashFilesToDelete)
            {
                string ext = Path.GetExtension(delFile).ToLower();
                if (ext == ".sldasm")
                {
                    Cash.DeleteSimilarFromCashOrig(delFile, ref deleteDict);
                }
                progressBar1.Increment(1);
            }
            var swDocMgr = SwAddin.GetSwDmApp();
            var src = swDocMgr.GetSearchOptionObject();
            foreach (var copyFile in _cashFilesToCopy)
            {
                //Cash.QuickCopyToCash(copyFile,deleteDict, src, swDocMgr);
                try
                {
                    string ext = Path.GetExtension(copyFile).ToLower();
                    if (ext == ".sldprt")
                    {
                        Cash.CopySldPrtOnUpdate(copyFile);
                    }
                    else if (ext == ".sldasm")
                    {
                        Cash.CopyToCash(copyFile, "01", false, src, swDocMgr, deleteDict);
                    }
                    else
                        continue;
                }
                catch (Exception e)
                {
                    Logging.Log.Instance.Debug("Ошибка при обновлении кэш: " + e.Message);
                }
                finally
                {
                    progressBar1.Increment(1);
                }
            }
        }
        private void WorkWithPatch(Dictionary<string, long> newPatches)
        {
            var sortedDateList = DownloadPatches(newPatches);
            sortedDateList.Sort((x, y) => (GetDate(x.Name)).CompareTo(GetDate(y.Name)));
            _tempDir = Directory.CreateDirectory(GetNewTempDirName());
            _tempForLastFiles = Directory.CreateDirectory(GetNewTempDirName());
            _cashFilesToDelete = new List<string>();
            _cashFilesToCopy = new List<string>();
            foreach (var newPatche in sortedDateList.Where(x => x.Extension == ".del"))
            {
                FileStream delStream = newPatche.OpenRead();
                var sr = new StreamReader(delStream, Encoding.GetEncoding(1251));
                string line;
                do
                {
                    try
                    {
                        line = sr.ReadLine();
                        if (line.First() == Convert.ToChar("\\"))
                        {
                            line = line.Substring(1);
                        }
                        string delPathFile = Path.Combine(_mainDir.FullName, line);
                        if (File.Exists(delPathFile))
                        {
                            var file = new FileInfo(delPathFile) { IsReadOnly = false };
                            _logErr.Add("Удаление файла " + delPathFile);
                            file.Delete();
                        }
                        if (Path.GetExtension(line).ToLower() == ".sldasm" || Path.GetExtension(line).ToLower() == "sldasm" || Path.GetExtension(line).ToLower() == ".sldprt" || Path.GetExtension(line).ToLower() == "sldprt")
                            _cashFilesToDelete.Add(delPathFile);//Cash.DeleteSimilarFromCashOrig(delPathFile);
                    }
                    catch { line = ""; }

                } while (line != "");
                sr.Close();
                delStream.Close();
                if (newPatche.Exists)
                    newPatche.Delete();
                Properties.Settings.Default.PatchVersion = GetDate(newPatche.Name).ToString();
                Properties.Settings.Default.Save();
            }

            foreach (var newPatche in sortedDateList.Where(x => x.Extension == ".zip"))
            {
                _logErr.Add("Загрузка патча от " + GetDate(newPatche.Name));
                UnZipPatch(newPatche.FullName);
                CopyTempToMainDir();
                RewriteAllFilesWhichNotAccessable();
                if (Directory.Exists(Furniture.Helpers.LocalAccounts.modelPathResult.Replace("_SWLIB_", "_SWLIB_BACKUP")))
                {
                    try
                    {
                        UpdateCash();
                    }
                    catch (Exception e)
                    {
                        string errText = "Произошла ошибка при обновление КЭШ. Основная библиотека обновлена корректно. Для корректного использование библиотеки в режиме кэш, пересоздайте его. Текст ошибки:" + e.Message;
                        Logging.Log.Instance.Debug(errText);
                        MessageBox.Show(errText);
                    }
                }
                _cashFilesToDelete.Clear();
                _cashFilesToCopy.Clear();
                if (newPatche.Exists)
                    newPatche.Delete();
                if (_tempDir.Exists)
                {
                    try
                    {
                        processDirectory(_tempDir.FullName,null,false);
                        File.SetAttributes(_tempDir.FullName, FileAttributes.Normal);
                        Directory.Delete(_tempDir.FullName, true);
                        //_tempDir.Delete(true);
                    }
                    catch(Exception)
                    {}
                }
                if (_tempForLastFiles.Exists)
                {
                    try
                    {
                        processDirectory(_tempForLastFiles.FullName, null, false);
                        File.SetAttributes(_tempForLastFiles.FullName, FileAttributes.Normal);
                        Directory.Delete(_tempForLastFiles.FullName, true);
                        //_tempForLastFiles.Delete(true);
                    }
                    catch (Exception)
                    {}
                    
                }
                Properties.Settings.Default.PatchVersion = GetDate(newPatche.Name).ToString();
                Properties.Settings.Default.Save();

            }

            label1.Text =@"Удаление файлов с ""~""";
            foreach (var tildaFile in _mainDir.GetFiles("~*.*",SearchOption.AllDirectories))
            {
                tildaFile.Attributes = FileAttributes.Normal;
                _logErr.Add("Удаление файла с тильдой: " + tildaFile.FullName);
                tildaFile.Delete();
            }
            label1.Text = @"Удаление пустых директорий";
            processDirectory(_mainDir.FullName,_logErr);
        }


        private List<FileInfo> DownloadPatches(Dictionary<string, long> listPatch)
        {
            var list = new List<FileInfo>();
            var wc = new WebClient
            {
                Credentials =
                    new NetworkCredential(Properties.Settings.Default.NameFtpUserForLibUpdate,
                                          Properties.Settings.Default.SecureFtpUser)
            };
            foreach (var patch in listPatch)
            {
                progressBar1.Minimum = 1;
                progressBar1.Value = 1;
                progressBar1.Maximum = 310000;

                string name = _mainDir.Root + "MrDoors_Solid_Update\\" + patch.Key;

                label2.Text = @"Загрузка патча от " + GetDate(patch.Key);
                Application.DoEvents();

                Downloading(wc, patch.Key, patch.Value);
                list.Add(new FileInfo(name));
            }
            wc.Dispose();
            return list;
        }

        private FileInfo DownloadLibrary()
        {
            long sz;
            string neededLibrary;
            bool isCritical;
            if (DefineSizeOfFiles(out sz, out neededLibrary, out isCritical).Count == 0 && sz == 0)
                return null;

            label2.Text = @"Загрузка библиотеки от " + GetDate(neededLibrary);
            Application.DoEvents();

            var wc = new WebClient { Credentials = new NetworkCredential(Properties.Settings.Default.NameFtpUserForLibUpdate, Properties.Settings.Default.SecureFtpUser) };
            Downloading(wc, neededLibrary, sz);
            wc.Dispose();

            return new FileInfo(_mainDir.Root + "MrDoors_Solid_Update\\" + neededLibrary);
        }

        private void Downloading(WebClient wc, string neededLibrary, long sz)
        {
            if (!Directory.Exists(_mainDir.Root + "MrDoors_Solid_Update"))
                Directory.CreateDirectory(_mainDir.Root + "MrDoors_Solid_Update");
            string fullDownloadingPath = _mainDir.Root + "MrDoors_Solid_Update\\" + neededLibrary;
            if (File.Exists(fullDownloadingPath))
            {
                if (new FileInfo(fullDownloadingPath).Length == sz)
                    return;
            }

            var fileStream = new FileInfo(fullDownloadingPath).Create();

            label3.Visible = true;
            DateTime dt1 = DateTime.Now;
            var str = wc.OpenRead(Properties.Settings.Default.FtpPath + "/" + neededLibrary);
            

            const int bufferSize = 1024;
            var z = (int)(sz / bufferSize);
            var buffer = new byte[bufferSize];
            int readCount = str.Read(buffer, 0, bufferSize);
            progressBar1.Minimum = 1;
            progressBar1.Value = 1;
            progressBar1.Maximum = z;
            int i = 0;
            int countBytePerSec = 0;
            long commonByte = 0;

            while (readCount > 0)
            {
                DateTime dt2 = DateTime.Now;
                fileStream.Write(buffer, 0, readCount);
                commonByte += readCount;
                countBytePerSec += readCount;
                readCount = str.Read(buffer, 0, bufferSize);
                var dt = dt2 - dt1;
                if (dt.TotalSeconds > i)
                {
                    long lasttime = ((sz - commonByte) / countBytePerSec) * 10000000;
                    var f = new TimeSpan(lasttime);
                    string time;
                    if (f.Hours > 0)
                    {
                        if (f.Hours == 1)
                        {
                            time = f.Hours + " час " + f.Minutes + " минуты " + f.Seconds + " секунд";
                        }
                        else
                        {
                            if (f.Hours > 1 && f.Hours < 5)
                                time = f.Hours + " часа " + f.Minutes + " минуты " + f.Seconds + " секунд";
                            else
                                time = f.Hours + " часов " + f.Minutes + " минуты " + f.Seconds + " секунд";
                        }
                    }
                    else
                        if (f.Minutes > 0)
                            time = f.Minutes + " минут " + f.Seconds + " секунд";
                        else
                            time = f.Seconds + " секунд";
                    countBytePerSec = countBytePerSec / 1024;
                    if (((countBytePerSec) / 1024) > 1)
                    {
                        countBytePerSec = countBytePerSec / 1024;
                        label3.Text = dt.Minutes + @":" + dt.Seconds + @" секунд   Скорость: " + countBytePerSec +
                                      @" Мб/с   Осталось приблизительно: " + time;
                    }
                    else
                        label3.Text = dt.Minutes + @":" + dt.Seconds + @" секунд   Скорость: " + countBytePerSec + @" Кб/с   Осталось приблизительно: " + time;
                    Application.DoEvents();
                    countBytePerSec = 0;
                    i++;
                }
                progressBar1.Increment(1);
            }
            str.Close();
            fileStream.Close();
        }

        internal static Dictionary<string, long> DefineSizeOfFiles(out long libSize, out string libName,out bool isCritical)
        {
            libSize = 0;
            libName = "";
            isCritical = false;
            var list = new List<string>();
            var dict = new Dictionary<string, long>();
            if (Properties.Settings.Default.FtpPath == "")
                DefineNeededNasAddress();
            if (Properties.Settings.Default.FtpPath == "")
            {
                MessageBox.Show(ErrStr + Environment.NewLine + @"Невозможно определить ftp путь, обратитесь к администратору", @"MrDoors",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return dict;
            }

           var reqFtp = WebRequest.Create(Properties.Settings.Default.FtpPath);
            

            reqFtp.Credentials = new NetworkCredential(Properties.Settings.Default.NameFtpUserForLibUpdate, Properties.Settings.Default.SecureFtpUser);
            reqFtp.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            WebResponse response;
            try
            {
                response = reqFtp.GetResponse();
            }
            catch (WebException e)
            {
                MessageBox.Show(ErrStr + Environment.NewLine + e.Message);
                reqFtp.Abort();
                Logging.Log.Instance.Fatal(e, @"Ошибка обновления библиотеки! Не удалось загрузить последнюю версию библиотеки, текущая версия " + Properties.Settings.Default.PatchVersion);
                return dict;
            }
            var stream = response.GetResponseStream();
            var reader = new StreamReader(stream);
            bool isHtmlFormat = false;
            string g;
            bool err = true;
            do
            {
                try
                {
                    g = reader.ReadLine();
                    if (g == "<HTML>")
                    {
                        isHtmlFormat = true;
                        break;
                    }
                    var f = g.Substring(30).TrimStart();
                    var size = f.Split(' ').First();
                    var r = f.Split().Last();
                    if (r.ToLower().Contains("sw") && (r.ToLower().Contains(".del") || r.ToLower().Contains(".zip") || r.ToLower().Contains(".crt")))
                    {
                        DateTime dt;
                        DateTime.TryParse(Properties.Settings.Default.PatchVersion, out dt);
                        if (GetDate(r) > dt || _forcedUpdateLib)
                        {
                            if (r.ToLower().Contains(".crt") && !isCritical)
                            {
                                isCritical = true;
                            }
                            if (r.ToLower().Contains("swl"))
                                list.Add(r);
                            var s = Convert.ToInt64(size);
                            dict.Add(r, s);
                        }
                    }
                }
                catch { err = false; }
            } while (err);

            if (isHtmlFormat)
            {
                err = true;
                do
                {
                    try
                    {
                        g = reader.ReadLine();
                        if (g.ToLower().Contains("sw") && (g.ToLower().Contains(".del") || g.ToLower().Contains(".zip")))
                        {
                            var r = g.Substring(g.ToLower().IndexOf("sw"), 17);
                            var proSize = g.Substring(20, g.ToUpper().IndexOf("<A") - 20).Trim();
                            var arr = proSize.Split(',');
                            string size = arr.Aggregate("", (current, a) => current + a);
                            DateTime dt;
                            DateTime.TryParse(Properties.Settings.Default.PatchVersion, out dt);
                            if (GetDate(r) > dt || _forcedUpdateLib)
                            {
                                if (r.ToLower().Contains("swl"))
                                    list.Add(r);
                                var s = Convert.ToInt64(size);
                                dict.Add(r, s);
                            }
                        }
                    }
                    catch { err = false; }
                } while (err);
            }

            reqFtp.Abort();
            list.Sort((x, y) => GetDate(x).CompareTo(GetDate(y)));
            if (list.Count > 0)
            {
                libName = list.Last();
                libSize = dict[libName];
            }
            return dict.Where(d => d.Key.ToLower().Contains("swp")).ToDictionary(d => d.Key, d => d.Value);
        }

        public static DateTime GetDate(string str)
        {
            var dat = new DateTime();
            if (str.ToLower().Contains("sw"))
            {
                string strDate = str.Substring(3, 10);
                strDate = strDate.Insert(2, ".");
                strDate = strDate.Insert(5, ".20");
                strDate = strDate.Insert(10, " ");
                strDate = strDate.Insert(13, ":");

                DateTime.TryParse(strDate, out dat);
            }
            return dat;
        }

        private void RewriteAllFilesWhichNotAccessable()
        {
            do
            {
                foreach (var d in _dict)
                {
                    try
                    {
                        File.SetAttributes(d.Key,FileAttributes.Normal);
                        File.Delete(d.Key);
                    }
                    catch
                    {
                        continue;
                    }
                    File.Copy(d.Value, d.Key);
                    _dict.Remove(d.Key);
                    new FileInfo(d.Value).IsReadOnly = false;
                    File.Delete(d.Value);
                }
            } while (_dict.Count > 0);
        }

        private void CopyTempToMainDir()
        {
            bool isErr = false;
            progressBar1.Value = 1;
            var tempFiles = _tempDir.GetFiles("*", SearchOption.AllDirectories);
            progressBar1.Maximum = tempFiles.Count();
            foreach (var file in tempFiles)
            {
                string newFileName = _mainDir.FullName + file.FullName.Substring(_tempDir.FullName.Length);
                if (File.Exists(newFileName))
                {
                    label2.Text = @"Замещение";
                    label1.Text = Path.GetFileName(newFileName);
                    Application.DoEvents();

                    try
                    {
                        File.SetAttributes(newFileName, FileAttributes.Hidden);
                        string newTempFileNameForReverse = _tempForLastFiles.FullName +
                                                           file.FullName.Substring(_tempDir.FullName.Length);
                        MakeDirInPath(newTempFileNameForReverse);
                        if (file.Name.First()!='~')
                            File.Copy(newFileName, newTempFileNameForReverse);
                        if (!_dictReverse.ContainsKey(newFileName))
                            _dictReverse.Add(newFileName, newTempFileNameForReverse);

                        File.Delete(newFileName);
                       
                    }
                    catch
                    {
                        if (!_dict.ContainsKey(newFileName))
                            _dict.Add(newFileName, file.FullName);
                        isErr = true;
                        continue;
                    }
                    _logErr.Add("Замещение файла " + newFileName);
                }
                else
                {
                    var listDirs = MakeDirInPath(newFileName);
                    foreach (var directoryInfo in GetAllDirectoriesFromPath(file.FullName))
                    {
                        foreach (var listDir in listDirs)
                        {
                            if (listDir.Name == directoryInfo.Name)
                                listDir.Attributes = directoryInfo.Attributes;
                        }
                    }
                    label2.Text = @"Добавление";
                    label1.Text = Path.GetFileName(newFileName);
                    Application.DoEvents();
                    _logErr.Add("Добавление файла " + newFileName);

                }

                if (file.Name.First() != '~')
                {
                    File.Copy(file.FullName, newFileName);
                    if (Path.GetExtension(newFileName).ToLower() == ".sldasm" || Path.GetExtension(newFileName).ToLower() == "sldasm" || Path.GetExtension(newFileName).ToLower() == ".sldprt" || Path.GetExtension(newFileName).ToLower() == "sldprt")
                    {
                        _cashFilesToDelete.Add(newFileName);//Cash.DeleteSimilarFromCashOrig(newFileName);
                        _cashFilesToCopy.Add(newFileName);//Cash.CopyToCash(newFileName, "01");

                    }
                }
                label3.Text = @"в " + Furniture.Helpers.LocalAccounts.modelPathResult;
                Application.DoEvents();
                progressBar1.Increment(1);
                if (!isErr)
                {
                    file.IsReadOnly = false;
                    file.Delete();
                }
            }
        }

        private static string GetUniqNameOfTempDirectory()
        {
            var rd = new Random();
            double randDouble = rd.NextDouble();
            string val = randDouble.ToString();
            string valWithOutIntPart = val.Substring(2);
            ulong codeName = Convert.ToUInt64(valWithOutIntPart);
            string ret = codeName.ToString("x");
            return ret;
        }

        private void UnZipPatch(string pathOfPatch)
        {
            using (ZipFile zip = ZipFile.Read(pathOfPatch, Encoding.GetEncoding(866)))
            {
                progressBar1.Minimum = 1;
                progressBar1.Value = 1;
                progressBar1.Maximum = zip.Count;
                label3.Text = @"в " + _tempDir.FullName;
                Application.DoEvents();

                foreach (var zEntry in zip.Entries)
                {
                    try
                    {
                        label2.Text = @"Распаковка";
                        label1.Text = Path.GetFileName(zEntry.FileName);
                        Application.DoEvents();
                        progressBar1.Increment(1);
                        zEntry.Extract(_tempDir.FullName);
                        if (zEntry.IsDirectory)
                        {
                            FileAttributes tmp = zEntry.Attributes;
                            if ((tmp & FileAttributes.Hidden) != 0)
                                tmp = FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.Directory;
                            else
                                tmp = FileAttributes.ReadOnly | FileAttributes.Directory;
                                
                            new DirectoryInfo(_tempDir.FullName + "\\" + zEntry.FileName).Attributes = tmp;//zEntry.Attributes;
                        }
                    }
                    catch (ZlibException ze)
                    {
                        label1.Text = ze.Message + Path.GetFileName(zEntry.FileName);
                        _logErr.Add(Path.GetFileName(zEntry.FileName) + " : " + ze.Message);
                        Application.DoEvents();
                    }
                }
            }
            return;
        }

        private static string GetNewTempDirName()
        {
            string tempDirName;
            if (_mainDir==null)
            {
                _mainDir = Directory.CreateDirectory(Furniture.Helpers.LocalAccounts.modelPathResult);
            }
            do
            {
                tempDirName = _mainDir.Root + GetUniqNameOfTempDirectory().ToLower();
            } while (Directory.Exists(tempDirName));
            return tempDirName;
        }

        private static List<DirectoryInfo> MakeDirInPath(string newFileName)
        {
            var ret = new List<DirectoryInfo>();
            string[] arrPaths = newFileName.Split('\\');
            for (int i = 0; i < arrPaths.Length - 1; i++)
            {
                if (!Directory.Exists(arrPaths[i]))
                    ret.Add(Directory.CreateDirectory(arrPaths[i]));
                arrPaths[i + 1] = arrPaths[i] + "\\" + arrPaths[i + 1];
            }
            return ret;
        }

        private IEnumerable<DirectoryInfo> GetAllDirectoriesFromPath(string fileName)
        {
            var ret = new List<DirectoryInfo>();
            string[] arrPaths = fileName.Split('\\');
            for (int i = 0; i < arrPaths.Length - 1; i++)
            {
                ret.Add(new DirectoryInfo(arrPaths[i]));
                arrPaths[i + 1] = arrPaths[i] + "\\" + arrPaths[i + 1];
            }
            return ret;
        }

        internal void UpdateLibFromLocalFile(FileInfo newLib)
        {
            _tempDir = Directory.CreateDirectory(GetNewTempDirName());
            _tempForLastFiles = Directory.CreateDirectory(GetNewTempDirName());
            UnZipPatch(newLib.FullName);
           
            //var mainFiles = _mainDir.GetFiles("*", SearchOption.AllDirectories).Where(x => x.IsReadOnly = true);
            //foreach (var file in mainFiles)
            //{
            //    file.IsReadOnly = false;
            //}
            _mainDir.Attributes = FileAttributes.Normal;
            setAttributesNormal(_mainDir);
            if (_mainDir.Exists)
                _mainDir.Delete(true);

            _logErr.Add("Удаление старой библиотеки от " + Properties.Settings.Default.PatchVersion);

            label1.Text = @"Копирование";
            Application.DoEvents();

            var tempFiles = _tempDir.GetFiles("*", SearchOption.AllDirectories);
            foreach (var tempFile in tempFiles.Where(x => x.IsReadOnly = true))
            {
                tempFile.IsReadOnly = false;
            }
            _mainDir.Create();
            progressBar1.Value = 1;
            progressBar1.Maximum = tempFiles.Count();
            foreach (var file in tempFiles)
            {
                label2.Text = file.Name;
                label3.Text = @"в " + _mainDir.FullName;
                Application.DoEvents();
                string newFileName = _mainDir.FullName + file.FullName.Substring(_tempDir.FullName.Length);

                var listDirs = MakeDirInPath(newFileName);
                foreach (var directoryInfo in GetAllDirectoriesFromPath(file.FullName))
                {
                    foreach (var listDir in listDirs)
                    {
                        if (listDir.Name == directoryInfo.Name)
                            listDir.Attributes = directoryInfo.Attributes;
                    }
                }
                if (file.Name.First() != '~')
                {
                    File.Copy(file.FullName, newFileName);
                    _logErr.Add("Запись файла " + newFileName);
                }

                progressBar1.Increment(1);
            }
            label1.Text = @"Удаление файлов с ""~""";
            foreach (var tildaFile in _mainDir.GetFiles("~*.*", SearchOption.AllDirectories))
            {
                tildaFile.Attributes = FileAttributes.Normal;
                _logErr.Add("Удаление файла с тильдой: " + tildaFile.FullName);
                tildaFile.Delete();
            }
            label1.Text = @"Удаление пустых директорий";
            processDirectory(_mainDir.FullName, _logErr);

            Properties.Settings.Default.PatchVersion = GetDate(newLib.Name).ToString();
            Properties.Settings.Default.Save();
            Close();
        }
    }
}
