using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.TypeParams
{
    /// <summary>
    /// Tests for DOC410 (MissingTypeParamTag): a generic type parameter exists but has no corresponding typeparam tag.
    /// </summary>
    public sealed class DOC410_MissingTypeParamTagTests
    {
        /// <summary>
        /// Provides code samples where a declared type parameter is missing a corresponding typeparam tag.
        /// Each case is designed to produce exactly one DOC410 finding and no additional typeparam smells.
        /// </summary>
        /// <returns>
        /// Test cases consisting of the code snippet, the expected missing type parameter name,
        /// and a value indicating whether the snippet is a complete source text.
        /// </returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T, U>() { }\n",
                "U",
                false
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public delegate void D<T, U>();\n",
                "U",
                false
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public sealed class C<T, U>\n" +
                "{\n" +
                "}\n",
                "U",
                true
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public struct S<T, U>\n" +
                "{\n" +
                "}\n",
                "U",
                true
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public interface I<T, U>\n" +
                "{\n" +
                "}\n",
                "U",
                true
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record R<T, U>\n" +
                "{\n" +
                "}\n",
                "U",
                true
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record struct RS<T, U>\n" +
                "{\n" +
                "}\n",
                "U",
                true
            };
        }

        /// <summary>
        /// Ensures that missing typeparam documentation is reported as DOC410 only,
        /// and that the reported message is formatted with the expected missing type parameter name.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="missingTypeParamName">The missing type parameter name expected in the finding message.</param>
        /// <param name="isFullSource">True if the code is a full source text; otherwise false.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void MissingTypeParamTag_IsDetected(string code, string missingTypeParamName, bool isFullSource)
        {
            List<Finding> findings = Run(code, isFullSource);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingTypeParamTag.ID);

            Finding finding = findings.Single();

            string expectedMessage = string.Format(finding.Smell.MessageTemplate, missingTypeParamName);
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

        /// <summary>
        /// Ensures that inheritdoc suppresses missing type parameter documentation.
        /// </summary>
        [Fact]
        public void Inheritdoc_DoesNotTriggerMissingTypeParamTag()
        {
            string memberCode =
                "/// <inheritdoc/>\n" +
                "public void M<T, U>()\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForMember(memberCode);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingTypeParamTag.ID);
        }
    }
}
