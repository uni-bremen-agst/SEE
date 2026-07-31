using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests runtime-target expansion for explicit and implicit awaiter
    /// pattern members.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphAwaitDispatchTests
    {
        /// <summary>
        /// Ensures that virtual GetAwaiter calls include every compatible
        /// known runtime implementation.
        /// </summary>
        [Fact]
        public void VirtualGetAwaiter_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(
                        BaseAwaitable value)
                    {
                        await value;
                    }
                }

                public class BaseAwaitable
                {
                    public virtual Awaiter GetAwaiter()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class DerivedAwaitable :
                    BaseAwaitable
                {
                    public override Awaiter GetAwaiter()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class Awaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public void GetResult()
                    {
                    }

                    public void OnCompleted(
                        Action continuation)
                    {
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
                        .AwaitGetAwaiterCall);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseAwaitable"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedAwaitable"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that awaiting a direct object creation uses only the
        /// exactly created runtime type.
        /// </summary>
        [Fact]
        public void ExactAwaitableCreation_UsesCreatedType()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M()
                    {
                        await new DerivedAwaitable();
                    }
                }

                public class BaseAwaitable
                {
                    public virtual Awaiter GetAwaiter()
                    {
                        throw new ArgumentException();
                    }
                }

                public class DerivedAwaitable :
                    BaseAwaitable
                {
                    public override Awaiter GetAwaiter()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedAwaitable :
                    DerivedAwaitable
                {
                    public override Awaiter GetAwaiter()
                    {
                        throw new FormatException();
                    }
                }

                public sealed class Awaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public void GetResult()
                    {
                    }

                    public void OnCompleted(
                        Action continuation)
                    {
                    }
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
                            .AwaitGetAwaiterCall));

            Assert.Equal(
                "DerivedAwaitable",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a virtual IsCompleted getter includes every compatible
        /// known runtime implementation.
        /// </summary>
        [Fact]
        public void VirtualIsCompletedGetter_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(
                        Awaitable value)
                    {
                        await value;
                    }
                }

                public sealed class Awaitable
                {
                    public BaseAwaiter GetAwaiter()
                    {
                        return new BaseAwaiter();
                    }
                }

                public class BaseAwaiter : INotifyCompletion
                {
                    public virtual bool IsCompleted
                    {
                        get
                        {
                            throw new ArgumentException();
                        }
                    }

                    public virtual void GetResult()
                    {
                    }

                    public void OnCompleted(
                        Action continuation)
                    {
                    }
                }

                public sealed class DerivedAwaiter :
                    BaseAwaiter
                {
                    public override bool IsCompleted
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
                        .AwaitIsCompletedGetter);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseAwaiter"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedAwaiter"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that virtual GetResult calls include every compatible known
        /// runtime implementation.
        /// </summary>
        [Fact]
        public void VirtualGetResult_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(
                        Awaitable value)
                    {
                        await value;
                    }
                }

                public sealed class Awaitable
                {
                    public BaseAwaiter GetAwaiter()
                    {
                        return new BaseAwaiter();
                    }
                }

                public class BaseAwaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public virtual void GetResult()
                    {
                        throw new ArgumentException();
                    }

                    public void OnCompleted(
                        Action continuation)
                    {
                    }
                }

                public sealed class DerivedAwaiter :
                    BaseAwaiter
                {
                    public override void GetResult()
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
                        .AwaitGetResultCall);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseAwaiter"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedAwaiter"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that extension GetAwaiter remains statically bound and
        /// retains receiver facts.
        /// </summary>
        [Fact]
        public void ExtensionGetAwaiter_RemainsDirectlyBound()
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
                        this Awaitable? value)
                    {
                        ArgumentNullException.ThrowIfNull(
                            value);

                        return new Awaiter();
                    }
                }

                public sealed class Awaiter : INotifyCompletion
                {
                    public bool IsCompleted => true;

                    public void GetResult()
                    {
                    }

                    public void OnCompleted(
                        Action continuation)
                    {
                    }
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
                            .AwaitGetAwaiterCall));

            Assert.Equal(
                "AwaitableExtensions",
                GetTargetMethod(edge).ContainingType.Name);

            Assert.Empty(
                run.GetRequiredSummary(
                        edge.Target)
                    .Sources);
        }

        /// <summary>
        /// Ensures that await-using expands the awaiter members used to consume
        /// the result of DisposeAsync.
        /// </summary>
        [Fact]
        public void AwaitUsing_ExpandsAwaiterMembers()
        {
            const string source =
                """
                using System;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(
                        Resource resource)
                    {
                        await using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource
                {
                    public BaseAwaitable DisposeAsync()
                    {
                        return new BaseAwaitable();
                    }
                }

                public class BaseAwaitable
                {
                    public virtual BaseAwaiter GetAwaiter()
                    {
                        return new BaseAwaiter();
                    }
                }

                public sealed class DerivedAwaitable :
                    BaseAwaitable
                {
                    public override BaseAwaiter GetAwaiter()
                    {
                        return new DerivedAwaiter();
                    }
                }

                public class BaseAwaiter : INotifyCompletion
                {
                    public virtual bool IsCompleted => true;

                    public virtual void GetResult()
                    {
                    }

                    public void OnCompleted(
                        Action continuation)
                    {
                    }
                }

                public sealed class DerivedAwaiter :
                    BaseAwaiter
                {
                    public override bool IsCompleted => true;

                    public override void GetResult()
                    {
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
                        .AwaitGetAwaiterCall)
                    .Length);

            Assert.Equal(
                2,
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .AwaitIsCompletedGetter)
                    .Length);

            Assert.Equal(
                2,
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind
                        .AwaitGetResultCall)
                    .Length);
        }

        /// <summary>
        /// Ensures that an interface awaiter acquisition selected from
        /// dependency metadata is mapped back to its source implementation.
        /// </summary>
        [Fact]
        public void CrossCompilationInterfaceAwait_UsesSourceImplementation()
        {
            const string dependencySource =
                """
                using System;
                using System.Runtime.CompilerServices;

                namespace Dependency
                {
                    public interface IAwaitable
                    {
                        Awaiter GetAwaiter();
                    }

                    public sealed class Awaitable : IAwaitable
                    {
                        public Awaiter GetAwaiter()
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    public sealed class Awaiter : INotifyCompletion
                    {
                        public bool IsCompleted => true;

                        public void GetResult()
                        {
                        }

                        public void OnCompleted(
                            Action continuation)
                        {
                        }
                    }
                }
                """;

            const string consumerSource =
                """
                using System.Threading.Tasks;
                using Dependency;

                public static class EntryPoint
                {
                    public static async Task M(
                        IAwaitable value)
                    {
                        await value;
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
                            .AwaitGetAwaiterCall));

            IMethodSymbol targetMethod =
                GetTargetMethod(edge);

            Assert.Equal(
                "Awaitable",
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
                       "The awaiter edge target was not a method.");
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
