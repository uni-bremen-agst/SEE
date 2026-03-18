using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Smoke tests ensuring that valid <see> and <seealso> usage produces no findings
    /// for the currently implemented missing-target rules.
    /// </summary>
    public sealed class Syntax_NoFinding_SeeDetector_SmokeTests
    {
        /// <summary>
        /// Ensures that a valid <see> cref target produces no findings.
        /// </summary>
        [Fact]
        public void ValidSeeWithCref_ProducesNoFindings()
        {
            string member =
                "/// <summary><see cref=\"string\" /></summary>\n" +
                "public void M() { }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a valid <see> href target produces no findings.
        /// </summary>
        [Fact]
        public void ValidSeeWithHref_ProducesNoFindings()
        {
            string member =
                "/// <summary><see href=\"https://example.com\" /></summary>\n" +
                "public void M() { }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a valid <see> langword target produces no findings.
        /// </summary>
        [Fact]
        public void ValidSeeWithLangword_ProducesNoFindings()
        {
            string member =
                "/// <summary><see langword=\"null\" /></summary>\n" +
                "public void M() { }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a valid <seealso> cref target produces no findings.
        /// </summary>
        [Fact]
        public void ValidSeeAlsoWithCref_ProducesNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"string\" />\n" +
                "public void M() { }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a valid <seealso> href target produces no findings.
        /// </summary>
        [Fact]
        public void ValidSeeAlsoWithHref_ProducesNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <seealso href=\"https://example.com\" />\n" +
                "public void M() { }\n";

            List<XMLDocNormalizer.Models.Finding> findings = CheckAssert.FindSeeFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
