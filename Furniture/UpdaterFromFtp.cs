using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;

namespace Furniture
{
    public static class UpdaterFromFtp
    {
        public static bool DownloadFromFtp(string fileName)
        {
            bool ret = true;
            var dirName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(dirName, fileName);
            try
            {

                var wc = new WebClient { Credentials = new NetworkCredential("solidk", "KSolid") };
                var fileStream = new FileInfo(path).Create();
                //string downloadPath = Path.Combine("ftp://194.84.146.5/ForDealers",fileName);
                string downloadPath = Path.Combine(Furniture.Helpers.FtpAccess.resultFtp+"ForDealers", fileName);
                var str = wc.OpenRead(downloadPath);

                const int bufferSize = 1024;
                var buffer = new byte[bufferSize];
                int readCount = str.Read(buffer, 0, bufferSize);
                while (readCount > 0)
                {
                    fileStream.Write(buffer, 0, readCount);
                    readCount = str.Read(buffer, 0, bufferSize);
                }
                str.Close();
                fileStream.Close();
                wc.Dispose();
            }
            catch
            {
                ret = false;
            }
            return ret;
        }
    }
}
