using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{


    /// <summary>
    /// Provides stateless framework-contract, invocation-shape, and callback
    /// syntax facts for ConditionalWeakTable value analysis.
    /// </summary>
    internal static partial class ExceptionFlowConditionalWeakTableValueFactsProvider
    {
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
        internal static bool TryGetConditionalWeakTableGetValueParts(
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
                int parameterIndex = ExceptionFlowArgumentMapper.GetParameterIndex(
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
        internal static bool IsConditionalWeakTableFieldInitializedEmpty(
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
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
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

    }


    /// <summary>
    /// Provides stateless receiver-to-factory matching for supported
    /// ConditionalWeakTable invocations.
    /// </summary>
    internal static partial class ExceptionFlowConditionalWeakTableValueFactsProvider
    {
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
        internal static bool TryGetConditionalWeakTableFactoryForFieldReference(
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

    }


    /// <summary>
    /// Provides stateless return-expression discovery for supported callback
    /// syntax forms.
    /// </summary>
    internal static partial class ExceptionFlowConditionalWeakTableValueFactsProvider
    {
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
        internal static List<ExpressionSyntax> GetCallbackReturnExpressions(
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

            return ExceptionFlowEnumValueFactsProvider.GetSourceReturnExpressions(
                declaration);
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

    }

}
