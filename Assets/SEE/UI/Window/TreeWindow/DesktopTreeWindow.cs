using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.UI;

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
        /// A set of all items (node IDs) that have been expanded.
        /// Note that this may contain items that are not currently visible due to collapsed parents.
        /// Such items will be expanded when they become visible again.
        /// </summary>
        private readonly ISet<string> expandedItems = new HashSet<string>();

        /// <summary>
        /// The amount by which the text of an item is indented per level.
        /// </summary>
        private const int IndentShift = 22;

        /// <summary>
        /// Replace slashes with backslashes in the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The ID to be cleaned up.</param>
        /// <returns>The cleaned up ID.</returns>
        private static string CleanupID(string id) => id.Replace('/', '\\');

        /// <summary>
        /// Adds the given <paramref name="node"/> to the bottom of the tree window.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        private void AddItem(Node node)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(TREE_ITEM_PREFAB, content, false);
            if (node.Parent != null)
            {
                // Position the item below its parent.
                // TODO: Use colors from the city (e.g., depending on node type).
                item.transform.SetSiblingIndex(content.Find(CleanupID(node.Parent.ID)).GetSiblingIndex() + 1);
                item.transform.Find("Foreground").localPosition += new Vector3(IndentShift * node.Level, 0, 0);
            }
            // Slashes will cause problems later on, so we replace them with backslashes.
            // NOTE: This becomes a problem if two nodes A and B exist where node A's name contains slashes and node B
            //       has an identical name, except for all slashes being replaced by backslashes.
            //       I hope this is unlikely enough to not be a problem for now.
            item.name = CleanupID(node.ID);
            item.transform.Find("Foreground/Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = node.ToShortString();
            if (node.NumberOfChildren() == 0)
            {
                item.transform.Find("Foreground/Expand Icon").gameObject.SetActive(false);
            }
            else if (item.TryGetComponentOrLog(out Button button))
            {
                button.onClick.AddListener(() =>
                {
                    if (expandedItems.Contains(node.ID))
                    {
                        CollapseItem(node, item);
                    }
                    else
                    {
                        ExpandItem(node, item);
                    }
                });

                // If this item was previously expanded, we need to expand it again.
                if (expandedItems.Contains(node.ID))
                {
                    ExpandItem(node, item);
                }
            }
        }

        /// <summary>
        /// Removes the given <paramref name="node"/> from the tree window.
        /// </summary>
        /// <param name="node">The node to be removed.</param>
        private void RemoveItem(Node node)
        {
            string id = CleanupID(node.ID);
            GameObject item = content.Find(id)?.gameObject;
            if (item == null)
            {
                Debug.LogWarning($"Item {id} not found.");
                return;
            }
            if (expandedItems.Contains(id))
            {
                foreach (Node child in node.Children())
                {
                    RemoveItem(child);
                }
            }
            Destroyer.Destroy(item);
        }

        /// <summary>
        /// Expands the given <paramref name="item"/>.
        /// Its children will be added to the tree window.
        /// </summary>
        /// <param name="node">The node represented by the item.</param>
        /// <param name="item">The item to be expanded.</param>
        private void ExpandItem(Node node, GameObject item)
        {
            expandedItems.Add(item.name);
            if (item.transform.Find("Foreground/Expand Icon").gameObject.TryGetComponentOrLog(out RectTransform rectTransform))
            {
                // TODO: Animate this.
                rectTransform.Rotate(0, 0, -90);
            }
            foreach (Node child in node.Children())
            {
                AddItem(child);
            }
        }

        /// <summary>
        /// Collapses the given <paramref name="item"/>.
        /// Its children will be removed from the tree window.
        /// </summary>
        /// <param name="node">The node represented by the item.</param>
        /// <param name="item">The item to be collapsed.</param>
        private void CollapseItem(Node node, GameObject item)
        {
            expandedItems.Remove(item.name);
            if (item.transform.Find("Foreground/Expand Icon").gameObject.TryGetComponentOrLog(out RectTransform rectTransform))
            {
                rectTransform.Rotate(0, 0, 90);
            }
            foreach (Node child in node.Children())
            {
                RemoveItem(child);
            }
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

            if (roots.Count == 0)
            {
                Debug.LogWarning("Graph has no roots. TreeView will be empty.");
            }
        }
    }
}
