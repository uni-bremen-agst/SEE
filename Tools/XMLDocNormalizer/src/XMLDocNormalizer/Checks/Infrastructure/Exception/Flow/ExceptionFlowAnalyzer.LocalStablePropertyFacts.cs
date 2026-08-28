using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning for stable properties of locally created
    /// objects.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets facts established for a stable property by the still-current
        /// object initializer of a guarded local receiver.
        /// </summary>
        /// <param name="expression">
        /// The property access being analyzed.
        /// </param>
        /// <param name="propertySymbol">
        /// The accessed property.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver and initializer analysis.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// Facts shared by every possible non-null locally created receiver
        /// represented by the current initializer.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetFactsProvenByCurrentLocalStablePropertyInitializer(
                ExpressionSyntax expression,
                IPropertySymbol propertySymbol,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callContext,
                HashSet<ISymbol> inspectedValueSources)
        {
            if (!IsSupportedStableAutoProperty(propertySymbol))
            {
                return ExceptionFlowValueFacts.None;
            }

            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression
                    is not MemberAccessExpressionSyntax memberAccess)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExpressionSyntax receiverExpression =
                UnwrapParenthesizedExpression(
                    memberAccess.Expression);

            SymbolInfo receiverSymbolInfo =
                semanticModel.GetSymbolInfo(
                    receiverExpression);

            if (receiverSymbolInfo.Symbol
                    is not ILocalSymbol receiverLocal
                || receiverLocal.DeclaringSyntaxReferences.Length != 1
                || receiverLocal.DeclaringSyntaxReferences[0].GetSyntax()
                    is not VariableDeclaratorSyntax variableDeclarator
                || variableDeclarator.Initializer == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExceptionFlowValueFacts receiverFacts =
                GetExpressionValueFacts(
                    receiverExpression,
                    semanticModel,
                    callContext,
                    inspectedValueSources);

            if (!receiverFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonNull)
                || !IsLocalInitializerStillCurrent(
                    unwrappedExpression,
                    receiverLocal,
                    variableDeclarator,
                    semanticModel))
            {
                return ExceptionFlowValueFacts.None;
            }

            if (!TryGetStablePropertyFactsFromLocalObjectSource(
                    variableDeclarator.Initializer.Value,
                    propertySymbol,
                    semanticModel,
                    callContext,
                    inspectedValueSources,
                    out bool hasObjectValue,
                    out ExceptionFlowValueFacts propertyFacts)
                || !hasObjectValue)
            {
                return ExceptionFlowValueFacts.None;
            }

            return propertyFacts.Normalize();
        }

        /// <summary>
        /// Gets facts for a stable property from all supported non-null object
        /// branches of a local initializer.
        /// </summary>
        /// <param name="sourceExpression">
        /// The initializer expression to inspect.
        /// </param>
        /// <param name="propertySymbol">
        /// The stable property whose facts are requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the initializer.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently being inspected recursively.
        /// </param>
        /// <param name="hasObjectValue">
        /// Whether the expression contains at least one supported non-null
        /// object-producing branch.
        /// </param>
        /// <param name="facts">
        /// Facts shared by all supported non-null object-producing branches.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every branch is either
        /// <see langword="null"/> or a supported object creation; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetStablePropertyFactsFromLocalObjectSource(
            ExpressionSyntax sourceExpression,
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources,
            out bool hasObjectValue,
            out ExceptionFlowValueFacts facts)
        {
            ExpressionSyntax unwrappedSource =
                UnwrapParenthesizedExpression(
                    sourceExpression);

            Optional<object?> constantValue =
                semanticModel.GetConstantValue(
                    unwrappedSource);

            if (constantValue.HasValue
                && constantValue.Value == null)
            {
                hasObjectValue = false;
                facts = ExceptionFlowValueFacts.None;
                return true;
            }

            if (unwrappedSource
                    is ConditionalExpressionSyntax conditionalExpression)
            {
                if (!TryGetStablePropertyFactsFromLocalObjectSource(
                        conditionalExpression.WhenTrue,
                        propertySymbol,
                        semanticModel,
                        callContext,
                        inspectedValueSources,
                        out bool trueHasObjectValue,
                        out ExceptionFlowValueFacts trueFacts)
                    || !TryGetStablePropertyFactsFromLocalObjectSource(
                        conditionalExpression.WhenFalse,
                        propertySymbol,
                        semanticModel,
                        callContext,
                        inspectedValueSources,
                        out bool falseHasObjectValue,
                        out ExceptionFlowValueFacts falseFacts))
                {
                    hasObjectValue = false;
                    facts = ExceptionFlowValueFacts.None;
                    return false;
                }

                hasObjectValue =
                    trueHasObjectValue
                    || falseHasObjectValue;

                if (trueHasObjectValue
                    && falseHasObjectValue)
                {
                    facts =
                        (trueFacts & falseFacts)
                        .Normalize();
                }
                else if (trueHasObjectValue)
                {
                    facts = trueFacts;
                }
                else
                {
                    facts = falseFacts;
                }

                return true;
            }

            InitializerExpressionSyntax? initializer;

            switch (unwrappedSource)
            {
                case ObjectCreationExpressionSyntax objectCreation:
                    initializer = objectCreation.Initializer;
                    break;

                case ImplicitObjectCreationExpressionSyntax implicitCreation:
                    initializer = implicitCreation.Initializer;
                    break;

                default:
                    hasObjectValue = false;
                    facts = ExceptionFlowValueFacts.None;
                    return false;
            }

            TypeInfo createdTypeInfo =
                semanticModel.GetTypeInfo(
                    unwrappedSource);

            if (createdTypeInfo.Type
                    is not INamedTypeSymbol createdType
                || !SymbolEqualityComparer.Default.Equals(
                    createdType.OriginalDefinition,
                    propertySymbol.ContainingType.OriginalDefinition))
            {
                hasObjectValue = false;
                facts = ExceptionFlowValueFacts.None;
                return false;
            }

            hasObjectValue = true;

            if (initializer != null
                && TryGetPropertyAssignmentFacts(
                    initializer,
                    propertySymbol,
                    semanticModel,
                    callContext,
                    inspectedValueSources,
                    out facts))
            {
                return true;
            }

            return TryGetStablePropertyDeclarationInitializerFacts(
                propertySymbol,
                semanticModel,
                callContext,
                inspectedValueSources,
                out facts);
        }

        /// <summary>
        /// Gets facts from an explicit assignment to one property in an object
        /// initializer.
        /// </summary>
        /// <param name="initializer">
        /// The object initializer to inspect.
        /// </param>
        /// <param name="propertySymbol">
        /// The property whose assigned value is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the initializer.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently being inspected recursively.
        /// </param>
        /// <param name="facts">
        /// The facts established by the matching property assignment.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the initializer explicitly assigns the
        /// requested property; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetPropertyAssignmentFacts(
            InitializerExpressionSyntax initializer,
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources,
            out ExceptionFlowValueFacts facts)
        {
            foreach (ExpressionSyntax initializerExpression
                     in initializer.Expressions)
            {
                if (initializerExpression
                        is not AssignmentExpressionSyntax assignment)
                {
                    continue;
                }

                SymbolInfo assignedSymbolInfo =
                    semanticModel.GetSymbolInfo(
                        assignment.Left);

                if (assignedSymbolInfo.Symbol
                        is not IPropertySymbol assignedProperty
                    || !SymbolEqualityComparer.Default.Equals(
                        assignedProperty.OriginalDefinition,
                        propertySymbol.OriginalDefinition))
                {
                    continue;
                }

                facts =
                    GetExpressionValueFacts(
                        assignment.Right,
                        semanticModel,
                        callContext,
                        inspectedValueSources);

                return true;
            }

            facts = ExceptionFlowValueFacts.None;
            return false;
        }

        /// <summary>
        /// Gets facts from the declaration initializer of a stable auto-property.
        /// </summary>
        /// <param name="propertySymbol">
        /// The property whose initializer is inspected.
        /// </param>
        /// <param name="semanticModel">
        /// A semantic model from the current compilation.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently being inspected recursively.
        /// </param>
        /// <param name="facts">
        /// The facts established by the property declaration initializer.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a declaration initializer could be analyzed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetStablePropertyDeclarationInitializerFacts(
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources,
            out ExceptionFlowValueFacts facts)
        {
            facts = ExceptionFlowValueFacts.None;

            if (propertySymbol.DeclaringSyntaxReferences.Length != 1
                || propertySymbol.DeclaringSyntaxReferences[0].GetSyntax()
                    is not PropertyDeclarationSyntax propertyDeclaration
                || propertyDeclaration.Initializer == null)
            {
                return false;
            }

            SemanticModel? initializerSemanticModel =
                GetSemanticModelForSyntaxTree(
                    semanticModel,
                    propertyDeclaration.SyntaxTree);

            if (initializerSemanticModel == null)
            {
                return false;
            }

            facts =
                GetExpressionValueFacts(
                    propertyDeclaration.Initializer.Value,
                    initializerSemanticModel,
                    callContext,
                    inspectedValueSources);

            return true;
        }
    }
}
