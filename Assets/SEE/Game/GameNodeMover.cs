using System;
using DG.Tweening;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using UnityEngine;
using static SEE.Utils.Raycasting;

namespace SEE.Game
{
    /// <summary>
    /// Allows to move game nodes (game objects representing a graph node).
    /// </summary>
    public static class GameNodeMover
    {
        /// <summary>
        /// The speed by which to move a selected object.
        /// </summary>
        private const float MOVING_SPEED = 1.0f;

        /// <summary>
        /// Factor by which nodes should be scaled relative to their parents in <see cref="PutOn"/>.
        /// </summary>
        public const float SCALING_FACTOR = 0.5f;

        /// <summary>
        /// Moves the given <paramref name="movingObject"/> on a sphere around the
        /// camera. The radius sphere of this sphere is the original distance
        /// from the <paramref name="movingObject"/> to the camera. The point
        /// on that sphere is determined by a ray driven by the user hitting
        /// this sphere. The speed of travel is defined by <see cref="MOVING_SPEED"/>.
        ///
        /// This method is expected to be called at every Update().
        /// </summary>
        /// <param name="movingObject">the object to be moved.</param>
        public static void MoveTo(GameObject movingObject)
        {
            float step = MOVING_SPEED * Time.deltaTime;
            // FIXME regarding 'target': currently, the tip of the ray is of a fixed distance from
            // the ray starting position into the ray direction. it would be better if we did a
            // raycast into the scene, see what we hit and use that as the tip. the result feels
            // way better and it also prevents intersection with other objects.
            Vector3 target = TipOfRayPosition(movingObject);
            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, target, step);
        }

        /// <summary>
        /// Finalizes the position of the <paramref name="movingObject"/>. If the current
        /// pointer of the user is pointing at a game object with a <see cref="NodeRef"/>, the final
        /// position of <paramref name="movingObject"/> will be the game object with a <see cref="NodeRef"/>
        /// that is at the deepest level of the node hierarchy (the pointer may actually
        /// hit multiple nested nodes), in the following called target parent. The
        /// <paramref name="movingObject"/> will then be placed onto the roof of the target
        /// parent and its associated graph node will be become a child of the graph node
        /// associated with the target parent and <paramref name="movingObject"/> becomes
        /// a child of the target node (the game-node hierarchy and the graph-node hierarchy
        /// must be in sync). That target node is returned.
        ///
        /// If no such target node can be identified, neither the graph-node hierarchy
        /// nor the game-node hierarchy will be changed and null will be returned.
        ///
        /// The assumption is that <paramref name="movingObject"/> is not the root node
        /// of a code city.
        /// </summary>
        /// <param name="movingObject">the object being moved</param>
        /// <returns>the game object that is the new parent or null</returns>
        public static GameObject FinalizePosition(GameObject movingObject, Vector3 oldPosition)
        {
            // The underlying graph node of the moving object.
            NodeRef movingNodeRef = movingObject.GetComponent<NodeRef>();
            Node movingNode = movingNodeRef.Value;

            RaycastLowestNode(out RaycastHit? raycastHit, out Node newGraphParent, movingNodeRef);

            if (newGraphParent != null && raycastHit != null)
            {
                // The new parent of the movingNode in the game-object hierarchy.
                GameObject newGameParent = raycastHit.Value.collider.gameObject;
                // Reflexion analysis: Dropping implementation node on architecture node
                if (newGraphParent.IsInArchitecture() && movingNode.IsInImplementation())
                {
                    // TODO: Make sure this action is still reversible
                    SEEReflexionCity reflexionCity = newGameParent.ContainingCity<SEEReflexionCity>();
                    Reflexion analysis = reflexionCity.Analysis;
                    analysis.AddToMapping(movingNode, newGraphParent, overrideMapping: true);
                    return newGameParent;
                }
                else if (newGraphParent.IsInImplementation() && movingNode.IsInArchitecture())
                {
                    ShowNotification.Error("Reflexion Analysis", "Please map from implementation to "
                                                                 + "architecture, not the other way around.");
                    return null;
                }
                else
                {
                    // The new position of the movingNode in world space.
                    Vector3 newPosition = raycastHit.Value.point;
                    movingObject.transform.position = newPosition;
                    PutOn(movingObject.transform, newGameParent);
                    if (movingNode.Parent != newGraphParent)
                    {
                        movingNode.Reparent(newGraphParent);
                        movingObject.transform.SetParent(newGameParent.transform);
                    }

                    return newGameParent;
                }
            }
            else
            {
                // Attempt to move the node outside of any node in the node hierarchy.
                if (movingNode.IsInImplementation() && movingNode.IsInMapping())
                {
                    SEEReflexionCity reflexionCity = movingObject.ContainingCity<SEEReflexionCity>();
                    
                    // We'll change its parent so it becomes a root node in the implementation city.
                    // The user will have to drop it on another node to re-parent it.
                    movingObject.transform.SetParent(reflexionCity.ImplementationRoot.RetrieveGameNode().transform);
                    
                    // If the node was already mapped, we'll unmap it again.
                    Reflexion analysis = reflexionCity.Analysis;
                    analysis.DeleteFromMapping(movingNode);
                    return movingObject.transform.parent.gameObject;
                }
                return null;
            }
        }

