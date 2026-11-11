using MiniSight.Devices;
using System;
using System.Threading;

namespace MiniSight.App
{
    public enum VisionSystemState { Created, Initializing, Ready, Faulted, Disposed }
    public enum VisionMode { Manual, Auto }
    public enum VisionStatus { Stop, Running }

    public sealed class StateChangedEventArgs : EventArgs
    {
        public StateChangedEventArgs(VisionSystemState state, VisionMode mode, VisionStatus status)
        {
            State = state;
            Mode = mode;
            Status = status;
        }

        public VisionSystemState State { get; private set; }
        public VisionMode Mode { get; private set; }
        public VisionStatus Status { get; private set; }    
    }

    public sealed class VisionSystem : IDisposable
    {
        private readonly DeviceManager _devices;
        private readonly object _sync = new object();
        
        public VisionSystem(DeviceManager devices)
        {
            _devices = devices ?? throw new ArgumentNullException(nameof(devices));
            State = VisionSystemState.Created;
            Mode = VisionMode.Manual;
            Status = VisionStatus.Stop;
        }

        public VisionSystemState State { get; private set; }
        public VisionMode Mode { get; private set; }
        public VisionStatus Status { get; private set; }

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public (bool ok, string message) Initialize(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (State != VisionSystemState.Created)
                {
                    return (false, "생성 상태에서만 준비할 수 있습니다. 현재: " + State);
                }

                ChangeState(VisionSystemState.Initializing, Mode, Status);

                try
                {
                    _devices.Initialize(cancellationToken);
                    ChangeState(VisionSystemState.Ready, Mode, Status);
                    return (true, "준비 완료");
                }
                catch (Exception ex)
                {
                    ChangeState(VisionSystemState.Faulted, Mode, Status);
                    return (false, "준비 실패: " + ex.Message);
                }
            }
        }

        public (bool ok, string message) Start(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (State != VisionSystemState.Ready)
                {
                    return (false, "준비 상태에서만 시작할 수 있습니다. 현재: " + State);
                }

                if (Status == VisionStatus.Running)
                {
                    return (false, "이미 운전 중입니다.");
                }

                try
                {
                    _devices.Start(cancellationToken);
                    ChangeState(VisionSystemState.Ready, VisionMode.Auto, VisionStatus.Running);
                    return (true, "운전 시작");
                }
                catch (Exception ex)
                {
                    return (false, "시작 실패: " + ex.Message);
                }
            }
        }

        public (bool ok, string message) Stop()
        {
            lock (_sync)
            {
                if (Status != VisionStatus.Running)
                {
                    return (false, "운전 중이 아닙니다.");
                }

                try
                {
                    _devices.Stop();
                    ChangeState(VisionSystemState.Ready, VisionMode.Manual, VisionStatus.Stop);
                    return (true, "운전 정지");
                }
                catch (Exception ex)
                {
                    ChangeState(VisionSystemState.Faulted, VisionMode.Manual, VisionStatus.Stop);
                    return (false, "정지 실패: " + ex.Message);
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (State == VisionSystemState.Disposed)
                {
                    return;
                }

                _devices.Dispose();
                ChangeState(VisionSystemState.Disposed, VisionMode.Manual, VisionStatus.Stop);
            }
        }

        private void ChangeState(VisionSystemState state, VisionMode mode, VisionStatus status)
        {
            VisionSystemState previous = State;
            State = state;
            Mode = mode;
            Status = status;
            Console.WriteLine("[시스템] " + previous + " -> " + state + "/" + mode + "/" + status);

            EventHandler<StateChangedEventArgs> handlers = StateChanged;
            if (handlers == null)
            {
                return;
            }

            StateChangedEventArgs args = new StateChangedEventArgs(state, mode, status);
            foreach (EventHandler<StateChangedEventArgs> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(this, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[시스템] 구독자 오류(무시하고 계속): " + ex.Message);
                }
            }
        }
    }


}
