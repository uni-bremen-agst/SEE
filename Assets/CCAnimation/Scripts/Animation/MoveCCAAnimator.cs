using System.Collections;
using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

/// <summary>
/// TODO flo doc
/// </summary>
public class MoveCCAAnimator : AbstractCCAAnimator
{
    protected override void AnimateToInternal(Node node, GameObject gameObject, Vector3 position, Vector3 scale)
    {
        gameObject.transform.localScale = scale;
        iTween.MoveTo(gameObject, iTween.Hash(
            "position", position,
            "time", MaxAnimationTime
        ));
    }

    protected override void AnimateToAndInternal(Node node, GameObject gameObject, Vector3 position, Vector3 scale, GameObject callBackTarget, string callbackName)
    {
        gameObject.transform.localScale = scale;
        iTween.MoveTo(gameObject, iTween.Hash(
            "position", position,
            "time", MaxAnimationTime,
            "oncompletetarget", callBackTarget,
            "oncomplete", callbackName,
            "oncompleteparams", gameObject
        ));
    }
}
