#if UNITY_EDITOR

using SEE.DataModel.DG.IO;
using SEE.Game.City;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECityRandom as an extension 
    /// of the AbstractSEECityEditor.
    /// </summary>
    [CustomEditor(typeof(SEECityRandom))]
    [CanEditMultipleObjects]
    public class SEECityRandomEditor : SEECityEditor
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
                    EditorGUI.LabelField(rect, "Leaf node attributes (Name, Mean, SD)");
                };
            leafAttributes.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = leafAttributes.serializedProperty.GetArrayElementAtIndex(index);
                    SerializedProperty name = element.FindPropertyRelative("Name");
                    SerializedProperty mean = element.FindPropertyRelative("Mean");
                    SerializedProperty standardDeviation = element.FindPropertyRelative("StandardDeviation");

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

        /// <summary>
        /// In addition to the other attributes inherited, the specific attributes of 
        /// the SEEDynCity instance are shown and set here.
        /// </summary>
        protected override void Attributes()
        {
            base.Attributes();
            SEECityRandom city = target as SEECityRandom;

            GUILayout.Label("Leaf nodes", EditorStyles.boldLabel);
            city.LeafConstraint.NodeType = EditorGUILayout.TextField("Node type", city.LeafConstraint.NodeType);
            city.LeafConstraint.NodeNumber = EditorGUILayout.IntField("Number of nodes", city.LeafConstraint.NodeNumber);
            city.LeafConstraint.EdgeType = EditorGUILayout.TextField("Edge type", city.LeafConstraint.EdgeType);
            city.LeafConstraint.EdgeDensity = Mathf.Clamp(EditorGUILayout.FloatField("Edge density", city.LeafConstraint.EdgeDensity), 0.0f, 1.0f);

            GUILayout.Label("Inner nodes", EditorStyles.boldLabel);
            city.InnerNodeConstraint.NodeType = EditorGUILayout.TextField("Node type", city.InnerNodeConstraint.NodeType);
            city.InnerNodeConstraint.NodeNumber = EditorGUILayout.IntField("Number of nodes", city.InnerNodeConstraint.NodeNumber);
            city.InnerNodeConstraint.EdgeType = EditorGUILayout.TextField("Edge type", city.InnerNodeConstraint.EdgeType);
            city.InnerNodeConstraint.EdgeDensity = Mathf.Clamp(EditorGUILayout.FloatField("Edge density", city.InnerNodeConstraint.EdgeDensity), 0.0f, 1.0f);

            // List of leaf attributes.
            serializedObject.Update();
            leafAttributes.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Generates the random graph data and saves it.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        protected override void Draw(SEECity city)
        {
            base.Draw(city);
            // We select one hierarchicalEdge from the set of hierarchical edges arbitrarily.
            foreach (string hierarchicalEdge in city.HierarchicalEdges)
            {
                GraphWriter.Save(city.GXLPath.Path, city.LoadedGraph, hierarchicalEdge);
                return;
            }
        }
    }
}

#endif
