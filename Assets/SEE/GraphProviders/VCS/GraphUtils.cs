using System;
using System.IO;
using System.Linq;
using SEE.DataModel.DG;

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
        /// Recursive algorithm to add a file with the path <paramref name="fullRelativePath"/>
        /// to the graph <paramref name="graph"/>.
        ///
        /// This method will also add all directories in between.
        ///
        /// Files will have the node type <see cref="DataModel.DG.VCS.FileType"/>.
        /// Their <see cref="GraphElement.Filename"/>  and <see cref="GraphElement.Directory"/>
        /// will be set to the full relative path of the file so that the files can be
        /// opened in the CodeEditor.
        ///
        /// Directories will have the node type <see cref="DataModel.DG.VCS.DirectoryType"/>.
        /// </summary>
        /// <param name="fullRelativePath">The full relative path of the file.
        /// This will become the ID of the newly created node.</param>
        /// <param name="rootNode">The root node of the repository.</param>
        /// <param name="graph">The graph to add the nodes to.</param>
        /// <returns>The found or newly create file node</returns>
        public static Node GetOrAddFileNode(string fullRelativePath, Node rootNode, Graph graph, string idSuffix = "") =>
            GetOrAddFileNode(fullRelativePath, fullRelativePath, rootNode, graph, idSuffix: idSuffix);

        /// <summary>
        /// The same as <see cref="GetOrAddFileNode"/> but with the actual logic.
        /// </summary>
        /// <param name="fullRelativePath">The full relative path of the file.</param>
        /// <param name="path">The root node of the repository.</param>
        /// <param name="parent">The parent of the current node.</param>
        /// <param name="graph">The graph to add the nodes to.</param>
        /// <returns>The newly created or found node.</returns>
        private static Node GetOrAddFileNode
            (string fullRelativePath,
            string path,
            Node parent,
            Graph graph,
            string idSuffix = "")
        {
            string[] pathSegments = path.Split(Path.AltDirectorySeparatorChar);
            // If we are in the directory of the file.
            if (pathSegments.Length == 1)
            {
                // If the file node exists.
                if (parent.Children().Any(x => x.ID + idSuffix == fullRelativePath))
                {
                    return parent.Children().First(x => x.ID + idSuffix == fullRelativePath);
                }

                string[] fileDirectorySplit = fullRelativePath.Split(Path.AltDirectorySeparatorChar);

                string fileDir = String.Join(Path.AltDirectorySeparatorChar,
                    fileDirectorySplit.Take(fileDirectorySplit.Length - 1));

                // Create a new file node and return it.
                Node addedFileNode = NewNode(graph, fullRelativePath + idSuffix,
                    DataModel.DG.VCS.FileType, path);
                addedFileNode.Filename = path;
                addedFileNode.Directory = fileDir;
                parent.AddChild(addedFileNode);
                return addedFileNode;
            }

            string directoryName = parent.ID + Path.AltDirectorySeparatorChar + pathSegments.First() + idSuffix;

            // If the current Node parent already has the next directory with the name directoryName.
            if (parent.Children().Any(x => x.ID == directoryName))
            {
                Node dirNode = parent.Children().First(x =>
                    x.ID == parent.ID + Path.AltDirectorySeparatorChar + pathSegments.First() + idSuffix);
                return GetOrAddFileNode(fullRelativePath, String.Join(Path.AltDirectorySeparatorChar, pathSegments.Skip(1)),
                    dirNode, graph, idSuffix: idSuffix);
            }

            // Create a new directory node.
            Node addedDirectoryNode = NewNode(graph, directoryName,
                DataModel.DG.VCS.DirectoryType, directoryName);
            addedDirectoryNode.Directory = directoryName;
            parent.AddChild(addedDirectoryNode);
            return GetOrAddFileNode(fullRelativePath, String.Join(Path.AltDirectorySeparatorChar, pathSegments.Skip(1)),
                addedDirectoryNode, graph, idSuffix: idSuffix);
        }
    }
}
