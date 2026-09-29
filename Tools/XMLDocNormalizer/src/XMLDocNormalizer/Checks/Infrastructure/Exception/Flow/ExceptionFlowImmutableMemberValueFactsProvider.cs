using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides stateless constructor and assignment facts for immutable
    /// member value analysis.
    /// </summary>
    internal static class ExceptionFlowImmutableMemberValueFactsProvider
    {
        /// <summary>
        /// Determines whether a constructor contains any assignment to the
        /// specified field or property.
        /// </summary>
        /// <param name="memberSymbol">
        /// The field or property whose assignments should be found.
        /// </param>
        /// <param name="constructorSymbol">The constructor to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the constructor syntax.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when at least one assignment exists or the
        /// constructor cannot be inspected safely; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool HasConstructorAssignmentToMember(
            ISymbol memberSymbol,
            IMethodSymbol constructorSymbol,
            SemanticModel semanticModel)
        {
            if (constructorSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return true;
            }

            SyntaxNode constructorNode =
                constructorSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            if (constructorNode
                is not ConstructorDeclarationSyntax constructor)
            {
                return true;
            }

            SemanticModel? constructorSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                    semanticModel,
                    constructor.SyntaxTree);

            if (constructorSemanticModel == null)
            {
                return true;
            }

            foreach (AssignmentExpressionSyntax assignment
                     in constructor.DescendantNodes(
                             static node =>
                                 node
                                     is not
                                     AnonymousFunctionExpressionSyntax
                                 && node
                                     is not
                                     LocalFunctionStatementSyntax)
                         .OfType<AssignmentExpressionSyntax>())
            {
                if (AssignmentTargetsSymbol(
                        assignment.Left,
                        memberSymbol,
                        constructorSemanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a static readonly field is assigned after its
        /// declaration initializer.
        /// </summary>
        /// <param name="fieldSymbol">The field to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain models for partial type
        /// declarations.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an additional assignment was found;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasFieldAssignmentOutsideInitializer(
            IFieldSymbol fieldSymbol,
            SemanticModel semanticModel)
        {
            foreach (SyntaxReference typeReference
                     in fieldSymbol.ContainingType
                         .DeclaringSyntaxReferences)
            {
                SyntaxNode typeNode =
                    typeReference.GetSyntax();

                SemanticModel? typeSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                        semanticModel,
                        typeNode.SyntaxTree);

                if (typeSemanticModel == null)
                {
                    return true;
                }

                IEnumerable<AssignmentExpressionSyntax> assignments =
                    typeNode.DescendantNodes(
                            static node =>
                                node
                                    is not
                                    AnonymousFunctionExpressionSyntax
                                && node
                                    is not
                                    LocalFunctionStatementSyntax)
                        .OfType<AssignmentExpressionSyntax>();

                foreach (AssignmentExpressionSyntax assignment
                         in assignments)
                {
                    if (AssignmentTargetsSymbol(
                            assignment.Left,
                            fieldSymbol,
                            typeSemanticModel))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a constructor delegates to another constructor
        /// of the same type through <c>this(...)</c>.
        /// </summary>
        /// <param name="constructorSymbol">The constructor to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the constructor has a
        /// <c>this(...)</c> initializer; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool IsThisDelegatingConstructor(
            IMethodSymbol constructorSymbol)
        {
            if (constructorSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode declarationNode =
                constructorSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            return declarationNode
                       is ConstructorDeclarationSyntax constructor
                   && constructor.Initializer?
                       .ThisOrBaseKeyword
                       .IsKind(
                           SyntaxKind.ThisKeyword) == true;
        }

        /// <summary>
        /// Finds the single unconditional direct assignment to a field or
        /// property in a terminal constructor.
        /// </summary>
        /// <param name="memberSymbol">
        /// The field or property assigned by the constructor.
        /// </param>
        /// <param name="constructorSymbol">The constructor to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain the constructor model.
        /// </param>
        /// <param name="assignedExpression">
        /// The expression assigned to the member when successful.
        /// </param>
        /// <param name="constructorSemanticModel">
        /// The semantic model for the constructor declaration when
        /// successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if exactly one unconditional direct
        /// assignment was found; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryGetDirectConstructorAssignment(
            ISymbol memberSymbol,
            IMethodSymbol constructorSymbol,
            SemanticModel semanticModel,
            out ExpressionSyntax? assignedExpression,
            out SemanticModel? constructorSemanticModel)
        {
            assignedExpression =
                null;

            constructorSemanticModel =
                null;

            if (constructorSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode constructorNode =
                constructorSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            if (constructorNode
                    is not ConstructorDeclarationSyntax constructor
                || constructor.Body == null
                || constructor.ExpressionBody != null
                || constructor.Initializer?
                    .ThisOrBaseKeyword
                    .IsKind(
                        SyntaxKind.ThisKeyword) == true)
            {
                return false;
            }

            constructorSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                    semanticModel,
                    constructor.SyntaxTree);

            if (constructorSemanticModel == null)
            {
                return false;
            }

            SemanticModel resolvedConstructorSemanticModel =
                constructorSemanticModel;

            List<AssignmentExpressionSyntax> assignments =
                constructor.DescendantNodes(
                        static node =>
                            node
                                is not
                                AnonymousFunctionExpressionSyntax
                            && node
                                is not
                                LocalFunctionStatementSyntax)
                    .OfType<AssignmentExpressionSyntax>()
                    .Where(
                        assignment =>
                            assignment.IsKind(
                                SyntaxKind
                                    .SimpleAssignmentExpression)
                            && AssignmentTargetsSymbol(
                                assignment.Left,
                                memberSymbol,
                                resolvedConstructorSemanticModel))
                    .ToList();

            if (assignments.Count != 1)
            {
                return false;
            }

            AssignmentExpressionSyntax assignment =
                assignments[0];

            if (assignment.Parent
                    is not ExpressionStatementSyntax expressionStatement
                || expressionStatement.Parent != constructor.Body)
            {
                return false;
            }

            assignedExpression =
                assignment.Right;

            return true;
        }

        /// <summary>
        /// Determines whether an assignment target resolves to the specified
        /// member.
        /// </summary>
        /// <param name="targetExpression">The assignment target.</param>
        /// <param name="expectedSymbol">The expected field or property.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the target resolves to the expected
        /// member; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AssignmentTargetsSymbol(
            ExpressionSyntax targetExpression,
            ISymbol expectedSymbol,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    targetExpression);

            return symbolInfo.Symbol != null
                && SymbolEqualityComparer.Default.Equals(
                    symbolInfo.Symbol.OriginalDefinition,
                    expectedSymbol.OriginalDefinition);
        }
    }
}
