#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECity.
    /// </summary>
    //[CustomEditor(typeof(SEECity))]
    [CanEditMultipleObjects]
    public class SEECityEditor : StoredSEECityEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            seeCity = target as SEECity;
            Attributes();
            ShowNodeTypes(seeCity);
            Buttons();
            CoseSettings(seeCity);
        }

        /// <summary>
        /// The city to display
        /// </summary>
        private SEECity seeCity;

        /// <summary>
        /// key: inner-node ids, value: bool, if true the inner node is shown in the foldout, if false the
        /// section foldout is collapsed
        /// </summary>
        private readonly Dictionary<string, bool> ShowFoldout = new Dictionary<string, bool>();

        private void CoseSettings(SEECity city)
        {
            EditorGUILayout.Separator();

            if (city.NodeLayoutSettings.Kind == NodeLayoutKind.CompoundSpringEmbedder)
            {
                Graph graph = city.LoadedGraph;

                if (graph != null)
                {
                    IList<Node> roots = graph.GetRoots();
                    if (roots.Count > 0)
                    {
                        GUILayout.Label("Choose sublayouts", EditorStyles.boldLabel);

                        if (ShowFoldout.Count == 0)
                        {
                            foreach (Node root in roots)
                            {
                                TraverseThruNodesCounter(root);
                            }
                        }

                        if (showGraphListing)
                        {
                            List<NodeLayoutKind> parentNodeLayouts = new List<NodeLayoutKind>();
                            foreach (Node root in roots)
                            {
                                TraverseThruNodes(root, parentNodeLayouts);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// traverses thru the nodes and adds them to a list
        /// </summary>
        /// <param name="root">the root node</param>
        private void TraverseThruNodesCounter(Node root)
        {
            if (root.Children() != null && !root.IsLeaf())
            {
                ShowFoldout.Add(root.ID, true);
                if (root.Children() != null || root.Children().Count > 0)
                {
                    foreach (Node child in root.Children())
                    {
                        TraverseThruNodesCounter(child);
                    }
                }
            }
        }

        /// <summary>
        /// Traverses through the nodes and displays the sublayout hierarchy graph
        /// </summary>
        /// <param name="root"></param>
        private void TraverseThruNodes(Node root, List<NodeLayoutKind> parentNodeLayouts)
        {
            EditorGUIUtility.labelWidth = 80;
            if (root.Children() != null && !root.IsLeaf())
            {
                GUILayout.BeginHorizontal();

                GUILayout.Space(20 * root.Level);

                if (root.Children() != null && root.Children().Count > 0)
                {
                    bool allLeaves = true;
                    foreach (Node child in root.Children())
                    {
                        if (!child.IsLeaf())
                        {
                            allLeaves = false;
                        }
                    }

                    if (!allLeaves)
                    {
                        bool showPosition = EditorGUILayout.Foldout(ShowFoldout[root.ID], root.ID, true);
                        ShowFoldout[root.ID] = showPosition;

                        if (showPosition)
                        {
                            ShowCheckBox(root, false, parentNodeLayouts);

                            GUILayout.EndHorizontal();

                            if (root.Children() != null && root.Children().Count > 0)
                            {
                                foreach (Node child in root.Children())
                                {
                                    TraverseThruNodes(child, new List<NodeLayoutKind>(parentNodeLayouts));
                                }
                            }
                        }
                        else
                        {
                            GUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        EditorGUIUtility.labelWidth = 80;
                        GUILayout.Label(root.ID, GUILayout.Width(120));
                        ShowCheckBox(root, true, parentNodeLayouts);
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

        /// <summary>
        /// displays the checkbox and dropdowns for each node
        /// </summary>
        /// <param name="root"></param>
        /// <param name="childrenAreLeaves"></param>
        private void ShowCheckBox(Node root, bool childrenAreLeaves, List<NodeLayoutKind> parentNodeLayouts)
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayoutOption[] guiOptionsToggle = { GUILayout.ExpandWidth(false), GUILayout.Width(20) };
            bool toggle = EditorGUILayout.Toggle("", seeCity.CoseGraphSettings.ListInnerNodeToggle[root.ID], guiOptionsToggle);
            seeCity.CoseGraphSettings.ListInnerNodeToggle[root.ID] = toggle;
            //var checkedToggle = editorSettings.CoseGraphSettings.ListDirToggle.Where(predicate: kvp => kvp.Value);

            if (toggle)
            {
                ShowSublayoutEnum(seeCity.CoseGraphSettings.InnerNodeLayout[root.ID], root, childrenAreLeaves, parentNodeLayouts);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                ShowSublayoutEnum(NodeLayoutKind.CompoundSpringEmbedder, root, childrenAreLeaves, new List<NodeLayoutKind>());
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (toggle)
            {
                ShowInnerNodesEnum(seeCity.CoseGraphSettings.InnerNodeLayout[root.ID], root);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                ShowInnerNodesEnum(seeCity.CoseGraphSettings.InnerNodeLayout[root.ID], root);
                EditorGUI.EndDisabledGroup();
            }
        }

        /// <summary>
        /// Dropdown for the inner node Kinds
        /// </summary>
        /// <param name="nodeLayout"></param>
        /// <param name="node"></param>
        private void ShowInnerNodesEnum(NodeLayoutKind nodeLayout, GraphElement node)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PrefixLabel("Inner nodes");
            Dictionary<NodeShapes, string> shapeKinds = nodeLayout.GetInnerNodeKinds().ToDictionary(kind => kind, kind => kind.ToString());

            if (shapeKinds.ContainsKey(seeCity.CoseGraphSettings.InnerNodeShape[node.ID]))
            {
                seeCity.CoseGraphSettings.InnerNodeShape[node.ID] = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(seeCity.CoseGraphSettings.InnerNodeShape[node.ID]), shapeKinds.Values.ToArray(), guiOptions)).Key;
            }
            else
            {
                seeCity.CoseGraphSettings.InnerNodeShape[node.ID] = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(shapeKinds.First().Key), shapeKinds.Values.ToArray(), guiOptions)).Key;
            }

            EditorGUIUtility.labelWidth = 150;
        }

        /// <summary>
        /// Dropdown for the sublayout kinds
        /// </summary>
        /// <param name="nodeLayout"></param>
        /// <param name="root"></param>
        /// <param name="childrenAreLeaves"></param>
        private void ShowSublayoutEnum(NodeLayoutKind nodeLayout, GraphElement root, bool childrenAreLeaves, List<NodeLayoutKind> parentNodeLayouts)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PrefixLabel("Sublayouts");
            Dictionary<NodeLayoutKind, string> subLayoutNodeLayouts;

            if (childrenAreLeaves)
            {
                //  Dictionary with all Nodelayouts only for leaf nodes
                subLayoutNodeLayouts = Enum.GetValues(typeof(NodeLayoutKind)).Cast<NodeLayoutKind>().OrderBy(x => x.ToString()).ToDictionary(i => i, i => i.ToString());
            }
            else
            {
                // Dictionary with all Nodelayouts for leaf and inner nodes
                subLayoutNodeLayouts = Enum.GetValues(typeof(NodeLayoutKind)).Cast<NodeLayoutKind>().Where(nl => !nl.GetModel().OnlyLeaves).OrderBy(x => x.ToString()).ToDictionary(i => i, i => i.ToString()); ;
            }

            foreach (NodeLayoutKind layout in parentNodeLayouts)
            {
                List<NodeLayoutKind> possible = layout.GetPossibleSublayouts();
                subLayoutNodeLayouts = subLayoutNodeLayouts.Where(elem => possible.Contains(elem.Key)).ToDictionary(x => x.Key, x => x.Value);
            }

            if (subLayoutNodeLayouts.ContainsKey(seeCity.CoseGraphSettings.InnerNodeLayout[root.ID]))
            {
                seeCity.CoseGraphSettings.InnerNodeLayout[root.ID] = subLayoutNodeLayouts.ElementAt(EditorGUILayout.Popup(subLayoutNodeLayouts.Keys.ToList().IndexOf(seeCity.CoseGraphSettings.InnerNodeLayout[root.ID]), subLayoutNodeLayouts.Values.ToArray(), guiOptions)).Key;

            }
            else
            {
                seeCity.CoseGraphSettings.InnerNodeLayout[root.ID] = subLayoutNodeLayouts.ElementAt(EditorGUILayout.Popup(subLayoutNodeLayouts.Keys.ToList().IndexOf(subLayoutNodeLayouts.First().Key), subLayoutNodeLayouts.Values.ToArray(), guiOptions)).Key;
            }

            parentNodeLayouts.Add(seeCity.CoseGraphSettings.InnerNodeLayout[root.ID]);
            EditorGUIUtility.labelWidth = 150;
        }

        /// <summary>
        /// Creates the buttons for loading and deleting a city.
        /// </summary>
        protected virtual void Buttons()
        {
            SEECity seeCity = target as SEECity;
            EditorGUILayout.BeginHorizontal();
            if (seeCity.LoadedGraph == null && GUILayout.Button("Load Graph"))
            {
                Load(seeCity);
            }
            if (seeCity.LoadedGraph != null && GUILayout.Button("Delete Graph"))
            {
                Reset(seeCity);
            }
            if (seeCity.LoadedGraph != null && GUILayout.Button("Save Graph"))
            {
                Save(seeCity);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (seeCity.LoadedGraph != null && GUILayout.Button("Draw"))
            {
                Draw(seeCity);
            }
            if (seeCity.LoadedGraph != null && GUILayout.Button("Re-Draw"))
            {
                ReDraw(seeCity);
            }

            if (seeCity.LoadedGraph != null && GUILayout.Button("Save Layout"))
            {
                SaveLayout(seeCity);
            }
            EditorGUILayout.EndHorizontal();

            if (seeCity.LoadedGraph != null && GUILayout.Button("Add References"))
            {
                AddReferences(seeCity);
            }
        }

        /// <summary>
        /// Whether the foldout for the data-file attributes of the city should be expanded.
        /// </summary>
        protected bool showDataFiles = false;

        /// <summary>
        /// Shows and sets the attributes of the SEECity managed here.
        /// This method should be overridden by subclasses if they have additional
        /// attributes to manage.
        /// </summary>
        protected virtual void Attributes()
        {
            SEECity city = target as SEECity;
            Assert.IsNotNull(city);
            showDataFiles = EditorGUILayout.Foldout(showDataFiles, "Data Files", true, EditorStyles.foldoutHeader);
            if (showDataFiles)
            {
                city.GXLPath = DataPathEditor.GetDataPath("GXL file", city.GXLPath, Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension)) as FilePath;
                city.CSVPath = DataPathEditor.GetDataPath("Metric file", city.CSVPath, Filenames.ExtensionWithoutPeriod(Filenames.CSVExtension)) as FilePath;
                city.SourceCodeDirectory = DataPathEditor.GetDataPath("Project directory", city.SourceCodeDirectory, fileDialogue: false) as DirectoryPath;
                city.SolutionPath = DataPathEditor.GetDataPath("Solution file", city.SolutionPath, Filenames.ExtensionWithoutPeriod(Filenames.SolutionExtension)) as FilePath;
            }
        }

        /// <summary>
        /// Loads the graph data and metric data from disk, aggregates the metrics to
        /// inner nodes.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        protected virtual void Load(SEECity city)
        {
            city.LoadData();
        }

        /// <summary>
        /// Renders the graph in the scene.
        /// </summary>
        /// <param name="city">the city to be set up</param>
        protected virtual void Draw(SEECity city)
        {
            city.DrawGraph();
        }

        /// <summary>
        /// Renders the graph in the scene once again without deleting the underlying graph loaded.
        /// </summary>
        /// <param name="city">the city to be re-drawn</param>
        protected virtual void ReDraw(SEECity city)
        {
            city.ReDrawGraph();
        }

        /// <summary>
        /// Deletes the underlying graph data of the given city and deletes all its game
        /// objects.
        /// </summary>
        private void Reset(SEECity city)
        {
            city.Reset();
        }

        /// <summary>
        /// Saves the underlying graph of the current city.
        /// </summary>
        private void Save(SEECity city)
        {
            city.SaveData();
        }

        /// <summary>
        /// Saves the current layout of the given <paramref name="city"/>.
        /// </summary>
        private void SaveLayout(SEECity city)
        {
            city.SaveLayout();
        }

        private void AddReferences(SEECity city)
        {
            city.SetNodeEdgeRefs();
        }
    }
}

#endif
