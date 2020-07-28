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
        private Material material;

        internal RotatePivot(int textureResolution)
        {
            int outer = textureResolution / 2;
            int inner = Mathf.RoundToInt((float)outer * 0.98f);
            Texture2D texture = CreateCircleOutlineTexture(outer, inner);
            texture.filterMode = FilterMode.Point; // TODO(torben): remove!
            circle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            circle.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            circle.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            material = new Material(Shader.Find("Unlit/CircleShader"));
            //circleMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Greater); // TODO(torben): make this different when occluded as the MovePivots
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Alpha", DefaultPrimaryAlpha);
            circle.GetComponent<MeshRenderer>().sharedMaterial = material;
            circle.SetActive(false);
        }

        internal void Enable(bool enable)
        {
            circle.SetActive(enable);
        }
        
        internal void SetMinAngle(float minAngleRadians)
        {
            material.SetFloat("_MinAngle", minAngleRadians);
        }

        internal void SetMaxAngle(float maxAngleRadians)
        {
            material.SetFloat("_MaxAngle", maxAngleRadians);
            material.SetColor("_Color", new Color(Mathf.Cos(maxAngleRadians), 1.0f, Mathf.Sin(-maxAngleRadians), DefaultPrimaryAlpha));
        }

        private Texture2D CreateCircleOutlineTexture(int outer, int inner)
        {
            int size = 2 * outer + 1;
            Texture2D result = new Texture2D(size, size, TextureFormat.R8, false);

            Color pixelColor = new Color(DefaultPrimaryAlpha, 0.0f, 0.0f, 0.0f);
            Color noPixelColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            Color[] colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = noPixelColor;
            }
            result.SetPixels(colors);

            int xc = outer;
            int yc = outer;
            int xo = outer;
            int xi = inner;
            int y = 0;
            int erro = 1 - xo;
            int erri = 1 - xi;

            void XLine(Texture2D _tex, int _x1, int _x2, int _y, Color _color)
            {
                while (_x1 <= _x2)
                {
                    _tex.SetPixel(_x1++, _y, _color);
                }
            }

            void YLine(Texture2D _tex, int _x, int _y1, int _y2, Color _color)
            {
                while (_y1 <= _y2)
                {
                    _tex.SetPixel(_x, _y1++, _color);
                }
            }

            while (xo >= y)
            {
                XLine(result, xc + xi, xc + xo, yc + y, pixelColor);
                YLine(result, xc + y, yc + xi, yc + xo, pixelColor);
                XLine(result, xc - xo, xc - xi, yc + y, pixelColor);
                YLine(result, xc - y, yc + xi, yc + xo, pixelColor);
                XLine(result, xc - xo, xc - xi, yc - y, pixelColor);
                YLine(result, xc - y, yc - xo, yc - xi, pixelColor);
                XLine(result, xc + xi, xc + xo, yc - y, pixelColor);
                YLine(result, xc + y, yc - xo, yc - xi, pixelColor);

                y++;

                if (erro < 0)
                {
                    erro += 2 * y + 1;
                }
                else
                {
                    xo--;
                    erro += 2 * (y - xo + 1);
                }

                if (y > inner)
                {
                    xi = y;
                }
                else
                {
                    if (erri < 0)
                    {
                        erri += 2 * y + 1;
                    }
                    else
                    {
                        xi--;
                        erri += 2 * (y - xi + 1);
                    }
                }
            }

            result.Apply(true, true);
            return result;
        }
    }
}
