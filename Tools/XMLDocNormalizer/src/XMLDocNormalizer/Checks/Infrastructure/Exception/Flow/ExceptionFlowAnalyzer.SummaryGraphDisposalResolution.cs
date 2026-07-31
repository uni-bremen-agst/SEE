using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains resolution of synchronous and asynchronous disposal targets
    /// selected by C# using constructs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Stores the metadata name of <see cref="IDisposable"/>.
        /// </summary>
        private const string DisposableInterfaceMetadataName =
            "System.IDisposable";

        /// <summary>
        /// Stores the metadata name of <see cref="IAsyncDisposable"/>.
        /// </summary>
        private const string AsyncDisposableInterfaceMetadataName =
            "System.IAsyncDisposable";

        /// <summary>
        /// Resolves the disposal method selected by C# using semantics.
        /// </summary>
        /// <param name="resourceType">
        /// The static resource type.
        /// </param>
        /// <param name="sourceNode">
        /// The using resource source node.
        /// </param>
        /// <param name="isAsynchronous">
        /// Whether <c>DisposeAsync</c> is required instead of
        /// <c>Dispose</c>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for speculative overload resolution and
        /// interface implementation lookup.
        /// </param>
        /// <param name="disposalMethod">
        /// The resolved disposal method.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if one method was resolved; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryResolveSummaryDisposalMethod(
            ITypeSymbol resourceType,
            SyntaxNode sourceNode,
            bool isAsynchronous,
            SemanticModel semanticModel,
            out IMethodSymbol? disposalMethod)
        {
            return TryResolveSummaryDisposalMethod(
                resourceType,
                sourceNode,
                isAsynchronous,
                semanticModel,
                out disposalMethod,
                out _);
        }

        /// <summary>
        /// Resolves the disposal method selected by C# using semantics and the
        /// method whose runtime dispatch slot determines the implementation.
        /// </summary>
        /// <param name="resourceType">
        /// The static resource type.
        /// </param>
        /// <param name="sourceNode">
        /// The using resource source node.
        /// </param>
        /// <param name="isAsynchronous">
        /// Whether <c>DisposeAsync</c> is required instead of
        /// <c>Dispose</c>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for speculative overload resolution and
        /// interface implementation lookup.
        /// </param>
        /// <param name="disposalMethod">
        /// The concrete implementation selected for the static resource type,
        /// or the interface member when no concrete implementation is fixed.
        /// </param>
        /// <param name="dispatchMethod">
        /// The virtual or interface member whose runtime slot must be
        /// expanded. For pattern-based disposal this is identical to
        /// <paramref name="disposalMethod"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a disposal method and dispatch slot were
        /// resolved; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveSummaryDisposalMethod(
            ITypeSymbol resourceType,
            SyntaxNode sourceNode,
            bool isAsynchronous,
            SemanticModel semanticModel,
            out IMethodSymbol? disposalMethod,
            out IMethodSymbol? dispatchMethod)
        {
            disposalMethod = null;
            dispatchMethod = null;

            ITypeSymbol effectiveType = isAsynchronous
                ? resourceType
                : UnwrapSummaryNullableResourceType(
                    resourceType);

            if (isAsynchronous)
            {
                if (TryResolveSummaryPatternDisposalMethod(
                        effectiveType,
                        sourceNode,
                        WellKnownMemberNames.DisposeAsyncMethodName,
                        false,
                        semanticModel,
                        out disposalMethod))
                {
                    dispatchMethod = disposalMethod;
                    return true;
                }

                return TryResolveSummaryInterfaceDisposalMethod(
                    effectiveType,
                    true,
                    semanticModel.Compilation,
                    out disposalMethod,
                    out dispatchMethod);
            }

            if (effectiveType is INamedTypeSymbol namedType &&
                namedType.IsRefLikeType &&
                TryResolveSummaryPatternDisposalMethod(
                    effectiveType,
                    sourceNode,
                    WellKnownMemberNames.DisposeMethodName,
                    true,
                    semanticModel,
                    out disposalMethod))
            {
                dispatchMethod = disposalMethod;
                return true;
            }

            return TryResolveSummaryInterfaceDisposalMethod(
                effectiveType,
                false,
                semanticModel.Compilation,
                out disposalMethod,
                out dispatchMethod);
        }

        /// <summary>
        /// Removes a nullable value-type wrapper from a resource type.
        /// </summary>
        /// <param name="resourceType">
        /// The resource type to normalize.
        /// </param>
        /// <returns>
        /// The nullable underlying type when present; otherwise the original
        /// resource type.
        /// </returns>
        private static ITypeSymbol UnwrapSummaryNullableResourceType(
            ITypeSymbol resourceType)
        {
            if (resourceType is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType ==
                    SpecialType.System_Nullable_T &&
                namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }

            return resourceType;
        }

        /// <summary>
        /// Resolves a pattern-based disposal method by asking Roslyn to bind
        /// a zero-explicit-argument invocation on the resource type.
        /// </summary>
        /// <param name="resourceType">
        /// The normalized resource type.
        /// </param>
        /// <param name="sourceNode">
        /// The source node supplying the speculative binding position.
        /// </param>
        /// <param name="methodName">
        /// The required disposal method name.
        /// </param>
        /// <param name="requiresVoidReturn">
        /// Whether the selected method must return <c>void</c>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for speculative overload resolution.
        /// </param>
        /// <param name="disposalMethod">
        /// The selected accessible instance method.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if Roslyn resolves a suitable pattern
        /// method; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveSummaryPatternDisposalMethod(
            ITypeSymbol resourceType,
            SyntaxNode sourceNode,
            string methodName,
            bool requiresVoidReturn,
            SemanticModel semanticModel,
            out IMethodSymbol? disposalMethod)
        {
            disposalMethod = null;

            InvocationExpressionSyntax invocation =
                CreateSummaryDisposalPatternInvocation(
                    resourceType,
                    methodName);

            SymbolInfo symbolInfo =
                semanticModel.GetSpeculativeSymbolInfo(
                    sourceNode.SpanStart,
                    invocation,
                    SpeculativeBindingOption.BindAsExpression);

            if (symbolInfo.Symbol
                    is not IMethodSymbol selectedMethod ||
                selectedMethod.IsStatic ||
                selectedMethod.ReducedFrom != null ||
                selectedMethod.Arity != 0 ||
                requiresVoidReturn &&
                !selectedMethod.ReturnsVoid ||
                !requiresVoidReturn &&
                selectedMethod.ReturnsVoid)
            {
                return false;
            }

            disposalMethod = selectedMethod;
            return true;
        }

        /// <summary>
        /// Creates a synthetic zero-argument disposal invocation for Roslyn
        /// overload resolution.
        /// </summary>
        /// <param name="resourceType">
        /// The receiver type.
        /// </param>
        /// <param name="methodName">
        /// The disposal method name.
        /// </param>
        /// <returns>
        /// A syntax-only invocation equivalent to
        /// <c>default(ResourceType).Dispose()</c> or
        /// <c>default(ResourceType).DisposeAsync()</c>.
        /// </returns>
        private static InvocationExpressionSyntax
            CreateSummaryDisposalPatternInvocation(
                ITypeSymbol resourceType,
                string methodName)
        {
            string typeName =
                resourceType.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat);

            TypeSyntax resourceTypeSyntax =
                SyntaxFactory.ParseTypeName(
                    typeName);

            DefaultExpressionSyntax receiver =
                SyntaxFactory.DefaultExpression(
                    resourceTypeSyntax);

            MemberAccessExpressionSyntax memberAccess =
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParenthesizedExpression(
                        receiver),
                    SyntaxFactory.IdentifierName(
                        methodName));

            return SyntaxFactory.InvocationExpression(
                memberAccess,
                SyntaxFactory.ArgumentList());
        }

        /// <summary>
        /// Resolves synchronous or asynchronous disposal through the
        /// corresponding framework interface.
        /// </summary>
        /// <param name="resourceType">
        /// The normalized static resource type.
        /// </param>
        /// <param name="isAsynchronous">
        /// Whether <see cref="IAsyncDisposable"/> is required instead of
        /// <see cref="IDisposable"/>.
        /// </param>
        /// <param name="compilation">
        /// The compilation used for interface and implementation resolution.
        /// </param>
        /// <param name="disposalMethod">
        /// The concrete implementation for the static resource type, or the
        /// interface member when the type is an interface or type parameter.
        /// </param>
        /// <param name="dispatchMethod">
        /// The framework interface member whose runtime implementation is
        /// selected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the resource type implements the required
        /// interface; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveSummaryInterfaceDisposalMethod(
            ITypeSymbol resourceType,
            bool isAsynchronous,
            Compilation compilation,
            out IMethodSymbol? disposalMethod,
            out IMethodSymbol? dispatchMethod)
        {
            disposalMethod = null;
            dispatchMethod = null;

            string interfaceMetadataName =
                isAsynchronous
                    ? AsyncDisposableInterfaceMetadataName
                    : DisposableInterfaceMetadataName;

            string methodName =
                isAsynchronous
                    ? WellKnownMemberNames.DisposeAsyncMethodName
                    : WellKnownMemberNames.DisposeMethodName;

            INamedTypeSymbol? interfaceType =
                compilation.GetTypeByMetadataName(
                    interfaceMetadataName);

            if (interfaceType == null)
            {
                return false;
            }

            IMethodSymbol? interfaceMethod =
                interfaceType.GetMembers(methodName)
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(
                        static method =>
                            !method.IsStatic &&
                            method.Arity == 0 &&
                            method.Parameters.Length == 0);

            if (interfaceMethod == null ||
                !SummaryTypeImplementsInterface(
                    resourceType,
                    interfaceType,
                    new HashSet<ISymbol>(
                        SymbolEqualityComparer.Default)))
            {
                return false;
            }

            dispatchMethod = interfaceMethod;

            if (resourceType is ITypeParameterSymbol ||
                resourceType.TypeKind == TypeKind.Interface)
            {
                disposalMethod = interfaceMethod;
                return true;
            }

            if (resourceType is not INamedTypeSymbol namedType)
            {
                return false;
            }

            ISymbol? implementation =
                namedType.FindImplementationForInterfaceMember(
                    interfaceMethod);

            if (implementation is not IMethodSymbol methodImplementation)
            {
                return false;
            }

            disposalMethod = methodImplementation;
            return true;
        }

        /// <summary>
        /// Determines whether a named type or type parameter implements one
        /// specified interface.
        /// </summary>
        /// <param name="typeSymbol">
        /// The resource type to inspect.
        /// </param>
        /// <param name="interfaceType">
        /// The required interface.
        /// </param>
        /// <param name="inspectedTypes">
        /// Types already inspected during recursive constraint traversal.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the interface is implemented or
        /// constrained; otherwise <see langword="false"/>.
        /// </returns>
        private static bool SummaryTypeImplementsInterface(
            ITypeSymbol typeSymbol,
            INamedTypeSymbol interfaceType,
            HashSet<ISymbol> inspectedTypes)
        {
            if (!inspectedTypes.Add(typeSymbol))
            {
                return false;
            }

            if (typeSymbol is INamedTypeSymbol namedType)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        namedType.OriginalDefinition,
                        interfaceType.OriginalDefinition))
                {
                    return true;
                }

                return namedType.AllInterfaces.Any(
                    implementedInterface =>
                        SymbolEqualityComparer.Default.Equals(
                            implementedInterface.OriginalDefinition,
                            interfaceType.OriginalDefinition));
            }

            if (typeSymbol
                is not ITypeParameterSymbol typeParameter)
            {
                return false;
            }

            foreach (ITypeSymbol constraintType
                     in typeParameter.ConstraintTypes)
            {
                if (SummaryTypeImplementsInterface(
                        constraintType,
                        interfaceType,
                        inspectedTypes))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
