using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests values that are intrinsically non-null because of C# language
    /// semantics or explicit framework contracts.
    /// </summary>
    public sealed class DOC611_IntrinsicNonNullValueFactsTests
    {
        /// <summary>
        /// Ensures that a method-group conversion produces a non-null delegate.
        /// </summary>
        [Fact]
        public void MethodGroupArgument_DoesNotProduceFinding()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Passes a method group to a guarded delegate parameter.
                    /// </summary>
                    public void M()
                    {
                        Validate(Convert);
                    }

                    private static string Convert(int value)
                    {
                        return value.ToString();
                    }

                    private static void Validate(Func<int, string>? callback)
                    {
                        ArgumentNullException.ThrowIfNull(callback);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that delegate-typed expressions are not treated as
        /// intrinsically non-null unless they are method-group conversions.
        /// </summary>
        [Fact]
        public void NullableDelegateParameter_StillProducesFinding()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Passes an unknown delegate value to a guarded parameter.
                    /// </summary>
                    public void M(Func<int, string>? callback)
                    {
                        Validate(callback);
                    }

                    private static void Validate(Func<int, string>? callback)
                    {
                        ArgumentNullException.ThrowIfNull(callback);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that Roslyn's mandatory variable-declaration children of
        /// field and event-field declarations are recognized as non-null.
        /// </summary>
        [Fact]
        public void RoslynFieldDeclarationChildren_DoNotProduceFinding()
        {
            const string source =
                """
                using System;
                using Microsoft.CodeAnalysis.CSharp.Syntax;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates mandatory Roslyn declaration children.
                    /// </summary>
                    public void M(
                        FieldDeclarationSyntax field,
                        EventFieldDeclarationSyntax eventField)
                    {
                        Validate(field.Declaration);
                        Validate(eventField.Declaration);
                    }

                    private static void Validate(VariableDeclarationSyntax? declaration)
                    {
                        ArgumentNullException.ThrowIfNull(declaration);
                    }
                }
                """;

            MetadataReference[] roslynReferences = GetRoslynMetadataReferences();

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive,
                roslynReferences);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an unrelated property named Declaration is not covered
        /// by the Roslyn-specific framework fact.
        /// </summary>
        [Fact]
        public void UnrelatedNullableDeclarationProperty_StillProducesFinding()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates an unrelated nullable property.
                    /// </summary>
                    public void M(Holder holder)
                    {
                        Validate(holder.Declaration);
                    }

                    private static void Validate(object? declaration)
                    {
                        ArgumentNullException.ThrowIfNull(declaration);
                    }

                    public sealed class Holder
                    {
                        /// <summary>
                        /// Gets an optional declaration.
                        /// </summary>
                        public object? Declaration { get; }
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that Roslyn's compilation-unit-root accessor is recognized
        /// as returning a non-null syntax node.
        /// </summary>
        [Fact]
        public void GetCompilationUnitRootResult_DoesNotProduceFinding()
        {
            const string source =
                """
                using System;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.CSharp.Syntax;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a compilation-unit root.
                    /// </summary>
                    public void M(SyntaxTree tree)
                    {
                        Validate(tree.GetCompilationUnitRoot());
                    }

                    private static void Validate(CompilationUnitSyntax? root)
                    {
                        ArgumentNullException.ThrowIfNull(root);
                    }
                }
                """;

            MetadataReference[] roslynReferences = GetRoslynMetadataReferences();

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive,
                roslynReferences);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that Roslyn's C# syntax-tree parser is recognized as
        /// returning a non-null syntax tree after successful completion.
        /// </summary>
        [Fact]
        public void CSharpSyntaxTreeParseTextResult_DoesNotProduceFinding()
        {
            const string source =
                """
                using System;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a successfully parsed syntax tree.
                    /// </summary>
                    public void M(string text)
                    {
                        Validate(CSharpSyntaxTree.ParseText(text));
                    }

                    private static void Validate(SyntaxTree? tree)
                    {
                        ArgumentNullException.ThrowIfNull(tree);
                    }
                }
                """;

            MetadataReference[] roslynReferences = GetRoslynMetadataReferences();

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive,
                roslynReferences);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an unrelated method named ParseText does not receive
        /// Roslyn's syntax-tree return postcondition.
        /// </summary>
        [Fact]
        public void UnrelatedParseTextResult_StillProducesFinding()
        {
            const string source =
                """
                using System;

                public static class Parser
                {
                    public static object? ParseText(string text)
                    {
                        return null;
                    }
                }

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates an unrelated parser result.
                    /// </summary>
                    public void M(string text)
                    {
                        Validate(Parser.ParseText(text));
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that converting an enum value to text produces a non-null
        /// string.
        /// </summary>
        [Fact]
        public void EnumToStringResult_DoesNotProduceFinding()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates the textual representation of an enum value.
                    /// </summary>
                    public void M(ConsoleColor value)
                    {
                        Validate(value.ToString());
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures the known-framework return classifiers retain their exact
        /// assembly, containing-type, return-type, and signature checks.
        /// </summary>
        [Fact]
        public void KnownFrameworkReturnClassifiers_RequireExactSupportedSignatures()
        {
            const string source =
                """
                using System;
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.CSharp.Syntax;

                public sealed class Foreign
                {
                    public object? ParseText(string text) => null;

                    public object? GetCompilationUnitRoot() => null;
                }

                public sealed class TestClass
                {
                    public SyntaxTree Parse(string text) =>
                        CSharpSyntaxTree.ParseText(text);

                    public CompilationUnitSyntax Root(SyntaxTree tree) =>
                        tree.GetCompilationUnitRoot();

                    public string EnumText(ConsoleColor value) =>
                        value.ToString();

                    public string FormattedEnumText(ConsoleColor value) =>
                        value.ToString("G");

                    public object? ForeignParse(Foreign value, string text) =>
                        value.ParseText(text);

                    public object? ForeignRoot(Foreign value) =>
                        value.GetCompilationUnitRoot();
                }
                """;

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
            MetadataReference[] references =
                MetadataReferences.Default
                    .Concat(GetRoslynMetadataReferences())
                    .ToArray();
            CSharpCompilation compilation = CSharpCompilation.Create(
                "KnownFrameworkReturnClassification",
                [syntaxTree],
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));
            SemanticModel semanticModel =
                compilation.GetSemanticModel(syntaxTree);
            CompilationUnitSyntax root =
                syntaxTree.GetCompilationUnitRoot();

            IMethodSymbol parseText = GetInvokedMethod(
                root,
                semanticModel,
                "Parse");
            IMethodSymbol compilationUnitRoot = GetInvokedMethod(
                root,
                semanticModel,
                "Root");
            IMethodSymbol enumToString = GetInvokedMethod(
                root,
                semanticModel,
                "EnumText");
            IMethodSymbol formattedEnumToString = GetInvokedMethod(
                root,
                semanticModel,
                "FormattedEnumText");
            IMethodSymbol foreignParseText = GetInvokedMethod(
                root,
                semanticModel,
                "ForeignParse");
            IMethodSymbol foreignCompilationUnitRoot = GetInvokedMethod(
                root,
                semanticModel,
                "ForeignRoot");

            Assert.True(
                ExceptionFlowNullabilityFactsProvider
                    .IsRoslynCSharpSyntaxTreeParseTextMethod(
                        parseText));
            Assert.True(
                ExceptionFlowNullabilityFactsProvider
                    .IsRoslynCompilationUnitRootMethod(
                        compilationUnitRoot));
            Assert.True(
                ExceptionFlowNullabilityFactsProvider
                    .IsSystemEnumToStringMethod(
                        enumToString));
            Assert.False(
                ExceptionFlowNullabilityFactsProvider
                    .IsSystemEnumToStringMethod(
                        formattedEnumToString));
            Assert.False(
                ExceptionFlowNullabilityFactsProvider
                    .IsRoslynCSharpSyntaxTreeParseTextMethod(
                        foreignParseText));
            Assert.False(
                ExceptionFlowNullabilityFactsProvider
                    .IsRoslynCompilationUnitRootMethod(
                        foreignCompilationUnitRoot));
        }

        /// <summary>
        /// Resolves the single invocation in the named source method to its
        /// original framework or source definition.
        /// </summary>
        /// <param name="root">The parsed compilation unit.</param>
        /// <param name="semanticModel">The semantic model for the source.</param>
        /// <param name="methodName">The containing source-method name.</param>
        /// <returns>The original invoked method definition.</returns>
        private static IMethodSymbol GetInvokedMethod(
            CompilationUnitSyntax root,
            SemanticModel semanticModel,
            string methodName)
        {
            InvocationExpressionSyntax invocation = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Single(
                    method => method.Identifier.ValueText == methodName)
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Single();
            IMethodSymbol? methodSymbol =
                semanticModel.GetSymbolInfo(invocation).Symbol
                    as IMethodSymbol;

            Assert.NotNull(methodSymbol);
            return methodSymbol.ReducedFrom?.OriginalDefinition
                ?? methodSymbol.OriginalDefinition;
        }

        /// <summary>
        /// Gets the Roslyn metadata references required by in-memory source
        /// tests that use Roslyn syntax APIs.
        /// </summary>
        /// <returns>
        /// The metadata references for the core Roslyn and C# Roslyn
        /// assemblies.
        /// </returns>
        private static MetadataReference[] GetRoslynMetadataReferences()
        {
            return
            [
                MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CSharpSyntaxTree).Assembly.Location)
            ];
        }
    }
}
