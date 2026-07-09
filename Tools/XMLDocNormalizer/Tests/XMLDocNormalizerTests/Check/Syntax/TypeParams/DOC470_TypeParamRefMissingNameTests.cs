using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.TypeParams
{
    /// <summary>
    /// Tests for DOC470 (TypeParamRefMissingName): a typeparamref tag exists without the required name attribute.
    /// </summary>
    public sealed class DOC470_TypeParamRefMissingNameTests
    {
        /// <summary>
        /// Provides snippets where a typeparamref tag is missing the required name attribute.
        /// </summary>
        /// <returns>Test cases containing code snippets and full-source flags.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Uses <typeparamref/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T>() { }\n",
                false
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public delegate void D<T>();\n",
                false
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public sealed class C<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public struct S<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public interface I<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record R<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record struct RS<T>\n" +
                "{\n" +
                "}\n",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref/>.</summary>\n" +
                "public void WithoutTypeParameters() { }\n",
                false
            };
        }

        /// <summary>
        /// Ensures that a typeparamref tag without a name attribute is detected for each supported declaration kind.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="isFullSource">True if the code is a full source text; otherwise false.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void TypeParamRefMissingName_IsDetected(string code, bool isFullSource)
        {
            List<Finding> findings = Run(code, isFullSource);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.TypeParamRefMissingName.ID);

            Finding finding = findings.Single();

            Assert.Equal("typeparamref", finding.TagName);
            Assert.Equal("<typeparamref> tag is missing required 'name' attribute.", finding.Message);
            Assert.Equal("TypeParamRefTag", finding.Context.SubjectKind);
            Assert.Null(finding.Context.TargetName);
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
