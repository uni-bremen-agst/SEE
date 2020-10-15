using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// General utilities related to the hierarchy of game objects representing
    /// nodes in code cities.
    /// </summary>
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

        /// <summary>
        /// Returns all roots of <paramref name="gameNodes"/>. A game object is a root
        /// if is has no ascendants or when it has ascendants, none of its ascendants 
        /// is tagged by the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="gameNodes">where to search for roots</param>
        /// <param name="tag">tag for relevant game objects</param>
        /// <returns>all roots in <paramref name="gameNodes"/></returns>
        public static ICollection<GameObject> Roots(ICollection<GameObject> gameNodes, string tag = Tags.Node)
        {
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject gameNode in gameNodes)
            {
                GameObject parent = Parent(gameNode, tag);
                if (ReferenceEquals(parent, null))
                {
                    result.Add(gameNode);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the parent of the given <paramref name="gameNode"/>. A node P is the 
        /// parent of <paramref name="gameNode"/> if it is the closest ascendants of
        /// <paramref name="gameNode"/> in the game-object hierarchy tagged with <paramref name="tag"/>.
        /// </summary>
        /// <param name="gameNode">node whose parent is requested</param>
        /// <param name="tag">the tag the parent must have</param>
        /// <returns></returns>
        public static GameObject Parent(GameObject gameNode, string tag = Tags.Node)
        {
            Transform cursor = gameNode.transform.parent;
            while (cursor != null)
            {
                if (cursor.tag.Equals(tag))
                {
                    return cursor.gameObject;
                }
                // ascendants with wrong tag are skipped; we continue
                // searching at the next ascendant
                cursor = cursor.transform.parent;
            }
            return null;
        }

        /// <summary>
        /// Returns all immediate children of <paramref name="parent"/> that are tagged by
        /// given <paramref name="tag"/>. A node C is an immediate child of <paramref name="parent"/>
        /// if it is a descendant of <paramref name="parent"/> and tagged by <paramref name="tag"/>
        /// and there is no other such descendant on the path from C to <paramref name="parent"/>
        /// in the game-object hierarchy.
        /// </summary>
        /// <param name="parent">parent whose children are requested</param>
        /// <param name="tag">the tag the parent must have</param>
        /// <returns></returns>
        public static ICollection<GameObject> Children(GameObject parent, string tag = Tags.Node)
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Transform child in parent.transform)
            {
                if (child.tag.Equals(tag))
                {
                    // immediate child tagged by tag
                    result.Add(child.gameObject);
                }
                else
                {
                    // immediate child, but not tagged by tag; we will skip it
                    // and continue searching in the children of child.
                    result.AddRange(Children(child.gameObject, tag));
                }
            }
            return result;
        }
    }
}
