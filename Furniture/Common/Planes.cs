using System;

namespace Furniture
{
    /// <summary>
    /// Перечисление всех возможных плоскостей моделей
    /// </summary>
    public static class Planes
    {
        public static Planes.Type[] AllPlanes = 
        {
            Planes.Type.Left,
            Planes.Type.Right,
            Planes.Type.Top,
            Planes.Type.Bottom,
            Planes.Type.Front,
            Planes.Type.Back,
            Planes.Type.swrfLeft,
            Planes.Type.swrfRight,
            Planes.Type.swrfTop,
            Planes.Type.swrfBottom,
            Planes.Type.swrfFront,
            Planes.Type.swrfBack,
            Planes.Type.Undefined
        };

        public enum Type
        {
            Left,
            Right,
            Top,
            Bottom,
            Front,
            Back,
            swrfLeft,
            swrfRight,
            swrfTop,
            swrfBottom,
            swrfFront,
            swrfBack,
            Undefined
        }              

        public static Planes.Type GetType(string typeName)
        {
            switch (typeName)
            {
                case "Левая":
                    return Type.Left;
                case "Правая":
                    return Type.Right;
                case "Верхняя":
                    return Type.Top;
                case "Нижняя":
                    return Type.Bottom;
                case "Передняя":
                    return Type.Front;
                case "Задняя":
                    return Type.Back;
                case "#swrfЛевая":
                    return Type.swrfLeft;
                case "#swrfПравая":
                    return Type.swrfRight;
                case "#swrfВерхняя":
                    return Type.swrfTop;
                case "#swrfНижняя":
                    return Type.swrfBottom;
                case "#swrfПередняя":
                    return Type.swrfFront;
                case "#swrfЗадняя":
                    return Type.swrfBack;
                default:
                    return Type.Undefined;
            } 
        }
        public static string GetName(Planes.Type type)
        {
            switch (type)
            {
                case Type.Left:
                    return "#Левая";
                case Type.Right:
                    return "#Правая";
                case Type.Top:
                    return "#Верхняя";
                case Type.Bottom:
                    return "#Нижняя";
                case Type.Front:
                    return "#Передняя";
                case Type.Back:
                    return "#Задняя";
                case Type.swrfLeft:
                    return "#swrfЛевая";
                case Type.swrfRight:
                    return "#swrfПравая";
                case Type.swrfTop:
                    return "#swrfВерхняя";
                case Type.swrfBottom:
                    return "#swrfНижняя";
                case Type.swrfFront:
                    return "#swrfПередняя";
                case Type.swrfBack:
                    return "#swrfЗадняя";
                default:
                    throw new ArgumentException();
            }
        }
    }
}