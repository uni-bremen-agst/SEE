using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests runtime-target expansion for property, indexer, and event
    /// accessors.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphAccessorDispatchTests
    {
        /// <summary>
        /// Ensures that a virtual property getter includes the base getter and
        /// every compatible known override.
        /// </summary>
        [Fact]
        public void VirtualPropertyGetter_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static int M(Base value)
                    {
                        return value.Value;
                    }
                }

                public class Base
                {
                    public virtual int Value
                    {
                        get
                        {
                            throw new ArgumentException();
                        }
                    }
                }

                public sealed class Derived : Base
                {
                    public override int Value
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
                    ExceptionFlowPathStepKind.PropertyGetter);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "Base"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "Derived"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an explicit interface property implementation becomes
        /// the runtime getter target.
        /// </summary>
        [Fact]
        public void InterfacePropertyGetter_UsesExplicitImplementation()
        {
            const string source =
                """
                using System;

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
                    int IService.Value
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

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind.PropertyGetter));

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
        /// Ensures that setter value facts are transferred by ordinal to an
        /// override accessor.
        /// </summary>
        [Fact]
        public void VirtualPropertySetter_TransfersValueFacts()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Base value)
                    {
                        value.Value = "valid";
                    }
                }

                public class Base
                {
                    public virtual string? Value
                    {
                        get
                        {
                            return null;
                        }

                        set
                        {
                            throw new ArgumentException();
                        }
                    }
                }

                public sealed class Derived : Base
                {
                    public override string? Value
                    {
                        get
                        {
                            return null;
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

            ExceptionFlowSummaryCallEdge[] edges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.PropertySetter);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "Base"),
                "ArgumentException");

            ExceptionFlowSummary derivedSummary =
                run.GetRequiredSummary(
                    GetEdgeForContainingType(
                            edges,
                            "Derived")
                        .Target);

            Assert.Empty(
                derivedSummary.Sources);
        }

        /// <summary>
        /// Ensures that indexer argument facts reach an explicit interface
        /// getter implementation.
        /// </summary>
        [Fact]
        public void InterfaceIndexerGetter_TransfersIndexFacts()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static string M(IStore store)
                    {
                        return store["valid"];
                    }
                }

                public interface IStore
                {
                    string this[string? key]
                    {
                        get;
                    }
                }

                public sealed class Store : IStore
                {
                    string IStore.this[string? index]
                    {
                        get
                        {
                            ArgumentNullException.ThrowIfNull(index);
                            throw new InvalidOperationException();
                        }
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
                        ExceptionFlowPathStepKind.IndexerGetter));

            ExceptionFlowSummary summary =
                run.GetRequiredSummary(
                    edge.Target);

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    summary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that index and value facts reach an explicit interface
        /// setter implementation.
        /// </summary>
        [Fact]
        public void InterfaceIndexerSetter_TransfersIndexAndValueFacts()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(IStore store)
                    {
                        store["valid"] = "value";
                    }
                }

                public interface IStore
                {
                    string this[string? key]
                    {
                        set;
                    }
                }

                public sealed class Store : IStore
                {
                    string IStore.this[string? index]
                    {
                        set
                        {
                            ArgumentNullException.ThrowIfNull(index);
                            ArgumentNullException.ThrowIfNull(value);
                            throw new NotSupportedException();
                        }
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
                        ExceptionFlowPathStepKind.IndexerSetter));

            ExceptionFlowSummary summary =
                run.GetRequiredSummary(
                    edge.Target);

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    summary.Sources);

            Assert.Equal(
                "NotSupportedException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a virtual event subscription includes every compatible
        /// custom add accessor.
        /// </summary>
        [Fact]
        public void VirtualEventAdd_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Base value,
                        EventHandler handler)
                    {
                        value.Changed += handler;
                    }
                }

                public class Base
                {
                    public virtual event EventHandler Changed
                    {
                        add
                        {
                            throw new ArgumentException();
                        }

                        remove
                        {
                        }
                    }
                }

                public sealed class Derived : Base
                {
                    public override event EventHandler Changed
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

            ExceptionFlowSummaryCallEdge[] edges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.EventAdd);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "Base"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "Derived"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an explicit interface event implementation becomes the
        /// runtime remove-accessor target.
        /// </summary>
        [Fact]
        public void InterfaceEventRemove_UsesExplicitImplementation()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        IService service,
                        EventHandler handler)
                    {
                        service.Changed -= handler;
                    }
                }

                public interface IService
                {
                    event EventHandler Changed;
                }

                public sealed class Service : IService
                {
                    event EventHandler IService.Changed
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

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind.EventRemove));

            Assert.Equal(
                "Service",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "NotSupportedException");
        }

        /// <summary>
        /// Ensures that a field-like interface event implementation does not
        /// create a user-code accessor edge.
        /// </summary>
        [Fact]
        public void FieldLikeInterfaceEvent_DoesNotCreateUserCodeEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        IService service,
                        EventHandler handler)
                    {
                        service.Changed += handler;
                    }
                }

                public interface IService
                {
                    event EventHandler Changed;
                }

                public sealed class Service : IService
                {
                    public event EventHandler? Changed;
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.EventAdd));
        }

        /// <summary>
        /// Ensures that an explicit base property access remains statically
        /// bound.
        /// </summary>
        [Fact]
        public void BasePropertyAccess_RemainsDirectlyBound()
        {
            const string source =
                """
                using System;

                public class Base
                {
                    public virtual int Value
                    {
                        get
                        {
                            throw new ArgumentException();
                        }
                    }
                }

                public sealed class Derived : Base
                {
                    public int M()
                    {
                        return base.Value;
                    }

                    public override int Value
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

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind.PropertyGetter));

            Assert.Equal(
                "Base",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "ArgumentException");
        }

        /// <summary>
        /// Ensures that a directly created receiver excludes overrides on
        /// more-derived runtime types.
        /// </summary>
        [Fact]
        public void ExactObjectCreationPropertyGetter_UsesCreatedType()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static int M()
                    {
                        return new Derived().Value;
                    }
                }

                public class Base
                {
                    public virtual int Value
                    {
                        get
                        {
                            throw new ArgumentException();
                        }
                    }
                }

                public class Derived : Base
                {
                }

                public sealed class MoreDerived : Derived
                {
                    public override int Value
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

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind.PropertyGetter));

            Assert.Equal(
                "Base",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "ArgumentException");
        }

        /// <summary>
        /// Ensures that a direct object initializer uses the exact created
        /// receiver type for setter dispatch.
        /// </summary>
        [Fact]
        public void ObjectInitializer_UsesExactReceiverType()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        _ = new Derived
                        {
                            Value = "valid"
                        };
                    }
                }

                public class Base
                {
                    public virtual string? Value
                    {
                        get
                        {
                            return null;
                        }

                        set
                        {
                            throw new ArgumentException();
                        }
                    }
                }

                public class Derived : Base
                {
                }

                public sealed class MoreDerived : Derived
                {
                    public override string? Value
                    {
                        get
                        {
                            return null;
                        }

                        set
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

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind.PropertySetter));

            Assert.Equal(
                "Base",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "ArgumentException");
        }

        /// <summary>
        /// Ensures that an unqualified property access through implicit
        /// <see langword="this"/> uses virtual dispatch.
        /// </summary>
        [Fact]
        public void UnqualifiedPropertyAccess_UsesVirtualDispatch()
        {
            const string source =
                """
                using System;

                public class Base
                {
                    public int M()
                    {
                        return Value;
                    }

                    public virtual int Value
                    {
                        get
                        {
                            throw new ArgumentException();
                        }
                    }
                }

                public sealed class Derived : Base
                {
                    public override int Value
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
                    ExceptionFlowPathStepKind.PropertyGetter);

            Assert.Equal(
                2,
                edges.Length);
        }

        /// <summary>
        /// Ensures that multiple receiver types inheriting one interface
        /// property implementation create one target edge.
        /// </summary>
        [Fact]
        public void InheritedInterfacePropertyImplementation_IsDeduplicated()
        {
            const string source =
                """
                using System;

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

                public class BaseService : IService
                {
                    public int Value
                    {
                        get
                        {
                            throw new InvalidOperationException();
                        }
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
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind.PropertyGetter));

            Assert.Equal(
                "BaseService",
                GetTargetMethod(edge).ContainingType.Name);
        }

        /// <summary>
        /// Ensures that an interface accessor selected from dependency
        /// metadata is mapped back to the source implementation.
        /// </summary>
        [Fact]
        public void CrossCompilationInterfaceProperty_UsesSourceImplementation()
        {
            const string dependencySource =
                """
                using System;

                namespace Dependency
                {
                    public interface IService
                    {
                        int Value
                        {
                            get;
                        }
                    }

                    public static class Container
                    {
                        public sealed class Service : IService
                        {
                            int IService.Value
                            {
                                get
                                {
                                    throw new InvalidOperationException();
                                }
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
                    public static int M(IService service)
                    {
                        return service.Value;
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
                        ExceptionFlowPathStepKind.PropertyGetter));

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
        /// Gets root call edges with one path-step kind.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <param name="stepKind">
        /// The required path-step kind.
        /// </param>
        /// <returns>The matching edges.</returns>
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
        /// <param name="edges">
        /// The candidate edges.
        /// </param>
        /// <param name="containingTypeName">
        /// The expected containing-type name.
        /// </param>
        /// <returns>The matching edge.</returns>
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
        /// Gets the method symbol represented by one edge target.
        /// </summary>
        /// <param name="edge">
        /// The edge to inspect.
        /// </param>
        /// <returns>The required method symbol.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the target is not a method.
        /// </exception>
        private static IMethodSymbol GetTargetMethod(
            ExceptionFlowSummaryCallEdge edge)
        {
            return edge.Target.Symbol as IMethodSymbol ??
                   throw new InvalidOperationException(
                       "The accessor edge target was not a method.");
        }

        /// <summary>
        /// Ensures that one target summary contains exactly one source with
        /// the expected exception type.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <param name="edge">
        /// The edge whose target should be inspected.
        /// </param>
        /// <param name="exceptionTypeName">
        /// The expected simple exception type name.
        /// </param>
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
