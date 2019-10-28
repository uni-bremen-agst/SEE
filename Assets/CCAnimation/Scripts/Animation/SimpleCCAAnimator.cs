using System;
using System.Collections;
using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

/// <summary>
/// TODO flo doc:
/// </summary>
public class SimpleCCAAnimator : AbstractCCAAnimator
{
    protected override void AnimateToInternal(Node node, GameObject gameObject, Vector3 position, Vector3 scale)
    {
        if (node.WasAdded())
        {
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;

            iTween.MoveFrom(gameObject, iTween.Hash(
                "y", -100, // TODO flo: -Sizeofbuilding
                "time", MaxAnimationTime
            ));
        }
        else if (node.WasModified())
        {
            iTween.MoveTo(gameObject, iTween.Hash(
                "position", position,
                "time", MaxAnimationTime
            ));
            iTween.ScaleTo(gameObject, iTween.Hash(
                "scale", scale,
                "time", MaxAnimationTime
            ));
            iTween.ShakeRotation(gameObject, iTween.Hash(
                "amount", new Vector3(0, 10, 0),
                "time", 1,
                "delay", 1
            ));
        }
        else if (node.WasRelocated(out string oldLinkageName))
        {
            iTween.MoveTo(gameObject, iTween.Hash(
                "position", position,
                "time", MaxAnimationTime
            ));
            iTween.ScaleTo(gameObject, iTween.Hash(
                "scale", scale,
                "time", MaxAnimationTime
            ));
        }
        else
        {
            iTween.MoveTo(gameObject, iTween.Hash(
                "position", position,
                "time", MaxAnimationTime
            ));
            iTween.ScaleTo(gameObject, iTween.Hash(
                "scale", scale,
                "time", MaxAnimationTime
            ));
        }
    }

    protected override void AnimateToAndInternal(Node node, GameObject gameObject, Vector3 position, Vector3 scale, GameObject callBackTarget, string callbackName)
    {
        iTween.MoveTo(gameObject, iTween.Hash(
            "y", -100,
            "time", MaxAnimationTime,
            "oncompletetarget", callBackTarget,
            "oncomplete", callbackName,
            "oncompleteparams", gameObject
        ));
    }
}
