namespace Furniture
{
    public class ref_object
    {
        private string _componentName;
        private int _objectsId;
        private string _axe;
        private double _correctionValueLeft;
        private double _correctionValueRight;

        public string ComponentName
        {
            get { return _componentName; }
        }
        public int ObjectId
        {
            get { return _objectsId; }
        }
        public string Axe
        {
            get { return _axe; }
        }
        public double CorrectionValueLeft
        {
            get { return _correctionValueLeft; }
        }
        public double CorrectionValueRight
        {
            get { return _correctionValueRight; }
        }
        public ref_object(string componentName,int objectsId,string axe,double correctionValueLeft,double correctionValueRight)
        {
            _componentName = componentName;
            _objectsId = objectsId;
            _axe = axe;
            _correctionValueLeft = correctionValueLeft;
            _correctionValueRight = correctionValueRight;
        }
    }
}
