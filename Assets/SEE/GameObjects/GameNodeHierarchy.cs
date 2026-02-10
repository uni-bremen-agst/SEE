using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Provides extensions for game objects representing game nodes regarding
    /// their parentship.
    /// </summary>
    internal static class GameNodeHierarchy
    {
        /// <summary>
        /// Returns all direct children of <paramref name="oldParent"/> for which the given <paramref name="predicate"/>
        /// holds true. All these children will have <paramref name="newParent"/> as their new parent.
        /// </summary>
        /// <param name="oldParent">The old parent whose children are to be re-parented and returned.</param>
        /// <param name="newParent">The new parent of all children returned.</param>
        /// <param name="predicate">Whether to consider a child.</param>
        /// <returns>All direct children re-parented.</returns>
        public static IList<Transform> ReparentChildren(this Transform oldParent, Transform newParent, Func<Transform, bool> predicate)
        {
            IList<Transform> children = new List<Transform>(oldParent.childCount);
            foreach (Transform child in oldParent)
            {
                if (predicate(child))
                {
                    children.Add(child);
                }
            }
            newParent.SetChildren(children);
            return children;
        }

        /// <summary>
        /// Adds all <paramref name="children"/> to their <paramref name="newParent"/>.
        /// </summary>
        /// <param name="newParent">The new parent of the <paramref name="children"/>.</param>
        /// <param name="children">The children to be assigned to <paramref name="newParent"/>.</param>
        public static void SetChildren(this Transform newParent, IList<Transform> children)
        {
            foreach (Transform child in children)
            {
                child.SetParent(newParent);
            }
        }
    }
}
