using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace Real_ESRGAN_GUI
{
    class ImageProcess
    {
        public static Bitmap ResizeBitmap(Bitmap image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static Bitmap ConvertBitmapToFormat32bppArgb(Bitmap bitmap)
        {
            Bitmap target = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
            target.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);   // Set both bitmap to same dpi to prevent scaling.
            using (Graphics g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            }
            return target;
        }

        public static void SplitChannel(Bitmap input, out Bitmap rgb, out Bitmap alpha)
        {
            rgb = new Bitmap(input.Width, input.Height, PixelFormat.Format24bppRgb);
            alpha = new Bitmap(input.Width, input.Height, PixelFormat.Format24bppRgb);
            var inputData = input.LockBits(new Rectangle(0, 0, input.Width, input.Height), ImageLockMode.ReadOnly, input.PixelFormat);
            var rgbData = rgb.LockBits(new Rectangle(0, 0, rgb.Width, rgb.Height), ImageLockMode.WriteOnly, rgb.PixelFormat);
            var alphaData = alpha.LockBits(new Rectangle(0, 0, alpha.Width, alpha.Height), ImageLockMode.WriteOnly, alpha.PixelFormat);
            unsafe
            {
                byte* inputPtr = (byte*)inputData.Scan0;
                byte* rgbPtr = (byte*)rgbData.Scan0;
                byte* alphaPtr = (byte*)alphaData.Scan0;
                int y, x;

                for (y = 0; y < input.Height; y++)
                {
                    for (x = 0; x < input.Width; x++)
                    {
                        rgbPtr[y * rgbData.Stride + x * 3 + 0] = inputPtr[y * inputData.Stride + x * 4 + 0];
                        rgbPtr[y * rgbData.Stride + x * 3 + 1] = inputPtr[y * inputData.Stride + x * 4 + 1];
                        rgbPtr[y * rgbData.Stride + x * 3 + 2] = inputPtr[y * inputData.Stride + x * 4 + 2];
                        alphaPtr[y * alphaData.Stride + x * 3 + 0] = inputPtr[y * inputData.Stride + x * 4 + 3];    // Save to B channel.
                    }
                }


                input.UnlockBits(inputData);
                rgb.UnlockBits(rgbData);
                alpha.UnlockBits(alphaData);
            }
        }

        public static Bitmap CombineChannel(Bitmap rgb, Bitmap alpha)
        {
            var output = new Bitmap(rgb.Width, rgb.Height, PixelFormat.Format32bppArgb);
            var outputData = output.LockBits(new Rectangle(0, 0, output.Width, output.Height), ImageLockMode.WriteOnly, output.PixelFormat);
            var rgbData = rgb.LockBits(new Rectangle(0, 0, rgb.Width, rgb.Height), ImageLockMode.ReadOnly, rgb.PixelFormat);
            var alphaData = alpha.LockBits(new Rectangle(0, 0, alpha.Width, alpha.Height), ImageLockMode.ReadOnly, alpha.PixelFormat);
            unsafe
            {
                byte* outputPtr = (byte*)outputData.Scan0;
                byte* rgbPtr = (byte*)rgbData.Scan0;
                byte* alphaPtr = (byte*)alphaData.Scan0;
                int y, x;

                for (y = 0; y < output.Height; y++)
                {
                    for (x = 0; x < output.Width; x++)
                    {
                        outputPtr[y * outputData.Stride + x * 4 + 0] = rgbPtr[y * rgbData.Stride + x * 3 + 0];
                        outputPtr[y * outputData.Stride + x * 4 + 1] = rgbPtr[y * rgbData.Stride + x * 3 + 1];
                        outputPtr[y * outputData.Stride + x * 4 + 2] = rgbPtr[y * rgbData.Stride + x * 3 + 2];
                        outputPtr[y * outputData.Stride + x * 4 + 3] = alphaPtr[y * alphaData.Stride + x * 4 + 0];
                    }
                }


                output.UnlockBits(outputData);
                rgb.UnlockBits(rgbData);
                alpha.UnlockBits(alphaData);
            }
            return output;
        }
    }
}
