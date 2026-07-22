using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests call-site value facts used to suppress impossible exceptions from
    /// framework argument-validation helpers.
    /// </summary>
    public sealed class DOC611_CallSiteValueFactsTests
    {
        /// <summary>
        /// Ensures that a non-empty, non-whitespace string literal satisfies the
        /// contract of <see cref="ArgumentException.ThrowIfNullOrWhiteSpace"/>.
        /// </summary>
        [Fact]
        public void NonWhitespaceLiteralPassedToThrowIfNullOrWhiteSpace_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a constant value.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Validate(\"value\");\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that execution after a terminating
        /// <see cref="string.IsNullOrWhiteSpace"/> guard proves that the guarded
        /// parameter is neither null, empty, nor whitespace.
        /// </summary>
        [Fact]
        public void ParameterAfterTerminatingNullOrWhiteSpaceGuard_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a guarded value.</summary>\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        if (string.IsNullOrWhiteSpace(value))\n" +
                "        {\n" +
                "            return;\n" +
                "        }\n" +
                "\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a preceding null guard suppresses only
        /// <see cref="ArgumentNullException"/> while a possible empty or whitespace
        /// value still propagates <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void ParameterAfterTerminatingNullGuard_StillProducesArgumentExceptionFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a partially guarded value.</summary>\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        if (value == null)\n" +
                "        {\n" +
                "            return;\n" +
                "        }\n" +
                "\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = Assert.Single(findings);

            Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID);

            Assert.Contains(
                "System.ArgumentException",
                finding.Message,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that string facts proven in the caller are preserved when the
        /// guarded value is passed to a constructor.
        /// </summary>
        [Fact]
        public void GuardedParameterPassedToConstructor_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a validated holder.</summary>\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        if (string.IsNullOrWhiteSpace(value))\n" +
                "        {\n" +
                "            return;\n" +
                "        }\n" +
                "\n" +
                "        _ = new Holder(value);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Holder\n" +
                "    {\n" +
                "        public Holder(string? value)\n" +
                "        {\n" +
                "            System.ArgumentException.ThrowIfNullOrWhiteSpace(value);\n" +
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
        /// Ensures that an empty string literal suppresses
        /// <see cref="ArgumentNullException"/> but still propagates
        /// <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void EmptyLiteralPassedToThrowIfNullOrWhiteSpace_ProducesOnlyArgumentException()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an empty value.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Validate(\"\");\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = Assert.Single(findings);

            Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID);

            Assert.Contains(
                "System.ArgumentException",
                finding.Message,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that facts proven by a terminating guard in an enclosing block
        /// remain available inside a safely nested conditional block.
        /// </summary>
        [Fact]
        public void GuardedParameterPassedToConstructorInsideNestedBlock_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a conditionally stored holder.</summary>\n" +
                "    public void M(string? value, bool create)\n" +
                "    {\n" +
                "        if (string.IsNullOrWhiteSpace(value))\n" +
                "        {\n" +
                "            return;\n" +
                "        }\n" +
                "\n" +
                "        if (create)\n" +
                "        {\n" +
                "            _ = new Holder(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Holder\n" +
                "    {\n" +
                "        public Holder(string? value)\n" +
                "        {\n" +
                "            System.ArgumentException.ThrowIfNullOrWhiteSpace(value);\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }
    }
}
