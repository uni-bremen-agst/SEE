using DG.Tweening;
using SEE.GO;
using TinySpline;

namespace SEE.Game.Operator
{
    public class EdgeOperator: AbstractOperator
    {

        private readonly MorphismOperation morphismOperation = new MorphismOperation();

        private SEESpline spline;
        
        public void AnimateToSpline(SEESpline target, float duration)
        {
            // We deactivate the target edge first so it's not visible.
            target.gameObject.SetActive(false);
            // We now use the MorphismOperation to actually move the edge.
            morphismOperation.AnimateTo(target, duration);
        }

        private void Awake()
        {
            gameObject.MustGetComponent(out spline);
            morphismOperation.TargetValue = spline;
            morphismOperation.AnimateToAction = (s, d) =>
            {
                SplineMorphism Animator = gameObject.AddOrGetComponent<SplineMorphism>();
                
                if (Animator.IsActive())
                {
                    // A tween already exists, we simply need to change its target.
                    Animator.ChangeTarget(s.Spline);
                } 
                else
                {
                    gameObject.MustGetComponent(out SEESpline sourceSpline);
                    Animator.CreateTween(sourceSpline.Spline, s.Spline, d)
                            .OnComplete(() => Destroy(s.gameObject)).Play();
                }

                return Animator;
            };
        }

        protected class MorphismOperation : Operation<SplineMorphism, SEESpline>
        {
            public override void KillAnimator(bool complete = false)
            {
                if (Animator.IsActive())
                {
                    Animator.tween.Kill();
                    Destroy(Animator);
                    Animator = null;
                }
                base.KillAnimator(complete);
            }

            protected override void ChangeAnimatorTarget(SEESpline newTarget, float duration)
            {
                // No need to kill any old animators, the spline morphism can change its target.
                AnimateToAction(newTarget, duration);
            }
        }
    }
}