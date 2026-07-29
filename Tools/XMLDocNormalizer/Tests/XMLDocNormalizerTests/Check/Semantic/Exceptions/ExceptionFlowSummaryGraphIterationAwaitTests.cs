using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests implicit enumeration and awaiter calls represented in
    /// exception-flow summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphIterationAwaitTests
    {
        /// <summary>
        /// Ensures that pattern-based synchronous enumeration records
        /// acquisition, advancement, current access, and disposal.
        /// </summary>
        [Fact]
        public void PatternForeach_CreatesCompleteEnumeratorCallChain()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Sequence sequence)
                    {
                        foreach (int item in sequence)
                        {
                        }
                    }
                }

                public sealed class Sequence
                {
                    public Enumerator GetEnumerator()
                    {
                        throw new ArgumentException();
                    }
                }

                public ref struct Enumerator
                {
                    public bool MoveNext()
                    {
                        throw new InvalidOperationException();
                    }

                    public int Current
                    {
                        get
                        {
                            throw new NotSupportedException();
                        }
                    }

                    public void Dispose()
                    {
                        throw new ApplicationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                4,
                edges.Length);

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.ForEachGetEnumeratorCall),
                "ArgumentException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.ForEachMoveNextCall),
                "InvalidOperationException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.ForEachCurrentGetter),
                "NotSupportedException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.DisposeCall),
                "ApplicationException");
        }

        /// <summary>
        /// Ensures that an enumerator without disposal semantics does not
        /// create a disposal edge.
        /// </summary>
        [Fact]
        public void EnumeratorWithoutDisposal_OmitsDisposeEdge()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(Sequence sequence)
                    {
                        foreach (int item in sequence)
                        {
                        }
                    }
                }

                public sealed class Sequence
                {
                    public Enumerator GetEnumerator()
                    {
                        return new Enumerator();
                    }
                }

                public ref struct Enumerator
                {
                    public bool MoveNext()
                    {
                        return false;
                    }

                    public int Current => 0;
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                3,
                edges.Length);

            Assert.DoesNotContain(
                edges,
                edge =>
                    edge.CallSiteStep.Kind ==
                    ExceptionFlowPathStepKind.DisposeCall);
        }

        /// <summary>
        /// Ensures that interface-typed synchronous enumeration retains the
        /// statically selected interface targets for later dispatch
        /// expansion.
        /// </summary>
        [Fact]
        public void InterfaceTypedForeach_RetainsInterfaceTargets()
        {
            const string source =
                """
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(IEnumerable<int> sequence)
                    {
                        foreach (int item in sequence)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                "IEnumerable",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind.ForEachGetEnumeratorCall)
                    .Target
                    .Symbol
                    .ContainingType?
                    .Name);

            Assert.Equal(
                "IEnumerator",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind.ForEachMoveNextCall)
                    .Target
                    .Symbol
                    .ContainingType?
                    .Name);

            Assert.Equal(
                "IEnumerator",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind.ForEachCurrentGetter)
                    .Target
                    .Symbol
                    .ContainingType?
                    .Name);

            Assert.Equal(
                "IDisposable",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind.DisposeCall)
                    .Target
                    .Symbol
                    .ContainingType?
                    .Name);
        }

        /// <summary>
        /// Ensures that array iteration does not invent callable targets for
        /// compiler-provided index-based lowering.
        /// </summary>
        [Fact]
        public void ArrayForeach_DoesNotCreateEnumeratorEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(int[] values)
                    {
                        foreach (int value in values)
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
                GetIterationAndAwaitEdges(run));
        }

        /// <summary>
        /// Ensures that string iteration does not invent callable targets for
        /// compiler-provided index-based lowering.
        /// </summary>
        [Fact]
        public void StringForeach_DoesNotCreateEnumeratorEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(string value)
                    {
                        foreach (char character in value)
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
                GetIterationAndAwaitEdges(run));
        }

        /// <summary>
        /// Ensures that built-in array iteration still records a
        /// user-defined conversion to the iteration-variable type.
        /// </summary>
        [Fact]
        public void ArrayForeachElementConversion_CreatesConversionEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Source[] values)
                    {
                        foreach (Target item in values)
                        {
                        }
                    }
                }

                public readonly struct Source
                {
                }

                public readonly struct Target
                {
                    public static implicit operator Target(Source value)
                    {
                        throw new FormatException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Single(
                edges);

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.ConversionOperatorCall),
                "FormatException");
        }

        /// <summary>
        /// Ensures that a user-defined conversion to the iteration-variable
        /// type creates a conversion edge.
        /// </summary>
        [Fact]
        public void ForeachElementConversion_CreatesConversionEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Sequence sequence)
                    {
                        foreach (Target item in sequence)
                        {
                        }
                    }
                }

                public readonly struct Source
                {
                }

                public readonly struct Target
                {
                    public static implicit operator Target(Source value)
                    {
                        throw new FormatException();
                    }
                }

                public sealed class Sequence
                {
                    public Enumerator GetEnumerator()
                    {
                        return new Enumerator();
                    }
                }

                public ref struct Enumerator
                {
                    public bool MoveNext()
                    {
                        return false;
                    }

                    public Source Current => new Source();
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge conversionEdge =
                GetRequiredEdge(
                    GetIterationAndAwaitEdges(run),
                    ExceptionFlowPathStepKind.ConversionOperatorCall);

            AssertTargetException(
                run,
                conversionEdge,
                "FormatException");
        }

        /// <summary>
        /// Ensures that a catch surrounding the complete foreach statement
        /// filters exceptions from all implicit enumeration calls.
        /// </summary>
        [Fact]
        public void CatchAroundForeach_SuppressesEnumeratorEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Sequence sequence)
                    {
                        try
                        {
                            foreach (int item in sequence)
                            {
                            }
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }

                public sealed class Sequence
                {
                    public Enumerator GetEnumerator()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public ref struct Enumerator
                {
                    public bool MoveNext()
                    {
                        throw new InvalidOperationException();
                    }

                    public int Current
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    public void Dispose()
                    {
                        throw new InvalidOperationException();
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
                GetIterationAndAwaitEdges(run),
                edge =>
                    Assert.True(
                        edge.Suppresses(
                            exceptionType)));
        }

        /// <summary>
        /// Ensures that a catch inside the loop body does not filter
        /// exceptions from implicit enumeration calls outside that body.
        /// </summary>
        [Fact]
        public void CatchInsideForeachBody_DoesNotSuppressEnumeratorEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Sequence sequence)
                    {
                        foreach (int item in sequence)
                        {
                            try
                            {
                                throw new InvalidOperationException();
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    }
                }

                public sealed class Sequence
                {
                    public Enumerator GetEnumerator()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public ref struct Enumerator
                {
                    public bool MoveNext()
                    {
                        throw new InvalidOperationException();
                    }

                    public int Current
                    {
                        get
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

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.InvalidOperationException");

            Assert.All(
                GetIterationAndAwaitEdges(run),
                edge =>
                    Assert.False(
                        edge.Suppresses(
                            exceptionType)));

            Assert.Empty(
                run.RootSummary.Sources);
        }

        /// <summary>
        /// Ensures that a foreach statement inside an uncalled lambda remains
        /// outside the containing method summary.
        /// </summary>
        [Fact]
        public void ForeachInsideUncalledLambda_IsExcluded()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Sequence sequence)
                    {
                        Action action =
                            () =>
                            {
                                foreach (int item in sequence)
                                {
                                }
                            };
                    }
                }

                public sealed class Sequence
                {
                    public Enumerator GetEnumerator()
                    {
                        return new Enumerator();
                    }
                }

                public ref struct Enumerator
                {
                    public bool MoveNext()
                    {
                        return false;
                    }

                    public int Current => 0;
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetIterationAndAwaitEdges(run));
        }

        /// <summary>
        /// Ensures that receiver facts are forwarded to a reduced extension
        /// <c>GetEnumerator</c> target.
        /// </summary>
        [Fact]
        public void ExtensionGetEnumerator_MapsReceiverFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (int item in new Sequence())
                        {
                        }
                    }
                }

                public sealed class Sequence
                {
                }

                public static class SequenceExtensions
                {
                    public static Enumerator GetEnumerator(
                        this Sequence? sequence)
                    {
                        ArgumentNullException.ThrowIfNull(sequence);
                        return new Enumerator();
                    }
                }

                public ref struct Enumerator
                {
                    public bool MoveNext()
                    {
                        return false;
                    }

                    public int Current => 0;
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge getEnumeratorEdge =
                GetRequiredEdge(
                    GetIterationAndAwaitEdges(run),
                    ExceptionFlowPathStepKind.ForEachGetEnumeratorCall);

            Assert.Equal(
                "SequenceExtensions",
                getEnumeratorEdge.Target.Symbol.ContainingType?.Name);

            Assert.Empty(
                run.GetRequiredSummary(
                        getEnumeratorEdge.Target)
                    .Sources);
        }

        /// <summary>
        /// Ensures that await foreach records asynchronous enumeration,
        /// current access, both awaiter chains, and asynchronous disposal.
        /// </summary>
        [Fact]
        public void PatternAwaitForeach_CreatesCompleteAsyncCallChain()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(AsyncSequence sequence)
                    {
                        await foreach (int item in sequence)
                        {
                        }
                    }
                }

                public sealed class AsyncSequence
                {
                    public AsyncEnumerator GetAsyncEnumerator()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class AsyncEnumerator
                {
                    public MoveNextAwaitable MoveNextAsync()
                    {
                        throw new InvalidOperationException();
                    }

                    public int Current
                    {
                        get
                        {
                            throw new NotSupportedException();
                        }
                    }

                    public DisposeAwaitable DisposeAsync()
                    {
                        throw new ApplicationException();
                    }
                }

                public sealed class MoveNextAwaitable
                {
                    public MoveNextAwaiter GetAwaiter()
                    {
                        throw new FormatException();
                    }
                }

                public sealed class MoveNextAwaiter : INotifyCompletion
                {
                    public bool IsCompleted
                    {
                        get
                        {
                            throw new OverflowException();
                        }
                    }

                    public bool GetResult()
                    {
                        throw new DivideByZeroException();
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }

                public sealed class DisposeAwaitable
                {
                    public DisposeAwaiter GetAwaiter()
                    {
                        throw new TimeoutException();
                    }
                }

                public sealed class DisposeAwaiter : INotifyCompletion
                {
                    public bool IsCompleted
                    {
                        get
                        {
                            throw new ArithmeticException();
                        }
                    }

                    public void GetResult()
                    {
                        throw new RankException();
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                10,
                edges.Length);

            Assert.Equal(
                1,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind
                        .AsyncForEachGetEnumeratorCall));

            Assert.Equal(
                1,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind
                        .AsyncForEachMoveNextCall));

            Assert.Equal(
                1,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind
                        .AsyncForEachCurrentGetter));

            Assert.Equal(
                1,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.DisposeAsyncCall));

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetAwaiterCall));

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitIsCompletedGetter));

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetResultCall));

            Assert.All(
                edges,
                edge =>
                    Assert.Single(
                        run.GetRequiredSummary(
                                edge.Target)
                            .Sources));
        }

        /// <summary>
        /// Ensures that the implicit await of MoveNextAsync resolves an
        /// extension GetAwaiter method through speculative Roslyn binding.
        /// </summary>
        [Fact]
        public void AwaitForeachMoveNextExtensionGetAwaiter_IsResolved()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(AsyncSequence sequence)
                    {
                        await foreach (int item in sequence)
                        {
                        }
                    }
                }

                public sealed class AsyncSequence
                {
                    public AsyncEnumerator GetAsyncEnumerator()
                    {
                        return new AsyncEnumerator();
                    }
                }

                public sealed class AsyncEnumerator
                {
                    public MoveNextAwaitable MoveNextAsync()
                    {
                        return new MoveNextAwaitable();
                    }

                    public int Current => 0;

                    public ValueTask DisposeAsync()
                    {
                        return ValueTask.CompletedTask;
                    }
                }

                public sealed class MoveNextAwaitable
                {
                }

                public static class MoveNextAwaitableExtensions
                {
                    public static MoveNextAwaiter GetAwaiter(
                        this MoveNextAwaitable? awaitable)
                    {
                        ArgumentNullException.ThrowIfNull(awaitable);
                        return new MoveNextAwaiter();
                    }
                }

                public sealed class MoveNextAwaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public bool GetResult()
                    {
                        return false;
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge getAwaiterEdge =
                Assert.Single(
                    GetIterationAndAwaitEdges(run)
                        .Where(
                            edge =>
                                edge.CallSiteStep.Kind ==
                                    ExceptionFlowPathStepKind
                                        .AwaitGetAwaiterCall &&
                                edge.Target.Symbol.ContainingType?.Name ==
                                    "MoveNextAwaitableExtensions"));

            Assert.Equal(
                "MoveNextAwaitableExtensions",
                getAwaiterEdge.Target.Symbol.ContainingType?.Name);

            AssertTargetException(
                run,
                getAwaiterEdge,
                "ArgumentNullException");
        }

        /// <summary>
        /// Ensures that interface-typed asynchronous enumeration retains its
        /// statically selected interface targets.
        /// </summary>
        [Fact]
        public void InterfaceTypedAwaitForeach_RetainsInterfaceTargets()
        {
            const string source =
                """
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(
                        IAsyncEnumerable<int> sequence)
                    {
                        await foreach (int item in sequence)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                "IAsyncEnumerable",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind
                            .AsyncForEachGetEnumeratorCall)
                    .Target
                    .Symbol
                    .ContainingType?
                    .Name);

            Assert.Equal(
                "IAsyncEnumerator",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind
                            .AsyncForEachMoveNextCall)
                    .Target
                    .Symbol
                    .ContainingType?
                    .Name);

            Assert.Equal(
                "IAsyncEnumerator",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind
                            .AsyncForEachCurrentGetter)
                    .Target
                    .Symbol
                    .ContainingType?
                    .Name);

            Assert.Equal(
                "IAsyncDisposable",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind.DisposeAsyncCall)
                    .Target
                    .Symbol
                    .ContainingType?
                    .Name);

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetAwaiterCall));

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitIsCompletedGetter));

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetResultCall));
        }

        /// <summary>
        /// Ensures that a catch around await foreach filters asynchronous
        /// enumerator, awaiter, current, and disposal edges.
        /// </summary>
        [Fact]
        public void CatchAroundAwaitForeach_SuppressesAsyncEdges()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(AsyncSequence sequence)
                    {
                        try
                        {
                            await foreach (int item in sequence)
                            {
                            }
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }

                public sealed class AsyncSequence
                {
                    public AsyncEnumerator GetAsyncEnumerator()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class AsyncEnumerator
                {
                    public Awaitable MoveNextAsync()
                    {
                        throw new InvalidOperationException();
                    }

                    public int Current
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    public Awaitable DisposeAsync()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class Awaitable
                {
                    public Awaiter GetAwaiter()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class Awaiter : INotifyCompletion
                {
                    public bool IsCompleted
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    public bool GetResult()
                    {
                        throw new InvalidOperationException();
                    }

                    public void OnCompleted(Action continuation)
                    {
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
                GetIterationAndAwaitEdges(run),
                edge =>
                    Assert.True(
                        edge.Suppresses(
                            exceptionType)));
        }

        /// <summary>
        /// Ensures that await foreach inside an uncalled lambda remains
        /// outside the containing method summary.
        /// </summary>
        [Fact]
        public void AwaitForeachInsideUncalledLambda_IsExcluded()
        {
            const string source =
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static void M(
                        IAsyncEnumerable<int> sequence)
                    {
                        Func<Task> action =
                            async () =>
                            {
                                await foreach (int item in sequence)
                                {
                                }
                            };
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetIterationAndAwaitEdges(run));
        }

        /// <summary>
        /// Ensures that an explicit custom await records GetAwaiter,
        /// IsCompleted, and GetResult.
        /// </summary>
        [Fact]
        public void CustomAwaitable_CreatesAwaiterCallChain()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Awaitable awaitable)
                    {
                        await awaitable;
                    }
                }

                public sealed class Awaitable
                {
                    public Awaiter GetAwaiter()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class Awaiter : INotifyCompletion
                {
                    public bool IsCompleted
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    public void GetResult()
                    {
                        throw new NotSupportedException();
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                3,
                edges.Length);

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetAwaiterCall),
                "ArgumentException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.AwaitIsCompletedGetter),
                "InvalidOperationException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetResultCall),
                "NotSupportedException");
        }

        /// <summary>
        /// Ensures that receiver facts are forwarded to a reduced extension
        /// <c>GetAwaiter</c> target.
        /// </summary>
        [Fact]
        public void ExtensionGetAwaiter_MapsReceiverFacts()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M()
                    {
                        await new Awaitable();
                    }
                }

                public sealed class Awaitable
                {
                }

                public static class AwaitableExtensions
                {
                    public static Awaiter GetAwaiter(
                        this Awaitable? awaitable)
                    {
                        ArgumentNullException.ThrowIfNull(awaitable);
                        return new Awaiter();
                    }
                }

                public sealed class Awaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public void GetResult()
                    {
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge getAwaiterEdge =
                GetRequiredEdge(
                    GetIterationAndAwaitEdges(run),
                    ExceptionFlowPathStepKind.AwaitGetAwaiterCall);

            Assert.Equal(
                "AwaitableExtensions",
                getAwaiterEdge.Target.Symbol.ContainingType?.Name);

            Assert.Empty(
                run.GetRequiredSummary(
                        getAwaiterEdge.Target)
                    .Sources);
        }

        /// <summary>
        /// Ensures that awaiting a framework task records its framework
        /// awaiter members without inventing source-level bodies.
        /// </summary>
        [Fact]
        public void TaskAwait_CreatesFrameworkAwaiterEdges()
        {
            const string source =
                """
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Task task)
                    {
                        await task;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                3,
                edges.Length);

            Assert.Equal(
                "GetAwaiter",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind.AwaitGetAwaiterCall)
                    .Target
                    .Symbol
                    .Name);

            Assert.Equal(
                "get_IsCompleted",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind.AwaitIsCompletedGetter)
                    .Target
                    .Symbol
                    .Name);

            Assert.Equal(
                "GetResult",
                GetRequiredEdge(
                        edges,
                        ExceptionFlowPathStepKind.AwaitGetResultCall)
                    .Target
                    .Symbol
                    .Name);
        }

        /// <summary>
        /// Ensures that a catch around an explicit await filters all
        /// compiler-selected awaiter calls.
        /// </summary>
        [Fact]
        public void CatchAroundAwait_SuppressesAwaiterEdges()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Awaitable awaitable)
                    {
                        try
                        {
                            await awaitable;
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }

                public sealed class Awaitable
                {
                    public Awaiter GetAwaiter()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class Awaiter : INotifyCompletion
                {
                    public bool IsCompleted
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    public void GetResult()
                    {
                        throw new InvalidOperationException();
                    }

                    public void OnCompleted(Action continuation)
                    {
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
                GetIterationAndAwaitEdges(run),
                edge =>
                    Assert.True(
                        edge.Suppresses(
                            exceptionType)));
        }

        /// <summary>
        /// Ensures that an await expression inside an uncalled lambda remains
        /// outside the containing method summary.
        /// </summary>
        [Fact]
        public void AwaitInsideUncalledLambda_IsExcluded()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static void M(Task task)
                    {
                        Func<Task> action =
                            async () =>
                            {
                                await task;
                            };
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetIterationAndAwaitEdges(run));
        }

        /// <summary>
        /// Ensures that await using records both DisposeAsync and the awaiter
        /// chain used to consume its result.
        /// </summary>
        [Fact]
        public void AwaitUsingStatement_CreatesDisposalAwaiterCallChain()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Resource resource)
                    {
                        await using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource
                {
                    public DisposeAwaitable DisposeAsync()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class DisposeAwaitable
                {
                    public DisposeAwaiter GetAwaiter()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class DisposeAwaiter : INotifyCompletion
                {
                    public bool IsCompleted
                    {
                        get
                        {
                            throw new NotSupportedException();
                        }
                    }

                    public void GetResult()
                    {
                        throw new ApplicationException();
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                4,
                edges.Length);

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.DisposeAsyncCall),
                "ArgumentException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetAwaiterCall),
                "InvalidOperationException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.AwaitIsCompletedGetter),
                "NotSupportedException");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetResultCall),
                "ApplicationException");
        }

        /// <summary>
        /// Ensures that separate await-using resources each contribute one
        /// disposal edge and one awaiter chain.
        /// </summary>
        [Fact]
        public void MultipleAwaitUsingResources_CreateSeparateAwaiterChains()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(
                        Resource first,
                        Resource second)
                    {
                        await using Resource firstAlias = first,
                                             secondAlias = second;
                    }
                }

                public sealed class Resource
                {
                    public DisposeAwaitable DisposeAsync()
                    {
                        return new DisposeAwaitable();
                    }
                }

                public sealed class DisposeAwaitable
                {
                    public DisposeAwaiter GetAwaiter()
                    {
                        return new DisposeAwaiter();
                    }
                }

                public sealed class DisposeAwaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public void GetResult()
                    {
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                8,
                edges.Length);

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.DisposeAsyncCall));

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetAwaiterCall));

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitIsCompletedGetter));

            Assert.Equal(
                2,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetResultCall));
        }

        /// <summary>
        /// Ensures that a resource proven null contributes neither a
        /// DisposeAsync edge nor an awaiter chain.
        /// </summary>
        [Fact]
        public void KnownNullAwaitUsingResource_CreatesNoImplicitEdges()
        {
            const string source =
                """
                #nullable enable
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M()
                    {
                        Resource? resource = null;

                        await using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource
                {
                    public DisposeAwaitable DisposeAsync()
                    {
                        return new DisposeAwaitable();
                    }
                }

                public sealed class DisposeAwaitable
                {
                    public DisposeAwaiter GetAwaiter()
                    {
                        return new DisposeAwaiter();
                    }
                }

                public sealed class DisposeAwaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public void GetResult()
                    {
                    }

                    public void OnCompleted(System.Action continuation)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetIterationAndAwaitEdges(run));
        }

        /// <summary>
        /// Ensures that an optional cancellation-token parameter selected by
        /// await foreach remains part of the GetAsyncEnumerator target.
        /// </summary>
        [Fact]
        public void AwaitForeachOptionalCancellationToken_UsesSelectedTarget()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(AsyncSequence sequence)
                    {
                        await foreach (int item in sequence)
                        {
                        }
                    }
                }

                public sealed class AsyncSequence
                {
                    public AsyncEnumerator GetAsyncEnumerator(
                        CancellationToken cancellationToken = default)
                    {
                        return new AsyncEnumerator();
                    }
                }

                public sealed class AsyncEnumerator
                {
                    public MoveNextAwaitable MoveNextAsync()
                    {
                        return new MoveNextAwaitable();
                    }

                    public int Current => 0;

                    public DisposeAwaitable DisposeAsync()
                    {
                        return new DisposeAwaitable();
                    }
                }

                public sealed class MoveNextAwaitable
                {
                    public MoveNextAwaiter GetAwaiter()
                    {
                        return new MoveNextAwaiter();
                    }
                }

                public sealed class MoveNextAwaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public bool GetResult()
                    {
                        return false;
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }

                public sealed class DisposeAwaitable
                {
                    public DisposeAwaiter GetAwaiter()
                    {
                        return new DisposeAwaiter();
                    }
                }

                public sealed class DisposeAwaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public void GetResult()
                    {
                    }

                    public void OnCompleted(Action continuation)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge getEnumeratorEdge =
                GetRequiredEdge(
                    GetIterationAndAwaitEdges(run),
                    ExceptionFlowPathStepKind
                        .AsyncForEachGetEnumeratorCall);

            IMethodSymbol getEnumeratorMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    getEnumeratorEdge.Target.Symbol);

            IParameterSymbol cancellationTokenParameter =
                Assert.Single(
                    getEnumeratorMethod.Parameters);

            Assert.True(
                cancellationTokenParameter.HasExplicitDefaultValue);

            Assert.Equal(
                "CancellationToken",
                cancellationTokenParameter.Type.Name);
        }

        /// <summary>
        /// Counts edges of one path-step kind.
        /// </summary>
        /// <param name="edges">
        /// The edges to inspect.
        /// </param>
        /// <param name="kind">
        /// The required path-step kind.
        /// </param>
        /// <returns>The number of matching edges.</returns>
        private static int CountEdges(
            IEnumerable<ExceptionFlowSummaryCallEdge> edges,
            ExceptionFlowPathStepKind kind)
        {
            return edges.Count(
                edge =>
                    edge.CallSiteStep.Kind ==
                    kind);
        }

        /// <summary>
        /// Gets all enumeration, awaiter, conversion, and disposal edges from
        /// the root summary.
        /// </summary>
        /// <param name="run">
        /// The completed summary-graph test run.
        /// </param>
        /// <returns>The relevant root edges in insertion order.</returns>
        private static ExceptionFlowSummaryCallEdge[]
            GetIterationAndAwaitEdges(
                ExceptionFlowSummaryGraphTestRun run)
        {
            return run.RootSummary.CallEdges
                .Where(
                    edge =>
                        IsIterationOrAwaitStep(
                            edge.CallSiteStep.Kind))
                .ToArray();
        }

        /// <summary>
        /// Determines whether a path-step kind belongs to this package.
        /// </summary>
        /// <param name="kind">
        /// The path-step kind to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for enumeration, awaiter, conversion, and
        /// disposal steps; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsIterationOrAwaitStep(
            ExceptionFlowPathStepKind kind)
        {
            return kind ==
                       ExceptionFlowPathStepKind
                           .ForEachGetEnumeratorCall ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .ForEachMoveNextCall ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .ForEachCurrentGetter ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .AsyncForEachGetEnumeratorCall ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .AsyncForEachMoveNextCall ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .AsyncForEachCurrentGetter ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .AwaitGetAwaiterCall ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .AwaitIsCompletedGetter ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .AwaitGetResultCall ||
                   kind ==
                       ExceptionFlowPathStepKind.RuntimeAwaitCall ||
                   kind ==
                       ExceptionFlowPathStepKind.DisposeCall ||
                   kind ==
                       ExceptionFlowPathStepKind.DisposeAsyncCall ||
                   kind ==
                       ExceptionFlowPathStepKind
                           .ConversionOperatorCall;
        }

        /// <summary>
        /// Gets the single edge of one required path-step kind.
        /// </summary>
        /// <param name="edges">
        /// The edges to inspect.
        /// </param>
        /// <param name="kind">
        /// The required path-step kind.
        /// </param>
        /// <returns>The single matching edge.</returns>
        private static ExceptionFlowSummaryCallEdge GetRequiredEdge(
            IEnumerable<ExceptionFlowSummaryCallEdge> edges,
            ExceptionFlowPathStepKind kind)
        {
            return Assert.Single(
                edges.Where(
                    edge =>
                        edge.CallSiteStep.Kind ==
                        kind));
        }

        /// <summary>
        /// Ensures that one edge target has exactly one local source of the
        /// expected exception type.
        /// </summary>
        /// <param name="run">
        /// The completed summary-graph test run.
        /// </param>
        /// <param name="edge">
        /// The edge whose target summary should be inspected.
        /// </param>
        /// <param name="expectedExceptionName">
        /// The expected simple exception type name.
        /// </param>
        private static void AssertTargetException(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowSummaryCallEdge edge,
            string expectedExceptionName)
        {
            ExceptionFlowSummarySource source =
                Assert.Single(
                    run.GetRequiredSummary(
                            edge.Target)
                        .Sources);

            Assert.Equal(
                expectedExceptionName,
                source.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a later assignment invalidates a null fact obtained from
        /// a resource local's initializer.
        /// </summary>
        [Fact]
        public void ReassignedAwaitUsingResource_StillCreatesImplicitEdges()
        {
            const string source =
                """
        #nullable enable
        using System;
        using System.Runtime.CompilerServices;
        using System.Threading.Tasks;

        public static class EntryPoint
        {
            public static async Task M()
            {
                Resource? resource = null;
                resource = new Resource();

                await using (resource)
                {
                }
            }
        }

        public sealed class Resource
        {
            public DisposeAwaitable DisposeAsync()
            {
                return new DisposeAwaitable();
            }
        }

        public sealed class DisposeAwaitable
        {
            public DisposeAwaiter GetAwaiter()
            {
                return new DisposeAwaiter();
            }
        }

        public sealed class DisposeAwaiter : INotifyCompletion
        {
            public bool IsCompleted => true;

            public void GetResult()
            {
            }

            public void OnCompleted(Action continuation)
            {
            }
        }
        """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetIterationAndAwaitEdges(run);

            Assert.Equal(
                4,
                edges.Length);

            Assert.Equal(
                1,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.DisposeAsyncCall));

            Assert.Equal(
                1,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetAwaiterCall));

            Assert.Equal(
                1,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitIsCompletedGetter));

            Assert.Equal(
                1,
                CountEdges(
                    edges,
                    ExceptionFlowPathStepKind.AwaitGetResultCall));
        }
    }
}
