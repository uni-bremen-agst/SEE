namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Pure closed selection rule for the two exact proven compiler builds; no execution or discovery.</summary>
    internal static class ExceptionFlowAnalyzerSelectionPolicy
    {
        internal const string CurrentCompilerVersion = "5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09";
        internal const string HistoricalCompilerVersion = "5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0";

        /// <summary>Decides from trusted Main evidence using exact ordinal identities, never a default/fallback.</summary>
        internal static ExceptionFlowAnalyzerSelectionDecision Select(ExceptionFlowAnalyzerSelectionContext? context)
        {
            ExceptionFlowAnalyzerSelectionDecision Fail(ExceptionFlowAnalyzerSelectionFailureCode code, string message)
                => new(ExceptionFlowAnalyzerSelection.Unspecified, new(code, message));

            if (context == null)
            {
                return Fail(ExceptionFlowAnalyzerSelectionFailureCode.MissingContext, "Validated selection context is required.");
            }
            if (context.Provenance is not (ExceptionFlowCompilerProvenance.CurrentCompilation or ExceptionFlowCompilerProvenance.ValidatedPortablePdb))
            {
                return Fail(ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedProvenance, "An explicit trusted C# compiler provenance is required.");
            }
            if (string.IsNullOrWhiteSpace(context.CompilerVersion))
            {
                return Fail(ExceptionFlowAnalyzerSelectionFailureCode.MissingCompilerVersion, "Exact compiler-version evidence is required.");
            }
            if (string.Equals(context.CompilerVersion, CurrentCompilerVersion, StringComparison.Ordinal))
            {
                return new(ExceptionFlowAnalyzerSelection.Current, null);
            }
            if (string.Equals(context.CompilerVersion, HistoricalCompilerVersion, StringComparison.Ordinal))
            {
                return context.Provenance == ExceptionFlowCompilerProvenance.ValidatedPortablePdb
                    ? new(ExceptionFlowAnalyzerSelection.Historical, null)
                    : Fail(ExceptionFlowAnalyzerSelectionFailureCode.ConflictingEvidence,
                        "Current-owned compilation evidence contradicts the Historical compiler requirement.");
            }
            return Fail(ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedCompilerVersion, "The exact required compiler build is not supported.");
        }
    }
}
