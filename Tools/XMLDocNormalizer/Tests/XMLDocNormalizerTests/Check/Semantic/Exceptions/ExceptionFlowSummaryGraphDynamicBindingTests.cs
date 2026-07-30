using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests uncertainty produced by C# operations whose callable, member,
    /// operator, or conversion target is selected through dynamic binding at
    /// runtime.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphDynamicBindingTests
    {
        /// <summary>
        /// Ensures that a dynamically bound method invocation contributes
        /// uncertainty instead of being silently ignored.
        /// </summary>
        [Fact]
        public void DynamicMethodInvocation_AddsUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        value.Execute();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Contains(
                "Dynamic invocation binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that overload resolution is deferred when a statically
        /// named method receives a dynamic argument.
        /// </summary>
        [Fact]
        public void DynamicArgumentOverloadResolution_AddsUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        Target.Execute(value);
                    }
                }

                public static class Target
                {
                    public static void Execute(int value)
                    {
                    }

                    public static void Execute(string value)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Contains(
                "Dynamic invocation binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that dynamic member reads and writes are represented as
        /// unresolved runtime member binding.
        /// </summary>
        [Fact]
        public void DynamicMemberReadAndWrite_AddUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        object current = value.State;
                        value.State = current;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Contains(
                "Dynamic member binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that dynamic indexer reads and writes are represented as
        /// unresolved runtime indexer binding.
        /// </summary>
        [Fact]
        public void DynamicIndexerReadAndWrite_AddUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        object current = value[0];
                        value[0] = current;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Contains(
                "Dynamic indexer binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that constructor overload resolution involving a dynamic
        /// argument contributes uncertainty.
        /// </summary>
        [Fact]
        public void DynamicObjectCreation_AddsUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        Target target = new Target(value);
                    }
                }

                public sealed class Target
                {
                    public Target(int value)
                    {
                    }

                    public Target(string value)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Contains(
                "Dynamic object-creation binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that the different dynamic operator categories contribute
        /// explicit uncertainty.
        /// </summary>
        [Fact]
        public void DynamicOperators_AddUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        dynamic binary = value + 1;
                        dynamic unary = -value;
                        value++;
                        value += 1;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Contains(
                "Dynamic binary-operator binding",
                run.RootSummary.UncertainTargets);

            Assert.Contains(
                "Dynamic unary-operator binding",
                run.RootSummary.UncertainTargets);

            Assert.Contains(
                "Dynamic increment or decrement binding",
                run.RootSummary.UncertainTargets);

            Assert.Contains(
                "Dynamic compound-assignment binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that converting a dynamic value to a runtime-checked type
        /// contributes uncertainty.
        /// </summary>
        [Fact]
        public void DynamicConversion_AddsUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        int converted = value;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Contains(
                "Dynamic conversion binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that assigning a dynamic value to object does not invent
        /// runtime conversion uncertainty.
        /// </summary>
        [Fact]
        public void DynamicToObjectConversion_DoesNotAddUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        object converted = value;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a collection initializer whose Add overload is bound
        /// dynamically contributes uncertainty.
        /// </summary>
        [Fact]
        public void DynamicCollectionInitializerElement_AddsUncertainty()
        {
            const string source =
                """
                using System.Collections;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        Bag bag = new Bag
                        {
                            value
                        };
                    }
                }

                public sealed class Bag : IEnumerable<int>
                {
                    public void Add(int value)
                    {
                    }

                    public void Add(string value)
                    {
                    }

                    public IEnumerator<int> GetEnumerator()
                    {
                        yield break;
                    }

                    IEnumerator IEnumerable.GetEnumerator()
                    {
                        return GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Contains(
                "Dynamic invocation binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a catch-all suppresses uncertainty from a dynamic
        /// invocation in its protected block.
        /// </summary>
        [Fact]
        public void CatchAll_SuppressesDynamicUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        try
                        {
                            value.Execute();
                        }
                        catch
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a typed catch cannot prove that every exception from
        /// dynamic binding is handled.
        /// </summary>
        [Fact]
        public void TypedCatch_RetainsDynamicUncertainty()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        try
                        {
                            value.Execute();
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Contains(
                "Dynamic invocation binding",
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that dynamic binding inside an uncalled lambda does not
        /// leak into the containing method summary.
        /// </summary>
        [Fact]
        public void UncalledLambdaDynamicInvocation_IsExcluded()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(dynamic value)
                    {
                        Action action = () => value.Execute();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }
    }
}
