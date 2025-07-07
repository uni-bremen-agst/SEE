using System;
using System.IO;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Utils;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// This utility class can be used to work more easily with graphs.
    /// </summary>
    /// <example>
    /// <para>Filling a graph with nodes representing files</para>
    /// <code>
    ///  Node n = GraphUtils.GetOrAddNode("/path/to/file", rootNode, initialGraph);
    /// </code>
    /// <para>adding a suffix to each node</para>
    /// <code>
    ///  Node n = GraphUtils.GetOrAddNode("/path/to/file", rootNode, initialGraph, "-mySuffix");
    /// </code>
    /// In this case, "-mySuffix" will be added to the ids of all nodes including those representing directories.
    /// </example>
    public static class GraphUtils
    {
        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Where to add the node.</param>
        /// <param name="id">Unique ID of the new node.</param>
        /// <param name="type">Type of the new node.</param>
        /// <param name="name">The source name of the node.</param>
        /// <returns>a new node added to <paramref name="graph"/>.</returns>
        public static Node NewNode(Graph graph, string id, string type, string name = null)
        {
            Node result = new()
            {
                SourceName = name,
                ID = id,
                Type = type,
            };

            graph.AddNode(result);
            return result;
        }

        /// <summary>
        /// Creates or retrieves a file node in the <paramref name="graph"/>
        /// for the given <paramref name="path"/>. The separator used to
        /// split the path into directories is specified by <paramref name="separator"/>.
        ///
        /// Along with the file node, it will also create the necessary directory
        /// containing the file if it does not already exist.
        ///
        /// The file node will be created with the type <see cref="DataModel.DG.VCS.FileType"/>
        /// and directory nodes with the type <see cref="DataModel.DG.VCS.DirectoryType"/>.
        /// </summary>
        /// <param name="graph">Where to look up or add the newly created file node</param>
        /// <param name="path">The path of the file.</param>
        /// <param name="separator">Separates directories in <paramref name="path"/>.</param>
        /// <returns>The existing or newly created file node</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="graph"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null
        /// or only whitespace.</exception>
        internal static Node GetOrAddFileNode(Graph graph, string path, char separator = '/')
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph), "Graph cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }
            if (graph.TryGetNode(path, out Node node))
            {
                return node;
            }
            else
            {
                Node result = NewNode(graph, path, DataModel.DG.VCS.FileType, Filenames.Basename(path, separator));
                Node parent = GetOrAddDirectoryNode(Filenames.GetDirectoryName(path, separator));
                parent?.AddChild(result);
                return result;
            }

            Node GetOrAddDirectoryNode(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null; // No directory needed for root or empty path.
                }
                if (graph.TryGetNode(path, out Node node))
                {
                    return node;
                }
                return NewNode(graph, path, DataModel.DG.VCS.DirectoryType, Filenames.Basename(path, separator));
            }
        }
    }
}
