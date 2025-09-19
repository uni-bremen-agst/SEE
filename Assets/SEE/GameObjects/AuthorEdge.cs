using SEE.GO;
using SEE.Utils;
using Sirenix.OdinInspector;
using TinySpline;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Attribute of an author edge connecting an <see cref="AuthorSphere"/>
    /// to <see cref="AuthorRef"/>.
    /// </summary>
    /// <remarks>This component will be attached to connections between authors and their edited files.</remarks>
    public class AuthorEdge : SerializedMonoBehaviour
    {
        /// <summary>
        /// Reference to the target node this edge connects to.
        /// </summary>
        public AuthorRef FileNode;

        /// <summary>
        /// Reference to the <see cref="GameObjects.AuthorSphere"/> that this edge originates from.
        /// </summary>
        public AuthorSphere AuthorSphere;

        /// <summary>
        /// The width of the edge.
        /// </summary>
        public float Width;

        /// <summary>
        /// Draws the edge between the <see cref="AuthorSphere"/> and the <see cref="FileNode"/>.
        /// </summary>
        internal void Draw()
        {
            LineRenderer line = gameObject.AddOrGetComponent<LineRenderer>();

            Color edgeColor = FileNode.gameObject.GetComponent<Renderer>().sharedMaterial.color;
            Material material = Materials.New(Materials.ShaderType.Opaque, edgeColor);
            material.shader = Shader.Find("Standard");
            line.sharedMaterial = material;

            LineFactory.SetDefaults(line);

            LineFactory.SetWidth(line, Width);

            line.useWorldSpace = false;

            SEESpline spline = gameObject.AddComponent<SEESpline>();
            BSpline bSpline = CreateSpline(AuthorSphere.gameObject.transform.position, FileNode.gameObject.GetRoofCenter());
            spline.Spline = bSpline;
            spline.GradientColors = (edgeColor, edgeColor);

            Vector3[] positions = TinySplineInterop.ListToVectors(bSpline.Sample());
            line.positionCount = positions.Length; // number of vertices
            line.SetPositions(positions);

            return;

            /// <summary>
            /// Creates <see cref="BSpline"/> connecting two points.
            /// </summary>
            /// <param name="start">The start point.</param>
            /// <param name="end">The end point.</param>
            /// <returns>A <see cref="BSpline"/> instance connecting two points.</returns>
            static BSpline CreateSpline(Vector3 start, Vector3 end)
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
}

