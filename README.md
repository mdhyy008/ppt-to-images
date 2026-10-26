# ppt-to-images

把 PPT 转成图片的 Windows 桌面工具。拖入演示文稿，逐页生成图片，勾选需要的页面批量导出。

## 功能

- 支持 `.ppt` `.pptx` `.pps` `.ppsx` `.pptm` `.pot` `.potx`
- 一个文件一个标签页，可同时打开多个演示文稿
- 缩略图勾选，支持全选、全不选、反选
- 导出 PNG 或 JPG
- 多个标签页的选中页面可一并导出，各自写入源文件旁的 `文件名_图片` 目录
- 双击缩略图全屏预览，方向键翻页
- 跟随系统深色 / 浅色主题

## 系统要求

- Windows 10 / 11
- .NET Framework 4.8
- 本机安装 Microsoft PowerPoint（转换通过 PowerPoint 完成）

## 使用

1. 把 PPT 文件拖进窗口，或用「文件 → 打开文档…」选择文件。
2. 在缩略图右上角勾选要导出的页面。
3. 选择 PNG 或 JPG，点「导出选中」。
4. 图片会写入源文件同目录下的 `文件名_图片` 文件夹。

## 构建

用 .NET Framework 4.8 的 MSBuild 编译：

```
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" SlideShot.csproj
```

输出在 `bin\Debug\SlideShot.exe`。

## 许可证

[MIT](LICENSE)

开发者：明灯花月夜 · [mdhyy.cn](https://mdhyy.cn)
