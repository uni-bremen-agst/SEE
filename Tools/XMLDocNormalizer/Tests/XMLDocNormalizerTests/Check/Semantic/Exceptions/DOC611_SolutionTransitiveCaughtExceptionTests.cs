using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests that exceptions from referenced projects do not escape in SolutionTransitive mode
    /// when they are caught in the reporting member.
    /// </summary>
    public sealed class DOC611_SolutionTransitiveCaughtExceptionTests
    {
        /// <summary>
        /// Ensures that a referenced-project exception caught in the reporting method
        /// does not produce DOC611 in SolutionTransitive mode.
        /// </summary>
        [Fact]
        public async Task ReferencedProjectException_CaughtInReportingMethod_IsNotDetected()
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
                "        try\n" +
                "        {\n" +
                "            ExternalHelper.ThrowingCall();\n" +
                "        }\n" +
                "        catch (System.InvalidOperationException)\n" +
                "        {\n" +
                "        }\n" +
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
                finding => finding.Smell.ID == XmlDocSmells.MissingTransitiveExceptionDocumentation.ID);
        }
    }
}
