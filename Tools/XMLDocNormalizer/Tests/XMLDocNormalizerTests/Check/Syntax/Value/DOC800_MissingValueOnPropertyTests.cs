using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests for DOC800 – MissingValueOnProperty.
    /// </summary>
    public sealed class DOC800_MissingValueOnPropertyTests
    {
        /// <summary>
        /// Ensures that a readable property with XML documentation but without a value tag triggers DOC800.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithoutValue_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingValueOnProperty.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
        }

        /// <summary>
        /// Ensures that a readable property with a value tag does not trigger DOC800.
        /// </summary>
        [Fact]
        public void ReadablePropertyWithValue_IsNotDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "/// <value>The current count.</value>\n" +
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that missing overall documentation does not trigger DOC800 here,
        /// because this is handled by the basic detector.
        /// </summary>
        [Fact]
        public void PropertyWithoutDocumentation_IsNotDetectedHere()
        {
            string member =
                "public int Count { get; set; }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that expression-bodied properties are treated as readable properties.
        /// </summary>
        [Fact]
        public void ExpressionBodiedPropertyWithoutValue_IsDetected()
        {
            string member =
                "/// <summary>Gets the value.</summary>\n" +
                "public int Count => 42;\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingValueOnProperty.ID, finding.Smell.ID);
        }

        /// <summary>
        /// Ensures that write-only properties do not trigger DOC800.
        /// </summary>
        [Fact]
        public void WriteOnlyProperty_DoesNotTriggerDoc800()
        {
            string member =
                "/// <summary>Sets the value.</summary>\n" +
                "public int Count { set { } }\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}