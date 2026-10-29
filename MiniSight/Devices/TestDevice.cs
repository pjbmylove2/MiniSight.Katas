using System;
using System.Threading;

namespace MiniSight.Devices
{
    public sealed class TestDevice : DeviceBase
    {
        private readonly string _name;
        public TestDevice(string name)
        {
            _name = name;
        }

        public override string Name { get { return _name; } }
        public bool FailOnStart { get; set; }

        protected override void OnInitialize(CancellationToken cancellationToken)
        {
            Console.WriteLine(" [" + _name + "] 연결");
        }

        protected override void OnStart(CancellationToken cancellationToken)
        {
            if (FailOnStart)
            {
                throw new InvalidOperationException(_name + " 시작 실패(테스트)");
            }
            Console.WriteLine(" [" + _name + "] 촬영 시작");
        }

        protected override void OnStop()
        {
            Console.WriteLine(" [" + _name + "] 촬영 정지");
        }

        protected override void OnDispose()
        {
            Console.WriteLine(" [" + _name + "] 자원 해제");
        }
    }
}
