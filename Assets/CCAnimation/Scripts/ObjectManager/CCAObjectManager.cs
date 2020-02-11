using SEE;
using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implements the <see cref="AbstractCCAObjectManager"/> by using a supplied
/// NodeFactory to create its GameObjects.
/// </summary>
public class CCAObjectManager : AbstractCCAObjectManager
{
    /// <summary>
    /// The root GameObject of a graph.
    /// </summary>
    private GameObject _root;

    /// <summary>
    /// A dictionary containing all created nodes that are in use.
    /// </summary>
    private readonly Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();

    /// <summary>
    /// A circle factory for the inner nodes.
    /// </summary>
    private readonly CircleFactory CircleFactory;

    /// <summary>
    /// Constructor that sets the used node factory to create the GameObjects
    /// </summary>
    public CCAObjectManager(NodeFactory nodeFactory) : base(nodeFactory)
    {
        CircleFactory = new CircleFactory(nodeFactory.Unit);
    }

    /// <summary>
    /// Returns a list containing all created nodes that are in use.
    /// </summary>
    public override List<GameObject> GameObjects => nodes.Values.ToList();

    /// <summary>
    /// Returns the root GameObject and creates it if necessary
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public override bool GetRoot(out GameObject root)
    {
        var hasRoot = _root != null;
        if (!hasRoot)
        {
            _root = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _root.name = "RootPlane";
            _root.tag = Tags.Decoration;

            var rootCircle = CircleFactory.NewBlock();
            rootCircle.name = "RootCircle";
            rootCircle.tag = Tags.Decoration;
            var newPosition = rootCircle.transform.position;
            newPosition.y = 1F;
            rootCircle.transform.localPosition = newPosition;
            rootCircle.transform.localScale = Vector3.one * 1.1F;

            rootCircle.transform.SetParent(_root.transform);

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

    /// <summary>
    /// Returns the GameObject for an inner node and creates it if necessary.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="innerNode"></param>
    /// <returns></returns>
    public override bool GetInnerNode(Node node, out GameObject innerNode)
    {
        node.AssertNotNull("node");

        var hasInnerNode = nodes.TryGetValue(node.LinkName, out innerNode);
        if (!hasInnerNode)
        {
            innerNode = CircleFactory.NewBlock();
            innerNode.name = node.LinkName;
            innerNode.tag = Tags.Node;
            nodes[node.LinkName] = innerNode;

            var textPosition = Vector3.zero;
            textPosition.y = 1F;
            var nodeText = TextFactory.GetText(node.LinkName, textPosition, 10, false);
            nodeText.transform.localScale = new Vector3(0.035F, 0.035F, 0.035F);
            nodeText.transform.SetParent(innerNode.transform);
        }

        NodeRef noderef = innerNode.GetComponent<NodeRef>();
        if (noderef == null)
        {
            noderef = innerNode.AddComponent<NodeRef>();
        }
        noderef.node = node;

        return hasInnerNode;
    }

    /// <summary>
    /// Returns the GameObject for a leaf node and creates it if necessary.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="leaf"></param>
    /// <returns></returns>
    public override bool GetLeaf(Node node, out GameObject leaf)
    {
        node.AssertNotNull("node");

        var hasLeaf = nodes.TryGetValue(node.LinkName, out leaf);
        if (!hasLeaf)
        {
            leaf = NodeFactory.NewBlock();
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

    /// <summary>
    /// Removes a supplied node by using its Node.LinkName and returns
    /// the removed node, if some was removed.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    public override bool RemoveNode(Node node, out GameObject gameObject)
    {
        node.AssertNotNull("node");

        var wasNodeRemoved = nodes.TryGetValue(node.LinkName, out gameObject);
        nodes.Remove(node.LinkName);
        return wasNodeRemoved;
    }

    /// <summary>
    /// Clears the internal lists containing the GameObjects,
    /// without destroing them.
    /// </summary>
    public override void Clear()
    {
        _root = null;
        nodes.Clear();
    }
}
