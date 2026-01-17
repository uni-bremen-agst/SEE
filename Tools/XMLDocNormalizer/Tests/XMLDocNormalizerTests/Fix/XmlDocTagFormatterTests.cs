using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Fix
{
    /// <summary>
    /// Tests for <see cref="XMLDocNormalizer.Rewriting.XmlDocTagFormatter"/> behavior as exercised
    /// through <see cref="XMLDocNormalizer.Rewriting.XmlDocRewriter"/> (Normalize is applied to
    /// <c>param</c>, <c>returns</c>, and <c>exception</c>).
    /// </summary>
    public sealed class XmlDocTagFormatterTests
    {
        /// <summary>
        /// Ensures that a period is appended after an element (e.g. <c>&lt;see /&gt;</c>) when the
        /// overall content does not already end with a period.
        /// </summary>
        [Fact]
        public void Returns_AppendsPeriod_AfterSeeElement()
        {
            string inputMember =
                "/// <returns>Uses <see cref=\"T:System.String\"/></returns>\n" +
                "string M() { return string.Empty; }\n";

            string expectedMember =
                "/// <returns>Uses <see cref=\"T:System.String\"/>.</returns>\n" +
                "string M() { return string.Empty; }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }

        /// <summary>
        /// Ensures that no additional period is appended if the content already ends with a period.
        /// </summary>
        [Fact]
        public void Returns_DoesNotDoublePeriod()
        {
            string inputMember =
                "/// <returns>Already ends with a period.</returns>\n" +
                "int M() { return 0; }\n";

            string expectedMember =
                "/// <returns>Already ends with a period.</returns>\n" +
                "int M() { return 0; }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }

        /// <summary>
        /// Ensures that if the content ends with a <c>&lt;para&gt;</c> element, no trailing period
        /// is appended after the element.
        /// </summary>
        [Fact]
        public void Returns_DoesNotAppendPeriod_AfterParaElement()
        {
            string inputMember =
                "/// <returns><para>paragraph without punctuation</para></returns>\n" +
                "int M() { return 0; }\n";

            string expectedMember =
                "/// <returns><para>paragraph without punctuation</para></returns>\n" +
                "int M() { return 0; }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }

        /// <summary>
        /// Ensures that capitalization does not apply when a no-capitalize element (e.g. <c>&lt;see /&gt;</c>)
        /// is encountered before the first non-whitespace text content.
        /// </summary>
        [Fact]
        public void Param_DoesNotCapitalize_WhenSeeAppearsBeforeText()
        {
            string inputMember =
                "/// <param name=\"x\"><see cref=\"T:System.String\"/> does not get capitalized</param>\n" +
                "void M(int x) { }\n";

            // Capitalize() returns early when it encounters <see/>, so the following text remains unchanged.
            // EnsurePeriod() still appends a period at the end.
            string expectedMember =
                "/// <param name=\"x\"><see cref=\"T:System.String\"/> does not get capitalized.</param>\n" +
                "void M(int x) { }\n";

            RewriteAssert.MemberEquals(inputMember, expectedMember);
        }
    }
}
