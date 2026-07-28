using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests property, indexer, init, and event accessor writes in
    /// exception-flow summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphAccessorWriteTests
    {
        /// <summary>
        /// Ensures that a simple property assignment invokes only the setter
        /// and transfers value facts to it.
        /// </summary>
        [Fact]
        public void SimplePropertyAssignment_CreatesOnlySetterEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(Target target)
                    {
                        target.Value = "value";
                    }
                }

                public sealed class Target
                {
                    public string Value
                    {
                        get
                        {
                            throw new NotSupportedException();
                        }

                        set
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

            ExceptionFlowSummaryCallEdge setterEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.PropertySetter,
                setterEdge.CallSiteStep.Kind);

            ExceptionFlowSummary setterSummary =
                run.GetRequiredSummary(
                    setterEdge.Target);

            Assert.Empty(
                setterSummary.Sources);
        }

        /// <summary>
        /// Ensures that an unknown assignment value remains unknown inside
        /// the setter.
        /// </summary>
        [Fact]
        public void PropertyAssignment_PropagatesUnknownValue()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Target target,
                        string? value)
                    {
                        target.Value = value;
                    }
                }

                public sealed class Target
                {
                    public string Value
                    {
                        get
                        {
                            return string.Empty;
                        }

                        set
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

            ExceptionFlowSummaryCallEdge setterEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            ExceptionFlowSummary setterSummary =
                run.GetRequiredSummary(
                    setterEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    setterSummary.Sources);

            Assert.Equal(
                "ArgumentNullException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a compound property assignment invokes both the
        /// getter and the setter.
        /// </summary>
        [Fact]
        public void CompoundPropertyAssignment_CreatesGetterAndSetterEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Target target)
                    {
                        target.Value += 1;
                    }
                }

                public sealed class Target
                {
                    public int Value
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }

                        set
                        {
                            throw new ArgumentException();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowPathStepKind[] kinds =
                run.RootSummary.CallEdges
                    .Select(
                        edge =>
                            edge.CallSiteStep.Kind)
                    .OrderBy(
                        static kind =>
                            kind)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    ExceptionFlowPathStepKind.PropertyGetter,
                    ExceptionFlowPathStepKind.PropertySetter
                }
                .OrderBy(
                    static kind =>
                        kind)
                .ToArray(),
                kinds);

            Assert.All(
                run.RootSummary.CallEdges,
                edge =>
                {
                    ExceptionFlowSummary summary =
                        run.GetRequiredSummary(
                            edge.Target);

                    Assert.Single(
                        summary.Sources);
                });
        }

        /// <summary>
        /// Ensures that incrementing a property invokes both accessors.
        /// </summary>
        [Fact]
        public void PropertyIncrement_CreatesGetterAndSetterEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(Target target)
                    {
                        target.Value++;
                    }
                }

                public sealed class Target
                {
                    public int Value
                    {
                        get
                        {
                            return 0;
                        }

                        set
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowPathStepKind[] kinds =
                run.RootSummary.CallEdges
                    .Select(
                        edge =>
                            edge.CallSiteStep.Kind)
                    .OrderBy(
                        static kind =>
                            kind)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    ExceptionFlowPathStepKind.PropertyGetter,
                    ExceptionFlowPathStepKind.PropertySetter
                }
                .OrderBy(
                    static kind =>
                        kind)
                .ToArray(),
                kinds);
        }

        /// <summary>
        /// Ensures that an object initializer uses a property init accessor
        /// and transfers the initialized value.
        /// </summary>
        [Fact]
        public void ObjectInitializer_CreatesPropertyInitEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        _ = new Target
                        {
                            Value = "value"
                        };
                    }
                }

                public sealed class Target
                {
                    public string Value
                    {
                        get
                        {
                            return string.Empty;
                        }

                        init
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

            ExceptionFlowSummaryCallEdge initEdge =
                Assert.Single(
                    run.RootSummary.CallEdges.Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                            ExceptionFlowPathStepKind.PropertyInit));

            ExceptionFlowSummary initSummary =
                run.GetRequiredSummary(
                    initEdge.Target);

            Assert.Empty(
                initSummary.Sources);
        }

        /// <summary>
        /// Ensures that a simple indexer assignment invokes only its setter
        /// and transfers both index and value facts.
        /// </summary>
        [Fact]
        public void SimpleIndexerAssignment_CreatesOnlySetterEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(Target target)
                    {
                        target["key"] = "value";
                    }
                }

                public sealed class Target
                {
                    public string this[string? index]
                    {
                        get
                        {
                            throw new NotSupportedException();
                        }

                        set
                        {
                            ArgumentNullException.ThrowIfNull(index);
                            ArgumentNullException.ThrowIfNull(value);
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge setterEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.IndexerSetter,
                setterEdge.CallSiteStep.Kind);

            ExceptionFlowSummary setterSummary =
                run.GetRequiredSummary(
                    setterEdge.Target);

            Assert.Empty(
                setterSummary.Sources);
        }

        /// <summary>
        /// Ensures that a compound indexer assignment invokes its getter and
        /// setter.
        /// </summary>
        [Fact]
        public void CompoundIndexerAssignment_CreatesGetterAndSetterEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(Target target)
                    {
                        target[0] += 1;
                    }
                }

                public sealed class Target
                {
                    public int this[int index]
                    {
                        get
                        {
                            return 0;
                        }

                        set
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowPathStepKind[] kinds =
                run.RootSummary.CallEdges
                    .Select(
                        edge =>
                            edge.CallSiteStep.Kind)
                    .OrderBy(
                        static kind =>
                            kind)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    ExceptionFlowPathStepKind.IndexerGetter,
                    ExceptionFlowPathStepKind.IndexerSetter
                }
                .OrderBy(
                    static kind =>
                        kind)
                .ToArray(),
                kinds);
        }

        /// <summary>
        /// Ensures that an indexer assignment in an object initializer invokes
        /// an indexer init accessor.
        /// </summary>
        [Fact]
        public void ObjectInitializer_CreatesIndexerInitEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        _ = new Target
                        {
                            [0] = "value"
                        };
                    }
                }

                public sealed class Target
                {
                    public string this[int index]
                    {
                        get
                        {
                            return string.Empty;
                        }

                        init
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

            ExceptionFlowSummaryCallEdge initEdge =
                Assert.Single(
                    run.RootSummary.CallEdges.Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                            ExceptionFlowPathStepKind.IndexerInit));

            ExceptionFlowSummary initSummary =
                run.GetRequiredSummary(
                    initEdge.Target);

            Assert.Empty(
                initSummary.Sources);
        }

        /// <summary>
        /// Ensures that subscribing to a custom event invokes its add
        /// accessor.
        /// </summary>
        [Fact]
        public void CustomEventSubscription_CreatesEventAddEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Target target,
                        EventHandler handler)
                    {
                        target.Changed += handler;
                    }
                }

                public sealed class Target
                {
                    public event EventHandler Changed
                    {
                        add
                        {
                            throw new InvalidOperationException();
                        }

                        remove
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge addEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.EventAdd,
                addEdge.CallSiteStep.Kind);

            ExceptionFlowSummary addSummary =
                run.GetRequiredSummary(
                    addEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    addSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that unsubscribing from a custom event invokes its remove
        /// accessor.
        /// </summary>
        [Fact]
        public void CustomEventUnsubscription_CreatesEventRemoveEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Target target,
                        EventHandler handler)
                    {
                        target.Changed -= handler;
                    }
                }

                public sealed class Target
                {
                    public event EventHandler Changed
                    {
                        add
                        {
                        }

                        remove
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge removeEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.EventRemove,
                removeEdge.CallSiteStep.Kind);

            ExceptionFlowSummary removeSummary =
                run.GetRequiredSummary(
                    removeEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    removeSummary.Sources);

            Assert.Equal(
                "NotSupportedException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that compiler-generated field-like event accessors do not
        /// create user-code call edges.
        /// </summary>
        [Fact]
        public void FieldLikeEventSubscription_DoesNotCreateCustomEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Target target,
                        EventHandler handler)
                    {
                        target.Changed += handler;
                    }
                }

                public sealed class Target
                {
                    public event EventHandler? Changed;
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.Sources);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a property root includes both explicit accessor
        /// bodies.
        /// </summary>
        [Fact]
        public void PropertyRoot_AnalyzesGetterAndSetterBodies()
        {
            const string source =
                """
                using System;

                public sealed class Target
                {
                    public int Value
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }

                        set
                        {
                            throw new ArgumentException();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.BuildProperty(
                    source,
                    "Value");

            string[] exceptionNames =
                run.RootSummary.Sources
                    .Select(
                        sourceEntry =>
                            sourceEntry.ExceptionType.Name)
                    .OrderBy(
                        static name =>
                            name,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "ArgumentException",
                    "InvalidOperationException"
                },
                exceptionNames);
        }

        /// <summary>
        /// Ensures that a custom event root includes its add and remove
        /// accessor bodies.
        /// </summary>
        [Fact]
        public void EventRoot_AnalyzesAddAndRemoveBodies()
        {
            const string source =
                """
                using System;

                public sealed class Target
                {
                    public event EventHandler Changed
                    {
                        add
                        {
                            throw new InvalidOperationException();
                        }

                        remove
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.BuildEvent(
                    source,
                    "Changed");

            string[] exceptionNames =
                run.RootSummary.Sources
                    .Select(
                        sourceEntry =>
                            sourceEntry.ExceptionType.Name)
                    .OrderBy(
                        static name =>
                            name,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "InvalidOperationException",
                    "NotSupportedException"
                },
                exceptionNames);
        }
    }
}
