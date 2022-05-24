using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Real_ESRGAN_GUI
{
    class ImageProcess
    {
        public static Bitmap ResizeAlphaChannel(Bitmap image, int width, int height)
        {
            var origin = image.ToImage<Rgb, Byte>().Resize(width, height, Inter.Cubic);

            float scale = width / image.Width;
            float pushStrength = scale / 4f;
            float pushGradStrength = scale / 2f;
            Bitmap img = origin.ToBitmap();
            // Push multiple times to get sharper lines.
            for (int i = 0; i < 3; i++)
            {
                // Compute Luminance and store it to alpha channel.
                img = Anime4KSharp.ImageProcess.ComputeLuminance(img);
                //img.Save("Luminance.png", ImageFormat.Png);

                // Push (Notice that the alpha channel is pushed with rgb channels).
                Bitmap img2 = Anime4KSharp.ImageProcess.PushColor(img, clamp((int)(pushStrength * 255), 0, 0xFF));
                //img2.Save("Push.png", ImageFormat.Png);
                img.Dispose();
                img = img2;

                // Compute Gradient of Luminance and store it to alpha channel.
                img2 = Anime4KSharp.ImageProcess.ComputeGradient(img);
                //img2.Save("Grad.png", ImageFormat.Png);
                img.Dispose();
                img = img2;

                // Push Gradient
                img2 = Anime4KSharp.ImageProcess.PushGradient(img, clamp((int)(pushGradStrength * 255), 0, 0xFF));
                img.Dispose();
                img = img2;
            }
            return img.ToImage<Rgb, Byte>().ToBitmap();
        }

        public static Bitmap ConvertBitmapToFormat(Bitmap bitmap, PixelFormat format)
        {
            Bitmap target = new Bitmap(bitmap.Width, bitmap.Height, format);
            target.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);   // Set both bitmap to same dpi to prevent scaling.
            using (Graphics g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            }
            return target;
        }

        /// <summary>
        /// Split Format32bppArgb bitmap into two Format24bppRgb bitmap, one contains the RGB channels and one contains the alpha channel in the B channel.
        /// </summary>
        /// <param name="input">Format32bppArgb bitmap</param>
        /// <param name="rgb">Format24bppRgb bitmap containing RGB channels.</param>
        /// <param name="alpha">Format24bppRgb bitmap containint alpha channel of the original image in the B channel.</param>
        public static void SplitChannel(Bitmap input, out Bitmap rgb, out Bitmap alpha)
        {
            if (input.PixelFormat != PixelFormat.Format32bppArgb)
            {
                throw new FormatException();
            }
            rgb = new Bitmap(input.Width, input.Height, PixelFormat.Format24bppRgb);
            alpha = new Bitmap(input.Width, input.Height, PixelFormat.Format24bppRgb);
            var origin=input.ToImage<Rgba, Byte>();
            rgb=origin.Convert<Rgb,Byte>().ToBitmap();
            var alphaImg = origin.CopyBlank().Convert<Rgb, Byte>();
            CvInvoke.MixChannels(origin, alphaImg, new int[] { 3, 2 });
            alpha = alphaImg.ToBitmap<Rgb, Byte>();
        }

        /// <summary>
        /// Combine RGB image and alpha image as one ARGB image.
        /// </summary>
        /// <param name="rgb">Format24bppRgb bitmap containing the RGB channels.</param>
        /// <param name="alpha">Format24bppRgb bitmap containng the alpha channels in the B channel.</param>
        /// <returns></returns>
        public static Bitmap CombineChannel(Bitmap rgb, Bitmap alpha, bool premutiply=true)
        {
            if (rgb.PixelFormat != PixelFormat.Format24bppRgb || alpha.PixelFormat != PixelFormat.Format24bppRgb)
            {
                throw new FormatException();
            }
            var output = rgb.ToImage<Rgba, Byte>();
            CvInvoke.MixChannels(alpha.ToImage<Rgb, Byte>(), output, new int[] { 2, 3 });
            alpha.ToImage<Rgb, Byte>()[1].Save(@"C:\Users\mcope\source\repos\Real-ESRGAN_GUI\testFolder\alpha.png");
            output[3].Save(@"C:\Users\mcope\source\repos\Real-ESRGAN_GUI\testFolder\alpha_.png");
            return output.Convert<Bgra, Byte>().ToBitmap();
        }

        private static int clamp(int i, int min, int max)
        {
            if (i < min)
            {
                i = min;
            }
            else if (i > max)
            {
                i = max;
            }

            return i;
        }
    }
}
