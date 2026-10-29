using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MiniSight.Devices.Cameras
{
    public sealed class FolderCamera : DeviceBase, IImageSource
    {
        private readonly string _name;
        private readonly string _folder;
        private readonly int _intervalMs;
        private readonly BlockingCollection<ImageFrame> _queue = new BlockingCollection<ImageFrame>(8);
        private string[] _files;
        private long _frameId;
        private Timer _timer;

        public event EventHandler<ImageFrame> FrameCaptured;

        public FolderCamera(string name, string folder, int intervalMs)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));
            if (intervalMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMs));
            }
            _intervalMs = intervalMs;
        }

        public override string Name => _name;

        // 연결 = 폴더 확인 + 사진 목록 읽기
        protected override void OnInitialize(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(_folder))
            {
                throw new DirectoryNotFoundException("폴더가 없습니다: " + _folder);
            }

            string[] files = Directory.GetFiles(_folder, "*.png");
            if (files.Length == 0)
            {
                throw new FileNotFoundException("PNG 파일이 없습니다: " + _folder);
            }

            Array.Sort(files, StringComparer.OrdinalIgnoreCase); // GetFiles는 순서를 보장 안 함 -> 이름순 정렬
            _files = files;
            Console.WriteLine(" [Folder] " + _folder + " 에서 " + files.Length + "장 찾음");
        }

        protected override void OnStart(CancellationToken cancellationToken)
        {
            if (_timer == null)
            {
                _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
            }
            _timer.Change(0, _intervalMs);
        }

        protected override void OnStop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        protected override void OnDispose()
        {
            _timer?.Dispose();
            _timer = null;
            _queue.CompleteAdding();
            ImageFrame left;
            while (_queue.TryTake(out left)) { left.Dispose(); }
            _queue.Dispose();
        }

        public Task<ImageFrame> WaitFrameAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => _queue.Take(cancellationToken), cancellationToken);
        }

        private void OnTick(object state) 
        {
            Mat mat = null;
            ImageFrame frame = null;

            try
            {
                long id = Interlocked.Increment(ref _frameId);
                string path = _files[(id -1) % _files.Length];
                mat = Cv2.ImRead(path, ImreadModes.Grayscale);
                if (mat.Empty())
                {
                    throw new IOException("이미지를 읽을 수 없습니다: " + path);
                }

                frame = new ImageFrame(id, _name, mat);
                mat = null;
                Console.WriteLine(" [Folder] #" + id + " " + Path.GetFileName(path));

                try { FrameCaptured?.Invoke(this, frame); }
                catch(Exception ex) { Console.WriteLine(" [" + _name + "] 미리보기 오류: " + ex.Message); }

                if (_queue.TryAdd(frame))
                {
                    frame = null;
                }
                else
                {
                    Console.WriteLine(" [" + _name + "] 큐 가득 참 -> #" + id + " 버림");
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(" [" + _name + "] 프레임 처리 오류: " + ex.Message);
            }
            finally
            {
                frame?.Dispose();
                mat?.Dispose();
            }
        }
    }
}
