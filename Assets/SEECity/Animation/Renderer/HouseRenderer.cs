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
using UnityEngine;

namespace SEE.Animation.Internal
{
    /*
/// <summary>
/// An Animation Renderer that is used to display buildings as graph leaf nodes.
/// </summary>
public class HouseRenderer : AbstractRenderer
{
protected override void RenderLeaf(Node node)
{
    var isLeafNew = !ObjectManager.GetLeaf(node, out GameObject leaf);
    var nodeTransform = NextLayoutToBeShown.GetNodeTransform(node);
    var nextPosition = nodeTransform.position;
    var nextScale = nodeTransform.scale;

    var oldPosition = leaf.transform.position;
    var oldSize = ObjectManager.GraphRenderer.GetSize(leaf);
    ObjectManager.GraphRenderer.SetSize(leaf, nextScale);
    ObjectManager.GraphRenderer.SetGroundPosition(leaf, nextPosition);
    var realNewPosition = leaf.transform.position;

    var actualSize = ObjectManager.GraphRenderer.GetSize(leaf);
    var sizeDifference = new Vector3(oldSize.x / actualSize.x, oldSize.y / actualSize.y, oldSize.z / actualSize.z);

    if (isLeafNew)
    {
        // if the leaf node is new, animate it by moving it out of the ground
        actualSize.y += 5; // for a smoother animation
        actualSize.x = 0;
        actualSize.z = 0;
        leaf.transform.position -= actualSize;
    }
    else
    {
        leaf.transform.position = oldPosition;
        leaf.transform.localScale = sizeDifference;
    }
    SimpleAnim.AnimateTo(node, leaf, realNewPosition, Vector3.one);
}
    protected override void RenderRemovedOldLeaf(Node node)
    {
        if (ObjectManager.RemoveNode(node, out GameObject leaf))
        {
            // if the node needs to be removed, let it sink into the ground
            var nodeTransform = NextLayoutToBeShown.GetNodeTransform(node);
            var actualSize = ObjectManager.GraphRenderer.GetSize(leaf) * 1.5F;
            actualSize.x = 0;
            actualSize.z = 0;
            var nextPosition = leaf.transform.position - actualSize;
            MoveAnim.AnimateTo(node, leaf, nextPosition, Vector3.one, OnRemovedNodeFinishedAnimation);
        }
    }

}
    */
}