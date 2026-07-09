using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Smoke tests ensuring that valid returns documentation produces no returns findings.
    /// </summary>
    public sealed class Syntax_NoFinding_ReturnsDetector_SmokeTests
    {
        /// <summary>
        /// Ensures that a correctly documented non-void method produces no returns smells.
        /// </summary>
        [Fact]
        public void ValidReturnsDocs_ProduceNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <returns>Ok.</returns>\n" +
                "public int M() { return 0; }\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a correctly documented readable property using value produces no returns findings.
        /// </summary>
        [Fact]
        public void ValidReadablePropertyValueDocs_ProduceNoReturnsFindings()
        {
            string member =
                "/// <summary>Gets the count.</summary>\n" +
                "/// <value>The count.</value>\n" +
                "public int Count\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a correctly documented write-only property without returns produces no returns findings.
        /// </summary>
        [Fact]
        public void ValidWriteOnlyPropertyDocs_ProduceNoReturnsFindings()
        {
            string member =
                "/// <summary>Sets the password.</summary>\n" +
                "/// <remarks>The assigned value is stored securely.</remarks>\n" +
                "public string Password\n" +
                "{\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a correctly documented indexer using value produces no returns findings.
        /// </summary>
        [Fact]
        public void ValidIndexerValueDocs_ProduceNoReturnsFindings()
        {
            string member =
                "/// <summary>Gets the item at the specified index.</summary>\n" +
                "/// <param name=\"index\">The item index.</param>\n" +
                "/// <value>The item at the specified index.</value>\n" +
                "public string this[int index]\n" +
                "{\n" +
                "    get { return string.Empty; }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
