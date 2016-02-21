using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using FirebirdSql.Data.FirebirdClient;

namespace WindowsFormsApplication1
{
    public partial class CopyFilesForm : Form
    {
        FbConnection fb;
        private Thread CopyThread = null;

        public CopyFilesForm()
        {
            InitializeComponent();
            saveDialog.SelectedPath = "D:\\1";

            FbConnectionStringBuilder fb_con = new FbConnectionStringBuilder();
            fb_con.Charset = "WIN1251"; //используемая кодировка
            fb_con.UserID = "SYSDBA"; //логин
            fb_con.Password = "masterkey"; //пароль
            fb_con.Database = "C:\\Source\\CopyFilesC-\\db.fdb"; //путь к файлу базы данных
            fb_con.ServerType = 0; //указываем тип сервера (0 - "полноценный Firebird" (classic или super server), 1 - встроенный (embedded))

            fb = new FbConnection(fb_con.ToString());

            if (fb.State == ConnectionState.Closed)
                fb.Open();

            FbTransaction fbt = fb.BeginTransaction(); //стартуем транзакцию; стартовать транзакцию можно только для открытой базы (т.е. мутод Open() уже был вызван ранее, иначе ошибка)

            FbCommand SelectSQL = new FbCommand("SELECT * FROM FILES", fb); //задаем запрос на выборку

            SelectSQL.Transaction = fbt; //необходимо проинициализить транзакцию для объекта SelectSQL
            FbDataReader reader = SelectSQL.ExecuteReader();

            DataTable dt = new DataTable();

            dt.Load(reader);

            dataGrid.DataSource = dt;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            switch (openDialog.ShowDialog())
            {
                case System.Windows.Forms.DialogResult.OK:
                    label1.Text = "";
                    foreach (string filePath in openDialog.FileNames){
                        label1.Text += filePath + ";";
                    };
                    break;
                default:
                    label1.Text = "Выберите файл";
                    break;
            }  
        }

        private void button2_Click(object sender, EventArgs e)
        {   
            switch (saveDialog.ShowDialog())
            {   
                case System.Windows.Forms.DialogResult.OK:
                    label2.Text = saveDialog.SelectedPath;
                    break;
                default:
                    label2.Text = "Выберите каталог сохранения";
                    break;
            };
        }

        Action<ProgressBar, int> SetValue1 = delegate(ProgressBar pb, int percent)
        { pb.Value = percent; };

        private void copyFiles()
        {
            
            for (int i = 0; i < openDialog.FileNames.Count(); i++)
            {
                ThreadPool.QueueUserWorkItem(delegate(object sender)
                {
                ProgressBar pb = (ProgressBar)sender;
                double percents = (100 / openDialog.FileNames.Count()) * (i + 1);
                pb.Invoke(SetValue1, new object[] { pb, Convert.ToInt32(Math.Ceiling(percents)) });
                }, progressBar1);
            
                FileCopy fc = new FileCopy(openDialog.FileNames[i], saveDialog.SelectedPath + "\\" + openDialog.SafeFileNames[i]);
                fc.OnProgressChanged += fc_OnProgressChanged;
                fc.OnComplete += fc_OnComplete;
                fc.Copy();
            }


        }

        void fc_OnComplete()
        {
            MessageBox.Show("Успех!", "Копирование", MessageBoxButtons.OK);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openDialog.FileNames.Count() > 0)
            {
                this.CopyThread = new Thread(new ThreadStart(this.copyFiles));
                this.CopyThread.Start();
            }
            else
            {
                MessageBox.Show("Выберите файлы для копирования", "Ошибка", MessageBoxButtons.OK);
            }   
        }

        void fc_OnProgressChanged(double Persentage)
        {
            ThreadPool.QueueUserWorkItem(delegate(object sender)
            {
                ProgressBar pb = (ProgressBar)sender;
                pb.Invoke(SetValue1, new object[] { pb, Convert.ToInt32(Math.Ceiling(Persentage)) });
            }, progressBar2);
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                CopyThread.Abort();
            }
            catch
            { }
        }

        private void dataGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }



    }
}
