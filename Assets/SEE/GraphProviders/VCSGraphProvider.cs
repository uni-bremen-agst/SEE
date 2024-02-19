using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using SEE.DataModel.DG;
using LibGit2Sharp;
using System.IO;
using System.Linq;
using SEE.Game.City;

namespace SEE.GraphProviders
{
    public class VCSGraphProvider : MonoBehaviour
    {
        //TODO: Only for test, we need to remove Start() etc. and replace it like in the other GraphProvider.
        // Start is called before the first frame update
        void Start()
        {
            //TODO: Only for test, we need to get the repositoryPath from the user.
            string assetsPfad = Application.dataPath;
            string repositoryPath = System.IO.Path.GetDirectoryName(assetsPfad);
            string[] pathSegments = repositoryPath.Split(Path.DirectorySeparatorChar);
            Graph graph = NewGraph();
            // The main directory.
            NewNode(graph, pathSegments[^1], "directory");
            using (var repo = new Repository(repositoryPath))
            {
                // Get all files using "git ls-files".
                //TODO: I limited the output to 200 for testing, because SEE is huge.
                var files = repo.Index
                    .Select(entry => entry.Path)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .ToList().Take(200);

                // Build the graph structure.
                foreach (var filePath in files.Where(path => !string.IsNullOrEmpty(path)))
                {
                    string[] filePathSegments = filePath.Split(Path.DirectorySeparatorChar);
                    // Files in the main directory.
                    if (filePathSegments.Length == 1)
                    {
                        graph.GetNode(pathSegments[^1]).AddChild(NewNode(graph, filePath, "file"));
                    }
                    // Other directorys/files.
                    else
                    {
                        BuildGraphFromPath(filePath, null, null, graph, graph.GetNode(pathSegments[^1]));
                    }
                }
                //TODO: Only for testing.
                Debug.Log(graph.ToString());
            }

        }
        //TODO: Documentation.
        static void BuildGraphFromPath(string path, Node parent, string parentPath, Graph graph, Node repositoryNode)
        {
            string[] pathSegments = path.Split(Path.DirectorySeparatorChar);
            string nodePath = string.Join(Path.DirectorySeparatorChar.ToString(), pathSegments, 1, pathSegments.Length - 1);
            // Current pathSegment is in the main directory.
            if (parentPath == null)
            {
                // Directory already exists.
                if (graph.GetNode(pathSegments[0]) != null)
                {
                    BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]), pathSegments[0], graph, repositoryNode);
                }
                // Directory does not exist.
                if (graph.GetNode(pathSegments[0]) == null && pathSegments.Length > 1 && parent == null)
                {
                    repositoryNode.AddChild(NewNode(graph, pathSegments[0], "directory"));
                    BuildGraphFromPath(nodePath, graph.GetNode(pathSegments[0]), parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, repositoryNode);
                }
                // I dont know, if this code ever gets used -> I dont know, how to handle empty directorys.
                if (graph.GetNode(pathSegments[0]) == null && pathSegments.Length == 1 && parent == null)
                {
                    repositoryNode.AddChild(NewNode(graph, pathSegments[0], "directory"));
                }
            }
            // Current pathSegment is not in the main directory.
            if (parentPath != null)
            {
                // The node for the current pathSegment exists.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) != null)
                {
                    BuildGraphFromPath(nodePath, graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]), parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, repositoryNode);
                }
                // The node for the current pathSegment does not exist, and the node is a directory.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) == null && pathSegments.Length > 1)
                {
                    parent.AddChild(NewNode(graph, parentPath + Path.DirectorySeparatorChar + pathSegments[0], "directory"));
                    BuildGraphFromPath(nodePath, graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]), parentPath + Path.DirectorySeparatorChar + pathSegments[0], graph, repositoryNode);
                }
                // The node for the current pathSegment does not exist, and the node is file.
                if (graph.GetNode(parentPath + Path.DirectorySeparatorChar + pathSegments[0]) == null && pathSegments.Length == 1)
                {
                    parent.AddChild(NewNode(graph, parentPath + Path.DirectorySeparatorChar + pathSegments[0], "file"));
                }
            }
        }
        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">where to add the node</param>
        /// <param name="id">unique ID of the new node</param>
        /// <param name="type">type of the new node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node NewNode(Graph graph, string id, string type = "Routine",
            string directory = null, string filename = null, int? line = null, int? length = null)
        {
            Node result = new()
            {
                SourceName = id,
                ID = id,
                Type = type,
                Directory = directory,
                Filename = filename,
                SourceLine = line,
                SourceLength = length
            };

            graph.AddNode(result);
            return result;
        }

        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/> as a child of <paramref name="parent"/>.
        /// </summary>
        /// <param name="graph">where to add the node</param>
        /// <param name="parent">the parent of the new node; must not be null</param>
        /// <param name="id">unique ID of the new node</param>
        /// <param name="type">type of the new node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node Child(Graph graph, Node parent, string id, string type = "Routine",
            string directory = null, string filename = null, int? line = null, int? length = null)
        {
            Node child = NewNode(graph, id, type, directory, filename, line, length);
            parent.AddChild(child);
            return child;
        }
        //TODO: Only for testing.
        /// <summary>
        /// Creates a new graph with default basepath and graph name.
        /// </summary>
        /// <param name="viewName">the name of the graph</param>
        /// <param name="basePath">the basepath of the graph for looking up the source code files</param>
        /// <returns>new graph</returns>
        protected static Graph NewGraph(string viewName = "CodeFacts", string basePath = "DUMMYBASEPATH")
        {
            return new Graph(basePath, viewName);
        }
    }
}
