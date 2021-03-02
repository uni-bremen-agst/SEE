using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Utility class for the traversion of GameObjects, more explicitly the graphical representation of the graph.
    /// </summary>
    public static class GameObjectTraversion
    {
        /// <summary>
        /// Traverses the GameObjects of the graph from the <paramref name="parent"/> to the last leaf and adds them to <see cref="allChildrenOfParent"/>.
        /// </summary>
        /// <param name="allChildrenOfParent">the list which has to contain all children of  the <paramref name="parent"/></param>
        /// <param name="parent">the parent from which the graph has to be traversed</param>
        /// <returns>All gameObjects in the hierachy from the <paramref name="parent"/> to the last leaf</returns>
        public static List<GameObject> GetAllChildNodes(List<GameObject> allChildrenOfParent, GameObject parent)
        {
            List<GameObject> childrenOfThisParent = new List<GameObject>();

            if (!allChildrenOfParent.Contains(parent))
            {
                allChildrenOfParent.Add(parent);
            }

            int numberOfAllGamenodes = allChildrenOfParent.Count;

            foreach (Transform child in parent.transform)
            {
                if (child.gameObject.CompareTag(Tags.Node))
                {
                    if (!allChildrenOfParent.Contains(child.gameObject))
                    {
                        allChildrenOfParent.Add(child.gameObject);
                    }
                    childrenOfThisParent.Add(child.gameObject);
                }
            }

            if (allChildrenOfParent.Count == numberOfAllGamenodes)
            {
                return allChildrenOfParent;
            }
            else
            {
                foreach (GameObject children in childrenOfThisParent)
                {
                    GetAllChildNodes(allChildrenOfParent, children);
                }

                return allChildrenOfParent;
            }
        }
    }
}
