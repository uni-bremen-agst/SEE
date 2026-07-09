using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Params
{
    /// <summary>
    /// Tests for DOC390 (InvalidParamRefAttribute): a paramref tag contains an attribute other than name.
    /// </summary>
    public sealed class DOC390_InvalidParamRefAttributeTests
    {
        /// <summary>
        /// Provides member snippets where a paramref tag contains an invalid attribute.
        /// </summary>
        /// <returns>Test cases containing member code snippets.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"x\" cref=\"Other\"/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public void M(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"x\" cref=\"Other\"/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public Wrapper(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"x\" cref=\"Other\"/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public delegate void D(int x);\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"index\" cref=\"Other\"/>.</summary>\n" +
                "/// <param name=\"index\">index</param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"left\" cref=\"Other\"/>.</summary>\n" +
                "/// <param name=\"left\">left</param>\n" +
                "/// <param name=\"right\">right</param>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"value\" cref=\"Other\"/>.</summary>\n" +
                "/// <param name=\"value\">value</param>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"ghost\" cref=\"Other\"/>.</summary>\n" +
                "public void WithoutParameters() { }\n"
            };
        }

        /// <summary>
        /// Ensures that an invalid attribute on a paramref tag is detected for each supported declaration kind.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void InvalidParamRefAttribute_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSingleSmell(findings, XmlDocSmells.InvalidParamRefAttribute.ID);

            Finding finding = findings.Single(item => item.Smell.ID == XmlDocSmells.InvalidParamRefAttribute.ID);

            string expectedMessage = string.Format(finding.Smell.MessageTemplate, "cref");
            Assert.Equal(expectedMessage, finding.Message);
            Assert.Equal("paramref", finding.TagName);
            Assert.Equal("ParamRefTag", finding.Context.SubjectKind);
            Assert.Equal("cref", finding.Context.TargetName);
        }
    }
}
