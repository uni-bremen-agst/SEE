using System.Reflection;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Guards the acyclic lower layer shared by context construction and
    /// contextual value-fact discovery.
    /// </summary>
    public sealed class ExceptionFlowFactComponentDependencyTests
    {
        /// <summary>
        /// Ensures all extracted fact components remain closed static
        /// implementation details without interface dispatch.
        /// </summary>
        [Fact]
        public void Components_AreStaticAndImplementNoInterfaces()
        {
            Type[] componentTypes =
            [
                typeof(ExceptionFlowArgumentMapper),
                typeof(ExceptionFlowDereferenceFactDiscovery),
                typeof(ExceptionFlowStableMemberFacts),
                typeof(ExceptionFlowSymbolUsageFacts)
            ];

            Assert.All(componentTypes, type => Assert.True(type.IsAbstract));
            Assert.All(componentTypes, type => Assert.True(type.IsSealed));
            Assert.All(componentTypes, type => Assert.Empty(type.GetInterfaces()));
        }

        /// <summary>
        /// Ensures the lower fact components cannot call back into the
        /// remaining analyzer partials and form a component cycle.
        /// </summary>
        [Fact]
        public void ExtractedComponents_DoNotDependOnAnalyzer()
        {
            string flowDirectory = GetFlowDirectory();
            string[] componentFiles =
            [
                "ExceptionFlowArgumentMapper.cs",
                "ExceptionFlowDereferenceFactDiscovery.Callee.cs",
                "ExceptionFlowDereferenceFactDiscovery.cs",
                "ExceptionFlowStableMemberFacts.cs",
                "ExceptionFlowSymbolUsageFacts.cs"
            ];

            Assert.All(
                componentFiles,
                file => Assert.DoesNotContain(
                    "ExceptionFlowAnalyzer",
                    File.ReadAllText(Path.Combine(flowDirectory, file)),
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures dereference discovery owns the one successful-dereference
        /// cache and that the old analyzer owner no longer has a copy.
        /// </summary>
        [Fact]
        public void DereferenceDiscovery_IsSoleSuccessfulDereferenceCacheOwner()
        {
            const string fieldName = "successfulDereferenceCaches";
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            Assert.NotNull(
                typeof(ExceptionFlowDereferenceFactDiscovery).GetField(
                    fieldName,
                    flags));
            Assert.Null(
                typeof(ExceptionFlowAnalyzer).GetField(
                    fieldName,
                    flags));
        }

        /// <summary>
        /// Ensures the extracted lower layer does not acquire summary,
        /// executable-entry, or historical semantic-environment dependencies.
        /// </summary>
        [Fact]
        public void ExtractedComponents_ExcludeForbiddenDependencies()
        {
            string flowDirectory = GetFlowDirectory();
            string source = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(
                        flowDirectory,
                        "ExceptionFlow*Facts.cs",
                        SearchOption.TopDirectoryOnly)
                    .Where(
                        static path =>
                            Path.GetFileName(path) is
                                "ExceptionFlowStableMemberFacts.cs" or
                                "ExceptionFlowSymbolUsageFacts.cs")
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowArgumentMapper.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowDereferenceFactDiscovery.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowDereferenceFactDiscovery.Callee.cs"))
                    .Select(File.ReadAllText));

            string[] forbiddenNames =
            [
                "ExceptionFlowSummaryGraphEvaluator",
                "ProjectClosureSemanticContext",
                "SemanticCompilationScope",
                "SupportingSourceSymbolResolver",
                "CrossCompilationSymbolResolver",
                "Program.Main"
            ];

            Assert.All(
                forbiddenNames,
                name => Assert.DoesNotContain(
                    name,
                    source,
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Locates the exception-flow source directory from the test process.
        /// </summary>
        /// <returns>The absolute exception-flow source directory.</returns>
        private static string GetFlowDirectory()
        {
            string[] startingDirectories =
            [
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            ];

            foreach (string startingDirectory in startingDirectories)
            {
                DirectoryInfo? directory = new(startingDirectory);

                while (directory != null)
                {
                    string solutionPath = Path.Combine(
                        directory.FullName,
                        "XMLDocNormalizer.sln");

                    if (File.Exists(solutionPath))
                    {
                        return Path.Combine(
                            directory.FullName,
                            "src",
                            "XMLDocNormalizer",
                            "Checks",
                            "Infrastructure",
                            "Exception",
                            "Flow");
                    }

                    directory = directory.Parent;
                }
            }

            throw new InvalidOperationException(
                "Could not locate XMLDocNormalizer.sln from the current test execution directories.");
        }
    }
}
