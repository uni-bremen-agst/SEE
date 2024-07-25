using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
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
    /// nodes have authors which are marked by the <see cref="AuthorAttributeName"/> attrinute.
    /// </summary>
    public partial class GraphRenderer
    {
        private const string AuthorAttributeName = "Metric.File.Authors";

        private const string ChurnAttributeName = "Metric.File.Churn";

        /// <summary>
        /// Draws author spheres for the rendered graph and should be executed after the graph was rendered.
        ///
        /// All nodes specified in the keys of <paramref name="nodeMap"/> will be scanned for the
        /// <see cref="AuthorAttributeName"/> attribute which sets the author.
        ///
        /// The collected authors are then rendered as spheres floating over the city.
        /// </summary>
        /// <param name="nodeMap"></param>
        /// <param name="parent"></param>
        public void DrawAuthorSpheres(IDictionary<Node, GameObject> nodeMap, GameObject parent)
        {
            List<string> authors =
                nodeMap.Keys.Where(x => x.Type == "file")
                    .SelectMany(x => x.StringAttributes.Where(y => y.Key == AuthorAttributeName))
                    .SelectMany(x => x.Value.Split(","))
                    .Distinct()
                    .ToList();

            IList<GameObject> gameSpheresObjects = RenderSpheres(authors, parent);

            // Drawing edges
            RenderEdgesForSpheres(nodeMap, gameSpheresObjects, parent);
        }


        /// <summary>
        /// Renders the edges connecting author spheres with the files the author has edited.
        /// </summary>
        /// <param name="nodeMap"></param>
        /// <param name="gameSpheresObjects"></param>
        /// <param name="parent"></param>
        private void RenderEdgesForSpheres(IDictionary<Node, GameObject> nodeMap,
            IEnumerable<GameObject> gameSpheresObjects, GameObject parent)
        {
            IEnumerable<Node> nodesWithChurn = nodeMap.Keys
                .Where(x => x.IntAttributes.ContainsKey(ChurnAttributeName));

            if (!nodesWithChurn.Any())
            {
                return;
            }

            int maximalChurn = nodesWithChurn
                .Max(x => x.IntAttributes[ChurnAttributeName]);


            foreach (var sphere in gameSpheresObjects)
            {
                AuthorSphere authorSphere = sphere.GetComponent<AuthorSphere>();
                var authorName = authorSphere.Author;

                var nodesOfAuthor = nodeMap
                    .Where(x => x.Key.StringAttributes.ContainsKey(AuthorAttributeName))
                    .Where(x =>
                        x.Key.StringAttributes[AuthorAttributeName]
                            .Split(',')
                            .Contains(authorName));

                foreach (var nodeOfAuthor in nodesOfAuthor)
                {
                    BSpline bSpline = CreateSpline(sphere.transform.position, nodeOfAuthor.Value.GetRoofCenter());
                    var churn = nodeOfAuthor.Key.IntAttributes[ChurnAttributeName + ":" + authorName];

                    GameObject gameEdge = new()
                    {
                        tag = Tags.Edge,
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
                    var width = Mathf.Clamp((float)churn / maximalChurn, Settings.EdgeLayoutSettings.EdgeWidth * 0.439f,
                        Settings.EdgeLayoutSettings.EdgeWidth);

                    LineFactory.SetWidth(line, width);

                    line.useWorldSpace = false;

                    SEESpline spline = gameEdge.AddComponent<SEESpline>();
                    spline.Spline = bSpline;
                    spline.GradientColors = (edgeColor, edgeColor);

                    Vector3[] positions = TinySplineInterop.ListToVectors(bSpline.Sample());
                    line.positionCount = positions.Length; // number of vertices
                    line.SetPositions(positions);
                    gameEdge.AddComponent<InteractableObject>();
                    AddLOD(gameEdge);

                    AuthorRef authorRef = nodeOfAuthor.Value.AddOrGetComponent<AuthorRef>();
                    authorRef.AuthorSpheres.Add(sphere);
                    authorRef.Edges.Add((gameEdge, churn));

                    authorSphere.Edges.Add((gameEdge, churn));
                }
            }
        }

        private BSpline CreateSpline(Vector3 start, Vector3 end)
        {
            Vector3[] points = new Vector3[2];
            points[0] = start;
            points[1] = end;
            return new TinySpline.BSpline(2, 3, 1)
            {
                ControlPoints = TinySplineInterop.VectorsToList(points)
            };
        }

        /// <summary>
        /// This method renders all spheres for the authors specified in <paramref name="authors"/>
        /// </summary>
        /// <param name="authors">The authors to create the spheres for</param>
        /// <param name="parent">The parent <see cref="GameObject"/> to add the to</param>
        /// <returns>A list of the generated sphere game objects</returns>
        private IList<GameObject> RenderSpheres(IList<string> authors, GameObject parent)
        {
            IList<GameObject> result = new List<GameObject>();
            Renderer parentRenderer = parent.GetComponent<Renderer>();
            int authorsCount = authors.Count;

            int rows = Mathf.FloorToInt(Mathf.Sqrt(authorsCount));
            int columns = Mathf.CeilToInt((float)authorsCount / rows);
            float spacingZ = (parentRenderer.bounds.size.z / (columns - 1));
            float spacingX = (parentRenderer.bounds.size.x / (rows - 1));

            // When we only have one row
            if (float.IsInfinity(spacingX) || float.IsNaN(spacingX))
            {
                spacingX = parentRenderer.bounds.size.x;
            }
            // When we only have one column
            if (float.IsInfinity(spacingZ) || float.IsNaN(spacingZ))
            {
                spacingZ = parentRenderer.bounds.size.z;
            }


            int counter = 0;
            // Define materials for the spheres
            var materials = new Materials(Materials.ShaderType.PortalFree,
                new ColorRange(Color.red, Color.blue, (uint)authorsCount + 1));

            // iterate over all rows
            for (int i = 0; i < rows; i++)
            {
                // iterate over all columns
                for (int j = 0; j < columns; j++)
                {
                    if (counter >= authorsCount)
                    {
                        return result;
                    }


                    GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    gameObject.name = "AuthorSphere:" + authors[counter];

                    AuthorSphere author = gameObject.AddComponent<AuthorSphere>();
                    author.Author = authors[counter];

                    gameObject.AddComponent<InteractableObject>();
                    gameObject.AddComponent<ShowHovering>();

                    Vector3 startLabelPosition = gameObject.GetTop();
                    float fontSize = 2f;

                    GameObject nodeLabel = new GameObject("Text " + authors[counter])
                    {
                        tag = Tags.Text
                    };
                    nodeLabel.transform.position = startLabelPosition;

                    TextMeshPro tm = nodeLabel.AddComponent<TextMeshPro>();
                    tm.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    tm.fontSize = fontSize;
                    tm.text = authors[counter];
                    tm.color = Color.white;
                    tm.alignment = TextAlignmentOptions.Center;

                    nodeLabel.name = "Label:" + authors[counter];
                    nodeLabel.AddComponent<FaceCamera>();
                    nodeLabel.transform.SetParent(gameObject.transform);

                    AddLOD(gameObject);

                    Renderer renderer = gameObject.GetComponent<Renderer>();
                    var mat = materials.Get(0, counter);
                    // Override shader
                    mat.shader = Shader.Find("Standard");
                    renderer.sharedMaterial = mat;
                    gameObject.transform.SetParent(parent.transform);
                    gameObject.transform.transform.localScale *= 0.25f;

                    // Calculate the position of the sphere
                    float xPos = (i * spacingX - (parentRenderer.bounds.size.x / 2));
                    float zPos = (j * spacingZ - (parentRenderer.bounds.size.z / 2));

                    Vector3 spherePosition = new Vector3(xPos, parentRenderer.bounds.size.y + 1.2f, zPos) +
                                             parent.transform.position;
                    gameObject.transform.position = spherePosition;

                    result.Add(gameObject);
                    counter++;
                }
            }

            return result;
        }
    }
}
