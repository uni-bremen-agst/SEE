using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentManager : MonoBehaviour
{
    private SEE.DataModel.Graph Graph;

    void Start()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            Graph = rootObjects[i].GetComponent<SEE.DataModel.Graph>();
            if (Graph != null)
                break;
        }
        GameObject listItemPrefab = Resources.Load("Prefabs/ListItem") as GameObject;

        List<GameObject> nodes = Graph.GetNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            SEE.DataModel.Node node = nodes[i].GetComponent<SEE.DataModel.Node>();
            GameObject listItem = GameObject.Instantiate(listItemPrefab, transform);
            listItem.name = listItemPrefab.name + " " + node.name;
            listItem.GetComponentInChildren<ListItem>().SetNode(node);
        }
    }
}
