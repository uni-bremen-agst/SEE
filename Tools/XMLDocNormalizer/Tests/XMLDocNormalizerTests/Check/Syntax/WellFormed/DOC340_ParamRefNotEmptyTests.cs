using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.WellFormed
{
    /// <summary>
    /// Rule tests for DOC340: paramref should be an empty element.
    /// </summary>
    public sealed class DOC340_ParamRefNotEmptyTests
    {
        /// <summary>
        /// Provides member snippets where a paramref tag contains content.
        /// </summary>
        /// <returns>Test cases containing member code snippets.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"x\">x</paramref>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public void M(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"x\">x</paramref>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public C(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"x\">x</paramref>.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public delegate void D(int x);\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"index\">index</paramref>.</summary>\n" +
                "/// <param name=\"index\">index</param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"left\">left</paramref>.</summary>\n" +
                "/// <param name=\"left\">left</param>\n" +
                "/// <param name=\"right\">right</param>\n" +
                "public static C operator +(C left, C right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n"
            };

            yield return new object[]
            {
                "/// <summary>Uses <paramref name=\"value\">value</paramref>.</summary>\n" +
                "/// <param name=\"value\">value</param>\n" +
                "public static explicit operator int(C value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n"
            };
        }

        /// <summary>
        /// Ensures that a non-empty paramref tag is detected for each supported declaration kind.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void Malformed_Paramref_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(memberCode);

            Finding finding = Assert.Single(findings);
            Assert.Equal("paramref", finding.TagName);
            Assert.Equal(XmlDocSmells.ParamRefNotEmpty.ID, finding.Smell.ID);
        }

        /// <summary>
        /// Ensures that multiple non-empty paramref tags are detected individually.
        /// </summary>
        [Fact]
        public void Malformed_Multiple_Paramref_IsDetected()
        {
            string source =
                "/// <summary>Test.</summary>\n" +
                "/// <returns><paramref name=\"x\"> equals 1.\n" +
                "/// and <paramref name=\"y\"> equals 0.</returns>\n" +
                "int M(int x) { return x; }\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForMember(source);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.ParamRefNotEmpty.ID, 2);
        }
    }
}
