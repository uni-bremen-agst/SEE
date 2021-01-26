using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

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
        public static float MovingSpeed = 4.2f;

        /// <summary>
        /// Moves the given <paramref name="movingObject"/> on a sphere around the
        /// camera. The radius sphere of this sphere is the original distance
        /// from the <paramref name="movingObject"/> to the camera. The point
        /// on that sphere is determined by a ray driven by the user hitting
        /// this sphere. The speed of travel is defind by <see cref="MovingSpeed"/>.
        /// 
        /// This method is expected to be called at every Update().
        /// </summary>
        /// <param name="movingObject">the object to be moved.</param>
        public static void MoveTo(GameObject movingObject)
        {
            float step = MovingSpeed * Time.deltaTime;
            Vector3 target = TipOfRayPosition(movingObject); // TODO(torben): this should probably not be the same distance but raycast onto everything and put object on top of the closest hit or something like that?
            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, target, step);
        }

        /// <summary>
        /// Moves the given <paramref name="movingObject"/> on a sphere around the
        /// camera. The radius sphere of this sphere is the original distance
        /// from the <paramref name="movingObject"/> to the camera. The point
        /// on that sphere is determined by a ray driven by the user hitting
        /// this sphere. The speed of travel is defind by <see cref="MovingSpeed"/>.
        /// 
        /// This method is expected to be called at every Update().
        /// 
        /// You can Lock one to three axes 
        /// </summary>
        /// <param name="movingObject">the object to be moved</param>
        /// <param name="x">True if it should be moved on this axes</param>
        /// <param name="y">True if it should be moved on this axes</param>
        /// <param name="z">True if it should be moved on this axes</param>
        public static void MoveToLockAxes(GameObject movingObject, bool x, bool y, bool z)
        {
            float step = MovingSpeed * Time.deltaTime;
            Vector3 target = TipOfRayPosition(movingObject);
            Vector3 movingObjectPos = movingObject.transform.position;

            if (!x)
            {
                target.x = movingObjectPos.x;
            }
            if (!y)
            {
                target.y = movingObjectPos.y;
            }
            if (!z)
            {
                target.z = movingObjectPos.z;
            }
            movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, target, step);
        }

        /// <summary>
        /// Finalizes the position of the <paramref name="movingObject"/>. If the current
        /// pointer of the user is pointing at a game object with a NodeRef the final
        /// position of <paramref name="movingObject"/> will be the game object with a NodeRef
        /// that is at the deepest level of the node hierarchy (the pointer may actually 
        /// hit multiple nested nodes), in the following called target parent. The 
        /// <paramref name="movingObject"/> will then be placed onto the roof of the target
        /// parent and its associated graph node will be become a child of the graph node
        /// associated with the target parent and <paramref name="movingObject"/> becomes
        /// a child of the target node (the game-node hierarchy and the graph-node hierarchy
        /// must be in sync). 
        /// 
        /// If no such target node can be identified, the <paramref name="movingObject"/> will 
        /// return to its <paramref name="originalPosition"/> and neither the graph-node hierarchy 
        /// nor the game-node hierarchy will be changed.
        ///
        /// </summary>
        /// <param name="movingObject">the object being moved</param>
        /// <param name="originalPosition">the original world-space position of <paramref name="movingObject"/>
        /// to be used if the movement cannot be finalized</param>
        public static void FinalizePosition(GameObject movingObject, Vector3 originalPosition)
        {
            // The underlying graph node of the moving object.
            Node movingNode = movingObject.GetComponent<NodeRef>().Value;
            // The new parent of the movingNode in the underlying graph.
            Node newGraphParent = null;
            // The new parent of the movingNode in the game-object hierarchy.
            GameObject newGameParent = null;
            // The new position of the movingNode in world space.
            Vector3 newPosition = Vector3.negativeInfinity;

            // Note that the order of the results of RaycastAll() is undefined.
            // Hence, we need to identify the node in the node hierarchy that
            // is at the lowest level in the tree (more precisely, the one with
            // the greatest value of the node attribute Level; Level counting
            // starts at the root and increases downward into the tree).            
            foreach (RaycastHit hit in Physics.RaycastAll(UserPointsTo()))
            {
                // Must be different from the movingObject itself
                if (hit.collider.gameObject != movingObject)
                {
                    NodeRef nodeRef = hit.transform.GetComponent<NodeRef>();
                    // Is it a node at all and if so, are they in the same graph?
                    if (nodeRef != null && nodeRef.Value.ItsGraph == movingNode.ItsGraph)
                    {
                        // update newParent when we found a node deeper into the tree
                        if (newGraphParent == null || nodeRef.Value.Level > newGraphParent.Level)
                        {
                            newGraphParent = nodeRef.Value;
                            newGameParent = hit.collider.gameObject;
                            newPosition = hit.point;
                        }
                    }
                }
            }

            if (newGraphParent != null)
            {
                movingObject.transform.position = newPosition;
                PutOn(movingObject, newGameParent);
                if (movingNode.Parent != newGraphParent)
                {
                    movingNode.Reparent(newGraphParent);
                    movingObject.transform.SetParent(newGameParent.transform);
                }
            }
            else
            {
                // Attempt to move the node outside of any node in the node hierarchy.
                // => Reset its original transform.
                Tweens.Move(movingObject, originalPosition, 1.0f);
            }
        }


        /// <summary>
        /// Sets the new Parent for a GameObject via Network
        /// </summary>
        /// <param name="child">child Gamobject</param>
        /// <param name="parentID">Parent GameObject ID</param>
        /// <param name="position">new position</param>
        public static void NetworkFinalizeNodePosition(GameObject child ,string parentID, Vector3 position)
        {
            GameObject parent = GameObject.Find(parentID);
            child.transform.position = position;
            PutOn(child, parent);
            child.GetComponent<NodeRef>().Value.Reparent(parent.GetComponent<NodeRef>().Value);
            child.transform.SetParent(parent.transform);
        }

        /// <summary>
        /// Puts <paramref name="child"/> on top of <paramref name="parent"/>.
        /// </summary>
        /// <param name="child">child</param>
        /// <param name="parent">parent</param>
        private static void PutOn(GameObject child, GameObject parent)
        {
            // FIXME: child may not actually fit into parent, in which we should 
            // downscale it until it fits
            Vector3 childCenter = child.transform.position;
            float parentRoof = parent.transform.position.y + parent.transform.lossyScale.y / 2;
            childCenter.y = parentRoof + child.transform.lossyScale.y / 2;
            child.transform.position = childCenter;
        }

        // -------------------------------------------------------------
        // User input
        // -------------------------------------------------------------

        /// <summary>
        /// A ray from the user.
        /// </summary>
        /// <returns>ray from the user</returns>
        private static Ray UserPointsTo()
        {
            // FIXME: We need to an interaction for VR, too.
            return MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
        }

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
            Ray ray = UserPointsTo();
            return ray.GetPoint(Vector3.Distance(ray.origin, selectedObject.transform.position));
        }
    }
}