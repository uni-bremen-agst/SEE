using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.TypeParams
{
    /// <summary>
    /// Tests for DOC480 (UnknownTypeParamRef): a typeparamref tag references a type parameter name that does not exist.
    /// </summary>
    public sealed class DOC480_UnknownTypeParamRefTests
    {
        /// <summary>
        /// Provides snippets where a typeparamref tag references a non-existent type parameter.
        /// </summary>
        /// <returns>Test cases containing code snippets, unknown type parameter names, and full-source flags.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"Ghost\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public void M<T>() { }\n",
                "Ghost",
                false
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"Ghost\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public delegate void D<T>();\n",
                "Ghost",
                false
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"Ghost\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public sealed class C<T>\n" +
                "{\n" +
                "}\n",
                "Ghost",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"Ghost\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public struct S<T>\n" +
                "{\n" +
                "}\n",
                "Ghost",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"Ghost\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public interface I<T>\n" +
                "{\n" +
                "}\n",
                "Ghost",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"Ghost\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record R<T>\n" +
                "{\n" +
                "}\n",
                "Ghost",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"Ghost\"/>.</summary>\n" +
                "/// <typeparam name=\"T\">T</typeparam>\n" +
                "public record struct RS<T>\n" +
                "{\n" +
                "}\n",
                "Ghost",
                true
            };

            yield return new object[]
            {
                "/// <summary>Uses <typeparamref name=\"Ghost\"/>.</summary>\n" +
                "public void WithoutTypeParameters() { }\n",
                "Ghost",
                false
            };
        }

        /// <summary>
        /// Ensures that an unknown typeparamref reference is reported with the expected message and context.
        /// </summary>
        /// <param name="code">The code snippet to analyze.</param>
        /// <param name="unknownTypeParamName">The unknown type parameter name expected in the finding.</param>
        /// <param name="isFullSource">True if the code is a full source text; otherwise false.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void UnknownTypeParamRef_IsDetected(string code, string unknownTypeParamName, bool isFullSource)
        {
            List<Finding> findings = Run(code, isFullSource);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.UnknownTypeParamRef.ID);

            Finding finding = findings.Single();

            string expectedMessage = string.Format(finding.Smell.MessageTemplate, unknownTypeParamName);
            Assert.Equal(expectedMessage, finding.Message);
            Assert.Equal("typeparamref", finding.TagName);
            Assert.Equal("TypeParamRefTag", finding.Context.SubjectKind);
            Assert.Equal(unknownTypeParamName, finding.Context.TargetName);
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
