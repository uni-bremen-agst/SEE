using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectTraversion
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="allChildrenOfParent"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
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
                GetAllChildNodesAsGameObject(allChildrenOfParent,childs);
            }

            return allChildrenOfParent;
        }
    }

}
