using UnityEngine;
using System.Collections.Generic;

using SEE.Utils;
using SEE.DataModel;
using SEE.GO;
using System;

namespace SEE.Game
{
    /// <summary>
    /// A behaviour component that can be attached to a node representing a
    /// code city in order to zoom into it.
    /// </summary>
    //[RequireComponent(typeof(AbstractSEECity))]
    public class Transformer : MonoBehaviour
    {

        private GameObject focus;

        private bool animationIsRunning = false;

        /// ----------------------------------------------------------------------------------------------
        /// Initial state
        /// ----------------------------------------------------------------------------------------------
        /// 
        /// <summary>
        /// The left lower corner of the initial bounding box of the gameObject in world space (x/z plane).
        /// </summary>
        private Vector2 initalLeftLowerCorner;
        /// <summary>
        /// The right upper corner of the initial bounding box of the gameObject in world space (x/z plane).
        /// </summary>
        private Vector2 initialRightUpperCorner;

        private Vector2 CenterPoint
        {
            get
            {
                float width = initialRightUpperCorner.x - initalLeftLowerCorner.y;
                float height = initialRightUpperCorner.y - initalLeftLowerCorner.y;
                return new Vector2(initalLeftLowerCorner.x + width / 2.0f, initalLeftLowerCorner.y + height / 2.0f);
            }
        }

        private struct ObjectMemento
        {
            public ObjectMemento(GameObject go)
            {
                this.go = go;
                this.position = go.transform.position;
                this.localScale = go.transform.localScale;
            }
            private readonly GameObject go;
            private Vector3 position;
            private Vector3 localScale;

            public void Reset()
            {
                go.transform.position = position;
                go.transform.localScale = localScale;

            }
            public GameObject Node
            {
                get => go;
            }
            /// <summary>
            /// Original world space position.
            /// </summary>
            public Vector3 Position 
            { 
                get => position;
            }
            /// <summary>
            /// Original world space scale (lossy scale).
            /// </summary>
            public Vector3 LocalScale 
            {
                get => localScale;
            }

            public override string ToString()
            {
                return go.name
                    + " position=" + position
                    + " localScale=" + localScale;
            }
        }

        /// <summary>
        /// Contains all ascendants of the currently visible nodes in the 
        /// node hierarchy. The top-most element is the immediate parent of
        /// those visible nodes. The deepest element on the stack is the
        /// root of all nodes. The stored memento represents the state
        /// when the node was entered and before it was fit into the visible
        /// area, that is, it has the scale and position of the node within
        /// the ascendant that was shown when the node was selected to be
        /// entered.
        /// </summary>
        private Stack<ObjectMemento> activeAscendants = new Stack<ObjectMemento>();

        // All descendants of gameObject tagged by Tags.Node.
        private Dictionary<string, ObjectMemento> initialTransforms;

        public void ResetAll()
        {
            foreach (ObjectMemento memento in initialTransforms.Values)
            {
                memento.Reset();
                Show(memento.Node, true);
            }
        }

        private void Reset(ICollection<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                Reset(go);
            }
        }

        private void Reset(GameObject go)
        {
            ObjectMemento initial = initialTransforms[go.ID()];
            initial.Reset();
        }

        private Dictionary<string, ObjectMemento> GetMementos(ICollection<GameObject> descendants)
        {
            Dictionary<string, ObjectMemento> result = new Dictionary<string, ObjectMemento>();
            foreach (GameObject go in descendants)
            {
                result[go.ID()] = new ObjectMemento(go);
            }
            return result;
        }

        /// ----------------------------------------------------------------------------------------------
        /// Start up
        /// ----------------------------------------------------------------------------------------------
        private void Start()
        {       
            focus = GetRootNode(gameObject);
            activeAscendants.Push(new ObjectMemento(focus));
            ICollection<GameObject> descendants = Descendants(focus);
            initialTransforms = GetMementos(descendants);
            BoundingBox.Get(descendants, out initalLeftLowerCorner, out initialRightUpperCorner);
        }

        /// ----------------------------------------------------------------------------------------------
        /// Hierarchy
        /// ----------------------------------------------------------------------------------------------
        /// 
        private GameObject GetRootNode(GameObject parent)
        {
            GameObject result = null;
            foreach (Transform child in parent.transform)
            {
                if (child.gameObject.tag == Tags.Node)
                {
                    if (result != null)
                    {
                        Debug.LogErrorFormat("Root node of {0} is not unique: {1} and {2}", parent.name, result.name, child.gameObject.name);
                    }
                    else
                    {
                        result = child.gameObject;
                    }
                }
            }
            if (result == null)
            {
                Debug.LogErrorFormat("Object {0} has no root.\n", parent.name);
            }
            return result;
        }

