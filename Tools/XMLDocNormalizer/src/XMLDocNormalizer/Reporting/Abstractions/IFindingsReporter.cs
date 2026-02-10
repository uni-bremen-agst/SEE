using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Reporting.Abstractions
{
    /// <summary>
    /// Defines an output mechanism for tool findings.
    /// </summary>
    internal interface IFindingsReporter
    {
        /// <summary>
        /// Reports findings produced for a single file.
        /// </summary>
        /// <param name="filePath">The analyzed source file path.</param>
        /// <param name="findings">The findings for the file.</param>
        void ReportFile(string filePath, IReadOnlyList<Finding> findings);

        /// <summary>
        /// Finalizes reporting and flushes any buffered output.
        /// </summary>
        void Complete();
    }
}
