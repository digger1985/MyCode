using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LibraryHelper
{
    public class Logger
    {
        #region Статические конструкторы

        public static Logger GetEntryAssemblyLogger()
        {
            String ProgramPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            String LogFileExtension = "log";
            String LogPath = String.Format("{0}.{1}", Path.GetFileNameWithoutExtension(ProgramPath), LogFileExtension);

            return new Logger(LogPath);
        }

        public static Logger GetLogger(String fileName)
        {
            return new Logger(fileName);
        }

        #endregion

        public String FullFilePath { get; private set; }

        private Logger(String fileName)
        {
            FullFilePath = Path.GetFullPath(fileName);
        }

        public void Log(String message)
        {
            String record = CreateLogRecord(message);
            File.AppendAllText(FullFilePath, record);
        }

        private String CreateLogRecord(String message)
        {
            String newLine = System.Environment.NewLine;
            String indent = File.Exists(FullFilePath) ? newLine : String.Empty;
            String record = String.Format("{0}[{2}]{1}{3}{1}", indent, newLine, DateTime.Now, message);

            return record;
        }
    }
}
