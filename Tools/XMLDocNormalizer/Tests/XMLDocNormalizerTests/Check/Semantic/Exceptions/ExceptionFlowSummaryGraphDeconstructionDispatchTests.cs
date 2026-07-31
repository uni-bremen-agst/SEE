using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests runtime-target expansion for compiler-selected
    /// <c>Deconstruct</c> calls.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphDeconstructionDispatchTests
    {
        /// <summary>
        /// Ensures that a virtual <c>Deconstruct</c> call includes the base
        /// implementation and every compatible known override.
        /// </summary>
        [Fact]
        public void VirtualDeconstruct_CreatesBaseAndOverrideEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(BaseValue value)
                    {
                        (int left, int right) = value;
                    }
                }

                public class BaseValue
                {
                    public virtual void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class DerivedValue : BaseValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
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
                GetDeconstructEdges(run);

            Assert.Equal(
                2,
                edges.Length);

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "BaseValue"),
                "ArgumentException");

            AssertTargetException(
                run,
                GetEdgeForContainingType(
                    edges,
                    "DerivedValue"),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a statically derived receiver excludes base and
        /// sibling implementations that cannot receive the runtime value.
        /// </summary>
        [Fact]
        public void DerivedStaticReceiver_ExcludesIncompatibleTargets()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(DerivedValue value)
                    {
                        (int left, int right) = value;
                    }
                }

                public class BaseValue
                {
                    public virtual void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new ArgumentException();
                    }
                }

                public class DerivedValue : BaseValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedValue : DerivedValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new FormatException();
                    }
                }

                public sealed class SiblingValue : BaseValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
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
                GetDeconstructEdges(run)
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
                    "DerivedValue",
                    "MoreDerivedValue"
                },
                targetTypeNames);
        }

        /// <summary>
        /// Ensures that direct object creation fixes the runtime receiver to
        /// the created type.
        /// </summary>
        [Fact]
        public void ExactObjectCreationReceiver_UsesCreatedType()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        (int left, int right) = new DerivedValue();
                    }
                }

                public class BaseValue
                {
                    public virtual void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new ArgumentException();
                    }
                }

                public class DerivedValue : BaseValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedValue : DerivedValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
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
                    GetDeconstructEdges(run));

            Assert.Equal(
                "DerivedValue",
                GetTargetMethod(edge).ContainingType.Name);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that nested dispatch uses the corresponding output
        /// parameter type and excludes incompatible base and sibling targets.
        /// </summary>
        [Fact]
        public void NestedDeconstruct_UsesOutputParameterReceiverType()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Outer value)
                    {
                        var (first, (second, third)) = value;
                    }
                }

                public sealed class Outer
                {
                    public void Deconstruct(
                        out int first,
                        out DerivedInner remainder)
                    {
                        first = 0;
                        remainder = new DerivedInner();
                    }
                }

                public class BaseInner
                {
                    public virtual void Deconstruct(
                        out int second,
                        out int third)
                    {
                        throw new ArgumentException();
                    }
                }

                public class DerivedInner : BaseInner
                {
                    public override void Deconstruct(
                        out int second,
                        out int third)
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedInner : DerivedInner
                {
                    public override void Deconstruct(
                        out int second,
                        out int third)
                    {
                        throw new FormatException();
                    }
                }

                public sealed class SiblingInner : BaseInner
                {
                    public override void Deconstruct(
                        out int second,
                        out int third)
                    {
                        throw new NotSupportedException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] nestedTargetTypeNames =
                GetDeconstructEdges(run)
                    .Where(
                        static edge =>
                            GetTargetMethod(edge)
                                .ContainingType.Name !=
                            "Outer")
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
                    "DerivedInner",
                    "MoreDerivedInner"
                },
                nestedTargetTypeNames);
        }

        /// <summary>
        /// Ensures that an explicit interface implementation becomes the
        /// runtime <c>Deconstruct</c> target.
        /// </summary>
        [Fact]
        public void InterfaceDeconstruct_UsesExplicitImplementation()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(IValue value)
                    {
                        (int left, int right) = value;
                    }
                }

                public interface IValue
                {
                    void Deconstruct(
                        out int left,
                        out int right);
                }

                public sealed class Value : IValue
                {
                    void IValue.Deconstruct(
                        out int left,
                        out int right)
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
                    GetDeconstructEdges(run));

            IMethodSymbol targetMethod =
                GetTargetMethod(edge);

            Assert.Equal(
                "Value",
                targetMethod.ContainingType.Name);

            Assert.Single(
                targetMethod.ExplicitInterfaceImplementations);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that multiple runtime types inheriting one implementation
        /// create one target edge.
        /// </summary>
        [Fact]
        public void InheritedDeconstructImplementation_IsDeduplicated()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(BaseValue value)
                    {
                        (int left, int right) = value;
                    }
                }

                public class BaseValue
                {
                    public virtual void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class DerivedValue : BaseValue
                {
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                Assert.Single(
                    GetDeconstructEdges(run));

            Assert.Equal(
                "BaseValue",
                GetTargetMethod(edge).ContainingType.Name);
        }

        /// <summary>
        /// Ensures that foreach-variable deconstruction uses Roslyn's element
        /// type to restrict compatible runtime targets.
        /// </summary>
        [Fact]
        public void ForEachVariableDeconstruct_UsesElementReceiverType()
        {
            const string source =
                """
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(
                        IEnumerable<DerivedValue> values)
                    {
                        foreach ((int left, int right) in values)
                        {
                        }
                    }
                }

                public class BaseValue
                {
                    public virtual void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new ArgumentException();
                    }
                }

                public class DerivedValue : BaseValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedValue : DerivedValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new FormatException();
                    }
                }

                public sealed class SiblingValue : BaseValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
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
                GetDeconstructEdges(run)
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
                    "DerivedValue",
                    "MoreDerivedValue"
                },
                targetTypeNames);
        }

        /// <summary>
        /// Ensures that an implementation selected from dependency metadata
        /// is mapped back to its source declaration.
        /// </summary>
        [Fact]
        public void CrossCompilationInterfaceDeconstruct_UsesSourceImplementation()
        {
            const string dependencySource =
                """
                using System;

                namespace Dependency
                {
                    public interface IValue
                    {
                        void Deconstruct(
                            out int left,
                            out int right);
                    }

                    public sealed class Value : IValue
                    {
                        void IValue.Deconstruct(
                            out int left,
                            out int right)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
                """;

            const string consumerSource =
                """
                using Dependency;

                public static class EntryPoint
                {
                    public static void M(IValue value)
                    {
                        (int left, int right) = value;
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
                    GetDeconstructEdges(run));

            IMethodSymbol targetMethod =
                GetTargetMethod(edge);

            Assert.Equal(
                "Value",
                targetMethod.ContainingType.Name);

            Assert.False(
                targetMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a typed catch filter is attached to every alternative
        /// <c>Deconstruct</c> target edge.
        /// </summary>
        [Fact]
        public void TypedCatch_IsAppliedToEveryDeconstructDispatchEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(BaseValue value)
                    {
                        try
                        {
                            (int left, int right) = value;
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }

                public class BaseValue
                {
                    public virtual void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new ArgumentException();
                    }
                }

                public sealed class DerivedValue : BaseValue
                {
                    public override void Deconstruct(
                        out int left,
                        out int right)
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
                GetDeconstructEdges(run);

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
        /// Gets all root <c>Deconstruct</c> call edges.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <returns>The matching edges.</returns>
        private static ExceptionFlowSummaryCallEdge[] GetDeconstructEdges(
            ExceptionFlowSummaryGraphTestRun run)
        {
            return run.RootSummary.CallEdges
                .Where(
                    static edge =>
                        edge.CallSiteStep.Kind ==
                        ExceptionFlowPathStepKind.DeconstructCall)
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
                       "The deconstruction edge target was not a method.");
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
