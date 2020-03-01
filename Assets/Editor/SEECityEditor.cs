using UnityEngine;
using UnityEditor;
using SEE;
using SEE.DataModel;
using SEE.Layout;
using System.Collections.Generic;
using System.Linq;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECity.
    /// </summary>
    [CustomEditor(typeof(SEECity))]
    [CanEditMultipleObjects]
    public class SEECityEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SEECity city = target as SEECity;

            GUILayout.Label("Graph", EditorStyles.boldLabel);
            if (city.pathPrefix == null)
            {
                // Application.dataPath (used within ProjectPath()) must not be called in a 
                // constructor. That is why we need to set it here if it is not yet defined.
                city.pathPrefix = UnityProject.GetPath();
            }
            city.pathPrefix = EditorGUILayout.TextField("Project path prefix", city.pathPrefix);
            city.gxlPath = EditorGUILayout.TextField("GXL file", city.gxlPath);
            city.csvPath = EditorGUILayout.TextField("CSV file", city.csvPath);
            city.origin = EditorGUILayout.Vector3Field("Origin", city.origin);

            GUILayout.Label("Attributes of leaf nodes", EditorStyles.boldLabel);
            city.WidthMetric = EditorGUILayout.TextField("Width", city.WidthMetric);
            city.HeightMetric = EditorGUILayout.TextField("Height", city.HeightMetric);
            city.DepthMetric = EditorGUILayout.TextField("Depth", city.DepthMetric);
            city.ColorMetric = EditorGUILayout.TextField("Color", city.ColorMetric);

            GUILayout.Label("Visual node attributes", EditorStyles.boldLabel);
            city.LeafObjects = (SEECity.LeafNodeKinds)EditorGUILayout.EnumPopup("Leaf nodes", city.LeafObjects);
            city.InnerNodeObjects = (SEECity.InnerNodeKinds)EditorGUILayout.EnumPopup("Inner nodes", city.InnerNodeObjects);
            city.NodeLayout = (SEECity.NodeLayouts)EditorGUILayout.EnumPopup("Layout", city.NodeLayout);

            city.ZScoreScale = EditorGUILayout.Toggle("Z-score scaling", city.ZScoreScale);
            city.ShowErosions = EditorGUILayout.Toggle("Show erosions", city.ShowErosions);

            GUILayout.Label("Visual edge attributes", EditorStyles.boldLabel);
            city.EdgeLayout = (SEECity.EdgeLayouts)EditorGUILayout.EnumPopup("Layout", city.EdgeLayout);
            city.EdgeWidth = EditorGUILayout.FloatField("Edge width", city.EdgeWidth);
            city.EdgesAboveBlocks = EditorGUILayout.Toggle("Edges above blocks", city.EdgesAboveBlocks);

            // TODO: We may want to allow a user to define all edge types to be considered hierarchical.
            // TODO: We may want to allow a user to define which node attributes should be mapped onto which icons

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();

            if (GUILayout.Button("Load City"))
            {
                SetUp(city);
            }
            if (GUILayout.Button("Delete City"))
            {
                Reset(city);
            }
        }

        /// <summary>
        /// Loads the graph data and metric data from disk, aggregates the metrics to
        /// inner nodes, renders the graph in the scene and adjusts the speed of all
        /// main cameras if necessary.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        private void SetUp(SEECity city)
        {
            city.LoadGraph();
            if (ReferenceEquals(city.ItsGraph, null))
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                GraphRenderer renderer = new GraphRenderer(city);
                renderer.Draw(city.ItsGraph);
                // If CScape buildings are used, the scale of the world is larger and, hence, the camera needs to move faster.
                Cameras.AdjustCameraSpeed(renderer.Unit());
            }
        }

        /// <summary>
        /// Deletes the underlying graph data of the given city.
        /// </summary>
        private void Reset(SEECity city)
        {
            city.DeleteGraph();   
        }
    }
}