# FlowIME 使用与配置

[返回项目首页](../README.md)

## 安装与旧版迁移

正式市场版的完整扩展 ID 是 `FlowIME.flowime`，显示名称为 **FlowIME: Auto Input Switcher**，发布者 ID 为 **FlowIME**，开发者为 **haochen12**。

```powershell
code --install-extension FlowIME.flowime
```

### 从 ContextIME 或本地版迁移

先卸载旧市场版 `ContextIME.codex-terminal-auto-input`，再安装 `FlowIME.flowime`。发布者变化后属于不同扩展，无法自动跨 ID 更新。原有 `autoInput.*` 设置继续生效。

1. 在扩展视图搜索 `@id:local-tools.codex-terminal-auto-input`，卸载早期本地版本。
2. 安装发布者为 **FlowIME** 的市场版。
3. 运行 `Developer: Reload Window`，多窗口使用时建议全部重载。

配置仍使用 `autoInput.*` 前缀，无需逐项改名；输入框偏好保存在助手内存中，重启后清空。若早期独立 AutoInput 托盘工具仍在运行，也应先退出。

### 离线安装

从 [GitHub Releases](https://github.com/Howzits/FlowIME/releases) 下载正式 Publisher 为 `FlowIME` 的 Windows x64 安装包，在扩展视图选择 `… → 从 VSIX 安装…`。

GitHub Releases 中早期的 `ContextIME-0.4.1.vsix` 使用 `local-tools` 发布者，与市场版是不同扩展 ID。迁移时请选市场版或上面的 `marketplace-win32-x64` 安装包。

## 沿用默认输入法

- 每次处理焦点变化时，重新读取 Windows 默认输入语言及已安装的键盘布局。
- 默认是中文输入法时，优先沿用它：终端请求英文模式，对话框请求中文模式。
- 默认是英文布局时，终端采用该默认布局；中文输入优先保留当前中文布局，否则从已安装列表中选择。
- 找不到可用输入方式时保持现状并记录日志，不安装新键盘、不加载固定布局、不修改系统默认设置。

无需额外安装美式英语键盘，不指定微软拼音、搜狗等品牌。Windows 输入语言接口不能准确区分同语言下所有 TSF 输入法配置，部分输入法也可能忽略跨进程模式切换请求。

## 状态与控制

点击状态栏的 **FlowIME** 暂停或启用。`目标 EN` / `目标 中` 表示布局请求已发出，**不代表已确认实际输入状态**；`保持` 表示当前控件不干预。出现请求失败或缺少输入法时，可查看日志。

命令面板提供：

- `FlowIME: 暂停 / 启用自动输入法`
- `FlowIME: 查看运行日志`
- `FlowIME: 重启输入法助手`
- `FlowIME: 诊断输入框（3 秒后采集）`
- `FlowIME: 此输入框记住英文 / 中文 / 保持不变（3 秒后采集）`
- `FlowIME: 清除本次会话的输入框偏好`

### 切换防抖

自动切换前，焦点需在同一输入框稳定至少 **80 ms**，并在执行时再次确认。快速经过其他输入框时，旧的待执行请求会被丢弃；进入代码区、密码框、只读区域或离开 VS Code 时取消待执行切换。

同一控件稳定后只处理一次，不会持续强制恢复语言。显式偏好命令已等待 3 秒采集，不再额外防抖。防抖期间会在到期时提前检查焦点，其余时间沿用 `autoInput.pollInterval`；实际响应还受焦点采样、控件和输入法影响。

### 输入框偏好记忆

在聊天或普通输入框手动切换中英文后，如果 Windows 接口能观察到状态变化，FlowIME 会记住该控件的偏好；离开再返回时优先恢复。不会把一次布局请求直接当作用户偏好，也不会在同一控件中不断强制切换。

自动切换后会尽早读取参考状态。只有观察到目标模式，才建立基线；随后不同模式需连续观察至少 250 ms，并经过 700 ms 的自动切换稳定期，才记为偏好。同一输入框不会因此被再次强制切换。未知读数会中断本次变化确认；离开控件、清除偏好或焦点查询异常会重置观察状态。

输入法的状态读取不总是可靠，尤其是 TSF 模式。无法确认目标、变化发生在两次采样之间或离开太快时，可能无法自动记忆；不会据此猜测用户选择。可使用“此输入框记住英文 / 中文 / 保持不变”命令，然后在 **3 秒内点击目标输入框**，显式指定偏好。终端仍默认英文；代码区、密码框和只读控件不接受偏好操作。

偏好仅保存在当前助手会话内，最多 128 个控件；不会写入磁盘或记录聊天内容。控件重建、重载窗口、修改设置或助手重启后可能失效或清空。新版本安装后建议重载所有 VS Code 窗口，避免旧助手占用共享控制通道。

### 输入框诊断

运行诊断命令后，在 **3 秒内点击目标输入框**，结果会显示在 FlowIME 输出面板：

```text
diagnostic reason=editable-input default=zh preference=en observed=en remember=True interval=100 debounce=80
```

`reason` 表示规则依据：`terminal`（终端）、`editable-input`（输入框）、`code-editor`（代码区）、`password`（密码框）、`readonly-or-noneditable`（只读或非输入控件）、`scope-preserve`（按范围保持）或无法获取可访问焦点。`default` 为规则目标，`preference` 为会话偏好；`observed` 仅是传统 Windows IME 接口的参考值，不保证是实际 TSF 状态。`debounce` 为焦点稳定等待时间（毫秒）。诊断不输出控件文字、窗口标题、代码或聊天内容。

## 设置

| 设置 | 默认值 | 说明 |
| --- | --- | --- |
| `autoInput.enabled` | `true` | 启用自动切换；关闭后停止助手 |
| `autoInput.rememberPreferences` | `true` | 记住当前会话的输入框偏好；关闭后始终按默认规则 |
| `autoInput.otherInputs` | `chinese` | 其他可编辑输入框也请求中文；设为 `preserve` 可减少对搜索、命令面板等区域的干预 |
| `autoInput.pollInterval` | `100` | 焦点检查间隔，50–1000 ms；实际切换延迟受控件与输入法影响 |

如果只想处理终端与能识别出聊天语义的输入区域：

```json
{
  "autoInput.otherInputs": "preserve"
}
```

没有暴露聊天语义标识的第三方输入框也可能因此不再切中文，改回 `chinese` 可恢复通用输入框规则。

设置项和内部命令 ID 保留原有 `autoInput` 前缀，方便已有本地版本升级；界面名称统一为 FlowIME。

## 兼容范围与限制

- 当前构建在 Windows x64 本机验证，要求 VS Code 1.96 或以上；不支持 macOS、Linux、浏览器版 VS Code 或系统全局切换。
- 扩展运行在本机 UI 侧。Windows 本机打开 SSH / WSL 工作区的焦点路径设计上适用，但这些流程尚待实际验证。
- 输入框必须暴露可访问的可编辑控件。自绘控件、不提供可写状态的 Webview 可能无法识别。
- Monaco 聊天编辑器还需有可识别的聊天容器标识；否则可能按代码编辑区保持不变。
- 跨进程输入法请求可能被应用或输入法忽略。日志中的 `layoutRequest=True` 仅表示请求发出，不等于中文已上屏。
- 建议先完成候选词上屏，再切换区域；未完成输入的处理由输入法决定。
- 多窗口共享一个活动助手；设置为用户级，暂停会同步到其他窗口。

## 没有自动切换？

1. 确认状态栏未暂停，聚焦目标输入框后稍等再试打。
2. 运行 `FlowIME: 诊断输入框（3 秒后采集）`，返回目标控件后查看规则原因；日志可进一步区分缺少输入法与请求失败。
3. 没有识别到控件时，可手动尝试把 `editor.accessibilitySupport` 设为 `on` 后重载窗口。这会启用 VS Code 辅助功能行为，扩展不会替你修改。
4. 已发出请求但没有中文上屏，先手动切换确认输入法本身正常；可能需要进一步适配该输入法。
5. 助手停止时运行重启命令。异常退出最多自动重试两次，暂停或禁用会取消重试。

同时启用操作同一区域的其他输入法工具可能互相覆盖。反馈时提供 Windows、VS Code、输入法版本、触发区域和日志即可，不需要聊天正文。

具体检查项目和待实测场景见 [兼容性验证记录](COMPATIBILITY.md)。自动测试通过不能替代真实界面上屏测试。

## 隐私与实现

FlowIME 通过 Windows UI Automation 查询焦点控件的类型、可写状态及结构标识，再发送 Windows 输入法请求。不读取输入值、代码正文或聊天内容，不监听按键，不联网，不上传遥测。日志仅留在 VS Code 输出面板，记录目标语言、请求结果和错误类型。

扩展会读取前台进程名以判断是否为 VS Code，仅对支持的 VS Code 进程应用规则。安装包中的 `InputHelper.exe` 由扩展启动，暂停或禁用时停止；退出或崩溃时也会通过进程生命周期清理。

## 开发

```powershell
npm ci --ignore-scripts
npm run build
```

需要 Windows .NET Framework 4.x 编译器、Node.js 24、PowerShell 7 和 Python 3，无 npm 运行时依赖。统一入口运行编译、自检、测试和官方打包，在 `dist/` 输出 Windows x64 VSIX 与校验文件。详见 [维护与发布流程](MAINTAINING.md)。

| 文件 | 职责 |
| --- | --- |
| `extension.js` | VS Code 命令、设置、状态栏与助手生命周期 |
| `native/InputHelper.cs` | 焦点采集、切换请求与轮询 |
| `native/InputPolicy.cs` | 终端、聊天及普通输入框判断规则 |
| `native/LayoutSelector.cs` | 默认及已安装输入法选择 |
| `native/Preferences.cs` | 有上限的会话偏好缓存 |
| `native/CommandBus.cs` | 多窗口命令路由与结果回复 |
| `native/NativeMethods.cs` | Windows 接口声明 |
| `test/extension.test.js` | 暂停、配置更新、状态消息和异常重试测试 |

