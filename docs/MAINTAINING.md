# 维护与发布

## 单一构建入口

需要 Windows x64、Node.js 24、PowerShell 7、Python 3，以及系统 .NET Framework 4.x 编译器。

```powershell
npm ci --ignore-scripts
npm run build
```

构建依次执行原生编译及自检、Node 扩展测试、官方 vsce 打包、安装包一致性检查，输出：

```text
dist/FlowIME-<version>-win32-x64.vsix
dist/SHA256SUMS.txt
```

Publisher 固定为 `flowime`，平台固定为 `win32-x64`。版本取自 `package.json`，开发工具版本锁定在 `package-lock.json`。运行时无 npm 依赖。旧的自制 `package.py` 打包入口已移除。

**Marketplace 和 GitHub Release 必须复用同一个 VSIX 文件，不要为两个渠道分别构建。** SHA-256 相同才能确认用户获得的是同一份文件。新安装包通过 Releases 分发；仓库中已有的历史二进制保留，不再加入新版本的构建产物。

## 自动检查

推送 main、提交 PR 或手动运行 Actions 会在 Windows runner 上执行同一入口。成功后可下载 `FlowIME-win32-x64` 构建产物。普通提交只有检查和产物，不会公开发布版本。

CI 不替代真实输入法上屏测试。发布前仍需参考 [兼容性记录](COMPATIBILITY.md) 完成实际界面检查。

## 发布新版本

1. 修改 `package.json` 的版本及 `CHANGELOG.md`，同步更新锁文件：`npm install --package-lock-only --ignore-scripts`。
2. 提交并推送，确认主分支检查通过。
3. 为该提交创建并推送一致的版本标签，例如 `v0.4.2`。标签与包版本不一致时会拒绝发布。
4. 标签构建通过后，Actions 从构建产物生成 **Release 草稿**，附上原样 VSIX 和校验文件。流程不会覆盖已有 Release 附件。
5. 在 GitHub 审阅草稿、补充版本说明并实际试用，再发布 Release。
6. 维护者完成市场授权后，发布公开的正式 Release 会自动触发 `Publish Marketplace`，下载并核验原始附件后上传市场，不会二次打包。未授权时仍可手动上传同一个 VSIX。

普通提交、Release 草稿和预发布版本不会自动更新市场。安装包版本已存在时跳过重复上传；内容发生变化则应提高版本号后重新走流程。

## 市场自动发布

发布授权由维护者在 Marketplace 中管理，本仓库不记录具体策略配置、账户数字 ID 或发布凭据。

自动流程复用 Release 中已核验的安装包，不重新构建。普通提交、草稿及预发布版本不会触发市场更新；市场验证和搜索同步仍由 Marketplace 完成。

维护者可以在 GitHub Actions 中先运行仅验证模式，检查附件、身份及 SHA-256；验证通过不代表市场授权已经验证。实际发布失败时，由维护者检查市场授权和工作流运行结果。

## 问题反馈

Issue 页面提供中英双语的故障和功能建议表单。排错需要系统、VS Code、插件、输入法及聊天插件版本、复现步骤和诊断输出，不需要用户的聊天正文、代码或完整个人配置。
