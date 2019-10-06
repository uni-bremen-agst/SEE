using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SEE.DataModel;

public class ListItem : MonoBehaviour
{
    public Text LinkageName;

    private Node Node;

    void Start()
    {
        LinkageName = GetComponentInChildren<Text>();
    }

    public Node GetNode()
    {
        return Node;
    }

    public void SetNode(Node node)
    {
        Node = node;
        LinkageName.text = node.GetString(Node.LinknameAttribute);
    }

    public void OnTeleport()
    {
        Camera.main.transform.position = Node.transform.GetChild(0).position;
    }

}
