using SEE;
using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CCARender : AbstractCCARender
{
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

        if (isLeafNew)
        {
            leaf.transform.position = Layout.GetPositon(node);
            leaf.transform.localScale = Layout.GetScale(node);

            iTween.MoveFrom(leaf, iTween.Hash(
                "y", -100, // TODO flo: -Sizeofbuilding
                "time", 2
            ));
        }
        else if (node.WasModified())
        {
            iTween.MoveTo(leaf, iTween.Hash(
                "position", Layout.GetPositon(node),
                "time", 2
            ));
            iTween.ScaleTo(leaf, iTween.Hash(
                "scale", Layout.GetScale(node),
                "time", 2
            ));
            iTween.ShakeRotation(leaf, iTween.Hash(
                "amount", new Vector3(0, 10, 0),
                "time", 1,
                "delay", 1
            ));
        }
        else if (node.WasRelocated(out string oldLinkageName))
        {
            iTween.MoveTo(leaf, iTween.Hash(
                "position", Layout.GetPositon(node),
                "time", 2
            ));
            iTween.ScaleTo(leaf, iTween.Hash(
                "scale", Layout.GetScale(node),
                "time", 2
            ));
        }
        else
        {
            iTween.MoveTo(leaf, iTween.Hash(
                "position", Layout.GetPositon(node),
                "time", 2
            ));
            iTween.ScaleTo(leaf, iTween.Hash(
                "scale", Layout.GetScale(node),
                "time", 2
            ));
        }
        //leaf.transform.position = Layout.GetPositon(node);
        //leaf.transform.localScale = Layout.GetScale(node);
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
        //var isLeafNew = !ObjectManager.GetLeaf(node, out GameObject leaf);
        if(ObjectManager.RemoveNode(node, out GameObject leaf))
        {
            iTween.MoveTo(leaf, iTween.Hash(
                "y", -100, // TODO flo: -Sizeofbuilding
                "time", 2,
                "oncompletetarget", this.gameObject,
                "oncomplete", "OnRemovedNodeFinishedAnimation",
                "oncompleteparams", leaf
            ));
            /*
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
            */
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
