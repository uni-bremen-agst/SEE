namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Identifies the structural kind of a canonical type.
    /// </summary>
    internal enum CanonicalTypeIdentityKind
    {
        /// <summary>
        /// A metadata named type.
        /// </summary>
        Named,

        /// <summary>
        /// An array type.
        /// </summary>
        Array,

        /// <summary>
        /// An unmanaged pointer type.
        /// </summary>
        Pointer,

        /// <summary>
        /// A generic type or method parameter.
        /// </summary>
        TypeParameter,

        /// <summary>
        /// The C# dynamic type.
        /// </summary>
        Dynamic,

        /// <summary>
        /// A function-pointer type.
        /// </summary>
        FunctionPointer
    }

    /// <summary>
    /// Identifies the declaration kind of a named type.
    /// </summary>
    internal enum CanonicalNamedTypeKind
    {
        /// <summary>
        /// An unknown or unsupported named-type classification.
        /// </summary>
        Unknown,

        /// <summary>
        /// A class type.
        /// </summary>
        Class,

        /// <summary>
        /// A structure type.
        /// </summary>
        Structure,

        /// <summary>
        /// An interface type.
        /// </summary>
        Interface,

        /// <summary>
        /// An enumeration type.
        /// </summary>
        Enumeration,

        /// <summary>
        /// A delegate type.
        /// </summary>
        Delegate
    }

    /// <summary>
    /// Identifies the declaration scope of a canonical type parameter.
    /// </summary>
    internal enum CanonicalTypeParameterScope
    {
        /// <summary>
        /// A type-level parameter.
        /// </summary>
        Type,

        /// <summary>
        /// A method-level parameter.
        /// </summary>
        Method
    }

    /// <summary>
    /// Identifies a managed-reference modifier without using Roslyn's
    /// <c>RefKind</c> enumeration.
    /// </summary>
    internal enum CanonicalRefKind
    {
        /// <summary>
        /// A by-value parameter or return.
        /// </summary>
        None,

        /// <summary>
        /// A writable managed reference.
        /// </summary>
        Ref,

        /// <summary>
        /// An output managed reference.
        /// </summary>
        Out,

        /// <summary>
        /// A read-only input managed reference.
        /// </summary>
        In,

        /// <summary>
        /// A read-only managed-reference return.
        /// </summary>
        RefReadOnly,

        /// <summary>
        /// A managed-reference form unknown to this model version.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Represents one function-pointer parameter or return signature.
    /// </summary>
    internal sealed class CanonicalFunctionPointerParameter
        : IEquatable<CanonicalFunctionPointerParameter>
    {
        /// <summary>
        /// Initializes a function-pointer signature component.
        /// </summary>
        /// <param name="type">The parameter or return type.</param>
        /// <param name="refKind">The managed-reference modifier.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalFunctionPointerParameter(
            CanonicalTypeIdentity type,
            CanonicalRefKind refKind)
        {
            ArgumentNullException.ThrowIfNull(type);

            Type = type;
            RefKind = refKind;
        }

        /// <summary>
        /// Gets the component type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity Type { get; }

        /// <summary>
        /// Gets the managed-reference modifier.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalRefKind RefKind { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalFunctionPointerParameter? other)
        {
            return other != null
                && Type.Equals(other.Type)
                && RefKind == other.RefKind;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalFunctionPointerParameter);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, RefKind);
        }
    }

    /// <summary>
    /// Represents an immutable, structured and Roslyn-independent type identity.
    /// </summary>
    internal sealed class CanonicalTypeIdentity
        : IEquatable<CanonicalTypeIdentity>
    {
        /// <summary>
        /// Stores constructed named-type arguments.
        /// </summary>
        private readonly CanonicalTypeIdentity[] typeArguments;

        /// <summary>
        /// Stores function-pointer parameter signatures.
        /// </summary>
        private readonly CanonicalFunctionPointerParameter[] functionPointerParameters;

        /// <summary>
        /// Initializes a canonical type identity.
        /// </summary>
        /// <param name="kind">The kind value.</param>
        /// <param name="namedTypeKind">The namedTypeKind value.</param>
        /// <param name="assembly">The assembly value.</param>
        /// <param name="module">The module value.</param>
        /// <param name="namespaceName">The namespaceName value.</param>
        /// <param name="metadataName">The metadataName value.</param>
        /// <param name="containingType">The containingType value.</param>
        /// <param name="typeArguments">The typeArguments value.</param>
        /// <param name="isUnboundGenericType">The isUnboundGenericType value.</param>
        /// <param name="isNativeIntegerType">The isNativeIntegerType value.</param>
        /// <param name="elementType">The elementType value.</param>
        /// <param name="arrayRank">The arrayRank value.</param>
        /// <param name="typeParameterScope">The typeParameterScope value.</param>
        /// <param name="typeParameterOrdinal">The typeParameterOrdinal value.</param>
        /// <param name="typeParameterOwnerAssembly">The typeParameterOwnerAssembly value.</param>
        /// <param name="typeParameterOwnerModule">The typeParameterOwnerModule value.</param>
        /// <param name="typeParameterOwnerDeclarationId">The typeParameterOwnerDeclarationId value.</param>
        /// <param name="functionPointerCallingConvention">The functionPointerCallingConvention value.</param>
        /// <param name="functionPointerReturn">The functionPointerReturn value.</param>
        /// <param name="functionPointerParameters">The functionPointerParameters value.</param>
        public CanonicalTypeIdentity(
            CanonicalTypeIdentityKind kind,
            CanonicalNamedTypeKind namedTypeKind,
            CanonicalAssemblyIdentity? assembly,
            CanonicalModuleIdentity? module,
            string? namespaceName,
            string? metadataName,
            CanonicalTypeIdentity? containingType,
            CanonicalTypeIdentity[]? typeArguments,
            bool isUnboundGenericType,
            bool isNativeIntegerType,
            CanonicalTypeIdentity? elementType,
            int arrayRank,
            CanonicalTypeParameterScope? typeParameterScope,
            int? typeParameterOrdinal,
            CanonicalAssemblyIdentity? typeParameterOwnerAssembly,
            CanonicalModuleIdentity? typeParameterOwnerModule,
            string? typeParameterOwnerDeclarationId,
            string? functionPointerCallingConvention,
            CanonicalFunctionPointerParameter? functionPointerReturn,
            CanonicalFunctionPointerParameter[]? functionPointerParameters)
        {
            Kind = kind;
            NamedTypeKind = namedTypeKind;
            Assembly = assembly;
            Module = module;
            NamespaceName = namespaceName;
            MetadataName = metadataName;
            ContainingType = containingType;
            this.typeArguments = typeArguments?.ToArray() ?? Array.Empty<CanonicalTypeIdentity>();
            IsUnboundGenericType = isUnboundGenericType;
            IsNativeIntegerType = isNativeIntegerType;
            ElementType = elementType;
            ArrayRank = arrayRank;
            TypeParameterScope = typeParameterScope;
            TypeParameterOrdinal = typeParameterOrdinal;
            TypeParameterOwnerAssembly = typeParameterOwnerAssembly;
            TypeParameterOwnerModule = typeParameterOwnerModule;
            TypeParameterOwnerDeclarationId = typeParameterOwnerDeclarationId;
            FunctionPointerCallingConvention = functionPointerCallingConvention;
            FunctionPointerReturn = functionPointerReturn;
            this.functionPointerParameters = functionPointerParameters?.ToArray()
                ?? Array.Empty<CanonicalFunctionPointerParameter>();
        }

        /// <summary>
        /// Gets the structural type kind.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentityKind Kind { get; }

        /// <summary>
        /// Gets the named-type declaration kind.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalNamedTypeKind NamedTypeKind { get; }

        /// <summary>
        /// Gets the containing assembly for a named type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalAssemblyIdentity? Assembly { get; }

        /// <summary>
        /// Gets the containing module for a named type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalModuleIdentity? Module { get; }

        /// <summary>
        /// Gets the metadata namespace for a top-level named type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string? NamespaceName { get; }

        /// <summary>
        /// Gets the metadata name, including generic arity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string? MetadataName { get; }

        /// <summary>
        /// Gets the containing type for a nested named type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity? ContainingType { get; }

        /// <summary>
        /// Gets a copy of the constructed named-type arguments.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity[] TypeArguments => typeArguments.ToArray();

        /// <summary>
        /// Gets whether the identity represents an unbound generic type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool IsUnboundGenericType { get; }

        /// <summary>
        /// Gets whether the named type uses native-integer language semantics.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool IsNativeIntegerType { get; }

        /// <summary>
        /// Gets the element type for an array or pointer.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity? ElementType { get; }

        /// <summary>
        /// Gets the array rank, or zero for a non-array type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int ArrayRank { get; }

        /// <summary>
        /// Gets the declaration scope for a type parameter.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeParameterScope? TypeParameterScope { get; }

        /// <summary>
        /// Gets the zero-based type-parameter ordinal.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int? TypeParameterOrdinal { get; }

        /// <summary>
        /// Gets the assembly containing the type-parameter owner.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalAssemblyIdentity? TypeParameterOwnerAssembly { get; }

        /// <summary>
        /// Gets the module containing the type-parameter owner.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalModuleIdentity? TypeParameterOwnerModule { get; }

        /// <summary>
        /// Gets the metadata declaration ID or source-bound declaration key of
        /// the type-parameter owner.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string? TypeParameterOwnerDeclarationId { get; }

        /// <summary>
        /// Gets the function-pointer calling convention.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string? FunctionPointerCallingConvention { get; }

        /// <summary>
        /// Gets the function-pointer return signature.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalFunctionPointerParameter? FunctionPointerReturn { get; }

        /// <summary>
        /// Gets a copy of the ordered function-pointer parameter signatures.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalFunctionPointerParameter[] FunctionPointerParameters =>
            functionPointerParameters.ToArray();

        /// <inheritdoc/>
        public bool Equals(CanonicalTypeIdentity? other)
        {
            return other != null
                && Kind == other.Kind
                && NamedTypeKind == other.NamedTypeKind
                && Equals(Assembly, other.Assembly)
                && Equals(Module, other.Module)
                && StringComparer.Ordinal.Equals(NamespaceName, other.NamespaceName)
                && StringComparer.Ordinal.Equals(MetadataName, other.MetadataName)
                && Equals(ContainingType, other.ContainingType)
                && typeArguments.SequenceEqual(other.typeArguments)
                && IsUnboundGenericType == other.IsUnboundGenericType
                && IsNativeIntegerType == other.IsNativeIntegerType
                && Equals(ElementType, other.ElementType)
                && ArrayRank == other.ArrayRank
                && TypeParameterScope == other.TypeParameterScope
                && TypeParameterOrdinal == other.TypeParameterOrdinal
                && Equals(TypeParameterOwnerAssembly, other.TypeParameterOwnerAssembly)
                && Equals(TypeParameterOwnerModule, other.TypeParameterOwnerModule)
                && StringComparer.Ordinal.Equals(
                    TypeParameterOwnerDeclarationId,
                    other.TypeParameterOwnerDeclarationId)
                && StringComparer.Ordinal.Equals(
                    FunctionPointerCallingConvention,
                    other.FunctionPointerCallingConvention)
                && Equals(FunctionPointerReturn, other.FunctionPointerReturn)
                && functionPointerParameters.SequenceEqual(other.functionPointerParameters);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalTypeIdentity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Kind);
            hash.Add(NamedTypeKind);
            hash.Add(Assembly);
            hash.Add(Module);
            hash.Add(NamespaceName, StringComparer.Ordinal);
            hash.Add(MetadataName, StringComparer.Ordinal);
            hash.Add(ContainingType);

            foreach (CanonicalTypeIdentity typeArgument in typeArguments)
            {
                hash.Add(typeArgument);
            }

            hash.Add(IsUnboundGenericType);
            hash.Add(IsNativeIntegerType);
            hash.Add(ElementType);
            hash.Add(ArrayRank);
            hash.Add(TypeParameterScope);
            hash.Add(TypeParameterOrdinal);
            hash.Add(TypeParameterOwnerAssembly);
            hash.Add(TypeParameterOwnerModule);
            hash.Add(TypeParameterOwnerDeclarationId, StringComparer.Ordinal);
            hash.Add(FunctionPointerCallingConvention, StringComparer.Ordinal);
            hash.Add(FunctionPointerReturn);

            foreach (CanonicalFunctionPointerParameter parameter in functionPointerParameters)
            {
                hash.Add(parameter);
            }

            return hash.ToHashCode();
        }
    }
}
