using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.Dto;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Performs transitive analysis of exceptions that may be thrown by a member
    /// by walking executable bodies and recursively inspecting invoked members.
    /// </summary>
    internal static class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Analyzes all exception types that may be thrown directly or transitively
        /// by the specified member.
        /// </summary>
        /// <param name="member">The member whose exception flow should be analyzed.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <returns>
        /// A result object containing all proven thrown exception types and a flag
        /// indicating whether at least one relevant transitive path was not decidable.
        /// </returns>
        public static ExceptionFlowAnalysisResult AnalyzeTransitivelyThrownExceptions(
            MemberDeclarationSyntax member,
            ProjectClosureSemanticContext semanticContext)
        {
            ExceptionFlowAnalysisResult result = new();

            if (!semanticContext.TryGetSemanticModel(member.SyntaxTree, out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return result;
            }

            HashSet<ISymbol> visited =
                new(SymbolEqualityComparer.Default);

            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? body) || body == null)
            {
                return result;
            }

            AnalyzeBody(body, semanticModel, semanticContext, result, visited);

            return result;
        }

        /// <summary>
        /// Analyzes a body node for directly thrown exceptions and recursively
        /// reachable callable symbols.
        /// </summary>
        /// <param name="body">The body node to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeBody(
            SyntaxNode body,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            AnalyzeThrows(body, semanticModel, result);
            AnalyzeInvocations(body, semanticModel, semanticContext, result, visited);
            AnalyzeObjectCreations(body, semanticModel, semanticContext, result, visited);
            AnalyzePropertyAndIndexerAccesses(body, semanticModel, semanticContext, result, visited);
        }

        /// <summary>
        /// Collects exception types that are thrown directly within the specified body.
        /// </summary>
        /// <param name="body">The body node to inspect for throw statements and throw expressions.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        private static void AnalyzeThrows(
            SyntaxNode body,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result)
        {
            foreach (ThrowStatementSyntax throwStatement in body.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>())
            {
                AddThrownExceptionType(result, semanticModel, throwStatement.Expression);
            }

            foreach (ThrowExpressionSyntax throwExpression in body.DescendantNodesAndSelf().OfType<ThrowExpressionSyntax>())
            {
                AddThrownExceptionType(result, semanticModel, throwExpression.Expression);
            }
        }

        /// <summary>
        /// Resolves the exception type from a thrown expression and adds it to the result
        /// if it represents an object creation of a named type.
        /// </summary>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="expression">The thrown expression to inspect.</param>
        private static void AddThrownExceptionType(
            ExceptionFlowAnalysisResult result,
            SemanticModel semanticModel,
            ExpressionSyntax? expression)
        {
            if (expression is not ObjectCreationExpressionSyntax creation)
            {
                return;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation.Type);

            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                result.ThrownExceptions.Add(typeSymbol);
            }
        }

        /// <summary>
        /// Resolves method invocations within the specified body and recursively
        /// analyzes the bodies of the invoked methods.
        /// </summary>
        /// <param name="body">The body node to inspect for invocations.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeInvocations(
            SyntaxNode body,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            foreach (InvocationExpressionSyntax invocation in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                CollectThrownExceptionsFromDelegateFactoryCall(
                    invocation,
                    methodSymbol,
                    semanticContext,
                    result);

                if (!visited.Add(methodSymbol))
                {
                    continue;
                }

                if (!AnalyzeSymbol(methodSymbol, semanticContext, result, visited))
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
                FindThrowingDelegateParameterIndexes(methodSymbol, semanticContext);

            if (throwingDelegateParameterIndexes.Count == 0)
            {
                return;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentSyntax argument = arguments[i];
                int parameterIndex = GetParameterIndexForArgument(argument, i, methodSymbol);

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

                if (!semanticContext.TryGetSemanticModel(creation.SyntaxTree, out SemanticModel creationSemanticModel) ||
                    creationSemanticModel == null)
                {
                    continue;
                }

                SymbolInfo creationSymbolInfo = creationSemanticModel.GetSymbolInfo(creation.Type);

                if (creationSymbolInfo.Symbol is INamedTypeSymbol typeSymbol)
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

            foreach (SyntaxReference syntaxRef in methodSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxRef.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(node.SyntaxTree, out SemanticModel nodeSemanticModel) ||
                    nodeSemanticModel == null)
                {
                    continue;
                }

                BaseMethodDeclarationSyntax? declaration = node as BaseMethodDeclarationSyntax;
                if (declaration == null)
                {
                    continue;
                }

                ParameterListSyntax? parameterList = declaration.ParameterList;
                if (parameterList == null)
                {
                    continue;
                }

                Dictionary<string, int> parameterNameToIndex = new(StringComparer.Ordinal);

                for (int i = 0; i < parameterList.Parameters.Count; i++)
                {
                    ParameterSyntax parameter = parameterList.Parameters[i];
                    parameterNameToIndex[parameter.Identifier.ValueText] = i;
                }

                IEnumerable<ThrowStatementSyntax> throwStatements =
                    declaration.DescendantNodes().OfType<ThrowStatementSyntax>();

                foreach (ThrowStatementSyntax throwStatement in throwStatements)
                {
                    if (throwStatement.Expression is not InvocationExpressionSyntax delegateInvocation)
                    {
                        continue;
                    }

                    if (delegateInvocation.Expression is not IdentifierNameSyntax identifier)
                    {
                        continue;
                    }

                    if (!parameterNameToIndex.TryGetValue(identifier.Identifier.ValueText, out int parameterIndex))
                    {
                        continue;
                    }

                    if (parameterIndex < 0 || parameterIndex >= methodSymbol.Parameters.Length)
                    {
                        continue;
                    }

                    IParameterSymbol parameterSymbol = methodSymbol.Parameters[parameterIndex];

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
        private static bool IsExceptionFactoryDelegate(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            IMethodSymbol? invokeMethod = namedType.DelegateInvokeMethod;
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
        /// <see langword="true"/> if the type is an exception type; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsExceptionTypeByName(ITypeSymbol typeSymbol)
        {
            INamedTypeSymbol? current = typeSymbol as INamedTypeSymbol;

            while (current != null)
            {
                if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Exception")
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Gets the effective target parameter index for an argument, taking named
        /// arguments into account.
        /// </summary>
        /// <param name="argument">The argument to inspect.</param>
        /// <param name="fallbackIndex">The positional fallback index.</param>
        /// <param name="methodSymbol">The target method symbol.</param>
        /// <returns>The resolved parameter index, or the fallback index if no named match exists.</returns>
        private static int GetParameterIndexForArgument(
            ArgumentSyntax argument,
            int fallbackIndex,
            IMethodSymbol methodSymbol)
        {
            if (argument.NameColon == null)
            {
                return fallbackIndex;
            }

            string name = argument.NameColon.Name.Identifier.ValueText;

            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                if (string.Equals(methodSymbol.Parameters[i].Name, name, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return fallbackIndex;
        }

        /// <summary>
        /// Extracts an exception object creation from a lambda or anonymous method
        /// used as an exception factory argument.
        /// </summary>
        /// <param name="expression">The argument expression to inspect.</param>
        /// <returns>
        /// The extracted exception object creation if found; otherwise <see langword="null"/>.
        /// </returns>
        private static ObjectCreationExpressionSyntax? GetExceptionObjectCreation(
            ExpressionSyntax expression)
        {
            switch (expression)
            {
                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                    return GetExceptionObjectCreationFromLambdaBody(parenthesizedLambda.Body);

                case SimpleLambdaExpressionSyntax simpleLambda:
                    return GetExceptionObjectCreationFromLambdaBody(simpleLambda.Body);

                case AnonymousMethodExpressionSyntax anonymousMethod:
                    if (anonymousMethod.Block != null)
                    {
                        ReturnStatementSyntax? returnStatement =
                            anonymousMethod.Block.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();

                        if (returnStatement?.Expression is ObjectCreationExpressionSyntax objectCreation)
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
        /// The extracted exception object creation if found; otherwise <see langword="null"/>.
        /// </returns>
        private static ObjectCreationExpressionSyntax? GetExceptionObjectCreationFromLambdaBody(
            CSharpSyntaxNode body)
        {
            if (body is ObjectCreationExpressionSyntax directCreation)
            {
                return directCreation;
            }

            if (body is BlockSyntax block)
            {
                ReturnStatementSyntax? returnStatement =
                    block.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();

                if (returnStatement?.Expression is ObjectCreationExpressionSyntax blockCreation)
                {
                    return blockCreation;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves constructor calls within the specified body and recursively
        /// analyzes the bodies of the called constructors.
        /// Object creations that are part of a direct throw are ignored here because
        /// they are already handled by direct throw analysis.
        /// </summary>
        /// <param name="body">The body node to inspect for object creation expressions.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeObjectCreations(
            SyntaxNode body,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            foreach (ObjectCreationExpressionSyntax creation in body.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>())
            {
                if (IsPartOfDirectThrow(creation))
                {
                    continue;
                }

                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation);

                if (symbolInfo.Symbol is not IMethodSymbol constructorSymbol)
                {
                    continue;
                }

                if (!visited.Add(constructorSymbol))
                {
                    continue;
                }

                if (!AnalyzeSymbol(constructorSymbol, semanticContext, result, visited))
                {
                    MarkUncertain(result, constructorSymbol);
                }
            }
        }

        /// <summary>
        /// Resolves property and indexer accesses within the specified body and recursively
        /// analyzes the bodies of the accessed members.
        /// </summary>
        /// <param name="body">The body node to inspect for property and indexer access.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzePropertyAndIndexerAccesses(
            SyntaxNode body,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            foreach (MemberAccessExpressionSyntax memberAccess in body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    if (!AnalyzePropertyLikeSymbol(propertySymbol, semanticContext, result, visited))
                    {
                        MarkUncertain(result, propertySymbol);
                    }
                }
            }

            foreach (ElementAccessExpressionSyntax elementAccess in body.DescendantNodesAndSelf().OfType<ElementAccessExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(elementAccess);

                if (symbolInfo.Symbol is IPropertySymbol indexerSymbol)
                {
                    if (!AnalyzePropertyLikeSymbol(indexerSymbol, semanticContext, result, visited))
                    {
                        MarkUncertain(result, indexerSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified object creation is part of a direct throw statement
        /// or throw expression and is therefore already covered by direct throw analysis.
        /// </summary>
        /// <param name="creation">The object creation to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the object creation is directly thrown; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsPartOfDirectThrow(ObjectCreationExpressionSyntax creation)
        {
            return creation.Parent is ThrowStatementSyntax
                || creation.Parent is ThrowExpressionSyntax;
        }

        /// <summary>
        /// Marks the given callable target as uncertain because its exception flow could not be analyzed.
        /// </summary>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="symbol">The symbol whose flow could not be decided.</param>
        private static void MarkUncertain(
            ExceptionFlowAnalysisResult result,
            ISymbol symbol)
        {
            string display = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (string.IsNullOrWhiteSpace(display))
            {
                display = symbol.Name;
            }

            if (!string.IsNullOrWhiteSpace(display))
            {
                result.UncertainTargets.Add(display);
            }
        }

        /// <summary>
        /// Analyzes a property-like symbol by first trying its getter symbol and
        /// then falling back to the property or indexer declaration itself.
        /// </summary>
        /// <param name="propertySymbol">The property or indexer symbol to analyze.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed for the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzePropertyLikeSymbol(
            IPropertySymbol propertySymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            bool analyzedGetter = false;

            if (propertySymbol.GetMethod != null && visited.Add(propertySymbol.GetMethod))
            {
                analyzedGetter = AnalyzeSymbol(propertySymbol.GetMethod, semanticContext, result, visited);
            }

            if (!analyzedGetter && visited.Add(propertySymbol))
            {
                return AnalyzeSymbol(propertySymbol, semanticContext, result, visited);
            }

            return analyzedGetter;
        }

        /// <summary>
        /// Analyzes the syntax declarations of a callable symbol and recursively
        /// processes any executable bodies found there.
        /// </summary>
        /// <param name="symbol">The callable symbol to analyze.</param>
        /// <param name="semanticContext">The project-closure semantic context.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed for the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSymbol(
            ISymbol symbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            bool analyzedAnyBody = false;

            if (symbol.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxRef.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(node.SyntaxTree, out SemanticModel nodeSemanticModel) ||
                    nodeSemanticModel == null)
                {
                    continue;
                }

                if (node is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(method, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, nodeSemanticModel, semanticContext, result, visited);
                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is ConstructorDeclarationSyntax constructor)
                {
                    if (SyntaxUtils.TryGetMemberBody(constructor, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, nodeSemanticModel, semanticContext, result, visited);
                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is PropertyDeclarationSyntax property)
                {
                    if (SyntaxUtils.TryGetMemberBody(property, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, nodeSemanticModel, semanticContext, result, visited);
                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter = property.AccessorList?.Accessors
                        .FirstOrDefault(static accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (!semanticContext.TryGetSemanticModel(getter.SyntaxTree, out SemanticModel getterSemanticModel) ||
                            getterSemanticModel == null)
                        {
                            continue;
                        }

                        if (getter.Body != null)
                        {
                            AnalyzeBody(getter.Body, getterSemanticModel, semanticContext, result, visited);
                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeBody(getter.ExpressionBody.Expression, getterSemanticModel, semanticContext, result, visited);
                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is IndexerDeclarationSyntax indexer)
                {
                    if (SyntaxUtils.TryGetMemberBody(indexer, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, nodeSemanticModel, semanticContext, result, visited);
                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter = indexer.AccessorList?.Accessors
                        .FirstOrDefault(static accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (!semanticContext.TryGetSemanticModel(getter.SyntaxTree, out SemanticModel getterSemanticModel) ||
                            getterSemanticModel == null)
                        {
                            continue;
                        }

                        if (getter.Body != null)
                        {
                            AnalyzeBody(getter.Body, getterSemanticModel, semanticContext, result, visited);
                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeBody(getter.ExpressionBody.Expression, getterSemanticModel, semanticContext, result, visited);
                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.Body != null)
                    {
                        AnalyzeBody(accessor.Body, nodeSemanticModel, semanticContext, result, visited);
                        analyzedAnyBody = true;
                    }
                    else if (accessor.ExpressionBody != null)
                    {
                        AnalyzeBody(accessor.ExpressionBody.Expression, nodeSemanticModel, semanticContext, result, visited);
                        analyzedAnyBody = true;
                    }
                }
            }

            return analyzedAnyBody;
        }
    }
}
