using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.UI.Window.TreeWindow
{
    /// <summary>
    /// Parts of the tree window that are specific to the desktop UI.
    /// </summary>
    public partial class TreeWindow
    {
        /// <summary>
        /// Transform of the object containing the items of the tree window.
        /// </summary>
        private RectTransform items;

        /// <summary>
        /// Component that allows scrolling through the items of the tree window.
        /// </summary>
        private ScrollRect scrollRect;

        /// <summary>
        /// A set of all items (node IDs) that have been expanded.
        /// Note that this may contain items that are not currently visible due to collapsed parents.
        /// Such items will be expanded when they become visible again.
        /// </summary>
        private readonly ISet<string> expandedItems = new HashSet<string>();

        /// <summary>
        /// The amount by which the text of an item is indented per level.
        /// </summary>
        private const int indentShift = 22;

        /// <summary>
        /// Replace slashes with backslashes in the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The ID to be cleaned up.</param>
        /// <returns>The cleaned up ID.</returns>
        private static string CleanupID(string id) => id?.Replace('/', '\\');

        /// <summary>
        /// The alpha keys for the gradient of a menu item (fully opaque).
        /// </summary>
        private static readonly GradientAlphaKey[] alphaKeys = { new(1, 0), new(1, 1) };

        /// <summary>
        /// The input field in which the user can enter a search term.
        /// </summary>
        private TMP_InputField SearchField;

        /// <summary>
        /// Adds the given <paramref name="node"/> to the bottom of the tree window.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        private void AddNode(Node node)
        {
            GameObject nodeGameObject = GraphElementIDMap.Find(node.ID);
            int children = node.NumberOfChildren() + Mathf.Min(node.Outgoings.Count, 1) + Mathf.Min(node.Incomings.Count, 1);

            AddItem(CleanupID(node.ID), CleanupID(node.Parent?.ID),
                    children, node.ToShortString(), node.Level, nodeTypeUnicode, nodeGameObject,
                    i => CollapseNode(node, i), i => ExpandNode(node, i, nodeGameObject));
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
        /// <param name="itemGameObject">The game object of the element represented by the item. May be null.</param>
        /// <param name="collapseItem">A function that collapses the item.</param>
        /// <param name="expandItem">A function that expands the item.</param>
        private void AddItem(string id, string parentId, int children, string text, int level,
                             char icon, GameObject itemGameObject,
                             Action<GameObject> collapseItem, Action<GameObject> expandItem)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(treeItemPrefab, items, false);
            Transform foreground = item.transform.Find("Foreground");
            GameObject expandIcon = foreground.Find("Expand Icon").gameObject;
            TextMeshProUGUI textMesh = foreground.Find("Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            TextMeshProUGUI iconMesh = foreground.Find("Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            Color[] gradient = null;
            Transform parent = null;

            textMesh.text = text;
            iconMesh.text = icon.ToString();

            if (parentId != null)
            {
                // Position the item below its parent.
                parent = items.Find(parentId);
                item.transform.SetSiblingIndex(parent.GetSiblingIndex() + 1);
                foreground.localPosition += new Vector3(indentShift * level, 0, 0);
            }

            ColorItem();

            // Slashes will cause problems later on, so we replace them with backslashes.
            // NOTE: This becomes a problem if two nodes A and B exist where node A's name contains slashes and node B
            //       has an identical name, except for all slashes being replaced by backslashes.
            //       I hope this is unlikely enough to not be a problem for now.
            item.name = CleanupID(id);
            if (children <= 0)
            {
                expandIcon.SetActive(false);
            }
            else if (expandedItems.Contains(id))
            {
                // If this item was previously expanded, we need to expand it again.
                expandItem(item);
            }

            RegisterClickHandler();
            AnimateIn();
            return;

            void ColorItem()
            {
                if (itemGameObject != null)
                {
                    if (itemGameObject.IsNode())
                    {
                        // We add a slight gradient to make it look nicer.
                        Color color = itemGameObject.GetComponent<Renderer>().material.color;
                        gradient = new[] { color, color.Darker(0.3f) };
                    }
                    else if (itemGameObject.IsEdge())
                    {
                        (Color start, Color end) = itemGameObject.EdgeOperator().TargetColor;
                        gradient = new[] { start, end };
                    }
                    else
                    {
                        throw new ArgumentException("Item must be either a node or an edge.");
                    }
                }
                else
                {
                    gradient = new[] { Color.gray, Color.gray.Darker() };
                }

                item.transform.Find("Background").GetComponent<UIGradient>().EffectGradient.SetKeys(gradient.ToGradientColorKeys().ToArray(), alphaKeys);

                // We also need to set the text color to a color that is readable on the background color.
                Color foregroundColor = gradient.Aggregate((x, y) => (x + y) / 2).IdealTextColor();
                textMesh.color = foregroundColor;
                iconMesh.color = foregroundColor;
                expandIcon.GetComponent<Graphic>().color = foregroundColor;
            }

            void AnimateIn()
            {
                item.transform.localScale = new Vector3(1, 0, 1);
                item.transform.DOScaleY(1, duration: 0.5f);
            }

            void RegisterClickHandler()
            {
                if (item.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    // Right click highlights the node, left/middle click expands/collapses it.
                    pointerHelper.ClickEvent.AddListener(e =>
                    {
                        // TODO: In the future, highlighting the node should be one available option in a right-click menu.
                        if (e.button == PointerEventData.InputButton.Right)
                        {
                            if (itemGameObject != null)
                            {
                                itemGameObject.Operator().Highlight(duration: 10);
                            }
                            else
                            {
                                ShowNotification.Warn("No game object", "There is nothing to highlight for this item.");
                            }
                        }
                        else
                        {
                            if (expandedItems.Contains(id))
                            {
                                collapseItem?.Invoke(item);
                            }
                            else
                            {
                                expandItem?.Invoke(item);
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Removes the given <paramref name="node"/>'s children from the tree window.
        /// </summary>
        /// <param name="node">The node to be removed.</param>
        private void RemoveNodeChildren(Node node)
        {
            foreach ((string childID, Node child) in GetChildItems(node))
            {
                RemoveItem(childID, child, GetChildItems);
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
                                   .Concat(edges.Select<Edge, (string, Node)>(x => ($"{cleanId}#{edgeType}#{CleanupID(x.ID)}", null)));
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
        private void RemoveItem<T>(string id, T initial, Func<T, IEnumerable<(string ID, T child)>> getChildItems)
        {
            GameObject item = items.Find(id)?.gameObject;
            if (item == null)
            {
                Debug.LogWarning($"Item {id} not found.");
                return;
            }

            if (expandedItems.Contains(id) && initial != null)
            {
                foreach ((string childID, T child) in getChildItems(initial))
                {
                    RemoveItem(childID, child, getChildItems);
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
                rectTransform.DORotate(new Vector3(0, 0, -180), duration: 0.5f);
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
                rectTransform.DORotate(new Vector3(0, 0, -90), duration: 0.5f);
            }
        }

        /// <summary>
        /// Expands the given <paramref name="item"/>.
        /// Its children will be added to the tree window.
        /// </summary>
        /// <param name="node">The node represented by the item.</param>
        /// <param name="item">The item to be expanded.</param>
        /// <param name="nodeGameObject">The game object of the element represented by the node.</param>
        private void ExpandNode(Node node, GameObject item, GameObject nodeGameObject)
        {
            ExpandItem(item);

            foreach (Node child in node.Children())
            {
                AddNode(child);
            }

            if (node.Outgoings.Count > 0)
            {
                AddEdgeButton("Outgoing", outgoingEdgeUnicode, node.Outgoings);
            }
            if (node.Incomings.Count > 0)
            {
                AddEdgeButton("Incoming", incomingEdgeUnicode, node.Incomings);
            }
            return;

            void AddEdgeButton(string edgesType, char icon, ICollection<Edge> edges)
            {
                string cleanedId = CleanupID(node.ID);
                string id = $"{cleanedId}#{edgesType}";
                // Note that an edge may appear multiple times in the tree view,
                // hence we make its ID dependent on the node it is connected to,
                // and whether it is an incoming or outgoing edge (to cover self-loops).
                AddItem(id, cleanedId, edges.Count, $"{edgesType} Edges", node.Level + 1, icon, nodeGameObject,
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
                                GameObject edgeObject = GraphElementIDMap.Find(edge.ID);
                                AddItem($"{id}#{CleanupID(edge.ID)}", id, 0, edge.ToShortString(), node.Level + 2, edgeTypeUnicode, edgeObject, null, null);
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

        /// <summary>
        /// Searches for the given <paramref name="searchTerm"/> in the graph
        /// and displays the results in the tree window.
        /// </summary>
        /// <param name="searchTerm">The search term to be searched for.</param>
        private void SearchFor(string searchTerm)
        {
            ClearTree();
            if (searchTerm == null || searchTerm.Trim().Length == 0)
            {
                AddRoots();
            }

            foreach (Node node in searcher.Search(searchTerm))
            {
                GameObject nodeGameObject = GraphElementIDMap.Find(node.ID, mustFindElement: true);
                AddItem(CleanupID(node.ID), null,
                        0, node.ToShortString(), 0, nodeTypeUnicode, nodeGameObject,
                        null, _ => MakeVisible(node).Forget());
            }

            items.position = items.position.WithXYZ(y: 0);
        }

        /// <summary>
        /// Makes the given <paramref name="node"/> visible in the tree window by expanding all its parents
        /// and scrolling to it.
        /// </summary>
        /// <param name="node">The node to be made visible.</param>
        private async UniTaskVoid MakeVisible(Node node)
        {
            SearchField.onValueChanged.RemoveListener(SearchFor);
            SearchField.text = string.Empty;
            SearchField.ReleaseSelection();
            SearchField.onValueChanged.AddListener(SearchFor);
            ClearTree();

            // We need to find a path from the root to the node, which we do by working our way up the hierarchy.
            // We then expand all nodes on the path.
            Node current = node;
            while (current.Parent != null)
            {
                current = current.Parent;
                expandedItems.Add(CleanupID(current.ID));
            }

            // FIXME: Node sometimes gets placed at wrong spot. This seems to be a very annoying racing condition.
            //        The only fix I can think of involves creating a data structure that represents the tree view
            //        and then using that to set the sibling indices of the items all at once.
            AddRoots();

            // We need to wait until the transform actually exists.
            await UniTask.Yield();
            RectTransform item = (RectTransform)items.Find(CleanupID(node.ID));
            scrollRect.ScrollTo(item, duration: 1f);

            // Make element blink.
            UIGradient uiGradient = item.Find("Background").GetComponent<UIGradient>();
            Gradient gradient = uiGradient.EffectGradient;
            DOTween.To(() => uiGradient.EffectGradient.colorKeys[0].color, x =>
                       {
                           gradient.SetKeys(new[]
                           {
                               new GradientColorKey(x, 0), new GradientColorKey(x.Darker(), 1)
                           }, alphaKeys);
                           uiGradient.EffectGradient = gradient;
                       },
                       gradient.colorKeys[0].color.Invert(), duration: 0.5f)
                   .SetEase(Ease.Linear)
                   .SetLoops(6, LoopType.Yoyo).Play();
        }

        /// <summary>
        /// Clears the tree view of all items.
        /// </summary>
        private void ClearTree()
        {
            foreach (Transform child in items)
            {
                Destroyer.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Adds the roots of the graph to the tree view.
        /// </summary>
        private void AddRoots()
        {
            // We will traverse the graph and add each node to the tree view.
            IList<Node> roots = Graph.GetRoots();
            foreach (Node root in roots)
            {
                AddNode(root);
            }

            if (roots.Count == 0)
            {
                ShowNotification.Warn("Empty graph", "Graph has no roots. TreeView will be empty.");
            }
        }

        protected override void StartDesktop()
        {
            if (Graph == null)
            {
                Debug.LogError("Graph must be set before starting the tree window.");
                return;
            }

            Title = $"{Graph.Name} – Tree View";
            base.StartDesktop();
            Transform root = PrefabInstantiator.InstantiatePrefab(treeWindowPrefab, Window.transform.Find("Content"), false).transform;
            items = (RectTransform)root.Find("Content/Items");
            scrollRect = root.gameObject.MustGetComponent<ScrollRect>();

            SearchField = root.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();
            SearchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            SearchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            SearchField.onValueChanged.AddListener(SearchFor);

            AddRoots();
        }
    }
}
