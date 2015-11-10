using System;

namespace Furniture
{
    /// <summary>
    /// Перечисление всех возможных имен кухонных компонентов
    /// </summary>
    public static class KitchenComponentTypes
    {
        public enum Type
        {
            Room,
            BottomStand,
            AngleBottomStand,
            TopStand,
            AngleTopStand,
            Column,
            Tabletop,
            AngleTabletop,
            Plinth,
            Undefined
        }

        public static KitchenComponentTypes.Type GetType(string typeName)
        {
            switch (typeName)
            {
                case "Замер":
                    return Type.Room;
                case "Тумба": //Нижняя тумба
                    return Type.BottomStand;
                case "Нижняя угловая тумба":
                    return Type.AngleBottomStand;
                case "Верхняя тумба":
                    return Type.TopStand;
                case "Верхняя угловая тумба":
                    return Type.AngleTopStand;
                case "Колонка":
                    return Type.Column;
                case "Столешница":
                    return Type.Tabletop;
                case "Столешница левая": //todo: потом удалить
                    return Type.Tabletop;
                case "Угловая столешница":
                    return Type.AngleTabletop;
                case "Цоколь":
                    return Type.Plinth;
                default:
                    return Type.Undefined;
            } 
        }
        public static string GetName(KitchenComponentTypes.Type type)
        {
            switch (type)
            {
                case Type.Room:
                    return "Замер";
                case Type.BottomStand:
                    return "Тумба"; //Нижняя тумба
                case Type.AngleBottomStand:
                    return "Нижняя угловая тумба";
                case Type.TopStand:
                    return "Верхняя тумба";
                case Type.AngleTopStand:
                    return "Верхняя угловая тумба";
                case Type.Column:
                    return "Колонка";
                case Type.Tabletop:
                    return "Столешница";
                case Type.AngleTabletop:
                    return "Угловая столешница";
                case Type.Plinth:
                    return "Цоколь";
                default:
                    throw new ArgumentException();
            }
        }
    }
}