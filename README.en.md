<p align="center">
  <img src="assets/flowime-logo-v2.png" alt="FlowIME Logo" width="176" height="176">
</p>

<p align="center">
  <strong>Switch less. Stay in flow.</strong><br>
  Chinese for chat. English for the terminal. Keep your thoughts moving.
</p>

<p align="center">
  FlowIME · Automatic Chinese/English input switching for VS Code on Windows.
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=FlowIME.flowime"><img src="https://img.shields.io/visual-studio-marketplace/v/FlowIME.flowime?style=flat-square&amp;color=6574F7&amp;label=Marketplace" alt="Visual Studio Marketplace version"></a>
  <img src="https://img.shields.io/badge/platform-Windows_x64-1678D4?style=flat-square" alt="Windows x64">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-20B886?style=flat-square" alt="MIT License"></a>
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=FlowIME.flowime"><strong>Install from the Marketplace</strong></a>
  &nbsp;·&nbsp;
  <a href="docs/USAGE.md">Usage guide (中文)</a>
  &nbsp;·&nbsp;
  <a href="https://github.com/Howzits/FlowIME/issues">Report an issue</a>
</p>

<p align="center">
  <a href="README.md">简体中文</a> / <strong>English</strong>
</p>

---

## From conversation to command

Discuss a solution with AI in Chinese, run a command in the terminal, then return to the conversation. FlowIME switches input modes with focus, reducing the interruptions of switching them yourself.

| What you are doing | FlowIME's default behavior |
| --- | --- |
| Describing a task in chat | Request Chinese mode |
| Entering a terminal command | Request English mode |
| Working in the code editor | Keep the current input mode |

- **Keep your familiar IME:** use the input method already configured in Windows, with no extra English keyboard required.
- **Make it yours:** observable manual language changes are remembered for the current session. You can also choose Chinese, English, or no intervention for an input.
- **Reduce back-and-forth switching:** focus debounce waits for a stable input before switching, filtering out brief stops along the way.
- **Take control anytime:** click the status bar to pause; use diagnostics to investigate unrecognized inputs. Password fields and read-only controls are left unchanged.

## Install

Requires **Windows x64, VS Code 1.96+ or Insiders**, and a configured Chinese input method.

Search the Marketplace for **FlowIME: Auto Input Switcher**, published by **FlowIME**, or run:

```powershell
code --install-extension FlowIME.flowime
```

Enabled as soon as you install it. Type a Chinese message in chat, then enter a terminal command to try focus-based switching.

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

[Usage, migration and troubleshooting (中文)](docs/USAGE.md) · [Compatibility (中文)](docs/COMPATIBILITY.md) · [Maintenance and releases (中文)](docs/MAINTAINING.md) · [Changelog](CHANGELOG.md) · [Report an issue](https://github.com/Howzits/FlowIME/issues/new/choose)

**Focused on focus.** No code or chat content is read. No keystroke listening, network requests, or telemetry. Currently limited to desktop VS Code on Windows; no system-wide switching.

Developed by **haochen12** · [MIT License](LICENSE) · Independent project, not affiliated with AI assistant vendors.
