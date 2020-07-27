using UnityEngine;

namespace SEE.Controls
{
    internal abstract class PivotBase
    {
        protected const float DefaultPrimaryAlpha = 0.5f;
        protected const float DefaultSecondaryAlpha = 0.5f * DefaultPrimaryAlpha;
    }

    internal abstract class MovePivotBase : PivotBase
    {
        protected const string DefaultShaderName = "Unlit/3DUIShader";
        protected readonly float scale;

        protected MovePivotBase(float scale)
        {
            this.scale = scale;
        }

        internal abstract void Enable(bool enable);
        internal abstract void SetPositions(Vector3 startPoint, Vector3 endPoint);

        protected Material CreateDefaultMaterial(bool primary)
        {
            Shader shader = Shader.Find(DefaultShaderName);
            Material material = null;
            if (shader)
            {
                material = new Material(shader);
                material.SetInt("_ZTest", (int)(primary ? UnityEngine.Rendering.CompareFunction.Greater : UnityEngine.Rendering.CompareFunction.LessEqual));
            }
            else
            {
                Debug.LogWarning("Shader could not be found!");
            }
            return material;
        }

        protected Color CreateDefaultColor(Vector3 startToEnd, bool primary)
        {
            float length = startToEnd.magnitude;
            float f = Mathf.Clamp(length / (0.5f * scale), 0.0f, 1.0f);
            Vector3 startToEndMapped = ((length == 0 ? Vector3.zero : startToEnd / length) * 0.5f + new Vector3(0.5f, 0.5f, 0.5f)) * f;
            Color color = new Color(startToEndMapped.x, startToEndMapped.y, startToEndMapped.z, primary ? DefaultPrimaryAlpha : DefaultSecondaryAlpha);
            return color;
        }
    }

    internal class PointMovePivot : MovePivotBase
    {
        private readonly GameObject[] pivots;

        internal PointMovePivot(float scale) : base(scale)
        {
            Material[] materials = new Material[2]
            {
                CreateDefaultMaterial(true),
                CreateDefaultMaterial(false)
            };

            pivots = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                pivots[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pivots[i].GetComponent<MeshRenderer>().sharedMaterial = materials[i];
                pivots[i].transform.position = Vector3.zero;
                pivots[i].transform.localScale = new Vector3(scale, scale, scale);
                pivots[i].SetActive(false);
            }
        }

        internal override void Enable(bool enable)
        {
            pivots[0].SetActive(enable);
            pivots[1].SetActive(enable);
        }

        internal override void SetPositions(Vector3 startPoint, Vector3 endPoint)
        {
            pivots[0].transform.position = startPoint;
            pivots[1].transform.position = startPoint;
            Vector3 startToEnd = endPoint - startPoint;
            pivots[0].GetComponent<MeshRenderer>().sharedMaterial.color = CreateDefaultColor(startToEnd, true);
            pivots[1].GetComponent<MeshRenderer>().sharedMaterial.color = CreateDefaultColor(startToEnd, false);
        }
    }

    internal class LineMovePivot : MovePivotBase
    {
        private const float GoldenRatio = 1.618034f;

        private readonly GameObject[] starts;
        private readonly GameObject[] ends;
        private readonly GameObject[] mains;

        internal LineMovePivot(float scale) : base(scale)
        {
            starts = new GameObject[2]
            {
                GameObject.CreatePrimitive(PrimitiveType.Sphere),
                GameObject.CreatePrimitive(PrimitiveType.Sphere)
            };
            ends = new GameObject[2]
            {
                GameObject.CreatePrimitive(PrimitiveType.Sphere),
                GameObject.CreatePrimitive(PrimitiveType.Sphere)
            };
            mains = new GameObject[2]
            {
                GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                GameObject.CreatePrimitive(PrimitiveType.Cylinder)
            };

            Material[] materials = new Material[2]
            {
                CreateDefaultMaterial(true),
                CreateDefaultMaterial(false)
            };

            for (int i = 0; i < 2; i++)
            {
                starts[i].GetComponent<MeshRenderer>().sharedMaterial = materials[i];
                ends[i].GetComponent<MeshRenderer>().sharedMaterial = materials[i];
                mains[i].GetComponent<MeshRenderer>().sharedMaterial = materials[i];

                starts[i].transform.position = Vector3.zero;
                ends[i].transform.position = Vector3.zero;
                mains[i].transform.position = Vector3.zero;

                starts[i].transform.localScale = new Vector3(scale, scale, scale);
                ends[i].transform.localScale = new Vector3(scale, scale, scale);
                mains[i].transform.localScale = new Vector3(scale, scale, scale) / GoldenRatio;

                starts[i].SetActive(false);
                ends[i].SetActive(false);
                mains[i].SetActive(false);
            }
        }

