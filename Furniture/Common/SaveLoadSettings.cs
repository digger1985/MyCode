using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Config = System.Configuration;
using System.Windows.Forms;
using System.Reflection;

namespace Furniture.Helpers
{
    public static class SaveLoadSettings
    {


        // Чтение свойств из  furniture.dll.config  xml
        public static string ReadAppSettings(string key)
        {
            try
            {
                System.Configuration.Configuration currentConfig = System.Configuration.ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
                string keyValue = currentConfig.AppSettings.Settings[key].Value;


                return keyValue;
            }
            catch { return string.Empty; }


        }
        // Запись свойств в  furniture.dll.config  xml
        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                System.Configuration.Configuration config = GetDllConfiguration(Assembly.GetExecutingAssembly());

                if (!config.AppSettings.Settings.AllKeys.Contains(key))
                    config.AppSettings.Settings.Add(new System.Configuration.KeyValueConfigurationElement(key, ""));
                config.AppSettings.Settings[key].Value = value;
                config.Save();
                System.Configuration.ConfigurationManager.RefreshSection("appSettings");
            }
            catch (System.Configuration.ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }

        static System.Configuration.Configuration GetDllConfiguration(Assembly targetAsm)
        {
            var configFile = targetAsm.Location + ".config";
            var map = new System.Configuration.ExeConfigurationFileMap
            {
                ExeConfigFilename = configFile
            };
            return System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(map, System.Configuration.ConfigurationUserLevel.None);
        }

       public static void SaveAllProperties()
        {

            if (ReadAppSettings("DBPath") == string.Empty)
                AddOrUpdateAppSettings("DBPath", Properties.Settings.Default.DBPath);
            if (ReadAppSettings("DrwPath") == string.Empty)
                AddOrUpdateAppSettings("DrwPath", Properties.Settings.Default.DrwPath);
            if (ReadAppSettings("ModelPath") == string.Empty)
                AddOrUpdateAppSettings("ModelPath", Properties.Settings.Default.ModelPath);
            if (ReadAppSettings("DecorPath") == string.Empty)
                AddOrUpdateAppSettings("DecorPath", Properties.Settings.Default.DecorPath);
            //if (ReadAppSettings("ConnectIniPath") == string.Empty)
            //    AddOrUpdateAppSettings("ConnectIniPath", Properties.Settings.Default.ConnectIniPath);
        }

    }
}