        private int GetDepth(GameObject parent)
        {
            int maxLevel = 0;
            foreach (Transform child in parent.transform)
            {
                int level = GetDepth(child.gameObject);
                if (level > maxLevel)
                {
                    maxLevel = level;
                }
            }
            return maxLevel + 1;
        }

        /// <summary>
        /// Returns all descendants of given <paramref name="parent"/> including
        /// <paramref name="parent"/> that are tagged by Tags.Node.
        /// 
        /// Precondition: <paramref name="parent"/> is tagged by Tags.Node.
        /// </summary>
        /// <param name="parent">the root of the subtree to be returned</param>
        /// <returns>all descendants tagged Tags.Node</returns>
        private static HashSet<GameObject> Descendants(GameObject parent)
        {
            // all descendants of gameObject including parent
            HashSet<GameObject> descendants = new HashSet<GameObject>();

            // collect all descendants (non-recursively)
            Stack<GameObject> toBeVisited = new Stack<GameObject>();
            toBeVisited.Push(parent);
            while (toBeVisited.Count > 0)
            {
                GameObject current = toBeVisited.Pop();
                descendants.Add(current);
                foreach (Transform child in current.transform)
                {
                    if (child.gameObject.tag == Tags.Node)
                    {
                        toBeVisited.Push(child.gameObject);
                    }
                }
            }
            return descendants;
        }

        /// ----------------------------------------------------------------------------------------------
        /// Zooming in
        /// ----------------------------------------------------------------------------------------------
        public void ZoomIn(GameObject enteredNode)
        {
            if (enteredNode != null)
            {
                Debug.LogFormat("Zooming into subtree at {0}\n", enteredNode.name);
                // Save temporary scale and position of the node to be entered
                // so that we can later restore it when zooming out. This must
                // be done before we fit it into the visible area, that is: here.
                activeAscendants.Push(new ObjectMemento(enteredNode));
                DumpActiveAscendants();
                // Currently, focus and all its descendants are visible.
                HashSet<GameObject> currentlyVisible = Descendants(focus);
                // All elements in the subtree rooted by enteredNode will be visible next.
                HashSet<GameObject> newlyVisible = Descendants(enteredNode);
                // All currently visible elements that need to be hidden.
                currentlyVisible.ExceptWith(newlyVisible);
                Hide(currentlyVisible);
                focus = enteredNode;
                FitInto(enteredNode, newlyVisible);
            }
        }

        /// <summary>
        /// Scales <paramref name="parent"/> so that the total width of the size
        /// requested for its <paramref name="descendants"/> fits into initial
        /// rectangle.
        /// The aspect ratio of every node is maintained.
        /// </summary>
        /// <param name="parent">the parent of all <paramref name="descendants"/></param>
        /// <param name="descendants">layout nodes to be scaled</param>
        /// <returns>the factor by which the scale of edge node was multiplied</returns>
        private float FitInto(GameObject parent, ICollection<GameObject> descendants)
        {
            BoundingBox.Get(descendants, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner);

            float scaleFactor = Mathf.Min((initialRightUpperCorner.x - initalLeftLowerCorner.x) / (rightUpperCorner.x - leftLowerCorner.x),
                                          (initialRightUpperCorner.y - initalLeftLowerCorner.y) / (rightUpperCorner.y - leftLowerCorner.y));
            // We maintain parent's y co-ordinate. We move it only within the x/z plane.
            Vector3 newPosition = parent.transform.position;
            Vector2 center = CenterPoint;
            newPosition.x = center.x;
            newPosition.z = center.y;
            Vector3 newScale = parent.transform.localScale * scaleFactor;

            Debug.LogFormat("Transforming {0} from [{1} {2}] to [{3} {4}]\n", 
                            parent.name, 
                            parent.transform.position, parent.transform.localScale,
                            newPosition, newScale);
            // Adjust position and scale by some animation.
            animationIsRunning = true;
            iTween.MoveTo(parent, iTween.Hash(
                                          "position", newPosition,
                                          "time", 1.5f
                ));
            iTween.ScaleTo(parent, iTween.Hash(
                              "scale", newScale,
                              //"delay", 1.0f,
                              "time", 1.5f,
                              "oncompletetarget", gameObject,
                              "oncomplete", "OnFitIntoCompleted",
                              "oncompleteparams", parent
                ));
            return scaleFactor;
        }

        /// <summary>
        /// This method will be called by iTween when the animation triggered in FitInto
        /// is completed.
        /// </summary>
        private void OnFitIntoCompleted(GameObject parent)
        {
            Debug.Log("OnFitIntoCompleted\n");
            Debug.LogFormat("Final transform result {0}: [{1} {2}]\n",
                            parent.name,
                            parent.transform.position, parent.transform.localScale);
            animationIsRunning = false;
        }

