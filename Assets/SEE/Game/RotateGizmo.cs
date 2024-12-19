using UnityEngine;
using SEE.GO;
using SEE.Utils;

namespace SEE.UI3D
{
    /// <summary>
    /// This gizmo visually represents a rotation of an object.
    /// </summary>
    public class RotateGizmo : MonoBehaviour
    {
        /// <summary>
        /// The name of the material responsible for rendering the circular rotation visualization.
        /// </summary>
        private const string rotateGizmoShaderName = "Unlit/RotateGizmoShader";

        /// <summary>
        /// The center of the rotation.
        /// </summary>
        public Vector3 Center { get => transform.position; set => transform.position = value; }

        /// <summary>
        /// The radius of the gizmo.
        /// </summary>
        public float Radius { get => transform.localScale.x; set => transform.localScale = new Vector3(value, value, value); }

        /// <summary>
        /// The material responsible for rendering the circular rotation visualization.
        /// </summary>
        private Material material;

        /// <summary>
        /// <see cref="StartAngle"/>
        /// </summary>
        private float startAngle;

        /// <summary>
        /// The start angle of the rotation.
        /// </summary>
        internal float StartAngle
        {
            get => startAngle;
            set
            {
                startAngle = value;
                material.SetFloat(minAngleProperty, value);
            }
        }

        /// <summary>
        /// The cached shader property ID for the gizmo's minimal angle.
        /// </summary>
        private static readonly int minAngleProperty = Shader.PropertyToID("_MinAngle");
        /// <summary>
        /// The cached shader property ID for the gizmo's maximum angle.
        /// </summary>
        private static readonly int maxAngleProperty = Shader.PropertyToID("_MaxAngle");
        /// <summary>
        /// The cached shader property ID for the gizmo's main texture.
        /// </summary>
        private static readonly int mainTexProperty = Shader.PropertyToID("_MainTex");
        /// <summary>
        /// The cached shader property ID for the gizmo's color.
        /// </summary>
        private static readonly int colorProperty = Shader.PropertyToID("_Color");
        /// <summary>
        /// The cached shader property ID for the gizmo's transparency alpha value.
        /// </summary>
        private static readonly int alphaProperty = Shader.PropertyToID("_Alpha");

        /// <summary>
        /// <see cref="TargetAngle"/>
        /// </summary>
        private float targetAngle;

        /// <summary>
        /// The target angle of the rotation.
        /// </summary>
        internal float TargetAngle
        {
            get => targetAngle;
            set
            {
                targetAngle = value;
                material.SetFloat(maxAngleProperty, value);
            }
        }

        /// <summary>
        /// Creates a rotation gizmo.
        /// </summary>
        /// <param name="textureResolution">The resolution of the outline texture.</param>
        /// <returns>The gizmo.</returns>
        internal static RotateGizmo Create(int textureResolution)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroyer.Destroy(go.GetComponent<MeshCollider>());
            go.name = "RotatePivot";
            go.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            go.transform.position = Vector3.zero;

            RotateGizmo rotateGizmo = go.AddComponent<RotateGizmo>();

            int outer = textureResolution / 2;
            int inner = Mathf.RoundToInt(outer * 0.98f);

            rotateGizmo.material = new Material(Shader.Find(rotateGizmoShaderName));
            rotateGizmo.material.SetTexture(mainTexProperty, TextureGenerator.CreateCircleOutlineTextureR8(outer, inner, UI3DProperties.DefaultAlpha, 0.0f));
            rotateGizmo.material.SetColor(colorProperty, UI3DProperties.DefaultColor);
            rotateGizmo.material.SetFloat(alphaProperty, UI3DProperties.DefaultAlpha);

            go.MustGetComponent<MeshRenderer>().sharedMaterial = rotateGizmo.material;
            go.SetActive(false);

            return rotateGizmo;
        }
    }
}
