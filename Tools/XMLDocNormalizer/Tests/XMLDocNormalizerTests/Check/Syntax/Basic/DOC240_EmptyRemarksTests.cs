using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Basic
{
    /// <summary>
    /// Verifies detection of empty remarks tags.
    /// </summary>
    public sealed class DOC240_EmptyRemarksTests
    {
        /// <summary>
        /// Provides remarks elements without meaningful content.
        /// </summary>
        /// <returns>Member snippets containing empty remarks tags.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <remarks></remarks>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <remarks>   </remarks>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures that empty remarks tags are reported.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void EmptyRemarks_AreDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.EmptyRemarks.ID);
        }

        /// <summary>
        /// Ensures that non-empty remarks tags do not produce an empty-remarks finding.
        /// </summary>
        [Fact]
        public void NonEmptyRemarks_DoNotProduceFinding()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "/// <remarks>Useful text.</remarks>\n" +
                "public void M() { }\n";

            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(member);

            Assert.DoesNotContain(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.EmptyRemarks.ID);
        }
    }
}
