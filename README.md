<p align="center">
  <img src="assets/flowime-logo.png" alt="FlowIME Logo" width="112" height="112">
</p>

<h1 align="center">FlowIME</h1>

<p align="center">
  <strong>聊天中文，终端英文。</strong><br>
  少切一次输入法，少打断一次思路。
</p>

<p align="center">
  为 Windows 上的 VS Code 用户设计，让输入法跟随 AI 编程工作流。
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=FlowIME.flowime"><img src="https://img.shields.io/visual-studio-marketplace/v/FlowIME.flowime?style=flat-square&amp;color=6574F7&amp;label=Marketplace" alt="Visual Studio Marketplace version"></a>
  <img src="https://img.shields.io/badge/platform-Windows_x64-1678D4?style=flat-square" alt="Windows x64">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-20B886?style=flat-square" alt="MIT License"></a>
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=FlowIME.flowime"><strong>从 VS Code 市场安装</strong></a>
  &nbsp;·&nbsp;
  <a href="docs/USAGE.md">配置指南</a>
  &nbsp;·&nbsp;
  <a href="https://github.com/Howzits/FlowIME/issues">反馈问题</a>
</p>

<p align="center">
  <strong>简体中文</strong> / <a href="README.en.md">English</a>
</p>

---

## 核心功能

- **随焦点切换**：终端默认英文，聊天和普通输入框默认中文。
- **沿用默认输入法**：不写死键盘布局，无需额外安装英语键盘。
- **记住输入框偏好**：当前会话内可指定中文、英文或保持不变。
- **随时掌控**：状态栏一键暂停，内置输入框诊断；代码区、密码框和只读控件不干预。

## 安装与使用

需要 **Windows x64、VS Code 1.96+ 或 Insiders**，以及已配置的中文输入法。

在扩展市场搜索 **FlowIME: Auto Input Switcher**，确认发布者为 **FlowIME** 后安装，或执行：

```powershell
code --install-extension FlowIME.flowime
```

安装后默认启用。切换到聊天框或终端试打，点击状态栏可暂停。

原名 ContextIME。更换发布者后，旧版用户需卸载旧扩展再安装 FlowIME，避免两个版本同时运行；`autoInput.*` 设置保持兼容。

界面跟随 VS Code 显示语言：中文显示中文，其他语言默认英文，无需额外配置。

## 常用设置

| 设置 | 默认值 | 用途 |
| --- | --- | --- |
| `autoInput.enabled` | `true` | 启用自动切换 |
| `autoInput.rememberPreferences` | `true` | 记住当前会话的输入框偏好 |
| `autoInput.otherInputs` | `chinese` | 设为 `preserve`，让搜索框等普通输入区域保持不变 |
| `autoInput.pollInterval` | `100` | 焦点检查间隔，50–1000 ms |

偏好不跨助手会话保存。切换效果依赖控件与输入法支持，状态栏显示的是请求目标，不代表已确认实际输入状态；未识别的聊天框可能需要适配。

## 文档与反馈

[使用与故障排查](docs/USAGE.md) · [兼容性记录](docs/COMPATIBILITY.md) · [维护与发布](docs/MAINTAINING.md) · [更新日志](CHANGELOG.md) · [反馈问题](https://github.com/Howzits/FlowIME/issues/new/choose)

不读取代码或聊天正文，不监听按键，不联网，不上传遥测。当前仅支持 Windows 桌面版 VS Code，不提供系统全局切换。

开发者：**haochen12** · [MIT License](LICENSE) · 独立项目，与 AI 助手厂商无隶属关系。
