using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests completeness and uncertainty for open runtime dispatch target
    /// sets.
    /// </summary>
    public sealed class
        ExceptionFlowSummaryGraphDispatchCompletenessTests
    {
        /// <summary>
        /// Ensures that known implementations of a public interface are
        /// retained together with uncertainty about external implementations.
        /// </summary>
        [Fact]
        public void PublicInterface_RetainsKnownTargetAndAddsUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(IService service)
                    {
                        service.Execute();
                    }
                }

                public interface IService
                {
                    void Execute();
                }

                public sealed class Service : IService
                {
                    public void Execute()
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
                    GetDispatchEdges(run));

            Assert.Equal(
                "Service",
                GetTargetMethod(edge).ContainingType.Name);

            AssertDispatchUncertainty(run);
        }

        /// <summary>
        /// Ensures that an internal interface without external friend access
        /// has a complete implementation set.
        /// </summary>
        [Fact]
        public void InternalInterface_HasCompleteTargetSet()
        {
            const string source =
                """
                internal static class EntryPoint
                {
                    internal static void M(IService service)
                    {
                        service.Execute();
                    }
                }

                internal interface IService
                {
                    void Execute();
                }

                internal sealed class Service : IService
                {
                    public void Execute()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                GetDispatchEdges(run));

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a public non-sealed class retains known overrides and
        /// records possible external subclasses.
        /// </summary>
        [Fact]
        public void PublicVirtualClass_AddsHierarchyUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(BaseService service)
                    {
                        service.Execute();
                    }
                }

                public class BaseService
                {
                    public virtual void Execute()
                    {
                    }
                }

                public sealed class DerivedService : BaseService
                {
                    public override void Execute()
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
                GetDispatchEdges(run).Length);

            AssertDispatchUncertainty(run);
        }

        /// <summary>
        /// Ensures that an internal non-sealed hierarchy without external
        /// friend access is complete.
        /// </summary>
        [Fact]
        public void InternalVirtualClass_HasCompleteTargetSet()
        {
            const string source =
                """
                internal static class EntryPoint
                {
                    internal static void M(BaseService service)
                    {
                        service.Execute();
                    }
                }

                internal class BaseService
                {
                    public virtual void Execute()
                    {
                    }
                }

                internal sealed class DerivedService : BaseService
                {
                    public override void Execute()
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
                GetDispatchEdges(run).Length);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a sealed override closes its virtual slot for a
        /// statically derived receiver.
        /// </summary>
        [Fact]
        public void SealedOverride_HasCompleteTargetSet()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(DerivedService service)
                    {
                        service.Execute();
                    }
                }

                public class BaseService
                {
                    public virtual void Execute()
                    {
                    }
                }

                public class DerivedService : BaseService
                {
                    public sealed override void Execute()
                    {
                    }
                }

                public sealed class MoreDerivedService :
                    DerivedService
                {
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetDispatchEdges(run));

            Assert.Equal(
                "DerivedService",
                GetTargetMethod(edge).ContainingType.Name);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a private-protected virtual slot cannot receive an
        /// override from an external assembly.
        /// </summary>
        [Fact]
        public void PrivateProtectedSlot_HasCompleteTargetSet()
        {
            const string source =
                """
                public class BaseService
                {
                    public void M()
                    {
                        Execute();
                    }

                    private protected virtual void Execute()
                    {
                    }
                }

                internal sealed class DerivedService : BaseService
                {
                    private protected override void Execute()
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
                GetDispatchEdges(run).Length);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that direct object creation produces a complete exact
        /// receiver target set.
        /// </summary>
        [Fact]
        public void ExactReceiver_HasCompleteTargetSet()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M()
                    {
                        new DerivedService().Execute();
                    }
                }

                public class BaseService
                {
                    public virtual void Execute()
                    {
                    }
                }

                public class DerivedService : BaseService
                {
                    public override void Execute()
                    {
                    }
                }

                public sealed class MoreDerivedService :
                    DerivedService
                {
                    public override void Execute()
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
                    GetDispatchEdges(run));

            Assert.Equal(
                "DerivedService",
                GetTargetMethod(edge).ContainingType.Name);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that a public interface remains open even when a known
        /// implementation is resolved from another analyzed project.
        /// </summary>
        [Fact]
        public void CrossCompilationPublicInterface_RemainsOpen()
        {
            const string dependencySource =
                """
                namespace Dependency
                {
                    public interface IService
                    {
                        void Execute();
                    }

                    public sealed class Service : IService
                    {
                        public void Execute()
                        {
                        }
                    }
                }
                """;

            const string consumerSource =
                """
                using Dependency;

                public static class EntryPoint
                {
                    public static void M(IService service)
                    {
                        service.Execute();
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
                    GetDispatchEdges(run));

            Assert.False(
                GetTargetMethod(edge)
                    .DeclaringSyntaxReferences
                    .IsDefaultOrEmpty);

            AssertDispatchUncertainty(run);
        }

        /// <summary>
        /// Ensures that an interface-constrained type parameter includes known
        /// compatible source types, excludes unrelated types, and remains
        /// open.
        /// </summary>
        [Fact]
        public void InterfaceConstrainedTypeParameter_FiltersKnownTargets()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M<TService>(
                        TService service)
                        where TService : IService
                    {
                        service.Execute();
                    }
                }

                public interface IService
                {
                    void Execute();
                }

                public sealed class FirstService : IService
                {
                    public void Execute()
                    {
                    }
                }

                public sealed class SecondService : IService
                {
                    public void Execute()
                    {
                    }
                }

                public sealed class UnrelatedService
                {
                    public void Execute()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] targetTypeNames =
                GetDispatchEdges(run)
                    .Select(
                        static edge =>
                            GetTargetMethod(edge)
                                .ContainingType.Name)
                    .OrderBy(
                        static name =>
                            name,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "FirstService",
                    "SecondService"
                },
                targetTypeNames);

            AssertDispatchUncertainty(run);
        }

        /// <summary>
        /// Ensures that a class-constrained type parameter excludes base and
        /// sibling hierarchies that do not satisfy the constraint.
        /// </summary>
        [Fact]
        public void ClassConstrainedTypeParameter_FiltersKnownTargets()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M<TService>(
                        TService service)
                        where TService : DerivedService
                    {
                        service.Execute();
                    }
                }

                public class BaseService
                {
                    public virtual void Execute()
                    {
                    }
                }

                public class DerivedService : BaseService
                {
                    public override void Execute()
                    {
                    }
                }

                public sealed class MoreDerivedService :
                    DerivedService
                {
                    public override void Execute()
                    {
                    }
                }

                public sealed class SiblingService :
                    BaseService
                {
                    public override void Execute()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] targetTypeNames =
                GetDispatchEdges(run)
                    .Select(
                        static edge =>
                            GetTargetMethod(edge)
                                .ContainingType.Name)
                    .OrderBy(
                        static name =>
                            name,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "DerivedService",
                    "MoreDerivedService"
                },
                targetTypeNames);

            AssertDispatchUncertainty(run);
        }

        /// <summary>
        /// Ensures that accessor dispatch uses the same open-interface
        /// completeness rules as method dispatch.
        /// </summary>
        [Fact]
        public void PublicInterfaceProperty_AddsUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static int M(IService service)
                    {
                        return service.Value;
                    }
                }

                public interface IService
                {
                    int Value
                    {
                        get;
                    }
                }

                public sealed class Service : IService
                {
                    public int Value => 0;
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                run.RootSummary.CallEdges.Where(
                    static edge =>
                        edge.CallSiteStep.Kind ==
                        ExceptionFlowPathStepKind.PropertyGetter));

            AssertDispatchUncertainty(run);
        }

        /// <summary>
        /// Ensures that interface-based disposal retains known implementations
        /// while marking the public framework interface as open.
        /// </summary>
        [Fact]
        public void PublicDisposableInterface_AddsUncertainty()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(IDisposable resource)
                    {
                        using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                run.RootSummary.CallEdges.Where(
                    static edge =>
                        edge.CallSiteStep.Kind ==
                        ExceptionFlowPathStepKind.DisposeCall));

            AssertDispatchUncertainty(run);
        }

        /// <summary>
        /// Ensures that a typed catch clause retains uncertainty because an
        /// unknown target may throw another exception type.
        /// </summary>
        [Fact]
        public void TypedCatch_RetainsOpenDispatchUncertainty()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(IService service)
                    {
                        try
                        {
                            service.Execute();
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }

                public interface IService
                {
                    void Execute();
                }

                public sealed class Service : IService
                {
                    public void Execute()
                    {
                        throw new ArgumentException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            AssertDispatchUncertainty(run);
        }

        /// <summary>
        /// Ensures that a catch-all removes both known dispatch edges and open
        /// dispatch uncertainty.
        /// </summary>
        [Fact]
        public void CatchAll_RemovesOpenDispatchUncertainty()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(IService service)
                    {
                        try
                        {
                            service.Execute();
                        }
                        catch
                        {
                        }
                    }
                }

                public interface IService
                {
                    void Execute();
                }

                public sealed class Service : IService
                {
                    public void Execute()
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

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that an explicitly statically bound base call does not add
        /// runtime-target uncertainty.
        /// </summary>
        [Fact]
        public void BaseCall_DoesNotAddDispatchUncertainty()
        {
            const string source =
                """
                public class BaseService
                {
                    public virtual void Execute()
                    {
                    }
                }

                public sealed class DerivedService : BaseService
                {
                    public void M()
                    {
                        base.Execute();
                    }

                    public override void Execute()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Gets all root method-dispatch edges.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <returns>The virtual and interface method edges.</returns>
        private static ExceptionFlowSummaryCallEdge[] GetDispatchEdges(
            ExceptionFlowSummaryGraphTestRun run)
        {
            return run.RootSummary.CallEdges
                .Where(
                    static edge =>
                        edge.CallSiteStep.Kind is
                            ExceptionFlowPathStepKind.VirtualMethodCall or
                            ExceptionFlowPathStepKind.InterfaceMethodCall)
                .ToArray();
        }

        /// <summary>
        /// Gets the method symbol represented by one call edge.
        /// </summary>
        /// <param name="edge">
        /// The edge to inspect.
        /// </param>
        /// <returns>The required target method.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the edge target is not a method.
        /// </exception>
        private static IMethodSymbol GetTargetMethod(
            ExceptionFlowSummaryCallEdge edge)
        {
            return edge.Target.Symbol as IMethodSymbol ??
                   throw new InvalidOperationException(
                       "The dispatch edge target was not a method.");
        }

        /// <summary>
        /// Ensures that exactly one open-dispatch uncertainty entry exists.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        private static void AssertDispatchUncertainty(
            ExceptionFlowSummaryGraphTestRun run)
        {
            string uncertainty =
                Assert.Single(
                    run.RootSummary.UncertainTargets);

            Assert.Contains(
                "Additional runtime dispatch targets",
                uncertainty);
        }
    }
}
