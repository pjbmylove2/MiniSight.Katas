using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace PipelineLab
{
    public partial class Form1 : Form
    {
        private Thread _cameraThread;       // 촬영을 맡은 작업자
        private ManualResetEvent _stop;     // 작업자에게 보낼 정지 신호

        private Bitmap _pendingPreview; // UI에 전달할 최신 사진 한 장

        public Form1()
        {
            InitializeComponent();
            SetRunning(false);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (_cameraThread != null)
            {
                return;
            }

            // 시작할 때마다 꺼진 상태의 새 정지 신호를 준비합니다.
            ManualResetEvent stop = new ManualResetEvent(false);
            Thread worker;

            try
            {
                worker = new Thread(() => CaptureLoop(stop));
                worker.Name = "가짜 카메라";
                worker.Start();
            }
            catch (Exception ex)
            {
                stop.Dispose();
                lblStatus.Text = "시작 실패: " + ex.Message;
                return;
            }

            _cameraThread = worker;
            _stop = stop;
            SetRunning(true);
        }

        private void CaptureLoop(ManualResetEvent stop)
        {

            int frameNumber = 0;

            // 500ms가 지나면 한 장 촬영한 것으로 기록합니다.
            while (!stop.WaitOne(500))
            {
                using (Bitmap frame = new Bitmap(160, 120))
                {
                    using (Graphics graphics = Graphics.FromImage(frame))
                    {
                        graphics.Clear(Color.White);
                        graphics.FillEllipse(Brushes.Blue, 45, 25, 70, 70);

                        QueuePreview(frame);
                    }

                    frameNumber++;
                    Color center = frame.GetPixel(80, 60);
                    bool isRed = center.ToArgb() == Color.Red.ToArgb();

                    Debug.WriteLine("촬영 " + frameNumber + " | " +
                        frame.Width + "x" + frame.Height + " | 가운데 빨강: " + isRed);
                }
            }

            Debug.WriteLine("카메라 스레드 종료");
        }

        private void CheckPictureBoxThread()
        {
            Debug.WriteLine("UI 전환 필요: " + picPreview.InvokeRequired);

            try
            {
                Debug.WriteLine("윈도우 핸들: " + picPreview.Handle);
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopCamera();
        }

        private void StopCamera()
        {
            if (_cameraThread == null)
            {
                return;
            }

            _stop.Set();           // 정지 신호 보내기
            _cameraThread.Join();  // 작업자 종료 기다리기

            Interlocked.Exchange(ref _pendingPreview, null)?.Dispose();
            Image displayed = picPreview.Image;
            picPreview.Image = null;
            displayed?.Dispose();

            _stop.Dispose();       // 작업자가 다 쓴 신호 객체 해제

            _cameraThread = null;
            _stop = null;
            SetRunning(false);

            Debug.WriteLine("메인: 정리 완료");
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (!e.Cancel)
            {
                StopCamera(); // 창의 X 버튼도 같은 정지 절차 사용
            }
        }

        private void SetRunning(bool running)
        {
            btnStart.Enabled = !running;
            btnStop.Enabled = running;
            lblStatus.Text = running ? "촬영 중" : "정지";
        }

        private void QueuePreview(Bitmap frame)
        {
            Bitmap copy = (Bitmap)frame.Clone();
            Bitmap previous = Interlocked.Exchange(ref _pendingPreview, copy);

            if (previous != null)
            {
                previous.Dispose(); // UI가 아직 가져가지 않은 이전 사진 해제
                return;             // 예약된 UI 작업이 최신 사진을 가져감
            }

            try
            {
                BeginInvoke(new Action(ShowPendingPreview));
            }
            catch (InvalidOperationException ex)
            {
                Interlocked.Exchange(ref _pendingPreview, null)?.Dispose();
                Debug.WriteLine("사진 표시 요청 실패: " + ex.Message);
            }
        }

        private void ShowPendingPreview()
        {
            Bitmap next = Interlocked.Exchange(ref _pendingPreview, null);

            if (next == null)
            {
                return;
            }

            Image previous = picPreview.Image;
            picPreview.Image = next;
            previous?.Dispose();
        }
    }
}
