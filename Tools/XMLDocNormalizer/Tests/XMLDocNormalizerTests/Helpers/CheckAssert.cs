using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides helpers for asserting checker behavior using full-string equality comparisons.
    /// </summary>
    internal static class CheckAssert
    {
        #region WellFormedDetector
        /// <summary>
        /// Runs the malformed XML documentation checker on an in-memory source snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, or field.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindWellFormedFindingsForMember(string memberCode)
        {
            string source = Wrapper.WrapInClass(memberCode);

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocWellFormedDetector.FindMalformedTags(tree, filePath: "InMemory.cs");
        }

        /// <summary>
        /// Runs the well-formed detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindWellFormedFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocWellFormedDetector.FindMalformedTags(tree, filePath: "InMemory.cs");
        }
        #endregion

        #region BasicDetector
        /// <summary>
        /// Runs the basic detector on a full in-memory source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocBasicDetector.FindBasicSmells(tree, filePath: "InMemory.cs");
        }

        /// <summary>
        /// Runs the basic detector on a full in-memory source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <param name="options">The documentation options to apply.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicFindingsForSource(string source, XmlDocOptions options)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocBasicDetector.FindBasicSmells(tree, filePath: "InMemory.cs", options);
        }

        /// <summary>
        /// Runs the basic detector on a member snippet wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, event, or field.</param>
        /// <param name="options">The documentation options to apply.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicFindingsForMember(string memberCode, XmlDocOptions options)
        {
            return FindBasicFindingsForSource(Wrapper.WrapInClass(memberCode), options);
        }

        /// <summary>
        /// Runs the basic detector on a member snippet wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, event, or field.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindBasicFindingsForMember(string memberCode)
        {
            return FindBasicFindingsForSource(Wrapper.WrapInClass(memberCode));
        }

        /// <summary>
        /// Runs the basic detector on multiple full in-memory source texts that are treated as separate files
        /// in the same directory, and returns all findings including aggregated DOC101 findings.
        /// </summary>
        /// <param name="sources">
        /// The input files consisting of file name and complete C# source text.
        /// All files are assumed to live in the same directory for aggregation purposes.
        /// </param>
        /// <param name="options">The documentation options to apply.</param>
        /// <returns>A list of findings including aggregated DOC101 findings.</returns>
        public static List<Finding> FindBasicFindingsForSources((string FileName, string Source)[] sources, XmlDocOptions options)
        {
            ArgumentNullException.ThrowIfNull(sources);
            ArgumentNullException.ThrowIfNull(options);

            // Use a stable directory so (directory, namespace) aggregation works reliably.
            string directory = "InMemory";

            XMLDocNormalizer.Utils.Namespace.NamespaceDocumentationAggregator namespaceAggregator =
                new XMLDocNormalizer.Utils.Namespace.NamespaceDocumentationAggregator(options.RequireDocumentationForNamespaces);

            List<Finding> findings = new List<Finding>();

            foreach ((string FileName, string Source) item in sources)
            {
                string filePath = directory + "/" + item.FileName;

                SyntaxTree tree = CSharpSyntaxTree.ParseText(item.Source);
                findings.AddRange(XmlDocBasicDetector.FindBasicSmells(tree, filePath, options, namespaceAggregator));
            }

            // Flush aggregated namespace findings (DOC101) after all files were processed.
            findings.AddRange(namespaceAggregator.CreateMissingCentralNamespaceFindings());

            return findings;
        }

        /// <summary>
        /// Runs the basic detector on multiple full in-memory source texts that are treated as separate files
        /// in the same directory, and returns all findings including aggregated DOC101 findings.
        /// </summary>
        /// <param name="sources">The input files consisting of file name and complete C# source text.</param>
        /// <returns>A list of findings including aggregated DOC101 findings.</returns>
        /// <remarks>
        /// Uses default <see cref="XmlDocOptions"/>.
        /// </remarks>
        public static List<Finding> FindBasicFindingsForSources((string FileName, string Source)[] sources)
        {
            XmlDocOptions options = new XmlDocOptions();
            return FindBasicFindingsForSources(sources, options);
        }
        #endregion

        #region ParamDetector
        /// <summary>
        /// Runs the parameter detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet (method, constructor, delegate, indexer, operator).</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindParamFindingsForMember(string memberCode)
        {
            return FindParamFindingsForSource(Wrapper.WrapInClass(memberCode));
        }

        /// <summary>
        /// Runs the parameter detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindParamFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocParamDetector.FindParamSmells(tree, filePath: "InMemory.cs");
        }
        #endregion

        #region TypeParamDetector
        /// <summary>
        /// Runs the type parameter detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindTypeParamFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocTypeParamDetector.FindTypeParamSmells(tree, filePath: "InMemory.cs");
        }

        /// <summary>
        /// Runs the type parameter detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet (method, delegate, etc.).</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindTypeParamFindingsForMember(string memberCode)
        {
            return FindTypeParamFindingsForSource(Wrapper.WrapInClass(memberCode));
        }
        #endregion

        #region ReturnsDetector
        /// <summary>
        /// Runs the returns detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindReturnsFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocReturnsDetector.FindReturnsSmells(tree, filePath: "InMemory.cs");
        }

        /// <summary>
        /// Runs the returns detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindReturnsFindingsForMember(string memberCode)
        {
            return FindReturnsFindingsForSource(Wrapper.WrapInClass(memberCode));
        }
        #endregion

        #region SyntaxExceptionDetector
        /// <summary>
        /// Runs the exception detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindExceptionFindingsForMember(string memberCode)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(Wrapper.WrapInClass(memberCode));
            return XmlDocExceptionDetector.FindExceptionSmells(tree, filePath: "InMemory.cs");
        }

        /// <summary>
        /// Runs the exception detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindExceptionFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocExceptionDetector.FindExceptionSmells(tree, filePath: "InMemory.cs");
        }
        #endregion

        #region MemberTagDetector
        /// <summary>
        /// Runs the XmlDocMemberTagDetector on a member snippet wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, or field.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindMemberTagFindingsForMember(string memberCode)
        {
            string source = Wrapper.WrapInClass(memberCode);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            return XmlDocMemberTagDetector.FindInvalidTags(tree, filePath: "InMemory.cs");
        }

        /// <summary>
        /// Runs the XmlDocMemberTagDetector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindMemberTagFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            return XmlDocMemberTagDetector.FindInvalidTags(
                tree,
                filePath: "InMemory.cs");
        }
        #endregion

        #region General
        /// <summary>
        /// Asserts that the formatted checker output equals the expected output exactly.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, or field.</param>
        /// <param name="expectedOutput">The expected formatted output.</param>
        public static void MemberEquals(string memberCode, string expectedOutput)
        {
            List<Finding> findings = FindWellFormedFindingsForMember(memberCode);

            string actual = FormatFindings(findings);
            Assert.Equal(NormalizeNewlines(expectedOutput).Trim(), NormalizeNewlines(actual).Trim());
        }

        /// <summary>
        /// Formats findings into a stable, comparable multi-line representation.
        /// </summary>
        /// <param name="findings">The findings to format.</param>
        /// <returns>A formatted string containing all findings.</returns>
        private static string FormatFindings(List<Finding> findings)
        {
            if (findings.Count == 0)
            {
                return string.Empty;
            }

            // Keep ordering stable for tests.
            IEnumerable<Finding> ordered =
                findings.OrderBy(f => f.Line).ThenBy(f => f.Column).ThenBy(f => f.TagName).ThenBy(f => f.Message);

            List<string> lines = new List<string>();

            foreach (Finding f in ordered)
            {
                // Format similar to your Finding.ToString(), but without the snippet to reduce brittleness.
                // If you want the snippet included as well, see the variant below.
                lines.Add($"[{f.Line},{f.Column}] <{f.TagName}>: {f.Message}");
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Normalizes newlines to avoid platform-specific differences in test output.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>The text with normalized newlines.</returns>
        private static string NormalizeNewlines(string text)
        {
            return text.Replace("\r\n", "\n");
        }
        #endregion
    }
}
