using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Utils
{
    /// <summary>
    /// Tests for timing-related metric calculations in <see cref="RunMetricsCalculator"/>.
    /// </summary>
    public sealed class RunMetricsCalculatorTimingTests
    {
        /// <summary>
        /// Ensures that analysis duration per KLOC is calculated from the configured duration and SLOC.
        /// </summary>
        [Fact]
        public void From_WithPositiveSloc_CalculatesAnalysisDurationPerKSloc()
        {
            RunResult result = CreateRunResult(sloc: 2000, analysisDurationMs: 1500);

            var metrics = RunMetricsCalculator.From(result);

            Assert.Equal(1500L, metrics.AnalysisDurationMs);
            Assert.Equal(750d, metrics.AnalysisDurationMsPerKSLoc, precision: 6);
        }

        /// <summary>
        /// Ensures that analysis duration per KLOC remains zero when SLOC is zero.
        /// </summary>
        [Fact]
        public void From_WithZeroSloc_LeavesAnalysisDurationPerKSlocAtZero()
        {
            RunResult result = CreateRunResult(sloc: 0, analysisDurationMs: 1500);

            var metrics = RunMetricsCalculator.From(result);

            Assert.Equal(1500L, metrics.AnalysisDurationMs);
            Assert.Equal(0d, metrics.AnalysisDurationMsPerKSLoc, precision: 6);
        }

        /// <summary>
        /// Creates a run result with deterministic counters for timing-metric tests.
        /// </summary>
        /// <param name="sloc">The source lines of code.</param>
        /// <param name="analysisDurationMs">The analysis duration in milliseconds.</param>
        /// <returns>A populated run result.</returns>
        private static RunResult CreateRunResult(int sloc, long analysisDurationMs)
        {
            RunResult result = new()
            {
                Sloc = sloc,
                AnalysisDurationMs = analysisDurationMs
            };

            result.AccumulateFindings(
            [
                TestFindingFactory.Create("DOC200", Severity.Warning),
                TestFindingFactory.Create("DOC210", Severity.Warning)
            ]);

            return result;
        }
    }
}
