using System.Collections.Generic;
using System.Linq;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC420 (EmptyTypeParamDescription): a <typeparam> tag exists but its description is empty.
    /// </summary>
    public sealed class DOC420_EmptyTypeParamDescriptionTests
    {
        /// <summary>
        /// Provides code samples where a <typeparam> tag exists but contains no meaningful content.
        /// Each case is designed to produce exactly one DOC420 finding and no additional typeparam smells.
        /// </summary>
        /// <returns>Test cases consisting of code, the affected type parameter name, and a full-source flag.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Method: T empty.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\"></typeparam>\n" +
                "public void M<T>() { }\n",
                "T",
                false
            };

            // Delegate: T whitespace.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\"> \n" +
                "/// </typeparam>\n" +
                "public delegate void D<T>();\n",
                "T",
                false
            };

            // Generic class: T empty.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\"></typeparam>\n" +
                "public sealed class C<T>\n" +
                "{\n" +
                "}\n",
                "T",
                true
            };

            // Generic interface: T empty.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\"></typeparam>\n" +
                "public interface I<T>\n" +
                "{\n" +
                "}\n",
                "T",
                true
            };
        }

        /// <summary>
        /// Ensures that empty <typeparam> descriptions are reported as DOC420 only,
        /// and that the reported message is formatted with the expected type parameter name.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="typeParamName">The type parameter name expected in the finding message.</param>
        /// <param name="isFullSource">True if <paramref name="code"/> is a full source text; otherwise false.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void EmptyTypeParamDescription_IsDetected(string code, string typeParamName, bool isFullSource)
        {
            List<Finding> findings = Run(code, isFullSource);

            FindingAsserts.HasExactlySmells(findings, "DOC420");

            Finding finding = findings.Single();

            string expectedMessage = string.Format(finding.Smell.MessageTemplate, typeParamName);
            Assert.Equal(expectedMessage, finding.Message);
            Assert.Equal("typeparam", finding.TagName);
        }

        /// <summary>
        /// Ensures that a <typeparam> containing nested XML elements is treated as non-empty (no DOC420).
        /// </summary>
        [Fact]
        public void TypeParamDescription_WithSee_IsNotEmpty()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\"><see cref=\"System.Int32\"/></typeparam>\n" +
                "public void M<T>() { }\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Runs the type parameter detector on the given code snippet.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="isFullSource">Whether the snippet is a full source text.</param>
        /// <returns>The produced list of findings.</returns>
        private static List<Finding> Run(string code, bool isFullSource)
        {
            if (isFullSource)
            {
                return CheckAssert.FindTypeParamFindingsForSource(code);
            }

            return CheckAssert.FindTypeParamFindingsForMember(code);
        }
    }
}
