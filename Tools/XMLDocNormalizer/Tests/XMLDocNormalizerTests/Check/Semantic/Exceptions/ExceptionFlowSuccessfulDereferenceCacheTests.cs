using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the exact semantic boundary of successful-dereference
    /// memoization.
    /// </summary>
    public sealed class ExceptionFlowSuccessfulDereferenceCacheTests
    {
        private const string NormalSource =
            """
            #nullable enable

            public sealed class Holder
            {
                public object? Value { get; }
            }

            public static class TestClass
            {
                public static void M(Holder? first, Holder? second)
                {
                    _ = first.Value;
                    Use(first);
                    Use(second);
                }

                private static void Use(object? value)
                {
                }
            }
            """;

        private const string PropertySource =
            """
            #nullable enable

            public sealed class Holder
            {
                public object? Value { get; }
            }

            public static class TestClass
            {
                public static void M(Holder first, Holder second)
                {
                    _ = first.Value.ToString();
                    Use(first.Value);
                    Use(second.Value);
                }

                private static void Use(object? value)
                {
                }
            }
            """;

        /// <summary>
        /// Verifies that an identical normal query is stored once and returns
        /// exactly the uncached result on every request.
        /// </summary>
        [Fact]
        public void NormalQuery_RepeatedRequestReusesSingleEntryAndMatchesUncachedResult()
        {
            DereferenceTestContext context =
                CreateContext(NormalSource, "NormalRepeated");
            ExpressionSyntax expression = GetUseArguments(context)[0];
            ISymbol symbol = GetRequiredExpressionSymbol(context, expression);

            ExceptionFlowValueFacts uncached = InvokeFacts(
                "ComputeFactsProvenByPrecedingSuccessfulDereference",
                expression,
                symbol,
                context.SemanticModel);
            ExceptionFlowValueFacts first = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expression,
                symbol,
                context.SemanticModel);
            int entriesAfterFirst = GetCacheEntryCount(context.SemanticModel);
            ExceptionFlowValueFacts second = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expression,
                symbol,
                context.SemanticModel);

            Assert.Equal(ExceptionFlowValueFacts.NonNull, uncached);
            Assert.Equal(uncached, first);
            Assert.Equal(first, second);
            Assert.Equal(1, entriesAfterFirst);
            Assert.Equal(entriesAfterFirst, GetCacheEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Verifies that the Roslyn symbol is part of a normal-query cache key.
        /// </summary>
        [Fact]
        public void NormalQuery_DifferentSymbolsUseDistinctEntries()
        {
            DereferenceTestContext context =
                CreateContext(NormalSource, "NormalSymbols");
            ExpressionSyntax expression = GetUseArguments(context)[0];
            ISymbol firstSymbol = GetRequiredParameterSymbol(context, "first");
            ISymbol secondSymbol = GetRequiredParameterSymbol(context, "second");

            ExceptionFlowValueFacts first = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expression,
                firstSymbol,
                context.SemanticModel);
            ExceptionFlowValueFacts second = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expression,
                secondSymbol,
                context.SemanticModel);

            Assert.Equal(ExceptionFlowValueFacts.NonNull, first);
            Assert.Equal(ExceptionFlowValueFacts.None, second);
            Assert.Equal(2, GetCacheEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Verifies that semantically similar syntax in two semantic models of
        /// the same compilation uses separate weak cache partitions.
        /// </summary>
        [Fact]
        public void EquivalentSyntaxInDifferentSemanticModelsUsesDistinctPartitions()
        {
            SyntaxTree firstTree = CSharpSyntaxTree.ParseText(
                NormalSource.Replace("TestClass", "FirstTestClass", StringComparison.Ordinal),
                path: "First.cs");
            SyntaxTree secondTree = CSharpSyntaxTree.ParseText(
                NormalSource
                    .Replace("Holder", "SecondHolder", StringComparison.Ordinal)
                    .Replace("TestClass", "SecondTestClass", StringComparison.Ordinal),
                path: "Second.cs");
            CSharpCompilation compilation = CreateCompilation(
                "DifferentModels",
                firstTree,
                secondTree);
            DereferenceTestContext firstContext = CreateContext(compilation, firstTree);
            DereferenceTestContext secondContext = CreateContext(compilation, secondTree);

            InvokeNormalFactsForFirstUse(firstContext);
            InvokeNormalFactsForFirstUse(secondContext);

            Assert.NotSame(firstContext.SemanticModel, secondContext.SemanticModel);
            Assert.Equal(1, GetCacheEntryCount(firstContext.SemanticModel));
            Assert.Equal(1, GetCacheEntryCount(secondContext.SemanticModel));
        }

        /// <summary>
        /// Verifies that the same syntax-tree object in different compilations
        /// cannot reuse a cache partition.
        /// </summary>
        [Fact]
        public void SharedSyntaxTreeInDifferentCompilationsUsesDistinctPartitions()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(NormalSource, path: "Shared.cs");
            CSharpCompilation firstCompilation = CreateCompilation("FirstCompilation", tree);
            CSharpCompilation secondCompilation = CreateCompilation("SecondCompilation", tree);
            DereferenceTestContext firstContext = CreateContext(firstCompilation, tree);
            DereferenceTestContext secondContext = CreateContext(secondCompilation, tree);
            ExpressionSyntax expression = GetUseArguments(firstContext)[0];
            ISymbol firstCompilationSymbol =
                GetRequiredExpressionSymbol(firstContext, expression);

            ExceptionFlowValueFacts first = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expression,
                firstCompilationSymbol,
                firstContext.SemanticModel);
            ExceptionFlowValueFacts second = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expression,
                firstCompilationSymbol,
                secondContext.SemanticModel);

            Assert.Equal(ExceptionFlowValueFacts.NonNull, first);
            Assert.Equal(ExceptionFlowValueFacts.None, second);
            Assert.Equal(1, GetCacheEntryCount(firstContext.SemanticModel));
            Assert.Equal(1, GetCacheEntryCount(secondContext.SemanticModel));
        }

        /// <summary>
        /// Verifies that an identical stable-property query is stored once and
        /// returns exactly the uncached receiver-sensitive result.
        /// </summary>
        [Fact]
        public void StablePropertyQuery_RepeatedRequestReusesSingleEntryAndMatchesUncachedResult()
        {
            DereferenceTestContext context =
                CreateContext(PropertySource, "PropertyRepeated");
            ExpressionSyntax expression = GetUseArguments(context)[0];
            IPropertySymbol property = GetRequiredPropertySymbol(context, expression);

            ExceptionFlowValueFacts uncached = InvokeFacts(
                "ComputeFactsProvenByPrecedingSuccessfulStablePropertyDereference",
                expression,
                property,
                context.SemanticModel);
            ExceptionFlowValueFacts first = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulStablePropertyDereference",
                expression,
                property,
                context.SemanticModel);
            int entriesAfterFirst = GetCacheEntryCount(context.SemanticModel);
            ExceptionFlowValueFacts second = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulStablePropertyDereference",
                expression,
                property,
                context.SemanticModel);

            Assert.Equal(ExceptionFlowValueFacts.NonNull, uncached);
            Assert.Equal(uncached, first);
            Assert.Equal(first, second);
            Assert.Equal(1, entriesAfterFirst);
            Assert.Equal(entriesAfterFirst, GetCacheEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Verifies receiver sensitivity and explicit separation of normal and
        /// stable-property query modes.
        /// </summary>
        [Fact]
        public void StablePropertyQuery_DifferentReceiversAndModesUseDistinctEntries()
        {
            DereferenceTestContext context =
                CreateContext(PropertySource, "PropertyReceivers");
            ExpressionSyntax[] expressions = GetUseArguments(context);
            IPropertySymbol property = GetRequiredPropertySymbol(context, expressions[0]);

            ExceptionFlowValueFacts firstReceiver = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulStablePropertyDereference",
                expressions[0],
                property,
                context.SemanticModel);
            ExceptionFlowValueFacts secondReceiver = InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulStablePropertyDereference",
                expressions[1],
                property,
                context.SemanticModel);
            InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expressions[0],
                property,
                context.SemanticModel);

            Assert.Equal(ExceptionFlowValueFacts.NonNull, firstReceiver);
            Assert.Equal(ExceptionFlowValueFacts.None, secondReceiver);
            Assert.Equal(3, GetCacheEntryCount(context.SemanticModel));
        }

        /// <summary>
        /// Differentially verifies cached and uncached results across writes,
        /// different write targets, if/else, a loop, nested control flow, and
        /// both short-circuit operators.
        /// </summary>
        [Fact]
        public void NormalQuery_WritesAndControlFlowMatchUncachedResults()
        {
            const string source =
                """
                #nullable enable

                public sealed class Holder
                {
                    public object? Value { get; }
                }

                public static class TestClass
                {
                    public static void M(bool condition, Holder? first, Holder? second)
                    {
                        _ = first.Value;
                        second = null;

                        if (condition && first.Value != null)
                        {
                            Use(first);
                        }
                        else
                        {
                            if (first.Value != null || condition)
                            {
                                Use(first);
                            }
                        }

                        while (condition)
                        {
                            Use(first);
                            break;
                        }

                        first = null;
                        Use(first);
                    }

                    private static void Use(object? value)
                    {
                    }
                }
                """;

            DereferenceTestContext context =
                CreateContext(source, "WritesAndControlFlow");
            ISymbol symbol = GetRequiredParameterSymbol(context, "first");
            ExpressionSyntax[] expressions = GetUseArguments(context);

            foreach (ExpressionSyntax expression in expressions)
            {
                ExceptionFlowValueFacts uncached = InvokeFacts(
                    "ComputeFactsProvenByPrecedingSuccessfulDereference",
                    expression,
                    symbol,
                    context.SemanticModel);
                ExceptionFlowValueFacts cached = InvokeFacts(
                    "GetFactsProvenByPrecedingSuccessfulDereference",
                    expression,
                    symbol,
                    context.SemanticModel);

                Assert.Equal(uncached, cached);
            }

            Assert.Equal(expressions.Length, GetCacheEntryCount(context.SemanticModel));
            Assert.Equal(ExceptionFlowValueFacts.None, InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expressions[^1],
                symbol,
                context.SemanticModel));
        }

        private static void InvokeNormalFactsForFirstUse(
            DereferenceTestContext context)
        {
            ExpressionSyntax expression = GetUseArguments(context)[0];
            ISymbol symbol = GetRequiredExpressionSymbol(context, expression);
            InvokeFacts(
                "GetFactsProvenByPrecedingSuccessfulDereference",
                expression,
                symbol,
                context.SemanticModel);
        }

        private static ExceptionFlowValueFacts InvokeFacts(
            string methodName,
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            MethodInfo method = typeof(ExceptionFlowAnalyzer).GetMethod(
                                    methodName,
                                    BindingFlags.NonPublic | BindingFlags.Static) ??
                                throw new InvalidOperationException(
                                    $"Could not resolve {methodName}.");

            object? result = method.Invoke(
                obj: null,
                parameters:
                [
                    expression,
                    symbol,
                    semanticModel
                ]);

            return Assert.IsType<ExceptionFlowValueFacts>(result);
        }

        private static int GetCacheEntryCount(SemanticModel semanticModel)
        {
            FieldInfo field = typeof(ExceptionFlowAnalyzer).GetField(
                                  "successfulDereferenceCaches",
                                  BindingFlags.NonPublic | BindingFlags.Static) ??
                              throw new InvalidOperationException(
                                  "Could not resolve the successful-dereference cache.");
            object table = field.GetValue(obj: null) ??
                           throw new InvalidOperationException(
                               "The successful-dereference cache is null.");
            MethodInfo tryGetValue = table.GetType().GetMethod("TryGetValue") ??
                                     throw new InvalidOperationException(
                                         "Could not resolve cache partition lookup.");
            object?[] arguments =
            [
                semanticModel,
                null
            ];
            bool found = Assert.IsType<bool>(tryGetValue.Invoke(table, arguments));

            if (!found || arguments[1] == null)
            {
                return 0;
            }

            object partition = arguments[1]!;
            FieldInfo entriesField = partition.GetType().GetField(
                                         "entries",
                                         BindingFlags.NonPublic | BindingFlags.Instance) ??
                                     throw new InvalidOperationException(
                                         "Could not resolve cache entries.");
            object entries = entriesField.GetValue(partition) ??
                             throw new InvalidOperationException(
                                 "The cache entries are null.");
            PropertyInfo countProperty = entries.GetType().GetProperty("Count") ??
                                         throw new InvalidOperationException(
                                             "Could not resolve cache entry count.");

            return Assert.IsType<int>(countProperty.GetValue(entries));
        }

        private static ExpressionSyntax[] GetUseArguments(
            DereferenceTestContext context)
        {
            return context.Method.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(
                    static invocation =>
                        invocation.Expression is IdentifierNameSyntax identifier
                        && identifier.Identifier.ValueText == "Use")
                .Select(
                    static invocation =>
                        invocation.ArgumentList.Arguments.Single().Expression)
                .ToArray();
        }

        private static ISymbol GetRequiredExpressionSymbol(
            DereferenceTestContext context,
            ExpressionSyntax expression)
        {
            return context.SemanticModel.GetSymbolInfo(expression).Symbol ??
                   throw new InvalidOperationException(
                       "Could not resolve the expression symbol.");
        }

        private static IPropertySymbol GetRequiredPropertySymbol(
            DereferenceTestContext context,
            ExpressionSyntax expression)
        {
            return context.SemanticModel.GetSymbolInfo(expression).Symbol
                       as IPropertySymbol ??
                   throw new InvalidOperationException(
                       "Could not resolve the property symbol.");
        }

        private static IParameterSymbol GetRequiredParameterSymbol(
            DereferenceTestContext context,
            string name)
        {
            ParameterSyntax parameter = context.Method.ParameterList.Parameters
                .Single(candidate => candidate.Identifier.ValueText == name);

            return context.SemanticModel.GetDeclaredSymbol(parameter) ??
                   throw new InvalidOperationException(
                       $"Could not resolve parameter '{name}'.");
        }

        private static DereferenceTestContext CreateContext(
            string source,
            string assemblyName)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                source,
                path: assemblyName + ".cs");
            CSharpCompilation compilation = CreateCompilation(assemblyName, tree);
            return CreateContext(compilation, tree);
        }

        private static DereferenceTestContext CreateContext(
            CSharpCompilation compilation,
            SyntaxTree tree)
        {
            Diagnostic[] errors = compilation.GetDiagnostics()
                .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (errors.Length > 0)
            {
                throw new InvalidOperationException(
                    "The cache test source did not compile:" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, errors.Select(static error => error.ToString())));
            }

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            MethodDeclarationSyntax method = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single(candidate => candidate.Identifier.ValueText == "M");

            return new DereferenceTestContext(
                compilation.GetSemanticModel(tree),
                method);
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

        private sealed record DereferenceTestContext(
            SemanticModel SemanticModel,
            MethodDeclarationSyntax Method);
    }
}
