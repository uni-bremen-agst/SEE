using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Params
{
    /// <summary>
    /// Tests for DOC380 (UnknownParamRef): a paramref tag references a parameter name that does not exist.
    /// </summary>
    public sealed class DOC380_UnknownParamRefTests
    {
        /// <summary>
        /// Provides member snippets where a paramref tag references a non-existent parameter.
        /// </summary>
        /// <returns>Test cases consisting of member code and the unknown parameter name.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"ghost\"/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public void M(int x) { }\n",
                "ghost"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"ghost\"/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public Wrapper(int x) { }\n",
                "ghost"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"ghost\"/>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public delegate void D(int x);\n",
                "ghost"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"ghost\"/>.</summary>\n" +
                "/// <param name=\"index\">index</param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n",
                "ghost"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"ghost\"/>.</summary>\n" +
                "/// <param name=\"left\">left</param>\n" +
                "/// <param name=\"right\">right</param>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n",
                "ghost"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"ghost\"/>.</summary>\n" +
                "/// <param name=\"value\">value</param>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n",
                "ghost"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"ghost\"/>.</summary>\n" +
                "public void WithoutParameters() { }\n",
                "ghost"
            };
        }

        /// <summary>
        /// Ensures that an unknown paramref reference is reported with the expected message and context.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        /// <param name="unknownParamName">The unknown parameter name expected in the finding.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void UnknownParamRef_IsDetected(string memberCode, string unknownParamName)
        {
            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.UnknownParamRef.ID);

            Finding finding = findings.Single();

            string expectedMessage = string.Format(finding.Smell.MessageTemplate, unknownParamName);
            Assert.Equal(expectedMessage, finding.Message);
            Assert.Equal("paramref", finding.TagName);
            Assert.Equal("ParamRefTag", finding.Context.SubjectKind);
            Assert.Equal(unknownParamName, finding.Context.TargetName);
        }
    }
}
