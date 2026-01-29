using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Traversal.Syntax
{
    /// <summary>
    /// Traversal tests ensuring that the well-formed detector analyzes XML documentation inside nested types.
    /// </summary>
    public sealed class Traversal_NestedTypes_WellFormedDetectorTests
    {
        /// <summary>
        /// Ensures that all well-formedness smells handled by the detector can be triggered inside nested types.
        /// This test also ensures that no unexpected smell ids are produced.
        /// </summary>
        [Fact]
        public void NestedTypes_TriggersAllWellFormedSmells()
        {
            string source =
                "public class Outer\n" +
                "{\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <foo>bar</foo>\n" + // Unknown tag
                "        public void UnknownTag() { }\n" +
                "\n" +
                "        /// <summary>Missing end tag\n" + // Missing end tag (may produce more than one finding)
                "        public void MissingEndTag() { }\n" +
                "\n" +
                "        /// <summary><paramref name=\"x\">x</paramref></summary>\n" + // paramref not empty
                "        public void ParamRefNotEmpty(int x) { }\n" +
                "\n" +
                "        /// <summary><typeparamref name=\"T\">T</typeparamref></summary>\n" + // typeparamref not empty
                "        public void TypeParamRefNotEmpty<T>() { }\n" +
                "\n" +
                "        /// <param>Missing name</param>\n" + // param missing name
                "        public void ParamMissingName(int x) { }\n" +
                "\n" +
                "        /// <typeparam>Missing name</typeparam>\n" + // typeparam missing name (DOC400)
                "        public void TypeParamMissingName<T>() { }\n" +
                "\n" +
                "        /// <exception>Missing cref</exception>\n" + // exception missing cref
                "        public void ExceptionMissingCref() { }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForSource(source);

            string unknownTagId = XmlDocSmells.UnknownTag.Id;
            string missingEndTagId = XmlDocSmells.MissingEndTag.Id;
            string paramRefNotEmptyId = XmlDocSmells.ParamRefNotEmpty.Id;
            string typeParamRefNotEmptyId = XmlDocSmells.TypeParamRefNotEmpty.Id;
            string paramMissingNameId = XmlDocSmells.ParamMissingName.Id;
            string typeParamMissingNameId = XmlDocSmells.TypeParamMissingName.Id;
            string exceptionMissingCrefId = XmlDocSmells.ExceptionMissingCref.Id;

            // Ensure no unexpected smell ids leak in (MissingEndTag might appear multiple times; that's ok).
            FindingAsserts.OnlyContainsSmells(
                findings,
                unknownTagId,
                missingEndTagId,
                paramRefNotEmptyId,
                typeParamRefNotEmptyId,
                paramMissingNameId,
                typeParamMissingNameId,
                exceptionMissingCrefId);

            // Ensure each expected smell occurs at least once.
            FindingAsserts.ContainsSmell(findings, unknownTagId);
            FindingAsserts.ContainsSmell(findings, missingEndTagId);
            FindingAsserts.ContainsSmell(findings, paramRefNotEmptyId);
            FindingAsserts.ContainsSmell(findings, typeParamRefNotEmptyId);
            FindingAsserts.ContainsSmell(findings, paramMissingNameId);
            FindingAsserts.ContainsSmell(findings, typeParamMissingNameId);
            FindingAsserts.ContainsSmell(findings, exceptionMissingCrefId);
        }

        /// <summary>
        /// Ensures that nested types with well-formed XML documentation produce no well-formedness findings.
        /// </summary>
        [Fact]
        public void NestedTypes_ProducesNoWellFormedFindings()
        {
            string source =
                "public class Outer\n" +
                "{\n" +
                "    public class Inner\n" +
                "    {\n" +
                "        /// <summary>Ok <paramref name=\"x\"/> <typeparamref name=\"T\"/>.</summary>\n" +
                "        /// <param name=\"x\">x</param>\n" +
                "        /// <typeparam name=\"T\">T</typeparam>\n" +
                "        /// <exception cref=\"System.InvalidOperationException\">when invalid</exception>\n" +
                "        public void M<T>(int x) { }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindWellFormedFindingsForSource(source);

            Assert.Empty(findings);
        }
    }
}
