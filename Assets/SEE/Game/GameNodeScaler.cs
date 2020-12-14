using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// This GameNodeScaler allows us to scale a game node without modifying the 
    /// scale of its children.
    /// </summary>
    [ExecuteInEditMode]
    public class GameNodeScaler : MonoBehaviour
    {
        /// <summary>
        /// The target scale of the game object in world space co-ordinates.
        /// </summary>
        public Vector3 TargetScale;

        /// <summary>
        /// Sets <see cref="TargetScale"/> to the current scale of the game object.
        /// </summary>
        void Start()
        {
            Init();
        }

        /// <summary>
        /// Sets <see cref="TargetScale"/> to the current scale of the game object.
        /// Called when the component is reset in the editor.
        /// </summary>
        void Reset()
        {
            Init();
        }

        private void Init()
        {
            TargetScale = transform.lossyScale;
            enabled = false;
        }

        /// <summary>
        /// If <see cref="TargetScale"/> has changed, the scale of the game object is set
        /// to <see cref="TargetScale"/> (in world space). The children of the game object
        /// are not re-sized.
        /// </summary>
        void Update()
        {
            if (TargetScale != transform.lossyScale)
            {
                Debug.LogFormat("{0} original world scale = {1}; target world scale: {2}\n",
                    name, transform.lossyScale.ToString("F4"), TargetScale.ToString("F4"));
                ScaleOnlyRootGameObject(gameObject, TargetScale);

#if UNITY_EDITOR
                EditorUtility.SetDirty(gameObject);
                SceneView.RepaintAll();
#endif
                TargetScale = transform.lossyScale;
            }
        }

        /// <summary>
        /// Resizes given <paramref name="gameObject"/> to given <paramref name="targetScale"/>
        /// in world space without resizing any of its children.
        /// </summary>
        /// <param name="gameObject">game object to be resized</param>
        /// <param name="targetScale">the target scale in world-space co-ordinates</param>
        private static void ScaleOnlyRootGameObject(GameObject gameObject, Vector3 targetScale)
        {
            // The children of gameObject:
            List<Transform> children = new List<Transform>();
            // Save the children of gameObject.
            foreach (Transform child in gameObject.transform)
            {
                children.Add(child);
            }
            // Unparent all children of gameObject so that they do not scale along with gameObject.
            foreach (Transform child in children)
            {
                child.parent = null;
            }
            {
                // Resize gameObject and only gameObject in world space.
                // The parent of gameObject (may be null):
                Transform parent = gameObject.transform.parent;
                // Unparent gameObject because targetScale is meant to be in world space.
                gameObject.transform.parent = null;
                // Resize gameObject.
                gameObject.transform.localScale = targetScale;
                // Re-parent gameObject.
                gameObject.transform.parent = parent;
            }
            // Re-parent all children of gameObject.
            foreach (Transform child in children)
            {
                child.parent = gameObject.transform;
            }
        }
    }
}