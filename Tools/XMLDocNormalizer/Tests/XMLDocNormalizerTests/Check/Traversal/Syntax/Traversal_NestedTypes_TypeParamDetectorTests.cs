using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Traversal.Syntax
{
    /// <summary>
    /// Traversal tests ensuring that the type parameter detector analyzes declarations inside nested types.
    /// </summary>
    public sealed class Traversal_NestedTypes_TypeParamDetectorTests
    {
        /// <summary>
        /// Ensures that all type parameter smells can be triggered inside nested types and that no additional smells are produced.
        /// </summary>
        [Fact]
        public void NestedTypes_TriggersAllTypeParamSmells()
        {
            string source =
                "/// <summary>Outer.</summary>\n" +
                "public class Outer\n" +
                "{\n" +
                "    /// <summary>Inner.</summary>\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>DOC410.</summary>\n" +
                "        public void MissingTypeParamTag<T>() { }\n" + // DOC410
                "\n" +
                "        /// <summary>DOC420.</summary>\n" +
                "        /// <typeparam name=\"T\"></typeparam>\n" +
                "        public void EmptyTypeParamDescription<T>() { }\n" + // DOC420
                "\n" +
                "        /// <summary>DOC430.</summary>\n" +
                "        /// <typeparam name=\"T\">T</typeparam>\n" +
                "        /// <typeparam name=\"Ghost\">ghost</typeparam>\n" +
                "        public void UnknownTypeParamTag<T>() { }\n" + // DOC430
                "\n" +
                "        /// <summary>DOC450.</summary>\n" +
                "        /// <typeparam name=\"T\">first</typeparam>\n" +
                "        /// <typeparam name=\"T\">second</typeparam>\n" +
                "        public void DuplicateTypeParamTag<T>() { }\n" + // DOC450
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForSource(source);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.MissingTypeParamTag.ID,
                XmlDocSmells.EmptyTypeParamDescription.ID,
                XmlDocSmells.UnknownTypeParamTag.ID,
                XmlDocSmells.DuplicateTypeParamTag.ID);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.MissingTypeParamTag.ID, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.EmptyTypeParamDescription.ID, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.UnknownTypeParamTag.ID, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.DuplicateTypeParamTag.ID, 1);

            AssertMessageArg0(findings.Single(f => f.Smell.ID == XmlDocSmells.MissingTypeParamTag.ID), "T");
            AssertMessageArg0(findings.Single(f => f.Smell.ID == XmlDocSmells.EmptyTypeParamDescription.ID), "T");
            AssertMessageArg0(findings.Single(f => f.Smell.ID == XmlDocSmells.UnknownTypeParamTag.ID), "Ghost");
            AssertMessageArg0(findings.Single(f => f.Smell.ID == XmlDocSmells.DuplicateTypeParamTag.ID), "T");
        }

        /// <summary>
        /// Ensures that nested types with valid type parameter documentation produce no type parameter findings.
        /// </summary>
        [Fact]
        public void NestedTypes_ProducesNoTypeParamFindings()
        {
            string source =
                "/// <summary>Outer.</summary>\n" +
                "public class Outer\n" +
                "{\n" +
                "    /// <summary>Inner.</summary>\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>Ok.</summary>\n" +
                "        /// <typeparam name=\"T\">T</typeparam>\n" +
                "        public void M<T>() { }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindTypeParamFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Asserts that the finding message equals the smell message template formatted with the given argument.
        /// </summary>
        /// <param name="finding">The finding to verify.</param>
        /// <param name="arg0">The expected placeholder argument.</param>
        private static void AssertMessageArg0(Finding finding, string arg0)
        {
            string expected = string.Format(finding.Smell.MessageTemplate, arg0);
            Assert.Equal(expected, finding.Message);
        }
    }
}
