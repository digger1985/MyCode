using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using System.Text.RegularExpressions;
using Furniture.Exceptions;
using System.IO.Compression;
using System.IO.Packaging;
using SolidWorks.Interop.swconst;
using Ionic.Zip;
using System.Collections.ObjectModel;

namespace Furniture
{
    public class Repository
    {
        private readonly string ConnectionString;
        private OracleConnection Connection;
        private static volatile Repository instance;
        private static object syncRoot = new Object();
        private string _orderNumber;
        private readonly string currentState = null;
        public string OrderNumber
        {
            get { return _orderNumber; }
            set { _orderNumber = value; }
        }

        public static void Flush()
        {
            if (instance != null)
                instance = null;
        }
        public static Repository Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Repository();
                    }
                }
                return instance;
            }
        }

        private Repository()
        {
            try
            {
                currentState = null;
                //посмотреть, есть ли это свойство, есть ли файлик
                if (string.IsNullOrEmpty(Furniture.Helpers.LocalAccounts.connectIniPath))
                {
                    currentState = "Не указан путь к файлу connect.ini";
                    return;
                }
                if (!File.Exists(Furniture.Helpers.LocalAccounts.connectIniPath))
                {
                    currentState = string.Format("Файл connect.ini по пути {0} не существует!", Furniture.Helpers.LocalAccounts.connectIniPath);
                    return;
                }
                if (string.IsNullOrEmpty(Properties.Settings.Default.OraDbLogin))
                {
                    currentState = "Не указан логин для связи с БД";
                    return;
                }
                if (string.IsNullOrEmpty(Properties.Settings.Default.OraDbPassword))
                {
                    currentState = "Не указан пароль для связи с БД";
                    return;
                }
                //распарсить файлик
                IniParser parser = new IniParser(Furniture.Helpers.LocalAccounts.connectIniPath);
                string test76 = parser.GetSetting("CONNECT", Properties.Settings.Default.ConnectParameterName);
                if (string.IsNullOrEmpty(test76))
                {
                    currentState = "В ini файле нет строки CONNECT: " + Properties.Settings.Default.ConnectParameterName;
                    return;
                }
                string[] arr = test76.Split(':');
                ConnectionString = string.Format("Data Source=(DESCRIPTION="
                                      + "(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1})))"
                                      + "(CONNECT_DATA=(sid={2})));"
                                      + "User Id={3};Password={4};", arr[0], arr[1], arr[2], Properties.Settings.Default.OraDbLogin, Properties.Settings.Default.OraDbPassword);

            }
            catch (Exception e)
            {
                Logging.Log.Instance.Debug(e.Message);
            }
        }

        public Exception ConnectionCheck()
        {
            Exception e = null;
            try
            {
                Connection = new OracleConnection(ConnectionString);
                Connection.Open();
            }
            catch (Exception ex)
            {
                e = ex;
            }
            finally
            {
                if (Connection.State != ConnectionState.Closed)
                    Connection.Close();
            }
            return e;
        }

        #region Расчет цен
        /// <summary>
        /// Вычисляет цену компонента
        /// </summary>
        /// <param name="projectModel">Модель компонента</param>
        /// <param name="nextPrice">Делагат, обрабатывающий поступившую цену очередного компонента</param>          
        /// <param name="nextError">Делагат, обрабатывающий возникающие ошибки</param>
        public void GetPriceForComponent(ModelDoc2 model, Action<decimal> nextPrice, Action<Exception, string> nextError)
        {
            try
            {
                Connection = new OracleConnection(ConnectionString);
                Connection.Open();
                getPriceForComponent(model, true, nextPrice, nextError);
            }
            finally
            {
                Connection.Close();
            }
        }
        /// <summary>
        /// Вычисляет цену компонента
        /// </summary>
        /// <param name="swSelModel">Модель компонента</param>
        /// <returns>Цена</returns>
        public decimal GetPriceForComponent(ModelDoc2 swSelModel)
        {
            decimal price = 0;
            Action<decimal> nextPrice =
                new Action<decimal>((prc) =>
                {
                    price += prc;
                });
            Action<Exception, string> nextError = new Action<Exception, string>((e, s) => { });
            GetPriceForComponent(swSelModel, nextPrice, nextError);
            return price;
        }
        /// <summary>
        /// Вычисляет цену компонента
        /// </summary>
        /// <param name="swSelModel">Модель компонента</param>
        /// <param name="errors">Ошибки</param>
        /// <returns>Цена</returns>
        public decimal GetPriceForComponent(ModelDoc2 swSelModel, List<string> errors)
        {
            decimal price = 0;
            errors.Clear();

            Action<decimal> nextPrice =
                new Action<decimal>((prc) =>
                {
                    price += prc;
                });

            Action<Exception, string> nextError =
                new Action<Exception, string>((e, s) =>
                {
                    errors.Add(s + ": " + e.Message);
                });

            GetPriceForComponent(swSelModel, nextPrice, nextError);
            return price;
        }

        /// <summary>
        /// Вычисляет цену заказа
        /// </summary>
        /// <param name="projectModel">Модель заказа</param>
        /// <param name="nextPrice">Делагат, обрабатывающий поступившую цену очередного компонента</param>          
        /// <param name="nextError">Делагат, обрабатывающий возникающие ошибки</param>
        public void GetPriceForOrder(ModelDoc2 model, Action<decimal> nextPrice, Action<Exception, string> nextError)
        {
            try
            {
                Connection = new OracleConnection(ConnectionString);
                Connection.Open();
                var components = (object[])((AssemblyDoc)model).GetComponents(true);
                foreach (var component in components)
                {
                    var comp = (Component2)component;
                    ModelDoc2 _model = comp.IGetModelDoc();
                    if (_model != null)
                    {
                        getPriceForComponent(_model, true, nextPrice, nextError);
                    }
                    else
                        nextPrice(0);
                }
            }
            finally
            {
                Connection.Close();
            }
        }


        private void getPriceForComponent(ModelDoc2 model, bool ParentCalcStruct, Action<decimal> nextPrice, Action<Exception, string> nextError)
        {
            string currentConfig = model.IGetActiveConfiguration().Name;

            if (currentConfig.ToLower().Contains("по умолчани") || currentConfig.ToLower().Contains("default"))
                currentConfig = string.Empty;

            string isProduct = model.GetCustomInfoValue(currentConfig, "IsProduct");
            if (string.IsNullOrEmpty(isProduct))
            {
                isProduct = model.GetCustomInfoValue(string.Empty, "IsProduct");
                if (string.IsNullOrEmpty(isProduct))
                    isProduct = "No";
            }

            if (isProduct == "Yes")
            {
                //изделие

                //Проверка существования артикула для компонента в случает отсутствия, расчет цены на сам компонент не производится 
                string articul = model.GetCustomInfoValue(string.Empty, "Articul");
                if (string.IsNullOrEmpty(articul))
                    articul = model.GetCustomInfoValue(currentConfig, "Articul");

                if (!string.IsNullOrEmpty(articul))
                {
                    try
                    {
                        nextPrice(сalculatePriceForModel(model));
                    }
                    catch (Exception e)
                    {
                        if (checkError(e))
                            nextError(e, Path.GetFileName(model.GetPathName()));
                    }
                }

                string s = model.GetPathName();
                AssemblyDoc model_assembly = model as AssemblyDoc;
                if (model_assembly != null)
                {
                    bool calcStruct = true;
                    if (!string.IsNullOrEmpty(articul))
                    {
                        string calcStructStr = model.get_CustomInfo2(string.Empty, "CalcStruct");
                        if (calcStructStr.ToLower() == "no")
                            calcStruct = false;
                    }

                    var components = (object[])((AssemblyDoc)model).GetComponents(true);
                    foreach (var component in components)
                    {
                        var comp = (Component2)component;
                        ModelDoc2 _model = comp.IGetModelDoc();
                        if (_model != null)
                        {
                            getPriceForComponent(_model, calcStruct, nextPrice, nextError);
                        }
                    }
                }
            }
            else
            {
                //не изделие

                string articul = model.GetCustomInfoValue(string.Empty, "Articul");
                if (string.IsNullOrEmpty(articul))
                    articul = model.GetCustomInfoValue(currentConfig, "Articul");

                string isIndependentStr = model.get_CustomInfo2(string.Empty, "IsIndependent");
                bool isIndependent = true;
                if (string.IsNullOrEmpty(isIndependentStr) || isIndependentStr.ToLower() == "no")
                    isIndependent = false;

                if (isIndependent)
                {
                    bool calcStruct = true;
                    if (!string.IsNullOrEmpty(articul))
                    {
                        //независимый и с артикулом
                        try
                        {
                            nextPrice(сalculatePriceForModel(model));
                        }
                        catch (Exception e)
                        {
                            if (checkError(e))
                                nextError(e, Path.GetFileName(model.GetPathName()));
                        }
                        string calcStructStr = model.get_CustomInfo2(string.Empty, "CalcStruct");

                        if (calcStructStr.ToLower() == "no")
                            calcStruct = false;
                    }

                    if (calcStruct)
                    {
                        AssemblyDoc model_assembly = model as AssemblyDoc;
                        if (model_assembly != null)
                        {
                            var components = (object[])((AssemblyDoc)model).GetComponents(true);
                            foreach (var component in components)
                            {
                                var comp = (Component2)component;
                                ModelDoc2 _model = comp.IGetModelDoc();
                                if (_model != null)
                                {
                                    getPriceForComponent(_model, calcStruct, nextPrice, nextError);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //компонент входит в состав изделия
                    if (ParentCalcStruct)
                    {
                        //нужно получить цену 
                        bool calcStruct = true;
                        if (!string.IsNullOrEmpty(articul))
                        {
                            try
                            {
                                nextPrice(сalculatePriceForModel(model));
                            }
                            catch (Exception e)
                            {
                                if (checkError(e))
                                    nextError(e, Path.GetFileName(model.GetPathName()));
                            }
                            string calcStructStr = model.get_CustomInfo2(string.Empty, "CalcStruct");
                            if (calcStructStr.ToLower() == "no")
                                calcStruct = false;
                        }


                        AssemblyDoc model_assembly = model as AssemblyDoc;
                        if (model_assembly != null)
                        {
                            var components = (object[])((AssemblyDoc)model).GetComponents(true);
                            foreach (var component in components)
                            {
                                var comp = (Component2)component;
                                ModelDoc2 _model = comp.IGetModelDoc();
                                if (_model != null)
                                {
                                    getPriceForComponent(_model, calcStruct, nextPrice, nextError);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (model as AssemblyDoc != null)
                        {
                            var components = (object[])((AssemblyDoc)model).GetComponents(true);
                            foreach (var component in components)
                            {
                                var comp = (Component2)component;
                                ModelDoc2 _model = comp.IGetModelDoc();
                                if (_model != null)
                                {
                                    getPriceForComponent(_model, false, nextPrice, nextError);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool checkError(Exception e)
        {
            //Если у модели нет артикула, она погашена или не является ModelDoc2 то сообщение не выводится
            if (e is ArticleNotFoundException || e is InvalidCastException || e is NullReferenceException || e.Message.Contains("Причина: Данный артикул не найден в прайс-листе"))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Возвращает цену компонента из БД
        /// </summary>
        /// <param name="model">Модель компонента</param>
        /// <returns>Цена</returns>
        private decimal сalculatePriceForModel(ModelDoc2 model)
        {
            try
            {
                bool isProduct = false;
                string isProductStr = model.GetCustomInfoValue("", "IsProduct");
                if (!string.IsNullOrEmpty(currentState))
                    throw new NullReferenceException("currentState");

                if (string.IsNullOrEmpty(isProductStr))
                    isProduct = false;
                if (isProductStr.ToLower().Contains("yes"))
                    isProduct = true;
                if (!isProduct)
                    return execQueryNonProduct("SELECT SCENTRE.GETSCICOMPPRICE_SWR(:P_NUMAGREE,:P_IDCOMPONENT,:P_SIZE1,:P_SIZE2,:P_SIZE1WORK,:P_SIZE2WORK,:P_IDCOLOR1,:P_IDCOLOR2,:P_IDCOLOR3,:P_IDFANER11,:P_IDFANER12,:P_IDCOLORFANER11,:P_IDCOLORFANER12,:P_IDFANER21,:P_IDFANER22,:P_IDCOLORFANER21,:P_IDCOLORFANER22,:P_ITPRODCALCPARENT) AS PRICE FROM DUAL", model);
                else
                    return execQueryProduct("SELECT SCENTRE.GETSCIPRODPRICE_SWR(:P_NUMAGREE,:P_IDCOMPONENT,:P_SIZE1,:P_SIZE2,:P_SIZE3,:P_SIZE4,:P_SIZE5,:P_SIZE6,:P_SIZE7,:P_SIZE8,:P_IDCOLOR1,:P_IDCOLOR2,:P_IDCOLOR3,:P_IDCOLOR4,:P_IDCOLOR5) AS PRICE FROM DUAL", model);

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("ORA-01012") || ex.Message.Contains("ORA - 02396"))
                {
                    instance = new Repository();
                }
                throw ex;
            }
        }

        //через открытое соединение
        private decimal execQueryProduct(string sql, ModelDoc2 swSelModel)
        {
            string currentConfig = swSelModel.IGetActiveConfiguration().Name;
            if (currentConfig.ToLower().Contains("по умолчани") || currentConfig.ToLower().Contains("default"))
                currentConfig = string.Empty;
            string articul = swSelModel.GetCustomInfoValue(currentConfig, "Articul");
            if (string.IsNullOrEmpty(articul))
            {
                articul = swSelModel.GetCustomInfoValue(string.Empty, "Articul");
                if (string.IsNullOrEmpty(articul))
                    throw new ArticleNotFoundException("Отсутствует артикул!", swSelModel);
            }
            int size1 = 0, size2 = 0, size3 = 0, size4 = 0, size5 = 0, size6 = 0, size7 = 0, size8 = 0;
            string idColor1 = string.Empty, idColor2 = string.Empty, idColor3 = string.Empty, idColor4 = string.Empty, idColor5 = string.Empty;
            FillForProductQuery(swSelModel, string.Empty, ref size1, ref size2, ref size3, ref size4, ref size5, ref size6, ref size7, ref size8, ref idColor1, ref idColor2, ref idColor3, ref idColor4, ref idColor5);
            if (currentConfig != string.Empty)
                FillForProductQuery(swSelModel, currentConfig, ref size1, ref size2, ref size3, ref size4, ref size5, ref size6, ref size7, ref size8, ref idColor1, ref idColor2, ref idColor3, ref idColor4, ref idColor5);

            using (OracleCommand cmd = new OracleCommand(sql))
            {
                cmd.Connection = Connection;
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("P_NUMAGREE", _orderNumber));//"040714-1255-0101"));
                cmd.Parameters.Add(new OracleParameter("P_IDCOMPONENT", articul));
                cmd.Parameters.Add(new OracleParameter("P_SIZE1", size1));
                cmd.Parameters.Add(new OracleParameter("P_SIZE2", size2));
                cmd.Parameters.Add(new OracleParameter("P_SIZE3", size3));
                cmd.Parameters.Add(new OracleParameter("P_SIZE4", size4));
                cmd.Parameters.Add(new OracleParameter("P_SIZE5", size5));
                cmd.Parameters.Add(new OracleParameter("P_SIZE6", size6));
                cmd.Parameters.Add(new OracleParameter("P_SIZE7", size7));
                cmd.Parameters.Add(new OracleParameter("P_SIZE8", size8));

                cmd.Parameters.Add(new OracleParameter("P_IDCOLOR1", idColor1));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLOR2", idColor2));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLOR3", idColor3));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLOR4", idColor4));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLOR5", idColor5));

                OracleDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    if (reader["PRICE"] is decimal)
                        return (decimal)reader["PRICE"];
                    else
                        throw new Exception("Неправильный результат цены:" + reader["PRICE"]);
                }
                else
                    throw new Exception("Ошибка соединения с БД.");
            }

        }
        private void FillForProductQuery(ModelDoc2 swSelModel, string config, ref int size1, ref int size2, ref int size3, ref int size4, ref int size5, ref int size6, ref int size7, ref int size8, ref string idColor1, ref string idColor2, ref string idColor3, ref string idColor4, ref string idColor5)
        {
            int tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size1"), out tmpInt))
                size1 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size2"), out tmpInt))
                size2 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size3"), out tmpInt))
                size3 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size4"), out tmpInt))
                size4 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size5"), out tmpInt))
                size5 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size6"), out tmpInt))
                size6 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size7"), out tmpInt))
                size7 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size8"), out tmpInt))
                size8 = tmpInt;

            string tmpString = swSelModel.GetCustomInfoValue(config, "Color1");
            if (tmpString.Contains("Color") || tmpString == "-")
                tmpString = string.Empty;
            if (!string.IsNullOrEmpty(tmpString))
                idColor1 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Color2");
            if (tmpString.Contains("Color") || tmpString == "-")
                tmpString = string.Empty;
            if (!string.IsNullOrEmpty(tmpString))
                idColor2 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Color3");
            if (tmpString.Contains("Color") || tmpString == "-")
                tmpString = string.Empty;
            if (!string.IsNullOrEmpty(tmpString))
                idColor3 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Color4");
            if (tmpString.Contains("Color") || tmpString == "-")
                tmpString = string.Empty;
            if (!string.IsNullOrEmpty(tmpString))
                idColor4 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Color5");
            if (tmpString.Contains("Color") || tmpString == "-")
                tmpString = string.Empty;
            if (!string.IsNullOrEmpty(tmpString))
                idColor5 = tmpString;
        }
        private decimal execQueryNonProduct(string sql, ModelDoc2 swSelModel)
        {
            string currentConfig = swSelModel.IGetActiveConfiguration().Name;
            if (currentConfig.ToLower().Contains("по умолчани") || currentConfig.ToLower().Contains("default"))
                currentConfig = string.Empty;
            string articul = swSelModel.GetCustomInfoValue(currentConfig, "Articul");
            if (string.IsNullOrEmpty(articul))
            {
                articul = swSelModel.GetCustomInfoValue(string.Empty, "Articul");
                if (string.IsNullOrEmpty(articul))
                    throw new ArticleNotFoundException("Отсутствует артикул!", swSelModel);
            }
            int size1Work = 0, size2Work = 0, size1 = 0, size2 = 0;
            string idColor1 = string.Empty, idColor2 = string.Empty, idColor3 = string.Empty, faner11 = string.Empty, faner12 = string.Empty, faner21 = string.Empty, faner22 = string.Empty, colorfaner11 = string.Empty, colorfaner12 = string.Empty, colorfaner21 = string.Empty, colorfaner22 = string.Empty;
            FillForNonProductQuery(swSelModel, string.Empty, ref size1, ref size2, ref size1Work, ref size2Work, ref idColor1, ref idColor2, ref idColor3, ref faner11, ref faner12, ref faner21, ref faner22, ref colorfaner11, ref colorfaner12, ref colorfaner21, ref colorfaner22);
            if (currentConfig != string.Empty)
                FillForNonProductQuery(swSelModel, currentConfig, ref size1, ref size2, ref size1Work, ref size2Work, ref idColor1, ref idColor2, ref idColor3, ref faner11, ref faner12, ref faner21, ref faner22, ref colorfaner11, ref colorfaner12, ref colorfaner21, ref colorfaner22);


            using (OracleCommand cmd = new OracleCommand(sql))
            {
                cmd.Connection = Connection;
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("P_NUMAGREE", _orderNumber));//"040714-1255-0101"));
                cmd.Parameters.Add(new OracleParameter("P_IDCOMPONENT", articul));
                cmd.Parameters.Add(new OracleParameter("P_SIZE1", size1));
                cmd.Parameters.Add(new OracleParameter("P_SIZE2", size2));
                var curPrm = new OracleParameter("P_SIZE1WORK", OracleDbType.Int32, ParameterDirection.Input);
                curPrm.Value = size1Work;
                cmd.Parameters.Add(curPrm);
                curPrm = new OracleParameter("P_SIZE2WORK", OracleDbType.Int32, ParameterDirection.Input);
                curPrm.Value = size2Work;
                cmd.Parameters.Add(curPrm);
                cmd.Parameters.Add(new OracleParameter("P_IDCOLOR1", idColor1));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLOR2", idColor2));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLOR3", idColor3));
                cmd.Parameters.Add(new OracleParameter("P_IDFANER11", faner11));
                cmd.Parameters.Add(new OracleParameter("P_IDFANER12", faner12));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLORFANER11", colorfaner11));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLORFANER12", colorfaner12));
                cmd.Parameters.Add(new OracleParameter("P_IDFANER21", faner21));
                cmd.Parameters.Add(new OracleParameter("P_IDFANER22", faner22));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLORFANER21", colorfaner21));
                cmd.Parameters.Add(new OracleParameter("P_IDCOLORFANER22", colorfaner22));
                cmd.Parameters.Add(new OracleParameter("P_ITPRODCALCPARENT", DBNull.Value));
                OracleDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    if (reader["PRICE"] is decimal)
                        return (decimal)reader["PRICE"];
                    else
                        return 0;
                }
                else
                    throw new Exception("Ошибка соединения с БД.");
            }

        }
        private void FillForNonProductQuery(ModelDoc2 swSelModel, string config, ref int size1, ref int size2, ref int size1Work, ref int size2Work, ref string idColor1, ref string idColor2, ref string idColor3, ref string faner11, ref string faner12, ref string faner21, ref string faner22, ref string colorfaner11, ref string colorfaner12, ref string colorfaner21, ref string colorfaner22)
        {
            int tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size1"), out tmpInt))
                size1 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Size2"), out tmpInt))
                size2 = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Part Size1"), out tmpInt))
                size1Work = tmpInt;
            if (int.TryParse(swSelModel.GetCustomInfoValue(config, "Part Size2"), out tmpInt))
                size2Work = tmpInt;

            string tmpString = swSelModel.GetCustomInfoValue(config, "Color1");
            if (tmpString.Contains("Color") || tmpString == "-")
                tmpString = string.Empty;
            if (!string.IsNullOrEmpty(tmpString))
                idColor1 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Color2");
            if (tmpString.Contains("Color") || tmpString == "-")
                tmpString = string.Empty;
            if (!string.IsNullOrEmpty(tmpString))
                idColor2 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Color3");
            if (tmpString.Contains("Color") || tmpString == "-")
                tmpString = string.Empty;
            if (!string.IsNullOrEmpty(tmpString))
                idColor3 = tmpString;


            tmpString = swSelModel.GetCustomInfoValue(config, "Faner11");
            if (!string.IsNullOrEmpty(tmpString))
                faner11 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Faner12");
            if (!string.IsNullOrEmpty(tmpString))
                faner12 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Faner21");
            if (!string.IsNullOrEmpty(tmpString))
                faner21 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "Faner22");
            if (!string.IsNullOrEmpty(tmpString))
                faner22 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "colorFaner11");
            if (!string.IsNullOrEmpty(tmpString))
                colorfaner11 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "colorFaner12");
            if (!string.IsNullOrEmpty(tmpString))
                colorfaner12 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "colorFaner21");
            if (!string.IsNullOrEmpty(tmpString))
                colorfaner21 = tmpString;
            tmpString = swSelModel.GetCustomInfoValue(config, "colorFaner22");
            if (!string.IsNullOrEmpty(tmpString))
                colorfaner22 = tmpString;
        }


        #endregion

        #region Экспорт заказа в БД

        /// <summary>
        /// Структура для хранения информации о считанных свойствах компонента
        /// </summary>
        private struct AttributeInfo
        {
            public string Path { get; set; }
            public string AttributeName { get; set; }
            public string AttributeValue { get; set; }

            public override bool Equals(object o)
            {
                AttributeInfo? other = o as AttributeInfo?;
                if (other != null)
                {
                    return this == other;
                }
                return false;
            }
            public static bool operator ==(AttributeInfo a, AttributeInfo b)
            {
                return (a.Path == b.Path && a.AttributeName == b.AttributeName && a.AttributeValue == b.AttributeValue);
            }
            public static bool operator !=(AttributeInfo a, AttributeInfo b)
            {
                return (a.Path != b.Path || a.AttributeName != b.AttributeName || a.AttributeValue != b.AttributeValue);
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        /// <summary>
        /// Экспорт свойств компонентов в базу данных
        /// </summary>         
        public void AttributesExport(ISldWorks swApp)
        {
            try
            {
                Connection = new OracleConnection(ConnectionString);
                Connection.Open();

                string orderPath = (((ModelDoc2)swApp.ActiveDoc)).GetPathName();
                string orderNum = Path.GetFileName(Path.GetDirectoryName(orderPath));

                //очистка данных по заказу

                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = Connection;
                    cmd.CommandText = "GENERAL.SWR_FILEDATA_CLEAR_SW";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = orderNum;
                    cmd.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';

                    cmd.ExecuteNonQuery();
                }


                //Коллекция уникальных сочетаний компонент-свойство
                //Dictionary<KeyValuePair<string, string>, string> componentsAttributesDictionary = new Dictionary<KeyValuePair<string, string>, string>();
                Collection<AttributeInfo> componentsAttributesCollection = new Collection<AttributeInfo>();

                Action<ModelDoc2> action =
                    new Action<ModelDoc2>((model) =>
                    {
                        string path = model.GetPathName();
                        DocumentSpecification swDocSpecification = (DocumentSpecification)swApp.GetOpenDocSpec(path);
                        int fileType = swDocSpecification.DocumentType;
                        string filePath = path.Substring(0, path.LastIndexOf(@"\") + 1);
                        string fileName = Path.GetFileName(Path.GetFileName(path));

                        string configuration = string.Empty;
                        sendAttributes(model, componentsAttributesCollection, path, orderNum, fileType, filePath, fileName, configuration);
                        configuration = model.IGetActiveConfiguration().Name;
                        sendAttributes(model, componentsAttributesCollection, path, orderNum, fileType, filePath, fileName, configuration);
                    });

                SolidWorksInterop.DoSmthForEachComponent((AssemblyDoc)swApp.ActiveDoc, action);
            }
            finally
            {
                Connection.Close();
            }
        }

        private void sendAttributes(ModelDoc2 model, Collection<AttributeInfo> componentsAttributesCollection, string path, string orderNumber, int fileType, string filePath, string fileName, string configuration)
        {
            string[] attributeNames = model.GetCustomInfoNames2(configuration);

            foreach (string attributeName in attributeNames)
            {
                string attributeValue = model.GetCustomInfoValue(configuration, attributeName);

                //Проверка записано ли свойство такого файла
                AttributeInfo attributeInfo = new AttributeInfo { Path = path, AttributeName = attributeName, AttributeValue = attributeValue };

                KeyValuePair<string, string> componentAttribute = new KeyValuePair<string, string>(path, attributeName);
                if (!componentsAttributesCollection.Contains(attributeInfo))
                {
                    componentsAttributesCollection.Add(attributeInfo);

                    //конвертация булевых атрибутов в формат БД
                    if (model.GetCustomInfoType3(configuration, attributeName) == 11)
                        attributeValue = (attributeValue == "Yes") ? "T" : "F";

                    //пропуск свойств без значений
                    if (!string.IsNullOrEmpty(attributeValue))

                        using (OracleCommand cmd = new OracleCommand())
                        {
                            cmd.Connection = Connection;
                            cmd.CommandText = "GENERAL.SWR_FILEDATA_ADD_SW";
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = orderNumber;
                            cmd.Parameters.Add("P_CODEFILETYPE", OracleDbType.Int32).Value = fileType;
                            cmd.Parameters.Add("P_FILEPATH", OracleDbType.Varchar2).Value = filePath;
                            cmd.Parameters.Add("P_FILENAME", OracleDbType.Varchar2).Value = fileName;
                            cmd.Parameters.Add("P_IDFILEATTRIBUTE", OracleDbType.Varchar2).Value = attributeName;
                            cmd.Parameters.Add("P_ATTRIBUTEVALUE", OracleDbType.Varchar2).Value = attributeValue;
                            cmd.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';
                            cmd.ExecuteNonQuery();
                        }

                }
                else
                {
                }
            }
        }

        /// <summary>
        /// Определяет наличие состава заказа в БД
        /// </summary>        
        public bool IsOrderEmpty(ISldWorks swApp)
        {
            string orderPath = (((ModelDoc2)swApp.ActiveDoc)).GetPathName();
            string orderNum = Path.GetFileName(Path.GetDirectoryName(orderPath));

            OracleConnection connection;
            using (connection = new OracleConnection(ConnectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.CommandText = "SELECT GENERAL.SWR_ISEMPTYAGREE(:P_NUMAGREE) ISEMP FROM DUAL";
                    cmd.CommandType = CommandType.Text;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = orderNum;

                    OracleDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var result = reader["ISEMP"];
                        if (result.ToString() == "0")
                            return false;
                        else
                            return true;
                    }
                    else
                        throw new Exception("Ошибка соединения с БД.");
                }
            }
        }

        public void ClearOrder()
        {
            OracleConnection connection;
            using (connection = new OracleConnection(ConnectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.CommandText = "GENERAL.SWR_AGREE_CLEAR_SW";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = OrderNumber;
                    cmd.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RegisterStart(bool add, string orderPath, DateTime startTime)
        {
            OracleConnection connection;
            using (connection = new OracleConnection(ConnectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.CommandText = "GENERAL.SWR_FILEDATA_LOADLOG_ADD_SW";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = OrderNumber;
                    cmd.Parameters.Add("P_FILEPATH", OracleDbType.Varchar2).Value = orderPath;
                    cmd.Parameters.Add("P_DATESTART", OracleDbType.Date).Value = startTime;
                    cmd.Parameters.Add("P_OPTIONAGREE", OracleDbType.Varchar2).Value = add ? "ADD" : "REPLACE";
                    cmd.Parameters.Add("P_OPTIONSKETCH", OracleDbType.Varchar2).Value = "Yes";
                    cmd.Parameters.Add("P_OPTIONXML", OracleDbType.Varchar2).Value = "Yes";
                    cmd.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void OptimizeOrderData()
        {
            OracleConnection connection;
            using (connection = new OracleConnection(ConnectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.CommandText = "GENERAL.SWR_FILEDATA_OPTIMIZE_SW";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = OrderNumber;
                    cmd.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<string[]> CheckOrderData()
        {
            List<string[]> errorList = new List<string[]>();

            OracleConnection connection;
            using (connection = new OracleConnection(ConnectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.CommandText = @"SELECT FILDATERR.FILEPATH,
                                        FILDATERR.FILENAME,
                                        FILDATERR.ERRMSG
                                        FROM TABLE(CAST(GENERAL.SWR_FILEDATA_VALIDATE_SW(:P_NUMAGREE) AS GENERAL.TBL_SWR_FILEDATA_ERRORS)) FILDATERR 
                                        ORDER BY FILDATERR.FILEPATH, FILDATERR.FILENAME";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = OrderNumber;

                    OracleDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        string[] error = new string[3];
                        error[0] = reader["FILEPATH"].ToString();
                        error[1] = reader["FILENAME"].ToString();
                        error[2] = reader["ERRMSG"].ToString();

                        errorList.Add(error);
                    }
                }
            }
            return errorList;
        }

        public void ExportSketches(ISldWorks swApp)
        {
            string projectFolderPath = Path.GetDirectoryName(((ModelDoc2)swApp.ActiveDoc).GetPathName());

            OracleConnection connection;
            using (connection = new OracleConnection(ConnectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.CommandText = "GENERAL.SWR_FILEDATA_SKETCHES_PRE_SW";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = OrderNumber;
                    cmd.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';
                    cmd.ExecuteNonQuery();
                }

                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = connection;

                    string commandText = @"SELECT  SWRFDS.IT,
                                                    SWRFDS.ITAGREEMENT,
                                                    SWRFDS.FILEPATH,
                                                    SWRFDS.FILENAME,
                                                    SWRFDS.IDSKETCH,
                                                    SWRFDS.FILENAME_DRW,
                                                    SWRFDS.FILENAME_DWG,
                                                    SWRFDS.FILENAME_PDF,
                                                    SWRFDS.FILENAME_XML,
                                                    SWRFDS.DWG_SKETCH,
                                                    SWRFDS.PDF_SKETCH,
                                                    SWRFDS.XML_SKETCH,
                                                    SWRFDS.CODEFILETYPE
                                            FROM GENERAL.VW_SWR_FILEDATA_SKETCHES SWRFDS
                                            WHERE SWRFDS.ITAGREEMENT = SCENTRE.AGR_GETITAGREE(:P_NUMAGREE)";

                    cmd.CommandText = commandText;
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = OrderNumber;

                    OracleDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        int it = (int)reader["IT"];
                        string filePath = reader["FILEPATH"].ToString();
                        string fileName = reader["FILENAME"].ToString();
                        string fileName_DRW = reader["FILENAME_DRW"].ToString();
                        string fileName_DWG = reader["FILENAME_DWG"].ToString();
                        string fileNamePDF = reader["FILENAME_PDF"].ToString();
                        string fileName_XML = reader["FILENAME_XML"].ToString();

                        if (File.Exists(filePath + fileName_DRW))
                        {
                            //открываем файл в солиде
                            int errors = 0;
                            int warnings = 0;
                            ModelDoc2 drwModel = swApp.OpenDoc6(filePath + fileName_DRW, (int)swDocumentTypes_e.swDocDRAWING, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty, errors, warnings);

                            //сохраняем в dwg
                            object exportData = null;
                            string dwgFilePath = filePath + fileName_DWG;
                            drwModel.Extension.SaveAs(dwgFilePath, 0, 0, exportData, errors, warnings);

                            //сохраняем в pdf
                            /*/
                            exportData = swApp.GetExportFileData(1);
                            string pdfFilePath = projectFolderPath + @"\\ЧЕРТЕЖИ_PDF\\" + fileNamePDF;                                                        
                            if (!Directory.Exists(projectFolderPath + @"\\ЧЕРТЕЖИ_PDF\\"))
                                Directory.CreateDirectory(projectFolderPath + @"\\ЧЕРТЕЖИ_PDF\\");
                            drwModel.Extension.SaveAs(pdfFilePath, 0, 0, exportData, errors, warnings);
                            /*/

                            //закрываем файл в солиде
                            swApp.QuitDoc(drwModel.GetTitle());

                            //архивируем dwg и pdf
                            string dwgZipFilePath = fileToZip(dwgFilePath);
                            //string pdfZipFilePath = fileToZip(pdfFilePath);                            

                            //отправляем dwg.zip в базу
                            byte[] dwgZipFile = File.ReadAllBytes(dwgZipFilePath);
                            using (OracleCommand cmdSendDwg = new OracleCommand())
                            {
                                cmdSendDwg.Connection = connection;
                                cmdSendDwg.CommandText = "GENERAL.SWR_FILEDATA_SKTDWG_UPD";
                                cmdSendDwg.CommandType = CommandType.StoredProcedure;
                                cmdSendDwg.Parameters.Add("P_IT", OracleDbType.Int32).Value = it;
                                cmdSendDwg.Parameters.Add("P_FILENAME_DWG", OracleDbType.Varchar2).Value = Path.GetFileName(dwgZipFilePath);
                                cmdSendDwg.Parameters.Add("P_DWG_SKETCH", OracleDbType.Blob).Value = dwgZipFile;
                                cmdSendDwg.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';
                                cmdSendDwg.ExecuteNonQuery();
                            }

                            //отправляем pdf.zip в базу      
                            /*/
                            byte[] pdfZipFile = File.ReadAllBytes(pdfZipFilePath);
                            using (OracleCommand cmdSendPdf = new OracleCommand())
                            {
                                cmdSendPdf.Connection = connection;
                                cmdSendPdf.CommandText = "GENERAL.SWR_FILEDATA_SKTPDF_UPD";
                                cmdSendPdf.CommandType = CommandType.StoredProcedure;
                                cmdSendPdf.Parameters.Add("P_IT", OracleDbType.Int32).Value = it;
                                cmdSendPdf.Parameters.Add("P_FILENAME_PDF", OracleDbType.Varchar2).Value = Path.GetFileName(pdfZipFilePath);
                                cmdSendPdf.Parameters.Add("P_PDF_SKETCH", OracleDbType.Blob).Value = pdfZipFile;
                                cmdSendPdf.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';
                                cmdSendPdf.ExecuteNonQuery();
                            }
                            /*/

                            //отправляем xml в базу
                            string xmlFilePath = projectFolderPath + @"\Программы\" + fileName_XML;
                            if (File.Exists(xmlFilePath))
                            {
                                byte[] xmlFile = File.ReadAllBytes(xmlFilePath);
                                using (OracleCommand cmdSendXml = new OracleCommand())
                                {
                                    cmdSendXml.Connection = connection;
                                    cmdSendXml.CommandText = "GENERAL.SWR_FILEDATA_SKTXML_UPD";
                                    cmdSendXml.CommandType = CommandType.StoredProcedure;
                                    cmdSendXml.Parameters.Add("P_IT", OracleDbType.Int32).Value = it;
                                    cmdSendXml.Parameters.Add("P_FILENAME_XML", OracleDbType.Varchar2).Value = Path.GetFileName(xmlFilePath);
                                    cmdSendXml.Parameters.Add("P_XML_SKETCH", OracleDbType.Blob).Value = xmlFile;
                                    cmdSendXml.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';
                                    cmdSendXml.ExecuteNonQuery();
                                }
                            }

                            //удаляем архив dwg.zip и сами файлы dwg
                            File.Delete(dwgFilePath);
                            File.Delete(dwgZipFilePath);

                            //удаляем архив pdf.zip
                            //File.Delete(pdfZipFilePath);                            
                        }
                        else
                            throw new FileNotFoundException("Не найден файл чертежа " + filePath + fileName_DRW);
                    }
                    //удаляем папку ЧЕРТЕЖИ_PDF
                    //Directory.Delete(projectFolderPath + @"\\ЧЕРТЕЖИ_PDF\\");
                }
            }
        }

        private string fileToZip(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            string zipFilePath = Path.ChangeExtension(filePath, "zip");
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(filePath, "");
                zip.Save(zipFilePath);
            }
            return zipFilePath;
        }

        public void CreatePZ()
        {
            OracleConnection connection;
            using (connection = new OracleConnection(ConnectionString))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.CommandText = "GENERAL.SWR_FILEDATA_LOAD_SW";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("P_NUMAGREE", OracleDbType.Varchar2).Value = OrderNumber;
                    cmd.Parameters.Add("P_COMMIT", OracleDbType.Char).Value = 'T';

                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion
    }

    #region IniParser

    public class IniParser
    {
        private Hashtable keyPairs = new Hashtable();
        private String iniFilePath;

        private struct SectionPair
        {
            public String Section;
            public String Key;
        }


        public IniParser(String iniPath)
        {
            TextReader iniFile = null;
            String strLine = null;
            String currentRoot = null;
            String[] keyPair = null;

            iniFilePath = iniPath;

            if (File.Exists(iniPath))
            {
                try
                {
                    iniFile = new StreamReader(iniPath);

                    strLine = iniFile.ReadLine();

                    while (strLine != null)
                    {
                        strLine = strLine.Trim().ToUpper();

                        if (strLine != "")
                        {
                            if (strLine.StartsWith("[") && strLine.EndsWith("]"))
                            {
                                currentRoot = strLine.Substring(1, strLine.Length - 2);
                            }
                            else
                            {
                                keyPair = strLine.Split(new char[] { '=' }, 2);

                                SectionPair sectionPair;
                                String value = null;

                                if (currentRoot == null)
                                    currentRoot = "ROOT";

                                sectionPair.Section = currentRoot;
                                sectionPair.Key = keyPair[0];

                                if (keyPair.Length > 1)
                                    value = keyPair[1];

                                keyPairs.Add(sectionPair, value);
                            }
                        }

                        strLine = iniFile.ReadLine();
                    }

                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (iniFile != null)
                        iniFile.Close();
                }
            }
            else
                throw new FileNotFoundException("Unable to locate " + iniPath);

        }

        public String GetSetting(String sectionName, String settingName)
        {
            SectionPair sectionPair;
            sectionPair.Section = sectionName.ToUpper();
            sectionPair.Key = settingName.ToUpper();

            return (String)keyPairs[sectionPair];
        }

        public String[] EnumSection(String sectionName)
        {
            ArrayList tmpArray = new ArrayList();

            foreach (SectionPair pair in keyPairs.Keys)
            {
                if (pair.Section == sectionName.ToUpper())
                    tmpArray.Add(pair.Key);
            }

            return (String[])tmpArray.ToArray(typeof(String));
        }

        public void AddSetting(String sectionName, String settingName, String settingValue)
        {
            SectionPair sectionPair;
            sectionPair.Section = sectionName.ToUpper();
            sectionPair.Key = settingName.ToUpper();

            if (keyPairs.ContainsKey(sectionPair))
                keyPairs.Remove(sectionPair);

            keyPairs.Add(sectionPair, settingValue);
        }

        public void AddSetting(String sectionName, String settingName)
        {
            AddSetting(sectionName, settingName, null);
        }

        public void DeleteSetting(String sectionName, String settingName)
        {
            SectionPair sectionPair;
            sectionPair.Section = sectionName.ToUpper();
            sectionPair.Key = settingName.ToUpper();

            if (keyPairs.ContainsKey(sectionPair))
                keyPairs.Remove(sectionPair);
        }

        public void SaveSettings(String newFilePath)
        {
            ArrayList sections = new ArrayList();
            String tmpValue = "";
            String strToSave = "";

            foreach (SectionPair sectionPair in keyPairs.Keys)
            {
                if (!sections.Contains(sectionPair.Section))
                    sections.Add(sectionPair.Section);
            }

            foreach (String section in sections)
            {
                strToSave += ("[" + section + "]\r\n");

                foreach (SectionPair sectionPair in keyPairs.Keys)
                {
                    if (sectionPair.Section == section)
                    {
                        tmpValue = (String)keyPairs[sectionPair];

                        if (tmpValue != null)
                            tmpValue = "=" + tmpValue;

                        strToSave += (sectionPair.Key + tmpValue + "\r\n");
                    }
                }

                strToSave += "\r\n";
            }

            try
            {
                TextWriter tw = new StreamWriter(newFilePath);
                tw.Write(strToSave);
                tw.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SaveSettings()
        {
            SaveSettings(iniFilePath);
        }
    }

    #endregion
}
