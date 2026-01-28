using System.Collections.Generic;
using System.Linq;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC450 (DuplicateTypeParamTag): multiple <typeparam> tags exist for the same type parameter name.
    /// </summary>
    public sealed class DOC450_DuplicateTypeParamTagTests
    {
        /// <summary>
        /// Provides code samples where at least one <typeparam> name is duplicated.
        /// Each case is designed to produce exactly one DOC450 finding and no additional typeparam smells.
        /// </summary>
        /// <returns>Test cases consisting of code, the duplicated type parameter name, and a full-source flag.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Method: T duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">first</typeparam>\n" +
                "/// <typeparam name=\"T\">second</typeparam>\n" +
                "public void M<T>() { }\n",
                "T",
                false
            };

            // Delegate: T duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">first</typeparam>\n" +
                "/// <typeparam name=\"T\">second</typeparam>\n" +
                "public delegate void D<T>();\n",
                "T",
                false
            };

            // Generic class: T duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">first</typeparam>\n" +
                "/// <typeparam name=\"T\">second</typeparam>\n" +
                "public sealed class C<T>\n" +
                "{\n" +
                "}\n",
                "T",
                true
            };

            // Generic interface: T duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">first</typeparam>\n" +
                "/// <typeparam name=\"T\">second</typeparam>\n" +
                "public interface I<T>\n" +
                "{\n" +
                "}\n",
                "T",
                true
            };
        }

        /// <summary>
        /// Ensures that duplicate <typeparam> tags are reported as DOC450 only,
        /// and that the reported message is formatted with the expected duplicated type parameter name.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="duplicatedTypeParamName">The duplicated type parameter name expected in the finding message.</param>
        /// <param name="isFullSource">True if <paramref name="code"/> is a full source text; otherwise false.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void DuplicateTypeParamTag_IsDetected(string code, string duplicatedTypeParamName, bool isFullSource)
        {
            List<Finding> findings = Run(code, isFullSource);

            FindingAsserts.HasExactlySmells(findings, "DOC450");

            Finding finding = findings.Single();

            string expectedMessage = string.Format(finding.Smell.MessageTemplate, duplicatedTypeParamName);
            Assert.Equal(expectedMessage, finding.Message);
            Assert.Equal("typeparam", finding.TagName);
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
