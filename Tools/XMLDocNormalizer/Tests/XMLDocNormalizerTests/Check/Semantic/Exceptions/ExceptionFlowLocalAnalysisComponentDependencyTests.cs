using System.Reflection;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Guards the dependency direction between catch semantics and direct
    /// local source discovery.
    /// </summary>
    public sealed class ExceptionFlowLocalAnalysisComponentDependencyTests
    {
        /// <summary>
        /// Ensures the extracted components are closed static implementation
        /// details without interface or inheritance dispatch.
        /// </summary>
        [Fact]
        public void Components_AreStaticAndImplementNoInterfaces()
        {
            Type[] componentTypes =
            [
                typeof(ExceptionFlowCatchSemantics),
                typeof(ExceptionFlowLocalSourceAnalyzer)
            ];

            Assert.All(componentTypes, type => Assert.True(type.IsAbstract));
            Assert.All(componentTypes, type => Assert.True(type.IsSealed));
            Assert.All(componentTypes, type => Assert.Empty(type.GetInterfaces()));
        }

        /// <summary>
        /// Ensures the extracted components own no mutable or long-lived
        /// analysis state. Compile-time metadata-name constants are allowed.
        /// </summary>
        [Fact]
        public void Components_OwnNoFields()
        {
            Type[] componentTypes =
            [
                typeof(ExceptionFlowCatchSemantics),
                typeof(ExceptionFlowLocalSourceAnalyzer)
            ];

            Assert.All(componentTypes, type => Assert.All(
                type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly),
                field => Assert.True(field.IsLiteral)));
        }

        /// <summary>
        /// Ensures catch semantics and direct local discovery cannot assume
        /// responsibility for summary-graph construction or evaluation.
        /// </summary>
        [Fact]
        public void CatchAndDirectLocalAnalysis_DoNotDependOnSummaryGraphComponents()
        {
            Type[] componentTypes =
            [
                typeof(ExceptionFlowCatchSemantics),
                typeof(ExceptionFlowLocalSourceAnalyzer)
            ];
            Type[] forbiddenTypes =
            [
                typeof(ExceptionFlowSummaryGraph),
                typeof(ExceptionFlowSummaryGraphBuilder),
                typeof(ExceptionFlowSummaryGraphEvaluator),
                typeof(ExceptionFlowSummaryAnalysisSession)
            ];

            Assert.All(
                componentTypes,
                type => Assert.Empty(GetDeclaredSignatureTypes(type)
                    .Where(forbiddenTypes.Contains)));
        }

        /// <summary>
        /// Ensures the remaining analyzer partials do not call back into the
        /// direct local component that already consumes analyzer fact logic.
        /// </summary>
        [Fact]
        public void AnalyzerSources_DoNotDependOnDirectLocalAnalysis()
        {
            string flowDirectory = GetFlowDirectory();

            foreach (string sourcePath in Directory.EnumerateFiles(
                         flowDirectory,
                         "ExceptionFlowAnalyzer*.cs",
                         SearchOption.TopDirectoryOnly))
            {
                Assert.DoesNotContain(
                    "ExceptionFlowLocalSourceAnalyzer",
                    File.ReadAllText(sourcePath),
                    StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Ensures catch semantics remains a leaf component rather than
        /// creating a dependency cycle with the remaining analyzer partials.
        /// </summary>
        [Fact]
        public void CatchSemantics_DoesNotDependOnAnalyzer()
        {
            string sourcePath = Path.Combine(
                GetFlowDirectory(),
                "ExceptionFlowCatchSemantics.cs");

            Assert.DoesNotContain(
                "ExceptionFlowAnalyzer",
                File.ReadAllText(sourcePath),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets all declared method return and parameter types for a component.
        /// </summary>
        /// <param name="type">The component type to inspect.</param>
        /// <returns>The types used by declared method signatures.</returns>
        private static IEnumerable<Type> GetDeclaredSignatureTypes(Type type)
        {
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            return methods.Select(method => method.ReturnType)
                .Concat(methods.SelectMany(method => method.GetParameters())
                    .Select(parameter => parameter.ParameterType));
        }

        /// <summary>
        /// Locates the exception-flow source directory from the test process.
        /// </summary>
        /// <returns>The absolute exception-flow source directory.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the solution cannot be located.
        /// </exception>
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
