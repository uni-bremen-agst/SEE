using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains method-invocation and exception-factory analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Resolves method invocations within the specified node, recognizes known
        /// framework exception sources, and optionally analyzes invoked method bodies
        /// transitively.
        /// </summary>
        /// <param name="node">The node to inspect for invocations.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="traversalState">The traversal state used to prevent recursive analysis cycles.</param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">The call-site facts known for the currently analyzed callable.</param>
        private static void AnalyzeInvocations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            foreach (InvocationExpressionSyntax invocation
                     in GetDescendantsAndSelfExcludingNestedTry
                         <InvocationExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                if (IsNonThrowingArgumentNullGuard(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        callContext))
                {
                    continue;
                }

                if (KnownFrameworkExceptionModel.TryAddThrownExceptionTypes(
                        methodSymbol,
                        semanticModel.Compilation,
                        result.ThrownExceptions))
                {
                    continue;
                }

                if (mode == ExceptionFlowTraversalMode.Direct)
                {
                    continue;
                }

                CollectThrownExceptionsFromDelegateFactoryCall(
                    invocation,
                    methodSymbol,
                    semanticContext,
                    result);

                ExceptionFlowCallContext calleeContext =
                    CreateCallContext(
                        methodSymbol,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callContext);

                if (!traversalState.TryMarkAnalyzed(
                        methodSymbol,
                        calleeContext))
                {
                    continue;
                }

                if (!AnalyzeSymbol(
                        methodSymbol,
                        semanticContext,
                        result,
                        traversalState,
                        calleeContext))
                {
                    MarkUncertain(result, methodSymbol);
                }
            }
        }

        /// <summary>
        /// Collects exception types from invocations where the callee throws the result
        /// of a delegate parameter invocation and the call site supplies a lambda or
        /// anonymous method that directly creates an exception object.
        /// </summary>
        /// <param name="invocation">The invocation to inspect.</param>
        /// <param name="methodSymbol">The resolved target method symbol.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        private static void CollectThrownExceptionsFromDelegateFactoryCall(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result)
        {
            HashSet<int> throwingDelegateParameterIndexes =
                FindThrowingDelegateParameterIndexes(
                    methodSymbol,
                    semanticContext);

            if (throwingDelegateParameterIndexes.Count == 0)
            {
                return;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentSyntax argument = arguments[i];

                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        i,
                        methodSymbol);

                if (!throwingDelegateParameterIndexes.Contains(parameterIndex))
                {
                    continue;
                }

                ObjectCreationExpressionSyntax? creation =
                    GetExceptionObjectCreation(argument.Expression);

                if (creation == null)
                {
                    continue;
                }

                if (!semanticContext.TryGetSemanticModel(
                        creation.SyntaxTree,
                        out SemanticModel creationSemanticModel) ||
                    creationSemanticModel == null)
                {
                    continue;
                }

                SymbolInfo creationSymbolInfo =
                    creationSemanticModel.GetSymbolInfo(creation.Type);

                if (creationSymbolInfo.Symbol
                    is INamedTypeSymbol typeSymbol)
                {
                    result.ThrownExceptions.Add(typeSymbol);
                }
            }
        }

        /// <summary>
        /// Finds the parameter indexes of delegate-typed parameters whose invocation result
        /// is directly thrown inside the callee body.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to inspect.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>
        /// The indexes of parameters that are treated as exception factory delegates.
        /// </returns>
        private static HashSet<int> FindThrowingDelegateParameterIndexes(
            IMethodSymbol methodSymbol,
            ProjectClosureSemanticContext semanticContext)
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
                        is not InvocationExpressionSyntax delegateInvocation)
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
                        parameterIndex >= methodSymbol.Parameters.Length)
                    {
                        continue;
                    }

                    IParameterSymbol parameterSymbol =
                        methodSymbol.Parameters[parameterIndex];

                    if (IsExceptionFactoryDelegate(parameterSymbol.Type))
                    {
                        indexes.Add(parameterIndex);
                    }
                }
            }

            return indexes;
        }

        /// <summary>
        /// Determines whether the specified type is a delegate type that returns
        /// <see cref="System.Exception"/> or a derived exception type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is treated as an exception factory delegate;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsExceptionFactoryDelegate(
            ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
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

            return IsExceptionTypeByName(invokeMethod.ReturnType);
        }

        /// <summary>
        /// Determines whether the specified type symbol represents
        /// <see cref="System.Exception"/> or a derived type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
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
        /// Extracts an exception object creation from a lambda or anonymous method
        /// used as an exception factory argument.
        /// </summary>
        /// <param name="expression">The argument expression to inspect.</param>
        /// <returns>
        /// The extracted exception object creation if found;
        /// otherwise <see langword="null"/>.
        /// </returns>
        private static ObjectCreationExpressionSyntax? GetExceptionObjectCreation(
            ExpressionSyntax expression)
        {
            switch (expression)
            {
                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
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
                            is ObjectCreationExpressionSyntax objectCreation)
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
        /// The extracted exception object creation if found;
        /// otherwise <see langword="null"/>.
        /// </returns>
        private static ObjectCreationExpressionSyntax?
            GetExceptionObjectCreationFromLambdaBody(
                CSharpSyntaxNode body)
        {
            if (body is ObjectCreationExpressionSyntax directCreation)
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
    }
}
