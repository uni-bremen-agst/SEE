using UnityEngine;
using System.Collections.Generic;

using SEE.Utils;
using SEE.DataModel;
using SEE.GO;

namespace SEE.Game
{
    /// <summary>
    /// Manages zooming for composite nodes tagged by Tags.Node.
    /// </summary>
    public class Transformer
    {
        /// <summary>
        /// Name of the method of the caller of ZoomInto or ZoomRoot to be
        /// called when those function's animations have finished. This
        /// method will be called via GameObject.SendMessage(). As a
        /// consequence, it will be received only by all MonoBehaviours
        /// of the caller game object.
        /// </summary>
        private const string OnZoomingComplete = "OnZoomingComplete";

        /// <summary>
        /// Name of the method of the caller of ZoomOutOf to be
        /// called when ZoomOutOf's animations have finished. This
        /// method will be called via GameObject.SendMessage(). As a
        /// consequence, it will be received only by all MonoBehaviours
        /// of the caller game object. The signature of this method
        /// is assumed to be as follows:
        /// 
        ///  void OnZoomingOutComplete(Transformer transfomer)
        /// 
        /// When the caller receives this notification, it is expected
        /// to call transfomer.FinalizeZoomOut() to finalize resetting
        /// the positions of all objects previously hidden.
        /// </summary>
        private const string OnZoomingOutComplete = "OnZoomingOutComplete";

        /// <summary>
        /// Checks, whether it is possible to zoom out of the given
        /// <paramref name="enteredNode"/>.
        /// 
        /// See <see cref="ZoomOutOf(GameObject, GameObject)"/> for further details.
        /// </summary>
        /// <param name="enteredNode">the object to be zoomed out of</param>
        /// <returns><code>true</code>, if zooming is possible, <code>false</code> otherwise</returns>
        public static bool CanZoomOutOf(GameObject enteredNode)
        {
            Transformer transformer = GetTransformer(enteredNode);
            return transformer != null && transformer.activeAscendants.Count > 1;
        }

        /// <summary>
        /// Checks, whether it is possible to zoom back to the root from given
        /// <paramref name="enteredNode"/>.
        /// </summary>
        /// <param name="enteredNode">the object to be zoomed out of</param>
        /// <returns><code>true</code>, if zooming is possible, <code>false</code> otherwise</returns>
        public static bool CanZoomRoot(GameObject enteredNode)
        {
            return GetTransformer(enteredNode) != null;
        }

        /// <summary>
        /// Checks, whether it is possible to zoom into the given
        /// <paramref name="enteredNode"/>.
        /// </summary>
        /// <param name="enteredNode">the object to be zoomed into</param>
        /// <returns><code>true</code>, if zooming is possible, <code>false</code> otherwise</returns>
        public static bool CanZoomInto(GameObject enteredNode)
        {
            Transformer transformer = GetTransformer(enteredNode);
            return transformer != null
                && enteredNode != null
                && enteredNode.tag == Tags.Node
                && transformer.activeAscendants.Peek().Node != enteredNode;
        }

        /// <summary>
        /// Zooms into the given <paramref name="gameObject"/>, that is, all ascendants
        /// and their respective descendants are hidden and <paramref name="gameObject"/>
        /// and its descendants are scaled up and relocated so that they occupy the
        /// complete space originally available for the whole game-object tree when 
        /// any of the game objects of that game-object tree was zoomed into.
        /// </summary>
        /// <param name="caller">the caller of this method to be notified when
        /// the zooming is finished; the parameter-less message "OnZoomingComplete" 
        /// will be used for the notification</param>
        /// <param name="gameObject">the object to be zoomed into</param>
        public static void ZoomInto(GameObject caller, GameObject gameObject)
        {
            Transformer transformer = GetTransformer(gameObject);
            if (transformer != null)
            {
                transformer.ZoomIn(caller, gameObject);
            }
            else
            {
                caller.SendMessage(OnZoomingComplete);
            }
        }

