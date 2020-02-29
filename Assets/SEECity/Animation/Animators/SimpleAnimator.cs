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
using System;
using System.Collections;
using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

/// <summary>
/// Animates the position and scale of a given GameObject over the full <see cref="AbstractAnimator.MaxAnimationTime"/>.
/// When <see cref="Node.WasModified()"/> is true the GameObject will shake to symbolize a file modification.
/// </summary>
public class SimpleAnimator : AbstractAnimator
{
    /// <summary>
    /// See <see cref="AbstractAnimator.AnimateToInternal(Node, GameObject, Vector3, Vector3)"/>
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
    /// See <see cref="AbstractAnimator.AnimateToAndInternal(Node, GameObject, Vector3, Vector3, GameObject, string)"/>
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
