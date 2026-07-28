using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests constructor initialization and instance member initializers in
    /// nonrecursive exception-flow summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphConstructorTests
    {
        /// <summary>
        /// Ensures that a <c>this(...)</c> initializer records both calls in
        /// its arguments and the delegated constructor itself.
        /// </summary>
        [Fact]
        public void ThisInitializer_RecordsArgumentCallAndConstructorEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public static void M(string? value)
                    {
                        _ = new TestClass(value);
                    }

                    public TestClass(string? value)
                        : this(Validate(value), 0)
                    {
                    }

                    private TestClass(string value, int marker)
                    {
                    }

                    private static string Validate(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                        return value;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary constructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            ExceptionFlowSummaryCallEdge validationEdge =
                Assert.Single(
                    constructorSummary.CallEdges.Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                                ExceptionFlowPathStepKind.MethodCall));

            Assert.Equal(
                "Validate",
                validationEdge.Target.Symbol.Name);

            ExceptionFlowSummaryCallEdge delegatedConstructorEdge =
                Assert.Single(
                    constructorSummary.CallEdges.Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                                ExceptionFlowPathStepKind.ConstructorCall));

            IMethodSymbol delegatedConstructor =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    delegatedConstructorEdge.Target.Symbol);

            Assert.Equal(
                2,
                delegatedConstructor.Parameters.Length);

            ExceptionFlowSummary validationSummary =
                run.GetRequiredSummary(
                    validationEdge.Target);

            ExceptionFlowSummarySource validationSource =
                Assert.Single(
                    validationSummary.Sources);

            Assert.Equal(
                "ArgumentNullException",
                validationSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that constructor-initializer arguments propagate value
        /// facts to the delegated constructor context.
        /// </summary>
        [Fact]
        public void ThisInitializer_PropagatesTargetParameterFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public static void M()
                    {
                        _ = new TestClass();
                    }

                    public TestClass()
                        : this("value")
                    {
                    }

                    private TestClass(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary delegatingConstructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            ExceptionFlowSummaryCallEdge delegatedConstructorEdge =
                Assert.Single(
                    delegatingConstructorSummary.CallEdges);

            ExceptionFlowSummary terminalConstructorSummary =
                run.GetRequiredSummary(
                    delegatedConstructorEdge.Target);

            Assert.Empty(
                terminalConstructorSummary.Sources);
        }

        /// <summary>
        /// Ensures that a <c>base(...)</c> initializer records both calls in
        /// its arguments and the selected base constructor.
        /// </summary>
        [Fact]
        public void BaseInitializer_RecordsArgumentCallAndConstructorEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(string? value)
                    {
                        _ = new Derived(value);
                    }
                }

                public class Base
                {
                    protected Base(string value)
                    {
                    }
                }

                public sealed class Derived : Base
                {
                    public Derived(string? value)
                        : base(Validate(value))
                    {
                    }

                    private static string Validate(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                        return value;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary constructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            ExceptionFlowSummaryCallEdge validationEdge =
                Assert.Single(
                    constructorSummary.CallEdges.Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                                ExceptionFlowPathStepKind.MethodCall));

            Assert.Equal(
                "Validate",
                validationEdge.Target.Symbol.Name);

            ExceptionFlowSummaryCallEdge baseConstructorEdge =
                Assert.Single(
                    constructorSummary.CallEdges.Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                                ExceptionFlowPathStepKind.ConstructorCall));

            Assert.Equal(
                "Base",
                baseConstructorEdge.Target.Symbol.ContainingType?.Name);
        }

        /// <summary>
        /// Ensures that a constructor without an explicit initializer records
        /// its implicit source-level base-constructor call.
        /// </summary>
        [Fact]
        public void ImplicitBaseInitializer_RecordsBaseConstructorEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        _ = new Derived();
                    }
                }

                public class Base
                {
                    protected Base()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class Derived : Base
                {
                    public Derived()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary derivedConstructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            ExceptionFlowSummaryCallEdge baseConstructorEdge =
                Assert.Single(
                    derivedConstructorSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.ConstructorCall,
                baseConstructorEdge.CallSiteStep.Kind);

            Assert.Equal(
                "Base",
                baseConstructorEdge.Target.Symbol.ContainingType?.Name);

            ExceptionFlowSummary baseConstructorSummary =
                run.GetRequiredSummary(
                    baseConstructorEdge.Target);

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    baseConstructorSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that an implicit base call can select a constructor whose
        /// parameters all have default values.
        /// </summary>
        [Fact]
        public void ImplicitBaseInitializer_SelectsOptionalConstructor()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        _ = new Derived();
                    }
                }

                public class Base
                {
                    protected Base(int value = 0)
                    {
                        if (value == 0)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                public sealed class Derived : Base
                {
                    public Derived()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary derivedConstructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            ExceptionFlowSummaryCallEdge baseConstructorEdge =
                Assert.Single(
                    derivedConstructorSummary.CallEdges);

            IMethodSymbol baseConstructor =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    baseConstructorEdge.Target.Symbol);

            Assert.Single(
                baseConstructor.Parameters);

            Assert.True(
                baseConstructor.Parameters[0].HasExplicitDefaultValue);

            ExceptionFlowSummary baseConstructorSummary =
                run.GetRequiredSummary(
                    baseConstructorEdge.Target);

            Assert.Single(
                baseConstructorSummary.Sources);
        }

        /// <summary>
        /// Ensures that an implicit base constructor remains analyzable when
        /// its base type declares only instance member initializers.
        /// </summary>
        [Fact]
        public void ImplicitBaseConstructor_AnalyzesBaseInitializers()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        _ = new Derived();
                    }
                }

                public class Base
                {
                    private readonly object field = CreateField();

                    private static object CreateField()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class Derived : Base
                {
                    public Derived()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary derivedConstructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            ExceptionFlowSummaryCallEdge baseConstructorEdge =
                Assert.Single(
                    derivedConstructorSummary.CallEdges);

            IMethodSymbol baseConstructor =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    baseConstructorEdge.Target.Symbol);

            Assert.True(
                baseConstructor.IsImplicitlyDeclared);

            ExceptionFlowSummary baseConstructorSummary =
                run.GetRequiredSummary(
                    baseConstructorEdge.Target);

            ExceptionFlowSummaryCallEdge initializerEdge =
                Assert.Single(
                    baseConstructorSummary.CallEdges);

            Assert.Equal(
                "CreateField",
                initializerEdge.Target.Symbol.Name);
        }

        /// <summary>
        /// Ensures that terminal constructors analyze instance field,
        /// event-field, and property initializers.
        /// </summary>
        [Fact]
        public void TerminalConstructor_AnalyzesInstanceMemberInitializers()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    private readonly object field = CreateField();
                    private event EventHandler? Changed = CreateHandler();
                    public object Property { get; } = CreateProperty();

                    public static void M()
                    {
                        _ = new TestClass();
                    }

                    public TestClass()
                    {
                    }

                    private static object CreateField()
                    {
                        throw new ArgumentException();
                    }

                    private static EventHandler CreateHandler()
                    {
                        throw new InvalidOperationException();
                    }

                    private static object CreateProperty()
                    {
                        throw new NotSupportedException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary constructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            string[] initializerTargets =
                constructorSummary.CallEdges
                    .Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                                ExceptionFlowPathStepKind.MethodCall)
                    .Select(
                        edge =>
                            edge.Target.Symbol.Name)
                    .OrderBy(
                        static name =>
                            name,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "CreateField",
                    "CreateHandler",
                    "CreateProperty"
                },
                initializerTargets);
        }

        /// <summary>
        /// Ensures that a constructor delegating through <c>this(...)</c>
        /// leaves instance member initializers to the terminal constructor.
        /// </summary>
        [Fact]
        public void ThisDelegatingConstructor_DoesNotDuplicateInitializers()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    private readonly object field = CreateField();

                    public static void M()
                    {
                        _ = new TestClass();
                    }

                    public TestClass()
                        : this(0)
                    {
                    }

                    private TestClass(int marker)
                    {
                    }

                    private static object CreateField()
                    {
                        throw new ArgumentException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary delegatingConstructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            Assert.DoesNotContain(
                delegatingConstructorSummary.CallEdges,
                edge =>
                    edge.CallSiteStep.Kind ==
                        ExceptionFlowPathStepKind.MethodCall);

            ExceptionFlowSummaryCallEdge delegatedConstructorEdge =
                Assert.Single(
                    delegatingConstructorSummary.CallEdges);

            ExceptionFlowSummary terminalConstructorSummary =
                run.GetRequiredSummary(
                    delegatedConstructorEdge.Target);

            ExceptionFlowSummaryCallEdge initializerEdge =
                Assert.Single(
                    terminalConstructorSummary.CallEdges.Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                                ExceptionFlowPathStepKind.MethodCall));

            Assert.Equal(
                "CreateField",
                initializerEdge.Target.Symbol.Name);
        }

        /// <summary>
        /// Ensures that source-declared implicit constructors execute instance
        /// member initializers.
        /// </summary>
        [Fact]
        public void ImplicitConstructor_AnalyzesInstanceInitializers()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        _ = new TestClass();
                    }
                }

                public sealed class TestClass
                {
                    private readonly object field = CreateField();

                    private static object CreateField()
                    {
                        throw new ArgumentException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge constructionEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            IMethodSymbol implicitConstructor =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    constructionEdge.Target.Symbol);

            Assert.True(
                implicitConstructor.IsImplicitlyDeclared);

            ExceptionFlowSummary constructorSummary =
                run.GetRequiredSummary(
                    constructionEdge.Target);

            Assert.True(
                constructorSummary.HasExecutableBody);

            ExceptionFlowSummaryCallEdge initializerEdge =
                Assert.Single(
                    constructorSummary.CallEdges);

            Assert.Equal(
                "CreateField",
                initializerEdge.Target.Symbol.Name);
        }

        /// <summary>
        /// Ensures that throw expressions in constructor-initializer arguments
        /// are retained as local exception sources.
        /// </summary>
        [Fact]
        public void ConstructorInitializerThrowExpression_AddsLocalSource()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public static void M(string? value)
                    {
                        _ = new TestClass(value);
                    }

                    public TestClass(string? value)
                        : this(
                            value ??
                            throw new ArgumentNullException(nameof(value)),
                            0)
                    {
                    }

                    private TestClass(string value, int marker)
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummary constructorSummary =
                GetSingleConstructedTypeSummary(
                    run);

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    constructorSummary.Sources);

            Assert.Equal(
                "ArgumentNullException",
                sourceEntry.ExceptionType.Name);

            Assert.Equal(
                ExceptionFlowPathStepKind.ExplicitThrow,
                sourceEntry.LocalPath.Steps[0].Kind);
        }

        /// <summary>
        /// Gets the summary targeted by the root method's single object
        /// creation.
        /// </summary>
        /// <param name="run">
        /// The completed graph test run.
        /// </param>
        /// <returns>
        /// The constructed type's constructor summary.
        /// </returns>
        private static ExceptionFlowSummary GetSingleConstructedTypeSummary(
            ExceptionFlowSummaryGraphTestRun run)
        {
            ExceptionFlowSummaryCallEdge constructionEdge =
                Assert.Single(
                    run.RootSummary.CallEdges.Where(
                        edge =>
                            edge.CallSiteStep.Kind ==
                                ExceptionFlowPathStepKind.ConstructorCall));

            return run.GetRequiredSummary(
                constructionEdge.Target);
        }
    }
}
