using SEE.Game;
using SEE.Game.Architecture;
using SEE.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of <see cref="SEECityArchitecture"/>.
    /// </summary>
    [CustomEditor(typeof(SEECityArchitecture))]
    [CanEditMultipleObjects]
    public class SEECityArchitectureEditor : Editor
    {
        private SEECityArchitecture city;


        private bool showElementSettings = false;
        private bool[] showElementTypeAttributes = new bool[(int)ArchitectureElementType.Count];
        private bool showFileSettings = true;
        private bool showEdgeLayout;


        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            city = target as SEECityArchitecture;
            Buttons();
            ArchitectureIOSettings();
            ElementSettings();
            EdgeSettings();
        }


        private void ArchitectureIOSettings()
        {
            showFileSettings = EditorGUILayout.Foldout(showFileSettings, "Architecture Data Settings", true,
                EditorStyles.foldoutHeader);
            if (showFileSettings)
            {
                city.GXLPath = DataPathEditor.GetDataPath("GXL graph file", city.GXLPath, Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension));
                city.ArchitectureLayoutPath = DataPathEditor.GetDataPath("Architecture Layout definition",
                    city.ArchitectureLayoutPath, Filenames.LayoutExtensions);
            }
            EditorGUILayout.Separator();
            
        }


        private void Buttons()
        {
            EditorGUILayout.BeginHorizontal();
            if (city.LoadedGraph == null && GUILayout.Button("Load Graph"))
            {
                city.LoadGraph();
            }

            if (city.LoadedGraph == null && GUILayout.Button("New Graph"))
            {
                city.NewGraph();
            }

            if (city.LoadedGraph != null && GUILayout.Button("Delete Graph"))
            {
                city.ResetGraph();
            }

            if (city.LoadedGraph != null && GUILayout.Button("Save Graph"))
            {
                city.SaveGraph();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (city.LoadedGraph != null && GUILayout.Button("Draw"))
            {
                city.DrawGraph();
            }

            if (city.LoadedGraph != null && GUILayout.Button("Re-Draw"))
            {
                city.ReDrawGraph();
            }

            if (city.LoadedGraph != null && GUILayout.Button("Save Layout"))
            {
                city.SaveLayout();
            }
            EditorGUILayout.EndHorizontal();
            if (city.LoadedGraph != null && GUILayout.Button("Add References"))
            {
                city.AddReferences();
            }
            
            
        }

        private void EdgeSettings()
        {
            showEdgeLayout = EditorGUILayout.Foldout(showEdgeLayout, "Edges and edge layout", true,
                EditorStyles.foldoutHeader);
            if (showEdgeLayout)
            {
                EdgeLayoutSettings settings = city.edgeLayoutSettings;
                Assert.IsTrue(settings.GetType().IsClass);
                
                settings.kind = (EdgeLayoutKind)EditorGUILayout.EnumPopup("Edge layout", settings.kind);
                settings.edgeWidth = EditorGUILayout.FloatField("Edge width", settings.edgeWidth);
                settings.edgesAboveBlocks = EditorGUILayout.Toggle("Edges above blocks", settings.edgesAboveBlocks);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Bundling tension");
                settings.tension = EditorGUILayout.Slider(settings.tension, 0.0f, 1.0f);
                EditorGUILayout.EndHorizontal();
                settings.rdp = EditorGUILayout.FloatField("RDP", settings.rdp);
                settings.tubularSegments = EditorGUILayout.IntField("Tubular Segments", settings.tubularSegments);
                settings.radius = EditorGUILayout.FloatField("Radius", settings.radius);
                settings.radialSegments = EditorGUILayout.IntField("Radial Segments", settings.radialSegments);
                settings.isEdgeSelectable = EditorGUILayout.Toggle("Edges selectable", settings.isEdgeSelectable);
            }
        }

        

        private void ElementSettings()
        {
            showElementSettings = EditorGUILayout.Foldout(showElementSettings, "Architecture Element settings", true,
                EditorStyles.foldoutHeader);
            if (showElementSettings)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < (int) ArchitectureElementType.Count; i++)
                {
                    string label = $"Element type: {(ArchitectureElementType) i}";
                    showElementTypeAttributes[i] =
                        EditorGUILayout.Foldout(showElementTypeAttributes[i], label, EditorStyles.foldout);
                    if (showElementTypeAttributes[i])
                    {
                        ArchitectureElementSettings settings = city.ArchitectureElementSettings[i];
                        Assert.IsTrue(settings.GetType().IsClass);

                        settings.ElementType = (ArchitectureElementType) i;
                        settings.ElementHeight = EditorGUILayout.FloatField("Element height", settings.ElementHeight);
                        settings.ColorRange.lower =
                            EditorGUILayout.ColorField("Lower color", settings.ColorRange.lower);
                        settings.ColorRange.upper =
                            EditorGUILayout.ColorField("Upper color", settings.ColorRange.upper);
                        settings.ColorRange.NumberOfColors = (uint)EditorGUILayout.IntSlider("# Colors",
                            (int) settings.ColorRange.NumberOfColors, 1, 15);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
    }
}