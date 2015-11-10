using System;
using System.Windows.Forms;
using System.IO;
using SolidWorks.Interop.sldworks;


namespace Furniture
{
    public partial class FrmReplaceComponents : Form
    {
        private readonly SwAddin _mSwAddin;
        private readonly ModelDoc2 _swModel;
        private readonly Component2[] _mSelComps;

        public FrmReplaceComponents(SwAddin swAddin, Component2[] selComps)
        {
            InitializeComponent();

            _mSwAddin = swAddin;
            _mSelComps = selComps;
            _swModel = (ModelDoc2)_mSwAddin.SwApp.ActiveDoc;
            
            Show();
        }

        private void BtnOkClick(object sender, EventArgs e)
        {
            bool isRightSelection = false;

            try
            {
                SelectionMgr swSelMgr = _swModel.ISelectionManager;
                if (swSelMgr.GetSelectedObjectCount() == 1)
                {
                        isRightSelection = true;
                        var swComp = swSelMgr.GetSelectedObjectsComponent3(1, 0);
                        
                        Component2 swDbComp;
                        if (_mSwAddin.GetParentLibraryComponent(swComp, out swDbComp))
                        {
                            var swCompModel = (ModelDoc2)swDbComp.GetModelDoc();
                            if (swCompModel != null)
                            {
                                string strModelName = swCompModel.GetPathName();
                                string strNewModelName = Path.GetDirectoryName(strModelName) + "\\" + Path.GetFileNameWithoutExtension(strModelName) + ".SLDDRW";
                                
                                foreach (Component2 comp in _mSelComps)
                                {
                                    string strcompName = Path.GetDirectoryName(comp.GetPathName()) + "\\" + Path.GetFileNameWithoutExtension(comp.GetPathName()) + ".SLDDRW";
                                    if (File.Exists(strcompName) && File.Exists(strNewModelName))
                                        File.SetCreationTime(strNewModelName, File.GetCreationTime(strcompName));
                                    if (comp.Select(false))
                                    {
                                        //На некоторых машинах ReplaceComponents генерирует ошибку
                                        //(но, тем не менее, компонент заменяется)
                                        try
                                        {
                                            ((AssemblyDoc)_swModel).ReplaceComponents(strModelName, "", false, true);
                                        }
                                        catch{}
                                    }
                                }
                                _swModel.EditRebuild3();
                                Close();
                            }
                        }
                 }

                if (!isRightSelection)
                    MessageBox.Show(@"Выберите компонент, которым будет производится замена!", _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, _mSwAddin.MyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            Close();
        }
    }
}