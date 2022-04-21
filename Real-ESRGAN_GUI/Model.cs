using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Real_ESRGAN_GUI
{
    public class Model : IDisposable
    {
        private string modelName = "";
        private InferenceSession session;
        private Logger logger = Logger.Instance;

        public async Task<bool> LoadModel(string modelPath, string modelName, int deviceId, CancellationToken token)
        {
            if (session == null || this.modelName != modelName)
            {
                this.modelName = modelName;
                try
                {
                    var sessionOptions = new SessionOptions();
                    if (deviceId == -1)
                    {
                        logger.Log("Creating inference session on CPU.");
                        sessionOptions.AppendExecutionProvider_CPU();
                    }
                    else
                    {
                        logger.Log($"Creating inference session on GPU{deviceId}.");
                        sessionOptions.AppendExecutionProvider_DML(deviceId);
                    }
                    session = await Task.Run(() => { return new InferenceSession(Path.Combine(modelPath, $"{modelName}.onnx"), sessionOptions); }).WaitOrCancel(token);
                }
                catch
                {
                    logger.Log($"Unable to initial inference session on GPU{deviceId}! Falling back to CPU.");
                    session = await Task.Run(() => { return new InferenceSession(Path.Combine(modelPath, $"{modelName}.onnx")); }).WaitOrCancel(token);
                }
            }

            if (session != null)
            {
                return true;
            }
            return false;
        }

        public async Task Scale(string baseInputPath, List<string> inputPaths, string outputPath, string outputFormat)
        {
            int count = inputPaths.Count();
            foreach(var inputPath in inputPaths)
            {
                Bitmap image = new Bitmap(inputPath);
                if (image.PixelFormat != PixelFormat.Format24bppRgb)
                {
                    image = ConvertBitmapToFormat24bppRgb(image);
                }
                //TODO: Add Alpha channel inference.

                logger.Log("Creating input image...");
                var inMat = ConvertImageToFloatTensorUnsafe(image);
                logger.Progress += 10/count;

                logger.Log("Inferencing...");
                var outMat = await Inference(inMat);
                logger.Progress += 10/count;

                if (outMat == null)
                {
                    logger.Log("A null image is returned.");
                    logger.Progress += 10/count;
                    image.Dispose();
                }

                logger.Log("Converting output tensor to image...");
                image = ConvertFloatTensorToImageUnsafe(outMat);

                var saveName = $"\\{Path.GetFileName(inputPath).Split(".")[0]}_{modelName}.{outputFormat}";
                var saveStructure = Path.GetRelativePath(baseInputPath, Path.GetDirectoryName(inputPath));
                var savePath = Path.GetFullPath(saveStructure,outputPath)+"\\";
                
                logger.Log($"Writing image to {savePath}...");
                Directory.CreateDirectory(savePath);
                image.Save(savePath+saveName);
                logger.Progress += 10/count;
                image.Dispose();
            }
        }

        public async Task<Tensor<float>> Inference(Tensor<float> input)
        {
            try
            {
                var inputName = session.InputMetadata.First().Key;
                var inputTensor = new List<NamedOnnxValue>() { NamedOnnxValue.CreateFromTensor<float>(inputName, input) };
                var output = await Task.Run(() => { return session.Run(inputTensor).First().Value; });
                return (Tensor<float>)output;
            }
            catch
            {
                logger.Log("An exception has occurred during inference session.");
            }
            return null;
        }

        public static Tensor<float> ConvertImageToFloatTensorUnsafe(Bitmap image)
        {
            // Create the Tensor with the appropiate dimensions for the NN
            Tensor<float> data = new DenseTensor<float>(new[] { 1, 3, image.Width, image.Height });

            BitmapData bmd = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
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
                        data[0, 0, x, y] = row[(x * PixelSize) + 2] / (float)255.0;
                        data[0, 1, x, y] = row[(x * PixelSize) + 1] / (float)255.0;
                        data[0, 2, x, y] = row[(x * PixelSize) + 0] / (float)255.0;
                    }
                }

                image.UnlockBits(bmd);
            }
            return data;
        }

        public static Bitmap ConvertFloatTensorToImageUnsafe(Tensor<float> tensor)
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

        public static Bitmap ConvertBitmapToFormat24bppRgb(Bitmap bitmap)
        {
            Bitmap target = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
            target.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);   // Set both bitmap to same dpi to prevent scaling.
            using (Graphics g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            }
            return target;
        }

        public void Dispose()
        {
            if (session!=null)
            {
                session.Dispose();
            }
        }
    }
}