        private void DumpActiveAscendants()
        {
            // Iteration starts at the top-most element on the stack.
            foreach (ObjectMemento memento in activeAscendants)
            {
                Debug.Log(memento.ToString() + "\n");
            }
        }

        /// ----------------------------------------------------------------------------------------------
        /// Zooming out
        /// ----------------------------------------------------------------------------------------------
        public void ZoomOut()
        {
            // The root will never be popped from the stack, that is why we are using 1
            // instead of 0 in the following condition.
            if (activeAscendants.Count > 1)
            {                                
                animationIsRunning = true;
                // This is the memento of the current focus.
                ObjectMemento memento = activeAscendants.Peek();
                Debug.Assert(memento.Node == focus);
                Debug.LogFormat("Zooming out of subtree at {0}\n", memento.Node.name);
                DumpActiveAscendants();
                // First restore the previous scale and position of focus by some animation.
                Debug.LogFormat("Transforming {0} from [{1} {2}] to [{3} {4}]\n",
                                memento.Node.name,
                                memento.Node.transform.position, memento.Node.transform.localScale,
                                memento.Position, memento.LocalScale);
                iTween.ScaleTo(focus, 
                               iTween.Hash("scale", memento.LocalScale,
                                           "time", 1.5f
                               ));
                iTween.MoveTo(focus, 
                              iTween.Hash("position", memento.Position,
                                          "time", 1.5f,
                                          //"delay", 0.6f,
                                          "oncompletetarget", gameObject,
                                          "oncomplete", "OnZoomOutCompleted"
                               ));                
                // Once the animation is finished, we continue in OnZoomOutCompleted()
                // to show the siblings of focus again.
            }
        }

        /// <summary>
        /// This method will be called by iTween when the animation triggered in ZoomOut
        /// is completed.
        /// </summary>
        private void OnZoomOutCompleted()
        {
            Debug.Log("OnZoomOutCompleted()\n");
            // This is the memento of the current focus.
            ObjectMemento memento = activeAscendants.Pop();
            Debug.Assert(memento.Node == focus);

            Debug.LogFormat("Final transform result {0}: [{1} {2}]\n",
                            memento.Node.name,
                            memento.Node.transform.position, memento.Node.transform.localScale);
            memento.Reset();
            // The current focus node has now its previous scale and position within its ascendant.
            ObjectMemento newFocusMemento = activeAscendants.Peek();
            //newFocusMemento.Reset();
            GameObject newFocus = newFocusMemento.Node;

            // All elements in the subtree rooted by newFocus will be visible next.
            Unhide(Descendants(newFocus));
            focus = newFocus;

            animationIsRunning = false;
        }

        private static void Show(GameObject go, bool show)
        {
            // Note: We disable the renderer rather than making the object
            // inactive, because we may want to show its descendants
            // and a descendant is inactive if one of its ascendants is
            // inactive.
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = show;
            }
        }

        private void Hide(HashSet<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                Show(go, false);
                // There is a FadeTo animation in iTween, but it does not
                // work for objects drawn by a LineRenderer
                //if (go.GetComponent<LineRenderer>() == null)
                //{
                //    iTween.FadeTo(go, iTween.Hash(
                //      "alpha", 0,
                //      "time", 1.5f
                //    //"oncompletetarget", gameObject,
                //    //"oncomplete", "OnFitIntoCompleted"
                //    ));
                //}
            }
        }

        private void Unhide(HashSet<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                Show(go, true);
            }
        }

        private void HideAll()
        {
            foreach (ObjectMemento memento in initialTransforms.Values)
            {
                Show(memento.Node, false);
            }
        }

        ///--------------------------------------------------------------------------------------------------
        /// To be removed
        ///--------------------------------------------------------------------------------------------------
        ///
        private void Update()
        {
            if (!animationIsRunning)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    ResetAll();
                }
                if (Input.GetKeyDown(KeyCode.H))
                {
                    HideAll();
                }
                if (Input.GetKeyDown(KeyCode.I))
                {
                    GameObject child = RandomChild(focus);
                    if (child != null)
                    {
                        ZoomIn(child);
                    }
                }
                if (Input.GetKeyDown(KeyCode.O))
                {
                    ZoomOut();
                }
            }
        }

        private GameObject RandomChild(GameObject parent)
        {
            GameObject selectedChild = null;
            int maxLevel = 0;

            // always select the child with the greatest depth
            foreach (Transform child in parent.transform)
            {
                int level = GetDepth(child.gameObject);
                if (level > maxLevel)
                {
                    maxLevel = level;
                    selectedChild = child.gameObject;
                }
            }
            return selectedChild;
        }
    }
}
