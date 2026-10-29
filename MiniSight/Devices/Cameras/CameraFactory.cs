using System;

namespace MiniSight.Devices.Cameras
{
    public enum CameraKind { Fake, Folder }

    public static class CameraFactory
    {
        public static IImageSource Create(CameraKind kind, string name, string folderOrNull, int intervalMs)
        {
            switch (kind)
            {
                case CameraKind.Fake:
                    // 람다가 intervalMs를 '기억'한다(클로저). 재연결할 때마다 같은 설정으로 새 드라이버를 만든다
                    return new DriverCameraAdapter(name, () => new FakeCameraDriver(intervalMs));
                case CameraKind.Folder:
                    return new FolderCamera(name, folderOrNull, intervalMs);
                default:
                    throw new NotSupportedException("지원하지 않는 카메라 종류: " + kind);
            }
        }
    }
}