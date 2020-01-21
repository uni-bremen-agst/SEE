using SEE;
using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// TODO flo doc
/// </summary>
public class CCARender : AbstractCCARender
{
    private readonly AbstractCCAAnimator SimpleAnim = new SimpleCCAAnimator();
    private readonly AbstractCCAAnimator MoveAnim = new MoveCCAAnimator();

    protected override void RegisterAllAnimators(List<AbstractCCAAnimator> animators)
    {
        animators.Add(SimpleAnim);
        animators.Add(MoveAnim);
    }

    protected override void RenderRoot(Node node)
    {
        var isPlaneNew = !ObjectManager.GetRoot(out GameObject root);
        var nodeTransform = Layout.GetNodeTransform(node);
        if(isPlaneNew)
        {
            root.transform.position = Vector3.zero;
            root.transform.localScale = nodeTransform.scale;
        }
        else
        {
            SimpleAnim.AnimateTo(node, root, Vector3.zero, nodeTransform.scale);
        }
    }

    protected override void RenderInnerNode(Node node)
    {
        var isCircleNew = !ObjectManager.GetInnerNode(node, out GameObject circle);
        var nodeTransform = Layout.GetNodeTransform(node);
        var circlePosition = nodeTransform.position;
        circlePosition.y = 0.5F;

        var circleRadius = nodeTransform.scale;
        circleRadius.x += 2;
        circleRadius.z += 2;

        if (isCircleNew)
        {
            circlePosition.y = -3;
            circle.transform.position = circlePosition;
            circle.transform.localScale = circleRadius;

            circlePosition.y = 0.5F;
            SimpleAnim.AnimateTo(node, circle, circlePosition, circleRadius);
        }
        else if (node.WasModified())
        {
            SimpleAnim.AnimateTo(node, circle, circlePosition, circleRadius);
        }
        else if (node.WasRelocated(out string oldLinkageName))
        {
            SimpleAnim.AnimateTo(node, circle, circlePosition, circleRadius);
        }
        else
        {
            SimpleAnim.AnimateTo(node, circle, circlePosition, circleRadius);
        }
    }

    protected override void RenderLeaf(Node node)
    {
        var isLeafNew = !ObjectManager.GetLeaf(node, out GameObject leaf);
        var nodeTransform = Layout.GetNodeTransform(node);
        var nextPosition = nodeTransform.position;
        var nextScale = nodeTransform.scale;

        var oldPosition = leaf.transform.position;
        var oldSize = ObjectManager.NodeFactory.GetSize(leaf);
        ObjectManager.NodeFactory.SetSize(leaf, nextScale);
        ObjectManager.NodeFactory.SetGroundPosition(leaf, nextPosition);
        var realNewPosition = leaf.transform.position;
        
        var actualSize = ObjectManager.NodeFactory.GetSize(leaf);
        var sizeDifference = new Vector3(oldSize.x / actualSize.x, oldSize.y / actualSize.y, oldSize.z / actualSize.z);

        if (isLeafNew)
        {
            actualSize.y += 5; // Für eine glattere Animation
            actualSize.x = 0;
            actualSize.z = 0;
            leaf.transform.position -= actualSize;
            SimpleAnim.AnimateTo(node, leaf, realNewPosition, Vector3.one);
        }
        else if (node.WasModified())
        {
            leaf.transform.position = oldPosition;
            leaf.transform.localScale = sizeDifference;
            SimpleAnim.AnimateTo(node, leaf, realNewPosition, Vector3.one);
        }
        else if (node.WasRelocated(out string oldLinkageName))
        {
            leaf.transform.position = oldPosition;
            leaf.transform.localScale = sizeDifference;
            SimpleAnim.AnimateTo(node, leaf, realNewPosition, Vector3.one);
        }
        else
        {
            leaf.transform.position = oldPosition;
            leaf.transform.localScale = sizeDifference;
            SimpleAnim.AnimateTo(node, leaf, realNewPosition, Vector3.one);
        }
    }

    protected override void RenderEdge(Edge edge)
    {

    }

    protected override void RenderRemovedOldInnerNode(Node node)
    {
        if (ObjectManager.RemoveNode(node, out GameObject gameObject))
        {
            var nextPosition = gameObject.transform.position;
            nextPosition.y = -2;
            MoveAnim.AnimateTo(node, gameObject, nextPosition, gameObject.transform.localScale, OnRemovedNodeFinishedAnimation);
        }
    }

    protected override void RenderRemovedOldLeaf(Node node)
    {
        if(ObjectManager.RemoveNode(node, out GameObject leaf))
        {
            var nodeTransform = Layout.GetNodeTransform(node);
            var actualSize = ObjectManager.NodeFactory.GetSize(leaf) * 1.5F;
            actualSize.x = 0;
            actualSize.z = 0;
            var nextPosition = leaf.transform.position - actualSize;
            MoveAnim.AnimateTo(node, leaf, nextPosition, Vector3.one, OnRemovedNodeFinishedAnimation);
        }
    }

    protected override void RenderRemovedOldEdge(Edge edge)
    {

    }

    /// <summary>
    /// TODO flo doc
    /// </summary>
    /// <param name="gameObject"></param>
    void OnRemovedNodeFinishedAnimation(object gameObject)
    {
        if (gameObject != null && gameObject.GetType() == typeof(GameObject) )
        {
            Destroy((GameObject)gameObject);
        }
    }
}
