using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace SlideShot
{
    internal static class ImageTools
    {
        public const int ThumbnailWidth = 320;

        public static void SaveThumbnail(string fullPath, string thumbPath)
        {
            using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image src = Image.FromStream(fs))
            {
                SaveThumbnail(src, thumbPath);
            }
        }

        public static void SaveThumbnail(Image src, string thumbPath)
        {
            int w = Math.Min(ThumbnailWidth, src.Width);
            int h = Math.Max(1, (int)Math.Round(src.Height * (double)w / src.Width));
            using (Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(src, new Rectangle(0, 0, w, h));
                }
                bmp.Save(thumbPath, ImageFormat.Png);
            }
        }

        public static void SaveJpeg(string pngPath, string jpgPath)
        {
            ImageCodecInfo encoder = null;
            foreach (ImageCodecInfo info in ImageCodecInfo.GetImageEncoders())
            {
                if (info.FormatID == ImageFormat.Jpeg.Guid)
                {
                    encoder = info;
                    break;
                }
            }

            using (FileStream fs = new FileStream(pngPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image src = Image.FromStream(fs))
            using (Bitmap bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height));
                }

                if (encoder == null)
                {
                    bmp.Save(jpgPath, ImageFormat.Jpeg);
                    return;
                }

                using (EncoderParameters p = new EncoderParameters(1))
                {
                    p.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
                    bmp.Save(jpgPath, encoder, p);
                }
            }
        }
    }
}
