using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Inheritdoc
{
    /// <summary>
    /// Tests DOC750 – multiple <c>inheritdoc</c> tags on the same declaration.
    /// </summary>
    public sealed class DOC750_DuplicateInheritdocTagTests
    {
        /// <summary>
        /// Ensures that two empty inheritdoc tags trigger DOC750.
        /// </summary>
        [Fact]
        public void TwoEmptyInheritdocTags_IsDetected()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "/// <inheritdoc/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.DuplicateInheritdocTag.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that two cref-based inheritdoc tags trigger DOC750.
        /// </summary>
        [Fact]
        public void TwoCrefInheritdocTags_IsDetected()
        {
            string member =
                "/// <inheritdoc cref=\"object.ToString\"/>\n" +
                "/// <inheritdoc cref=\"object.GetHashCode\"/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.DuplicateInheritdocTag.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that a mixed combination of empty and cref-based inheritdoc tags triggers DOC750.
        /// </summary>
        [Fact]
        public void MixedInheritdocTags_IsDetected()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "/// <inheritdoc cref=\"object.ToString\"/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.DuplicateInheritdocTag.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that a single inheritdoc tag does not trigger DOC750.
        /// </summary>
        [Fact]
        public void SingleInheritdoc_DoesNotTriggerFinding()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a full-element inheritdoc and an empty-element inheritdoc
        /// are both considered by DOC750.
        /// </summary>
        [Fact]
        public void FullAndEmptyInheritdocTags_IsDetected()
        {
            string member =
                "/// <inheritdoc></inheritdoc>\n" +
                "/// <inheritdoc/>\n" +
                "public override string ToString() => base.ToString();\n";

            List<Finding> findings = CheckAssert.FindInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.DuplicateInheritdocTag.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }
    }
}