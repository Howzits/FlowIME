'use strict';
const vscode = require('vscode');
const { spawn } = require('child_process');
let stopActive = () => {};

function activate(context) {
  const output = vscode.window.createOutputChannel('FlowIME');
  const status = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 10);
  status.name = 'FlowIME';
  status.command = 'autoInput.toggle';
  status.tooltip = vscode.l10n.t("FlowIME: English for terminals, Chinese for chat. Click to pause/enable. The target does not confirm the actual input state.");
  let child;
  let retry;
  let retries = 0;
  let disposed = false;
  const config = () => vscode.workspace.getConfiguration('autoInput');

  function stop() {
    clearTimeout(retry);
    retry = undefined;
    if (child) {
      const old = child;
      child = undefined;
      old.stdin.end();
      old.kill();
    }
  }

  function start(resetRetries = true) {
    stop();
    if (disposed) return;
    if (resetRetries) retries = 0;
    status.show();
    if (process.platform !== 'win32') {
      status.text = vscode.l10n.t("$(info) FlowIME: Windows only");
      return;
    }
    if (!config().get('enabled', true)) {
      status.text = vscode.l10n.t("$(debug-pause) FlowIME: Paused");
      return;
    }
    const args = [
      `--other-inputs=${config().get('otherInputs', 'chinese') === 'preserve' ? 'preserve' : 'chinese'}`,
      `--poll=${config().get('pollInterval', 100)}`,
      `--remember=${config().get('rememberPreferences', true)}`
    ];
    const worker = spawn(context.asAbsolutePath('bin/InputHelper.exe'), args, {
      windowsHide: true, stdio: ['pipe', 'pipe', 'pipe']
    });
    child = worker;
    status.text = vscode.l10n.t("$(keyboard) FlowIME: Auto");
    output.appendLine(`${new Date().toISOString()} ${vscode.l10n.t('Starting focus helper: {0}', args.join(' '))}`);
    let buffer = '';
    worker.stdout.on('data', data => {
      if (child !== worker) return;
      buffer += data.toString('utf8');
      const lines = buffer.split(/\r?\n/);
      buffer = lines.pop();
      for (const line of lines) {
        output.appendLine(line);
        const target = /target=(en|zh) layoutRequest=(True|False)/.exec(line);
        if (target) {
          status.text = target[2] === 'True'
            ? vscode.l10n.t('$(keyboard) FlowIME: Target {0}', target[1] === 'en' ? vscode.l10n.t('EN') : vscode.l10n.t('ZH'))
            : vscode.l10n.t("$(warning) FlowIME: Request failed");
        } else if (line.includes('focus=preserve')) status.text = vscode.l10n.t("$(keyboard) FlowIME: Preserved");
        else if (line.includes('No configured input method')) status.text = vscode.l10n.t("$(warning) FlowIME: Missing input method");
        else if (line.startsWith('diagnostic ')) output.show(true);
        else if (/^preference=(en|zh|keep) source=command/.test(line)) {
          vscode.window.setStatusBarMessage(vscode.l10n.t("FlowIME: Input preference saved for this session."), 4000);
        } else if (line.startsWith('preferences=cleared')) {
          vscode.window.setStatusBarMessage(vscode.l10n.t("FlowIME: Input preferences cleared for this session."), 4000);
        } else if (line.startsWith('preference=skipped') || line.startsWith('command=')) output.show(true);
      }
    });
    worker.stderr.on('data', data => output.append(data.toString()));
    worker.stdin.on('error', () => {});
    worker.on('error', error => {
      output.appendLine(vscode.l10n.t('Helper startup failed: {0}', error.message));
      if (child === worker) status.text = vscode.l10n.t("$(warning) FlowIME: Startup failed");
    });
    worker.on('exit', code => {
      if (child !== worker) return;
      child = undefined;
      status.text = vscode.l10n.t("$(warning) FlowIME: Helper stopped");
      output.appendLine(vscode.l10n.t('Helper exited ({0})', code));
      if (!disposed && config().get('enabled', true) && retries < 2) {
        retries += 1;
        retry = setTimeout(() => start(false), 1000 * retries);
      }
    });
  }

  const shutdown = () => { disposed = true; stop(); };
  function send(command) {
    if (!child || !child.stdin.writable) {
      output.appendLine(vscode.l10n.t("The helper is not running. Enable FlowIME and restart the helper."));
      output.show(true);
      return;
    }
    child.stdin.write(command + '\n');
    if (command !== 'clear') vscode.window.setStatusBarMessage(vscode.l10n.t("FlowIME: Click the target input within 3 seconds."), 3000);
  }
  stopActive = shutdown;
  context.subscriptions.push(output, status,
    vscode.commands.registerCommand('autoInput.toggle', async () => {
      await config().update('enabled', !config().get('enabled', true), vscode.ConfigurationTarget.Global);
    }),
    vscode.commands.registerCommand('autoInput.showLog', () => output.show()),
    vscode.commands.registerCommand('autoInput.restart', () => start()),
    vscode.commands.registerCommand('autoInput.diagnose', () => send('diagnose')),
    vscode.commands.registerCommand('autoInput.preferEnglish', () => send('pin-en')),
    vscode.commands.registerCommand('autoInput.preferChinese', () => send('pin-zh')),
    vscode.commands.registerCommand('autoInput.preferKeep', () => send('pin-keep')),
    vscode.commands.registerCommand('autoInput.clearPreferences', () => send('clear')),
    vscode.workspace.onDidChangeConfiguration(event => {
      if (event.affectsConfiguration('autoInput')) start();
    }),
    { dispose: shutdown }
  );
  start();
}

function deactivate() { stopActive(); }
module.exports = { activate, deactivate };
