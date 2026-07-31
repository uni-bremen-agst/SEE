using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests runtime-target expansion for compiler-selected synchronous and
    /// asynchronous foreach members.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphForEachDispatchTests
    {
        /// <summary>
        /// Ensures that virtual enumerator acquisition includes the base
        /// method and every compatible known override.
        /// </summary>
        [Fact]
        public void VirtualGetEnumerator_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(BaseSequence sequence)
                    {
                        foreach (int item in sequence)
                        {
                        }
                    }
                }

                public class BaseSequence
                {
                    public virtual Enumerator GetEnumerator()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class DerivedSequence : BaseSequence
                {
                    public override Enumerator GetEnumerator()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class Enumerator
                {
                    public bool MoveNext()
                    {
                        return false;
                    }

                    public int Current
                    {
                        get
                        {
                            return 0;
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .ForEachGetEnumeratorCall);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseSequence"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedSequence"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that direct collection creation fixes the runtime receiver
        /// to the created type.
        /// </summary>
        [Fact]
        public void ExactCollectionCreation_UsesCreatedGetEnumerator()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (
                            int item
                            in new DerivedSequence())
                        {
                        }
                    }
                }

                public class BaseSequence
                {
                    public virtual Enumerator GetEnumerator()
                    {
                        throw new ArgumentException();
                    }
                }

                public class DerivedSequence : BaseSequence
                {
                    public override Enumerator GetEnumerator()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedSequence :
                    DerivedSequence
                {
                    public override Enumerator GetEnumerator()
                    {
                        throw new FormatException();
                    }
                }

                public sealed class Enumerator
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

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind
                            .ForEachGetEnumeratorCall));

            Assert.Equal(
                "DerivedSequence",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that virtual enumerator advancement includes every known
        /// compatible implementation.
        /// </summary>
        [Fact]
        public void VirtualMoveNext_CreatesBaseAndOverrideEdges()
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
                    public BaseEnumerator GetEnumerator()
                    {
                        return new BaseEnumerator();
                    }
                }

                public class BaseEnumerator
                {
                    public virtual bool MoveNext()
                    {
                        throw new ArgumentException();
                    }

                    public virtual int Current => 0;
                }

                public sealed class DerivedEnumerator :
                    BaseEnumerator
                {
                    public override bool MoveNext()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .ForEachMoveNextCall);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseEnumerator"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedEnumerator"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a virtual Current getter includes every compatible
        /// known implementation.
        /// </summary>
        [Fact]
        public void VirtualCurrentGetter_CreatesBaseAndOverrideEdges()
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
                    public BaseEnumerator GetEnumerator()
                    {
                        return new BaseEnumerator();
                    }
                }

                public class BaseEnumerator
                {
                    public bool MoveNext()
                    {
                        return false;
                    }

                    public virtual int Current
                    {
                        get
                        {
                            throw new ArgumentException();
                        }
                    }
                }

                public sealed class DerivedEnumerator :
                    BaseEnumerator
                {
                    public override int Current
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

            ExceptionFlowSummaryCallEdge[] edges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .ForEachCurrentGetter);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseEnumerator"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedEnumerator"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that synchronous enumerator disposal follows the virtual
        /// implementation slot.
        /// </summary>
        [Fact]
        public void VirtualEnumeratorDispose_CreatesBaseAndOverrideEdges()
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
                    public BaseEnumerator GetEnumerator()
                    {
                        return new BaseEnumerator();
                    }
                }

                public class BaseEnumerator : IDisposable
                {
                    public bool MoveNext()
                    {
                        return false;
                    }

                    public int Current => 0;

                    public virtual void Dispose()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class DerivedEnumerator :
                    BaseEnumerator
                {
                    public override void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.DisposeCall);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseEnumerator"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedEnumerator"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that await-foreach expands acquisition, advancement,
        /// Current, and asynchronous disposal targets.
        /// </summary>
        [Fact]
        public void AwaitForEach_ExpandsAllEnumeratorMembers()
        {
            const string source =
                """
                using System.Threading;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(
                        BaseSequence sequence)
                    {
                        await foreach (int item in sequence)
                        {
                        }
                    }
                }

                public class BaseSequence
                {
                    public virtual BaseEnumerator
                        GetAsyncEnumerator(
                            CancellationToken token = default)
                    {
                        return new BaseEnumerator();
                    }
                }

                public sealed class DerivedSequence :
                    BaseSequence
                {
                    public override BaseEnumerator
                        GetAsyncEnumerator(
                            CancellationToken token = default)
                    {
                        return new DerivedEnumerator();
                    }
                }

                public class BaseEnumerator
                {
                    public virtual ValueTask<bool> MoveNextAsync()
                    {
                        return new ValueTask<bool>(false);
                    }

                    public virtual int Current => 0;

                    public virtual ValueTask DisposeAsync()
                    {
                        return ValueTask.CompletedTask;
                    }
                }

                public sealed class DerivedEnumerator :
                    BaseEnumerator
                {
                    public override ValueTask<bool> MoveNextAsync()
                    {
                        return new ValueTask<bool>(false);
                    }

                    public override int Current => 1;

                    public override ValueTask DisposeAsync()
                    {
                        return ValueTask.CompletedTask;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                2,
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .AsyncForEachGetEnumeratorCall)
                    .Length);

            Assert.Equal(
                2,
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .AsyncForEachMoveNextCall)
                    .Length);

            Assert.Equal(
                2,
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .AsyncForEachCurrentGetter)
                    .Length);

            Assert.Equal(
                2,
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.DisposeAsyncCall)
                    .Length);
        }

        /// <summary>
        /// Ensures that an interface enumerator acquisition selected from
        /// dependency metadata is mapped back to the source implementation.
        /// </summary>
        [Fact]
        public void CrossCompilationInterfaceForeach_UsesSourceImplementation()
        {
            const string dependencySource =
                """
                using System;

                namespace Dependency
                {
                    public interface ISequence
                    {
                        Enumerator GetEnumerator();
                    }

                    public sealed class Sequence : ISequence
                    {
                        public Enumerator GetEnumerator()
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    public sealed class Enumerator
                    {
                        public bool MoveNext()
                        {
                            return false;
                        }

                        public int Current => 0;
                    }
                }
                """;

            const string consumerSource =
                """
                using Dependency;

                public static class EntryPoint
                {
                    public static void M(ISequence sequence)
                    {
                        foreach (int item in sequence)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.Build(
                    dependencySource,
                    consumerSource,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind
                            .ForEachGetEnumeratorCall));

            IMethodSymbol targetMethod =
                GetTargetMethod(edge);

            Assert.Equal(
                "Sequence",
                targetMethod.ContainingType.Name);

            Assert.False(
                targetMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Gets root edges with the specified path-step kind.
        /// </summary>
        private static ExceptionFlowSummaryCallEdge[] GetEdges(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowPathStepKind stepKind)
        {
            return run.RootSummary.CallEdges
                .Where(
                    edge =>
                        edge.CallSiteStep.Kind ==
                        stepKind)
                .ToArray();
        }

        /// <summary>
        /// Gets the uniquely matching edge declared by one containing type.
        /// </summary>
        private static ExceptionFlowSummaryCallEdge
            GetEdgeForContainingType(
                IEnumerable<ExceptionFlowSummaryCallEdge> edges,
                string containingTypeName)
        {
            return Assert.Single(
                edges.Where(
                    edge =>
                        string.Equals(
                            GetTargetMethod(edge)
                                .ContainingType.Name,
                            containingTypeName,
                            StringComparison.Ordinal)));
        }

        /// <summary>
        /// Gets the target method represented by an edge.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the edge target is not a method.
        /// </exception>
        private static IMethodSymbol GetTargetMethod(
            ExceptionFlowSummaryCallEdge edge)
        {
            return edge.Target.Symbol as IMethodSymbol ??
                   throw new InvalidOperationException(
                       "The foreach edge target was not a method.");
        }

        /// <summary>
        /// Ensures that one target summary contains exactly one expected
        /// exception source.
        /// </summary>
        private static void AssertTargetException(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowSummaryCallEdge edge,
            string exceptionTypeName)
        {
            ExceptionFlowSummary summary =
                run.GetRequiredSummary(
                    edge.Target);

            ExceptionFlowSummarySource source =
                Assert.Single(
                    summary.Sources);

            Assert.Equal(
                exceptionTypeName,
                source.ExceptionType.Name);
        }
    }
}
