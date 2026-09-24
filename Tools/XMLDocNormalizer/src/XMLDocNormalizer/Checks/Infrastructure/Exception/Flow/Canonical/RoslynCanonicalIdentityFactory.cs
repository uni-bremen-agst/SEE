using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Converts active Roslyn identities into Roslyn-independent canonical
    /// identities.
    /// </summary>
    internal static class RoslynCanonicalIdentityFactory
    {
        /// <summary>
        /// Creates a canonical assembly identity.
        /// </summary>
        /// <param name="identity">The Roslyn assembly identity.</param>
        /// <returns>The canonical identity.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public static CanonicalAssemblyIdentity CreateAssemblyIdentity(
            AssemblyIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            Version version = identity.Version;

            return new CanonicalAssemblyIdentity(
                identity.Name,
                version.Major,
                version.Minor,
                version.Build,
                version.Revision,
                identity.CultureName ?? string.Empty,
                Convert.ToHexString(
                    (identity.HasPublicKey
                        ? identity.PublicKey
                        : identity.PublicKeyToken).AsSpan()),
                identity.HasPublicKey,
                identity.IsRetargetable,
                identity.ContentType == AssemblyContentType.WindowsRuntime
                    ? CanonicalAssemblyContentType.WindowsRuntime
                    : CanonicalAssemblyContentType.Default);
        }

        /// <summary>
        /// Creates a canonical assembly identity from an assembly symbol.
        /// </summary>
        /// <param name="assembly">The assembly symbol.</param>
        /// <returns>The canonical identity.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public static CanonicalAssemblyIdentity CreateAssemblyIdentity(
            IAssemblySymbol assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            return CreateAssemblyIdentity(assembly.Identity);
        }

        /// <summary>
        /// Creates a canonical module identity.
        /// </summary>
        /// <param name="module">The module symbol.</param>
        /// <returns>The canonical identity.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public static CanonicalModuleIdentity CreateModuleIdentity(
            IModuleSymbol module)
        {
            ArgumentNullException.ThrowIfNull(module);

            IAssemblySymbol assembly = module.ContainingAssembly;
            IModuleSymbol[] modules = assembly.Modules.ToArray();
            int ordinal = -1;

            for (int index = 0; index < modules.Length; index++)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        modules[index],
                        module))
                {
                    ordinal = index;
                    break;
                }
            }

            return new CanonicalModuleIdentity(
                CreateAssemblyIdentity(assembly),
                module.MetadataName,
                ordinal,
                moduleVersionId: null);
        }

        /// <summary>
        /// Creates a canonical type identity.
        /// </summary>
        /// <param name="type">The type symbol.</param>
        /// <returns>The canonical identity.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the Roslyn type kind cannot be represented without
        /// approximation.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public static CanonicalTypeIdentity CreateTypeIdentity(
            ITypeSymbol type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type is IDynamicTypeSymbol)
            {
                return CreateSimpleTypeIdentity(CanonicalTypeIdentityKind.Dynamic);
            }

            if (type is IArrayTypeSymbol arrayType)
            {
                return new CanonicalTypeIdentity(
                    CanonicalTypeIdentityKind.Array,
                    CanonicalNamedTypeKind.Unknown,
                    assembly: null,
                    module: null,
                    namespaceName: null,
                    metadataName: null,
                    containingType: null,
                    typeArguments: null,
                    isUnboundGenericType: false,
                    isNativeIntegerType: false,
                    CreateTypeIdentity(arrayType.ElementType),
                    arrayType.Rank,
                    typeParameterScope: null,
                    typeParameterOrdinal: null,
                    typeParameterOwnerAssembly: null,
                    typeParameterOwnerModule: null,
                    typeParameterOwnerDeclarationId: null,
                    functionPointerCallingConvention: null,
                    functionPointerReturn: null,
                    functionPointerParameters: null);
            }

            if (type is IPointerTypeSymbol pointerType)
            {
                return new CanonicalTypeIdentity(
                    CanonicalTypeIdentityKind.Pointer,
                    CanonicalNamedTypeKind.Unknown,
                    assembly: null,
                    module: null,
                    namespaceName: null,
                    metadataName: null,
                    containingType: null,
                    typeArguments: null,
                    isUnboundGenericType: false,
                    isNativeIntegerType: false,
                    CreateTypeIdentity(pointerType.PointedAtType),
                    arrayRank: 0,
                    typeParameterScope: null,
                    typeParameterOrdinal: null,
                    typeParameterOwnerAssembly: null,
                    typeParameterOwnerModule: null,
                    typeParameterOwnerDeclarationId: null,
                    functionPointerCallingConvention: null,
                    functionPointerReturn: null,
                    functionPointerParameters: null);
            }

            if (type is ITypeParameterSymbol typeParameter)
            {
                return CreateTypeParameterIdentity(typeParameter);
            }

            if (type is IFunctionPointerTypeSymbol functionPointer)
            {
                return CreateFunctionPointerIdentity(functionPointer);
            }

            if (type is INamedTypeSymbol namedType)
            {
                return CreateNamedTypeIdentity(namedType);
            }

            throw new NotSupportedException(
                $"Canonical type identity does not support Roslyn type kind '{type.TypeKind}'.");
        }

        /// <summary>
        /// Creates a canonical identity for a callable graph symbol.
        /// </summary>
        /// <param name="symbol">The method, property, or event symbol.</param>
        /// <param name="normalizeToOriginalDefinition">
        /// Whether to normalize the symbol to its original definition.
        /// </param>
        /// <returns>The canonical callable identity.</returns>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public static CanonicalCallableIdentity CreateCallableIdentity(
            ISymbol symbol,
            bool normalizeToOriginalDefinition = false)
        {
            ArgumentNullException.ThrowIfNull(symbol);

            return symbol switch
            {
                IMethodSymbol method => CreateCallableIdentity(
                    method,
                    normalizeToOriginalDefinition),
                IPropertySymbol property => CreatePropertyIdentity(
                    normalizeToOriginalDefinition
                        ? property.OriginalDefinition
                        : property),
                IEventSymbol eventSymbol => CreateEventIdentity(
                    normalizeToOriginalDefinition
                        ? eventSymbol.OriginalDefinition
                        : eventSymbol),
                INamedTypeSymbol namedType => CreateTypeDeclarationIdentity(
                    normalizeToOriginalDefinition
                        ? namedType.OriginalDefinition
                        : namedType),
                IFieldSymbol field => CreateFieldDeclarationIdentity(
                    normalizeToOriginalDefinition
                        ? field.OriginalDefinition
                        : field),
                INamespaceSymbol namespaceSymbol =>
                    CreateNamespaceDeclarationIdentity(namespaceSymbol),
                _ => throw new NotSupportedException(
                    $"Canonical callable identity does not support symbol kind '{symbol.Kind}'.")
            };
        }

        /// <summary>
        /// Creates the canonical graph-root identity of a namespace declaration.
        /// </summary>
        /// <param name="namespaceSymbol">The namespaceSymbol value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalCallableIdentity CreateNamespaceDeclarationIdentity(
            INamespaceSymbol namespaceSymbol)
        {
            IAssemblySymbol assembly = namespaceSymbol.ContainingAssembly
                ?? throw new NotSupportedException(
                    "A merged namespace without one declaring assembly cannot be canonicalized.");
            string fullName = GetNamespaceFullName(namespaceSymbol);

            return new CanonicalCallableIdentity(
                containingType: null,
                namespaceSymbol.Name,
                CanonicalCallableKind.NamespaceDeclaration,
                arity: 0,
                returnType: null,
                CanonicalRefKind.None,
                parameters: null,
                typeArguments: null,
                explicitInterfaceImplementations: null,
                reducedFrom: null,
                sourceLocation: null,
                new CanonicalNamespaceIdentity(
                    CreateAssemblyIdentity(assembly),
                    fullName));
        }

        /// <summary>
        /// Creates the canonical graph-root identity of a field declaration.
        /// </summary>
        /// <param name="field">The field value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalCallableIdentity CreateFieldDeclarationIdentity(
            IFieldSymbol field)
        {
            return new CanonicalCallableIdentity(
                CreateTypeIdentity(field.ContainingType),
                field.MetadataName,
                CanonicalCallableKind.FieldDeclaration,
                arity: 0,
                CreateTypeIdentity(field.Type),
                CanonicalRefKind.None,
                parameters: null,
                typeArguments: null,
                explicitInterfaceImplementations: null,
                reducedFrom: null,
                sourceLocation: null);
        }

        /// <summary>
        /// Creates the canonical graph-root identity of a type declaration.
        /// </summary>
        /// <param name="namedType">The namedType value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalCallableIdentity CreateTypeDeclarationIdentity(
            INamedTypeSymbol namedType)
        {
            CanonicalTypeIdentity typeIdentity = CreateTypeIdentity(namedType);

            return new CanonicalCallableIdentity(
                typeIdentity,
                namedType.MetadataName,
                CanonicalCallableKind.TypeDeclaration,
                namedType.Arity,
                typeIdentity,
                CanonicalRefKind.None,
                parameters: null,
                typeArguments: null,
                explicitInterfaceImplementations: null,
                reducedFrom: null,
                sourceLocation: null);
        }

        /// <summary>
        /// Creates a canonical callable identity.
        /// </summary>
        /// <param name="method">The method or accessor symbol.</param>
        /// <param name="normalizeToOriginalDefinition">
        /// Whether to use the original definition, matching summary-graph key
        /// semantics.
        /// </param>
        /// <returns>The canonical callable identity.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        public static CanonicalCallableIdentity CreateCallableIdentity(
            IMethodSymbol method,
            bool normalizeToOriginalDefinition = false)
        {
            ArgumentNullException.ThrowIfNull(method);

            IMethodSymbol selectedMethod = normalizeToOriginalDefinition
                ? method.OriginalDefinition
                : method;

            CanonicalCallableParameterIdentity[] parameters = selectedMethod.Parameters
                .Select(CreateParameterIdentity)
                .ToArray();

            CanonicalTypeIdentity[] typeArguments = selectedMethod.IsDefinition
                ? Array.Empty<CanonicalTypeIdentity>()
                : selectedMethod.TypeArguments
                    .Select(CreateTypeIdentity)
                    .ToArray();

            CanonicalCallableIdentity[] explicitImplementations =
                selectedMethod.ExplicitInterfaceImplementations
                    .Select(
                        implementation => CreateCallableIdentity(
                            implementation,
                            normalizeToOriginalDefinition: true))
                    .OrderBy(CreateDeterministicSortKey, StringComparer.Ordinal)
                    .ToArray();

            CanonicalCallableIdentity? reducedFrom = selectedMethod.ReducedFrom == null
                ? null
                : CreateCallableIdentity(
                    selectedMethod.ReducedFrom,
                    normalizeToOriginalDefinition: true);

            return new CanonicalCallableIdentity(
                CreateTypeIdentity(selectedMethod.ContainingType),
                selectedMethod.MetadataName,
                MapCallableKind(selectedMethod.MethodKind),
                selectedMethod.Arity,
                CreateTypeIdentity(selectedMethod.ReturnType),
                GetReturnRefKind(selectedMethod),
                parameters,
                typeArguments,
                explicitImplementations,
                reducedFrom,
                RequiresSourceIdentity(selectedMethod)
                    ? TryCreateSourceLocation(selectedMethod)
                    : null);
        }

        /// <summary>
        /// Creates a canonical property-root identity.
        /// </summary>
        /// <param name="property">The property value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalCallableIdentity CreatePropertyIdentity(
            IPropertySymbol property)
        {
            CanonicalCallableIdentity[] implementations = property.ExplicitInterfaceImplementations
                .Select(implementation => CreatePropertyIdentity(implementation.OriginalDefinition))
                .OrderBy(CreateDeterministicSortKey, StringComparer.Ordinal)
                .ToArray();

            return new CanonicalCallableIdentity(
                CreateTypeIdentity(property.ContainingType),
                property.MetadataName,
                CanonicalCallableKind.Property,
                arity: 0,
                CreateTypeIdentity(property.Type),
                MapRefKind(property.RefKind),
                property.Parameters.Select(CreateParameterIdentity).ToArray(),
                typeArguments: null,
                implementations,
                reducedFrom: null,
                sourceLocation: null);
        }

        /// <summary>
        /// Creates a canonical event-root identity.
        /// </summary>
        /// <param name="eventSymbol">The eventSymbol value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalCallableIdentity CreateEventIdentity(
            IEventSymbol eventSymbol)
        {
            CanonicalCallableIdentity[] implementations = eventSymbol.ExplicitInterfaceImplementations
                .Select(implementation => CreateEventIdentity(implementation.OriginalDefinition))
                .OrderBy(CreateDeterministicSortKey, StringComparer.Ordinal)
                .ToArray();

            return new CanonicalCallableIdentity(
                CreateTypeIdentity(eventSymbol.ContainingType),
                eventSymbol.MetadataName,
                CanonicalCallableKind.Event,
                arity: 0,
                CreateTypeIdentity(eventSymbol.Type),
                CanonicalRefKind.None,
                parameters: null,
                typeArguments: null,
                implementations,
                reducedFrom: null,
                sourceLocation: null);
        }

        /// <summary>
        /// Creates a canonical identity for a stable property or field.
        /// </summary>
        /// <param name="member">The stable member symbol.</param>
        /// <returns>The canonical member identity.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when <paramref name="member"/> is not a property or field.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        public static CanonicalStableMemberIdentity CreateStableMemberIdentity(
            ISymbol member)
        {
            ArgumentNullException.ThrowIfNull(member);

            ISymbol definition = member.OriginalDefinition;

            if (definition is IFieldSymbol field)
            {
                return new CanonicalStableMemberIdentity(
                    CreateTypeIdentity(field.ContainingType),
                    field.MetadataName,
                    CanonicalStableMemberKind.Field,
                    CreateTypeIdentity(field.Type),
                    field.IsStatic,
                    parameters: null);
            }

            if (definition is IPropertySymbol property)
            {
                return new CanonicalStableMemberIdentity(
                    CreateTypeIdentity(property.ContainingType),
                    property.MetadataName,
                    CanonicalStableMemberKind.Property,
                    CreateTypeIdentity(property.Type),
                    property.IsStatic,
                    property.Parameters.Select(CreateParameterIdentity).ToArray());
            }

            throw new NotSupportedException(
                $"Canonical stable-member identity does not support symbol kind '{member.Kind}'.");
        }

        /// <summary>
        /// Creates a deterministic structural sort key for a callable.
        /// </summary>
        /// <param name="identity">The callable identity.</param>
        /// <returns>The structural sort key.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        internal static string CreateDeterministicSortKey(
            CanonicalCallableIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            return CanonicalIdentityKeyWriter.Write(identity);
        }

        /// <summary>
        /// Creates a named-type identity after normalizing tuple types to their
        /// underlying metadata representation.
        /// </summary>
        /// <param name="namedType">The namedType value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalTypeIdentity CreateNamedTypeIdentity(
            INamedTypeSymbol namedType)
        {
            INamedTypeSymbol normalizedType = namedType.IsTupleType
                ? namedType.TupleUnderlyingType ?? namedType
                : namedType;

            CanonicalTypeIdentity[] typeArguments = normalizedType.IsDefinition
                ? Array.Empty<CanonicalTypeIdentity>()
                : normalizedType.TypeArguments
                    .Select(CreateTypeIdentity)
                    .ToArray();

            return new CanonicalTypeIdentity(
                CanonicalTypeIdentityKind.Named,
                MapNamedTypeKind(normalizedType.TypeKind),
                CreateAssemblyIdentity(normalizedType.ContainingAssembly),
                CreateModuleIdentity(normalizedType.ContainingModule),
                normalizedType.ContainingType == null
                    ? normalizedType.ContainingNamespace?.IsGlobalNamespace == true
                        ? string.Empty
                        : normalizedType.ContainingNamespace == null
                            ? string.Empty
                            : GetNamespaceFullName(normalizedType.ContainingNamespace)
                    : null,
                normalizedType.MetadataName,
                normalizedType.ContainingType == null
                    ? null
                    : CreateTypeIdentity(normalizedType.ContainingType),
                typeArguments,
                normalizedType.IsUnboundGenericType,
                normalizedType.IsNativeIntegerType,
                elementType: null,
                arrayRank: 0,
                typeParameterScope: null,
                typeParameterOrdinal: null,
                typeParameterOwnerAssembly: null,
                typeParameterOwnerModule: null,
                typeParameterOwnerDeclarationId: null,
                functionPointerCallingConvention: null,
                functionPointerReturn: null,
                functionPointerParameters: null);
        }

        /// <summary>
        /// Builds a namespace name from its individual metadata-name segments.
        /// </summary>
        /// <param name="namespaceSymbol">The namespace to inspect.</param>
        /// <returns>The dot-separated namespace name, or an empty string for the global namespace.</returns>
        private static string GetNamespaceFullName(INamespaceSymbol namespaceSymbol)
        {
            List<string> segments = new();
            INamespaceSymbol? current = namespaceSymbol;

            while (current != null && !current.IsGlobalNamespace)
            {
                segments.Add(current.MetadataName);
                current = current.ContainingNamespace;
            }

            segments.Reverse();
            return string.Join('.', segments);
        }

        /// <summary>
        /// Creates a canonical type-parameter identity with an authoritative
        /// declaration owner key.
        /// </summary>
        /// <param name="typeParameter">The typeParameter value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalTypeIdentity CreateTypeParameterIdentity(
            ITypeParameterSymbol typeParameter)
        {
            ISymbol owner = typeParameter.ContainingSymbol;
            string? ownerDeclarationId = DocumentationCommentId.CreateDeclarationId(
                owner.OriginalDefinition);

            if (string.IsNullOrWhiteSpace(ownerDeclarationId))
            {
                CanonicalSourceLocationIdentity? location = TryCreateSourceLocation(owner);

                if (location == null)
                {
                    throw new NotSupportedException(
                        "A type parameter without a metadata or source declaration identity cannot be canonicalized.");
                }

                ownerDeclarationId = CreateSourceOwnerKey(location);
            }

            return new CanonicalTypeIdentity(
                CanonicalTypeIdentityKind.TypeParameter,
                CanonicalNamedTypeKind.Unknown,
                assembly: null,
                module: null,
                namespaceName: null,
                metadataName: null,
                containingType: null,
                typeArguments: null,
                isUnboundGenericType: false,
                isNativeIntegerType: false,
                elementType: null,
                arrayRank: 0,
                typeParameter.TypeParameterKind == TypeParameterKind.Method
                    ? CanonicalTypeParameterScope.Method
                    : CanonicalTypeParameterScope.Type,
                typeParameter.Ordinal,
                CreateAssemblyIdentity(typeParameter.ContainingAssembly),
                CreateModuleIdentity(typeParameter.ContainingModule),
                ownerDeclarationId,
                functionPointerCallingConvention: null,
                functionPointerReturn: null,
                functionPointerParameters: null);
        }

        /// <summary>
        /// Creates a canonical function-pointer identity.
        /// </summary>
        /// <param name="functionPointer">The functionPointer value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalTypeIdentity CreateFunctionPointerIdentity(
            IFunctionPointerTypeSymbol functionPointer)
        {
            IMethodSymbol signature = functionPointer.Signature;
            CanonicalFunctionPointerParameter returnSignature =
                new(
                    CreateTypeIdentity(signature.ReturnType),
                    GetReturnRefKind(signature));

            CanonicalFunctionPointerParameter[] parameters = signature.Parameters
                .Select(
                    parameter => new CanonicalFunctionPointerParameter(
                        CreateTypeIdentity(parameter.Type),
                        MapRefKind(parameter.RefKind)))
                .ToArray();

            return new CanonicalTypeIdentity(
                CanonicalTypeIdentityKind.FunctionPointer,
                CanonicalNamedTypeKind.Unknown,
                assembly: null,
                module: null,
                namespaceName: null,
                metadataName: null,
                containingType: null,
                typeArguments: null,
                isUnboundGenericType: false,
                isNativeIntegerType: false,
                elementType: null,
                arrayRank: 0,
                typeParameterScope: null,
                typeParameterOrdinal: null,
                typeParameterOwnerAssembly: null,
                typeParameterOwnerModule: null,
                typeParameterOwnerDeclarationId: null,
                signature.CallingConvention.ToString(),
                returnSignature,
                parameters);
        }

        /// <summary>
        /// Creates an identity containing only its structural kind.
        /// </summary>
        /// <param name="kind">The kind value.</param>
        /// <returns>The operation result.</returns>
        private static CanonicalTypeIdentity CreateSimpleTypeIdentity(
            CanonicalTypeIdentityKind kind)
        {
            return new CanonicalTypeIdentity(
                kind,
                CanonicalNamedTypeKind.Unknown,
                assembly: null,
                module: null,
                namespaceName: null,
                metadataName: null,
                containingType: null,
                typeArguments: null,
                isUnboundGenericType: false,
                isNativeIntegerType: false,
                elementType: null,
                arrayRank: 0,
                typeParameterScope: null,
                typeParameterOrdinal: null,
                typeParameterOwnerAssembly: null,
                typeParameterOwnerModule: null,
                typeParameterOwnerDeclarationId: null,
                functionPointerCallingConvention: null,
                functionPointerReturn: null,
                functionPointerParameters: null);
        }

        /// <summary>
        /// Creates one canonical callable parameter.
        /// </summary>
        /// <param name="parameter">The parameter value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.NotSupportedException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalCallableParameterIdentity CreateParameterIdentity(
            IParameterSymbol parameter)
        {
            return new CanonicalCallableParameterIdentity(
                parameter.Ordinal,
                CreateTypeIdentity(parameter.Type),
                MapRefKind(parameter.RefKind),
                parameter.IsParams);
        }

        /// <summary>
        /// Gets the canonical return-reference modifier.
        /// </summary>
        /// <param name="method">The method value.</param>
        /// <returns>The operation result.</returns>
        private static CanonicalRefKind GetReturnRefKind(IMethodSymbol method)
        {
            if (method.ReturnsByRefReadonly)
            {
                return CanonicalRefKind.RefReadOnly;
            }

            return method.ReturnsByRef
                ? CanonicalRefKind.Ref
                : CanonicalRefKind.None;
        }

        /// <summary>
        /// Maps a Roslyn reference kind to its canonical equivalent.
        /// </summary>
        /// <param name="refKind">The refKind value.</param>
        /// <returns>The operation result.</returns>
        private static CanonicalRefKind MapRefKind(RefKind refKind)
        {
            return refKind switch
            {
                RefKind.None => CanonicalRefKind.None,
                RefKind.Ref => CanonicalRefKind.Ref,
                RefKind.Out => CanonicalRefKind.Out,
                RefKind.In => CanonicalRefKind.In,
                RefKind.RefReadOnlyParameter => CanonicalRefKind.RefReadOnly,
                _ => CanonicalRefKind.Unknown
            };
        }

        /// <summary>
        /// Maps a Roslyn type kind to its canonical named-type kind.
        /// </summary>
        /// <param name="typeKind">The typeKind value.</param>
        /// <returns>The operation result.</returns>
        private static CanonicalNamedTypeKind MapNamedTypeKind(TypeKind typeKind)
        {
            return typeKind switch
            {
                TypeKind.Class => CanonicalNamedTypeKind.Class,
                TypeKind.Struct => CanonicalNamedTypeKind.Structure,
                TypeKind.Interface => CanonicalNamedTypeKind.Interface,
                TypeKind.Enum => CanonicalNamedTypeKind.Enumeration,
                TypeKind.Delegate => CanonicalNamedTypeKind.Delegate,
                _ => CanonicalNamedTypeKind.Unknown
            };
        }

        /// <summary>
        /// Maps a Roslyn method kind to its canonical callable kind.
        /// </summary>
        /// <param name="methodKind">The methodKind value.</param>
        /// <returns>The operation result.</returns>
        private static CanonicalCallableKind MapCallableKind(MethodKind methodKind)
        {
            return methodKind switch
            {
                MethodKind.Ordinary => CanonicalCallableKind.Ordinary,
                MethodKind.Constructor => CanonicalCallableKind.Constructor,
                MethodKind.StaticConstructor => CanonicalCallableKind.StaticConstructor,
                MethodKind.PropertyGet => CanonicalCallableKind.PropertyGetter,
                MethodKind.PropertySet => CanonicalCallableKind.PropertySetter,
                MethodKind.EventAdd => CanonicalCallableKind.EventAdd,
                MethodKind.EventRemove => CanonicalCallableKind.EventRemove,
                MethodKind.UserDefinedOperator => CanonicalCallableKind.Operator,
                MethodKind.Conversion => CanonicalCallableKind.Conversion,
                MethodKind.LocalFunction => CanonicalCallableKind.LocalFunction,
                MethodKind.AnonymousFunction => CanonicalCallableKind.AnonymousFunction,
                _ => CanonicalCallableKind.Unknown
            };
        }

        /// <summary>
        /// Determines whether a callable needs source-bound identity because it
        /// has no metadata member identity.
        /// </summary>
        /// <param name="method">The method value.</param>
        /// <returns>The operation result.</returns>
        private static bool RequiresSourceIdentity(IMethodSymbol method)
        {
            return method.MethodKind is MethodKind.LocalFunction or MethodKind.AnonymousFunction;
        }

        /// <summary>
        /// Creates a source location for a source-declared symbol.
        /// </summary>
        /// <param name="symbol">The symbol value.</param>
        /// <returns>The operation result.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        private static CanonicalSourceLocationIdentity? TryCreateSourceLocation(
            ISymbol symbol)
        {
            SyntaxReference? syntaxReference = symbol.DeclaringSyntaxReferences
                .OrderBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal)
                .ThenBy(reference => reference.Span.Start)
                .FirstOrDefault();

            if (syntaxReference == null)
            {
                return null;
            }

            SourceText sourceText = syntaxReference.SyntaxTree.GetText();
            ImmutableArray<byte> checksum = sourceText.GetChecksum();

            return new CanonicalSourceLocationIdentity(
                syntaxReference.SyntaxTree.FilePath ?? string.Empty,
                sourceText.ChecksumAlgorithm.ToString(),
                Convert.ToHexString(checksum.AsSpan()),
                syntaxReference.Span.Start,
                syntaxReference.Span.Length);
        }

        /// <summary>
        /// Creates a deterministic owner key from source evidence.
        /// </summary>
        /// <param name="location">The location value.</param>
        /// <returns>The operation result.</returns>
        private static string CreateSourceOwnerKey(
            CanonicalSourceLocationIdentity location)
        {
            return string.Join(
                "|",
                "source",
                location.DocumentName.Length.ToString(CultureInfo.InvariantCulture),
                location.DocumentName,
                location.ChecksumAlgorithm,
                location.Checksum,
                location.SpanStart.ToString(CultureInfo.InvariantCulture),
                location.SpanLength.ToString(CultureInfo.InvariantCulture));
        }
    }
}
