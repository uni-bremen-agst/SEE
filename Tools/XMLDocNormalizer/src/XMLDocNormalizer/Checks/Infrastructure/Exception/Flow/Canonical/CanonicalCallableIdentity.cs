namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Identifies a callable kind without retaining Roslyn's
    /// <c>MethodKind</c> value.
    /// </summary>
    internal enum CanonicalCallableKind
    {
        /// <summary>
        /// An ordinary method.
        /// </summary>
        Ordinary,

        /// <summary>
        /// An instance constructor.
        /// </summary>
        Constructor,

        /// <summary>
        /// A static constructor.
        /// </summary>
        StaticConstructor,

        /// <summary>
        /// A property getter.
        /// </summary>
        PropertyGetter,

        /// <summary>
        /// A property setter or init accessor.
        /// </summary>
        PropertySetter,

        /// <summary>
        /// A property or indexer root whose accessors are analyzed together.
        /// </summary>
        Property,

        /// <summary>
        /// An event add accessor.
        /// </summary>
        EventAdd,

        /// <summary>
        /// An event remove accessor.
        /// </summary>
        EventRemove,

        /// <summary>
        /// An event root whose add and remove accessors are analyzed together.
        /// </summary>
        Event,

        /// <summary>
        /// A user-defined operator.
        /// </summary>
        Operator,

        /// <summary>
        /// A user-defined conversion.
        /// </summary>
        Conversion,

        /// <summary>
        /// A local function.
        /// </summary>
        LocalFunction,

        /// <summary>
        /// An anonymous function.
        /// </summary>
        AnonymousFunction,

        /// <summary>
        /// A type declaration used as an existing summary-graph root.
        /// </summary>
        TypeDeclaration,

        /// <summary>
        /// A field declaration used as an existing summary-graph root.
        /// </summary>
        FieldDeclaration,

        /// <summary>
        /// A namespace declaration used as an existing summary-graph root.
        /// </summary>
        NamespaceDeclaration,

        /// <summary>
        /// A callable kind unknown to this model version.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Represents one ordered callable parameter identity.
    /// </summary>
    internal sealed class CanonicalCallableParameterIdentity
        : IEquatable<CanonicalCallableParameterIdentity>
    {
        /// <summary>
        /// Initializes a canonical callable parameter.
        /// </summary>
        /// <param name="ordinal">The zero-based parameter ordinal.</param>
        /// <param name="type">The parameter type.</param>
        /// <param name="refKind">The managed-reference modifier.</param>
        /// <param name="isParams">
        /// Whether the parameter is declared with <c>params</c>. This is
        /// retained for fidelity but does not include the parameter name.
        /// </param>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalCallableParameterIdentity(
            int ordinal,
            CanonicalTypeIdentity type,
            CanonicalRefKind refKind,
            bool isParams)
        {
            ArgumentNullException.ThrowIfNull(type);

            Ordinal = ordinal;
            Type = type;
            RefKind = refKind;
            IsParams = isParams;
        }

        /// <summary>
        /// Gets the zero-based parameter ordinal.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int Ordinal { get; }

        /// <summary>
        /// Gets the parameter type.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity Type { get; }

        /// <summary>
        /// Gets the managed-reference modifier.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalRefKind RefKind { get; }

        /// <summary>
        /// Gets whether the parameter is declared with <c>params</c>.
        /// </summary>
        /// <value>The value described by this property.</value>
        public bool IsParams { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalCallableParameterIdentity? other)
        {
            return other != null
                && Ordinal == other.Ordinal
                && Type.Equals(other.Type)
                && RefKind == other.RefKind
                && IsParams == other.IsParams;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalCallableParameterIdentity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Ordinal, Type, RefKind, IsParams);
        }
    }

    /// <summary>
    /// Represents an immutable, structured and Roslyn-independent callable identity.
    /// </summary>
    internal sealed class CanonicalCallableIdentity
        : IEquatable<CanonicalCallableIdentity>
    {
        /// <summary>
        /// Stores ordered parameter identities.
        /// </summary>
        private readonly CanonicalCallableParameterIdentity[] parameters;

        /// <summary>
        /// Stores constructed method type arguments.
        /// </summary>
        private readonly CanonicalTypeIdentity[] typeArguments;

        /// <summary>
        /// Stores explicitly implemented interface callables in canonical order.
        /// </summary>
        private readonly CanonicalCallableIdentity[] explicitInterfaceImplementations;

        /// <summary>
        /// Initializes a canonical callable identity.
        /// </summary>
        /// <param name="containingType">The containingType value.</param>
        /// <param name="metadataName">The metadataName value.</param>
        /// <param name="kind">The kind value.</param>
        /// <param name="arity">The arity value.</param>
        /// <param name="returnType">The returnType value.</param>
        /// <param name="returnRefKind">The returnRefKind value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <param name="typeArguments">The typeArguments value.</param>
        /// <param name="explicitInterfaceImplementations">The explicitInterfaceImplementations value.</param>
        /// <param name="reducedFrom">The reducedFrom value.</param>
        /// <param name="sourceLocation">The sourceLocation value.</param>
        /// <param name="containingNamespace">The containingNamespace value.</param>
        /// <exception cref="System.ArgumentException">Thrown when the supplied input cannot be processed.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public CanonicalCallableIdentity(
            CanonicalTypeIdentity? containingType,
            string metadataName,
            CanonicalCallableKind kind,
            int arity,
            CanonicalTypeIdentity? returnType,
            CanonicalRefKind returnRefKind,
            CanonicalCallableParameterIdentity[]? parameters,
            CanonicalTypeIdentity[]? typeArguments,
            CanonicalCallableIdentity[]? explicitInterfaceImplementations,
            CanonicalCallableIdentity? reducedFrom,
            CanonicalSourceLocationIdentity? sourceLocation,
            CanonicalNamespaceIdentity? containingNamespace = null)
        {
            ArgumentNullException.ThrowIfNull(metadataName);

            if (kind == CanonicalCallableKind.NamespaceDeclaration)
            {
                ArgumentNullException.ThrowIfNull(containingNamespace);
            }
            else
            {
                ArgumentNullException.ThrowIfNull(containingType);
                ArgumentNullException.ThrowIfNull(returnType);
            }

            ContainingType = containingType;
            MetadataName = metadataName;
            Kind = kind;
            Arity = arity;
            ReturnType = returnType;
            ReturnRefKind = returnRefKind;
            this.parameters = parameters?.ToArray()
                ?? Array.Empty<CanonicalCallableParameterIdentity>();
            this.typeArguments = typeArguments?.ToArray()
                ?? Array.Empty<CanonicalTypeIdentity>();
            this.explicitInterfaceImplementations =
                explicitInterfaceImplementations?.ToArray()
                ?? Array.Empty<CanonicalCallableIdentity>();
            ReducedFrom = reducedFrom;
            SourceLocation = sourceLocation;
            ContainingNamespace = containingNamespace;

            if (string.IsNullOrWhiteSpace(metadataName) && sourceLocation == null)
            {
                throw new ArgumentException(
                    "A callable without a metadata name requires source-bound identity.",
                    nameof(metadataName));
            }
        }

        /// <summary>
        /// Gets the containing type identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity? ContainingType { get; }

        /// <summary>
        /// Gets the containing namespace for a namespace-declaration graph root.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalNamespaceIdentity? ContainingNamespace { get; }

        /// <summary>
        /// Gets the metadata callable name.
        /// </summary>
        /// <value>The value described by this property.</value>
        public string MetadataName { get; }

        /// <summary>
        /// Gets the callable kind.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableKind Kind { get; }

        /// <summary>
        /// Gets the method generic arity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public int Arity { get; }

        /// <summary>
        /// Gets the return type identity.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity? ReturnType { get; }

        /// <summary>
        /// Gets the return managed-reference modifier.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalRefKind ReturnRefKind { get; }

        /// <summary>
        /// Gets a copy of the ordered parameter identities.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableParameterIdentity[] Parameters => parameters.ToArray();

        /// <summary>
        /// Gets a copy of the constructed method type arguments.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalTypeIdentity[] TypeArguments => typeArguments.ToArray();

        /// <summary>
        /// Gets a copy of the canonical explicit-interface implementations.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableIdentity[] ExplicitInterfaceImplementations =>
            explicitInterfaceImplementations.ToArray();

        /// <summary>
        /// Gets the underlying extension method for a reduced extension callable.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalCallableIdentity? ReducedFrom { get; }

        /// <summary>
        /// Gets source-bound identity for local or anonymous functions.
        /// </summary>
        /// <value>The value described by this property.</value>
        public CanonicalSourceLocationIdentity? SourceLocation { get; }

        /// <inheritdoc/>
        public bool Equals(CanonicalCallableIdentity? other)
        {
            return other != null
                && Equals(ContainingType, other.ContainingType)
                && Equals(ContainingNamespace, other.ContainingNamespace)
                && StringComparer.Ordinal.Equals(MetadataName, other.MetadataName)
                && Kind == other.Kind
                && Arity == other.Arity
                && Equals(ReturnType, other.ReturnType)
                && ReturnRefKind == other.ReturnRefKind
                && parameters.SequenceEqual(other.parameters)
                && typeArguments.SequenceEqual(other.typeArguments)
                && explicitInterfaceImplementations.SequenceEqual(
                    other.explicitInterfaceImplementations)
                && Equals(ReducedFrom, other.ReducedFrom)
                && Equals(SourceLocation, other.SourceLocation);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as CanonicalCallableIdentity);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(ContainingType);
            hash.Add(ContainingNamespace);
            hash.Add(MetadataName, StringComparer.Ordinal);
            hash.Add(Kind);
            hash.Add(Arity);
            hash.Add(ReturnType);
            hash.Add(ReturnRefKind);

            foreach (CanonicalCallableParameterIdentity parameter in parameters)
            {
                hash.Add(parameter);
            }

            foreach (CanonicalTypeIdentity typeArgument in typeArguments)
            {
                hash.Add(typeArgument);
            }

            foreach (CanonicalCallableIdentity implementation in explicitInterfaceImplementations)
            {
                hash.Add(implementation);
            }

            hash.Add(ReducedFrom);
            hash.Add(SourceLocation);
            return hash.ToHashCode();
        }
    }
}
