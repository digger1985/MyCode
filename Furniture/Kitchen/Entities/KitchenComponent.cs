using SolidWorks.Interop.sldworks;

namespace Furniture
{
    public static partial class Kitchen
    {
        /// <summary>
        /// Возвращает экземпляр класса, представляющего кухонный компонент
        /// </summary>      
        public static KitchenComponent GetKitchenComponent(Component2 component)
        {
            ModelDoc2 model = component.GetModelDoc2();
            CustomPropertyManager propertyManager = model.Extension.get_CustomPropertyManager(string.Empty);
            string kitchenType = string.Empty;
            string resolvedKitchenType = string.Empty;
            propertyManager.Get4("KitchenType", false, out kitchenType, out resolvedKitchenType);

            KitchenComponentTypes.Type kitchentype = KitchenComponentTypes.GetType(kitchenType);

            switch (kitchentype)
            {
                case KitchenComponentTypes.Type.BottomStand:
                    return new BottomStand(component);
                case KitchenComponentTypes.Type.AngleBottomStand:
                    return null;
                case KitchenComponentTypes.Type.TopStand:
                    return null;
                case KitchenComponentTypes.Type.AngleTopStand:
                    return null;
                case KitchenComponentTypes.Type.Column:
                    return null;
                case KitchenComponentTypes.Type.Tabletop:
                    return new Tabletop(component);
                case KitchenComponentTypes.Type.AngleTabletop:
                    return null;
                case KitchenComponentTypes.Type.Plinth:
                    return new Plinth(component);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Представляет абстрактный кухонный компонент
        /// </summary>
        public abstract class KitchenComponent
        {
            public Component2 Component { get; private set; }
            protected KitchenComponent(Component2 component)
            {
                this.Component = component;
            }
            
            /// <summary>
            /// Функция позиционирования
            /// </summary>        
            public abstract bool PositionProcess();
            /// <summary>
            /// Возвращает имя кухонного компонента
            /// </summary>
            public abstract string GetName();
            /// <summary>
            /// Возвращает тип кухонного компонента
            /// </summary>
            public abstract KitchenComponentTypes.Type GetKitchenType();            
        }        
    }
}