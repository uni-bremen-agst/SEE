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

using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// An Animation Renderer that is used to display blocks as graph nodes.
    /// </summary>
    class BlockRenderer : AbstractRenderer
    {
        /// <summary>
        /// A SimpleAnimator used for animation.
        /// </summary>
        private readonly AbstractAnimator SimpleAnim = new SimpleAnimator();

        /// <summary>
        /// A MoveAnimator used for move animations.
        /// </summary>
        private readonly AbstractAnimator MoveAnim = new MoveAnimator();

        protected override void RegisterAllAnimators(List<AbstractAnimator> animators)
        {
            animators.Add(SimpleAnim);
            animators.Add(MoveAnim);
        }

        protected override void RenderRoot(Node node)
        {
            var isPlaneNew = !ObjectManager.GetRoot(out GameObject root);
            var nodeTransform = Layout.GetNodeTransform(node);
            if (isPlaneNew)
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

        protected override void RenderEdge(Edge edge)
        {
            
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
                // if the node is new instantly apply the position and size
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

            if (isLeafNew)
            {
                // if the leaf node is new animate it, by moving it out of the ground
                var newPosition = nodeTransform.position;
                newPosition.y = -nodeTransform.scale.y;
                leaf.transform.position = newPosition;

                SimpleAnim.AnimateTo(node, leaf, nodeTransform.position, nodeTransform.scale);
            }
            else if (node.WasModified())
            {
                SimpleAnim.AnimateTo(node, leaf, nodeTransform.position, nodeTransform.scale);
            }
            else if (node.WasRelocated(out string oldLinkageName))
            {
                SimpleAnim.AnimateTo(node, leaf, nodeTransform.position, nodeTransform.scale);
            }
            else
            {
                SimpleAnim.AnimateTo(node, leaf, nodeTransform.position, nodeTransform.scale);
            }
        }

        protected override void RenderRemovedOldEdge(Edge edge)
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
            if (ObjectManager.RemoveNode(node, out GameObject leaf))
            {
                // if the node needs to be removed, let it sink into the ground
                var newPosition = leaf.transform.position;
                newPosition.y = -leaf.transform.localScale.y;

                SimpleAnim.AnimateTo(node, leaf, newPosition, leaf.transform.localScale, OnRemovedNodeFinishedAnimation);
            }
        }
    }
}
