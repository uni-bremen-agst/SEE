using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Tests for DOC541 (ReturnsOnIndexer): returns documentation is used on an indexer.
    /// </summary>
    public sealed class DOC541_ReturnsOnIndexerTests
    {
        /// <summary>
        /// Ensures that returns documentation on a read-only indexer is detected.
        /// </summary>
        [Fact]
        public void ReturnsOnReadOnlyIndexer_IsDetected()
        {
            string memberCode =
                "/// <summary>Gets the item at the specified index.</summary>\n" +
                "/// <param name=\"index\">The item index.</param>\n" +
                "/// <returns>The item at the specified index.</returns>\n" +
                "public string this[int index]\n" +
                "{\n" +
                "    get { return string.Empty; }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ReturnsOnIndexer.ID);

            Finding finding = findings.Single();

            Assert.Equal("returns", finding.TagName);
            Assert.Equal("<returns> must not be used on indexer 'this[]'.", finding.Message);
            Assert.Equal("Indexer", finding.Context.OwnerKind);
            Assert.Equal("ReturnValue", finding.Context.SubjectKind);
            Assert.Equal("this[]", finding.Context.SymbolName);
            Assert.Equal("this[]", finding.Context.TargetName);
        }

        /// <summary>
        /// Ensures that returns documentation on a get-set indexer is detected.
        /// </summary>
        [Fact]
        public void ReturnsOnGetSetIndexer_IsDetected()
        {
            string memberCode =
                "/// <summary>Gets or sets the item at the specified index.</summary>\n" +
                "/// <param name=\"index\">The item index.</param>\n" +
                "/// <returns>The item at the specified index.</returns>\n" +
                "public string this[int index]\n" +
                "{\n" +
                "    get { return string.Empty; }\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ReturnsOnIndexer.ID);
        }

        /// <summary>
        /// Ensures that returns documentation on a write-only indexer is detected.
        /// </summary>
        [Fact]
        public void ReturnsOnWriteOnlyIndexer_IsDetected()
        {
            string memberCode =
                "/// <summary>Sets the item at the specified index.</summary>\n" +
                "/// <param name=\"index\">The item index.</param>\n" +
                "/// <returns>The item at the specified index.</returns>\n" +
                "public string this[int index]\n" +
                "{\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ReturnsOnIndexer.ID);
        }

        /// <summary>
        /// Ensures that multiple returns tags on an indexer produce only one invalid-target finding.
        /// </summary>
        [Fact]
        public void MultipleReturnsTags_OnIndexer_ProduceOnlyOneFinding()
        {
            string memberCode =
                "/// <summary>Gets the item at the specified index.</summary>\n" +
                "/// <param name=\"index\">The item index.</param>\n" +
                "/// <returns>First.</returns>\n" +
                "/// <returns>Second.</returns>\n" +
                "public string this[int index]\n" +
                "{\n" +
                "    get { return string.Empty; }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.ReturnsOnIndexer.ID, 1);
            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ReturnsOnIndexer.ID);
        }

        /// <summary>
        /// Ensures that a correctly documented indexer using value instead of returns produces no returns findings.
        /// </summary>
        [Fact]
        public void Indexer_WithValueDocumentation_ProducesNoReturnsFindings()
        {
            string memberCode =
                "/// <summary>Gets the item at the specified index.</summary>\n" +
                "/// <param name=\"index\">The item index.</param>\n" +
                "/// <value>The item at the specified index.</value>\n" +
                "public string this[int index]\n" +
                "{\n" +
                "    get { return string.Empty; }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            Assert.Empty(findings);
        }
    }
}
