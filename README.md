# Real-ESRGAN_GUI
A C# GUI inference implementation of [Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN).

PRs are welcomed.

---

## Usage
You know how a GUI works.
![UI](./assets/screenshot_2021-09-07_194309.png)

## Result
From ![256px image](./assets/avatar_256px.png) to ![1024px image](./assets/avatar_256px_realesrgan-x4plus_anime_6B.png) with the magic of [Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN).

## Build Prerequisites
- Visual Studio 2019 or higher.

## Known Issue
- GPU support is not working.
- Directory input is not implemented yet.
- Alpha channel will be ignored.
- Huge memory consumption when handling large image (~1000x1000, eats up ~18.5G memory easily).

## Acknowledgements
This repository contains ONNX models converted from [Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN) repo. All copyrights and trademarks of the materials used belong to their respective owners and are not being sold.

This repository is created only for learning purpose. I DO NOT take any responsibilities for any possible damages.

Image [upscale example](./assets/avatar_256px.png) and [result](./assets/avatar_256px_realesrgan-x4plus_anime_6B.png) attached in assets folder are derivatives of my personal artwork for my own SNS avatar. Please do not use without permission, especially for commercial purposes.

---

2021, net2cn.