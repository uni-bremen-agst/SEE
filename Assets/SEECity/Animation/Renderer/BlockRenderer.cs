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
    /// An Animation Renderer that is used to display blocks as graph leaf nodes.
    /// </summary>
    class BlockRenderer : AbstractRenderer
    {
        /*
        protected override void RenderLeaf(Node node)
        {
            var isLeafNew = !ObjectManager.GetLeaf(node, out GameObject leaf);
            var nodeTransform = NextLayoutToBeShown.GetNodeTransform(node);

            if (isLeafNew)
            {
                // if the leaf node is new, animate it by moving it out of the ground
                var newPosition = nodeTransform.position;
                newPosition.y = -nodeTransform.scale.y;
                leaf.transform.position = newPosition;
            }
            SimpleAnim.AnimateTo(node, leaf, nodeTransform.position, nodeTransform.scale);
        }


        protected override void RenderRemovedOldLeaf(Node node)
        {
            if (ObjectManager.RemoveNode(node, out GameObject leaf))
            {
                // if the node needs to be removed, let it sink into the ground
                var newPosition = leaf.transform.position;
                newPosition.y = -leaf.transform.localScale.y;

                SimpleAnim.AnimateTo(node, leaf, newPosition, leaf.transform.localScale, OnRemovedNodeFinishedAnimation);
            }
        }
    }
            */
    }
