using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
using SEE.GraphProviders.VCS;
using SEE.Utils;
using TinySpline;
using TMPro;
using UnityEngine;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Implements the methods for rendering author spheres in a branch city.
    ///
    /// This functionality is primarily needed for a <see cref="BranchCity"/> and builds on the precondition that
    /// nodes have authors which are marked by the <see cref="DataModel.DG.VCS.AuthorAttributeName"/> attribute.
    /// </summary>
    public partial class GraphRenderer
    {
        /// <summary>
        /// Draws the author spheres for the rendered graph.
        /// This method should be executed after the graph was rendered.
        ///
        /// All nodes specified in the keys of <paramref name="nodeMap"/> will be scanned for the
        /// <see cref="AuthorAttributeName"/> attribute which sets the author.
        ///
        /// The collected authors are then rendered as spheres floating over the city.
        /// </summary>
        /// <param name="nodeMap">A mapping from the graph nodes to the gameobject.</param>
        /// <param name="parent">Parent <see cref="GameObject"/>. All sphere will be child elements of this object.</param>
        /// <param name="graph">The graph which was rendered.</param>
        public void DrawAuthorSpheres(IDictionary<Node, GameObject> nodeMap, GameObject parent, Graph graph)
        {
            /// Collecting all authors from the file nodes. The authors reside in the string attribute
            /// <see cref="DataModel.DG.VCS.AuthorAttributeName"/> separated by commas.
            List<FileAuthor> authors =
                nodeMap.Keys.Where(x => x.Type == DataModel.DG.VCS.FileType)
                    .SelectMany(x => x.StringAttributes.Where(y => y.Key == DataModel.DG.VCS.AuthorAttributeName))
                    .SelectMany(x => x.Value.Split(","))
                    .Distinct()
                    .Select(x => new FileAuthor(x))
                    .ToList();

            IList<GameObject> gameSpheresObjects = RenderSpheres(authors, parent, graph);
            RenderEdgesForSpheres(nodeMap, gameSpheresObjects, parent);
        }

        /// <summary>
        /// This method renders all spheres for the authors specified in <paramref name="authors"/>.
        /// </summary>
        /// <param name="authors">The authors to create the spheres for.</param>
        /// <param name="parent">The parent <see cref="GameObject"/> to add the author game objects to.</param>
        /// <param name="graph">The graph which was rendered.</param>
        /// <returns>A list of the generated sphere game objects.</returns>
        private IList<GameObject> RenderSpheres(IList<FileAuthor> authors, GameObject parent, Graph graph)
        {
            IList<GameObject> result = new List<GameObject>();
            Renderer parentRenderer = parent.GetComponent<Renderer>();
            int authorsCount = authors.Count;
            Node rootNode = graph.GetRoots().First();

            // Calculating number of rows and columns needed and the space between the spheres.
            // The spheres will be distributed in a rectangle around the code city table.
            int rows = Mathf.FloorToInt(Mathf.Sqrt(authorsCount));
            int columns = Mathf.CeilToInt((float)authorsCount / rows);
            float spacingZ = (parentRenderer.bounds.size.z / (columns - 1));
            float spacingX = (parentRenderer.bounds.size.x / (rows - 1));

            // When we only have one row.
            if (float.IsInfinity(spacingX) || float.IsNaN(spacingX))
            {
                spacingX = parentRenderer.bounds.size.x;
            }
            // When we only have one column.
            if (float.IsInfinity(spacingZ) || float.IsNaN(spacingZ))
            {
                spacingZ = parentRenderer.bounds.size.z;
            }

            int currentAuthor = 0;
            // Define materials for the spheres.
            Materials materials = new(Materials.ShaderType.PortalFree,
                new ColorRange(Color.red, Color.blue, (uint)authorsCount + 1));

            // iterate over all rows.
            for (int i = 0; i < rows; i++)
            {
                // iterate over all columns.
                for (int j = 0; j < columns; j++)
                {
                    if (currentAuthor >= authorsCount)
                    {
                        return result;
                    }

                    GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    gameObject.name = "AuthorSphere:" + authors[currentAuthor];

                    AuthorSphere author = gameObject.AddComponent<AuthorSphere>();
                    author.Author = authors[currentAuthor];

                    // FIXME
                    //gameObject.AddComponent<NodeRef>().Value = rootNode;

                    // FIXME
                    //gameObject.AddComponent<InteractableObject>();
                    gameObject.AddComponent<ShowAuthorEdges>();

                    Vector3 startLabelPosition = gameObject.GetTop();

                    AddLabel(authors, parent, currentAuthor, materials, gameObject, startLabelPosition);

                    AddLOD(gameObject);

                    // Calculate the position of the sphere.
                    float xPos = (i * spacingX - (parentRenderer.bounds.size.x / 2));
                    float zPos = (j * spacingZ - (parentRenderer.bounds.size.z / 2));

                    gameObject.transform.position
                        = new Vector3(xPos,
                                      parentRenderer.bounds.size.y + 1.2f, zPos) + parent.transform.position;

                    result.Add(gameObject);
                    currentAuthor++;
                }
            }

            return result;

            // Adds a label with the authors email which will float above the sphere.
            static void AddLabel(IList<FileAuthor> authors, GameObject parent, int currentAuthor, Materials materials,
                                 GameObject gameObject, Vector3 startLabelPosition, float fontSize = 2f)
            {
                GameObject nodeLabel = new("Text " + authors[currentAuthor])
                {
                    tag = Tags.Text
                };
                nodeLabel.transform.position = startLabelPosition;

                TextMeshPro tm = nodeLabel.AddComponent<TextMeshPro>();
                tm.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                tm.fontSize = fontSize;
                tm.text = authors[currentAuthor].ToString();
                tm.color = Color.white;
                tm.alignment = TextAlignmentOptions.Center;

                nodeLabel.name = "Label:" + authors[currentAuthor];
                nodeLabel.AddComponent<FaceCamera>();
                nodeLabel.transform.SetParent(gameObject.transform);

                Renderer renderer = gameObject.GetComponent<Renderer>();
                Material mat = materials.Get(0, currentAuthor);
                // Override shader so the spheres don't clip over the code city.
                mat.shader = Shader.Find("Standard");
                renderer.sharedMaterial = mat;
                gameObject.transform.SetParent(parent.transform);
                gameObject.transform.transform.localScale *= 0.25f;
            }
        }

        /// <summary>
        /// This method renders the edges connecting the author spheres with their corresponding files.
        ///
        /// This method should be called after the sphere where rendered.
        /// </summary>
        /// <param name="nodeMap">A mapping from the graph nodes to the gameobject.</param>
        /// <param name="spheresObjects">A list of the previously rendered spheres.</param>
        /// <param name="parent">Parent <see cref="GameObject"/>. All edges will be child elements of this object.</param>
        private void RenderEdgesForSpheres(IDictionary<Node, GameObject> nodeMap,
            IEnumerable<GameObject> spheresObjects, GameObject parent)
        {
            IEnumerable<Node> nodesWithChurn = nodeMap.Keys
                .Where(x => x.IntAttributes.ContainsKey(DataModel.DG.VCS.Churn));

            if (!nodesWithChurn.Any())
            {
                return;
            }

            int maximalChurn = nodesWithChurn
                .Max(x => x.IntAttributes[DataModel.DG.VCS.Churn]);

            foreach (GameObject sphere in spheresObjects)
            {
                AuthorSphere authorSphere = sphere.GetComponent<AuthorSphere>();
                FileAuthor authorName = authorSphere.Author;

                IEnumerable<KeyValuePair<Node, GameObject>> nodesOfAuthor = nodeMap
                    .Where(x => x.Key.StringAttributes.ContainsKey(DataModel.DG.VCS.AuthorAttributeName))
                    .Where(x =>
                        x.Key.StringAttributes[DataModel.DG.VCS.AuthorAttributeName]
                            .Split(',')
                            .Contains(authorName.ToString()));

                foreach (KeyValuePair<Node, GameObject> nodeOfAuthor in nodesOfAuthor)
                {
                    BSpline bSpline = CreateSpline(sphere.transform.position, nodeOfAuthor.Value.GetRoofCenter());
                    int churn = nodeOfAuthor.Key.IntAttributes[DataModel.DG.VCS.Churn + ":" + authorName];

                    GameObject gameEdge = new()
                    {
                        tag = Tags.Edge,
                        layer = Layers.InteractableGraphObjects,
                        isStatic = false,
                        name = authorName + ":" + nodeOfAuthor.Key.ID
                    };
                    gameEdge.transform.parent = parent.transform;

                    LineRenderer line = gameEdge.AddComponent<LineRenderer>();

                    Color edgeColor = sphere.GetComponent<Renderer>().sharedMaterial.color;
                    Material material = Materials.New(Materials.ShaderType.Opaque, edgeColor);
                    material.shader = Shader.Find("Standard");
                    line.sharedMaterial = material;

                    LineFactory.SetDefaults(line);
                    float width = Mathf.Clamp((float)churn / maximalChurn, Settings.EdgeLayoutSettings.EdgeWidth * 0.439f,
                        Settings.EdgeLayoutSettings.EdgeWidth);

                    LineFactory.SetWidth(line, width);

                    line.useWorldSpace = false;

                    SEESpline spline = gameEdge.AddComponent<SEESpline>();
                    spline.Spline = bSpline;
                    spline.GradientColors = (edgeColor, edgeColor);

                    Vector3[] positions = TinySplineInterop.ListToVectors(bSpline.Sample());
                    line.positionCount = positions.Length; // number of vertices
                    line.SetPositions(positions);

                    AddLOD(gameEdge);

                    AuthorRef authorRef = nodeOfAuthor.Value.AddOrGetComponent<AuthorRef>();
                    authorRef.AuthorSpheres.Add(sphere);
                    authorRef.Edges.Add((gameEdge, churn));

                    AuthorEdge authorEdge = gameEdge.AddComponent<AuthorEdge>();
                    authorEdge.targetNode = authorRef;
                    authorEdge.authorSphere = authorSphere;

                    authorSphere.Edges.Add((gameEdge, churn));

                    if (Settings is BranchCity branchCity)
                    {
                        switch (branchCity.ShowAuthorEdgesStrategy)
                        {
                            case ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors:
                                if (Settings.EdgeLayoutSettings.AnimationKind == EdgeAnimationKind.None)
                                {
                                    throw new System.Exception("If author edges are to be shown on hovering, an edge animation must be activated.");
                                }
                                if (Application.isPlaying)
                                {
                                    // The containing method may be called in the Unity editor, but hiding the edges
                                    // only makes sense when the game is running. Moreover, hiding the edges in the editor
                                    // may even lead to exceptions because the city's BaseAnimationDuration will be queried
                                    // but the city is set only OnEnable of the edge operator.
                                    gameEdge.EdgeOperator().Hide(Settings.EdgeLayoutSettings.AnimationKind, 0f);

                                    if (authorRef.AuthorSpheres.Count >= branchCity.AuthorThreshold)
                                    {
                                        // Show only edges for nodes with multiple authors.
                                        foreach (GameObject edge in authorRef.Edges.Select(x => x.Item1))
                                        {
                                            edge.EdgeOperator().Show(Settings.EdgeLayoutSettings.AnimationKind, 0f);
                                        }
                                    }
                                }
                                break;
                            case ShowAuthorEdgeStrategy.ShowOnHover:
                                if (Settings.EdgeLayoutSettings.AnimationKind == EdgeAnimationKind.None)
                                {
                                    throw new System.Exception("If author edges are to be shown on hovering, an edge animation must be activated.");
                                }
                                if (Application.isPlaying)
                                {
                                    // See above. Must not be run in editor mode.
                                    gameEdge.EdgeOperator().Hide(Settings.EdgeLayoutSettings.AnimationKind, 0f);
                                }
                                break;
                            case ShowAuthorEdgeStrategy.ShowAlways:
                                break; // nothing to do here, edges are always shown
                            default:
                                throw new System.ArgumentOutOfRangeException(nameof(branchCity.ShowAuthorEdgesStrategy),
                                    branchCity.ShowAuthorEdgesStrategy,
                                    $"Unhandled {nameof(ShowAuthorEdgeStrategy)}: {branchCity.ShowAuthorEdgesStrategy}.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates <see cref="BSpline"/> connecting two points.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <returns>A <see cref="BSpline"/> instance connecting two points.</returns>
        private BSpline CreateSpline(Vector3 start, Vector3 end)
        {
            Vector3[] points = new Vector3[2];
            points[0] = start;
            points[1] = end;
            return new BSpline(2, 3, 1)
            {
                ControlPoints = TinySplineInterop.VectorsToList(points)
            };
        }
    }
}
