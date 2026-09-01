using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning for source-owned
    /// <see cref="ConditionalWeakTable{TKey, TValue}"/> caches.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Stores cache-invariant results in weak semantic-model partitions so
        /// cached Roslyn symbols cannot outlive their semantic world.
        /// </summary>
        private static readonly ConditionalWeakTable<
            SemanticModel,
            ConditionalWeakTableValueFactCachePartition>
            conditionalWeakTableValueFactCaches = new();

        /// <summary>
        /// Determines whether a supported
        /// <see cref="ConditionalWeakTable{TKey, TValue}.GetValue"/> invocation
        /// is guaranteed to return a non-null value.
        /// </summary>
        /// <param name="invocation">The invocation to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the receiver is a private, empty,
        /// source-owned cache and every possible stored value is proven
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsConditionalWeakTableGetValueResultDefinitelyNonNull(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            if (!TryGetConditionalWeakTableGetValueParts(
                    invocation,
                    semanticModel,
                    out ExpressionSyntax? receiver,
                    out ExpressionSyntax? factory)
                || receiver == null
                || factory == null
                || semanticModel.GetSymbolInfo(receiver).Symbol
                    is not IFieldSymbol fieldSymbol
                || fieldSymbol.DeclaredAccessibility != Accessibility.Private
                || !fieldSymbol.IsStatic
                || !fieldSymbol.IsReadOnly)
            {
                return false;
            }

            ISymbol normalizedField = fieldSymbol.OriginalDefinition;

            if (!inspectedValueSources.Add(normalizedField))
            {
                return false;
            }

            try
            {
                ConditionalWeakTableValueFactCachePartition cache =
                    conditionalWeakTableValueFactCaches.GetValue(
                        semanticModel,
                        static _ =>
                            new ConditionalWeakTableValueFactCachePartition());

                if (cache.TryGetValue(normalizedField, out bool cachedResult))
                {
                    return cachedResult;
                }

                HashSet<ISymbol> invariantInspectedValueSources =
                    new(SymbolEqualityComparer.Default)
                    {
                        normalizedField
                    };

                bool result =
                    IsConditionalWeakTableFieldInitializedEmpty(
                        fieldSymbol,
                        semanticModel)
                    && AreAllConditionalWeakTableFieldValuesDefinitelyNonNull(
                        fieldSymbol,
                        semanticModel,
                        invariantInspectedValueSources);

                cache.Store(normalizedField, result);
                return result;
            }
            finally
            {
                inspectedValueSources.Remove(normalizedField);
            }
        }

        /// <summary>
        /// Attempts to resolve the receiver and value-factory argument of the
        /// exact framework <c>ConditionalWeakTable.GetValue</c> overload.
        /// </summary>
        /// <param name="invocation">The invocation to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <param name="receiver">
        /// The table receiver when the invocation matches.
        /// </param>
        /// <param name="factory">
        /// The value-factory expression when the invocation matches.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the invocation binds to the supported
        /// framework overload and both expressions were identified; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetConditionalWeakTableGetValueParts(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            out ExpressionSyntax? receiver,
            out ExpressionSyntax? factory)
        {
            receiver = null;
            factory = null;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
                || semanticModel.GetSymbolInfo(invocation).Symbol
                    is not IMethodSymbol selectedMethod
                || !IsConditionalWeakTableGetValueMethod(
                    selectedMethod,
                    semanticModel.Compilation))
            {
                return false;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            for (int index = 0; index < arguments.Count; index++)
            {
                ArgumentSyntax argument = arguments[index];
                int parameterIndex = GetParameterIndexForArgument(
                    argument,
                    index,
                    selectedMethod);

                if (parameterIndex == 1)
                {
                    factory = argument.Expression;
                    break;
                }
            }

            if (factory == null)
            {
                return false;
            }

            receiver = memberAccess.Expression;
            return true;
        }

        /// <summary>
        /// Determines whether a method is the exact framework
        /// <c>ConditionalWeakTable&lt;TKey, TValue&gt;.GetValue(
        /// TKey, CreateValueCallback)</c> overload.
        /// </summary>
        /// <param name="methodSymbol">The resolved method symbol.</param>
        /// <param name="compilation">
        /// The compilation used to resolve the framework type identity.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the method is the supported framework
        /// overload; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsConditionalWeakTableGetValueMethod(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            INamedTypeSymbol? conditionalWeakTableType =
                compilation.GetTypeByMetadataName(
                    "System.Runtime.CompilerServices.ConditionalWeakTable`2");

            if (conditionalWeakTableType == null)
            {
                return false;
            }

            IMethodSymbol originalMethod = methodSymbol.OriginalDefinition;

            foreach (IMethodSymbol candidate
                     in conditionalWeakTableType.GetMembers("GetValue")
                         .OfType<IMethodSymbol>())
            {
                if (candidate.IsStatic
                    || candidate.Arity != 0
                    || candidate.Parameters.Length != 2
                    || candidate.Parameters[1].Type
                        is not INamedTypeSymbol callbackType
                    || callbackType.TypeKind != TypeKind.Delegate
                    || !string.Equals(
                        callbackType.Name,
                        "CreateValueCallback",
                        StringComparison.Ordinal)
                    || !SymbolEqualityComparer.Default.Equals(
                        callbackType.ContainingType?.OriginalDefinition,
                        conditionalWeakTableType)
                    || callbackType.DelegateInvokeMethod
                        is not IMethodSymbol callbackInvoke
                    || callbackInvoke.Parameters.Length != 1
                    || !SymbolEqualityComparer.Default.Equals(
                        callbackInvoke.Parameters[0].Type,
                        conditionalWeakTableType.TypeParameters[0])
                    || !SymbolEqualityComparer.Default.Equals(
                        callbackInvoke.ReturnType,
                        conditionalWeakTableType.TypeParameters[1]))
                {
                    continue;
                }

                return SymbolEqualityComparer.Default.Equals(
                    originalMethod,
                    candidate.OriginalDefinition);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a source-owned table field starts as a new empty
        /// instance of its declared framework type.
        /// </summary>
        /// <param name="fieldSymbol">The candidate cache field.</param>
        /// <param name="semanticModel">
        /// A semantic model from the field's compilation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the field has one direct parameterless
        /// framework-table creation initializer; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsConditionalWeakTableFieldInitializedEmpty(
            IFieldSymbol fieldSymbol,
            SemanticModel semanticModel)
        {
            if (fieldSymbol.DeclaringSyntaxReferences.Length != 1
                || fieldSymbol.DeclaringSyntaxReferences[0].GetSyntax()
                    is not VariableDeclaratorSyntax declarator
                || declarator.Initializer == null)
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                GetSemanticModelForSyntaxTree(
                    semanticModel,
                    declarator.SyntaxTree);

            if (declarationSemanticModel == null
                || declarationSemanticModel.GetSymbolInfo(
                    declarator.Initializer.Value).Symbol
                    is not IMethodSymbol constructor
                || constructor.MethodKind != MethodKind.Constructor
                || constructor.Parameters.Length != 0)
            {
                return false;
            }

            return SymbolEqualityComparer.Default.Equals(
                constructor.ContainingType,
                fieldSymbol.Type);
        }

        /// <summary>
        /// Determines whether every source use of a private table field can
        /// store only callback results proven to be non-null.
        /// </summary>
        /// <param name="fieldSymbol">The table field to inspect.</param>
        /// <param name="semanticModel">
        /// A semantic model from the field's compilation.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The active field and callback return-analysis guard.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every field reference is the receiver of
        /// the exact supported <c>GetValue</c> overload and every factory is
        /// proven non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AreAllConditionalWeakTableFieldValuesDefinitelyNonNull(
            IFieldSymbol fieldSymbol,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            Compilation compilation = semanticModel.Compilation;
            bool foundReference = false;

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                SemanticModel treeSemanticModel =
                    compilation.GetSemanticModel(syntaxTree);

                foreach (IdentifierNameSyntax identifier
                         in syntaxTree.GetRoot()
                             .DescendantNodes()
                             .OfType<IdentifierNameSyntax>())
                {
                    if (!string.Equals(
                            identifier.Identifier.ValueText,
                            fieldSymbol.Name,
                            StringComparison.Ordinal)
                        || treeSemanticModel.GetSymbolInfo(identifier).Symbol
                            is not IFieldSymbol referencedField
                        || !SymbolEqualityComparer.Default.Equals(
                            referencedField.OriginalDefinition,
                            fieldSymbol.OriginalDefinition))
                    {
                        continue;
                    }

                    foundReference = true;

                    if (!TryGetConditionalWeakTableFactoryForFieldReference(
                            identifier,
                            fieldSymbol,
                            treeSemanticModel,
                            out ExpressionSyntax? factory)
                        || factory == null
                        || !IsCallbackReturnDefinitelyNonNull(
                            factory,
                            treeSemanticModel,
                            inspectedValueSources))
                    {
                        return false;
                    }
                }
            }

            return foundReference;
        }

        /// <summary>
        /// Attempts to identify the factory of the supported
        /// <c>GetValue</c> invocation whose receiver contains a field
        /// reference.
        /// </summary>
        /// <param name="fieldReference">The exact field reference.</param>
        /// <param name="fieldSymbol">The expected table field.</param>
        /// <param name="semanticModel">
        /// The semantic model associated with the reference.
        /// </param>
        /// <param name="factory">
        /// The resolved factory expression when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is part of the receiver of
        /// the exact supported invocation; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetConditionalWeakTableFactoryForFieldReference(
            IdentifierNameSyntax fieldReference,
            IFieldSymbol fieldSymbol,
            SemanticModel semanticModel,
            out ExpressionSyntax? factory)
        {
            factory = null;

            foreach (InvocationExpressionSyntax invocation
                     in fieldReference.Ancestors()
                         .OfType<InvocationExpressionSyntax>())
            {
                if (!TryGetConditionalWeakTableGetValueParts(
                        invocation,
                        semanticModel,
                        out ExpressionSyntax? receiver,
                        out ExpressionSyntax? candidateFactory)
                    || receiver == null
                    || candidateFactory == null
                    || !receiver.Span.Contains(fieldReference.Span)
                    || semanticModel.GetSymbolInfo(receiver).Symbol
                        is not IFieldSymbol receiverField
                    || !SymbolEqualityComparer.Default.Equals(
                        receiverField.OriginalDefinition,
                        fieldSymbol.OriginalDefinition))
                {
                    continue;
                }

                factory = candidateFactory;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether every normal return of one statically resolved
        /// source callback is proven to be non-null.
        /// </summary>
        /// <param name="factory">The delegate-valued factory expression.</param>
        /// <param name="semanticModel">
        /// The semantic model associated with the factory expression.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The active field and callback return-analysis guard.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when one stable source callback was resolved
        /// and all of its normal returns are non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsCallbackReturnDefinitelyNonNull(
            ExpressionSyntax factory,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            if (!TryResolveDelegateTarget(
                    factory,
                    semanticModel,
                    out IMethodSymbol? targetMethod)
                || targetMethod == null
                || targetMethod.ReturnsVoid
                || targetMethod.IsAsync
                || targetMethod.IsExtern
                || targetMethod.IsAbstract
                || targetMethod.IsIterator
                || targetMethod.ReturnsByRef
                || targetMethod.ReturnsByRefReadonly
                || targetMethod.ReducedFrom != null
                || RequiresSummaryRuntimeDispatch(targetMethod)
                || targetMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            ISymbol normalizedTarget = targetMethod.OriginalDefinition;

            if (!inspectedValueSources.Add(normalizedTarget))
            {
                return false;
            }

            try
            {
                SyntaxNode declaration =
                    targetMethod.DeclaringSyntaxReferences[0].GetSyntax();

                SemanticModel? declarationSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        declaration.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    return false;
                }

                List<KeyValuePair<int, ExceptionFlowValueFacts>> parameterFacts =
                    new();

                if (targetMethod.Parameters.Length > 0)
                {
                    parameterFacts.Add(
                        new KeyValuePair<int, ExceptionFlowValueFacts>(
                            0,
                            ExceptionFlowValueFacts.NonNull));
                }

                ExceptionFlowCallContext callbackContext =
                    new(targetMethod, parameterFacts);

                List<ExpressionSyntax> returnExpressions =
                    GetCallbackReturnExpressions(declaration);

                if (returnExpressions.Count == 0)
                {
                    return false;
                }

                foreach (ExpressionSyntax returnExpression in returnExpressions)
                {
                    if (!IsDefinitelyNonNull(
                            returnExpression,
                            declarationSemanticModel,
                            callbackContext,
                            inspectedValueSources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedValueSources.Remove(normalizedTarget);
            }
        }

        /// <summary>
        /// Gets the normal return expressions of a supported source callback.
        /// </summary>
        /// <param name="declaration">
        /// The resolved callback declaration.
        /// </param>
        /// <returns>
        /// The explicit return expressions, or an empty collection for an
        /// unsupported callback form.
        /// </returns>
        private static List<ExpressionSyntax> GetCallbackReturnExpressions(
            SyntaxNode declaration)
        {
            if (declaration is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                return GetLambdaReturnExpressions(parenthesizedLambda.Body);
            }

            if (declaration is SimpleLambdaExpressionSyntax simpleLambda)
            {
                return GetLambdaReturnExpressions(simpleLambda.Body);
            }

            if (declaration is AnonymousMethodExpressionSyntax anonymousMethod)
            {
                return GetBlockReturnExpressions(anonymousMethod.Block);
            }

            return GetSourceReturnExpressions(declaration);
        }

        /// <summary>
        /// Gets return expressions represented by an expression- or
        /// block-bodied lambda.
        /// </summary>
        /// <param name="body">The lambda body.</param>
        /// <returns>The represented return expressions.</returns>
        private static List<ExpressionSyntax> GetLambdaReturnExpressions(
            CSharpSyntaxNode body)
        {
            if (body is ExpressionSyntax expression)
            {
                return new List<ExpressionSyntax>
                {
                    expression
                };
            }

            return body is BlockSyntax block
                ? GetBlockReturnExpressions(block)
                : new List<ExpressionSyntax>();
        }

        /// <summary>
        /// Gets explicit return expressions from a callback block while
        /// excluding nested callables.
        /// </summary>
        /// <param name="block">The callback block.</param>
        /// <returns>The explicit normal return expressions.</returns>
        private static List<ExpressionSyntax> GetBlockReturnExpressions(
            BlockSyntax block)
        {
            return block.DescendantNodesAndSelf(
                    static node =>
                        node is not AnonymousFunctionExpressionSyntax
                        && node is not LocalFunctionStatementSyntax)
                .OfType<ReturnStatementSyntax>()
                .Where(static statement => statement.Expression != null)
                .Select(static statement => statement.Expression!)
                .ToList();
        }

        /// <summary>
        /// Stores immutable table-field invariant results for one semantic
        /// model.
        /// </summary>
        private sealed class ConditionalWeakTableValueFactCachePartition
        {
            /// <summary>
            /// Synchronizes cache access without holding the lock during
            /// Roslyn analysis.
            /// </summary>
            private readonly object gate = new();

            /// <summary>
            /// Stores results by Roslyn field identity.
            /// </summary>
            private readonly Dictionary<ISymbol, bool> entries =
                new(SymbolEqualityComparer.Default);

            /// <summary>
            /// Attempts to retrieve one cached invariant result.
            /// </summary>
            /// <param name="field">The normalized table field.</param>
            /// <param name="result">
            /// The cached result when present; otherwise the default value.
            /// </param>
            /// <returns>
            /// <see langword="true"/> when the field has a cached result;
            /// otherwise <see langword="false"/>.
            /// </returns>
            public bool TryGetValue(ISymbol field, out bool result)
            {
                lock (gate)
                {
                    return entries.TryGetValue(field, out result);
                }
            }

            /// <summary>
            /// Stores one immutable field-invariant result.
            /// </summary>
            /// <param name="field">The normalized table field.</param>
            /// <param name="result">The invariant result to store.</param>
            public void Store(ISymbol field, bool result)
            {
                lock (gate)
                {
                    entries.TryAdd(field, result);
                }
            }
        }
    }
}
