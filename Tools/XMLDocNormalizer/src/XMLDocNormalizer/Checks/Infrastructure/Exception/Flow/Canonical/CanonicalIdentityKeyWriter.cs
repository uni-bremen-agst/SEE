using System.Globalization;
using System.Text;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical
{
    /// <summary>
    /// Produces deterministic structural keys for canonical identities.
    /// </summary>
    /// <remarks>
    /// Keys are derived from authoritative structured fields. They support
    /// deterministic ordering and legacy string-key integration; equality of
    /// canonical value objects remains authoritative.
    /// </remarks>
    internal static class CanonicalIdentityKeyWriter
    {
        /// <summary>
        /// Writes a callable identity as a length-delimited structural key.
        /// </summary>
        /// <param name="identity">The callable identity.</param>
        /// <returns>The deterministic structural key.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public static string Write(CanonicalCallableIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            StringBuilder builder = new();
            AppendCallable(builder, identity);
            return builder.ToString();
        }

        /// <summary>
        /// Writes a canonical type identity as a deterministic structural key.
        /// </summary>
        /// <param name="identity">The type identity.</param>
        /// <returns>The deterministic structural key.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public static string Write(CanonicalTypeIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            StringBuilder builder = new();
            AppendType(builder, identity);
            return builder.ToString();
        }

        /// <summary>
        /// Writes a stable-member identity as a length-delimited structural key.
        /// </summary>
        /// <param name="identity">The stable-member identity.</param>
        /// <returns>The deterministic structural key.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public static string Write(CanonicalStableMemberIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            StringBuilder builder = new();
            AppendType(builder, identity.ContainingType);
            Append(builder, identity.MetadataName);
            Append(builder, ((int)identity.Kind).ToString(CultureInfo.InvariantCulture));
            AppendType(builder, identity.MemberType);
            Append(builder, identity.IsStatic ? "1" : "0");

            foreach (CanonicalCallableParameterIdentity parameter in identity.Parameters)
            {
                AppendParameter(builder, parameter);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Writes a canonical call context as a deterministic structural key.
        /// </summary>
        /// <param name="context">The canonical call context.</param>
        /// <returns>The deterministic structural key.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the supplied input cannot be processed.</exception>
        public static string Write(CanonicalExceptionFlowCallContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            StringBuilder builder = new();
            Append(builder, context.Callable == null ? "0" : "1");

            if (context.Callable != null)
            {
                AppendCallable(builder, context.Callable);
            }

            CanonicalParameterValueFact[] parameterFacts = context.ParameterFacts;
            Append(builder, parameterFacts.Length.ToString(CultureInfo.InvariantCulture));

            foreach (CanonicalParameterValueFact fact in parameterFacts)
            {
                Append(builder, fact.ParameterOrdinal.ToString(CultureInfo.InvariantCulture));
                Append(builder, ((int)fact.Facts).ToString(CultureInfo.InvariantCulture));
            }

            CanonicalParameterMemberFact[] memberFacts = context.MemberFacts;
            Append(builder, memberFacts.Length.ToString(CultureInfo.InvariantCulture));

            foreach (CanonicalParameterMemberFact fact in memberFacts)
            {
                Append(builder, fact.ParameterOrdinal.ToString(CultureInfo.InvariantCulture));
                Append(builder, Write(fact.Member));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Appends a callable identity recursively.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="identity">The identity value.</param>
        private static void AppendCallable(
            StringBuilder builder,
            CanonicalCallableIdentity identity)
        {
            Append(builder, identity.ContainingType == null ? "0" : "1");

            if (identity.ContainingType != null)
            {
                AppendType(builder, identity.ContainingType);
            }

            Append(builder, identity.ContainingNamespace == null ? "0" : "1");

            if (identity.ContainingNamespace != null)
            {
                AppendAssembly(builder, identity.ContainingNamespace.Assembly);
                Append(builder, identity.ContainingNamespace.FullName);
            }

            Append(builder, identity.MetadataName);
            Append(builder, ((int)identity.Kind).ToString(CultureInfo.InvariantCulture));
            Append(builder, identity.Arity.ToString(CultureInfo.InvariantCulture));
            Append(builder, identity.ReturnType == null ? "0" : "1");

            if (identity.ReturnType != null)
            {
                AppendType(builder, identity.ReturnType);
            }

            Append(builder, ((int)identity.ReturnRefKind).ToString(CultureInfo.InvariantCulture));

            CanonicalCallableParameterIdentity[] parameters = identity.Parameters;
            Append(builder, parameters.Length.ToString(CultureInfo.InvariantCulture));

            foreach (CanonicalCallableParameterIdentity parameter in parameters)
            {
                AppendParameter(builder, parameter);
            }

            CanonicalTypeIdentity[] typeArguments = identity.TypeArguments;
            Append(builder, typeArguments.Length.ToString(CultureInfo.InvariantCulture));

            foreach (CanonicalTypeIdentity typeArgument in typeArguments)
            {
                AppendType(builder, typeArgument);
            }

            CanonicalCallableIdentity[] implementations =
                identity.ExplicitInterfaceImplementations;
            Append(builder, implementations.Length.ToString(CultureInfo.InvariantCulture));

            foreach (CanonicalCallableIdentity implementation in implementations)
            {
                AppendCallable(builder, implementation);
            }

            Append(builder, identity.ReducedFrom == null ? "0" : "1");

            if (identity.ReducedFrom != null)
            {
                AppendCallable(builder, identity.ReducedFrom);
            }

            AppendSourceLocation(builder, identity.SourceLocation);
        }

        /// <summary>
        /// Appends one callable parameter.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="parameter">The parameter value.</param>
        private static void AppendParameter(
            StringBuilder builder,
            CanonicalCallableParameterIdentity parameter)
        {
            Append(builder, parameter.Ordinal.ToString(CultureInfo.InvariantCulture));
            AppendType(builder, parameter.Type);
            Append(builder, ((int)parameter.RefKind).ToString(CultureInfo.InvariantCulture));
            Append(builder, parameter.IsParams ? "1" : "0");
        }

        /// <summary>
        /// Appends a canonical type identity recursively.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="identity">The identity value.</param>
        private static void AppendType(
            StringBuilder builder,
            CanonicalTypeIdentity identity)
        {
            Append(builder, ((int)identity.Kind).ToString(CultureInfo.InvariantCulture));
            Append(builder, ((int)identity.NamedTypeKind).ToString(CultureInfo.InvariantCulture));
            AppendAssembly(builder, identity.Assembly);
            AppendModule(builder, identity.Module);
            Append(builder, identity.NamespaceName ?? string.Empty);
            Append(builder, identity.MetadataName ?? string.Empty);
            Append(builder, identity.ContainingType == null ? "0" : "1");

            if (identity.ContainingType != null)
            {
                AppendType(builder, identity.ContainingType);
            }

            CanonicalTypeIdentity[] typeArguments = identity.TypeArguments;
            Append(builder, typeArguments.Length.ToString(CultureInfo.InvariantCulture));

            foreach (CanonicalTypeIdentity typeArgument in typeArguments)
            {
                AppendType(builder, typeArgument);
            }

            Append(builder, identity.IsUnboundGenericType ? "1" : "0");
            Append(builder, identity.IsNativeIntegerType ? "1" : "0");
            Append(builder, identity.ElementType == null ? "0" : "1");

            if (identity.ElementType != null)
            {
                AppendType(builder, identity.ElementType);
            }

            Append(builder, identity.ArrayRank.ToString(CultureInfo.InvariantCulture));
            Append(
                builder,
                identity.TypeParameterScope.HasValue
                    ? ((int)identity.TypeParameterScope.Value).ToString(CultureInfo.InvariantCulture)
                    : string.Empty);
            Append(
                builder,
                identity.TypeParameterOrdinal?.ToString(CultureInfo.InvariantCulture)
                    ?? string.Empty);
            AppendAssembly(builder, identity.TypeParameterOwnerAssembly);
            AppendModule(builder, identity.TypeParameterOwnerModule);
            Append(builder, identity.TypeParameterOwnerDeclarationId ?? string.Empty);
            Append(builder, identity.FunctionPointerCallingConvention ?? string.Empty);
            Append(builder, identity.FunctionPointerReturn == null ? "0" : "1");

            if (identity.FunctionPointerReturn != null)
            {
                AppendFunctionPointerParameter(builder, identity.FunctionPointerReturn);
            }

            CanonicalFunctionPointerParameter[] functionParameters =
                identity.FunctionPointerParameters;
            Append(builder, functionParameters.Length.ToString(CultureInfo.InvariantCulture));

            foreach (CanonicalFunctionPointerParameter parameter in functionParameters)
            {
                AppendFunctionPointerParameter(builder, parameter);
            }
        }

        /// <summary>
        /// Appends one function-pointer signature component.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="parameter">The parameter value.</param>
        private static void AppendFunctionPointerParameter(
            StringBuilder builder,
            CanonicalFunctionPointerParameter parameter)
        {
            AppendType(builder, parameter.Type);
            Append(builder, ((int)parameter.RefKind).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Appends an optional assembly identity.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="identity">The identity value.</param>
        private static void AppendAssembly(
            StringBuilder builder,
            CanonicalAssemblyIdentity? identity)
        {
            Append(builder, identity == null ? "0" : "1");

            if (identity == null)
            {
                return;
            }

            Append(builder, identity.Name);
            Append(builder, identity.VersionMajor.ToString(CultureInfo.InvariantCulture));
            Append(builder, identity.VersionMinor.ToString(CultureInfo.InvariantCulture));
            Append(builder, identity.VersionBuild.ToString(CultureInfo.InvariantCulture));
            Append(builder, identity.VersionRevision.ToString(CultureInfo.InvariantCulture));
            Append(builder, identity.CultureName);
            Append(builder, identity.PublicKeyOrToken);
            Append(builder, identity.HasPublicKey ? "1" : "0");
            Append(builder, identity.IsRetargetable ? "1" : "0");
            Append(builder, ((int)identity.ContentType).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Appends an optional module identity.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="identity">The identity value.</param>
        private static void AppendModule(
            StringBuilder builder,
            CanonicalModuleIdentity? identity)
        {
            Append(builder, identity == null ? "0" : "1");

            if (identity == null)
            {
                return;
            }

            AppendAssembly(builder, identity.Assembly);
            Append(builder, identity.Name);
            Append(builder, identity.Ordinal.ToString(CultureInfo.InvariantCulture));
            Append(builder, identity.ModuleVersionId?.ToString("N") ?? string.Empty);
        }

        /// <summary>
        /// Appends an optional source location.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="identity">The identity value.</param>
        private static void AppendSourceLocation(
            StringBuilder builder,
            CanonicalSourceLocationIdentity? identity)
        {
            Append(builder, identity == null ? "0" : "1");

            if (identity == null)
            {
                return;
            }

            Append(builder, identity.DocumentName);
            Append(builder, identity.ChecksumAlgorithm);
            Append(builder, identity.Checksum);
            Append(builder, identity.SpanStart.ToString(CultureInfo.InvariantCulture));
            Append(builder, identity.SpanLength.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Appends one length-delimited scalar value.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        /// <param name="value">The value value.</param>
        private static void Append(StringBuilder builder, string value)
        {
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
            builder.Append('|');
        }
    }
}
