using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Integration
{
    /// <summary>
    /// Integration tests for see and seealso tag interactions in the full syntax detector pipeline.
    /// </summary>
    public sealed class FullSyntaxPipelineSeeIntegrationTests
    {
        /// <summary>
        /// Ensures that valid see and seealso tags do not produce syntax findings.
        /// </summary>
        [Fact]
        public void ValidSeeAndSeeAlsoTags_DoNotTriggerAnySyntaxFindings()
        {
            string memberCode =
                "/// <summary>Creates a new instance of <see cref=\"C\"/>.</summary>\n" +
                "/// <returns>The created instance.</returns>\n" +
                "/// <seealso cref=\"C\"/>\n" +
                "public C M()\n" +
                "{\n" +
                "    return new C();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings);
        }

        /// <summary>
        /// Ensures that missing see and seealso targets are reported precisely.
        /// </summary>
        [Fact]
        public void MissingSeeAndSeeAlsoTargets_ReportOnlyMissingTargetFindings()
        {
            string memberCode =
                "/// <summary>Creates a new instance of <see/>.</summary>\n" +
                "/// <returns>The created instance.</returns>\n" +
                "/// <seealso/>\n" +
                "public C M()\n" +
                "{\n" +
                "    return new C();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.SeeAlsoMissingTarget.ID,
                XmlDocSmells.SeeMissingTarget.ID);
        }

        /// <summary>
        /// Ensures that invalid see and seealso attribute combinations are reported precisely.
        /// </summary>
        [Fact]
        public void InvalidSeeAttributeCombinations_ReportOnlyCombinationFindings()
        {
            string memberCode =
                "/// <summary>Uses <see cref=\"C\" href=\"https://example.com\"/>.</summary>\n" +
                "/// <returns>The created instance.</returns>\n" +
                "/// <seealso cref=\"C\" href=\"https://example.com\"/>\n" +
                "public C M()\n" +
                "{\n" +
                "    return new C();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.InvalidSeeAlsoAttributeCombination.ID,
                XmlDocSmells.InvalidSeeAttributeCombination.ID);
        }

        /// <summary>
        /// Ensures that unknown see and seealso attributes are reported precisely.
        /// </summary>
        [Fact]
        public void InvalidSeeAttributes_ReportOnlyInvalidAttributeFindings()
        {
            string memberCode =
                "/// <summary>Uses <see cref=\"C\" unknown=\"value\"/>.</summary>\n" +
                "/// <returns>The created instance.</returns>\n" +
                "/// <seealso cref=\"C\" unknown=\"value\"/>\n" +
                "public C M()\n" +
                "{\n" +
                "    return new C();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.InvalidSeeAlsoAttribute.ID,
                XmlDocSmells.InvalidSeeAttribute.ID);
        }

        /// <summary>
        /// Ensures that invalid see and seealso href values are reported precisely.
        /// </summary>
        [Fact]
        public void InvalidSeeHrefValues_ReportOnlyHrefFindings()
        {
            string memberCode =
                "/// <summary>Uses <see href=\"not-a-url\"/>.</summary>\n" +
                "/// <returns>The created instance.</returns>\n" +
                "/// <seealso href=\"not-a-url\"/>\n" +
                "public C M()\n" +
                "{\n" +
                "    return new C();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.InvalidSeeAlsoHref.ID,
                XmlDocSmells.InvalidSeeHref.ID);
        }

        /// <summary>
        /// Ensures that unsupported see langword values and seealso langword usage are reported precisely.
        /// </summary>
        [Fact]
        public void InvalidLangwordUsage_ReportOnlyLangwordFindings()
        {
            string memberCode =
                "/// <summary>Uses <see langword=\"unknown\"/>.</summary>\n" +
                "/// <returns>The created instance.</returns>\n" +
                "/// <seealso langword=\"true\"/>\n" +
                "public C M()\n" +
                "{\n" +
                "    return new C();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.SeeAlsoLangwordNotSupported.ID,
                XmlDocSmells.InvalidSeeLangword.ID);
        }

        /// <summary>
        /// Ensures that non-empty see and seealso tags are reported precisely.
        /// </summary>
        [Fact]
        public void NonEmptySeeAndSeeAlsoTags_ReportOnlyNonEmptyFindings()
        {
            string memberCode =
                "/// <summary>Uses <see cref=\"C\">content</see>.</summary>\n" +
                "/// <returns>The created instance.</returns>\n" +
                "/// <seealso cref=\"C\">content</seealso>\n" +
                "public C M()\n" +
                "{\n" +
                "    return new C();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.SeeAlsoNotEmpty.ID,
                XmlDocSmells.SeeNotEmpty.ID);
        }

        /// <summary>
        /// Ensures that duplicate seealso targets are reported once per duplicate group.
        /// </summary>
        [Fact]
        public void DuplicateSeeAlsoTargets_ReportOnlyOneDuplicateSeeAlsoFinding()
        {
            string memberCode =
                "/// <summary>Creates a new instance.</summary>\n" +
                "/// <returns>The created instance.</returns>\n" +
                "/// <seealso cref=\"C\"/>\n" +
                "/// <seealso cref=\"C\"/>\n" +
                "public C M()\n" +
                "{\n" +
                "    return new C();\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindAllFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.DuplicateSeeAlsoTarget.ID);
        }
    }
}
