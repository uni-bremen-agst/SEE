using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Window.TreeWindow
{
    // Parts of the tree window that are specific to the desktop UI.
    public partial class TreeWindow
    {

        /// <summary>
        /// Transform of the content of the tree window.
        /// </summary>
        private Transform content;

        /// <summary>
        /// Adds the given <paramref name="node"/> to the bottom of the tree window.
        /// </summary>
        /// <param name="node">the node to be added</param>
        private void AddItem(Node node)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(TREE_ITEM_PREFAB, content, false);
            item.name = node.ID;
            item.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = node.ToShortString();
            // TODO: Setup Expand Icon (or whole row) to receive clicks and to be angled correctly
        }

        protected override void StartDesktop()
        {
            if (graph == null)
            {
                Debug.LogError("Graph must be set before starting the tree window.");
                return;
            }

            Title = $"{graph.Name} – Tree View";
            base.StartDesktop();
            content = PrefabInstantiator.InstantiatePrefab(TREE_WINDOW_PREFAB, Window.transform.Find("Content"), false).transform.Find("Content");

            // We will traverse the graph and add each node to the tree view.
            IList<Node> roots = graph.GetRoots();
            foreach (Node root in roots)
            {
                AddItem(root);
            }
            // Traverse is pre-order, so we can always assume parent exists already.
            graph.Traverse(HandleInnerNode, HandleLeafNode);

            if (roots.Count == 0)
            {
                Debug.LogWarning("Graph has no roots. TreeView will be empty.");
                return;
            }
            return;

            #region Local Functions

            void HandleInnerNode(Node inner)
            {
                HandleLeafNode(inner);
            }

            void HandleLeafNode(Node leaf)
            {
                AddItem(leaf);
            }

            #endregion
        }
    }
}