        internal override void Enable(bool enable)
        {
            for (int i = 0; i < 2; i++)
            {
                starts[i].SetActive(enable);
                ends[i].SetActive(enable);
                mains[i].SetActive(enable);
            }
        }

        internal override void SetPositions(Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 startToEnd = endPoint - startPoint;
            Color color0 = CreateDefaultColor(startToEnd, true);
            Color color1 = CreateDefaultColor(startToEnd, false);

            for (int i = 0; i < 2; i++)
            {
                starts[i].transform.up = startToEnd;
                ends[i].transform.up = startToEnd;
                mains[i].transform.up = startToEnd;

                starts[i].transform.position = startPoint;
                ends[i].transform.position = endPoint;
                mains[i].transform.position = (startPoint + endPoint) / 2.0f;
                mains[i].transform.localScale = new Vector3(scale / GoldenRatio, 0.5f * startToEnd.magnitude, scale / GoldenRatio);
            }

            starts[0].GetComponent<MeshRenderer>().sharedMaterial.color = color0;
            ends[0].GetComponent<MeshRenderer>().sharedMaterial.color = color0;
            mains[0].GetComponent<MeshRenderer>().sharedMaterial.color = color0;
            starts[1].GetComponent<MeshRenderer>().sharedMaterial.color = color1;
            ends[1].GetComponent<MeshRenderer>().sharedMaterial.color = color1;
            mains[1].GetComponent<MeshRenderer>().sharedMaterial.color = color1;
        }
    }

    internal class RotatePivot : PivotBase
    {
        public Vector3 Center { get => circle.transform.position; set => circle.transform.position = value; }
        public float Radius { get => circle.transform.localScale.x; set => circle.transform.localScale = new Vector3(value, value, value); }

        private GameObject circle;

        internal RotatePivot(int textureResolution)
        {
            // TODO(torben): this does not support an odd resolution right now
            Texture2D texture = CreateCircleOutlineTexture(textureResolution / 2);
            circle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            circle.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            circle.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            Material circleMaterial = new Material(Shader.Find("Unlit/CircleShader"));
            //circleMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Greater); // TODO(torben): make this different when occluded as the MovePivots
            circleMaterial.SetTexture("_MainTex", texture);
            circle.GetComponent<MeshRenderer>().sharedMaterial = circleMaterial;
            circle.SetActive(false);
        }

        internal void Enable(bool enable)
        {
            circle.SetActive(enable);
        }

        private Texture2D CreateCircleOutlineTexture(int radiusInPixels)
        {
            Texture2D result = new Texture2D(2 * radiusInPixels, 2 * radiusInPixels, TextureFormat.R8, false);

            Color pixelColor = new Color(DefaultPrimaryAlpha, 0.0f, 0.0f, 0.0f);
            Color noPixelColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            Color[] colors = new Color[4 * radiusInPixels * radiusInPixels];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = noPixelColor;
            }
            result.SetPixels(colors);

            int rSqr = radiusInPixels * radiusInPixels;
            int x0 = radiusInPixels;
            int y0 = 0;
            int x = x0;
            int y = y0;

            int SquaredRadiusError(int xi, int yi)
            {
                int radiusError = xi * xi + yi * yi - rSqr;
                return radiusError * radiusError;
            }

            void Draw(int xi, int yi)
            {
                // octants are drawn counter-clockwise
                result.SetPixel(radiusInPixels - 1 + xi, radiusInPixels - 1 + yi, pixelColor);
                result.SetPixel(radiusInPixels - 1 + yi, radiusInPixels - 1 + xi, pixelColor);
                result.SetPixel(radiusInPixels - yi, radiusInPixels - 1 + xi, pixelColor);
                result.SetPixel(radiusInPixels - xi, radiusInPixels - 1 + yi, pixelColor);
                result.SetPixel(radiusInPixels - xi, radiusInPixels - yi, pixelColor);
                result.SetPixel(radiusInPixels - yi, radiusInPixels - xi, pixelColor);
                result.SetPixel(radiusInPixels - 1 + yi, radiusInPixels - xi, pixelColor);
                result.SetPixel(radiusInPixels - 1 + xi, radiusInPixels - yi, pixelColor);
            }

            while (x > y)
            {
                Draw(x, y);
                if (SquaredRadiusError(x - 1, y + 1) < SquaredRadiusError(x, y + 1))
                {
                    x--;
                }
                y++;
            }
            Draw(x, y);
            result.Apply(true, true);

            return result;
        }
    }
}
