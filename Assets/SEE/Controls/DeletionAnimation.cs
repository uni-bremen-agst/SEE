using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel.DG;
using System.Collections.Concurrent;

namespace SEE.Controls
{
    /// <summary>
    /// A class providing methods needed for the animations of gameobjects having been deleted
    /// by the user, for instance, the movement of a game node to the garbage can, as well
    /// as the inverse undo mechanism.
    /// </summary>
    public static class DeletionAnimation
    {
        //-----------------------------------------
        // General settings of the animation
        //-----------------------------------------

        /// <summary>
        /// The garbage can the deleted nodes and edges will be moved to. It is the object named
        /// <see cref="GarbageCanName"/>.
        /// </summary>
        private static GameObject garbageCan;

        /// <summary>
        /// The name of the game object representing the garbage can.
        /// </summary>
        private const string GarbageCanName = "GarbageCan";

        /// <summary>
        /// The animation time of the animation of moving a deleted object to the top of the garbage can.
        /// </summary>
        private const float TimeForAnimation = 1f;

        /// <summary>
        /// The waiting time of the animation for moving a deleted object into the garbage can from
        /// over the garbage can.
        /// </summary>
        private const float TimeToWait = 1f;

        /// <summary>
        /// The vertical height above the garbage can the removed objects move to during the undo process, respectively,
        /// the objects move to having been deleted.
        /// </summary>
        private const float VerticalHeight = 1.4f;

        /// <summary>
        /// Number of animations used for an object's expansion, removing it from the garbage can.
        /// </summary>
        private const float StepsOfExpansionAnimation = 10;

        /// <summary>
        /// The time (in seconds) between animations of expanding a deleted object that is being restored
        /// from the garbage can.
        /// </summary>
        private const float TimeBetweenExpansionAnimation = 0.14f;

        /// <summary>
        ///  A vector for an objects localScale which fits into the garbage can.
        ///  TODO: Currently set to an absolute value. Should be set abstract, e.g., half of the
        ///  garbage can's diameter.
        /// </summary>
        private static readonly Vector3 defaultGarbageVector = new Vector3(0.1f, 0.1f, 0.1f);

        /// <summary>
        /// A list of ratios of the current localScale and a target scale.
        ///
        /// FIXME: Why is a ConcurrentDictionary needed?
        /// </summary>
        private static ConcurrentDictionary<GameObject, Vector3> shrinkFactors { get; set; } = new ConcurrentDictionary<GameObject, Vector3>();

        /// <summary>
        /// A history of all edges and the graph where they were attached to, deleted by this action.
        ///
        /// FIXME: Why is a ConcurrentDictionary needed?
        /// </summary>
        private static ConcurrentDictionary<GameObject, Graph> deletedEdges { get; set; } = new ConcurrentDictionary<GameObject, Graph>();

        /// <summary>
        /// A vector containing the position liniearly interpolated for the edges which have to be moved to the garbage can.
        /// </summary>
        private static Vector3 GarbageCanPositionForEdges = new Vector3();

        /// <summary>
        /// Returns the position of the garbage can if one exists in the scene.
        /// If none exists, an empty game object representing it will be used
        /// as a substitute at location <see cref="Vector3.zero"/>.
        /// </summary>
        /// <returns>position of the garbage can in world space</returns>
        private static Vector3 GarbageCanPosition()
        {
            if (garbageCan == null)
            {
                garbageCan = GameObject.Find(GarbageCanName);
                if (garbageCan == null)
                {
                    Debug.LogError($"No {GarbageCanName} found.\n");
                    // We want the error message to be emitted only once.
                    // For this reason, we will create an empty game object for the missing garbage can
                    // and use that as a substitute.
                    garbageCan = new GameObject(GarbageCanName);
                    garbageCan.transform.position = Vector3.zero;
                }
            }
            return garbageCan.transform.position;
        }

