using System.IO;
using SEE.DataModel.DG;

namespace SEE.Utils
{
    public static class GraphUtils
    {
        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">Where to add the node</param>
        /// <param name="id">Unique ID of the new node</param>
        /// <param name="type">Type of the new node</param>
        /// <param name="name">The source name of the node</param>
        /// <param name="commitID">The commitID of the node</param>
        /// <param name="repositoryPath">The repositoryPath of the node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        public static Node NewNode(this Graph graph, string id, string type = "Routine",
            string name = null, string commitID = null, string repositoryPath = null)
        {
            Node result = new()
            {
                SourceName = name,
                ID = id,
                Type = type
            };
            result.Filename = result.SourceName;
            result.Directory = Path.GetDirectoryName(result.ID);
            result.CommitID = commitID;
            result.RepositoryPath = repositoryPath;
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
        /// <param name="commitID">The commitID of the graph</param>
        /// <param name="repositoryPath">The repositoryPath of the graph</param>
        public static Node BuildGraphFromPath(string path, Node parent, string parentPath,
            Graph graph, Node mainNode, string commitID, string repositoryPath)
        {
            string[] pathSegments = path.Split('/');
            string nodePath = string.Join('/', pathSegments, 1,
                pathSegments.Length - 1);

            // Current pathSegment is in the main directory.
            if (parentPath == null)
            {
                Node currentSegmentNode = graph.GetNode(pathSegments[0]);

                // Directory already exists.
                if (currentSegmentNode != null)
                {
                    return BuildGraphFromPath(nodePath, currentSegmentNode, pathSegments[0], graph,
                        mainNode, commitID, repositoryPath);
                }

                // Directory does not exist.
                if (currentSegmentNode == null && pathSegments.Length > 1 && parent == null)
                {
                    mainNode.AddChild(graph.NewNode(pathSegments[0], "directory", pathSegments[0], commitID, repositoryPath));
                    return BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]),
                        pathSegments[0], graph, mainNode, commitID, repositoryPath);
                }
            }

            // Current pathSegment is not in the main directory.
            if (parentPath != null)
            {
                string currentPathSegment = parentPath + '/' + pathSegments[0];
                Node currentPathSegmentNode = graph.GetNode(currentPathSegment);

                // The node for the current pathSegment exists.
                if (currentPathSegmentNode != null)
                {
                    return BuildGraphFromPath(nodePath, currentPathSegmentNode,
                        currentPathSegment, graph, mainNode, commitID, repositoryPath);
                }

                // The node for the current pathSegment does not exist, and the node is a directory.
                if (currentPathSegmentNode == null &&
                    pathSegments.Length > 1)
                {
                    parent.AddChild(graph.NewNode(currentPathSegment, "directory", pathSegments[0], commitID, repositoryPath));
                    return BuildGraphFromPath(nodePath, graph.GetNode(currentPathSegment),
                        currentPathSegment, graph, mainNode, commitID, repositoryPath);
                }

                // The node for the current pathSegment does not exist, and the node is a file.
                if (currentPathSegmentNode == null &&
                    pathSegments.Length == 1)
                {
                    Node addedFileNode = graph.NewNode(currentPathSegment, "file", pathSegments[0], commitID, repositoryPath);
                    parent.AddChild(addedFileNode);
                    return addedFileNode;
                }
            }

            return null;
        }
    }
}
