using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SEE.Game.City;
using SEE.GO;
using SEE.GO.Factories;
using SEE.Utils;
using TinySpline;
using UnityEngine;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the edge it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    public partial class EdgeOperator : GraphElementOperator<(Color start, Color end)>
    {
        /// <summary>
        /// Operation handling edge morphing.
        /// </summary>
        private MorphismOperation morphism;

        /// <summary>
        /// Operation handling the construction of edges from subsplines.
        /// </summary>
        private TweenOperation<bool> construction;

        /// <summary>
        /// The <see cref="SEESpline"/> represented by this edge.
        /// </summary>
        private SEESpline spline;

        #region Public API

        /// <summary>
        /// Morph the spline represented by this edge to the given <paramref name="target"/> spline.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<TweenCallback> MorphTo(BSpline target, float factor = 1)
        {
            // TODO(#929): The comment says a later OnKill registration will overwrite this one,
            // but IOperationCallback explicitly promises callbacks won't override, and
            // TweenOperationCallback.OnKill uses Delegate.Combine to append callbacks.
            // This note is misleading; consider removing it or clarifying that this
            // limitation applies to raw DOTween Tween.OnKill usage (not the project's
            // IOperationCallback wrapper).
            IOperationCallback<TweenCallback> result = morphism.AnimateTo((target, null), ToDuration(factor));
            // Note: Whenever OnComplete is called, OnKill is called immediately afterward.
            // On the other hand, OnComplete will be called only when the tween completed
            // successfully, while OnKill will also be called when the tween is killed or
            // the tween's object is destroyed. Hence, it is sufficient to await OnKill.
            // IMPORTANT NOTE: If we register on OnKill here, a later register on OnKill
            // will be overwrite this registration here. There can be only at most one
            // registration on OnKill at a time.
            result.OnKill(AdjustCollider);
            return result;

            /// The edge moved, hence, its collider must be adjusted.
            /// At the end of the animation, we may need to adjust the collider
            /// of the edge via <see cref="spline"/>. We may want to add a little
            /// delay to avoid unnecessary updates to the collider. For instance,
            /// in the reflexion city the user grabs a node and its edges move along
            /// with it over a longer period of time until the node reaches its
            /// final destination.
            void AdjustCollider()
            {
                spline.AdjustCollider();
            }
        }

        /// <summary>
        /// Construct the edge from subsplines.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> Construct(float factor = 1)
        {
            return construction.AnimateTo(true, ToDuration(factor)).OnComplete(() => EnableCollider(true));
        }

        /// <summary>
        /// Enables/disables the collider of the <see cref="spline"/> depending on <paramref name="enableCollider"/>.
        /// </summary>
        /// <param name="enableCollider">Whether to enable the collider. Will be disabled if this is false.</param>
        private void EnableCollider(bool enableCollider)
        {
            spline.IsSelectable = enableCollider;
        }

        /// <summary>
        /// Destruct the edge from subsplines.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> Destruct(float factor = 1)
        {
            return construction.AnimateTo(false, ToDuration(factor)).OnComplete(() => EnableCollider(false));
        }

        /// <summary>
        /// Show the edge, revealing it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to reveal the edge.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation.</returns>
        public IOperationCallback<Action> Show(EdgeAnimationKind animationKind, float factor = 1)
        {
            return ShowOrHide(true, animationKind, factor);
        }

        /// <summary>
        /// Hide the edge, animating it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to hide the edge.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        public IOperationCallback<Action> Hide(EdgeAnimationKind animationKind, float factor = 1)
        {
            return ShowOrHide(false, animationKind, factor);
        }

        /// <summary>
        /// Show or hide the edge, animating it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="show">Whether to show or hide the edge.</param>
        /// <param name="animationKind">In which way to animate the edge.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="animationKind"/>
        /// is unknown.</exception>
        public IOperationCallback<Action> ShowOrHide(bool show, EdgeAnimationKind animationKind, float factor = 1)
        {
            return animationKind switch
            {
                EdgeAnimationKind.None => new DummyOperationCallback<Action>(),
                EdgeAnimationKind.Fading => FadeTo(show ? 1.0f : 0.0f, factor).OnComplete(() => EnableCollider(show)),
                EdgeAnimationKind.Buildup => show ? Construct(factor) : Destruct(factor),
                _ => throw new ArgumentOutOfRangeException(nameof(animationKind), "Unknown edge animation kind supplied.")
            };
        }

        /// <summary>
        /// Enables or disables the data flow animation to indicate edge direction.
        /// </summary>
        /// <param name="enable">Enable or disable animation.</param>
        public void AnimateDataFlow(bool enable = true)
        {
            if (gameObject.TryGetComponentOrLog(out MeshRenderer meshRenderer))
            {
                EdgeMaterial.SetEdgeFlow(meshRenderer.material, enable);
            }
        }

        #endregion

        protected override IEnumerable<Color> AsEnumerable((Color start, Color end) color)
        {
            yield return color.start;
            yield return color.end;
        }

        protected override void OnEnable()
        {
            // Assigned so that the expensive getter isn't called everytime.
            GameObject go = gameObject;
            spline = go.MustGetComponent<SEESpline>();
            base.OnEnable();

            morphism = new MorphismOperation(AnimateToMorphismAction, spline.Spline, null);
            construction = new TweenOperation<bool>(ConstructAction, spline.VisibleSegmentEnd >= 1);
            return;

            SplineMorphism AnimateToMorphismAction((BSpline targetSpline, GameObject temporaryGameObject) s, float d)
            {
                SplineMorphism animator = go.AddOrGetComponent<SplineMorphism>();

                if (animator.IsActive())
                {
                    // A tween already exists, we simply need to change its target.
                    animator.ChangeTarget(s.targetSpline);
                }
                else
                {
                    SEESpline sourceSpline = go.MustGetComponent<SEESpline>();
                    animator.CreateTween(sourceSpline.Spline, s.targetSpline, d)
                            .OnComplete(() =>
                            {
                                if (s.temporaryGameObject != null)
                                {
                                    Destroyer.Destroy(s.temporaryGameObject);
                                }
                            }).Play();
                }

                return animator;
            }

            Tween[] ConstructAction(bool extending, float duration)
            {
                return new Tween[]
                {
                    DOTween.To(() => spline.VisibleSegmentEnd,
                               u => spline.VisibleSegmentEnd = u,
                               extending ? 1.0f : 0.0f,
                               duration).SetEase(Ease.InOutCubic).Play()
                };
            }
        }

        protected override TweenOperation<(Color start, Color end)> InitializeColorOperation()
        {
            return new TweenOperation<(Color start, Color end)>(AnimateToColorAction, spline.GradientColors);

            Tween[] AnimateToColorAction((Color start, Color end) colors, float d)
            {
                Tween startTween = DOTween.To(() => spline.GradientColors.start,
                                              c => spline.GradientColors = (c, spline.GradientColors.end),
                                              colors.start, d);
                Tween endTween = DOTween.To(() => spline.GradientColors.end,
                                            c => spline.GradientColors = (spline.GradientColors.start, c),
                                            colors.end, d);
                return new[] { startTween.Play(), endTween.Play() };
            }
        }

        protected override Tween[] BlinkAction(int count, float duration)
        {
            // If we're interrupting another blinking, we need to make sure the color still has the correct value.
            spline.GradientColors = Color.TargetValue;

            if (count != 0)
            {
                Color newStart = Color.TargetValue.start.Invert();
                Color newEnd = Color.TargetValue.end.Invert();
                float loopDuration = duration / (2 * Mathf.Abs(count));

                Tween startTween = DOTween.To(() => spline.GradientColors.start,
                                              c => spline.GradientColors = (c, spline.GradientColors.end),
                                              newStart, loopDuration);
                Tween endTween = DOTween.To(() => spline.GradientColors.end,
                                            c => spline.GradientColors = (spline.GradientColors.start, c),
                                            newEnd, loopDuration);
                return new[] { startTween, endTween }.Select(x => x.SetEase(Ease.Linear).SetLoops(2 * count, LoopType.Yoyo).Play()).ToArray();
            }
            else
            {
                return new Tween[] { };
            }
        }

        protected override (Color start, Color end) ModifyColor((Color start, Color end) color, Func<Color, Color> modifier)
        {
            return (modifier(color.start), modifier(color.end));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            morphism.KillAnimator();
            morphism = null;
            construction.KillAnimator();
            construction = null;
        }
    }
}
