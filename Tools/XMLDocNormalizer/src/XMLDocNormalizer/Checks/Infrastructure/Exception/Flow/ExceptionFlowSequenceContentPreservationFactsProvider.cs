using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ExceptionFlowDataFlowFacts = XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowDataFlowFactsProvider.ExceptionFlowDataFlowFacts;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Determines whether existing sequence-content facts remain current
    /// across supported local, parameter, and source-helper observations.
    /// </summary>
    internal static class ExceptionFlowSequenceContentPreservationFactsProvider
    {
        /// <summary>
        /// Determines whether a local sequence reference is passed to one
        /// statically bound source helper whose corresponding parameter is
        /// only observed in ways that preserve the sequence contents.
        /// </summary>
        /// <param name="reference">
        /// The local sequence reference to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the call site.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the helper is source-available,
        /// statically bound, and every use of the corresponding parameter is
        /// proven read-only for the supplied materialized sequence; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool
            IsSourceHelperArgumentProvenToPreserveSequenceContents(
                IdentifierNameSyntax reference,
                SemanticModel semanticModel)
        {
            if (reference.Parent
                    is not ArgumentSyntax argument ||
                !ReferenceEquals(
                    argument.Expression,
                    reference) ||
                argument.Parent?.Parent
                    is not InvocationExpressionSyntax invocation ||
                !argument.RefKindKeyword.IsKind(
                    SyntaxKind.None))
            {
                return false;
            }

            ITypeSymbol? sourceType =
                semanticModel.GetTypeInfo(reference).Type;

            Conversion conversion =
                semanticModel.GetConversion(reference);

            if (sourceType == null ||
                conversion.IsUserDefined ||
                !ExceptionFlowSequenceCollectionFactsProvider.IsKnownMaterializedSequenceType(sourceType))
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol
                    is not IMethodSymbol selectedMethod ||
                selectedMethod.ReducedFrom != null ||
                selectedMethod.IsAbstract ||
                selectedMethod.IsExtern ||
                ExceptionFlowRuntimeDispatchClassifier.RequiresRuntimeDispatch(selectedMethod) ||
                selectedMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            int fallbackIndex =
                invocation.ArgumentList.Arguments.IndexOf(argument);

            if (fallbackIndex < 0)
            {
                return false;
            }

            int parameterIndex =
                ExceptionFlowArgumentMapper.GetParameterIndex(
                    argument,
                    fallbackIndex,
                    selectedMethod);

            if (parameterIndex < 0 ||
                parameterIndex >= selectedMethod.Parameters.Length)
            {
                return false;
            }

            IParameterSymbol parameterSymbol =
                selectedMethod.Parameters[parameterIndex];

            return parameterSymbol.RefKind == RefKind.None &&
                   DoesSourceParameterPreserveSequenceContents(
                       parameterSymbol,
                       semanticModel);
        }


        /// <summary>
        /// Determines whether every source-level use of one helper parameter
        /// preserves the contents of a materialized sequence passed to it.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The helper parameter receiving the sequence.
        /// </param>
        /// <param name="semanticModel">
        /// A semantic model from the caller compilation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every parameter reference is a null
        /// comparison, a supported collection-count read, or direct foreach
        /// enumeration; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool DoesSourceParameterPreserveSequenceContents(
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            if (parameterSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode parameterDeclaration =
                parameterSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            SyntaxNode? containingCallable =
                parameterDeclaration.Ancestors()
                    .FirstOrDefault(
                        static node =>
                            node is MethodDeclarationSyntax ||
                            node is LocalFunctionStatementSyntax);

            if (containingCallable == null)
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                    semanticModel,
                    containingCallable.SyntaxTree);

            if (declarationSemanticModel == null)
            {
                return false;
            }

            IEnumerable<IdentifierNameSyntax> references =
                containingCallable.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            ExpressionReferencesSymbol(
                                identifier,
                                parameterSymbol,
                                declarationSemanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsInsideNestedCallable(
                        reference,
                        containingCallable) ||
                    (!IsSupportedReadOnlySequenceObservation(
                         reference,
                         declarationSemanticModel) &&
                     !IsSupportedSequenceNullObservation(reference) &&
                     !IsDirectForeachSequenceObservation(reference)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a parameter reference is nested inside another
        /// callable and therefore represents a captured sequence reference.
        /// </summary>
        /// <param name="reference">
        /// The parameter reference to inspect.
        /// </param>
        /// <param name="containingCallable">
        /// The source helper whose parameter is being analyzed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is located inside a
        /// nested anonymous or local function; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool IsInsideNestedCallable(
            IdentifierNameSyntax reference,
            SyntaxNode containingCallable)
        {
            foreach (SyntaxNode ancestor in reference.Ancestors())
            {
                if (ReferenceEquals(
                        ancestor,
                        containingCallable))
                {
                    return false;
                }

                if (ancestor is AnonymousFunctionExpressionSyntax ||
                    ancestor is LocalFunctionStatementSyntax)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a sequence reference is used directly as the
        /// source of a foreach statement.
        /// </summary>
        /// <param name="reference">
        /// The sequence reference to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is the foreach source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsDirectForeachSequenceObservation(
            IdentifierNameSyntax reference)
        {
            if (reference.Parent
                    is ForEachStatementSyntax forEachStatement)
            {
                return ReferenceEquals(
                    forEachStatement.Expression,
                    reference);
            }

            return reference.Parent
                       is ForEachVariableStatementSyntax
                           forEachVariableStatement &&
                   ReferenceEquals(
                       forEachVariableStatement.Expression,
                       reference);
        }

        /// <summary>
        /// Determines whether a non-null element fact received for a sequence
        /// parameter is still valid at a foreach statement.
        /// </summary>
        /// <param name="foreachStatement">
        /// The foreach statement consuming the parameter.
        /// </param>
        /// <param name="parameterSymbol">
        /// The sequence parameter carrying the fact.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when no preceding operation or loop-body use
        /// can mutate or expose the sequence; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool IsSequenceParameterFactStillCurrent(
            ForEachStatementSyntax foreachStatement,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            if (!IsSequenceParameterFactStillCurrentAtUse(
                    foreachStatement.Expression,
                    parameterSymbol,
                    semanticModel))
            {
                return false;
            }

            IEnumerable<IdentifierNameSyntax> bodyReferences =
                foreachStatement.Statement
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            ExpressionReferencesSymbol(
                                identifier,
                                parameterSymbol,
                                semanticModel));

            return !bodyReferences.Any();
        }

        /// <summary>
        /// Determines whether a sequence-element fact received for a
        /// parameter remains valid at a specific expression in the callable's
        /// top-level statement block.
        /// </summary>
        /// <param name="expression">The parameter use being analyzed.</param>
        /// <param name="parameterSymbol">The parameter carrying the fact.</param>
        /// <param name="semanticModel">
        /// The semantic model used for reference analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when no preceding top-level statement can
        /// mutate or expose the sequence; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsSequenceParameterFactStillCurrentAtUse(
            ExpressionSyntax expression,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            StatementSyntax? useStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (useStatement?.Parent is not BlockSyntax block
                || block.Parent is not MethodDeclarationSyntax
                    and not LocalFunctionStatementSyntax)
            {
                return false;
            }

            foreach (StatementSyntax statement in block.Statements)
            {
                if (statement.SpanStart >= useStatement.SpanStart)
                {
                    break;
                }

                if (!DoesStatementPreserveSequenceParameterContents(
                        statement,
                        parameterSymbol,
                        semanticModel))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a statement before a foreach loop only observes
        /// a sequence parameter without mutating or exposing it.
        /// </summary>
        /// <param name="statement">
        /// The statement to inspect.
        /// </param>
        /// <param name="parameterSymbol">
        /// The sequence parameter whose element fact must remain valid.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every parameter reference is a
        /// supported read-only observation; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool DoesStatementPreserveSequenceParameterContents(
            StatementSyntax statement,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            IEnumerable<IdentifierNameSyntax> references =
                statement.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            ExpressionReferencesSymbol(
                                identifier,
                                parameterSymbol,
                                semanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsSupportedReadOnlySequenceObservation(
                        reference,
                        semanticModel)
                    || IsSupportedSequenceNullObservation(reference))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether a sequence reference is used only for a direct
        /// comparison with <see langword="null"/>.
        /// </summary>
        /// <param name="reference">
        /// The sequence reference to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for supported equality or inequality
        /// comparisons with <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool IsSupportedSequenceNullObservation(
            IdentifierNameSyntax reference)
        {
            if (reference.Parent
                is not BinaryExpressionSyntax comparison)
            {
                return false;
            }

            if (!comparison.IsKind(
                    SyntaxKind.EqualsExpression)
                && !comparison.IsKind(
                    SyntaxKind.NotEqualsExpression))
            {
                return false;
            }

            ExpressionSyntax otherExpression;

            if (ReferenceEquals(
                    comparison.Left,
                    reference))
            {
                otherExpression =
                    comparison.Right;
            }
            else if (ReferenceEquals(
                         comparison.Right,
                         reference))
            {
                otherExpression =
                    comparison.Left;
            }
            else
            {
                return false;
            }

            return otherExpression.IsKind(
                SyntaxKind.NullLiteralExpression);
        }

        /// <summary>
        /// Determines whether a local sequence has remained unchanged between
        /// its declaration and the current use site.
        /// </summary>
        /// <param name="expression">
        /// The current local-variable use.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol whose writes, mutations, and escapes are inspected.
        /// </param>
        /// <param name="variableDeclarator">
        /// The declaration containing the sequence initializer.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow and symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a supported path connects declaration
        /// and use and no intervening operation can replace, mutate, or expose
        /// the sequence; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsLocalSequenceInitializerStillCurrent(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            VariableDeclaratorSyntax variableDeclarator,
            SemanticModel semanticModel)
        {
            if (variableDeclarator.Parent?.Parent
                    is not LocalDeclarationStatementSyntax declarationStatement)
            {
                return false;
            }

            StatementSyntax? useStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (useStatement == null
                || useStatement.SyntaxTree != declarationStatement.SyntaxTree
                || useStatement.SpanStart <= declarationStatement.SpanStart)
            {
                return false;
            }

            StatementSyntax currentStatement = useStatement;

            while (currentStatement.Parent is BlockSyntax containingBlock)
            {
                int currentIndex =
                    containingBlock.Statements.IndexOf(currentStatement);

                if (currentIndex < 0)
                {
                    return false;
                }

                for (int index = currentIndex - 1; index >= 0; index--)
                {
                    StatementSyntax precedingStatement =
                        containingBlock.Statements[index];

                    if (ReferenceEquals(precedingStatement, declarationStatement))
                    {
                        return true;
                    }

                    ExceptionFlowDataFlowFacts dataFlow =
                        ExceptionFlowDataFlowFactsProvider.GetFacts(
                            precedingStatement,
                            semanticModel);
                    if (!dataFlow.Succeeded
                        || dataFlow.WrittenInside.Any(
                            writtenSymbol =>
                                SymbolEqualityComparer.Default.Equals(
                                    writtenSymbol,
                                    localSymbol))
                        || !DoesStatementPreserveLocalSequenceContents(
                            precedingStatement,
                            localSymbol,
                            semanticModel))
                    {
                        return false;
                    }
                }

                StatementSyntax? containingStatement =
                    ExceptionFlowDereferenceFactDiscovery.GetSafeContainingStatement(
                        containingBlock,
                        localSymbol,
                        semanticModel);
                if (containingStatement == null
                    || !DoesContainingStatementEntryPreserveLocalSequenceContents(
                        containingBlock,
                        localSymbol,
                        semanticModel))
                {
                    return false;
                }

                currentStatement = containingStatement;
            }

            return false;
        }

        /// <summary>
        /// Determines whether entering a supported nested statement can
        /// mutate or expose a local sequence before the nested block executes.
        /// </summary>
        /// <param name="block">The nested block containing the use.</param>
        /// <param name="localSymbol">The tracked local sequence.</param>
        /// <param name="semanticModel">
        /// The semantic model used for reference analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when entry into the block preserves the
        /// sequence contents; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool DoesContainingStatementEntryPreserveLocalSequenceContents(
            BlockSyntax block,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            SyntaxNode? entryExpression =
                block.Parent switch
                {
                    IfStatementSyntax ifStatement => ifStatement.Condition,
                    ElseClauseSyntax { Parent: IfStatementSyntax ifStatement } =>
                        ifStatement.Condition,
                    CommonForEachStatementSyntax forEachStatement =>
                        forEachStatement.Expression,
                    BlockSyntax => null,
                    _ => block
                };

            return entryExpression == null
                || !ReferenceEquals(entryExpression, block)
                    && DoesSyntaxPreserveLocalSequenceContents(
                        entryExpression,
                        localSymbol,
                        semanticModel);
        }

        /// <summary>
        /// Determines whether an intervening statement preserves the contents
        /// and ownership of a local sequence whose element facts are being
        /// reused.
        /// </summary>
        /// <param name="statement">
        /// The intervening statement to inspect.
        /// </param>
        /// <param name="localSymbol">
        /// The sequence local whose contents must remain unchanged.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the statement does not reference the
        /// sequence or only performs a supported read-only observation;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool DoesStatementPreserveLocalSequenceContents(
            StatementSyntax statement,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            return DoesSyntaxPreserveLocalSequenceContents(
                statement,
                localSymbol,
                semanticModel);
        }

        /// <summary>
        /// Determines whether syntax only observes a local sequence without
        /// mutating or exposing its contents.
        /// </summary>
        /// <param name="syntax">The syntax to inspect.</param>
        /// <param name="localSymbol">The tracked local sequence.</param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every reference is a supported
        /// read-only use; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool DoesSyntaxPreserveLocalSequenceContents(
            SyntaxNode syntax,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            IEnumerable<IdentifierNameSyntax> references =
                syntax.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            ExpressionReferencesSymbol(
                                identifier,
                                localSymbol,
                                semanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsSupportedReadOnlySequenceObservation(reference, semanticModel)
                    || IsSourceHelperArgumentProvenToPreserveSequenceContents(reference, semanticModel))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether a local sequence reference is a supported
        /// read-only observation that cannot explicitly replace, mutate, or
        /// expose the sequence contents.
        /// </summary>
        /// <param name="reference">
        /// The local sequence reference to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for member resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference only reads a known
        /// framework collection count or an array length; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool IsSupportedReadOnlySequenceObservation(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel)
        {
            if (reference.Parent
                    is not MemberAccessExpressionSyntax memberAccess ||
                !ReferenceEquals(
                    memberAccess.Expression,
                    reference))
            {
                return false;
            }

            SymbolInfo memberSymbolInfo =
                semanticModel.GetSymbolInfo(
                    memberAccess);

            if (memberSymbolInfo.Symbol
                    is not IPropertySymbol propertySymbol)
            {
                return false;
            }

            if (ExceptionFlowSequenceCollectionFactsProvider.IsFrameworkCollectionCountProperty(
                    propertySymbol))
            {
                return true;
            }

            if (!string.Equals(
                    propertySymbol.Name,
                    "Length",
                    StringComparison.Ordinal) ||
                propertySymbol.GetMethod == null ||
                propertySymbol.SetMethod != null ||
                propertySymbol.Parameters.Length != 0)
            {
                return false;
            }

            TypeInfo receiverTypeInfo =
                semanticModel.GetTypeInfo(
                    reference);

            return receiverTypeInfo.Type
                is IArrayTypeSymbol;
        }
    }
}
