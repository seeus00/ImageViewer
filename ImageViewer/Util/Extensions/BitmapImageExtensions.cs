using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageViewer.Util.Extensions
{
    public static class BitmapImageExtensions
    {
        public static byte[] ConvertToByteArray(this BitmapImage image)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using var memStream = new MemoryStream();
            encoder.Save(memStream);

            return memStream.ToArray();
        }

        public static byte[] ImageSourceToBytes(this Image image)
        {
            byte[] bytes = null;
            var bitmapSource = image.Source as BitmapSource;

            if (bitmapSource != null)
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    bytes = stream.ToArray();
                }
            }

            return bytes;
        }
    }
}
