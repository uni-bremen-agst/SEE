using SEE;
using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        if (isPlaneNew)
        {

        }

        /*
         * TODO Animation
         void onAnimateStart
         void onAnimateTo
         void onAnimateRemove
        */
        root.transform.position = Layout.PlanePositon;
        var scal = new Vector3(100, 1, 100);
        scal.Scale(Layout.GetScale(node));
        iTween.ScaleTo(root, iTween.Hash(
            "scale", scal,
            "time", 2
        ));
        //root.transform.localScale = scal;
    }

    protected override void RenderInnerNode(Node node)
    {
        //var isCircleNew = !objectManager.GetInnerNode(node, out GameObject circle);

        //var circlePosition = Layout.CirclePosition(node);
        //var circleRadius = Layout.CircleRadius(node);

        /*
         * TODO Animation
         void onAnimateStart
         void onAnimateTo
         void onAnimateRemove
        */

        //ExtendedTextFactory.UpdateText(circleText, node.SourceName, circlePosition, circleRadius);
    }

    protected override void RenderLeaf(Node node)
    {
        var isLeafNew = !ObjectManager.GetLeaf(node, out GameObject leaf);
        var position = Layout.GetPositon(node);
        var scale = Layout.GetScale(node);
        // TODO more leaf initialisation
        void animateWith(AbstractCCAAnimator animator)
        {
            animator.AnimateTo(node, leaf, Layout.GetPositon(node), Layout.GetScale(node));
        }

        if (isLeafNew)
        {
            var height = 100; // TODO object size
            position.y -= height;
            leaf.transform.position = position;
            position.y += height;
            MoveAnim.AnimateTo(node, leaf, position, scale);
        }
        else if (node.WasModified())
        {
            animateWith(SimpleAnim);
        }
        else if (node.WasRelocated(out string oldLinkageName))
        {
            animateWith(SimpleAnim);
        }
        else
        {
            animateWith(SimpleAnim);
        }
    }

    protected override void RenderEdge(Edge edge)
    {

    }

    protected override void RenderRemovedOldInnerNode(Node node)
    {
        ObjectManager.RemoveNode(node, out GameObject gameObject);
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    protected override void RenderRemovedOldLeaf(Node node)
    {
        if(ObjectManager.RemoveNode(node, out GameObject leaf))
        {
            var height = 100; // TODO object size
            var position = Layout.GetPositon(node);
            var scale = Layout.GetScale(node);
            gameObject.transform.position = position;
            position.y -= height;
            MoveAnim.AnimateTo(node, leaf, position, scale, OnRemovedNodeFinishedAnimation);
        }
    }

    protected override void RenderRemovedOldEdge(Edge edge)
    {

    }

    void OnRemovedNodeFinishedAnimation(object gameObject)
    {
        if (gameObject != null && gameObject.GetType() == typeof(GameObject) )
        {
            Destroy((GameObject)gameObject);
        }
    }
}
