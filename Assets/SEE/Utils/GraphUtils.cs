using System.IO;
using SEE.DataModel.DG;

namespace SEE.Utils
{
    public class GraphUtils
    {
        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Where to add the node</param>
        /// <param name="id">Unique ID of the new node</param>
        /// <param name="type">Type of the new node</param>
        /// <param name="name">The source name of the node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        public static Node NewNode(Graph graph, string id, string type = "Routine", string name = null)
        {
            Node result = new()
            {
                SourceName = name,
                ID = id,
                Type = type
            };

            graph.AddNode(result);
            return result;
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