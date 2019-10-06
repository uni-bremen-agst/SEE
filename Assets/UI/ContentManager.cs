using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;

public class ContentManager : MonoBehaviour
{
    private Graph Graph;

    void Start()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            Graph = rootObjects[i].GetComponent<Graph>();
            if (Graph != null)
                break;
        }
        GameObject listItemPrefab = Resources.Load("Prefabs/ListItem") as GameObject;

        List<GameObject> nodes = Graph.GetNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            Node node = nodes[i].GetComponent<Node>();
            GameObject listItem = GameObject.Instantiate(listItemPrefab, transform);
            listItem.name = listItemPrefab.name + " " + node.name;
            listItem.GetComponentInChildren<ListItem>().SetNode(node);
        }
    }

    public void Filter(string filterString)
    {
        ListItem[] listItems = GetComponentsInChildren<ListItem>(true);
        for (int i = 0; i < listItems.Length; i++)
        {
            if (listItems[i].GetNode().GetString(Node.LinknameAttribute).Contains(filterString))
            {
                listItems[i].gameObject.SetActive(true);
            }
            else
            {
                listItems[i].gameObject.SetActive(false);
            }
        }
    }
}
