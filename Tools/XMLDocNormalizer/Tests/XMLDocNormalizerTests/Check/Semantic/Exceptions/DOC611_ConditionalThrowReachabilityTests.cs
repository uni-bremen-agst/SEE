using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the suppression of explicit throws in branches that are proven
    /// unreachable by call-site value facts.
    /// </summary>
    public sealed class DOC611_ConditionalThrowReachabilityTests
    {
        /// <summary>
        /// Ensures that an explicit throw guarded by
        /// <see cref="string.IsNullOrWhiteSpace"/> is suppressed when the
        /// argument is proven to contain a non-whitespace character.
        /// </summary>
        [Fact]
        public void ExplicitThrowInsideImpossibleWhitespaceGuard_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a known string.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Validate(\"value\");\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        if (string.IsNullOrWhiteSpace(value))\n" +
                "        {\n" +
                "            throw new System.ArgumentException();\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a throw expression on the right side of a null-coalescing
        /// expression is suppressed when the left operand is proven non-null.
        /// </summary>
        [Fact]
        public void ThrowExpressionAfterKnownNonNullOperand_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    private static readonly object Value = new object();\n" +
                "\n" +
                "    /// <summary>Validates a known object.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Validate(Value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(object? value)\n" +
                "    {\n" +
                "        _ = value ??\n" +
                "            throw new System.ArgumentNullException(nameof(value));\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that impossible explicit throws remain suppressed through two
        /// transitive call levels.
        /// </summary>
        [Fact]
        public void KnownFactsPassedThroughFactoryToConstructor_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    private static readonly object Smell = new object();\n" +
                "\n" +
                "    /// <summary>Creates a validated result.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Factory(\"file.cs\", \"namespace\", Smell);\n" +
                "    }\n" +
                "\n" +
                "    private static void Factory(\n" +
                "        string filePath,\n" +
                "        string tagName,\n" +
                "        object smell)\n" +
                "    {\n" +
                "        _ = new Result(filePath, tagName, smell);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Result\n" +
                "    {\n" +
                "        public Result(\n" +
                "            string? filePath,\n" +
                "            string? tagName,\n" +
                "            object? smell)\n" +
                "        {\n" +
                "            Smell = smell ??\n" +
                "                throw new System.ArgumentNullException(nameof(smell));\n" +
                "\n" +
                "            if (string.IsNullOrWhiteSpace(filePath))\n" +
                "            {\n" +
                "                throw new System.ArgumentException();\n" +
                "            }\n" +
                "\n" +
                "            if (string.IsNullOrWhiteSpace(tagName))\n" +
                "            {\n" +
                "                throw new System.ArgumentException();\n" +
                "            }\n" +
                "        }\n" +
                "\n" +
                "        public object Smell { get; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an explicit conditional throw remains visible when the
        /// argument does not have the required string facts.
        /// </summary>
        [Fact]
        public void ExplicitThrowWithUnknownStringValue_ProducesFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an unknown string.</summary>\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        if (string.IsNullOrWhiteSpace(value))\n" +
                "        {\n" +
                "            throw new System.ArgumentException();\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = Assert.Single(findings);

            Assert.Contains(
                "System.ArgumentException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a null-coalescing throw remains visible when the left
        /// operand is not proven non-null.
        /// </summary>
        [Fact]
        public void ThrowExpressionWithUnknownOperand_ProducesFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an unknown object.</summary>\n" +
                "    public void M(object? value)\n" +
                "    {\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(object? value)\n" +
                "    {\n" +
                "        _ = value ??\n" +
                "            throw new System.ArgumentNullException(nameof(value));\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = Assert.Single(findings);

            Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }
    }
}
