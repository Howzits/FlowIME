<p align="center">
  <img src="assets/flowime-logo.png" alt="FlowIME Logo" width="112" height="112">
</p>

<h1 align="center">FlowIME</h1>

<p align="center">
  <strong>Chinese for chat. English for the terminal.</strong><br>
  Fewer input-method switches. Fewer interruptions.
</p>

<p align="center">
  Focus-aware input switching for AI-assisted coding in VS Code on Windows.
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=ContextIME.codex-terminal-auto-input"><img src="https://img.shields.io/visual-studio-marketplace/v/ContextIME.codex-terminal-auto-input?style=flat-square&amp;color=6574F7&amp;label=Marketplace" alt="Visual Studio Marketplace version"></a>
  <img src="https://img.shields.io/badge/platform-Windows_x64-1678D4?style=flat-square" alt="Windows x64">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-20B886?style=flat-square" alt="MIT License"></a>
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=ContextIME.codex-terminal-auto-input"><strong>Install from the Marketplace</strong></a>
  &nbsp;·&nbsp;
  <a href="docs/USAGE.md">Usage guide (中文)</a>
  &nbsp;·&nbsp;
  <a href="https://github.com/Howzits/ContextIME/issues">Report an issue</a>
</p>

<p align="center">
  <a href="README.md">简体中文</a> / <strong>English</strong>
</p>

---

## Features

- **Switch with focus:** English in the terminal; Chinese in chat and ordinary inputs by default.
- **Use your configured IME:** no fixed keyboard IDs or extra English layout required.
- **Remember input preferences:** choose Chinese, English, or no intervention for individual inputs within the current session.
- **Stay in control:** pause from the status bar and inspect inputs with diagnostics. Code editors, password fields, and read-only controls are left unchanged.

## Install

Requires **Windows x64, VS Code 1.96+ or Insiders**, and a configured Chinese input method.

Search the Marketplace for **FlowIME**, published by **ContextIME**, or run:

```powershell
code --install-extension ContextIME.codex-terminal-auto-input
```

Enabled by default. Try typing in a chat input and the terminal; click the status bar item to pause.

Formerly ContextIME. Existing users can update directly; extension ID and settings are unchanged.

The interface follows the VS Code display language: Chinese for Chinese locales, English otherwise. No extra configuration is needed.

## Settings

| Setting | Default | Purpose |
| --- | --- | --- |
| `autoInput.enabled` | `true` | Enable automatic switching |
| `autoInput.rememberPreferences` | `true` | Remember input preferences for the current session |
| `autoInput.otherInputs` | `chinese` | Use `preserve` to leave ordinary inputs, such as search boxes, unchanged |
| `autoInput.pollInterval` | `100` | Focus-check interval in milliseconds, from 50 to 1000 |

Preferences do not persist across helper sessions. Switching depends on control accessibility and IME support. The status bar shows the requested target, not a verified input state; unrecognized chat inputs may need adaptation.

## Documentation and support

[Usage and troubleshooting (中文)](docs/USAGE.md) · [Compatibility (中文)](docs/COMPATIBILITY.md) · [Maintenance and releases (中文)](docs/MAINTAINING.md) · [Changelog](CHANGELOG.md) · [Report an issue](https://github.com/Howzits/ContextIME/issues/new/choose)

No code or chat content is read. No keystroke listening, network requests, or telemetry. Currently limited to desktop VS Code on Windows; no system-wide switching.

Developed by **haochen12** · [MIT License](LICENSE) · Independent project, not affiliated with AI assistant vendors.
