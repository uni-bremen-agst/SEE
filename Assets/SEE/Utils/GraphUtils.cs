using System;
using System.IO;
using System.Linq;
using SEE.DataModel.DG;

namespace SEE.Utils
{
    public class GraphUtils
    {
        private const string FileType = "file";

        private const string DirectoryType = "directory";


        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Where to add the node</param>
        /// <param name="id">Unique ID of the new node</param>
        /// <param name="type">Type of the new node</param>
        /// <param name="name">The source name of the node</param>
        /// <param name="length">The length of the graph element, measured in number of lines</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        public static Node NewNode(Graph graph, string id, string type = "Routine", string name = null,
            int? length = null)
        {
            Node result = new()
            {
                SourceName = name,
                ID = id,
                Type = type,
                //SourceLength = length
            };

            graph.AddNode(result);
            return result;
        }

        /// <summary>
        /// Recursive algorithm to add a file with the path <paramref name="fullRelativePath"/> to the graph <paramref name="g"/>
        ///
        /// This method will also add all directories in between.
        ///
        /// Files will have the node type <see cref="FileType"/>
        /// Diecotries will have the node type <see cref="DirectoryType"/> 
        /// </summary>
        /// <param name="fullRelativePath">The full relative path of the file</param>
        /// <param name="rootNode">The root node of the repository</param>
        /// <param name="g">The graph to add the nodes to</param>
        /// <returns>The found file node</returns>
        public static Node GetOrAddNode(string fullRelativePath, Node rootNode, Graph g) =>
            GetOrAddNode(fullRelativePath, fullRelativePath, rootNode, g);


        /// <summary>
        /// The same as <see cref="GetOrAddNode"/> but with the actual logic.
        /// </summary>
        /// <param name="fullRelativePath">The full relative path of the file</param>
        /// <param name="path">The root node of the repository</param>
        /// <param name="parent">The parent of the current node</param>
        /// <param name="g">The graph to add the nodes to</param>
        /// <returns></returns>
        private static Node GetOrAddNode(string fullRelativePath, string path, Node parent, Graph g)
        {
            string[] pathSegments = path.Split(Path.AltDirectorySeparatorChar);
            // If we are in the directory of the file
            if (pathSegments.Length == 1)
            {
                // If the file node exists
                if (parent.Children().Any(x => x.ID == fullRelativePath))
                {
                    return parent.Children().First(x => x.ID == fullRelativePath);
                }

                // Create a new file node and return it
                Node addedFileNode = NewNode(g, fullRelativePath,
                    FileType, path);
                parent.AddChild(addedFileNode);
                return addedFileNode;
            }

            string directoryName = parent.ID + Path.AltDirectorySeparatorChar + pathSegments.First();

            // If the current Node parent already has the next directory with the name directoryName
            if (parent.Children().Any(x => x.ID == directoryName))
            {
                Node dirNode = parent.Children().First(x =>
                    x.ID == parent.ID + Path.AltDirectorySeparatorChar + pathSegments.First());
                return GetOrAddNode(fullRelativePath, String.Join(Path.AltDirectorySeparatorChar, pathSegments.Skip(1)),
                    dirNode, g);
            }

            // Create a new directory node
            Node addedDirectoryNode = NewNode(g, directoryName,
                DirectoryType, directoryName);
            parent.AddChild(addedDirectoryNode);
            return GetOrAddNode(fullRelativePath, String.Join(Path.AltDirectorySeparatorChar, pathSegments.Skip(1)),
                addedDirectoryNode, g);
        }

        /// <summary>
        /// Creates a new node for each element of a filepath, that does not
        /// already exists in the graph.
        /// </summary>
        /// <param name="path">The remaining part of the path</param>
        /// <param name="parent">The parent node from the current element of the path</param>
        /// <param name="parentPath">The path of the current parent, which will eventually be part of the ID</param>
        /// <param name="graph">The graph to which the new node belongs to</param>
        /// <param name="mainNode">The root node of the main directory</param>
        [Obsolete("Replaced by GetOrAddNode")]
        public static Node BuildGraphFromPath(string path, Node parent, string parentPath, Graph graph, Node mainNode)
        {
            string[] pathSegments = path.Split(Path.AltDirectorySeparatorChar);
            string nodePath = string.Join(Path.AltDirectorySeparatorChar.ToString(), pathSegments, 1,
                pathSegments.Length - 1);
            // Current pathSegment is in the main directory.
            if (parentPath == null)
            {
                // Directory already exists.
                if (graph.GetNode(pathSegments[0]) != null)
                {
                    return BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]), pathSegments[0], graph,
                        mainNode);
                }

                // Directory does not exist.
                if (graph.GetNode(pathSegments[0]) == null && pathSegments.Length > 1 && parent == null)
                {
                    mainNode.AddChild(NewNode(graph, pathSegments[0], "directory", pathSegments[0]));
                    return BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]), pathSegments[0], graph,
                        mainNode);
                }

                // I dont know, if this code ever gets used -> I dont know, how to handle empty directorys.
                if (graph.GetNode(pathSegments[0]) == null && pathSegments.Length == 1 && parent == null)
                {
                    mainNode.AddChild(NewNode(graph, pathSegments[0], "directory", pathSegments[0]));
                    return null;
                }
            }

            // Current pathSegment is not in the main directory.
            if (parentPath != null)
            {
                // The node for the current pathSegment exists.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) != null)
                {
                    return BuildGraphFromPath(nodePath,
                        graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]),
                        parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, mainNode);
                }

                // The node for the current pathSegment does not exist, and the node is a directory.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) == null &&
                    pathSegments.Length > 1)
                {
                    parent.AddChild(NewNode(graph, parentPath + Path.DirectorySeparatorChar + pathSegments[0],
                        "directory", pathSegments[0]));
                    return BuildGraphFromPath(nodePath,
                        graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]),
                        parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, mainNode);
                }

                // The node for the current pathSegment does not exist, and the node is file.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) == null &&
                    pathSegments.Length == 1)
                {
                    Node addedFileNode = NewNode(graph, parentPath + Path.DirectorySeparatorChar + pathSegments[0],
                        "file", pathSegments[0]);
                    parent.AddChild(addedFileNode);
                    return addedFileNode;
                }
            }

            return null;
        }
    }
}