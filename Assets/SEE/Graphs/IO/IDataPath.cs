using Cysharp.Threading.Tasks;
using System.IO;

namespace SEE.Graphs.IO
{
    /// <summary>
    /// An abstract representation of a path to data that can be loaded into a graph.
    /// Implementations of this interface should provide the necessary methods to access
    /// and load the data from the specified path.
    /// </summary>
    public interface IDataPath
    {
        /// <summary>
        /// Returns the path to the data as a string. This path can be used to identify
        /// the location of the data source. It may represent a file path, a URL, or any
        /// other form of data location.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Loads the data from the specified path asynchronously and returns a stream containing the data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with a stream containing the data as
        /// its result.</returns>
        UniTask<Stream> LoadAsync();
    }
}
