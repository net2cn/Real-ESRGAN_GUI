using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Real_ESRGAN_GUI
{
    public class Model : IDisposable
    {
        private string modelPath = "";
        private InferenceSession session;
        private Logger logger = Logger.Instance;

        public async Task LoadModel(string modelPath, string modelName)
        {
            if (session == null || this.modelPath != modelPath)
            {
                session = await Task.Run(() => { return new InferenceSession(Path.Combine(modelPath, $"{modelName}.onnx")); });
            }
        }

        public async Task Scale(string inputPath, string outputPath, string outputFormat, int scale)
        {
            Bitmap image = new Bitmap(inputPath);

            logger.Log("Creating input image...");
            var inMat = ConvertImageToFloatTensorUnsafe(image);
            logger.Progress += 10;

            logger.Log("Inferencing...");
            var outMat = await Inference(inMat);
            logger.Progress += 10;

            logger.Log("Converting output tensor to image...");
            image = ConvertFloatTensorToImageUnsafe(outMat);

            var saveName = Path.GetFileName(inputPath);
            logger.Log($"Writing image to {outputPath}{saveName.Split(".")[0]}_{scale}x.{outputFormat} ...");
            image.Save($"{outputPath}{saveName.Split(".")[0]}_{scale}x.{outputFormat}");
            logger.Progress += 10;
        }

        public async Task<Tensor<float>> Inference(Tensor<float> input)
        {
            var inputName = session.InputMetadata.First().Key;
            var inputTensor = new List<NamedOnnxValue>() { NamedOnnxValue.CreateFromTensor<float>(inputName, input) };
            var output = await Task.Run(()=> { return session.Run(inputTensor).First().Value; });
            return (Tensor<float>)output;
        }

        public Tensor<float> ConvertImageToFloatTensorUnsafe(Bitmap image)
        {
            // Create the Tensor with the appropiate dimensions for the NN
            Tensor<float> data = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });

            BitmapData bmd = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, image.PixelFormat);
            int PixelSize = 3;

            unsafe
            {
                for (int y = 0; y < bmd.Height; y++)
                {
                    // row is a pointer to a full row of data with each of its colors
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);
                    for (int x = 0; x < bmd.Width; x++)
                    {
                        // note the order of colors is BGR, convert to RGB
                        data[0, 0, x, y] = row[x * PixelSize + 2] / (float)255.0;
                        data[0, 1, x, y] = row[x * PixelSize + 1] / (float)255.0;
                        data[0, 2, x, y] = row[x * PixelSize + 0] / (float)255.0;
                    }
                }

                image.UnlockBits(bmd);
            }
            return data;
        }

        public Bitmap ConvertFloatTensorToImageUnsafe(Tensor<float> tensor)
        {
            Bitmap bmp = new Bitmap(tensor.Dimensions[2], tensor.Dimensions[3], PixelFormat.Format24bppRgb);
            BitmapData bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            int PixelSize = 3;
            unsafe
            {
                for (int y = 0; y < bmd.Height; y++)
                {
                    // row is a pointer to a full row of data with each of its colors
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);
                    for (int x = 0; x < bmd.Width; x++)
                    {
                        // note the order of colors is RGB, convert to BGR
                        // remember clamp to [0, 1]
                        row[x * PixelSize + 2] = (byte)(Math.Clamp(tensor[0, 0, x, y], 0, 1) * 255.0);
                        row[x * PixelSize + 1] = (byte)(Math.Clamp(tensor[0, 1, x, y], 0, 1) * 255.0);
                        row[x * PixelSize + 0] = (byte)(Math.Clamp(tensor[0, 2, x, y], 0, 1) * 255.0);
                    }
                }

                bmp.UnlockBits(bmd);
            }
            return bmp;
        }

        public void Dispose()
        {
            session.Dispose();
        }
    }
}
