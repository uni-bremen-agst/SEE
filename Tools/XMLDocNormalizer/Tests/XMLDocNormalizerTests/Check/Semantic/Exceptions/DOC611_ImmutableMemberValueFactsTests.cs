using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests value facts inferred from immutable fields and properties.
    /// </summary>
    public sealed class DOC611_ImmutableMemberValueFactsTests
    {
        /// <summary>
        /// Ensures that a static readonly field initialized with an object creation
        /// expression is recognized as non-null.
        /// </summary>
        [Fact]
        public void StaticReadonlyFieldInitializedWithNew_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    private static readonly object Value = new object();\n" +
                "\n" +
                "    /// <summary>Validates the stored value.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(Value);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a get-only property assigned from a constructor parameter
        /// after null validation is recognized as non-null.
        /// </summary>
        [Fact]
        public void GetOnlyPropertyAssignedAfterNullGuard_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an immutable stored value.</summary>\n" +
                "    public void M(Holder holder)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(holder.Value);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Holder\n" +
                "    {\n" +
                "        public Holder(object? value)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(value);\n" +
                "            Value = value;\n" +
                "        }\n" +
                "\n" +
                "        public object Value { get; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that all string facts proven for a constructor parameter are
        /// transferred to the get-only property initialized from that parameter.
        /// </summary>
        [Fact]
        public void GetOnlyPropertyAssignedAfterWhitespaceGuard_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an immutable stored path.</summary>\n" +
                "    public void M(Holder holder)\n" +
                "    {\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(holder.Path);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Holder\n" +
                "    {\n" +
                "        public Holder(string? path)\n" +
                "        {\n" +
                "            System.ArgumentException.ThrowIfNullOrWhiteSpace(path);\n" +
                "            Path = path;\n" +
                "        }\n" +
                "\n" +
                "        public string Path { get; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that constructor facts are not transferred to a property that
        /// can be changed after construction.
        /// </summary>
        [Fact]
        public void PropertyWithSetter_DoesNotReuseConstructorFacts()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a mutable stored path.</summary>\n" +
                "    public void M(Holder holder)\n" +
                "    {\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(holder.Path);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Holder\n" +
                "    {\n" +
                "        public Holder(string? path)\n" +
                "        {\n" +
                "            System.ArgumentException.ThrowIfNullOrWhiteSpace(path);\n" +
                "            Path = path;\n" +
                "        }\n" +
                "\n" +
                "        public string? Path { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Equal(2, findings.Count);

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentException",
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that a get-only property assignment without validation does not
        /// establish null or string-content facts.
        /// </summary>
        [Fact]
        public void GetOnlyPropertyAssignedWithoutGuard_ProducesFindings()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an unguarded stored path.</summary>\n" +
                "    public void M(Holder holder)\n" +
                "    {\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(holder.Path);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Holder\n" +
                "    {\n" +
                "        public Holder(string? path)\n" +
                "        {\n" +
                "            Path = path;\n" +
                "        }\n" +
                "\n" +
                "        public string? Path { get; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Equal(2, findings.Count);

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentException",
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that facts are not assigned to a property when one constructor
        /// can initialize it without the required validation.
        /// </summary>
        [Fact]
        public void PropertyWithOneUnguardedConstructor_ProducesFindings()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a conditionally guarded stored path.</summary>\n" +
                "    public void M(Holder holder)\n" +
                "    {\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(holder.Path);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Holder\n" +
                "    {\n" +
                "        public Holder(string? path)\n" +
                "        {\n" +
                "            System.ArgumentException.ThrowIfNullOrWhiteSpace(path);\n" +
                "            Path = path;\n" +
                "        }\n" +
                "\n" +
                "        public Holder(string? path, bool skipValidation)\n" +
                "        {\n" +
                "            Path = path;\n" +
                "        }\n" +
                "\n" +
                "        public string? Path { get; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Equal(2, findings.Count);

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentException",
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that immutable member facts are preserved when the member values
        /// are passed through a method call containing named, optional, and parameter-array
        /// arguments.
        /// </summary>
        [Fact]
        public void ImmutableMemberFactsPassedThroughFactoryMethod_DoesNotProduceFinding()
        {
            string source =
                "public class TestClass\n" +
                "{\n" +
                "    private static readonly object Smell = new object();\n" +
                "\n" +
                "    /// <summary>Creates a finding from an immutable location.</summary>\n" +
                "    public void M(Location location)\n" +
                "    {\n" +
                "        CreateFinding(\n" +
                "            location.Tree,\n" +
                "            location.FilePath,\n" +
                "            tagName: \"namespace\",\n" +
                "            Smell,\n" +
                "            absolutePosition: 0,\n" +
                "            snippet: \"\",\n" +
                "            \"message\");\n" +
                "    }\n" +
                "\n" +
                "    private static void CreateFinding(\n" +
                "        object tree,\n" +
                "        string filePath,\n" +
                "        string tagName,\n" +
                "        object smell,\n" +
                "        int absolutePosition,\n" +
                "        string snippet = \"\",\n" +
                "        params object[] messageArgs)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(tree);\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(filePath);\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(tagName);\n" +
                "        System.ArgumentNullException.ThrowIfNull(smell);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Location\n" +
                "    {\n" +
                "        public Location(object? tree, string? filePath)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(tree);\n" +
                "            System.ArgumentException.ThrowIfNullOrWhiteSpace(filePath);\n" +
                "\n" +
                "            Tree = tree;\n" +
                "            FilePath = filePath;\n" +
                "        }\n" +
                "\n" +
                "        public object Tree { get; }\n" +
                "\n" +
                "        public string FilePath { get; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that immutable-member facts are preserved when declarations and
        /// their use are located in different syntax trees of the same compilation.
        /// </summary>
        [Fact]
        public void ImmutableMemberFactsAcrossSyntaxTrees_DoesNotProduceFinding()
        {
            string callerSource =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a finding from an immutable location.</summary>\n" +
                "    public void M(Location location)\n" +
                "    {\n" +
                "        FindingFactory.Create(\n" +
                "            location.Tree,\n" +
                "            location.FilePath,\n" +
                "            \"namespace\",\n" +
                "            Smells.MissingDocumentation);\n" +
                "    }\n" +
                "}\n";

            string locationSource =
                "public sealed class Location\n" +
                "{\n" +
                "    public Location(object? tree, string? filePath)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(tree);\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(filePath);\n" +
                "\n" +
                "        Tree = tree;\n" +
                "        FilePath = filePath;\n" +
                "    }\n" +
                "\n" +
                "    public object Tree { get; }\n" +
                "\n" +
                "    public string FilePath { get; }\n" +
                "}\n";

            string smellsSource =
                "public static class Smells\n" +
                "{\n" +
                "    public static readonly object MissingDocumentation =\n" +
                "        new object();\n" +
                "}\n";

            string factorySource =
                "public static class FindingFactory\n" +
                "{\n" +
                "    public static void Create(\n" +
                "        object tree,\n" +
                "        string filePath,\n" +
                "        string tagName,\n" +
                "        object smell)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(tree);\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(filePath);\n" +
                "        System.ArgumentException.ThrowIfNullOrWhiteSpace(tagName);\n" +
                "        System.ArgumentNullException.ThrowIfNull(smell);\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSources(
                    ExceptionAnalysisMode.ProjectTransitive,
                    ("Caller.cs", callerSource),
                    ("Location.cs", locationSource),
                    ("Smells.cs", smellsSource),
                    ("FindingFactory.cs", factorySource));

            Assert.Empty(findings);
        }
    }
}
