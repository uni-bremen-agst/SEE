using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests exact exception-type matching for documentation coverage.
    /// </summary>
    public sealed class ExceptionCoverageExactTypeTests
    {
        /// <summary>
        /// Ensures that direct exception documentation is considered covered only when the
        /// documented and thrown Roslyn type symbols are identical.
        /// </summary>
        /// <param name="documentedType">The exception type used by the exception tag.</param>
        /// <param name="thrownType">The exception type constructed by the member.</param>
        /// <param name="isExactMatch">Whether both exception types are expected to match exactly.</param>
        [Theory]
        [InlineData("System.ArgumentNullException", "System.ArgumentNullException", true)]
        [InlineData("System.ArgumentException", "System.ArgumentException", true)]
        [InlineData("System.IO.FileNotFoundException", "System.IO.FileNotFoundException", true)]
        [InlineData("System.IO.IOException", "System.IO.IOException", true)]
        [InlineData("System.InvalidOperationException", "System.InvalidOperationException", true)]
        [InlineData("System.Exception", "System.Exception", true)]
        [InlineData("System.ArgumentException", "System.ArgumentNullException", false)]
        [InlineData("System.ArgumentException", "System.ArgumentOutOfRangeException", false)]
        [InlineData("System.IO.IOException", "System.IO.FileNotFoundException", false)]
        [InlineData("System.Exception", "System.InvalidOperationException", false)]
        [InlineData("System.ArgumentNullException", "System.ArgumentException", false)]
        [InlineData("System.IO.FileNotFoundException", "System.IO.IOException", false)]
        [InlineData("System.InvalidOperationException", "System.Exception", false)]
        public void DirectCoverage_RequiresExactExceptionType(
            string documentedType,
            string thrownType,
            bool isExactMatch)
        {
            string member =
                $"/// <summary>Does something.</summary>\n" +
                $"/// <exception cref=\"{documentedType}\">Documented.</exception>\n" +
                "public void M()\n" +
                "{\n" +
                $"    throw new {thrownType}();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(member, ExceptionAnalysisMode.Direct);

            if (isExactMatch)
            {
                Assert.DoesNotContain(
                    findings,
                    finding => finding.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);
                Assert.DoesNotContain(
                    findings,
                    finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);
                return;
            }

            Finding missingFinding = Assert.Single(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);
            Finding unprovenFinding = Assert.Single(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);

            Assert.Contains(thrownType, missingFinding.Message, StringComparison.Ordinal);
            Assert.Contains(documentedType, unprovenFinding.Message, StringComparison.Ordinal);
        }
    }
}
