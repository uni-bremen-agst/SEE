using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Returns
{
    /// <summary>
    /// Tests for DOC540 (ReturnsOnWriteOnlyProperty): returns documentation is used on a write-only property.
    /// </summary>
    public sealed class DOC540_ReturnsOnWriteOnlyPropertyTests
    {
        /// <summary>
        /// Ensures that returns documentation on a write-only property is detected.
        /// </summary>
        [Fact]
        public void ReturnsOnWriteOnlyProperty_IsDetected()
        {
            string memberCode =
                "/// <summary>Sets the password.</summary>\n" +
                "/// <returns>The password.</returns>\n" +
                "public string Password\n" +
                "{\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ReturnsOnWriteOnlyProperty.ID);

            Finding finding = findings.Single();

            Assert.Equal("returns", finding.TagName);
            Assert.Equal("<returns> must not be used on write-only property 'Password'.", finding.Message);
            Assert.Equal("Property", finding.Context.OwnerKind);
            Assert.Equal("ReturnValue", finding.Context.SubjectKind);
            Assert.Equal("Password", finding.Context.SymbolName);
            Assert.Equal("Password", finding.Context.TargetName);
        }

        /// <summary>
        /// Ensures that multiple returns tags on a write-only property produce only one invalid-target finding.
        /// </summary>
        [Fact]
        public void MultipleReturnsTags_OnWriteOnlyProperty_ProduceOnlyOneFinding()
        {
            string memberCode =
                "/// <summary>Sets the password.</summary>\n" +
                "/// <returns>First.</returns>\n" +
                "/// <returns>Second.</returns>\n" +
                "public string Password\n" +
                "{\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.ReturnsOnWriteOnlyProperty.ID, 1);
            FindingAsserts.HasExactlySmells(findings, XmlDocSmells.ReturnsOnWriteOnlyProperty.ID);
        }

        /// <summary>
        /// Ensures that a write-only property without returns documentation produces no returns findings.
        /// </summary>
        [Fact]
        public void WriteOnlyProperty_WithoutReturns_ProducesNoReturnsFindings()
        {
            string memberCode =
                "/// <summary>Sets the password.</summary>\n" +
                "/// <remarks>The assigned value is stored securely.</remarks>\n" +
                "public string Password\n" +
                "{\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a readable property without returns documentation does not trigger missing returns.
        /// </summary>
        [Fact]
        public void ReadableProperty_WithoutReturns_DoesNotTriggerMissingReturns()
        {
            string memberCode =
                "/// <summary>Gets the count.</summary>\n" +
                "/// <value>The count.</value>\n" +
                "public int Count\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a readable get-set property with returns documentation does not produce DOC540.
        /// </summary>
        [Fact]
        public void ReadableProperty_WithReturns_DoesNotTriggerReturnsOnWriteOnlyProperty()
        {
            string memberCode =
                "/// <summary>Gets or sets the count.</summary>\n" +
                "/// <returns>Not recommended, but not DOC540.</returns>\n" +
                "public int Count\n" +
                "{\n" +
                "    get { return 0; }\n" +
                "    set { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindReturnsFindingsForMember(memberCode);

            FindingAsserts.DoesNotContainSmell(findings, XmlDocSmells.ReturnsOnWriteOnlyProperty.ID);
        }
    }
}
