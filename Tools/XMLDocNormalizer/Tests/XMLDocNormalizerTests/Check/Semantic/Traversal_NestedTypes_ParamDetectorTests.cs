using System.Collections.Generic;
using System.Linq;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic
{
    /// <summary>
    /// Traversal tests ensuring that the parameter detector analyzes declarations inside nested types.
    /// </summary>
    public sealed class Traversal_NestedTypes_ParamDetectorTests
    {
        /// <summary>
        /// Ensures that all parameter smells can be triggered inside nested types and that no additional smells are produced.
        /// </summary>
        [Fact]
        public void NestedTypes_TriggersAllParamSmells()
        {
            string source =
                "/// <summary>Outer.</summary>\n" +
                "public class Outer\n" +
                "{\n" +
                "    /// <summary>Inner.</summary>\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>DOC310.</summary>\n" +
                "        public void MissingParamTag(int x) { }\n" + // DOC310
                "\n" +
                "        /// <summary>DOC320.</summary>\n" +
                "        /// <param name=\"x\"></param>\n" +
                "        public void EmptyParamDescription(int x) { }\n" + // DOC320
                "\n" +
                "        /// <summary>DOC330.</summary>\n" +
                "        /// <param name=\"x\">x</param>\n" +
                "        /// <param name=\"ghost\">ghost</param>\n" +
                "        public void UnknownParamTag(int x) { }\n" + // DOC330
                "\n" +
                "        /// <summary>DOC350.</summary>\n" +
                "        /// <param name=\"x\">first</param>\n" +
                "        /// <param name=\"x\">second</param>\n" +
                "        public void DuplicateParamTag(int x) { }\n" + // DOC350
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForSource(source);

            FindingAsserts.HasExactlySmells(
                findings,
                XmlDocSmells.MissingParamTag.Id,
                XmlDocSmells.EmptyParamDescription.Id,
                XmlDocSmells.UnknownParamTag.Id,
                XmlDocSmells.DuplicateParamTag.Id);

            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.MissingParamTag.Id, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.EmptyParamDescription.Id, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.UnknownParamTag.Id, 1);
            FindingAsserts.ContainsSmellTimes(findings, XmlDocSmells.DuplicateParamTag.Id, 1);

            AssertMessageArg0(findings.Single(f => f.Smell.Id == XmlDocSmells.MissingParamTag.Id), "x");
            AssertMessageArg0(findings.Single(f => f.Smell.Id == XmlDocSmells.EmptyParamDescription.Id), "x");
            AssertMessageArg0(findings.Single(f => f.Smell.Id == XmlDocSmells.UnknownParamTag.Id), "ghost");
            AssertMessageArg0(findings.Single(f => f.Smell.Id == XmlDocSmells.DuplicateParamTag.Id), "x");
        }

        /// <summary>
        /// Ensures that nested types with valid parameter documentation produce no parameter findings.
        /// </summary>
        [Fact]
        public void NestedTypes_ProducesNoParamFindings()
        {
            string source =
                "/// <summary>Outer.</summary>\n" +
                "public class Outer\n" +
                "{\n" +
                "    /// <summary>Inner.</summary>\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>Ok.</summary>\n" +
                "        /// <param name=\"x\">x</param>\n" +
                "        public void M(int x) { }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindParamFindingsForSource(source);

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
