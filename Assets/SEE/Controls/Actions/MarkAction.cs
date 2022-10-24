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
    
    ///
    /// <summary>
    /// Author: Hannes Kuss
    /// 
    /// An action for selecting nodes in a code-city.
    ///
    /// The selected node are marked with a unity-primitive node.
    /// The marked nodes can also be deselected with another click.
    ///
    ///
    /// The Action should also keep full track of the users interactions.
    /// So all actions can be undone/redone
    /// </summary>
    public class MarkAction : AbstractPlayerAction
    {
        public override HashSet<string> GetChangedObjects() =>
            new HashSet<string>(markedNodes.Select(x => x.Item1.ID()).ToList());

        // Internal visibility because GameNodeMarker also uses it.
        // This suffix is appended to all node markers GameObject names
        internal static string MARKER_NAME_SUFFIX = "-MARKED";

        // Tuple for marked nodes (node, markerSphere)
        private List<(GameObject, GameObject)> markedNodes = new List<(GameObject, GameObject)>();

        // A stack with a tupel (node, marked) for keeping track of the last actions/interactions with nodes.
        // marked is true, when a node was marked in that action and false if unmarked
        private Stack<(GameObject, bool)> undoMarkers = new Stack<(GameObject, bool)>();

        // A stack with a tupel (node, marked)
        // marked is true, when a node was marked in that action and false if unmarked
        private Stack<(GameObject, bool)> redoMarkers = new Stack<(GameObject, bool)>();

        // Is set, when the undo/redo stack should be cleaned up next time
        private bool doCleanUpUndoNextTime;

        private (GameObject, bool) lastA;
        
        public static MarkAction CreateMarkAction() => new MarkAction();

        public static ReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        public override void Undo()
        {
            //base.Undo();
            doCleanUpUndoNextTime = true;
            var lastAction = undoMarkers.Pop();

            // When the last action was, to mark a node, then the node should be unmarked
            if (lastAction.Item2)
            {
                GameObject node = lastAction.Item1;
                GameObject marker = GetMarkerOfNode(node) ?? throw new ArgumentNullException("GetMarkerOfNode(node)");
                // Destroy marker
                Destroyer.DestroyGameObject(marker);

                // probably redundant but just to make sure.
                // TODO: Remove this later
                if (IsNodeMarked(node))
                {
                    RemoveNodeFromMarked(node);
                    // Add the node to the redo list as removed
                    redoMarkers.Push((node, false));
                }
            }
            // When the last action was, to unmark a node, then the node should be marked again
            else
            {
                GameObject node = lastAction.Item1;
                string sphereTag = node.tag += MARKER_NAME_SUFFIX;
                GameObject marker = GameNodeMarker.CreateMarker(node);
                marker.name = sphereTag;
                markedNodes.Add((node, marker));
                // Add the node to the redo list as added
                redoMarkers.Push((node, true));
            }
        }

        public override void Redo()
        {
            var lastAction = undoMarkers.Pop();

            // Properly also redundant but also just to make sure. 
            doCleanUpUndoNextTime = true;

            // When the last action was, to mark a node, then the node should be unmarked
            if (lastAction.Item2)
            {
                GameObject node = lastAction.Item1;
                GameObject marker = GetMarkerOfNode(node) ?? throw new ArgumentNullException("GetMarkerOfNode(node)");
                // Destroy marker
                Destroyer.DestroyGameObject(marker);

                // probably redundant but just to make sure.
                // TODO: Remove this later
                if (IsNodeMarked(node))
                {
                    RemoveNodeFromMarked(node);
                    // Add the node to the redo list as removed
                    undoMarkers.Push((node, false));
                }
            }
            // When the last action was, to unmark a node, then the node should be marked again
            else
            {
                GameObject node = lastAction.Item1;
                string sphereTag = node.name += MARKER_NAME_SUFFIX;
                GameObject marker = GameNodeMarker.CreateMarker(node);
                marker.name = sphereTag;
                markedNodes.Add((node, marker));
                // Add the node to the redo list as added
                undoMarkers.Push((node, true));
            }
        }

        public override ActionStateType GetActionStateType()
        {
            throw new System.NotImplementedException();
        }

        public override ReversibleAction NewInstance()
        {
            throw new System.NotImplementedException();
        }
        
        /// <summary>
        /// Checks if a node is marked
        /// </summary>
        /// <param name="node">The node to check</param>
        /// <returns></returns>
        private bool IsNodeMarked(GameObject node)
        {
            foreach (var i in markedNodes)
            {
                if (i.Item1 == node)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveNodeFromMarked(GameObject node)
        {
            foreach (var i in markedNodes)
            {
                if (i.Item1)
                {
                    markedNodes.Remove(i);
                }
            }
        }

        private GameObject GetMarkerOfNode(GameObject node)
        {
            foreach (var i in markedNodes)
            {
                if (i.Item1 == node)
                {
                    return i.Item2;
                }
            }

            return null;
        }


        public override bool Update()
        {
            var ret = true;
            // When the user clicks the left mouse button and is pointing to a node
            if (Input.GetMouseButtonDown(0) &&
                Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) ==
                HitGraphElement.Node)
            {
                GameObject cnode = raycastHit.collider.gameObject;

                if (!IsNodeMarked(cnode))
                {
                    currentState = ReversibleAction.Progress.Completed;
                    // Extract the code city node.
                    string sphereTag = cnode.tag += "-MARKED";
                    GameObject marker = GameNodeMarker.CreateMarker(cnode);
                    marker.name = sphereTag;
                    markedNodes.Add((cnode, marker));
                    lastA = (cnode, true);
                    this.undoMarkers.Push((cnode, true));
                }
                }
                else
                {
                    currentState = ReversibleAction.Progress.Completed;
                    GameObject marker = GetMarkerOfNode(cnode) ??
                                        throw new ArgumentNullException("GetMarkerOfNode(cnode)");
                    Destroyer.DestroyGameObject(marker);
                    RemoveNodeFromMarked(cnode);
                }
            }

            return ret;
        }
    }
}