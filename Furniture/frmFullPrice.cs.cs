using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Furniture.Exceptions;
using SolidWorks.Interop.sldworks;

namespace Furniture
{
    public partial class frmFullPrice : Form
    {
        #region Поля и события

        /// <summary>
        /// Структура для отображения ошибок в таблицу
        /// </summary>
        private struct ErrorInfo
        {
            public string ComponentName { get; set; }
            public string ErrorMessage { get; set; }
        }
        /// <summary>
        /// Состояния интерфейса (ошибки показаны/скрыты)
        /// </summary>
        private enum State { ErrorsHide, ErrorsShowed }
        /// <summary>
        /// Модель всего проекта, по которой будут произведен расчет цены
        /// </summary>
        private readonly ModelDoc2 model;
        /// <summary>
        /// Поле для сбора общей цены
        /// </summary>
        private decimal FullPrice;
        /// <summary>
        /// Коллекция возникающих ошибок при расчете компонентов модели
        /// </summary>        
        private Collection<ErrorInfo> errors;
        /// <summary>
        /// Текущее состояние интерфейса
        /// </summary>
        private State CurrentState;
        /// <summary>
        /// Таймер для отсчета времени выполнения операции
        /// </summary>
        private Stopwatch stopWatch;
        /// <summary>
        /// Событие завершения вычисления
        /// </summary>
        private event EventHandler CalculatingCompleted;
        /// <summary>
        /// Обработчик события завершения вычисления цены
        /// </summary>
        private void OnCalculatingCompleted()
        {
            if (CalculatingCompleted != null)
                CalculatingCompleted(this, null);
        }

        #endregion

        public frmFullPrice(ModelDoc2 model)
        {
            InitializeComponent();

            CurrentState = State.ErrorsHide;
            this.model = model;
        }

        /// <summary>
        /// Начинает вычисление при загрузке формы
        /// </summary>
        private void frmFullPrice_Load(object sender, EventArgs e)
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();

            Thread timerThread = new Thread(timerTicker);
            Thread CalculatingThread = new Thread(calculatingDel);

            this.FormClosing += ((s, a) =>
            {
                if (CalculatingThread.IsAlive)
                    CalculatingThread.Abort();
                if (timerThread.IsAlive)
                    timerThread.Abort();
            });

            lblPrice.Text = "Вычисление...";

            timerThread.Start();
            CalculatingThread.Start();
        }

        /// <summary>
        /// Метод, обновляющий время в интерфейсе. Работает в потоке обновления интерфейса
        /// </summary>
        private void timerTicker()
        {
            bool flag = true;
            CalculatingCompleted += ((s, a) => { flag = false; });

            while (flag)
            {
                changeView(new Action(() =>
                    {
                        string time;
                        TimeSpan ts = stopWatch.Elapsed;

                        if (ts.TotalMinutes > 1)
                            time = string.Format("{0} мин {1} сек", ts.Minutes, ts.Seconds);
                        else
                            time = string.Format("{0} сек", ts.Seconds);

                        lblTime.Text = "Прошло времени: " + time;
                    }));
                Thread.Sleep(100);
            }

            changeView(new Action(() =>
            {
                lblTime.Text = lblTime.Text.Replace("Прошло времени:", "Расчет завершен за");
            }));
        }

        /// <summary>
        /// Метод, вычисляющий цену. Работает в потоке вычисления
        /// </summary>
        private void calculatingDel()
        {
            changeView(new Action(() =>
                {
                    pBar.Style = ProgressBarStyle.Marquee;
                    //pBar.Maximum = ((object[])((AssemblyDoc)model).GetComponents(false)).Count() - 4;
                    //pBar.Step = 1;
                }));

            FullPrice = 0;
            errors = new Collection<ErrorInfo>();

            string path = model.GetPathName();
            Repository.Instance.OrderNumber = Path.GetFileName(Path.GetDirectoryName(path));

            Repository.Instance.GetPriceForOrder(model, addPrice, addError);

            changeView(new Action(() =>
                {
                    dgwErrors.DataSource = errors;

                    lblPrice.Text = string.Format("Цена: {0} р.", FullPrice.ToString());

                    if (errors.Count != 0)
                    {
                        btnShowErrors.Visible = true;
                        lblPrice.Text += " (обнаружены ошибки)";
                    }
                    pBar.Visible = false;
                }));

            OnCalculatingCompleted();
        }

        /// <summary>
        /// Метод для обработки возникающих ошибок
        /// </summary>
        /// <param name="e">Ошибка</param>
        /// <param name="compName">Имя компонента</param>
        private void addError(Exception e, string compName)
        {
            changeView(new Action(() =>
                {
                    //pBar.PerformStep();
                }));

            //Если у модели нет артикула, она погашена или не является ModelDoc2 то сообщение не выводится
            if (e is ArticleNotFoundException || e is InvalidCastException || e is NullReferenceException || e.Message.Contains("Причина: Данный артикул не найден в прайс-листе"))
                return;

            errors.Add(
                new ErrorInfo()
                {
                    ComponentName = compName,
                    ErrorMessage = e.Message
                });
        }

        /// <summary>
        /// Метод для обратоки вычисленной цены компонента
        /// </summary>
        /// <param name="price">Цена</param>
        private void addPrice(decimal price)
        {
            FullPrice += price;

            changeView(new Action(() =>
                {
                    //pBar.PerformStep();
                }));
        }

        /// <summary>
        /// Переключение режимов отображения ошибок
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnShowErrors_Click_1(object sender, EventArgs e)
        {
            if (CurrentState == State.ErrorsHide)
            {
                this.Size = new Size(590, 450);
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                btnShowErrors.Text = "Скрыть ошибки";
                CurrentState = State.ErrorsShowed;
            }
            else
            {
                this.Size = new Size(590, 150);
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                btnShowErrors.Text = "Показать ошибки";
                CurrentState = State.ErrorsHide;
            }
        }

        /// <summary>
        /// Обновление интерфейса
        /// </summary>
        private void changeView(Action action)
        {
            while (!this.InvokeRequired)
                Thread.Sleep(10);

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() =>
                {
                    action();
                }));
            }
            else
                changeView(action);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
