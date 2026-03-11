using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Builds a semantic analysis context for the selected reporting projects and their
    /// recursively referenced project closure inside the loaded solution.
    /// </summary>
    internal static class ProjectClosureSemanticContextBuilder
    {
        /// <summary>
        /// Builds a new <see cref="ProjectClosureSemanticContext"/> for the given reporting projects.
        /// </summary>
        /// <param name="reportingProjects">The projects for which findings may be reported.</param>
        /// <returns>A semantic context covering the reporting projects and their project-reference closure.</returns>
        public static ProjectClosureSemanticContext Build(
            IReadOnlyCollection<Project> reportingProjects)
        {
            if (reportingProjects.Count == 0)
            {
                throw new ArgumentException("At least one reporting project is required.", nameof(reportingProjects));
            }

            Solution solution = reportingProjects.First().Solution;

            HashSet<ProjectId> reportingProjectIds =
                new(reportingProjects.Select(static project => project.Id));

            HashSet<ProjectId> analysisProjectIds = new();
            Stack<ProjectId> stack = new(reportingProjectIds);

            while (stack.Count > 0)
            {
                ProjectId projectId = stack.Pop();

                if (!analysisProjectIds.Add(projectId))
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
    }
}
