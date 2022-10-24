using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    internal class NodeNotFoundException : Exception
    {
    }


    /// <summary>
    /// Author: Hannes Kuss
    /// 
    /// An action for selecting nodes in a code-city.
    ///
    /// The selected node are marked with a unity-primitive node.
    /// The marked nodes can also be deselected with another click.
    ///
    /// </summary>
    public class MarkAction : AbstractPlayerAction
    {
        public override HashSet<string> GetChangedObjects() =>
            new HashSet<string>
            {
                lastAction.Item1.name, GetMarkerOfNode(lastAction.Item1).name
            };

        // Internal visibility because GameNodeMarker also uses it.
        // This suffix is appended to all node markers GameObject names
        internal static string MARKER_NAME_SUFFIX = "-MARKED";

        // A tuple representing the last action (node, marked)
        // node os the GameObject which the user interacted with.
        // marked is true when the node was marked and is false when the node was unmarked
        private (GameObject, bool) lastAction;
        

        public static MarkAction CreateMarkAction() => new MarkAction();

        public static ReversibleAction CreateReversibleAction()
        {
            throw new NotImplementedException();
        }

        public override void Undo()
        {
            //base.Undo();
            // When the last action was, to mark a node, then the node should be unmarked
            if (lastAction.Item2)
            {
                GameObject node = lastAction.Item1;
                GameObject marker = GetMarkerOfNode(node) ?? throw new ArgumentNullException("GetMarkerOfNode(node)");
                // Destroy marker
                Destroyer.DestroyGameObject(marker);

            }
            // When the last action was, to unmark a node, then the node should be marked again
            else
            {
                GameObject node = lastAction.Item1;
                string sphereTag = node.name += MARKER_NAME_SUFFIX;
                GameObject marker = GameNodeMarker.CreateMarker(node);
                marker.name = sphereTag;
        
            }
        }

        public override void Redo()
        {
            // Properly also redundant but also just to make sure. 

            // When the last action was, to mark a node, then the node should be unmarked
            if (lastAction.Item2)
            {
                GameObject node = lastAction.Item1;
                GameObject marker = GetMarkerOfNode(node) ?? throw new ArgumentNullException("GetMarkerOfNode(node)");
                // Destroy marker
                Destroyer.DestroyGameObject(marker);
                
            }
            // When the last action was, to unmark a node, then the node should be marked again
            else
            {
                GameObject node = lastAction.Item1;
                string sphereTag = node.name += MARKER_NAME_SUFFIX;
                GameObject marker = GameNodeMarker.CreateMarker(node);
                marker.name = sphereTag;

            }
        }

        public override ActionStateType GetActionStateType() => ActionStateType.MarkNode;

        public override ReversibleAction NewInstance() => new MarkAction();

        /// <summary>
        /// Checks if a node is marked
        /// </summary>
        /// <param name="node">The node to check</param>
        /// <returns></returns>
        private bool IsNodeMarked(GameObject node)
        {
            for (int i = 0; i < node.transform.childCount; i++)
            {
                // When a the child has the tag -MARKED
                if (node.transform.GetChild(i).name.EndsWith(MARKER_NAME_SUFFIX))
                {
                    return true;
                }
            }
            return false;
        }
        

        /// <summary>
        /// Returns the Marker sphere of a node
        /// </summary>
        /// <param name="node"></param>
        /// <returns>The marker object of the node.
        /// Can be null</returns>
        private GameObject GetMarkerOfNode(GameObject node)
        {
            for (int i = 0; i < node.transform.childCount; i++)
            {
                if (node.transform.GetChild(i).name.EndsWith("MARKED"))
                {
                    return node.transform.GetChild(i).gameObject;
                }
            }

            return null;
        }


        public override bool Update()
        {
            var ret = true;
            if (Input.GetKeyDown(KeyCode.U))
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    Redo();
                }
                else
                {
                    Undo();
                }
            }

            // When the user clicks the left mouse button and is pointing to a node
            if (Input.GetMouseButtonDown(0) &&
                Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) ==
                HitGraphElement.Node)
            {
                GameObject cnode = raycastHit.collider.gameObject;


                // When the clicked node wasn't marked until now
                if (!IsNodeMarked(cnode))
                {
                    currentState = ReversibleAction.Progress.Completed;
                    // Extract the code city node.
                    string sphereTag = cnode.name += MARKER_NAME_SUFFIX;
                    GameObject marker = GameNodeMarker.CreateMarker(cnode);
                    marker.name = sphereTag;
                }
                // When the clicked node was already marked
                else
                {
                    GameObject marker = GetMarkerOfNode(cnode) ??
                                        throw new ArgumentNullException("GetMarkerOfNode(cnode)");
                    Destroyer.DestroyGameObject(marker);
                }
            }

            return ret;
        }
    }
}