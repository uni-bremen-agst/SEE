using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests implicit calls produced by classic collection initializers.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphCollectionInitializerTests
    {
        /// <summary>
        /// Ensures that construction and every Add call are represented.
        /// </summary>
        [Fact]
        public void CollectionInitializer_CreatesConstructorAndAddEdges()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            "first",
                            "second"
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public Collection()
                    {
                        throw new ArgumentException();
                    }

                    public void Add(string value)
                    {
                        throw new InvalidOperationException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.ConstructorCall));

            Assert.Equal(
                2,
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall)
                    .Length);
        }

        /// <summary>
        /// Ensures that a complex element selects a multi-parameter Add.
        /// </summary>
        [Fact]
        public void ComplexElementInitializer_SelectsMultiParameterAdd()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            { 1, "value" }
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(
                        int key,
                        string value)
                    {
                        throw new InvalidOperationException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            IMethodSymbol addMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    GetRequiredEdge(
                            run,
                            ExceptionFlowPathStepKind.CollectionAddCall)
                        .Target
                        .Symbol);

            Assert.Equal(
                2,
                addMethod.Parameters.Length);
        }

        /// <summary>
        /// Ensures that Roslyn's selected overload is retained.
        /// </summary>
        [Fact]
        public void OverloadedAdd_SelectsApplicableOverload()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            "value"
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(int value)
                    {
                        throw new ArgumentException();
                    }

                    public void Add(string value)
                    {
                        throw new FormatException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall),
                "FormatException");
        }

        /// <summary>
        /// Ensures that omitted optional parameters contribute facts.
        /// </summary>
        [Fact]
        public void OptionalAddParameter_UsesDefaultFacts()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            1
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(
                        int value,
                        string? name = "known")
                    {
                        ArgumentNullException.ThrowIfNull(name);
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge addEdge =
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall);

            Assert.Empty(
                run.GetRequiredSummary(
                        addEdge.Target)
                    .Sources);
        }

        /// <summary>
        /// Ensures that expanded params create a non-null array.
        /// </summary>
        [Fact]
        public void ExpandedParamsAdd_ProvidesNonNullArrayFact()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            { 1, new object(), new object() }
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(
                        int value,
                        params object[] remaining)
                    {
                        ArgumentNullException.ThrowIfNull(remaining);
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge addEdge =
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall);

            Assert.Empty(
                run.GetRequiredSummary(
                        addEdge.Target)
                    .Sources);
        }

        /// <summary>
        /// Ensures that a directly supplied nullable params array remains
        /// nullable.
        /// </summary>
        [Fact]
        public void DirectParamsArrayArgument_PreservesNullPossibility()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            (object[]?)null
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(params object[]? values)
                    {
                        ArgumentNullException.ThrowIfNull(values);
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall),
                "ArgumentNullException");
        }

        /// <summary>
        /// Ensures that extension Add receives receiver facts.
        /// </summary>
        [Fact]
        public void ExtensionAdd_MapsFreshReceiverFacts()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            "known"
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }

                public static class CollectionExtensions
                {
                    public static void Add(
                        this Collection? collection,
                        string? value)
                    {
                        ArgumentNullException.ThrowIfNull(collection);
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge addEdge =
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall);

            Assert.Equal(
                "CollectionExtensions",
                addEdge.Target.Symbol.ContainingType?.Name);

            Assert.Empty(
                run.GetRequiredSummary(
                        addEdge.Target)
                    .Sources);
        }

        /// <summary>
        /// Ensures that a nested property uses its getter, not its setter.
        /// </summary>
        [Fact]
        public void NestedPropertyCollectionInitializer_UsesGetterNotSetter()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Container container = new Container
                        {
                            Values =
                            {
                                "value"
                            }
                        };
                    }
                }

                public sealed class Container
                {
                    public Collection Values
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }
                        set
                        {
                            throw new NotSupportedException();
                        }
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(string value)
                    {
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.PropertyGetter),
                "InvalidOperationException");

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall));

            Assert.Empty(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.PropertySetter));
        }

        /// <summary>
        /// Ensures that a nested indexer uses its getter, not its setter.
        /// </summary>
        [Fact]
        public void NestedIndexerCollectionInitializer_UsesGetterNotSetter()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Container container = new Container
                        {
                            [1] =
                            {
                                "value"
                            }
                        };
                    }
                }

                public sealed class Container
                {
                    public Collection this[int index]
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }
                        set
                        {
                            throw new NotSupportedException();
                        }
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(string value)
                    {
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.IndexerGetter),
                "InvalidOperationException");

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall));

            Assert.Empty(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.IndexerSetter));
        }

        /// <summary>
        /// Ensures that an index initializer remains an indexer write.
        /// </summary>
        [Fact]
        public void IndexInitializer_UsesIndexerSetterNotCollectionAdd()
        {
            const string source =
                """
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            [1] = "value"
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public string this[int index]
                    {
                        get
                        {
                            return string.Empty;
                        }
                        set
                        {
                        }
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return System.Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.IndexerSetter));

            Assert.Empty(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall));
        }

        /// <summary>
        /// Ensures that target-typed construction and Add are represented.
        /// </summary>
        [Fact]
        public void TargetTypedNewCollectionInitializer_CreatesBothEdges()
        {
            const string source =
                """
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new()
                        {
                            "value"
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public Collection()
                    {
                    }

                    public void Add(string value)
                    {
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return System.Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.ConstructorCall));

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall));
        }

        /// <summary>
        /// Ensures constructor and Add exception sources remain separate.
        /// </summary>
        [Fact]
        public void ConstructorAndAddExceptions_RemainSeparate()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            "value"
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public Collection()
                    {
                        throw new ArgumentException();
                    }

                    public void Add(string value)
                    {
                        throw new InvalidOperationException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.ConstructorCall),
                "ArgumentException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an outer catch filters constructor and Add edges.
        /// </summary>
        [Fact]
        public void CatchAroundCollectionInitializer_SuppressesImplicitEdges()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        try
                        {
                            Collection collection = new Collection
                            {
                                "value"
                            };
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public Collection()
                    {
                        throw new InvalidOperationException();
                    }

                    public void Add(string value)
                    {
                        throw new InvalidOperationException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.InvalidOperationException");

            Assert.All(
                run.RootSummary.CallEdges,
                edge =>
                    Assert.True(
                        edge.Suppresses(
                            exceptionType)));
        }

        /// <summary>
        /// Ensures that an uncalled lambda remains excluded.
        /// </summary>
        [Fact]
        public void CollectionInitializerInsideUncalledLambda_IsExcluded()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Action action =
                            () =>
                            {
                                Collection collection =
                                    new Collection
                                    {
                                        "value"
                                    };
                            };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(string value)
                    {
                        throw new InvalidOperationException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.CallEdges);
        }

        /// <summary>
        /// Ensures that a user-defined conversion supplies no unsafe facts.
        /// </summary>
        [Fact]
        public void UserDefinedArgumentConversion_DoesNotCreateUnsafeAddFacts()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            new Source()
                        };
                    }
                }

                public readonly struct Source
                {
                    public static implicit operator string?(Source value)
                    {
                        return null;
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .ConversionOperatorCall));

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall),
                "ArgumentNullException");
        }

        /// <summary>
        /// Ensures that Add call sites preserve source order.
        /// </summary>
        [Fact]
        public void MultipleElements_PreserveAddSourceOrder()
        {
            const string source =
                """
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Collection collection = new Collection
                        {
                            "first",
                            "second",
                            "third"
                        };
                    }
                }

                public sealed class Collection : IEnumerable
                {
                    public void Add(string value)
                    {
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return System.Array.Empty<object>().GetEnumerator();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] addEdges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.CollectionAddCall);

            Assert.Equal(
                3,
                addEdges.Length);

            int firstLine =
                Assert.IsType<int>(
                    addEdges[0].CallSiteStep.Line);

            int secondLine =
                Assert.IsType<int>(
                    addEdges[1].CallSiteStep.Line);

            int thirdLine =
                Assert.IsType<int>(
                    addEdges[2].CallSiteStep.Line);

            Assert.True(
                firstLine < secondLine);

            Assert.True(
                secondLine < thirdLine);
        }

        /// <summary>
        /// Gets all root edges of one kind.
        /// </summary>
        private static ExceptionFlowSummaryCallEdge[] GetEdges(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowPathStepKind kind)
        {
            return run.RootSummary.CallEdges
                .Where(
                    edge =>
                        edge.CallSiteStep.Kind ==
                        kind)
                .ToArray();
        }

        /// <summary>
        /// Gets the single root edge of one kind.
        /// </summary>
        private static ExceptionFlowSummaryCallEdge GetRequiredEdge(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowPathStepKind kind)
        {
            return Assert.Single(
                GetEdges(
                    run,
                    kind));
        }

        /// <summary>
        /// Checks the single local exception of an edge target.
        /// </summary>
        private static void AssertTargetException(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowSummaryCallEdge edge,
            string expectedExceptionName)
        {
            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    run.GetRequiredSummary(
                            edge.Target)
                        .Sources);

            Assert.Equal(
                expectedExceptionName,
                exceptionSource.ExceptionType.Name);
        }
    }
}
