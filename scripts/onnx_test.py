import math
import onnxruntime as rt
import cv2
import numpy as np
import os

def nearest_of_value(x, base):  
    return math.ceil(x/base)*base

def pad(img):
    pad_w = nearest_of_value(img.shape[0], 64) - img.shape[0]
    pad_h = nearest_of_value(img.shape[1], 64) - img.shape[1]
    return (pad_w, pad_h), cv2.copyMakeBorder(img, 0,  pad_w, 0, pad_h, cv2.BORDER_REFLECT)

def crop_center(img,cropx,cropy):
    y,x,_ = img.shape
    startx = x//2-(cropx//2)
    starty = y//2-(cropy//2)    
    return crop(img, startx, starty, cropx, cropy)
    
def crop(img, startx, starty, cropx, cropy):
    return img[starty:starty+cropy,startx:startx+cropx]

def display_image(img, color_mode):
    if len(img.shape)==4:
        img = np.squeeze(img, axis=0)
    img = np.transpose(img, (2,1,0))
    if color_mode != None:
        img = cv2.cvtColor(img, color_mode)
    print("image display")
    cv2.imshow("out_mat", img)
    cv2.waitKey()

project_path=os.path.abspath("../Real-ESRGAN_GUI/")
sess = rt.InferenceSession(os.path.join(project_path, "models/realesrgan-x4plus_anime_6B.onnx"), providers=["DmlExecutionProvider"], provider_options={"deviceId": "1"})
print("loaded model.")

in_image = cv2.imread(os.path.join("../assets", "avatar_256px.png"), cv2.IMREAD_UNCHANGED)
print("loaded input image.")

in_mat = cv2.cvtColor(in_image, cv2.COLOR_BGR2RGB)
in_mat = np.transpose(in_mat, (2, 1, 0))[np.newaxis]
in_mat = in_mat.astype(np.float32)
in_mat = in_mat/255
print("loaded image.")

# display_image(in_mat, cv2.COLOR_RGB2BGR)
print("sess run.")
input_name = sess.get_inputs()[0].name
output_name = sess.get_outputs()[0].name
out_mat = sess.run([output_name], {input_name: in_mat})[0]

print("convert out_mat to image")
out_mat = np.squeeze(out_mat, axis=0)
out_mat = np.clip(out_mat, 0, 1)
out_mat = (out_mat*255.).round().astype(np.uint8)
out_mat = out_mat.T
out_mat = cv2.cvtColor(out_mat, cv2.COLOR_RGB2BGR)
cv2.imshow("out_mat", out_mat)
cv2.waitKey()