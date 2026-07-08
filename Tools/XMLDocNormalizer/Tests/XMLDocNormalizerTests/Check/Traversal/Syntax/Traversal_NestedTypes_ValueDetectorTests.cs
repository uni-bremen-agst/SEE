using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Traversal.Syntax
{
    /// <summary>
    /// Tests traversal behavior of the value detector across nested and multiple type declarations.
    /// </summary>
    public sealed class Traversal_NestedTypes_ValueDetectorTests
    {
        /// <summary>
        /// Ensures that a readable property inside a nested class is analyzed and triggers DOC800.
        /// </summary>
        [Fact]
        public void NestedClassPropertyWithoutValue_IsDetected()
        {
            string source =
                "namespace TestNamespace\n" +
                "{\n" +
                "    public class Outer\n" +
                "    {\n" +
                "        public class Inner\n" +
                "        {\n" +
                "            /// <summary>Gets the value.</summary>\n" +
                "            public int Count { get; set; }\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            AssertMissingValueTagFinding(finding, "Property", "Count");
        }

        /// <summary>
        /// Ensures that an indexer inside a nested class is analyzed and triggers DOC800.
        /// </summary>
        [Fact]
        public void NestedClassIndexerWithoutValue_IsDetected()
        {
            string source =
                "namespace TestNamespace\n" +
                "{\n" +
                "    public class Outer\n" +
                "    {\n" +
                "        public class Inner\n" +
                "        {\n" +
                "            /// <summary>Gets an item.</summary>\n" +
                "            public int this[int i] => 0;\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            AssertMissingValueTagFinding(finding, "Indexer", "this[]");
        }

        /// <summary>
        /// Ensures that only the invalid property among multiple type declarations produces a finding.
        /// </summary>
        [Fact]
        public void MultipleTypes_OnlyInvalidPropertyProducesFinding()
        {
            string source =
                "namespace TestNamespace\n" +
                "{\n" +
                "    public class First\n" +
                "    {\n" +
                "        /// <summary>Gets the value.</summary>\n" +
                "        /// <value>The current value.</value>\n" +
                "        public int Valid { get; set; }\n" +
                "    }\n" +
                "\n" +
                "    public class Second\n" +
                "    {\n" +
                "        /// <summary>Gets the value.</summary>\n" +
                "        public int MissingValue { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            AssertMissingValueTagFinding(finding, "Property", "MissingValue");
        }

        /// <summary>
        /// Ensures that valid and invalid nested members are analyzed independently.
        /// </summary>
        [Fact]
        public void NestedTypes_ValidAndInvalidMembers_AreHandledIndependently()
        {
            string source =
                "namespace TestNamespace\n" +
                "{\n" +
                "    public class Outer\n" +
                "    {\n" +
                "        /// <summary>Gets the value.</summary>\n" +
                "        /// <value>The current value.</value>\n" +
                "        public int Valid { get; set; }\n" +
                "\n" +
                "        public class Inner\n" +
                "        {\n" +
                "            /// <summary>Gets the value.</summary>\n" +
                "            public int MissingValue { get; set; }\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            AssertMissingValueTagFinding(finding, "Property", "MissingValue");
        }

        /// <summary>
        /// Ensures that multiple invalid members across nested types each produce their own findings.
        /// </summary>
        [Fact]
        public void NestedTypes_MultipleInvalidMembers_AllProduceFindings()
        {
            string source =
                "namespace TestNamespace\n" +
                "{\n" +
                "    public class Outer\n" +
                "    {\n" +
                "        /// <summary>Gets the value.</summary>\n" +
                "        public int FirstMissing { get; set; }\n" +
                "\n" +
                "        public class Inner\n" +
                "        {\n" +
                "            /// <summary>Gets the value.</summary>\n" +
                "            public int SecondMissing { get; set; }\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindValueFindingsForSource(source);

            Assert.Equal(2, findings.Count);
            Assert.All(findings, finding =>
            {
                Assert.Equal(XmlDocSmells.MissingValueTag.ID, finding.Smell.ID);
                Assert.Equal("value", finding.TagName);
                Assert.Equal("Property", finding.Context.OwnerKind);
                Assert.Equal("ValueTag", finding.Context.SubjectKind);
            });

            Assert.Contains(findings, finding => finding.Context.TargetName == "FirstMissing");
            Assert.Contains(findings, finding => finding.Context.TargetName == "SecondMissing");
        }

        /// <summary>
        /// Asserts a missing-value-tag finding.
        /// </summary>
        /// <param name="finding">
        /// The finding to assert.
        /// </param>
        /// <param name="expectedOwnerKind">
        /// The expected owner kind.
        /// </param>
        /// <param name="expectedTargetName">
        /// The expected target name.
        /// </param>
        private static void AssertMissingValueTagFinding(
            Finding finding,
            string expectedOwnerKind,
            string expectedTargetName)
        {
            Assert.Equal(XmlDocSmells.MissingValueTag.ID, finding.Smell.ID);
            Assert.Equal("value", finding.TagName);
            Assert.Equal(expectedOwnerKind, finding.Context.OwnerKind);
            Assert.Equal("ValueTag", finding.Context.SubjectKind);
            Assert.Equal(expectedTargetName, finding.Context.TargetName);
        }
    }
}
