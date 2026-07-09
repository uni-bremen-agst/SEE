using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.WellFormed
{
    /// <summary>
    /// Rule tests for DOC300: param-tag missing required name attribute.
    /// </summary>
    public sealed class DOC300_ParamMissingNameTests
    {
        /// <summary>
        /// Provides member snippets where a param tag is missing the required name attribute.
        /// </summary>
        /// <returns>Test cases containing member code snippets.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param>Missing name.</param>\n" +
                "public void M(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param>Missing name.</param>\n" +
                "public C(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param>Missing name.</param>\n" +
                "public delegate void D(int x);\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param>Missing name.</param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param>Missing name.</param>\n" +
                "/// <param name=\"right\">right</param>\n" +
                "public static C operator +(C left, C right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param>Missing name.</param>\n" +
                "public static explicit operator int(C value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n"
            };
        }

        /// <summary>
        /// Ensures that a param tag without a name attribute is detected for each supported declaration kind.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void Param_WithoutName_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(memberCode);

            Finding finding = Assert.Single(findings);
            Assert.Equal("param", finding.TagName);
            Assert.Equal(XmlDocSmells.ParamMissingName.ID, finding.Smell.ID);
            Assert.Equal(Severity.Error, finding.Smell.Severity);
        }
    }
}
