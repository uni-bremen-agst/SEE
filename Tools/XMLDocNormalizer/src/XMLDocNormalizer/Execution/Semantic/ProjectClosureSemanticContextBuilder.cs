using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Builds a semantic analysis context for the selected reporting projects and,
    /// depending on the configured mode, their semantic analysis scope.
    /// </summary>
    internal static class ProjectClosureSemanticContextBuilder
    {
        /// <summary>
        /// Builds a new <see cref="ProjectClosureSemanticContext"/> for the given reporting projects.
        /// </summary>
        /// <param name="reportingProjects">The projects for which findings may be reported.</param>
        /// <param name="exceptionAnalysisMode">The configured exception analysis mode.</param>
        /// <returns>
        /// A semantic context covering the reporting projects and the analysis scope required
        /// by the configured exception analysis mode.
        /// </returns>
        public static ProjectClosureSemanticContext Build(
            IReadOnlyCollection<Project> reportingProjects,
            ExceptionAnalysisMode exceptionAnalysisMode)
        {
            if (reportingProjects.Count == 0)
            {
                throw new ArgumentException("At least one reporting project is required.", nameof(reportingProjects));
            }

            Solution solution = reportingProjects.First().Solution;

            HashSet<ProjectId> reportingProjectIds =
                new(reportingProjects.Select(static project => project.Id));

            HashSet<ProjectId> analysisProjectIds = exceptionAnalysisMode switch
            {
                ExceptionAnalysisMode.SolutionTransitive => CollectSolutionClosure(solution, reportingProjectIds),
                _ => new HashSet<ProjectId>(reportingProjectIds)
            };

            Dictionary<ProjectId, Compilation> compilations = new();
            Dictionary<SyntaxTree, ProjectId> syntaxTreeToProjectId = new();

            foreach (ProjectId projectId in analysisProjectIds)
            {
                Project? project = solution.GetProject(projectId);
                if (project == null)
                {
                    continue;
                }

                Compilation? compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
                if (compilation == null)
                {
                    continue;
                }

                compilations[projectId] = compilation;

                foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
                {
                    if (!syntaxTreeToProjectId.ContainsKey(syntaxTree))
                    {
                        syntaxTreeToProjectId.Add(syntaxTree, projectId);
                    }
                }
            }

            return new ProjectClosureSemanticContext(
                reportingProjectIds,
                analysisProjectIds,
                compilations,
                syntaxTreeToProjectId);
        }

        /// <summary>
        /// Collects the recursive project-reference closure for the given root projects.
        /// </summary>
        /// <param name="solution">The solution containing the projects.</param>
        /// <param name="rootProjectIds">The root reporting projects.</param>
        /// <returns>The complete recursive project-reference closure.</returns>
        private static HashSet<ProjectId> CollectSolutionClosure(
            Solution solution,
            HashSet<ProjectId> rootProjectIds)
        {
            HashSet<ProjectId> closure = new();
            Stack<ProjectId> stack = new(rootProjectIds);

            while (stack.Count > 0)
            {
                ProjectId projectId = stack.Pop();

                if (!closure.Add(projectId))
                {
                    continue;
                }

                Project? project = solution.GetProject(projectId);
                if (project == null)
                {
                    continue;
                }

                foreach (ProjectReference reference in project.ProjectReferences)
                {
                    stack.Push(reference.ProjectId);
                }
            }

            return closure;
        }
    }
}
