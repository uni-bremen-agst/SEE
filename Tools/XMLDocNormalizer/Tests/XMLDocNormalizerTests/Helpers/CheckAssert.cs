using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Namespace;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides helpers for asserting checker behavior using full-string equality comparisons.
    /// </summary>
    internal static class CheckAssert
    {
        /// <summary>
        /// Runs all syntax detectors on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindAllFindingsForSource(string source)
        {
            return FindAllFindingsForSource(source, new XmlDocOptions());
        }

        /// <summary>
        /// Runs all syntax detectors on a full in-memory C# source text with explicit XML documentation options.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <param name="options">The XML documentation options used by option-aware detectors.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindAllFindingsForSource(string source, XmlDocOptions options)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(options);

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            const string filePath = "InMemory.cs";

            List<Finding> findings = new(XmlDocBasicDetector.FindBasicSmells(tree, filePath, options));

            foreach (XmlDocDetectorCatalog.SyntaxDetector detector in XmlDocDetectorCatalog.SyntaxDetectors)
            {
                findings.AddRange(detector(tree, filePath));
            }

            findings.AddRange(XmlDocValueDetector.FindValueSmells(tree, filePath, options));

            return findings;
        }

        /// <summary>
        /// Runs all syntax detectors on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindAllFindingsForMember(string memberCode)
        {
            return FindAllFindingsForMember(memberCode, new XmlDocOptions());
        }

        /// <summary>
        /// Runs all syntax detectors on an in-memory member snippet that is wrapped into a class with explicit XML documentation options.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <param name="options">The XML documentation options used by option-aware detectors.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindAllFindingsForMember(string memberCode, XmlDocOptions options)
        {
            return FindAllFindingsForSource(Wrapper.WrapInClass(memberCode), options);
        }

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

            NamespaceDocumentationAggregator namespaceAggregator =
                new(options.RequireDocumentationForNamespaces);

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

        #region SemanticExceptionDetector
        /// <summary>
        /// Runs the semantic exception detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSemanticExceptionFindingsForMember(string memberCode)
        {
            return FindSemanticExceptionFindingsForSource(
                Wrapper.WrapInClass(memberCode),
                ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions);
        }

        /// <summary>
        /// Runs the semantic exception detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <param name="mode">The exception analysis mode.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSemanticExceptionFindingsForMember(
            string memberCode,
            ExceptionAnalysisMode mode)
        {
            return FindSemanticExceptionFindingsForSource(
                Wrapper.WrapInClass(memberCode),
                mode);
        }

        /// <summary>
        /// Runs the semantic exception detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSemanticExceptionFindingsForSource(string source)
        {
            return FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions);
        }

        /// <summary>
        /// Runs the semantic exception detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <param name="mode">The exception analysis mode.</param>
        /// <param name="additionalReferences">
        /// Additional metadata references available to semantic analysis.
        /// </param>
        /// <returns>A list of findings.</returns>
        public static List<Finding>
            FindSemanticExceptionFindingsForSource(
                string source,
                ExceptionAnalysisMode mode,
                params MetadataReference[] additionalReferences)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            List<MetadataReference> references =
                new(MetadataReferences.Default);

            references.AddRange(
                additionalReferences);

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InMemoryAssembly",
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);

            XmlDocOptions options = new()
            {
                ExceptionAnalysisMode = mode
            };

            return XmlDocExceptionSemanticDetector.FindExceptionSmells(
                tree,
                filePath: "InMemory.cs",
                semanticModel,
                semanticContext,
                options);
        }

        /// <summary>
        /// Runs the semantic exception detector on the first of multiple in-memory
        /// source files that are compiled together as one project.
        /// </summary>
        /// <param name="mode">The exception analysis mode.</param>
        /// <param name="sources">
        /// The source files consisting of file name and complete C# source text.
        /// The first source is treated as the reporting file; all sources are
        /// available for transitive semantic analysis.
        /// </param>
        /// <returns>A list of findings reported for the first source file.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="sources"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when no source file was supplied.
        /// </exception>
        public static List<Finding> FindSemanticExceptionFindingsForSources(
            ExceptionAnalysisMode mode,
            params (string FileName, string Source)[] sources)
        {
            ArgumentNullException.ThrowIfNull(sources);

            if (sources.Length == 0)
            {
                throw new ArgumentException(
                    "At least one source file must be supplied.",
                    nameof(sources));
            }

            SyntaxTree[] trees = sources
                .Select(source =>
                    CSharpSyntaxTree.ParseText(
                        source.Source,
                        path: source.FileName))
                .ToArray();

            SyntaxTree reportingTree = trees[0];

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InMemoryAssembly",
                syntaxTrees: trees,
                references: MetadataReferences.Default,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));

            SemanticModel semanticModel =
                compilation.GetSemanticModel(reportingTree);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    reportingTree,
                    compilation);

            XmlDocOptions options = new()
            {
                ExceptionAnalysisMode = mode
            };

            return XmlDocExceptionSemanticDetector.FindExceptionSmells(
                reportingTree,
                filePath: reportingTree.FilePath,
                semanticModel,
                semanticContext,
                options);
        }
        #endregion

        #region InheritdocDetector
        /// <summary>
        /// Runs the inheritdoc detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindInheritdocFindingsForMember(string memberCode)
        {
            return FindInheritdocFindingsForSource(Wrapper.WrapInClass(memberCode));
        }

        /// <summary>
        /// Runs the inheritdoc detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindInheritdocFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocInheritdocDetector.FindInheritdocSmells(tree, filePath: "InMemory.cs");
        }
        #endregion

        #region SemanticInheritdocDetector
        /// <summary>
        /// Runs the semantic inheritdoc detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSemanticInheritdocFindingsForMember(string memberCode)
        {
            return FindSemanticInheritdocFindingsForSource(Wrapper.WrapInClass(memberCode));
        }

        /// <summary>
        /// Runs the semantic inheritdoc detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSemanticInheritdocFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InMemoryAssembly",
                syntaxTrees: new[] { tree },
                references: MetadataReferences.Default,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            return XmlDocInheritdocSemanticDetector.FindInheritdocSmells(
                tree,
                filePath: "InMemory.cs",
                semanticModel);
        }

        /// <summary>
        /// Runs the semantic inheritdoc detector on multiple in-memory source texts
        /// that are compiled together as one project.
        /// </summary>
        /// <param name="sources">The input files consisting of file name and complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSemanticInheritdocFindingsForSources(
            params (string FileName, string Source)[] sources)
        {
            ArgumentNullException.ThrowIfNull(sources);

            SyntaxTree[] trees = sources
                .Select(s => CSharpSyntaxTree.ParseText(s.Source, path: s.FileName))
                .ToArray();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InMemoryAssembly",
                syntaxTrees: trees,
                references: MetadataReferences.Default,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            List<Finding> findings = new();

            foreach (SyntaxTree tree in trees)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);

                findings.AddRange(XmlDocInheritdocSemanticDetector.FindInheritdocSmells(
                    tree,
                    filePath: tree.FilePath,
                    semanticModel));
            }

            return findings;
        }
        #endregion

        #region ValueDetector
        /// <summary>
        /// Runs the value detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindValueFindingsForSource(string source)
        {
            return FindValueFindingsForSource(source, new XmlDocOptions());
        }

        /// <summary>
        /// Runs the value detector on a full in-memory C# source text with explicit XML documentation options.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <param name="options">The XML documentation options used by the detector.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindValueFindingsForSource(string source, XmlDocOptions options)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(options);

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocValueDetector.FindValueSmells(tree, filePath: "InMemory.cs", options);
        }

        /// <summary>
        /// Runs the value detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindValueFindingsForMember(string memberCode)
        {
            return FindValueFindingsForSource(Wrapper.WrapInClass(memberCode));
        }

        /// <summary>
        /// Runs the value detector on an in-memory member snippet that is wrapped into a class with explicit XML documentation options.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <param name="options">The XML documentation options used by the detector.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindValueFindingsForMember(string memberCode, XmlDocOptions options)
        {
            return FindValueFindingsForSource(Wrapper.WrapInClass(memberCode), options);
        }
        #endregion

        #region SeeDetector
        /// <summary>
        /// Runs the see/seealso detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSeeFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            return XmlDocSeeDetector.FindSeeSmells(tree, filePath: "InMemory.cs");
        }

        /// <summary>
        /// Runs the see/seealso detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSeeFindingsForMember(string memberCode)
        {
            return FindSeeFindingsForSource(Wrapper.WrapInClass(memberCode));
        }
        #endregion

        #region SemanticSeeDetector
        /// <summary>
        /// Runs the semantic see/seealso detector on a full in-memory C# source text.
        /// </summary>
        /// <param name="source">A complete C# source text.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSemanticSeeFindingsForSource(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InMemoryAssembly",
                syntaxTrees: new[] { tree },
                references: MetadataReferences.Default,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            return XmlDocSeeSemanticDetector.FindSeeSmells(
                tree,
                filePath: "InMemory.cs",
                semanticModel);
        }

        /// <summary>
        /// Runs the semantic see/seealso detector on an in-memory member snippet that is wrapped into a class.
        /// </summary>
        /// <param name="memberCode">A member declaration snippet.</param>
        /// <returns>A list of findings.</returns>
        public static List<Finding> FindSemanticSeeFindingsForMember(string memberCode)
        {
            return FindSemanticSeeFindingsForSource(Wrapper.WrapInClass(memberCode));
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
