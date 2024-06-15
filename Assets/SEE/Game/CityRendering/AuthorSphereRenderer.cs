using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
using SEE.Utils;
using TinySpline;
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
        private string AuthorAttributeName = "Metric.File.Authors";

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

            Renderer parentRenderer = parent.GetComponent<Renderer>();
            IList<GameObject> gameSpheresObjects = RenderSpheres(authors, parent);

            // Drawing edges
            RenderEdgesForSpheres(nodeMap, gameSpheresObjects, parent);
        }


        private void RenderEdgesForSpheres(IDictionary<Node, GameObject> nodeMap,
            IEnumerable<GameObject> gameSpheresObjects, GameObject parent)
        {
            var maxHeight = nodeMap.Values.Max(x => x.transform.position.y);
            float offset = Mathf.Max(2.5f * Settings.EdgeLayoutSettings.EdgeWidth, 0.2f * maxHeight);

            int maximalChurn = nodeMap.Keys
                .Where(x => x.IntAttributes.ContainsKey("Metric.File.Churn"))
                .Max(x => x.IntAttributes["Metric.File.Churn"]);

            foreach (var sphere in gameSpheresObjects)
            {
                NodeRef nodeRef = sphere.GetComponent<NodeRef>();
                var authorName = nodeRef.Value.StringAttributes["Source.Name"];

                var nodesOfAuthor = nodeMap
                    .Where(x => x.Key.StringAttributes.ContainsKey("Metric.File.Authors"))
                    .Where(x =>
                        x.Key.StringAttributes["Metric.File.Authors"]
                            .Split(',')
                            .Contains(authorName));

                foreach (var nodeOfAuthor in nodesOfAuthor)
                {
                    //var bSpline = SplineEdgeLayout.CreateSpline(sphere.transform.position,
                    //    nodeOfAuthor.Value.transform.position,
                    //   above: true, offset);

                    BSpline bSpline = CreateSpline(sphere.transform.position, nodeOfAuthor.Value.transform.position);
                    var churn = nodeOfAuthor.Key.IntAttributes["Metric.File.Churn" + ":" + authorName];

                    GameObject gameEdge = new()
                    {
                        tag = Tags.Edge,
                        isStatic = false,
                        name = authorName + ":" + nodeOfAuthor.Key.ID
                    };
                    gameEdge.transform.parent = parent.transform;


                    // Add a line renderer which serves as a preview in the Unity
                    // editor. The line renderer will be replaced with a mesh
                    // renderer at runtime (i.e., when starting the application).
                    LineRenderer line = gameEdge.AddComponent<LineRenderer>();
                    //gameEdge.AddComponent<MeshRenderer>();
                    // Use sharedMaterial if changes to the original material
                    // should affect all objects using this material;
                    // renderer.material instead will create a copy of the
                    // material and will not be affected by changes of the
                    // original material.
                    Color edgeColor = sphere.GetComponent<Renderer>().sharedMaterial.color;
                    line.sharedMaterial = Materials.New(Materials.ShaderType.Opaque, edgeColor);


                    LineFactory.SetDefaults(line);
                    var width = Mathf.Clamp(churn / maximalChurn, Settings.EdgeLayoutSettings.EdgeWidth * 0.5f,
                        Settings.EdgeLayoutSettings.EdgeWidth);
                    LineFactory.SetWidth(line, width);
                    LineFactory.SetColor(line, edgeColor);

                    // If enabled, the lines are defined in world space. This
                    // means the object's position is ignored and the lines are
                    // rendered around world origin.
                    line.useWorldSpace = false;

                    // Draw spline as concatenation of subsplines (polyline).
                    SEESpline spline = gameEdge.AddComponent<SEESpline>();
                    spline.Spline = bSpline;
                    spline.GradientColors = (edgeColor, edgeColor);
                    //spline.CreateMesh();

                    Vector3[] positions = TinySplineInterop.ListToVectors(bSpline.Sample());
                    line.positionCount = positions.Length; // number of vertices
                    line.SetPositions(positions);

                    InteractionDecorator.PrepareForInteraction(gameEdge);
                    AddLOD(gameEdge);

                    AuthorRef authorRef = nodeOfAuthor.Value.AddOrGetComponent<AuthorRef>();
                    authorRef.AuthorSphere = sphere;
                    authorRef.Edges.Add(gameEdge);
                    // return gameEdge;
                }
            }
        }

        private BSpline CreateSpline(Vector3 start, Vector3 end)
        {
            Vector3[] points = new Vector3[2];
            points[0] = start;
            //points[1] = points[0]; // we are maintaining the x and z co-ordinates,
            //points[1].y = yLevel;   // but adjust the y co-ordinate
            // points[2] = end;
            //points[2].y = yLevel;
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
        /// <returns></returns>
        private IList<GameObject> RenderSpheres(IList<string> authors, GameObject parent)
        {
            IList<GameObject> result = new List<GameObject>();
            Renderer parentRenderer = parent.GetComponent<Renderer>();
            int authorsCount = authors.Count;

            int rows = Mathf.FloorToInt(Mathf.Sqrt(authorsCount));
            int columns = Mathf.CeilToInt((float)authorsCount / rows);
            float spacingZ = (parentRenderer.bounds.size.z / (columns - 1));
            float spacingX = (parentRenderer.bounds.size.x / (rows - 1));

            if (float.IsInfinity(spacingX) || float.IsNaN(spacingX))
            {
                spacingX = parentRenderer.bounds.size.x;
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
                    int sphereIndex = i * rows + j;
                    if (sphereIndex >= authorsCount)
                    {
                        return result;
                    }


                    GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    gameObject.name = "AuthorSphere:" + authors[counter];
                    NodeRef nodeRef = gameObject.AddComponent<NodeRef>();
                    nodeRef.Value = new();
                    nodeRef.Value.StringAttributes.Add("Source.Name", authors[counter]);
                    counter++;
                    InteractionDecorator.PrepareForInteraction(gameObject);

                    AddLOD(gameObject);

                    Renderer renderer = gameObject.GetComponent<Renderer>();
                    var mat = materials.Get(0, counter);
                    // Override shader
                    mat.shader = Shader.Find("Standard");
                    renderer.sharedMaterial = mat;
                    gameObject.transform.parent = parent.transform;
                    gameObject.transform.transform.localScale *= 0.25f;

                    // Calculate the position of the sphere
                    float xPos = (i * spacingX - (parentRenderer.bounds.size.x / 2));
                    float zPos = (j * spacingZ - (parentRenderer.bounds.size.z / 2));

                    Vector3 spherePosition = new Vector3(xPos, parentRenderer.bounds.size.y + 0.9f, zPos) +
                                             parent.transform.position;
                    gameObject.transform.position = spherePosition;

                    result.Add(gameObject);
                }
            }

            return result;
        }
    }
}
