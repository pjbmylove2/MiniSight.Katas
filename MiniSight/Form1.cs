using MiniSight.Devices;
using OpenCvSharp;
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

        private void button4_Click(object sender, EventArgs e)
        {
            using (IDevice device = new TestDevice("연습장치"))
            {
                MessageBox.Show("장치: " + device.Name + Environment.NewLine + "상태: " + device.State);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Mat image = new Mat(100, 200, MatType.CV_8UC1, Scalar.All(0));

            using (ImageFrame frame = new ImageFrame(1, "Fake", image))
            {
                string before = "폭: " + frame.Image.Width;

                frame.Dispose();
                frame.Dispose();

                string after = "정리 후 Image는 null인가? " + (frame.Image == null);
                string original = "처음 Mat도 정리됐나? " + image.IsDisposed; 

                MessageBox.Show(before + Environment.NewLine + after + Environment.NewLine + original);
            }
        }
    }
}
