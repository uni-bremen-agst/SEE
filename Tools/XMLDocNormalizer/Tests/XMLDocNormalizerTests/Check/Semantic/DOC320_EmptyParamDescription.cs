using System.Collections.Generic;
using System.Linq;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC320 (EmptyParamDescription): a <param> tag exists but its description is empty.
    /// </summary>
    public sealed class DOC320_EmptyParamDescriptionTests
    {
        /// <summary>
        /// Provides member snippets where a <param> tag exists but contains no meaningful content.
        /// Each case is designed to produce exactly one DOC320 finding and no additional param smells.
        /// </summary>
        /// <returns>Test cases consisting of member code and the parameter name with empty description.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Method: x is empty.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\"></param>\n" +
                "public void M(int x) { }\n",
                "x"
            };

            // Constructor: x is whitespace.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\"> \n" +
                "/// </param>\n" +
                "public Wrapper(int x) { }\n",
                "x"
            };

            // Delegate: x empty.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\"></param>\n" +
                "public delegate void D(int x);\n",
                "x"
            };

            // Indexer: index empty.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"index\"></param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n",
                "index"
            };

            // Operator: left empty, right ok.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"left\"></param>\n" +
                "/// <param name=\"right\">ok</param>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n",
                "left"
            };

            // Conversion operator: value empty.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"value\"></param>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n",
                "value"
            };
        }

        /// <summary>
        /// Ensures that empty <param> descriptions are reported as DOC320, that no other param smells are produced,
        /// and that the reported message is formatted with the expected parameter name.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        /// <param name="paramName">The parameter name expected to be referenced by the finding.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void EmptyParamDescription_IsDetected(string memberCode, string paramName)
        {
            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC320");

            Finding doc320 = findings.Single();

            string expectedMessage = string.Format(doc320.Smell.MessageTemplate, paramName);
            Assert.Equal(expectedMessage, doc320.Message);
            Assert.Equal("param", doc320.TagName);
        }

        /// <summary>
        /// Ensures that a <param> containing nested XML elements is treated as non-empty (no DOC320).
        /// </summary>
        [Fact]
        public void ParamDescription_WithSee_IsNotEmpty()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\"><see cref=\"System.Int32\"/></param>\n" +
                "public void M(int x) { }\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
