using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC830 – ValueOnWriteOnlyProperty.
    /// </summary>
    public sealed class DOC830_ValueOnWriteOnlyPropertyTests
    {
        /// <summary>
        /// Ensures that a value tag on a write-only property triggers DOC830.
        /// </summary>
        [Fact]
        public void SingleValueTag_IsDetected()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "/// <value>The assigned value.</value>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ValueOnWriteOnlyProperty.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that every value tag on a write-only property is reported.
        /// </summary>
        [Fact]
        public void EveryValueTag_IsDetected()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "/// <value>First.</value>\n" +
                "/// <value>Second.</value>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                Assert.Equal(XmlDocSmells.ValueOnWriteOnlyProperty.ID, finding.Smell.ID);
                Assert.Equal("value", finding.TagName);
            });
        }

        /// <summary>
        /// Ensures that a write-only property without a value tag does not trigger DOC830.
        /// </summary>
        [Fact]
        public void NoValueTag_IsNotDetected()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that readable properties do not trigger DOC830.
        /// </summary>
        [Fact]
        public void ReadableProperty_DoesNotTriggerDoc830()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.ValueOnWriteOnlyProperty.ID);
        }

        /// <summary>
        /// Ensures that undocumented write-only properties are ignored here.
        /// </summary>
        [Fact]
        public void WriteOnlyPropertyWithoutDocumentation_IsIgnored()
        {
            string member =
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
