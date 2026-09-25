using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Resolves Roslyn symbols from one compilation into another compilation
    /// while preserving assembly identity and constructed containing types.
    /// </summary>
    internal static class ExceptionFlowCrossCompilationResolver
    {
        /// <summary>
        /// Resolves a method or accessor symbol into another compilation while
        /// preserving its constructed containing type when possible.
        /// </summary>
        /// <param name="methodSymbol">The method or accessor to resolve.</param>
        /// <param name="compilation">The destination compilation.</param>
        /// <returns>
        /// The corresponding method symbol, or <see langword="null"/> when no
        /// assembly-identical declaration can be resolved.
        /// </returns>
        public static IMethodSymbol? ResolveMethod(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            IMethodSymbol? originalMethod = ResolveAssociatedAccessor(methodSymbol, compilation);

            if (originalMethod == null)
            {
                string? declarationId =
                    DocumentationCommentId.CreateDeclarationId(methodSymbol.OriginalDefinition);

                if (string.IsNullOrEmpty(declarationId))
                {
                    return null;
                }

                originalMethod = GetUniqueMatch(
                    DocumentationCommentId
                        .GetSymbolsForDeclarationId(declarationId, compilation)
                        .OfType<IMethodSymbol>(),
                    candidate => HasSameAssemblyIdentity(candidate, methodSymbol));
            }

            if (originalMethod == null)
            {
                return null;
            }

            INamedTypeSymbol? containingType = ResolveNamedType(
                methodSymbol.ContainingType,
                compilation);

            if (containingType == null)
            {
                return originalMethod;
            }

            return ResolveMethodOnContainingType(originalMethod, containingType) ?? originalMethod;
        }

        /// <summary>
        /// Resolves a named type symbol into another compilation.
        /// </summary>
        /// <param name="typeSymbol">The named type to resolve.</param>
        /// <param name="compilation">The destination compilation.</param>
        /// <returns>
        /// The corresponding named type, or <see langword="null"/> when no
        /// assembly-identical type can be resolved.
        /// </returns>
        public static INamedTypeSymbol? ResolveNamedType(
            INamedTypeSymbol typeSymbol,
            Compilation compilation)
        {
            string? referenceId = DocumentationCommentId.CreateReferenceId(typeSymbol);

            if (string.IsNullOrEmpty(referenceId))
            {
                return null;
            }

            return GetUniqueMatch(
                DocumentationCommentId
                    .GetSymbolsForReferenceId(referenceId, compilation)
                    .OfType<INamedTypeSymbol>(),
                candidate => HasSameAssemblyIdentity(candidate, typeSymbol));
        }

        /// <summary>
        /// Resolves one method or accessor definition on a constructed or
        /// substituted containing type.
        /// </summary>
        /// <param name="methodSymbol">The method definition to resolve.</param>
        /// <param name="containingType">The containing type on which to resolve it.</param>
        /// <returns>
        /// The corresponding method member, or <see langword="null"/> when no
        /// matching member exists.
        /// </returns>
        public static IMethodSymbol? ResolveMethodOnContainingType(
            IMethodSymbol methodSymbol,
            INamedTypeSymbol containingType)
        {
            if (methodSymbol.AssociatedSymbol is IPropertySymbol associatedProperty)
            {
                IPropertySymbol? candidateProperty = GetUniqueMatch(
                    containingType.GetMembers().OfType<IPropertySymbol>(),
                    candidate => SymbolEqualityComparer.Default.Equals(
                        candidate.OriginalDefinition,
                        associatedProperty.OriginalDefinition));

                return GetAssociatedAccessor(candidateProperty, methodSymbol.MethodKind);
            }

            if (methodSymbol.AssociatedSymbol is IEventSymbol associatedEvent)
            {
                IEventSymbol? candidateEvent = GetUniqueMatch(
                    containingType.GetMembers().OfType<IEventSymbol>(),
                    candidate => SymbolEqualityComparer.Default.Equals(
                        candidate.OriginalDefinition,
                        associatedEvent.OriginalDefinition));

                return GetAssociatedAccessor(candidateEvent, methodSymbol.MethodKind);
            }

            return GetUniqueMatch(
                containingType.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>(),
                candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    methodSymbol.OriginalDefinition));
        }

        /// <summary>
        /// Resolves a stable property or field symbol into another compilation.
        /// </summary>
        /// <param name="memberSymbol">The property or field to resolve.</param>
        /// <param name="compilation">The destination compilation.</param>
        /// <returns>
        /// The corresponding property or field symbol, or
        /// <see langword="null"/> when the symbol kind is not supported or no
        /// assembly-identical declaration can be resolved.
        /// </returns>
        public static ISymbol? ResolveStableMember(
            ISymbol memberSymbol,
            Compilation compilation)
        {
            if (memberSymbol is not IPropertySymbol
                && memberSymbol is not IFieldSymbol)
            {
                return null;
            }

            string? declarationId =
                DocumentationCommentId.CreateDeclarationId(memberSymbol.OriginalDefinition);

            if (string.IsNullOrEmpty(declarationId))
            {
                return null;
            }

            return GetUniqueMatch(
                DocumentationCommentId.GetSymbolsForDeclarationId(declarationId, compilation),
                candidate => HasSameAssemblyIdentity(candidate, memberSymbol));
        }

        /// <summary>
        /// Resolves a property or event accessor through the declaration id of
        /// its associated symbol.
        /// </summary>
        /// <param name="methodSymbol">The accessor to resolve.</param>
        /// <param name="compilation">The destination compilation.</param>
        /// <returns>
        /// The corresponding accessor, or <see langword="null"/> when the
        /// method is not an accessor or cannot be resolved.
        /// </returns>
        private static IMethodSymbol? ResolveAssociatedAccessor(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            ISymbol? associatedSymbol = methodSymbol.AssociatedSymbol;

            if (associatedSymbol == null)
            {
                return null;
            }

            string? declarationId =
                DocumentationCommentId.CreateDeclarationId(associatedSymbol.OriginalDefinition);

            if (string.IsNullOrEmpty(declarationId))
            {
                return null;
            }

            ISymbol? resolvedAssociatedSymbol = GetUniqueMatch(
                DocumentationCommentId.GetSymbolsForDeclarationId(declarationId, compilation),
                candidate => HasSameAssemblyIdentity(candidate, associatedSymbol));

            return GetAssociatedAccessor(resolvedAssociatedSymbol, methodSymbol.MethodKind);
        }

        /// <summary>
        /// Gets the accessor of one associated property or event that
        /// corresponds to a method kind.
        /// </summary>
        /// <param name="associatedSymbol">The resolved property or event.</param>
        /// <param name="methodKind">The required accessor method kind.</param>
        /// <returns>
        /// The corresponding accessor, or <see langword="null"/> when the
        /// symbol does not expose the requested accessor.
        /// </returns>
        private static IMethodSymbol? GetAssociatedAccessor(
            ISymbol? associatedSymbol,
            MethodKind methodKind)
        {
            if (associatedSymbol is IPropertySymbol propertySymbol)
            {
                return methodKind switch
                {
                    MethodKind.PropertyGet => propertySymbol.GetMethod,
                    MethodKind.PropertySet => propertySymbol.SetMethod,
                    _ => null
                };
            }

            if (associatedSymbol is IEventSymbol eventSymbol)
            {
                return methodKind switch
                {
                    MethodKind.EventAdd => eventSymbol.AddMethod,
                    MethodKind.EventRemove => eventSymbol.RemoveMethod,
                    _ => null
                };
            }

            return null;
        }

        /// <summary>
        /// Determines whether two symbols belong to the same assembly identity.
        /// </summary>
        /// <param name="leftSymbol">The first symbol.</param>
        /// <param name="rightSymbol">The second symbol.</param>
        /// <returns>
        /// <see langword="true"/> when both containing assembly identities are
        /// equal; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasSameAssemblyIdentity(ISymbol leftSymbol, ISymbol rightSymbol)
        {
            AssemblyIdentity? leftIdentity = leftSymbol.ContainingAssembly?.Identity;
            AssemblyIdentity? rightIdentity = rightSymbol.ContainingAssembly?.Identity;

            return leftIdentity != null && leftIdentity.Equals(rightIdentity);
        }

        /// <summary>
        /// Returns the single exact symbol satisfying a resolution predicate.
        /// </summary>
        /// <typeparam name="TSymbol">The resolved Roslyn symbol type.</typeparam>
        /// <param name="candidates">The candidate symbols.</param>
        /// <param name="predicate">The exact identity predicate.</param>
        /// <returns>
        /// The unique matching symbol, or <see langword="null"/> when no
        /// symbol or more than one distinct symbol matches.
        /// </returns>
        private static TSymbol? GetUniqueMatch<TSymbol>(
            IEnumerable<TSymbol> candidates,
            Func<TSymbol, bool> predicate)
            where TSymbol : class, ISymbol
        {
            TSymbol? result = null;

            foreach (TSymbol candidate in candidates)
            {
                if (!predicate(candidate))
                {
                    continue;
                }

                if (result != null
                    && !SymbolEqualityComparer.Default.Equals(result, candidate))
                {
                    return null;
                }

                result = candidate;
            }

            return result;
        }
    }
}
