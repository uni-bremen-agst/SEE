using System.Text;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides xUnit assertion helpers for collections of <see cref="Finding"/> instances.
    /// </summary>
    internal static class FindingAsserts
    {
        /// <summary>
        /// Asserts that the findings contain at least one finding with the specified smell id.
        /// </summary>
        /// <param name="findings">The findings to assert on.</param>
        /// <param name="smellId">The expected smell id (e.g. "DOC200").</param>
        public static void ContainsSmell(IEnumerable<Finding> findings, string smellId)
        {
            Assert.NotNull(findings);

            Assert.Contains(findings, f =>
                string.Equals(f.Smell.Id, smellId, StringComparison.Ordinal));
        }

        /// <summary>
        /// Asserts that the findings do not contain any finding with the specified smell id.
        /// </summary>
        /// <param name="findings">The findings to assert on.</param>
        /// <param name="smellId">The smell id that must not be present.</param>
        public static void DoesNotContainSmell(IEnumerable<Finding> findings, string smellId)
        {
            Assert.NotNull(findings);

            Assert.DoesNotContain(findings, f =>
                string.Equals(f.Smell.Id, smellId, StringComparison.Ordinal));
        }

        /// <summary>
        /// Asserts that the findings contain exactly one occurrence of the specified smell id.
        /// </summary>
        /// <param name="findings">The findings to assert on.</param>
        /// <param name="smellId">The expected smell id.</param>
        public static void ContainsSingleSmell(IEnumerable<Finding> findings, string smellId)
        {
            Assert.NotNull(findings);

            int count = findings.Count(f => string.Equals(f.Smell.Id, smellId, StringComparison.Ordinal));

            Assert.True(
                count == 1,
                BuildFailure(
                    findings,
                    $"Expected exactly one occurrence of smell '{smellId}', but found {count}."));
        }

        /// <summary>
        /// Asserts that the findings contain the specified smell id a given number of times.
        /// </summary>
        /// <param name="findings">The findings to assert on.</param>
        /// <param name="smellId">The smell id.</param>
        /// <param name="expectedCount">The expected number of occurrences.</param>
        public static void ContainsSmellTimes(IEnumerable<Finding> findings, string smellId, int expectedCount)
        {
            Assert.NotNull(findings);

            int actualCount = findings.Count(f => string.Equals(f.Smell.Id, smellId, StringComparison.Ordinal));

            Assert.True(
                actualCount == expectedCount,
                BuildFailure(
                    findings,
                    $"Expected smell '{smellId}' to occur {expectedCount} time(s), but found {actualCount}."));
        }

        /// <summary>
        /// Asserts that the findings contain at least one finding with the specified smell id
        /// and the expected severity.
        /// </summary>
        /// <param name="findings">The findings to assert on.</param>
        /// <param name="smellId">The smell id.</param>
        /// <param name="severity">The expected severity.</param>
        public static void ContainsSmellWithSeverity(IEnumerable<Finding> findings, string smellId, Severity severity)
        {
            Assert.NotNull(findings);

            Assert.Contains(findings, f =>
                string.Equals(f.Smell.Id, smellId, StringComparison.Ordinal) &&
                f.Smell.Severity == severity);
        }

        /// <summary>
        /// Asserts that the findings contain only smells from the specified set
        /// and no additional smell ids.
        /// </summary>
        /// <param name="findings">The findings to assert on.</param>
        /// <param name="allowedSmellIds">The set of allowed smell ids.</param>
        public static void OnlyContainsSmells(IEnumerable<Finding> findings, params string[] allowedSmellIds)
        {
            Assert.NotNull(findings);
            Assert.NotNull(allowedSmellIds);

            HashSet<string> allowed = new HashSet<string>(allowedSmellIds, StringComparer.Ordinal);

            string[] unexpected = findings
                .Select(f => f.Smell.Id)
                .Where(id => !allowed.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                unexpected.Length == 0,
                BuildFailure(
                    findings,
                    $"Expected only smells [{string.Join(", ", allowedSmellIds)}], but found unexpected [{string.Join(", ", unexpected)}]."));
        }

        /// <summary>
        /// Asserts that the findings contain exactly the specified smell ids
        /// (same multiset, order-independent).
        /// </summary>
        /// <param name="findings">The findings to assert on.</param>
        /// <param name="expectedSmellIds">The exact set of expected smell ids.</param>
        public static void HasExactlySmells(IEnumerable<Finding> findings, params string[] expectedSmellIds)
        {
            Assert.NotNull(findings);
            Assert.NotNull(expectedSmellIds);

            string[] actual = findings
                .Select(f => f.Smell.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            string[] expected = expectedSmellIds
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                actual.SequenceEqual(expected, StringComparer.Ordinal),
                BuildFailure(
                    findings,
                    $"Expected smells [{string.Join(", ", expected)}], but got [{string.Join(", ", actual)}]."));
        }

        /// <summary>
        /// Builds a detailed assertion failure message including all actual findings.
        /// </summary>
        /// <param name="findings">The findings that were produced by the analyzer.</param>
        /// <param name="assertionMessage">The high-level assertion failure message.</param>
        /// <returns>A formatted assertion failure message.</returns>
        private static string BuildFailure(IEnumerable<Finding> findings, string assertionMessage)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(assertionMessage);
            sb.AppendLine("Actual findings:");

            foreach (Finding finding in findings)
            {
                sb.Append("  - ");
                sb.AppendLine(finding.ToString());
            }

            return sb.ToString();
        }
    }
}
