using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the shared complete and partial known-framework contract pipeline.
    /// </summary>
    public sealed class ExceptionFlowKnownFrameworkContractInfrastructureTests
    {
        /// <summary>
        /// Ensures that both default and populated contract results expose a
        /// valid immutable exception collection.
        /// </summary>
        [Fact]
        public void ContractResult_AlwaysExposesValidImmutableCollection()
        {
            KnownFrameworkExceptionContractResult emptyResult = default;

            Assert.Empty(emptyResult.PossibleExceptionTypes);

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    "public static class EntryPoint { public static void M() { } }",
                    "M");
            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.InvalidOperationException");
            ImmutableArray<INamedTypeSymbol> exceptionTypes =
                ImmutableArray.Create(exceptionType);
            KnownFrameworkExceptionContractResult populatedResult = new(exceptionTypes);

            Assert.Equal(exceptionTypes, populatedResult.PossibleExceptionTypes);
        }

        /// <summary>
        /// Ensures that applying a contract ignores an invalid null candidate
        /// while retaining every valid modeled exception source.
        /// </summary>
        [Fact]
        public void ContractApplication_IgnoresNullCandidateAndAddsValidSource()
        {
            const string source =
                "public static class EntryPoint\n" +
                "{\n" +
                "    public static void M()\n" +
                "    {\n" +
                "        External();\n" +
                "    }\n" +
                "    private static extern void External();\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(source, "M");
            InvocationExpressionSyntax invocation = Assert.Single(
                run.Method.DescendantNodes().OfType<InvocationExpressionSyntax>());
            SemanticModel semanticModel = run.Compilation.GetSemanticModel(run.SyntaxTree);
            IMethodSymbol methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(
                semanticModel.GetSymbolInfo(invocation).Symbol);
            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.InvalidOperationException");
            KnownFrameworkExceptionContractResult contractResult = new(
                ImmutableArray.Create(
                    null!,
                    exceptionType));
            KnownFrameworkExceptionContractEvaluation evaluation =
                KnownFrameworkExceptionContractEvaluation.CreateMatched(
                    KnownFrameworkExceptionContractCompleteness.Partial,
                    contractResult);
            ExceptionFlowAnalysisResult result = new();

            ExceptionFlowAnalyzer.AddKnownFrameworkContractExceptions(
                evaluation,
                result,
                methodSymbol,
                invocation);

            Assert.Equal(
                new[] { exceptionType },
                result.ThrownExceptions);
            Assert.Single(result.GetExceptionPaths(exceptionType));
        }

        /// <summary>
        /// Ensures that partial positive evidence is added without closing
        /// external analysis or removing existing uncertainty and documentation
        /// evidence.
        /// </summary>
        [Fact]
        public void PartialContract_AddsPositiveSourceAndPreservesOpenEvidence()
        {
            const string source =
                "public static class EntryPoint\n" +
                "{\n" +
                "    public static void M()\n" +
                "    {\n" +
                "        External();\n" +
                "    }\n" +
                "    private static extern void External();\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(source, "M");
            InvocationExpressionSyntax invocation = Assert.Single(
                run.Method.DescendantNodes().OfType<InvocationExpressionSyntax>());
            SemanticModel semanticModel = run.Compilation.GetSemanticModel(run.SyntaxTree);
            IMethodSymbol methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(
                semanticModel.GetSymbolInfo(invocation).Symbol);
            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.InvalidOperationException");

            KnownFrameworkExceptionContract contract = new(
                KnownFrameworkExceptionContractCompleteness.Partial,
                (candidate, _) => SymbolEqualityComparer.Default.Equals(candidate, methodSymbol),
                (_, _) => new KnownFrameworkExceptionContractResult(
                    ImmutableArray.Create(exceptionType)));
            KnownFrameworkExceptionContractEvaluation evaluation = contract.Evaluate(
                Array.Empty<KnownFrameworkExceptionContractArgument>(),
                run.Compilation);

            ExceptionFlowAnalysisResult result = new();
            result.UncertainTargets.Add("External()");
            result.AddExternalDocumentationEvidencePath(
                exceptionType,
                CreatePath(ExceptionFlowPathStepKind.ExternalDocumentationEvidence));

            ExceptionFlowAnalyzer.AddKnownFrameworkContractExceptions(
                evaluation,
                result,
                methodSymbol,
                invocation);

            Assert.True(evaluation.IsMatch);
            Assert.Equal(
                KnownFrameworkExceptionContractCompleteness.Partial,
                evaluation.Completeness);
            Assert.False(evaluation.ClosesExternalAnalysis);
            Assert.Contains(exceptionType, result.ThrownExceptions);
            Assert.Single(result.GetExceptionPaths(exceptionType));
            Assert.Contains("External()", result.UncertainTargets);
            Assert.Contains(exceptionType, result.ExternalDocumentationEvidenceExceptions);
            Assert.Single(result.GetExternalDocumentationEvidencePaths(exceptionType));
        }

        /// <summary>
        /// Ensures that a complete registered throw-helper contract contributes
        /// its source and closes unknown external handling.
        /// </summary>
        [Fact]
        public void CompleteThrowHelper_AddsSourceAndClosesExternalAnalysis()
        {
            const string source =
                "public static class EntryPoint\n" +
                "{\n" +
                "    public static void M(object? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "M");
            InvocationExpressionSyntax invocation = Assert.Single(
                run.Method.DescendantNodes().OfType<InvocationExpressionSyntax>());
            SemanticModel semanticModel = run.Compilation.GetSemanticModel(run.SyntaxTree);
            IMethodSymbol methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(
                semanticModel.GetSymbolInfo(invocation).Symbol);
            KnownFrameworkExceptionContractEvaluation evaluation =
                KnownFrameworkExceptionModel.EvaluateContract(
                    methodSymbol,
                    run.Compilation,
                    new KnownFrameworkExceptionContractArgument[methodSymbol.Parameters.Length]);
            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentNullException");

            Assert.True(evaluation.IsMatch);
            Assert.Equal(
                KnownFrameworkExceptionContractCompleteness.Complete,
                evaluation.Completeness);
            Assert.True(evaluation.ClosesExternalAnalysis);
            Assert.Single(run.Result.GetExceptionPaths(exceptionType));
            Assert.DoesNotContain(
                run.Result.UncertainTargets,
                target => target.Contains("ThrowIfNull", StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that summary evaluation retains the caller prefix and the
        /// established framework-helper terminal step.
        /// </summary>
        [Fact]
        public void SummaryThrowHelper_PreservesCallerPrefixAndTerminalStep()
        {
            const string source =
                "public static class EntryPoint\n" +
                "{\n" +
                "    public static void M(object? value)\n" +
                "    {\n" +
                "        Guard(value);\n" +
                "    }\n" +
                "    private static void Guard(object? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "M");
            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentNullException");
            ExceptionFlowPath path = Assert.Single(run.Result.GetExceptionPaths(exceptionType));

            Assert.Equal(
                new[]
                {
                    ExceptionFlowPathStepKind.MethodCall,
                    ExceptionFlowPathStepKind.FrameworkThrowHelper
                },
                path.Steps.Select(static step => step.Kind).ToArray());
        }

        /// <summary>
        /// Ensures that existing catch filtering applies unchanged to a
        /// framework-contract source in summary analysis.
        /// </summary>
        /// <param name="catchClause">The catch clause to insert.</param>
        /// <param name="exceptionEscapes">
        /// Whether the modeled exception should escape the caller.
        /// </param>
        [Theory]
        [InlineData("catch (System.ArgumentNullException)", false)]
        [InlineData("catch (System.ArgumentException)", false)]
        [InlineData("catch (System.InvalidOperationException)", true)]
        [InlineData("catch", false)]
        public void SummaryThrowHelper_UsesExistingCatchSemantics(
            string catchClause,
            bool exceptionEscapes)
        {
            string source =
                "public static class EntryPoint\n" +
                "{\n" +
                "    public static void M(object? value)\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            Guard(value);\n" +
                "        }\n" +
                $"        {catchClause}\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "    private static void Guard(object? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "M");
            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentNullException");

            Assert.Equal(
                exceptionEscapes,
                run.Result.ThrownExceptions.Contains(exceptionType));
        }

        /// <summary>
        /// Ensures that a framework constructor does not match an ordinary
        /// throw-helper contract merely because its containing type matches.
        /// </summary>
        [Fact]
        public void FrameworkConstructor_IsNotMatchedAsThrowHelper()
        {
            const string source =
                "public static class EntryPoint\n" +
                "{\n" +
                "    public static void M(string name)\n" +
                "    {\n" +
                "        _ = new System.ArgumentNullException(name);\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(source, "M");
            ObjectCreationExpressionSyntax creation = Assert.Single(
                run.Method.DescendantNodes().OfType<ObjectCreationExpressionSyntax>());
            SemanticModel semanticModel = run.Compilation.GetSemanticModel(run.SyntaxTree);
            IMethodSymbol constructor = Assert.IsAssignableFrom<IMethodSymbol>(
                semanticModel.GetSymbolInfo(creation).Symbol);

            KnownFrameworkExceptionContractEvaluation evaluation =
                KnownFrameworkExceptionModel.EvaluateContract(
                    constructor,
                    run.Compilation,
                    Array.Empty<KnownFrameworkExceptionContractArgument>());

            Assert.False(evaluation.IsMatch);
            Assert.Null(evaluation.Completeness);
            Assert.Empty(evaluation.PossibleExceptionTypes);
        }

        /// <summary>
        /// Ensures that wrong parameter counts and ref kinds do not match a
        /// throw-helper contract even when type and method metadata names match.
        /// </summary>
        /// <param name="parameters">The declared helper parameters.</param>
        /// <param name="arguments">The invocation arguments.</param>
        [Theory]
        [InlineData("object value", "value")]
        [InlineData("ref object value, string? paramName = null", "ref value")]
        public void WrongThrowHelperSignature_IsNotMatched(
            string parameters,
            string arguments)
        {
            string source =
                "namespace System\n" +
                "{\n" +
                "    public static class ArgumentNullException\n" +
                "    {\n" +
                $"        public static void ThrowIfNull({parameters}) {{ }}\n" +
                "    }\n" +
                "}\n" +
                "public static class EntryPoint\n" +
                "{\n" +
                "    public static void M(object value)\n" +
                "    {\n" +
                $"        System.ArgumentNullException.ThrowIfNull({arguments});\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(source, "M");
            InvocationExpressionSyntax invocation = Assert.Single(
                run.Method.DescendantNodes().OfType<InvocationExpressionSyntax>());
            SemanticModel semanticModel = run.Compilation.GetSemanticModel(run.SyntaxTree);
            IMethodSymbol methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(
                semanticModel.GetSymbolInfo(invocation).Symbol);

            KnownFrameworkExceptionContractEvaluation evaluation =
                KnownFrameworkExceptionModel.EvaluateContract(
                    methodSymbol,
                    run.Compilation,
                    new KnownFrameworkExceptionContractArgument[methodSymbol.Parameters.Length]);

            Assert.False(evaluation.IsMatch);
            Assert.False(evaluation.ClosesExternalAnalysis);
            Assert.Empty(evaluation.PossibleExceptionTypes);
        }

        /// <summary>
        /// Creates a one-step path for preserving existing test evidence.
        /// </summary>
        /// <param name="kind">The path step kind.</param>
        /// <returns>The created path.</returns>
        private static ExceptionFlowPath CreatePath(ExceptionFlowPathStepKind kind)
        {
            return new ExceptionFlowPath(
                new ExceptionFlowPathStep(
                    kind,
                    "External",
                    ExceptionFlowAnalyzerTestHelper.SourcePath,
                    Line: 1,
                    Column: 1));
        }
    }
}
