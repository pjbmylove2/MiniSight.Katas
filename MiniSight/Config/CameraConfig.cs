using MiniSight.Devices.Cameras;

namespace MiniSight.Config
{
    public sealed class CameraConfig
    {
        public CameraKind Kind { get; set; }
        public string Name { get; set; }
        public string Folder { get; set; }
        public int IntervalMs
        {
            get; set;
        }
    }
}
