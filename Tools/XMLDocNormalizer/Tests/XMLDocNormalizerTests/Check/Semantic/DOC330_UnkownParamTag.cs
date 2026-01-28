using System.Collections.Generic;
using System.Linq;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Tests for DOC330 (UnknownParamTag): a <param> tag references a parameter name that does not exist.
    /// </summary>
    public sealed class DOC330_UnknownParamTagTests
    {
        /// <summary>
        /// Provides member snippets where an orphaned <param name="..."> exists for a non-existent parameter.
        /// Each case is designed to produce exactly one DOC330 finding and no additional param smells.
        /// </summary>
        /// <returns>Test cases consisting of member code and the unknown parameter name.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            // Method: ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "/// <param name=\"ghost\">ghost</param>\n" +
                "public void M(int x) { }\n",
                "ghost"
            };

            // Constructor: ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "/// <param name=\"ghost\">ghost</param>\n" +
                "public Wrapper(int x) { }\n",
                "ghost"
            };

            // Delegate: ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">x</param>\n" +
                "/// <param name=\"ghost\">ghost</param>\n" +
                "public delegate void D(int x);\n",
                "ghost"
            };

            // Indexer: ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"index\">index</param>\n" +
                "/// <param name=\"ghost\">ghost</param>\n" +
                "public int this[int index]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n",
                "ghost"
            };

            // Operator: ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"left\">left</param>\n" +
                "/// <param name=\"right\">right</param>\n" +
                "/// <param name=\"ghost\">ghost</param>\n" +
                "public static Wrapper operator +(Wrapper left, Wrapper right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n",
                "ghost"
            };

            // Conversion operator: ghost does not exist.
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"value\">value</param>\n" +
                "/// <param name=\"ghost\">ghost</param>\n" +
                "public static explicit operator int(Wrapper value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n",
                "ghost"
            };
        }

        /// <summary>
        /// Ensures that unknown parameter references are reported as DOC330, that no other param smells are produced,
        /// and that the reported message is formatted with the expected unknown parameter name.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        /// <param name="unknownParamName">The unknown parameter name expected to be referenced by the finding.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void UnknownParamTag_IsDetected(string memberCode, string unknownParamName)
        {
            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, "DOC330");

            Finding doc330 = findings.Single();

            string expectedMessage = string.Format(doc330.Smell.MessageTemplate, unknownParamName);
            Assert.Equal(expectedMessage, doc330.Message);
            Assert.Equal("param", doc330.TagName);
        }
    }
}
