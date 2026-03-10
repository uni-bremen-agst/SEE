using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution
{
    /// <summary>
    /// Provides the registered XML documentation detectors used by the tool runner.
    /// </summary>
    /// <remarks>
    /// The catalog contains only detectors with a common signature.
    /// Detectors that require additional parameters remain explicit special cases
    /// in the runner.
    /// </remarks>
    internal static class XmlDocDetectorCatalog
    {
        /// <summary>
        /// Represents a syntax-based detector function.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <returns>A list of findings.</returns>
        internal delegate List<Finding> SyntaxDetector(SyntaxTree tree, string filePath);

        /// <summary>
        /// Represents a semantic-based detector function.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns>A list of findings.</returns>
        internal delegate List<Finding> SemanticDetector(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel);

        /// <summary>
        /// Gets all registered syntax-based detectors with a common signature.
        /// </summary>
        public static IReadOnlyList<SyntaxDetector> SyntaxDetectors { get; } =
        [
            XmlDocWellFormedDetector.FindMalformedTags,
            XmlDocParamDetector.FindParamSmells,
            XmlDocTypeParamDetector.FindTypeParamSmells,
            XmlDocReturnsDetector.FindReturnsSmells,
            XmlDocExceptionDetector.FindExceptionSmells,
            XmlDocMemberTagDetector.FindInvalidTags,
            XmlDocInheritdocDetector.FindInheritdocSmells,
            XmlDocValueDetector.FindValueSmells
        ];

        /// <summary>
        /// Gets all registered semantic-based detectors with a common signature.
        /// </summary>
        /// <remarks>
        /// This list is intentionally empty for now and can be extended once
        /// semantic XML documentation detectors are introduced.
        /// </remarks>
        public static IReadOnlyList<SemanticDetector> SemanticDetectors { get; } =
        [
            XmlDocInheritdocSemanticDetector.FindInheritdocSmells,
            XmlDocExceptionSemanticDetector.FindExceptionSmells,
        ];
    }
}