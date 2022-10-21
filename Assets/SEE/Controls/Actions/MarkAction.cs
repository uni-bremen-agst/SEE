using System;
using System.Collections;
using System.Collections.Generic;
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
    /// </summary>
    public class MarkAction : AbstractPlayerAction
    {
        public override HashSet<string> GetChangedObjects()
        {
            throw new System.NotImplementedException();
        }

        // Tupel (node, markerSphere)
        private List<(GameObject, GameObject)> markedNodes = new List<(GameObject, GameObject)>();


        public static MarkAction CreateMarkAction() => new MarkAction();

        public override void Undo()
        {
            base.Undo();
        }

        public override void Redo()
        {
            base.Redo();
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
                    // Extract the code city node.
                    string sphereTag = cnode.tag += "-MARKED";
                    GameObject marker = GameNodeMarker.CreateMarker(cnode);
                    marker.name = sphereTag;
                    markedNodes.Add((cnode, marker));
                }
                else
                {
                    GameObject marker = GetMarkerOfNode(cnode) ?? throw new ArgumentNullException("GetMarkerOfNode(cnode)");
                    Destroyer.DestroyGameObject(marker);
                    RemoveNodeFromMarked(cnode);
                }
            }

            return ret;
        }
    }
}