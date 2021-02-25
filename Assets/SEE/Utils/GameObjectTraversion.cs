using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectTraversion
{

    public static List<GameObject> GetAllChildNodesAsGameObject(List<GameObject> childrenOfParent, GameObject parent)
    {
        List<GameObject> childrenOfThisParent = new List<GameObject>();
        if (!childrenOfParent.Contains(parent))
        {
            childrenOfParent.Add(parent);
        }
        int gameNodeCount = childrenOfParent.Count;

        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.CompareTag(Tags.Node))
            {
                if (!childrenOfParent.Contains(child.gameObject))
                {
                    childrenOfParent.Add(child.gameObject);
                }
                childrenOfThisParent.Add(child.gameObject);
            }
        }

        if (childrenOfParent.Count == gameNodeCount)
        {
            return childrenOfParent;
        }
        else
        {
            foreach (GameObject childs in childrenOfThisParent)
            {
                GetAllChildNodesAsGameObject(childrenOfParent,childs);
            }
            return childrenOfParent;
        }
    }

}
