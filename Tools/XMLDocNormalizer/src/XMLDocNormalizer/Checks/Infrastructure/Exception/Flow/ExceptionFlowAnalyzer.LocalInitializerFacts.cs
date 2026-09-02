using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains validity checks for facts derived from local-variable
    /// declaration initializers.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether the value established by a local declaration
        /// initializer is still current at a later use site.
        /// </summary>
        /// <param name="expression">
        /// The later use of the local variable.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol whose initializer fact is being considered.
        /// </param>
        /// <param name="variableDeclarator">
        /// The local declaration containing the initializer.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for write analysis.
        /// </param>
        /// <returns>
        /// <see langword="false"/> when a write to the local is proven between
        /// its declaration and the use site; otherwise
        /// <see langword="true"/>.
        /// </returns>
        private static bool IsLocalInitializerStillCurrent(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            VariableDeclaratorSyntax variableDeclarator,
            SemanticModel semanticModel)
        {
            if (variableDeclarator.Parent?.Parent
                    is not LocalDeclarationStatementSyntax
                        declarationStatement)
            {
                return true;
            }

            StatementSyntax? currentStatement =
                expression
                    .AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (currentStatement == null)
            {
                return true;
            }

            while (currentStatement.Parent
                   is BlockSyntax containingBlock)
            {
                int currentIndex =
                    containingBlock.Statements.IndexOf(
                        currentStatement);

                if (currentIndex < 0)
                {
                    return true;
                }

                for (int index = currentIndex - 1;
                     index >= 0;
                     index--)
                {
                    StatementSyntax precedingStatement =
                        containingBlock.Statements[index];

                    if (ReferenceEquals(
                            precedingStatement,
                            declarationStatement))
                    {
                        return true;
                    }

                    if (StatementWritesSymbol(
                            precedingStatement,
                            localSymbol,
                            semanticModel))
                    {
                        return false;
                    }
                }

                currentStatement =
                    GetSafeContainingStatement(
                        containingBlock,
                        localSymbol,
                        semanticModel);

                if (currentStatement == null)
                {
                    /*
                     * No supported path back to the declaration was found.
                     * Do not destroy an existing initializer fact merely
                     * because this syntactic shape is not modeled here.
                     */
                    return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the declaration initializer that still determines a local variable's
        /// value at a later use within the same straight-line block.
        /// </summary>
        /// <param name="useExpression">
        /// The later use of the local variable.
        /// </param>
        /// <param name="localSymbol">
        /// The local variable whose initializer should be inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for write analysis.
        /// </param>
        /// <param name="initializer">
        /// The declaration initializer when it is proven to remain current.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the declaration initializer still determines
        /// the local value at the use site; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetStraightLineCurrentLocalInitializerExpression(
            ExpressionSyntax useExpression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            out ExpressionSyntax? initializer)
        {
            initializer = null;

            if (localSymbol.DeclaringSyntaxReferences.Length != 1
                || localSymbol.DeclaringSyntaxReferences[0].GetSyntax()
                    is not VariableDeclaratorSyntax declarator
                || declarator.Initializer == null
                || declarator.Parent?.Parent
                    is not LocalDeclarationStatementSyntax declarationStatement
                || declarationStatement.Parent is not BlockSyntax declarationBlock)
            {
                return false;
            }

            StatementSyntax? useStatement =
                useExpression
                    .AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (useStatement == null
                || useStatement.Parent is not BlockSyntax useBlock
                || !ReferenceEquals(
                    declarationBlock,
                    useBlock))
            {
                return false;
            }

            int declarationIndex =
                declarationBlock.Statements.IndexOf(
                    declarationStatement);

            int useIndex =
                declarationBlock.Statements.IndexOf(
                    useStatement);

            if (declarationIndex < 0
                || useIndex <= declarationIndex)
            {
                return false;
            }

            for (int index = declarationIndex + 1;
                 index < useIndex;
                 index++)
            {
                StatementSyntax interveningStatement =
                    declarationBlock.Statements[index];

                ExceptionFlowDataFlowFacts dataFlow =
                    GetDataFlowFacts(
                        interveningStatement,
                        semanticModel);

                if (!dataFlow.Succeeded
                    || dataFlow.WrittenInside.Any(
                        writtenSymbol =>
                            SymbolEqualityComparer.Default.Equals(
                                writtenSymbol,
                                localSymbol)))
                {
                    return false;
                }
            }

            initializer =
                declarator.Initializer.Value;

            return true;
        }
    }
}
