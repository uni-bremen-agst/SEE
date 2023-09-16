using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private static string CleanupID(string id) => id?.Replace('/', '\\');

        /// <summary>
        /// Adds the given <paramref name="node"/> to the bottom of the tree window.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        private void AddNode(Node node)
        {
            int children = node.NumberOfChildren() + Mathf.Min(node.Outgoings.Count, 1) + Mathf.Min(node.Incomings.Count, 1);
            AddItem(CleanupID(node.ID), CleanupID(node.Parent?.ID),
                    children, node.ToShortString(), node.Level,
                    NODE_TYPE_UNICODE, i => CollapseNode(node, i), i => ExpandNode(node, i));
        }

        /// <summary>
        /// Adds the given item beneath its parent to the tree window.
        /// </summary>
        /// <param name="id">The ID of the item to be added.</param>
        /// <param name="parentId">The ID of the parent of the item to be added.</param>
        /// <param name="children">The number of children of the item to be added.</param>
        /// <param name="text">The text of the item to be added.</param>
        /// <param name="level">The level of the item to be added.</param>
        /// <param name="icon">The icon of the item to be added, given as a unicode character.</param>
        /// <param name="collapseItem">A function that collapses the item.</param>
        /// <param name="expandItem">A function that expands the item.</param>
        private void AddItem(string id, string parentId, int children, string text, int level,
                             char icon, Action<GameObject> collapseItem, Action<GameObject> expandItem)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(TREE_ITEM_PREFAB, content, false);
            if (parentId != null)
            {
                // Position the item below its parent.
                // TODO: Use colors from the city (e.g., depending on node type).
                // TODO: Include number badge in title.
                item.transform.SetSiblingIndex(content.Find(parentId).GetSiblingIndex() + 1);
                item.transform.Find("Foreground").localPosition += new Vector3(IndentShift * level, 0, 0);
            }
            // Slashes will cause problems later on, so we replace them with backslashes.
            // NOTE: This becomes a problem if two nodes A and B exist where node A's name contains slashes and node B
            //       has an identical name, except for all slashes being replaced by backslashes.
            //       I hope this is unlikely enough to not be a problem for now.
            item.name = CleanupID(id);
            item.transform.Find("Foreground/Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = text;
            item.transform.Find("Foreground/Type Icon").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = icon.ToString();
            if (children <= 0)
            {
                item.transform.Find("Foreground/Expand Icon").gameObject.SetActive(false);
            }
            else if (item.TryGetComponentOrLog(out Button button))
            {
                button.onClick.AddListener(() =>
                {
                    if (expandedItems.Contains(id))
                    {
                        collapseItem(item);
                    }
                    else
                    {
                        expandItem(item);
                    }
                });

                // If this item was previously expanded, we need to expand it again.
                if (expandedItems.Contains(id))
                {
                    expandItem(item);
                }
            }
        }

        /// <summary>
        /// Removes the given <paramref name="node"/>'s children from the tree window.
        /// </summary>
        /// <param name="node">The node to be removed.</param>
        private void RemoveNodeChildren(Node node)
        {
            foreach ((string ID, Node child) in GetChildItems(node))
            {
                RemoveItem(ID, child, GetChildItems);
            }
            return;

            IEnumerable<(string ID, Node child)> GetChildItems(Node n)
            {
                string cleanId = CleanupID(n.ID);
                IEnumerable<(string, Node)> children = n.Children().Select(x => (CleanupID(x.ID), x));
                // We need to remove the "Outgoing" and "Incoming" buttons if they exist, along with their children.
                if (n.Outgoings.Count > 0)
                {
                    children = appendEdgeChildren("Outgoing", n.Outgoings);
                }
                if (n.Incomings.Count > 0)
                {
                    children = appendEdgeChildren("Incoming", n.Incomings);
                }
                return children;

                IEnumerable<(string, Node)> appendEdgeChildren(string edgeType, IEnumerable<Edge> edges)
                {
                    return children.Append((cleanId + "#" + edgeType, null))
                                   .Concat(edges.Select<Edge, (string, Node)>(
                                               x => ($"{cleanId}#{edgeType}#{CleanupID(x.ID)}", null))
                                           );
                }
            }
        }

        /// <summary>
        /// Removes the item with the given <paramref name="id"/> from the tree window.
        /// Calls itself recursively for all children of the item.
        /// </summary>
        /// <param name="id">The ID of the item to be removed.</param>
        /// <param name="initial">The initial item whose children will be removed.</param>
        /// <param name="getChildItems">A function that returns the children, along with their ID, of an item.</param>
        /// <typeparam name="T">The type of the item.</typeparam>
        private void RemoveItem<T>(string id, T initial, Func<T,IEnumerable<(string ID, T child)>> getChildItems)
        {
            GameObject item = content.Find(id)?.gameObject;
            if (item == null)
            {
                Debug.LogWarning($"Item {id} not found.");
                return;
            }

            if (expandedItems.Contains(id) && initial != null)
            {
                foreach ((string ID, T child) in getChildItems(initial))
                {
                    RemoveItem(ID, child, getChildItems);
                }
            }
            Destroyer.Destroy(item);
        }

        /// <summary>
        /// Removes the item with the given <paramref name="id"/> from the tree window.
        /// </summary>
        /// <param name="id">The ID of the item to be removed.</param>
        private void RemoveItem(string id) => RemoveItem<object>(id, null, null);

        /// <summary>
        /// Expands the given <paramref name="item"/>.
        /// This does not add the item's children to the tree window.
        /// </summary>
        /// <param name="item">The item to be expanded.</param>
        private void ExpandItem(GameObject item)
        {
            expandedItems.Add(item.name);
            if (item.transform.Find("Foreground/Expand Icon").gameObject.TryGetComponentOrLog(out RectTransform rectTransform))
            {
                // TODO: Animate this.
                rectTransform.Rotate(0, 0, -90);
            }
        }

        /// <summary>
        /// Collapses the given <paramref name="item"/>.
        /// This does not remove the item's children from the tree window.
        /// </summary>
        /// <param name="item">The item to be collapsed.</param>
        private void CollapseItem(GameObject item)
        {
            expandedItems.Remove(item.name);
            if (item.transform.Find("Foreground/Expand Icon").gameObject.TryGetComponentOrLog(out RectTransform rectTransform))
            {
                rectTransform.Rotate(0, 0, 90);
            }
        }

        /// <summary>
        /// Expands the given <paramref name="item"/>.
        /// Its children will be added to the tree window.
        /// </summary>
        /// <param name="node">The node represented by the item.</param>
        /// <param name="item">The item to be expanded.</param>
        private void ExpandNode(Node node, GameObject item)
        {
            ExpandItem(item);

            foreach (Node child in node.Children())
            {
                AddNode(child);
            }

            if (node.Outgoings.Count > 0)
            {
                AddEdgeButton("Outgoing", OUTGOING_EDGE_UNICODE, node.Outgoings);
            }
            if (node.Incomings.Count > 0)
            {
                AddEdgeButton("Incoming", INCOMING_EDGE_UNICODE, node.Incomings);
            }
            return;

            void AddEdgeButton(string edgesType, char icon, ICollection<Edge> edges)
            {
                string cleanedId = CleanupID(node.ID);
                string id = $"{cleanedId}#{edgesType}";
                // Note that an edge may appear multiple times in the tree view,
                // hence we make its ID dependent on the node it is connected to,
                // and whether it is an incoming or outgoing edge (to cover self-loops).
                AddItem(id, cleanedId, edges.Count, $"{edgesType} Edges", node.Level + 1, icon,
                        i =>
                        {
                            CollapseItem(i);
                            foreach (Edge edge in edges)
                            {
                                RemoveItem($"{id}#{CleanupID(edge.ID)}");
                            }
                        }, i =>
                        {
                            ExpandItem(i);
                            foreach (Edge edge in edges)
                            {
                                AddItem($"{id}#{CleanupID(edge.ID)}", id, 0, edge.ToShortString(), node.Level + 2, EDGE_TYPE_UNICODE, null, null);
                            }
                        });
            }
        }

        /// <summary>
        /// Collapses the given <paramref name="item"/>.
        /// Its children will be removed from the tree window.
        /// </summary>
        /// <param name="node">The node represented by the item.</param>
        /// <param name="item">The item to be collapsed.</param>
        private void CollapseNode(Node node, GameObject item)
        {
            CollapseItem(item);
            RemoveNodeChildren(node);
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
                AddNode(root);
            }

            if (roots.Count == 0)
            {
                Debug.LogWarning("Graph has no roots. TreeView will be empty.");
            }
        }
    }
}
