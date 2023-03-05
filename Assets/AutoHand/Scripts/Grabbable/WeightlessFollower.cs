using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    [DefaultExecutionOrder(-5)]
    public class WeightlessFollower : MonoBehaviour {
        [HideInInspector]
        public Transform follow1 = null;
        [HideInInspector]
        public Transform follow2 = null;
        [HideInInspector]
        public Hand hand1 = null;
        [HideInInspector]
        public Hand hand2 = null;

        public Dictionary<Hand, Transform> heldMoveTo = new Dictionary<Hand, Transform>();

        [HideInInspector]
        public float followPositionStrength = 30;
        [HideInInspector]
        public float followRotationStrength = 30;

        [HideInInspector]
        public float maxVelocity = 5;

        [HideInInspector]
        public Grabbable grab;

        Transform _pivot = null;
        public Transform pivot {
            get {
                if(!gameObject.activeInHierarchy)
                    return null;

                if(_pivot == null) {
                    _pivot = new GameObject().transform;
                    _pivot.parent = transform.parent;
                    _pivot.name = "WEIGHTLESS PIVOT";
                }

                return _pivot;
            }
        }

        internal Rigidbody body;
        Transform moveTo = null;

        float startMass;
        float startDrag;
        float startAngleDrag;
        float startHandMass;
        float startHandDrag;
        float startHandAngleDrag;
        bool useGravity;


        Vector3 lastPos;
        Vector3[] velocities = new Vector3[3];
        Vector3 velocity {
            get {
                var vel = Vector3.zero;
                for(int i = 0; i < velocities.Length; i++) {
                    vel += velocities[i];
                }

                return vel / velocities.Length;
            }
        }



        Vector3 lastRotation;
        Vector3[] angularVelocities = new Vector3[3];
        Vector3 angularVelocity {
            get {
                var vel = Vector3.zero;
                for(int i = 0; i < angularVelocities.Length; i++) {
                    vel += angularVelocities[i];
                }

                return vel / angularVelocities.Length;
            }
        }


        public void Start() {
            if(body == null)
                body = GetComponent<Rigidbody>();

            if(startMass == 0) {
                startMass = body.mass;
                startDrag = body.drag;
                startAngleDrag = body.angularDrag;
                useGravity = body.useGravity;
            }
        }








        public virtual void Set(Hand hand, Grabbable grab) {
            if (body == null)
                body = grab.body;

            if(!heldMoveTo.ContainsKey(hand)) {
                heldMoveTo.Add(hand, new GameObject().transform);
                heldMoveTo[hand].name = "HELD FOLLOW POINT";
            }

            var tempTransform = AutoHandExtensions.transformRuler;
            tempTransform.position = hand.transform.position;
            tempTransform.rotation = hand.transform.rotation;

            var tempTransformChild = AutoHandExtensions.transformRulerChild;
            tempTransformChild.position = grab.transform.position;
            tempTransformChild.rotation = grab.transform.rotation;

            if(grab.maintainGrabOffset) {
                tempTransform.position = hand.follow.position + hand.grabPositionOffset;
                tempTransform.rotation = hand.follow.rotation * hand.grabRotationOffset;
            }
            else {
                tempTransform.position = hand.follow.position;
                tempTransform.rotation = hand.follow.rotation;
            }

            heldMoveTo[hand].parent = hand.moveTo;
            heldMoveTo[hand].position = tempTransformChild.position;
            heldMoveTo[hand].rotation = tempTransformChild.rotation;


            if(follow1 == null) {
                follow1 = heldMoveTo[hand];
                hand1 = hand;
            }
            else if(follow2 == null) {
                follow2 = heldMoveTo[hand];
                hand2 = hand;
                pivot.parent = body.transform;
                pivot.position = Vector3.Lerp(hand1.handGrabPoint.position, hand2.handGrabPoint.position, 0.5f);
                pivot.rotation = Quaternion.LookRotation((hand1.handGrabPoint.position - hand2.handGrabPoint.position).normalized, 
                                 Vector3.Lerp(hand1.handGrabPoint.up, hand2.handGrabPoint.up, 0.5f));
            }


            if (startMass == 0) {
                startMass = body.mass;
                startDrag = body.drag;
                startAngleDrag = body.angularDrag;
                useGravity = body.useGravity;
            }


            startHandMass = hand.body.mass;
            startHandDrag = hand.startDrag;
            startHandAngleDrag = hand.startAngularDrag;

            body.mass = hand.body.mass;
            body.drag = hand.startDrag;
            body.angularDrag = hand.startAngularDrag;
            body.useGravity = false;

            hand.body.mass = 0f;
            hand.body.angularDrag = 0;
            hand.body.drag = 0;

            followPositionStrength = hand.followPositionStrength;
            followRotationStrength = hand.followRotationStrength;
            maxVelocity = grab.maxHeldVelocity;
            this.grab = grab;

            if(moveTo == null) {
                moveTo = new GameObject().transform;
                moveTo.name = gameObject.name + " FOLLOW POINT";
                moveTo.parent = AutoHandExtensions.transformParent;
            }

            hand.OnReleased += OnHandReleased;
        }


        void OnHandReleased(Hand hand, Grabbable grab){
            RemoveFollow(hand, heldMoveTo[hand]);
            hand.body.mass = startHandMass;
            hand.body.drag = startHandDrag;
            hand.body.angularDrag = startHandAngleDrag;
        }

        int velI = 0;
        public virtual void FixedUpdate() {
            if(follow1 == null)
                return;

            //Calls physics movements
            if (grab.ignoreWeight) {

                foreach(var hand in heldMoveTo) {
                    hand.Key.transform.position = hand.Key.handGrabPoint.position;
                    hand.Key.body.position = hand.Key.handGrabPoint.position;
                    hand.Key.transform.rotation = hand.Key.handGrabPoint.rotation;
                    hand.Key.body.rotation = hand.Key.handGrabPoint.rotation;
                    hand.Key.SetMoveTo();
                }

                MoveTo(Time.fixedDeltaTime);
                TorqueTo(Time.fixedDeltaTime);

                if(CollisionCount() > 0)
                    noCollisionFrames = 0;
                else
                    noCollisionFrames++;

                velI = (velI++) % velocities.Length;
                angularVelocities[velI] = (moveTo.rotation.eulerAngles - lastRotation)/Time.fixedDeltaTime;
                lastRotation = moveTo.rotation.eulerAngles;

                velocities[velI] = (moveTo.position - lastPos) / Time.fixedDeltaTime;
                lastPos = moveTo.position;

            }
        }


        protected virtual void Update() {
            if(grab.ignoreWeight) {
                foreach(var hand in heldMoveTo) {
                    hand.Key.transform.position = hand.Key.handGrabPoint.position;
                    hand.Key.body.position = hand.Key.handGrabPoint.position;
                    hand.Key.transform.rotation = hand.Key.handGrabPoint.rotation;
                    hand.Key.body.rotation = hand.Key.handGrabPoint.rotation;
                    hand.Key.SetMoveTo();
                }

            }

            if(grab.HeldCount() == 0)
                Destroy(this);
        }


        protected void SetMoveTo() {
            if(follow1 == null || moveTo == null)
                return;


            if(follow2 != null) {
                moveTo.position = Vector3.Lerp(hand1.moveTo.position, hand2.moveTo.position, 0.5f);
                moveTo.rotation = Quaternion.LookRotation((hand1.moveTo.position - hand2.moveTo.position).normalized,
                                 Vector3.Lerp(hand1.moveTo.up, hand2.moveTo.up, 0.5f));
                moveTo.position -= pivot.position - pivot.parent.transform.position;
                moveTo.rotation *= Quaternion.Inverse(pivot.localRotation);
            }
            else {
                moveTo.position = follow1.position;//Vector3.MoveTowards(transform.position, follow1.position, 0.05f);
                moveTo.rotation = follow1.rotation;
            }
        }

        private void OnDrawGizmos() {
            if(follow2 != null) {
                Gizmos.DrawSphere(hand1.moveTo.position, 0.02f);
                Gizmos.DrawSphere(hand2.moveTo.position, 0.02f);
                Gizmos.DrawLine(hand1.moveTo.position, hand2.moveTo.position);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(hand1.handGrabPoint.position, hand2.handGrabPoint.position);
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(Vector3.Lerp(hand1.moveTo.position, hand2.moveTo.position, 0.5f), Vector3.Lerp(hand1.moveTo.up, hand2.moveTo.up, 0.5f));
            }
        }


        protected bool ignoreMoveFrame;
        private int noCollisionFrames;

        /// <summary>Moves the hand to the controller position using physics movement</summary>
        protected virtual void MoveTo(float deltaTime) {
            if(followPositionStrength <= 0 || moveTo == null)
                return;

            SetMoveTo();



            var movePos = moveTo.position;
            var distance = Vector3.Distance(movePos, transform.position);

            distance = Mathf.Clamp(distance, 0, 0.5f);

            SetVelocity(0.55f);


            void SetVelocity(float minVelocityChange) {
                var velocityClamp = grab.maxHeldVelocity;

                Vector3 vel = (movePos - transform.position).normalized * followPositionStrength * distance * Time.fixedDeltaTime * 60;
                vel.x = Mathf.Clamp(vel.x, -velocityClamp, velocityClamp);
                vel.y = Mathf.Clamp(vel.y, -velocityClamp, velocityClamp);
                vel.z = Mathf.Clamp(vel.z, -velocityClamp, velocityClamp);

                var towardsVel = new Vector3(
                    Mathf.MoveTowards(body.velocity.x, vel.x, minVelocityChange + Mathf.Abs(body.velocity.x) * Time.fixedDeltaTime * 60),
                    Mathf.MoveTowards(body.velocity.y, vel.y, minVelocityChange + Mathf.Abs(body.velocity.y) * Time.fixedDeltaTime * 60),
                    Mathf.MoveTowards(body.velocity.z, vel.z, minVelocityChange + Mathf.Abs(body.velocity.z) * Time.fixedDeltaTime * 60)
                );
                body.velocity = towardsVel;
            }
        }


        /// <summary>Rotates the hand to the controller rotation using physics movement</summary>
        protected virtual void TorqueTo(float deltaTime) {
            var delta = (moveTo.rotation * Quaternion.Inverse(body.rotation));
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if(float.IsInfinity(axis.x))
                return;

            if(angle > 180f)
                angle -= 360f;

            var multiLinear = Mathf.Deg2Rad * angle * followRotationStrength;
            Vector3 angular = multiLinear * axis.normalized;
            angle = Mathf.Abs(angle);

            body.angularDrag = Mathf.Lerp(startHandDrag + 10, startHandDrag, angle) * Time.fixedDeltaTime * 60;


            body.angularVelocity = new Vector3(
                Mathf.MoveTowards(body.angularVelocity.x, angular.x, followRotationStrength * 3f * Time.fixedDeltaTime * 60),
                Mathf.MoveTowards(body.angularVelocity.y, angular.y, followRotationStrength * 3f * Time.fixedDeltaTime * 60),
                Mathf.MoveTowards(body.angularVelocity.z, angular.z, followRotationStrength * 3f * Time.fixedDeltaTime * 60)
            );

        }


        int CollisionCount() {
            return grab.CollisionCount();
        }

        public void RemoveFollow(Hand hand, Transform follow) {
            hand.OnReleased -= OnHandReleased;

            if(this.follow1 == follow) {
                this.follow1 = null;
                hand1 = null;
            }
            if(follow2 == follow) {
                follow2 = null;
                hand2 = null;
            }

            if(this.follow1 == null && follow2 != null) {
                this.follow1 = follow2;
                this.hand1 = hand2;
                hand2 = null;
                follow2 = null;
            }

            if(this.follow1 == null && follow2 == null && !grab.beingGrabbed) {
                if(body != null) {
                    body.mass = startMass;
                    body.drag = startDrag;
                    body.angularDrag = startAngleDrag;
                    body.useGravity = useGravity;
                }
                Destroy(this);
            }

            heldMoveTo.Remove(hand);
        }

        private void OnDestroy()
        {
            if(moveTo != null)
                Destroy(moveTo.gameObject);

            foreach(var transform in heldMoveTo)
                Destroy(transform.Value.gameObject);

            if (body != null)
            {
                body.mass = startMass;
                body.drag = startDrag;
                body.angularDrag = startAngleDrag;
                body.useGravity = useGravity;
            }
        }


    }


}
