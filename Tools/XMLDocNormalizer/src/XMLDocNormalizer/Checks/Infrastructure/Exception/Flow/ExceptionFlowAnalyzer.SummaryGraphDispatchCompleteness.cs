using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains completeness analysis for known runtime dispatch targets and
    /// compatibility checks for generic receiver constraints.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Resolves every known runtime target and records uncertainty when
        /// additional implementations may exist outside the analyzed project
        /// closure.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method or accessor selected by compile-time binding.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type, including a possible type parameter, or
        /// <see langword="null"/> when no receiver type is available.
        /// </param>
        /// <param name="exactReceiverType">
        /// The exact runtime receiver type proven directly from the source,
        /// or <see langword="null"/>.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="fragment">
        /// The fragment receiving dispatch uncertainty.
        /// </param>
        /// <returns>
        /// The distinct known executable runtime targets.
        /// </returns>
        private static IReadOnlyList<IMethodSymbol>
            ResolveSummaryRuntimeTargets(
                IMethodSymbol methodSymbol,
                ITypeSymbol? receiverType,
                INamedTypeSymbol? exactReceiverType,
                ProjectClosureSemanticContext semanticContext,
                ExceptionFlowSummaryFragment fragment)
        {
            IReadOnlyList<IMethodSymbol> runtimeTargets =
                ResolveSummaryRuntimeTargets(
                    methodSymbol,
                    receiverType,
                    exactReceiverType,
                    semanticContext);

            if (!IsSummaryDispatchTargetSetComplete(
                    methodSymbol,
                    receiverType,
                    exactReceiverType,
                    semanticContext))
            {
                fragment.AddUncertainTarget(
                    CreateSummaryDispatchUncertainty(
                        methodSymbol,
                        receiverType));
            }

            return runtimeTargets;
        }

        /// <summary>
        /// Determines whether every runtime implementation reachable through
        /// one dispatch operation is represented by the analyzed target set.
        /// </summary>
        /// <param name="methodSymbol">
        /// The statically selected dispatch member.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type.
        /// </param>
        /// <param name="exactReceiverType">
        /// The exact runtime receiver type, when known.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when no additional runtime target can be
        /// introduced outside the analysis scope; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryDispatchTargetSetComplete(
            IMethodSymbol methodSymbol,
            ITypeSymbol? receiverType,
            INamedTypeSymbol? exactReceiverType,
            ProjectClosureSemanticContext semanticContext)
        {
            if (exactReceiverType != null ||
                methodSymbol.IsStatic ||
                !RequiresSummaryRuntimeDispatch(
                    methodSymbol))
            {
                return true;
            }

            if (receiverType == null ||
                receiverType is ITypeParameterSymbol)
            {
                return false;
            }

            if (receiverType
                is not INamedTypeSymbol namedReceiverType)
            {
                return false;
            }

            if (namedReceiverType.IsValueType ||
                namedReceiverType.IsSealed)
            {
                return true;
            }

            if (methodSymbol.ContainingType.TypeKind ==
                TypeKind.Interface)
            {
                return IsSummaryInterfaceDispatchTargetSetComplete(
                    methodSymbol,
                    namedReceiverType,
                    semanticContext);
            }

            return IsSummaryClassDispatchTargetSetComplete(
                methodSymbol,
                namedReceiverType,
                semanticContext);
        }

        /// <summary>
        /// Determines whether an interface-dispatch target set is complete for
        /// one static receiver type.
        /// </summary>
        /// <param name="interfaceMethod">
        /// The selected interface member.
        /// </param>
        /// <param name="receiverType">
        /// The static interface or class receiver type.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when no external implementation or
        /// reimplementation can introduce another target; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool
            IsSummaryInterfaceDispatchTargetSetComplete(
                IMethodSymbol interfaceMethod,
                INamedTypeSymbol receiverType,
                ProjectClosureSemanticContext semanticContext)
        {
            if (receiverType.TypeKind ==
                TypeKind.Interface)
            {
                return !CanSummaryInterfaceBeImplementedOutsideAnalysis(
                    receiverType,
                    semanticContext);
            }

            if (!CanSummaryTypeBeDerivedOutsideAnalysis(
                    receiverType,
                    semanticContext))
            {
                return true;
            }

            IMethodSymbol? implementationMethod =
                receiverType.FindImplementationForInterfaceMember(
                    interfaceMethod)
                as IMethodSymbol;

            if (implementationMethod != null &&
                CanSummaryMethodBeOverriddenOutsideAnalysis(
                    implementationMethod,
                    semanticContext))
            {
                return false;
            }

            return !CanSummaryInterfaceBeImplementedOutsideAnalysis(
                interfaceMethod.ContainingType,
                semanticContext);
        }

        /// <summary>
        /// Determines whether a virtual class-dispatch target set is complete
        /// for one static receiver type.
        /// </summary>
        /// <param name="methodSymbol">
        /// The selected virtual class member.
        /// </param>
        /// <param name="receiverType">
        /// The static class receiver type.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when external subclasses cannot introduce
        /// another override; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryClassDispatchTargetSetComplete(
            IMethodSymbol methodSymbol,
            INamedTypeSymbol receiverType,
            ProjectClosureSemanticContext semanticContext)
        {
            IMethodSymbol effectiveMethod =
                ResolveSummaryMostDerivedRuntimeOverride(
                    receiverType,
                    methodSymbol);

            if (effectiveMethod.IsSealed ||
                !CanSummaryTypeBeDerivedOutsideAnalysis(
                    receiverType,
                    semanticContext))
            {
                return true;
            }

            return !CanSummaryMethodBeOverriddenOutsideAnalysis(
                effectiveMethod,
                semanticContext);
        }

        /// <summary>
        /// Determines whether an interface may receive another implementation
        /// from an assembly outside the analysis scope.
        /// </summary>
        /// <param name="interfaceType">
        /// The interface type to inspect.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when an unanalysed assembly may implement
        /// the interface; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            CanSummaryInterfaceBeImplementedOutsideAnalysis(
                INamedTypeSymbol interfaceType,
                ProjectClosureSemanticContext semanticContext)
        {
            return interfaceType.TypeKind ==
                       TypeKind.Interface &&
                   IsSummaryTypeAccessibleOutsideAnalysis(
                       interfaceType,
                       semanticContext);
        }

        /// <summary>
        /// Determines whether an unanalysed assembly may derive from one class
        /// receiver type.
        /// </summary>
        /// <param name="typeSymbol">
        /// The class type to inspect.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the type is externally accessible,
        /// non-sealed, and exposes a usable constructor; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool CanSummaryTypeBeDerivedOutsideAnalysis(
            INamedTypeSymbol typeSymbol,
            ProjectClosureSemanticContext semanticContext)
        {
            if (typeSymbol.TypeKind !=
                    TypeKind.Class ||
                typeSymbol.IsSealed ||
                !IsSummaryTypeAccessibleOutsideAnalysis(
                    typeSymbol,
                    semanticContext))
            {
                return false;
            }

            foreach (IMethodSymbol constructor
                     in typeSymbol.InstanceConstructors)
            {
                if (IsSummaryMemberAccessibleOutsideAnalysis(
                        constructor,
                        semanticContext))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether an external subclass may override one virtual
        /// implementation method.
        /// </summary>
        /// <param name="methodSymbol">
        /// The virtual implementation to inspect.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the slot remains externally
        /// overridable; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            CanSummaryMethodBeOverriddenOutsideAnalysis(
                IMethodSymbol methodSymbol,
                ProjectClosureSemanticContext semanticContext)
        {
            if (methodSymbol.IsStatic ||
                methodSymbol.IsSealed ||
                (!methodSymbol.IsVirtual &&
                 !methodSymbol.IsAbstract &&
                 !methodSymbol.IsOverride))
            {
                return false;
            }

            return IsSummaryMemberAccessibleOutsideAnalysis(
                methodSymbol,
                semanticContext);
        }

        /// <summary>
        /// Determines whether a type can be referenced by code outside the
        /// analyzed project closure.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type whose effective accessibility should be inspected.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the type is public, protected, or
        /// exposed through an unanalysed friend assembly; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryTypeAccessibleOutsideAnalysis(
            INamedTypeSymbol typeSymbol,
            ProjectClosureSemanticContext semanticContext)
        {
            bool internalAccessIsOpen =
                !IsSummaryAssemblyInAnalysisScope(
                    typeSymbol.ContainingAssembly,
                    semanticContext) ||
                HasSummaryUnanalyzedFriendAssembly(
                    typeSymbol.ContainingAssembly,
                    semanticContext);

            for (INamedTypeSymbol? currentType = typeSymbol;
                 currentType != null;
                 currentType = currentType.ContainingType)
            {
                switch (currentType.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        continue;

                    case Accessibility.Internal:
                    case Accessibility.ProtectedAndInternal:
                        if (internalAccessIsOpen)
                        {
                            continue;
                        }

                        return false;

                    case Accessibility.Private:
                    case Accessibility.NotApplicable:
                    default:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a method or constructor can be accessed from an
        /// unanalysed assembly.
        /// </summary>
        /// <param name="memberSymbol">
        /// The member whose accessibility should be inspected.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when code outside the analyzed closure may
        /// access the member; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryMemberAccessibleOutsideAnalysis(
            ISymbol memberSymbol,
            ProjectClosureSemanticContext semanticContext)
        {
            bool internalAccessIsOpen =
                !IsSummaryAssemblyInAnalysisScope(
                    memberSymbol.ContainingAssembly,
                    semanticContext) ||
                HasSummaryUnanalyzedFriendAssembly(
                    memberSymbol.ContainingAssembly,
                    semanticContext);

            return memberSymbol.DeclaredAccessibility switch
            {
                Accessibility.Public =>
                    true,

                Accessibility.Protected =>
                    true,

                Accessibility.ProtectedOrInternal =>
                    true,

                Accessibility.Internal =>
                    internalAccessIsOpen,

                Accessibility.ProtectedAndInternal =>
                    internalAccessIsOpen,

                _ =>
                    false
            };
        }

        /// <summary>
        /// Determines whether an assembly represented in the analysis scope
        /// exposes its internals to at least one unanalysed friend assembly.
        /// </summary>
        /// <param name="assemblySymbol">
        /// The assembly whose friend declarations should be inspected.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when at least one declared friend assembly
        /// is outside the analysis scope; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasSummaryUnanalyzedFriendAssembly(
            IAssemblySymbol assemblySymbol,
            ProjectClosureSemanticContext semanticContext)
        {
            foreach (AttributeData attribute
                     in assemblySymbol.GetAttributes())
            {
                string? attributeName =
                    attribute.AttributeClass?.ToDisplayString();

                if (!string.Equals(
                        attributeName,
                        "System.Runtime.CompilerServices." +
                        "InternalsVisibleToAttribute",
                        StringComparison.Ordinal) ||
                    attribute.ConstructorArguments.Length != 1 ||
                    attribute.ConstructorArguments[0].Value
                        is not string friendDeclaration)
                {
                    continue;
                }

                string friendAssemblyName =
                    GetSummaryFriendAssemblyName(
                        friendDeclaration);

                if (string.IsNullOrWhiteSpace(
                        friendAssemblyName))
                {
                    continue;
                }

                bool friendIsAnalyzed =
                    semanticContext
                        .GetAnalysisCompilationScopes()
                        .Any(
                            scope =>
                                string.Equals(
                                    scope.Compilation
                                        .Assembly
                                        .Identity
                                        .Name,
                                    friendAssemblyName,
                                    StringComparison
                                        .OrdinalIgnoreCase));

                if (!friendIsAnalyzed)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts the simple assembly name from an
        /// <c>InternalsVisibleTo</c> declaration.
        /// </summary>
        /// <param name="friendDeclaration">
        /// The raw friend assembly declaration.
        /// </param>
        /// <returns>
        /// The trimmed simple assembly name.
        /// </returns>
        private static string GetSummaryFriendAssemblyName(
            string friendDeclaration)
        {
            int separatorIndex =
                friendDeclaration.IndexOf(
                    ',',
                    StringComparison.Ordinal);

            string assemblyName =
                separatorIndex < 0
                    ? friendDeclaration
                    : friendDeclaration[..separatorIndex];

            return assemblyName.Trim();
        }

        /// <summary>
        /// Determines whether one assembly is represented by a compilation in
        /// the semantic analysis scope.
        /// </summary>
        /// <param name="assemblySymbol">
        /// The assembly to locate.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the assembly is analyzed; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryAssemblyInAnalysisScope(
            IAssemblySymbol assemblySymbol,
            ProjectClosureSemanticContext semanticContext)
        {
            return semanticContext
                .GetAnalysisCompilationScopes()
                .Any(
                    scope =>
                        string.Equals(
                            scope.Compilation
                                .Assembly
                                .Identity
                                .Name,
                            assemblySymbol.Identity.Name,
                            StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates a stable uncertainty description for an incomplete runtime
        /// dispatch target set.
        /// </summary>
        /// <param name="methodSymbol">
        /// The statically selected method or accessor.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type.
        /// </param>
        /// <returns>
        /// The uncertainty description.
        /// </returns>
        private static string CreateSummaryDispatchUncertainty(
            IMethodSymbol methodSymbol,
            ITypeSymbol? receiverType)
        {
            string targetName =
                methodSymbol.ToDisplayString(
                    SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (string.IsNullOrWhiteSpace(
                    targetName))
            {
                targetName =
                    methodSymbol.Name;
            }

            string receiverName =
                receiverType?.ToDisplayString(
                    SymbolDisplayFormat.CSharpErrorMessageFormat) ??
                "<unknown receiver type>";

            if (string.IsNullOrWhiteSpace(
                    receiverName))
            {
                receiverName =
                    "<unknown receiver type>";
            }

            return
                $"Additional runtime dispatch targets for '{targetName}' " +
                $"on receiver type '{receiverName}' may exist outside " +
                "the analyzed project closure.";
        }

        /// <summary>
        /// Determines whether one source type can be used as a concrete
        /// runtime receiver for a named type or type-parameter receiver.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible concrete runtime receiver.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type or type parameter.
        /// </param>
        /// <param name="compilation">
        /// The compilation containing <paramref name="runtimeType"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the runtime type satisfies the receiver
        /// restriction; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryCompatibleRuntimeReceiver(
            INamedTypeSymbol runtimeType,
            ITypeSymbol? receiverType,
            Compilation compilation)
        {
            if (receiverType == null ||
                receiverType.TypeKind ==
                    TypeKind.Dynamic)
            {
                return true;
            }

            if (receiverType
                is INamedTypeSymbol namedReceiverType)
            {
                INamedTypeSymbol? scopedReceiverType =
                    ResolveSummaryTypeInCompilation(
                        namedReceiverType,
                        compilation);

                if (scopedReceiverType == null)
                {
                    return false;
                }

                if (scopedReceiverType.TypeKind ==
                    TypeKind.Interface)
                {
                    return FindSummaryMatchingInterface(
                               runtimeType,
                               scopedReceiverType) != null;
                }

                return FindSummaryMatchingBaseType(
                           runtimeType,
                           scopedReceiverType) != null;
            }

            if (receiverType
                is ITypeParameterSymbol typeParameter)
            {
                return SatisfiesSummaryTypeParameterConstraints(
                    runtimeType,
                    typeParameter,
                    compilation,
                    new HashSet<ITypeParameterSymbol>(
                        SymbolEqualityComparer.Default));
            }

            return false;
        }

        /// <summary>
        /// Determines whether one source type satisfies every relevant
        /// constraint of a type parameter.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible concrete type argument.
        /// </param>
        /// <param name="typeParameter">
        /// The constrained type parameter.
        /// </param>
        /// <param name="compilation">
        /// The compilation containing <paramref name="runtimeType"/>.
        /// </param>
        /// <param name="visitedTypeParameters">
        /// The recursively visited type parameters.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when all constraints are satisfied;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool SatisfiesSummaryTypeParameterConstraints(
            INamedTypeSymbol runtimeType,
            ITypeParameterSymbol typeParameter,
            Compilation compilation,
            HashSet<ITypeParameterSymbol> visitedTypeParameters)
        {
            if (!visitedTypeParameters.Add(
                    typeParameter))
            {
                return true;
            }

            if (typeParameter.HasReferenceTypeConstraint &&
                !runtimeType.IsReferenceType)
            {
                return false;
            }

            if (typeParameter.HasValueTypeConstraint &&
                !runtimeType.IsValueType)
            {
                return false;
            }

            if (typeParameter.HasUnmanagedTypeConstraint &&
                !runtimeType.IsUnmanagedType)
            {
                return false;
            }

            if (typeParameter.HasConstructorConstraint &&
                !HasSummaryPublicParameterlessConstructor(
                    runtimeType))
            {
                return false;
            }

            foreach (ITypeSymbol constraintType
                     in typeParameter.ConstraintTypes)
            {
                if (!SatisfiesSummaryConstraintType(
                        runtimeType,
                        constraintType,
                        compilation,
                        visitedTypeParameters))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether one runtime type satisfies an individual named
        /// or type-parameter constraint.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible concrete type argument.
        /// </param>
        /// <param name="constraintType">
        /// The constraint to evaluate.
        /// </param>
        /// <param name="compilation">
        /// The compilation containing <paramref name="runtimeType"/>.
        /// </param>
        /// <param name="visitedTypeParameters">
        /// The recursively visited type parameters.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the constraint is satisfied; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool SatisfiesSummaryConstraintType(
            INamedTypeSymbol runtimeType,
            ITypeSymbol constraintType,
            Compilation compilation,
            HashSet<ITypeParameterSymbol> visitedTypeParameters)
        {
            if (constraintType
                is ITypeParameterSymbol nestedTypeParameter)
            {
                return SatisfiesSummaryTypeParameterConstraints(
                    runtimeType,
                    nestedTypeParameter,
                    compilation,
                    visitedTypeParameters);
            }

            if (constraintType
                is not INamedTypeSymbol namedConstraintType)
            {
                return false;
            }

            INamedTypeSymbol? scopedConstraintType =
                ResolveSummaryTypeInCompilation(
                    namedConstraintType,
                    compilation);

            if (scopedConstraintType == null)
            {
                return false;
            }

            if (scopedConstraintType.TypeKind ==
                TypeKind.Interface)
            {
                return FindSummaryMatchingInterface(
                           runtimeType,
                           scopedConstraintType) != null;
            }

            return FindSummaryMatchingBaseType(
                       runtimeType,
                       scopedConstraintType) != null;
        }

        /// <summary>
        /// Determines whether one possible type argument satisfies a
        /// <c>new()</c> constraint.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible concrete type argument.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for value types or reference types exposing
        /// a public parameterless constructor; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool HasSummaryPublicParameterlessConstructor(
            INamedTypeSymbol runtimeType)
        {
            if (runtimeType.IsValueType)
            {
                return true;
            }

            return runtimeType.InstanceConstructors.Any(
                static constructor =>
                    constructor.Parameters.Length == 0 &&
                    constructor.DeclaredAccessibility ==
                        Accessibility.Public);
        }
    }
}
