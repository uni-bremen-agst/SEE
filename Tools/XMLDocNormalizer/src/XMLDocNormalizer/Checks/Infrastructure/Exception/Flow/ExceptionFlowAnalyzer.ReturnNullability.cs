using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains analysis of invocation return values for proven non-null results.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether an invocation is guaranteed to return a non-null value.
        /// </summary>
        /// <param name="invocation">The invocation expression to inspect.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if every source-level return value of the invoked
        /// method is proven to be non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsInvocationResultDefinitelyNonNull(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            if (IsKnownNonNullFrameworkFactory(methodSymbol))
            {
                return true;
            }

            IMethodSymbol originalMethod =
                methodSymbol.OriginalDefinition;

            if (originalMethod.IsAsync ||
                originalMethod.ReturnsByRef ||
                originalMethod.ReturnsByRefReadonly ||
                originalMethod.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            if (!inspectedReturnSymbols.Add(originalMethod))
            {
                return false;
            }

            bool foundExecutableDeclaration = false;

            try
            {
                foreach (SyntaxReference syntaxReference
                         in originalMethod.DeclaringSyntaxReferences)
                {
                    SyntaxNode declarationNode =
                        syntaxReference.GetSyntax();

                    SemanticModel? declarationSemanticModel =
                        GetSemanticModelForSyntaxTree(
                            semanticModel,
                            declarationNode.SyntaxTree);

                    if (declarationSemanticModel == null)
                    {
                        return false;
                    }

                    if (declarationNode
                        is MethodDeclarationSyntax methodDeclaration)
                    {
                        if (methodDeclaration.ExpressionBody != null)
                        {
                            foundExecutableDeclaration = true;

                            if (!IsDefinitelyNonNull(
                                    methodDeclaration.ExpressionBody.Expression,
                                    declarationSemanticModel,
                                    callContext,
                                    inspectedReturnSymbols))
                            {
                                return false;
                            }

                            continue;
                        }

                        if (methodDeclaration.Body != null)
                        {
                            foundExecutableDeclaration = true;

                            if (!AreAllReturnValuesDefinitelyNonNull(
                                    methodDeclaration.Body,
                                    declarationSemanticModel,
                                    callContext,
                                    inspectedReturnSymbols))
                            {
                                return false;
                            }
                        }

                        continue;
                    }

                    if (declarationNode
                        is LocalFunctionStatementSyntax localFunction)
                    {
                        if (localFunction.ExpressionBody != null)
                        {
                            foundExecutableDeclaration = true;

                            if (!IsDefinitelyNonNull(
                                    localFunction.ExpressionBody.Expression,
                                    declarationSemanticModel,
                                    callContext,
                                    inspectedReturnSymbols))
                            {
                                return false;
                            }

                            continue;
                        }

                        if (localFunction.Body != null)
                        {
                            foundExecutableDeclaration = true;

                            if (!AreAllReturnValuesDefinitelyNonNull(
                                    localFunction.Body,
                                    declarationSemanticModel,
                                    callContext,
                                    inspectedReturnSymbols))
                            {
                                return false;
                            }
                        }
                    }
                }

                return foundExecutableDeclaration;
            }
            finally
            {
                inspectedReturnSymbols.Remove(originalMethod);
            }
        }

        /// <summary>
        /// Determines whether all return statements in a block return values proven
        /// to be non-null.
        /// </summary>
        /// <param name="body">The method or local-function body to inspect.</param>
        /// <param name="semanticModel">The semantic model used for expression analysis.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the block contains at least one return statement
        /// and every returned expression is proven to be non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool AreAllReturnValuesDefinitelyNonNull(
            BlockSyntax body,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            List<ReturnStatementSyntax> returnStatements =
                body.DescendantNodes(
                        static node =>
                            node is not AnonymousFunctionExpressionSyntax &&
                            node is not LocalFunctionStatementSyntax)
                    .OfType<ReturnStatementSyntax>()
                    .ToList();

            if (returnStatements.Count == 0)
            {
                return false;
            }

            foreach (ReturnStatementSyntax returnStatement
                     in returnStatements)
            {
                if (returnStatement.Expression == null ||
                    !IsDefinitelyNonNull(
                        returnStatement.Expression,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a framework method is known to return a non-null value.
        /// </summary>
        /// <param name="methodSymbol">The resolved invoked method.</param>
        /// <returns>
        /// <see langword="true"/> if the method is a supported non-null framework
        /// factory; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsKnownNonNullFrameworkFactory(
            IMethodSymbol methodSymbol)
        {
            IMethodSymbol originalMethod =
                methodSymbol.OriginalDefinition;

            if (originalMethod.IsStatic &&
                originalMethod.Name == "Empty" &&
                originalMethod.Arity == 1 &&
                originalMethod.Parameters.Length == 0 &&
                originalMethod.ContainingType.SpecialType ==
                SpecialType.System_Array)
            {
                return true;
            }

            return originalMethod.IsStatic &&
                   originalMethod.Name == nameof(string.Join) &&
                   originalMethod.ContainingType.SpecialType ==
                   SpecialType.System_String;
        }
    }
}
