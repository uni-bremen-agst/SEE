using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SEE.Tools.ReflexionAnalysis;
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
        private PopupMenu PopupMenu;

        private void Start()
        {
            PopupMenu = gameObject.AddComponent<PopupMenu>();
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

                PopupMenu.ClearActions();
                PopupMenu.AddActions(actions);

                // We want to move the popup menu to the cursor position before showing it.
                PopupMenu.MoveTo(Input.mousePosition);
                PopupMenu.ShowMenu().Forget();
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
                new("Delete", DeleteElement, '\uF1F8'),
                // TODO (#666): Better properties view
                new("Properties", ShowProperties, '\uF05A'),
            };

            if (gameObject != null)
            {
                actions.Add(new("Highlight", Highlight, '\uF0EB'));
            }

            if (graphElement.Filename() != null)
            {
                actions.Add(new("Show Code", ShowCode, '\uF121'));
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

            void ShowCode()
            {
                ActivateWindow(ShowCodeAction.ShowCode(gameObject.MustGetComponent<GraphElementRef>()));
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

        private static TreeWindow ActivateCandidateRecommendationWindow(GraphElement graphElement, 
                                                                        Transform transform, 
                                                                        CandidateRecommendation candidateRecommendation) 
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];

            Graph recommendationTree = candidateRecommendation.GetRecommendationTree((Node)graphElement);

            // TODO: Can this actually happen? Talk to falko51
            TreeWindow openWindow = manager.Windows.OfType<TreeWindow>().FirstOrDefault(x => x.Graph == recommendationTree);
            if (openWindow == null)
            {
                // Window is not open yet, so we create it.
                GameObject city = SceneQueries.GetCodeCity(transform).gameObject;
                openWindow = city.AddComponent<TreeWindow>();
                openWindow.Graph = recommendationTree;
                manager.AddWindow(openWindow);
            }
            manager.ActiveWindow = openWindow;
            return openWindow;
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
                new("Show in TreeView", RevealInTreeView, '\uF802'),
            };

            GameObject city = SceneQueries.GetCodeCity(gameObject.transform).gameObject;
            CandidateRecommendationVisualization candidateRecommendationViz = city.GetComponent<CandidateRecommendationVisualization>();

            // Add action if candidate recommendation is active and if it is unmapped and part of the implementation graph
            if (candidateRecommendationViz != null && 
                (!node.IsInImplementation() || ((ReflexionGraph)node.ItsGraph).MapsTo(node) == null))
            {
                actions.Add(new("Show Candidate Recommendation", RevealInCandidateRecommendation, '\uF802'));
            }

            return actions;

            void RevealInTreeView()
            {
                ActivateTreeWindow(node, gameObject.transform).RevealElement(node).Forget();
            }

            void RevealInCandidateRecommendation()
            {
                ActivateCandidateRecommendationWindow(node, gameObject.transform, candidateRecommendationViz.CandidateRecommendation).RevealElement(node).Forget();
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
                new("Show at Source (TreeView)", RevealAtSource, '\uF802'),
                new("Show at Target (TreeView)", RevealAtTarget, '\uF802'),
            };

            if (edge.Type == "Clone")
            {
                actions.Add(new("Show Unified Diff", ShowUnifiedDiff, '\uE13A'));
            }

            if (edge.IsInImplementation() && ReflexionGraph.IsDivergent(edge))
            {
                actions.Add(new("Accept Divergence", AcceptDivergence, '\uF00C'));
            }

            return actions;

            void RevealAtSource()
            {
                ActivateTreeWindow(edge, gameObject.transform).RevealElement(edge, viaSource: true).Forget();
            }

            void RevealAtTarget()
            {
                ActivateTreeWindow(edge, gameObject.transform).RevealElement(edge, viaSource: false).Forget();
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
