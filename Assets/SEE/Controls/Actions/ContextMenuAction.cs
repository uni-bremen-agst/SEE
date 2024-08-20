using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Notification;
using SEE.UI.PopupMenu;
using SEE.UI.Window;
using SEE.UI.Window.TreeWindow;
using SEE.Utils;
using UnityEngine;
using SEE.Game.City;
using SEE.Utils.History;
using SEE.GO.Menu;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows a context menu with available actions when the user requests it.
    /// </summary>
    public class ContextMenuAction : MonoBehaviour
    {
        /// <summary>
        /// The popup menu that is shown when the user requests the context menu.
        /// </summary>
        private PopupMenu popupMenu;

        /// <summary>
        /// The position where the menu should be opened.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The interactable object during the start must be the same as when
        /// the right mouse button is released in order for the context menu to open.
        /// </summary>
        private InteractableObject startObject;

        private void Start()
        {
            popupMenu = gameObject.AddComponent<PopupMenu>();
        }

        private void Update()
        {
            if (SEEInput.StartOpenContextMenu())
            {
                Raycasting.RaycastInteractableObject(out _, out InteractableObject o);
                startObject = o;
            }
            if (SEEInput.OpenContextMenu())
            {
                // TODO (#664): Detect if multiple elements are selected and adjust options accordingly.
                HitGraphElement hit = Raycasting.RaycastInteractableObject(out _, out InteractableObject o);
                if (hit == HitGraphElement.None)
                {
                    return;
                }
                if (o == startObject)
                {
                    position = Input.mousePosition;
                    IEnumerable<PopupMenuEntry> entries = GetApplicableOptions(popupMenu, position, o.GraphElemRef.Elem, o.gameObject);
                    popupMenu.ShowWith(entries, position);
                }
            }
        }

        /// <summary>
        /// Updates the context menu.
        /// </summary>
        /// <param name="popupMenu">The popup menu in which the options should be displayed.</param>
        /// <param name="position">The position to be displayed the popup menu.</param>
        /// <param name="entries">The new entries for the context menu.</param>
        public static void UpdateEntries(PopupMenu popupMenu, Vector3 position, IEnumerable<PopupMenuEntry> entries)
        {
            popupMenu.ShowWith(entries, position);
        }

        /// <summary>
        /// Returns the options available for the given graph element.
        /// </summary>
        /// <param name="popupMenu">The popup menu in which the options should be displayed.</param>
        /// <param name="position"></param>
        /// <param name="graphElement">The graph element to get the options for</param>
        /// <param name="gameObject">The game object that the graph element is attached to</param>
        /// <returns>Options available for the given graph element</returns>
        /// <param name="appendActions">Actions to be append at the end of the entries.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the graph element is neither a node nor an edge</exception>
        public static IEnumerable<PopupMenuEntry> GetApplicableOptions(PopupMenu popupMenu, Vector3 position, GraphElement graphElement,
            GameObject gameObject = null, IEnumerable<PopupMenuAction> appendActions = null)
        {
            IEnumerable<PopupMenuEntry> options = GetCommonOptions(popupMenu, position, graphElement, gameObject, appendActions);
            return options.Concat(graphElement switch
            {
                Node node => GetNodeOptions(popupMenu, position, node, gameObject, appendActions),
                Edge edge => GetEdgeOptions(popupMenu, position, edge, gameObject, appendActions),
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        /// <summary>
        /// Returns the common options available for all graph elements.
        /// </summary>
        /// <param name="popupMenu">The popup menu in which the options should be displayed.</param>
        /// <param name="position">The position to be displayed the popup menu.</param>
        /// <param name="graphElement">The graph element to get the options for</param>
        /// <param name="gameObject">The game object that the graph element is attached to</param>
        /// <param name="appendActions">Actions to be append at the end of the entries.</param>
        /// <returns>Common options available for all graph elements</returns>
        private static IEnumerable<PopupMenuEntry> GetCommonOptions(PopupMenu popupMenu, Vector3 position, GraphElement graphElement,
            GameObject gameObject = null, IEnumerable<PopupMenuAction> appendActions = null)
        {
            string name = graphElement.ID;
            string target, source = target = null;
            if (graphElement is Node node
                && !string.IsNullOrEmpty(node.SourceName))
            {
                name = node.SourceName;
            }
            if (graphElement is Edge edge)
            {
                name = edge.Type;
                source = edge.Source.SourceName ?? edge.Source.ID;
                target = edge.Target.SourceName ?? edge.Target.ID;
            }
            IList<PopupMenuEntry> entries = new List<PopupMenuEntry>
            {
                new PopupMenuHeading(name)
            };
            if (source != null && target != null)
            {
                entries.Add(new PopupMenuHeading("Source: " + source));
                entries.Add(new PopupMenuHeading("Target: " + target));
            }

            // TODO (#665): Ask for confirmation or allow undo.
            entries.Add(new PopupMenuAction("Delete", DeleteElement, Icons.Trash));

            entries.Add(new PopupMenuActionDoubleIcon("Show Actions", () =>
            {
                List<PopupMenuEntry> subMenuEntries = new()
                    {
                        new PopupMenuAction("Show Actions", () =>
                        {
                            if (appendActions != null)
                            {
                                List<PopupMenuAction> actions = new (GetApplicableOptions(popupMenu, position, graphElement, gameObject, appendActions)
                                                                    .OfType<PopupMenuAction>()
                                                                    .Where(x=>!x.Name.Contains("TreeWindow")));
                                actions.AddRange(appendActions);
                                UpdateEntries(popupMenu, position, actions);
                            }
                            else
                            {
                                UpdateEntries(popupMenu, position, GetApplicableOptions(popupMenu, position, graphElement, gameObject));
                            }
                        },
                            Icons.ArrowLeft, CloseAfterClick: false),
                        // TODO (#666): Better properties view
                        new PopupMenuAction("Properties", ShowProperties, Icons.Info),
                        new PopupMenuAction("Show Metrics", ShowMetrics, Icons.Info),
                    };
                if (gameObject != null)
                {
                    subMenuEntries.Add(new PopupMenuAction("Show in City", Highlight, Icons.LightBulb));
                }

                if (graphElement.Filename != null)
                {
                    subMenuEntries.Add(new PopupMenuAction("Show Code", ShowCode, Icons.Code));
                    if (gameObject.ContainingCity<DiffCity>() != null)
                    {
                        subMenuEntries.Add(new PopupMenuAction("Show Code Diff", ShowDiffCode, Icons.Code));
                    }
                }
                subMenuEntries.AddRange(graphElement switch
                {
                    Node node => GetNodeShowOptions(node, gameObject),
                    Edge edge => GetEdgeShowOptions(edge, gameObject),
                    _ => throw new ArgumentOutOfRangeException()
                });
                UpdateEntries(popupMenu, position, subMenuEntries);

            }, Icons.Info, Icons.ArrowRight, CloseAfterClick: false));

            return entries;

            void DeleteElement()
            {
                if (gameObject != null)
                {
                    GameElementDeleter.Delete(gameObject);
                }
                else
                {
                    graphElement.ItsGraph.RemoveElement(graphElement);
                }
            }

            void ShowProperties()
            {
                ShowNotification.Info("Node Properties", graphElement.ToString(), log: false);
            }

            void ShowMetrics()
            {
                ActivateWindow(CreateMetricWindow(gameObject.MustGetComponent<GraphElementRef>()));
            }

            void ShowCode()
            {
                ActivateWindow(ShowCodeAction.ShowCode(gameObject.MustGetComponent<GraphElementRef>()));
            }

            void ShowDiffCode()
            {
                ActivateWindow(ShowCodeAction.ShowVCSDiff(gameObject.MustGetComponent<GraphElementRef>(),
                                                          gameObject.ContainingCity<DiffCity>()));
            }

            void Highlight()
            {
                if (gameObject != null)
                {
                    gameObject.Operator().Highlight(duration: 10);
                }
                else
                {
                    ShowNotification.Warn("No game object", "There is nothing to highlight for this element.");
                }
            }
        }

        /// <summary>
        /// Activates the tree window for the given graph element and returns it.
        /// </summary>
        /// <param name="graphElement">The graph element to activate the tree window for</param>
        /// <param name="transform">The transform of the game object that the graph element is attached to</param>
        /// <param name="title">The title of the tree window to be used. Should only be set if this is not supposed
        /// to be the main tree window.</param>
        /// <returns>The activated tree window</returns>
        private static TreeWindow ActivateTreeWindow(GraphElement graphElement, Transform transform, string title = null)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            TreeWindow openWindow = manager.Windows.OfType<TreeWindow>().FirstOrDefault(x => x.Graph == graphElement.ItsGraph && (title == null || x.Title == title));
            if (openWindow == null)
            {
                // Window is not open yet, so we create it.
                GameObject city = SceneQueries.GetCodeCity(transform).gameObject;
                openWindow = city.AddComponent<TreeWindow>();
                openWindow.Graph = graphElement.ItsGraph;
                if (title != null)
                {
                    openWindow.Title = title;
                }
                manager.AddWindow(openWindow);
            }
            manager.ActiveWindow = openWindow;
            return openWindow;
        }

        /// <summary>
        /// Returns a <see cref="MetricWindow"/> showing the attributes of <paramref name="graphElementRef"/>.
        /// </summary>
        /// <param name="graphElementRef">The graph element to activate the metric window for</param>
        /// <returns>The <see cref="MetricWindow"/> object showing the attributes of the specified graph element.</returns>
        private static MetricWindow CreateMetricWindow(GraphElementRef graphElementRef)
        {
            // Create new window for active selection, or use existing one
            if (!graphElementRef.TryGetComponent(out MetricWindow metricMenu))
            {
                metricMenu = graphElementRef.gameObject.AddComponent<MetricWindow>();
                metricMenu.Title = "Metrics for " + graphElementRef.Elem.ToShortString();
                metricMenu.GraphElement = graphElementRef.Elem;
            }
            return metricMenu;
        }

        /// <summary>
        /// Activates the given window, that is, adds it to the window space and makes it the active window.
        /// </summary>
        /// <param name="window">The window to activate</param>
        private static void ActivateWindow(BaseWindow window)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (!manager.Windows.Contains(window))
            {
                manager.AddWindow(window);
            }
            manager.ActiveWindow = window;
        }

        /// <summary>
        /// Returns the show options available for the given node.
        /// </summary>
        /// <param name="node">The node to get the show options for</param>
        /// <param name="gameObject">The game object that the node is attached to</param>
        /// <returns>Show options available for the given node</returns>
        private static IEnumerable<PopupMenuEntry> GetNodeShowOptions(Node node, GameObject gameObject)
        {
            IList<PopupMenuEntry> actions = new List<PopupMenuEntry> { new PopupMenuAction("Reveal in TreeView", RevealInTreeView, Icons.TreeView), };

            if (node.OutgoingsOfType(LSP.Reference).Any())
            {
                actions.Add(new PopupMenuAction("Show References", () => ShowTargets(LSP.Reference, false).Forget(), Icons.IncomingEdge));
            }
            if (node.OutgoingsOfType(LSP.Declaration).Any())
            {
                actions.Add(new PopupMenuAction("Show Declaration", () => ShowTargets(LSP.Declaration).Forget(), Icons.OutgoingEdge));
            }
            if (node.OutgoingsOfType(LSP.Definition).Any())
            {
                actions.Add(new PopupMenuAction("Show Definition", () => ShowTargets(LSP.Definition).Forget(), Icons.OutgoingEdge));
            }
            if (node.OutgoingsOfType(LSP.OfType).Any())
            {
                actions.Add(new PopupMenuAction("Show Type", () => ShowTargets(LSP.OfType).Forget(), 'T'));
            }
            return actions;


            void RevealInTreeView()
            {
                ActivateTreeWindow(node, gameObject.transform).RevealElementAsync(node).Forget();
            }

            // Highlights all nodes that are targets of the given kind of edge.
            async UniTaskVoid ShowTargets(string kind, bool outgoings = true)
            {
                IList<Node> nodes;
                if (outgoings)
                {
                    nodes = node.OutgoingsOfType(kind).Select(e => e.Target).ToList();
                }
                else
                {
                    nodes = node.IncomingsOfType(kind).Select(e => e.Source).ToList();
                }
                if (nodes.Count == 1)
                {
                    // We will just highlight the target node directly.
                    nodes.First().Operator().Highlight(duration: 10);
                }
                else
                {
                    TreeWindow window = ActivateTreeWindow(node, gameObject.transform, title: $"{kind}s of " + node.SourceName);
                    await UniTask.Yield();
                    window.ConstrainToAsync(nodes).Forget();
                }
            }
        }

        /// <summary>
        /// Returns the options available for the given node.
        /// </summary>
        /// <param name="popupMenu">The popup menu in which the options should be displayed.</param>
        /// <param name="position">The position to be displayed the popup menu.</param>
        /// <param name="node">The node to get the options for</param>
        /// <param name="gameObject">The game object that the node is attached to</param>
        /// <param name="appendActions">Actions to be append at the end of the entries.</param>
        /// <returns>Options available for the given node</returns>
        private static IEnumerable<PopupMenuEntry> GetNodeOptions(PopupMenu popupMenu, Vector3 position,
            Node node, GameObject gameObject, IEnumerable<PopupMenuAction> appendActions)
        {
            IList<PopupMenuEntry> actions = new List<PopupMenuEntry>
            {
                new PopupMenuAction("Move", MoveNode, Icons.Move),
                new PopupMenuAction("Rotate", RotateNode, Icons.Rotate),
                new PopupMenuAction("New Node", NewNode, '+'),
                new PopupMenuAction("New Edge", NewEdge, Icons.Edge),
                new PopupMenuAction("Edit Node", EditNode, Icons.PenToSquare),
                new PopupMenuAction("Scale Node", ScaleNode, Icons.Scale),
            };
            return new List<PopupMenuEntry>() { CreateSubMenu(popupMenu, position, "Node Options", Icons.Node, actions, node, gameObject, appendActions) };

            void MoveNode()
            {

            }

            void RotateNode()
            {

            }

            void NewNode()
            {

            }

            void NewEdge()
            {

            }

            void EditNode()
            {

            }

            void ScaleNode()
            {

            }
        }

        /// <summary>
        /// Creates a sub menu for the context menu.
        /// </summary>
        /// <param name="popupMenu">The popup menu in which the options should be displayed.</param>
        /// <param name="position">The position to be displayed the popup menu.</param>
        /// <param name="name">The name for the sub menu.</param>
        /// <param name="icon">The icon for the sub menu.</param>
        /// <param name="actions">A list of the actions which should be displayed in the sub menu.</param>
        /// <param name="graphElement">The graph element to get the options for</param>
        /// <param name="gameObject">The game object that the graph element is attached to</param>
        /// <param name="appendActions">Actions to be append at the end of the entries.</param>
        /// <returns>The created sub menu.</returns>
        private static PopupMenuActionDoubleIcon CreateSubMenu(PopupMenu popupMenu, Vector3 position, string name, char icon, IEnumerable<PopupMenuEntry> actions,
            GraphElement graphElement, GameObject gameObject = null, IEnumerable<PopupMenuAction> appendActions = null)
        {
            return new(name, () =>
            {
                List<PopupMenuEntry> entries = new()
                    {
                        new PopupMenuAction(name, () =>
                        {
                            if (appendActions != null)
                            {
                                List<PopupMenuAction> actions = new (GetApplicableOptions(popupMenu, position, graphElement, gameObject, appendActions)
                                                                    .OfType<PopupMenuAction>()
                                                                    .Where(x=>!x.Name.Contains("TreeWindow")));
                                actions.AddRange(appendActions);
                                UpdateEntries(popupMenu, position, actions);
                            }
                            else
                            {
                                UpdateEntries(popupMenu, position, GetApplicableOptions(popupMenu, position, graphElement, gameObject));
                            }
                        },
                        Icons.ArrowLeft, CloseAfterClick: false)
                    };
                entries.AddRange(actions);
                UpdateEntries(popupMenu, position, entries);

            }, icon, Icons.ArrowRight, CloseAfterClick: false);
        }

        /// <summary>
        /// Returns the show options available for the given edge.
        /// </summary>
        /// <param name="edge">The edge to get the show options for</param>
        /// <param name="gameObject">The game object that the edge is attached to</param>
        /// <returns>Show options available for the given edge</returns>
        private static IEnumerable<PopupMenuEntry> GetEdgeShowOptions(Edge edge, GameObject gameObject)
        {
            IList<PopupMenuEntry> entries = new List<PopupMenuEntry>
            {
                new PopupMenuAction("Show at Source (TreeView)", RevealAtSource, Icons.TreeView),
                new PopupMenuAction("Show at Target (TreeView)", RevealAtTarget, Icons.TreeView),
            };

            if (edge.Type == "Clone")
            {
                entries.Add(new PopupMenuAction("Show Unified Diff", ShowUnifiedDiff, Icons.Compare));
            }

            return entries;


            void RevealAtSource()
            {
                ActivateTreeWindow(edge, gameObject.transform).RevealElementAsync(edge, viaSource: true).Forget();
            }

            void RevealAtTarget()
            {
                ActivateTreeWindow(edge, gameObject.transform).RevealElementAsync(edge, viaSource: false).Forget();
            }

            void ShowUnifiedDiff()
            {
                ActivateWindow(ShowCodeAction.ShowUnifiedDiff(gameObject.MustGetComponent<EdgeRef>()));
            }

        }

        /// <summary>
        /// Returns the options available for the given edge.
        /// </summary>
        /// <param name="popupMenu">The popup menu in which the options should be displayed.</param>
        /// <param name="popupMenu">The popup menu in which the options should be displayed.</param>
        /// <param name="edge">The edge to get the options for</param>
        /// <param name="gameObject">The game object that the edge is attached to</param>
        /// <param name="appendActions">Options to be append at the end of the entries.</param>
        /// <returns>Options available for the given edge</returns>
        private static IEnumerable<PopupMenuEntry> GetEdgeOptions(PopupMenu popupMenu, Vector3 position,
            Edge edge, GameObject gameObject, IEnumerable<PopupMenuAction> appendActions = null)
        {
            IList<PopupMenuEntry> actions = new List<PopupMenuEntry>
            {
                new PopupMenuAction("Edit Edge", EditEdge, Icons.PenToSquare)
            };

            if (edge.IsInImplementation() && ReflexionGraph.IsDivergent(edge))
            {
                actions.Add(new PopupMenuAction("Accept Divergence", AcceptDivergence, Icons.Checkmark));
            }

            return new List<PopupMenuEntry>() { CreateSubMenu(popupMenu, position, "Edge Options", Icons.Node, actions, edge, gameObject, appendActions) };

            void AcceptDivergence()
            {
                ActionStateType previousAction = GlobalActionHistory.Current();
                GlobalActionHistory.Execute(ActionStateTypes.AcceptDivergence);
                AcceptDivergenceAction action = (AcceptDivergenceAction)GlobalActionHistory.CurrentAction();
                action.CreateConvergentEdge(edge);
                ExcecutePreviousAction(action, previousAction);
            }

            void EditEdge()
            {

            }
        }

        /// <summary>
        /// Ensures that the previous action is executed again after the current action has been fully completed (<see cref="IReversibleAction.Progress.Completed"/>).
        /// Additionally, the <see cref="PlayerMenu"> is updated.
        /// </summary>
        /// <param name="action">The current action which was executed via context menu.</param>
        /// <param name="previousAction">The previously executed action to be re-executed.</param>
        private static async void ExcecutePreviousAction(IReversibleAction action, ActionStateType previousAction)
        {
            while (action.CurrentProgress() != IReversibleAction.Progress.Completed)
            {
                await UniTask.Yield();
            }
            GlobalActionHistory.Execute(previousAction);
            LocalPlayer.TryGetPlayerMenu(out PlayerMenu menu);
            menu.UpdateActiveEntry();
        }
    }
}
