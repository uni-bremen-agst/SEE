using System;
using SEE.DataModel.DG;
using SEE.Utils;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Simplifies the creation of file and directory nodes in a graph.
    /// </summary>
    public static class GraphUtils
    {
        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// Sets the node's <see cref="Node.ID"/> to <paramref name="path"/>,
        /// its <see cref="GraphElement.Type"/> to <paramref name="type"/>,
        /// its <see cref="GraphElement.Filename"/> and <see cref="Node.SourceName"/>
        /// to the basename of <paramref name="path"/>,
        /// and its <see cref="GraphElement.Filename"/> to the directory part of <paramref name="path"/>.
        /// </summary>
        /// <param name="graph">Where to add the node.</param>
        /// <param name="path">path of the file-system entity, also used as the unique ID of the new node.</param>
        /// <param name="type">Type of the new node.</param>
        /// <param name="separator">Separates directories in <paramref name="path"/>.</param>
        /// <returns>a new node added to <paramref name="graph"/>.</returns>
        private static Node NewNode(Graph graph, string path, string type, char separator)
        {
            string filename = Filenames.Basename(path, separator);
            Node result = new()
            {
                SourceName = filename,
                Filename = filename,
                Directory = Filenames.GetDirectoryName(path, separator),
                ID = path,
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
                Node result = NewNode(graph, path, DataModel.DG.VCS.FileType, separator);
                Node parent = GetOrAddDirectoryNode(Filenames.GetDirectoryName(path, separator));
                parent?.AddChild(result);
                return result;
            }

            // Returns the parent directory node for given path. If none exists,
            // the parent directory node will be created (including all its
            // non-existing ancestors.
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
                return NewNode(graph, path, DataModel.DG.VCS.DirectoryType, separator);
            }
        }
    }
}
