using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains method-invocation and exception-factory analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Resolves method invocations within the specified node, recognizes
        /// known framework exception sources, and optionally analyzes invoked
        /// method bodies transitively.
        /// </summary>
        /// <param name="node">The node to inspect for invocations.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">
        /// The call-site facts known for the currently analyzed callable.
        /// </param>
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
                     in GetCurrentCallableDescendantsAndSelf
                         <InvocationExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol
                    is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                if (TryAddKnownFrameworkThrownExceptions(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        result,
                        callContext))
                {
                    continue;
                }

                if (mode == ExceptionFlowTraversalMode.Direct)
                {
                    continue;
                }

                if (methodSymbol.MethodKind ==
                    MethodKind.DelegateInvoke)
                {
                    AnalyzeDelegateInvocation(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        callContext);

                    continue;
                }

                CollectThrownExceptionsFromDelegateFactoryCall(
                    invocation,
                    methodSymbol,
                    semanticContext,
                    result);

                if (TryAnalyzeRecursiveRuntimeDispatch(
                        invocation,
                        methodSymbol,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        callContext))
                {
                    continue;
                }

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
                    MarkUncertain(
                        result,
                        methodSymbol);
                }
            }
        }

        /// <summary>
        /// Adds exceptions from a known framework throw helper while
        /// suppressing exception types whose preconditions are proven false
        /// at the current call site.
        /// </summary>
        /// <param name="invocation">
        /// The framework helper invocation.
        /// </param>
        /// <param name="methodSymbol">
        /// The resolved framework helper symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression analysis.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the invocation is a known framework
        /// exception source; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryAddKnownFrameworkThrownExceptions(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowCallContext callContext)
        {
            HashSet<INamedTypeSymbol> modeledExceptions =
                new(SymbolEqualityComparer.Default);

            if (!KnownFrameworkExceptionModel
                    .TryAddThrownExceptionTypes(
                        methodSymbol,
                        semanticModel.Compilation,
                        modeledExceptions))
            {
                return false;
            }

            ExceptionFlowValueFacts guardedArgumentFacts =
                GetGuardedArgumentFacts(
                    invocation,
                    methodSymbol,
                    semanticModel,
                    callContext);

            bool isArgumentNullGuard =
                KnownFrameworkExceptionModel
                    .IsArgumentNullThrowIfNull(
                        methodSymbol,
                        semanticModel.Compilation);

            bool isNullOrEmptyGuard =
                KnownFrameworkExceptionModel
                    .IsArgumentExceptionThrowIfNullOrEmpty(
                        methodSymbol,
                        semanticModel.Compilation);

            bool isNullOrWhiteSpaceGuard =
                KnownFrameworkExceptionModel
                    .IsArgumentExceptionThrowIfNullOrWhiteSpace(
                        methodSymbol,
                        semanticModel.Compilation);

            foreach (INamedTypeSymbol exceptionType
                     in modeledExceptions)
            {
                if (exceptionType == null)
                {
                    continue;
                }

                if (IsSuppressedKnownFrameworkException(
                        exceptionType,
                        semanticModel.Compilation,
                        guardedArgumentFacts,
                        isArgumentNullGuard,
                        isNullOrEmptyGuard,
                        isNullOrWhiteSpaceGuard))
                {
                    continue;
                }

                result.AddExceptionPath(
                    exceptionType,
                    CreateTerminalPath(
                        ExceptionFlowPathStepKind
                            .FrameworkThrowHelper,
                        methodSymbol,
                        invocation));
            }

            return true;
        }

        /// <summary>
        /// Gets the facts proven for the argument mapped to the first helper
        /// parameter.
        /// </summary>
        /// <param name="invocation">The helper invocation.</param>
        /// <param name="methodSymbol">
        /// The resolved helper symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>The proven facts for the guarded argument.</returns>
        private static ExceptionFlowValueFacts
            GetGuardedArgumentFacts(
                InvocationExpressionSyntax invocation,
                IMethodSymbol methodSymbol,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callContext)
        {
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

                if (parameterIndex != 0 ||
                    argument.RefKindKeyword.IsKind(
                        SyntaxKind.OutKeyword))
                {
                    continue;
                }

                return GetExpressionValueFacts(
                    argument.Expression,
                    semanticModel,
                    callContext);
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Determines whether a modeled framework exception is impossible
        /// because of proven value facts at the current call site.
        /// </summary>
        /// <param name="exceptionType">
        /// The modeled exception type.
        /// </param>
        /// <param name="compilation">
        /// The compilation used for framework type resolution.
        /// </param>
        /// <param name="guardedArgumentFacts">
        /// The facts proven for the guarded argument.
        /// </param>
        /// <param name="isArgumentNullGuard">
        /// Whether the invocation is
        /// <see cref="ArgumentNullException"/>.<c>ThrowIfNull</c>.
        /// </param>
        /// <param name="isNullOrEmptyGuard">
        /// Whether the invocation is
        /// <see cref="ArgumentException.ThrowIfNullOrEmpty"/>.
        /// </param>
        /// <param name="isNullOrWhiteSpaceGuard">
        /// Whether the invocation is
        /// <see cref="ArgumentException.ThrowIfNullOrWhiteSpace"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the exception is proven impossible;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSuppressedKnownFrameworkException(
            INamedTypeSymbol exceptionType,
            Compilation compilation,
            ExceptionFlowValueFacts guardedArgumentFacts,
            bool isArgumentNullGuard,
            bool isNullOrEmptyGuard,
            bool isNullOrWhiteSpaceGuard)
        {
            if ((isArgumentNullGuard ||
                 isNullOrEmptyGuard ||
                 isNullOrWhiteSpaceGuard) &&
                IsFrameworkType(
                    exceptionType,
                    compilation,
                    "System.ArgumentNullException"))
            {
                return guardedArgumentFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonNull);
            }

            if (isNullOrEmptyGuard &&
                IsFrameworkType(
                    exceptionType,
                    compilation,
                    "System.ArgumentException"))
            {
                return guardedArgumentFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonEmptyString);
            }

            if (isNullOrWhiteSpaceGuard &&
                IsFrameworkType(
                    exceptionType,
                    compilation,
                    "System.ArgumentException"))
            {
                return guardedArgumentFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonWhiteSpaceString);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a type symbol represents the specified
        /// framework type.
        /// </summary>
        /// <param name="actualType">The actual type symbol.</param>
        /// <param name="compilation">
        /// The compilation used for type resolution.
        /// </param>
        /// <param name="metadataName">
        /// The expected metadata name.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the symbols represent the same type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsFrameworkType(
            INamedTypeSymbol actualType,
            Compilation compilation,
            string metadataName)
        {
            INamedTypeSymbol? expectedType =
                compilation.GetTypeByMetadataName(metadataName);

            return expectedType != null &&
                   SymbolEqualityComparer.Default.Equals(
                       actualType.OriginalDefinition,
                       expectedType.OriginalDefinition);
        }

        /// <summary>
        /// Collects exception types from invocations where the callee throws
        /// the result of a delegate parameter invocation and the call site
        /// supplies a lambda or anonymous method that directly creates an
        /// exception object.
        /// </summary>
        /// <param name="invocation">
        /// The invocation to inspect.
        /// </param>
        /// <param name="methodSymbol">
        /// The resolved target method symbol.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        private static void
            CollectThrownExceptionsFromDelegateFactoryCall(
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

                if (!throwingDelegateParameterIndexes.Contains(
                        parameterIndex))
                {
                    continue;
                }

                ObjectCreationExpressionSyntax? creation =
                    GetExceptionObjectCreation(
                        argument.Expression);

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
                    creationSemanticModel.GetSymbolInfo(
                        creation.Type);

                if (creationSymbolInfo.Symbol
                    is INamedTypeSymbol typeSymbol)
                {
                    ExceptionFlowPathStep invocationStep =
                        CreatePathStep(
                            ExceptionFlowPathStepKind.MethodCall,
                            methodSymbol,
                            invocation);

                    ExceptionFlowPathStep factoryStep =
                        CreatePathStep(
                            ExceptionFlowPathStepKind
                                .DelegateExceptionFactory,
                            typeSymbol,
                            creation);

                    result.AddExceptionPath(
                        typeSymbol,
                        new ExceptionFlowPath(factoryStep)
                            .Prepend(invocationStep));
                }
            }
        }

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
        private static HashSet<int>
            FindThrowingDelegateParameterIndexes(
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
        private static ObjectCreationExpressionSyntax?
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
    }
}
