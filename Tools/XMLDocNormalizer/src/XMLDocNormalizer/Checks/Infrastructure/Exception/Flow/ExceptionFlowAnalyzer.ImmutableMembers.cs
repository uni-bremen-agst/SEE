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

            if (HasFieldAssignmentOutsideInitializer(
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
                GetSemanticModelForSyntaxTree(
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
                if (IsThisDelegatingConstructor(
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
                else if (TryGetDirectConstructorAssignment(
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
                    if (HasConstructorAssignmentToMember(
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
                GetSemanticModelForSyntaxTree(
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
        /// Determines whether a constructor contains any assignment to the
        /// specified field or property.
        /// </summary>
        /// <param name="memberSymbol">
        /// The field or property whose assignments should be found.
        /// </param>
        /// <param name="constructorSymbol">
        /// The constructor to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the constructor syntax.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when at least one assignment exists or the
        /// constructor cannot be inspected safely; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool HasConstructorAssignmentToMember(
            ISymbol memberSymbol,
            IMethodSymbol constructorSymbol,
            SemanticModel semanticModel)
        {
            if (constructorSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return true;
            }

            SyntaxNode constructorNode =
                constructorSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            if (constructorNode
                is not ConstructorDeclarationSyntax constructor)
            {
                return true;
            }

            SemanticModel? constructorSemanticModel =
                GetSemanticModelForSyntaxTree(
                    semanticModel,
                    constructor.SyntaxTree);

            if (constructorSemanticModel == null)
            {
                return true;
            }

            foreach (AssignmentExpressionSyntax assignment
                     in constructor.DescendantNodes(
                             static node =>
                                 node
                                     is not
                                     AnonymousFunctionExpressionSyntax &&
                                 node
                                     is not
                                     LocalFunctionStatementSyntax)
                         .OfType<AssignmentExpressionSyntax>())
            {
                if (AssignmentTargetsSymbol(
                        assignment.Left,
                        memberSymbol,
                        constructorSemanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a static readonly field is assigned after its
        /// declaration initializer.
        /// </summary>
        /// <param name="fieldSymbol">
        /// The field to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain models for partial type
        /// declarations.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an additional assignment was found;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasFieldAssignmentOutsideInitializer(
            IFieldSymbol fieldSymbol,
            SemanticModel semanticModel)
        {
            foreach (SyntaxReference typeReference
                     in fieldSymbol.ContainingType
                         .DeclaringSyntaxReferences)
            {
                SyntaxNode typeNode =
                    typeReference.GetSyntax();

                SemanticModel? typeSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        typeNode.SyntaxTree);

                if (typeSemanticModel == null)
                {
                    return true;
                }

                IEnumerable<AssignmentExpressionSyntax> assignments =
                    typeNode.DescendantNodes(
                            static node =>
                                node
                                    is not
                                    AnonymousFunctionExpressionSyntax &&
                                node
                                    is not
                                    LocalFunctionStatementSyntax)
                        .OfType<AssignmentExpressionSyntax>();

                foreach (AssignmentExpressionSyntax assignment
                         in assignments)
                {
                    if (AssignmentTargetsSymbol(
                            assignment.Left,
                            fieldSymbol,
                            typeSemanticModel))
                    {
                        return true;
                    }
                }
            }

            return false;
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
                if (IsThisDelegatingConstructor(
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
                else if (TryGetDirectConstructorAssignment(
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
                    if (HasConstructorAssignmentToMember(
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
                GetSemanticModelForSyntaxTree(
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

        /// <summary>
        /// Determines whether a constructor delegates to another constructor
        /// of the same type through <c>this(...)</c>.
        /// </summary>
        /// <param name="constructorSymbol">
        /// The constructor to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the constructor has a
        /// <c>this(...)</c> initializer; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsThisDelegatingConstructor(
            IMethodSymbol constructorSymbol)
        {
            if (constructorSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode declarationNode =
                constructorSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            return declarationNode
                       is ConstructorDeclarationSyntax constructor &&
                   constructor.Initializer?
                       .ThisOrBaseKeyword
                       .IsKind(
                           SyntaxKind.ThisKeyword) == true;
        }

        /// <summary>
        /// Determines whether a declaration is a get-only auto-property
        /// without a custom getter body.
        /// </summary>
        /// <param name="propertyDeclaration">
        /// The property declaration to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the declaration is a supported get-only
        /// auto-property; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSupportedGetOnlyAutoProperty(
            PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.ExpressionBody != null ||
                propertyDeclaration.AccessorList == null ||
                propertyDeclaration.AccessorList.Accessors.Count != 1)
            {
                return false;
            }

            AccessorDeclarationSyntax accessor =
                propertyDeclaration.AccessorList.Accessors[0];

            return accessor.IsKind(
                       SyntaxKind.GetAccessorDeclaration) &&
                   accessor.Body == null &&
                   accessor.ExpressionBody == null &&
                   accessor.SemicolonToken.IsKind(
                       SyntaxKind.SemicolonToken);
        }

        /// <summary>
        /// Finds the single unconditional direct assignment to a field or
        /// property in a terminal constructor.
        /// </summary>
        /// <param name="memberSymbol">
        /// The field or property assigned by the constructor.
        /// </param>
        /// <param name="constructorSymbol">
        /// The constructor to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain the constructor model.
        /// </param>
        /// <param name="assignedExpression">
        /// The expression assigned to the member when successful.
        /// </param>
        /// <param name="constructorSemanticModel">
        /// The semantic model for the constructor declaration when
        /// successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if exactly one unconditional direct
        /// assignment was found; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetDirectConstructorAssignment(
            ISymbol memberSymbol,
            IMethodSymbol constructorSymbol,
            SemanticModel semanticModel,
            out ExpressionSyntax? assignedExpression,
            out SemanticModel? constructorSemanticModel)
        {
            assignedExpression =
                null;

            constructorSemanticModel =
                null;

            if (constructorSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode constructorNode =
                constructorSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax();

            if (constructorNode
                    is not ConstructorDeclarationSyntax constructor ||
                constructor.Body == null ||
                constructor.ExpressionBody != null ||
                constructor.Initializer?
                    .ThisOrBaseKeyword
                    .IsKind(
                        SyntaxKind.ThisKeyword) == true)
            {
                return false;
            }

            constructorSemanticModel =
                GetSemanticModelForSyntaxTree(
                    semanticModel,
                    constructor.SyntaxTree);

            if (constructorSemanticModel == null)
            {
                return false;
            }

            SemanticModel resolvedConstructorSemanticModel =
                constructorSemanticModel;

            List<AssignmentExpressionSyntax> assignments =
                constructor.DescendantNodes(
                        static node =>
                            node
                                is not
                                AnonymousFunctionExpressionSyntax &&
                            node
                                is not
                                LocalFunctionStatementSyntax)
                    .OfType<AssignmentExpressionSyntax>()
                    .Where(
                        assignment =>
                            assignment.IsKind(
                                SyntaxKind
                                    .SimpleAssignmentExpression) &&
                            AssignmentTargetsSymbol(
                                assignment.Left,
                                memberSymbol,
                                resolvedConstructorSemanticModel))
                    .ToList();

            if (assignments.Count != 1)
            {
                return false;
            }

            AssignmentExpressionSyntax assignment =
                assignments[0];

            if (assignment.Parent
                    is not ExpressionStatementSyntax expressionStatement ||
                expressionStatement.Parent != constructor.Body)
            {
                return false;
            }

            assignedExpression =
                assignment.Right;

            return true;
        }

        /// <summary>
        /// Determines whether an assignment target resolves to the specified
        /// member.
        /// </summary>
        /// <param name="targetExpression">
        /// The assignment target.
        /// </param>
        /// <param name="expectedSymbol">
        /// The expected field or property.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the target resolves to the expected
        /// member; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AssignmentTargetsSymbol(
            ExpressionSyntax targetExpression,
            ISymbol expectedSymbol,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    targetExpression);

            return symbolInfo.Symbol != null &&
                   SymbolEqualityComparer.Default.Equals(
                       symbolInfo.Symbol.OriginalDefinition,
                       expectedSymbol.OriginalDefinition);
        }
    }
}
