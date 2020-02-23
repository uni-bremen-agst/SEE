//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A CCARender that is used to display houses as graph nodes.
/// </summary>
public class CCHouseRenderer : AbstractCCARender
{
    /// <summary>
    /// A SimpleAnimator used for animation.
    /// </summary>
    private readonly AbstractCCAAnimator SimpleAnim = new SimpleCCAAnimator();

    /// <summary>
    /// A MoveAnimator used for move animations.
    /// </summary>
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
            // if the plane is new instantly apply the position and size
            root.transform.position = Vector3.zero;
            root.transform.localScale = nodeTransform.scale;
        }
        else
        {
            // if the tranform of the plane changed animate it
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
            // if the inner node is new, animate it by moving it out of the ground
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
            // if the leaf node is new animate it, by moving it out of the ground
            actualSize.y += 5; // for a smoother animation
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
            // if the node needs to be removed, let it sink into the ground
            var nextPosition = gameObject.transform.position;
            nextPosition.y = -2;
            MoveAnim.AnimateTo(node, gameObject, nextPosition, gameObject.transform.localScale, OnRemovedNodeFinishedAnimation);
        }
    }

    protected override void RenderRemovedOldLeaf(Node node)
    {
        if(ObjectManager.RemoveNode(node, out GameObject leaf))
        {
            // if the node needs to be removed, let it sink into the ground
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
    /// Event function that destroys a given GameObject.
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
