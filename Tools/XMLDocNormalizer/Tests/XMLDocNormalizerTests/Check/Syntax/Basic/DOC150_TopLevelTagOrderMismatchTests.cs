using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Basic
{
    /// <summary>
    /// Verifies detection of top-level XML documentation tag order mismatches.
    /// </summary>
    public sealed class DOC150_TopLevelTagOrderMismatchTests
    {
        /// <summary>
        /// Provides member snippets with invalid top-level tag order.
        /// </summary>
        /// <returns>Member snippets that should produce DOC150.</returns>
        public static IEnumerable<object[]> InvalidDeclarationSources()
        {
            yield return new object[]
            {
                "/// <remarks>More details.</remarks>\n" +
                "/// <summary>Test.</summary>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <param name=\"x\">Value.</param>\n" +
                "/// <summary>Test.</summary>\n" +
                "public void M(int x) { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <returns>Value.</returns>\n" +
                "/// <param name=\"x\">Value.</param>\n" +
                "public int M(int x) { return x; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <seealso cref=\"string\" />\n" +
                "/// <exception cref=\"System.Exception\">Failure.</exception>\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Provides member snippets with valid top-level tag order.
        /// </summary>
        /// <returns>Member snippets that should not produce DOC150.</returns>
        public static IEnumerable<object[]> ValidDeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <remarks>More details.</remarks>\n" +
                "public void M() { }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"x\">Value.</param>\n" +
                "/// <returns>Value.</returns>\n" +
                "public int M(int x) { return x; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <typeparam name=\"T\">Type parameter.</typeparam>\n" +
                "public T M<T>(T value) { return value; }\n"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <exception cref=\"System.Exception\">Failure.</exception>\n" +
                "/// <seealso cref=\"string\" />\n" +
                "public void M() { }\n"
            };
        }

        /// <summary>
        /// Ensures DOC150 is reported when top-level tags appear in the wrong order.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(InvalidDeclarationSources))]
        public void InvalidTopLevelOrder_IsDetected(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(memberCode);

            Assert.Contains(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.TopLevelTagOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures DOC150 is not reported when top-level tags appear in the expected order.
        /// </summary>
        /// <param name="memberCode">The member snippet to analyze.</param>
        [Theory]
        [MemberData(nameof(ValidDeclarationSources))]
        public void ValidTopLevelOrder_ProducesNoFinding(string memberCode)
        {
            List<Finding> findings = CheckAssert.FindBasicFindingsForMember(memberCode);

            Assert.DoesNotContain(
                findings,
                static finding => finding.Smell.ID == XmlDocSmells.TopLevelTagOrderMismatch.ID);
        }
    }
}
