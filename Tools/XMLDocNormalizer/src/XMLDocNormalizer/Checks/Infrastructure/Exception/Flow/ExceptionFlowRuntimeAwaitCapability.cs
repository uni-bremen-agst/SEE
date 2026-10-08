using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Reads exactly the public RuntimeAwaitMethod capability without confusing
    /// an absent API with a native null result.
    /// </summary>
    internal static class ExceptionFlowRuntimeAwaitCapability
    {
        /// <summary>Invokes the native getter without boxing its struct receiver.</summary>
        /// <param name="info">The await information to read.</param>
        /// <returns>The native runtime helper, including a known null.</returns>
        private delegate IMethodSymbol? RuntimeAwaitGetter(ref AwaitExpressionInfo info);

        /// <summary>
        /// Stores one stateless accessor, published by CLR static initialization.
        /// No semantic models, compilations or returned symbols are retained.
        /// </summary>
        private static readonly RuntimeAwaitGetter? getter = CreateGetter();

        /// <summary>Reads the native value or reports unavailable information.</summary>
        /// <param name="info">The compiler's await information.</param>
        /// <returns>A known method/null, or an explicit unavailable state.</returns>
        internal static Information Read(AwaitExpressionInfo info)
        {
            return getter == null
                ? default
                : Information.Known(getter(ref info));
        }

        /// <summary>Creates an accessor for the one supported public API shape.</summary>
        /// <returns>The native getter, or null when the API is absent.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when a present property has an unexpected contract.
        /// </exception>
        private static RuntimeAwaitGetter? CreateGetter()
        {
            PropertyInfo? property = typeof(AwaitExpressionInfo).GetProperty(
                "RuntimeAwaitMethod", BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                return null;
            }

            MethodInfo? method = property.GetMethod;
            if (property.PropertyType != typeof(IMethodSymbol)
                || property.GetIndexParameters().Length != 0
                || method == null || method.IsStatic || !method.IsPublic)
            {
                throw new NotSupportedException("Unexpected RuntimeAwaitMethod metadata contract.");
            }

            return method.CreateDelegate<RuntimeAwaitGetter>();
        }

        /// <summary>
        /// Represents known-symbol, known-null and unavailable information.
        /// The default value is unavailable, never a fabricated known null.
        /// </summary>
        internal readonly struct Information
        {
            /// <summary>Creates information supplied by a native getter.</summary>
            /// <param name="method">The native value, including null.</param>
            private Information(IMethodSymbol? method)
            {
                IsAvailable = true;
                Method = method;
            }

            /// <summary>
            /// Stores whether the runtime-helper value is known. This readonly
            /// flag is true for both native-symbol and native-null results.
            /// </summary>
            internal readonly bool IsAvailable;

            /// <summary>Gets the native value when information is available.</summary>
            /// <value>The native method; null alone does not establish availability.</value>
            internal IMethodSymbol? Method { get; }

            /// <summary>Represents an actual native getter result.</summary>
            /// <param name="method">The native value.</param>
            /// <returns>Available information, even when the value is null.</returns>
            internal static Information Known(IMethodSymbol? method) => new(method);
        }
    }
}
