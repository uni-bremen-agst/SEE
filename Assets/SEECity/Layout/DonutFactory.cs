using SEE.DataModel;
using System;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A factory for Donut charts for inner nodes of the tree in the Ballon layout.
    /// </summary>
    internal class DonutFactory
    {
        /// <summary>
        /// Constructor of DonutFactory specifying the names of the metrics to be visualized.
        /// There must be at least one metric. The first metric will be used for the inner
        /// circle. All remaining metrics will be put on outer donut circle sectors.
        /// 
        /// Precondition: 1 <= metrics.Length <= 13; otherwise an exception will be raised.
        /// </summary>
        /// <param name="metrics">the names of the metrics to be visualized</param>
        public DonutFactory(string[] metrics)
        {
            if (metrics.Length == 0)
            {
                throw new System.Exception("[DonutFactory] number of metrics must be greater than one.");
            }
            // the number of metrics to be put onto the Donut circle sectors.
            int numberOfDonutMetrics = metrics.Length - 1;
            if (numberOfDonutMetrics > c.Length)
            {
                throw new System.Exception("[DonutFactory] number of metrics must not exceed " + (c.Length + 1) + ".");
            }
            this.materials = GetMaterials(numberOfDonutMetrics);
            innerMetric = metrics[0];
            this.metrics = new string[numberOfDonutMetrics];
            Array.Copy(metrics, 1, this.metrics, 0, numberOfDonutMetrics);
        }

        /// <summary>
        /// The name of the metric to be put onto the inner circle.
        /// </summary>
        private readonly string innerMetric;

        /// <summary>
        /// The name of the metrics to be visualized by the Donut circle sectors.
        /// </summary>
        private readonly string[] metrics;

        /// <summary>
        /// The offset from one point of the circle line to its neighboring point defining
        /// a triangle. Its unit is radian. The smaller the value, the closer the donut chart
        /// resembles a circle and the more triangles need to created for the mesh.
        /// </summary>
        private readonly float radianOffset = 0.1f;

        /// <summary>
        /// The materials of the circle sectors.
        /// </summary>
        private readonly Material[] materials;

        /// <summary>
        /// Returns an array of howMany new materials with colors taken from the 
        /// viridis color palette with maximal contrast.
        /// 
        /// Precondition: 2 <= howMany <= 12; otherwise null will be returned.
        /// </summary>
        /// <param name="howMany"></param>
        /// <returns>array of new materials</returns>
        private Material[] GetMaterials(int howMany)
        {
            switch(howMany)
            {
                case 0: return NewMaterials();
                case 1: return NewMaterials(c[0]);
                case 2: return NewMaterials(c[0], c[11]);
                case 3: return NewMaterials(c[0], c[5], c[11]);
                case 4: return NewMaterials(c[0], c[3], c[6], c[11]);
                case 5: return NewMaterials(c[0], c[3], c[5], c[8], c[11]);
                case 6: return NewMaterials(c[0], c[2], c[4], c[6], c[8], c[11]);
                case 7: return NewMaterials(c[0], c[2], c[4], c[6], c[8], c[10], c[11]);
                case 8: return NewMaterials(c[0], c[2], c[4], c[6], c[8], c[9], c[10], c[11]);
                case 9: return NewMaterials(c[0], c[2], c[3], c[4], c[6], c[8], c[9], c[10], c[11]);
                case 10: return NewMaterials(c[0], c[1], c[2], c[3], c[4], c[6], c[8], c[9], c[10], c[11]);
                case 11: return NewMaterials(c[0], c[1], c[2], c[3], c[4], c[6], c[7], c[8], c[9], c[10], c[11]);
                case 12: return NewMaterials(c);
                default: return null; // cannot happen
            }
        }

        /// <summary>
        /// Returns an array of new materials with the given colors.
        /// The length of this array corresponds to the length of the color list.
        /// The first material has the first color in the list and so on.
        /// </summary>
        /// <param name="list">list of colors for the materials</param>
        /// <returns>array of new materials</returns>
        private Material[] NewMaterials(params Color[] list)
        {
            Material[] result = new Material[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                result[i] = NewMaterial(list[i]);
            }
            return result;
        }

        /// <summary>
        /// Returns a new material (shader Standard) with given color.
        /// </summary>
        /// <param name="color">color of the material</param>
        /// <returns>new material</returns>
        private Material NewMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Standard"))
            {
                color = color
            };
            return material;
        }

        // viridis color palette from which we choose the colors of the materials
        private readonly Color[] c = GetPalette();

        /// <summary>
        /// Returns the viridis palette, which offers a wide perceptual range in brightness in blue-yellow
        /// and does not rely as much on red-green contrast and, hence, does generally well for color-blind
        /// people. It does less well under tritanopia (blue-blindness), but this is an extrememly rare form of 
        /// colorblindness. The values here are generated using the R package 'colormap' and the command:
        /// scales::show_col(colormap(nshades = 12), labels = T).
        /// </summary>
        /// <returns>viridis color palette (twelve colors)</returns>
        private static Color[] GetPalette()
        {
            Color[] result = new Color[12];

            result[0] = GetColor("44", "01", "55", "FF");
            result[1] = GetColor("46", "20", "06", "FF");
            result[2] = GetColor("42", "3C", "81", "FF");
            result[3] = GetColor("29", "56", "8B", "FF");

            result[4] = GetColor("2D", "6E", "8E", "FF");
            result[5] = GetColor("25", "85", "8D", "FF");
            result[6] = GetColor("23", "9A", "89", "FF");
            result[7] = GetColor("29", "AE", "80", "FF");

            result[8] = GetColor("53", "C4", "68", "FF");
            result[9] = GetColor("85", "D3", "49", "FF");
            result[10] = GetColor("BD", "DF", "2F", "FF");
            result[11] = GetColor("FD", "E7", "25", "FF");

            return result;
        }

        /// <summary>
        /// Returns the color defined as RGB with alpha (for transparency).
        /// All strings are interpreted as hexidecimal number. They must be
        /// in the range ["00", "FF"].
        /// </summary>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="alpha">transparency ("FF" is completely opaque, "00" is completely transparent).</param>
        /// <returns>color as specified</returns>
        private static Color GetColor(string r, string g, string b, string alpha)
        {
            return new Color(ParseHexString(r) / 255.0f,
                             ParseHexString(g) / 255.0f,
                             ParseHexString(b) / 255.0f,
                             ParseHexString(alpha) / 255.0f);
        }

        /// <summary>
        /// Returns the given hexadecimal number as int value.
        /// </summary>
        /// <param name="hexNumber">encoding of hexidecimal numnber</param>
        /// <returns></returns>
        private static float ParseHexString(string hexNumber)
        {
            int.TryParse(hexNumber, System.Globalization.NumberStyles.HexNumber, null, out int result);
            return result;
        }

        /// <summary>
        /// Draws the donut chart at the given center position with given radius. 
        /// The innerValue must be in the range [0, 1] and will be visualized by 
        /// a linear color gradient between white and block in the inner circle
        /// of the donut chart. The values must not be negative and are used to
        /// specify the proportion of the corresponding outer donut circle sectors.
        /// The resulting game object is a container of the meshes of the donut
        /// circle sectors and the cylinder for the inner circle.
        /// 
        /// values.Length must be the same as the number of metric names - 1 passed to
        /// the constructor.
        /// </summary>
        /// <param name="center">position of the center of the circle</param>
        /// <param name="radius">radius of the circle</param>
        /// <param name="innerValue">the value to be put onto the inner circle</param>
        /// <param name="values">the values to be put onto the outer donut circle sectors</param>
        /// <returns>composite game object containing the inner circle and the outer
        /// circle sectors as children</returns>
        public GameObject DonutChart(Vector3 center,
                                     float radius,
                                     float innerValue,
                                     float[] values)
        {
            GameObject donutChart = new GameObject();
            donutChart.transform.position = center;
            donutChart.tag = Tags.Decoration;
            DonutChart(donutChart, radius, innerValue, values);
            return donutChart;
        }

        /// <summary>
        /// Draws the donut chart at donutChart's position with given radius. 
        /// That is, an inner circle is added depicting innerValue, and
        /// one outer donut circle sectors are added for each other value.
        /// For these elements, new game objects are created and added as
        /// children to donutChart.
        /// 
        /// The innerValue must be in the range [0, 1] and will be visualized by 
        /// a linear color gradient between white and block in the inner circle
        /// of the donut chart. The values must not be negative and are used to
        /// specify the proportion of the corresponding outer donut circle sectors.
        /// values.Length must be the same as the number of metric names - 1 passed to
        /// the constructor.
        /// </summary>
        /// <param name="donutChart">the game object determining the center position and to which the child elements are added</param>
        /// <param name="radius">radius of the circle</param>
        /// <param name="innerValue">the value to be put onto the inner circle</param>
        /// <param name="values">the values to be put onto the outer donut circle sectors</param>
        /// <returns>composite game object containing the inner circle and the outer
        /// circle sectors as children</returns>
        public void DonutChart(GameObject donutChart,
                               float radius,
                               float innerValue,
                               float[] values,
                               float innerScale = 0.75f)
        {
            if (values.Length != materials.Length)
            {
                throw new System.Exception("[DonutChart] expected " + materials.Length + " values; "
                                           + " received " + values.Length + " values.");
            }
            if (innerValue < 0.0f || innerValue > 1.0f)
            {
                throw new System.Exception("[DonutChart] value for inner circle must be in the range [0, 1].");
            }
            if (innerScale < 0.0f || innerScale > 1.0f)
            {
                throw new System.Exception("[DonutChart] value for inner scale must be in the range [0, 1].");
            }

            donutChart.isStatic = true;     
            donutChart.transform.localScale = new Vector3(2.0f * radius, 0.05f, 2.0f * radius);

            if (metrics.Length > 0)
            {
                float sum = 0.0f;
                foreach (float value in values)
                {
                    if (value < 0.0f)
                    {
                        throw new System.Exception("[DonutChart] values must not be negative.");
                    }
                    sum += value;
                }
                if (sum > 0)
                {
                    {
                        float previousRadian = 0.0f;
                        int i = 0;
                        foreach (float value in values)
                        {
                            float newRadian = (value / sum) * 2.0f * Mathf.PI + previousRadian;
                            GameObject child = CreateCircleSector(donutChart.transform.position, radius, previousRadian, newRadian, materials[i]);
                            child.name = metrics[i] + " = " + value;
                            child.tag = Tags.Decoration;
                            child.transform.parent = donutChart.transform;
                            previousRadian = newRadian;
                            i++;
                        }
                    }
                }
            }
            {
                // Add inner circle.
                GameObject innerCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                innerCircle.name = innerMetric + " = " + innerValue;
                innerCircle.tag = Tags.Decoration;
                innerCircle.isStatic = true;
                innerCircle.transform.parent = donutChart.transform;
                innerCircle.transform.localPosition = Vector3.zero;
                innerCircle.transform.localScale = new Vector3(innerScale, 1.0f, innerScale);
                Renderer renderer = innerCircle.GetComponent<Renderer>();
                // We need to create a new material so that we can change its color
                // independently from other cylinders.
                renderer.sharedMaterial = new Material(renderer.sharedMaterial)
                {
                    color = Color.Lerp(Color.white, Color.black, innerValue)
                };
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        /// <summary>
        /// Creates and returns a game object for a outer donut circle sector on the
        /// circle defined by center and radius. The circle sector starts at 
        /// startRadian and ends at endRadian and uses the given material. The
        /// unit of startRadian and endRadian is circle radian.
        /// 0 radian is to the right of the center, pi/2 is above the center,
        /// pi is to left of the center; 3*pi/2 degree is below the center
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="startRadian"></param>
        /// <param name="endRadian"></param>
        /// <param name="material"></param>
        /// <returns>the circle sector</returns>
        private GameObject CreateCircleSector(Vector3 center,
                                              float radius,
                                              float startRadian,
                                              float endRadian,
                                              Material material)
        {
            // the resulting game object for which we create the circle sector as a mesh
            GameObject circleSector = new GameObject
            {
                isStatic = true
            };

            MeshFilter meshFilter = circleSector.AddComponent<MeshFilter>();
            MeshRenderer renderer = circleSector.AddComponent<MeshRenderer>();



            // The circle segment is drawn by a set of consecutive triangles defined by
            // the center point and two additional points on the circle line.

            // The number of points on the circle segment. A pair of neighboring
            // points defines the side of the triangle drawing the circle line.
            // There will be at least L = (endRadian - startRadian) / radianOffset 
            // such triangle sides. In the perfect situation where the last
            // point on the circle is the end point of the circle segment, we
            // need L+1 many points (if there were a single line, we would need
            // two points to define it). There may be rounding errors, however,
            // such that the last point of a line is not the end point of the 
            // circle segment we want to draw. That is why we add yet another
            // smaller triangle to fill this gap. Hence, we need L+2 points.
            int numberOfCirclePoints = (int)((endRadian - startRadian) / radianOffset) + 2;

            // In addition to the vertices on the circle segment, we also have the center point. 
            // In summary, we have L+3 vertices.
            Vector3[] vertices = new Vector3[numberOfCirclePoints + 1]; // +1 because of the center point
            vertices[0] = center;

            // Create the vertices on the circle sector line.
            {
                int i = 1;
                for (float radian = startRadian; radian <= endRadian && i < vertices.Length - 1; radian += radianOffset, i++)
                {
                    float x = radius * Mathf.Cos(radian);
                    float z = radius * Mathf.Sin(radian);
                    vertices[i] = new Vector3(center.x + x, 0.0f, center.z + z);
                }
            }
            // Add the very last vertex on the circle line, which is the point where the
            // circle segment ends. This vertex allows us to add another smaller triangle
            // to close a possible gap.
            {
                float x = radius * Mathf.Cos(endRadian);
                float z = radius * Mathf.Sin(endRadian);
                vertices[vertices.Length - 1] = new Vector3(center.x + x, 0.0f, center.z + z);
            }

            // Create the triangles for the circle sector. 
            // There is one triangle less than the number of vertices on the circle.
            // Each triangle is defined by three points.
            int[] triangles = new int[(numberOfCirclePoints - 1) * 3];
            for (int i = 0; i < numberOfCirclePoints - 1; i++)
            {
                // points of triangle must be unwided clockwise; otherwise the
                // triangle could be seen only from below
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                meshFilter.mesh = new Mesh();
                mesh = meshFilter.sharedMesh;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            return circleSector;
        }
    }
}
