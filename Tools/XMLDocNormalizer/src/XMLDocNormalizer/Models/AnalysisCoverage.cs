namespace XMLDocNormalizer.Models;

/// <summary>Compiler-independent product report, separate from finding counts.</summary>
internal sealed record AnalysisCoverageReport(
    string Execution, string Completion, string Scope, string Mode, string[] ReasonCodes);

/// <summary>Monotone aggregation of outcomes observed during the existing analysis.</summary>
internal sealed class AnalysisCoverage
{
    private readonly HashSet<string> reasons = new(StringComparer.Ordinal);
    private readonly object sync = new();
    private bool observed, incomplete, unsupported, failed;

    /// <summary>Gets the selected scope.</summary>
    public string Scope { get; private set; } = "Unspecified";
    /// <summary>Gets the configured exception mode.</summary>
    public string Mode { get; private set; } = "Unspecified";

    /// <summary>Records the scope of an executed operation.</summary>
    public void Begin(string scope, ExceptionAnalysisMode mode)
    {
        lock (sync) { observed = true; Scope = scope; Mode = mode.ToString(); }
    }

    /// <summary>Records missing coverage.</summary>
    public void Incomplete(string reason) { lock (sync) { incomplete = true; reasons.Add(reason); } }
    /// <summary>Records an unsupported input or semantic profile.</summary>
    public void Unsupported(string reason) { lock (sync) { unsupported = true; reasons.Add(reason); } }
    /// <summary>Records technical failure without claiming a complete result.</summary>
    public void Fail(string reason) { lock (sync) { failed = true; Incomplete(reason); } }

    /// <summary>Merges outcomes without dependence on order or finding counts.</summary>
    public void Merge(AnalysisCoverage other)
    {
        other = other.Clone();
        lock (sync)
        {
            bool unknownLeft = !observed && !incomplete && !unsupported && !failed;
            bool unknownRight = !other.observed && !other.incomplete && !other.unsupported && !other.failed;
            observed |= other.observed; incomplete |= other.incomplete;
            unsupported |= other.unsupported; failed |= other.failed;
            reasons.UnionWith(other.reasons);
            Scope = MergeLabel(Scope, other.Scope); Mode = MergeLabel(Mode, other.Mode);
            if (unknownLeft || unknownRight) Incomplete("CoverageNotAssessed");
        }
    }

    private static string MergeLabel(string left, string right)
        => left == "Unspecified" ? right : right == "Unspecified" || left == right ? left : "Mixed";

    /// <summary>Creates a detached, deterministically ordered public snapshot.</summary>
    public AnalysisCoverageReport Report()
    {
        lock (sync) return new(
            failed ? "Failed" : observed ? "Succeeded" : "NotRun",
            incomplete ? "Incomplete" : unsupported ? "Unsupported" : observed ? "Complete" : "NotAssessed",
            Scope, Mode, reasons.Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>Creates a copy without shared mutable reason storage.</summary>
    public AnalysisCoverage Clone()
    {
        lock (sync)
        {
            AnalysisCoverage copy = new()
            { observed = observed, incomplete = incomplete, unsupported = unsupported, failed = failed, Scope = Scope, Mode = Mode };
            copy.reasons.UnionWith(reasons); return copy;
        }
    }
}
