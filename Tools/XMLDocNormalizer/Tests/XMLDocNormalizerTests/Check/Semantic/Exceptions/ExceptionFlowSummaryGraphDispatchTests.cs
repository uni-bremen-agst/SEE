using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests runtime-target expansion for explicitly written virtual and
    /// interface method invocations in exception-flow summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphDispatchTests
    {
        /// <summary>
        /// Ensures that a virtual call on a concrete base type includes the
        /// base implementation and every known override.
        /// </summary>
        [Fact]
        public void VirtualCall_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Base value)
                    {
                        value.Execute();
                    }
                }

                public class Base
                {
                    public virtual void Execute()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class Derived : Base
                {
                    public override void Execute()
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
                GetDispatchEdges(
                    run);

            Assert.Equal(
                2,
                edges.Length);

            Dictionary<string, ExceptionFlowSummaryCallEdge> edgesByType =
                edges.ToDictionary(
                    static edge =>
                        GetTargetMethod(edge).ContainingType.Name,
                    StringComparer.Ordinal);

            AssertTargetException(
                run,
                edgesByType["Base"],
                "ArgumentException");

            AssertTargetException(
                run,
                edgesByType["Derived"],
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an abstract base declaration is not emitted as a
        /// runtime target when a concrete override is available.
        /// </summary>
        [Fact]
        public void AbstractVirtualCall_ExcludesAbstractTarget()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Base value)
                    {
                        value.Execute();
                    }
                }

                public abstract class Base
                {
                    public abstract void Execute();
                }

                public sealed class Derived : Base
                {
                    public override void Execute()
                    {
                        throw new InvalidOperationException();
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
                "Derived",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an implicit interface implementation becomes the
        /// runtime target of an interface invocation.
        /// </summary>
        [Fact]
        public void InterfaceCall_UsesImplicitImplementation()
        {
            const string source =
                """
                using System;

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
                        throw new InvalidOperationException();
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
                ExceptionFlowPathStepKind.InterfaceMethodCall,
                edge.CallSiteStep.Kind);

            Assert.Equal(
                "Service",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an explicit interface implementation becomes the
        /// runtime target of an interface invocation.
        /// </summary>
        [Fact]
        public void InterfaceCall_UsesExplicitImplementation()
        {
            const string source =
                """
                using System;

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
                    void IService.Execute()
                    {
                        throw new InvalidOperationException();
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

            IMethodSymbol targetMethod =
                GetTargetMethod(
                    edge);

            Assert.Equal(
                "Service",
                targetMethod.ContainingType.Name);

            Assert.Single(
                targetMethod.ExplicitInterfaceImplementations);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that every known concrete implementation of an interface
        /// member creates a possible runtime target edge.
        /// </summary>
        [Fact]
        public void InterfaceCall_CreatesEdgesForAllKnownImplementations()
        {
            const string source =
                """
                using System;

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

                public sealed class FirstService : IService
                {
                    public void Execute()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class SecondService : IService
                {
                    public void Execute()
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
                GetDispatchEdges(
                    run);

            Assert.Equal(
                2,
                edges.Length);

            string[] containingTypes =
                edges.Select(
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
                containingTypes);
        }

        /// <summary>
        /// Ensures that multiple runtime types inheriting the same interface
        /// implementation create one alternative target edge.
        /// </summary>
        [Fact]
        public void InheritedInterfaceImplementation_IsDeduplicated()
        {
            const string source =
                """
                using System;

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

                public class BaseService : IService
                {
                    public void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class DerivedService : BaseService
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
                "BaseService",
                GetTargetMethod(edge).ContainingType.Name);
        }

        /// <summary>
        /// Ensures that an invocation through a sealed override has one fixed
        /// method target even when Roslyn no longer reports virtual dispatch.
        /// </summary>
        [Fact]
        public void SealedOverrideCall_CreatesSingleTargetEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Derived value)
                    {
                        value.Execute();
                    }
                }

                public class Base
                {
                    public virtual void Execute()
                    {
                    }
                }

                public class Derived : Base
                {
                    public sealed override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerived : Derived
                {
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetMethodEdges(run));

            Assert.Equal(
                "Derived",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an explicit base invocation remains statically bound
        /// and does not expand to overrides.
        /// </summary>
        [Fact]
        public void BaseInvocation_RemainsDirectlyBound()
        {
            const string source =
                """
                using System;

                public class Base
                {
                    public virtual void Execute()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class Derived : Base
                {
                    public void M()
                    {
                        base.Execute();
                    }

                    public override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetMethodEdges(run));

            Assert.Equal(
                ExceptionFlowPathStepKind.MethodCall,
                edge.CallSiteStep.Kind);

            Assert.Equal(
                "Base",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "ArgumentException");
        }

        /// <summary>
        /// Ensures that an object created directly at a virtual call site is
        /// treated as an exact receiver instead of expanding to unrelated
        /// derived runtime types.
        /// </summary>
        [Fact]
        public void ExactObjectCreationReceiver_UsesOnlyCreatedType()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        new Derived().Execute();
                    }
                }

                public class Base
                {
                    public virtual void Execute()
                    {
                        throw new ArgumentException();
                    }
                }

                public class Derived : Base
                {
                    public override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerived : Derived
                {
                    public override void Execute()
                    {
                        throw new NotSupportedException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetMethodEdges(run));

            Assert.Equal(
                "Derived",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that named arguments are mapped using the statically
        /// selected parameter names before their value facts are transferred
        /// by ordinal to an override with different parameter names.
        /// </summary>
        [Fact]
        public void NamedArguments_PreserveStaticParameterMapping()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Base value)
                    {
                        value.Execute(
                            second: "",
                            first: "valid");
                    }
                }

                public class Base
                {
                    public virtual void Execute(
                        string? first,
                        string? second)
                    {
                    }
                }

                public sealed class Derived : Base
                {
                    public override void Execute(
                        string? left,
                        string? right)
                    {
                        ArgumentNullException.ThrowIfNull(left);
                        ArgumentException.ThrowIfNullOrWhiteSpace(right);
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge derivedEdge =
                GetMethodEdgeForContainingType(
                    run,
                    "Derived");

            AssertTargetException(
                run,
                derivedEdge,
                "ArgumentException");
        }

        /// <summary>
        /// Ensures that an omitted optional argument uses the default value of
        /// the compile-time method rather than a different default written on
        /// the runtime override.
        /// </summary>
        [Fact]
        public void OptionalArgument_UsesStaticDefaultValue()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Base value)
                    {
                        value.Execute();
                    }
                }

                public class Base
                {
                    public virtual void Execute(string? value = null)
                    {
                    }
                }

                public sealed class Derived : Base
                {
                    public override void Execute(string? value = "known")
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge derivedEdge =
                GetMethodEdgeForContainingType(
                    run,
                    "Derived");

            AssertTargetException(
                run,
                derivedEdge,
                "ArgumentNullException");
        }

        /// <summary>
        /// Ensures that a typed catch filter is attached to every alternative
        /// runtime dispatch edge created for one call site.
        /// </summary>
        [Fact]
        public void TypedCatch_IsAppliedToEveryDispatchEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Base value)
                    {
                        try
                        {
                            value.Execute();
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }

                public class Base
                {
                    public virtual void Execute()
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class Derived : Base
                {
                    public override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            INamedTypeSymbol argumentException =
                run.GetRequiredType(
                    "System.ArgumentException");

            ExceptionFlowSummaryCallEdge[] edges =
                GetDispatchEdges(
                    run);

            Assert.Equal(
                2,
                edges.Length);

            Assert.All(
                edges,
                edge =>
                    Assert.True(
                        edge.Suppresses(
                            argumentException)));
        }

        /// <summary>
        /// Ensures that a catch-all removes every alternative runtime target
        /// edge from its protected fragment.
        /// </summary>
        [Fact]
        public void CatchAll_RemovesDispatchEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(Base value)
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

                public class Base
                {
                    public virtual void Execute()
                    {
                    }
                }

                public sealed class Derived : Base
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

            Assert.Empty(
                run.RootSummary.CallEdges);
        }

        /// <summary>
        /// Ensures that constructed generic virtual invocations still resolve
        /// their base and override method definitions.
        /// </summary>
        [Fact]
        public void GenericVirtualMethod_CreatesOverrideEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Base value)
                    {
                        value.Execute(1);
                    }
                }

                public class Base
                {
                    public virtual void Execute<T>(T value)
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class Derived : Base
                {
                    public override void Execute<T>(T value)
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
                GetDispatchEdges(
                    run);

            Assert.Equal(
                2,
                edges.Length);

            Assert.Contains(
                edges,
                edge =>
                    GetTargetMethod(edge).ContainingType.Name ==
                    "Base");

            Assert.Contains(
                edges,
                edge =>
                    GetTargetMethod(edge).ContainingType.Name ==
                    "Derived");
        }

        /// <summary>
        /// Ensures that an interface method selected from dependency metadata
        /// is mapped back to the dependency's source compilation and its
        /// nested implementation body.
        /// </summary>
        [Fact]
        public void CrossCompilationInterfaceCall_UsesSourceImplementation()
        {
            const string dependencySource =
                """
                using System;

                namespace Dependency
                {
                    public interface IService
                    {
                        void Execute();
                    }

                    public static class Container
                    {
                        public sealed class Service : IService
                        {
                            public void Execute()
                            {
                                throw new InvalidOperationException();
                            }
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

            IMethodSymbol targetMethod =
                GetTargetMethod(
                    edge);

            Assert.Equal(
                "Service",
                targetMethod.ContainingType.Name);

            Assert.False(
                targetMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a receiver statically typed as a derived class cannot
        /// dispatch to the base implementation or an unrelated sibling
        /// override.
        /// </summary>
        [Fact]
        public void DerivedStaticReceiver_ExcludesIncompatibleTargets()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Derived value)
                    {
                        value.Execute();
                    }
                }

                public class Base
                {
                    public virtual void Execute()
                    {
                        throw new ArgumentException();
                    }
                }

                public class Derived : Base
                {
                    public override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerived : Derived
                {
                    public override void Execute()
                    {
                        throw new FormatException();
                    }
                }

                public sealed class Sibling : Base
                {
                    public override void Execute()
                    {
                        throw new NotSupportedException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetDispatchEdges(
                    run);

            Assert.Equal(
                2,
                edges.Length);

            string[] targetTypes =
                edges.Select(
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
                    "Derived",
                    "MoreDerived"
                },
                targetTypes);
        }

        /// <summary>
        /// Ensures that a receiver statically typed as a derived interface
        /// excludes implementations of only its base interface.
        /// </summary>
        [Fact]
        public void DerivedInterfaceReceiver_ExcludesBaseOnlyImplementation()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(IDerived service)
                    {
                        service.Execute();
                    }
                }

                public interface IBase
                {
                    void Execute();
                }

                public interface IDerived : IBase
                {
                }

                public sealed class BaseOnlyService : IBase
                {
                    public void Execute()
                    {
                    }
                }

                public sealed class DerivedService : IDerived
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
                "DerivedService",
                GetTargetMethod(edge).ContainingType.Name);
        }

        /// <summary>
        /// Gets every alternative virtual or interface dispatch edge from the
        /// root summary.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <returns>The dispatch edges in source insertion order.</returns>
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
        /// Gets every explicit method-call edge from the root summary,
        /// including direct and runtime-dispatch calls.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <returns>The method edges in source insertion order.</returns>
        private static ExceptionFlowSummaryCallEdge[] GetMethodEdges(
            ExceptionFlowSummaryGraphTestRun run)
        {
            return run.RootSummary.CallEdges
                .Where(
                    static edge =>
                        edge.CallSiteStep.Kind is
                            ExceptionFlowPathStepKind.MethodCall or
                            ExceptionFlowPathStepKind.VirtualMethodCall or
                            ExceptionFlowPathStepKind.InterfaceMethodCall)
                .ToArray();
        }

        /// <summary>
        /// Gets the method edge whose target is declared by one named type.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <param name="containingTypeName">
        /// The expected target containing-type name.
        /// </param>
        /// <returns>The uniquely matching method edge.</returns>
        private static ExceptionFlowSummaryCallEdge
            GetMethodEdgeForContainingType(
                ExceptionFlowSummaryGraphTestRun run,
                string containingTypeName)
        {
            return Assert.Single(
                GetMethodEdges(run)
                    .Where(
                        edge =>
                            string.Equals(
                                GetTargetMethod(edge)
                                    .ContainingType.Name,
                                containingTypeName,
                                StringComparison.Ordinal)));
        }

        /// <summary>
        /// Gets the method symbol represented by one call edge target.
        /// </summary>
        /// <param name="edge">
        /// The call edge to inspect.
        /// </param>
        /// <returns>The required method target.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the edge target is not a method symbol.
        /// </exception>
        private static IMethodSymbol GetTargetMethod(
            ExceptionFlowSummaryCallEdge edge)
        {
            return edge.Target.Symbol as IMethodSymbol ??
                   throw new InvalidOperationException(
                       "The dispatch edge target was not a method.");
        }

        /// <summary>
        /// Ensures that one target summary contains exactly one exception
        /// source with the expected simple type name.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <param name="edge">
        /// The edge whose target summary should be inspected.
        /// </param>
        /// <param name="exceptionTypeName">
        /// The expected simple exception type name.
        /// </param>
        private static void AssertTargetException(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowSummaryCallEdge edge,
            string exceptionTypeName)
        {
            ExceptionFlowSummary targetSummary =
                run.GetRequiredSummary(
                    edge.Target);

            Assert.True(
                targetSummary.HasExecutableBody);

            ExceptionFlowSummarySource source =
                Assert.Single(
                    targetSummary.Sources);

            Assert.Equal(
                exceptionTypeName,
                source.ExceptionType.Name);
        }
    }
}
