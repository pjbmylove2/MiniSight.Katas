using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniSight
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            throw new InvalidOperationException("버튼에서 터뜨림");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Thread worker = new Thread(() =>
            {
                throw new InvalidOperationException("일꾼에서 터뜨림");
            });
            worker.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            StartLostTask();
            Thread.Sleep(200);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            MessageBox.Show("버튼3 끝. 프로그램 살아 있음");
        }

        private void StartLostTask()
        {
            Task.Run(() => 
            {
                throw new InvalidOperationException("Task에서 터뜨림");
            });
        }
    }
}
