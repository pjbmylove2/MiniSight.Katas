using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineLab
{
    public static class AppleInspector
    {
        public static int CountBruisePixels(Bitmap frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            int count = 0;
            int black = Color.Black.ToArgb();

            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    if (frame.GetPixel(x, y).ToArgb() == black)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
