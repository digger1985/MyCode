using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using System.Threading;
using System.IO;

namespace Furniture
{
    public partial class frmPurchaseExport : Form
    {
        private readonly ISldWorks iSwApp;
        private enum ExportMode { Add, Replace }
        private ExportMode mode;
        private Thread worker;
        private bool sketchesExport = false;

        public frmPurchaseExport(ISldWorks _iSwApp)
        {
            InitializeComponent();
            iSwApp = _iSwApp;

            this.FormClosing += ((s, a) =>
            {
                if (worker != null && worker.IsAlive)
                    worker.Abort();
            });
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Visible = false;
            pBar.Visible = true;

            string path = ((ModelDoc2)iSwApp.ActiveDoc).GetPathName();

            Repository.Instance.OrderNumber = Path.GetFileName(Path.GetDirectoryName(path));

            lstbReport.Items.Add("Проверка заполнения заказа...");
            bool isEmpty = true;
            try
            {
                isEmpty = Repository.Instance.IsOrderEmpty(iSwApp);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось выполнить процедуру проверки заполнения заказа. Ошибка: " + ex.Message);
                return;
            }
            lstbReport.Items.Add("Готово.");

            if (isEmpty)
            {
                //пустой
                mode = ExportMode.Add;
            }
            else
            {
                //заполнен
                frmPurchaseExportMessageBox fpmb = new frmPurchaseExportMessageBox();
                switch (fpmb.ShowDialog())
                {
                    case DialogResult.Yes:
                        lstbReport.Items.Add("Добавление содержимого...");
                        mode = ExportMode.Add;
                        break;
                    case DialogResult.No:
                        lstbReport.Items.Add("Замена содержимого...");
                        mode = ExportMode.Replace;
                        break;
                    case DialogResult.Cancel:
                        lstbReport.Items.Add("Отмена...");
                        this.Close();
                        break;
                }
            }

            worker = new Thread(export);
            worker.Start();
        }

        private void export()
        {
            try
            {


                DateTime dt = DateTime.Now;

                //регистрация начала экспорта
                this.BeginInvoke(new Action(() =>
                {
                    lstbReport.Items.Add("Регистрация начала экспорта...");
                }));
                Repository.Instance.RegisterStart((mode == ExportMode.Add) ? true : false, Path.GetDirectoryName(((ModelDoc2)iSwApp.ActiveDoc).GetPathName()), dt);
                this.BeginInvoke(new Action(() =>
                {
                    lstbReport.Items.Add("Готово.");
                }));

                if (mode == ExportMode.Replace)
                {
                    //очистка заказа    
                    this.BeginInvoke(new Action(() =>
                    {
                        lstbReport.Items.Add("Очистка заказа...");
                    }));
                    Repository.Instance.ClearOrder();
                    this.BeginInvoke(new Action(() =>
                    {
                        lstbReport.Items.Add("Готово.");
                    }));
                }

                //экспорт свойств
                this.BeginInvoke(new Action(() =>
                {
                    lstbReport.Items.Add("Экспорт свойств компонентов...");
                }));

                Repository.Instance.AttributesExport(iSwApp);
                this.BeginInvoke(new Action(() =>
                {
                    lstbReport.Items.Add("Готово.");
                }));

                this.BeginInvoke(new Action(() =>
                {
                    lstbReport.Items.Add("Оптимизация данных заказа...");
                }));

                Repository.Instance.OptimizeOrderData();
                this.BeginInvoke(new Action(() =>
                {
                    lstbReport.Items.Add("Готово.");
                }));

                this.BeginInvoke(new Action(() =>
                {
                    lstbReport.Items.Add("Проверка данных заказа...");
                }));

                List<string[]> errors = Repository.Instance.CheckOrderData();
                if (errors.Count != 0)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        lstbReport.Items.Add("Готово.");
                    }));

                    frmPurchaseExportErrors free = new frmPurchaseExportErrors(errors);
                    free.ShowDialog();
                    this.BeginInvoke(new Action(() =>
                    {
                        lstbReport.Items.Add("Экспорт прерван.");
                    }));

                }
                else
                {
                    bool createPZ = true;
                    if (sketchesExport)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            lstbReport.Items.Add("Готово.");
                            lstbReport.Items.Add("Экспорт эскизов...");
                        }));

                        try
                        {
                            //todo: если экспорт с эскизами 
                            Repository.Instance.ExportSketches(iSwApp);
                        }
                        catch (Exception e)
                        {
                            createPZ = false;
                            MessageBox.Show(e.Message, "Ошибка экспорта эскизов!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            this.BeginInvoke(new Action(() =>
                            {
                                pBar.Visible = false;
                                btnExit.Visible = true;
                                lstbReport.Items.Add("");
                                lstbReport.Items.Add("Экспорт прерван.");
                            }));
                        }
                    }

                    if (createPZ)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            lstbReport.Items.Add("Готово.");
                            lstbReport.Items.Add("Создание ПЗ...");
                        }));

                        try
                        {
                            Repository.Instance.CreatePZ();

                            this.BeginInvoke(new Action(() =>
                            {
                                lstbReport.Items.Add("Готово.");
                            }));

                            this.BeginInvoke(new Action(() =>
                            {
                                pBar.Visible = false;
                                btnExit.Visible = true;
                                lstbReport.Items.Add("");
                                TimeSpan ts = DateTime.Now - dt;
                                lstbReport.Items.Add(string.Format("Экспорт завершен за {0} мин {1} сек", Math.Floor(ts.TotalMinutes), (int)ts.Seconds));
                            }));
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Ошибка создания ПЗ!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            this.BeginInvoke(new Action(() =>
                            {
                                pBar.Visible = false;
                                btnExit.Visible = true;
                                lstbReport.Items.Add("");
                                lstbReport.Items.Add("Экспорт прерван.");
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                    pBar.Visible = false;
                    btnExit.Visible = true;
                    lstbReport.Items.Add("");
                    lstbReport.Items.Add("Экспорт прерван.");
                }));
                return;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmPurchaseExport_Load(object sender, EventArgs e)
        {
            if (MessageBox.Show("Экспортировать эскизы?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                sketchesExport = true;
        }
    }
}
