using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides focused in-memory construction of exception-flow summary
    /// graphs.
    /// </summary>
    internal static class ExceptionFlowSummaryGraphTestHelper
    {
        /// <summary>
        /// Builds a summary graph rooted at one uniquely named method.
        /// </summary>
        /// <param name="source">
        /// The complete compilable C# source.
        /// </param>
        /// <param name="methodName">
        /// The uniquely occurring root method name.
        /// </param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the source does not compile, the root method cannot be
        /// resolved uniquely, or graph construction fails.
        /// </exception>
        public static ExceptionFlowSummaryGraphTestRun Build(
            string source,
            string methodName)
        {
            return BuildMember<MethodDeclarationSyntax>(
                source,
                methodName,
                static method =>
                    method.Identifier.ValueText);
        }

        /// <summary>
        /// Builds a summary graph rooted at one uniquely named property.
        /// </summary>
        /// <param name="source">
        /// The complete compilable C# source.
        /// </param>
        /// <param name="propertyName">
        /// The uniquely occurring property name.
        /// </param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the source does not compile, the property cannot be
        /// resolved uniquely, or graph construction fails.
        /// </exception>
        public static ExceptionFlowSummaryGraphTestRun BuildProperty(
            string source,
            string propertyName)
        {
            return BuildMember<PropertyDeclarationSyntax>(
                source,
                propertyName,
                static property =>
                    property.Identifier.ValueText);
        }

        /// <summary>
        /// Builds a summary graph rooted at one uniquely named custom event.
        /// </summary>
        /// <param name="source">
        /// The complete compilable C# source.
        /// </param>
        /// <param name="eventName">
        /// The uniquely occurring event name.
        /// </param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the source does not compile, the event cannot be
        /// resolved uniquely, or graph construction fails.
        /// </exception>
        public static ExceptionFlowSummaryGraphTestRun BuildEvent(
            string source,
            string eventName)
        {
            return BuildMember<EventDeclarationSyntax>(
                source,
                eventName,
                static eventDeclaration =>
                    eventDeclaration.Identifier.ValueText);
        }

        /// <summary>
        /// Builds a summary graph rooted at one uniquely named member of a
        /// specified syntax type.
        /// </summary>
        /// <typeparam name="TMemberSyntax">
        /// The expected member syntax type.
        /// </typeparam>
        /// <param name="source">
        /// The complete compilable C# source.
        /// </param>
        /// <param name="memberName">
        /// The uniquely occurring member name.
        /// </param>
        /// <param name="getMemberName">
        /// The function extracting the member name.
        /// </param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when compilation, member resolution, or graph construction
        /// fails.
        /// </exception>
        private static ExceptionFlowSummaryGraphTestRun
            BuildMember<TMemberSyntax>(
                string source,
                string memberName,
                Func<TMemberSyntax, string> getMemberName)
            where TMemberSyntax : MemberDeclarationSyntax
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                source);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                memberName);

            SyntaxTree syntaxTree =
                CSharpSyntaxTree.ParseText(
                    source,
                    path:
                        ExceptionFlowAnalyzerTestHelper.SourcePath);

            CSharpCompilation compilation =
                CSharpCompilation.Create(
                    assemblyName:
                        "ExceptionFlowSummaryGraphBuilderTests",
                    syntaxTrees:
                    [
                        syntaxTree
                    ],
                    references:
                        MetadataReferences.Default,
                    options:
                        new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary,
                            nullableContextOptions:
                                NullableContextOptions.Enable));

            Diagnostic[] errors =
                compilation.GetDiagnostics()
                    .Where(
                        static diagnostic =>
                            diagnostic.Severity ==
                            DiagnosticSeverity.Error)
                    .ToArray();

            if (errors.Length > 0)
            {
                throw new InvalidOperationException(
                    "The summary-graph test source did not compile:" +
                    Environment.NewLine +
                    string.Join(
                        Environment.NewLine,
                        errors.Select(
                            static error =>
                                error.ToString())));
            }

            TMemberSyntax[] matchingMembers =
                syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<TMemberSyntax>()
                    .Where(
                        member =>
                            getMemberName(member) ==
                            memberName)
                    .ToArray();

            if (matchingMembers.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one {typeof(TMemberSyntax).Name} " +
                    $"named '{memberName}', but found " +
                    $"{matchingMembers.Length}.");
            }

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContext
                    .CreateSingleCompilationContext(
                        syntaxTree,
                        compilation);

            bool built =
                ExceptionFlowAnalyzer
                    .TryBuildTransitiveSummaryGraph(
                        matchingMembers[0],
                        semanticContext,
                        out ExceptionFlowSummaryGraph graph,
                        out ExceptionFlowCallableKey rootKey);

            if (!built)
            {
                throw new InvalidOperationException(
                    "The exception-flow summary graph could not be built.");
            }

            return new ExceptionFlowSummaryGraphTestRun(
                graph,
                rootKey,
                compilation);
        }
    }

    /// <summary>
    /// Contains the graph and Roslyn compilation of one focused graph-build
    /// test.
    /// </summary>
    /// <param name="Graph">The constructed summary graph.</param>
    /// <param name="RootKey">The root callable key.</param>
    /// <param name="Compilation">The in-memory compilation.</param>
    internal sealed record ExceptionFlowSummaryGraphTestRun(
        ExceptionFlowSummaryGraph Graph,
        ExceptionFlowCallableKey RootKey,
        CSharpCompilation Compilation)
    {
        /// <summary>
        /// Gets the root callable summary.
        /// </summary>
        /// <value>The required root summary.</value>
        public ExceptionFlowSummary RootSummary =>
            GetRequiredSummary(
                RootKey);

        /// <summary>
        /// Gets one required callable summary.
        /// </summary>
        /// <param name="key">The callable key.</param>
        /// <returns>The associated summary.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the graph does not contain the key.
        /// </exception>
        public ExceptionFlowSummary GetRequiredSummary(
            ExceptionFlowCallableKey key)
        {
            if (Graph.TryGetSummary(
                    key,
                    out ExceptionFlowSummary? summary) &&
                summary != null)
            {
                return summary;
            }

            throw new InvalidOperationException(
                "The required summary was not present in the graph.");
        }

        /// <summary>
        /// Resolves one required exception type.
        /// </summary>
        /// <param name="metadataName">
        /// The full metadata type name.
        /// </param>
        /// <returns>The resolved type.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the type cannot be resolved.
        /// </exception>
        public INamedTypeSymbol GetRequiredType(
            string metadataName)
        {
            return Compilation.GetTypeByMetadataName(
                       metadataName) ??
                   throw new InvalidOperationException(
                       $"Could not resolve type '{metadataName}'.");
        }
    }
}
