using System.Text;

namespace XMLDocNormalizer.Cli.Output;

/// <summary>Interrupts the dashboard lazily, only when a report action actually writes console text.</summary>
internal sealed class ProgressOutputWriter(TextWriter writer, Action beforeWrite) : TextWriter
{
    public override Encoding Encoding => writer.Encoding;
    public override void Flush() => writer.Flush();
    public override void Write(char value) { beforeWrite(); writer.Write(value); }
    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        beforeWrite(); writer.Write(value);
    }
    public override void Write(char[] buffer, int index, int count)
    {
        if (count == 0) return;
        beforeWrite(); writer.Write(buffer, index, count);
    }
    public override void Write(ReadOnlySpan<char> buffer)
    {
        if (buffer.IsEmpty) return;
        beforeWrite(); writer.Write(buffer);
    }
}
