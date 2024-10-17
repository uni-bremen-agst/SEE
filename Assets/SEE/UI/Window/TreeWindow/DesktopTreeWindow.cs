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
using ArgumentException = System.ArgumentException;
using Edge = SEE.DataModel.DG.Edge;
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
        private TMP_InputField searchField;

        /// <summary>
        /// The button that opens the filter menu.
        /// </summary>
        private ButtonManagerBasic filterButton;

        /// <summary>
        /// The button that opens the grouping menu.
        /// </summary>
        private ButtonManagerBasic groupButton;

        /// <summary>
        /// The button that opens the sorting menu.
        /// </summary>
        private ButtonManagerBasic sortButton;

        /// <summary>
        /// Orders the tree below the given <paramref name="orderBelow"/> group according to the graph hierarchy.
        /// </summary>
        /// <param name="orderBelow">The group below which the tree should be ordered.</param>
        private void OrderTree(TreeWindowGroup orderBelow)
        {
            Transform groupItem = items.Find(CleanupID(orderBelow.Text));
            if (groupItem == null)
            {
                return;
            }

            int groupIndex = groupItem.GetSiblingIndex() + 1;
            foreach (Node node in GetRoots(orderBelow))
            {
                // The groups are always at the top level of the tree window.
                // Thus, we put the root nodes (indented by one level) directly below the group.
                items.Find(ElementId(node, orderBelow)).SetSiblingIndex(groupIndex);
                // The next root should be added below the last one, which is why we use the current index.
                groupIndex = OrderTree(node, 1, orderBelow) ?? groupIndex;
            }
        }

        /// <summary>
        /// Orders the tree below the given <paramref name="orderBelow"/> node according to the graph hierarchy.
        /// This needs to be called whenever the tree is expanded.
        /// </summary>
        /// <param name="orderBelow">The node below which the tree should be ordered.</param>
        /// <param name="nodeLevel">The level of the given <paramref name="orderBelow"/> node.</param>
        /// <param name="inGroup">The group in which <paramref name="orderBelow"/> is contained, if any.</param>
        /// <returns>The index in the hierarchy of the last node handled by this method.
        /// If no nodes were ordered, null is returned.</returns>
        private int? OrderTree(Node orderBelow, int? nodeLevel = null, TreeWindowGroup inGroup = null)
        {
            Transform nodeItem = items.Find(ElementId(orderBelow, inGroup));
            if (nodeItem == null)
            {
                return null;
            }

            // We determine the node level based on the indent of the foreground.
            if (!nodeLevel.HasValue)
            {
                nodeLevel = Mathf.RoundToInt(((RectTransform)nodeItem.Find("Foreground")).offsetMin.x) / indentShift;
            }
            int index = nodeItem.GetSiblingIndex();

            OrderTreeRecursive(orderBelow, nodeLevel.Value);

            return index;

            // Orders the item with the given id to the current index and increments the index.
            void OrderItemHere(string id, int level)
            {
                Transform item = items.Find(id);
                if (item != null)
                {
                    item.SetSiblingIndex(index++);
                    RectTransform foreground = (RectTransform)item.Find("Foreground");
                    RectTransform background = (RectTransform)item.Find("Background");
                    foreground.offsetMin = foreground.offsetMin.WithXY(x: indentShift * level);
                    background.offsetMin = background.offsetMin.WithXY(x: indentShift * level);
                }
            }

            // Recurses over the tree in pre-order and assigns indices to each node.
            void OrderTreeRecursive(Node node, int level)
            {
                string id = ElementId(node, inGroup);
                OrderItemHere(id, level);
                if (expandedItems.Contains(id))
                {
                    IEnumerable<Node> children = searcher.Sorter.Apply(WithHiddenChildren(node.Children(), inGroup));
                    // When grouping is active, we sort by the count of group elements in this node.
                    if (grouper.IsActive)
                    {
                        children = children.OrderByDescending(x => grouper.DescendantsInGroup(x, inGroup));
                    }
                    foreach (Node child in children)
                    {
                        OrderTreeRecursive(child, level + 1);
                    }

                    foreach ((List<Edge> edges, string edgesType) in RelevantEdges(node, inGroup))
                    {
                        HandleEdges($"{id}#{edgesType}", edges, level + 1);
                    }
                }
            }

            // Orders the edges under the given id (outgoing/incoming) to the current index and increments the index.
            void HandleEdges(string edgesId, ICollection<Edge> edges, int level)
            {
                if (edges.Count > 0)
                {
                    OrderItemHere(edgesId, level);
                    if (expandedItems.Contains(edgesId))
                    {
                        foreach (Edge edge in edges)
                        {
                            OrderItemHere($"{edgesId}#{CleanupID(edge.ID)}", level + 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the TreeWindow ID of the given <paramref name="element"/> in the given <paramref name="group"/>.
        /// </summary>
        /// <param name="element">The element whose ID shall be returned.</param>
        /// <param name="group">The group in which the element is contained, if any.</param>
        /// <returns>The TreeWindow ID of the given <paramref name="element"/> in the given <paramref name="group"/>.</returns>
        private static string ElementId(GraphElement element, TreeWindowGroup group)
        {
            string id = CleanupID(element.ID);
            if (group != null)
            {
                // If it belongs to a group, we will need to append the group name, otherwise the ID will not be unique,
                // as the element may be used repeatedly across multiple groups.
                id = $"{id}#{CleanupID(group.Text)}";
            }
            return id;
        }

        /// <summary>
        /// Adds the given <paramref name="group"/> to the bottom of the tree window.
        /// </summary>
        /// <param name="group">The group to be added.</param>
        private void AddGroup(TreeWindowGroup group)
        {
            AddItem(CleanupID(group.Text), true, $"{group.Text} [{grouper.MembersInGroup(group)}]",
                    group.IconGlyph, gradient: group.Gradient,
                    collapseItem: CollapseGroup, expandItem: ExpandGroup);
            if (expandedItems.Contains(CleanupID(group.Text)))
            {
                ExpandGroup(items.Find(CleanupID(group.Text))?.gameObject, order: false);
            }
            return;

            void CollapseGroup(GameObject item)
            {
                CollapseItem(item);
                foreach (GraphElement element in GetRoots(group))
                {
                    RemoveItem(ElementId(element, group), element,
                               x => x is Node node ? GetChildItems(node, group) : Enumerable.Empty<(string, Node)>());
                }
            }

            void ExpandGroup(GameObject item, bool order)
            {
                ExpandItem(item);
                foreach (Node element in GetRoots(group))
                {
                    AddNode(element, group);
                }
                if (order)
                {
                    OrderTree(group);
                }
            }
        }

        /// <summary>
        /// Whether the given <paramref name="element"/> shall be displayed in the tree window.
        /// </summary>
        /// <param name="element">The element to be checked.</param>
        /// <param name="inGroup">The group in which the element is contained, if any.</param>
        /// <returns>Whether the given <paramref name="element"/> shall be displayed in the tree window.</returns>
        private bool ShouldBeDisplayed(GraphElement element, TreeWindowGroup inGroup = null)
        {
            return (inGroup != null && grouper.IsRelevantFor(element, inGroup)) || (inGroup == null && searcher.Filter.Includes(element));
        }

        /// <summary>
        /// Adds the given <paramref name="node"/> to the bottom of the tree window.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        /// <param name="inGroup">The group in which the node is contained, if any.</param>
        private void AddNode(Node node, TreeWindowGroup inGroup = null)
        {
            GameObject nodeGameObject = GraphElementIDMap.Find(node.ID);
            int children = node.Children().Count(x => ShouldBeDisplayed(x, inGroup)) + node.Edges.Count(x => ShouldBeDisplayed(x, inGroup));

            if (ShouldBeDisplayed(node, inGroup))
            {
                string text = node.ToShortString();
                string id = ElementId(node, inGroup);
                if (inGroup != null)
                {
                    // Not actually the number of direct children, but this doesn't matter, as we only
                    // need it for the text and to check whether there are any children at all.
                    children = grouper.DescendantsInGroup(node, inGroup);
                    if (grouper.GetGroupFor(node) != inGroup)
                    {
                        // This node is only included because it has relevant descendants.
                        text = $"<i>{text}</i>";
                    }
                    if (children > 0)
                    {
                        text = $"{text} [{children}]";
                    }
                }
                AddItem(id, children > 0, text, Icons.Node, nodeGameObject, node,
                        collapseItem: item => CollapseNode(node, item, inGroup),
                        expandItem: (item, order) => ExpandNode(node, item, orderTree: order, inGroup));
            }
            else
            {
                // The node itself may not be included, but its children (or edges) could be.
                // Thus, we assume this invisible node to be expanded by default and add its children.
                ExpandNode(node, null, inGroup: inGroup);
            }
        }

        /// <summary>
        /// Adds the given item to the tree window.
        /// </summary>
        /// <param name="id">The ID of the item to be added.</param>
        /// <param name="hasChildren">Whether the item has children.</param>
        /// <param name="text">The text of the item to be added.</param>
        /// <param name="icon">The icon of the item to be added, given as a unicode character.</param>
        /// <param name="representedGameObject">The game object of the element represented by the item. May be null.</param>
        /// <param name="representedGraphElement">The graph element represented by the item. May be null.</param>
        /// <param name="gradient">The gradient to be used for the item's background. May be null.</param>
        /// <param name="collapseItem">A function that collapses the item.
        /// It takes the item that was collapsed as an argument. May be null.</param>
        /// <param name="expandItem">A function that expands the item.
        /// It takes the item that was expanded and a boolean indicating whether the
        /// tree should be ordered after expanding the item as arguments. May be null.</param>
        private void AddItem(string id, bool hasChildren, string text, char icon,
                             GameObject representedGameObject = null, GraphElement representedGraphElement = null,
                             Color[] gradient = null,
                             Action<GameObject> collapseItem = null, Action<GameObject, bool> expandItem = null)
        {
            GameObject item = PrefabInstantiator.InstantiatePrefab(treeItemPrefab, items, false);
            Transform background = item.transform.Find("Background");
            Transform foreground = item.transform.Find("Foreground");
            GameObject expandIcon = foreground.Find("Expand Icon").gameObject;
            TextMeshProUGUI textMesh = foreground.Find("Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            TextMeshProUGUI iconMesh = foreground.Find("Type Icon").gameObject.MustGetComponent<TextMeshProUGUI>();

            textMesh.text = text;
            iconMesh.text = icon.ToString();

            ColorItem();

            // Slashes will cause problems later on in the `transform.Find` method, so we replace them with backslashes.
            // NOTE: This becomes a problem if two nodes A and B exist where node A's name contains slashes and node B
            //       has an identical name, except for all slashes being replaced by backslashes.
            //       I hope this is unlikely enough to not be a problem for now.
            item.name = id;
            if (!hasChildren)
            {
                expandIcon.SetActive(false);
            }
            else if (expandedItems.Contains(id) && expandItem != null)
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
                if (gradient == null && representedGameObject != null)
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
                else if (gradient == null)
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
                    if (SceneSettings.InputType == PlayerInputType.VRPlayer)
                    {
                        pointerHelper.EnterEvent.AddListener(_ =>
                        {
                            XRSEEActions.OnTreeViewToggle = true;
                            XRSEEActions.TreeViewEntry = item;
                        });
                        pointerHelper.ExitEvent.AddListener(_ => XRSEEActions.OnTreeViewToggle = false);
                        pointerHelper.ThumbstickEvent.AddListener(e =>
                        {
                            if (XRSEEActions.TooltipToggle)
                            {
                                if (representedGraphElement == null)
                                {
                                    // There are no applicable actions for this item.
                                    return;
                                }

                                // We want all applicable actions for the element, except ones where the element
                                // is shown in the TreeWindow, since we are already in the TreeWindow.
                                IEnumerable<PopupMenuEntry> actions = CreateContextMenuActions(contextMenu, e.position, representedGraphElement, representedGameObject);
                                XRSEEActions.TooltipToggle = false;
                                XRSEEActions.OnSelectToggle = true;
                                XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit ray);
                                contextMenu.ShowWith(actions, ray.point);
                            }
                        });
                    }

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
                            // is shown in the TreeWindow, since we are already in the TreeWindow.
                            IEnumerable<PopupMenuEntry> actions = CreateContextMenuActions(contextMenu, e.position, representedGraphElement, representedGameObject);
                            contextMenu.ShowWith(actions, e.position);
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

                IEnumerable<PopupMenuEntry> CreateContextMenuActions (TreeWindowContextMenu contextMenu, Vector2 position, GraphElement representedGraphElement, GameObject representedGameObject)
                {
                    List<PopupMenuAction> appends = new()
                    {
                        new("Hide in TreeWindow", () =>
                        {
                            searcher.Filter.ExcludeElements.Add(representedGraphElement);
                            Rebuild();
                        }, Icons.Hide)
                    };

                    IEnumerable<PopupMenuEntry> actions = ContextMenuAction
                        .GetOptionsForTreeView(contextMenu.ContextMenu, position, representedGraphElement, representedGameObject, appends);

                    return actions.Concat(appends);
                }
            }
        }

        /// <summary>
        /// Returns those nodes within <paramref name="nodes"/> which are included in the current filter,
        /// and transitively adds all children of those nodes within <paramref name="nodes"/>
        /// which are not included in the current filter.
        /// </summary>
        /// <param name="nodes">The nodes to be filtered.</param>
        /// <param name="inGroup">The group in which the nodes are contained, if any.</param>
        /// <returns>The filtered nodes with any hidden transitive children.</returns>
        private IEnumerable<Node> WithHiddenChildren(IList<Node> nodes, TreeWindowGroup inGroup)
        {
            return nodes.Where(x => ShouldBeDisplayed(x, inGroup))
                        .Concat(nodes.Where(x => !ShouldBeDisplayed(x, inGroup))
                                     .SelectMany(x => WithHiddenChildren(x.Children(), inGroup)));
        }

        /// <summary>
        /// Returns those nodes within <paramref name="nodes"/> which are not included in the current filter,
        /// transitively including all hidden children of those nodes.
        /// </summary>
        /// <param name="nodes">The nodes to be filtered.</param>
        /// <param name="inGroup">The group in which the nodes are contained, if any.</param>
        /// <returns>The transitive hidden children of the given nodes.</returns>
        private IEnumerable<Node> HiddenChildren(IEnumerable<Node> nodes, TreeWindowGroup inGroup)
        {
            return nodes.Where(x => !ShouldBeDisplayed(x, inGroup)).SelectMany(x => HiddenChildren(x.Children(), inGroup).Append(x));
        }

        /// <summary>
        /// Removes the given <paramref name="node"/>'s children from the tree window.
        /// </summary>
        /// <param name="node">The node to be removed.</param>
        /// <param name="inGroup">The group in which the node is contained, if any.</param>
        private void RemoveNodeChildren(Node node, TreeWindowGroup inGroup = null)
        {
            foreach ((string childID, Node child) in GetChildItems(node, inGroup))
            {
                RemoveItem(childID, child, x => GetChildItems(x, inGroup));
            }
        }

        /// <summary>
        /// Returns the child items of the given <paramref name="node"/> along with their ID.
        /// </summary>
        /// <param name="node">The node whose child items are requested.</param>
        /// <param name="inGroup">The group in which the node is contained, if any.</param>
        /// <returns>The child items of the given <paramref name="node"/> along with their ID.</returns>
        private IEnumerable<(string ID, Node child)> GetChildItems(Node node, TreeWindowGroup inGroup = null)
        {
            string cleanId = ElementId(node, inGroup);
            IEnumerable<(string, Node)> children = WithHiddenChildren(node.Children(), inGroup).Select(x => (ElementId(x, inGroup), x));
            foreach ((List<Edge> edges, string edgesType) in RelevantEdges(node, inGroup))
            {
                // The "Outgoing" and "Incoming" buttons if they exist, along with their children, belong here too.
                children = children.Append((cleanId + "#" + edgesType, null))
                                   .Concat(edges.Select<Edge, (string, Node)>(x => ($"{cleanId}#{edgesType}#{CleanupID(x.ID)}", null)));
            }
            return children;
        }

        /// <summary>
        /// Removes the item with the given <paramref name="id"/> from the tree window.
        /// Calls itself recursively for all children of the item.
        /// </summary>
        /// <param name="id">The ID of the item to be removed.</param>
        /// <param name="initial">The initial item whose children will be removed.</param>
        /// <param name="getChildItems">A function that returns the children, along with their ID, of an item.</param>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <typeparam name="V">The type of the children of the item.</typeparam>
        private void RemoveItem<T, V>(string id, T initial, Func<T, IEnumerable<(string ID, V child)>> getChildItems) where V : T
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
        private void RemoveItem(string id) => RemoveItem<object, object>(id, null, null);

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
        /// Returns connected and lifted edges of the given <paramref name="node"/>.
        /// The edges are grouped by their type (outgoing, incoming, lifted outgoing, lifted incoming)
        /// and each is returned as a list along with its type. Only those types are included that
        /// have at least one edge.
        /// </summary>
        /// <param name="node">The node whose edges are requested.</param>
        /// <param name="inGroup">The group in which the node is contained, if any.</param>
        /// <returns>The edges of the node, grouped by their type.</returns>
        private IEnumerable<(List<Edge> edges, string edgesType)> RelevantEdges(Node node, TreeWindowGroup inGroup = null)
        {
            List<Edge> outgoings = searcher.Filter.Apply(node.Outgoings).ToList();
            List<Edge> incomings = searcher.Filter.Apply(node.Incomings).ToList();
            // We need to lift edges of any hidden children upwards to the first visible parent, which is
            // this node. We then need to filter them again, since they may have been hidden by the filter.
            List<Node> hiddenChildren = HiddenChildren(node.Children(), inGroup).ToList();
            List<Edge> liftedOutgoings = searcher.Filter.Apply(hiddenChildren.SelectMany(x => x.Outgoings)).ToList();
            List<Edge> liftedIncomings = searcher.Filter.Apply(hiddenChildren.SelectMany(x => x.Incomings)).ToList();
            return new[]
                {
                    (outgoings, "Outgoing"),
                    (incomings, "Incoming"),
                    (liftedOutgoings, "Lifted Outgoing"),
                    (liftedIncomings, "Lifted Incoming")
                }.Select(x => inGroup == null ? x : (x.Item1.Where(y => grouper.IsRelevantFor(y, inGroup)).ToList(), x.Item2))
                 .Where(x => x.Item1.Count > 0);
        }

        /// <summary>
        /// Expands the given <paramref name="item"/>.
        /// Its children will be added to the tree window.
        /// </summary>
        /// <param name="node">The node represented by the item.</param>
        /// <param name="item">The item to be expanded. If this is null
        /// (i.e., no item actually exists in the TreeWindow)
        /// only the children of the node will be added, not its connected edges.</param>
        /// <param name="orderTree">Whether to order the tree after expanding the node.</param>
        /// <param name="inGroup">The group in which the node is contained, if any.</param>
        private void ExpandNode(Node node, GameObject item, bool orderTree = false, TreeWindowGroup inGroup = null)
        {
            foreach (Node child in node.Children())
            {
                AddNode(child, inGroup);
            }

            if (item != null)
            {
                ExpandItem(item);

                foreach ((List<Edge> edges, string edgesType) in RelevantEdges(node, inGroup))
                {
                    AddEdgeButton(edges, edgesType);
                }

                if (orderTree)
                {
                    OrderTree(node, inGroup: inGroup);
                }
            }
            return;

            void AddEdgeButton(ICollection<Edge> edges, string edgesType)
            {
                char icon = edgesType switch
                {
                    "Incoming" => Icons.IncomingEdge,
                    "Outgoing" => Icons.OutgoingEdge,
                    "Lifted Incoming" => Icons.LiftedIncomingEdge,
                    "Lifted Outgoing" => Icons.LiftedOutgoingEdge,
                    _ => Icons.QuestionMark
                };
                string cleanedId = ElementId(node, inGroup);
                string id = $"{cleanedId}#{edgesType}";
                // Note that an edge may appear multiple times in the tree view,
                // hence we make its ID dependent on the node it is connected to,
                // and whether it is an incoming or outgoing edge (to cover self-loops).
                AddItem(id, edges.Count > 0, $"{edgesType} Edges", icon,
                        collapseItem: collapsedItem =>
                        {
                            CollapseItem(collapsedItem);
                            foreach (Edge edge in edges)
                            {
                                RemoveItem($"{id}#{CleanupID(edge.ID)}");
                            }
                        },
                        expandItem: (expandedItem, order) =>
                        {
                            ExpandItem(expandedItem);
                            AddEdges(id, edges);
                            if (order)
                            {
                                OrderTree(node, inGroup: inGroup);
                            }
                        });
            }
        }

        /// <summary>
        /// Adds the given <paramref name="edges"/> to the tree window.
        /// </summary>
        /// <param name="id">The ID of the TreeWindow item to which the edges belong.</param>
        /// <param name="edges">The edges to be added.</param>
        private void AddEdges(string id, IEnumerable<Edge> edges)
        {
            foreach (Edge edge in edges)
            {
                GameObject edgeObject = GraphElementIDMap.Find(edge.ID);
                string title = edge.ToShortString();
                AddItem($"{id}#{CleanupID(edge.ID)}", false, title, Icons.Edge, edgeObject, edge);
            }
        }

        /// <summary>
        /// Collapses the given <paramref name="item"/>.
        /// Its children will be removed from the tree window.
        /// </summary>
        /// <param name="node">The node represented by the item.</param>
        /// <param name="item">The item to be collapsed.</param>
        /// <param name="inGroup">The group in which the node is contained, if any.</param>
        private void CollapseNode(Node node, GameObject item, TreeWindowGroup inGroup = null)
        {
            CollapseItem(item);
            RemoveNodeChildren(node, inGroup);
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
                AddRootsAsync().Forget();
                return;
            }

            foreach (Node node in searcher.Search(searchTerm))
            {
                GameObject nodeGameObject = GraphElementIDMap.Find(node.ID, mustFindElement: true);
                AddItem(CleanupID(node.ID),
                        false, node.ToShortString(), Icons.Node, nodeGameObject, node,
                        expandItem: (_, _) => RevealElementAsync(node).Forget());
            }

            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer)
            {
                items.position = items.position.WithXYZ(y: 0);
            }
        }

        /// <summary>
        /// Displays only the given <paramref name="nodes"/> in the tree window.
        /// All other nodes will be hidden, and cannot be added back by the user.
        /// Hence, this method can be useful to create specialized views of the graph when combined
        /// with a custom title for the tree window.
        /// </summary>
        /// <param name="nodes">The nodes to be displayed in the tree window.</param>
        public async UniTaskVoid ConstrainToAsync(ICollection<Node> nodes)
        {
            searcher.Filter.IncludeElements.Clear();
            searcher.Filter.ExcludeElements.Clear();
            searcher.Filter.IncludeElements.UnionWith(nodes);

            ClearTree();
            foreach (Node node in nodes)
            {
                ExpandPathFor(node);
            }
            await AddRootsAsync();
        }

        /// <summary>
        /// Makes the given <paramref name="element"/> visible in the tree window by expanding all its parents
        /// and scrolling to it.
        /// If an edge is given, the source/target node will be made visible instead,
        /// depending on the value of <paramref name="viaSource"/>.
        /// </summary>
        /// <param name="element">The element to be made visible.</param>
        /// <param name="viaSource">Whether to make the source or target node of the edge visible.</param>
        public async UniTaskVoid RevealElementAsync(GraphElement element, bool viaSource = false)
        {
            TreeWindowGroup group = grouper?.GetGroupFor(element);
            if (searchField == null)
            {
                // We need to wait until the window is initialized.
                // This case may occur when the method is called from the outside.
                await UniTask.WaitUntil(() => searchField != null);
            }
            if (!ShouldBeDisplayed(element) || (group == null && grouper is { IsActive: true }))
            {
                ShowNotification.Warn("Element filtered out",
                                      "Element is not included in the current filter or group and thus can't be shown.");
                return;
            }
            searchField.onValueChanged.RemoveListener(SearchFor);
            searchField.text = string.Empty;
            searchField.ReleaseSelection();
            searchField.onValueChanged.AddListener(SearchFor);
            ClearTree();

            Node current = element switch
            {
                Node node => node,
                Edge edge => viaSource ? edge.Source : edge.Target,
                _ => throw new ArgumentOutOfRangeException(nameof(element))
            };
            string transformID = ElementId(current, group);
            if (element is Edge)
            {
                expandedItems.Add(transformID);
                transformID += viaSource ? "#Outgoing" : "#Incoming";
                expandedItems.Add(transformID);
                transformID += $"#{ElementId(element, group)}";
            }

            ExpandPathFor(current);

            // We need to wait until the transform actually exists, hence the await.
            await AddRootsAsync();

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

        /// <summary>
        /// Expands the path from the given <paramref name="node"/> to the root of the tree,
        /// such that the node becomes visible.
        /// </summary>
        /// <param name="node">The node to be made visible.</param>
        private void ExpandPathFor(Node node)
        {
            TreeWindowGroup group = grouper?.GetGroupFor(node);
            // We need to find a path from the root to the node, which we do by working our way up the hierarchy.
            // We then expand all nodes on the path.
            while (node.Parent != null)
            {
                node = node.Parent;
                expandedItems.Add(ElementId(node, group));
            }

            // Finally, if the element is in a group, we need to expand the group.
            if (group != null)
            {
                expandedItems.Add(CleanupID(group.Text));
            }
        }

        protected override void StartDesktop()
        {
            if (Graph == null)
            {
                Debug.LogError("Graph must be set before starting the tree window.");
                return;
            }

            string graphName;
            if (!string.IsNullOrEmpty(Graph.Name))
            {
                graphName = Graph.Name;
            }
            else if (!string.IsNullOrEmpty(Graph.GetRoots().FirstOrDefault()?.SourceName))
            {
                graphName = Graph.GetRoots().First().SourceName;
            }
            else
            {
                graphName = "City Graph";
            }
            Title ??= $"{graphName} (Tree)";
            base.StartDesktop();
            Transform root = PrefabInstantiator.InstantiatePrefab(treeWindowPrefab, Window.transform.Find("Content"), false).transform;
            items = (RectTransform)root.Find("Content/Items");
            scrollRect = root.gameObject.MustGetComponent<ScrollRect>();

            searchField = root.Find("Search/SearchField").gameObject.MustGetComponent<TMP_InputField>();
            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            searchField.onValueChanged.AddListener(SearchFor);

            filterButton = root.Find("Search/Filter").gameObject.MustGetComponent<ButtonManagerBasic>();
            filterButton.clickEvent.AddListener(() => {
                XRSEEActions.OnSelectToggle = true;
            });
            sortButton = root.Find("Search/Sort").gameObject.MustGetComponent<ButtonManagerBasic>();
            sortButton.clickEvent.AddListener(() => {
                XRSEEActions.OnSelectToggle = true;
            });
            groupButton = root.Find("Search/Group").gameObject.MustGetComponent<ButtonManagerBasic>();
            groupButton.clickEvent.AddListener(() => {
                XRSEEActions.OnSelectToggle = true;
            });
            PopupMenu.PopupMenu popupMenu = gameObject.AddComponent<PopupMenu.PopupMenu>();
            contextMenu = new TreeWindowContextMenu(popupMenu, searcher, grouper, Rebuild,
                                                    filterButton, sortButton, groupButton);

            Rebuild();
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        /// Rebuilds the tree window.
        /// </summary>
        private void Rebuild()
        {
            ClearTree();
            grouper?.RebuildCounts();
            AddRootsAsync().Forget();
        }
    }
}
