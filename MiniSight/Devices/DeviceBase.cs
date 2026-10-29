using System;
using System.Threading;

namespace MiniSight.Devices
{
    public abstract class DeviceBase : IDevice
    {
        private readonly object _sync = new object(); // initialize 동시 호출 방지용 자물쇠
        private bool _disposed;
        public abstract string Name { get; }
        public DeviceState State { get; private set; }

        protected DeviceBase()
        {
            State = DeviceState.Created;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                Stop();
            }
            finally
            {
                try
                {
                    OnDispose();
                }
                finally
                {
                    _disposed = true;
                    ChangeState(DeviceState.Disposed);
                }
            }
        }

        public void Initialize(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                if (State == DeviceState.Ready || State == DeviceState.Running)
                {
                    return;
                }
                ChangeState(DeviceState.Initializing);
            }

            try
            {
                OnInitialize(cancellationToken);
                ChangeState(DeviceState.Ready);
            }
            catch
            {
                ChangeState(DeviceState.Faulted);
                throw;
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if ((State != DeviceState.Ready) && (State != DeviceState.Stopped))
            {
                throw new InvalidOperationException(Name + " is not ready.");
            }

            try
            {
                OnStart(cancellationToken);
                ChangeState(DeviceState.Running);
            }
            catch
            {
                ChangeState(DeviceState.Faulted);
                throw;
            }
        }

        public void Stop()
        {
            if ((_disposed) || (State == DeviceState.Created) || (State == DeviceState.Disposed))
            {
                return;
            }

            try
            {
                OnStop();
                ChangeState(DeviceState.Stopped);
            }
            catch
            {
                ChangeState(DeviceState.Faulted);
                throw;
            }
        }

        protected abstract void OnInitialize(CancellationToken cancellationToken);
        protected virtual void OnStart(CancellationToken cancellationToken) { }
        protected virtual void OnStop() { }
        protected virtual void OnDispose() { }

        private void ThrowIfDisposed()
        {
            if (_disposed || State == DeviceState.Disposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        private void ChangeState(DeviceState next)
        {
            DeviceState old = State;
            State = next;
            Console.WriteLine(Name + ": " + old + " -> " + next);
        }
    }
}
