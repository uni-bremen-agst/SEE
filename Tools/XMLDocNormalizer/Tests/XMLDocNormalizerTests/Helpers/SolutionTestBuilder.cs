using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Builds small in-memory Roslyn solutions with multiple projects for semantic integration tests.
    /// </summary>
    internal static class SolutionTestBuilder
    {
        /// <summary>
        /// Creates a solution containing two C# projects where the first project references the second.
        /// </summary>
        /// <param name="reportingProjectSource">The source code of the reporting project.</param>
        /// <param name="referencedProjectSource">The source code of the referenced project.</param>
        /// <returns>
        /// A tuple containing the solution, the reporting project, and the first document of the reporting project.
        /// </returns>
        public static (Solution Solution, Project ReportingProject, Document ReportingDocument) CreateTwoProjectSolution(
            string reportingProjectSource,
            string referencedProjectSource)
        {
            AdhocWorkspace workspace = new();
            Solution solution = workspace.CurrentSolution;

            ProjectId reportingProjectId = ProjectId.CreateNewId();
            ProjectId referencedProjectId = ProjectId.CreateNewId();

            DocumentId reportingDocumentId = DocumentId.CreateNewId(reportingProjectId);
            DocumentId referencedDocumentId = DocumentId.CreateNewId(referencedProjectId);

            ProjectInfo referencedProjectInfo = ProjectInfo.Create(
                referencedProjectId,
                VersionStamp.Create(),
                name: "ReferencedProject",
                assemblyName: "ReferencedProject",
                language: LanguageNames.CSharp,
                metadataReferences: MetadataReferences.Default);

            ProjectInfo reportingProjectInfo = ProjectInfo.Create(
                reportingProjectId,
                VersionStamp.Create(),
                name: "ReportingProject",
                assemblyName: "ReportingProject",
                language: LanguageNames.CSharp,
                metadataReferences: MetadataReferences.Default,
                projectReferences: new[]
                {
                    new ProjectReference(referencedProjectId)
                });

            solution = solution.AddProject(referencedProjectInfo);
            solution = solution.AddProject(reportingProjectInfo);

            solution = solution.AddDocument(
                referencedDocumentId,
                "Referenced.cs",
                SourceText.From(referencedProjectSource));

            solution = solution.AddDocument(
                reportingDocumentId,
                "Reporting.cs",
                SourceText.From(reportingProjectSource));

            Project reportingProject = solution.GetProject(reportingProjectId)!;
            Document reportingDocument = solution.GetDocument(reportingDocumentId)!;

            return (solution, reportingProject, reportingDocument);
        }
    }
}
