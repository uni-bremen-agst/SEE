namespace XMLDocNormalizer.Cli.Output;

internal enum ProgressTone { Neutral, Active, Completed, Notice, Error, Muted }
internal readonly record struct ProgressRow(string Text, ProgressTone Tone = ProgressTone.Neutral);

internal static class ProgressColorPolicy
{
    internal static bool Enable(bool redirected, bool ci, string? noColor, string? terminal)
        => !redirected && !ci && string.IsNullOrEmpty(noColor)
            && !string.Equals(terminal, "dumb", StringComparison.OrdinalIgnoreCase);
}
