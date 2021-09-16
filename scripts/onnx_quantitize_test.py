import os
from onnxruntime.quantization import quantize_qat, QuantType, CalibrationDataReader

project_path=os.path.abspath("../Real-ESRGAN_GUI/")
input_model_path = os.path.join(project_path, "models/realesrgan-x4plus_anime_6B.onnx")
output_model_path = os.path.join(project_path,"models/realesrgan-x4plus_anime_6B_quantitized.onnx")
quantize_qat(input_model_path, output_model_path, weight_type=QuantType.QUInt8)