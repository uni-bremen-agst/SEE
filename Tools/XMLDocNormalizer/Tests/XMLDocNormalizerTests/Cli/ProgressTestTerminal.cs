using XMLDocNormalizer.Cli.Output;

namespace XMLDocNormalizerTests.Cli;

/// <summary>A character-cell screen with separate native row writes and ordinary console text.</summary>
internal sealed class ProgressTestTerminal(TextWriter? transcript = null, int width = 300) : IProgressTerminal
{
    internal int Width { get; set; } = width;
    internal int BufferWidth { get; set; } = width;
    internal int Height { get; set; } = 40;
    internal bool Available { get; set; } = true;
    public bool SupportsColor { get; set; }
    internal int Reserved { get; private set; }
    internal int CursorMoves { get; private set; }
    internal int Row { get; private set; }
    internal int Column { get; private set; }
    internal List<(int Row, string Text)> Writes { get; } = [];
    internal List<(int Row, string Text, ProgressTone Tone)> StyledWrites { get; } = [];
    private readonly Dictionary<int, string> screen = [];
    internal string Screen => string.Join("\n", Enumerable.Range(0, Row + 1).Select(i => screen.GetValueOrDefault(i, "").TrimEnd()));
    internal string EntireScreen => string.Join("\n", screen.OrderBy(p => p.Key).Select(p => p.Value.TrimEnd()));

    public bool TryGetState(out ProgressTerminalState state)
    {
        state = new(BufferWidth, 10000, 0, Height, Width, Column, Row);
        return Available;
    }
    public bool TrySetCursor(int column, int row)
    {
        if (!Available || row < 0 || row >= Height || column < 0 || column >= BufferWidth) return false;
        Column = column; Row = row; CursorMoves++; return true;
    }
    public bool TryWriteRow(int row, string text)
    {
        if (!Available) return false;
        Assert.InRange(row, 0, Height - 1);
        Assert.True(text.Length < BufferWidth, "A native row write must never wrap.");
        string old = screen.GetValueOrDefault(row, "");
        screen[row] = text + (old.Length > text.Length ? old[text.Length..] : "");
        Writes.Add((row, text)); transcript?.Write("\r" + text); return true;
    }
    public bool TryWriteRow(int row, string text, ProgressTone tone)
    {
        StyledWrites.Add((row, text, tone));
        return TryWriteRow(row, text);
    }
    public void ReserveLines(int count) { Reserved += count; Row += count; Column = 0; }
    internal void WriteConsole(string text)
    {
        foreach (char c in text)
        {
            if (c == '\r') { Column = 0; continue; }
            if (c == '\n') { Row++; Column = 0; continue; }
            string old = screen.GetValueOrDefault(Row, "").PadRight(Column + 1);
            screen[Row] = old[..Column] + c + old[(Column + 1)..]; Column++;
        }
    }
}
