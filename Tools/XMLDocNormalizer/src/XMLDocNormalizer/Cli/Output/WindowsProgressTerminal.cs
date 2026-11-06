using System.Runtime.InteropServices;

namespace XMLDocNormalizer.Cli.Output;

/// <summary>Uses only a verified Windows stderr console handle; never emits ANSI or writes progress to stdout.</summary>
internal sealed class WindowsProgressTerminal : IProgressTerminal
{
    private readonly IntPtr handle;
    private readonly TextWriter writer;
    private readonly ushort normal;
    private bool attributesAvailable = true;
    private readonly bool color;
    private WindowsProgressTerminal(IntPtr handle, TextWriter writer, ushort normal, bool color)
    { this.handle = handle; this.writer = writer; this.normal = normal; this.color = color; }
    public bool SupportsColor => color && attributesAvailable;

    internal static IProgressTerminal? TryCreate(TextWriter writer)
    {
        if (!OperatingSystem.IsWindows() || Console.IsErrorRedirected || Console.IsOutputRedirected) return null;
        IntPtr error = GetStdHandle(-12), output = GetStdHandle(-11);
        if (!GetConsoleMode(error, out _) || !GetConsoleScreenBufferInfo(error, out var stderr)
            || !GetConsoleScreenBufferInfo(output, out var stdout)
            || stderr.Size.X != stdout.Size.X || stderr.Size.Y != stdout.Size.Y
            || stderr.Cursor.X != stdout.Cursor.X || stderr.Cursor.Y != stdout.Cursor.Y
            || stderr.Window.Left != 0 || stderr.Window.Right != stdout.Window.Right
            || !SetConsoleCursorPosition(error, stderr.Cursor)) return null;
        bool ci = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        return new WindowsProgressTerminal(error, writer, stderr.Attributes,
            ProgressColorPolicy.Enable(false, ci, Environment.GetEnvironmentVariable("NO_COLOR"), Environment.GetEnvironmentVariable("TERM")));
    }

    public bool TryGetState(out ProgressTerminalState state)
    {
        state = default;
        if (!GetConsoleScreenBufferInfo(handle, out var info) || info.Window.Left != 0) return false;
        state = new(info.Size.X, info.Size.Y, info.Window.Top, info.Window.Bottom - info.Window.Top + 1,
            info.Window.Right - info.Window.Left + 1, info.Cursor.X, info.Cursor.Y);
        return true;
    }

    public bool TrySetCursor(int column, int row)
        => column is >= 0 and <= short.MaxValue && row is >= 0 and <= short.MaxValue
            && SetConsoleCursorPosition(handle, new((short)column, (short)row));

    public bool TryWriteRow(int row, string text)
        => TryWriteRow(row, text, ProgressTone.Neutral);

    public bool TryWriteRow(int row, string text, ProgressTone tone)
    {
        if (row is < 0 or > short.MaxValue) return false;
        if (text.Length == 0) return true;
        // Set cell attributes only, retaining the user's background and global stdout/stderr text attributes.
        ushort attributes = Attributes(normal, SupportsColor ? tone : ProgressTone.Neutral);
        if (attributesAvailable && (!FillConsoleOutputAttribute(handle, attributes, (uint)text.Length, new(0, (short)row), out uint colored)
            || colored != text.Length)) attributesAvailable = false;
        return WriteConsoleOutputCharacterW(handle, text, (uint)text.Length, new(0, (short)row), out uint written) && written == text.Length;
    }

    internal static ushort Attributes(ushort normal, ProgressTone tone)
        => tone == ProgressTone.Neutral ? normal : (ushort)((normal & 0xfff0) | (tone switch
        {
            ProgressTone.Active => 6, ProgressTone.Completed => 10, ProgressTone.Notice => 14,
            ProgressTone.Error => 12, ProgressTone.Muted => 8, _ => normal & 15
        }));

    public void ReserveLines(int count) { for (int i = 0; i < count; i++) writer.WriteLine(); writer.Flush(); }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Coord(short x, short y) { internal readonly short X = x, Y = y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { internal short Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct BufferInfo { internal Coord Size, Cursor; internal ushort Attributes; internal Rect Window; internal Coord Maximum; }

    [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int standardHandle);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetConsoleScreenBufferInfo(IntPtr handle, out BufferInfo info);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetConsoleCursorPosition(IntPtr handle, Coord position);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteConsoleOutputCharacterW(IntPtr handle, string text, uint length, Coord position, out uint written);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FillConsoleOutputAttribute(IntPtr handle, ushort attributes, uint length, Coord position, out uint written);
}
