using System.Text;

namespace XMLDocNormalizer.Cli.Output;

/// <summary>Character-cell console operations, isolated from analysis and progress counters.</summary>
internal interface IProgressTerminal
{
    bool TryGetState(out ProgressTerminalState state);
    bool TrySetCursor(int column, int row);
    bool TryWriteRow(int row, string text);
    bool SupportsColor => false;
    bool TryWriteRow(int row, string text, ProgressTone tone) => TryWriteRow(row, text);
    void ReserveLines(int count);
}

internal readonly record struct ProgressTerminalState(int BufferWidth, int BufferHeight,
    int WindowTop, int WindowHeight, int WindowWidth, int CursorColumn, int CursorRow);

/// <summary>A fixed console region. Periodic updates touch changed rows only and never move the cursor.</summary>
internal sealed class ProgressRegion(IProgressTerminal terminal, int fixedHeight = 0)
{
    private readonly List<ProgressRow> previous = [];
    private int height;
    private int top, bufferWidth, windowWidth;
    private bool reserved, failed;

    internal bool IsReserved => reserved && !failed;
    internal bool TryGetState(out ProgressTerminalState state) => terminal.TryGetState(out state);

    internal bool Render(IReadOnlyList<string> rows)
        => Render(rows.Select(text => new ProgressRow(text)).ToArray());

    internal bool Render(IReadOnlyList<ProgressRow> rows)
    {
        int required = fixedHeight > 0 ? fixedHeight : rows.Count;
        if (failed || rows.Count > required || !terminal.TryGetState(out var state) || !Usable(state, required)) return Fail();
        if (reserved && !Resize(state, required)) return Fail();
        height = required;
        if (!reserved && !Reserve(state)) return Fail();
        if (!terminal.TryGetState(out state) || !Usable(state, height)) return Fail();
        // A buffer-width change can reflow existing rows; their original coordinates are no longer trustworthy.
        if (state.BufferWidth != bufferWidth || top < state.WindowTop || top + height >= state.WindowTop + state.WindowHeight)
            return Fail();
        int columns = Math.Min(state.BufferWidth, state.WindowWidth) - 1;
        bool resized = windowWidth != state.WindowWidth;
        for (int index = 0; index < height; index++)
        {
            ProgressRow row = index < rows.Count ? rows[index] : new("");
            row = row with { Text = Fit(row.Text, columns), Tone = terminal.SupportsColor ? row.Tone : ProgressTone.Neutral };
            ProgressRow old = index < previous.Count ? previous[index] : new("");
            if (!resized && old == row) { if (index == previous.Count) previous.Add(row); continue; }
            int clear = Math.Min(state.BufferWidth - 1, Math.Max(old.Text.Length, row.Text.Length));
            if (!terminal.TryWriteRow(top + index, row.Text.PadRight(clear), row.Tone)) return Fail();
            if (index < previous.Count) previous[index] = row; else previous.Add(row);
        }
        windowWidth = state.WindowWidth;
        return true;
    }

    internal bool Suspend()
    {
        if (!IsReserved) return false;
        if (!terminal.TryGetState(out var state) || !Usable(state, height) || state.BufferWidth != bufferWidth
            || top < state.WindowTop || top + height >= state.WindowTop + state.WindowHeight) return Fail();
        // Only actual console text interrupts the region. Empty/file-only report actions never reach here.
        for (int i = 0; i < height; i++)
            if (!terminal.TryWriteRow(top + i, new string(' ', i < previous.Count ? previous[i].Text.Length : 0), ProgressTone.Neutral)) return Fail();
        if (!terminal.TrySetCursor(0, top)) return Fail();
        reserved = false;
        previous.Clear();
        return true;
    }

    internal void LeaveVisible()
    {
        if (!IsReserved || !terminal.TryGetState(out var state) || state.BufferWidth != bufferWidth) return;
        if (top >= state.WindowTop && top + height < state.WindowTop + state.WindowHeight)
            terminal.TrySetCursor(0, top + height);
    }

    private bool Reserve(ProgressTerminalState state)
    {
        if (!Usable(state, height)) return false;
        if (state.CursorColumn != 0) terminal.ReserveLines(1);
        terminal.ReserveLines(height);
        if (!terminal.TryGetState(out state)) return false;
        top = state.CursorRow - height;
        bufferWidth = state.BufferWidth; windowWidth = state.WindowWidth;
        reserved = top >= state.WindowTop && top + height < state.WindowTop + state.WindowHeight;
        return reserved;
    }

    private bool Resize(ProgressTerminalState state, int required)
    {
        if (state.BufferWidth != bufferWidth || top < state.WindowTop || top + height >= state.WindowTop + state.WindowHeight) return false;
        if (required == height) return true;
        if (required < height)
        {
            for (int i = required; i < height; i++)
                if (!terminal.TryWriteRow(top + i, new string(' ', i < previous.Count ? previous[i].Text.Length : 0), ProgressTone.Neutral)) return false;
            previous.RemoveRange(required, Math.Max(0, previous.Count - required));
            return terminal.TrySetCursor(0, top + required);
        }
        if (!terminal.TrySetCursor(0, top + height)) return false;
        terminal.ReserveLines(required - height);
        if (!terminal.TryGetState(out state)) return false;
        top = state.CursorRow - required;
        return top >= state.WindowTop && top + required < state.WindowTop + state.WindowHeight;
    }

    private static bool Usable(ProgressTerminalState state, int rows)
        => state.WindowWidth >= 40 && state.BufferWidth >= state.WindowWidth && state.WindowHeight > rows
            && state.BufferHeight > rows && state.CursorRow >= 0;

    private bool Fail() { failed = true; return false; }

    internal static string Fit(string text, int columns)
    {
        // Native console fonts disagree on wide/combining glyph widths. Use one ASCII cell per Unicode scalar
        // in the live dashboard only; ordinary output and serialized results retain their original Unicode.
        var result = new System.Text.StringBuilder(); int cells = 0; bool truncated = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (cells++ >= columns) { truncated = true; break; }
            result.Append(System.Text.Rune.IsControl(rune) ? ' ' : rune.Value is >= 32 and <= 126 ? (char)rune.Value : '?');
        }
        if (truncated && result.Length >= 3) { result.Length -= 3; result.Append("..."); }
        return result.ToString();
    }
}
