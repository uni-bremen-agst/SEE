#if UNITY_EDITOR

using UnityEditor;

namespace SEEEditor
{
    /// <summary>
    /// An extension of UnityEditor.Rendering.Universal.ShaderGUI.LitShader.
    /// Unfortunately, that class is internal and, thus, not visible here.
    /// If it were, we would need the following assembly reference to
    /// Assets/Editor/SEE_Editor.asmdef:
    /// Unity.RenderPipelines.Core.Editor and Unity.RenderPipelines.Universal.Editor.
    /// </summary>
    public class PortalShaderEditor : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Draw the standard Lit properties
            // This uses the base logic to draw the Surface Options and Surface Inputs
            // without needing to inherit from the internal LitShader class.
            base.OnGUI(materialEditor, properties);

            // The following code is actually not needed. The call to base.OnGUI above
            // shows them already.
            /*
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Portal Settings", EditorStyles.boldLabel);

            // 2. Manual Property Drawing
            // This is where you draw your X/Z Clipping variables from your Shader Graph
            MaterialProperty portal = FindProperty("_Portal", properties, false);

            if (portal != null)
            {
                materialEditor.ShaderProperty(portal, "Portal");
            }
            else
            {
                EditorGUILayout.HelpBox("Portal properties not found. Check Shader Graph Reference names.", MessageType.Warning);
            }

            MaterialProperty alphaCutoff = FindProperty("_Cutoff", properties, false);
            if (alphaCutoff != null)
            {
                materialEditor.ShaderProperty(alphaCutoff, "Alpha Clip Threshold");
            }
            */
        }
    }
}
#endif
