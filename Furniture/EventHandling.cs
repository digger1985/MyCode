using System.Collections;
using SolidWorks.Interop.sldworks;

namespace Furniture
{
    public class DocumentEventHandler
    {
        protected ISldWorks SwApp;
        protected ModelDoc2 Document;
        protected SwAddin UserAddin;
        protected Hashtable OpenModelViews;

        public DocumentEventHandler(ModelDoc2 modDoc, SwAddin addin)
        { 
            Document = modDoc;
            UserAddin = addin;
            SwApp = UserAddin.SwApp;
            OpenModelViews = new Hashtable();
        }

        virtual public bool AttachEventHandlers()
        {
            return true;
        }

        virtual public bool DetachEventHandlers()
        {
            return true;
        }

        public bool ConnectModelViews()
        {
            var mView = (IModelView)Document.GetFirstModelView();

            while (mView != null)
            {
                if (!OpenModelViews.Contains(mView))
                {
                    var dView = new DocView(mView, this);
                    OpenModelViews.Add(mView, dView);
                }
                mView = (IModelView)mView.GetNext();
            }
            return true;
        }

        public bool DisconnectModelViews()
        {
            //Close events on all currently open docs
            DocView dView;
            int numKeys = OpenModelViews.Count;
            var keys = new object[numKeys];

            //Remove all ModelView event handlers
            OpenModelViews.Keys.CopyTo(keys, 0);
            foreach (ModelView key in keys)
            {
                dView = (DocView)OpenModelViews[key];
                dView.DetachEventHandlers();
                OpenModelViews.Remove(key);
            }
            return true;
        }

        public bool DetachModelViewEventHandler(ModelView mView)
        {
            if (OpenModelViews.Contains(mView))
            {
                OpenModelViews.Remove(mView);
            }
            return true;
        }
    }

    public class PartEventHandler : DocumentEventHandler
    {
        readonly PartDoc _doc;

        public PartEventHandler(ModelDoc2 modDoc, SwAddin addin)
            : base(modDoc, addin)
        {
            _doc = (PartDoc)Document;
        }

        override public bool AttachEventHandlers()
        {
            _doc.DestroyNotify += OnDestroy;
            _doc.NewSelectionNotify += UserAddin.OnNewSelection;
            ConnectModelViews();

            return true;
        }

        override public bool DetachEventHandlers()
        {
            _doc.DestroyNotify -= OnDestroy;
            _doc.NewSelectionNotify -= UserAddin.OnNewSelection;
            DisconnectModelViews();

            UserAddin.DetachModelEventHandler(Document);
            return true;
        }

        public int OnDestroy()
        {
            DetachEventHandlers();
            return 0;
        }
    }

    public class AssemblyEventHandler : DocumentEventHandler
    {
        private readonly AssemblyDoc _doc;
        
        public AssemblyEventHandler(ModelDoc2 modDoc, SwAddin addin)
            : base(modDoc, addin)
        {
            _doc = (AssemblyDoc)Document;
        }

        override public bool AttachEventHandlers()
        {
            _doc.DestroyNotify += OnDestroy;
            _doc.NewSelectionNotify += UserAddin.OnNewSelection;
            _doc.AddItemNotify += UserAddin.OnAddNewItem;
			ConnectModelViews();

            return true;
        }

        override public bool DetachEventHandlers()
        {
            _doc.DestroyNotify -= OnDestroy;
            _doc.NewSelectionNotify -= UserAddin.OnNewSelection;
            _doc.AddItemNotify -= UserAddin.OnAddNewItem;
			DisconnectModelViews();

            UserAddin.DetachModelEventHandler(Document);
            return true;
        }

        //Event Handlers
        public int OnDestroy()
        {
            DetachEventHandlers();
            return 0;
        }
    }
    
    public class DrawingEventHandler : DocumentEventHandler
    {
        private readonly DrawingDoc _doc;
        public DrawingEventHandler(ModelDoc2 modDoc, SwAddin addin)
            : base(modDoc, addin)
        {
            _doc = (DrawingDoc)modDoc;
        }

        override public bool AttachEventHandlers()
        {
            _doc.DestroyNotify += OnDestroy;
            ConnectModelViews();
            return true;
        }

        override public bool DetachEventHandlers()
        {
            _doc.DestroyNotify -= OnDestroy;
            DisconnectModelViews();
            UserAddin.DetachModelEventHandler(Document);
            return true;
        }

        public int OnDestroy()
        {
            DetachEventHandlers();
            return 0;
        }
    }

    public class DocView
    {
        readonly ModelView _mView;
        readonly DocumentEventHandler _parent;

        public DocView(IModelView mv, DocumentEventHandler doc)
        {
            _mView = (ModelView)mv;
            _parent = doc;
        }
        
        public bool DetachEventHandlers()
        {
            _parent.DetachModelViewEventHandler(_mView);
            return true;
        }
    }
}