        /// <summary>
        /// Restores the top-level view of the game-object hierarchy the given <paramref name="gameObject"/>
        /// belongs to.
        /// Note: <paramref name="gameObject"/> is used only to determine the node hierarchy
        /// where we want to zoom out. It is not necessarily the root of this hierarchy.
        /// </summary>
        /// <param name="caller">the caller of this method to be notified when
        /// the zooming is finished; the parameter-less message "OnZoomingComplete" 
        /// will be used for the notification</param>
        /// <param name="gameObject">the node used to identify the game-object hierarchy
        /// in which we are zooming</param>
        public static void ZoomRoot(GameObject caller, GameObject gameObject)
        {
            Transformer transformer = GetTransformer(gameObject);
            if (transformer != null)
            {
                transformer.ZoomToTopLevel(caller);
            }
            else
            {
                caller.SendMessage(OnZoomingComplete);
            }
        }

        /// <summary>
        /// Zooms out of the node that was last zoomed in, that is, for which ZoomInto()
        /// was called last in the game-object hierarchy the given <paramref name="gameObject"/>
        /// is contained in. 
        /// Note: <paramref name="gameObject"/> is used only to determine the node hierarchy
        /// where we want to zoom out. It is not necessarily the node being zoomed out.
        /// 
        /// Let N be the node we want to zoom out. Zooming out of N means to restore the 
        /// scales and positions of all nodes in the game-object hierarchy that were visible 
        /// just before we zoomed into N.
        /// 
        /// The caller will receive a OnZoomingOutComplete(Transformer) notification when all 
        /// animations have finished. The signature of this method is assumed to be as follows:
        /// 
        ///  void OnZoomingOutComplete(Transformer transfomer)
        /// 
        /// When the caller receives this notification, it is expected
        /// to call transfomer.FinalizeZoomOut() to finalize resetting
        /// the positions of all objects previously hidden.
        /// </summary>
        /// <param name="caller">the caller of this method to be notified when
        /// the zooming is finished; the parameter-less message "OnZoomingOutComplete" 
        /// will be used for the notification</param>
        /// <param name="gameObject">the node used to identify the game-object hierarchy
        /// in which we are zooming</param>
        public static void ZoomOutOf(GameObject caller, GameObject gameObject)
        {
            Transformer transformer = GetTransformer(gameObject);
            if (transformer != null)
            {
                transformer.ZoomOut(caller);
            }
            else
            {
                caller.SendMessage(OnZoomingComplete);
            }
        }

        /// <summary>
        /// This method will be called by iTween when the animation triggered in ZoomOut()
        /// is completed.
        /// </summary>
        /// <param name="caller">the original caller of the zooming request to be notified when
        /// the zooming is finished; the parameter-less message "OnZoomingComplete" 
        /// will be used for the notification</param>        
        public void FinalizeZoomOut()
        {
            // This is the memento of the current focus.
            ObjectMemento memento = activeAscendants.Pop();
            memento.Reset();
            // The current focus node has now its previous scale and position within its ascendant.
            ObjectMemento newFocusMemento = activeAscendants.Peek();
            GameObject newFocus = newFocusMemento.Node;

            // All elements in the subtree rooted by newFocus will be visible next
            // (no matter how they are tagged).
            Unhide(GameObjectHierarchy.Descendants(newFocus));
            focus = newFocus;
        }
        
