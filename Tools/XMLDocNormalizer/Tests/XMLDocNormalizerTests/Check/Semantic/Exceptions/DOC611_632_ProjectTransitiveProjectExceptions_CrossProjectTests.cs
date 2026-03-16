using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests cross-project filtering behavior of ProjectTransitiveProjectExceptions mode.
    /// </summary>
    public sealed class DOC611_632_ProjectTransitiveProjectExceptions_CrossProjectTests
    {
        /// <summary>
        /// Ensures that an exception type defined only in a referenced project is not treated
        /// as a relevant project-defined exception in ProjectTransitiveProjectExceptions mode.
        /// </summary>
        [Fact]
        public async Task ReferencedProjectDefinedException_IsIgnoredInProjectTransitiveProjectExceptionsMode()
        {
            string referencedSource =
                "public sealed class ExternalCustomException : System.Exception { }\n" +
                "\n" +
                "public static class ExternalHelper\n" +
                "{\n" +
                "    public static void ThrowingCall()\n" +
                "    {\n" +
                "        throw new ExternalCustomException();\n" +
                "    }\n" +
                "}\n";

            string reportingSource =
                "public class TestClass\n" +
                "{\n" +
                "    /// <summary>Entry point.</summary>\n" +
                "    /// <exception cref=\"ExternalCustomException\">Defined only in referenced project.</exception>\n" +
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
                    ExceptionAnalysisMode.ProjectTransitiveProjectExceptions);

            SyntaxTree tree = (await reportingDocument.GetSyntaxTreeAsync())!;
            Compilation compilation = (await reportingProject.GetCompilationAsync())!;
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            XmlDocOptions options = new()
            {
                ExceptionAnalysisMode = ExceptionAnalysisMode.ProjectTransitiveProjectExceptions
            };

            List<Finding> findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(
                tree,
                "Reporting.cs",
                semanticModel,
                semanticContext,
                options);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionTagWithoutTransitiveThrow.ID);

            Assert.DoesNotContain(
                findings,
                finding => finding.Smell.ID == XmlDocSmells.ExceptionFlowNotDecidable.ID);
        }
    }
}
