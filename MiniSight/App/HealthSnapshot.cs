using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSight.App
{
    public sealed class HealthSnapshot
    {
        public HealthSnapshot(DateTime time, long workingSetMb, long privateMb, int threads, int handles) 
        { 
            Time = time;
            WorkingSetMb = workingSetMb;
            PrivateMb = privateMb;
            Threads = threads;
            Handles = handles;
        }

        public DateTime Time { get; }
        public long WorkingSetMb { get; }
        public long PrivateMb { get; }
        public int Threads { get; }
        public int Handles { get; }

        public override string ToString()
        {
            return Time.ToString("HH:mm:ss") + " 메모리 " + WorkingSetMb + "MB / 전용 " + PrivateMb
                + "MB / 스레드 " + Threads + "개 / 핸들 " + Handles + "개";
        }
    }

        
    
    }
