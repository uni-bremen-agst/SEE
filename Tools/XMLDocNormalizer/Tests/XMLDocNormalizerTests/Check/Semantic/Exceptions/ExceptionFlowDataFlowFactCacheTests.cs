using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the exact semantic and lifetime boundaries of cached Roslyn
    /// data-flow facts.
    /// </summary>
    public sealed class ExceptionFlowDataFlowFactCacheTests
    {
        private const string Source =
            """
            #nullable enable

            public static class TestClass
            {
                public static void M(int parameter)
                {
                    int local = parameter;
                    local = parameter + 1;
                    Use(local);
                }

                private static void Use(int value)
                {
                }
            }
            """;

        /// <summary>
        /// Verifies that repeated requests for the same semantic model, exact
        /// syntax object, and overload kind compute only once.
        /// </summary>
        [Fact]
        public void IdenticalStatementRequestComputesOnce()
        {
            DataFlowTestContext context = CreateContext(Source, "Repeated");
            StatementSyntax statement = GetAssignmentStatement(context);
            CalculatorProbe probe = new();
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(probe.Calculate);

            ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts first =
                cache.GetFacts(statement, context.SemanticModel);
            ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts second =
                cache.GetFacts(statement, context.SemanticModel);

            Assert.True(first.Succeeded);
            Assert.Equal(first.Succeeded, second.Succeeded);
            Assert.Equal(first.WrittenInside, second.WrittenInside);
            Assert.Equal(1, probe.CalculationCount);
            Assert.Equal(1, cache.GetEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Verifies that different syntax objects in one semantic model use
        /// distinct entries even when both select the statement overload.
        /// </summary>
        [Fact]
        public void DifferentStatementRegionsUseDistinctEntries()
        {
            DataFlowTestContext context = CreateContext(Source, "Regions");
            StatementSyntax[] statements = context.Method.Body!.Statements.ToArray();
            CalculatorProbe probe = new();
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(probe.Calculate);

            cache.GetFacts(statements[0], context.SemanticModel);
            cache.GetFacts(statements[1], context.SemanticModel);
            cache.GetFacts(statements[0], context.SemanticModel);

            Assert.Equal(2, probe.CalculationCount);
            Assert.Equal(2, cache.GetEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Verifies that statement and expression overloads are represented by
        /// separate exact region kinds.
        /// </summary>
        [Fact]
        public void StatementAndExpressionRegionsUseDistinctKinds()
        {
            DataFlowTestContext context = CreateContext(Source, "Kinds");
            ExpressionStatementSyntax statement =
                (ExpressionStatementSyntax)GetAssignmentStatement(context);
            ExpressionSyntax expression = statement.Expression;
            List<ExceptionFlowAnalyzer.DataFlowRegionKind> kinds = new();
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(
                    (key, _) =>
                    {
                        kinds.Add(key.Kind);
                        return SuccessfulFacts();
                    });

            cache.GetFacts(statement, context.SemanticModel);
            cache.GetFacts(expression, context.SemanticModel);

            Assert.Equal(
                new[]
                {
                    ExceptionFlowAnalyzer.DataFlowRegionKind.Statement,
                    ExceptionFlowAnalyzer.DataFlowRegionKind.Expression
                },
                kinds);
            Assert.Equal(2, cache.GetEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Verifies that identical source text in distinct syntax trees and
        /// semantic models cannot share entries.
        /// </summary>
        [Fact]
        public void IdenticalTextInDifferentSyntaxTreesUsesDistinctPartitions()
        {
            SyntaxTree firstTree = CSharpSyntaxTree.ParseText(Source, path: "First.cs");
            SyntaxTree secondTree = CSharpSyntaxTree.ParseText(
                Source.Replace("TestClass", "SecondTestClass", StringComparison.Ordinal),
                path: "Second.cs");
            CSharpCompilation compilation = CreateCompilation(
                "DifferentTrees",
                firstTree,
                secondTree);
            DataFlowTestContext firstContext = CreateContext(compilation, firstTree);
            DataFlowTestContext secondContext = CreateContext(compilation, secondTree);
            CalculatorProbe probe = new();
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(probe.Calculate);

            cache.GetFacts(
                GetAssignmentStatement(firstContext),
                firstContext.SemanticModel);
            cache.GetFacts(
                GetAssignmentStatement(secondContext),
                secondContext.SemanticModel);

            Assert.Equal(2, probe.CalculationCount);
            Assert.Equal(1, cache.GetEntryCount(firstContext.SemanticModel));
            Assert.Equal(1, cache.GetEntryCount(secondContext.SemanticModel));
        }

        /// <summary>
        /// Verifies that the same syntax-tree object used by different
        /// compilations receives distinct semantic-model partitions.
        /// </summary>
        [Fact]
        public void SharedSyntaxTreeInDifferentCompilationsUsesDistinctPartitions()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(Source, path: "Shared.cs");
            CSharpCompilation firstCompilation =
                CreateCompilation("FirstCompilation", tree);
            CSharpCompilation secondCompilation =
                CreateCompilation("SecondCompilation", tree);
            DataFlowTestContext firstContext = CreateContext(firstCompilation, tree);
            DataFlowTestContext secondContext = CreateContext(secondCompilation, tree);
            StatementSyntax statement = GetAssignmentStatement(firstContext);
            List<SemanticModel> calculatedModels = new();
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(
                    (_, semanticModel) =>
                    {
                        calculatedModels.Add(semanticModel);
                        return SuccessfulFacts();
                    });

            cache.GetFacts(statement, firstContext.SemanticModel);
            cache.GetFacts(statement, secondContext.SemanticModel);

            Assert.Equal(2, calculatedModels.Count);
            Assert.Same(firstContext.SemanticModel, calculatedModels[0]);
            Assert.Same(secondContext.SemanticModel, calculatedModels[1]);
            Assert.Equal(1, cache.GetEntryCount(firstContext.SemanticModel));
            Assert.Equal(1, cache.GetEntryCount(secondContext.SemanticModel));
        }

        /// <summary>
        /// Differentially verifies successful statement and expression
        /// snapshots against direct Roslyn data-flow results, including symbol
        /// identity and enumeration order.
        /// </summary>
        [Fact]
        public void CachedFactsMatchDirectRoslynFacts()
        {
            DataFlowTestContext context = CreateContext(Source, "Differential");
            ExpressionStatementSyntax statement =
                (ExpressionStatementSyntax)GetAssignmentStatement(context);
            ExpressionSyntax expression = statement.Expression;
            DataFlowAnalysis? directStatement =
                context.SemanticModel.AnalyzeDataFlow(statement);
            DataFlowAnalysis? directExpression =
                context.SemanticModel.AnalyzeDataFlow(expression);

            ExceptionFlowAnalyzer.DataFlowFactCache cache = new();
            ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts cachedStatement =
                cache.GetFacts(statement, context.SemanticModel);
            ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts cachedExpression =
                cache.GetFacts(expression, context.SemanticModel);

            Assert.NotNull(directStatement);
            Assert.NotNull(directExpression);
            Assert.Equal(directStatement.Succeeded, cachedStatement.Succeeded);
            Assert.Equal(directExpression.Succeeded, cachedExpression.Succeeded);
            AssertSymbolsEqual(
                directStatement.WrittenInside,
                cachedStatement.WrittenInside);
            AssertSymbolsEqual(
                directExpression.WrittenInside,
                cachedExpression.WrittenInside);
        }

        /// <summary>
        /// Verifies that an unsuccessful result is reproduced from the cache
        /// without pretending that analysis succeeded.
        /// </summary>
        [Fact]
        public void UnsuccessfulResultIsCachedExactly()
        {
            DataFlowTestContext context = CreateContext(Source, "Unsuccessful");
            StatementSyntax statement = GetAssignmentStatement(context);
            int calculations = 0;
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(
                    (_, _) =>
                    {
                        calculations++;
                        return ExceptionFlowAnalyzer
                            .ExceptionFlowDataFlowFacts
                            .Unsuccessful;
                    });

            ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts first =
                cache.GetFacts(statement, context.SemanticModel);
            ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts second =
                cache.GetFacts(statement, context.SemanticModel);

            Assert.False(first.Succeeded);
            Assert.False(second.Succeeded);
            Assert.Empty(first.WrittenInside);
            Assert.Empty(second.WrittenInside);
            Assert.Equal(1, calculations);
        }

        /// <summary>
        /// Verifies that calculator exceptions are propagated and are not
        /// stored as cache values.
        /// </summary>
        [Fact]
        public void CalculatorExceptionIsNotCached()
        {
            DataFlowTestContext context = CreateContext(Source, "Exception");
            StatementSyntax statement = GetAssignmentStatement(context);
            int calculations = 0;
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(
                    (_, _) =>
                    {
                        calculations++;
                        if (calculations == 1)
                        {
                            throw new InvalidOperationException("Expected test exception.");
                        }

                        return SuccessfulFacts();
                    });

            Assert.Throws<InvalidOperationException>(
                () => cache.GetFacts(statement, context.SemanticModel));
            ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts facts =
                cache.GetFacts(statement, context.SemanticModel);

            Assert.True(facts.Succeeded);
            Assert.Equal(2, calculations);
            Assert.Equal(1, cache.GetEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Verifies that concurrent identical requests perform one atomic
        /// calculation and return the same immutable facts.
        /// </summary>
        [Fact]
        public async Task ConcurrentIdenticalRequestsComputeOnce()
        {
            DataFlowTestContext context = CreateContext(Source, "Concurrent");
            StatementSyntax statement = GetAssignmentStatement(context);
            CalculatorProbe probe = new(delayMilliseconds: 25);
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(probe.Calculate);

            Task<ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts>[] tasks =
                Enumerable.Range(0, 8)
                    .Select(
                        _ => Task.Run(
                            () => cache.GetFacts(
                                statement,
                                context.SemanticModel)))
                    .ToArray();

            ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts[] results =
                await Task.WhenAll(tasks);

            Assert.All(results, static result => Assert.True(result.Succeeded));
            Assert.Equal(1, probe.CalculationCount);
            Assert.Equal(1, cache.GetEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Verifies that a live cache does not keep its semantic-model key or
        /// compilation alive through a weak partition and cached symbols.
        /// </summary>
        [Fact]
        public void CacheDoesNotRetainSemanticModelOrCompilation()
        {
            ExceptionFlowAnalyzer.DataFlowFactCache cache =
                new(CreateRealFacts);
            WeakDataFlowReferences references =
                CreateCachedWeakReferences(cache);

            for (int attempt = 0;
                 attempt < 8
                    && (references.SemanticModel.IsAlive
                        || references.Compilation.IsAlive);
                 attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            GC.KeepAlive(cache);
            Assert.False(references.SemanticModel.IsAlive);
            Assert.False(references.Compilation.IsAlive);
        }

        private static ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts SuccessfulFacts()
        {
            return new ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts(
                succeeded: true,
                ImmutableArray<ISymbol>.Empty);
        }

        private static ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts CreateRealFacts(
            ExceptionFlowAnalyzer.DataFlowRegionKey key,
            SemanticModel semanticModel)
        {
            DataFlowAnalysis? analysis =
                semanticModel.AnalyzeDataFlow((StatementSyntax)key.Region);

            if (analysis?.Succeeded != true)
            {
                return ExceptionFlowAnalyzer
                    .ExceptionFlowDataFlowFacts
                    .Unsuccessful;
            }

            return new ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts(
                succeeded: true,
                analysis.WrittenInside.ToImmutableArray());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakDataFlowReferences CreateCachedWeakReferences(
            ExceptionFlowAnalyzer.DataFlowFactCache cache)
        {
            DataFlowTestContext context = CreateContext(Source, "Lifetime");
            StatementSyntax statement = GetAssignmentStatement(context);
            cache.GetFacts(statement, context.SemanticModel);

            return new WeakDataFlowReferences(
                new WeakReference(context.SemanticModel),
                new WeakReference(context.SemanticModel.Compilation));
        }

        private static void AssertSymbolsEqual(
            IEnumerable<ISymbol> expected,
            ImmutableArray<ISymbol> actual)
        {
            ISymbol[] expectedArray = expected.ToArray();
            Assert.Equal(expectedArray.Length, actual.Length);

            for (int index = 0; index < expectedArray.Length; index++)
            {
                Assert.Same(expectedArray[index], actual[index]);
            }
        }

        private static StatementSyntax GetAssignmentStatement(
            DataFlowTestContext context)
        {
            return context.Method.Body!.Statements[1];
        }

        private static DataFlowTestContext CreateContext(
            string source,
            string assemblyName)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                source,
                path: $"{assemblyName}.cs");
            CSharpCompilation compilation = CreateCompilation(assemblyName, tree);
            return CreateContext(compilation, tree);
        }

        private static DataFlowTestContext CreateContext(
            CSharpCompilation compilation,
            SyntaxTree tree)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            MethodDeclarationSyntax method = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single(declaration => declaration.Identifier.ValueText == "M");

            return new DataFlowTestContext(semanticModel, method);
        }

        private static CSharpCompilation CreateCompilation(
            string assemblyName,
            params SyntaxTree[] trees)
        {
            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: trees,
                references: MetadataReferences.Default,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));
        }

        private sealed class CalculatorProbe
        {
            private readonly int delayMilliseconds;
            private int calculationCount;

            internal CalculatorProbe(int delayMilliseconds = 0)
            {
                this.delayMilliseconds = delayMilliseconds;
            }

            internal int CalculationCount => Volatile.Read(ref calculationCount);

            internal ExceptionFlowAnalyzer.ExceptionFlowDataFlowFacts Calculate(
                ExceptionFlowAnalyzer.DataFlowRegionKey key,
                SemanticModel semanticModel)
            {
                Interlocked.Increment(ref calculationCount);
                if (delayMilliseconds > 0)
                {
                    Thread.Sleep(delayMilliseconds);
                }

                Assert.NotNull(key.Region);
                Assert.NotNull(semanticModel);
                return SuccessfulFacts();
            }
        }

        private sealed record DataFlowTestContext(
            SemanticModel SemanticModel,
            MethodDeclarationSyntax Method);

        private sealed record WeakDataFlowReferences(
            WeakReference SemanticModel,
            WeakReference Compilation);
    }
}
