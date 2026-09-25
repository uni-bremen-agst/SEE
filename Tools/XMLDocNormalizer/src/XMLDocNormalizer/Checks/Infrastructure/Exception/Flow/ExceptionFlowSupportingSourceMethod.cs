using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Couples one exactly rebound supporting-source callable to the semantic
    /// scope that owns it.
    /// </summary>
    /// <remarks>
    /// Both values belong to the same Roslyn compilation and are valid for
    /// the lifetime of the providing semantic environment. No metadata-only
    /// or ambiguous resolution is represented by this type.
    /// </remarks>
    internal sealed class ExceptionFlowSupportingSourceMethod
    {
        /// <summary>
        /// The semantic scope that owns the resolved method.
        /// </summary>
        private readonly ExceptionFlowSemanticScope scope;

        /// <summary>
        /// Initializes an exact supporting-source method resolution.
        /// </summary>
        /// <param name="method">The source-backed method.</param>
        /// <param name="scope">The scope that owns the method.</param>
        public ExceptionFlowSupportingSourceMethod(
            IMethodSymbol method,
            ExceptionFlowSemanticScope scope)
        {
            Method = method;
            this.scope = scope;
        }

        /// <summary>
        /// Gets the exact source-backed method.
        /// </summary>
        /// <value>The method owned by <see cref="Scope"/>.</value>
        public IMethodSymbol Method { get; }

        /// <summary>
        /// Gets the semantic scope that owns the resolved method.
        /// </summary>
        /// <value>The method's compilation scope.</value>
        public ExceptionFlowSemanticScope Scope
        {
            get
            {
                return scope;
            }
        }
    }
}
