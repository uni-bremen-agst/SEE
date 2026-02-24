using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides helper methods for resolving anchor positions for member declarations.
    /// </summary>
    internal static class MemberAnchorResolver
    {
        /// <summary>
        /// Gets an anchor position for a member declaration.
        /// </summary>
        /// <param name="member">
        /// The member declaration to compute an anchor for.
        /// </param>
        /// <returns>
        /// A span start position that can be used as a stable anchor for findings.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="member"/> is null.
        /// </exception>
        public static int GetAnchorPosition(MemberDeclarationSyntax member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            int fallback = member.GetFirstToken().SpanStart;

            switch (member)
            {
                case BaseTypeDeclarationSyntax typeDecl:
                    return typeDecl.Identifier.SpanStart;

                case EnumMemberDeclarationSyntax enumMemberDecl:
                    return enumMemberDecl.Identifier.SpanStart;

                case DelegateDeclarationSyntax delegateDecl:
                    return delegateDecl.Identifier.SpanStart;

                case MethodDeclarationSyntax methodDecl:
                    return methodDecl.Identifier.SpanStart;

                case ConstructorDeclarationSyntax ctorDecl:
                    return ctorDecl.Identifier.SpanStart;

                case DestructorDeclarationSyntax dtorDecl:
                    return dtorDecl.Identifier.SpanStart;

                case PropertyDeclarationSyntax propDecl:
                    return propDecl.Identifier.SpanStart;

                case IndexerDeclarationSyntax indexerDecl:
                    return indexerDecl.ThisKeyword.SpanStart;

                case EventDeclarationSyntax eventDecl:
                    return eventDecl.Identifier.SpanStart;

                case OperatorDeclarationSyntax operatorDecl:
                    return operatorDecl.OperatorToken.SpanStart;

                case ConversionOperatorDeclarationSyntax conversionDecl:
                    return conversionDecl.Type.SpanStart;

                case EventFieldDeclarationSyntax eventFieldDecl:
                    VariableDeclaratorSyntax? firstEventVariable =
                        eventFieldDecl.Declaration.Variables.FirstOrDefault();

                    if (firstEventVariable != null)
                    {
                        return firstEventVariable.Identifier.SpanStart;
                    }

                    return fallback;

                case FieldDeclarationSyntax fieldDecl:
                    VariableDeclaratorSyntax? firstVariable =
                        fieldDecl.Declaration.Variables.FirstOrDefault();

                    if (firstVariable != null)
                    {
                        return firstVariable.Identifier.SpanStart;
                    }

                    return fallback;

                default:
                    return fallback;
            }
        }
    }
}