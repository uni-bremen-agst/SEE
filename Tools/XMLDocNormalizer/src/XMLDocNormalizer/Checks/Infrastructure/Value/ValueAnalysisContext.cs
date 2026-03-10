using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Value
{
    /// <summary>
    /// Carries all precomputed information required to analyze value-related smells for one member.
    /// </summary>
    internal sealed class ValueAnalysisContext
    {
        /// <summary>
        /// Gets the analyzed member.
        /// </summary>
        public required MemberDeclarationSyntax Member { get; init; }

        /// <summary>
        /// Gets the XML documentation comment of the member.
        /// </summary>
        public required DocumentationCommentTriviaSyntax Doc { get; init; }

        /// <summary>
        /// Gets all value tags found in the XML documentation comment.
        /// </summary>
        public required List<XmlElementSyntax> ValueTags { get; init; }

        /// <summary>
        /// Gets the classified value-target kind of the member.
        /// </summary>
        public required ValueTargetKind TargetKind { get; init; }

        /// <summary>
        /// Gets the member name used for smell message formatting where applicable.
        /// </summary>
        public string? MemberName { get; init; }
    }
}