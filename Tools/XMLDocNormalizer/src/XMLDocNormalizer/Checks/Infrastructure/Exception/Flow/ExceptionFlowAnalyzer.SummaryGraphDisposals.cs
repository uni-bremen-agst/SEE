using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for implicit disposal performed by
    /// synchronous and asynchronous using constructs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects implicit disposal calls belonging to using statements and
        /// using declarations in the current callable.
        /// </summary>
        /// <param name="node">
        /// The syntax node whose using constructs should be inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for resource and disposal resolution.
        /// </param>
        /// <param name="graph">
        /// The graph receiving disposal target nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving disposal edges or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AnalyzeSummaryDisposals(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            List<SummaryDisposalResource> resources =
                CollectSummaryDisposalResources(
                    node,
                    semanticModel);

            resources.Sort(
                static (left, right) =>
                {
                    int disposalPositionComparison =
                        left.DisposalPosition.CompareTo(
                            right.DisposalPosition);

                    if (disposalPositionComparison != 0)
                    {
                        return disposalPositionComparison;
                    }

                    return right.SourceNode.SpanStart.CompareTo(
                        left.SourceNode.SpanStart);
                });

            foreach (SummaryDisposalResource resource
                     in resources)
            {
                AnalyzeSummaryDisposalResource(
                    resource,
                    semanticModel,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Collects resource types and source locations for using statements
        /// and using declarations in the current callable.
        /// </summary>
        /// <param name="node">
        /// The syntax node whose using constructs should be inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for local and expression types.
        /// </param>
        /// <returns>
        /// The collected disposal resources in unspecified order.
        /// </returns>
        private static List<SummaryDisposalResource>
            CollectSummaryDisposalResources(
                SyntaxNode node,
                SemanticModel semanticModel)
        {
            List<SummaryDisposalResource> resources =
                new();

            foreach (UsingStatementSyntax usingStatement
                     in GetSummaryDescendantsAndSelf
                         <UsingStatementSyntax>(node))
            {
                bool isAsynchronous =
                    usingStatement.AwaitKeyword.IsKind(
                        SyntaxKind.AwaitKeyword);

                if (usingStatement.Declaration != null)
                {
                    AddSummaryDeclaredDisposalResources(
                        usingStatement.Declaration,
                        isAsynchronous,
                        usingStatement.Span.End,
                        semanticModel,
                        resources);
                }
                else if (usingStatement.Expression != null)
                {
                    AddSummaryExpressionDisposalResource(
                        usingStatement.Expression,
                        isAsynchronous,
                        usingStatement.Span.End,
                        semanticModel,
                        resources);
                }
            }

            foreach (LocalDeclarationStatementSyntax declaration
                     in GetSummaryDescendantsAndSelf
                         <LocalDeclarationStatementSyntax>(node))
            {
                if (!declaration.UsingKeyword.IsKind(
                        SyntaxKind.UsingKeyword))
                {
                    continue;
                }

                bool isAsynchronous =
                    declaration.AwaitKeyword.IsKind(
                        SyntaxKind.AwaitKeyword);

                AddSummaryDeclaredDisposalResources(
                    declaration.Declaration,
                    isAsynchronous,
                    GetSummaryUsingDeclarationDisposalPosition(
                        declaration),
                    semanticModel,
                    resources);
            }

            return resources;
        }

        /// <summary>
        /// Gets the end position of the scope owning a using declaration.
        /// </summary>
        /// <param name="declaration">
        /// The using declaration to inspect.
        /// </param>
        /// <returns>
        /// The source position at which the declared resource is disposed.
        /// </returns>
        private static int GetSummaryUsingDeclarationDisposalPosition(
            LocalDeclarationStatementSyntax declaration)
        {
            if (declaration.Parent is BlockSyntax containingBlock)
            {
                return containingBlock.CloseBraceToken.SpanStart;
            }

            BlockSyntax? nearestBlock =
                declaration.Ancestors()
                    .OfType<BlockSyntax>()
                    .FirstOrDefault();

            if (nearestBlock != null)
            {
                return nearestBlock.CloseBraceToken.SpanStart;
            }

            CompilationUnitSyntax compilationUnit =
                declaration.SyntaxTree.GetCompilationUnitRoot();

            return compilationUnit.EndOfFileToken.SpanStart;
        }

        /// <summary>
        /// Adds all resources declared by one using statement or declaration.
        /// </summary>
        /// <param name="declaration">
        /// The resource variable declaration.
        /// </param>
        /// <param name="isAsynchronous">
        /// Whether asynchronous disposal is required.
        /// </param>
        /// <param name="disposalPosition">
        /// The source position at which the declaration scope ends.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for local-symbol resolution.
        /// </param>
        /// <param name="resources">
        /// The destination resource list.
        /// </param>
        private static void AddSummaryDeclaredDisposalResources(
            VariableDeclarationSyntax declaration,
            bool isAsynchronous,
            int disposalPosition,
            SemanticModel semanticModel,
            List<SummaryDisposalResource> resources)
        {
            foreach (VariableDeclaratorSyntax variable
                     in declaration.Variables)
            {
                ITypeSymbol? resourceType =
                    (semanticModel.GetDeclaredSymbol(variable)
                        as ILocalSymbol)?.Type;

                ExpressionSyntax? valueExpression =
                    variable.Initializer?.Value;

                resourceType ??=
                    valueExpression == null
                        ? null
                        : GetSummaryResourceType(
                            valueExpression,
                            semanticModel);

                if (resourceType == null)
                {
                    resources.Add(
                        SummaryDisposalResource.CreateUnresolved(
                            variable,
                            isAsynchronous,
                            disposalPosition));

                    continue;
                }

                bool isKnownNull =
                    valueExpression != null &&
                    IsSummaryKnownNullResource(
                        valueExpression,
                        semanticModel);

                resources.Add(
                    new SummaryDisposalResource(
                        resourceType,
                        variable,
                        isAsynchronous,
                        isKnownNull,
                        disposalPosition));
            }
        }

        /// <summary>
        /// Adds one expression-form using resource.
        /// </summary>
        /// <param name="expression">
        /// The resource expression.
        /// </param>
        /// <param name="isAsynchronous">
        /// Whether asynchronous disposal is required.
        /// </param>
        /// <param name="disposalPosition">
        /// The source position at which the using statement ends.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for resource-type resolution.
        /// </param>
        /// <param name="resources">
        /// The destination resource list.
        /// </param>
        private static void AddSummaryExpressionDisposalResource(
            ExpressionSyntax expression,
            bool isAsynchronous,
            int disposalPosition,
            SemanticModel semanticModel,
            List<SummaryDisposalResource> resources)
        {
            ITypeSymbol? resourceType =
                GetSummaryResourceType(
                    expression,
                    semanticModel);

            if (resourceType == null)
            {
                resources.Add(
                    SummaryDisposalResource.CreateUnresolved(
                        expression,
                        isAsynchronous,
                        disposalPosition));

                return;
            }

            resources.Add(
                new SummaryDisposalResource(
                    resourceType,
                    expression,
                    isAsynchronous,
                    IsSummaryKnownNullResource(
                        expression,
                        semanticModel),
                    disposalPosition));
        }

        /// <summary>
        /// Resolves the static source type of one using resource expression.
        /// </summary>
        /// <param name="expression">
        /// The resource expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <returns>
        /// The source expression type, or its converted type when no source
        /// type is available.
        /// </returns>
        private static ITypeSymbol? GetSummaryResourceType(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(
                    expression);

            return typeInfo.Type ??
                   typeInfo.ConvertedType;
        }

        /// <summary>
        /// Determines whether a using resource expression is proven to
        /// evaluate to <see langword="null"/>.
        /// </summary>
        /// <param name="expression">
        /// The resource expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for operation and local-symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression necessarily evaluates to
        /// <see langword="null"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryKnownNullResource(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            IOperation? operation =
                semanticModel.GetOperation(
                    expression);

            if (operation == null)
            {
                return false;
            }

            HashSet<ILocalSymbol> inspectedLocals =
                new(SymbolEqualityComparer.Default);

            return IsSummaryKnownNullResourceOperation(
                operation,
                expression.SpanStart,
                semanticModel,
                inspectedLocals);
        }

        /// <summary>
        /// Determines recursively whether an operation or a stable local
        /// initializer is proven to produce the null value.
        /// </summary>
        /// <param name="operation">
        /// The operation to inspect.
        /// </param>
        /// <param name="usePosition">
        /// The source position at which the operation value is consumed.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and operation analysis.
        /// </param>
        /// <param name="inspectedLocals">
        /// The local variables already visited during alias resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation necessarily produces
        /// <see langword="null"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryKnownNullResourceOperation(
            IOperation operation,
            int usePosition,
            SemanticModel semanticModel,
            HashSet<ILocalSymbol> inspectedLocals)
        {
            if (IsSummaryKnownNullOperation(
                    operation))
            {
                return true;
            }

            switch (operation)
            {
                case IParenthesizedOperation parenthesizedOperation:
                    return IsSummaryKnownNullResourceOperation(
                        parenthesizedOperation.Operand,
                        usePosition,
                        semanticModel,
                        inspectedLocals);

                case IConversionOperation conversionOperation
                    when !conversionOperation.Conversion.IsUserDefined &&
                         CanSummaryOperationTypeBeNull(
                             conversionOperation.Type):
                    return IsSummaryKnownNullResourceOperation(
                        conversionOperation.Operand,
                        usePosition,
                        semanticModel,
                        inspectedLocals);

                case ILocalReferenceOperation localReference:
                    return IsSummaryLocalKnownNullAtPosition(
                        localReference.Local,
                        usePosition,
                        semanticModel,
                        inspectedLocals);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether a local variable is proven null at one later source
        /// position through a stable null-valued initializer.
        /// </summary>
        /// <param name="localSymbol">
        /// The local variable to inspect.
        /// </param>
        /// <param name="usePosition">
        /// The source position at which the value is consumed.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for declaration and write resolution.
        /// </param>
        /// <param name="inspectedLocals">
        /// The local variables already visited during alias resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local has a proven null initializer and
        /// has not potentially been written or aliased before use; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryLocalKnownNullAtPosition(
            ILocalSymbol localSymbol,
            int usePosition,
            SemanticModel semanticModel,
            HashSet<ILocalSymbol> inspectedLocals)
        {
            if (!inspectedLocals.Add(
                    localSymbol) ||
                localSymbol.DeclaringSyntaxReferences.Length != 1 ||
                localSymbol.DeclaringSyntaxReferences[0].GetSyntax()
                    is not VariableDeclaratorSyntax declarator ||
                declarator.Initializer == null ||
                declarator.SyntaxTree !=
                    semanticModel.SyntaxTree ||
                declarator.SpanStart >=
                    usePosition ||
                HasSummaryLocalWriteOrAliasBeforeUse(
                    localSymbol,
                    declarator.Span.End,
                    usePosition,
                    semanticModel))
            {
                return false;
            }

            IOperation? initializerOperation =
                semanticModel.GetOperation(
                    declarator.Initializer.Value);

            if (initializerOperation == null)
            {
                return false;
            }

            return IsSummaryKnownNullResourceOperation(
                initializerOperation,
                declarator.SpanStart,
                semanticModel,
                inspectedLocals);
        }

        /// <summary>
        /// Determines whether a local variable may be changed or exposed through
        /// a writable alias between its declaration and one later use.
        /// </summary>
        /// <param name="localSymbol">
        /// The local variable to inspect.
        /// </param>
        /// <param name="startPosition">
        /// The first source position after the variable declaration.
        /// </param>
        /// <param name="endPosition">
        /// The exclusive source position of the later use.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol comparison.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a write or writable alias may affect the
        /// local before use; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasSummaryLocalWriteOrAliasBeforeUse(
            ILocalSymbol localSymbol,
            int startPosition,
            int endPosition,
            SemanticModel semanticModel)
        {
            SyntaxNode root =
                semanticModel.SyntaxTree.GetRoot();

            foreach (AssignmentExpressionSyntax assignment
                     in root.DescendantNodes()
                         .OfType<AssignmentExpressionSyntax>())
            {
                if (!IsSummaryNodeBetweenPositions(
                        assignment,
                        startPosition,
                        endPosition) ||
                    !ContainsLocalSymbolReference(
                        assignment.Left,
                        localSymbol,
                        semanticModel))
                {
                    continue;
                }

                return true;
            }

            foreach (PrefixUnaryExpressionSyntax prefixExpression
                     in root.DescendantNodes()
                         .OfType<PrefixUnaryExpressionSyntax>())
            {
                if (!IsSummaryIncrementOrDecrement(
                        prefixExpression.Kind()) ||
                    !IsSummaryNodeBetweenPositions(
                        prefixExpression,
                        startPosition,
                        endPosition) ||
                    !ContainsLocalSymbolReference(
                        prefixExpression.Operand,
                        localSymbol,
                        semanticModel))
                {
                    continue;
                }

                return true;
            }

            foreach (PostfixUnaryExpressionSyntax postfixExpression
                     in root.DescendantNodes()
                         .OfType<PostfixUnaryExpressionSyntax>())
            {
                if (!IsSummaryIncrementOrDecrement(
                        postfixExpression.Kind()) ||
                    !IsSummaryNodeBetweenPositions(
                        postfixExpression,
                        startPosition,
                        endPosition) ||
                    !ContainsLocalSymbolReference(
                        postfixExpression.Operand,
                        localSymbol,
                        semanticModel))
                {
                    continue;
                }

                return true;
            }

            foreach (ArgumentSyntax argument
                     in root.DescendantNodes()
                         .OfType<ArgumentSyntax>())
            {
                if (!IsSummaryNodeBetweenPositions(
                        argument,
                        startPosition,
                        endPosition) ||
                    !argument.RefKindKeyword.IsKind(
                        SyntaxKind.RefKeyword) &&
                    !argument.RefKindKeyword.IsKind(
                        SyntaxKind.OutKeyword))
                {
                    continue;
                }

                if (ContainsLocalSymbolReference(
                        argument.Expression,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            foreach (RefExpressionSyntax refExpression
                     in root.DescendantNodes()
                         .OfType<RefExpressionSyntax>())
            {
                if (!IsSummaryNodeBetweenPositions(
                        refExpression,
                        startPosition,
                        endPosition) ||
                    !ContainsLocalSymbolReference(
                        refExpression.Expression,
                        localSymbol,
                        semanticModel))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a syntax node lies inside one open source-position
        /// interval.
        /// </summary>
        /// <param name="node">
        /// The syntax node to inspect.
        /// </param>
        /// <param name="startPosition">
        /// The inclusive lower source position.
        /// </param>
        /// <param name="endPosition">
        /// The exclusive upper source position.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the node starts within the interval;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryNodeBetweenPositions(
            SyntaxNode node,
            int startPosition,
            int endPosition)
        {
            return node.SpanStart >=
                       startPosition &&
                   node.SpanStart <
                       endPosition;
        }

        /// <summary>
        /// Determines whether a unary syntax kind changes its operand value.
        /// </summary>
        /// <param name="kind">
        /// The unary syntax kind.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for prefix or postfix increment and decrement;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryIncrementOrDecrement(
            SyntaxKind kind)
        {
            return kind ==
                       SyntaxKind.PreIncrementExpression ||
                   kind ==
                       SyntaxKind.PreDecrementExpression ||
                   kind ==
                       SyntaxKind.PostIncrementExpression ||
                   kind ==
                       SyntaxKind.PostDecrementExpression;
        }

        /// <summary>
        /// Resolves and records the implicit disposal call for one resource.
        /// </summary>
        /// <param name="resource">
        /// The resource to dispose.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for disposal-method lookup.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the disposal target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the edge or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AnalyzeSummaryDisposalResource(
            SummaryDisposalResource resource,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (resource.IsKnownNull)
            {
                return;
            }

            if (resource.ResourceType == null)
            {
                fragment.AddUncertainTarget(
                    resource.IsAsynchronous
                        ? "Asynchronous using resource type could not be resolved."
                        : "Synchronous using resource type could not be resolved.");

                return;
            }

            if (resource.ResourceType.TypeKind ==
                TypeKind.Dynamic)
            {
                fragment.AddUncertainTarget(
                    resource.IsAsynchronous
                        ? "Dynamic asynchronous using conversion and DisposeAsync target"
                        : "Dynamic synchronous using conversion and Dispose target");

                return;
            }

            if (!TryResolveSummaryDisposalMethod(
                    resource.ResourceType,
                    resource.SourceNode,
                    resource.IsAsynchronous,
                    semanticModel,
                    out IMethodSymbol? disposalMethod) ||
                disposalMethod == null)
            {
                string resourceTypeName =
                    resource.ResourceType.ToDisplayString(
                        SymbolDisplayFormat
                            .CSharpErrorMessageFormat);

                if (string.IsNullOrWhiteSpace(
                        resourceTypeName))
                {
                    resourceTypeName =
                        "<unknown resource type>";
                }

                fragment.AddUncertainTarget(
                    resource.IsAsynchronous
                        ? $"DisposeAsync target for '{resourceTypeName}' could not be resolved."
                        : $"Dispose target for '{resourceTypeName}' could not be resolved.");

                return;
            }

            AddSummaryDisposalEdge(
                disposalMethod,
                resource.SourceNode,
                resource.IsAsynchronous,
                semanticModel,
                graph,
                fragment,
                callContext);
        }

        /// <summary>
        /// Adds one implicit synchronous or asynchronous disposal edge.
        /// </summary>
        /// <param name="disposalMethod">
        /// The resolved disposal target.
        /// </param>
        /// <param name="sourceNode">
        /// The source-level using resource.
        /// </param>
        /// <param name="isAsynchronous">
        /// Whether this is an asynchronous disposal call.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to create the implicit call context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target node.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the call edge.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="disposalMethod"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static void AddSummaryDisposalEdge(
            IMethodSymbol disposalMethod,
            SyntaxNode sourceNode,
            bool isAsynchronous,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            ArgumentNullException.ThrowIfNull(disposalMethod);

            ExceptionFlowCallContext targetContext =
                CreateCallContext(
                    disposalMethod,
                    default,
                    semanticModel,
                    callContext);

            ExceptionFlowCallableKey targetKey =
                new(
                    disposalMethod,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        isAsynchronous
                            ? ExceptionFlowPathStepKind.DisposeAsyncCall
                            : ExceptionFlowPathStepKind.DisposeCall,
                        disposalMethod,
                        sourceNode)));
        }

        /// <summary>
        /// Describes one resource whose lifetime ends through an implicit
        /// using disposal operation.
        /// </summary>
        private readonly struct SummaryDisposalResource
        {
            /// <summary>
            /// Initializes a disposal resource.
            /// </summary>
            /// <param name="resourceType">
            /// The static resource type, or <see langword="null"/> when it
            /// could not be resolved.
            /// </param>
            /// <param name="sourceNode">
            /// The source node representing the resource.
            /// </param>
            /// <param name="isAsynchronous">
            /// Whether asynchronous disposal is required.
            /// </param>
            /// <param name="isKnownNull">
            /// Whether the resource is proven to evaluate to null.
            /// </param>
            /// <param name="disposalPosition">
            /// The source position at which the resource is disposed.
            /// </param>
            public SummaryDisposalResource(
                ITypeSymbol? resourceType,
                SyntaxNode sourceNode,
                bool isAsynchronous,
                bool isKnownNull,
                int disposalPosition)
            {
                ResourceType = resourceType;
                SourceNode = sourceNode;
                IsAsynchronous = isAsynchronous;
                IsKnownNull = isKnownNull;
                DisposalPosition = disposalPosition;
            }

            /// <summary>
            /// Gets the resolved static resource type.
            /// </summary>
            /// <value>
            /// The resource type, or <see langword="null"/> when unresolved.
            /// </value>
            public ITypeSymbol? ResourceType { get; }

            /// <summary>
            /// Gets the source node representing the resource.
            /// </summary>
            /// <value>The resource source node.</value>
            public SyntaxNode SourceNode { get; }

            /// <summary>
            /// Gets whether asynchronous disposal is required.
            /// </summary>
            /// <value>
            /// <see langword="true"/> for <c>await using</c>; otherwise
            /// <see langword="false"/>.
            /// </value>
            public bool IsAsynchronous { get; }

            /// <summary>
            /// Gets whether the resource is proven to evaluate to null.
            /// </summary>
            /// <value>
            /// <see langword="true"/> when no disposal call can occur;
            /// otherwise <see langword="false"/>.
            /// </value>
            public bool IsKnownNull { get; }

            /// <summary>
            /// Gets the source position at which disposal occurs.
            /// </summary>
            /// <value>The zero-based source position.</value>
            public int DisposalPosition { get; }

            /// <summary>
            /// Creates a resource whose static type could not be resolved.
            /// </summary>
            /// <param name="sourceNode">
            /// The source node representing the resource.
            /// </param>
            /// <param name="isAsynchronous">
            /// Whether asynchronous disposal is required.
            /// </param>
            /// <param name="disposalPosition">
            /// The source position at which disposal occurs.
            /// </param>
            /// <returns>The unresolved disposal resource.</returns>
            public static SummaryDisposalResource CreateUnresolved(
                SyntaxNode sourceNode,
                bool isAsynchronous,
                int disposalPosition)
            {
                return new SummaryDisposalResource(
                    null,
                    sourceNode,
                    isAsynchronous,
                    false,
                    disposalPosition);
            }
        }
    }
}
