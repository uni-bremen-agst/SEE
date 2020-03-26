using UnityEditor;
using UnityEngine;
using SEE.Game;
using UnityEditorInternal;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECityRandom as an extension 
    /// of the AbstractSEECityEditor.
    /// </summary>
    [CustomEditor(typeof(SEECityRandom))]
    [CanEditMultipleObjects]
    public class SEECityRandomEditor : AbstractSEECityEditor
    {
        private ReorderableList leafAttributes;

        public void OnEnable()
        {
            SEECityRandom city = target as SEECityRandom;

            leafAttributes = new ReorderableList(serializedObject, serializedObject.FindProperty("LeafAttributes"),
                                                 false, true, true, true);
            leafAttributes.drawHeaderCallback
                = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Node attributes (Name, Mean, SD)");
                };
            leafAttributes.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = leafAttributes.serializedProperty.GetArrayElementAtIndex(index);
                    var name = element.FindPropertyRelative("Name");
                    var mean = element.FindPropertyRelative("Mean");
                    var standardDeviation = element.FindPropertyRelative("StandardDeviation");

                    rect.y += 2;
                    float nameLength = rect.width * 0.75f;
                    float valueLength = (rect.width - nameLength) / 2.0f;

                    city.LeafAttributes[index].Name 
                        = EditorGUI.TextField(new Rect(rect.x, rect.y, nameLength, EditorGUIUtility.singleLineHeight),
                                              name.stringValue);
                    float meanValue 
                        = EditorGUI.FloatField(new Rect(rect.x + nameLength, rect.y, valueLength, EditorGUIUtility.singleLineHeight),
                                               mean.floatValue);
                    if (meanValue >= 0.0f)
                    {
                        city.LeafAttributes[index].Mean = meanValue;
                    }
                    float sdValue
                        = EditorGUI.FloatField(new Rect(rect.x + nameLength + valueLength, rect.y, valueLength, EditorGUIUtility.singleLineHeight),
                                               standardDeviation.floatValue);
                    if (sdValue >= 0)
                    {
                        city.LeafAttributes[index].StandardDeviation = sdValue;
                    }
                };
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            SEECityRandom city = target as SEECityRandom;

            city.LeafNodeType = EditorGUILayout.TextField("Type of leaf nodes", city.LeafNodeType);
            city.InnerNodeType = EditorGUILayout.TextField("Type of inner nodes", city.InnerNodeType);

            city.NumberOfLeaves = EditorGUILayout.IntField("Number of leaf nodes", city.NumberOfLeaves);
            city.NumberOfInnerNodes = EditorGUILayout.IntField("Number of inner nodes", city.NumberOfInnerNodes);

            // List of leaf attributes.
            serializedObject.Update();
            leafAttributes.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load City"))
            {
                SetUp(city);
            }
            if (GUILayout.Button("Delete City"))
            {
                Reset(city);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Generates the graph, aggregates the metrics to inner nodes and renders the graph in the scene.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        private void SetUp(SEECityRandom city)
        {
            city.LoadData();
        }

        /// <summary>
        /// Deletes the underlying graph data of the given city.
        /// </summary>
        private void Reset(SEECityRandom city)
        {
            city.DeleteGraph();
        }
    }
}