        /// <summary>
        /// Sets the new parent for <paramref name="child"/> to the game node with <paramref name="parentName"/>
        /// at the given <paramref name="position"/> in world space.
        /// </summary>
        /// <param name="child">child whose parent is to be set</param>
        /// <param name="parentName">the new parent's name (assumed to be unique)</param>
        /// <param name="position">new position</param>
        public static void Reparent(GameObject child, string parentName, Vector3 position)
        {
            GameObject parent = GameObject.Find(parentName);
            if (parent != null)
            {
                child.transform.position = position;
                PutOn(child.transform, parent, parent.transform.position.XZ());
                child.GetComponent<NodeRef>().Value.Reparent(parent.GetComponent<NodeRef>().Value);
                child.transform.SetParent(parent.transform);
            }
            else
            {
                throw new Exception($"No parent found with name {parentName}.");
            }
        }

        /// <summary>
        /// Puts <paramref name="child"/> on top of <paramref name="parent"/> and scales it down,
        /// assuming <paramref name="scaleDown"/> is true.
        /// </summary>
        /// <param name="child">child to be put on <paramref name="parent"/></param>
        /// <param name="parent">parent the <paramref name="child"/> is put on</param>
        /// <param name="targetXZ">XZ coordinates <paramref name="child"/> should be placed at
        /// (will be center of <paramref name="parent"/> if not given)</param>
        /// <param name="topPadding">Additional amount of empty space that should be between <paramref name="parent"/>
        /// and <paramref name="child"/>, given as a percentage of the parent's height</param>
        /// <param name="setParent">Whether <paramref name="parent"/> should become a parent of
        /// <paramref name="child"/></param>
        /// <param name="scaleDown">Whether <paramref name="child"/> should be scaled down to fit into
        /// <paramref name="parent"/></param>
        /// <returns>Old scale (i.e., before the changes from this function were applied, but after its parent
        /// was changed if <paramref name="setParent"/> was true) of <paramref name="child"/></returns>
        public static Vector3 PutOn(Transform child, GameObject parent, Vector2? targetXZ = null, float topPadding = 0,
                                 bool setParent = true, bool scaleDown = false)
        {
            if (setParent)
            {
                child.SetParent(parent.transform);
            }
            Vector3 oldScale = child.localScale;
            if (scaleDown)
            {
                child.localScale = new Vector3(SCALING_FACTOR, SCALING_FACTOR, SCALING_FACTOR);
            }
            
            targetXZ ??= child.position.XZ();
            float parentRoof = parent.GetRoof();
            Vector3 targetPosition = new Vector3(targetXZ.Value.x, parentRoof + child.lossyScale.y / 2.0f + topPadding * parent.transform.lossyScale.y, targetXZ.Value.y);
            child.position = targetPosition;
            return oldScale;
        }

        /// <summary>
        /// Moves the given <paramref name="movingObject"/> on a sphere around the
        /// camera. The radius of this sphere is the original distance
        /// from the <paramref name="movingObject"/> to the camera. The point
        /// on that sphere is determined by a ray driven by the user hitting
        /// this sphere. The speed of travel is defined by <see cref="MOVING_SPEED"/>.
        ///
        /// This method is expected to be called at every Update().
        ///
        /// You can lock any of the three axes.
        /// </summary>
        /// <param name="movingObject">the object to be moved</param>
        /// <param name="lockX">whether the movement should be locked on this axis</param>
        /// <param name="lockY">whether the movement should be locked on this axis</param>
        /// <param name="lockZ">whether the movement should be locked on this axis</param>
        public static void MoveToLockAxes(GameObject movingObject, bool lockX, bool lockY, bool lockZ)
        {
            float step = MOVING_SPEED * Time.deltaTime;
            Vector3 target = TipOfRayPosition(movingObject);
            Vector3 movingObjectPos = movingObject.transform.position;

            if (!lockX)
            {
                target.x = movingObjectPos.x;
            }

            if (!lockY)
            {
                target.y = movingObjectPos.y;
            }

            if (!lockZ)
            {
                target.z = movingObjectPos.z;
            }

            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, target, step);
        }

        // -------------------------------------------------------------
        // User input
        // -------------------------------------------------------------

        /// <summary>
        /// Returns the position of the tip of the ray drawn from the camera towards
        /// the position the user is currently pointing to. The distance of that
        /// point along this ray is the distance between the camera from which the
        /// ray originated and the position of the given <paramref name="selectedObject"/>.
        ///
        /// That means, the selected object moves on a sphere around the camera
        /// at the distance of the selected object.
        /// </summary>
        /// <param name="selectedObject">the selected object currently moved around</param>
        /// <returns>tip of the ray</returns>
        private static Vector3 TipOfRayPosition(GameObject selectedObject)
        {
            return UserPointsTo().GetPoint(Vector3.Distance(UserPointsTo().origin, selectedObject.transform.position));
        }
    }
}