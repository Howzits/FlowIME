using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Automation;

internal static class Program {
    static volatile bool running = true;
    static bool otherInputsChinese = true;
    static int pollInterval = 100;
    static bool remember = true;
    static readonly Preferences preferences = new Preferences();
    sealed class Command { internal string Text; internal Action<string> Reply; }
    static readonly ConcurrentQueue<Command> commands = new ConcurrentQueue<Command>();
    static volatile bool primary;
    // Use control types and structural IDs/classes, independent of chat provider.
    // Typed text, values and document contents are never read or logged.
    internal static Snapshot ReadFocus() {
        IntPtr window = Native.GetForegroundWindow();
        uint pid;
        Native.GetWindowThreadProcessId(window, out pid);
        string process;
        try { using (var p = Process.GetProcessById((int)pid)) process = p.ProcessName; } catch { return null; }
        if (!Regex.IsMatch(process, @"^Code( - Insiders)?$", RegexOptions.IgnoreCase)) return null;
        var element = AutomationElement.FocusedElement;
        if (element == null) return null;
        // Chromium accessibility providers can live in a renderer process.
        // Verify that provider belongs to Code as well as the foreground app.
        try {
            using (var provider = Process.GetProcessById(element.Current.ProcessId))
                if (!Regex.IsMatch(provider.ProcessName, @"^Code( - Insiders)?$", RegexOptions.IgnoreCase)) return null;
        } catch { return null; }
        var current = element.Current;
        string identity = window + ":" + String.Join(".", element.GetRuntimeId());
        bool editable = current.ControlType == ControlType.Edit;
        bool codeEditor = Regex.IsMatch(current.ClassName ?? "", @"\binputarea\b|\bnative-edit-context\b", RegexOptions.IgnoreCase);
        object pattern;
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out pattern)) editable = !((ValuePattern)pattern).Current.IsReadOnly;
        bool password = current.IsPassword;
        editable &= current.IsEnabled && !password;
        string metadata = "";
        bool terminalName = false;
        bool findLabel = false;
        for (int depth = 0; element != null && depth < 20; depth++) {
            current = element.Current;
            // Never use the top-level window title to guess the control's purpose.
            if (current.ControlType == ControlType.Window) break;
            metadata += " " + current.AutomationId + " " + current.ClassName;
            // Terminal textarea accessible labels are stable and distinct from their value.
            if (depth == 0) {
                terminalName = Regex.IsMatch(current.Name ?? "", @"^(Terminal input|终端输入|终端输入框)$", RegexOptions.IgnoreCase);
                findLabel = editable && Rules.IsFindLabel(current.Name);
            }
            element = TreeWalker.ControlViewWalker.GetParent(element);
        }
        bool findInput = findLabel || Rules.IsFindInput(metadata);
        string mode = password ? null : Rules.Decide(process, metadata, editable, terminalName, codeEditor, otherInputsChinese, findInput);
        string reason = password ? "password" : findInput ? "search-input" : mode == "en" ? "terminal" : mode == "zh" ? "editable-input" : codeEditor ? "code-editor" : !editable ? "readonly-or-noneditable" : "scope-preserve";
        return new Snapshot { Window = window, Focus = Native.Focus(window), Identity = identity, Mode = mode, Reason = reason };
    }
    static bool StillFocused(Snapshot s) {
        if (Native.GetForegroundWindow() != s.Window || Native.Focus(s.Window) != s.Focus) return false;
        var fresh = ReadFocus();
        return fresh != null && fresh.Identity == s.Identity && fresh.Mode == s.Mode;
    }
    static void Apply(Snapshot s, string target) {
        if (s.Focus == IntPtr.Zero || !StillFocused(s)) return;
        IntPtr layout = Layouts.Resolve(target, s.Window);
        if (layout == IntPtr.Zero) { Console.WriteLine("No configured input method available for: " + target + " (keeping current input method)"); return; }
        bool posted = Native.PostMessage(s.Focus, 0x0050, IntPtr.Zero, layout);
        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " target=" + target + " layoutRequest=" + posted);
        // Briefly allow the posted layout request to reach the focused control.
        Thread.Sleep(50);
        if (!running || !StillFocused(s)) return;
        IntPtr ime = Native.ImmGetDefaultIMEWnd(s.Focus);
        if (ime == IntPtr.Zero) return;
        UIntPtr result;
        // Change only the native conversion flag, preserving width and other user flags.
        IntPtr read = Native.SendMessageTimeout(ime, 0x0283, new IntPtr(1), IntPtr.Zero, 2, 100, out result);
        if (read != IntPtr.Zero) {
            long flags = (long)(result.ToUInt64() & 0xffffffff);
            flags = target == "zh" ? flags | 1 : flags & ~1L;
            Native.SendMessageTimeout(ime, 0x0283, new IntPtr(2), new IntPtr(flags), 2, 100, out result);
        }
        Native.SendMessageTimeout(ime, 0x0283, new IntPtr(6), new IntPtr(target == "zh" ? 1 : 0), 2, 100, out result);
    }
    static string Observe(Snapshot s) {
        if (s.Focus == IntPtr.Zero || !StillFocused(s)) return null;
        uint pid;
        string language = Layouts.Language(Native.GetKeyboardLayout(Native.GetWindowThreadProcessId(s.Focus, out pid)));
        if (language == "en") return "en";
        if (language != "zh") return null;
        IntPtr ime = Native.ImmGetDefaultIMEWnd(s.Focus);
        if (ime == IntPtr.Zero) return null;
        UIntPtr open, flags;
        if (Native.SendMessageTimeout(ime, 0x0283, new IntPtr(5), IntPtr.Zero, 2, 80, out open) == IntPtr.Zero) return null;
        if (open.ToUInt64() == 0) return "en";
        if (Native.SendMessageTimeout(ime, 0x0283, new IntPtr(1), IntPtr.Zero, 2, 80, out flags) == IntPtr.Zero) return null;
        return (flags.ToUInt64() & 1) != 0 ? "zh" : "en";
    }
    static string Diagnose(Snapshot s) {
        return "diagnostic reason=" + (s == null ? "outside-vscode-or-no-accessible-focus" : s.Reason) +
            " default=" + (s == null ? "keep" : s.Mode ?? "keep") +
            " preference=" + (s == null || s.Mode != "zh" || !remember ? "none" : preferences.Get(s.Identity, "none")) +
            " observed=" + (s == null || s.Mode == null ? "unknown" : Observe(s) ?? "unknown") +
            " remember=" + remember + " interval=" + pollInterval + " debounce=" + FocusDebouncer.DelayMilliseconds;
    }
    static int SelfTest() {
        Rules.SelfTest();
        Layouts.SelfTest();
        Preferences.SelfTest();
        FocusDebouncer.SelfTest();
        ManualPreferenceTracker.SelfTest();
        CommandBus.SelfTest();
        if (Rules.Decide("Code", "xterm-helper-textarea", true, false) != "en" ||
            Rules.Decide("Code", "search", true, false, false, false) != null ||
            Rules.Decide("Code", "chat-input", true, false, true, false) != "zh" ||
            Rules.Decide("Code", "Codex chatgpt.sidebarView", true, false) != "zh" ||
            Rules.Decide("Code", "Codex xterm", true, false) != "en" ||
            Rules.Decide("Code", "editor", true, false, true) != null ||
            Rules.Decide("Code", "unknown-provider-composer", true, false) != "zh" ||
            Rules.Decide("Code", "dialog input", true, false) != "zh" ||
            Rules.Decide("Code", "chat-input-editor inputarea", true, false, true) != "zh" ||
            Rules.Decide("Code", "xterm chat-input", true, false, true) != "en" ||
            Rules.Decide("Code", "readonly", false, false) != null ||
            Rules.Decide("Code", "Codex", false, false) != null ||
            Rules.Decide("WeChat", "Codex", true, false) != null ||
            Rules.Decide("Code - Insiders", "", true, true) != "en") return 1;
        Console.WriteLine("Focus classification checks passed."); return 0;
    }
    [MTAThread] static int Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;
        foreach (string arg in args) {
            if (arg == "--other-inputs=preserve") otherInputsChinese = false;
            if (arg == "--remember=false") remember = false;
            if (arg.StartsWith("--poll=")) {
                int parsed;
                if (Int32.TryParse(arg.Substring(7), out parsed)) pollInterval = Math.Max(50, Math.Min(1000, parsed));
            }
        }
        if (args.Length > 0 && args[0] == "--self-test") return SelfTest();
        if (args.Length > 0 && args[0] == "--check-layouts") {
            foreach (string mode in new[] { "en", "zh" }) {
                IntPtr selected = Layouts.Resolve(mode, Native.GetForegroundWindow());
                Console.WriteLine(mode + " -> " + (selected == IntPtr.Zero ? "not configured" : Layouts.Language(selected)));
            }
            return 0;
        }
        if (args.Length > 0 && args[0] == "--probe") {
            var s = ReadFocus();
            Console.WriteLine(Diagnose(s));
            return 0;
        }
        // Parent closes stdin on extension disposal or crash; helper exits with it.
        Task.Run(delegate {
            try {
                string line;
                while ((line = Console.ReadLine()) != null) {
                    if (primary) commands.Enqueue(new Command { Text = line, Reply = Console.WriteLine });
                    else Console.WriteLine(CommandBus.Send(line));
                }
            } catch {} running = false;
        });
        bool owns = false;
        using (var mutex = new Mutex(false, "Local\\VSCode.AutoInput.Helper")) {
            var debounce = new FocusDebouncer();
            var clock = Stopwatch.StartNew();
            var manual = new ManualPreferenceTracker();
            string pendingCommand = null;
            Action<string> reply = Console.WriteLine;
            DateTime commandAt = DateTime.MaxValue;
            DateTime lastError = DateTime.MinValue;
            try {
                while (running) {
                    // Multiple VS Code windows share one active worker; others can take over.
                    if (!owns) {
                        try { owns = mutex.WaitOne(0); } catch (AbandonedMutexException) { owns = true; }
                        if (owns) { primary = true; CommandBus.Listen(delegate(string command, Action<string> respond) { commands.Enqueue(new Command { Text = command, Reply = respond }); }); }
                    }
                    if (owns) {
                        try {
                            Command command;
                            while (commands.TryDequeue(out command)) {
                                if (command.Text == "clear") {
                                    if (pendingCommand != null) { reply("command=superseded"); pendingCommand = null; }
                                    preferences.Clear(); manual.Reset(); debounce.Reset(); command.Reply("preferences=cleared");
                                }
                                else if (command.Text == "diagnose" || command.Text == "pin-en" || command.Text == "pin-zh" || command.Text == "pin-keep") {
                                    if (pendingCommand != null) reply("command=superseded");
                                    pendingCommand = command.Text; reply = command.Reply; commandAt = DateTime.UtcNow.AddSeconds(3);
                                } else command.Reply("command=unsupported");
                            }
                            var s = ReadFocus();
                            if (pendingCommand != null && DateTime.UtcNow >= commandAt) {
                                if (pendingCommand == "diagnose") reply(Diagnose(s));
                                else if (!remember) reply("preference=skipped reason=memory-disabled");
                                else if (s != null && s.Mode == "zh" && StillFocused(s)) {
                                    string mode = pendingCommand.Substring(4);
                                    preferences.Set(s.Identity, mode);
                                    reply("preference=" + mode + " source=command");
                                    if (mode != "keep") Apply(s, mode);
                                    debounce.Commit(s);
                                    if (mode != "keep") manual.Start(mode, Observe(s), clock.ElapsedMilliseconds);
                                    else manual.Reset();
                                } else reply("preference=skipped reason=not-an-eligible-input");
                                pendingCommand = null;
                            }
                            if (debounce.Update(s, clock.ElapsedMilliseconds)) {
                                manual.Reset();
                                string target = remember && s.Mode == "zh" ? preferences.Get(s.Identity, s.Mode) : s.Mode;
                                if (target != null && target != "keep") Apply(s, target);
                                else Console.WriteLine("focus=preserve");
                                if (remember && s.Mode == "zh" && target != "keep") manual.Start(target, Observe(s), clock.ElapsedMilliseconds);
                            } else if (!debounce.IsSettled(s)) {
                                manual.Reset();
                            } else if (remember && s.Mode == "zh" && preferences.Get(s.Identity, "") != "keep") {
                                string changed = manual.Update(Observe(s), clock.ElapsedMilliseconds);
                                if (changed != null) {
                                    preferences.Set(s.Identity, changed);
                                    Console.WriteLine("preference=" + changed + " source=observed-change");
                                }
                            }
                        } catch (Exception error) {
                            debounce.Reset(); manual.Reset();
                            if (pendingCommand != null && DateTime.UtcNow >= commandAt) {
                                reply("diagnostic reason=focus-query-error error=" + error.GetType().Name);
                                pendingCommand = null;
                            }
                            if ((DateTime.UtcNow - lastError).TotalSeconds > 10) {
                                Console.WriteLine("Focus query unavailable: " + error.GetType().Name);
                                lastError = DateTime.UtcNow;
                            }
                        }
                    }
                    Thread.Sleep(owns ? debounce.NextCheck(clock.ElapsedMilliseconds, pollInterval) : pollInterval);
                }
            } finally { if (owns) mutex.ReleaseMutex(); }
        }
        return 0;
    }
}
