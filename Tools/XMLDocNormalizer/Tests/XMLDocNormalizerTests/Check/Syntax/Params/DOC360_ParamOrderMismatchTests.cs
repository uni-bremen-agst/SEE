using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Params
{
    /// <summary>
    /// Tests for DOC360 (ParamOrderMismatch): param documentation tags are not ordered like the declaration parameters.
    /// </summary>
    public sealed class DOC360_ParamOrderMismatchTests
    {
        /// <summary>
        /// Provides supported declarations where param documentation tags are ordered differently than declaration parameters.
        /// </summary>
        /// <returns>Test cases containing member code snippets and expected owner kinds.</returns>
        public static IEnumerable<object[]> DeclarationSources()
        {
            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"c\">c</param>\n" +
                "/// <param name=\"b\">b</param>\n" +
                "/// <param name=\"a\">a</param>\n" +
                "public void M(int a, int b, int c) { }\n",
                "Method"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"c\">c</param>\n" +
                "/// <param name=\"b\">b</param>\n" +
                "/// <param name=\"a\">a</param>\n" +
                "public C(int a, int b, int c) { }\n",
                "Constructor"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"c\">c</param>\n" +
                "/// <param name=\"b\">b</param>\n" +
                "/// <param name=\"a\">a</param>\n" +
                "public delegate void D(int a, int b, int c);\n",
                "Delegate"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"c\">c</param>\n" +
                "/// <param name=\"b\">b</param>\n" +
                "/// <param name=\"a\">a</param>\n" +
                "public int this[int a, int b, int c]\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n",
                "Indexer"
            };

            yield return new object[]
            {
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"right\">right</param>\n" +
                "/// <param name=\"left\">left</param>\n" +
                "public static C operator +(C left, C right)\n" +
                "{\n" +
                "    return left;\n" +
                "}\n",
                "Operator"
            };
        }

        /// <summary>
        /// Ensures that param order mismatch is detected once for each supported declaration kind.
        /// </summary>
        /// <param name="memberCode">The member code snippet.</param>
        /// <param name="expectedOwnerKind">The expected owner kind in the finding context.</param>
        [Theory]
        [MemberData(nameof(DeclarationSources))]
        public void ParamOrderMismatch_IsDetected(string memberCode, string expectedOwnerKind)
        {
            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ParamOrderMismatch.ID);

            Finding finding = findings.Single();

            Assert.Equal("param", finding.TagName);
            Assert.Equal("<param> tags should follow the declaration parameter order.", finding.Message);
            Assert.Equal(expectedOwnerKind, finding.Context.OwnerKind);
            Assert.Equal("ParameterTag", finding.Context.SubjectKind);
            Assert.Null(finding.Context.TargetName);
        }

        /// <summary>
        /// Ensures that a method with three parameters in reversed documentation order produces only one DOC360 finding.
        /// </summary>
        [Fact]
        public void ParamOrderMismatch_WithThreeReorderedParameters_ProducesOnlyOneFinding()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"c\">c</param>\n" +
                "/// <param name=\"b\">b</param>\n" +
                "/// <param name=\"a\">a</param>\n" +
                "public void M(int a, int b, int c) { }\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.ParamOrderMismatch.ID, 1);
            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that correctly ordered param tags produce no findings.
        /// </summary>
        [Fact]
        public void CorrectParamOrder_ProducesNoFindings()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"a\">a</param>\n" +
                "/// <param name=\"b\">b</param>\n" +
                "/// <param name=\"c\">c</param>\n" +
                "public void M(int a, int b, int c) { }\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that missing param documentation does not also produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void MissingParamTag_DoesNotTriggerParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"c\">c</param>\n" +
                "/// <param name=\"a\">a</param>\n" +
                "public void M(int a, int b, int c) { }\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSingleSmell(findings, XmlDocSmells.MissingParamTag.ID);
            FindingAsserts.DoesNotContainSmell(findings, XmlDocSmells.ParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that an unknown param tag does not also produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void UnknownParamTag_DoesNotTriggerParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"a\">a</param>\n" +
                "/// <param name=\"ghost\">ghost</param>\n" +
                "/// <param name=\"b\">b</param>\n" +
                "/// <param name=\"c\">c</param>\n" +
                "public void M(int a, int b, int c) { }\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSingleSmell(findings, XmlDocSmells.UnknownParamTag.ID);
            FindingAsserts.DoesNotContainSmell(findings, XmlDocSmells.ParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that duplicate param documentation does not also produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void DuplicateParamTag_DoesNotTriggerParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"b\">b</param>\n" +
                "/// <param name=\"a\">first</param>\n" +
                "/// <param name=\"a\">second</param>\n" +
                "public void M(int a, int b) { }\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            FindingAsserts.ContainsSingleSmell(findings, XmlDocSmells.DuplicateParamTag.ID);
            FindingAsserts.DoesNotContainSmell(findings, XmlDocSmells.ParamOrderMismatch.ID);
        }

        /// <summary>
        /// Ensures that a declaration with only one documented parameter cannot produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void SingleParameter_ProducesNoParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"value\">value</param>\n" +
                "public void M(int value) { }\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that conversion operators with their single required parameter do not produce an order mismatch finding.
        /// </summary>
        [Fact]
        public void ConversionOperator_WithSingleParameter_ProducesNoParamOrderMismatch()
        {
            string memberCode =
                "/// <summary>Test.</summary>\n" +
                "/// <param name=\"value\">value</param>\n" +
                "public static explicit operator int(C value)\n" +
                "{\n" +
                "    return 0;\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForMember(memberCode);

            Assert.Empty(findings);
        }
    }
}
