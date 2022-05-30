# Convert Models From PyTorch

In case you're wondering how to convert the original PyTorch models provided, or generated from [the official Real-ESRGAN repo](https://github.com/xinntao/Real-ESRGAN)(hereinafter referred to as "the official repo"), here's a how-to guide for you.

## Prerequisitions

First, the official repo comes with [a simple script](https://github.com/xinntao/Real-ESRGAN/blob/master/scripts/pytorch2onnx.py) that you can use to convert official PyTorch models to ONNX models. If your goal is to convert the model for other purposes, I recommend you to use this script to do the trick. But before you do that, you may want to read the section [# Working with models with different surffixes](#Working-with-models-with-different-surffixes) and do some changes to the script provided byt the official repo.

If your goal is to produce models for this GUI utility, please refer to [this script](https://github.com/net2cn/Real-ESRGAN_GUI/blob/master/scripts/pytorch2onnx.py)(referred to as "the script") that is exactly the same one that I utilized to convert the models.

### Environment

I strongly recommend you to do the conversion in the [Colab notebook demo](https://colab.research.google.com/drive/1k2Zod6kSHEvraybHl50Lys0LerhyTMCo?usp=sharing) provided by the official repo, as this can save you a ton of hassles.

## Working with models with different surffixes

If you're working with the script I provided, you can continue reading. If not, you're going to apply chagnes accordingly.

### x4plus

The script works out-of-the-box with models that have surffixes contains `x4plus`(so `x4plus_anime_6B` works too).

### x2plus

If you're working with a model comes with a surffix like `x2plus`, you're gonna change the script's line 7 to

```Python
model = RRDBNet(num_in_ch=3, num_out_ch=3, num_feat=64, num_block=23, num_grow_ch=32, scale=2)
```

Note that the `scale` parameter is 2 instead of 4 of the script.

### animevideo

In this case, there're a little bit more you're gonna change.

line 3 to
```Python
from realesrgan.archs.srvgg_arch import SRVGGNetCompact
```

And of course, line 7 to
```Python
model = SRVGGNetCompact(num_in_ch=3, num_out_ch=3, num_feat=64, num_conv=16, upscale=4, act_type='prelu')
```

What's more, you're gonna change the `upscale` parameter according to the input model's surffix, e.g. `animevideo-xsx4` is 4 and `animevideo-xsx2` is 2.

---
If you still have any questions, please feel free to draft a new issue in this repo.