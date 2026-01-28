using System.Collections.Generic;
using System.Linq;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC350 (DuplicateParamTag): multiple <param> tags exist for the same parameter name.
    /// </summary>
    public sealed class DOC350_DuplicateParamTagTests
    {
        /// <summary>
        /// Provides member snippets where at least one <param> name is duplicated.
        /// Each case is designed to produce exactly one DOC350 finding and no additional param smells.
        /// </summary>
        /// <returns>Test cases consisting of member code and the duplicated parameter name.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Method: x duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">first</param>\n" +
                "/// <param name=\"x\">second</param>\n" +
                "public void M(int x) { }\n",
                "x"
            };

            // Constructor: x duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">first</param>\n" +
                "/// <param name=\"x\">second</param>\n" +
                "public Wrapper(int x) { }\n",
                "x"
            };

            // Delegate: x duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">first</param>\n" +
                "/// <param name=\"x\">second</param>\n" +
                "public delegate void D(int x);\n",
                "x"
            };

            // Indexer: index duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"index\">first</param>\n" +
                "/// <param name=\"index\">second</param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n",
                "index"
            };

            // Operator: left duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"left\">first</param>\n" +
                "/// <param name=\"left\">second</param>\n" +
                "/// <param name=\"right\">ok</param>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n",
                "left"
            };

            // Conversion operator: value duplicated.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"value\">first</param>\n" +
                "/// <param name=\"value\">second</param>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n",
                "value"
            };
        }

        /// <summary>
        /// Ensures that duplicate <param> tags are reported as DOC350, that no other param smells are produced,
        /// and that the reported message is formatted with the expected duplicated parameter name.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        /// <param name="duplicatedParamName">The duplicated parameter name expected to be referenced by the finding.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void DuplicateParamTag_IsDetected(string memberCode, string duplicatedParamName)
        {
            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC350");

            Finding doc350 = findings.Single();

            string expectedMessage = string.Format(doc350.Smell.MessageTemplate, duplicatedParamName);
            Assert.Equal(expectedMessage, doc350.Message);
            Assert.Equal("param", doc350.TagName);
        }
    }
}
