using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests framework methods whose normal return values are guaranteed to
    /// be non-null.
    /// </summary>
    public sealed class DOC611_KnownNonNullFrameworkReturnTests
    {
        /// <summary>
        /// Ensures that a get-only property assigned directly from
        /// <see cref="string.Join(string?, IEnumerable{string?})"/> is
        /// recognized as non-null when passed to a guarded method.
        /// </summary>
        [Fact]
        public void StringJoinAssignedToGetOnlyProperty_DoesNotProduceFinding()
        {
            const string source =
                "using System;\n" +
                "using System.Collections.Generic;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a generated context key.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Context context = new Context();\n" +
                "        Validate(context.Key);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Context\n" +
                "    {\n" +
                "        public Context()\n" +
                "        {\n" +
                "            IEnumerable<string> values =\n" +
                "                new[] { \"first\", \"second\" };\n" +
                "\n" +
                "            Key = string.Join(\",\", values);\n" +
                "        }\n" +
                "\n" +
                "        public string Key { get; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a get-only property initialized by a terminal
        /// constructor remains non-null when another constructor delegates
        /// to it through <c>this(...)</c>.
        /// </summary>
        [Fact]
        public void
            StringJoinAssignedByDelegatedConstructor_DoesNotProduceFinding()
        {
            const string source =
                "using System;\n" +
                "using System.Collections.Generic;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates a generated context key.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Context context = new Context();\n" +
                "        Validate(context.Key);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Context\n" +
                "    {\n" +
                "        public Context()\n" +
                "            : this(Array.Empty<string>())\n" +
                "        {\n" +
                "        }\n" +
                "\n" +
                "        private Context(IEnumerable<string> values)\n" +
                "        {\n" +
                "            Key = string.Join(\",\", values);\n" +
                "        }\n" +
                "\n" +
                "        public string Key { get; }\n" +
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
