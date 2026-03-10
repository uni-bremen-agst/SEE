using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC831 – ValueOnInvalidMember.
    /// </summary>
    public sealed class DOC831_ValueOnInvalidMemberTests
    {
        /// <summary>
        /// Ensures that a value tag on a method triggers DOC831.
        /// </summary>
        [Fact]
        public void ValueOnMethod_IsDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ValueOnInvalidMember.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that a value tag on a field triggers DOC831.
        /// </summary>
        [Fact]
        public void ValueOnField_IsDetected()
        {
            string member =
                "/// <summary>Stores something.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public int count;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ValueOnInvalidMember.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that every invalid value tag is reported.
        /// </summary>
        [Fact]
        public void EveryInvalidValueTag_IsDetected()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <value>First.</value>\n" +
                "/// <value>Second.</value>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                Assert.Equal(XmlDocSmells.ValueOnInvalidMember.ID, finding.Smell.ID);
                Assert.Equal("value", finding.TagName);
            });
        }

        /// <summary>
        /// Ensures that readable properties do not trigger DOC831.
        /// </summary>
        [Fact]
        public void ReadableProperty_DoesNotTriggerDoc831()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.ValueOnInvalidMember.ID);
        }

        /// <summary>
        /// Ensures that indexers do not trigger DOC831.
        /// </summary>
        [Fact]
        public void Indexer_DoesNotTriggerDoc831()
        {
            string member =
                "/// <summary>Gets an item.</summary>\n" +
                "/// <value>The indexed value.</value>\n" +
                "public int this[int i] => 0;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.ValueOnInvalidMember.ID);
        }

        /// <summary>
        /// Ensures that write-only properties do not trigger DOC831 because DOC830 handles them.
        /// </summary>
        [Fact]
        public void WriteOnlyProperty_DoesNotTriggerDoc831()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "/// <value>Assigned value.</value>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.ValueOnInvalidMember.ID);
        }
    }
}
