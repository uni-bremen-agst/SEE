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
    public static class AnimationsOfDeletion
    {
        /// <summary>
        /// The garbage can the deleted nodes will be moved to. It is the object named 
        /// <see cref="GarbageCanName"/>.
        /// </summary>
        private static GameObject garbageCan;

        /// <summary>
        /// The name of the game object representing the garbage can.
        /// </summary>
        private const string GarbageCanName = "GarbageCan";

        /// <summary>
        /// The animation time of the animation of moving a node to the top of the garbage can.
        /// </summary>
        private const float TimeForAnimation = 1f;

        /// <summary>
        /// The waiting time of the animation for moving a node into a garbage can from over the garbage can.
        /// </summary>
        private const float TimeToWait = 1f;

        /// <summary>
        /// The vertical height above the garbage can the removed nodes move to during the undo process respectively
        /// the nodes move to having been deleted.
        /// </summary>
        private const float verticalHeight = 1.4f;

        /// <summary>
        ///  A vector for an objects localScale which fits into the garbage can.
        ///  TODO: Currently set to an absolute value. Should be set abstract, e.g., half of the 
        ///  garbage can's diameter. 
        /// </summary>
        private static readonly Vector3 defaultGarbageVector = new Vector3(0.1f, 0.1f, 0.1f);

        /// <summary>
        /// A list of ratios of the current localScale and a target scale.
        /// </summary>
        private static ConcurrentDictionary<GameObject, Vector3> shrinkFactors { get; set; } = new ConcurrentDictionary<GameObject, Vector3>();

        // <summary>
        /// A history of the old positions of the nodes deleted by this action.
        /// </summary>
        private static Dictionary<GameObject, Vector3> oldPositions = new Dictionary<GameObject, Vector3>();

        /// <summary>
        /// Number of animations used for an object's expansion, removing it from the garbage can.
        /// </summary>
        private const float StepsOfExpansionAnimation = 10;

        /// <summary>
        /// The time (in seconds) between animations of expanding a node that is being restored
        /// from the garbage can.
        /// </summary>
        private const float TimeBetweenExpansionAnimation = 0.14f;

        /// <summary>
        /// A history of all edges and the graph where they were attached to, deleted by this action.
        /// </summary>
        private static ConcurrentDictionary<GameObject, Graph> deletedEdges { get; set; } = new ConcurrentDictionary<GameObject, Graph>();

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
        /// Moves all nodes in <paramref name="deletedNodes"/> to the garbage can
        /// using an animation. When they finally arrive there, they will be 
        /// deleted. 
        /// 
        /// Assumption: <paramref name="deletedNodes"/> contains all nodes in a subtree
        /// of the game-node hierarchy. All of them represent graph nodes.
        /// </summary>
        /// <param name="deletedNodes">the deleted nodes which will be moved to the garbage can.</param>
        /// <returns>the waiting time between moving deleted nodes over the garbage can and then into the garbage can</returns>
        public static IEnumerator MoveNodeToGarbage(IList<GameObject> deletedNodes)
        {
            Vector3 garbageCanPosition = GarbageCanPosition();

            // We need to reset the portal of all all deletedNodes so that we can move
            // them to the garbage bin. Otherwise they will become invisible if they 
            // leave their portal.
            foreach (GameObject deletedNode in deletedNodes)
            {
                oldPositions[deletedNode] = deletedNode.transform.position;
                Portal.SetInfinitePortal(deletedNode);
            }
            foreach (GameObject deletedNode in deletedNodes)
            {
                Tweens.Move(deletedNode, 
                            new Vector3(garbageCanPosition.x,
                                        garbageCanPosition.y + verticalHeight,
                                        garbageCanPosition.z), 
                            TimeForAnimation);
            }
            yield return new WaitForSeconds(TimeToWait);

            foreach (GameObject deletedNode in deletedNodes)
            {
                Vector3 shrinkFactor = VectorOperations.DivideVectors(deletedNode.transform.localScale, defaultGarbageVector);
                shrinkFactors.TryAdd(deletedNode, shrinkFactor);
                deletedNode.transform.localScale = Vector3.Scale(deletedNode.transform.localScale, shrinkFactor);
                Tweens.Move(deletedNode, garbageCanPosition, TimeForAnimation);
            }
            yield return new WaitForSeconds(TimeToWait);
        }

        /// <summary>
        /// Removes all given nodes from the garbage can back to their original location.
        /// </summary>
        /// <param name="deletedNodes">The nodes to be removed from the garbage can</param>
        /// <returns>the waiting time between moving deleted nodes from the garbage can and then to the city</returns>
        public static IEnumerator RemoveNodeFromGarbage(IList<GameObject> deletedNodes)
        {
            Vector3 garbageCanPosition = GarbageCanPosition();

            // vertical movement of nodes
            foreach (GameObject deletedNode in deletedNodes)
            {
                Tweens.Move(deletedNode, new Vector3(garbageCanPosition.x, garbageCanPosition.y + verticalHeight, garbageCanPosition.z), TimeForAnimation);
                PlayerSettings.GetPlayerSettings().StartCoroutine(WaitAndExpand(deletedNode));
            }

            yield return new WaitForSeconds(TimeToWait);

            // back to the original position
            foreach (GameObject node in deletedNodes)
            {
                Tweens.Move(node, oldPositions[node], TimeForAnimation);
            }

            yield return new WaitForSeconds(TimeToWait);
            deletedNodes.Clear();
            deletedEdges.Clear();
            InteractableObject.UnselectAll(true);
        }

        /// <summary>
        /// Coroutine that waits and expands the shrunk object which is currently being removed from the garbage can.
        /// </summary>
        /// <param name="deletedNode">The nodes to be removed from the garbage-can</param>
        /// <returns>the waiting time between moving deleted nodes from the garbage can and then to the city</returns>
        private static IEnumerator WaitAndExpand(GameObject deletedNode)
        {
            yield return new WaitForSeconds(TimeToWait);
            Vector3 shrinkFactor = shrinkFactors[deletedNode];
            float exponent = 1 / StepsOfExpansionAnimation;
            shrinkFactor = VectorOperations.ExponentOfVectorCoordinates(shrinkFactor, exponent);

            for (float animationsCount = StepsOfExpansionAnimation; animationsCount > 0; animationsCount--)
            {
                deletedNode.transform.localScale = VectorOperations.DivideVectors(shrinkFactor, deletedNode.transform.localScale);
                yield return new WaitForSeconds(TimeBetweenExpansionAnimation);
            }
        }

        /// <summary>
        /// Unhides the given <paramref name="gameEdge"/> after some delay.
        /// 
        /// Intended to be used as a co-routine.
        /// </summary>
        /// <param name="gameEdge">game edge to be shown again</param>
        /// <returns>The time of the delay</returns>
        public static IEnumerator UnhideEdge(GameObject gameEdge)
        {
            yield return new WaitForSeconds(TimeForAnimation + TimeToWait);
            gameEdge.SetVisibility(true, true);
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
