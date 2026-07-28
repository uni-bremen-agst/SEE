using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the compact callable-summary graph used by transitive
    /// exception-flow analysis.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphTests
    {
        /// <summary>
        /// Ensures that callable keys normalize constructed generic methods
        /// while retaining distinct call-context identities.
        /// </summary>
        [Fact]
        public void CallableKey_NormalizesSymbolAndIncludesContext()
        {
            TestSymbols symbols =
                CreateTestSymbols();

            IMethodSymbol constructedMethod =
                symbols.GenericMethod.Construct(
                    symbols.Compilation.GetSpecialType(
                        SpecialType.System_Int32));

            ExceptionFlowCallableKey definitionKey =
                new(
                    symbols.GenericMethod,
                    "0:1");

            ExceptionFlowCallableKey constructedKey =
                new(
                    constructedMethod,
                    "0:1");

            ExceptionFlowCallableKey differentContextKey =
                new(
                    symbols.GenericMethod,
                    "0:3");

            Assert.Equal(
                definitionKey,
                constructedKey);

            Assert.Equal(
                definitionKey.GetHashCode(),
                constructedKey.GetHashCode());

            Assert.NotEqual(
                definitionKey,
                differentContextKey);
        }

        /// <summary>
        /// Ensures that one context-sensitive callable is scheduled exactly
        /// once even when it is reached repeatedly.
        /// </summary>
        [Fact]
        public void Graph_GetOrAdd_QueuesCallableExactlyOnce()
        {
            TestSymbols symbols =
                CreateTestSymbols();

            ExceptionFlowCallableKey key =
                new(
                    symbols.FirstMethod,
                    string.Empty);

            ExceptionFlowSummaryGraph graph =
                new();

            ExceptionFlowSummary firstSummary =
                graph.GetOrAdd(key);

            ExceptionFlowSummary secondSummary =
                graph.GetOrAdd(key);

            Assert.Same(
                firstSummary,
                secondSummary);

            Assert.Equal(
                1,
                graph.Count);

            Assert.True(
                graph.TryDequeuePending(
                    out ExceptionFlowCallableKey dequeuedKey));

            Assert.Equal(
                key,
                dequeuedKey);

            Assert.False(
                graph.TryDequeuePending(
                    out _));
        }

        /// <summary>
        /// Ensures that a typed catch removes matching local sources but
        /// retains call edges with an appropriate exception filter.
        /// </summary>
        [Fact]
        public void TypedCatch_RemovesMatchingSourcesAndFiltersCallEdges()
        {
            TestSymbols symbols =
                CreateTestSymbols();

            INamedTypeSymbol argumentException =
                GetRequiredType(
                    symbols.Compilation,
                    "System.ArgumentException");

            INamedTypeSymbol argumentNullException =
                GetRequiredType(
                    symbols.Compilation,
                    "System.ArgumentNullException");

            INamedTypeSymbol invalidOperationException =
                GetRequiredType(
                    symbols.Compilation,
                    "System.InvalidOperationException");

            ExceptionFlowSummaryFragment fragment =
                new();

            fragment.AddSource(
                CreateSource(
                    argumentNullException,
                    20));

            fragment.AddSource(
                CreateSource(
                    invalidOperationException,
                    21));

            ExceptionFlowSummaryCallEdge callEdge =
                new(
                    new ExceptionFlowCallableKey(
                        symbols.SecondMethod,
                        string.Empty),
                    CreateCallStep(
                        symbols.SecondMethod,
                        10));

            fragment.AddCallEdge(
                callEdge);

            fragment.AddUncertainTarget(
                "External.Unknown()");

            fragment.SuppressCaughtException(
                argumentException);

            ExceptionFlowSummarySource remainingSource =
                Assert.Single(
                    fragment.Sources);

            Assert.True(
                SymbolEqualityComparer.Default.Equals(
                    invalidOperationException,
                    remainingSource.ExceptionType));

            ExceptionFlowSummaryCallEdge remainingEdge =
                Assert.Single(
                    fragment.CallEdges);

            Assert.True(
                remainingEdge.Suppresses(
                    argumentException));

            Assert.True(
                remainingEdge.Suppresses(
                    argumentNullException));

            Assert.False(
                remainingEdge.Suppresses(
                    invalidOperationException));

            Assert.Contains(
                "External.Unknown()",
                fragment.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a catch-all removes every kind of escaping flow from
        /// the protected fragment.
        /// </summary>
        [Fact]
        public void CatchAll_ClearsSourcesEdgesAndUncertainty()
        {
            TestSymbols symbols =
                CreateTestSymbols();

            INamedTypeSymbol exceptionType =
                GetRequiredType(
                    symbols.Compilation,
                    "System.InvalidOperationException");

            ExceptionFlowSummaryFragment fragment =
                new();

            fragment.AddSource(
                CreateSource(
                    exceptionType,
                    20));

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    new ExceptionFlowCallableKey(
                        symbols.SecondMethod,
                        string.Empty),
                    CreateCallStep(
                        symbols.SecondMethod,
                        10)));

            fragment.AddUncertainTarget(
                "External.Unknown()");

            fragment.SuppressAll();

            Assert.Empty(
                fragment.Sources);

            Assert.Empty(
                fragment.CallEdges);

            Assert.Empty(
                fragment.UncertainTargets);
        }

        /// <summary>
        /// Ensures that two different source-level calls to the same target
        /// remain distinct graph edges.
        /// </summary>
        [Fact]
        public void Summary_PreservesDistinctCallSitesToSameTarget()
        {
            TestSymbols symbols =
                CreateTestSymbols();

            ExceptionFlowCallableKey target =
                new(
                    symbols.SecondMethod,
                    string.Empty);

            ExceptionFlowSummaryFragment fragment =
                new();

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    target,
                    CreateCallStep(
                        symbols.SecondMethod,
                        10)));

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    target,
                    CreateCallStep(
                        symbols.SecondMethod,
                        11)));

            ExceptionFlowSummary summary =
                new();

            summary.MarkExecutableBodyAnalyzed();
            summary.Merge(fragment);

            Assert.True(
                summary.HasExecutableBody);

            Assert.Equal(
                2,
                summary.CallEdges.Count);

            int?[] callSiteLines =
                summary.CallEdges
                    .Select(
                        edge => edge.CallSiteStep.Line)
                    .OrderBy(
                        static line => line)
                    .ToArray();

            Assert.Equal(
                new int?[] { 10, 11 },
                callSiteLines);

            Assert.All(
                summary.CallEdges,
                edge =>
                    Assert.Equal(
                        target,
                        edge.Target));
        }

        /// <summary>
        /// Creates a local exception source for one test exception type.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type represented by the source.
        /// </param>
        /// <param name="line">The source line.</param>
        /// <returns>The created local exception source.</returns>
        private static ExceptionFlowSummarySource CreateSource(
            INamedTypeSymbol exceptionType,
            int line)
        {
            ExceptionFlowPathStep terminalStep =
                new(
                    ExceptionFlowPathStepKind.ExplicitThrow,
                    exceptionType.ToDisplayString(
                        SymbolDisplayFormat.CSharpErrorMessageFormat),
                    ExceptionFlowAnalyzerTestHelper.SourcePath,
                    line,
                    9);

            return new ExceptionFlowSummarySource(
                exceptionType,
                new ExceptionFlowPath(
                    terminalStep));
        }

        /// <summary>
        /// Creates one source-level method-call step.
        /// </summary>
        /// <param name="methodSymbol">
        /// The called method symbol.
        /// </param>
        /// <param name="line">The source line.</param>
        /// <returns>The created method-call step.</returns>
        private static ExceptionFlowPathStep CreateCallStep(
            IMethodSymbol methodSymbol,
            int line)
        {
            return new ExceptionFlowPathStep(
                ExceptionFlowPathStepKind.MethodCall,
                methodSymbol.ToDisplayString(
                    SymbolDisplayFormat.CSharpErrorMessageFormat),
                ExceptionFlowAnalyzerTestHelper.SourcePath,
                line,
                9);
        }

        /// <summary>
        /// Creates the Roslyn symbols used by the summary-graph tests.
        /// </summary>
        /// <returns>The created compilation and method symbols.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the test source does not compile or its symbols cannot
        /// be resolved.
        /// </exception>
        private static TestSymbols CreateTestSymbols()
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void First()\n" +
                "    {\n" +
                "    }\n" +
                "\n" +
                "    public void Second()\n" +
                "    {\n" +
                "    }\n" +
                "\n" +
                "    public void Generic<T>()\n" +
                "    {\n" +
                "    }\n" +
                "}\n";

            SyntaxTree syntaxTree =
                CSharpSyntaxTree.ParseText(
                    source,
                    path:
                        ExceptionFlowAnalyzerTestHelper.SourcePath);

            CSharpCompilation compilation =
                CSharpCompilation.Create(
                    assemblyName:
                        "ExceptionFlowSummaryGraphTests",
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

            INamedTypeSymbol testClass =
                compilation.GetTypeByMetadataName(
                    "TestClass") ??
                throw new InvalidOperationException(
                    "Could not resolve TestClass.");

            IMethodSymbol firstMethod =
                GetRequiredMethod(
                    testClass,
                    "First");

            IMethodSymbol secondMethod =
                GetRequiredMethod(
                    testClass,
                    "Second");

            IMethodSymbol genericMethod =
                GetRequiredMethod(
                    testClass,
                    "Generic");

            return new TestSymbols(
                compilation,
                firstMethod,
                secondMethod,
                genericMethod);
        }

        /// <summary>
        /// Resolves one required method from a test type.
        /// </summary>
        /// <param name="typeSymbol">
        /// The containing test type.
        /// </param>
        /// <param name="methodName">
        /// The required method name.
        /// </param>
        /// <returns>The uniquely resolved method.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the method cannot be resolved uniquely.
        /// </exception>
        private static IMethodSymbol GetRequiredMethod(
            INamedTypeSymbol typeSymbol,
            string methodName)
        {
            IMethodSymbol[] methods =
                typeSymbol.GetMembers(methodName)
                    .OfType<IMethodSymbol>()
                    .ToArray();

            if (methods.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one method named '{methodName}', " +
                    $"but found {methods.Length}.");
            }

            return methods[0];
        }

        /// <summary>
        /// Resolves one required type from a test compilation.
        /// </summary>
        /// <param name="compilation">
        /// The test compilation.
        /// </param>
        /// <param name="metadataName">
        /// The required metadata name.
        /// </param>
        /// <returns>The resolved type symbol.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the type cannot be resolved.
        /// </exception>
        private static INamedTypeSymbol GetRequiredType(
            Compilation compilation,
            string metadataName)
        {
            return compilation.GetTypeByMetadataName(
                       metadataName) ??
                   throw new InvalidOperationException(
                       $"Could not resolve type '{metadataName}'.");
        }

        /// <summary>
        /// Contains the Roslyn symbols used by one test.
        /// </summary>
        /// <param name="Compilation">
        /// The in-memory test compilation.
        /// </param>
        /// <param name="FirstMethod">
        /// The first ordinary method.
        /// </param>
        /// <param name="SecondMethod">
        /// The second ordinary method.
        /// </param>
        /// <param name="GenericMethod">
        /// The generic method definition.
        /// </param>
        private sealed record TestSymbols(
            CSharpCompilation Compilation,
            IMethodSymbol FirstMethod,
            IMethodSymbol SecondMethod,
            IMethodSymbol GenericMethod);
    }
}
