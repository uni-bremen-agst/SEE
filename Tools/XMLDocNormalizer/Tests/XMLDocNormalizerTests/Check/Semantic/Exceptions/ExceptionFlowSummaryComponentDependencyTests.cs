using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Guards the dependency direction between summary construction,
    /// evaluation, and session orchestration.
    /// </summary>
    public sealed class ExceptionFlowSummaryComponentDependencyTests
    {
        /// <summary>
        /// Ensures the extracted components use concrete, closed types rather
        /// than introducing interfaces or inheritance dispatch.
        /// </summary>
        [Fact]
        public void Components_AreSealedAndImplementNoInterfaces()
        {
            Type[] componentTypes =
            [
                typeof(ExceptionFlowSummaryGraphBuilder),
                typeof(ExceptionFlowSummaryGraphEvaluator),
                typeof(ExceptionFlowSummaryAnalysisSession)
            ];

            Assert.All(componentTypes, type => Assert.True(type.IsSealed));
            Assert.All(componentTypes, type => Assert.Empty(type.GetInterfaces()));
        }

        /// <summary>
        /// Ensures graph evaluation cannot reach syntax or semantic lookup
        /// services through fields or method signatures.
        /// </summary>
        [Fact]
        public void Evaluator_HasOnlyGraphLevelDependencies()
        {
            Type evaluatorType =
                typeof(ExceptionFlowSummaryGraphEvaluator);

            Assert.Empty(evaluatorType.GetFields(
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly));

            Type[] forbiddenTypes =
            [
                typeof(ExceptionFlowSemanticEnvironment),
                typeof(SemanticModel),
                typeof(SyntaxNode),
                typeof(MemberDeclarationSyntax),
                typeof(ExceptionFlowSummaryGraphBuilder)
            ];

            Assert.DoesNotContain(
                evaluatorType.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetParameters())
                    .Select(parameter => parameter.ParameterType),
                forbiddenTypes.Contains);
        }

        /// <summary>
        /// Ensures construction neither exposes evaluated results nor owns an
        /// evaluator dependency.
        /// </summary>
        [Fact]
        public void Builder_DoesNotDependOnEvaluation()
        {
            Type builderType =
                typeof(ExceptionFlowSummaryGraphBuilder);

            Assert.DoesNotContain(
                builderType.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly),
                field => field.FieldType ==
                    typeof(ExceptionFlowSummaryGraphEvaluator));

            Assert.DoesNotContain(
                builderType.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly),
                method => method.ReturnType ==
                              typeof(ExceptionFlowAnalysisResult) ||
                          method.GetParameters().Any(
                              parameter => parameter.ParameterType ==
                                  typeof(ExceptionFlowAnalysisResult)));
        }

        /// <summary>
        /// Ensures only the session composes shared graph state with the two
        /// extracted components.
        /// </summary>
        [Fact]
        public void Session_OwnsExactlyTheExpectedLongLivedState()
        {
            FieldInfo[] fields =
                typeof(ExceptionFlowSummaryAnalysisSession).GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

            Assert.Equal(
                new[]
                {
                    typeof(ExceptionFlowSemanticEnvironment),
                    typeof(ExceptionFlowSummaryGraph),
                    typeof(ExceptionFlowSummaryGraphBuilder),
                    typeof(ExceptionFlowSummaryGraphEvaluator)
                }.OrderBy(type => type.FullName),
                fields.Select(field => field.FieldType)
                    .OrderBy(type => type.FullName));
        }
    }
}
