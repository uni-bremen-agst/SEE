using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests path collection for direct exception sources.
    /// </summary>
    public sealed class ExceptionFlowDirectPathTests
    {
        /// <summary>
        /// Ensures that an explicit throw statement produces one terminal
        /// source path.
        /// </summary>
        [Fact]
        public void ExplicitThrowStatement_ProducesTerminalPath()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        throw new ArgumentException();\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    source,
                    "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentException");

            ExceptionFlowPath path = Assert.Single(
                run.Result.GetExceptionPaths(exceptionType));

            ExceptionFlowPathStep step = Assert.Single(path.Steps);

            Assert.Equal(
                ExceptionFlowPathStepKind.ExplicitThrow,
                step.Kind);

            Assert.Equal(
                "System.ArgumentException",
                step.SymbolName);

            ThrowStatementSyntax throwStatement =
                Assert.Single(
                    run.Method.DescendantNodes()
                        .OfType<ThrowStatementSyntax>());

            AssertStepLocation(step, throwStatement);
        }

        /// <summary>
        /// Ensures that a throw expression produces one terminal source
        /// path at the throw expression.
        /// </summary>
        [Fact]
        public void ThrowExpression_ProducesTerminalPath()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public string M(string? value)\n" +
                "    {\n" +
                "        return value ??\n" +
                "               throw new ArgumentNullException(nameof(value));\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    source,
                    "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentNullException");

            ExceptionFlowPath path = Assert.Single(
                run.Result.GetExceptionPaths(exceptionType));

            ExceptionFlowPathStep step = Assert.Single(path.Steps);

            Assert.Equal(
                ExceptionFlowPathStepKind.ExplicitThrow,
                step.Kind);

            ThrowExpressionSyntax throwExpression =
                Assert.Single(
                    run.Method.DescendantNodes()
                        .OfType<ThrowExpressionSyntax>());

            AssertStepLocation(step, throwExpression);
        }

        /// <summary>
        /// Ensures that separate throw locations of the same exception type
        /// remain separate paths.
        /// </summary>
        [Fact]
        public void TwoThrowLocationsOfSameType_ProduceTwoPaths()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M(bool condition)\n" +
                "    {\n" +
                "        if (condition)\n" +
                "        {\n" +
                "            throw new ArgumentException();\n" +
                "        }\n" +
                "\n" +
                "        throw new ArgumentException();\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    source,
                    "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentException");

            IReadOnlyList<ExceptionFlowPath> paths =
                run.Result.GetExceptionPaths(exceptionType);

            Assert.Equal(2, paths.Count);
            Assert.False(
                run.Result.ArePathsTruncated(exceptionType));

            int[] lines = paths
                .Select(path => Assert.Single(path.Steps).Line)
                .OfType<int>()
                .OrderBy(static line => line)
                .ToArray();

            Assert.Equal([8, 11], lines);
        }

        /// <summary>
        /// Ensures that a throw in a branch proven unreachable does not
        /// produce an exception type or path.
        /// </summary>
        [Fact]
        public void ProvenUnreachableThrow_ProducesNoPath()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        if (false)\n" +
                "        {\n" +
                "            throw new ArgumentException();\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    source,
                    "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentException");

            Assert.DoesNotContain(
                exceptionType,
                run.Result.ThrownExceptions);

            Assert.Empty(
                run.Result.GetExceptionPaths(exceptionType));
        }

        /// <summary>
        /// Ensures that a modeled framework guard produces one helper path.
        /// </summary>
        [Fact]
        public void FrameworkThrowHelper_ProducesTerminalHelperPath()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M(object? value)\n" +
                "    {\n" +
                "        ArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    source,
                    "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentNullException");

            ExceptionFlowPath path = Assert.Single(
                run.Result.GetExceptionPaths(exceptionType));

            ExceptionFlowPathStep step = Assert.Single(path.Steps);

            Assert.Equal(
                ExceptionFlowPathStepKind.FrameworkThrowHelper,
                step.Kind);

            Assert.Contains(
                "ThrowIfNull",
                step.SymbolName,
                StringComparison.Ordinal);

            InvocationExpressionSyntax invocation =
                Assert.Single(
                    run.Method.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>());

            AssertStepLocation(step, invocation);
        }

        /// <summary>
        /// Ensures that a framework helper modeled with two possible
        /// exception types attaches a path to both types.
        /// </summary>
        [Fact]
        public void FrameworkHelperWithTwoTypes_ProducesPathForEachType()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M(string? value)\n" +
                "    {\n" +
                "        ArgumentException.ThrowIfNullOrWhiteSpace(value);\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    source,
                    "M");

            InvocationExpressionSyntax invocation =
                Assert.Single(
                    run.Method.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>());

            foreach (string metadataName in new[]
                     {
                         "System.ArgumentNullException",
                         "System.ArgumentException"
                     })
            {
                INamedTypeSymbol exceptionType =
                    run.GetRequiredType(metadataName);

                ExceptionFlowPath path = Assert.Single(
                    run.Result.GetExceptionPaths(exceptionType));

                ExceptionFlowPathStep step = Assert.Single(path.Steps);

                Assert.Equal(
                    ExceptionFlowPathStepKind.FrameworkThrowHelper,
                    step.Kind);

                Assert.Contains(
                    "ThrowIfNullOrWhiteSpace",
                    step.SymbolName,
                    StringComparison.Ordinal);

                AssertStepLocation(step, invocation);
            }
        }

        /// <summary>
        /// Ensures that a framework guard proven safe by value facts does
        /// not leave an exception type or path.
        /// </summary>
        [Fact]
        public void ProvenSafeFrameworkGuard_ProducesNoPath()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        ArgumentException.ThrowIfNullOrWhiteSpace(\"valid\");\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    source,
                    "M");

            foreach (string metadataName in new[]
                     {
                         "System.ArgumentNullException",
                         "System.ArgumentException"
                     })
            {
                INamedTypeSymbol exceptionType =
                    run.GetRequiredType(metadataName);

                Assert.DoesNotContain(
                    exceptionType,
                    run.Result.ThrownExceptions);

                Assert.Empty(
                    run.Result.GetExceptionPaths(exceptionType));
            }
        }

        /// <summary>
        /// Ensures that a delegate exception factory records both the
        /// throwing helper call and the concrete factory creation.
        /// </summary>
        [Fact]
        public void DelegateExceptionFactory_ProducesTwoStepPath()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        ThrowFactory(() => new InvalidOperationException());\n" +
                "    }\n" +
                "\n" +
                "    private static void ThrowFactory(Func<Exception> factory)\n" +
                "    {\n" +
                "        throw factory();\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.InvalidOperationException");

            ExceptionFlowPath path = Assert.Single(
                run.Result.GetExceptionPaths(exceptionType));

            Assert.Equal(2, path.Steps.Count);

            ExceptionFlowPathStep invocationStep = path.Steps[0];
            ExceptionFlowPathStep factoryStep = path.Steps[1];

            Assert.Equal(
                ExceptionFlowPathStepKind.MethodCall,
                invocationStep.Kind);

            Assert.Contains(
                "ThrowFactory",
                invocationStep.SymbolName,
                StringComparison.Ordinal);

            Assert.Equal(
                ExceptionFlowPathStepKind.DelegateExceptionFactory,
                factoryStep.Kind);

            Assert.Equal(
                "System.InvalidOperationException",
                factoryStep.SymbolName);

            InvocationExpressionSyntax throwingHelperInvocation =
                run.Method.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Single(invocation =>
                        invocation.Expression.ToString() ==
                        "ThrowFactory");

            ObjectCreationExpressionSyntax factoryCreation =
                Assert.Single(
                    run.Method.DescendantNodes()
                        .OfType<ObjectCreationExpressionSyntax>());

            AssertStepLocation(
                invocationStep,
                throwingHelperInvocation);

            AssertStepLocation(
                factoryStep,
                factoryCreation);
        }

        /// <summary>
        /// Ensures that catch-based suppression removes both the exception
        /// type and its direct source path.
        /// </summary>
        [Fact]
        public void CaughtDirectException_RemovesTypeAndPath()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            throw new ArgumentException();\n" +
                "        }\n" +
                "        catch (ArgumentException)\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(
                    source,
                    "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType("System.ArgumentException");

            Assert.DoesNotContain(
                exceptionType,
                run.Result.ThrownExceptions);

            Assert.Empty(
                run.Result.GetExceptionPaths(exceptionType));
        }

        /// <summary>
        /// Verifies that a path step contains the expected source position.
        /// </summary>
        /// <param name="step">The path step to inspect.</param>
        /// <param name="node">The source node represented by the step.</param>
        private static void AssertStepLocation(
            ExceptionFlowPathStep step,
            SyntaxNode node)
        {
            FileLinePositionSpan lineSpan =
                node.GetLocation().GetLineSpan();

            Assert.Equal(
                ExceptionFlowAnalyzerTestHelper.SourcePath,
                step.FilePath);

            Assert.Equal(
                lineSpan.StartLinePosition.Line + 1,
                step.Line);

            Assert.Equal(
                lineSpan.StartLinePosition.Character + 1,
                step.Column);
        }
    }
}
