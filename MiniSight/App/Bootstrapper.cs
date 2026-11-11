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

        public static VisionSystem BuildSystem(string configPath)
        {
            IImageSource camera = BuildCamera(configPath);
            IDevice light = new TestDevice("조명");
            DeviceManager devices = new DeviceManager(new IDevice[] {light, camera});
            return new VisionSystem(devices);
        }
    }
}
