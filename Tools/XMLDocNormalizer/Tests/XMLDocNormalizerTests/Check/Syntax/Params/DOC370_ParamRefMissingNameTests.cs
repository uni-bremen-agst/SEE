using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Params
{
    /// <summary>
    /// Tests for DOC370 (ParamRefMissingName): a paramref tag exists without the required name attribute.
    /// </summary>
    public sealed class DOC370_ParamRefMissingNameTests
    {
        /// <summary>
        /// Provides member snippets where a paramref tag is missing the required name attribute.
        /// </summary>
        /// <returns>Test cases containing member code snippets.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Uses <paramref/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public void M(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public Wrapper(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public delegate void D(int x);\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref/>.</summary>\n" +
                "/// <param name=\"index\">index</param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref/>.</summary>\n" +
                "/// <param name=\"left\">left</param>\n" +
                "/// <param name=\"right\">right</param>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref/>.</summary>\n" +
                "/// <param name=\"value\">value</param>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref/>.</summary>\n" +
                "public void WithoutParameters() { }\n"
            };
        }

        /// <summary>
        /// Ensures that a paramref tag without a name attribute is detected for each supported declaration kind.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void ParamRefMissingName_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ParamRefMissingName.ID);

            Finding finding = findings.Single();

            Assert.Equal("paramref", finding.TagName);
            Assert.Equal("<paramref> tag is missing required 'name' attribute.", finding.Message);
            Assert.Equal("ParamRefTag", finding.Context.SubjectKind);
            Assert.Null(finding.Context.TargetName);
        }
    }
}
