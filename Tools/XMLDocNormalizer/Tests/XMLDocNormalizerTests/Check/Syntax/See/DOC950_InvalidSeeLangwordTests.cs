using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.See;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.See
{
    /// <summary>
    /// Verifies detection of DOC950 for unsupported
    /// <c>langword</c> values on <c>see</c> tags.
    /// </summary>
    public sealed class DOC950_InvalidSeeLangwordTests
    {
        /// <summary>
        /// Provides see declarations with invalid langword values.
        /// </summary>
        /// <returns>
        /// Member snippets containing invalid see langword values and the
        /// expected value.
        /// </returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary><see langword=\"invalid\" /></summary>\n" +
                "public void M() { }\n",
                "invalid"
            };

            yield return new object[]
            {
                "/// <summary><see langword=\"String\" /></summary>\n" +
                "public void M() { }\n",
                "String"
            };

            yield return new object[]
            {
                "/// <summary><see langword=\"123\" /></summary>\n" +
                "public void M() { }\n",
                "123"
            };
        }

        /// <summary>
        /// Ensures DOC950 is reported when a see tag contains an unsupported
        /// langword value.
        /// </summary>
        /// <param name="memberCode">
        /// The member snippet to analyze.
        /// </param>
        /// <param name="invalidLangword">
        /// The invalid langword value expected in the message.
        /// </param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void SeeWithInvalidLangword_IsDetected(
            string memberCode,
            string invalidLangword)
        {
            List<Finding> findings =
                CheckAssert.FindSeeFindingsForMember(
                    memberCode);

            Finding finding =
                findings.Single(
                    item =>
                        item.Smell.ID ==
                        XmlDocSmells.InvalidSeeLangword.ID);

            string expectedMessage =
                string.Format(
                    finding.Smell.MessageTemplate,
                    invalidLangword);

            Assert.Equal(
                expectedMessage,
                finding.Message);

            Assert.Equal(
                "see",
                finding.TagName);

            Assert.Equal(
                "SeeTag",
                finding.Context.SubjectKind);

            Assert.Equal(
                "langword:" + invalidLangword,
                finding.Context.TargetName);
        }

        /// <summary>
        /// Ensures that every reserved and contextual keyword recognized by
        /// Roslyn is accepted as a see langword.
        /// </summary>
        [Fact]
        public void AllCSharpKeywords_AreAccepted()
        {
            IEnumerable<string> keywords =
                SyntaxFacts.GetKeywordKinds()
                    .Concat(SyntaxFacts.GetContextualKeywordKinds())
                    .Select(SyntaxFacts.GetText)
                    .Where(static keyword => !string.IsNullOrEmpty(keyword))
                    .Concat(
                    [
                        "dynamic",
                        "var"
                    ])
                    .Distinct(StringComparer.Ordinal);

            foreach (string keyword in keywords)
            {
                Assert.True(
                    XmlSeeTagRules.IsValidLangword(keyword),
                    $"Expected '{keyword}' to be accepted as a langword.");
            }
        }
    }
}
