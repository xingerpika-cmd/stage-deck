# StageDeck

Windows 版"台前调度"（Stage Manager）桌面窗口管理工具。

## 功能

- 按热键进入台前调度：自动最小化其他窗口、隐藏桌面图标，左侧显示所有打开窗口的实时缩略图
- 点击缩略图切换窗口，DWM 实时预览
- 鼠标移到屏幕最左边缘，侧栏自动置顶弹出；移开后自动沉底
- 卡片大小可调，设置自动保存
- 新窗口自动加入侧栏

## 热键

| 热键 | 功能 |
|---|---|
| Ctrl+Alt+S | 切换台前调度 |
| Ctrl+Alt+O | 显示设置窗口 |
| Ctrl+Alt+Q | 退出 |

## 技术栈

- C# / .NET 8
- Avalonia UI 11
- Win32 P/Invoke（user32.dll、dwmapi.dll、shell32.dll）

## 构建

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

生成单文件 exe 在 `bin/Release/net8.0/win-x64/publish/`。
