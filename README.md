# StageDeck

Windows 版"台前调度"（Stage Manager）桌面窗口管理工具。灵感来自 iPad/macOS Stage Manager，用 Win32 + DWM 原生 API 实现。

## 效果

- 按热键进入台前调度：自动最小化其他窗口、隐藏桌面图标，左侧悬浮显示所有打开窗口的实时缩略图
- 点击缩略图切换窗口，DWM 实时预览
- 鼠标移到屏幕最左边缘，侧栏自动置顶弹出；移开后自动沉底
- hover 卡片平滑放大 + 边框高亮
- 卡片大小可调，设置自动保存

## 已实现功能

- [x] Ctrl+Alt+S 一键进入/退出台前调度
- [x] DWM 实时缩略图（非截图，实时预览）
- [x] 点击卡片切换窗口（旧窗口自动最小化）
- [x] 桌面图标隐藏（保留右键刷新功能）
- [x] 鼠标左边缘自动置顶弹出侧栏
- [x] hover 平滑放大动画
- [x] 卡片大小滑块调节（设置自动保存到 %APPDATA%）
- [x] 新窗口实时加入侧栏（SetWinEventHook）
- [x] 开机自动进入台前调度
- [x] 设置窗口（Ctrl+Alt+O 呼出）

## 计划中

- [ ] 系统托盘图标（右键菜单：显示设置/退出）
- [ ] 多显示器支持
- [ ] 窗口分组
- [ ] 毛玻璃效果
- [ ] 全屏游戏豁免（游戏时不触发）
- [ ] 开机自启选项
- [ ] 窗口位置/大小记忆
- [ ] 黑白名单（指定窗口不参与调度）

## 热键

| 热键 | 功能 |
|---|---|
| Ctrl+Alt+S | 切换台前调度 |
| Ctrl+Alt+O | 显示设置窗口 |
| Ctrl+Alt+Q | 退出 |

## 技术栈

- C# / .NET 8
- Avalonia UI 11
- Win32 P/Invoke（user32.dll、dwmapi.dll）
- DWM Thumbnail API 实时窗口预览

## 构建

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

生成单文件 exe 在 `bin/Release/net8.0/win-x64/publish/`。
