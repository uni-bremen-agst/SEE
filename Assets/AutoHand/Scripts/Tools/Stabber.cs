using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand {
    public delegate void StabEvent(Stabber stabber, Stabbable stab);

    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/stabbing")]
    public class Stabber : MonoBehaviour {
        [Tooltip("Can be left empty/null")]
        public Grabbable grabbable;
        [Header("Stab Settings")]
        public CapsuleCollider stabCapsule;
        [Tooltip("If left empty, will default to grabbable layers")]
        public LayerMask stabbableLayers;
        [Tooltip("The index that must match the stabbables index to allow stabbing")]
        public int stabIndex;
        public int maxStabs = 3;


        [Header("Joint Settings")]
        public Vector3 axis;
        public float limit = float.MaxValue;
        public ConfigurableJointMotion xMotion;
        public ConfigurableJointMotion yMotion;
        public ConfigurableJointMotion zMotion;
        public ConfigurableJointMotion angularXMotion;
        public ConfigurableJointMotion angularYMotion;
        public ConfigurableJointMotion angularZMotion;
        [Space]
        public float positionDampeningMultiplyer = 1;
        public float rotationDampeningMultiplyer = 1;

        [Header("Events")]
        public UnityEvent StartStab;
        public UnityEvent EndStab;

        //Progammer Events <3
        public StabEvent StartStabEvent;
        public StabEvent EndStabEvent;


        List<Stabbable> stabbed;
        List<ConfigurableJoint> stabbedJoints;
        /// <summary>Helps prevent stabbable from being smashed through objects</summary>
        Dictionary<Stabbable, int> stabbedFrames;
        Collider[] resultsNonAlloc;

        const int STABFRAMES = 3;

        Vector3 startPos;
        Quaternion startRot;

        Vector3 lastPos;
        Quaternion lastRot;
        int frames;

        Dictionary<Transform, Transform> originalParents = new Dictionary<Transform, Transform>();

        void Start() {
            stabbedFrames = new Dictionary<Stabbable, int>();
            stabbed = new List<Stabbable>();
            stabbedJoints = new List<ConfigurableJoint>();
            resultsNonAlloc = new Collider[25];
            if(stabbableLayers == 0)
                stabbableLayers = LayerMask.GetMask(Hand.grabbableLayers);

            StartStabEvent += (stabber, stabbable) => { StartStab?.Invoke(); };
            EndStabEvent += (stabber, stabbable) => { EndStab?.Invoke(); };

            startPos = transform.position;
            startRot = transform.rotation;

            gameObject.CanGetComponent(out grabbable);

            StartCoroutine(StartWait());
        }


        //This will keep the stabbables in place for the start stab
        IEnumerator StartWait() {
            for(int i = 0; i < STABFRAMES; i++) {
                transform.position = startPos;
                transform.rotation = startRot;
                yield return new WaitForFixedUpdate();
            }
        }

        private void FixedUpdate() {
            if(transform.position != lastPos || lastRot != transform.rotation) {
                frames = 0;
                lastPos = transform.position;
                lastRot = transform.rotation;
            }
            if(frames < STABFRAMES) {
                CheckStabArea();
                frames++;
            }
        }

        protected virtual void CheckStabArea() {
            Vector3 point1;
            Vector3 point2;
            Vector3 capsuleAxis;
            var height = stabCapsule.height;
            var radius = stabCapsule.radius;

            if(stabCapsule.direction == 0) {
                capsuleAxis = Vector3.right;
                height *= stabCapsule.transform.lossyScale.x;
                radius *= stabCapsule.transform.lossyScale.y > stabCapsule.transform.lossyScale.z ? stabCapsule.transform.lossyScale.y : stabCapsule.transform.lossyScale.z;
            }
            else if(stabCapsule.direction == 1) {
                capsuleAxis = Vector3.up;
                height *= stabCapsule.transform.lossyScale.y;
                radius *= stabCapsule.transform.lossyScale.z > stabCapsule.transform.lossyScale.x ? stabCapsule.transform.lossyScale.z : stabCapsule.transform.lossyScale.x;
            }
            else {
                capsuleAxis = Vector3.forward;
                height *= stabCapsule.transform.lossyScale.z;
                radius *= stabCapsule.transform.lossyScale.y > stabCapsule.transform.lossyScale.x ? stabCapsule.transform.lossyScale.y : stabCapsule.transform.lossyScale.x;
            }

            if(height / 2 <= radius) {
                height = 0;
            }
            else {
                height /= 2;
                height -= radius;
            }

            point1 = stabCapsule.bounds.center + stabCapsule.transform.rotation * capsuleAxis * (height);
            point2 = stabCapsule.bounds.center - stabCapsule.transform.rotation * capsuleAxis * (height);
            Physics.OverlapCapsuleNonAlloc(point1, point2, radius, resultsNonAlloc, stabbableLayers, QueryTriggerInteraction.Ignore);

            List<Stabbable> newStabbed = new List<Stabbable>();

            for(int i = 0; i < resultsNonAlloc.Length; i++) {
                Stabbable tempStab;
                if(resultsNonAlloc[i] != null) {
                    if(resultsNonAlloc[i].CanGetComponent(out tempStab))
                        if(tempStab.gameObject != gameObject)
                            newStabbed.Add(tempStab);
                }
            }

            for(int i = stabbed.Count - 1; i >= 0; i--)
                if(!newStabbed.Contains(stabbed[i]))
                    OnStabbableExit(stabbed[i]);

            if(stabbed.Count < maxStabs)
                for(int i = 0; i < newStabbed.Count; i++)
                    if(!stabbed.Contains(newStabbed[i]) && newStabbed[i].CanStab(this))
                        OnStabbableEnter(newStabbed[i]);

            for(int i = 0; i < resultsNonAlloc.Length; i++)
                resultsNonAlloc[i] = null;

            if(stabbedFrames.Count > 0) {
                var stabFrameKeys = new Stabbable[stabbedFrames.Count];
                stabbedFrames.Keys.CopyTo(stabFrameKeys, 0);
                foreach(var stabFrame in stabFrameKeys)
                    if(!stabbed.Contains(stabFrame) && !newStabbed.Contains(stabFrame))
                        stabbedFrames.Remove(stabFrame);
            }

            newStabbed.Clear();
        }

        protected virtual void OnStabbableEnter(Stabbable stab) {
            if(stabbedFrames.ContainsKey(stab))
                stabbedFrames[stab]++;
            else
                stabbedFrames.Add(stab, 1);

            if(stabbedFrames[stab] < STABFRAMES)
                return;


            stabbed.Add(stab);
            var joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.secondaryAxis = axis;
            joint.connectedBody = stab.body;
            joint.xMotion = xMotion;
            joint.yMotion = yMotion;
            joint.zMotion = zMotion;
            joint.angularXMotion = angularXMotion;
            joint.angularYMotion = angularYMotion;
            joint.angularZMotion = angularZMotion;

            joint.linearLimit = new SoftJointLimit() { limit = this.limit };
            joint.linearLimitSpring = new SoftJointLimitSpring() { damper = stab.positionDamper * positionDampeningMultiplyer };
            joint.xDrive = new JointDrive() { positionDamper = stab.positionDamper * positionDampeningMultiplyer, maximumForce = float.MaxValue };
            joint.yDrive = new JointDrive() { positionDamper = stab.positionDamper * positionDampeningMultiplyer, maximumForce = float.MaxValue };
            joint.zDrive = new JointDrive() { positionDamper = stab.positionDamper * positionDampeningMultiplyer, maximumForce = float.MaxValue };
            joint.slerpDrive = new JointDrive() { positionDamper = stab.positionDamper * positionDampeningMultiplyer };

            joint.angularXLimitSpring = new SoftJointLimitSpring() { damper = stab.rotationDamper * rotationDampeningMultiplyer };
            joint.angularYZLimitSpring = new SoftJointLimitSpring() { damper = stab.rotationDamper * rotationDampeningMultiplyer };
            joint.angularXDrive = new JointDrive() { positionDamper = stab.rotationDamper * rotationDampeningMultiplyer, maximumForce = float.MaxValue };
            joint.angularYZDrive = new JointDrive() { positionDamper = stab.rotationDamper * rotationDampeningMultiplyer, maximumForce = float.MaxValue };
            joint.projectionDistance /= 4f;

            joint.enablePreprocessing = true;
            joint.enableCollision = false;

            Rigidbody jointBody;
            joint.CanGetComponent(out jointBody);
            jointBody.detectCollisions = false;
            jointBody.detectCollisions = true;

            stab.body.WakeUp();
            jointBody.WakeUp();

            stabbedJoints.Add(joint);
            stab.OnStab(this);
            StartStabEvent?.Invoke(this, stab);
            if(stab.parentOnStab && grabbable) {
                grabbable.AddJointedBody(stab.body);
            }
        }

        protected virtual void OnStabbableExit(Stabbable stab) {
            var removeIndex = stabbed.IndexOf(stab);
            stabbed.Remove(stab);
            var joint = stabbedJoints[removeIndex];
            stabbedJoints.RemoveAt(removeIndex);
            Destroy(joint);
            stab.OnEndStab(this);
            stabbedFrames.Remove(stab);
            EndStabEvent?.Invoke(this, stab);
            if(stab.parentOnStab && grabbable) {
                grabbable.RemoveJointedBody(stab.body);
            }
        }

        public List<Stabbable> GetStabbed() {
            return stabbed;
        }

        public int GetStabbedCount() {
            return stabbed.Count;
        }



        void OnDrawGizmosSelected() {
            Vector3 point1;
            Vector3 point2;
            Vector3 capsuleAxis;
            var height = stabCapsule.height;
            var radius = stabCapsule.radius;

            if(stabCapsule.direction == 0) {
                capsuleAxis = Vector3.right;
                height *= stabCapsule.transform.lossyScale.x;
                radius *= stabCapsule.transform.lossyScale.y > stabCapsule.transform.lossyScale.z ? stabCapsule.transform.lossyScale.y : stabCapsule.transform.lossyScale.z;
            }
            else if(stabCapsule.direction == 1) {
                capsuleAxis = Vector3.up;
                height *= stabCapsule.transform.lossyScale.y;
                radius *= stabCapsule.transform.lossyScale.z > stabCapsule.transform.lossyScale.x ? stabCapsule.transform.lossyScale.z : stabCapsule.transform.lossyScale.x;
            }
            else {
                capsuleAxis = Vector3.forward;
                height *= stabCapsule.transform.lossyScale.z;
                radius *= stabCapsule.transform.lossyScale.y > stabCapsule.transform.lossyScale.x ? stabCapsule.transform.lossyScale.y : stabCapsule.transform.lossyScale.x;
            }

            if(height / 2 <= radius) {
                height = 0;
            }
            else {
                height /= 2;
                height -= radius;
            }

            point1 = stabCapsule.bounds.center + stabCapsule.transform.rotation * capsuleAxis * (height);
            point2 = stabCapsule.bounds.center - stabCapsule.transform.rotation * capsuleAxis * (height);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(point1, radius);
            Gizmos.DrawSphere(point2, radius);
        }
    }
}
