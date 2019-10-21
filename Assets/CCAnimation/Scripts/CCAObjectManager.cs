using SEE;
using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCAObjectManager
{
    private GameObject _plane;

    public bool GetPlane(out GameObject plane)
    {
        if (_plane == null)
        {
            _plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _plane.name = "RootPlane";
            _plane.tag = Tags.Decoration;
            plane = _plane;
            return true;
        }
        plane = _plane;
        return false;
    }

    private readonly Dictionary<string, GameObject> blocks = new Dictionary<string, GameObject>();
    // TODO Create or get existing NodeGameObjects

    public bool GetBlock(Node node, out GameObject block)
    {
        block = null;// TODO block = xxxx;
        BlockFactory blockFactory = null; // TODO

        var isNewBlock = block == null;
        if (isNewBlock)
        {
            block = blockFactory.NewBlock();
            block.name = node.LinkName;
            blocks[node.LinkName] = block;
        }

        NodeRef noderef = block.GetComponent<NodeRef>();
        if (noderef == null)
        {
            noderef = block.AddComponent<NodeRef>();
        }
        noderef.node = node;

        return isNewBlock;
    }

    private readonly Dictionary<string, GameObject> circles = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> circleTexts = new Dictionary<string, GameObject>();

    public bool GetCircle(Node node, out GameObject circle, out GameObject circleText)
    {
        var isNewCircle = !circles.TryGetValue(node.LinkName, out circle);
        if (isNewCircle)
        {
            circle = new GameObject();
            circle.name = node.LinkName;
            circle.tag = Tags.Node;
            circles[node.LinkName] = circle;
        }

        NodeRef noderef = circle.GetComponent<NodeRef>();
        if (noderef == null)
        {
            noderef = circle.AddComponent<NodeRef>();
        }
        noderef.node = node;

        var isNewCircleText = !circleTexts.TryGetValue(node.LinkName, out circleText);
        if (isNewCircleText)
        {
            circleText = ExtendedTextFactory.GetEmpty(node.LinkName);
            circleTexts[node.LinkName] = circleText;
        }

        return isNewCircle || isNewCircleText;
    }

    private BlockFactory blockFactory;

    public void Init(GraphSettings editorSettings)
    {
        if (editorSettings.CScapeBuildings)
        {
            blockFactory = new BuildingFactory();
        }
        else
        {
            blockFactory = new CubeFactory();
        }
    }

    public void Clear()
    {

    }
}
