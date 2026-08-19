using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains try/catch-specific exception-flow analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Analyzes a try-statement and suppresses exceptions from the
        /// try-block that are fully handled by one of its catch-clauses.
        /// </summary>
        /// <param name="tryStatement">
        /// The try-statement to analyze.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="mode">The traversal mode.</param>
        /// <param name="callContext">
        /// The call-site facts known for the currently analyzed callable.
        /// </param>
        private static void AnalyzeTryStatement(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowTraversalMode mode,
            ExceptionFlowCallContext callContext)
        {
            ExceptionFlowAnalysisResult tryResult =
                new();

            AnalyzeNode(
                tryStatement.Block,
                semanticModel,
                semanticContext,
                tryResult,
                traversalState,
                mode,
                callContext);

            SuppressCaughtExceptionsFromTry(
                tryStatement,
                semanticModel,
                tryResult);

            MergeResults(
                result,
                tryResult);

            foreach (CatchClauseSyntax catchClause
                     in tryStatement.Catches)
            {
                if (catchClause.Filter != null)
                {
                    AnalyzeNode(
                        catchClause.Filter.FilterExpression,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        mode,
                        callContext);
                }

                AnalyzeNode(
                    catchClause.Block,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    mode,
                    callContext);
            }

            if (tryStatement.Finally != null)
            {
                AnalyzeNode(
                    tryStatement.Finally.Block,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    mode,
                    callContext);
            }
        }

        /// <summary>
        /// Suppresses exceptions from a try-block that are fully handled by
        /// the associated catch-clauses.
        /// </summary>
        /// <param name="tryStatement">
        /// The try-statement whose catches should be evaluated.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for catch type and rethrow resolution.
        /// </param>
        /// <param name="tryResult">
        /// The exception-flow result produced for the try-block.
        /// </param>
        private static void SuppressCaughtExceptionsFromTry(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult tryResult)
        {
            foreach (CatchClauseSyntax catchClause
                     in tryStatement.Catches)
            {
                if (!CatchSuppressesOriginalException(
                        catchClause,
                        semanticModel) ||
                    catchClause.Filter != null)
                {
                    continue;
                }

                if (IsCatchAll(
                        catchClause,
                        semanticModel))
                {
                    tryResult.ClearThrownExceptions();
                    tryResult.ClearExternalDocumentationEvidence();
                    tryResult.UncertainTargets.Clear();

                    return;
                }

                INamedTypeSymbol? caughtType =
                    GetCaughtExceptionType(
                        catchClause,
                        semanticModel);

                if (caughtType == null)
                {
                    continue;
                }

                tryResult.RemoveThrownExceptions(
                    thrownType =>
                        thrownType.InheritsFromOrEquals(
                            caughtType));

                tryResult.RemoveExternalDocumentationEvidence(
                    evidenceType =>
                        evidenceType.InheritsFromOrEquals(caughtType));
            }
        }

        /// <summary>
        /// Determines whether a catch-clause fully handles the original
        /// exception instead of rethrowing it.
        /// </summary>
        /// <param name="catchClause">
        /// The catch-clause to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for rethrow resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the original caught exception is never
        /// rethrown by the catch-clause; otherwise <see langword="false"/>.
        /// </returns>
        private static bool CatchSuppressesOriginalException(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            foreach (SyntaxNode throwNode
                     in GetCatchOwnedThrowNodes(
                         catchClause))
            {
                if (TryGetCaughtExceptionRethrow(
                        throwNode,
                        semanticModel,
                        out _))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a throw operation rethrows the exception owned
        /// by its nearest containing catch-clause.
        /// </summary>
        /// <param name="throwNode">
        /// The throw statement or throw expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for operation and symbol resolution.
        /// </param>
        /// <param name="hasPotentiallyThrowingConversion">
        /// Indicates whether an explicit conversion occurs between the
        /// caught exception reference and the throw operation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation is a parameterless
        /// rethrow, a direct caught-variable rethrow, or a stable local alias
        /// of the caught variable; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetCaughtExceptionRethrow(
            SyntaxNode throwNode,
            SemanticModel semanticModel,
            out bool hasPotentiallyThrowingConversion)
        {
            hasPotentiallyThrowingConversion =
                false;

            CatchClauseSyntax? owningCatch =
                GetOwningCatchClause(
                    throwNode);

            if (owningCatch == null ||
                semanticModel.GetOperation(
                    throwNode)
                is not IThrowOperation throwOperation)
            {
                return false;
            }

            if (throwOperation.Exception == null)
            {
                return true;
            }

            if (owningCatch.Declaration == null ||
                semanticModel.GetDeclaredSymbol(
                    owningCatch.Declaration)
                is not ILocalSymbol caughtExceptionSymbol)
            {
                return false;
            }

            HashSet<ILocalSymbol> inspectedLocals =
                new(SymbolEqualityComparer.Default);

            return TryResolveCaughtExceptionReference(
                throwOperation.Exception,
                caughtExceptionSymbol,
                owningCatch,
                throwNode,
                semanticModel,
                inspectedLocals,
                ref hasPotentiallyThrowingConversion);
        }

        /// <summary>
        /// Resolves an operation to the caught exception variable or a stable
        /// local alias of that variable.
        /// </summary>
        /// <param name="operation">
        /// The operation to inspect.
        /// </param>
        /// <param name="caughtExceptionSymbol">
        /// The catch-clause exception variable.
        /// </param>
        /// <param name="owningCatch">
        /// The catch-clause owning the throw operation.
        /// </param>
        /// <param name="throwNode">
        /// The throw operation being analyzed.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and operation resolution.
        /// </param>
        /// <param name="inspectedLocals">
        /// The local aliases already visited during the current resolution.
        /// </param>
        /// <param name="hasPotentiallyThrowingConversion">
        /// Indicates whether an explicit conversion was encountered.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the operation resolves to the caught
        /// exception; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveCaughtExceptionReference(
            IOperation operation,
            ILocalSymbol caughtExceptionSymbol,
            CatchClauseSyntax owningCatch,
            SyntaxNode throwNode,
            SemanticModel semanticModel,
            HashSet<ILocalSymbol> inspectedLocals,
            ref bool hasPotentiallyThrowingConversion)
        {
            IOperation currentOperation =
                operation;

            while (currentOperation
                   is IConversionOperation conversionOperation)
            {
                if (conversionOperation.Conversion.IsUserDefined)
                {
                    return false;
                }

                if (!conversionOperation.Conversion.IsIdentity &&
                    conversionOperation.Syntax
                        is CastExpressionSyntax)
                {
                    hasPotentiallyThrowingConversion =
                        true;
                }

                currentOperation =
                    conversionOperation.Operand;
            }

            if (currentOperation
                is not ILocalReferenceOperation localReference)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(
                    localReference.Local,
                    caughtExceptionSymbol))
            {
                return true;
            }

            ILocalSymbol localAlias =
                localReference.Local;

            if (!inspectedLocals.Add(
                    localAlias) ||
                localAlias.DeclaringSyntaxReferences.Length != 1 ||
                localAlias.DeclaringSyntaxReferences[0]
                    .GetSyntax()
                    is not VariableDeclaratorSyntax declarator ||
                declarator.Initializer == null ||
                declarator.SpanStart >= throwNode.SpanStart ||
                !owningCatch.Block.Span.Contains(
                    declarator.Span) ||
                HasCatchAliasWriteBeforeThrow(
                    localAlias,
                    owningCatch,
                    throwNode,
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

            return TryResolveCaughtExceptionReference(
                initializerOperation,
                caughtExceptionSymbol,
                owningCatch,
                throwNode,
                semanticModel,
                inspectedLocals,
                ref hasPotentiallyThrowingConversion);
        }

        /// <summary>
        /// Determines whether a local catch alias may be modified before the
        /// analyzed throw operation.
        /// </summary>
        /// <param name="localAlias">
        /// The local alias to inspect.
        /// </param>
        /// <param name="owningCatch">
        /// The owning catch-clause.
        /// </param>
        /// <param name="throwNode">
        /// The throw operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for local-symbol comparison.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a write, ref argument, or out argument
        /// may target the alias before the throw; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool HasCatchAliasWriteBeforeThrow(
            ILocalSymbol localAlias,
            CatchClauseSyntax owningCatch,
            SyntaxNode throwNode,
            SemanticModel semanticModel)
        {
            foreach (AssignmentExpressionSyntax assignment
                     in GetCatchOwnedNodes
                         <AssignmentExpressionSyntax>(owningCatch))
            {
                if (assignment.SpanStart >=
                        throwNode.SpanStart ||
                    !ContainsLocalSymbolReference(
                        assignment.Left,
                        localAlias,
                        semanticModel))
                {
                    continue;
                }

                return true;
            }

            foreach (ArgumentSyntax argument
                     in GetCatchOwnedNodes
                         <ArgumentSyntax>(owningCatch))
            {
                if (argument.SpanStart >=
                        throwNode.SpanStart ||
                    !argument.RefKindKeyword.IsKind(
                        Microsoft.CodeAnalysis.CSharp.SyntaxKind
                            .RefKeyword) &&
                    !argument.RefKindKeyword.IsKind(
                        Microsoft.CodeAnalysis.CSharp.SyntaxKind
                            .OutKeyword))
                {
                    continue;
                }

                if (ContainsLocalSymbolReference(
                        argument.Expression,
                        localAlias,
                        semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets throw statements and throw expressions owned directly by one
        /// catch-clause.
        /// </summary>
        /// <param name="catchClause">
        /// The catch-clause to inspect.
        /// </param>
        /// <returns>
        /// Throw nodes excluding nested catch-clauses, local functions,
        /// lambdas, and anonymous methods.
        /// </returns>
        private static IEnumerable<SyntaxNode> GetCatchOwnedThrowNodes(
            CatchClauseSyntax catchClause)
        {
            foreach (SyntaxNode node
                     in catchClause.Block.DescendantNodesAndSelf(
                         descendIntoChildren:
                             child =>
                                 ReferenceEquals(
                                     child,
                                     catchClause.Block) ||
                                 child is not CatchClauseSyntax &&
                                 child is not
                                     LocalFunctionStatementSyntax &&
                                 child is not
                                     AnonymousFunctionExpressionSyntax))
            {
                if (node is ThrowStatementSyntax ||
                    node is ThrowExpressionSyntax)
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// Gets matching syntax nodes owned directly by one catch-clause.
        /// </summary>
        /// <typeparam name="TNode">
        /// The syntax-node type to return.
        /// </typeparam>
        /// <param name="catchClause">
        /// The catch-clause to inspect.
        /// </param>
        /// <returns>
        /// Matching nodes excluding nested catch-clauses and nested
        /// callables.
        /// </returns>
        private static IEnumerable<TNode>
            GetCatchOwnedNodes<TNode>(
                CatchClauseSyntax catchClause)
            where TNode : SyntaxNode
        {
            return catchClause.Block
                .DescendantNodesAndSelf(
                    descendIntoChildren:
                        child =>
                            ReferenceEquals(
                                child,
                                catchClause.Block) ||
                            child is not CatchClauseSyntax &&
                            child is not
                                LocalFunctionStatementSyntax &&
                            child is not
                                AnonymousFunctionExpressionSyntax)
                .OfType<TNode>();
        }

        /// <summary>
        /// Gets the nearest catch-clause whose execution scope owns a throw
        /// operation.
        /// </summary>
        /// <param name="throwNode">
        /// The throw operation.
        /// </param>
        /// <returns>
        /// The nearest owning catch-clause, or <see langword="null"/> when
        /// the throw belongs to a nested callable.
        /// </returns>
        private static CatchClauseSyntax? GetOwningCatchClause(
            SyntaxNode throwNode)
        {
            foreach (SyntaxNode ancestor
                     in throwNode.Ancestors())
            {
                if (ancestor
                        is LocalFunctionStatementSyntax ||
                    ancestor
                        is AnonymousFunctionExpressionSyntax)
                {
                    return null;
                }

                if (ancestor
                    is CatchClauseSyntax catchClause)
                {
                    return catchClause;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the catch-clause catches all exceptions.
        /// </summary>
        /// <param name="catchClause">
        /// The catch-clause to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the catch-clause catches all exceptions;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsCatchAll(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            if (catchClause.Declaration == null)
            {
                return true;
            }

            INamedTypeSymbol? caughtType =
                GetCaughtExceptionType(
                    catchClause,
                    semanticModel);

            if (caughtType == null)
            {
                return false;
            }

            return IsSystemExceptionType(
                caughtType);
        }

        /// <summary>
        /// Resolves the caught exception type of a catch-clause.
        /// </summary>
        /// <param name="catchClause">
        /// The catch-clause to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <returns>
        /// The caught exception type if it can be resolved; otherwise
        /// <see langword="null"/>.
        /// </returns>
        private static INamedTypeSymbol? GetCaughtExceptionType(
            CatchClauseSyntax catchClause,
            SemanticModel semanticModel)
        {
            if (catchClause.Declaration?.Type == null)
            {
                return null;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    catchClause.Declaration.Type);

            return symbolInfo.Symbol
                as INamedTypeSymbol;
        }

        /// <summary>
        /// Determines whether a type is
        /// <see cref="System.Exception"/>.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the type is
        /// <see cref="System.Exception"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSystemExceptionType(
            INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString(
                       SymbolDisplayFormat.FullyQualifiedFormat) ==
                   "global::System.Exception";
        }
    }
}
