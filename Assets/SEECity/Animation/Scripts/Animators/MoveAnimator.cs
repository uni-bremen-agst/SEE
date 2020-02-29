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
