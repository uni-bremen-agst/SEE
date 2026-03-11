using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests cross-project exception flow analysis for semantic exception smells.
    /// </summary>
    public sealed class DOC630_CrossProjectExceptionFlowTests
    {
        /// <summary>
        /// Ensures that a documented exception does not trigger DOC630 when it is thrown
        /// transitively from a referenced project contained in the same solution.
        /// </summary>
        [Fact]
        public async Task DocumentedException_ThrownInReferencedProject_IsNotReportedAsDoc630()
        {
            string referencedSource =
                "public static class ExternalHelper\n" +
                "{\n" +
                "    public static void ThrowingCall()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n";

            string reportingSource =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        ExternalHelper.ThrowingCall();\n" +
                "    }\n" +
                "}\n";

            (Solution solution, Project reportingProject, Document reportingDocument) =
                SolutionTestBuilder.CreateTwoProjectSolution(reportingSource, referencedSource);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContextBuilder.Build(new[] { reportingProject });

            SyntaxTree tree = (await reportingDocument.GetSyntaxTreeAsync())!;
            Compilation compilation = (await reportingProject.GetCompilationAsync())!;
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            List<Finding> findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(
                tree,
                "Reporting.cs",
                semanticModel,
                semanticContext);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutDirectThrow.ID);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionFlowNotDecidable.ID);
        }

        /// <summary>
        /// Ensures that an undocumented exception thrown in a referenced project
        /// is reported as DOC610 for the reporting project member.
        /// </summary>
        [Fact]
        public async Task UndocumentedException_ThrownInReferencedProject_IsReportedAsDoc610()
        {
            string referencedSource =
                "public static class ExternalHelper\n" +
                "{\n" +
                "    public static void ThrowingCall()\n" +
                "    {\n" +
                "        throw new System.InvalidOperationException();\n" +
                "    }\n" +
                "}\n";

            string reportingSource =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        ExternalHelper.ThrowingCall();\n" +
                "    }\n" +
                "}\n";

            (Solution solution, Project reportingProject, Document reportingDocument) =
                SolutionTestBuilder.CreateTwoProjectSolution(reportingSource, referencedSource);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContextBuilder.Build(new[] { reportingProject });

            SyntaxTree tree = (await reportingDocument.GetSyntaxTreeAsync())!;
            Compilation compilation = (await reportingProject.GetCompilationAsync())!;
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            List<Finding> findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(
                tree,
                "Reporting.cs",
                semanticModel,
                semanticContext);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.MissingExceptionTag.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }
    }
}
