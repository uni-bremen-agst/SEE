using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizerTests.Check.Semantic.Exceptions
{
    /// <summary>
    /// Guards the semantic dependency direction of the Roslyn-bound
    /// exception-flow analyzer source set.
    /// </summary>
    public sealed class ExceptionFlowSemanticDependencyGuardTests
    {
        /// <summary>
        /// Ensures analyzer components use the capability seam instead of
        /// Main project-closure, catalog, and resolver implementations.
        /// </summary>
        [Fact]
        public void AnalyzerSources_DoNotReferenceMainSemanticImplementations()
        {
            string solutionDirectory = Path.GetDirectoryName(FindSolutionPath())!;
            string flowDirectory = Path.Combine(
                solutionDirectory,
                "src",
                "XMLDocNormalizer",
                "Checks",
                "Infrastructure",
                "Exception",
                "Flow");
            string[] forbiddenIdentifiers =
            [
                "ProjectClosureSemanticContext",
                "SemanticCompilationScope",
                "SupportingSourceSymbolResolver",
                "CrossCompilationSymbolResolver",
                "SupportingSourceCatalog",
                "ProjectId"
            ];

            foreach (string sourcePath in Directory.EnumerateFiles(
                         flowDirectory,
                         "*.cs",
                         SearchOption.TopDirectoryOnly))
            {
                CompilationUnitSyntax root = CSharpSyntaxTree
                    .ParseText(File.ReadAllText(sourcePath))
                    .GetCompilationUnitRoot();
                string[] identifiers = root
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Select(static identifier => identifier.Identifier.ValueText)
                    .ToArray();

                Assert.DoesNotContain(
                    root.Usings,
                    directive => string.Equals(
                        directive.Name?.ToString(),
                        "XMLDocNormalizer.Execution.Semantic",
                        StringComparison.Ordinal));
                Assert.All(
                    forbiddenIdentifiers,
                    identifier => Assert.DoesNotContain(identifier, identifiers));
            }
        }

        /// <summary>
        /// Locates the repository solution from the test execution directory.
        /// </summary>
        /// <returns>The absolute solution path.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the solution cannot be located.
        /// </exception>
        private static string FindSolutionPath()
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
                    string candidate = Path.Combine(
                        directory.FullName,
                        "XMLDocNormalizer.sln");

                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }

                    directory = directory.Parent;
                }
            }

            throw new InvalidOperationException(
                "Could not locate XMLDocNormalizer.sln from the current test execution directories.");
        }
    }
}