        /// <summary>
        /// Sets the initial state. This can only be called, if no zooming is currently
        /// active!
        /// </summary>
        /// <param name="gameObjects">The GameObjects to be zoomed into</param>
        public static void SetInitialState(GameObject[] gameObjects)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                Transformer transformer = GetTransformer(gameObject);
                if (transformer != null)
                {
                    transformer.activeAscendants.Push(new ObjectMemento(gameObject));
                    HashSet<GameObject> currentlyVisible = GameObjectHierarchy.Descendants(transformer.focus);
                    HashSet<GameObject> newlyVisible = GameObjectHierarchy.Descendants(gameObject);
                    currentlyVisible.ExceptWith(newlyVisible);
                    transformer.Hide(currentlyVisible);
                    transformer.focus = gameObject;

                    BoundingBox.Get(
                        GameObjectHierarchy.Descendants(gameObject, Tags.Node),
                        out Vector2 leftLowerCorner, out Vector2 rightUpperCorner
                    );
                    float scaleFactor = Mathf.Min(
                        (transformer.initialRightUpperCorner.x - transformer.initialLeftLowerCorner.x) / (rightUpperCorner.x - leftLowerCorner.x),
                        (transformer.initialRightUpperCorner.y - transformer.initialLeftLowerCorner.y) / (rightUpperCorner.y - leftLowerCorner.y)
                    );
                    Vector3 newPosition = gameObject.transform.position;
                    Vector2 center = transformer.CenterPoint;
                    newPosition.x = center.x;
                    newPosition.z = center.y;
                    Vector3 newScale = gameObject.transform.localScale * scaleFactor;
                    gameObject.transform.position = newPosition;
                    gameObject.transform.localScale = newScale;
                }
            }
        }

        /// <summary>
        /// Returns a Transformer instance responsible for the given <paramref name="gameObject"/>.
        /// If no such Transformer instance has existed yet, it will be created, added to 
        /// responsibleTransformer, and returned. May return null if <paramref name="gameObject"/>
        /// and none of its ascendants is tagged by Tags.Node.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>responsible transformer or null</returns>
        private static Transformer GetTransformer(GameObject gameObject)
        {
            GameObject root = Root(gameObject);
            if (root != null)
            {
                if (!responsibleTransformer.TryGetValue(root, out Transformer transformer))
                {
                    // If we do not yet have a responsible transformer, we will create
                    // one on demand.
                    transformer = new Transformer(root);
                    responsibleTransformer[root] = transformer;
                }
                return transformer;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// A mapping of root nodes tagged by Tags.Node onto the Transformer
        /// instance handling its zooming. Because we could have multiple composite
        /// objects we want to zoom in and out, we store the Transformer
        /// instances responsible for those here in this mapping.
        /// </summary>
        private static Dictionary<GameObject, Transformer> responsibleTransformer 
            = new Dictionary<GameObject, Transformer>();

        /// <summary>
        /// Searches in all ascendants of given <paramref name="gameObject"/> for the 
        /// farthest game object that is tagged by Tags.Node. If no such node
        /// exists, null is returned.
        /// </summary>
        private static GameObject Root(GameObject gameObject)
        {
            GameObject result = gameObject;
            while (result.transform.parent != null 
                   && result.transform.parent.gameObject.CompareTag(Tags.Node))
            {
                result = result.transform.parent.gameObject;
            }
            return result;
        }

        /// <summary>
        /// The game object in the game-object hierarchy that is currently 
        /// shown along with it descendants.
        /// </summary>
        private GameObject focus;

        /// ----------------------------------------------------------------------------------------------
        /// Start up
        /// ----------------------------------------------------------------------------------------------
        private Transformer(GameObject root)
        {
            focus = root;
            activeAscendants.Push(new ObjectMemento(focus));
            // We store the states of only those descendants that are tagged by Tags.Node
            // because only their absolute position and scale will be changed. There may be
            // other kinds of descendants such as erosion indicators or labels but since
            // their scale and position is relative to a game object tagged by Tags.Node
            // they will adjust along with their parent. 
            ICollection<GameObject> descendants = GameObjectHierarchy.Descendants(focus, Tags.Node);
            initialTransforms = GetMementos(descendants);
            // Similarly, only the descands tagged by Tags.Node are relevant for calculating
            // the bounding box. We assume that all other kinds of game objects in this game-object
            // hierarchy are visualized above the nodes. The bounding box is the area in the x/z plane.
            BoundingBox.Get(descendants, out initialLeftLowerCorner, out initialRightUpperCorner);
        }

        /// ----------------------------------------------------------------------------------------------
        /// Initial state
        /// ----------------------------------------------------------------------------------------------
        /// 
        /// <summary>
        /// The left lower corner of the initial bounding box of the gameObject in world space (x/z plane).
        /// </summary>
        private readonly Vector2 initialLeftLowerCorner;
        /// <summary>
        /// The right upper corner of the initial bounding box of the gameObject in world space (x/z plane).
        /// </summary>
        private readonly Vector2 initialRightUpperCorner;
        
        /// <summary>
        /// The center point of the rectangle defined by initalLeftLowerCorner and initialRightUpperCorner 
        /// in world space.
        /// </summary>
        private Vector2 CenterPoint
        {
            get
            {
                float width = initialRightUpperCorner.x - initialLeftLowerCorner.x;
                float depth = initialRightUpperCorner.y - initialLeftLowerCorner.y;
                return new Vector2(initialLeftLowerCorner.x + width / 2.0f, initialLeftLowerCorner.y + depth / 2.0f);
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

        /// <summary>
        /// All descendants of gameObject tagged by Tags.Node.
        /// </summary>

        private Dictionary<string, ObjectMemento> initialTransforms;

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
        /// Hierarchy
        /// ----------------------------------------------------------------------------------------------

        private static int GetDepth(GameObject parent)
        {
            int maxLevel = 0;
            foreach (Transform child in parent.transform)
            {
                if (child.gameObject.tag == Tags.Node)
                {
                    int level = GetDepth(child.gameObject);
                    if (level > maxLevel)
                    {
                        maxLevel = level;
                    }
                }
            }
            return maxLevel + 1;
        }

        /// ----------------------------------------------------------------------------------------------
        /// Zooming home to the top level.
        /// ----------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Resets all game objects to their original state and shows them.
        /// That means, we are showing the top level again.
        /// </summary>
        private void ZoomToTopLevel(GameObject caller)
        {
            foreach (ObjectMemento memento in initialTransforms.Values)
            {
                memento.Reset();
                Show(memento.Node, true);
            }
            // Remove all elements but the deepest one (the original root) from the
            // stack of the active ascendants. We are at top level again.
            while (activeAscendants.Count > 1)
            {
                activeAscendants.Pop();
            }
            focus = activeAscendants.Peek().Node;
            caller?.SendMessage(OnZoomingComplete);
        }

        /// ----------------------------------------------------------------------------------------------
        /// Zooming in
        /// ----------------------------------------------------------------------------------------------
        private void ZoomIn(GameObject caller, GameObject enteredNode)
        {
            if (enteredNode != null
                && enteredNode.tag == Tags.Node
                && activeAscendants.Peek().Node != enteredNode)
            {                
                // When zooming into enteredNode, we need to save the current state
                // of enteredNode only. When this node is entered, the siblings of
                // enteredNode and its descendant become hidden, so their positions
                // cannot be changed. If any of the descendants of enteredNode 
                // are changed, then only their positions relative to enteredNode
                // will be changed. If we restore only the state of the enteredNode
                // when zooming out, then the user's changes will be maintained.

                // Save temporary scale and position of the node to be entered
                // so that we can later restore it when zooming out. This must
                // be done before we fit it into the visible area, that is: here.
                activeAscendants.Push(new ObjectMemento(enteredNode));
                // DumpActiveAscendants();
                // Currently, focus and all its descendants are visible.
                HashSet<GameObject> currentlyVisible = GameObjectHierarchy.Descendants(focus);
                // All elements in the subtree rooted by enteredNode will be visible next.
                HashSet<GameObject> newlyVisible = GameObjectHierarchy.Descendants(enteredNode);
                // All currently visible elements that need to be hidden.
                currentlyVisible.ExceptWith(newlyVisible);
                Hide(currentlyVisible);
                focus = enteredNode;
                FitInto(caller, enteredNode);
            }
        }

        /// <summary>
        /// Scales <paramref name="parent"/> so that the total width of the size
        /// requested for its descendants fits into initial rectangle.
        /// The aspect ratio of every node is maintained.
        /// </summary>
        /// <param name="caller">the original caller of the zooming request to be notified when
        /// the zooming is finished; the parameter-less message "OnZoomingComplete" 
        /// will be used for the notification</param>        
        /// <param name="parent">the parent of all <paramref name="descendants"/></param>
        /// <returns>the factor by which the scale of edge node was multiplied</returns>
        private float FitInto(GameObject caller, GameObject parent)
        {
            // All elements tagged by Tags.Node in the subtree rooted by parent must fit into the area.
            BoundingBox.Get(GameObjectHierarchy.Descendants(parent, Tags.Node), 
                            out Vector2 leftLowerCorner, out Vector2 rightUpperCorner);

            float scaleFactor = Mathf.Min((initialRightUpperCorner.x - initialLeftLowerCorner.x) / (rightUpperCorner.x - leftLowerCorner.x),
                                          (initialRightUpperCorner.y - initialLeftLowerCorner.y) / (rightUpperCorner.y - leftLowerCorner.y));

            // We maintain parent's y co-ordinate. We move it only within the x/z plane.
            Vector3 newPosition = parent.transform.position;
            Vector2 center = CenterPoint;
            newPosition.x = center.x;
            newPosition.z = center.y;
            Vector3 newScale = parent.transform.localScale * scaleFactor;

            // Adjust position and scale by some animation.
            iTween.MoveTo(
                parent,
                iTween.Hash(
                    "position", newPosition,
                    "time", 1.5f
                )
            );
            iTween.ScaleTo(
                parent,
                iTween.Hash(
                    "scale", newScale,
                    "time", 1.5f,
                    "oncompletetarget", caller,
                    "oncomplete", OnZoomingComplete
                )
            );
            return scaleFactor;
        }

        private void DumpActiveAscendants()
        {
            // Iteration starts at the top-most element on the stack.
            foreach (ObjectMemento memento in activeAscendants)
            {
                Debug.Log("ancestor stack " + memento.ToString() + "\n");
            }
        }

        /// ----------------------------------------------------------------------------------------------
        /// Zooming out
        /// ----------------------------------------------------------------------------------------------
        private void ZoomOut(GameObject caller)
        {
            // The root will never be popped from the stack, that is why we are using 1
            // instead of 0 in the following condition.
            if (activeAscendants.Count > 1)
            {                                
                // This is the memento of the current focus.
                ObjectMemento memento = activeAscendants.Peek();
                //DumpActiveAscendants();
                // First restore the previous scale and position of focus by some animation.
                iTween.ScaleTo(focus, 
                               iTween.Hash("scale", memento.LocalScale,
                                           "time", 1.5f
                               ));
                iTween.MoveTo(focus, 
                              iTween.Hash("position", memento.LocalPosition,
                                          "time", 1.5f,
                                          //"delay", 0.6f,
                                          "oncompletetarget", caller,
                                          "oncomplete", OnZoomingOutComplete,
                                          "oncompleteparams", this
                               ));
                // Once the animation is finished, we continue in OnZoomOutCompleted()
                // to show the siblings of focus again. OnZoomOutCompleted() in turn
                // will notifiy the original caller that everything is then finished.
            }
            else
            {
                caller.SendMessage(OnZoomingComplete);
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

        private void Unhide(HashSet<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                Show(go, true);
            }
        }

        //private void HideAll()
        //{
        //    foreach (ObjectMemento memento in initialTransforms.Values)
        //    {
        //        Show(memento.Node, false);
        //    }
        //}

        ///--------------------------------------------------------------------------------------------------
        /// To be removed
        ///--------------------------------------------------------------------------------------------------
        ///
        //private void Update()
        //{
        //    if (!animationIsRunning)
        //    {
        //        if (Input.GetKeyDown(KeyCode.R))
        //        {
        //            ZoomToTopLevel();
        //        }
        //        if (Input.GetKeyDown(KeyCode.H))
        //        {
        //            HideAll();
        //        }
        //        if (Input.GetKeyDown(KeyCode.I))
        //        {
        //            GameObject child = RandomChild(focus);
        //            if (child != null)
        //            {
        //                ZoomIn(child);
        //            }
        //        }
        //        if (Input.GetKeyDown(KeyCode.O))
        //        {
        //            ZoomOut();
        //        }
        //    }
        //}

        //private GameObject RandomChild(GameObject parent)
        //{
        //    GameObject selectedChild = null;
        //    int maxLevel = 0;

        //    // always select the child with the greatest depth
        //    foreach (Transform child in parent.transform)
        //    {
        //        if (child.gameObject.tag == Tags.Node)
        //        {
        //            int level = GetDepth(child.gameObject);
        //            if (level > maxLevel)
        //            {
        //                maxLevel = level;
        //                selectedChild = child.gameObject;
        //            }
        //        }
        //    }
        //    return selectedChild;
        //}
    }
}
