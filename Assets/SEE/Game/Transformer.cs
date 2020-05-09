using UnityEngine;
using System.Collections.Generic;

using SEE.Utils;
using System;
using SEE.DataModel;

namespace SEE.Game
{
    /// <summary>
    /// A behaviour component that can be attached to a node representing a
    /// code city in order to zoom into it.
    /// </summary>
    //[RequireComponent(typeof(AbstractSEECity))]
    public class Transformer : MonoBehaviour
    {
        [Tooltip("The center of the visible area")]
        public Vector3 CenterOfVisibleArea = Vector3.zero;

        [Tooltip("The width of the visible area")]
        public float WidthOfVisibleArea = 1.0f;

        [Tooltip("The depth of the visible area")]
        public float DepthOfVisibleArea = 1.0f;

        private GameObject focus;

        /// <summary>
        /// The left lower corner of the initial bounding box of the gameObject in world space (x/z plane).
        /// </summary>
        private Vector2 initalLeftLowerCorner;
        /// <summary>
        /// The right upper corner of the initial bounding box of the gameObject in world space (x/z plane).
        /// </summary>
        private Vector2 initialRightUpperCorner;

        private void Start()
        {
            initialTransforms = new Dictionary<GameObject, Transform>();            
            focus = GetRootNode(gameObject);
            SaveTransforms(focus);
            BoundingBox.Get(initialTransforms.Keys, out initalLeftLowerCorner, out initialRightUpperCorner);
            //ZoomIn(gameObject);
        }

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

        private void Update()
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

        private void SaveTransforms(GameObject parent)
        {
            // if parent is not the Plane
            if (parent.name != "Plane" && parent.tag != Tags.Decoration)
            {
                initialTransforms[parent] = parent.transform;
                foreach (Transform child in parent.transform)
                {
                    SaveTransforms(child.gameObject);
                }
            }
        }

        // All descendants of gameObject. Does include the gameObject itself,
        // but does not include the Plane.
        private Dictionary<GameObject, Transform> initialTransforms;

        public void ResetAll()
        {
            foreach (var entry in initialTransforms)
            {
                entry.Key.transform.localPosition = entry.Value.localPosition;
                entry.Key.transform.localScale = entry.Value.localScale;
                entry.Key.transform.localRotation = entry.Value.localRotation;
                Show(entry.Key, true);

            }
        }

        public void ZoomIn(GameObject parent)
        {
            Debug.LogFormat("Zooming into subtree at {0}\n", parent.name);

            // Currently, focus and all its descendants are visible.
            HashSet<GameObject> currentlyVisible = Descendants(focus);
            // All elements in the subtree rooted by parent will be visible next.
            HashSet<GameObject> newlyVisible = Descendants(parent);
            // All currently visible elements that need to be hidden.
            currentlyVisible.ExceptWith(newlyVisible);
            ResetAndHide(currentlyVisible);
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

        public void ResetAndHide(HashSet<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                Show(go, false);
            }
            Reset(gameObjects);
        }

        public void HideAll()
        {
            foreach (GameObject go in initialTransforms.Keys)
            {
                Show(go, false);
            }
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

        /// <summary>
        /// Adds the given <paramref name="offset"/> to every node in <paramref name="gameObjects"/>.
        /// </summary>
        /// <param name="gameObjects">game objects to be moved</param>
        /// <param name="offset">offset to be added</param>
        private static void Move(ICollection<GameObject> gameObjects, Vector3 offset)
        {
            foreach (GameObject layoutNode in gameObjects)
            {
                layoutNode.transform.position += offset;
            }
        }

        /// <summary>
        /// Scales all nodes in <paramref name="descendants"/> so that the total width
        /// of the layout (along the x axis) equals <paramref name="width"/>.
        /// The aspect ratio of every node is maintained.
        /// </summary>
        /// <param name="descendants">layout nodes to be scaled</param>
        /// <param name="width">the absolute width (x axis) the required space for the laid out nodes must have</param>
        /// <returns>the factor by which the scale of edge node was multiplied</returns>
        private float FitInto(GameObject parent, ICollection<GameObject> descendants)
        {
            float requestedWidth = initialRightUpperCorner.x - initalLeftLowerCorner.x;
            // We always start with the original positions, rotations, and scales
            //Reset(descendants);

            BoundingBox.Get(descendants, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner);

            float currentWidth = rightUpperCorner.x - leftLowerCorner.x;
            float scaleFactor = requestedWidth / currentWidth;

            Debug.LogFormat("requestedWidth {0} currentWidth {1} scaleFactor {2}\n", requestedWidth, currentWidth, scaleFactor);
            parent.transform.localScale *= scaleFactor;
            return scaleFactor;
        }

        private void Reset(ICollection<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                Transform initial = initialTransforms[go];
                go.transform.localPosition = initial.localPosition;
                go.transform.localScale = initial.localScale;
                go.transform.localRotation = initial.localRotation;
            }
        }
    }
}
