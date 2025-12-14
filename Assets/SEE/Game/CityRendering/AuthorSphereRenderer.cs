using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GameObjects.BranchCity;
using SEE.GO;
using SEE.GraphProviders.VCS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Implements the methods for rendering author spheres in a branch city.
    ///
    /// This functionality is primarily needed for a <see cref="BranchCity"/> and builds on the precondition that
    /// nodes have authors which are marked by the <see cref="DataModel.DG.VCS.AuthorsAttributeName"/> attribute.
    /// </summary>
    public partial class GraphRenderer
    {
        /// <summary>
        /// Draws the author spheres for the rendered graph.
        /// This method should be executed after the graph was rendered.
        ///
        /// All nodes specified in the keys of <paramref name="nodeMap"/> will be scanned for the
        /// <see cref="DataModel.DG.VCS.AuthorsAttributeName"/> attribute which sets the author.
        ///
        /// The collected authors are then rendered as spheres floating over the rectangular plane
        /// defined by <paramref name="planeCenterposition"/> and <paramref name="planeRectangle"/>.
        /// </summary>
        /// <param name="nodeMap">A mapping from each graph node onto its gameobject (game node).</param>
        /// <param name="parent">Parent <see cref="GameObject"/>. All spheres will become children of this object.</param>
        /// <param name="graph">The graph which was rendered.</param>
        /// <param name="planeCenterposition">The world-space center position of the plane on which the city was rendered.</param>
        /// <param name="planeRectangle">The world-space rectangle of the plane on which the city was rendered.</param>
        private void DrawAuthorSpheres
            (IDictionary<Node, GameObject> nodeMap,
            GameObject parent,
            Graph graph,
            Vector3 planeCenterposition,
            Vector2 planeRectangle)
        {
            IList<GameObject> gameSpheresObjects = RenderAuthors(nodeMap, parent, graph, planeCenterposition, planeRectangle);
            RenderEdges(nodeMap, gameSpheresObjects, parent);
        }

        /// <summary>
        /// This method renders all spheres for the authors specified in <paramref name="authors"/>.
        /// </summary>
        ///  <param name="nodeMap">A mapping from each graph node onto its gameobject (game node).</param>
        /// <param name="parent">The parent <see cref="GameObject"/> to add the author game objects to.</param>
        /// <param name="graph">The graph which was rendered.</param>
        /// <param name="planeCenterposition">The world-space center position of the plane on which the city was rendered.</param>
        /// <param name="planeRectangle">The world-space rectangle of the plane on which the city was rendered.</param>
        /// <returns>A list of the generated sphere game objects.</returns>
        private IList<GameObject> RenderAuthors
            (IDictionary<Node, GameObject> nodeMap,
            GameObject parent,
            Graph graph,
            Vector3 planeCenterposition,
            Vector2 planeRectangle)
        {
            /// Collecting all authors from the file nodes. The authors reside in the string attribute
            /// <see cref="DataModel.DG.VCS.AuthorsAttributeName"/> separated by commas.
            List<FileAuthor> authors =
                nodeMap.Keys.Where(x => x.Type == DataModel.DG.VCS.FileType)
                    .SelectMany(x => x.StringAttributes.Where(y => y.Key == DataModel.DG.VCS.AuthorsAttributeName))
                    .SelectMany(x => x.Value.Split(","))
                    .Distinct()
                    .Select(x => new FileAuthor(x))
                    .ToList();

            IList<GameObject> result = new List<GameObject>();
            int authorsCount = authors.Count;
            Node rootNode = graph.GetRoots().First();

            int currentAuthor = 0;
            // Define materials for the spheres.
            MaterialsFactory materials = new(MaterialsFactory.ShaderType.PortalFree,
                new ColorRange(Color.red, Color.blue, (uint)authorsCount + 1));

            // Position the spheres on the plane above the city at sky level.
            planeCenterposition.y = AbstractSEECity.SkyLevel;
            foreach (Vector3 position in GetEvenlyDistributedPositions(authorsCount, planeRectangle.x, 0, planeRectangle.y))
            {
                GameObject gameObject = AuthorSphere.CreateAuthor(parent, authors[currentAuthor], materials.Get(0, currentAuthor), planeCenterposition + position);
                result.Add(gameObject);
                currentAuthor++;
            }
            return result;
        }

        /// <summary>
        /// Returns the positions of <paramref name="n"/> objects evenly distributed on the border
        /// of a rectangle with width <paramref name="x"/> and depth <paramref name="z"/> at
        /// given <paramref name="height"/>.
        ///
        /// The first position will be at the top-left corner of the rectangle. The positions
        /// will be distributed clockwise. The center of the rectangle is to be interpreted
        /// as (<paramref name="x"/>, <paramref name="height"/>, <paramref name="z"/>). That
        /// means, the first position will be at (-<paramref name="x"/>/2, <paramref name="height"/>,
        /// <paramref name="z"/> / 2).
        /// </summary>
        /// <param name="n">Number of objects</param>
        /// <param name="x">Width of the rectangle</param>
        /// <param name="height">Height of the rectangle. All positions will have this height.</param>
        /// <param name="z">Depth of the rectangle</param>
        /// <returns>Positions on the rectangle</returns>
        public List<Vector3> GetEvenlyDistributedPositions(int n, float x, float height, float z)
        {
            List<Vector3> positions = new(n);

            // Calculate the perimeter and distance between objects
            float perimeter = 2 * (x + z);
            float distanceBetweenObjects = perimeter / n;

            // The starting corner of the rectangle (Top-Left)
            Vector3 startCorner = new(-x / 2, height, z / 2);

            for (int i = 0; i < n; i++)
            {
                Vector3 currentPosition;
                float currentDistance = i * distanceBetweenObjects;

                // Top edge
                if (currentDistance <= x)
                {
                    currentPosition = startCorner + new Vector3(currentDistance, height, 0);
                }
                // Right edge
                else if (currentDistance <= x + z)
                {
                    currentPosition = startCorner + new Vector3(x, height, -(currentDistance - x));
                }
                // Bottom edge
                else if (currentDistance <= 2 * x + z)
                {
                    currentPosition = startCorner + new Vector3(x - (currentDistance - (x + z)), height, -z);
                }
                // Left edge
                else
                {
                    currentPosition = startCorner + new Vector3(0, height, -z + (currentDistance - (2 * x + z)));
                }

                positions.Add(currentPosition);
            }

            return positions;
        }

        /// <summary>
        /// This method renders the edges connecting the author spheres with their corresponding files.
        ///
        /// This method should be called after the sphere where rendered.
        /// </summary>
        /// <param name="nodeMap">A mapping from the graph file nodes to the gameobject.</param>
        /// <param name="spheresObjects">A list of the previously rendered spheres.</param>
        /// <param name="parent">Parent <see cref="GameObject"/>. All edges will be child elements of this object.</param>
        private void RenderEdges
                       (IDictionary<Node, GameObject> nodeMap,
                        IEnumerable<GameObject> spheresObjects,
                        GameObject parent)
        {
            float maximalChurn = MaximalChurn(nodeMap);
            if (maximalChurn == 0)
            {
                // Avoid division by zero below.
                maximalChurn = Settings.EdgeLayoutSettings.EdgeWidth;
            }

            foreach (GameObject sphere in spheresObjects)
            {
                AuthorSphere authorSphere = sphere.GetComponent<AuthorSphere>();
                // The author represented by the current sphere.
                FileAuthor authorName = authorSphere.Author;

                // Collect all (file) nodes the current author has contributed to.
                // Maps from the graph node (a file) onto the game object representing the (file) node.
                // Looks like this can be simplified and optimized.
                IEnumerable<KeyValuePair<Node, GameObject>> filesOfAuthor = nodeMap
                    .Where(x => x.Key.StringAttributes.ContainsKey(DataModel.DG.VCS.AuthorsAttributeName))
                    .Where(x => x.Key.StringAttributes[DataModel.DG.VCS.AuthorsAttributeName]
                                   .Split(',').Contains(authorName.ToString()));

                // For all files of the given author.
                foreach (KeyValuePair<Node, GameObject> fileOfAuthor in filesOfAuthor)
                {
                    // The game object representing the edge between the author and the file.
                    GameObject connectingLine = new()
                    {
                        layer = Layers.InteractableGraphObjects,
                        isStatic = false,
                        name = authorName + ":" + fileOfAuthor.Key.ID
                    };
                    // It will be the child of parent.
                    connectingLine.transform.parent = parent.transform;

                    // Specific churn of the current author for the current sphere.
                    int churn = fileOfAuthor.Key.IntAttributes[DataModel.DG.VCS.Churn + ":" + authorName];

                    AddLOD(connectingLine);

                    // The target of the connectingLine is the file node.
                    // An AuthorRef will be added to a file node. One may exist already
                    // because a different author may have contributed to the same file.
                    AuthorRef authorRef = fileOfAuthor.Value.AddOrGetComponent<AuthorRef>();

                    AuthorEdge authorEdge = connectingLine.AddComponent<AuthorEdge>();
                    authorEdge.FileNode = authorRef;
                    authorEdge.AuthorSphere = authorSphere;
                    authorEdge.Width = Mathf.Clamp((float)churn / maximalChurn,
                                                    Settings.EdgeLayoutSettings.EdgeWidth * 0.439f,
                                                    Settings.EdgeLayoutSettings.EdgeWidth);
                    authorEdge.Draw();

                    authorRef.Add(authorEdge);
                    authorSphere.Edges.Add(authorEdge);
                }
            }
        }

        /// <summary>
        /// Returns the maximal churn value of all nodes in <paramref name="nodeMap"/>.
        /// </summary>
        /// <param name="nodeMap">the nodes to be queried</param>
        /// <returns>maximal churn</returns>
        private float MaximalChurn(IDictionary<Node, GameObject> nodeMap)
        {
            float max = 0;

            foreach (Node node in nodeMap.Keys)
            {
                if (node.TryGetInt(DataModel.DG.VCS.Churn, out int churn))
                {
                    if (churn > max)
                    {
                        max = churn;
                    }
                }
            }
            return max;
        }
    }
}
