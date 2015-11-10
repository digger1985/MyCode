using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SwDocumentMgr;

namespace Furniture
{
    public static class linksToCash
    {
        public static void Save(string pathToWrite)
        {
            string filePath = Path.Combine(Furniture.Helpers.LocalAccounts.modelPathResult, "linksToCash.txt");
            bool doesntExists = false;
            if (!File.Exists(filePath))
            {
                using (File.CreateText(filePath))

                    doesntExists = true;
            }
            string[] lines;
            if (!doesntExists)
                lines = GetAll(filePath);
            else
                lines = new string[0];
            if (lines.Contains(pathToWrite))
                return;

            File.SetAttributes(filePath, FileAttributes.Normal);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
            {
                file.WriteLine(pathToWrite);
            }
        }
        public static void Remove(string pathToRemove)
        {
            string filePath = Path.Combine(Furniture.Helpers.LocalAccounts.modelPathResult, "linksToCash.txt");
            var listLines = GetAll(filePath).ToList();
            if (!listLines.Contains(pathToRemove))
                return;
            int index = listLines.IndexOf(pathToRemove);
            listLines.RemoveAt(index);
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.WriteAllLines(filePath, listLines);
        }
        private static string[] GetAll(string filePath)
        {

            if (!File.Exists(filePath))
                return new string[0];
            else
            {
                return File.ReadLines(filePath).ToArray();
            }
        }
        public static string[] CheckForExternal()
        {
            List<string> result = new List<string>();
            string filePath = Path.Combine(Furniture.Helpers.LocalAccounts.modelPathResult, "linksToCash.txt");
            string[] lines = GetAll(filePath);
            List<string> pathsToRemove = new List<string>();
            foreach (var line in lines)
            {                
                SwDMApplication swDocMgr = SwAddin.GetSwDmApp();
                SwDmDocumentOpenError oe;
                SwDMSearchOption src = swDocMgr.GetSearchOptionObject();
                object brokenRefVar;

                if (!File.Exists(line))
                {
                    pathsToRemove.Add(line);
                    continue;
                }
                var swDoc = (SwDMDocument8)swDocMgr.GetDocument(line,
                                                         SwDmDocumentType.swDmDocumentAssembly,
                                                         true, out oe);
                if (swDoc != null)
                {
                    var varRef = (string[])swDoc.GetAllExternalReferences2(src, out brokenRefVar);
                    if (varRef != null && varRef.Length != 0)
                    {
                        foreach (string o in varRef)
                        {
                            if (o.ToUpper().Contains("_SWLIB_BACKUP"))
                            {
                                result.Add(line);
                                break;
                            }
                        }
                    }
                    else
                    {
                        swDoc.CloseDoc();
                    }
                    swDoc.CloseDoc();
                }
            }
            foreach (var path in pathsToRemove)
            {
                Remove(path);
            }
            return result.ToArray();
        }
    }
}
