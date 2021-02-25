using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Contains a utility for the traversion of game-objects, more explicit the graphical representation of the graph.
    /// </summary>
    public static class GameObjectTraversion
    {
        /// <summary>
        /// Traverses the graphical representation of the nodes of the graph from the <paramref name="parent"/> parent to the last leaf.
        /// </summary>
        /// <param name="allChildrenOfParent">the list which has to be containing all children of parent</param>
        /// <param name="parent">the parent from where the graph has to be traversed</param>
        /// <returns>All gameObjects in the hierachy from the <paramref name="parent"/> to the last leaf</returns>
        public static List<GameObject> GetAllChildNodesAsGameObject(List<GameObject> allChildrenOfParent, GameObject parent)
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
                foreach (GameObject childs in childrenOfThisParent)
                {
                    GetAllChildNodesAsGameObject(allChildrenOfParent, childs);
                }

                return allChildrenOfParent;
            }
        }
    }
}
