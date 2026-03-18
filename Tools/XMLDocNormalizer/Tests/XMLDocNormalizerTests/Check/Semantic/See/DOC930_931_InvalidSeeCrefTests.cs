using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.See
{
    /// <summary>
    /// Verifies detection of unresolved cref targets on <see> and <seealso>.
    /// </summary>
    public sealed class DOC930_931_InvalidSeeCrefTests
    {
        /// <summary>
        /// Ensures DOC930 is reported when a <see> cref cannot be resolved.
        /// </summary>
        [Fact]
        public void InvalidSeeCref_IsDetected()
        {
            string member =
                "/// <summary><see cref=\"DoesNotExist\" /></summary>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticSeeFindingsForMember(member);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.InvalidSeeCref.ID);
        }

        /// <summary>
        /// Ensures DOC931 is reported when a <seealso> cref cannot be resolved.
        /// </summary>
        [Fact]
        public void InvalidSeeAlsoCref_IsDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"DoesNotExist\" />\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticSeeFindingsForMember(member);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.InvalidSeeAlsoCref.ID);
        }

        /// <summary>
        /// Ensures that a resolvable <see> cref produces no semantic finding.
        /// </summary>
        [Fact]
        public void ValidSeeCref_ProducesNoFindings()
        {
            string member =
                "/// <summary><see cref=\"string\" /></summary>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticSeeFindingsForMember(member);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a resolvable <seealso> cref produces no semantic finding.
        /// </summary>
        [Fact]
        public void ValidSeeAlsoCref_ProducesNoFindings()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"string\" />\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindSemanticSeeFindingsForMember(member);

            Assert.Empty(findings);
        }
    }
}
