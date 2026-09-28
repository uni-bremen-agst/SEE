using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Maps source arguments to the effective parameters of one resolved
    /// callable without performing value-fact or call-context evaluation.
    /// </summary>
    internal static class ExceptionFlowArgumentMapper
    {
        /// <summary>
        /// Gets the effective target parameter index for an argument, taking
        /// named arguments into account.
        /// </summary>
        /// <param name="argument">The argument to inspect.</param>
        /// <param name="fallbackIndex">The positional fallback index.</param>
        /// <param name="methodSymbol">The target method symbol.</param>
        /// <returns>
        /// The resolved parameter index, or the fallback index if no named
        /// match exists.
        /// </returns>
        internal static int GetParameterIndex(
            ArgumentSyntax argument,
            int fallbackIndex,
            IMethodSymbol methodSymbol)
        {
            if (argument.NameColon == null)
            {
                return fallbackIndex;
            }

            string name =
                argument.NameColon.Name.Identifier.ValueText;

            for (int index = 0;
                 index < methodSymbol.Parameters.Length;
                 index++)
            {
                if (string.Equals(
                        methodSymbol.Parameters[index].Name,
                        name,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return fallbackIndex;
        }
    }
}
