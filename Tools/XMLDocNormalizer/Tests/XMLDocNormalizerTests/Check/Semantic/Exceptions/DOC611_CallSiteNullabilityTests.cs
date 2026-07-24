using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests call-site non-null facts used by transitive DOC611 exception analysis.
    /// </summary>
    public sealed class DOC611_CallSiteNullabilityTests
    {
        /// <summary>
        /// Ensures that a value introduced by a successful type pattern does not propagate
        /// an impossible <see cref="ArgumentNullException"/> from a called guard method.
        /// </summary>
        [Fact]
        public void PatternVariablePassedToThrowIfNull_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a value.</summary>\n" +
                "    public void M(object? value)\n" +
                "    {\n" +
                "        if (value is string text)\n" +
                "        {\n" +
                "            Validate(text);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a string literal remains known to be non-null through multiple
        /// project-local method calls.
        /// </summary>
        [Fact]
        public void NonNullLiteralPassedThroughWrapper_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a constant value.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Wrapper(\"value\");\n" +
                "    }\n" +
                "\n" +
                "    private static void Wrapper(string? value)\n" +
                "    {\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that pattern matching and a literal argument can jointly prove all guarded
        /// parameters of a deeper call to be non-null.
        /// </summary>
        [Fact]
        public void PatternVariableAndLiteralPassedThroughWrapper_DoNotProduceFinding()
        {
            string source =
                "public class BaseNode { }\n" +
                "public sealed class DerivedNode : BaseNode { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Reads a node attribute.</summary>\n" +
                "    public void M(BaseNode? node)\n" +
                "    {\n" +
                "        Read(node, \"name\");\n" +
                "    }\n" +
                "\n" +
                "    private static void Read(BaseNode? node, string? name)\n" +
                "    {\n" +
                "        if (node is DerivedNode derivedNode)\n" +
                "        {\n" +
                "            Validate(derivedNode, name);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(DerivedNode? node, string? name)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(node);\n" +
                "        System.ArgumentNullException.ThrowIfNull(name);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a possibly null argument still propagates
        /// <see cref="ArgumentNullException"/> to the documented caller.
        /// </summary>
        [Fact]
        public void PossiblyNullArgumentPassedToThrowIfNull_ProducesFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a value.</summary>\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
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
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a non-nullable reference-type annotation alone is not treated as a
        /// runtime proof that an argument cannot be null.
        /// </summary>
        [Fact]
        public void NonNullableParameterAnnotationAlone_StillProducesFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a value.</summary>\n" +
                "    public void M(string value)\n" +
                "    {\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
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
        }

        /// <summary>
        /// Ensures that analyzing a safe invocation context does not hide another invocation
        /// of the same method with a possibly null argument.
        /// </summary>
        [Fact]
        public void SafeAndUnsafeCallsToSameMethod_UnsafeCallStillProducesFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates two values.</summary>\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        Wrapper(\"safe\");\n" +
                "        Wrapper(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Wrapper(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
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
        }

        /// <summary>
        /// Ensures that a <c>var</c> pattern is not treated as a non-null proof because it
        /// also matches <see langword="null"/>.
        /// </summary>
        [Fact]
        public void VarPatternPassedToThrowIfNull_StillProducesFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a captured value.</summary>\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        if (value is var captured)\n" +
                "        {\n" +
                "            Validate(captured);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
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
        }

        /// <summary>
        /// Ensures that a local variable initialized with an object creation is treated
        /// as non-null when passed to a guarded method.
        /// </summary>
        [Fact]
        public void LocalInitializedWithObjectCreation_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a locally created value.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        object value = new object();\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(object? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a local variable initialized from a project method whose return
        /// values are all non-null is treated as non-null.
        /// </summary>
        [Fact]
        public void LocalInitializedFromNonNullReturningMethod_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a value created by a helper.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        object value = CreateValue();\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static object CreateValue()\n" +
                "    {\n" +
                "        return new object();\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(object? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that <see cref="Array.Empty{T}"/> is treated as returning a
        /// non-null array.
        /// </summary>
        [Fact]
        public void ArrayEmptyPassedToThrowIfNull_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an empty array.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Validate(System.Array.Empty<int>());\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(int[]? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a nullable local is treated as non-null after a terminating
        /// equality-based null guard.
        /// </summary>
        [Fact]
        public void LocalAfterTerminatingNullGuard_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a guarded local value.</summary>\n" +
                "    public void M(string? input)\n" +
                "    {\n" +
                "        string? value = input;\n" +
                "\n" +
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
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a nullable out variable is treated as non-null after a
        /// terminating compound null guard.
        /// </summary>
        [Fact]
        public void OutVariableAfterCompoundNullGuard_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a guarded out value.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        if (!TryGetValue(out string? value) || value == null)\n" +
                "        {\n" +
                "            return;\n" +
                "        }\n" +
                "\n" +
                "        Validate(value);\n" +
                "    }\n" +
                "\n" +
                "    private static bool TryGetValue(out string? value)\n" +
                "    {\n" +
                "        value = \"value\";\n" +
                "        return true;\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that foreach elements returned through a project-local
        /// helper backed by <see cref="Enumerable.OfType{TResult}"/> are
        /// treated as non-null.
        /// </summary>
        [Fact]
        public void ForeachElementFromOfTypeHelper_DoesNotProduceFinding()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates filtered values.</summary>\n" +
                "    /// <param name=\"values\">The values to filter.</param>\n" +
                "    /// <exception cref=\"System.ArgumentNullException\">\n" +
                "    /// Thrown when <paramref name=\"values\"/> is null.\n" +
                "    /// </exception>\n" +
                "    public void M(IEnumerable<object?> values)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(values);\n" +
                "\n" +
                "        foreach (string value in GetStrings(values))\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static IEnumerable<string> GetStrings(\n" +
                "        IEnumerable<object?> values)\n" +
                "    {\n" +
                "        return values.OfType<string>();\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that <see cref="string.Empty"/> is recognized as a
        /// guaranteed non-null fallback in a null-coalescing expression.
        /// </summary>
        [Fact]
        public void StringEmptyFallbackPassedToGuardedMethod_DoesNotProduceFinding()
        {
            string source =
                "using System.Text;\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Appends a normalized value.</summary>\n" +
                "    /// <param name=\"value\">The optional value.</param>\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        StringBuilder builder = new();\n" +
                "        Append(builder, value ?? string.Empty);\n" +
                "    }\n" +
                "\n" +
                "    private static void Append(\n" +
                "        StringBuilder? builder,\n" +
                "        string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(builder);\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a local introduced by a negated type pattern is
        /// treated as non-null after the matching branch terminates the
        /// current loop iteration.
        /// </summary>
        [Fact]
        public void NegatedPatternVariableAfterContinue_DoesNotProduceFinding()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "\n" +
                "public interface IMarker { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates matching values.</summary>\n" +
                "    public void M(IEnumerable<object?> values)\n" +
                "    {\n" +
                "        foreach (object? candidate in values)\n" +
                "        {\n" +
                "            if (candidate is not IMarker marker)\n" +
                "            {\n" +
                "                continue;\n" +
                "            }\n" +
                "\n" +
                "            Validate(marker);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(IMarker? marker)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(marker);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a pattern variable is not treated as definitely
        /// assigned outside the control-flow region in which the pattern
        /// guarantees its value.
        /// </summary>
        [Fact]
        public void PatternFlowStateControlsNonNullInference()
        {
            string source =
                "public interface IMarker { }\n" +
                "\n" +
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a candidate.</summary>\n" +
                "    public void M(object? candidate)\n" +
                "    {\n" +
                "        if (candidate is IMarker marker)\n" +
                "        {\n" +
                "            Validate(marker);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(IMarker? marker)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(marker);\n" +
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
