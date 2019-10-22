using SEE;
using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCAObjectManager : AbstractCCAObjectManager
{
    private GameObject _root;
    private readonly Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();

    public CCAObjectManager() : base(new BuildingFactory())
    {

    }

    public override bool GetRoot(out GameObject root)
    {
        var hasRoot = _root != null;
        if (!hasRoot)
        {
            _root = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _root.name = "RootPlane";
            _root.tag = Tags.Decoration;

            var planeRenderer = _root.GetComponent<Renderer>();
            planeRenderer.sharedMaterial = new Material(planeRenderer.sharedMaterial)
            {
                color = Color.gray
            };

            // Turn off reflection of plane
            planeRenderer.sharedMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            planeRenderer.sharedMaterial.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            planeRenderer.sharedMaterial.SetFloat("_SpecularHighlights", 0.0f);
        }
        root = _root;
        return hasRoot;
    }

    public override bool GetInnerNode(Node node, out GameObject innerNode)
    {
        var hasInnerNode = nodes.TryGetValue(node.LinkName, out innerNode);
        if (!hasInnerNode)
        {
            innerNode = new GameObject
            {
                name = node.LinkName,
                tag = Tags.Node
            };
            nodes[node.LinkName] = innerNode;
        }

        NodeRef noderef = innerNode.GetComponent<NodeRef>();
        if (noderef == null)
        {
            noderef = innerNode.AddComponent<NodeRef>();
        }
        noderef.node = node;

        /*
        private readonly Dictionary<string, GameObject> circleTexts = new Dictionary<string, GameObject>();
        var isNewCircleText = !circleTexts.TryGetValue(node.LinkName, out circleText);
        if (isNewCircleText)
        {
            circleText = ExtendedTextFactory.GetEmpty(node.LinkName);
            circleTexts[node.LinkName] = circleText;
        }

        return isNewCircle || isNewCircleText;
        */
        return hasInnerNode;
    }

    public override bool GetLeaf(Node node, out GameObject leaf)
    {
        var hasLeaf = nodes.TryGetValue(node.LinkName, out leaf);
        if (!hasLeaf)
        {
            leaf = BlockFactory.NewBlock();
            leaf.name = node.LinkName;
            nodes[node.LinkName] = leaf;
        }

        NodeRef noderef = leaf.GetComponent<NodeRef>();
        if (noderef == null)
        {
            noderef = leaf.AddComponent<NodeRef>();
        }
        noderef.node = node;

        return hasLeaf;
    }

    public override bool RemoveNode(Node node, out GameObject gameObject)
    {
        var wasNodeRemoved = nodes.TryGetValue(node.LinkName, out gameObject);
        nodes.Remove(node.LinkName);
        return wasNodeRemoved;
    }

    public override void Clear()
    {
        _root = null;
        nodes.Clear();
    }
}
