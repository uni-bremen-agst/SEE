using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.WellFormed
{
    /// <summary>
    /// Rule tests for DOC440: typeparamref should be an empty element.
    /// </summary>
    public sealed class DOC440_TypeParamRefNotEmptyTests
    {
        /// <summary>
        /// Provides snippets where a typeparamref tag contains content.
        /// </summary>
        /// <returns>Test cases containing code snippets and full-source flags.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\">T</typeparamref>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T>() { }\n",
                false
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\">T</typeparamref>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public delegate void D<T>();\n",
                false
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\">T</typeparamref>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public sealed class C<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\">T</typeparamref>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public struct S<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\">T</typeparamref>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public interface I<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\">T</typeparamref>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record R<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"T\">T</typeparamref>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record struct RS<T>\n" +
                "{\n" +
                "}\n",
                true
            };
        }

        /// <summary>
        /// Ensures that a non-empty typeparamref tag is detected for each supported declaration kind.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="isFullSource">True if the code is a full source text; otherwise false.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void Malformed_TypeParamRef_IsDetected(string code, bool isFullSource)
        {
            List<Finding> findings = Run(code, isFullSource);

            Finding finding = Assert.Single(findings);
            Assert.Equal("typeparamref", finding.TagName);
            Assert.Equal(XmlDocSmells.TypeParamRefNotEmpty.ID, finding.Smell.ID);
        }

        /// <summary>
        /// Runs the well-formed detector on the given code snippet.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="isFullSource">Whether the snippet is a full source text.</param>
        /// <returns>The produced list of findings.</returns>
        private static List<Finding> Run(string code, bool isFullSource)
        {
            if (isFullSource)
            {
                return CheckAssert.FindWellFormedFindingsForSource(code);
            }

            return CheckAssert.FindWellFormedFindingsForMember(code);
        }
    }
}
