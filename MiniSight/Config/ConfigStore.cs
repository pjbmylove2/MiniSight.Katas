using MiniSight.Devices.Cameras;
using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace MiniSight.Config
{
    public sealed class ConfigStore
    {
        private static readonly Lazy<ConfigStore> _instance = new Lazy<ConfigStore>(() => new ConfigStore());

        public static ConfigStore Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public CameraConfig Current { get; private set; }
        private ConfigStore()
        {
            Current = new CameraConfig();
            Current.Kind = CameraKind.Fake;
            Current.Name = "연습 카메라";
            Current.Folder = null;
            Current.IntervalMs = 500;
        }

        public void Save(string path)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(CameraConfig));

            using (FileStream stream = File.Create(path))
            {
                serializer.WriteObject(stream, Current);
            }
        }

        public void Load(string path)
        {
            if (!File.Exists(path))
            {
                Save(path);
                return;
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(CameraConfig));

            using (FileStream stream = File.OpenRead(path))
            {
                CameraConfig loaded = (CameraConfig)serializer.ReadObject(stream);
                if (loaded == null || loaded.IntervalMs <= 0)
                {
                    throw new InvalidDataException("카메라 설정과 촬영 간격을 확인하세요.");
                }

                Current = loaded;
            }
        }
    }
}
