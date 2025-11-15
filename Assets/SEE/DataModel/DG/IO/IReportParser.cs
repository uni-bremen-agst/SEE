using Cysharp.Threading.Tasks;
using System.Threading;
using SEE.Utils.Paths;
using System.IO;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Defines the contract for components that transform raw report files into
    /// <see cref="MetricSchema"/> instances that can be applied to a graph.
    /// </summary>
    public interface IReportParser
    {
        /// <summary>
        /// Initializes parser-internal state (e.g., XML reader settings) so that a
        /// subsequent <see cref="ParseAsync"/> call can run without extra setup.
        /// </summary>
        void Prepare();

        /// <summary>
        /// Parses the report found at <paramref name="path"/> and returns the extracted metrics.
        /// </summary>
        /// <param name="path">data source that describes how to load the report stream</param>
        /// <param name="token">optional cancellation handle for long-running parses</param>
        /// <returns>a populated <see cref="MetricSchema"/> describing the report contents</returns>
        UniTask<MetricSchema> ParseAsync(DataPath path, CancellationToken token = default);
    }
}
