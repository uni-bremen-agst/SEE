using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Resolves canonical identities inside one explicit Roslyn compilation.
    /// </summary>
    /// <remarks>
    /// Resolution is fail closed. It returns a symbol only when exactly one
    /// candidate has the complete requested canonical identity.
    /// </remarks>
    internal sealed class RoslynCanonicalIdentityResolver
    {
        /// <summary>
        /// Initializes a resolver bound to one compilation identity universe.
        /// </summary>
        /// <param name="compilation">The destination compilation.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public RoslynCanonicalIdentityResolver(Compilation compilation)
        {
            ArgumentNullException.ThrowIfNull(compilation);

            Compilation = compilation;
        }

        /// <summary>
        /// Gets the compilation that owns every resolved symbol.
        /// </summary>
        /// <value>The value described by this property.</value>
        public Compilation Compilation { get; }

        /// <summary>
        /// Resolves a canonical named type in the bound compilation.
        /// </summary>
        /// <param name="identity">The canonical type identity.</param>
        /// <returns>
        /// The exact type, or <see langword="null"/> when resolution is
        /// unsupported, missing, or ambiguous.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        public ITypeSymbol? ResolveType(CanonicalTypeIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            switch (identity.Kind)
            {
                case CanonicalTypeIdentityKind.Dynamic:
                    return Compilation.DynamicType;

                case CanonicalTypeIdentityKind.Array:
                    ITypeSymbol? elementType = identity.ElementType == null
                        ? null
                        : ResolveType(identity.ElementType);

                    return elementType == null || identity.ArrayRank <= 0
                        ? null
                        : Compilation.CreateArrayTypeSymbol(elementType, identity.ArrayRank);

                case CanonicalTypeIdentityKind.Pointer:
                    ITypeSymbol? pointedAtType = identity.ElementType == null
                        ? null
                        : ResolveType(identity.ElementType);

                    return pointedAtType == null
                        ? null
                        : Compilation.CreatePointerTypeSymbol(pointedAtType);

                case CanonicalTypeIdentityKind.Named:
                    return ResolveNamedType(identity);

                case CanonicalTypeIdentityKind.TypeParameter:
                case CanonicalTypeIdentityKind.FunctionPointer:
                default:
                    return null;
            }
        }

        /// <summary>
        /// Resolves a canonical callable in the bound compilation.
        /// </summary>
        /// <param name="identity">The canonical callable identity.</param>
        /// <returns>
        /// The exact callable, or <see langword="null"/> when resolution is
        /// missing, unsupported, or ambiguous.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        public IMethodSymbol? ResolveCallable(CanonicalCallableIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            if (identity.SourceLocation != null)
            {
                return ResolveSourceCallable(identity);
            }

            if (identity.ContainingType == null
                || ResolveType(identity.ContainingType) is not INamedTypeSymbol containingType)
            {
                return null;
            }

            List<IMethodSymbol> matches = new();

            foreach (IMethodSymbol candidate in EnumerateCallableMembers(containingType))
            {
                IMethodSymbol? comparableCandidate = TryConstructMethod(candidate, identity);

                if (comparableCandidate != null
                    && RoslynCanonicalIdentityFactory.CreateCallableIdentity(comparableCandidate)
                        .Equals(identity))
                {
                    matches.Add(comparableCandidate);
                }
            }

            return matches.Count == 1
                ? matches[0]
                : null;
        }

        /// <summary>
        /// Resolves any callable symbol kind represented by the summary graph.
        /// </summary>
        /// <param name="identity">The canonical callable identity.</param>
        /// <returns>
        /// The exact method, property, or event symbol; otherwise
        /// <see langword="null"/> when resolution is missing or ambiguous.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        public ISymbol? ResolveCallableSymbol(CanonicalCallableIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            if (identity.Kind == CanonicalCallableKind.TypeDeclaration)
            {
                ISymbol? type = identity.ContainingType == null
                    ? null
                    : ResolveType(identity.ContainingType);

                return type != null
                    && RoslynCanonicalIdentityFactory.CreateCallableIdentity(type)
                        .Equals(identity)
                        ? type
                        : null;
            }

            if (identity.Kind == CanonicalCallableKind.NamespaceDeclaration)
            {
                return ResolveNamespace(identity);
            }

            if (identity.Kind == CanonicalCallableKind.FieldDeclaration)
            {
                if (identity.ContainingType == null
                    || ResolveType(identity.ContainingType) is not INamedTypeSymbol fieldContainingType)
                {
                    return null;
                }

                IFieldSymbol[] fields = fieldContainingType.GetMembers(identity.MetadataName)
                    .OfType<IFieldSymbol>()
                    .Where(
                        field => RoslynCanonicalIdentityFactory.CreateCallableIdentity(field)
                            .Equals(identity))
                    .ToArray();

                return fields.Length == 1
                    ? fields[0]
                    : null;
            }

            if (identity.Kind != CanonicalCallableKind.Property
                && identity.Kind != CanonicalCallableKind.Event)
            {
                return ResolveCallable(identity);
            }

            if (identity.ContainingType == null
                || ResolveType(identity.ContainingType) is not INamedTypeSymbol containingType)
            {
                return null;
            }

            ISymbol[] matches = containingType.GetMembers(identity.MetadataName)
                .Where(
                    candidate => identity.Kind == CanonicalCallableKind.Property
                        ? candidate is IPropertySymbol
                        : candidate is IEventSymbol)
                .Where(
                    candidate => RoslynCanonicalIdentityFactory
                        .CreateCallableIdentity(candidate)
                        .Equals(identity))
                .ToArray();

            return matches.Length == 1
                ? matches[0]
                : null;
        }

        /// <summary>
        /// Resolves a namespace-declaration graph root in the source assembly.
        /// </summary>
        /// <param name="identity">The identity value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private INamespaceSymbol? ResolveNamespace(CanonicalCallableIdentity identity)
        {
            CanonicalNamespaceIdentity? namespaceIdentity = identity.ContainingNamespace;

            if (namespaceIdentity == null
                || !RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(Compilation.Assembly)
                    .Equals(namespaceIdentity.Assembly))
            {
                return null;
            }

            INamespaceSymbol current = Compilation.Assembly.GlobalNamespace;

            if (!string.IsNullOrEmpty(namespaceIdentity.FullName))
            {
                foreach (string part in namespaceIdentity.FullName.Split('.'))
                {
                    INamespaceSymbol[] matches = current.GetNamespaceMembers()
                        .Where(candidate => StringComparer.Ordinal.Equals(candidate.Name, part))
                        .ToArray();

                    if (matches.Length != 1)
                    {
                        return null;
                    }

                    current = matches[0];
                }
            }

            return RoslynCanonicalIdentityFactory.CreateCallableIdentity(current)
                .Equals(identity)
                    ? current
                    : null;
        }

        /// <summary>
        /// Resolves a canonical stable-member identity in the bound compilation.
        /// </summary>
        /// <param name="identity">The canonical stable-member identity.</param>
        /// <returns>
        /// The exact property or field, or <see langword="null"/> when no
        /// unique exact match exists.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        public ISymbol? ResolveStableMember(CanonicalStableMemberIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            if (ResolveType(identity.ContainingType) is not INamedTypeSymbol containingType)
            {
                return null;
            }

            ISymbol[] matches = containingType
                .GetMembers(identity.MetadataName)
                .Where(
                    candidate =>
                        candidate is IFieldSymbol or IPropertySymbol)
                .Where(
                    candidate =>
                        RoslynCanonicalIdentityFactory
                            .CreateStableMemberIdentity(candidate)
                            .Equals(identity))
                .ToArray();

            return matches.Length == 1
                ? matches[0]
                : null;
        }

        /// <summary>
        /// Resolves a canonical named type from one exact assembly identity.
        /// </summary>
        /// <param name="identity">The identity value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private INamedTypeSymbol? ResolveNamedType(CanonicalTypeIdentity identity)
        {
            if (identity.Assembly == null || identity.MetadataName == null)
            {
                return null;
            }

            string metadataName = CreateFullMetadataName(identity);
            List<INamedTypeSymbol> matches = new();
            INamedTypeSymbol? compilationCandidate =
                Compilation.GetTypeByMetadataName(metadataName);

            if (TryMatchNamedType(compilationCandidate, identity) is INamedTypeSymbol matchedCompilationType)
            {
                return matchedCompilationType;
            }

            foreach (IAssemblySymbol assembly in EnumerateAssemblies())
            {
                if (!RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(assembly)
                    .Equals(identity.Assembly))
                {
                    continue;
                }

                INamedTypeSymbol? candidate = TryMatchNamedType(
                    assembly.GetTypeByMetadataName(metadataName),
                    identity);

                if (candidate != null)
                {
                    matches.Add(candidate);
                }
            }

            INamedTypeSymbol[] distinctMatches = matches
                .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                .ToArray();

            return distinctMatches.Length == 1
                ? distinctMatches[0]
                : null;
        }

        /// <summary>
        /// Applies canonical construction and verifies an exact named-type candidate.
        /// </summary>
        /// <param name="candidate">The candidate value.</param>
        /// <param name="identity">The identity value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private INamedTypeSymbol? TryMatchNamedType(
            INamedTypeSymbol? candidate,
            CanonicalTypeIdentity identity)
        {
            if (candidate == null)
            {
                return null;
            }

            CanonicalTypeIdentity[] typeArguments = identity.TypeArguments;

            if (typeArguments.Length > 0)
            {
                ITypeSymbol?[] resolvedArguments = typeArguments
                    .Select(ResolveType)
                    .ToArray();

                if (resolvedArguments.Any(static argument => argument == null)
                    || resolvedArguments.Length != candidate.TypeParameters.Length)
                {
                    return null;
                }

                candidate = candidate.Construct(
                    resolvedArguments.Cast<ITypeSymbol>().ToArray());
            }
            else if (identity.IsUnboundGenericType)
            {
                candidate = candidate.ConstructUnboundGenericType();
            }

            return RoslynCanonicalIdentityFactory.CreateTypeIdentity(candidate)
                .Equals(identity)
                    ? candidate
                    : null;
        }

        /// <summary>
        /// Enumerates the root assembly and exact referenced assemblies.
        /// </summary>
        /// <returns>The operation result.</returns>
        private IEnumerable<IAssemblySymbol> EnumerateAssemblies()
        {
            yield return Compilation.Assembly;

            foreach (IAssemblySymbol assembly in Compilation.SourceModule.ReferencedAssemblySymbols)
            {
                yield return assembly;
            }
        }

        /// <summary>
        /// Creates the namespace-qualified nested metadata name.
        /// </summary>
        /// <param name="identity">The identity value.</param>
        /// <returns>The operation result.</returns>
        private static string CreateFullMetadataName(CanonicalTypeIdentity identity)
        {
            Stack<string> names = new();
            CanonicalTypeIdentity? current = identity;

            while (current != null)
            {
                names.Push(current.MetadataName ?? string.Empty);
                current = current.ContainingType;
            }

            string nestedName = string.Join("+", names);
            CanonicalTypeIdentity outermost = identity;

            while (outermost.ContainingType != null)
            {
                outermost = outermost.ContainingType;
            }

            return string.IsNullOrEmpty(outermost.NamespaceName)
                ? nestedName
                : outermost.NamespaceName + "." + nestedName;
        }

        /// <summary>
        /// Enumerates methods and associated accessors exactly once.
        /// </summary>
        /// <param name="containingType">The containingType value.</param>
        /// <returns>The operation result.</returns>
        private static IEnumerable<IMethodSymbol> EnumerateCallableMembers(
            INamedTypeSymbol containingType)
        {
            HashSet<IMethodSymbol> methods = new(SymbolEqualityComparer.Default);

            foreach (ISymbol member in containingType.GetMembers())
            {
                if (member is IMethodSymbol method)
                {
                    methods.Add(method);
                }
                else if (member is IPropertySymbol property)
                {
                    if (property.GetMethod != null)
                    {
                        methods.Add(property.GetMethod);
                    }

                    if (property.SetMethod != null)
                    {
                        methods.Add(property.SetMethod);
                    }
                }
                else if (member is IEventSymbol eventSymbol)
                {
                    if (eventSymbol.AddMethod != null)
                    {
                        methods.Add(eventSymbol.AddMethod);
                    }

                    if (eventSymbol.RemoveMethod != null)
                    {
                        methods.Add(eventSymbol.RemoveMethod);
                    }
                }
            }

            return methods;
        }

        /// <summary>
        /// Applies requested method type arguments before exact comparison.
        /// </summary>
        /// <param name="method">The method value.</param>
        /// <param name="identity">The identity value.</param>
        /// <returns>The operation result.</returns>
        private IMethodSymbol? TryConstructMethod(
            IMethodSymbol method,
            CanonicalCallableIdentity identity)
        {
            CanonicalTypeIdentity[] typeArguments = identity.TypeArguments;

            if (typeArguments.Length == 0)
            {
                return method;
            }

            if (method.Arity != typeArguments.Length)
            {
                return null;
            }

            ITypeSymbol?[] resolvedArguments = typeArguments
                .Select(ResolveType)
                .ToArray();

            return resolvedArguments.Any(static argument => argument == null)
                ? null
                : method.Construct(resolvedArguments.Cast<ITypeSymbol>().ToArray());
        }

        /// <summary>
        /// Resolves a source-only local or anonymous callable by checksum and span.
        /// </summary>
        /// <param name="identity">The identity value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private IMethodSymbol? ResolveSourceCallable(
            CanonicalCallableIdentity identity)
        {
            CanonicalSourceLocationIdentity location = identity.SourceLocation!;
            List<IMethodSymbol> matches = new();

            foreach (SyntaxTree syntaxTree in Compilation.SyntaxTrees)
            {
                SourceText sourceText = syntaxTree.GetText();

                if (!StringComparer.Ordinal.Equals(
                        syntaxTree.FilePath ?? string.Empty,
                        location.DocumentName)
                    || !StringComparer.Ordinal.Equals(
                        sourceText.ChecksumAlgorithm.ToString(),
                        location.ChecksumAlgorithm)
                    || !StringComparer.Ordinal.Equals(
                        Convert.ToHexString(sourceText.GetChecksum().AsSpan()),
                        location.Checksum))
                {
                    continue;
                }

                SyntaxNode root = syntaxTree.GetRoot();
                TextSpan span = new(location.SpanStart, location.SpanLength);

                if (!root.FullSpan.Contains(span))
                {
                    continue;
                }

                SemanticModel semanticModel = Compilation.GetSemanticModel(syntaxTree);

                foreach (SyntaxNode node in root.DescendantNodesAndSelf()
                             .Where(candidate => candidate.Span == span))
                {
                    ISymbol? declaredSymbol = semanticModel.GetDeclaredSymbol(node);
                    IMethodSymbol? candidate = declaredSymbol as IMethodSymbol;

                    if (candidate == null
                        && semanticModel.GetOperation(node) is IAnonymousFunctionOperation operation)
                    {
                        candidate = operation.Symbol;
                    }

                    if (candidate != null
                        && RoslynCanonicalIdentityFactory.CreateCallableIdentity(candidate)
                            .Equals(identity))
                    {
                        matches.Add(candidate);
                    }
                }
            }

            return matches.Count == 1
                ? matches[0]
                : null;
        }
    }
}
