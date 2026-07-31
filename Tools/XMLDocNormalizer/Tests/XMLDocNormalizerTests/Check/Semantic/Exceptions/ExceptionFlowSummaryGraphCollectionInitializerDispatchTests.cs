using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests runtime-target expansion for compiler-selected collection
    /// initializer <c>Add</c> calls.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphCollectionInitializerDispatchTests
    {
        /// <summary>
        /// Ensures that a virtual <c>Add</c> call on a nested collection
        /// receiver includes the base implementation and compatible override.
        /// </summary>
        [Fact]
        public void VirtualAdd_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M(BaseCollection values)
                    {
                        _ = new Holder(values)
                        {
                            Values =
                            {
                                "known"
                            }
                        };
                    }
                }

                public sealed class Holder
                {
                    public Holder(BaseCollection values)
                    {
                        Values = values;
                    }

                    public BaseCollection Values
                    {
                        get;
                    }
                }

                public class BaseCollection : IEnumerable
                {
                    public virtual void Add(string value)
                    {
                        throw new ArgumentException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }

                public sealed class DerivedCollection : BaseCollection
                {
                    public override void Add(string value)
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
                GetAddEdges(run);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseCollection"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedCollection"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a statically derived nested receiver excludes base and
        /// sibling implementations that cannot receive the runtime value.
        /// </summary>
        [Fact]
        public void DerivedStaticReceiver_ExcludesIncompatibleTargets()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M(DerivedCollection values)
                    {
                        _ = new Holder(values)
                        {
                            Values =
                            {
                                "known"
                            }
                        };
                    }
                }

                public sealed class Holder
                {
                    public Holder(DerivedCollection values)
                    {
                        Values = values;
                    }

                    public DerivedCollection Values
                    {
                        get;
                    }
                }

                public class BaseCollection : IEnumerable
                {
                    public virtual void Add(string value)
                    {
                        throw new ArgumentException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }

                public class DerivedCollection : BaseCollection
                {
                    public override void Add(string value)
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedCollection : DerivedCollection
                {
                    public override void Add(string value)
                    {
                        throw new FormatException();
                    }
                }

                public sealed class SiblingCollection : BaseCollection
                {
                    public override void Add(string value)
                    {
                        throw new NotSupportedException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] targetTypeNames =
                GetAddEdges(run)
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
                    "DerivedCollection",
                    "MoreDerivedCollection"
                },
                targetTypeNames);
        }

        /// <summary>
        /// Ensures that direct collection creation fixes the runtime receiver
        /// to the created type.
        /// </summary>
        [Fact]
        public void ExactObjectCreationReceiver_UsesCreatedType()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        _ = new DerivedCollection
                        {
                            "known"
                        };
                    }
                }

                public class BaseCollection : IEnumerable
                {
                    public virtual void Add(string value)
                    {
                        throw new ArgumentException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }

                public class DerivedCollection : BaseCollection
                {
                    public override void Add(string value)
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedCollection : DerivedCollection
                {
                    public override void Add(string value)
                    {
                        throw new FormatException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetAddEdges(run));

            Assert.Equal(
                "DerivedCollection",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an explicit interface implementation becomes the
        /// runtime target of a nested collection initializer.
        /// </summary>
        [Fact]
        public void InterfaceAdd_UsesExplicitImplementation()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M(IValues values)
                    {
                        _ = new Holder(values)
                        {
                            Values =
                            {
                                "known"
                            }
                        };
                    }
                }

                public interface IValues : IEnumerable
                {
                    void Add(string value);
                }

                public sealed class Holder
                {
                    public Holder(IValues values)
                    {
                        Values = values;
                    }

                    public IValues Values
                    {
                        get;
                    }
                }

                public sealed class Values : IValues
                {
                    void IValues.Add(string value)
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

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetAddEdges(run));

            IMethodSymbol targetMethod =
                GetTargetMethod(edge);

            Assert.Equal(
                "Values",
                targetMethod.ContainingType.Name);

            Assert.Single(
                targetMethod.ExplicitInterfaceImplementations);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that multiple runtime types inheriting one <c>Add</c>
        /// implementation create one target edge.
        /// </summary>
        [Fact]
        public void InheritedAddImplementation_IsDeduplicated()
        {
            const string source =
                """
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M(BaseCollection values)
                    {
                        _ = new Holder(values)
                        {
                            Values =
                            {
                                "known"
                            }
                        };
                    }
                }

                public sealed class Holder
                {
                    public Holder(BaseCollection values)
                    {
                        Values = values;
                    }

                    public BaseCollection Values
                    {
                        get;
                    }
                }

                public class BaseCollection : IEnumerable
                {
                    public virtual void Add(string value)
                    {
                        throw new InvalidOperationException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }

                public sealed class DerivedCollection : BaseCollection
                {
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetAddEdges(run));

            Assert.Equal(
                "BaseCollection",
                GetTargetMethod(edge).ContainingType.Name);
        }

        /// <summary>
        /// Ensures that explicit element facts are transferred by ordinal to
        /// an override whose parameter has a different name.
        /// </summary>
        [Fact]
        public void VirtualAdd_TransfersElementFactsByOrdinal()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M(BaseCollection values)
                    {
                        _ = new Holder(values)
                        {
                            Values =
                            {
                                "known"
                            }
                        };
                    }
                }

                public sealed class Holder
                {
                    public Holder(BaseCollection values)
                    {
                        Values = values;
                    }

                    public BaseCollection Values
                    {
                        get;
                    }
                }

                public class BaseCollection : IEnumerable
                {
                    public virtual void Add(string? value)
                    {
                        throw new ArgumentException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }

                public sealed class DerivedCollection : BaseCollection
                {
                    public override void Add(string? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetAddEdges(run);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseCollection"),
                "ArgumentException");

            ExceptionFlowSummary derivedSummary =
                run.GetRequiredSummary(
                    GetEdgeForContainingType(
                            edges,
                            "DerivedCollection")
                        .Target);

            Assert.Empty(
                derivedSummary.Sources);
        }

        /// <summary>
        /// Ensures that optional default facts from the statically selected
        /// method are transferred to an override.
        /// </summary>
        [Fact]
        public void VirtualAdd_TransfersOptionalDefaultFacts()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections;

                public static class EntryPoint
                {
                    public static void M(BaseCollection values)
                    {
                        _ = new Holder(values)
                        {
                            Values =
                            {
                                { "known" }
                            }
                        };
                    }
                }

                public sealed class Holder
                {
                    public Holder(BaseCollection values)
                    {
                        Values = values;
                    }

                    public BaseCollection Values
                    {
                        get;
                    }
                }

                public class BaseCollection : IEnumerable
                {
                    public virtual void Add(
                        string first,
                        string? second = "default")
                    {
                        throw new ArgumentException();
                    }

                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }

                public sealed class DerivedCollection : BaseCollection
                {
                    public override void Add(
                        string first,
                        string? second)
                    {
                        ArgumentNullException.ThrowIfNull(second);
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge derivedEdge =
                GetEdgeForContainingType(
                    GetAddEdges(run),
                    "DerivedCollection");

            AssertTargetException(
                run,
                derivedEdge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an interface implementation selected from dependency
        /// metadata is mapped back to its source declaration.
        /// </summary>
        [Fact]
        public void CrossCompilationInterfaceAdd_UsesSourceImplementation()
        {
            const string dependencySource =
                """
                using System;
                using System.Collections;

                namespace Dependency
                {
                    public interface IValues : IEnumerable
                    {
                        void Add(string value);
                    }

                    public sealed class Values : IValues
                    {
                        void IValues.Add(string value)
                        {
                            throw new InvalidOperationException();
                        }

                        public IEnumerator GetEnumerator()
                        {
                            return Array.Empty<object>().GetEnumerator();
                        }
                    }
                }
                """;

            const string consumerSource =
                """
                using Dependency;

                public static class EntryPoint
                {
                    public static void M(IValues values)
                    {
                        _ = new Holder(values)
                        {
                            Values =
                            {
                                "known"
                            }
                        };
                    }
                }

                public sealed class Holder
                {
                    public Holder(IValues values)
                    {
                        Values = values;
                    }

                    public IValues Values
                    {
                        get;
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
                    GetAddEdges(run));

            IMethodSymbol targetMethod =
                GetTargetMethod(edge);

            Assert.Equal(
                "Values",
                targetMethod.ContainingType.Name);

            Assert.False(
                targetMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an extension <c>Add</c> remains statically bound and
        /// retains its receiver and element facts.
        /// </summary>
        [Fact]
        public void ExtensionAdd_RemainsDirectlyBound()
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
                        _ = new Values
                        {
                            "known"
                        };
                    }
                }

                public class Values : IEnumerable
                {
                    public IEnumerator GetEnumerator()
                    {
                        return Array.Empty<object>().GetEnumerator();
                    }
                }

                public sealed class DerivedValues : Values
                {
                }

                public static class ValuesExtensions
                {
                    public static void Add(
                        this Values? values,
                        string? item)
                    {
                        ArgumentNullException.ThrowIfNull(values);
                        ArgumentNullException.ThrowIfNull(item);
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetAddEdges(run));

            Assert.Equal(
                "ValuesExtensions",
                GetTargetMethod(edge).ContainingType.Name);

            Assert.Empty(
                run.GetRequiredSummary(
                        edge.Target)
                    .Sources);
        }

        /// <summary>
        /// Gets all root collection-initializer <c>Add</c> edges.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <returns>The matching edges.</returns>
        private static ExceptionFlowSummaryCallEdge[] GetAddEdges(
            ExceptionFlowSummaryGraphTestRun run)
        {
            return run.RootSummary.CallEdges
                .Where(
                    static edge =>
                        edge.CallSiteStep.Kind ==
                        ExceptionFlowPathStepKind.CollectionAddCall)
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
                       "The collection initializer edge target was not a method.");
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
            ExceptionFlowSummary targetSummary =
                run.GetRequiredSummary(
                    edge.Target);

            ExceptionFlowSummarySource source =
                Assert.Single(
                    targetSummary.Sources);

            Assert.Equal(
                exceptionTypeName,
                source.ExceptionType.Name);
        }
    }
}
