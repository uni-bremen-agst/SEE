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
    /// Action to solve a Divergence between Implementation and
    /// Architecture by adding the Edge to the Architecture that
    /// solves the Divergence.
    /// </summary>
    internal class AcceptDivergenceAction : AbstractPlayerAction
    {
        /// <summary>
        /// The edge that was hit by the user to be accepted
        /// into the ArchitectureGraph. Set in <see cref="Update"/>.
        /// </summary>
        private GameObject selectedDivergenceEdge;

        /// <summary>
        /// Contains all Edges that were explicitly added to
        /// solve Divergences.
        ///</summary>
        private ISet<GameObject> syncedGameObjects;

        /// <summary>
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(
                    out RaycastHit raycastHit,
                    out GraphElementRef _) != HitGraphElement.None)
            {
                // Find the edge representing the specific Divergence that should be solved.
                selectedDivergenceEdge = raycastHit.collider.gameObject;

                // Check whether the object selected is actually an edge.
                if (selectedDivergenceEdge.TryGetEdge(out Edge selectedEdge))
                {
                    if (ReflexionGraph.IsDivergent(selectedEdge))
                    {
                        // get the containing ReflexionGraph
                        var graph = (ReflexionGraph)selectedEdge.ItsGraph;

                        // Find Node in ArchitectureGraph the
                        // divergence's Source is explicitly or
                        // implicitly mapped to.
                        Node source = graph.MapsTo(selectedEdge.Source);

                        // Find Node in ArchitectureGraph the
                        // divergence's Target is explicitly or
                        // implicitly mapped to.
                        Node target = graph.MapsTo(selectedEdge.Target);

                        // Create a new Edge that will solve the Divergence
                        Edge newArchitectureEdge = graph.AddToArchitecture(source, target, selectedEdge.Type);

                        // Sync the Solution of the Divergence via Network
                        // new AcceptDivergenceNetAction(selectedDivergenceEdge.name).Execute();
                        return true; // the selected object is synced and this action is done
                    }
                }
                else
                {
                    Debug.LogWarning($"Selected Element {selectedDivergenceEdge.name} is not an edge.\n");
                }
            }
            return false;
        }


        /// <summary>
        /// Undoes this AcceptDivergenceAction.
        /// </summary>
        public override void Undo()
        {
        }

        /// <summary>
        /// Redoes this AcceptDivergenceAction.
        /// </summary>
        public override void Redo()
        {
        }

        /// <summary>
        /// Returns a new instance of <see cref="AcceptDivergenceAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AcceptDivergenceAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AcceptDivergenceAction"/>.
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
