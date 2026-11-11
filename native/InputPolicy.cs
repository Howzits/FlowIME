using System;
using System.Text.RegularExpressions;

internal sealed class Snapshot {
    internal IntPtr Window, Focus;
    internal string Identity, Mode, Reason;
}

internal static class Rules {
    internal static bool IsFindInput(string metadata) {
        // Structural classes used by VS Code's SimpleFindWidget and FindInput.
        // Do not match arbitrary "search" text or a webview/chat container alone.
        return Regex.IsMatch(metadata ?? "", @"(?:^|\s)(?:simple-find-part(?:-wrapper)?|webview-find-widget|find-widget|monaco-findInput)(?:\s|$)", RegexOptions.IgnoreCase);
    }
    internal static bool IsFindLabel(string label) {
        // Fallback when UI Automation omits the find widget's structural wrappers.
        return Regex.IsMatch(label ?? "", @"^(?:Find|查找|尋找|查尋)(?:$|\s*[（(])", RegexOptions.IgnoreCase);
    }
    internal static string Decide(string process, string metadata, bool editable, bool terminalName, bool codeEditor = false, bool otherInputsChinese = true, bool findInput = false) {
        if (!Regex.IsMatch(process, @"^Code( - Insiders)?$", RegexOptions.IgnoreCase)) return null;
        // Find widgets can live inside chat or terminal containers and can refocus
        // or recreate their input while updating results. Never switch their IME.
        if (findInput || IsFindInput(metadata)) return null;
        if (terminalName || Regex.IsMatch(metadata, @"xterm|terminal[ -]?(input|textarea)", RegexOptions.IgnoreCase)) return "en";
        // Chat extensions may embed Monaco too; semantic chat containers take priority.
        bool chatEditor = Regex.IsMatch(metadata, @"chat[-_]input|chat[-_]editor|interactive[-_]input|chatgpt|codex", RegexOptions.IgnoreCase);
        if (editable && (chatEditor || (!codeEditor && otherInputsChinese))) return "zh";
        return null;
    }
    internal static void SelfTest() {
        foreach (string widget in new[] { "simple-find-part", "simple-find-part-wrapper", "webview-find-widget", "find-widget", "monaco-findInput" }) {
            foreach (string container in new[] { "markdown-preview webview", "chat-input codex", "xterm", "editor" }) {
                foreach (bool otherInputsChinese in new[] { true, false }) {
                    if (Decide("Code", container + " " + widget, true, container == "xterm", false, otherInputsChinese) != null)
                        throw new Exception("Find widget must preserve IME: " + widget);
                }
            }
        }
        foreach (string label in new[] { "Find", "查找", "尋找", "Find (Up/Down for history)", "查找（历史记录）" }) {
            if (!IsFindLabel(label) || Decide("Code", "webview", true, false, false, true, IsFindLabel(label)) != null)
                throw new Exception("Find label fallback failed");
        }
        foreach (string label in new[] { "Find a solution", "查找问题的解决方法", "Search", "" })
            if (IsFindLabel(label)) throw new Exception("Find label fallback too broad");
        if (Decide("Code", "chat-input research-tools", true, false) != "zh" ||
            Decide("Code", "xterm-helper-textarea", true, true) != "en" ||
            Decide("Code", "ordinary-search", true, false) != "zh" ||
            Decide("Code", "ordinary-search", true, false, false, false) != null)
            throw new Exception("Find protection changed ordinary input policy");

        var gate = new FocusDebouncer();
        for (int i = 0; i < 12; i++) {
            // Simulate redraws and focus changes on each result update.
            var search = new Snapshot { Identity = "find-" + i, Mode = Decide("Code", "markdown-preview simple-find-part", true, false) };
            gate.Update(search, i * 100);
            if (search.Mode != null) throw new Exception("Search redraw must not request an IME change");
            if (i % 2 == 0) gate.Update(null, i * 100 + 50);
        }
        var chat = new Snapshot { Identity = "chat", Mode = Decide("Code", "chat-input", true, false) };
        if (gate.Update(chat, 2000) || !gate.Update(chat, 2000 + FocusDebouncer.DelayMilliseconds) || chat.Mode != "zh")
            throw new Exception("Chat switching must resume after find");
        var terminal = new Snapshot { Identity = "terminal", Mode = Decide("Code", "xterm", true, true) };
        if (gate.Update(terminal, 3000) || !gate.Update(terminal, 3000 + FocusDebouncer.DelayMilliseconds) || terminal.Mode != "en")
            throw new Exception("Terminal switching must resume after find");
        Console.WriteLine("Find widget preservation checks passed.");
    }
}
