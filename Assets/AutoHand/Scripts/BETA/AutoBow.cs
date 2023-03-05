using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Autohand;

namespace Autohand.Demo {
    public class AutoBow : MonoBehaviour
    {
        public AutoAnimation bowAnimation;
        public Grabbable bowHandleGrabbable;
        public PlaceJoint arrowPoint;
        [Space]
        public float drawbackRange = 0.3f;
        public float drawbackSpring = 100f;
        public float drawbackDamper = 10f;
        public float tolerance = 0.005f;
        [Space]
        public Vector3 arrowForceDirection = Vector3.forward;
        public float arrowForce = 1f;
        public float arrowImpactForceMultiplier = 1f;
        public AnimationCurve arrowForceCurve = AnimationCurve.Linear(0, 0, 1, 1);

        protected Grabbable arrow;
        protected SpringJoint arrowJoint;

        int placePointIndex = 0;

        public void Start()
        {
            placePointIndex = bowAnimation.GetTransformIndex(arrowPoint.transform);
            arrowPoint.enabled = false;
            arrowPoint.OnPlaceEvent += OnArrowPlace;
            bowHandleGrabbable.OnGrabEvent += OnBowHandleGrab;
            bowHandleGrabbable.OnReleaseEvent += OnBowHandleRelease;

        }

        public void Update()
        {
            BowStringAnimation();
        }

        float arrowPointValue = 0.5f;
        public void BowStringAnimation()
        {

            if (arrowPoint.placedObject != null)
            {
                var arrowHand = arrowPoint.placedObject.GetHeldBy()[0];
                arrowPointValue = 0.5f;
                float closestPoint = 0.5f;
                for (int i = 0; i < 20; i++) {
                    var postitionA = bowAnimation.GetTransformPositionAtPoint(placePointIndex, closestPoint + arrowPointValue / 2f);
                    var postitionB = bowAnimation.GetTransformPositionAtPoint(placePointIndex, closestPoint - arrowPointValue / 2f);
                    var postitionC = bowAnimation.GetTransformPositionAtPoint(placePointIndex, closestPoint);
                    var distanceA = Vector3.Distance(postitionA, arrowPoint.placedObject.transform.position);
                    var distanceB = Vector3.Distance(postitionB, arrowPoint.placedObject.transform.position);
                    var distanceC = Vector3.Distance(postitionC, arrowPoint.placedObject.transform.position);

                    if(distanceC < distanceA && distanceC < distanceB) { }
                    else if(distanceA < distanceB)
                        closestPoint += arrowPointValue / 2f;
                    else
                        closestPoint -= arrowPointValue / 2f;

                    arrowPointValue /= 2f;
                }

                bowAnimation.SetAnimation(closestPoint);
            }
            else if(arrowPointValue != 0) {
                arrowPointValue = 0;
                bowAnimation.SetAnimation(arrowPointValue);
            }
        }

        public void OnBowHandleGrab(Hand hand, Grabbable grab)
        {
            arrowPoint.enabled = true;
        }

        public void OnBowHandleRelease(Hand hand, Grabbable grab)
        {
            arrowPoint.enabled = false;
        }



        public void OnArrowPlace(PlacePoint point, Grabbable grab)
        {
            point.placedObject.OnReleaseEvent += OnArrowRelease;
            if(bowHandleGrabbable.HeldCount() > 0)
                point.placedObject.IgnoreHand(bowHandleGrabbable.GetHeldBy()[0], true);
            point.placedObject.ignoreReleaseTime = 1f;
        }


        public void OnArrowRelease(Hand hand, Grabbable grab)
        {
            if (arrowPoint.placedObject != null)
            {
                //arrowPoint.Destroyjoint();
                arrowPoint.Remove(arrowPoint.placedObject);
                if (bowHandleGrabbable.HeldCount() > 0){
                    bowAnimation.SetAnimation(0);
                    var bowHand = bowHandleGrabbable.GetHeldBy()[0];
                    EnableCollisionDelay(3f, grab, bowHandleGrabbable.GetHeldBy()[0], arrowPoint);
                }

                AutoArrow arrow;
                if (grab.body.TryGetComponent<AutoArrow>(out arrow))
                {
                    arrow.FireArrow(arrowForceCurve.Evaluate(arrowPointValue)*arrowForce, grab, this);
                }
                else
                {
                    arrow = grab.body.gameObject.AddComponent<AutoArrow>();
                    arrow.FireArrow(arrowForceCurve.Evaluate(arrowPointValue) * arrowForce, grab, this);
                }

            }
        }

        IEnumerator EnableCollisionDelay(float delay, Grabbable grab, Hand hand, PlacePoint placePoint)
        {
            var preDrag = grab.body.angularDrag;
            bowHandleGrabbable.IgnoreGrabbableCollisionUntilNone(grab);
            placePoint.dontAllows.Add(grab);
            yield return new WaitForSeconds(delay);
            grab.IgnoreHand(hand, false);
            placePoint.dontAllows.Remove(grab);

        }
    }
}