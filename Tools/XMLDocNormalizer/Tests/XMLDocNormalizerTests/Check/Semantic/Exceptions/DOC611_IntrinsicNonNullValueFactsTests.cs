using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
