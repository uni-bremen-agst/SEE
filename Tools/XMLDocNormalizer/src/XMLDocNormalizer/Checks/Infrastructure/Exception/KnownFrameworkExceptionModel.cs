using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception
{
    /// <summary>
    /// Provides deterministic exception-flow models for selected framework APIs.
    /// </summary>
    /// <remarks>
    /// Registered contracts explicitly distinguish complete exception models
    /// from partial positive evidence. External XML documentation remains a
    /// separate evidence source.
    /// </remarks>
    internal static class KnownFrameworkExceptionModel
    {
        /// <summary>
        /// Stores curated contracts in deterministic lookup order.
        /// </summary>
        private static readonly IReadOnlyList<KnownFrameworkExceptionContract> contracts =
        [
            new(
                KnownFrameworkExceptionContractCompleteness.Complete,
                IsArgumentNullThrowIfNull,
                EvaluateArgumentNullGuard),
            new(
                KnownFrameworkExceptionContractCompleteness.Complete,
                IsArgumentExceptionThrowIfNullOrEmpty,
                EvaluateArgumentNullOrEmptyGuard),
            new(
                KnownFrameworkExceptionContractCompleteness.Complete,
                IsArgumentExceptionThrowIfNullOrWhiteSpace,
                EvaluateArgumentNullOrWhiteSpaceGuard),
            new(
                KnownFrameworkExceptionContractCompleteness.Complete,
                IsArgumentOutOfRangeThrowHelper,
                EvaluateArgumentOutOfRangeGuard),
            new(
                KnownFrameworkExceptionContractCompleteness.Complete,
                IsObjectDisposedThrowIf,
                EvaluateObjectDisposedGuard),
            new(
                KnownFrameworkExceptionContractCompleteness.Complete,
                IsCancellationRequestedThrowHelper,
                EvaluateCancellationGuard)
        ];

        /// <summary>
        /// Determines whether the specified method is the supported
        /// <see cref="ArgumentNullException"/> <c>ThrowIfNull</c> framework helper.
        /// </summary>
        /// <param name="methodSymbol">The resolved method symbol to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <returns>
        /// <see langword="true"/> if the method is the exact framework helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsArgumentNullThrowIfNull(IMethodSymbol methodSymbol, Compilation compilation)
        {
            IMethodSymbol method = methodSymbol.OriginalDefinition;

            return HasOrdinaryVoidSignature(
                       method,
                       compilation,
                       "System.ArgumentNullException",
                       "ThrowIfNull",
                       isStatic: true,
                       arity: 0,
                       parameterCount: 2)
                && IsParameter(method.Parameters[0], compilation.GetSpecialType(SpecialType.System_Object))
                && IsParameter(method.Parameters[1], compilation.GetSpecialType(SpecialType.System_String));
        }

        /// <summary>
        /// Determines whether the specified method is the supported
        /// <see cref="ArgumentException"/> <c>ThrowIfNullOrEmpty</c> helper.
        /// </summary>
        /// <param name="methodSymbol">The resolved method symbol to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <returns>
        /// <see langword="true"/> if the method is the exact framework helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsArgumentExceptionThrowIfNullOrEmpty(IMethodSymbol methodSymbol, Compilation compilation)
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
        /// <see langword="true"/> if the method is the exact framework helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsArgumentExceptionThrowIfNullOrWhiteSpace(IMethodSymbol methodSymbol, Compilation compilation)
        {
            return IsArgumentExceptionThrowHelper(
                methodSymbol,
                compilation,
                "ThrowIfNullOrWhiteSpace");
        }

        /// <summary>
        /// Looks up and evaluates a curated framework exception contract.
        /// </summary>
        /// <param name="methodSymbol">The resolved callable symbol.</param>
        /// <param name="compilation">
        /// The compilation used to resolve exact framework symbols.
        /// </param>
        /// <param name="arguments">
        /// Argument facts ordered by parameter ordinal.
        /// </param>
        /// <returns>
        /// An explicit matched or unmatched contract evaluation containing
        /// completeness and possible exception types.
        /// </returns>
        public static KnownFrameworkExceptionContractEvaluation EvaluateContract(
            IMethodSymbol methodSymbol,
            Compilation compilation,
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments)
        {
            KnownFrameworkExceptionContract? contract = FindContract(methodSymbol, compilation);

            return contract == null
                ? KnownFrameworkExceptionContractEvaluation.NoMatch
                : contract.Evaluate(arguments, compilation);
        }

        /// <summary>
        /// Finds the curated contract for an exact framework callable.
        /// </summary>
        /// <param name="methodSymbol">The resolved callable symbol.</param>
        /// <param name="compilation">
        /// The compilation used to resolve exact framework symbols.
        /// </param>
        /// <returns>The matching contract, or <see langword="null"/>.</returns>
        public static KnownFrameworkExceptionContract? FindContract(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            foreach (KnownFrameworkExceptionContract contract in contracts)
            {
                if (contract.Matches(methodSymbol, compilation))
                {
                    return contract;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the method is a supported
        /// <see cref="ArgumentException"/> string throw helper.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <param name="expectedMethodName">The expected helper method name.</param>
        /// <returns>
        /// <see langword="true"/> if the method is the exact framework helper;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsArgumentExceptionThrowHelper(
            IMethodSymbol methodSymbol,
            Compilation compilation,
            string expectedMethodName)
        {
            IMethodSymbol method = methodSymbol.OriginalDefinition;
            INamedTypeSymbol stringType = compilation.GetSpecialType(SpecialType.System_String);

            return HasOrdinaryVoidSignature(
                       method,
                       compilation,
                       "System.ArgumentException",
                       expectedMethodName,
                       isStatic: true,
                       arity: 0,
                       parameterCount: 2)
                && IsParameter(method.Parameters[0], stringType)
                && IsParameter(method.Parameters[1], stringType);
        }

        /// <summary>
        /// Determines whether a callable is a supported
        /// <see cref="ArgumentOutOfRangeException"/> throw helper.
        /// </summary>
        /// <param name="methodSymbol">The callable to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <returns>
        /// <see langword="true"/> when the callable is supported; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsArgumentOutOfRangeThrowHelper(IMethodSymbol methodSymbol, Compilation compilation)
        {
            IMethodSymbol method = methodSymbol.OriginalDefinition;
            int valueParameterCount;

            if (method.Name is "ThrowIfZero" or "ThrowIfNegative" or "ThrowIfNegativeOrZero")
            {
                valueParameterCount = 1;
            }
            else if (method.Name is
                "ThrowIfEqual" or
                "ThrowIfNotEqual" or
                "ThrowIfGreaterThan" or
                "ThrowIfGreaterThanOrEqual" or
                "ThrowIfLessThan" or
                "ThrowIfLessThanOrEqual")
            {
                valueParameterCount = 2;
            }
            else
            {
                return false;
            }

            if (!HasOrdinaryVoidSignature(
                    method,
                    compilation,
                    "System.ArgumentOutOfRangeException",
                    method.Name,
                    isStatic: true,
                    arity: 1,
                    parameterCount: valueParameterCount + 1))
            {
                return false;
            }

            for (int index = 0; index < valueParameterCount; index++)
            {
                if (!IsMethodTypeParameter(method.Parameters[index], ordinal: 0))
                {
                    return false;
                }
            }

            return IsParameter(
                method.Parameters[valueParameterCount],
                compilation.GetSpecialType(SpecialType.System_String));
        }

        /// <summary>
        /// Determines whether a callable is the supported
        /// <see cref="ObjectDisposedException"/> <c>ThrowIf</c> helper.
        /// </summary>
        /// <param name="methodSymbol">The callable to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <returns>
        /// <see langword="true"/> when the callable is the helper; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsObjectDisposedThrowIf(IMethodSymbol methodSymbol, Compilation compilation)
        {
            IMethodSymbol method = methodSymbol.OriginalDefinition;

            if (!HasOrdinaryVoidSignature(
                    method,
                    compilation,
                    "System.ObjectDisposedException",
                    "ThrowIf",
                    isStatic: true,
                    arity: 0,
                    parameterCount: 2)
                || !IsParameter(
                    method.Parameters[0],
                    compilation.GetSpecialType(SpecialType.System_Boolean)))
            {
                return false;
            }

            ITypeSymbol instanceType = method.Parameters[1].Type;
            INamedTypeSymbol? systemType = compilation.GetTypeByMetadataName("System.Type");

            return SymbolEqualityComparer.Default.Equals(
                       instanceType,
                       compilation.GetSpecialType(SpecialType.System_Object))
                || systemType != null
                && SymbolEqualityComparer.Default.Equals(instanceType, systemType);
        }

        /// <summary>
        /// Determines whether a callable is the cancellation-token throw helper.
        /// </summary>
        /// <param name="methodSymbol">The callable to inspect.</param>
        /// <param name="compilation">The compilation used to resolve framework types.</param>
        /// <returns>
        /// <see langword="true"/> when the callable is the helper; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsCancellationRequestedThrowHelper(IMethodSymbol methodSymbol, Compilation compilation)
        {
            return HasOrdinaryVoidSignature(
                methodSymbol.OriginalDefinition,
                compilation,
                "System.Threading.CancellationToken",
                "ThrowIfCancellationRequested",
                isStatic: false,
                arity: 0,
                parameterCount: 0);
        }

        /// <summary>
        /// Evaluates the argument-null guard contract.
        /// </summary>
        /// <param name="arguments">Arguments ordered by parameter ordinal.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <returns>The possible exception types.</returns>
        private static KnownFrameworkExceptionContractResult EvaluateArgumentNullGuard(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation)
        {
            if (GetArgumentFacts(arguments, 0).ContainsAll(ExceptionFlowValueFacts.NonNull))
            {
                return default;
            }

            return ResolveExceptionTypes(compilation, "System.ArgumentNullException");
        }

        /// <summary>
        /// Evaluates the null-or-empty string guard contract.
        /// </summary>
        /// <param name="arguments">Arguments ordered by parameter ordinal.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <returns>The possible exception types.</returns>
        private static KnownFrameworkExceptionContractResult EvaluateArgumentNullOrEmptyGuard(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation)
        {
            return EvaluateArgumentStringGuard(
                arguments,
                compilation,
                ExceptionFlowValueFacts.NonEmptyString);
        }

        /// <summary>
        /// Evaluates the null-or-white-space string guard contract.
        /// </summary>
        /// <param name="arguments">Arguments ordered by parameter ordinal.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <returns>The possible exception types.</returns>
        private static KnownFrameworkExceptionContractResult EvaluateArgumentNullOrWhiteSpaceGuard(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation)
        {
            return EvaluateArgumentStringGuard(
                arguments,
                compilation,
                ExceptionFlowValueFacts.NonWhiteSpaceString);
        }

        /// <summary>
        /// Evaluates a string guard that can throw both null and value exceptions.
        /// </summary>
        /// <param name="arguments">Arguments ordered by parameter ordinal.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <param name="validStringFacts">Facts proving valid string content.</param>
        /// <returns>The possible exception types.</returns>
        private static KnownFrameworkExceptionContractResult EvaluateArgumentStringGuard(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation,
            ExceptionFlowValueFacts validStringFacts)
        {
            ExceptionFlowValueFacts facts = GetArgumentFacts(arguments, 0);
            List<string> exceptionMetadataNames = new();

            if (!facts.ContainsAll(ExceptionFlowValueFacts.NonNull))
            {
                exceptionMetadataNames.Add("System.ArgumentNullException");
            }

            if (!facts.ContainsAll(validStringFacts))
            {
                exceptionMetadataNames.Add("System.ArgumentException");
            }

            return ResolveExceptionTypes(compilation, exceptionMetadataNames);
        }

        /// <summary>
        /// Evaluates a supported argument-range throw helper.
        /// </summary>
        /// <param name="arguments">Arguments ordered by parameter ordinal.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <returns>The possible exception types.</returns>
        private static KnownFrameworkExceptionContractResult EvaluateArgumentOutOfRangeGuard(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation)
        {
            return ResolveExceptionTypes(compilation, "System.ArgumentOutOfRangeException");
        }

        /// <summary>
        /// Evaluates a supported object-disposal throw helper.
        /// </summary>
        /// <param name="arguments">Arguments ordered by parameter ordinal.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <returns>The possible exception types.</returns>
        private static KnownFrameworkExceptionContractResult EvaluateObjectDisposedGuard(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation)
        {
            return ResolveExceptionTypes(compilation, "System.ObjectDisposedException");
        }

        /// <summary>
        /// Evaluates the cancellation-token throw helper.
        /// </summary>
        /// <param name="arguments">Arguments ordered by parameter ordinal.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <returns>The possible exception types.</returns>
        private static KnownFrameworkExceptionContractResult EvaluateCancellationGuard(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            Compilation compilation)
        {
            return ResolveExceptionTypes(compilation, "System.OperationCanceledException");
        }

        /// <summary>
        /// Gets normalized facts for one argument ordinal.
        /// </summary>
        /// <param name="arguments">Arguments ordered by parameter ordinal.</param>
        /// <param name="parameterIndex">The requested parameter ordinal.</param>
        /// <returns>The argument facts or no facts when unavailable.</returns>
        private static ExceptionFlowValueFacts GetArgumentFacts(
            IReadOnlyList<KnownFrameworkExceptionContractArgument> arguments,
            int parameterIndex)
        {
            return parameterIndex >= 0 && parameterIndex < arguments.Count
                ? arguments[parameterIndex].Facts
                : ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Resolves exception symbols by metadata name.
        /// </summary>
        /// <param name="compilation">The current compilation.</param>
        /// <param name="metadataNames">The exception metadata names.</param>
        /// <returns>The resolved exception symbols.</returns>
        private static KnownFrameworkExceptionContractResult ResolveExceptionTypes(
            Compilation compilation,
            params string[] metadataNames)
        {
            return ResolveExceptionTypes(compilation, (IEnumerable<string>)metadataNames);
        }

        /// <summary>
        /// Resolves exception symbols by metadata name.
        /// </summary>
        /// <param name="compilation">The current compilation.</param>
        /// <param name="metadataNames">The exception metadata names.</param>
        /// <returns>The resolved exception symbols.</returns>
        private static KnownFrameworkExceptionContractResult ResolveExceptionTypes(
            Compilation compilation,
            IEnumerable<string> metadataNames)
        {
            List<INamedTypeSymbol> exceptionTypes = new();

            foreach (string metadataName in metadataNames)
            {
                INamedTypeSymbol? exceptionType = compilation.GetTypeByMetadataName(metadataName);

                if (exceptionType != null)
                {
                    exceptionTypes.Add(exceptionType);
                }
            }

            return new KnownFrameworkExceptionContractResult(
                exceptionTypes.ToImmutableArray());
        }

        /// <summary>
        /// Determines whether a method has the required ordinary void signature.
        /// </summary>
        /// <param name="method">The method to inspect.</param>
        /// <param name="compilation">The current compilation.</param>
        /// <param name="containingTypeMetadataName">The exact containing type.</param>
        /// <param name="methodName">The exact method name.</param>
        /// <param name="isStatic">The required static state.</param>
        /// <param name="arity">The required generic arity.</param>
        /// <param name="parameterCount">The required parameter count.</param>
        /// <returns>
        /// <see langword="true"/> when all common signature dimensions match;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasOrdinaryVoidSignature(
            IMethodSymbol method,
            Compilation compilation,
            string containingTypeMetadataName,
            string methodName,
            bool isStatic,
            int arity,
            int parameterCount)
        {
            if (method.MethodKind != MethodKind.Ordinary
                || method.IsStatic != isStatic
                || method.Name != methodName
                || method.Arity != arity
                || method.Parameters.Length != parameterCount
                || method.ReturnType.SpecialType != SpecialType.System_Void
                || !IsType(
                    method.ContainingType.OriginalDefinition,
                    compilation,
                    containingTypeMetadataName))
            {
                return false;
            }

            return method.Parameters.All(static parameter => parameter.RefKind == RefKind.None);
        }

        /// <summary>
        /// Determines whether a parameter has the required by-value type.
        /// </summary>
        /// <param name="parameter">The parameter to inspect.</param>
        /// <param name="expectedType">The expected parameter type.</param>
        /// <returns>
        /// <see langword="true"/> when the parameter type and ref kind match;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsParameter(IParameterSymbol parameter, ITypeSymbol expectedType)
        {
            return parameter.RefKind == RefKind.None
                && SymbolEqualityComparer.Default.Equals(parameter.Type, expectedType);
        }

        /// <summary>
        /// Determines whether a parameter uses the specified method type parameter.
        /// </summary>
        /// <param name="parameter">The parameter to inspect.</param>
        /// <param name="ordinal">The required method type-parameter ordinal.</param>
        /// <returns>
        /// <see langword="true"/> when the parameter matches; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsMethodTypeParameter(IParameterSymbol parameter, int ordinal)
        {
            return parameter.RefKind == RefKind.None
                && parameter.Type is ITypeParameterSymbol typeParameter
                && typeParameter.TypeParameterKind == TypeParameterKind.Method
                && typeParameter.Ordinal == ordinal;
        }

        /// <summary>
        /// Determines whether a resolved type is the specified framework type.
        /// </summary>
        /// <param name="actualType">The resolved containing type.</param>
        /// <param name="compilation">The compilation used to resolve the expected type.</param>
        /// <param name="metadataName">The expected framework metadata name.</param>
        /// <returns>
        /// <see langword="true"/> if both symbols represent the same type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsType(
            INamedTypeSymbol actualType,
            Compilation compilation,
            string metadataName)
        {
            INamedTypeSymbol? expectedType = compilation.GetTypeByMetadataName(metadataName);

            return expectedType != null
                && SymbolEqualityComparer.Default.Equals(
                    actualType,
                    expectedType.OriginalDefinition);
        }
    }
}
