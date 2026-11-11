'use strict';
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const { readFileSync, existsSync } = require('node:fs');
const { join } = require('node:path');
const vm = require('node:vm');

function harness(initial = {}, language = 'zh-cn') {
  const settings = { enabled: true, otherInputs: 'chinese', pollInterval: 100, ...initial };
  const children = [];
  const commands = new Map();
  const timers = new Map();
  const status = { show() {}, dispose() {} };
  const output = { lines: [], appendLine(s) { this.lines.push(s); }, append(s) { this.lines.push(s); }, show() { this.shown = true; }, dispose() {} };
  let changed;
  const locale = language.toLowerCase();
  const bundlePath = [locale, locale.split('-')[0]]
    .map(value => join(__dirname, '..', 'l10n', `bundle.l10n.${value}.json`))
    .find(existsSync);
  const translations = bundlePath ? JSON.parse(readFileSync(bundlePath, 'utf8')) : {};
  const messages = [];
  const vscode = {
    l10n: { t: (message, ...args) => (translations[message] || message).replace(/\{(\d+)\}/g, (_, n) => String(args[n])) },
    StatusBarAlignment: { Right: 1 }, ConfigurationTarget: { Global: 1 },
    window: { createOutputChannel: () => output, createStatusBarItem: () => status, setStatusBarMessage(message) { messages.push(message); } },
    workspace: {
      getConfiguration: () => ({ get: (key, fallback) => settings[key] ?? fallback, update: async (key, value) => {
        settings[key] = value; changed({ affectsConfiguration: () => true });
      } }),
      onDidChangeConfiguration: handler => { changed = handler; return { dispose() {} }; }
    },
    commands: { registerCommand: (name, fn) => { commands.set(name, fn); return { dispose() {} }; } }
  };
  const spawn = (file, args) => {
    const worker = new EventEmitter();
    worker.stdout = new EventEmitter(); worker.stderr = new EventEmitter(); worker.stdin = new EventEmitter();
    worker.stdin.end = () => { worker.ended = true; };
    worker.stdin.writable = true;
    worker.messages = [];
    worker.stdin.write = text => worker.messages.push(text);
    worker.kill = () => { worker.killed = true; };
    worker.args = args;
    children.push(worker);
    return worker;
  };
  const sandbox = {
    module: { exports: {} }, process: { platform: 'win32' },
    require: name => name === 'vscode' ? vscode : name === 'child_process' ? { spawn } : require(name),
    setTimeout: fn => { const id = {}; timers.set(id, fn); return id; },
    clearTimeout: id => timers.delete(id)
  };
  vm.runInNewContext(readFileSync(join(__dirname, '..', 'extension.js'), 'utf8'), sandbox);
  const context = { subscriptions: [], asAbsolutePath: name => join(__dirname, '..', name) };
  sandbox.module.exports.activate(context);
  return { settings, children, commands, timers, status, output, messages,
    change() { changed({ affectsConfiguration: () => true }); },
    dispose() { sandbox.module.exports.deactivate(); },
    runRetry() { const [id, fn] = timers.entries().next().value; timers.delete(id); fn(); }
  };
}

test('settings reach the helper and configuration changes replace it', () => {
  const h = harness({ otherInputs: 'preserve', pollInterval: 400 });
  assert.deepEqual(Array.from(h.children[0].args), ['--other-inputs=preserve', '--poll=400', '--remember=true']);
  h.change();
  assert.equal(h.children[0].killed, true);
  assert.equal(h.children[0].ended, true);
  assert.equal(h.children.length, 2);
  h.dispose();
  assert.equal(h.children[1].killed, true);
});

test('partial messages distinguish requested target from failure', () => {
  const h = harness();
  const worker = h.children[0];
  worker.stdout.emit('data', Buffer.from('target=zh layoutReq'));
  assert.match(h.status.text, /自动/);
  worker.stdout.emit('data', Buffer.from('uest=True\r\n'));
  assert.match(h.status.text, /目标 中/);
  worker.stdout.emit('data', Buffer.from('target=en layoutRequest=False\n'));
  assert.match(h.status.text, /请求失败/);
  worker.stdout.emit('data', Buffer.from('focus=preserve\n'));
  assert.match(h.status.text, /保持/);
  h.dispose();
});

test('pause stops the helper, clears pending retry and resume starts it', async () => {
  const h = harness();
  h.children[0].emit('exit', 1);
  assert.equal(h.timers.size, 1);
  await h.commands.get('autoInput.toggle')();
  assert.equal(h.timers.size, 0);
  assert.match(h.status.text, /已暂停/);
  await h.commands.get('autoInput.toggle')();
  assert.equal(h.children.length, 2);
  h.dispose();
});

