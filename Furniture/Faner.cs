using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Furniture
{
    public class Faner
    {
        private string _fanerName;
        private string _defaultFanerType;
        private string _decorGroup;


        public string FanerName
        {
            get { return _fanerName; }
        }
        public string AxFanerName
        {
            get { return _fanerName.Replace("FANER", "AXFANER"); } //string.Format("NONE{0}", _fanerName.Substring(_fanerName.Length - 2, 2)); }
        }
        public string DefaultFanerType
        {
            get { return _defaultFanerType; }
        }

        public  List<string> DecorGroup
        {
            get
            {
                OleDbCommand cm;
                OleDbDataReader rd;
                OleDbConnection oleDb;
                if (!FrmSetParameters.OpenOleDecors(out oleDb))
                    return new List<string>();
                string decPathDef = Furniture.Helpers.LocalAccounts.decorPathResult;
                string selectStr = "SELECT * FROM decordef WHERE " + _decorGroup + " = true";
                cm = new OleDbCommand(selectStr, oleDb);
                rd = cm.ExecuteReader();

                var strNameList = new List<string>();
                while (rd.Read())
                {
                    strNameList.Add(rd["FILEJPG"].ToString());
                }
                rd.Close();
                strNameList.Sort((x, y) => x.CompareTo(y));
                List<string> result = new List<string>();
                foreach (var fileName in strNameList)
                {
                    string oneOfPath = decPathDef + fileName + ".jpg";
                    if (File.Exists(oneOfPath))
                    {
                        result.Add(Path.GetFileNameWithoutExtension(oneOfPath));
                    }
                    else
                        MessageBox.Show(
                            @"Файл" + Environment.NewLine + oneOfPath
                            + Environment.NewLine + @"не существует!",
                            "MrDoors", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                }

                return result;
            }
        }


        public Faner(string fanerName, string defaultFanerType,string decorGroup)
        {
            _fanerName = fanerName;
            _defaultFanerType = defaultFanerType;
            _decorGroup = decorGroup;
        }
    }
}
