using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC810 – EmptyValueOnProperty.
    /// </summary>
    public sealed class DOC810_EmptyValueOnPropertyTests
    {
        /// <summary>
        /// Ensures that an empty value tag on a readable property triggers DOC810.
        /// </summary>
        [Fact]
        public void EmptyValueTag_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value></value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.EmptyValueOnProperty.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that a whitespace-only value tag on a readable property triggers DOC810.
        /// </summary>
        [Fact]
        public void WhitespaceOnlyValueTag_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>   </value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.EmptyValueOnProperty.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that a non-empty value tag on a readable property does not trigger DOC810.
        /// </summary>
        [Fact]
        public void NonEmptyValueTag_IsNotDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that missing value tags are not reported as DOC810.
        /// </summary>
        [Fact]
        public void MissingValueTag_DoesNotTriggerDoc810()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.EmptyValueOnProperty.ID);
        }

        /// <summary>
        /// Ensures that expression-bodied properties are treated as readable properties for DOC810.
        /// </summary>
        [Fact]
        public void ExpressionBodiedPropertyWithEmptyValue_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value></value>\n" +
                "public int Count => 42;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.EmptyValueOnProperty.ID, finding.Smell.ID);
        }

        /// <summary>
        /// Ensures that write-only properties do not trigger DOC810.
        /// </summary>
        [Fact]
        public void WriteOnlyProperty_DoesNotTriggerDoc810()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "/// <value></value>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.EmptyValueOnProperty.ID);
        }
    }
}