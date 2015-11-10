using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;

namespace Furniture
{

    public class PmpHandler : IPropertyManagerPage2Handler2
    {
        ISldWorks _iSwApp;
        readonly SwAddin _userAddin;

        public PmpHandler(SwAddin addin)
        {
            _userAddin = addin;
            _iSwApp = _userAddin.SwApp;
        }

        //Implement these methods from the interface
        public void AfterClose()
        {
            //This function must contain code, even if it does nothing, to prevent the
            //.NET runtime environment from doing garbage collection at the wrong time.
            int indentSize = System.Diagnostics.Debug.IndentSize;
            System.Diagnostics.Debug.WriteLine(indentSize);
        }

        public void OnCheckboxCheck(int id, bool status)
        {

        }

        public void OnClose(int reason)
        {
            //This function must contain code, even if it does nothing, to prevent the
            //.NET runtime environment from doing garbage collection at the wrong time.
            int indentSize = System.Diagnostics.Debug.IndentSize;
            System.Diagnostics.Debug.WriteLine(indentSize);
        }

        public void OnComboboxEditChanged(int id, string text)
        {

        }

        public int OnActiveXControlCreated(int id, bool status)
        {
            return -1;
        }

        public void OnButtonPress(int id)
        {

        }

        public void OnComboboxSelectionChanged(int id, int item)
        {

        }

        public void OnGroupCheck(int id, bool status)
        {

        }

        public void OnGroupExpand(int id, bool status)
        {

        }

        public bool OnHelp()
        {
            return true;
        }

        public void OnListboxSelectionChanged(int id, int item)
        {

        }

        public bool OnNextPage()
        {
            return true;
        }

        public void OnNumberboxChanged(int id, double val)
        {

        }

        public void OnOptionCheck(int id)
        {

        }

        public bool OnPreviousPage()
        {
            return true;
        }

        public void OnSelectionboxCalloutCreated(int id)
        {

        }

        public void OnSelectionboxCalloutDestroyed(int id)
        {

        }

        public void OnSelectionboxFocusChanged(int id)
        {

        }

        public void OnSelectionboxListChanged(int id, int item)
        {

        }

        public void OnTextboxChanged(int id, string text)
        {

        }
    }
}
