using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Params
{
    /// <summary>
    /// Verifies param documentation handling for primary constructors.
    /// </summary>
    public sealed class PrimaryConstructorParamDocumentationTests
    {
        /// <summary>
        /// Provides valid primary constructor declarations with complete param documentation.
        /// </summary>
        /// <returns>Test sources and expected owner kinds.</returns>
        public static IEnumerable<object[]> ValidPrimaryConstructorSources()
        {
            yield return new object[]
            {
                "/// <summary>Represents a class with a primary constructor.</summary>\n" +
                "/// <param name=\"name\">The name.</param>\n" +
                "/// <param name=\"count\">The count.</param>\n" +
                "public sealed class Sample(string name, int count)\n" +
                "{\n" +
                "}\n",
                "Class"
            };

            yield return new object[]
            {
                "/// <summary>Represents a struct with a primary constructor.</summary>\n" +
                "/// <param name=\"name\">The name.</param>\n" +
                "/// <param name=\"count\">The count.</param>\n" +
                "public readonly struct Sample(string name, int count)\n" +
                "{\n" +
                "}\n",
                "Struct"
            };

            yield return new object[]
            {
                "/// <summary>Represents a record with a primary constructor.</summary>\n" +
                "/// <param name=\"name\">The name.</param>\n" +
                "/// <param name=\"count\">The count.</param>\n" +
                "public sealed record Sample(string name, int count);\n",
                "Record"
            };

            yield return new object[]
            {
                "/// <summary>Represents a record struct with a primary constructor.</summary>\n" +
                "/// <param name=\"name\">The name.</param>\n" +
                "/// <param name=\"count\">The count.</param>\n" +
                "public readonly record struct Sample(string name, int count);\n",
                "RecordStruct"
            };
        }

        /// <summary>
        /// Ensures param tags on primary constructors are not reported as invalid member tags.
        /// </summary>
        /// <param name="source">The full source to analyze.</param>
        /// <param name="expectedOwnerKind">The expected owner kind.</param>
        [Theory]
        [MemberData(nameof(ValidPrimaryConstructorSources))]
        public void PrimaryConstructorParamTags_AreAllowedByMemberTagDetector(
            string source,
            string expectedOwnerKind)
        {
            Assert.False(string.IsNullOrWhiteSpace(expectedOwnerKind));

            List<Finding> findings = CheckAssert.FindMemberTagFindingsForSource(source);

            Assert.DoesNotContain(findings, finding => finding.Smell.ID == XmlDocSmells.InvalidTagOnMember.ID);
        }

        /// <summary>
        /// Ensures complete primary constructor param documentation produces no param findings.
        /// </summary>
        /// <param name="source">The full source to analyze.</param>
        /// <param name="expectedOwnerKind">The expected owner kind.</param>
        [Theory]
        [MemberData(nameof(ValidPrimaryConstructorSources))]
        public void CompletePrimaryConstructorParamDocumentation_ProducesNoParamFindings(
            string source,
            string expectedOwnerKind)
        {
            Assert.False(string.IsNullOrWhiteSpace(expectedOwnerKind));

            List<Finding> findings = CheckAssert.FindParamFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures missing param documentation is detected on class primary constructors.
        /// </summary>
        [Fact]
        public void MissingPrimaryConstructorParamTag_OnClass_IsDetected()
        {
            string source =
                "/// <summary>Represents a class with a primary constructor.</summary>\n" +
                "/// <param name=\"name\">The name.</param>\n" +
                "public sealed class Sample(string name, int count)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForSource(source);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.MissingParamTag.ID);

            Finding finding = findings.Single();

            Assert.Equal("param", finding.TagName);
            Assert.Equal("Class", finding.Context.OwnerKind);
            Assert.Equal("Parameter", finding.Context.SubjectKind);
            Assert.Equal("Sample", finding.Context.SymbolName);
            Assert.Equal("count", finding.Context.TargetName);
            Assert.Equal("Missing <param> documentation for parameter 'count'.", finding.Message);
        }

        /// <summary>
        /// Ensures unknown param documentation is detected on class primary constructors.
        /// </summary>
        [Fact]
        public void UnknownPrimaryConstructorParamTag_OnClass_IsDetected()
        {
            string source =
                "/// <summary>Represents a class with a primary constructor.</summary>\n" +
                "/// <param name=\"name\">The name.</param>\n" +
                "/// <param name=\"ghost\">Unknown.</param>\n" +
                "public sealed class Sample(string name)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForSource(source);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.UnknownParamTag.ID);

            Finding finding = findings.Single();

            Assert.Equal("param", finding.TagName);
            Assert.Equal("Class", finding.Context.OwnerKind);
            Assert.Equal("Parameter", finding.Context.SubjectKind);
            Assert.Equal("ghost", finding.Context.TargetName);
            Assert.Equal("<param> references unknown parameter 'ghost'.", finding.Message);
        }

        /// <summary>
        /// Ensures duplicate param documentation is detected on struct primary constructors.
        /// </summary>
        [Fact]
        public void DuplicatePrimaryConstructorParamTag_OnStruct_IsDetected()
        {
            string source =
                "/// <summary>Represents a struct with a primary constructor.</summary>\n" +
                "/// <param name=\"name\">First.</param>\n" +
                "/// <param name=\"name\">Second.</param>\n" +
                "public readonly struct Sample(string name)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForSource(source);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.DuplicateParamTag.ID);

            Finding finding = findings.Single();

            Assert.Equal("param", finding.TagName);
            Assert.Equal("Struct", finding.Context.OwnerKind);
            Assert.Equal("Parameter", finding.Context.SubjectKind);
            Assert.Equal("name", finding.Context.TargetName);
            Assert.Equal("Duplicate <param> documentation for parameter 'name'.", finding.Message);
        }

        /// <summary>
        /// Ensures param order mismatch is detected on class primary constructors.
        /// </summary>
        [Fact]
        public void ParamOrderMismatch_OnClassPrimaryConstructor_IsDetected()
        {
            string source =
                "/// <summary>Represents a class with a primary constructor.</summary>\n" +
                "/// <param name=\"c\">The third parameter.</param>\n" +
                "/// <param name=\"b\">The second parameter.</param>\n" +
                "/// <param name=\"a\">The first parameter.</param>\n" +
                "public sealed class Sample(int a, int b, int c)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForSource(source);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ParamOrderMismatch.ID);

            Finding finding = findings.Single();

            Assert.Equal("param", finding.TagName);
            Assert.Equal("Class", finding.Context.OwnerKind);
            Assert.Equal("ParameterTag", finding.Context.SubjectKind);
            Assert.Null(finding.Context.TargetName);
            Assert.Equal("<param> tags should follow the declaration parameter order.", finding.Message);
        }

        /// <summary>
        /// Ensures unknown paramref references are detected on class primary constructors.
        /// </summary>
        [Fact]
        public void UnknownParamRef_OnClassPrimaryConstructor_IsDetected()
        {
            string source =
                "/// <summary>Uses <paramref name=\"ghost\"/>.</summary>\n" +
                "/// <param name=\"name\">The name.</param>\n" +
                "public sealed class Sample(string name)\n" +
                "{\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForSource(source);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.UnknownParamRef.ID);

            Finding finding = findings.Single();

            Assert.Equal("paramref", finding.TagName);
            Assert.Equal("Class", finding.Context.OwnerKind);
            Assert.Equal("ParamRefTag", finding.Context.SubjectKind);
            Assert.Equal("ghost", finding.Context.TargetName);
            Assert.Equal("<paramref> references unknown parameter 'ghost'.", finding.Message);
        }
    }
}
