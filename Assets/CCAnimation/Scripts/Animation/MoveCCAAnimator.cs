using System.Collections;
using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

/// <summary>
/// Animates the position of a given GameObject over the full <see cref="AbstractCCAAnimator.MaxAnimationTime"/>.
/// The scale is instantly applied.
/// </summary>
public class MoveCCAAnimator : AbstractCCAAnimator
{
    /// <summary>
    /// See <see cref="AbstractCCAAnimator.AnimateToInternal(Node, GameObject, Vector3, Vector3)"/>
    /// </summary>
    /// <param name="node"></param>
    /// <param name="gameObject"></param>
    /// <param name="position"></param>
    /// <param name="scale"></param>
    protected override void AnimateToInternal(Node node, GameObject gameObject, Vector3 position, Vector3 scale)
    {
        gameObject.transform.localScale = scale;
        iTween.MoveTo(gameObject, iTween.Hash(
            "position", position,
            "time", MaxAnimationTime
        ));
    }

    /// <summary>
    /// See <see cref="AbstractCCAAnimator.AnimateToAndInternal(Node, GameObject, Vector3, Vector3, GameObject, string)"/>
    /// </summary>
    /// <param name="node"></param>
    /// <param name="gameObject"></param>
    /// <param name="position"></param>
    /// <param name="scale"></param>
    /// <param name="callBackTarget"></param>
    /// <param name="callbackName"></param>
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
