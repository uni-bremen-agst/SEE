using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests productive summary-graph analysis of expression-bodied
    /// properties and indexers.
    /// </summary>
    public sealed class
        ExceptionFlowSummaryGraphExpressionBodiedAccessorTests
    {
        /// <summary>
        /// Ensures that an expression-bodied property getter remains a getter
        /// graph target while its property expression is analyzed.
        /// </summary>
        [Fact]
        public void ExpressionBodiedPropertyGetter_HasExecutableGetterSummary()
        {
            const string source =
                """
                using System;

                public sealed class Helper
                {
                    public int Value =>
                        throw new InvalidOperationException();
                }

                public sealed class TestClass
                {
                    public int M()
                    {
                        Helper helper = new Helper();
                        return helper.Value;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge getterEdge =
                Assert.Single(
                    run.RootSummary.CallEdges,
                    edge =>
                        edge.CallSiteStep.Kind ==
                        ExceptionFlowPathStepKind.PropertyGetter);

            IMethodSymbol getterSymbol =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    getterEdge.Target.Symbol);

            Assert.Equal(
                MethodKind.PropertyGet,
                getterSymbol.MethodKind);

            ExceptionFlowSummary getterSummary =
                run.GetRequiredSummary(
                    getterEdge.Target);

            Assert.True(
                getterSummary.HasExecutableBody);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    getterSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that an expression-bodied indexer getter remains a getter
        /// graph target while its indexer expression is analyzed.
        /// </summary>
        [Fact]
        public void ExpressionBodiedIndexerGetter_HasExecutableGetterSummary()
        {
            const string source =
                """
                using System;

                public sealed class Helper
                {
                    public int this[int index] =>
                        throw new InvalidOperationException();
                }

                public sealed class TestClass
                {
                    public int M()
                    {
                        Helper helper = new Helper();
                        return helper[0];
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge getterEdge =
                Assert.Single(
                    run.RootSummary.CallEdges,
                    edge =>
                        edge.CallSiteStep.Kind ==
                        ExceptionFlowPathStepKind.IndexerGetter);

            IMethodSymbol getterSymbol =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    getterEdge.Target.Symbol);

            Assert.Equal(
                MethodKind.PropertyGet,
                getterSymbol.MethodKind);

            ExceptionFlowSummary getterSummary =
                run.GetRequiredSummary(
                    getterEdge.Target);

            Assert.True(
                getterSummary.HasExecutableBody);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    getterSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                exceptionSource.ExceptionType.Name);
        }
    }
}
