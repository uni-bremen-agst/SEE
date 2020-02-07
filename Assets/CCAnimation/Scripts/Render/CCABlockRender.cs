using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEE.DataModel;
using UnityEngine;

/// <summary>
/// TODO DOc
/// </summary>
namespace Assets.CCAnimation.Scripts.Render
{
    class CCABlockRender : AbstractCCARender
    {
        private readonly AbstractCCAAnimator SimpleAnim = new SimpleCCAAnimator();
        private readonly AbstractCCAAnimator MoveAnim = new MoveCCAAnimator();

        protected override void RegisterAllAnimators(List<AbstractCCAAnimator> animators)
        {
            animators.Add(SimpleAnim);
            animators.Add(MoveAnim);
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

                var newPosition = nodeTransform.position;
                newPosition.y = -nodeTransform.scale.y;
                leaf.transform.position = newPosition;
                //leaf.transform.localScale = nodeTransform.scale;

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
                var nextPosition = gameObject.transform.position;
                nextPosition.y = -2;
                MoveAnim.AnimateTo(node, gameObject, nextPosition, gameObject.transform.localScale, OnRemovedNodeFinishedAnimation);
            }
        }

        protected override void RenderRemovedOldLeaf(Node node)
        {
            if (ObjectManager.RemoveNode(node, out GameObject leaf))
            {
                var newPosition = leaf.transform.position;
                newPosition.y = -leaf.transform.localScale.y;

                SimpleAnim.AnimateTo(node, leaf, newPosition, leaf.transform.localScale, OnRemovedNodeFinishedAnimation);
            }
        }

        protected override void RenderRoot(Node node)
        {
            var isPlaneNew = !ObjectManager.GetRoot(out GameObject root);
            var nodeTransform = Layout.GetNodeTransform(node);
            if (isPlaneNew)
            {
                root.transform.position = Vector3.zero;
                root.transform.localScale = nodeTransform.scale;
            }
            else
            {
                SimpleAnim.AnimateTo(node, root, Vector3.zero, nodeTransform.scale);
            }
        }

        /// <summary>
        /// TODO flo doc
        /// </summary>
        /// <param name="gameObject"></param>
        void OnRemovedNodeFinishedAnimation(object gameObject)
        {
            if (gameObject != null && gameObject.GetType() == typeof(GameObject))
            {
                Destroy((GameObject)gameObject);
            }
        }
    }
}
