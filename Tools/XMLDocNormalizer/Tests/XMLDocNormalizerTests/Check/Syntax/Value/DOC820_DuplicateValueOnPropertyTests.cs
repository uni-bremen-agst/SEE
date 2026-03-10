using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC820 – DuplicateValueOnProperty.
    /// </summary>
    public sealed class DOC820_DuplicateValueOnPropertyTests
    {
        /// <summary>
        /// Ensures that a second value tag on a readable property triggers DOC820.
        /// </summary>
        [Fact]
        public void SecondValueTag_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.DuplicateValueOnProperty.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that every value tag after the first is reported as duplicate.
        /// </summary>
        [Fact]
        public void EveryAdditionalValueTag_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "/// <value>Third value.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                Assert.Equal(XmlDocSmells.DuplicateValueOnProperty.ID, finding.Smell.ID);
                Assert.Equal("value", finding.TagName);
            });
        }

        /// <summary>
        /// Ensures that a single value tag does not trigger DOC820.
        /// </summary>
        [Fact]
        public void SingleValueTag_IsNotDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.DuplicateValueOnProperty.ID);
        }

        /// <summary>
        /// Ensures that undocumented properties are ignored here.
        /// </summary>
        [Fact]
        public void PropertyWithoutDocumentation_IsIgnored()
        {
            string member =
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that write-only properties do not trigger DOC820.
        /// </summary>
        [Fact]
        public void WriteOnlyProperty_DoesNotTriggerDoc820()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "/// <value>First value.</value>\n" +
                "/// <value>Second value.</value>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.DuplicateValueOnProperty.ID);
        }
    }
}
