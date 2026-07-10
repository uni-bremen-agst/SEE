using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the behavioral difference between ProjectTransitive and SolutionTransitive exception analysis.
    /// </summary>
    public sealed class DOC631_632_ProjectVsSolutionTransitiveTests
    {
        /// <summary>
        /// Ensures that a documented exception from a referenced project produces DOC631
        /// in ProjectTransitive mode because the flow cannot be decided within the reporting project.
        /// </summary>
        [Fact]
        public async Task ReferencedProjectException_InProjectTransitiveMode_ProducesDoc631()
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
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively in referenced project.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        ExternalHelper.ThrowingCall();\n" +
                "    }\n" +
                "}\n";

            (Solution solution, Project reportingProject, Document reportingDocument) =
                SolutionTestBuilder.CreateTwoProjectSolution(reportingSource, referencedSource);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContextBuilder.Build(
                    new[] { reportingProject },
                    ExceptionAnalysisMode.ProjectTransitive);

            SyntaxTree tree = (await reportingDocument.GetSyntaxTreeAsync())!;
            Compilation compilation = (await reportingProject.GetCompilationAsync())!;
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            XmlDocOptions options = new()
            {
                ExceptionAnalysisMode = ExceptionAnalysisMode.ProjectTransitive
            };

            List<Finding> findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(
                tree,
                "Reporting.cs",
                semanticModel,
                semanticContext,
                options);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.ExceptionFlowNotDecidable.ID, finding.Smell.ID);
            Assert.Equal("exception", finding.TagName);
        }

        /// <summary>
        /// Ensures that the same referenced-project exception is resolved in SolutionTransitive mode
        /// and therefore produces neither DOC631 nor DOC632.
        /// </summary>
        [Fact]
        public async Task ReferencedProjectException_InSolutionTransitiveMode_IsResolved()
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
                "    /// <exception cref=\"System.InvalidOperationException\">Thrown transitively in referenced project.</exception>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        ExternalHelper.ThrowingCall();\n" +
                "    }\n" +
                "}\n";

            (Solution solution, Project reportingProject, Document reportingDocument) =
                SolutionTestBuilder.CreateTwoProjectSolution(reportingSource, referencedSource);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContextBuilder.Build(
                    new[] { reportingProject },
                    ExceptionAnalysisMode.SolutionTransitive);

            SyntaxTree tree = (await reportingDocument.GetSyntaxTreeAsync())!;
            Compilation compilation = (await reportingProject.GetCompilationAsync())!;
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            XmlDocOptions options = new()
            {
                ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive
            };

            List<Finding> findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(
                tree,
                "Reporting.cs",
                semanticModel,
                semanticContext,
                options);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionFlowNotDecidable.ID);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);
        }
    }
}
