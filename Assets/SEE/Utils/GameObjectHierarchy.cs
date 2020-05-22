using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    public static class GameObjectHierarchy
    {
        /// <summary>
        /// Returns all descendants of given <paramref name="parent"/> including
        /// <paramref name="parent"/> that are tagged by <paramref name="requestedTag"/>.
        /// unless <paramref name="requestedTag"/> equals Tags.None (i.e., if
        /// <paramref name="requestedTag"/> equals Tags.None, the tag of the object
        /// is not considered).
        /// <param name="parent">the root of the subtree to be returned</param>
        /// <paramref name="requestedTag">the tag a descendant must have to be included;
        /// if Tags.None is given, all tags are acceptable</param>
        /// <returns>all descendants</returns>
        public static HashSet<GameObject> Descendants(GameObject parent, string requestedTag = Tags.None)
        {
            // all descendants of gameObject including parent
            HashSet<GameObject> descendants = new HashSet<GameObject>();

            // collect all descendants (non-recursively)
            Stack<GameObject> toBeVisited = new Stack<GameObject>();
            toBeVisited.Push(parent);
            while (toBeVisited.Count > 0)
            {
                GameObject current = toBeVisited.Pop();
                if (requestedTag == Tags.None || current.CompareTag(requestedTag))
                {
                    descendants.Add(current);
                }
                foreach (Transform child in current.transform)
                {
                    toBeVisited.Push(child.gameObject);
                }
            }
            return descendants;
        }
    }
}
