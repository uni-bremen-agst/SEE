using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains direct throw analysis for callable summary graphs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Stores the metadata name of <see cref="Exception"/>.
        /// </summary>
        private const string ExceptionMetadataName =
            "System.Exception";

        /// <summary>
        /// Stores the metadata name of
        /// <see cref="NullReferenceException"/>.
        /// </summary>
        private const string NullReferenceExceptionMetadataName =
            "System.NullReferenceException";

        /// <summary>
        /// Collects directly thrown exception types from throw statements and
        /// throw expressions belonging to the current callable.
        /// </summary>
        /// <param name="node">
        /// The syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for operation, type, and nullable-flow
        /// analysis.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving exception sources and
        /// uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        private static void AnalyzeSummaryThrows(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (ThrowStatementSyntax throwStatement
                     in GetSummaryDescendantsAndSelf
                         <ThrowStatementSyntax>(node))
            {
                if (IsThrowStatementProvenUnreachable(
                        throwStatement,
                        node,
                        semanticModel,
                        callContext))
                {
                    continue;
                }

                AnalyzeSummaryThrowOperation(
                    throwStatement,
                    semanticModel,
                    fragment);
            }

            foreach (ThrowExpressionSyntax throwExpression
                     in GetSummaryDescendantsAndSelf
                         <ThrowExpressionSyntax>(node))
            {
                if (IsThrowExpressionProvenUnreachable(
                        throwExpression,
                        node,
                        semanticModel,
                        callContext))
                {
                    continue;
                }

                AnalyzeSummaryThrowOperation(
                    throwExpression,
                    semanticModel,
                    fragment);
            }
        }

        /// <summary>
        /// Analyzes one throw statement or throw expression.
        /// </summary>
        /// <param name="throwNode">
        /// The throw statement or throw expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for operation and type analysis.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        private static void AnalyzeSummaryThrowOperation(
            SyntaxNode throwNode,
            SemanticModel semanticModel,
            ExceptionFlowSummaryFragment fragment)
        {
            if (semanticModel.GetOperation(
                    throwNode)
                is not IThrowOperation throwOperation)
            {
                fragment.AddUncertainTarget(
                    "Thrown expression operation could not be resolved.");

                return;
            }

            if (TryGetCaughtExceptionRethrow(
                    throwNode,
                    semanticModel,
                    out bool hasPotentiallyThrowingConversion))
            {
                if (hasPotentiallyThrowingConversion)
                {
                    fragment.AddUncertainTarget(
                        "Catch exception rethrow contains an explicit " +
                        "conversion whose runtime behavior is not modeled.");
                }

                return;
            }

            if (throwOperation.Exception == null)
            {
                fragment.AddUncertainTarget(
                    "Rethrown exception context could not be resolved.");

                return;
            }

            HashSet<INamedTypeSymbol> addedExceptionTypes =
                new(SymbolEqualityComparer.Default);

            if (IsConstantNullOperation(
                    throwOperation.Exception))
            {
                AddSummaryNullReferenceExceptionSource(
                    semanticModel.Compilation,
                    throwNode,
                    fragment,
                    addedExceptionTypes);

                return;
            }

            ExpressionSyntax? thrownExpression =
                GetThrownExpression(
                    throwNode);

            ITypeSymbol? thrownType =
                thrownExpression != null
                    ? semanticModel.GetTypeInfo(
                        thrownExpression).Type
                    : null;

            thrownType ??=
                throwOperation.Exception.Type;

            bool addedThrownType =
                TryAddSummaryThrownTypeSource(
                    thrownType,
                    semanticModel.Compilation,
                    throwNode,
                    fragment,
                    addedExceptionTypes);

            if (thrownExpression != null)
            {
                AnalyzeSummaryThrownExpressionNullability(
                    thrownExpression,
                    thrownType,
                    semanticModel,
                    throwNode,
                    fragment,
                    addedExceptionTypes);
            }

            if (!addedThrownType &&
                addedExceptionTypes.Count == 0)
            {
                fragment.AddUncertainTarget(
                    "Thrown expression type could not be resolved.");
            }
        }

        /// <summary>
        /// Gets the expression supplied to a throw statement or throw
        /// expression.
        /// </summary>
        /// <param name="throwNode">
        /// The throw syntax node.
        /// </param>
        /// <returns>
        /// The thrown expression, or <see langword="null"/> for a
        /// parameterless rethrow.
        /// </returns>
        private static ExpressionSyntax? GetThrownExpression(
            SyntaxNode throwNode)
        {
            return throwNode switch
            {
                ThrowStatementSyntax throwStatement =>
                    throwStatement.Expression,

                ThrowExpressionSyntax throwExpression =>
                    throwExpression.Expression,

                _ => null
            };
        }

        /// <summary>
        /// Determines whether an operation is proven to produce
        /// <see langword="null"/>.
        /// </summary>
        /// <param name="operation">
        /// The operation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation has the constant value
        /// <see langword="null"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsConstantNullOperation(
            IOperation operation)
        {
            return operation.ConstantValue.HasValue &&
                   operation.ConstantValue.Value == null;
        }

        /// <summary>
        /// Adds the statically known exception type of a thrown expression.
        /// </summary>
        /// <param name="thrownType">
        /// The static type of the thrown expression.
        /// </param>
        /// <param name="compilation">
        /// The compilation used to resolve <see cref="Exception"/>.
        /// </param>
        /// <param name="throwNode">
        /// The source-level throw operation.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="addedExceptionTypes">
        /// The exception types already added for the throw operation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one exception type was added;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryAddSummaryThrownTypeSource(
            ITypeSymbol? thrownType,
            Compilation compilation,
            SyntaxNode throwNode,
            ExceptionFlowSummaryFragment fragment,
            HashSet<INamedTypeSymbol> addedExceptionTypes)
        {
            INamedTypeSymbol? exceptionBaseType =
                compilation.GetTypeByMetadataName(
                    ExceptionMetadataName);

            if (exceptionBaseType == null)
            {
                fragment.AddUncertainTarget(
                    "System.Exception could not be resolved.");

                return false;
            }

            if (thrownType is INamedTypeSymbol namedType)
            {
                if (!namedType.InheritsFromOrEquals(
                        exceptionBaseType))
                {
                    return false;
                }

                AddSummaryThrownSource(
                    namedType,
                    throwNode,
                    fragment,
                    addedExceptionTypes);

                return true;
            }

            if (thrownType
                is not ITypeParameterSymbol typeParameter)
            {
                return false;
            }

            INamedTypeSymbol[] exceptionConstraints =
                typeParameter.ConstraintTypes
                    .OfType<INamedTypeSymbol>()
                    .Where(
                        constraintType =>
                            constraintType.InheritsFromOrEquals(
                                exceptionBaseType))
                    .Distinct<INamedTypeSymbol>(
                        SymbolEqualityComparer.Default)
                    .ToArray();

            if (exceptionConstraints.Length == 0)
            {
                fragment.AddUncertainTarget(
                    $"Thrown type parameter '{typeParameter.Name}' " +
                    "has no resolvable exception constraint.");

                return false;
            }

            foreach (INamedTypeSymbol constraintType
                     in exceptionConstraints)
            {
                AddSummaryThrownSource(
                    constraintType,
                    throwNode,
                    fragment,
                    addedExceptionTypes);
            }

            fragment.AddUncertainTarget(
                $"Thrown type parameter '{typeParameter.Name}' may " +
                "produce a more specific runtime exception type.");

            return true;
        }

        /// <summary>
        /// Adds possible null-related throw behavior for one thrown
        /// expression.
        /// </summary>
        /// <param name="expression">
        /// The thrown expression.
        /// </param>
        /// <param name="thrownType">
        /// The static type of the expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for nullable-flow analysis.
        /// </param>
        /// <param name="throwNode">
        /// The source-level throw operation.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="addedExceptionTypes">
        /// The exception types already added for the operation.
        /// </param>
        private static void AnalyzeSummaryThrownExpressionNullability(
            ExpressionSyntax expression,
            ITypeSymbol? thrownType,
            SemanticModel semanticModel,
            SyntaxNode throwNode,
            ExceptionFlowSummaryFragment fragment,
            HashSet<INamedTypeSymbol> addedExceptionTypes)
        {
            NullableFlowState flowState =
                semanticModel.GetTypeInfo(
                    expression)
                    .Nullability
                    .FlowState;

            if (flowState ==
                NullableFlowState.MaybeNull)
            {
                AddSummaryNullReferenceExceptionSource(
                    semanticModel.Compilation,
                    throwNode,
                    fragment,
                    addedExceptionTypes);

                return;
            }

            if (flowState != NullableFlowState.None ||
                thrownType?.IsReferenceType != true)
            {
                return;
            }

            string typeName =
                thrownType.ToDisplayString(
                    SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (string.IsNullOrWhiteSpace(
                    typeName))
            {
                typeName =
                    "<unknown exception type>";
            }

            fragment.AddUncertainTarget(
                $"Nullability of thrown expression type '{typeName}' " +
                "could not be determined.");
        }

        /// <summary>
        /// Adds <see cref="NullReferenceException"/> for an expression that
        /// is proven or may evaluate to <see langword="null"/> when thrown.
        /// </summary>
        /// <param name="compilation">
        /// The compilation used to resolve the exception type.
        /// </param>
        /// <param name="throwNode">
        /// The source-level throw operation.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="addedExceptionTypes">
        /// The exception types already added for the operation.
        /// </param>
        private static void AddSummaryNullReferenceExceptionSource(
            Compilation compilation,
            SyntaxNode throwNode,
            ExceptionFlowSummaryFragment fragment,
            HashSet<INamedTypeSymbol> addedExceptionTypes)
        {
            INamedTypeSymbol? nullReferenceExceptionType =
                compilation.GetTypeByMetadataName(
                    NullReferenceExceptionMetadataName);

            if (nullReferenceExceptionType == null)
            {
                fragment.AddUncertainTarget(
                    "System.NullReferenceException could not be resolved.");

                return;
            }

            AddSummaryThrownSource(
                nullReferenceExceptionType,
                throwNode,
                fragment,
                addedExceptionTypes);
        }

        /// <summary>
        /// Adds one deduplicated direct throw source.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type to add.
        /// </param>
        /// <param name="throwNode">
        /// The source-level throw operation.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="addedExceptionTypes">
        /// The exception types already added for the operation.
        /// </param>
        private static void AddSummaryThrownSource(
            INamedTypeSymbol exceptionType,
            SyntaxNode throwNode,
            ExceptionFlowSummaryFragment fragment,
            HashSet<INamedTypeSymbol> addedExceptionTypes)
        {
            INamedTypeSymbol normalizedType =
                exceptionType.OriginalDefinition;

            if (!addedExceptionTypes.Add(
                    normalizedType))
            {
                return;
            }

            fragment.AddSource(
                new ExceptionFlowSummarySource(
                    exceptionType,
                    CreateTerminalPath(
                        ExceptionFlowPathStepKind.ExplicitThrow,
                        exceptionType,
                        throwNode)));
        }
    }
}
