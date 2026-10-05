using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Protects the semantic boundaries of sequence-range and dictionary-
    /// mutation facts extracted from <see cref="ExceptionFlowAnalyzer"/>.
    /// </summary>
    public sealed class ExceptionFlowSequenceRangeDictionaryFactOwnershipTests
    {
        /// <summary>
        /// Ensures framework dictionary/list shapes are recognized while
        /// lookalike invocations and unrelated arguments are rejected.
        /// </summary>
        [Fact]
        public void CollectionFacts_RecognizeOnlySupportedFrameworkShapes()
        {
            const string source =
                """
                using System.Collections.Generic;

                public sealed class Lookalike
                {
                    public bool TryGetValue(string key, out List<string> value)
                    {
                        value = new();
                        return false;
                    }
                }

                public static class TestClass
                {
                    public static void M(
                        Dictionary<string, List<string>> dictionary,
                        List<string> target,
                        List<string> source,
                        Lookalike lookalike)
                    {
                        dictionary.TryGetValue("x", out List<string>? value);
                        lookalike.TryGetValue("x", out value);
                        target.AddRange(source);
                        target.Add(source[0]);
                    }
                }
                """;
            SemanticFixture fixture = CreateFixture(source);
            InvocationExpressionSyntax[] invocations =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .ToArray();
            InvocationExpressionSyntax dictionaryTryGetValue =
                invocations[0];
            InvocationExpressionSyntax lookalikeTryGetValue =
                invocations[1];
            InvocationExpressionSyntax addRange = invocations[2];
            InvocationExpressionSyntax add = invocations[3];
            IParameterSymbol dictionaryParameter =
                GetParameter(fixture, "M", "dictionary");
            IParameterSymbol targetParameter =
                GetParameter(fixture, "M", "target");

            Assert.True(
                ExceptionFlowSequenceCollectionFactsProvider
                    .TryGetDictionaryReceiverFromTryGetValue(
                        dictionaryTryGetValue,
                        fixture.Model,
                        out ExpressionSyntax? receiver,
                        out IMethodSymbol? method));
            Assert.Equal("dictionary", receiver?.ToString());
            Assert.Equal("TryGetValue", method?.Name);
            Assert.False(
                ExceptionFlowSequenceCollectionFactsProvider
                    .TryGetDictionaryReceiverFromTryGetValue(
                        lookalikeTryGetValue,
                        fixture.Model,
                        out _,
                        out _));
            Assert.True(
                ExceptionFlowSequenceCollectionFactsProvider
                    .IsDictionaryOfListsType(dictionaryParameter.Type));
            Assert.False(
                ExceptionFlowSequenceCollectionFactsProvider
                    .IsDictionaryOfListsType(targetParameter.Type));
            Assert.True(
                ExceptionFlowSequenceCollectionFactsProvider
                    .IsListAddRangeSourceArgument(
                        Assert.Single(addRange.ArgumentList.Arguments),
                        addRange,
                        fixture.Model));
            Assert.False(
                ExceptionFlowSequenceCollectionFactsProvider
                    .IsListAddRangeSourceArgument(
                        Assert.Single(add.ArgumentList.Arguments),
                        addRange,
                        fixture.Model));
        }

        /// <summary>
        /// Ensures successful guards and untouched out values are accepted,
        /// while negated guards and intervening references are rejected.
        /// </summary>
        [Fact]
        public void GuardAndPreservationFacts_RespectBranchAndUseBoundaries()
        {
            const string source =
                """
                using System.Collections.Generic;

                public static class TestClass
                {
                    public static void M(
                        Dictionary<string, List<string>> dictionary,
                        bool flag)
                    {
                        if (dictionary.TryGetValue("a", out List<string>? first) && flag)
                        {
                            Consume(first);
                        }

                        if (dictionary.TryGetValue("b", out List<string>? second) && flag)
                        {
                            _ = second.Count;
                            Consume(second);
                        }

                        if (!dictionary.TryGetValue("c", out List<string>? third))
                        {
                            Consume(third);
                        }
                    }

                    private static void Consume(List<string>? value)
                    {
                    }
                }
                """;
            SemanticFixture fixture = CreateFixture(source);
            InvocationExpressionSyntax[] tryGetValues =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(
                        static invocation =>
                            invocation.Expression.ToString()
                                .EndsWith(
                                    ".TryGetValue",
                                    StringComparison.Ordinal))
                    .ToArray();
            IdentifierNameSyntax[] uses =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(
                        static invocation =>
                            invocation.Expression.ToString() == "Consume")
                    .Select(
                        static invocation =>
                            Assert.IsType<IdentifierNameSyntax>(
                                Assert.Single(invocation.ArgumentList.Arguments)
                                    .Expression))
                    .ToArray();
            ILocalSymbol first =
                Assert.IsAssignableFrom<ILocalSymbol>(
                    fixture.Model.GetSymbolInfo(uses[0]).Symbol);
            ILocalSymbol second =
                Assert.IsAssignableFrom<ILocalSymbol>(
                    fixture.Model.GetSymbolInfo(uses[1]).Symbol);

            Assert.True(
                ExceptionFlowGuardFactsProvider
                    .IsUseGuardedBySuccessfulTryGetValue(
                        uses[0],
                        tryGetValues[0]));
            Assert.False(
                ExceptionFlowGuardFactsProvider
                    .IsUseGuardedBySuccessfulTryGetValue(
                        uses[2],
                        tryGetValues[2]));
            Assert.True(
                ExceptionFlowSequenceContentPreservationFactsProvider
                    .DoesOutSequenceRemainUnchangedBeforeUse(
                        uses[0],
                        first,
                        tryGetValues[0],
                        fixture.Model));
            Assert.False(
                ExceptionFlowSequenceContentPreservationFactsProvider
                    .DoesOutSequenceRemainUnchangedBeforeUse(
                        uses[1],
                        second,
                        tryGetValues[1],
                        fixture.Model));
        }

        /// <summary>
        /// Ensures supported property ownership, out-symbol resolution, and
        /// same-property assignment targets are classified exactly.
        /// </summary>
        [Fact]
        public void PropertyAndSymbolFacts_ClassifySupportedOwnership()
        {
            const string source =
                """
                using System.Collections.Generic;

                public static class TestClass
                {
                    private sealed class Prepared
                    {
                        public Dictionary<string, List<string>> Values { get; } = new();
                        public Dictionary<string, List<string>> Mutable { get; set; } = new();
                        public Dictionary<string, object> WrongValue { get; } = new();
                    }

                    public static void M()
                    {
                        Prepared prepared = new();
                        List<string>? existing = null;
                        prepared.Values.TryGetValue("x", out List<string>? declared);
                        prepared.Values.TryGetValue("x", out existing);
                        prepared.Values["x"] = declared;
                        prepared.Mutable["x"] = declared;
                    }
                }
                """;
            SemanticFixture fixture = CreateFixture(source);
            PropertyDeclarationSyntax[] properties =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .ToArray();
            IPropertySymbol valuesProperty =
                GetProperty(fixture, properties, "Values");
            IPropertySymbol mutableProperty =
                GetProperty(fixture, properties, "Mutable");
            IPropertySymbol wrongValueProperty =
                GetProperty(fixture, properties, "WrongValue");
            ArgumentSyntax[] outArguments =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ArgumentSyntax>()
                    .Where(
                        static argument =>
                            argument.RefKindKeyword.IsKind(
                                SyntaxKind.OutKeyword))
                    .ToArray();
            AssignmentExpressionSyntax[] assignments =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<AssignmentExpressionSyntax>()
                    .ToArray();

            Assert.True(
                ExceptionFlowSequenceContentPreservationFactsProvider
                    .IsSupportedDictionarySequenceProperty(
                        valuesProperty,
                        fixture.Model,
                        out PropertyDeclarationSyntax? declaration,
                        out SemanticModel? declarationModel));
            Assert.Equal("Values", declaration?.Identifier.ValueText);
            Assert.Same(fixture.Model, declarationModel);
            Assert.False(
                ExceptionFlowSequenceContentPreservationFactsProvider
                    .IsSupportedDictionarySequenceProperty(
                        mutableProperty,
                        fixture.Model,
                        out _,
                        out _));
            Assert.False(
                ExceptionFlowSequenceContentPreservationFactsProvider
                    .IsSupportedDictionarySequenceProperty(
                        wrongValueProperty,
                        fixture.Model,
                        out _,
                        out _));
            Assert.Equal(
                "declared",
                ExceptionFlowSymbolUsageFacts
                    .GetOutArgumentSymbol(outArguments[0], fixture.Model)
                    ?.Name);
            Assert.Equal(
                "existing",
                ExceptionFlowSymbolUsageFacts
                    .GetOutArgumentSymbol(outArguments[1], fixture.Model)
                    ?.Name);
            Assert.True(
                ExceptionFlowSymbolUsageFacts
                    .AssignmentTargetsDictionaryProperty(
                        assignments[0].Left,
                        valuesProperty,
                        fixture.Model));
            Assert.False(
                ExceptionFlowSymbolUsageFacts
                    .AssignmentTargetsDictionaryProperty(
                        assignments[1].Left,
                        valuesProperty,
                        fixture.Model));
        }

        /// <summary>
        /// Creates a semantic fixture for one source snippet.
        /// </summary>
        /// <param name="source">The source to compile.</param>
        /// <returns>The syntax tree and semantic model.</returns>
        private static SemanticFixture CreateFixture(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            CSharpCompilation compilation =
                CSharpCompilation.Create(
                    "SequenceRangeDictionaryFactOwnershipTests",
                    [tree],
                    MetadataReferences.Default,
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        nullableContextOptions:
                            NullableContextOptions.Enable));
            Diagnostic[] errors =
                compilation.GetDiagnostics()
                    .Where(
                        static diagnostic =>
                            diagnostic.Severity ==
                                DiagnosticSeverity.Error)
                    .ToArray();

            Assert.Empty(errors);

            return new SemanticFixture(
                tree,
                compilation.GetSemanticModel(tree));
        }

        /// <summary>
        /// Gets a parameter from a uniquely named method.
        /// </summary>
        private static IParameterSymbol GetParameter(
            SemanticFixture fixture,
            string methodName,
            string parameterName)
        {
            MethodDeclarationSyntax method =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single(
                        method =>
                            method.Identifier.ValueText == methodName);
            IMethodSymbol methodSymbol =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    fixture.Model.GetDeclaredSymbol(method));

            return methodSymbol.Parameters.Single(
                parameter => parameter.Name == parameterName);
        }

        /// <summary>
        /// Gets a property symbol by declaration name.
        /// </summary>
        private static IPropertySymbol GetProperty(
            SemanticFixture fixture,
            IEnumerable<PropertyDeclarationSyntax> properties,
            string name)
        {
            PropertyDeclarationSyntax declaration =
                properties.Single(
                    property =>
                        property.Identifier.ValueText == name);

            return Assert.IsAssignableFrom<IPropertySymbol>(
                fixture.Model.GetDeclaredSymbol(declaration));
        }

        /// <summary>
        /// Holds one in-memory syntax tree and its semantic model.
        /// </summary>
        private sealed record SemanticFixture(
            SyntaxTree Tree,
            SemanticModel Model);
    }
}
