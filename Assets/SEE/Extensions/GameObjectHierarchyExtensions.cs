using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Extensions
{
    /// <summary>
    /// Extensions methods dealing with the hierarchy of general <see cref="UnityEngine.GameObject"/>s.
    /// </summary>
    internal static class GameObjectHierarchyExtensions
    {
        /// <summary>
        /// Searches for the first descendant <see cref="GameObject"/> with the specified <paramref name="descendantName"/>
        /// within the hierarchy of the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The game object whose descendants will be searched.</param>
        /// <param name="descendantName">The name of the descendant to search for.</param>
        /// <param name="includeInactive">If set to true, the search wil include inactive <see cref="GameObject"/>s.
        /// Otherwise, only active ones will be considered.</param>
        /// <returns>The first matching descendant <see cref="GameObject"/> with the specified <paramref name="descendantName"/>,
        /// or null if none is found.</returns>
        public static GameObject FindDescendant(this GameObject gameObject, string descendantName, bool includeInactive = true)
        {
            return gameObject
                    .GetComponentsInChildren<Transform>(includeInactive)
                    .FirstOrDefault(t => t.gameObject.name == descendantName)?
                    .gameObject;
        }

        /// <summary>
        /// Searches for the first descendant <see cref="GameObject"/> with the specified <paramref name="tag"/>
        /// within the hierarchy of the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The game object whose descendants will be searched.</param>
        /// <param name="tag">The tag to search for.</param>
        /// <param name="includeInactive">If set to true, the search will include inactive <see cref="GameObject"/>s.
        /// Otherwise, only active ones will be considered.</param>
        /// <returns>The first matching descendant <see cref="GameObject"/> with the specified tag, or null if none is found.</returns>
        public static GameObject FindDescendantWithTag(this GameObject gameObject, string tag, bool includeInactive = true)
        {
            return gameObject
                .GetComponentsInChildren<Transform>(includeInactive)
                .FirstOrDefault(t => t.gameObject.CompareTag(tag))?
                .gameObject;
        }

        /// <summary>
        /// QDetermines whether the <paramref name="gameObject"/> has any descendant
        /// with the specified <paramref name="tag"/>.
        /// </summary>
        /// <param name="gameObject">The root <see cref="GameObject"/> to search from.</param>
        /// <param name="tag">The tag to search for.</param>
        /// <returns>True if a descendant with the specified tag is found; otherwise, false.</returns>
        public static bool HasDescendantWithTag(this GameObject gameObject, string tag)
        {
            return gameObject.FindDescendantWithTag(tag) != null;
        }

        /// <summary>
        /// Finds all descendant <see cref="GameObject"/>s of the given <paramref name="gameObject"/>
        /// that have the specified tag.
        /// </summary>
        /// <param name="gameObject">The root <see cref="GameObject"/> to start the search from.</param>
        /// <param name="tag">The tag that matching descendants must have.</param>
        /// <param name="includeInactive">Whether to include inactive <see cref="GameObject"/>s in the search.</param>
        /// <returns>A list of all descendant <see cref="GameObject"/>s with the specified tag.</returns>
        public static IList<GameObject> FindAllDescendantsWithTag(this GameObject gameObject, string tag, bool includeInactive = true)
        {
            return gameObject
                .GetComponentsInChildren<Transform>(includeInactive)
                .Where(t => t.CompareTag(tag))
                .Select(t => t.gameObject)
                .ToList();
        }
        /// <summary>
        /// Finds all descendant <see cref="GameObject"/>s with the specified <paramref name="descendantTag"/>,
        /// exluding those whose immediate parent has the specified <paramref name="immediateParentTag"/>.
        /// </summary>
        /// <param name="gameObject">The root <see cref="GameObject"/> to search from.</param>
        /// <param name="descendantTag">The tag that matching descendants must have.</param>
        /// <param name="immediateParentTag">If the immediate parent has this tag, the child will be excluded from the result.</param>
        /// <param name="includeInactive">Whether inactive <see cref="GameObject"/>s should be included in the search.</param>
        /// <returns>A list of matching descendant <see cref="GameObject"/>s, excluding those whose parent has the specified tag.</returns>
        public static List<GameObject> FindAllDescendantsWithTagExcludingSpecificParentTag(this GameObject gameObject,
            string descendantTag, string immediateParentTag, bool includeInactive = true)
        {
            return gameObject
                .GetComponentsInChildren<Transform>(includeInactive)
                .Where(t => t.CompareTag(descendantTag) &&
                            t.parent != null &&
                            !t.parent.CompareTag(immediateParentTag))
                .Select(t => t.gameObject)
                .ToList();
        }

        /// <summary>
        /// Determines whether the specified <paramref name="gameObject"/> has any ancestor
        /// with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="gameObject">The starting <see cref="GameObject"/> whose parent hierarhcy will be searched.</param>
        /// <param name="tag">The tag to search for.</param>
        /// <returns>True if a parent or ancestor with the specified tag is found; otherwise, false.</returns>
        public static bool HasParentWithTag(this GameObject gameObject, string tag)
        {
            Transform transform = gameObject.transform;
            while (transform.parent != null)
            {
                if (transform.parent.gameObject.CompareTag(tag))
                {
                    return true;
                }
                transform = transform.parent;
            }
            return false;
        }

        /// <summary>
        /// Searches upward through the transform hierarchy to find the first parent GameObject
        /// with the specified name.
        /// </summary>
        /// <param name="gameObject">The starting GameObject from which the search begins.</param>
        /// <param name="name">The exact name of the parent GameObject to look for.</param>
        /// <returns>
        /// The first matching parent GameObject, or null if no parent with the given name is found.
        /// </returns>
        public static GameObject FindParentWithName(this GameObject gameObject, string name)
        {
            if (gameObject.transform.parent == null)
            {
                return null;
            }
            else
            {
                return gameObject.transform.parent.name == name ?
                          gameObject.transform.parent.gameObject
                        : FindParentWithName(gameObject.transform.parent.gameObject, name);
            }
        }

        /// <summary>
        /// Checks recursively whether the specified GameObject has any parent
        /// with the given layer.
        /// </summary>
        /// <param name="gameObject">The starting GameObject from which the search begins.</param>
        /// <param name="layer">The layer number to check against.</param>
        /// <returns>
        /// True if any parent GameObject has the specified layer;
        /// otherwise, false.
        /// </returns>
        public static bool HasParentWithLayer(this GameObject gameObject, uint layer)
        {
            if (gameObject.transform.parent == null)
            {
                return false;
            }
            else
            {
                return gameObject.transform.parent.gameObject.layer == layer
                       || HasParentWithLayer(gameObject.transform.parent.gameObject, layer);
            }
        }

        /// <summary>
        /// Traverses up the hierachy from the given <paramref name="gameObject"/>
        /// and returns the highest parent.
        /// </summary>
        /// <param name="gameObject">The starting <see cref="GameObject"/> in the hierarchy.</param>
        /// <returns>The root <see cref="GameObject"/> at the top of the hierarchy.
        /// If the given object has no parent, it is returned itself.</returns>
        public static GameObject GetRootParent(this GameObject gameObject)
        {
            Transform parent = gameObject.transform.parent;
            return parent != null ? GetRootParent(parent.gameObject) : gameObject;
        }
    }
}
