using Katas.Day02;
using Katas.Day05;
using System;
using System.Collections.Generic;

namespace Katas.Day06
{
    public enum InspectionState
    {
        Created, Ready
    }

    public enum InspectionStatus
    {
        Stop, Running
    }

    public enum InspectionMode
    {
        Manual, Auto
    }

    public sealed class InspectionFacade
    {
        private readonly PowerDisplay _display;
        private readonly Inspector _inspector;

        public InspectionState State { get; private set; }
        public InspectionStatus Status { get; private set; }
        public InspectionMode Mode { get; private set; }    

        public event Action<InspectionState> StateChanged;

        public InspectionFacade(PowerDisplay display, Inspector inspector)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
            State = InspectionState.Created;
            Status = InspectionStatus.Stop;
            Mode = InspectionMode.Manual;
        }

        public void Initialize()
        {
            if (State != InspectionState.Created)
            {
                throw new InvalidOperationException("생성 상태에서만 준비할 수 있습니다.");
            }

            State = InspectionState.Ready;
            NotifyStateChanged(State);
        }

        public void Start()
        {
            if (State != InspectionState.Ready)
            {
                throw new InvalidOperationException("준비가 필요합니다.");
            }

            if (Status != InspectionStatus.Stop)
            {
                throw new InvalidOperationException("이미 실행 중입니다.");
            }

            Status = InspectionStatus.Running;
        }

        public void Stop()
        {
            if (Status == InspectionStatus.Stop)
            {
                return;
            }

            Status = InspectionStatus.Stop;
            Console.WriteLine("정지 처리");
        }

        public void SetMode(InspectionMode mode)
        {
            if (Status != InspectionStatus.Stop)
            {
                throw new InvalidOperationException("정지 중에만 모드를 변경할 수 있습니다.");
            }

            Mode = mode;
        }


        public void Check(int brightness)
        {
            if (State != InspectionState.Ready)
            {
                throw new InvalidOperationException("준비가 필요합니다.");
            }

            _display.Show();
            Console.WriteLine("검사 통과: " + _inspector.Inspect(brightness));
        }

        private void NotifyStateChanged(InspectionState state)
        {
            Action<InspectionState> handlers = StateChanged;

            if (handlers == null)
            {
                return;
            }

            foreach (Action<InspectionState> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(state);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("알림 오류: " + ex.Message);
                }
            }
        }
    }

    public sealed class PracticeDevice
    {
        private readonly string _name;

        public PracticeDevice(string name)
        {
            _name = name;
        }

        public void Start()
        {
            Console.WriteLine(_name + " 시작");

            if (_name == "카메라")
            {
                throw new InvalidOperationException("카메라 시작 실패");
            }
        }

        public void Stop()
        {
            Console.WriteLine(_name + " 정지 시도");

            if (_name == "카메라")
            {
                throw new InvalidOperationException("카메라 정지 실패");
            }

            Console.WriteLine(_name + " 정지 완료");
        }
    }

    public sealed class PracticeDeviceManager
    {
        private readonly PracticeDevice[] _devices;

        public PracticeDeviceManager(PracticeDevice[] devices)
        {
            _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        }

        public void StartAll()
        {
            List<PracticeDevice> attempted = new List<PracticeDevice>();

            try
            {
                foreach (PracticeDevice device in _devices)
                {
                    attempted.Add(device);
                    device.Start();
                }
            }
            catch (Exception startError)
            {
                List<Exception> errors = new List<Exception>();
                errors.Add(startError);

                for (int i = attempted.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        attempted[i].Stop();
                    }
                    catch (Exception stopError)
                    {
                        errors.Add(stopError);
                    }
                }

                throw new AggregateException("장치 시작 및 정리 실패", errors);
            }
        }
    }

    public static class KataDay06
    {
        public static void Run()
        {
            PracticeDevice[] devices =
            {
                new PracticeDevice("조명"),
                new PracticeDevice("카메라"),
                new PracticeDevice("저장 장치"),
            };

            PracticeDeviceManager manager = new PracticeDeviceManager(devices);

            try
            {
                manager.StartAll();
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("오류 개수: " + ex.InnerExceptions.Count);

                foreach (Exception error in ex.InnerExceptions)
                {
                    Console.WriteLine(error.Message);
                }
            }
        }
    }
}
