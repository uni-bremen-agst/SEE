using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Performs transitive analysis of thrown exceptions by walking method bodies
    /// and recursively analyzing invoked members.
    /// </summary>
    internal static class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects all exception types that may be thrown transitively by the specified member.
        /// </summary>
        public static HashSet<INamedTypeSymbol> CollectTransitivelyThrownExceptions(
            MemberDeclarationSyntax member,
            SemanticModel semanticModel)
        {
            HashSet<INamedTypeSymbol> exceptions =
                new(SymbolEqualityComparer.Default);

            HashSet<ISymbol> visited =
                new(SymbolEqualityComparer.Default);

            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? body) || body == null)
            {
                return exceptions;
            }

            AnalyzeBody(body, semanticModel, exceptions, visited);

            return exceptions;
        }

        /// <summary>
        /// Analyzes a body node for thrown exceptions and invoked members.
        /// </summary>
        private static void AnalyzeBody(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            AnalyzeThrows(body, semanticModel, exceptions);
            AnalyzeInvocations(body, semanticModel, exceptions, visited);
        }

        /// <summary>
        /// Collects exceptions thrown directly in the body.
        /// </summary>
        private static void AnalyzeThrows(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions)
        {
            foreach (ThrowStatementSyntax throwStatement in body.DescendantNodes().OfType<ThrowStatementSyntax>())
            {
                if (throwStatement.Expression is ObjectCreationExpressionSyntax creation)
                {
                    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation.Type);

                    if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
                    {
                        exceptions.Add(typeSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes method invocations and recursively inspects their bodies.
        /// </summary>
        private static void AnalyzeInvocations(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            foreach (InvocationExpressionSyntax invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                if (!visited.Add(methodSymbol))
                {
                    continue;
                }

                AnalyzeSymbol(methodSymbol, semanticModel, exceptions, visited);
            }
        }

        /// <summary>
        /// Recursively analyzes a callable symbol.
        /// </summary>
        private static void AnalyzeSymbol(
            ISymbol symbol,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxRef.GetSyntax();

                if (node is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(method, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, semanticModel, exceptions, visited);
                    }
                }

                if (node is ConstructorDeclarationSyntax ctor)
                {
                    if (SyntaxUtils.TryGetMemberBody(ctor, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, semanticModel, exceptions, visited);
                    }
                }

                if (node is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.Body != null)
                    {
                        AnalyzeBody(accessor.Body, semanticModel, exceptions, visited);
                    }
                }
            }
        }
    }
}
