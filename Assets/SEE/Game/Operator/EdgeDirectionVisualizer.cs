using System.Linq;
using MoreLinq;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Operator
{
    /// <summary>
    /// Provides the <see cref="EdgeDirectionVisualizer"/> to the <see cref="EdgeOperator"/> .
    /// </summary>
    public partial class EdgeOperator : GraphElementOperator<(Color start, Color end)>
    {
        /// <summary>
        /// Implements a data flow visualization to indicate the direction of an edge.
        /// </summary>
        private class EdgeDirectionVisualizer : MonoBehaviour
        {
            /// <summary>
            /// Maximal count of particles.
            /// </summary>
            private const int maxParticleCount = 12;
            /// <summary>
            /// Minimal distance between particles for the actual particle count.
            /// </summary>
            private const float minParticleDistance = 0.16f;
            /// <summary>
            /// Scale of the particle meshes.
            /// </summary>
            private static readonly Vector3 particleScale = new(0.012f, 0.012f, 0.012f);
            /// <summary>
            /// Color of the particle material.
            /// </summary>
            private static readonly Color particleColor = new(0.06f, 0.81f, 1.0f, 1.0f);
            /// <summary>
            /// Particle speed.
            /// </summary>
            private const float particleSpeed = 50f;

            /// <summary>
            /// The spline the edge is based on.
            /// </summary>
            private SEESpline seeSpline;
            /// <summary>
            /// The coordinates of the edge's vertices.
            /// </summary>
            private Vector3[] vertices;

            /// <summary>
            /// The actual particle count as calculated based on <see cref="minParticleDistance"/>
            /// and capped by <see cref="maxParticleCount"/>.
            /// </summary>
            private int particleCount;
            /// <summary>
            /// The particle game objects.
            /// </summary>
            private GameObject[] particles;
            /// <summary>
            /// The current position of the particles.
            /// </summary>
            private float[] particlePositions;

            /// <summary>
            /// Destroys the particles when the component is destroyed.
            /// </summary>
            private void OnDestroy()
            {
                foreach (GameObject particle in particles)
                {
                    seeSpline.OnRendererChanged -= OnSplineChanged;
                    Destroyer.Destroy(particle);
                }
            }

            /// <summary>
            /// Initializes the particles and fields.
            /// </summary>
            public void Start()
            {
                seeSpline = GetComponent<SEESpline>();
                seeSpline.OnRendererChanged += OnSplineChanged;
                vertices = seeSpline.GenerateVertices();

                particleCount = (int)Mathf.Max(Mathf.Min(GetApproxEdgeLength() / minParticleDistance, maxParticleCount), 1);

                particles = new GameObject[particleCount];
                particlePositions = new float[particleCount];

                float separation = vertices.Length / (float)particleCount;
                for (int i = 0; i < particleCount; i++)
                {
                    particles[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    particles[i].GetComponent<Renderer>().material.color = particleColor;
                    particlePositions[i] = separation * i;
                    particles[i].transform.localScale = particleScale;
                    particles[i].transform.SetParent(transform);
                    particles[i].transform.localPosition = GetPositionOnEdge(particlePositions[i]);
                }
            }

            /// <summary>
            /// Updates the position and color of the vertices.
            /// </summary>
            private void Update()
            {
                for (int i = 0; i < particleCount; i++)
                {
                    particlePositions[i] += particleSpeed * Time.deltaTime;
                    if (particlePositions[i] >= vertices.Length)
                    {
                        particlePositions[i] = 0;
                    }
                    particles[i].transform.localPosition = GetPositionOnEdge(particlePositions[i]);
                }
            }

            /// <summary>
            /// This callback is triggered whenever the spline has changed.
            /// It will then re-calculate <see cref="vertices"/>.
            /// </summary>
            private void OnSplineChanged()
            {
                vertices = seeSpline.GenerateVertices();
            }

            /// <summary>
            /// Calculates the coordinate of the position on the edge by interpolating between two
            /// neighboring vertices.
            /// <para>
            /// The vertices of the edge are derived from the integer places of <paramref name="position"/>,
            /// while the decimal places represent the progress between the two vertices.
            /// </para>
            /// </summary>
            /// <param name="position">The position on the edge between zero and <see cref="vertices"/>
            /// <c>.Length - 1</c></param>.
            /// <returns>The coordinate of the position on the edge.</returns>
            private Vector3 GetPositionOnEdge(float position)
            {
                if (position >= vertices.Length - 1)
                {
                    return vertices[^1]; // last element
                }

                return Vector3.Lerp(vertices[(int)position], vertices[(int)position + 1], position - (int)position);
            }

            /// <summary>
            /// Calculates the approximate length of the edge that is represented by <see cref="vertices"/>.
            /// </summary>
            /// <returns>Approximate edge length.</returns>
            private float GetApproxEdgeLength() => vertices.Pairwise(Vector3.Distance).Sum();
        }
    }
}
