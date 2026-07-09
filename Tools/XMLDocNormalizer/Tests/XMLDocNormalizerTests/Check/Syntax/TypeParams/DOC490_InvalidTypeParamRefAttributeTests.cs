using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.TypeParams
{
    /// <summary>
    /// Tests for DOC490 (InvalidTypeParamRefAttribute): a typeparamref tag contains an attribute other than name.
    /// </summary>
    public sealed class DOC490_InvalidTypeParamRefAttributeTests
    {
        /// <summary>
        /// Provides snippets where a typeparamref tag contains an invalid attribute.
        /// </summary>
        /// <returns>Test cases containing code snippets and full-source flags.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\" cref=\"Other\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T>() { }\n",
                false
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\" cref=\"Other\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public delegate void D<T>();\n",
                false
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\" cref=\"Other\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public sealed class C<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\" cref=\"Other\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public struct S<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\" cref=\"Other\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public interface I<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\" cref=\"Other\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record R<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\" cref=\"Other\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record struct RS<T>\n" +
                "{\n" +
                "}\n",
                true
            };
        }

        /// <summary>
        /// Ensures that an invalid attribute on a typeparamref tag is detected for each supported declaration kind.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="isFullSource">True if the code is a full source text; otherwise false.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void InvalidTypeParamRefAttribute_IsDetected(string code, bool isFullSource)
        {
            List<Finding> findings = Run(code, isFullSource);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.InvalidTypeParamRefAttribute.ID);

            Finding finding = findings.Single();

            string expectedMessage = string.Format(finding.Smell.MessageTemplate, "cref");
            Assert.Equal(expectedMessage, finding.Message);
            Assert.Equal("typeparamref", finding.TagName);
            Assert.Equal("TypeParamRefTag", finding.Context.SubjectKind);
            Assert.Equal("cref", finding.Context.TargetName);
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
