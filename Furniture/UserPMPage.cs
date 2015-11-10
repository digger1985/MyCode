using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Furniture
{
    public class UserPmPage
    {
        //Local Objects
        IPropertyManagerPage2 _swPropertyPage;
        PmpHandler _handler;
        readonly ISldWorks _iSwApp;
        readonly SwAddin _userAddin;

        #region Property Manager Page Controls
        //Groups
        IPropertyManagerPageGroup _group1;
        IPropertyManagerPageGroup _group2;

        //Controls
        IPropertyManagerPageTextbox _textbox1;
        IPropertyManagerPageCheckbox _checkbox1;
        IPropertyManagerPageOption _option1;
        IPropertyManagerPageOption _option2;
        IPropertyManagerPageOption _option3;
        IPropertyManagerPageListbox _list1;

        IPropertyManagerPageSelectionbox _selection1;
        IPropertyManagerPageNumberbox _num1;
        IPropertyManagerPageCombobox _combo1;

        //Control IDs
        public const int Group1Id = 0;
        public const int Group2Id = 1;

        public const int Textbox1Id = 2;
        public const int Checkbox1Id = 3;
        public const int Option1Id = 4;
        public const int Option2Id = 5;
        public const int Option3Id = 6;
        public const int List1Id = 7;

        public const int Selection1Id = 8;
        public const int Num1Id = 9;
        public const int Combo1Id = 10;
        #endregion

        public UserPmPage(SwAddin addin)
        {
            _userAddin = addin;
            _iSwApp = _userAddin.SwApp;
            CreatePropertyManagerPage();
        }


        protected void CreatePropertyManagerPage()
        {
            int errors = -1;
            const int options = (int)swPropertyManagerPageOptions_e.swPropertyManagerOptions_OkayButton |
                                (int)swPropertyManagerPageOptions_e.swPropertyManagerOptions_CancelButton;

            _handler = new PmpHandler(_userAddin);
            _swPropertyPage = (IPropertyManagerPage2)_iSwApp.CreatePropertyManagerPage("Sample PMP", options, _handler, ref errors);
            if (_swPropertyPage != null && errors == (int)swPropertyManagerPageStatus_e.swPropertyManagerPage_Okay)
            {
                try
                {
                    AddControls();
                }
                catch (Exception e)
                {
                    _iSwApp.SendMsgToUser2(e.Message, 0, 0);
                }
            }
        }


        //Controls are displayed on the page top to bottom in the order 
        //in which they are added to the object.
        protected void AddControls()
        {
            //Add the groups
            int options = (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Expanded |
                          (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Visible;

            _group1 = (IPropertyManagerPageGroup)_swPropertyPage.AddGroupBox(Group1Id, "Sample Group 1", options);

            options = (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Checkbox |
                      (int)swAddGroupBoxOptions_e.swGroupBoxOptions_Visible;

            _group2 = (IPropertyManagerPageGroup)_swPropertyPage.AddGroupBox(Group2Id, "Sample Group 2", options);

            //Add the controls to group1

            //textbox1
            short controlType = (int)swPropertyManagerPageControlType_e.swControlType_Textbox;
            short align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _textbox1 = (IPropertyManagerPageTextbox)_group1.AddControl(Textbox1Id, controlType, "Type Here", align, options, "This is an example textbox");

            //checkbox1
            controlType = (int)swPropertyManagerPageControlType_e.swControlType_Checkbox;
            align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _checkbox1 = (IPropertyManagerPageCheckbox)_group1.AddControl(Checkbox1Id, controlType, "Sample Checkbox", align, options, "This is a sample checkbox");

            //option1
            controlType = (int)swPropertyManagerPageControlType_e.swControlType_Option;
            align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _option1 = (IPropertyManagerPageOption)_group1.AddControl(Option1Id, controlType, "Option1", align, options, "Radio Buttons");

            //option2
            controlType = (int)swPropertyManagerPageControlType_e.swControlType_Option;
            align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _option2 = (IPropertyManagerPageOption)_group1.AddControl(Option2Id, controlType, "Option2", align, options, "Radio Buttons");

            //option3
            controlType = (int)swPropertyManagerPageControlType_e.swControlType_Option;
            align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _option3 = (IPropertyManagerPageOption)_group1.AddControl(Option3Id, controlType, "Option3", align, options, "Radio Buttons");

            //list1
            controlType = (int)swPropertyManagerPageControlType_e.swControlType_Listbox;
            align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _list1 = (IPropertyManagerPageListbox)_group1.AddControl(List1Id, controlType, "Sample Listbox", align, options, "List of selectable items");
            if (_list1 != null)
            {
                string[] items = { "One Fish", "Two Fish", "Red Fish", "Blue Fish" };
                _list1.Height = 50;
                _list1.AddItems(items);
            }

            //Add controls to group2
            //selection1
            controlType = (int)swPropertyManagerPageControlType_e.swControlType_Selectionbox;
            align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _selection1 = (IPropertyManagerPageSelectionbox)_group2.AddControl(Selection1Id, controlType, "Sample Selection", align, options, "Displays features selected in main view");
            if (_selection1 != null)
            {
                int[] filter = { (int)swSelectType_e.swSelEDGES, (int)swSelectType_e.swSelVERTICES };
                _selection1.Height = 40;
                _selection1.SetSelectionFilters(filter);
            }

            //num1
            controlType = (int)swPropertyManagerPageControlType_e.swControlType_Numberbox;
            align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _num1 = (IPropertyManagerPageNumberbox)_group2.AddControl(Num1Id, controlType, "Sample Numberbox", align, options, "Allows for numerical input");
            if (_num1 != null)
            {
                _num1.Value = 50.0;
                _num1.SetRange((int)swNumberboxUnitType_e.swNumberBox_UnitlessDouble, 0.0, 100.0, 0.01, true);
            }

            //combo1
            controlType = (int)swPropertyManagerPageControlType_e.swControlType_Combobox;
            align = (int)swPropertyManagerPageControlLeftAlign_e.swControlAlign_LeftEdge;
            options = (int)swAddControlOptions_e.swControlOptions_Enabled |
                      (int)swAddControlOptions_e.swControlOptions_Visible;

            _combo1 = (IPropertyManagerPageCombobox)_group2.AddControl(Combo1Id, controlType, "Sample Combobox", align, options, "Combo list");
            if (_combo1 != null)
            {
                string[] items = { "One Fish", "Two Fish", "Red Fish", "Blue Fish" };
                _combo1.AddItems(items);
                _combo1.Height = 50;

            }
        }

        public void Show()
        {
            _swPropertyPage.Show();
        }
    }
}
