using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains creation and mapping of exception-flow call contexts.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Creates the initial call context for a top-level member analysis.
        /// </summary>
        /// <param name="member">The member whose body is analyzed.</param>
        /// <param name="semanticModel">The semantic model used to resolve the member symbol.</param>
        /// <returns>A call context without assumed non-null parameter facts.</returns>
        private static ExceptionFlowCallContext CreateRootCallContext(
            MemberDeclarationSyntax member,
            SemanticModel semanticModel)
        {
            ISymbol? memberSymbol =
                semanticModel.GetDeclaredSymbol(member);

            return new ExceptionFlowCallContext(memberSymbol);
        }

        /// <summary>
        /// Creates the call context for an invoked method or constructor.
        /// </summary>
        /// <param name="methodSymbol">The invoked method or constructor.</param>
        /// <param name="arguments">The arguments supplied at the call site.</param>
        /// <param name="semanticModel">
        /// The semantic model used for expression and constant analysis.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// The call context containing the value facts proven for the target parameters.
        /// </returns>
        private static ExceptionFlowCallContext CreateCallContext(
            IMethodSymbol methodSymbol,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext)
        {
            Dictionary<int, ExceptionFlowValueFacts> knownParameterFacts = new();
            HashSet<int> suppliedParameterIndexes = new();

            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentSyntax argument = arguments[i];

                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        i,
                        methodSymbol);

                if (parameterIndex < 0 ||
                    parameterIndex >= methodSymbol.Parameters.Length)
                {
                    continue;
                }

                suppliedParameterIndexes.Add(parameterIndex);

                if (argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    continue;
                }

                ExceptionFlowValueFacts facts =
                    GetExpressionValueFacts(
                        argument.Expression,
                        semanticModel,
                        callerContext);

                if (facts != ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[parameterIndex] = facts;
                }
            }

            foreach (IParameterSymbol parameterSymbol in methodSymbol.Parameters)
            {
                if (suppliedParameterIndexes.Contains(parameterSymbol.Ordinal))
                {
                    continue;
                }

                ExceptionFlowValueFacts facts =
                    ExceptionFlowValueFacts.None;

                if (parameterSymbol.IsParams)
                {
                    facts = ExceptionFlowValueFacts.NonNull;
                }
                else if (parameterSymbol.HasExplicitDefaultValue)
                {
                    facts = GetConstantValueFacts(
                        parameterSymbol.ExplicitDefaultValue);
                }

                if (facts != ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[parameterSymbol.Ordinal] = facts;
                }
            }

            return new ExceptionFlowCallContext(
                methodSymbol,
                knownParameterFacts);
        }

        /// <summary>
        /// Gets the effective target parameter index for an argument, taking named
        /// arguments into account.
        /// </summary>
        /// <param name="argument">The argument to inspect.</param>
        /// <param name="fallbackIndex">The positional fallback index.</param>
        /// <param name="methodSymbol">The target method symbol.</param>
        /// <returns>
        /// The resolved parameter index, or the fallback index if no named match exists.
        /// </returns>
        private static int GetParameterIndexForArgument(
            ArgumentSyntax argument,
            int fallbackIndex,
            IMethodSymbol methodSymbol)
        {
            if (argument.NameColon == null)
            {
                return fallbackIndex;
            }

            string name =
                argument.NameColon.Name.Identifier.ValueText;

            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                if (string.Equals(
                        methodSymbol.Parameters[i].Name,
                        name,
                        StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return fallbackIndex;
        }
    }
}
