using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Furniture.Helpers
{
    public static class LocalAccounts
    {
        public static string modelPathResult = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("ModelPath") == string.Empty
            ? Properties.Settings.Default.ModelPath : Furniture.Helpers.SaveLoadSettings.ReadAppSettings("ModelPath");

        public static string decorPathResult = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DecorPath") == string.Empty
            ? Properties.Settings.Default.DecorPath : Furniture.Helpers.SaveLoadSettings.ReadAppSettings("DecorPath");

        public static string connectIniPath = Furniture.Helpers.SaveLoadSettings.ReadAppSettings("ConnectIniPath") == string.Empty
            ? Properties.Settings.Default.ConnectIniPath : Furniture.Helpers.SaveLoadSettings.ReadAppSettings("ConnectIniPath");
    }
}
