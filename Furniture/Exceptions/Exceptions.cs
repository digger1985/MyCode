using System;
using SolidWorks.Interop.sldworks;

namespace Furniture.Exceptions
{
    /// <summary>
    /// Не найдено свойство в модели
    /// </summary>
    public class ProperyNotFoundException : Exception
    {      
        /// <summary>
        /// Не найдено свойство в модели
        /// </summary>
        /// <param name="message">Сообщение</param>
        public ProperyNotFoundException(string message)
            :base(message) { }
        /// <summary>
        /// Не найдено свойство в модели
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="model">Модель</param>
        public ProperyNotFoundException(string message, ModelDoc2 model) 
            :base(message)
        {
            Model = model;
        }        
        /// <summary>
        /// Модель, в которой не найдено свойство
        /// </summary>
        public ModelDoc2 Model { get; private set; }
    }

    /// <summary>
    /// Ошибка считывания свойств модели: не указан артикул
    /// </summary>
    public class ArticleNotFoundException : Exception
    {
        /// <summary>
        /// Ошибка считывания свойств модели: не указан артикул
        /// </summary>
        public ArticleNotFoundException() { }
        /// <summary>
        /// Ошибка считывания свойств модели: не указан артикул
        /// </summary>
        /// <param name="message">Сообщение</param>
        public ArticleNotFoundException(string message)
            : base(message) { }
        /// <summary>
        /// Ошибка считывания свойств модели: не указан артикул
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="model">Модель с некорректным артикулом</param>
        public ArticleNotFoundException(string message, ModelDoc2 model) { }
        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public override string Message { get { return "Отсутствует артикул."; } }
    }


}
