using MiniSight.Config;
using MiniSight.Devices;
using MiniSight.Devices.Cameras;

namespace MiniSight.App
{
    public static class Bootstrapper
    {
        public static IImageSource BuildCamera(string configPath)
        {
            ConfigStore.Instance.Load(configPath);
            CameraConfig config = ConfigStore.Instance.Current;

            return CameraFactory.Create(config.Kind, config.Name, config.Folder, config.IntervalMs);
        }
    }
}
