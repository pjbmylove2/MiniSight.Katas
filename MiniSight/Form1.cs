using MiniSight.App;
using MiniSight.Config;
using MiniSight.Devices;
using OpenCvSharp;
using System;
using System.IO;
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

        private void button6_Click(object sender, EventArgs e)
        {
            using (IDevice device = new TestDevice("연습 장치"))
            {
                string states = "생성 직후: " + device.State;

                device.Initialize(CancellationToken.None);
                states += Environment.NewLine + "초기화 후: " + device.State;

                device.Start(CancellationToken.None);
                states += Environment.NewLine + "시작 후: " + device.State;

                device.Stop();
                states += Environment.NewLine + "정지 후: " + device.State;

                MessageBox.Show(states);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            using (TestDevice device = new TestDevice("연습 장치"))
            {
                device.Initialize(CancellationToken.None);
                device.FailOnStart = true;

                try
                {
                    device.Start(CancellationToken.None);
                    MessageBox.Show("시작 성공");
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + "현재 상태: " + device.State);
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            TestDevice device = new TestDevice("연습 장치");

            try
            {
                using (device)
                {
                    device.Initialize(CancellationToken.None);
                    device.Start(CancellationToken.None);
                    device.FailOnStop = true;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine
                    + "정리 단계 실행: " + device.CleanupCalled + Environment.NewLine
                    + "최종 상태: " + device.State);
            }
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(Application.StartupPath, "camera-study.json");
            IImageSource camera;

            try
            {
                camera = Bootstrapper.BuildCamera(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("카메라 준비 실패: " + ex.Message);
                return;
            }

            using (camera)
            {
                MessageBox.Show("읽어온 촬영 간격: " + ConfigStore.Instance.Current.IntervalMs + "ms");

                camera.Initialize(CancellationToken.None);
                camera.Start(CancellationToken.None);

                ImageFrame frame = await camera.WaitFrameAsync(CancellationToken.None);
                Mat borrowed = frame.Image;
                string inside;

                using (frame)
                {
                    camera.Stop();
                    inside = "블록 안 이미지 있음: " + (frame.Image != null);
                }

                MessageBox.Show(inside
                    + Environment.NewLine + "블록 밖 이미지 없음: " + (frame.Image == null)
                    + Environment.NewLine + "빌린 Mat 정리됨: " + borrowed.IsDisposed);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            TestDevice light = new TestDevice("조명");
            TestDevice cam1 = new TestDevice("카메라1");
            TestDevice cam2 = new TestDevice("카메라2");
            cam2.FailOnStart = true;                         // 2번째 카메라만 시작 실패

            DeviceManager manager = new DeviceManager(new IDevice[] { light, cam1, cam2 });
            manager.Initialize(CancellationToken.None);
            Console.WriteLine("--- 시작 ---");

            try
            {
                manager.Start(CancellationToken.None);
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("오류 개수: " + ex.InnerExceptions.Count);
                foreach (Exception error in ex.InnerExceptions)
                {
                    Console.WriteLine("  " + error.Message);
                }
            }

            MessageBox.Show("최종: " + light.State + " / " + cam1.State + " / " + cam2.State);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            TestDevice light = new TestDevice("조명");
            TestDevice cam1 = new TestDevice("카메라1");
            cam1.FailOnStop = true;                          // 카메라1만 정지 실패

            DeviceManager manager = new DeviceManager(new IDevice[] { light, cam1 });
            manager.Initialize(CancellationToken.None);
            manager.Start(CancellationToken.None);
            Console.WriteLine("--- 정지 ---");

            try
            {
                manager.Stop();
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("정지 오류 개수: " + ex.InnerExceptions.Count);
            }

            Console.WriteLine("--- 정리 ---");
            manager.Dispose();
            MessageBox.Show("최종: " + light.State + " / " + cam1.State + " / 해제 " + cam1.CleanupCalled);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            DeviceManager devices = new DeviceManager(new IDevice[] { new TestDevice("카메라1") });
            VisionSystem system = new VisionSystem(devices);

            system.StateChanged += (s, args) =>
            {
                throw new InvalidOperationException("화면 갱신 실패");
            };

            system.StateChanged += (s, args) =>
            {
                Console.WriteLine("    로그 수신: " + args.State);
            };

            (bool ok, string message) result = system.Initialize(CancellationToken.None);
            Console.WriteLine("결과: " + result.ok + " / " + result.message);

            result = system.Initialize(CancellationToken.None);
            MessageBox.Show("두 번째: " + result.ok + " / " + result.message);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            throw new InvalidOperationException("카메라 연결 실패(테스트)");   // 글러브 없음
        }

        private void button14_Click(object sender, EventArgs e)
        {
            DeviceManager devices = new DeviceManager(new IDevice[] { new TestDevice("조명"), new TestDevice("카메라1") });
            VisionSystem system = new VisionSystem(devices);

            system.StateChanged += (s, args) =>
            {
                throw new InvalidOperationException("화면 갱신 실패");
            };

            system.StateChanged += (s, args) =>
            {
                Console.WriteLine("    로그 수신: " + args.State);
            };

            (bool ok, string message) result = system.Start(CancellationToken.None);
            Console.WriteLine("① 준비 전 시작: " + result.ok + " / " + result.message);

            system.Initialize(CancellationToken.None);

            result = system.Start(CancellationToken.None);
            Console.WriteLine("② 시작: " + result.ok + " / " + result.message);

            result = system.Start(CancellationToken.None);
            Console.WriteLine("③ 또 시작: " + result.ok + " / " + result.message);

            result = system.Stop();
            Console.WriteLine("④ 정지: " + result.ok + " / " + result.message);

            result = system.Stop();
            MessageBox.Show("⑤ 또 정지: " + result.ok + " / " + result.message);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            TestDevice camera2 = new TestDevice("카메라2");
            camera2.FailOnStart = true;

            DeviceManager devices = new DeviceManager(new IDevice[] { new TestDevice("조명"), new TestDevice("카메라1"), camera2 });
            VisionSystem system = new VisionSystem(devices);

            system.Initialize(CancellationToken.None);
            Console.WriteLine("----- 시작 (카메라2 고장) -----");

            (bool ok, string message) result = system.Start(CancellationToken.None);
            Console.WriteLine("결과: " + result.ok + " / " + result.message);
            Console.WriteLine("시스템: " + system.State + "/" + system.Mode + "/" + system.Status);

            Console.WriteLine("----- 고친 뒤 다시 시작 -----");
            camera2.FailOnStart = false;                                  // 고치기
            result = system.Start(CancellationToken.None);
            MessageBox.Show("다시 시작: " + result.ok + " / " + result.message);
        }
    }
}
