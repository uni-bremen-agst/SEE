using System;
using System.Collections;
using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

/// <summary>
/// Animates the position and scale of a given GameObject over the full <see cref="AbstractCCAAnimator.MaxAnimationTime"/>.
/// When <see cref="Node.WasModified()"/> is true the GameObject will shake to symbolize a file modification.
/// </summary>
public class SimpleCCAAnimator : AbstractCCAAnimator
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
        iTween.MoveTo(gameObject, iTween.Hash(
                "position", position,
                "time", MaxAnimationTime
            ));
        iTween.ScaleTo(gameObject, iTween.Hash(
            "scale", scale,
            "time", MaxAnimationTime
        ));

        if (node.WasModified())
        {
            iTween.ShakeRotation(gameObject, iTween.Hash(
                "amount", new Vector3(0, 10, 0),
                "time", MaxAnimationTime / 2,
                "delay", MaxAnimationTime / 2
            ));
        }
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

        if (node.WasModified())
        {
            iTween.ShakeRotation(gameObject, iTween.Hash(
                "amount", new Vector3(0, 10, 0),
                "time", MaxAnimationTime / 2,
                "delay", MaxAnimationTime / 2
            ));
        }
    }
}
