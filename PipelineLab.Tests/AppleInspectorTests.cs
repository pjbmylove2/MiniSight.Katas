using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

namespace PipelineLab.Tests
{
    [TestClass]
    public class AppleInspectorTests
    {
        [TestMethod]
        public void CountBruisePixels_OneBlackPixel_ReturnsOne()
        {
            using (Bitmap frame = new Bitmap(2, 1))
            {
                frame.SetPixel(0, 0, Color.Red);
                frame.SetPixel(1, 0, Color.Black);

                int actual = AppleInspector.CountBruisePixels(frame);

                Assert.AreEqual(1, actual);
            }
        }

        [TestMethod]
        public void CountBruisePixels_NoBlackPixels_ReturnsZero()
        {
            using (Bitmap frame = new Bitmap(2, 1))
            {
                frame.SetPixel(0, 0, Color.Red);
                frame.SetPixel(1, 0, Color.Red);

                int actual = AppleInspector.CountBruisePixels(frame);

                Assert.AreEqual(0, actual);
            }
        }

        [TestMethod]
        public void CountBruisePixels_BlackPixelOnEachRow_ReturnsTwo()
        {
            using (Bitmap frame = new Bitmap(2, 2))
            {
                frame.SetPixel(0, 0, Color.Red);
                frame.SetPixel(1, 0, Color.Black);
                frame.SetPixel(0, 1, Color.Red);
                frame.SetPixel(1, 1, Color.Black);

                int actual = AppleInspector.CountBruisePixels(frame);

                Assert.AreEqual(2, actual);
            }
        }
    }
}
