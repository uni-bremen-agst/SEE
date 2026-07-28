using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests local functions, lambdas, anonymous methods, and locally
    /// resolvable delegate invocations in exception-flow summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphLocalCallableTests
    {
        /// <summary>
        /// Ensures that a local function contributes no flow when it is never
        /// invoked.
        /// </summary>
        [Fact]
        public void UncalledLocalFunction_IsExcludedFromOuterSummary()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        void Local()
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                1,
                run.Graph.Count);

            Assert.Empty(
                run.RootSummary.Sources);

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a directly invoked local function becomes a separate
        /// graph node.
        /// </summary>
        [Fact]
        public void CalledLocalFunction_CreatesLocalFunctionEdge()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Local();

                        void Local()
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge callEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.LocalFunctionCall,
                callEdge.CallSiteStep.Kind);

            IMethodSymbol localFunction =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    callEdge.Target.Symbol);

            Assert.Equal(
                MethodKind.LocalFunction,
                localFunction.MethodKind);

            ExceptionFlowSummary localSummary =
                run.GetRequiredSummary(
                    callEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    localSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that argument facts are transferred to a local function.
        /// </summary>
        [Fact]
        public void LocalFunctionCall_PropagatesParameterFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Local("value");

                        static void Local(string? value)
                        {
                            ArgumentNullException.ThrowIfNull(value);
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge callEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            ExceptionFlowSummary localSummary =
                run.GetRequiredSummary(
                    callEdge.Target);

            Assert.Empty(
                localSummary.Sources);
        }

        /// <summary>
        /// Ensures that recursive local functions terminate during graph
        /// construction while retaining their cycle edge.
        /// </summary>
        [Fact]
        public void RecursiveLocalFunction_TerminatesAndRetainsCycle()
        {
            const string source =
                """
                public sealed class TestClass
                {
                    public void M()
                    {
                        Local();

                        static void Local()
                        {
                            Local();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                2,
                run.Graph.Count);

            ExceptionFlowSummaryCallEdge rootToLocal =
                Assert.Single(
                    run.RootSummary.CallEdges);

            ExceptionFlowSummary localSummary =
                run.GetRequiredSummary(
                    rootToLocal.Target);

            ExceptionFlowSummaryCallEdge recursiveEdge =
                Assert.Single(
                    localSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.LocalFunctionCall,
                recursiveEdge.CallSiteStep.Kind);

            Assert.Equal(
                rootToLocal.Target,
                recursiveEdge.Target);
        }

        /// <summary>
        /// Ensures that an uncalled lambda body is excluded from its
        /// containing method.
        /// </summary>
        [Fact]
        public void UncalledLambda_IsExcludedFromOuterSummary()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action action =
                            () => throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                1,
                run.Graph.Count);

            Assert.Empty(
                run.RootSummary.Sources);

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a stable local delegate initialized with a lambda is
        /// resolved to the lambda body.
        /// </summary>
        [Fact]
        public void InvokedLocalLambda_CreatesDelegateEdge()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action action =
                            () => throw new InvalidOperationException();

                        action();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge delegateEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.DelegateInvocation,
                delegateEdge.CallSiteStep.Kind);

            IMethodSymbol anonymousFunction =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    delegateEdge.Target.Symbol);

            Assert.Equal(
                MethodKind.AnonymousFunction,
                anonymousFunction.MethodKind);

            ExceptionFlowSummary lambdaSummary =
                run.GetRequiredSummary(
                    delegateEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    lambdaSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that invocation arguments are transferred to lambda
        /// parameters.
        /// </summary>
        [Fact]
        public void LambdaInvocation_PropagatesParameterFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action<string?> action =
                            value =>
                                ArgumentNullException.ThrowIfNull(value);

                        action("value");
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge delegateEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            ExceptionFlowSummary lambdaSummary =
                run.GetRequiredSummary(
                    delegateEdge.Target);

            Assert.Empty(
                lambdaSummary.Sources);
        }

        /// <summary>
        /// Ensures that anonymous methods are analyzed when their stable local
        /// delegate is invoked.
        /// </summary>
        [Fact]
        public void InvokedAnonymousMethod_CreatesDelegateEdge()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action action =
                            delegate
                            {
                                throw new NotSupportedException();
                            };

                        action();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge delegateEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.DelegateInvocation,
                delegateEdge.CallSiteStep.Kind);

            ExceptionFlowSummary anonymousSummary =
                run.GetRequiredSummary(
                    delegateEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    anonymousSummary.Sources);

            Assert.Equal(
                "NotSupportedException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a stable delegate method group can resolve to a local
        /// function.
        /// </summary>
        [Fact]
        public void LocalFunctionMethodGroup_ResolvesDelegateTarget()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action action = Local;
                        action();

                        static void Local()
                        {
                            throw new ApplicationException();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge delegateEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.DelegateInvocation,
                delegateEdge.CallSiteStep.Kind);

            IMethodSymbol targetMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    delegateEdge.Target.Symbol);

            Assert.Equal(
                MethodKind.LocalFunction,
                targetMethod.MethodKind);

            ExceptionFlowSummary localSummary =
                run.GetRequiredSummary(
                    delegateEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    localSummary.Sources);

            Assert.Equal(
                "ApplicationException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a delegate local with another assignment is not
        /// treated as having one proven target.
        /// </summary>
        [Fact]
        public void ReassignedDelegate_IsMarkedUncertain()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action action = First;
                        action = Second;
                        action();

                        static void First()
                        {
                            throw new ArgumentException();
                        }

                        static void Second()
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                1,
                run.Graph.Count);

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.Sources);

            Assert.Contains(
                "Delegate invocation",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a directly invoked lambda is resolved without an
        /// intermediate local variable.
        /// </summary>
        [Fact]
        public void ImmediatelyInvokedLambda_CreatesDelegateEdge()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        ((Action)(() =>
                            throw new InvalidOperationException()))();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge delegateEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.DelegateInvocation,
                delegateEdge.CallSiteStep.Kind);

            ExceptionFlowSummary lambdaSummary =
                run.GetRequiredSummary(
                    delegateEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    lambdaSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                exceptionSource.ExceptionType.Name);
        }
    }
}