test('unexpected exits retry twice, disposed workers cannot update the status', () => {
  const h = harness();
  h.children[0].emit('exit', 1); h.runRetry();
  h.children[1].emit('exit', 1); h.runRetry();
  h.children[2].emit('exit', 1);
  assert.equal(h.timers.size, 0);
  assert.equal(h.children.length, 3);
  h.dispose();
  const text = h.status.text;
  h.children[0].stdout.emit('data', Buffer.from('target=zh layoutRequest=True\n'));
  assert.equal(h.status.text, text);
});

test('disabled extension does not start a helper', () => {
  const h = harness({ enabled: false });
  assert.equal(h.children.length, 0);
  assert.match(h.status.text, /已暂停/);
  h.dispose();
});

test('diagnostic and preference commands use the helper control channel', () => {
  const h = harness();
  h.commands.get('autoInput.diagnose')();
  h.commands.get('autoInput.preferEnglish')();
  h.commands.get('autoInput.preferChinese')();
  h.commands.get('autoInput.preferKeep')();
  h.commands.get('autoInput.clearPreferences')();
  assert.deepEqual(h.children[0].messages, ['diagnose\n', 'pin-en\n', 'pin-zh\n', 'pin-keep\n', 'clear\n']);
  h.children[0].stdout.emit('data', Buffer.from('diagnostic reason=code-editor default=keep\n'));
  assert.equal(h.output.shown, true);
  h.dispose();
});

test('diagnostic command reports a paused worker instead of starting another', () => {
  const h = harness({ enabled: false });
  h.commands.get('autoInput.diagnose')();
  assert.equal(h.children.length, 0);
  assert.match(h.output.lines.join('\n'), /助手未运行/);
  h.dispose();
});

test('preference memory can be disabled through configuration', () => {
  const h = harness({ rememberPreferences: false });
  assert.ok(h.children[0].args.includes('--remember=false'));
  h.dispose();
});

for (const language of ['en', 'fr', 'zh-cn', 'zh-tw']) {
  test(`UI follows VS Code display language (${language})`, async () => {
    const h = harness({}, language);
    const chinese = language.startsWith('zh');
    assert.match(h.status.text, chinese ? /自动/ : /Auto/);
    assert.match(h.status.tooltip, chinese ? /终端英文/ : /English for terminals/);
    h.children[0].stdout.emit('data', Buffer.from('target=en layoutRequest=True\n'));
    assert.match(h.status.text, chinese ? /目标 英/ : /Target EN/);
    h.commands.get('autoInput.diagnose')();
    assert.match(h.messages.at(-1), chinese ? /3 秒/ : /3 seconds/);
    h.children[0].stdout.emit('data', Buffer.from('preferences=cleared\n'));
    assert.match(h.messages.at(-1), chinese ? /已清除/ : /cleared/);
    await h.commands.get('autoInput.toggle')();
    assert.match(h.status.text, chinese ? /已暂停/ : /Paused/);
    h.dispose();
  });
}

test('all manifest translations resolve and runtime bundles cover every message', () => {
  const root = join(__dirname, '..');
  const metadata = JSON.parse(readFileSync(join(root, 'package.json'), 'utf8'));
  const english = JSON.parse(readFileSync(join(root, 'package.nls.json'), 'utf8'));
  assert.equal(metadata.l10n, './l10n');
  const chinese = JSON.parse(readFileSync(join(root, 'package.nls.zh-cn.json'), 'utf8'));
  assert.deepEqual(chinese, JSON.parse(readFileSync(join(root, 'package.nls.zh.json'), 'utf8')));
  const keys = JSON.stringify(metadata).match(/%([^%]+)%/g);
  for (const placeholder of keys) {
    const key = placeholder.slice(1, -1);
    assert.ok(english[key], key);
    assert.ok(chinese[key], key);
  }
  const source = readFileSync(join(root, 'extension.js'), 'utf8');
  const bundle = JSON.parse(readFileSync(join(root, 'l10n', 'bundle.l10n.zh-cn.json'), 'utf8'));
  assert.deepEqual(bundle, JSON.parse(readFileSync(join(root, 'l10n', 'bundle.l10n.zh.json'), 'utf8')));
  for (const match of source.matchAll(/vscode\.l10n\.t\((['"])(.*?)\1/g)) {
    assert.ok(bundle[match[2]], match[2]);
    assert.deepEqual(bundle[match[2]].match(/\{\d+\}/g), match[2].match(/\{\d+\}/g));
  }
});

test('default polling uses the responsive 100 ms interval', () => {
  const h = harness();
  assert.ok(h.children[0].args.includes('--poll=100'));
  h.dispose();
});
