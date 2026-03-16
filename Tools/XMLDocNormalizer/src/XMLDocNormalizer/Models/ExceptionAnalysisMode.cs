namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Determines how exception documentation analysis is performed.
    /// </summary>
    internal enum ExceptionAnalysisMode
    {
        /// <summary>
        /// Only directly thrown exceptions inside the analyzed member are considered.
        /// </summary>
        Direct,

        /// <summary>
        /// Exceptions are analyzed transitively inside the current project.
        /// </summary>
        ProjectTransitive,

        /// <summary>
        /// Exceptions are analyzed transitively inside the current project,
        /// but only exception types defined in the project itself are considered
        /// for documentation requirements.
        /// </summary>
        ProjectTransitiveProjectExceptions,

        /// <summary>
        /// Exceptions are analyzed transitively across all projects
        /// in the loaded solution closure.
        /// </summary>
        SolutionTransitive
    }
}