using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests semantic exception findings for known framework throw helpers.
    /// </summary>
    public sealed class DOC610_KnownFrameworkThrowHelperTests
    {
        /// <summary>
        /// Ensures that an undocumented ArgumentNullException throw helper
        /// produces DOC610 in direct mode.
        /// </summary>
        [Fact]
        public void UndocumentedThrowIfNull_ProducesDoc610()
        {
            string member =
                "/// <summary>Executes the operation.</summary>\n" +
                "public void M(object? value)\n" +
                "{\n" +
                "    System.ArgumentNullException.ThrowIfNull(value);\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    member,
                    ExceptionAnalysisMode.Direct);

            Finding finding = Assert.Single(
                findings,
                current =>
                    current.Smell.ID ==
                    XmlDocSmells.MissingExceptionTag.ID);

            Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that ArgumentException documentation covers both possible
        /// exceptions from ThrowIfNullOrWhiteSpace.
        /// </summary>
        [Fact]
        public void DocumentedThrowIfNullOrWhiteSpace_ProducesNoFinding()
        {
            string member =
                "/// <summary>Executes the operation.</summary>\n" +
                "/// <exception cref=\"System.ArgumentException\">Thrown when value is invalid.</exception>\n" +
                "public void M(string? value)\n" +
                "{\n" +
                "    System.ArgumentException.ThrowIfNullOrWhiteSpace(value);\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    member,
                    ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions);

            Assert.DoesNotContain(
                findings,
                current =>
                    current.Smell.ID ==
                    XmlDocSmells.MissingExceptionTag.ID ||
                    current.Smell.ID ==
                    XmlDocSmells.ExceptionFlowNotDecidable.ID ||
                    current.Smell.ID ==
                    XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }

        /// <summary>
        /// Ensures that a caught exception from a framework throw helper
        /// does not escape from the member.
        /// </summary>
        [Fact]
        public void CaughtFrameworkThrowHelper_DoesNotProduceDoc610()
        {
            string member =
                "/// <summary>Executes the operation.</summary>\n" +
                "public void M(bool disposed)\n" +
                "{\n" +
                "    try\n" +
                "    {\n" +
                "        System.ObjectDisposedException.ThrowIf(disposed, this);\n" +
                "    }\n" +
                "    catch (System.ObjectDisposedException)\n" +
                "    {\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    member,
                    ExceptionAnalysisMode.Direct);

            Assert.DoesNotContain(
                findings,
                current =>
                    current.Smell.ID ==
                    XmlDocSmells.MissingExceptionTag.ID);
        }

        /// <summary>
        /// Ensures that cancellation-token validation is recognized as a source
        /// of OperationCanceledException.
        /// </summary>
        [Fact]
        public void UndocumentedCancellationCheck_ProducesDoc610()
        {
            string member =
                "/// <summary>Executes the operation.</summary>\n" +
                "public void M(System.Threading.CancellationToken token)\n" +
                "{\n" +
                "    token.ThrowIfCancellationRequested();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    member,
                    ExceptionAnalysisMode.Direct);

            Finding finding = Assert.Single(
                findings,
                current =>
                    current.Smell.ID ==
                    XmlDocSmells.MissingExceptionTag.ID);

            Assert.Contains(
                "System.OperationCanceledException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a proven framework guard exception suppresses DOC631
        /// even when another invocation remains uncertain.
        /// </summary>
        [Fact]
        public void ProvenGuardWithOtherUncertainTarget_DoesNotProduceDoc631ForGuard()
        {
            string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Executes the operation.</summary>\n" +
                "    /// <exception cref=\"System.ArgumentNullException\">Thrown when value is null.</exception>\n" +
                "    public void M(object? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "        Unknown();\n" +
                "    }\n" +
                "\n" +
                "    private static extern void Unknown();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions);

            Assert.DoesNotContain(
                findings,
                current =>
                    current.Smell.ID ==
                    XmlDocSmells.ExceptionFlowNotDecidable.ID &&
                    current.Context.TargetName ==
                    "cref:ArgumentNullException");
        }
    }
}
