using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System.Linq;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create a new node for a selected city.
    /// </summary>
    internal class AcceptDivergenceAction : AbstractPlayerAction
    {
        /// <summary>
        /// </summary>
        private GameObject hitGraphElement;

        /// <summary>
        /// </summary>
        private ISet<GameObject> syncedGameObjects;

        /// <summary>
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
            {

                // Syncing:
                //      Get Selected Edge
                hitGraphElement = raycastHit.collider.gameObject;

                Debug.Log("1");
                // Check if Edge
                if (hitGraphElement.TryGetEdge(out Edge selectedEdge))
                {

                    // FIXME: This is a stupid workaround until the Diversion Edge can be selected
                    var graph = (ReflexionGraph)selectedEdge.ItsGraph;
                    foreach (Edge edge in graph.Edges())
                    {
                        if (ReflexionGraph.IsDivergent(edge))
                        {
                            graph.Transition(edge, State.Convergent);

                            // Insert Edge into Architecture
                            graph.Transition(edge, State.Allowed);

                            // Find Node in ArchitectureGraph the
                            // divergence's Source is explicitly or
                            // implicitly mapped to.
                            Node source = graph.MapsTo(edge.Source);

                            // Find Node in ArchitectureGraph the
                            // divergence's Target is explicitly or
                            // implicitly mapped to.
                            Node target = graph.MapsTo(edge.Target);

                            // Ensure the required Edge has not
                            // already been added (shouldn't be
                            // necessary outside of this workaround
                            // due to Incremental nature of
                            // ReflexionGraph)
                            if (graph.Edges().Any(x => x.Source == edge.Source && x.Target == edge.Target))
                            {
                                // add new edge to node
                                graph.AddToArchitecture(source, target, edge.Type);
                            }
                        }
                    }

                    // // Check if Edge is within architecture
                    // if (ReflexionGraph.IsDivergent(edge))
                    // {
                    //     // Syncing over Network
                    //     new AcceptDivergenceNetAction(hitGraphElement.name).Execute();
                    //     return true; // the selected object is synced and this action is done
                    // }
                }
                else
                {
                    Debug.LogWarning($"Selected Element {hitGraphElement.name} is not a reflexion edge.\n");
                }
            }
            return false;
        }

        /// <summary>
        /// Undoes this AddNodeAction.
        /// </summary>
        public override void Undo()
        {
        }

        /// <summary>
        /// Redoes this AddNodeAction.
        /// </summary>
        public override void Redo()
        {
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AcceptDivergenceAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Sync;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (syncedGameObjects == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>(syncedGameObjects.Select(x => x.name));
            }

        }
    }
}
