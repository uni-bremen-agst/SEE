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
        // TODO remove if things
        if (node.WasModified())
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
                "time", MaxAnimationTime / 2,
                "delay", MaxAnimationTime / 2
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
            "position", position,
            "time", MaxAnimationTime,
            "oncompletetarget", callBackTarget,
            "oncomplete", callbackName,
            "oncompleteparams", gameObject
        ));
        iTween.ScaleTo(gameObject, iTween.Hash(
            "scale", scale,
            "time", MaxAnimationTime
        ));
    }
}
