using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Protects the semantic boundaries of the dictionary value-fact helpers
    /// extracted from <see cref="ExceptionFlowAnalyzer"/>.
    /// </summary>
    public sealed class ExceptionFlowDictionaryValueFactOwnershipTests
    {
        /// <summary>
        /// Ensures a dictionary-value call-context fact reaches the framework
        /// <see cref="KeyValuePair{TKey,TValue}.Value"/> property.
        /// </summary>
        [Fact]
        public void DictionaryEntryValue_ProjectsCallContextFact()
        {
            const string source =
                """
                using System.Collections.Generic;

                public static class TestClass
                {
                    public static void M(Dictionary<string, string> source)
                    {
                        foreach (KeyValuePair<string, string> pair in source)
                        {
                            _ = pair.Value;
                        }
                    }
                }
                """;
            SemanticFixture fixture = CreateFixture(source);
            MethodDeclarationSyntax method =
                GetMethod(fixture.Tree, "M");
            IMethodSymbol methodSymbol =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    fixture.Model.GetDeclaredSymbol(method));
            MemberAccessExpressionSyntax valueAccess =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MemberAccessExpressionSyntax>()
                    .Single(
                        static access =>
                            access.Name.Identifier.ValueText == "Value");
            IPropertySymbol valueProperty =
                Assert.IsAssignableFrom<IPropertySymbol>(
                    fixture.Model.GetSymbolInfo(valueAccess).Symbol);
            ExceptionFlowCallContext callContext =
                new(
                    methodSymbol,
                    [
                        new KeyValuePair<int, ExceptionFlowValueFacts>(
                            0,
                            ExceptionFlowValueFacts.NonNullDictionaryValues)
                    ]);

            ExceptionFlowValueFacts facts =
                ExceptionFlowCallContextFactProjector.GetDictionaryEntryValueFacts(
                    valueAccess,
                    valueProperty,
                    fixture.Model,
                    callContext);

            Assert.True(
                facts.ContainsAll(ExceptionFlowValueFacts.NonNull));
        }

        /// <summary>
        /// Ensures an unrelated property named <c>Value</c> cannot receive the
        /// dictionary-entry projection.
        /// </summary>
        [Fact]
        public void UnrelatedValueProperty_DoesNotProjectCallContextFact()
        {
            const string source =
                """
                using System.Collections.Generic;

                public sealed class Holder
                {
                    public string Value => "";
                }

                public static class TestClass
                {
                    public static void M(IEnumerable<Holder> source)
                    {
                        foreach (Holder item in source)
                        {
                            _ = item.Value;
                        }
                    }
                }
                """;
            SemanticFixture fixture = CreateFixture(source);
            MethodDeclarationSyntax method =
                GetMethod(fixture.Tree, "M");
            IMethodSymbol methodSymbol =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    fixture.Model.GetDeclaredSymbol(method));
            MemberAccessExpressionSyntax valueAccess =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MemberAccessExpressionSyntax>()
                    .Single(
                        static access =>
                            access.Name.Identifier.ValueText == "Value");
            IPropertySymbol valueProperty =
                Assert.IsAssignableFrom<IPropertySymbol>(
                    fixture.Model.GetSymbolInfo(valueAccess).Symbol);
            ExceptionFlowCallContext callContext =
                new(
                    methodSymbol,
                    [
                        new KeyValuePair<int, ExceptionFlowValueFacts>(
                            0,
                            ExceptionFlowValueFacts.NonNullDictionaryValues)
                    ]);

            ExceptionFlowValueFacts facts =
                ExceptionFlowCallContextFactProjector.GetDictionaryEntryValueFacts(
                    valueAccess,
                    valueProperty,
                    fixture.Model,
                    callContext);

            Assert.Equal(ExceptionFlowValueFacts.None, facts);
        }

        /// <summary>
        /// Ensures only the wrapped-source argument of the framework
        /// <c>ReadOnlyDictionary&lt;TKey, TValue&gt;</c> constructor is
        /// recognized.
        /// </summary>
        [Fact]
        public void ReadOnlyDictionaryWrapper_RecognizesOnlyFrameworkSourceArgument()
        {
            const string source =
                """
                using System.Collections.Generic;
                using System.Collections.ObjectModel;

                public sealed class Holder
                {
                    public Holder(Dictionary<string, string> source)
                    {
                    }
                }

                public static class TestClass
                {
                    public static void M(Dictionary<string, string> source)
                    {
                        _ = new ReadOnlyDictionary<string, string>(source);
                        _ = new Holder(source);
                    }
                }
                """;
            SemanticFixture fixture = CreateFixture(source);
            ObjectCreationExpressionSyntax[] creations =
                fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .ToArray();
            ObjectCreationExpressionSyntax readOnlyCreation =
                creations.Single(
                    static creation =>
                        creation.Type.ToString()
                            .StartsWith(
                                "ReadOnlyDictionary",
                                StringComparison.Ordinal));
            ObjectCreationExpressionSyntax holderCreation =
                creations.Single(
                    static creation =>
                        creation.Type.ToString() == "Holder");
            ArgumentSyntax readOnlyArgument =
                Assert.Single(readOnlyCreation.ArgumentList!.Arguments);
            ArgumentSyntax holderArgument =
                Assert.Single(holderCreation.ArgumentList!.Arguments);

            Assert.True(
                ExceptionFlowSequenceCollectionFactsProvider
                    .IsReadOnlyDictionaryWrapperConstruction(
                        readOnlyArgument,
                        readOnlyCreation,
                        fixture.Model));
            Assert.False(
                ExceptionFlowSequenceCollectionFactsProvider
                    .IsReadOnlyDictionaryWrapperConstruction(
                        holderArgument,
                        holderCreation,
                        fixture.Model));
            Assert.False(
                ExceptionFlowSequenceCollectionFactsProvider
                    .IsReadOnlyDictionaryWrapperConstruction(
                        holderArgument,
                        readOnlyCreation,
                        fixture.Model));
        }

        /// <summary>
        /// Ensures nearest-write discovery returns the nearest qualifying
        /// straight-line simple assignment.
        /// </summary>
        [Fact]
        public void PrecedingLocalAssignment_ReturnsNearestSimpleWrite()
        {
            const string source =
                """
                public static class TestClass
                {
                    public static void M()
                    {
                        string? value = null;
                        value = "first";
                        _ = value.Length;
                        value = "nearest";
                        Consume(value);
                    }

                    private static void Consume(string? value)
                    {
                    }
                }
                """;
            SemanticFixture fixture = CreateFixture(source);
            IdentifierNameSyntax valueUse =
                GetConsumeArgument(fixture.Tree);
            ILocalSymbol localSymbol =
                Assert.IsAssignableFrom<ILocalSymbol>(
                    fixture.Model.GetSymbolInfo(valueUse).Symbol);

            bool found =
                ExceptionFlowSymbolUsageFacts
                    .TryGetPrecedingSimpleLocalAssignment(
                        valueUse,
                        localSymbol,
                        fixture.Model,
                        out ExpressionSyntax? assignedExpression);

            Assert.True(found);
            Assert.Equal(@"""nearest""", assignedExpression?.ToString());
        }

        /// <summary>
        /// Ensures conditional and compound preceding writes are not treated as
        /// safe straight-line simple assignments.
        /// </summary>
        [Fact]
        public void PrecedingLocalAssignment_RejectsUnsafeWrites()
        {
            AssertPrecedingAssignmentRejected(
                """
                public static class TestClass
                {
                    public static void M(bool condition)
                    {
                        string? value = null;

                        if (condition)
                        {
                            value = "conditional";
                        }

                        Consume(value);
                    }

                    private static void Consume(string? value)
                    {
                    }
                }
                """);
            AssertPrecedingAssignmentRejected(
                """
                public static class TestClass
                {
                    public static void M()
                    {
                        string? value = null;
                        value += "compound";
                        Consume(value);
                    }

                    private static void Consume(string? value)
                    {
                    }
                }
                """);
        }

        /// <summary>
        /// Verifies that no qualifying preceding assignment is reported.
        /// </summary>
        /// <param name="source">The complete source to inspect.</param>
        private static void AssertPrecedingAssignmentRejected(string source)
        {
            SemanticFixture fixture = CreateFixture(source);
            IdentifierNameSyntax valueUse =
                GetConsumeArgument(fixture.Tree);
            ILocalSymbol localSymbol =
                Assert.IsAssignableFrom<ILocalSymbol>(
                    fixture.Model.GetSymbolInfo(valueUse).Symbol);

            bool found =
                ExceptionFlowSymbolUsageFacts
                    .TryGetPrecedingSimpleLocalAssignment(
                        valueUse,
                        localSymbol,
                        fixture.Model,
                        out ExpressionSyntax? assignedExpression);

            Assert.False(found);
            Assert.Null(assignedExpression);
        }

        /// <summary>
        /// Creates a semantic fixture for one source snippet.
        /// </summary>
        /// <param name="source">The source to compile.</param>
        /// <returns>The syntax tree and semantic model.</returns>
        private static SemanticFixture CreateFixture(string source)
        {
            SyntaxTree tree =
                CSharpSyntaxTree.ParseText(source);
            CSharpCompilation compilation =
                CSharpCompilation.Create(
                    "DictionaryValueFactOwnershipTests",
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
        /// Gets one uniquely named method declaration.
        /// </summary>
        /// <param name="tree">The source tree.</param>
        /// <param name="name">The method name.</param>
        /// <returns>The matching method declaration.</returns>
        private static MethodDeclarationSyntax GetMethod(
            SyntaxTree tree,
            string name)
        {
            return tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single(
                    method =>
                        string.Equals(
                            method.Identifier.ValueText,
                            name,
                            StringComparison.Ordinal));
        }

        /// <summary>
        /// Gets the local argument passed to <c>Consume</c>.
        /// </summary>
        /// <param name="tree">The source tree.</param>
        /// <returns>The argument identifier.</returns>
        private static IdentifierNameSyntax GetConsumeArgument(
            SyntaxTree tree)
        {
            InvocationExpressionSyntax invocation =
                tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Single(
                        static candidate =>
                            candidate.Expression.ToString() == "Consume");

            return Assert.IsType<IdentifierNameSyntax>(
                Assert.Single(invocation.ArgumentList.Arguments)
                    .Expression);
        }

        /// <summary>
        /// Holds one in-memory syntax tree and its semantic model.
        /// </summary>
        /// <param name="Tree">The compiled syntax tree.</param>
        /// <param name="Model">The semantic model for <paramref name="Tree"/>.</param>
        private sealed record SemanticFixture(
            SyntaxTree Tree,
            SemanticModel Model);
    }
}
