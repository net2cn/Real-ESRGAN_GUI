using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Anime4KSharp
{
    public sealed class ImageProcess
    {
        public static unsafe Bitmap ComputeLuminance(Bitmap origBitmap)
        {
            Bitmap newBitmap = new Bitmap(origBitmap.Width, origBitmap.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newBitmap))
                g.DrawImage(origBitmap, 0, 0, origBitmap.Width, origBitmap.Height);

            BitmapData data = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height), ImageLockMode.ReadWrite, newBitmap.PixelFormat);
            // This can be done in-place.
            int w = newBitmap.Width - 1;
            Parallel.For(0, newBitmap.Height - 1, y =>
            {
                int* scanline = GetScanline(data, y);
                for (int x = 0; x < w; x++, scanline++)
                {
                    Color pixel = GetPixel(scanline);
                    float lum = pixel.GetBrightness();
                    byte castedLum = clamp(Convert.ToByte(lum * 255), 0, 0xFF);
                    *(((byte*)scanline) + 3) = castedLum;
                }
            });
            newBitmap.UnlockBits(data);
            return newBitmap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Color GetPixel(int* pixel)
        {
            return Color.FromArgb(*pixel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Color GetPixel(byte* scan0, int stride, int x, int y)
        {
            return Color.FromArgb(*((int*)(scan0 + (stride * y)) + x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SetPixel(byte* scan0, int stride, int x, int y, Color color)
        {
            *((int*)(scan0 + (stride * y)) + x) = color.ToArgb();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SetPixel(byte* scan0, Color color)
        {
            *(int*)scan0 = color.ToArgb();
        }

        private static unsafe int* GetScanline(BitmapData data, int y)
        {
            return (int*)(((byte*)data.Scan0) + (y * data.Stride));
        }

        public static unsafe Bitmap PushColor(Bitmap oldBitmap, int strength)
        {
            // Push color based on luminance.
            Rectangle entireRect = new Rectangle(0, 0, oldBitmap.Width, oldBitmap.Height);
            Bitmap newBitmap = new Bitmap(oldBitmap.Width, oldBitmap.Height, PixelFormat.Format32bppArgb);
            Color zero = oldBitmap.GetPixel(0, 0);
            BitmapData newData = newBitmap.LockBits(entireRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData oldData = oldBitmap.LockBits(entireRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int h = oldBitmap.Height - 1;
            int w = oldBitmap.Width - 1;
            byte* oldScan0 = (byte*)oldData.Scan0;
            byte* newScan0 = (byte*)newData.Scan0;
            Parallel.For(0, h, y =>
            {
                int* scanline = GetScanline(newData, y);
                for (int x = 0; x < w; x++, scanline++)
                {
                    //Default translation constants
                    int xn = -1;
                    int xp = 1;
                    int yn = -1;
                    int yp = 1;

                    //If x or y is on the border, don't move out of bounds
                    if (x == 0)
                    {
                        xn = 0;
                    }
                    else if (x == w)
                    {
                        xp = 0;
                    }
                    if (y == 0)
                    {
                        yn = 0;
                    }
                    else if (y == h)
                    {
                        yp = 0;
                    }

                    /*
                     * Kernel defination:
                     * --------------
                     * [tl] [tc] [tr]
                     * [ml] [mc] [mc]
                     * [bl] [bc] [br]
                     * --------------
                     */

                    //Top column
                    var tl = GetPixel(oldScan0, oldData.Stride, x + xn, y + yn);
                    var tc = GetPixel(oldScan0, oldData.Stride, x, y + yn);
                    var tr = GetPixel(oldScan0, oldData.Stride, x + xp, y + yn);

                    //Middle column
                    var ml = GetPixel(oldScan0, oldData.Stride, x + xn, y);
                    var mc = GetPixel(oldScan0, oldData.Stride, x, y);
                    var mr = GetPixel(oldScan0, oldData.Stride, x + xp, y);

                    //Bottom column
                    var bl = GetPixel(oldScan0, oldData.Stride, x + xn, y + yp);
                    var bc = GetPixel(oldScan0, oldData.Stride, x, y + yp);
                    var br = GetPixel(oldScan0, oldData.Stride, x + xp, y + yp);

                    var lightestColor = mc;

                    //Kernel 0 and 4
                    float maxDark = max3(br, bc, bl);
                    float minLight = min3(tl, tc, tr);

                    if (minLight > mc.A && minLight > maxDark)
                    {
                        lightestColor = getLargest(mc, lightestColor, tl, tc, tr, strength);
                    }
                    else
                    {
                        maxDark = max3(tl, tc, tr);
                        minLight = min3(br, bc, bl);
                        if (minLight > mc.A && minLight > maxDark)
                        {
                            lightestColor = getLargest(mc, lightestColor, br, bc, bl, strength);
                        }
                    }

                    //Kernel 1 and 5
                    maxDark = max3(mc, ml, bc);
                    minLight = min3(mr, tc, tr);

                    if (minLight > maxDark)
                    {
                        lightestColor = getLargest(mc, lightestColor, mr, tc, tr, strength);
                    }
                    else
                    {
                        maxDark = max3(mc, mr, tc);
                        minLight = min3(bl, ml, bc);
                        if (minLight > maxDark)
                        {
                            lightestColor = getLargest(mc, lightestColor, bl, ml, bc, strength);
                        }
                    }

                    //Kernel 2 and 6
                    maxDark = max3(ml, tl, bl);
                    minLight = min3(mr, br, tr);

                    if (minLight > mc.A && minLight > maxDark)
                    {
                        lightestColor = getLargest(mc, lightestColor, mr, br, tr, strength);
                    }
                    else
                    {
                        maxDark = max3(mr, br, tr);
                        minLight = min3(ml, tl, bl);
                        if (minLight > mc.A && minLight > maxDark)
                        {
                            lightestColor = getLargest(mc, lightestColor, ml, tl, bl, strength);
                        }
                    }

                    //Kernel 3 and 7
                    maxDark = max3(mc, ml, tc);
                    minLight = min3(mr, br, bc);

                    if (minLight > maxDark)
                    {
                        lightestColor = getLargest(mc, lightestColor, mr, br, bc, strength);
                    }
                    else
                    {
                        maxDark = max3(mc, mr, bc);
                        minLight = min3(tc, ml, tl);
                        if (minLight > maxDark)
                        {
                            lightestColor = getLargest(mc, lightestColor, tc, ml, tl, strength);
                        }
                    }

                    *scanline = lightestColor.ToArgb();
                }
            });

            // Note that we don't have to re-calculate luminance again.
            oldBitmap.UnlockBits(oldData);
            newBitmap.UnlockBits(newData);
            return newBitmap;
        }
        public static unsafe Bitmap ComputeGradient(Bitmap oldBitmap)
        {
            // Don't overwrite bm itself instantly after the one convolution is done. Do it after all convonlutions are done.
            Bitmap newBitmap = new Bitmap(oldBitmap.Width, oldBitmap.Height, PixelFormat.Format32bppArgb);
            Rectangle entireRect = new Rectangle(0, 0, oldBitmap.Width, oldBitmap.Height);
            BitmapData newData = newBitmap.LockBits(entireRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData oldData = oldBitmap.LockBits(entireRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int h = oldBitmap.Height - 1;
            int w = oldBitmap.Width - 1;
            byte* oldScan0 = (byte*)oldData.Scan0;
            byte* newScan0 = (byte*)newData.Scan0;

            // Sobel operator.
            int[,] sobelx = {{-1, 0, 1},
                              {-2, 0, 2},
                              {-1, 0, 1}};

            int[,] sobely = {{-1, -2, -1},
                              { 0, 0, 0},
                              { 1, 2, 1}};

            // Loop over each pixel and do convolution.
            Parallel.For(1, h, y =>
            {
                for (int x = 1; x < w; x++)
                {
                    int dx = GetPixel(oldScan0, oldData.Stride, x - 1, y - 1).A * sobelx[0, 0] + GetPixel(oldScan0, oldData.Stride, x, y - 1).A * sobelx[0, 1] + GetPixel(oldScan0, oldData.Stride, x + 1, y - 1).A * sobelx[0, 2]
                              + GetPixel(oldScan0, oldData.Stride, x - 1, y).A * sobelx[1, 0] + GetPixel(oldScan0, oldData.Stride, x, y).A * sobelx[1, 1] + GetPixel(oldScan0, oldData.Stride, x + 1, y).A * sobelx[1, 2]
                              + GetPixel(oldScan0, oldData.Stride, x - 1, y + 1).A * sobelx[2, 0] + GetPixel(oldScan0, oldData.Stride, x, y + 1).A * sobelx[2, 1] + GetPixel(oldScan0, oldData.Stride, x + 1, y + 1).A * sobelx[2, 2];

                    int dy = GetPixel(oldScan0, oldData.Stride, x - 1, y - 1).A * sobely[0, 0] + GetPixel(oldScan0, oldData.Stride, x, y - 1).A * sobely[0, 1] + GetPixel(oldScan0, oldData.Stride, x + 1, y - 1).A * sobely[0, 2]
                           + GetPixel(oldScan0, oldData.Stride, x - 1, y).A * sobely[1, 0] + GetPixel(oldScan0, oldData.Stride, x, y).A * sobely[1, 1] + GetPixel(oldScan0, oldData.Stride, x + 1, y).A * sobely[1, 2]
                           + GetPixel(oldScan0, oldData.Stride, x - 1, y + 1).A * sobely[2, 0] + GetPixel(oldScan0, oldData.Stride, x, y + 1).A * sobely[2, 1] + GetPixel(oldScan0, oldData.Stride, x + 1, y + 1).A * sobely[2, 2];
                    int derivata = (dx * dx) + (dy * dy);

                    var pixel = GetPixel(oldScan0, oldData.Stride, x, y);
                    if (derivata > 255 * 255)
                    {
                        SetPixel(newScan0, newData.Stride, x, y, Color.FromArgb(0, pixel.R, pixel.G, pixel.B));
                    }
                    else
                    {
                        SetPixel(newScan0, newData.Stride, x, y, Color.FromArgb(0xFF - (int)Math.Sqrt(derivata), pixel.R, pixel.G, pixel.B));
                    }
                }
            });

            oldBitmap.UnlockBits(oldData);
            newBitmap.UnlockBits(newData);
            return newBitmap;
        }

        //Original HLSL's C# equivalent.
        //public static void ComputeGradient(ref Bitmap bm)
        //{
        //    Bitmap temp = new Bitmap(bm.Width, bm.Height);

        //    for (int x = 0; x < bm.Width - 1; x++)
        //    {
        //        for (int y = 0; y < bm.Height - 1; y++)
        //        {
        //            //Default translation constants
        //            int xn = -1;
        //            int xp = 1;
        //            int yn = -1;
        //            int yp = 1;

        //            //If x or y is on the border, don't move out of bounds
        //            if (x == 0)
        //            {
        //                xn = 0;
        //            }
        //            else if (x == bm.Width - 1)
        //            {
        //                xp = 0;
        //            }
        //            if (y == 0)
        //            {
        //                yn = 0;
        //            }
        //            else if (y == bm.Height - 1)
        //            {
        //                yp = 0;
        //            }

        //            var kernel = new List<Point>();
        //            //Top column
        //            //Point tl = new Point(x + xn, y + yn);
        //            //Point tc = new Point(x, y + yn);
        //            //Point tr = new Point(x + xp, y + yn);
        //            var tl = bm.GetPixel(x + xn, y + yn);
        //            var tc = bm.GetPixel(x, y + yn);
        //            var tr = bm.GetPixel(x + xp, y + yn);

        //            //Middle column
        //            //Point ml = new Point(x + xn, y);
        //            //Point mc = new Point(x, y);
        //            //Point mr = new Point(x + xp, y);
        //            var ml = bm.GetPixel(x + xn, y);
        //            var mc = bm.GetPixel(x, y);
        //            var mr = bm.GetPixel(x + xp, y);

        //            //Bottom column
        //            //Point bl = new Point(x + xn, y + yp);
        //            //Point bc = new Point(x, y + yp);
        //            //Point br = new Point(x + xp, y + yp);
        //            var bl = bm.GetPixel(x + xn, y + yp);
        //            var bc = bm.GetPixel(x, y + yp);
        //            var br = bm.GetPixel(x + xp, y + yp);

        //            int xgrad = (-tl.A + tr.A - ml.A - ml.A + mr.A + mr.A - bl.A + br.A);
        //            int ygrad = (-tl.A - tc.A - tc.A - tr.A + bl.A + bc.A + bc.A + br.A);

        //            double derivata = Math.Sqrt((xgrad * xgrad) + (ygrad * ygrad));

        //            if (derivata > 255)
        //            {
        //                temp.SetPixel(x, y, Color.FromArgb(255, mc.R, mc.G, mc.B));
        //            }
        //            else
        //            {
        //                temp.SetPixel(x, y, Color.FromArgb((int)derivata, mc.R, mc.G, mc.B));
        //            }
        //        }
        //    }

        //    // Write result to bm's alpha channel.
        //    Rectangle rect = new Rectangle(0, 0, bm.Width, bm.Height);
        //    bm = temp.Clone(rect, PixelFormat.Format32bppArgb);
        //    temp.Dispose();
        //}

        public static unsafe Bitmap PushGradient(Bitmap oldBitmap, int strength)
        {
            // Push color based on gradient.
            Bitmap newBitmap = new Bitmap(oldBitmap.Width, oldBitmap.Height, PixelFormat.Format32bppArgb);
            Rectangle entireRect = new Rectangle(0, 0, oldBitmap.Width, oldBitmap.Height);
            BitmapData newData = newBitmap.LockBits(entireRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData oldData = oldBitmap.LockBits(entireRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int h = oldBitmap.Height - 1;
            int w = oldBitmap.Width - 1;
            byte* oldScan0 = (byte*)oldData.Scan0;
            byte* newScan0 = (byte*)newData.Scan0;

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    //Default translation constants
                    int xn = -1;
                    int xp = 1;
                    int yn = -1;
                    int yp = 1;

                    //If x or y is on the border, don't move out of bounds
                    if (x == 0)
                    {
                        xn = 0;
                    }
                    else if (x == w)
                    {
                        xp = 0;
                    }
                    if (y == 0)
                    {
                        yn = 0;
                    }
                    else if (y == h)
                    {
                        yp = 0;
                    }

                    //Top column
                    var tl = GetPixel(oldScan0, oldData.Stride, x + xn, y + yn);
                    var tc = GetPixel(oldScan0, oldData.Stride, x, y + yn);
                    var tr = GetPixel(oldScan0, oldData.Stride, x + xp, y + yn);

                    //Middle column
                    var ml = GetPixel(oldScan0, oldData.Stride, x + xn, y);
                    var mc = GetPixel(oldScan0, oldData.Stride, x, y);
                    var mr = GetPixel(oldScan0, oldData.Stride, x + xp, y);

                    //Bottom column
                    var bl = GetPixel(oldScan0, oldData.Stride, x + xn, y + yp);
                    var bc = GetPixel(oldScan0, oldData.Stride, x, y + yp);
                    var br = GetPixel(oldScan0, oldData.Stride, x + xp, y + yp);

                    var lightestColor = GetPixel(oldScan0, oldData.Stride, x, y);

                    //Kernel 0 and 4
                    float maxDark = max3(br, bc, bl);
                    float minLight = min3(tl, tc, tr);

                    if (minLight > mc.A && minLight > maxDark)
                    {
                        lightestColor = getAverage(mc, tl, tc, tr, strength);
                    }
                    else
                    {
                        maxDark = max3(tl, tc, tr);
                        minLight = min3(br, bc, bl);
                        if (minLight > mc.A && minLight > maxDark)
                        {
                            lightestColor = getAverage(mc, br, bc, bl, strength);
                        }
                    }

                    //Kernel 1 and 5
                    maxDark = max3(mc, ml, bc);
                    minLight = min3(mr, tc, tr);

                    if (minLight > maxDark)
                    {
                        lightestColor = getAverage(mc, mr, tc, tr, strength);
                    }
                    else
                    {
                        maxDark = max3(mc, mr, tc);
                        minLight = min3(bl, ml, bc);
                        if (minLight > maxDark)
                        {
                            lightestColor = getAverage(mc, bl, ml, bc, strength);
                        }
                    }

                    //Kernel 2 and 6
                    maxDark = max3(ml, tl, bl);
                    minLight = min3(mr, br, tr);

                    if (minLight > mc.A && minLight > maxDark)
                    {
                        lightestColor = getAverage(mc, mr, br, tr, strength);
                    }
                    else
                    {
                        maxDark = max3(mr, br, tr);
                        minLight = min3(ml, tl, bl);
                        if (minLight > mc.A && minLight > maxDark)
                        {
                            lightestColor = getAverage(mc, ml, tl, bl, strength);
                        }
                    }

                    //Kernel 3 and 7
                    maxDark = max3(mc, ml, tc);
                    minLight = min3(mr, br, bc);

                    if (minLight > maxDark)
                    {
                        lightestColor = getAverage(mc, mr, br, bc, strength);
                    }
                    else
                    {
                        maxDark = max3(mc, mr, bc);
                        minLight = min3(tc, ml, tl);
                        if (minLight > maxDark)
                        {
                            lightestColor = getAverage(mc, tc, ml, tl, strength);
                        }
                    }

                    // Remove alpha channel (which contains our graident) that is not needed.
                    lightestColor = Color.FromArgb(255, lightestColor.R, lightestColor.G, lightestColor.B);
                    SetPixel(newScan0, newData.Stride, x, y, lightestColor);
                }
            });

            oldBitmap.UnlockBits(oldData);
            newBitmap.UnlockBits(newData);
            return newBitmap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte clamp(byte i, byte min, byte max)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int min3(Color a, Color b, Color c)
        {
            return Math.Min(Math.Min(a.A, b.A), c.A);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int max3(Color a, Color b, Color c)
        {
            return Math.Max(Math.Max(a.A, b.A), c.A);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Color getLargest(Color cc, Color lightestColor, Color a, Color b, Color c, int strength)
        {
            int inverseStrength = 0xFF - strength;
            int aa = (cc.A * inverseStrength + ((a.A + b.A + c.A) / 3) * strength) / 0xFF;
            if (aa > lightestColor.A)
            {
                int ra = (cc.R * inverseStrength + ((a.R + b.R + c.R) / 3) * strength) / 0xFF;
                int ga = (cc.G * inverseStrength + ((a.G + b.G + c.G) / 3) * strength) / 0xFF;
                int ba = (cc.B * inverseStrength + ((a.B + b.B + c.B) / 3) * strength) / 0xFF;

                return Color.FromArgb(aa, ra, ga, ba);
            }

            return lightestColor;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Color getAverage(Color cc, Color a, Color b, Color c, int strength)
        {
            int inverseStrength = (0xFF - strength);
            int ra = (cc.R * inverseStrength + ((a.R + b.R + c.R) / 3) * strength) / 0xFF;
            int ga = (cc.G * inverseStrength + ((a.G + b.G + c.G) / 3) * strength) / 0xFF;
            int ba = (cc.B * inverseStrength + ((a.B + b.B + c.B) / 3) * strength) / 0xFF;
            int aa = (cc.A * inverseStrength + ((a.A + b.A + c.A) / 3) * strength) / 0xFF;

            return Color.FromArgb(aa, ra, ga, ba);
        }
    }
}
