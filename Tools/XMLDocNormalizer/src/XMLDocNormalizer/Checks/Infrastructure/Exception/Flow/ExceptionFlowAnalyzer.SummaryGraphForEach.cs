using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for synchronous and asynchronous
    /// enumeration operations selected by C# foreach binding.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects implicit enumerator acquisition, advancement,
        /// current-value access, conversion, disposal, and asynchronous
        /// awaiter calls belonging to foreach statements in the current
        /// callable.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for foreach binding information.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving compiler-selected callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AnalyzeSummaryForEachOperations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (CommonForEachStatementSyntax forEachStatement
                     in GetSummaryDescendantsAndSelf
                         <CommonForEachStatementSyntax>(node))
            {
                AnalyzeSummaryForEachOperation(
                    forEachStatement,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Adds every compiler-selected implicit operation belonging to one
        /// synchronous or asynchronous foreach statement.
        /// </summary>
        /// <param name="forEachStatement">
        /// The foreach statement to analyze.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for foreach binding information.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving compiler-selected callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AnalyzeSummaryForEachOperation(
            CommonForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            ForEachStatementInfo forEachInfo =
                semanticModel.GetForEachStatementInfo(
                    forEachStatement);

            bool isAsynchronous =
                forEachInfo.IsAsynchronous;

            if (!isAsynchronous &&
                IsSummaryBuiltInForEachCollection(
                    forEachStatement.Expression,
                    semanticModel))
            {
                AddSummaryForEachConversionEdges(
                    forEachInfo,
                    forEachStatement,
                    semanticModel,
                    graph,
                    fragment,
                    callContext);

                return;
            }

            if (forEachInfo.GetEnumeratorMethod == null)
            {
                AddSummaryUnresolvedForEachTarget(
                    forEachStatement,
                    isAsynchronous,
                    semanticModel,
                    fragment);

                return;
            }

            INamedTypeSymbol? collectionReceiverType =
                GetSummaryImplicitReceiverType(
                    forEachStatement.Expression,
                    semanticModel);

            INamedTypeSymbol? exactCollectionReceiverType =
                GetSummaryImplicitExactReceiverType(
                    forEachStatement.Expression,
                    semanticModel);

            AddSummaryImplicitDispatchMethodEdges(
                forEachInfo.GetEnumeratorMethod,
                isAsynchronous
                    ? ExceptionFlowPathStepKind
                        .AsyncForEachGetEnumeratorCall
                    : ExceptionFlowPathStepKind
                        .ForEachGetEnumeratorCall,
                forEachStatement,
                forEachStatement.Expression,
                collectionReceiverType,
                exactCollectionReceiverType,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            INamedTypeSymbol? enumeratorReceiverType =
                forEachInfo.GetEnumeratorMethod.ReturnType
                    as INamedTypeSymbol;

            if (forEachInfo.MoveNextMethod == null)
            {
                fragment.AddUncertainTarget(
                    isAsynchronous
                        ? "Await-foreach MoveNextAsync target could not be resolved."
                        : "Foreach MoveNext target could not be resolved.");
            }
            else
            {
                AddSummaryImplicitDispatchMethodEdges(
                    forEachInfo.MoveNextMethod,
                    isAsynchronous
                        ? ExceptionFlowPathStepKind
                            .AsyncForEachMoveNextCall
                        : ExceptionFlowPathStepKind
                            .ForEachMoveNextCall,
                    forEachStatement,
                    reducedExtensionReceiver: null,
                    enumeratorReceiverType,
                    exactReceiverType: null,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);

                if (isAsynchronous)
                {
                    AddSummaryImplicitAwaitDispatchEdges(
                        forEachInfo.MoveNextMethod.ReturnType,
                        forEachStatement,
                        "Await-foreach MoveNextAsync",
                        semanticModel,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);
                }
            }

            if (!TryAddSummaryImplicitDispatchGetterEdges(
                    forEachInfo.CurrentProperty,
                    isAsynchronous
                        ? ExceptionFlowPathStepKind
                            .AsyncForEachCurrentGetter
                        : ExceptionFlowPathStepKind
                            .ForEachCurrentGetter,
                    forEachStatement,
                    enumeratorReceiverType,
                    exactReceiverType: null,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext))
            {
                fragment.AddUncertainTarget(
                    isAsynchronous
                        ? "Await-foreach Current getter could not be resolved."
                        : "Foreach Current getter could not be resolved.");
            }

            AddSummaryForEachConversionEdges(
                forEachInfo,
                forEachStatement,
                semanticModel,
                graph,
                fragment,
                callContext);

            if (forEachInfo.DisposeMethod == null)
            {
                return;
            }

            AddSummaryImplicitDispatchMethodEdges(
                forEachInfo.DisposeMethod,
                isAsynchronous
                    ? ExceptionFlowPathStepKind.DisposeAsyncCall
                    : ExceptionFlowPathStepKind.DisposeCall,
                forEachStatement,
                reducedExtensionReceiver: null,
                enumeratorReceiverType,
                exactReceiverType: null,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            if (isAsynchronous)
            {
                AddSummaryImplicitAwaitDispatchEdges(
                    forEachInfo.DisposeMethod.ReturnType,
                    forEachStatement,
                    "Await-foreach DisposeAsync",
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Adds user-defined current-value and iteration-variable conversion
        /// calls selected for one foreach statement.
        /// </summary>
        /// <param name="forEachInfo">
        /// Roslyn's semantic information for the foreach statement.
        /// </param>
        /// <param name="forEachStatement">
        /// The foreach statement responsible for the conversions.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the statement.
        /// </param>
        /// <param name="graph">
        /// The graph receiving conversion targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving conversion edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AddSummaryForEachConversionEdges(
            ForEachStatementInfo forEachInfo,
            CommonForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            HashSet<IMethodSymbol> addedMethods =
                new(SymbolEqualityComparer.Default);

            AddSummaryForEachConversionEdge(
                forEachInfo.CurrentConversion.MethodSymbol,
                forEachStatement,
                semanticModel,
                graph,
                fragment,
                callContext,
                addedMethods);

            AddSummaryForEachConversionEdge(
                forEachInfo.ElementConversion.MethodSymbol,
                forEachStatement,
                semanticModel,
                graph,
                fragment,
                callContext,
                addedMethods);
        }

        /// <summary>
        /// Adds one deduplicated user-defined foreach conversion edge.
        /// </summary>
        /// <param name="conversionMethod">
        /// The conversion method selected by Roslyn.
        /// </param>
        /// <param name="forEachStatement">
        /// The foreach statement responsible for the conversion.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the statement.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the conversion target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the conversion edge.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        /// <param name="addedMethods">
        /// The conversion methods already added for the statement.
        /// </param>
        private static void AddSummaryForEachConversionEdge(
            IMethodSymbol? conversionMethod,
            CommonForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<IMethodSymbol> addedMethods)
        {
            if (conversionMethod == null ||
                !addedMethods.Add(
                    conversionMethod.OriginalDefinition))
            {
                return;
            }

            AddSummaryImplicitMethodEdge(
                conversionMethod,
                ExceptionFlowPathStepKind.ConversionOperatorCall,
                forEachStatement,
                reducedExtensionReceiver: null,
                semanticModel,
                graph,
                fragment,
                callContext);
        }

        /// <summary>
        /// Determines whether a foreach statement uses compiler-provided
        /// array or string enumeration without source-level callable symbols.
        /// </summary>
        /// <param name="expression">
        /// The collection expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for collection-type resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for arrays and strings; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryBuiltInForEachCollection(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(
                    expression);

            ITypeSymbol? collectionType =
                typeInfo.Type ??
                typeInfo.ConvertedType;

            return collectionType is IArrayTypeSymbol ||
                   collectionType?.SpecialType ==
                       SpecialType.System_String;
        }

        /// <summary>
        /// Adds explicit uncertainty for a non-built-in foreach operation
        /// whose compiler-selected enumerator target is unavailable.
        /// </summary>
        /// <param name="forEachStatement">
        /// The foreach statement to describe.
        /// </param>
        /// <param name="isAsynchronous">
        /// Whether asynchronous enumeration was requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for collection-type resolution.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving uncertainty.
        /// </param>
        private static void AddSummaryUnresolvedForEachTarget(
            CommonForEachStatementSyntax forEachStatement,
            bool isAsynchronous,
            SemanticModel semanticModel,
            ExceptionFlowSummaryFragment fragment)
        {
            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(
                    forEachStatement.Expression);

            ITypeSymbol? collectionType =
                typeInfo.Type ??
                typeInfo.ConvertedType;

            if (collectionType?.TypeKind ==
                TypeKind.Dynamic)
            {
                fragment.AddUncertainTarget(
                    isAsynchronous
                        ? "Dynamic await-foreach enumerator binding"
                        : "Dynamic foreach enumerator binding");

                return;
            }

            string typeName =
                collectionType?.ToDisplayString(
                    SymbolDisplayFormat
                        .CSharpErrorMessageFormat) ??
                "<unknown collection type>";

            if (string.IsNullOrWhiteSpace(
                    typeName))
            {
                typeName =
                    "<unknown collection type>";
            }

            fragment.AddUncertainTarget(
                isAsynchronous
                    ? $"Await-foreach enumerator target for '{typeName}' could not be resolved."
                    : $"Foreach enumerator target for '{typeName}' could not be resolved.");
        }
    }
}
