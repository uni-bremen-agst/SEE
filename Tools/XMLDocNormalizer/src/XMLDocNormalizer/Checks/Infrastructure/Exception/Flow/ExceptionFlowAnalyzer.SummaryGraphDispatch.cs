using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains runtime-target resolution for explicitly written virtual and
    /// interface method invocations in summary graphs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Adds one direct invocation edge or one edge for every known runtime
        /// target of a virtual invocation.
        /// </summary>
        /// <param name="invocation">
        /// The invocation syntax being analyzed.
        /// </param>
        /// <param name="methodSymbol">
        /// The method selected by compile-time binding.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to locate runtime target
        /// declarations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving target summaries.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryInvocationEdges(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (semanticModel.GetOperation(invocation)
                    is not IInvocationOperation invocationOperation ||
                !invocationOperation.IsVirtual)
            {
                AddSummaryDirectInvocationEdge(
                    invocation,
                    methodSymbol,
                    semanticModel,
                    graph,
                    fragment,
                    callerContext);

                return;
            }

            IReadOnlyList<IMethodSymbol> runtimeTargets =
                ResolveSummaryInvocationRuntimeTargets(
                    invocationOperation,
                    methodSymbol,
                    semanticContext,
                    fragment);

            if (runtimeTargets.Count == 0)
            {
                AddSummaryDirectInvocationEdge(
                    invocation,
                    methodSymbol,
                    semanticModel,
                    graph,
                    fragment,
                    callerContext);

                return;
            }

            ExceptionFlowPathStepKind stepKind =
                methodSymbol.ContainingType.TypeKind ==
                    TypeKind.Interface
                        ? ExceptionFlowPathStepKind
                            .InterfaceMethodCall
                        : ExceptionFlowPathStepKind
                            .VirtualMethodCall;

            foreach (IMethodSymbol runtimeTarget
                     in runtimeTargets)
            {
                ExceptionFlowCallContext targetContext =
                    CreateDispatchCallContext(
                        methodSymbol,
                        runtimeTarget,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callerContext);

                ExceptionFlowCallableKey targetKey =
                    new(
                        runtimeTarget,
                        targetContext.Key);

                graph.GetOrAdd(
                    targetKey,
                    targetContext);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        CreatePathStep(
                            stepKind,
                            runtimeTarget,
                            invocation)));
            }
        }

        /// <summary>
        /// Adds one invocation edge for a call whose target is statically
        /// fixed.
        /// </summary>
        /// <param name="invocation">
        /// The invocation syntax being analyzed.
        /// </param>
        /// <param name="methodSymbol">
        /// The statically selected target method.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target summary.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the call edge.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryDirectInvocationEdge(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            ExceptionFlowCallContext targetContext =
                CreateCallContext(
                    methodSymbol,
                    invocation.ArgumentList.Arguments,
                    semanticModel,
                    callerContext);

            ExceptionFlowCallableKey targetKey =
                new(
                    methodSymbol,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            ExceptionFlowPathStepKind stepKind =
                methodSymbol.MethodKind ==
                    MethodKind.LocalFunction
                        ? ExceptionFlowPathStepKind
                            .LocalFunctionCall
                        : ExceptionFlowPathStepKind
                            .MethodCall;

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        stepKind,
                        methodSymbol,
                        invocation)));
        }

        /// <summary>
        /// Resolves every known executable runtime target for one virtual or
        /// interface invocation and records incomplete-target uncertainty.
        /// </summary>
        /// <param name="invocationOperation">
        /// The Roslyn invocation operation.
        /// </param>
        /// <param name="methodSymbol">
        /// The method selected by compile-time binding.
        /// </param>
        /// <param name="semanticContext">
        /// The semantic context containing analysis compilations and source
        /// types.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving dispatch uncertainty.
        /// </param>
        /// <returns>
        /// The distinct known runtime target methods compatible with the
        /// static receiver.
        /// </returns>
        private static IReadOnlyList<IMethodSymbol>
            ResolveSummaryInvocationRuntimeTargets(
                IInvocationOperation invocationOperation,
                IMethodSymbol methodSymbol,
                ProjectClosureSemanticContext semanticContext,
                ExceptionFlowSummaryFragment fragment)
        {
            ITypeSymbol? receiverType =
                invocationOperation.Instance?.Type;

            INamedTypeSymbol? exactReceiverType =
                GetSummaryExactReceiverType(
                    invocationOperation.Instance);

            return ResolveSummaryRuntimeTargets(
                methodSymbol,
                receiverType,
                exactReceiverType,
                semanticContext,
                fragment);
        }

        /// <summary>
        /// Resolves every known executable runtime target for one virtual or
        /// interface member while restricting candidates to the static
        /// receiver type or generic receiver constraints.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method or accessor selected by compile-time binding.
        /// </param>
        /// <param name="receiverType">
        /// The static receiver type, including a possible type parameter, or
        /// <see langword="null"/>.
        /// </param>
        /// <param name="exactReceiverType">
        /// The exact runtime receiver type when proven directly from the
        /// source, or <see langword="null"/>.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <returns>
        /// The distinct known executable runtime targets.
        /// </returns>
        private static IReadOnlyList<IMethodSymbol>
            ResolveSummaryRuntimeTargets(
                IMethodSymbol methodSymbol,
                ITypeSymbol? receiverType,
                INamedTypeSymbol? exactReceiverType,
                ProjectClosureSemanticContext semanticContext)
        {
            Dictionary<string, IMethodSymbol> runtimeTargets =
                new(StringComparer.Ordinal);

            foreach (ProjectClosureCompilationScope scope
                     in semanticContext.GetAnalysisCompilationScopes())
            {
                IMethodSymbol? scopedMethod =
                    ResolveSummaryMethodInCompilation(
                        methodSymbol,
                        scope.Compilation);

                if (scopedMethod == null)
                {
                    continue;
                }

                if (exactReceiverType != null)
                {
                    INamedTypeSymbol? scopedExactReceiverType =
                        ResolveSummaryTypeInCompilation(
                            exactReceiverType,
                            scope.Compilation);

                    if (scopedExactReceiverType != null &&
                        IsSummaryCompatibleRuntimeReceiver(
                            scopedExactReceiverType,
                            receiverType,
                            scope.Compilation))
                    {
                        TryAddSummaryRuntimeTarget(
                            scopedExactReceiverType,
                            scopedMethod,
                            runtimeTargets);
                    }

                    continue;
                }

                foreach (INamedTypeSymbol candidateType
                         in scope.SourceTypes)
                {
                    if (!IsSummaryCompatibleRuntimeReceiver(
                            candidateType,
                            receiverType,
                            scope.Compilation))
                    {
                        continue;
                    }

                    TryAddSummaryRuntimeTarget(
                        candidateType,
                        scopedMethod,
                        runtimeTargets);
                }
            }

            return runtimeTargets.Values
                .OrderBy(
                    static target =>
                        target.ToDisplayString(
                            SymbolDisplayFormat
                                .CSharpErrorMessageFormat),
                    StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Resolves and adds the executable method reached for one possible
        /// runtime receiver type.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible concrete runtime receiver type.
        /// </param>
        /// <param name="methodSymbol">
        /// The virtual class member or interface member selected by static
        /// binding.
        /// </param>
        /// <param name="runtimeTargets">
        /// The destination containing distinct executable source targets.
        /// </param>
        private static void TryAddSummaryRuntimeTarget(
            INamedTypeSymbol runtimeType,
            IMethodSymbol methodSymbol,
            Dictionary<string, IMethodSymbol> runtimeTargets)
        {
            if (runtimeType.IsAbstract ||
                runtimeType.TypeKind ==
                TypeKind.Interface)
            {
                return;
            }

            IMethodSymbol? runtimeTarget;

            if (methodSymbol.ContainingType.TypeKind ==
                TypeKind.Interface)
            {
                runtimeTarget =
                    runtimeType.FindImplementationForInterfaceMember(
                        methodSymbol)
                    as IMethodSymbol;
            }
            else
            {
                runtimeTarget =
                    ResolveSummaryMostDerivedRuntimeOverride(
                        runtimeType,
                        methodSymbol);
            }

            if (runtimeTarget == null)
            {
                return;
            }

            runtimeTarget =
                ResolveSummaryMostDerivedRuntimeOverride(
                    runtimeType,
                    runtimeTarget);

            if (runtimeTarget.IsAbstract ||
                runtimeTarget.DeclaringSyntaxReferences.IsDefaultOrEmpty)
            {
                return;
            }

            string targetKey =
                CreateSummaryRuntimeTargetKey(
                    runtimeTarget);

            runtimeTargets.TryAdd(
                targetKey,
                runtimeTarget);
        }

        /// <summary>
        /// Resolves the most-derived override of an implementation method for
        /// one possible runtime receiver type.
        /// </summary>
        /// <param name="runtimeType">
        /// The concrete receiver type whose executed implementation should be
        /// determined.
        /// </param>
        /// <param name="implementationMethod">
        /// The class method initially selected directly or as the
        /// implementation of an interface member.
        /// </param>
        /// <returns>
        /// The most-derived override belonging to the same virtual slot, or
        /// <paramref name="implementationMethod"/> when the method is
        /// non-virtual or no overriding method exists.
        /// </returns>
        private static IMethodSymbol
            ResolveSummaryMostDerivedRuntimeOverride(
                INamedTypeSymbol runtimeType,
                IMethodSymbol implementationMethod)
        {
            if (!implementationMethod.IsVirtual &&
                !implementationMethod.IsAbstract &&
                !implementationMethod.IsOverride)
            {
                return implementationMethod;
            }

            for (INamedTypeSymbol? currentType = runtimeType;
                 currentType != null;
                 currentType = currentType.BaseType)
            {
                foreach (IMethodSymbol candidateMethod
                         in currentType.GetMembers(
                                 implementationMethod.Name)
                             .OfType<IMethodSymbol>())
                {
                    if (!IsSummarySameVirtualMethodSlot(
                            candidateMethod,
                            implementationMethod))
                    {
                        continue;
                    }

                    return candidateMethod;
                }

                if (SymbolEqualityComparer.Default.Equals(
                        currentType.OriginalDefinition,
                        implementationMethod
                            .ContainingType
                            .OriginalDefinition))
                {
                    break;
                }
            }

            return implementationMethod;
        }

        /// <summary>
        /// Determines whether a candidate method belongs to the same virtual
        /// method slot as an initially selected implementation.
        /// </summary>
        /// <param name="candidateMethod">
        /// The possible override declared on a runtime receiver type or one of
        /// its base types.
        /// </param>
        /// <param name="implementationMethod">
        /// The implementation whose virtual slot should be matched.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the candidate is the implementation
        /// itself or overrides it directly or transitively; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummarySameVirtualMethodSlot(
            IMethodSymbol candidateMethod,
            IMethodSymbol implementationMethod)
        {
            for (IMethodSymbol? currentMethod = candidateMethod;
                 currentMethod != null;
                 currentMethod = currentMethod.OverriddenMethod)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        currentMethod.OriginalDefinition,
                        implementationMethod.OriginalDefinition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a stable deduplication key for one runtime target method.
        /// </summary>
        /// <param name="runtimeTarget">
        /// The runtime target method.
        /// </param>
        /// <returns>
        /// A key combining the containing assembly and declaration identity.
        /// </returns>
        private static string CreateSummaryRuntimeTargetKey(
            IMethodSymbol runtimeTarget)
        {
            string assemblyIdentity =
                runtimeTarget.ContainingAssembly.Identity.ToString();

            string? declarationId =
                DocumentationCommentId.CreateDeclarationId(
                    runtimeTarget.OriginalDefinition);

            string methodIdentity =
                string.IsNullOrEmpty(
                    declarationId)
                    ? runtimeTarget.OriginalDefinition.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat)
                    : declarationId;

            return $"{assemblyIdentity}|{methodIdentity}";
        }

        /// <summary>
        /// Resolves the method that implements an interface member for one
        /// possible runtime receiver type.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible runtime receiver type.
        /// </param>
        /// <param name="interfaceMethod">
        /// The interface method represented in the same compilation.
        /// </param>
        /// <returns>
        /// The implicit, explicit, inherited, or default implementation, or
        /// <see langword="null"/> when the runtime type does not implement the
        /// interface member.
        /// </returns>
        private static IMethodSymbol?
            ResolveSummaryInterfaceRuntimeTarget(
                INamedTypeSymbol runtimeType,
                IMethodSymbol interfaceMethod)
        {
            INamedTypeSymbol? implementedInterface =
                FindSummaryMatchingInterface(
                    runtimeType,
                    interfaceMethod.ContainingType);

            if (implementedInterface == null)
            {
                return null;
            }

            IMethodSymbol? implementedInterfaceMethod =
                ResolveSummaryMethodOnContainingType(
                    interfaceMethod,
                    implementedInterface);

            if (implementedInterfaceMethod == null)
            {
                return null;
            }

            ISymbol? implementation =
                runtimeType.FindImplementationForInterfaceMember(
                    implementedInterfaceMethod);

            if (implementation is IMethodSymbol implementationMethod)
            {
                return implementationMethod;
            }

            return implementedInterfaceMethod.IsAbstract
                ? null
                : implementedInterfaceMethod;
        }

        /// <summary>
        /// Resolves the most-derived override selected for one possible
        /// runtime receiver type.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible runtime receiver type.
        /// </param>
        /// <param name="virtualMethod">
        /// The virtual class method represented in the same compilation.
        /// </param>
        /// <returns>
        /// The effective override or inherited virtual method, or
        /// <see langword="null"/> when the runtime type is incompatible with
        /// the method's containing type.
        /// </returns>
        private static IMethodSymbol?
            ResolveSummaryVirtualRuntimeTarget(
                INamedTypeSymbol runtimeType,
                IMethodSymbol virtualMethod)
        {
            INamedTypeSymbol? matchingBaseType =
                FindSummaryMatchingBaseType(
                    runtimeType,
                    virtualMethod.ContainingType);

            if (matchingBaseType == null)
            {
                return null;
            }

            IMethodSymbol? matchingVirtualMethod =
                ResolveSummaryMethodOnContainingType(
                    virtualMethod,
                    matchingBaseType);

            if (matchingVirtualMethod == null)
            {
                return null;
            }

            INamedTypeSymbol? currentType =
                runtimeType;

            while (currentType != null)
            {
                foreach (IMethodSymbol candidateMethod
                         in currentType.GetMembers(
                                 matchingVirtualMethod.Name)
                             .OfType<IMethodSymbol>())
                {
                    if (candidateMethod.IsStatic)
                    {
                        continue;
                    }

                    if (SymbolEqualityComparer.Default.Equals(
                            candidateMethod.OriginalDefinition,
                            matchingVirtualMethod.OriginalDefinition) ||
                        DoesSummaryMethodOverride(
                            candidateMethod,
                            matchingVirtualMethod))
                    {
                        return candidateMethod;
                    }
                }

                if (IsSummarySamePossibleType(
                        currentType,
                        matchingBaseType))
                {
                    break;
                }

                currentType = currentType.BaseType;
            }

            return matchingVirtualMethod.IsAbstract
                ? null
                : matchingVirtualMethod;
        }

        /// <summary>
        /// Determines whether one method overrides another method directly or
        /// transitively.
        /// </summary>
        /// <param name="candidateMethod">
        /// The candidate override.
        /// </param>
        /// <param name="baseMethod">
        /// The expected base method.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the candidate's override chain reaches
        /// the base method; otherwise <see langword="false"/>.
        /// </returns>
        private static bool DoesSummaryMethodOverride(
            IMethodSymbol candidateMethod,
            IMethodSymbol baseMethod)
        {
            IMethodSymbol? overriddenMethod =
                candidateMethod.OverriddenMethod;

            while (overriddenMethod != null)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        overriddenMethod.OriginalDefinition,
                        baseMethod.OriginalDefinition))
                {
                    return true;
                }

                overriddenMethod =
                    overriddenMethod.OverriddenMethod;
            }

            return false;
        }

        /// <summary>
        /// Finds the interface implemented by a runtime type that can satisfy
        /// the compile-time receiver interface.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible runtime receiver type.
        /// </param>
        /// <param name="targetInterface">
        /// The compile-time receiver interface.
        /// </param>
        /// <returns>
        /// The matching implemented interface, or <see langword="null"/>.
        /// </returns>
        private static INamedTypeSymbol? FindSummaryMatchingInterface(
            INamedTypeSymbol runtimeType,
            INamedTypeSymbol targetInterface)
        {
            foreach (INamedTypeSymbol implementedInterface
                     in runtimeType.AllInterfaces)
            {
                if (IsSummarySamePossibleType(
                        implementedInterface,
                        targetInterface))
                {
                    return implementedInterface;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the base-type relation through which a runtime type can
        /// receive one virtual class method.
        /// </summary>
        /// <param name="runtimeType">
        /// The possible runtime receiver type.
        /// </param>
        /// <param name="targetType">
        /// The compile-time method-containing type.
        /// </param>
        /// <returns>
        /// The matching runtime or base type, or <see langword="null"/>.
        /// </returns>
        private static INamedTypeSymbol? FindSummaryMatchingBaseType(
            INamedTypeSymbol runtimeType,
            INamedTypeSymbol targetType)
        {
            INamedTypeSymbol? currentType =
                runtimeType;

            while (currentType != null)
            {
                if (IsSummarySamePossibleType(
                        currentType,
                        targetType))
                {
                    return currentType;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Determines whether two named types are equal or can represent the
        /// same constructed type after substituting source type parameters.
        /// </summary>
        /// <param name="candidateType">
        /// The candidate runtime relation.
        /// </param>
        /// <param name="targetType">
        /// The compile-time target type.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for exact equality or compatible generic
        /// definitions; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummarySamePossibleType(
            INamedTypeSymbol candidateType,
            INamedTypeSymbol targetType)
        {
            if (SymbolEqualityComparer.Default.Equals(
                    candidateType,
                    targetType))
            {
                return true;
            }

            return ContainsSummaryTypeParameter(
                       candidateType) &&
                   SymbolEqualityComparer.Default.Equals(
                       candidateType.OriginalDefinition,
                       targetType.OriginalDefinition);
        }

        /// <summary>
        /// Determines whether a type contains a source type parameter whose
        /// runtime substitution can make a generic relation compatible.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the type contains a type parameter;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContainsSummaryTypeParameter(
            ITypeSymbol typeSymbol)
        {
            return typeSymbol switch
            {
                ITypeParameterSymbol => true,
                IArrayTypeSymbol arrayType =>
                    ContainsSummaryTypeParameter(
                        arrayType.ElementType),
                IPointerTypeSymbol pointerType =>
                    ContainsSummaryTypeParameter(
                        pointerType.PointedAtType),
                INamedTypeSymbol namedType =>
                    namedType.TypeArguments.Any(
                        ContainsSummaryTypeParameter),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether a named type can occur as a concrete runtime
        /// receiver.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for nonabstract classes and structs;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryConcreteRuntimeType(
            INamedTypeSymbol typeSymbol)
        {
            return !typeSymbol.IsAbstract &&
                   typeSymbol.TypeKind is
                       TypeKind.Class or
                       TypeKind.Struct;
        }

        /// <summary>
        /// Extracts an exact runtime receiver type when the receiver expression
        /// directly creates the invoked object.
        /// </summary>
        /// <param name="instanceOperation">
        /// The invocation receiver operation.
        /// </param>
        /// <returns>
        /// The created named type, or <see langword="null"/> when the receiver
        /// is not known exactly.
        /// </returns>
        private static INamedTypeSymbol? GetSummaryExactReceiverType(
            IOperation? instanceOperation)
        {
            IOperation? currentOperation =
                instanceOperation;

            while (currentOperation != null)
            {
                switch (currentOperation)
                {
                    case IConversionOperation conversionOperation:
                        currentOperation =
                            conversionOperation.Operand;
                        continue;

                    case IParenthesizedOperation parenthesizedOperation:
                        currentOperation =
                            parenthesizedOperation.Operand;
                        continue;

                    case IObjectCreationOperation objectCreationOperation:
                        return objectCreationOperation.Type
                            as INamedTypeSymbol;

                    default:
                        return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves a method or accessor symbol into another compilation while
        /// preserving its constructed containing type when possible.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method or accessor to resolve.
        /// </param>
        /// <param name="compilation">
        /// The destination compilation.
        /// </param>
        /// <returns>
        /// The corresponding method symbol, or <see langword="null"/>.
        /// </returns>
        private static IMethodSymbol? ResolveSummaryMethodInCompilation(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            IMethodSymbol? originalMethod =
                ResolveSummaryAssociatedAccessorInCompilation(
                    methodSymbol,
                    compilation);

            if (originalMethod == null)
            {
                string? declarationId =
                    DocumentationCommentId.CreateDeclarationId(
                        methodSymbol.OriginalDefinition);

                if (string.IsNullOrEmpty(
                        declarationId))
                {
                    return null;
                }

                originalMethod =
                    DocumentationCommentId
                        .GetSymbolsForDeclarationId(
                            declarationId,
                            compilation)
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(
                            candidate =>
                                HasSummarySameAssemblyIdentity(
                                    candidate,
                                    methodSymbol));
            }

            if (originalMethod == null)
            {
                return null;
            }

            INamedTypeSymbol? containingType =
                ResolveSummaryTypeInCompilation(
                    methodSymbol.ContainingType,
                    compilation);

            if (containingType == null)
            {
                return originalMethod;
            }

            return ResolveSummaryMethodOnContainingType(
                       originalMethod,
                       containingType) ??
                   originalMethod;
        }

        /// <summary>
        /// Resolves a property or event accessor through the declaration id of
        /// its associated symbol.
        /// </summary>
        /// <param name="methodSymbol">
        /// The accessor to resolve.
        /// </param>
        /// <param name="compilation">
        /// The destination compilation.
        /// </param>
        /// <returns>
        /// The corresponding accessor, or <see langword="null"/> when the
        /// method is not an accessor or cannot be resolved.
        /// </returns>
        private static IMethodSymbol?
            ResolveSummaryAssociatedAccessorInCompilation(
                IMethodSymbol methodSymbol,
                Compilation compilation)
        {
            ISymbol? associatedSymbol =
                methodSymbol.AssociatedSymbol;

            if (associatedSymbol == null)
            {
                return null;
            }

            string? declarationId =
                DocumentationCommentId.CreateDeclarationId(
                    associatedSymbol.OriginalDefinition);

            if (string.IsNullOrEmpty(
                    declarationId))
            {
                return null;
            }

            ISymbol? resolvedAssociatedSymbol =
                DocumentationCommentId
                    .GetSymbolsForDeclarationId(
                        declarationId,
                        compilation)
                    .FirstOrDefault(
                        candidate =>
                            HasSummarySameAssemblyIdentity(
                                candidate,
                                associatedSymbol));

            return GetSummaryAssociatedAccessor(
                resolvedAssociatedSymbol,
                methodSymbol.MethodKind);
        }

        /// <summary>
        /// Gets the accessor of one associated property or event that
        /// corresponds to a method kind.
        /// </summary>
        /// <param name="associatedSymbol">
        /// The resolved property or event.
        /// </param>
        /// <param name="methodKind">
        /// The required accessor method kind.
        /// </param>
        /// <returns>
        /// The corresponding accessor, or <see langword="null"/>.
        /// </returns>
        private static IMethodSymbol? GetSummaryAssociatedAccessor(
            ISymbol? associatedSymbol,
            MethodKind methodKind)
        {
            if (associatedSymbol
                is IPropertySymbol propertySymbol)
            {
                return methodKind switch
                {
                    MethodKind.PropertyGet =>
                        propertySymbol.GetMethod,

                    MethodKind.PropertySet =>
                        propertySymbol.SetMethod,

                    _ => null
                };
            }

            if (associatedSymbol
                is IEventSymbol eventSymbol)
            {
                return methodKind switch
                {
                    MethodKind.EventAdd =>
                        eventSymbol.AddMethod,

                    MethodKind.EventRemove =>
                        eventSymbol.RemoveMethod,

                    _ => null
                };
            }

            return null;
        }

        /// <summary>
        /// Resolves a named type symbol into another compilation.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to resolve.
        /// </param>
        /// <param name="compilation">
        /// The destination compilation.
        /// </param>
        /// <returns>
        /// The corresponding type symbol, or <see langword="null"/>.
        /// </returns>
        private static INamedTypeSymbol? ResolveSummaryTypeInCompilation(
            INamedTypeSymbol typeSymbol,
            Compilation compilation)
        {
            string? referenceId =
                DocumentationCommentId.CreateReferenceId(
                    typeSymbol);

            if (string.IsNullOrEmpty(
                    referenceId))
            {
                return null;
            }

            return DocumentationCommentId
                .GetSymbolsForReferenceId(
                    referenceId,
                    compilation)
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault(
                    candidate =>
                        HasSummarySameAssemblyIdentity(
                            candidate,
                            typeSymbol));
        }

        /// <summary>
        /// Resolves one method or accessor definition on a constructed or
        /// substituted containing type.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method definition to resolve.
        /// </param>
        /// <param name="containingType">
        /// The containing type on which to resolve it.
        /// </param>
        /// <returns>
        /// The corresponding method member, or <see langword="null"/>.
        /// </returns>
        private static IMethodSymbol? ResolveSummaryMethodOnContainingType(
            IMethodSymbol methodSymbol,
            INamedTypeSymbol containingType)
        {
            if (methodSymbol.AssociatedSymbol
                is IPropertySymbol associatedProperty)
            {
                foreach (IPropertySymbol candidateProperty
                         in containingType.GetMembers()
                             .OfType<IPropertySymbol>())
                {
                    if (!SymbolEqualityComparer.Default.Equals(
                            candidateProperty.OriginalDefinition,
                            associatedProperty.OriginalDefinition))
                    {
                        continue;
                    }

                    return GetSummaryAssociatedAccessor(
                        candidateProperty,
                        methodSymbol.MethodKind);
                }

                return null;
            }

            if (methodSymbol.AssociatedSymbol
                is IEventSymbol associatedEvent)
            {
                foreach (IEventSymbol candidateEvent
                         in containingType.GetMembers()
                             .OfType<IEventSymbol>())
                {
                    if (!SymbolEqualityComparer.Default.Equals(
                            candidateEvent.OriginalDefinition,
                            associatedEvent.OriginalDefinition))
                    {
                        continue;
                    }

                    return GetSummaryAssociatedAccessor(
                        candidateEvent,
                        methodSymbol.MethodKind);
                }

                return null;
            }

            foreach (IMethodSymbol candidateMethod
                     in containingType.GetMembers(
                             methodSymbol.Name)
                         .OfType<IMethodSymbol>())
            {
                if (SymbolEqualityComparer.Default.Equals(
                        candidateMethod.OriginalDefinition,
                        methodSymbol.OriginalDefinition))
                {
                    return candidateMethod;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether two symbols belong to the same assembly
        /// identity across different Roslyn compilations.
        /// </summary>
        /// <param name="leftSymbol">
        /// The first symbol.
        /// </param>
        /// <param name="rightSymbol">
        /// The second symbol.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when both containing assembly identities are
        /// equal; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasSummarySameAssemblyIdentity(
            ISymbol leftSymbol,
            ISymbol rightSymbol)
        {
            return string.Equals(
                leftSymbol.ContainingAssembly?.Identity.ToString(),
                rightSymbol.ContainingAssembly?.Identity.ToString(),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates a stable cross-compilation identity for one method
        /// declaration.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method whose identity should be created.
        /// </param>
        /// <returns>
        /// The assembly-qualified documentation declaration id, or a stable
        /// display fallback when no declaration id is available.
        /// </returns>
        private static string CreateSummaryDeclarationIdentity(
            IMethodSymbol methodSymbol)
        {
            string assemblyIdentity =
                methodSymbol.ContainingAssembly?.Identity.ToString() ??
                string.Empty;

            string declarationIdentity =
                DocumentationCommentId.CreateDeclarationId(
                    methodSymbol.OriginalDefinition) ??
                methodSymbol.OriginalDefinition.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat);

            return assemblyIdentity +
                   "|" +
                   declarationIdentity;
        }
    }
}
