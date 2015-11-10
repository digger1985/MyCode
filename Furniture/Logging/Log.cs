using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Layout;


namespace Furniture.Logging
{
    public class Log
    {
        private static readonly Log instance = new Log();
        private ILog log;
        public string FilePath;
        public static Log Instance
        {
            get { return instance; }
        }

        protected Log()
        {
            #region конфигурирование FileLog
            //log4net.Config.XmlConfigurator.Configure();//конфиг не происходит, тк очевидно что у либы не может быть app.config-а,
            //а тут он есть, но получается что он "ненастоящий"
            var apender = new RollingFileAppender();
            apender.File = Path.Combine(Furniture.Helpers.LocalAccounts.modelPathResult, "LogUpd.txt");
            if (Path.GetFileName(apender.File) != "LogUpd.txt")
                apender.File = Path.Combine(Furniture.Helpers.LocalAccounts.modelPathResult, "\\LogUpd.txt");
            //apender.File = "D:\\_SWLIB_\\LogUpd.txt";
            apender.AppendToFile = true;
            apender.RollingStyle = RollingFileAppender.RollingMode.Size;
            apender.MaxSizeRollBackups = 0;
            apender.MaximumFileSize = "2MB";
            apender.StaticLogFileName = true;
            apender.Encoding = Encoding.UTF8;
            apender.LockingModel = new FileAppender.MinimalLock();
            apender.Layout = new PatternLayout("%date [%thread] %-5level [%property{NDC}] - %message%newline");
            apender.ActivateOptions();
            FilePath = apender.File;
            File.SetAttributes(FilePath, FileAttributes.Normal);
            #endregion


            log4net.Config.BasicConfigurator.Configure(apender);

            log = LogManager.GetLogger(typeof(Log));

        }
        private string GetLogFileAsString()
        {
            TextReader textReader = new StreamReader(FilePath);
            return textReader.ReadToEnd();
        }

        public void Debug(string message)
        {
            if (Properties.Settings.Default.LoggingOn)
                log.Debug(message);
        }
        public void Info(List<string> messages)
        {
            foreach (var message in messages)
            {
                log.Info(message);
            }
        }
        Dictionary<int,Stopwatch> stopwatches = new Dictionary<int,Stopwatch>();
        private int id = int.MinValue;
        public int TraceStart(string msg)
        {
            if (!Properties.Settings.Default.LoggingOn)
                return 0;
            Debug(msg);
            var stopwatch = new Stopwatch();
            
            if (id < int.MaxValue)
                id = id + 1;
            else
                id = int.MinValue;
            stopwatches.Add(id, stopwatch);
            stopwatch.Reset();
            stopwatch.Start();
            return id;
        }
        public double TraceStop(int id,string msg)
        {
            if (!Properties.Settings.Default.LoggingOn)
                return 0;
            if (!stopwatches.ContainsKey(id))
            {
                Debug("Trace Error: No such Key in Dictionary!!!!!!!!!!!");
                return 0;
            }
            var stopwatch = stopwatches[id];
            stopwatch.Stop();
            double ms = (stopwatch.ElapsedTicks * 1000.0) / Stopwatch.Frequency;
            Debug(msg+" TraceTime: "+ ms.ToString());
            stopwatches.Remove(id);
            return ms;
        }
        public void Debug(List<string> messages)
        {

            if (!Properties.Settings.Default.LoggingOn)
                return;
            foreach (var message in messages)
            {
                log.Debug(message);
            }
        }
        public void Fatal(Exception e, string message)
        {
            if (!Properties.Settings.Default.FatalMailOn)
                return;
            try
            {
                if (e != null)
                    log.Debug(e.Message);
                else
                    log.Debug(message);

                string userNameWin, compName,  myHost;

                myHost = Dns.GetHostName();
                userNameWin = Environment.UserName;
                compName = Environment.MachineName;

                string identificateInfo =
                    string.Format("{3}Имя хоста: {0} , Имя пользователя Win: {1} , Имя компьютера: {2}{3} Версия: {4}", myHost,
                                  userNameWin, compName, Environment.NewLine, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                MailMessage mailMessage = new MailMessage(Properties.Settings.Default.MailFrom, "swsupport@mrdoors.ru"); //Properties.Settings.Default.MailTo);
                mailMessage.Subject = "error log message";
                string logString = string.Empty;//Instance.GetLogFileAsString();
                string exceptionTitle;
                if (e != null)
                    exceptionTitle = string.Format("{3}Exception type:{0}, Exception message:{1}, Description message: {2}{3}",
                                                        e.GetType().ToString(), e.Message, message, Environment.NewLine);
                else
                    exceptionTitle = string.Format("{1}Description message: {0}{1}", message, Environment.NewLine);

                mailMessage.Body = identificateInfo+ logString + exceptionTitle;
                SmtpClient smtpClient = new SmtpClient(Properties.Settings.Default.MailHost);
                smtpClient.Credentials = new NetworkCredential(Properties.Settings.Default.MailFrom, Properties.Settings.Default.MailHostPass);
                smtpClient.Send(mailMessage);
            }
            catch (Exception)
            {
                //собсно exception никак не обрабатывается
            }
        }
        public void Fatal(string message)
        {
            Fatal(null, message);
        }
        public void SendMail(string body, string subject = "error log message",string orderNumber = null)
        {
            if (!Properties.Settings.Default.FatalMailOn)
                return;
            try
            {

                string userNameWin, compName, myHost;

                myHost = Dns.GetHostName();
                userNameWin = Environment.UserName;
                compName = Environment.MachineName;
                if (orderNumber == null)
                    orderNumber = string.Empty;
                string identificateInfo =
                    string.Format("{4}Имя хоста: {0} , Имя пользователя Win: {1} , Имя компьютера: {2}, Номер заказа:{3}{4}", myHost,
                                  userNameWin, compName,orderNumber,Environment.NewLine);

                MailMessage mailMessage = new MailMessage(Properties.Settings.Default.MailFrom, "dorogavtsev@mrdoors.ru");
                mailMessage.Subject = subject;
              
                mailMessage.Body = identificateInfo+Environment.NewLine + body ;
                SmtpClient smtpClient = new SmtpClient(Properties.Settings.Default.MailHost);
                smtpClient.Credentials = new NetworkCredential(Properties.Settings.Default.MailFrom, Properties.Settings.Default.MailHostPass);
                smtpClient.Send(mailMessage);
            }
            catch (Exception)
            {
                //собсно exception никак не обрабатывается
            }
        }
    }
}
