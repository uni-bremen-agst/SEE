using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowStableMemberFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact analysis for immutable fields and properties.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets value facts guaranteed by the initialization of an immutable
        /// member.
        /// </summary>
        /// <param name="memberSymbol">
        /// The field or property to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the current expression.
        /// </param>
        /// <param name="inspectedImmutableMembers">
        /// The immutable members currently being inspected.
        /// </param>
        /// <returns>
        /// The facts guaranteed for every supported initialization path.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetImmutableMemberValueFacts(
                ISymbol memberSymbol,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedImmutableMembers)
        {
            ISymbol normalizedMember =
                memberSymbol switch
                {
                    IFieldSymbol fieldSymbol =>
                        fieldSymbol.OriginalDefinition,

                    IPropertySymbol propertySymbol =>
                        propertySymbol.OriginalDefinition,

                    _ => memberSymbol
                };

            if (!inspectedImmutableMembers.Add(
                    normalizedMember))
            {
                return ExceptionFlowValueFacts.None;
            }

            try
            {
                return normalizedMember switch
                {
                    IFieldSymbol fieldSymbol
                        when fieldSymbol.IsStatic =>
                            GetStaticReadonlyFieldValueFacts(
                                fieldSymbol,
                                semanticModel,
                                inspectedImmutableMembers),

                    IFieldSymbol fieldSymbol =>
                        GetInstanceReadonlyFieldValueFacts(
                            fieldSymbol,
                            semanticModel,
                            inspectedImmutableMembers),

                    IPropertySymbol propertySymbol =>
                        GetGetOnlyPropertyValueFacts(
                            propertySymbol,
                            semanticModel,
                            inspectedImmutableMembers),

                    _ =>
                        ExceptionFlowValueFacts.None
                };
            }
            finally
            {
                inspectedImmutableMembers.Remove(
                    normalizedMember);
            }
        }

        /// <summary>
        /// Gets facts guaranteed by the initializer of a static readonly
        /// field.
        /// </summary>
        /// <param name="fieldSymbol">
        /// The field to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the current expression.
        /// </param>
        /// <param name="inspectedImmutableMembers">
        /// The immutable members currently being inspected.
        /// </param>
        /// <returns>
        /// The initializer facts if the field has no later assignment;
        /// otherwise <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetStaticReadonlyFieldValueFacts(
                IFieldSymbol fieldSymbol,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedImmutableMembers)
        {
            if (!fieldSymbol.IsStatic ||
                !fieldSymbol.IsReadOnly ||
                fieldSymbol.IsConst ||
                fieldSymbol.IsVolatile ||
                fieldSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return ExceptionFlowValueFacts.None;
            }

            if (ExceptionFlowImmutableMemberValueFactsProvider.HasFieldAssignmentOutsideInitializer(
                    fieldSymbol,
                    semanticModel))
            {
                return ExceptionFlowValueFacts.None;
            }

            SyntaxNode declarationNode =
                fieldSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            if (declarationNode
                    is not VariableDeclaratorSyntax variableDeclarator ||
                variableDeclarator.Initializer == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            SemanticModel? declarationSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                    semanticModel,
                    variableDeclarator.SyntaxTree);

            if (declarationSemanticModel == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExceptionFlowCallContext initializerContext =
                new(callableSymbol: null);

            return GetExpressionValueFacts(
                variableDeclarator.Initializer.Value,
                declarationSemanticModel,
                initializerContext,
                inspectedImmutableMembers);
        }

        /// <summary>
        /// Gets value facts guaranteed for an instance readonly field by its
        /// declaration initializer and every terminal instance constructor.
        /// </summary>
        /// <param name="fieldSymbol">
        /// The instance readonly field to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the current expression.
        /// </param>
        /// <param name="inspectedImmutableMembers">
        /// The immutable members currently being inspected.
        /// </param>
        /// <returns>
        /// The intersection of facts guaranteed by every supported terminal
        /// constructor, or <see cref="ExceptionFlowValueFacts.None"/> when the
        /// field initialization cannot be proven safely.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetInstanceReadonlyFieldValueFacts(
                IFieldSymbol fieldSymbol,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedImmutableMembers)
        {
            if (fieldSymbol.IsStatic ||
                !fieldSymbol.IsReadOnly ||
                fieldSymbol.IsConst ||
                fieldSymbol.IsVolatile ||
                fieldSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return ExceptionFlowValueFacts.None;
            }

            SyntaxNode declarationNode =
                fieldSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            if (declarationNode
                is not VariableDeclaratorSyntax variableDeclarator)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExceptionFlowValueFacts? commonFacts =
                null;

            bool foundTerminalConstructor =
                false;

            foreach (IMethodSymbol constructorSymbol
                     in fieldSymbol.ContainingType.InstanceConstructors)
            {
                if (ExceptionFlowImmutableMemberValueFactsProvider.IsThisDelegatingConstructor(
                        constructorSymbol))
                {
                    continue;
                }

                ExceptionFlowValueFacts constructorFacts;

                if (constructorSymbol.IsImplicitlyDeclared)
                {
                    if (!TryGetInstanceFieldInitializerFacts(
                            variableDeclarator,
                            semanticModel,
                            inspectedImmutableMembers,
                            out constructorFacts))
                    {
                        return ExceptionFlowValueFacts.None;
                    }
                }
                else if (ExceptionFlowImmutableMemberValueFactsProvider.TryGetDirectConstructorAssignment(
                             fieldSymbol,
                             constructorSymbol,
                             semanticModel,
                             out ExpressionSyntax? assignedExpression,
                             out SemanticModel?
                                 constructorSemanticModel) &&
                         assignedExpression != null &&
                         constructorSemanticModel != null)
                {
                    ExceptionFlowCallContext constructorContext =
                        new(constructorSymbol);

                    constructorFacts =
                        GetExpressionValueFacts(
                            assignedExpression,
                            constructorSemanticModel,
                            constructorContext,
                            inspectedImmutableMembers);
                }
                else
                {
                    if (ExceptionFlowImmutableMemberValueFactsProvider.HasConstructorAssignmentToMember(
                            fieldSymbol,
                            constructorSymbol,
                            semanticModel))
                    {
                        return ExceptionFlowValueFacts.None;
                    }

                    if (!TryGetInstanceFieldInitializerFacts(
                            variableDeclarator,
                            semanticModel,
                            inspectedImmutableMembers,
                            out constructorFacts))
                    {
                        return ExceptionFlowValueFacts.None;
                    }
                }

                commonFacts =
                    commonFacts == null
                        ? constructorFacts
                        : commonFacts.Value &
                          constructorFacts;

                foundTerminalConstructor =
                    true;
            }

            if (!foundTerminalConstructor)
            {
                return ExceptionFlowValueFacts.None;
            }

            return commonFacts?.Normalize() ??
                   ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Attempts to derive value facts from an instance-field declaration
        /// initializer.
        /// </summary>
        /// <param name="variableDeclarator">
        /// The field variable declarator.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the initializer model.
        /// </param>
        /// <param name="inspectedImmutableMembers">
        /// The immutable members currently being inspected.
        /// </param>
        /// <param name="facts">
        /// The initializer facts when analysis succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a supported initializer was analyzed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetInstanceFieldInitializerFacts(
            VariableDeclaratorSyntax variableDeclarator,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedImmutableMembers,
            out ExceptionFlowValueFacts facts)
        {
            facts =
                ExceptionFlowValueFacts.None;

            if (variableDeclarator.Initializer == null)
            {
                return false;
            }

            SemanticModel? initializerSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                    semanticModel,
                    variableDeclarator.SyntaxTree);

            if (initializerSemanticModel == null)
            {
                return false;
            }

            ExceptionFlowCallContext initializerContext =
                new(callableSymbol: null);

            facts =
                GetExpressionValueFacts(
                    variableDeclarator.Initializer.Value,
                    initializerSemanticModel,
                    initializerContext,
                    inspectedImmutableMembers);

            return true;
        }

        /// <summary>
        /// Gets value facts guaranteed by a get-only auto-property's
        /// declaration initializer and every terminal constructor.
        /// </summary>
        /// <param name="propertySymbol">
        /// The property to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the current expression.
        /// </param>
        /// <param name="inspectedImmutableMembers">
        /// The immutable members currently being inspected.
        /// </param>
        /// <returns>
        /// The intersection of facts guaranteed by every supported terminal
        /// initialization path.
        /// </returns>
        /// <remarks>
        /// Constructors that delegate through <c>this(...)</c> are not
        /// separate initialization endpoints. Their property initialization
        /// is performed by the terminal constructor reached by the
        /// delegation chain.
        /// </remarks>
        private static ExceptionFlowValueFacts
            GetGetOnlyPropertyValueFacts(
                IPropertySymbol propertySymbol,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedImmutableMembers)
        {
            if (propertySymbol.IsStatic ||
                propertySymbol.IsIndexer ||
                propertySymbol.SetMethod != null ||
                propertySymbol.ReturnsByRef ||
                propertySymbol.ReturnsByRefReadonly ||
                propertySymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return ExceptionFlowValueFacts.None;
            }

            SyntaxNode propertyNode =
                propertySymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            if (propertyNode
                    is not PropertyDeclarationSyntax propertyDeclaration ||
                !IsSupportedGetOnlyAutoProperty(
                    propertyDeclaration))
            {
                return ExceptionFlowValueFacts.None;
            }

            ExceptionFlowValueFacts? commonFacts =
                null;

            bool foundTerminalConstructor =
                false;

            foreach (IMethodSymbol constructorSymbol
                     in propertySymbol.ContainingType.InstanceConstructors)
            {
                if (ExceptionFlowImmutableMemberValueFactsProvider.IsThisDelegatingConstructor(
                        constructorSymbol))
                {
                    continue;
                }

                ExceptionFlowValueFacts constructorFacts;

                if (constructorSymbol.IsImplicitlyDeclared)
                {
                    if (!TryGetGetOnlyPropertyInitializerFacts(
                            propertyDeclaration,
                            semanticModel,
                            inspectedImmutableMembers,
                            out constructorFacts))
                    {
                        return ExceptionFlowValueFacts.None;
                    }
                }
                else if (ExceptionFlowImmutableMemberValueFactsProvider.TryGetDirectConstructorAssignment(
                             propertySymbol,
                             constructorSymbol,
                             semanticModel,
                             out ExpressionSyntax? assignedExpression,
                             out SemanticModel?
                                 constructorSemanticModel) &&
                         assignedExpression != null &&
                         constructorSemanticModel != null)
                {
                    ExceptionFlowCallContext constructorContext =
                        new(constructorSymbol);

                    constructorFacts =
                        GetExpressionValueFacts(
                            assignedExpression,
                            constructorSemanticModel,
                            constructorContext,
                            inspectedImmutableMembers);
                }
                else
                {
                    if (ExceptionFlowImmutableMemberValueFactsProvider.HasConstructorAssignmentToMember(
                            propertySymbol,
                            constructorSymbol,
                            semanticModel))
                    {
                        return ExceptionFlowValueFacts.None;
                    }

                    if (!TryGetGetOnlyPropertyInitializerFacts(
                            propertyDeclaration,
                            semanticModel,
                            inspectedImmutableMembers,
                            out constructorFacts))
                    {
                        return ExceptionFlowValueFacts.None;
                    }
                }

                commonFacts =
                    commonFacts == null
                        ? constructorFacts
                        : commonFacts.Value &
                          constructorFacts;

                foundTerminalConstructor =
                    true;
            }

            if (!foundTerminalConstructor)
            {
                return ExceptionFlowValueFacts.None;
            }

            return commonFacts?.Normalize() ??
                   ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Attempts to derive value facts from a get-only property's
        /// declaration initializer.
        /// </summary>
        /// <param name="propertyDeclaration">
        /// The property declaration.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the initializer model.
        /// </param>
        /// <param name="inspectedImmutableMembers">
        /// The immutable members currently being inspected.
        /// </param>
        /// <param name="facts">
        /// The initializer facts when analysis succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a supported initializer was analyzed;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetGetOnlyPropertyInitializerFacts(
            PropertyDeclarationSyntax propertyDeclaration,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedImmutableMembers,
            out ExceptionFlowValueFacts facts)
        {
            facts =
                ExceptionFlowValueFacts.None;

            if (propertyDeclaration.Initializer == null)
            {
                return false;
            }

            SemanticModel? initializerSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                    semanticModel,
                    propertyDeclaration.SyntaxTree);

            if (initializerSemanticModel == null)
            {
                return false;
            }

            ExceptionFlowCallContext initializerContext =
                new(callableSymbol: null);

            facts =
                GetExpressionValueFacts(
                    propertyDeclaration.Initializer.Value,
                    initializerSemanticModel,
                    initializerContext,
                    inspectedImmutableMembers);

            return true;
        }

    }
}
