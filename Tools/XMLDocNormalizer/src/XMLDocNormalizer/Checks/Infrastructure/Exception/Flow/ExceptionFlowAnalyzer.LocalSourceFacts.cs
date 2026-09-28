using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains syntax and symbol facts shared by direct and summary-local
    /// exception-source discovery.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Finds the parameter indexes of delegate-typed parameters whose
        /// invocation result is directly thrown inside the callee body.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method symbol to inspect.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// The indexes of parameters that are treated as exception factory
        /// delegates.
        /// </returns>
        internal static HashSet<int>
            FindThrowingDelegateParameterIndexes(
                IMethodSymbol methodSymbol,
                ExceptionFlowSemanticEnvironment semanticContext)
        {
            HashSet<int> indexes = new();

            if (methodSymbol.DeclaringSyntaxReferences.Length == 0)
            {
                return indexes;
            }

            foreach (SyntaxReference syntaxReference
                     in methodSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node =
                    syntaxReference.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(
                        node.SyntaxTree,
                        out SemanticModel nodeSemanticModel) ||
                    nodeSemanticModel == null)
                {
                    continue;
                }

                BaseMethodDeclarationSyntax? declaration =
                    node as BaseMethodDeclarationSyntax;

                if (declaration == null)
                {
                    continue;
                }

                ParameterListSyntax? parameterList =
                    declaration.ParameterList;

                if (parameterList == null)
                {
                    continue;
                }

                Dictionary<string, int> parameterNameToIndex =
                    new(StringComparer.Ordinal);

                for (int i = 0;
                     i < parameterList.Parameters.Count;
                     i++)
                {
                    ParameterSyntax parameter =
                        parameterList.Parameters[i];

                    parameterNameToIndex[
                        parameter.Identifier.ValueText] = i;
                }

                IEnumerable<ThrowStatementSyntax> throwStatements =
                    declaration.DescendantNodes()
                        .OfType<ThrowStatementSyntax>();

                foreach (ThrowStatementSyntax throwStatement
                         in throwStatements)
                {
                    if (throwStatement.Expression
                        is not InvocationExpressionSyntax
                            delegateInvocation)
                    {
                        continue;
                    }

                    if (delegateInvocation.Expression
                        is not IdentifierNameSyntax identifier)
                    {
                        continue;
                    }

                    if (!parameterNameToIndex.TryGetValue(
                            identifier.Identifier.ValueText,
                            out int parameterIndex))
                    {
                        continue;
                    }

                    if (parameterIndex < 0 ||
                        parameterIndex >=
                        methodSymbol.Parameters.Length)
                    {
                        continue;
                    }

                    IParameterSymbol parameterSymbol =
                        methodSymbol.Parameters[parameterIndex];

                    if (IsExceptionFactoryDelegate(
                            parameterSymbol.Type))
                    {
                        indexes.Add(parameterIndex);
                    }
                }
            }

            return indexes;
        }

        /// <summary>
        /// Determines whether the specified type is a delegate type that
        /// returns <see cref="System.Exception"/> or a derived exception
        /// type.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type symbol to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the type is treated as an exception
        /// factory delegate; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsExceptionFactoryDelegate(
            ITypeSymbol typeSymbol)
        {
            if (typeSymbol
                is not INamedTypeSymbol namedType)
            {
                return false;
            }

            IMethodSymbol? invokeMethod =
                namedType.DelegateInvokeMethod;

            if (invokeMethod == null)
            {
                return false;
            }

            if (invokeMethod.Parameters.Length != 0)
            {
                return false;
            }

            return IsExceptionTypeByName(
                invokeMethod.ReturnType);
        }

        /// <summary>
        /// Determines whether the specified type symbol represents
        /// <see cref="System.Exception"/> or a derived type.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type symbol to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the type is an exception type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsExceptionTypeByName(
            ITypeSymbol typeSymbol)
        {
            INamedTypeSymbol? current =
                typeSymbol as INamedTypeSymbol;

            while (current != null)
            {
                if (current.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat) ==
                    "global::System.Exception")
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Extracts an exception object creation from a lambda or anonymous
        /// method used as an exception factory argument.
        /// </summary>
        /// <param name="expression">
        /// The argument expression to inspect.
        /// </param>
        /// <returns>
        /// The extracted exception object creation if found; otherwise
        /// <see langword="null"/>.
        /// </returns>
        internal static ObjectCreationExpressionSyntax?
            GetExceptionObjectCreation(
                ExpressionSyntax expression)
        {
            switch (expression)
            {
                case ParenthesizedLambdaExpressionSyntax
                    parenthesizedLambda:
                    return GetExceptionObjectCreationFromLambdaBody(
                        parenthesizedLambda.Body);

                case SimpleLambdaExpressionSyntax simpleLambda:
                    return GetExceptionObjectCreationFromLambdaBody(
                        simpleLambda.Body);

                case AnonymousMethodExpressionSyntax anonymousMethod:
                    if (anonymousMethod.Block != null)
                    {
                        ReturnStatementSyntax? returnStatement =
                            anonymousMethod.Block.Statements
                                .OfType<ReturnStatementSyntax>()
                                .FirstOrDefault();

                        if (returnStatement?.Expression
                            is ObjectCreationExpressionSyntax
                                objectCreation)
                        {
                            return objectCreation;
                        }
                    }

                    break;
            }

            return null;
        }

        /// <summary>
        /// Extracts an exception object creation from a lambda body.
        /// </summary>
        /// <param name="body">The lambda body to inspect.</param>
        /// <returns>
        /// The extracted exception object creation if found; otherwise
        /// <see langword="null"/>.
        /// </returns>
        private static ObjectCreationExpressionSyntax?
            GetExceptionObjectCreationFromLambdaBody(
                CSharpSyntaxNode body)
        {
            if (body
                is ObjectCreationExpressionSyntax directCreation)
            {
                return directCreation;
            }

            if (body is BlockSyntax block)
            {
                ReturnStatementSyntax? returnStatement =
                    block.Statements
                        .OfType<ReturnStatementSyntax>()
                        .FirstOrDefault();

                if (returnStatement?.Expression
                    is ObjectCreationExpressionSyntax blockCreation)
                {
                    return blockCreation;
                }
            }

            return null;
        }
        /// <summary>
        /// Determines whether the specified object creation is part of a
        /// direct throw statement or throw expression and is therefore
        /// already covered by direct throw analysis.
        /// </summary>
        /// <param name="creation">
        /// The object creation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the object creation is directly thrown;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsPartOfDirectThrow(
            BaseObjectCreationExpressionSyntax creation)
        {
            return creation.Parent
                       is ThrowStatementSyntax ||
                   creation.Parent
                       is ThrowExpressionSyntax;
        }

    }
}
