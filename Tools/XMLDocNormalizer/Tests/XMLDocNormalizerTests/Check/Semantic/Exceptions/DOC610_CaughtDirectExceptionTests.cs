using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests that directly thrown exceptions caught inside the same member do not produce DOC610.
    /// </summary>
    public sealed class DOC610_CaughtDirectExceptionTests
    {
        /// <summary>
        /// Ensures that a directly thrown exception caught by a matching catch-clause is not reported.
        /// </summary>
        [Fact]
        public void DirectlyThrownException_CaughtInSameMember_IsNotDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "public void M()\n" +
                "{\n" +
                "    try\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "    catch (System.InvalidOperationException)\n" +
                "    {\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(member, ExceptionAnalysisMode.Direct);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingExceptionTag.ID);
        }

        /// <summary>
        /// Ensures that a directly thrown exception remains relevant when it is rethrown from the catch-clause.
        /// </summary>
        [Fact]
        public void DirectlyThrownException_RethrownFromCatch_IsStillDetected()
        {
            string member =
                "/// <summary>Test.</summary>\n" +
                "public void M()\n" +
                "{\n" +
                "    try\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "    catch (System.InvalidOperationException)\n" +
                "    {\n" +
                "        throw;\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(member, ExceptionAnalysisMode.Direct);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
        }
    }
}
