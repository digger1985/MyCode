using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Furniture
{
    public abstract class Decors
    { 
        private static Dictionary<string, string> _dictionary = new Dictionary<string, string>();

        internal static void AddModelToList(SwAddin mSwAddin, ModelDoc2 newModel)
        {
            List<ModelDoc2> fM = null;
            string xNameOfAss = mSwAddin.GetXNameForAssembly(false, Path.GetFileNameWithoutExtension(mSwAddin.SwModel.GetPathName()));
            #region Проверка на наличие модели,которая была удалена
            var checkList = new List<ModelDoc2>();
            if (!DictionaryListMdb.ContainsKey(xNameOfAss))
                DictionaryListMdb.Add(xNameOfAss, new List<ModelDoc2>());
            foreach (var m in DictionaryListMdb[xNameOfAss])
            {
                try
                {
                    m.GetPathName();
                    checkList.Add(m);
                }
                catch (COMException)
                {
                    continue;
                }
            }
            #endregion
            if (!checkList.Contains(newModel))
                CheckMdbForDecors(mSwAddin, newModel, checkList, null, fM);
            DictionaryListMdb[xNameOfAss] = checkList;
        }

        private static List<string> GetComponentsNamesOnConfiguration(SwAddin _mSwAddin, ModelDoc2 parentModel, int configNumber)
        {
            List<string> elems = new List<string>();

            OleDbConnection oleDb;
            if (_mSwAddin.OpenModelDatabase(parentModel, out oleDb))
            {
                using (oleDb)
                {
                    OleDbCommand cm = new OleDbCommand("SELECT * FROM decors WHERE Number = " + configNumber, oleDb);
                    OleDbDataReader rd = cm.ExecuteReader();
                    while (rd.Read())
                    {
                        elems.Add((string)rd["Element"]);
                    }
                    rd.Close();
                }
            }

            AssemblyDoc pmAssembly = parentModel as AssemblyDoc;
            if (pmAssembly != null)
            {
                var comps = new LinkedList<Component2>();                
                _mSwAddin.GetComponents(parentModel.IGetActiveConfiguration().IGetRootComponent2(), comps, true, false);                
                foreach (Component2 comp in comps)
                {                    
                    ModelDoc2 model = comp.IGetModelDoc();
                    AssemblyDoc aDoc = model as AssemblyDoc;
                    if (aDoc != null)
                        elems.AddRange(GetComponentsNamesOnConfiguration(_mSwAddin, model, configNumber));                     
                }
            }
            return elems; 
        }

        internal static List<Component2> GetConfigComponents(SwAddin swAddin, ModelDoc2 model, int configNumber)
        {  
            List<Component2> outComps = new List<Component2>();

            List<string> compsNames = GetComponentsNamesOnConfiguration(swAddin, model, configNumber);

            LinkedList<Component2> modelComponents = new LinkedList<Component2>();
            swAddin.GetComponents(model.IGetActiveConfiguration().IGetRootComponent2(), modelComponents, true, false);

            foreach (var component in modelComponents)
            {
                if (component.IsSuppressed()) continue;
                string compName = Path.GetFileNameWithoutExtension(swAddin.GetModelNameWithoutSuffix(component.GetPathName()));
                
                foreach (string name in compsNames)
                {
                    if (compName.Contains(name))
                    {
                        outComps.Add(component);
                        break;
                    }
                }
            }
            return outComps;
        }

        internal static List<DecorsListS> GetListComponentForDecors(SwAddin swAddin, OleDbDataReader reader, LinkedList<Component2> components)
        {
            var listDetailComps = components.Select(x => x.IGetModelDoc()).Where(x => x != null
                                                                                          &&
                                                                                          x.GetType() ==
                                                                                          (int)
                                                                                          swDocumentTypes_e
                                                                                              .swDocPART)
                    .Select(
                        x => Path.GetFileNameWithoutExtension(swAddin.
                                                                  GetModelNameWithoutSuffix(
                                                                      x.GetPathName())));



            var decorsLists = new List<DecorsListS>();
            int i = 0;
            while (reader.Read())
            {
                //if(listDetailComps.Contains((string)reader["Element"]))
                string rr = (string)reader["Element"];
                if (listDetailComps.Where(x => x.Contains(rr)).FirstOrDefault() != null)
                {
                    var numbPos = Convert.ToInt32(reader["Number"]);
                    if (numbPos != i)
                    {
                        foreach (var component2 in components)
                        {
                            if (component2.IsSuppressed()) continue;
                            //if (Path.GetFileNameWithoutExtension(swAddin.GetModelNameWithoutSuffix(component2.GetPathName()))== ((string)reader["Element"]))
                            if (Path.GetFileNameWithoutExtension(swAddin.GetModelNameWithoutSuffix(component2.GetPathName())).Contains((string)reader["Element"]))
                            {
                                decorsLists.Add(new DecorsListS((int)reader["Number"], component2));
                                break;
                            }
                        }
                        i = numbPos;
                    }
                }
            }
            return decorsLists;
        }

        internal static List<DimensionConfiguration> GetListComponentForDimension(SwAddin swAddin, OleDbDataReader reader, LinkedList<Component2> components)
        {
            var listDetailComps = components.Select(x => x.IGetModelDoc()).Where(x => x != null)
                    .Select(
                        x => Path.GetFileNameWithoutExtension(swAddin.
                                                                  GetModelNameWithoutSuffix(
                                                                      x.GetPathName()))).ToArray();
            string tmpChar;
            string[] listDetailCompTmp = new string[listDetailComps.Count()];
            int ii = 0;
            foreach (var listDetailComp in listDetailComps)
            {
                if (listDetailComp.Length < 6)
                {
                    listDetailCompTmp[ii] = listDetailComp;
                    ii++;
                    continue;
                }
                tmpChar = listDetailComp.Substring(listDetailComp.Length - 4, 1);
                if ((listDetailComp.Last() == 'P' || listDetailComp.Last() == 'p') && (tmpChar == "#"))
                    listDetailCompTmp[ii] = listDetailComp.Substring(0, listDetailComp.Length - 4);
                else
                    listDetailCompTmp[ii] = listDetailComp;
                ii++;
            }
            //listDetailComps = new string[listDetailCompTmp.Length];
            listDetailComps = listDetailCompTmp;
            var decorsLists = new List<DimensionConfiguration>();
            int i = 0;
            while (reader.Read())
            {
                if (listDetailComps.Contains((string)reader["element"]))
                {
                    var numbPos = Convert.ToInt32(reader["number"]);
                 
                    if (numbPos != i)
                    {
                        List<Component2> compsWithNumberCopies = new List<Component2>();
                        foreach (var comp in components)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(swAddin.GetModelNameWithoutSuffix(comp.GetPathName()));
                            int rIndex = fileName.LastIndexOf('#');
                            if (rIndex != -1)
                                fileName = fileName.Remove(rIndex, fileName.Length - rIndex);
                            if (fileName == (string)reader["Element"])
                                compsWithNumberCopies.Add(comp); 
                        }

                        try
                        {
                            compsWithNumberCopies.Sort(
                                (x, y) =>
                                Convert.ToInt32(x.Name.Substring(x.Name.LastIndexOf('-') + 1)).CompareTo(
                                    Convert.ToInt32(y.Name.Substring(y.Name.LastIndexOf('-') + 1))));

                        }
                        catch { }
                        if (compsWithNumberCopies.Count > 0)
                        {
                            Component2 neededComp = compsWithNumberCopies.First();
                            decorsLists.Add(new DimensionConfiguration((int)reader["number"], neededComp,
                                                                       (string)reader["caption"],
                                                                       (string)reader["idslave"], (int)reader["id"]));
                        }
                        i = numbPos;
                    }
                }
            }
            return decorsLists;
        }

        internal static Dictionary<string, string> MemoryForDecors
        {
            get
            {
                return _dictionary;
            }
            set
            {
                _dictionary = value;
            }
        }

        internal static Dictionary<string, List<ModelDoc2>> DictionaryListMdb = new Dictionary<string, List<ModelDoc2>>();

        internal static List<ModelDoc2> GetAllModelsWithMdb(SwAddin mSwAddin, ModelDoc2 swModel)
        {
            UserProgressBar pb;
            mSwAddin.SwApp.GetUserProgressBar(out pb);
            var list = new List<ModelDoc2>();
            var outComps = new LinkedList<Component2>();
            var faulsMod = new List<ModelDoc2>();

            if (mSwAddin.GetComponents(swModel.IGetActiveConfiguration().IGetRootComponent2(),
                                        outComps, true, false))
            {
                pb.Start(0, outComps.Count, "Перебор деталей");
                int i = 0;
                foreach (var component in outComps)
                {
                    if (component.IsSuppressed())
                    {
                        i++;
                        pb.UpdateProgress(i);
                        continue;
                    }
                    var inModel = component.IGetModelDoc();
                    if (list.Contains(inModel))
                    {
                        i++;
                        pb.UpdateProgress(i);
                        continue;
                    }
                    var comp = component;

                    int errCount = 0;
                    while ((inModel == null || mSwAddin.GetModelDatabaseFileName(inModel) == "") && errCount < 10)
                    {
                        if (comp != null)
                        {
                            comp = comp.GetParent();
                            if (comp != null && !comp.IsSuppressed())
                            {
                                inModel = comp.IGetModelDoc();
                            }
                        }
                        errCount++;
                    }
                    if (errCount == 10 && inModel != null)
                    {
                        i++;
                        pb.UpdateProgress(i);
                        continue;
                    }

                    if (inModel != null &&
                        Path.GetFileNameWithoutExtension(inModel.GetPathName()).Contains("#"/* +
                                                                                         mSwAddin.GetXNameForAssembly()*/) &&
                        !list.Contains(inModel) && !faulsMod.Contains(inModel))
                        CheckMdbForDecors(mSwAddin, inModel, list, comp, faulsMod);

                    i++;
                    pb.UpdateProgress(i);
                }
                pb.End();
            }
            return list;
        }

        private static void CheckMdbForDecors(SwAddin mSwAddin, ModelDoc2 inModel, List<ModelDoc2> list, Component2 comp, List<ModelDoc2> faulsModels)
        {
            OleDbConnection oleDb;
            if (mSwAddin.OpenModelDatabase(inModel, out oleDb))
            {
                var oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                if (oleSchem.Rows.Cast<DataRow>().Any(row => (string)row["TABLE_NAME"] == "decors"))
                {
                    if (!list.Contains(inModel))
                        list.Add(inModel);
                }
                else
                {
                    if (faulsModels != null && !faulsModels.Contains(inModel))
                        faulsModels.Add(inModel);
                    oleDb.Close();
                    if (comp != null)
                    {
                        comp = comp.GetParent();
                        if (comp != null && !comp.IsSuppressed())
                        {
                            inModel = comp.IGetModelDoc();
                            if (inModel != null && mSwAddin.OpenModelDatabase(inModel, out oleDb) && !list.Contains(inModel))
                            {
                                oleSchem = oleDb.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                                if (oleSchem.Rows.Cast<DataRow>().Any(row => (string)row["TABLE_NAME"] == "decors"))
                                {
                                    if (!list.Contains(inModel))
                                        list.Add(inModel);
                                }
                                oleDb.Close();
                            }
                        }
                    }
                }
                if (oleDb != null)
                    oleDb.Close();
            }
        }

        internal static bool CheckListForDeletedModels(SwAddin mSwAddin)
        {
            var list = new List<ModelDoc2>();
            var swModel = mSwAddin.SwApp.IActiveDoc2;
            string xNameOfAss = mSwAddin.GetXNameForAssembly(false, Path.GetFileNameWithoutExtension(swModel.GetPathName()));
            foreach (var lmdb in DictionaryListMdb[xNameOfAss])
            {
                try
                {
                    lmdb.GetPathName();
                    list.Add(lmdb);
                }
                catch (COMException)
                {
                    continue;
                }
            }
            DictionaryListMdb[xNameOfAss] = list;
            return list.Count == 0;
        }
    }

    public class DecorsListS
    {
        public int Number;
        public Component2 Component;

        public DecorsListS(int inNumber, Component2 inComponent)
        {
            Number = inNumber;
            Component = inComponent;
        }
    }

    public class DecorsListL
    {
        public int Number;
        public Component2 Component;
        public string LabelConfName;
        public string LabelDecName;

        public DecorsListL(int inNumber, Component2 inComponent, string inLabelConfName, string inLabelDecName)
        {
            Number = inNumber;
            Component = inComponent;
            LabelConfName = inLabelConfName;
            LabelDecName = inLabelDecName;
        }
    }

    internal struct DimensionConfiguration
    {
        public int Number;
        public Component2 Component;
        public string Caption;
        public string IdSlave;
        public int Id;


        internal DimensionConfiguration(int inNumber, Component2 inComponent, string caption, string idSlave, int id)
        {
            Number = inNumber;
            Component = inComponent;
            Caption = caption;
            IdSlave = idSlave;
            Id = id;
        }
    }
}
