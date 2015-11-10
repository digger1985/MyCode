using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LibraryHelper
{
    static class Program
    {
        public static Logger Logger { get; private set; }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Logger = Logger.GetEntryAssemblyLogger();
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.Run(new MainForm());
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.Log(args.ExceptionObject.ToString());
        }
    }
}
