using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Params
{
    /// <summary>
    /// Tests for DOC301 (MissingParamTag): a parameter exists but no corresponding <param name="..."> tag exists.
    /// </summary>
    public sealed class DOC301_MissingParamTagTests
    {
        /// <summary>
        /// Provides member snippets where at least one parameter is missing a corresponding <param> tag.
        /// Each case is designed to produce exactly one DOC301 finding and no additional param smells.
        /// </summary>
        /// <returns>Test cases consisting of member code and the missing parameter name.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Method: y is missing.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public void M(int x, int y) { }\n",
                "y"
            };

            // Constructor: y is missing.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public Wrapper(int x, int y) { }\n",
                "y"
            };

            // Delegate: y is missing.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "public delegate void D(int x, int y);\n",
                "y"
            };

            // Indexer: index is missing.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n",
                "index"
            };

            // Operator: right is missing.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"left\">left</param>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n",
                "right"
            };

            // Conversion operator: value is missing.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n",
                "value"
            };
        }

        /// <summary>
        /// Ensures that missing <param> documentation is reported as DOC301, that no other param smells are produced,
        /// and that the reported message is formatted with the expected missing parameter name.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        /// <param name="missingParamName">The missing parameter name expected to be referenced by the finding.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void MissingParamTag_IsDetected(string memberCode, string missingParamName)
        {
            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSingleSmell(findings, XmlDocSmells.MissingParamTag.Id);
            FindingAsserts.OnlyContainsSmells(findings, XmlDocSmells.MissingParamTag.Id);

            Finding doc301 = findings.Single(f => f.Smell.Id == XmlDocSmells.MissingParamTag.Id);

            string expectedMessage = string.Format(doc301.Smell.MessageTemplate, missingParamName);
            Assert.Equal(expectedMessage, doc301.Message);
            Assert.Equal("param", doc301.TagName);
        }
    }
}
