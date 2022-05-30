# Real-ESRGAN_GUI
一个使用C#编写的图形化[Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN)超分辨率推断小工具，采用了DirectML提供GPU加速。

[[English README]](README.md)

欢迎提出Pull Requests。

## 使用方法
首先安装.NET Desktop Runtime 5.0.15。（x64）

然后我觉得你应该知道GUI应该怎么用。;-)

要使用GPU加速，你需要一块兼容DirectML的GPU与Windows 10 1709或更高的版本，以及兼容的GPU。请将Device Id设置为你想要使用的GPU序号以启用GPU加速（在一台只有一块GPU的PC上，0是默认GPU。但是在配备了集成GPU的PC上，0是集成GPU，1才是独立GPU）。你可以查看[这里](https://github.com/microsoft/DirectML#hardware-requirements)来获取一个更详细的GPU硬件需求。

当超分绘画时，推荐使用带有“6B”后缀的模型，现实图像推荐使用没有后缀的那个。

![UI](./assets/screenshot_2022-03-26_171403.png)

## 结果
使用[Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN)的魔法将图片从![256px image](./assets/avatar_256px.png)超分到![1024px image](./assets/avatar_256px_realesrgan-x4plus_anime_6B.png)。

## 构建环境
- Visual Studio 2019或更高。

## Convert models
请参阅[Convert_Models_From_PyTorch.md](./Convert_Models_From_PyTorch.md)（英语）

## 已知问题
- 在有些环境下GPU加速不可用（可能会产生一张纯黑色的图片）。
- 从文件夹输入图片还未实现。
- Alpha通道会被丢弃。如果你需要这个通道，你可以将它导出为一张单独的图片并分别处理它，然后在后期使用像GIMP那样的图像操纵软件将它们合成在一起。
- 在处理较大的图像时会吃掉海量的内存（在\~1000x1000分辨率下可以轻松的使用掉\~18.5G的内存）。

## 声明
这个仓库包含了转换自[Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN)仓库的ONNX模型。所用材料的所有版权和商标均属于其各自所有者，不得出售。

这个仓库是为了学习而创建的。我*不会*承担任何可能造成损失的风险。

assets文件夹里附带的图像[超分辨率样本](./assets/avatar_256px.png)和[结果](./assets/avatar_256px_realesrgan-x4plus_anime_6B.png)是我为我个人SNS所绘制的的美术作品。请不要在无授权的情况下使用，尤其是用于商业用途。

2022, net2cn.