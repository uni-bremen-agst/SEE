using System.Collections.Generic;
using System.Linq;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC430 (UnknownTypeParamTag): a <typeparam> tag references a type parameter name that does not exist.
    /// </summary>
    public sealed class DOC430_UnknownTypeParamTagTests
    {
        /// <summary>
        /// Provides code samples where a <typeparam> exists for a non-existent type parameter.
        /// Each case is designed to produce exactly one DOC430 finding and no additional typeparam smells.
        /// </summary>
        /// <returns>Test cases consisting of code, the unknown type parameter name, and a full-source flag.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Method: Ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "/// <typeparam name=\"Ghost\">ghost</typeparam>\n" +
                "public void M<T>() { }\n",
                "Ghost",
                false
            };

            // Delegate: Ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "/// <typeparam name=\"Ghost\">ghost</typeparam>\n" +
                "public delegate void D<T>();\n",
                "Ghost",
                false
            };

            // Generic class: Ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "/// <typeparam name=\"Ghost\">ghost</typeparam>\n" +
                "public sealed class C<T>\n" +
                "{\n" +
                "}\n",
                "Ghost",
                true
            };

            // Generic interface: Ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "/// <typeparam name=\"Ghost\">ghost</typeparam>\n" +
                "public interface I<T>\n" +
                "{\n" +
                "}\n",
                "Ghost",
                true
            };
        }

        /// <summary>
        /// Ensures that unknown <typeparam> references are reported as DOC430 only,
        /// and that the reported message is formatted with the expected unknown type parameter name.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="unknownTypeParamName">The unknown type parameter name expected in the finding message.</param>
        /// <param name="isFullSource">True if <paramref name="code"/> is a full source text; otherwise false.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void UnknownTypeParamTag_IsDetected(string code, string unknownTypeParamName, bool isFullSource)
        {
            List<Finding> findings = Run(code, isFullSource);

            FindingAsserts.HasExactlySmells(findings, "DOC430");

            Finding finding = findings.Single();

            string expectedMessage = string.Format(finding.Smell.MessageTemplate, unknownTypeParamName);
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
