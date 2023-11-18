using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.UI.Notification;
using SEE.UI.PopupMenu;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Node = SEE.DataModel.DG.Node;

namespace SEE.UI.Window.TreeWindow
{
    /// <summary>
    /// Parts of the tree window that are specific to the desktop UI.
    /// </summary>
    public partial class TreeWindow
    {
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
        /// Orders the tree below the given <paramref name="orderBelow"/> node according to the graph hierarchy.
        /// This needs to be called whenever the tree is expanded.
        /// </summary>
        /// <param name="orderBelow">The node below which the tree should be ordered.</param>
        private void OrderTree(Node orderBelow)
        {
            int index = items.Find(CleanupID(orderBelow.ID)).GetSiblingIndex();
            OrderTreeRecursive(orderBelow);

            return;

            // Orders the item with the given id to the current index and increments the index.
            void OrderItemHere(string id)
            {
                Transform item = items.Find(id);
                if (item == null)
                {
                    Debug.LogError($"Item {id} not found.");
                }
                else
                {
                    item.SetSiblingIndex(index++);
                }
            }

            // Recurses over the tree in pre-order and assigns indices to each node.
            void OrderTreeRecursive(Node node)
            {
                string id = CleanupID(node.ID);
                OrderItemHere(id);
                if (expandedItems.Contains(id))
                {
                    foreach (Node child in node.Children().OrderBy(x => x.SourceName))
                    {
                        OrderTreeRecursive(child);
                    }

                    HandleEdges($"{id}#Outgoing", node.Outgoings);
                    HandleEdges($"{id}#Incoming", node.Incomings);
                }
            }

            // Orders the edges under the given id (outgoing/incoming) to the current index and increments the index.
            void HandleEdges(string edgesId, ICollection<Edge> edges)
            {
                if (edges.Count > 0)
                {
                    OrderItemHere(edgesId);
                    if (expandedItems.Contains(edgesId))
                    {
                        foreach (Edge edge in edges)
                        {
                            OrderItemHere($"{edgesId}#{CleanupID(edge.ID)}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the given <paramref name="node"/> to the bottom of the tree window.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        private void AddNode(Node node)
        {
            GameObject nodeGameObject = GraphElementIDMap.Find(node.ID);
            int children = node.NumberOfChildren() + Mathf.Min(node.Outgoings.Count, 1) + Mathf.Min(node.Incomings.Count, 1);

            AddItem(CleanupID(node.ID), children, node.ToShortString(), node.Level, nodeTypeUnicode, nodeGameObject, node,
                    item => CollapseNode(node, item), (item, order) => ExpandNode(node, item, orderTree: order));
        }

        /// <summary>
        /// Adds the given item to the tree window.
        /// </summary>
        /// <param name="id">The ID of the item to be added.</param>
        /// <param name="children">The number of children of the item to be added.</param>
        /// <param name="text">The text of the item to be added.</param>
        /// <param name="level">The level of the item to be added.</param>
        /// <param name="icon">The icon of the item to be added, given as a unicode character.</param>
        /// <param name="representedGameObject">The game object of the element represented by the item. May be null.</param>
        /// <param name="representedGraphElement">The graph element represented by the item. May be null.</param>
        /// <param name="collapseItem">A function that collapses the item.
        /// It takes the item that was collapsed as an argument.</param>
        /// <param name="expandItem">A function that expands the item.
        /// It takes the item that was expanded and a boolean indicating whether the
        /// tree should be ordered after expanding the item as arguments.</param>
        private void AddItem(string id, int children, string text, int level,
                             char icon, GameObject representedGameObject, GraphElement representedGraphElement,
                             Action<GameObject> collapseItem, Action<GameObject, bool> expandItem)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(treeItemPrefab, items, false);
            Transform background = item.transform.Find("Background");
            Transform foreground = item.transform.Find("Foreground");
            GameObject expandIcon = foreground.Find("Expand Icon").gameObject;
            TextMeshProUGUI textMesh = foreground.Find("Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            TextMeshProUGUI iconMesh = foreground.Find("Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();

            textMesh.text = text;
            iconMesh.text = icon.ToString();

            foreground.localPosition += new Vector3(indentShift * level, 0, 0);
            background.localPosition += new Vector3(indentShift * level, 0, 0);

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
                // The tree should not be reordered after this – this should only happen at the end of the expansion,
                // and thus needs to be done at the originating call.
                expandItem(item, false);
            }

            RegisterClickHandler();
            AnimateIn();
            return;

            // Colors the item according to its game object.
            void ColorItem()
            {
                Color[] gradient;
                if (representedGameObject != null)
                {
                    if (representedGameObject.IsNode())
                    {
                        // We add a slight gradient to make it look nicer.
                        Color color = representedGameObject.GetComponent<Renderer>().material.color;
                        gradient = new[] { color, color.Darker(0.3f) };
                    }
                    else if (representedGameObject.IsEdge())
                    {
                        (Color start, Color end) = representedGameObject.EdgeOperator().TargetColor;
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

                background.GetComponent<UIGradient>().EffectGradient.SetKeys(gradient.ToGradientColorKeys().ToArray(), alphaKeys);

                // We also need to set the text color to a color that is readable on the background color.
                Color foregroundColor = gradient.Aggregate((x, y) => (x + y) / 2).IdealTextColor();
                textMesh.color = foregroundColor;
                iconMesh.color = foregroundColor;
                expandIcon.GetComponent<Graphic>().color = foregroundColor;
            }

            // Expands the item by animating its scale.
            void AnimateIn()
            {
                item.transform.localScale = new Vector3(1, 0, 1);
                item.transform.DOScaleY(1, duration: 0.5f);
            }

            // Registers a click handler for the item.
            void RegisterClickHandler()
            {
                if (item.TryGetComponentOrLog(out PointerHelper pointerHelper))
                {
                    // Right click opens the context menu, left/middle click expands/collapses the item.
                    pointerHelper.ClickEvent.AddListener(e =>
                    {
                        if (e.button == PointerEventData.InputButton.Right)
                        {
                            if (representedGraphElement == null)
                            {
                                // There are no applicable actions for this item.
                                return;
                            }

                            // We want all applicable actions for the element, except ones where the element
                            // element is shown in the TreeView, since we are already in the TreeView.
                            IEnumerable<PopupMenuAction> actions = ContextMenuAction
                                                                   .GetApplicableOptions(representedGraphElement,
                                                                                         representedGameObject)
                                                                   .Where(x => !x.Name.Contains("TreeView"));
                            ContextMenu.ClearActions();
                            ContextMenu.AddActions(actions);
                            ContextMenu.MoveTo(e.position);
                            ContextMenu.ShowMenu().Forget();
                        }
                        else
                        {
                            if (expandedItems.Contains(id))
                            {
                                collapseItem?.Invoke(item);
                            }
                            else
                            {
                                expandItem?.Invoke(item, true);
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
                    children = AppendEdgeChildren("Outgoing", n.Outgoings);
                }
                if (n.Incomings.Count > 0)
                {
                    children = AppendEdgeChildren("Incoming", n.Incomings);
                }
                return children;

                IEnumerable<(string, Node)> AppendEdgeChildren(string edgeType, IEnumerable<Edge> edges)
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
        /// <param name="orderTree">Whether to order the tree after expanding the node.</param>
        private void ExpandNode(Node node, GameObject item, bool orderTree = false)
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
            if (orderTree)
            {
                OrderTree(node);
            }
            return;

            void AddEdgeButton(string edgesType, char icon, ICollection<Edge> edges)
            {
                string cleanedId = CleanupID(node.ID);
                string id = $"{cleanedId}#{edgesType}";
                // Note that an edge may appear multiple times in the tree view,
                // hence we make its ID dependent on the node it is connected to,
                // and whether it is an incoming or outgoing edge (to cover self-loops).
                AddItem(id, edges.Count, $"{edgesType} Edges", node.Level + 1, icon,
                        representedGameObject: null, representedGraphElement: null,
                        collapsedItem =>
                        {
                            CollapseItem(collapsedItem);
                            foreach (Edge edge in edges)
                            {
                                RemoveItem($"{id}#{CleanupID(edge.ID)}");
                            }
                        }, (expandedItem, order) =>
                        {
                            ExpandItem(expandedItem);
                            foreach (Edge edge in edges)
                            {
                                GameObject edgeObject = GraphElementIDMap.Find(edge.ID);
                                AddItem($"{id}#{CleanupID(edge.ID)}", 0, edge.ToShortString(), node.Level + 2, edgeTypeUnicode, edgeObject, edge, null, null);
                            }
                            if (order)
                            {
                                OrderTree(node);
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
                return;
            }

            foreach (Node node in searcher.Search(searchTerm))
            {
                GameObject nodeGameObject = GraphElementIDMap.Find(node.ID, mustFindElement: true);
                AddItem(CleanupID(node.ID),
                        0, node.ToShortString(), 0, nodeTypeUnicode, nodeGameObject, node,
                        null, (_, _) => RevealElement(node).Forget());
            }

            items.position = items.position.WithXYZ(y: 0);
        }

        /// <summary>
        /// Makes the given <paramref name="element"/> visible in the tree window by expanding all its parents
        /// and scrolling to it.
        /// If an edge is given, the source/target node will be made visible instead,
        /// depending on the value of <paramref name="viaSource"/>.
        /// </summary>
        /// <param name="element">The element to be made visible.</param>
        /// <param name="viaSource">Whether to make the source or target node of the edge visible.</param>
        public async UniTaskVoid RevealElement(GraphElement element, bool viaSource = false)
        {
            if (SearchField == null)
            {
                // We need to wait until the window is initialized.
                // This case may occur when the method is called from the outside.
                await UniTask.WaitUntil(() => SearchField != null);
            }
            SearchField.onValueChanged.RemoveListener(SearchFor);
            SearchField.text = string.Empty;
            SearchField.ReleaseSelection();
            SearchField.onValueChanged.AddListener(SearchFor);
            ClearTree();

            Node current = element switch
            {
                Node node => node,
                Edge edge => viaSource ? edge.Source : edge.Target,
                _ => throw new ArgumentOutOfRangeException(nameof(element))
            };
            string transformID = CleanupID(current.ID);
            if (element is Edge)
            {
                expandedItems.Add(transformID);
                transformID += viaSource ? "#Outgoing" : "#Incoming";
                expandedItems.Add(transformID);
                transformID += $"#{CleanupID(element.ID)}";
            }

            // We need to find a path from the root to the node, which we do by working our way up the hierarchy.
            // We then expand all nodes on the path.
            while (current.Parent != null)
            {
                current = current.Parent;
                expandedItems.Add(CleanupID(current.ID));
            }

            AddRoots();

            // We need to wait until the transform actually exists, hence the yield.
            await UniTask.Yield();
            RectTransform item = (RectTransform)items.Find(transformID);
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
