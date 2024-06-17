using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI;
using SEE.UI.Notification;
using SEE.UI.PopupMenu;
using SEE.UI.Window;
using SEE.UI.Window.TreeWindow;
using SEE.Utils;
using UnityEngine;
using SEE.Game.City;
using SEE.UI.Window.NoteWindow;

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

        private void Start()
        {
            popupMenu = gameObject.AddComponent<PopupMenu>();
        }

        private void Update()
        {
            if (SEEInput.OpenContextMenu())
            {
                // TODO (#664): Detect if multiple elements are selected and adjust options accordingly.
                HitGraphElement hit = Raycasting.RaycastInteractableObject(out _, out InteractableObject o);
                if (hit == HitGraphElement.None)
                {
                    return;
                }

                IEnumerable<PopupMenuAction> actions = GetApplicableOptions(o.GraphElemRef.Elem, o.gameObject);
                popupMenu.ShowWith(actions, Input.mousePosition);
            }
        }

        /// <summary>
        /// Returns the options available for the given graph element.
        /// </summary>
        /// <param name="graphElement">The graph element to get the options for</param>
        /// <param name="gameObject">The game object that the graph element is attached to</param>
        /// <returns>Options available for the given graph element</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the graph element is neither a node nor an edge</exception>
        public static IEnumerable<PopupMenuAction> GetApplicableOptions(GraphElement graphElement, GameObject gameObject = null)
        {
            IEnumerable<PopupMenuAction> options = GetCommonOptions(graphElement, gameObject);
            return options.Concat(graphElement switch
            {
                Node node => GetNodeOptions(node, gameObject),
                Edge edge => GetEdgeOptions(edge, gameObject),
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        /// <summary>
        /// Returns the common options available for all graph elements.
        /// </summary>
        /// <param name="graphElement">The graph element to get the options for</param>
        /// <param name="gameObject">The game object that the graph element is attached to</param>
        /// <returns>Common options available for all graph elements</returns>
        private static IEnumerable<PopupMenuAction> GetCommonOptions(GraphElement graphElement, GameObject gameObject = null)
        {
            IList<PopupMenuAction> actions = new List<PopupMenuAction>
            {
                // TODO (#665): Ask for confirmation or allow undo.
                new("Delete", DeleteElement, Icons.Trash),
                // TODO (#666): Better properties view
                new("Properties", ShowProperties, Icons.Info),
                new("Show Metrics", ShowMetrics, Icons.Info),
                new("Create Note", CreateNote, Icons.Node),
            };

            if (gameObject != null)
            {
                actions.Add(new("Show in City", Highlight, Icons.LightBulb));
            }

            if (graphElement.Filename != null)
            {
                actions.Add(new("Show Code", ShowCode, Icons.Code));
                if (gameObject.ContainingCity<DiffCity>() != null)
                {
                    actions.Add(new("Show Code Diff", ShowDiffCode, Icons.Code));
                }
            }

            return actions;

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

            void CreateNote()
            {
                ActivateWindow(CreateNoteWindow(gameObject.MustGetComponent<GraphElementRef>()));
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
        /// <returns>The activated tree window</returns>
        private static TreeWindow ActivateTreeWindow(GraphElement graphElement, Transform transform)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            TreeWindow openWindow = manager.Windows.OfType<TreeWindow>().FirstOrDefault(x => x.Graph == graphElement.ItsGraph);
            if (openWindow == null)
            {
                // Window is not open yet, so we create it.
                GameObject city = SceneQueries.GetCodeCity(transform).gameObject;
                openWindow = city.AddComponent<TreeWindow>();
                openWindow.Graph = graphElement.ItsGraph;
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

        private static NoteWindow CreateNoteWindow(GraphElementRef graphElementRef)
        {
            // Create new window for active selection, or use existing one
            if (!graphElementRef.TryGetComponent(out NoteWindow noteWindow))
            {
                noteWindow = graphElementRef.gameObject.AddComponent<NoteWindow>();
                noteWindow.Title = "Notes for " + graphElementRef.Elem.ToShortString();
                //noteWindow.graphElement = graphElementRef.Elem;
            }
            return noteWindow;
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
        /// Returns the options available for the given node.
        /// </summary>
        /// <param name="node">The node to get the options for</param>
        /// <param name="gameObject">The game object that the node is attached to</param>
        /// <returns>Options available for the given node</returns>
        private static IEnumerable<PopupMenuAction> GetNodeOptions(Node node, GameObject gameObject)
        {
            IList<PopupMenuAction> actions = new List<PopupMenuAction>
            {
                new("Show in TreeView", RevealInTreeView, Icons.TreeView),
            };

            return actions;

            void RevealInTreeView()
            {
                ActivateTreeWindow(node, gameObject.transform).RevealElementAsync(node).Forget();
            }
        }

        /// <summary>
        /// Returns the options available for the given edge.
        /// </summary>
        /// <param name="edge">The edge to get the options for</param>
        /// <param name="gameObject">The game object that the edge is attached to</param>
        /// <returns>Options available for the given edge</returns>
        private static IEnumerable<PopupMenuAction> GetEdgeOptions(Edge edge, GameObject gameObject)
        {
            IList<PopupMenuAction> actions = new List<PopupMenuAction>
            {
                new("Show at Source (TreeView)", RevealAtSource, Icons.TreeView),
                new("Show at Target (TreeView)", RevealAtTarget, Icons.TreeView),
            };

            if (edge.Type == "Clone")
            {
                actions.Add(new("Show Unified Diff", ShowUnifiedDiff, Icons.Compare));
            }

            if (edge.IsInImplementation() && ReflexionGraph.IsDivergent(edge))
            {
                actions.Add(new("Accept Divergence", AcceptDivergence, Icons.Checkmark));
            }

            return actions;

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

            void AcceptDivergence()
            {
                AcceptDivergenceAction.CreateConvergentEdge(edge);
            }
        }
    }
}
