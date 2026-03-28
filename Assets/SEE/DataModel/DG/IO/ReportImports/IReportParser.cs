using Cysharp.Threading.Tasks;
using System.Threading;
using SEE.Utils.Paths;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Defines the contract for components that transform raw report files into
    /// <see cref="MetricSchema"/> instances that can be applied to a graph.
    /// Preconditions: Implementations must be initialized via <see cref="Prepare"/> before calling <see cref="ParseAsync"/>.
    /// </summary>
    internal interface IReportParser
    {
        /// <summary>
        /// Initializes parser-internal state (for example, XML reader settings) so that a
        /// subsequent <see cref="ParseAsync"/> call can run without extra setup.
        /// Preconditions: Must be called exactly once before the first call to <see cref="ParseAsync"/>.
        /// </summary>
        void Prepare();

        /// <summary>
        /// Parses the report found at <paramref name="path"/> and returns the extracted metrics.
        /// Preconditions: <paramref name="path"/> must not be null.
        /// </summary>
        /// <param name="path">Data source that describes how to load the report stream.</param>
        /// <param name="token">Optional cancellation token for long-running parses.</param>
        /// <returns>A populated <see cref="MetricSchema"/> describing the report contents.</returns>
        UniTask<MetricSchema> ParseAsync(DataPath path, CancellationToken token = default);
    }
}