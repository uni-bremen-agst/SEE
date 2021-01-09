﻿#if UNITY_EDITOR

using SEE.DataModel.DG;
using SEE.Game;
using SEE.Layout.NodeLayouts;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of SEECity.
    /// </summary>
    [CustomEditor(typeof(SEECity))]
    [CanEditMultipleObjects]
    public class SEECityEditor : StoredSEECityEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            city = target as SEECity;
            Attributes();
            ShowNodeTypes(city);
            Buttons();
            CoseSettings(city);
        }

        /// <summary>
        /// the city to display
        /// </summary>
        private SEECity city;

        /// <summary>
        /// key: inner-node ids, value: bool, if true the inner node is shown in the foldout, if false the 
        /// section foldout is collapsed 
        /// </summary>
        private Dictionary<string, bool> ShowFoldout = new Dictionary<string, bool>();

        private void CoseSettings(SEECity city)
        {
            EditorGUILayout.Separator();

            if (city.NodeLayout == NodeLayoutKind.CompoundSpringEmbedder)
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

                        if (ShowGraphListing)
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
            bool toggle = EditorGUILayout.Toggle("", city.CoseGraphSettings.ListInnerNodeToggle[root.ID], guiOptionsToggle);
            city.CoseGraphSettings.ListInnerNodeToggle[root.ID] = toggle;
            //var checkedToggle = editorSettings.CoseGraphSettings.ListDirToggle.Where(predicate: kvp => kvp.Value);

            if (toggle)
            {
                ShowSublayoutEnum(city.CoseGraphSettings.InnerNodeLayout[root.ID], root, childrenAreLeaves, parentNodeLayouts);
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
                ShowInnerNodesEnum(city.CoseGraphSettings.InnerNodeLayout[root.ID], root);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                ShowInnerNodesEnum(city.CoseGraphSettings.InnerNodeLayout[root.ID], root);
                EditorGUI.EndDisabledGroup();
            }
        }

        /// <summary>
        /// Dropdown for the inner node Kinds 
        /// </summary>
        /// <param name="nodeLayout"></param>
        /// <param name="node"></param>
        private void ShowInnerNodesEnum(NodeLayoutKind nodeLayout, Node node)
        {
            GUILayoutOption[] guiOptions = { GUILayout.ExpandWidth(false), GUILayout.Width(200) };
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PrefixLabel("Inner nodes");
            Dictionary<AbstractSEECity.InnerNodeKinds, string> shapeKinds = nodeLayout.GetInnerNodeKinds().ToDictionary(kind => kind, kind => kind.ToString());

            if (shapeKinds.ContainsKey(city.CoseGraphSettings.InnerNodeShape[node.ID]))
            {
                city.CoseGraphSettings.InnerNodeShape[node.ID] = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(city.CoseGraphSettings.InnerNodeShape[node.ID]), shapeKinds.Values.ToArray(), guiOptions)).Key;
            }
            else
            {
                city.CoseGraphSettings.InnerNodeShape[node.ID] = shapeKinds.ElementAt(EditorGUILayout.Popup(shapeKinds.Keys.ToList().IndexOf(shapeKinds.First().Key), shapeKinds.Values.ToArray(), guiOptions)).Key;
            }

            EditorGUIUtility.labelWidth = 150;
        }

        /// <summary>
        /// Dropdown for the sublayout kinds
        /// </summary>
        /// <param name="nodeLayout"></param>
        /// <param name="root"></param>
        /// <param name="childrenAreLeaves"></param>
        private void ShowSublayoutEnum(NodeLayoutKind nodeLayout, Node root, bool childrenAreLeaves, List<NodeLayoutKind> parentNodeLayouts)
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
                subLayoutNodeLayouts = Enum.GetValues(typeof(NodeLayoutKind)).Cast<NodeLayoutKind>().Where(nodeLayout => !nodeLayout.GetModel().OnlyLeaves).OrderBy(x => x.ToString()).ToDictionary(i => i, i => i.ToString()); ;
            }

            foreach (NodeLayoutKind layout in parentNodeLayouts)
            {
                List<NodeLayoutKind> possible = layout.GetPossibleSublayouts();
                subLayoutNodeLayouts = subLayoutNodeLayouts.Where(elem => possible.Contains(elem.Key)).ToDictionary(x => x.Key, x => x.Value);
            }

            if (subLayoutNodeLayouts.ContainsKey(city.CoseGraphSettings.InnerNodeLayout[root.ID]))
            {
                city.CoseGraphSettings.InnerNodeLayout[root.ID] = subLayoutNodeLayouts.ElementAt(EditorGUILayout.Popup(subLayoutNodeLayouts.Keys.ToList().IndexOf(city.CoseGraphSettings.InnerNodeLayout[root.ID]), subLayoutNodeLayouts.Values.ToArray(), guiOptions)).Key;

            }
            else
            {
                city.CoseGraphSettings.InnerNodeLayout[root.ID] = subLayoutNodeLayouts.ElementAt(EditorGUILayout.Popup(subLayoutNodeLayouts.Keys.ToList().IndexOf(subLayoutNodeLayouts.First().Key), subLayoutNodeLayouts.Values.ToArray(), guiOptions)).Key;
            }

            parentNodeLayouts.Add(city.CoseGraphSettings.InnerNodeLayout[root.ID]);
            EditorGUIUtility.labelWidth = 150;
        }

        /// <summary>
        /// Creates the buttons for loading and deleting a city.
        /// </summary>
        protected void Buttons()
        {
            SEECity city = target as SEECity;
            EditorGUILayout.BeginHorizontal();
            if (city.LoadedGraph == null && GUILayout.Button("Load Graph"))
            {
                Load(city);
            }
            if (city.LoadedGraph != null && GUILayout.Button("Delete Graph"))
            {
                Reset(city);
            }
            if (city.LoadedGraph != null && GUILayout.Button("Save Graph"))
            {
                Save(city);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (city.LoadedGraph != null && GUILayout.Button("Draw"))
            {
                Draw(city);
            }
            if (city.LoadedGraph != null && GUILayout.Button("Re-Draw"))
            {
                ReDraw(city);
            }

            if (city.LoadedGraph != null && GUILayout.Button("Save Layout"))
            {
                SaveLayout(city);
            }
            EditorGUILayout.EndHorizontal();

            if (city.LoadedGraph != null && GUILayout.Button("Add References"))
            {
                AddReferences(city);
            }
        }

        /// <summary>
        /// Whether the foldout for the data-file attributes of the city should be expanded.
        /// </summary>
        private bool showDataFiles = true;

        /// <summary>
        /// Shows and sets the attributes of the SEECity managed here.
        /// This method should be overridden by subclasses if they have additional
        /// attributes to manage.
        /// </summary>
        protected virtual void Attributes()
        {
            SEECity city = target as SEECity;
            showDataFiles = EditorGUILayout.Foldout(showDataFiles,
                                                    "Data Files", true, EditorStyles.foldoutHeader);
            if (showDataFiles)
            {
                city.GXLPath = GetDataPath("GXL file", city.GXLPath, Filenames.ExtensionWithoutPeriod(Filenames.GXLExtension));
                city.CSVPath = GetDataPath("Metric file", city.CSVPath, Filenames.ExtensionWithoutPeriod(Filenames.CSVExtension));
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