        /// <summary>
        /// Moves all nodes and edges in <paramref name="deletedNodesAndEdges"/> to the garbage can
        /// using an animation. When they finally arrive there, they will be deleted.
        ///
        /// Assumption: <paramref name="deletedNodesAndEdges"/> contains all nodes in a subtree
        /// of the game-node hierarchy. All of them represent graph nodes.
        /// </summary>
        /// <param name="deletedNodesAndEdges">the deleted nodes and edges which will be moved to the garbage can.</param>
        /// <returns>the waiting time between moving deleted objects over the garbage can and then into the garbage can</returns>
        public static IEnumerator MoveNodeOrEdgeToGarbage(IList<GameObject> deletedNodesAndEdges)
        {
            Vector3 garbageCanPosition = GarbageCanPosition();

            // We need to reset the portal of all all deletedNodesOrEdges so that we can move
            // them to the garbage bin. Otherwise they will become invisible if they
            // leave their portal.
            foreach (GameObject deletedNode in deletedNodesAndEdges)
            {
                Portal.SetInfinitePortal(deletedNode);
            }
            foreach (GameObject deletedNode in deletedNodesAndEdges)
            {
                if (deletedNode.HasNodeRef())
                {
                    Tweens.Move(deletedNode,
                            new Vector3(garbageCanPosition.x,
                                        garbageCanPosition.y + VerticalHeight,
                                        garbageCanPosition.z),
                            TimeForAnimation);
                }


                   else if (deletedNode.TryGetComponentOrLog(out EdgeRef edgeReference))
                    {
                        Node target = edgeReference.Value.Target;
                        GameObject targetObject = SceneQueries.RetrieveGameNode(target.ID);
                        Vector3 targetPosi = targetObject.transform.position;
                        GarbageCanPositionForEdges = new Vector3(garbageCanPosition.x, garbageCanPosition.y + VerticalHeight - targetPosi.y, garbageCanPosition.z);

                        if (targetPosi.x > 0)
                        {
                            GarbageCanPositionForEdges.x -= targetPosi.x;
                        }
                        else
                        {
                            GarbageCanPositionForEdges.x += (targetPosi.x * -1); // might be written in "math.Abs way"
                        }

                        if (targetPosi.z > 0)
                        {
                            GarbageCanPositionForEdges.z -= targetPosi.z;
                        }
                        else
                        {
                            GarbageCanPositionForEdges.z += (targetPosi.z * -1);
                        }

                        Tweens.Move(deletedNode,
                                GarbageCanPositionForEdges,
                                TimeForAnimation);
                    }

            }
            yield return new WaitForSeconds(TimeToWait);

            foreach (GameObject deletedNode in deletedNodesAndEdges)
            {
                if (deletedNode.HasNodeRef())
                {
                    Vector3 shrinkFactor = VectorOperations.DivideVectors(deletedNode.transform.localScale, defaultGarbageVector);
                    shrinkFactors.TryAdd(deletedNode, shrinkFactor);
                    deletedNode.transform.localScale = Vector3.Scale(deletedNode.transform.localScale, shrinkFactor);
                    Tweens.Move(deletedNode, garbageCanPosition, TimeForAnimation);
                }

                else
                {
                    Tweens.Move(deletedNode, new Vector3(deletedNode.transform.position.x, deletedNode.transform.position.y - VerticalHeight, deletedNode.transform.position.z), TimeForAnimation);
                }
            }
            yield return new WaitForSeconds(TimeToWait);
        }

        /// <summary>
        /// Removes all given nodes respectively edges from the garbage can back to their original location.
        /// </summary>
        /// <param name="deletedNodesAndEdges">The nodes to be removed from the garbage can</param>
        /// <returns>the waiting time between moving deleted nodes from the garbage can and then to the city</returns>
        public static IEnumerator RemoveFromGarbage(IList<GameObject> deletedNodesOrEdges, Dictionary<GameObject, Vector3> oldPositions)
        {
            Vector3 garbageCanPosition = GarbageCanPosition();

            // vertical movement of nodes respectively edges.
            foreach (GameObject deletedNode in deletedNodesOrEdges)
            {
                if (deletedNode.HasNodeRef())
                {
                    Portal.SetInfinitePortal(deletedNode);
                    Tweens.Move(deletedNode, new Vector3(garbageCanPosition.x, garbageCanPosition.y + VerticalHeight, garbageCanPosition.z), TimeForAnimation);
                }
                else
                {
                    Tweens.Move(deletedNode, new Vector3(deletedNode.transform.position.x, deletedNode.transform.position.y + VerticalHeight, deletedNode.transform.position.z), TimeForAnimation);
                }
                if (deletedNode.HasNodeRef())
                {
                    PlayerSettings.GetPlayerSettings().StartCoroutine(WaitAndExpand(deletedNode));
                }
            }

            yield return new WaitForSeconds(TimeToWait);

            // back to the original position
            foreach (GameObject nodeOrEdgeReference in deletedNodesOrEdges)
            {
                Portal.SetInfinitePortal(nodeOrEdgeReference);
                Tweens.Move(nodeOrEdgeReference, oldPositions[nodeOrEdgeReference], TimeForAnimation);
            }

            yield return new WaitForSeconds(TimeToWait);
            deletedNodesOrEdges.Clear();
            deletedEdges.Clear();
            InteractableObject.UnselectAll(true);
        }

        /// <summary>
        /// Unhides the given <paramref name="deletedObject"/> after some delay.
        ///
        /// Intended to be used as a co-routine.
        /// </summary>
        /// <param name="deletedObject">The object to be removed from the garbage can</param>
        /// <returns>the waiting time between moving deleted objects from the garbage can and then to the city</returns>
        private static IEnumerator WaitAndExpand(GameObject deletedObject)
        {
            yield return new WaitForSeconds(TimeToWait);
            Vector3 shrinkFactor = shrinkFactors[deletedObject];
            float exponent = 1 / StepsOfExpansionAnimation;
            shrinkFactor = VectorOperations.ExponentOfVectorCoordinates(shrinkFactor, exponent);

            for (float animationsCount = StepsOfExpansionAnimation; animationsCount > 0; animationsCount--)
            {
                deletedObject.transform.localScale = VectorOperations.DivideVectors(shrinkFactor, deletedObject.transform.localScale);
                yield return new WaitForSeconds(TimeBetweenExpansionAnimation);
            }
        }

        /// <summary>
        /// Hides given <paramref name="gameEdge"/> and adds it to the list of deleted edges.
        /// </summary>
        /// <param name="gameEdge">game edge to be hidden</param>
        public static void HideEdge(GameObject gameEdge)
        {
            gameEdge.SetVisibility(false, true);
            deletedEdges.TryAdd(gameEdge, gameEdge.GetGraph());
        }
    }
}
