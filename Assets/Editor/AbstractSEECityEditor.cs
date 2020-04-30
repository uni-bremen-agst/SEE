using UnityEngine;
using UnityEditor;
using SEE.Game;

namespace SEEEditor
{
    /// <summary>
    /// An abstract custom editor for instances of AbstractSEECity.
    /// </summary>
    [CustomEditor(typeof(AbstractSEECity))]
    [CanEditMultipleObjects]
    public class AbstractSEECityEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AbstractSEECity city = target as AbstractSEECity;

            GUILayout.Label("Graph", EditorStyles.boldLabel);
            city.origin = EditorGUILayout.Vector3Field("Origin", city.origin);
            city.width = EditorGUILayout.FloatField("Width (x axis)", city.width);

            GUILayout.Label("Attributes of leaf nodes", EditorStyles.boldLabel);
            city.WidthMetric = EditorGUILayout.TextField("Width", city.WidthMetric);
            city.HeightMetric = EditorGUILayout.TextField("Height", city.HeightMetric);
            city.DepthMetric = EditorGUILayout.TextField("Depth", city.DepthMetric);
            city.LeafStyleMetric = EditorGUILayout.TextField("Style", city.LeafStyleMetric);

            GUILayout.Label("Attributes of inner nodes", EditorStyles.boldLabel);
            city.InnerNodeStyleMetric = EditorGUILayout.TextField("Style", city.InnerNodeStyleMetric);

            GUILayout.Label("Nodes and Node Layout", EditorStyles.boldLabel);
            city.LeafObjects = (SEECity.LeafNodeKinds)EditorGUILayout.EnumPopup("Leaf nodes", city.LeafObjects);
            city.InnerNodeObjects = (SEECity.InnerNodeKinds)EditorGUILayout.EnumPopup("Inner nodes", city.InnerNodeObjects);
            city.NodeLayout = (SEECity.NodeLayouts)EditorGUILayout.EnumPopup("Node layout", city.NodeLayout);

            city.ZScoreScale = EditorGUILayout.Toggle("Z-score scaling", city.ZScoreScale);
            city.ShowErosions = EditorGUILayout.Toggle("Show erosions", city.ShowErosions);

            GUILayout.Label("Edges and Edge Layout", EditorStyles.boldLabel);
            city.EdgeLayout = (SEECity.EdgeLayouts)EditorGUILayout.EnumPopup("Edge layout", city.EdgeLayout);
            city.EdgeWidth = EditorGUILayout.FloatField("Edge width", city.EdgeWidth);
            city.EdgesAboveBlocks = EditorGUILayout.Toggle("Edges above blocks", city.EdgesAboveBlocks);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Bundling tension");
            city.Tension = EditorGUILayout.Slider(city.Tension, 0.0f, 1.0f);
            EditorGUILayout.EndHorizontal();
            city.RDP = EditorGUILayout.FloatField("RDP", city.RDP);

            GUILayout.Label("Data", EditorStyles.boldLabel);
            if (city.PathPrefix == null)
            {
                // Application.dataPath (used within ProjectPath()) must not be called in a 
                // constructor. That is why we need to set it here if it is not yet defined.
                city.PathPrefix = UnityProject.GetPath();
            }
            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();
        }
    }
}