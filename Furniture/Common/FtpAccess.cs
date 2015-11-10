using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Furniture.Helpers
{
   public static class FtpAccess
    {           
       // Результат 
       public static string resultFtp = accessibilityForFtp();

       

       // Проверка на доступность FTP
       static bool CheckFTPLogin(String ftpServer, uint ftpPort, String userName, String password)
       {
           if (String.IsNullOrEmpty(ftpServer) ||
           String.IsNullOrEmpty(userName) ||
           String.IsNullOrEmpty(password))
               return false;

           using (TcpClient client = new TcpClient())
           {
               try
               {
                   client.Connect(ftpServer, Convert.ToInt32(ftpPort));
                   NetworkStream nStream = client.GetStream();
                   StreamReader myReader = new StreamReader(nStream);


                   string retValue = myReader.ReadLine();

                   if (retValue.StartsWith("220"))
                   {
                       retValue = myReader.ReadLine();

                       if (retValue.StartsWith("220"))
                       {
                           retValue = myReader.ReadLine();

                           if (retValue.StartsWith("220"))
                           {
                               //Login Success
                               myReader.Close();
                               return true;
                           }
                       }
                       else
                       {
                           //Login Error.
                           myReader.Close();
                           return false;
                       }
                   }
                   else
                   {
                       //FTP Connection error.
                       myReader.Close();
                       return false;
                   }
                   return false;
               }
               catch
               {
                   //FTP Connection Error
                   return false;
               }
           }
       }


        // Возвращает рабочий FTP
        public static string accessibilityForFtp()
        {
            // проверка на доступность FTP 
            
            bool repomrdoors = CheckFTPLogin("repo.mrdoors.ru", 21, "solidk", "KSolid");
            bool localrepomrdoors = CheckFTPLogin("localrepo.mrdoors.ru", 21, "solidk", "KSolid");

            if (localrepomrdoors)
            {
                return String.Format(@"ftp://" + "localrepo.mrdoors.ru" + "/"); //return String.Format(@"ftp://" + "localrepo.mrdoors.ru" + "/");
            }
            else if (repomrdoors)
            {
                return String.Format(@"ftp://" + "repo.mrdoors.ru" + "/");// return String.Format(@"ftp://" + "repo.mrdoors.ru" + "/");
            }
            else
            {
                return null;

            }
        }
      
    }
}
