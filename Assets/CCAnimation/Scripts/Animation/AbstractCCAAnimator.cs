using SEE.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An abstract animator that makes it simple to swap an existing type of animation.
/// For example there could be a RotationAnimator and BounceAnimator for different
/// kind of states.
/// </summary>
public abstract class AbstractCCAAnimator
{
    /// <summary>
    /// Defines the default time an animation takes in seconds.
    /// </summary>
    public const int DefaultAnimationTime = 2;

    private float _maxAnimationTime;
    private bool _animationsDisabled = false;

    /// <summary>
    /// Defines the maximum time an animation is allowed to Take, in seconds, the animation is allowed to take.
    /// </summary>
    public float MaxAnimationTime { get => _maxAnimationTime; set => _maxAnimationTime = value; }

    /// <summary>
    /// If set to true animations are skipped and the new values are instantly applied.
    /// </summary>
    public bool AnimationsDisabled { get => _animationsDisabled; set => _animationsDisabled = value; }

    /// <summary>
    /// Creates a new animator with a given maximal animation time.
    /// </summary>
    /// <param name="maxAnimationTime">The maximum time the animation is allowed to run.</param>
    public AbstractCCAAnimator(float maxAnimationTime = DefaultAnimationTime)
    {
        this.MaxAnimationTime = maxAnimationTime;
    }

    /// <summary>
    /// Animates a give GameObject to a new position an scale. If needed a callback that is called
    /// after the animation is finished, can be defined. The animation is implemented by
    /// <see cref="AnimateToInternal(Node, GameObject, Vector3, Vector3)"/>
    /// </summary>
    /// <param name="node">Node of the given GameObject</param>
    /// <param name="gameObject">GameObject to animate</param>
    /// <param name="position">The new position</param>
    /// <param name="scale">The new scale</param>
    /// <param name="callback">An optional callback</param>
    public void AnimateTo(Node node, GameObject gameObject, Vector3 position, Vector3 scale, Action<object> callback = null)
    {
        node.AssertNotNull("node");
        gameObject.AssertNotNull("gameObject");
        position.AssertNotNull("position");
        scale.AssertNotNull("scale");

        if (AnimationsDisabled)
        {
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            callback?.Invoke(gameObject);
        }
        else if(callback == null)
        {
            AnimateToInternal(node, gameObject, position, scale);
        }
        else
        {
            AnimateToAndInternal(node, gameObject, position, scale, ((MonoBehaviour)callback.Target).gameObject, callback.Method.Name);
        }
    }

    /// <summary>
    /// Abstract method, called by <see cref="AnimateTo"/> for an animation without a callback.
    /// </summary>
    /// <param name="node">Node of the given GameObject</param>
    /// <param name="gameObject">GameObject to animate</param>
    /// <param name="position">The new position</param>
    /// <param name="scale">The new scale</param>
    protected abstract void AnimateToInternal(Node node, GameObject gameObject, Vector3 position, Vector3 scale);

    /// <summary>
    /// Abstract method, called by <see cref="AnimateTo"/> for an animation with a callback.
    /// </summary>
    /// <param name="node">Node of the given GameObject</param>
    /// <param name="gameObject">GameObject to animate</param>
    /// <param name="position">The new position</param>
    /// <param name="scale">The new scale</param>
    /// <param name="callback">An optional callback</param>
    protected abstract void AnimateToAndInternal(Node node, GameObject gameObject, Vector3 position, Vector3 scale, GameObject callBackTarget, string callbackName);
}
