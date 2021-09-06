using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NcnnDotNet;
using NcnnDotNet.OpenCV;

namespace Real_ESRGAN_GUI
{
    public sealed class Model:IDisposable
    {
        public int NumThread = Environment.ProcessorCount;
        private Net model = new Net();
        private int prepadding = 0;
        private int tileSize = 100;

        public void LoadModel(string modelPath, string modelName)
        {
            string paramExtension = ".param";
            string binExtension = ".bin";

            try
            {
                model.LoadParam($"{modelPath}/{modelName}{paramExtension}");
                model.LoadModel($"{modelPath}/{modelName}{binExtension}");
            }
            catch
            {
                throw;
            }
        }

        public async Task Scale(string imagePath, string imageSavePath, string imageSaveFormat, int scale)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException(imagePath);
            }

            var file = await File.ReadAllBytesAsync(imagePath);
            using var image = Cv2.ImDecode(file, CvLoadImage.Color);

            if (image.IsEmpty)
            {
                throw new NotSupportedException($"{imagePath} is not a supported format.");
            }

            var inImage = new NcnnDotNet.Mat(image.Cols, image.Rows, image.Channels(), image.Data);
            var outMat = Inference(inImage, scale);
            var mat = new NcnnDotNet.Mat();
            mat.CreateLike(outMat);
            if (image.Channels() == 3)
            {
                outMat.ToPixels(mat.Data, PixelType.Rgb2Bgr);
            }
            if (image.Channels() == 4)
            {
                outMat.ToPixels(mat.Data, PixelType.Rgba2Bgra);
            }

            var saveName = Path.GetFileName(imagePath);

            //using (StreamWriter sw = new StreamWriter($"{imageSavePath}{saveName.Split(".")[0]}_{scale}x.ppm"))
            //{
            //    sw.Write("P3\r\n{0} {1}\r\n{2}\r\n", outMat.W, outMat.H, 255);
            //    for (int i = 0; i < data.Length; i+=4)
            //        sw.Write("{0} {1} {2}\r\n", data[i], data[i+1], data[i+2]);
            //    sw.Close();
            //}
            //if (outMat.C == 3)
            //{
            //    NcnnDotNet.C.Ncnn.MatToPixels(outMat, data, PixelType.Rgb2Bgr, outMat.W * 3);
            //}
            //if (outMat.C == 4)
            //{
            //    NcnnDotNet.C.Ncnn.MatToPixels(outMat, data, PixelType.Rgba2Bgra, outMat.W * 4);
            //}

            NcnnDotNet.OpenCV.Mat outImg = new NcnnDotNet.OpenCV.Mat(mat.H, mat.W, image.Channels()==3?Cv2.CV_8UC3:Cv2.CV_8UC4, mat.Data);
            Cv2.ImShow("outImg", outImg);
            Cv2.ImWrite($"{imageSavePath}{saveName.Split(".")[0]}_{scale}x.{imageSaveFormat}", outImg);
        }

        private NcnnDotNet.Mat Inference(NcnnDotNet.Mat image, int scale)
        {
            var outMat = new NcnnDotNet.Mat();

            using (var inMat = image.C == 3 ? NcnnDotNet.Mat.FromPixels(image.Data, PixelType.Bgr2Rgb, image.W, image.H) : NcnnDotNet.Mat.FromPixels(image.Data, PixelType.Bgra2Rgba, image.W, image.H))
            {
                using (var ex = model.CreateExtractor())
                {
                    ex.SetNumThreads(NumThread);
                    ex.Input("data", inMat);

                    ex.Extract("output", outMat);
                }
            }

            return outMat;
        }

        public void Dispose()
        {
            model?.Dispose();
        }
    }
}
