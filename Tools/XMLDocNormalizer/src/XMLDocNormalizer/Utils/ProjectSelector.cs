using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Reporting.Logging;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides project selection logic for a loaded solution.
    /// </summary>
    internal static class ProjectSelector
    {
        /// <summary>
        /// Selects the projects that should be analyzed based on
        /// input path and tool options.
        /// </summary>
        /// <param name="solution">The loaded Roslyn solution.</param>
        /// <param name="inputPath">The original user-provided path.</param>
        /// <param name="options">The parsed tool options.</param>
        /// <returns>The projects to analyze.</returns>
        public static IReadOnlyCollection<Project> SelectProjects(
            Solution solution,
            string inputPath,
            ToolOptions options)
        {
            bool isSolution =
                inputPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);

            bool isProject =
                inputPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);

            if (isProject)
            {
                return SelectByPath(solution, inputPath);
            }

            if (!isSolution)
            {
                throw new InvalidOperationException(
                    "Input must be a .sln or .csproj file.");
            }

            if (options.FullAnalysis)
            {
                Logger.InfoVerbose("Full analysis enabled – analyzing all projects.");
                return solution.Projects.ToList();
            }

            if (!string.IsNullOrWhiteSpace(options.ProjectName))
            {
                return SelectByName(solution, options.ProjectName);
            }

            // Default behavior with warning
            Logger.Warn(
                "No --full or --project specified. Defaulting to single project.");

            Project defaultProject =
                solution.Projects
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .First();

            Logger.InfoVerbose($"Default project selected: {defaultProject.Name}");

            return new List<Project> { defaultProject };
        }

        /// <summary>
        /// Selects a project by matching the provided path to the project file paths in the solution.
        /// </summary>
        /// <param name="solution">The loaded Roslyn solution.</param>
        /// <param name="path">The path to the project file.</param>
        /// <returns>The selected project.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the project is not found in the solution.</exception>
        private static IReadOnlyCollection<Project> SelectByPath(
            Solution solution,
            string path)
        {
            string fullPath = Path.GetFullPath(path);

            Project? project =
                solution.Projects
                    .FirstOrDefault(p =>
                        string.Equals(
                            Path.GetFullPath(p.FilePath ?? string.Empty),
                            fullPath,
                            StringComparison.OrdinalIgnoreCase));

            if (project == null)
            {
                throw new InvalidOperationException(
                    $"Project not found in solution: {path}");
            }

            return new List<Project> { project };
        }

        private static IReadOnlyCollection<Project> SelectByName(
            Solution solution,
            string name)
        {
            Project? project =
                solution.Projects
                    .FirstOrDefault(p =>
                        string.Equals(
                            p.Name,
                            name,
                            StringComparison.OrdinalIgnoreCase));

            if (project == null)
            {
                throw new InvalidOperationException(
                    $"Project '{name}' not found in solution.");
            }

            Logger.InfoVerbose($"Analyzing project: {project.Name}");

            return new List<Project> { project };
        }
    }
}