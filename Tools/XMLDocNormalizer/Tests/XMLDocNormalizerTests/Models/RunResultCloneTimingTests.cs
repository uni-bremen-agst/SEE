using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Models
{
    /// <summary>
    /// Tests for cloning timing-related values in <see cref="RunResult"/>.
    /// </summary>
    public sealed class RunResultCloneTimingTests
    {
        /// <summary>
        /// Ensures that cloning a run result preserves the analysis duration together with the other aggregated counters.
        /// </summary>
        [Fact]
        public void Clone_WithTimingValues_CopiesAnalysisDurationAndCounters()
        {
            RunResult original = new()
            {
                Sloc = 1234,
                AnalysisDurationMs = 13810,
                ChangedFiles = 2
            };

            original.AccumulateFindings(
            [
                TestFindingFactory.Create("DOC200", Severity.Warning),
                TestFindingFactory.Create("DOC210", Severity.Error)
            ]);

            original.AccumulateTotals(
                new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["MethodsTotal"] = 5
                });

            RunResult clone = original.Clone();

            Assert.NotSame(original, clone);
            Assert.Equal(1234, clone.Sloc);
            Assert.Equal(13810L, clone.AnalysisDurationMs);
            Assert.Equal(2, clone.ChangedFiles);
            Assert.Equal(original.FindingCount, clone.FindingCount);
            Assert.Equal(original.ErrorCount, clone.ErrorCount);
            Assert.Equal(original.WarningCount, clone.WarningCount);
            Assert.Equal(original.SuggestionCount, clone.SuggestionCount);
            Assert.Equal(original.SmellCounts["DOC200"], clone.SmellCounts["DOC200"]);
            Assert.Equal(original.SmellCounts["DOC210"], clone.SmellCounts["DOC210"]);
            Assert.Equal(original.Totals["MethodsTotal"], clone.Totals["MethodsTotal"]);
        }
    }
}
