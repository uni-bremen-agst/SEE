using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SEE.Game.City;
using SEE.GO;
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
        /// Morph the spline represented by this edge to the given <paramref name="target"/> spline,
        /// destroying the associated game object of <paramref name="target"/> once the animation is complete.
        /// This will also disable the <paramref name="target"/>'s game object immediately so it's invisible.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<TweenCallback> MorphTo(SEESpline target, float factor = 1)
        {
            // We deactivate the target edge first so it's not visible.
            target.gameObject.SetActive(false);
            // We now use the MorphismOperation to actually move the edge.
            return morphism.AnimateTo((target.Spline, target.gameObject), ToDuration(factor));
        }

        /// <summary>
        /// Morph the spline represented by this edge to the given <paramref name="target"/> spline.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<TweenCallback> MorphTo(BSpline target, float factor = 1)
        {
            return morphism.AnimateTo((target, null), ToDuration(factor));
        }

        /// <summary>
        /// Construct the edge from subsplines.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Construct(float factor = 1)
        {
            return construction.AnimateTo(true, ToDuration(factor));
        }

        /// <summary>
        /// Destruct the edge from subsplines.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Destruct(float factor = 1)
        {
            return construction.AnimateTo(false, ToDuration(factor));
        }

        /// <summary>
        /// Show the edge, revealing it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to reveal the edge.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
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
        /// <returns>An operation callback for the requested animation</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="animationKind"/>
        /// is unknown.</exception>
        public IOperationCallback<Action> ShowOrHide(bool show, EdgeAnimationKind animationKind, float factor = 1)
        {
            return animationKind switch
            {
                EdgeAnimationKind.None => new DummyOperationCallback<Action>(),
                EdgeAnimationKind.Fading => FadeTo(show ? 1.0f : 0.0f, factor),
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
            if (enable)
            {
                gameObject.AddOrGetComponent<EdgeDataFlowVisualizer>();
            }
            else
            {
                EdgeDataFlowVisualizer edfv = gameObject.GetComponent<EdgeDataFlowVisualizer>();
                Destroyer.Destroy(edfv);
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
            construction = new TweenOperation<bool>(ConstructAction, spline.SubsplineEndT >= 1);
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
                    DOTween.To(() => spline.SubsplineEndT,
                               u => spline.SubsplineEndT = u,
                               extending ? 1.0f : 0.0f,
                               duration).SetEase(Ease.InOutCubic).Play()
                };
            }
        }

        protected override TweenOperation<(Color start, Color end)> InitializeColorOperation()
        {
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

            return new TweenOperation<(Color start, Color end)>(AnimateToColorAction, spline.GradientColors);
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

        /// <summary>
        /// Implements a data flow visualization to indicate the direction of an edge.
        /// </summary>
        private class EdgeDataFlowVisualizer : MonoBehaviour
        {
            /// <summary>
            /// Maximal count of particles.
            /// </summary>
            private static readonly int maxParticleCount = 12;
            /// <summary>
            /// Minimal distance between particles for the actual particle count.
            /// </summary>
            private static readonly float minParticleDistance = 0.16f;
            /// <summary>
            /// Scale of the particle meshes.
            /// </summary>
            private static readonly Vector3 particleScale = new (0.012f, 0.012f, 0.012f);
            /// <summary>
            /// Color of the particle material.
            /// </summary>
            private static readonly Color particleColor = new (0.06f, 0.81f, 1.0f, 1.0f);
            /// <summary>
            /// Particle speed.
            /// </summary>
            private static readonly float particleSpeed = 50f;

            /// <summary>
            /// The spline the edge is based on.
            /// </summary>
            private SEESpline seeSpline;
            /// <summary>
            /// The coordinates of the edge's vertices.
            /// </summary>
            private Vector3[] vertices;

            /// <summary>
            /// The actual particle count as calculated based on <see cref="minParticleDistance"/>
            /// and capped by <see cref="maxParticleCount"/>.
            /// </summary>
            private int particleCount;
            /// <summary>
            /// The particle game objects.
            /// </summary>
            private GameObject[] particles;
            /// <summary>
            /// The current position of the particles.
            /// </summary>
            private float[] particlePositions;

            /// <summary>
            /// Destroys the particles when the component is destroyed.
            /// </summary>
            private void OnDestroy()
            {
                foreach (GameObject particle in particles)
                {
                    seeSpline.OnRendererChanged -= OnSplineChanged;
                    Destroyer.Destroy(particle);
                }
            }

            /// <summary>
            /// Initializes the particles and fields.
            /// </summary>
            public void Start()
            {
                seeSpline = GetComponent<SEESpline>();
                seeSpline.OnRendererChanged += OnSplineChanged;
                vertices = seeSpline.GenerateVertices();

                particleCount = (int)Mathf.Max(Mathf.Min(GetApproxEdgeLength() / minParticleDistance, maxParticleCount), 1);

                particles = new GameObject[particleCount];
                particlePositions = new float[particleCount];

                float separation = vertices.Length / (float)particleCount;
                for (int i = 0; i < particleCount; i++)
                {
                    particles[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    particles[i].GetComponent<Renderer>().material.color = particleColor;
                    particlePositions[i] = separation * i;
                    particles[i].transform.localScale = particleScale;
                    particles[i].transform.SetParent(transform);
                    particles[i].transform.localPosition = GetPositionOnEdge(particlePositions[i]);
                }
            }

            /// <summary>
            /// Updates the position and color of the vertices.
            /// </summary>
            private void Update()
            {
                for (int i = 0; i < particleCount; i++)
                {
                    particlePositions[i] += particleSpeed * Time.deltaTime;
                    if (particlePositions[i] >= vertices.Length)
                    {
                        particlePositions[i] = 0;
                    }
                    particles[i].transform.localPosition = GetPositionOnEdge(particlePositions[i]);
                }
            }

            /// <summary>
            /// This callback is triggered whenever the spline has changed.
            /// It will then re-calculate <see cref="vertices"/>.
            /// </summary>
            private void OnSplineChanged()
            {
                vertices = seeSpline.GenerateVertices();
            }

            /// <summary>
            /// Calculates the coordinate of the position on the edge by interpolating between two
            /// neighboring vertices.
            /// </summary>
            /// <param name="position">The position between zero and <see cref="vertices"/><c>.Length - 1</c></param>
            /// <returns>The coordinate of the position on the edge.</returns>
            private Vector3 GetPositionOnEdge(float position)
            {
                if (position >= vertices.Length - 1)
                {
                    return vertices[^1]; // last element
                }

                return Vector3.Lerp(vertices[(int)position], vertices[(int)position + 1], position - (int)position);
            }

            /// <summary>
            /// Calculates the approximate length of the edge that is represented by <see cref="vertices"/>.
            /// </summary>
            /// <returns>Approximate edge length.</returns>
            private float GetApproxEdgeLength()
            {
                float length = 0;
                for (int i = 1; i < vertices.Length; i++)
                {
                    length += Vector3.Distance(vertices[i - 1], vertices[i]);
                }
                return length;
            }
        }
    }
}
