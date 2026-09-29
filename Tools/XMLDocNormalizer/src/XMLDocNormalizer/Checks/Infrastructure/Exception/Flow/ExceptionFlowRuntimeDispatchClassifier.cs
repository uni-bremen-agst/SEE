using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Classifies methods whose runtime implementation can differ from the
    /// statically selected method.
    /// </summary>
    internal static class ExceptionFlowRuntimeDispatchClassifier
    {
        /// <summary>
        /// Determines whether a method requires runtime target expansion.
        /// </summary>
        /// <param name="methodSymbol">The method to inspect.</param>
        /// <returns>
        /// <see langword="true"/> for dispatchable class and interface
        /// methods; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool RequiresRuntimeDispatch(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.IsStatic || methodSymbol.IsSealed)
            {
                return false;
            }

            if (methodSymbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                return true;
            }

            return methodSymbol.IsAbstract
                || methodSymbol.IsVirtual
                || methodSymbol.IsOverride;
        }
    }
}
