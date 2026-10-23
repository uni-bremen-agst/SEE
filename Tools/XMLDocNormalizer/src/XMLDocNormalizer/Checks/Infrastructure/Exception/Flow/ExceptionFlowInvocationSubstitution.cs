using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>Simultaneous, owner-scoped substitution of one invocation frame.</summary>
    internal sealed class ExceptionFlowInvocationSubstitution
    {
        private readonly Dictionary<string, ITypeSymbol> arguments = new(StringComparer.Ordinal);
        private readonly Compilation compilation;

        /// <summary>Creates the frame without recursively substituting replacement values.</summary>
        /// <param name="invocation">The invocation, distinct from its declaration handle.</param>
        /// <param name="compilation">The compilation owning the expression being interpreted.</param>
        internal ExceptionFlowInvocationSubstitution(ISymbol? invocation, Compilation compilation)
        {
            this.compilation = compilation;
            foreach (var pair in Bindings(invocation)) arguments.Add(Binder(pair.Formal), pair.Actual);
        }

        /// <summary>Gets all containing and method coordinates, in outer-to-inner order.</summary>
        /// <param name="invocation">The actual invocation.</param>
        /// <returns>The total declaration-to-invocation coordinate pairs.</returns>
        internal static IEnumerable<(ITypeParameterSymbol Formal, ITypeSymbol Actual)> Bindings(ISymbol? invocation)
        {
            Stack<INamedTypeSymbol> containers = new();
            for (INamedTypeSymbol? type = invocation as INamedTypeSymbol ?? invocation?.ContainingType;
                 type != null; type = type.ContainingType) containers.Push(type);
            foreach (INamedTypeSymbol type in containers)
                for (int ordinal = 0; ordinal < type.Arity; ordinal++)
                    yield return (type.OriginalDefinition.TypeParameters[ordinal], type.TypeArguments[ordinal]);
            if (invocation is IMethodSymbol method)
                for (int ordinal = 0; ordinal < method.Arity; ordinal++)
                    yield return (method.OriginalDefinition.TypeParameters[ordinal], method.TypeArguments[ordinal]);
        }

        /// <summary>Gets the complete existing neutral binder identity, not its display name.</summary>
        /// <param name="parameter">The formal type/method parameter.</param>
        /// <returns>The canonical owner/scope/ordinal key.</returns>
        internal static string Binder(ITypeParameterSymbol parameter)
            => CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateTypeIdentity(parameter));

        /// <summary>Applies this frame exactly once to each variable occurrence.</summary>
        /// <param name="type">A finite type expression in the body frame.</param>
        /// <returns>The corresponding type, preserving nested constructions.</returns>
        internal ITypeSymbol Apply(ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol parameter)
                return arguments.TryGetValue(Binder(parameter), out ITypeSymbol? actual)
                    ? new ExceptionFlowInvocationSubstitution(null, compilation).Apply(actual) : type;
            if (type.TypeKind == TypeKind.Dynamic)
                return compilation.GetSpecialType(SpecialType.System_Object);
            if (type is IArrayTypeSymbol array)
                return compilation.CreateArrayTypeSymbol(Apply(array.ElementType), array.Rank);
            if (type is IPointerTypeSymbol pointer)
                return compilation.CreatePointerTypeSymbol(Apply(pointer.PointedAtType));
            if (type is not INamedTypeSymbol named) return type;
            if (named.IsTupleType && named.TupleUnderlyingType != null) named = named.TupleUnderlyingType;
            if (named.IsNativeIntegerType) named = compilation.GetSpecialType(named.SpecialType);
            INamedTypeSymbol definition = named.OriginalDefinition;
            if (named.ContainingType != null)
            {
                var containing = (INamedTypeSymbol)Apply(named.ContainingType);
                INamedTypeSymbol[] candidates = containing.GetTypeMembers(named.Name, named.Arity)
                    .Where(candidate => candidate.MetadataName == named.MetadataName).ToArray();
                if (candidates.Length != 1) throw new ExceptionFlowProfileViolation("OwnerBindingFailure", "Nested type declaration is not unique.");
                definition = candidates[0];
            }
            return named.Arity == 0 ? definition
                : definition.Construct(named.TypeArguments.Select(Apply).ToArray());
        }

        /// <summary>Instantiates a selected callable/member in this caller frame.</summary>
        /// <param name="symbol">The symbol selected in the declaration body.</param>
        /// <returns>The complete constructed symbol.</returns>
        internal ISymbol ApplyCallable(ISymbol symbol)
        {
            // A reduced extension is a call-site view, not a member of its
            // static containing type. Recover every inferred/explicit method
            // coordinate before unique declaration lookup; receiver/value facts
            // already mapped by the caller remain in the unchanged context.
            if (symbol is IMethodSymbol { ReducedFrom: { } unreduced } reduced)
            {
                int remaining = 0;
                symbol = unreduced.Arity == 0 ? unreduced : unreduced.ConstructedFrom.Construct(
                    unreduced.TypeParameters.Select(parameter => reduced.GetTypeInferredDuringReduction(parameter)
                        ?? reduced.TypeArguments[remaining++]).ToArray());
            }
            if (symbol is INamedTypeSymbol type) return Apply(type);
            if (symbol.ContainingType == null) return symbol;
            INamedTypeSymbol containing = (INamedTypeSymbol)Apply(symbol.ContainingType);
            IEnumerable<ISymbol> candidates = containing.GetMembers(symbol.Name);
            if (symbol is IMethodSymbol { AssociatedSymbol: IPropertySymbol property })
                candidates = containing.GetMembers(property.Name).OfType<IPropertySymbol>()
                    .SelectMany(p => new[] { p.GetMethod, p.SetMethod }).OfType<ISymbol>();
            else if (symbol is IMethodSymbol { AssociatedSymbol: IEventSymbol @event })
                candidates = containing.GetMembers(@event.Name).OfType<IEventSymbol>()
                    .SelectMany(e => new[] { e.AddMethod, e.RemoveMethod, e.RaiseMethod }).OfType<ISymbol>();
            var declarationIdentity = RoslynCanonicalIdentityFactory.CreateCallableIdentity(symbol, true);
            ISymbol[] matches = candidates.Where(candidate =>
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(candidate, true).Equals(declarationIdentity)).ToArray();
            if (matches.Length != 1)
            {
                // Local/anonymous callables have a syntax-owned identity and no
                // containing-type member entry. Their lexical frame is checked
                // separately by admission; never pick a same-named member.
                if (symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction }) return symbol;
                throw new ExceptionFlowProfileViolation("OwnerBindingFailure", "Invocation member declaration is not unique.");
            }
            return matches[0] is IMethodSymbol method && method.Arity != 0 && symbol is IMethodSymbol invocation
                ? method.Construct(invocation.TypeArguments.Select(Apply).ToArray()) : matches[0];
        }
    }
}
