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
                this.localPosition = go.transform.localPosition;
                this.localScale = go.transform.localScale;

            }
            private GameObject go;
            private Vector3 localPosition;
            private Vector3 localScale;
            public void Reset()
            {
                go.transform.localPosition = localPosition;
                go.transform.localScale = localScale;

            }
            public GameObject Node
            {
                get => go;
            }
        }

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
                ObjectMemento initial = initialTransforms[go.ID()];
                initial.Reset();
            }
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
                Show(current, true);
                descendants.Add(current);
                foreach (Transform child in current.transform)
                {
                    toBeVisited.Push(child.gameObject);
                }
            }
            return descendants;
        }

        /// ----------------------------------------------------------------------------------------------
        /// Zooming in and out
        /// ----------------------------------------------------------------------------------------------
        /// 
        public void ZoomIn(GameObject parent)
        {
            Debug.LogFormat("Zooming into subtree at {0}\n", parent.name);

            // Currently, focus and all its descendants are visible.
            HashSet<GameObject> currentlyVisible = Descendants(focus);
            // All elements in the subtree rooted by parent will be visible next.
            HashSet<GameObject> newlyVisible = Descendants(parent);
            // All currently visible elements that need to be hidden.
            currentlyVisible.ExceptWith(newlyVisible);
            Hide(currentlyVisible);
            focus = parent;
            FitInto(parent, newlyVisible);
            // Invariant: all nodes except for newlyVisible have their original
            // position, size, and rotation.
        }

        public void ZoomOut()
        {
            if (focus != null)
            {
                Transform parent = focus.transform.parent;
                if (parent != null)
                {
                    GameObject newFocus = parent.gameObject;
                    if (newFocus.tag == Tags.Node)
                    {
                        // Currently, focus and all its descendants are visible.
                        HashSet<GameObject> currentlyVisible = Descendants(focus);

                        // TODO: Shrink and move currentlyVisible to their previous location within parent

                        Reset(currentlyVisible);
                        // All elements in the subtree rooted by parent will be visible next.
                        // This set includes currentlyVisible.
                        HashSet<GameObject> newlyVisible = Descendants(newFocus);
                        // Assert: all newlyVisibles have their original position, size, and scale. 
                        focus = newFocus;
                        FitInto(newFocus, newlyVisible);
                    }
                }
            }
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

        private void HideAll()
        {
            foreach (ObjectMemento memento in initialTransforms.Values)
            {
                Show(memento.Node, false);
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
            float requestedWidth = initialRightUpperCorner.x - initalLeftLowerCorner.x;
            // We always start with the original positions, rotations, and scales
            //Reset(descendants);

            BoundingBox.Get(descendants, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner);

            float currentWidth = rightUpperCorner.x - leftLowerCorner.x;
            float scaleFactor = requestedWidth / currentWidth;            
            // We maintain parent's y co-ordinate. We move it only within the x/z plane.
            Vector3 position = parent.transform.position;
            Vector2 center = CenterPoint;
            position.x = center.x;
            position.z = center.y;

            Debug.LogFormat("Moving {0} from {1} to {2}\n", parent.name, parent.transform.position, position);
            // Adjust position and scale by some animation.
            animationIsRunning = true;
            iTween.MoveTo(parent, iTween.Hash(
                                          "position", position,
                                          "time", 1.0f
                ));
            iTween.ScaleTo(parent, iTween.Hash(
                              "scale", parent.transform.localScale * scaleFactor,
                              "delay", 1.0f,
                              "time", 1.5f,
                              "oncompletetarget", gameObject,
                              "oncomplete", "OnFitIntoCompleted"
                ));
            return scaleFactor;
        }

        /// <summary>
        /// This method will be called by iTween when the animation triggered in FitInto
        /// is completed.
        /// </summary>
        private void OnFitIntoCompleted()
        {
            Debug.Log("OnFitIntoCompleted\n");
            animationIsRunning = false;
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
