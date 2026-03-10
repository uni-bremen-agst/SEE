using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Integration tests for interactions between value-related and generic invalid-tag detection.
    /// </summary>
    public sealed class DOC831_DOC143_IntegrationTests
    {
        /// <summary>
        /// Ensures that a value tag on an invalid member is reported only as DOC831 and not additionally as DOC143.
        /// </summary>
        [Fact]
        public void ValueOnMethod_ReportsOnlyDoc831()
        {
            string member =
                "/// <summary>Does something.</summary>\n" +
                "/// <value>Invalid.</value>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(member);

            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.ValueOnInvalidMember.ID);
            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.InvalidTagOnMember.ID);
        }

        /// <summary>
        /// Ensures that a valid value tag on a readable property does not additionally trigger DOC143.
        /// </summary>
        [Fact]
        public void ValueOnReadableProperty_DoesNotReportDoc143()
        {
            string member =
                "/// <summary>Gets something.</summary>\n" +
                "/// <value>The current value.</value>\n" +
                "public int P { get; set; }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(member);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.InvalidTagOnMember.ID);
        }

        /// <summary>
        /// Ensures that a value tag on a write-only property is reported only as DOC830 and not additionally as DOC143.
        /// </summary>
        [Fact]
        public void ValueOnWriteOnlyProperty_ReportsOnlyDoc830()
        {
            string member =
                "/// <summary>Sets something.</summary>\n" +
                "/// <value>Assigned value.</value>\n" +
                "public int P { set { } }\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(member);

            Assert.Contains(findings, finding => finding.Smell.ID == XmlDocSmells.ValueOnWriteOnlyProperty.ID);
            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.InvalidTagOnMember.ID);
        }
    }
}