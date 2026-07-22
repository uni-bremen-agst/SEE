using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception
{
    /// <summary>
    /// Provides deterministic exception-flow models for known framework throw helpers.
    /// </summary>
    /// <remarks>
    /// Framework methods normally have no source declaration in the analyzed compilation.
    /// This model records the documented behavior of selected framework methods whose
    /// possible exception types are stable and unambiguous.
    /// </remarks>
    internal static class KnownFrameworkExceptionModel
    {
        /// <summary>
        /// Determines whether the specified method is the supported
        /// <see cref="ArgumentNullException"/> <c>ThrowIfNull</c> framework helper.
        /// </summary>
        /// <param name="methodSymbol">The resolved method symbol to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <returns>
        /// <see langword="true"/> if the method is the framework
        /// <see cref="ArgumentNullException"/> <c>ThrowIfNull</c> helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsArgumentNullThrowIfNull(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            IMethodSymbol originalMethod = methodSymbol.OriginalDefinition;
            INamedTypeSymbol containingType =
                originalMethod.ContainingType.OriginalDefinition;

            return IsType(
                       containingType,
                       compilation,
                       "System.ArgumentNullException") &&
                   originalMethod.IsStatic &&
                   originalMethod.Name == "ThrowIfNull";
        }

        /// <summary>
        /// Determines whether the specified method is the supported
        /// <see cref="ArgumentException"/> <c>ThrowIfNullOrEmpty</c> helper.
        /// </summary>
        /// <param name="methodSymbol">The resolved method symbol to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <returns>
        /// <see langword="true"/> if the method is the framework
        /// <see cref="ArgumentException"/> <c>ThrowIfNullOrEmpty</c> helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsArgumentExceptionThrowIfNullOrEmpty(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            return IsArgumentExceptionThrowHelper(
                methodSymbol,
                compilation,
                "ThrowIfNullOrEmpty");
        }

        /// <summary>
        /// Determines whether the specified method is the supported
        /// <see cref="ArgumentException"/> <c>ThrowIfNullOrWhiteSpace</c> helper.
        /// </summary>
        /// <param name="methodSymbol">The resolved method symbol to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <returns>
        /// <see langword="true"/> if the method is the framework
        /// <see cref="ArgumentException"/> <c>ThrowIfNullOrWhiteSpace</c> helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsArgumentExceptionThrowIfNullOrWhiteSpace(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            return IsArgumentExceptionThrowHelper(
                methodSymbol,
                compilation,
                "ThrowIfNullOrWhiteSpace");
        }

        /// <summary>
        /// Adds the exception types associated with a known framework throw helper.
        /// </summary>
        /// <param name="methodSymbol">The resolved invoked method.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <param name="thrownExceptions">
        /// The collection to which modeled exception types are added.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the method is a known framework exception source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryAddThrownExceptionTypes(
            IMethodSymbol methodSymbol,
            Compilation compilation,
            ISet<INamedTypeSymbol> thrownExceptions)
        {
            IMethodSymbol originalMethod = methodSymbol.OriginalDefinition;
            INamedTypeSymbol containingType =
                originalMethod.ContainingType.OriginalDefinition;

            if (IsArgumentNullThrowIfNull(originalMethod, compilation))
            {
                AddExceptionType(
                    compilation,
                    thrownExceptions,
                    "System.ArgumentNullException");

                return true;
            }

            if (IsArgumentExceptionThrowIfNullOrEmpty(originalMethod, compilation)
                || IsArgumentExceptionThrowIfNullOrWhiteSpace(originalMethod, compilation))
            {
                AddExceptionType(
                    compilation,
                    thrownExceptions,
                    "System.ArgumentNullException");

                AddExceptionType(
                    compilation,
                    thrownExceptions,
                    "System.ArgumentException");

                return true;
            }

            if (IsType(
                    containingType,
                    compilation,
                    "System.ArgumentOutOfRangeException") &&
                originalMethod.IsStatic &&
                IsArgumentOutOfRangeThrowHelper(originalMethod.Name))
            {
                AddExceptionType(
                    compilation,
                    thrownExceptions,
                    "System.ArgumentOutOfRangeException");

                return true;
            }

            if (IsType(
                    containingType,
                    compilation,
                    "System.ObjectDisposedException") &&
                originalMethod.IsStatic &&
                originalMethod.Name == "ThrowIf")
            {
                AddExceptionType(
                    compilation,
                    thrownExceptions,
                    "System.ObjectDisposedException");

                return true;
            }

            if (IsType(
                    containingType,
                    compilation,
                    "System.Threading.CancellationToken") &&
                !originalMethod.IsStatic &&
                originalMethod.Name == "ThrowIfCancellationRequested" &&
                originalMethod.Parameters.Length == 0)
            {
                AddExceptionType(
                    compilation,
                    thrownExceptions,
                    "System.OperationCanceledException");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the method is a supported
        /// <see cref="ArgumentException"/> throw helper with the expected name.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <param name="expectedMethodName">The expected helper method name.</param>
        /// <returns>
        /// <see langword="true"/> if the method is the expected framework helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsArgumentExceptionThrowHelper(
            IMethodSymbol methodSymbol,
            Compilation compilation,
            string expectedMethodName)
        {
            IMethodSymbol originalMethod = methodSymbol.OriginalDefinition;
            INamedTypeSymbol containingType =
                originalMethod.ContainingType.OriginalDefinition;

            return IsType(
                       containingType,
                       compilation,
                       "System.ArgumentException") &&
                   originalMethod.IsStatic &&
                   originalMethod.Name == expectedMethodName;
        }

        /// <summary>
        /// Determines whether the method is a supported
        /// <see cref="ArgumentOutOfRangeException"/> throw helper.
        /// </summary>
        /// <param name="methodName">The method name to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the method is a supported
        /// <see cref="ArgumentOutOfRangeException"/> throw helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsArgumentOutOfRangeThrowHelper(string methodName)
        {
            return methodName is
                "ThrowIfZero" or
                "ThrowIfNegative" or
                "ThrowIfNegativeOrZero" or
                "ThrowIfEqual" or
                "ThrowIfNotEqual" or
                "ThrowIfGreaterThan" or
                "ThrowIfGreaterThanOrEqual" or
                "ThrowIfLessThan" or
                "ThrowIfLessThanOrEqual";
        }

        /// <summary>
        /// Determines whether a resolved type is the specified framework type.
        /// </summary>
        /// <param name="actualType">The resolved containing type.</param>
        /// <param name="compilation">The compilation used to resolve the expected type.</param>
        /// <param name="metadataName">The expected framework metadata name.</param>
        /// <returns>
        /// <see langword="true"/> if both symbols represent the same framework type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsType(
            INamedTypeSymbol actualType,
            Compilation compilation,
            string metadataName)
        {
            INamedTypeSymbol? expectedType =
                compilation.GetTypeByMetadataName(metadataName);

            return expectedType != null &&
                   SymbolEqualityComparer.Default.Equals(
                       actualType,
                       expectedType.OriginalDefinition);
        }

        /// <summary>
        /// Resolves an exception type and adds it to the target collection.
        /// </summary>
        /// <param name="compilation">The compilation used for type resolution.</param>
        /// <param name="thrownExceptions">
        /// The collection to which the exception type is added.
        /// </param>
        /// <param name="metadataName">The exception type metadata name.</param>
        private static void AddExceptionType(
            Compilation compilation,
            ISet<INamedTypeSymbol> thrownExceptions,
            string metadataName)
        {
            INamedTypeSymbol? exceptionType =
                compilation.GetTypeByMetadataName(metadataName);

            if (exceptionType != null)
            {
                thrownExceptions.Add(exceptionType);
            }
        }
    }
}
