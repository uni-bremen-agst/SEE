using UnityEngine;

namespace Autohand{
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/hand/finger-component")]
    public class Finger : MonoBehaviour{
        [Header("Tips")]
        [Tooltip("This transfrom will represent the tip/stopper of the finger")]
        public Transform tip;
        [Tooltip("This determines the radius of the spherecast check when bending fingers")]
        public float tipRadius = 0.01f;
        [Tooltip("This will offset the fingers bend (0 is no bend, 1 is full bend)")]
        [Range(0, 1f)]
        public float bendOffset;
        public float fingerSmoothSpeed = 1;

        [HideInInspector]
        public float secondaryOffset = 0;
        
        float currBendOffset = 0;
        float bend = 0;

        [SerializeField]
        [HideInInspector]
        Quaternion[] minGripRotPose;

        [SerializeField]
        [HideInInspector]
        Vector3[] minGripPosPose;

        [SerializeField]
        [HideInInspector]
        Quaternion[] maxGripRotPose;

        [SerializeField]
        [HideInInspector]
        Vector3[] maxGripPosPose;
    
        [SerializeField]
        [HideInInspector]
        Transform[] fingerJoints;
        
        float lastHitBend;

        Collider[] results = new Collider[2];



        void Update() {
            SlowBend();
        }



        /// <summary>Forces the finger to a bend until it hits something on the given physics layer</summary>
        /// <param name="steps">The number of steps and physics checks it will make lerping from 0 to 1</param>
        public bool BendFingerUntilHit(int steps, int layermask) {
            ResetBend();
            lastHitBend = 0;

            for(float i = 0; i <= steps / 5f; i++) {
                results[0] = null;
                lastHitBend = i / (steps / 5f);
                for(int j = 0; j < fingerJoints.Length; j++) {
                    fingerJoints[j].localPosition = Vector3.Lerp(minGripPosPose[j], maxGripPosPose[j], lastHitBend);
                    fingerJoints[j].localRotation = Quaternion.Lerp(minGripRotPose[j], maxGripRotPose[j], lastHitBend);
                }
                Physics.OverlapSphereNonAlloc(tip.transform.position, tipRadius, results, layermask, QueryTriggerInteraction.Ignore);

                if(results[0] != null) {
                    lastHitBend = Mathf.Clamp01(lastHitBend);
                    if(i == 0)
                        return true;
                    break;
                }

            }


            lastHitBend -= (5f / steps);
            for(int i = 0; i <= steps / 10f; i++) {
                results[0] = null;
                lastHitBend += (1f / steps);
                for(int j = 0; j < fingerJoints.Length; j++) {
                    fingerJoints[j].localPosition = Vector3.Lerp(minGripPosPose[j], maxGripPosPose[j], lastHitBend);
                    fingerJoints[j].localRotation = Quaternion.Lerp(minGripRotPose[j], maxGripRotPose[j], lastHitBend);
                }
                Physics.OverlapSphereNonAlloc(tip.transform.position, tipRadius, results, layermask, QueryTriggerInteraction.Ignore);


                if(results[0] != null) {
                    bend = lastHitBend;
                    currBendOffset = lastHitBend;
                    lastHitBend = Mathf.Clamp01(lastHitBend);
                    return true;
                }

                if(lastHitBend >= 1) {
                    lastHitBend = Mathf.Clamp01(lastHitBend);
                    return true;
                }
            }



            return false;
        }



        /// <summary>Bends the finger unless its hitting something</summary>
        /// <param name="bend">0 is no bend / 1 is full bend</param>
        public bool UpdateFingerBend(float bend, int layermask) {
            var results = new Collider[]{ null };
            Physics.OverlapSphereNonAlloc(tip.transform.position, tipRadius, results, layermask, QueryTriggerInteraction.Ignore);
            if(this.bend > bend || results[0] == null){
                this.bend = bend;
                for(int i = 0; i < fingerJoints.Length; i++) {
                    fingerJoints[i].localPosition = Vector3.Lerp(minGripPosPose[i], maxGripPosPose[i], currBendOffset+secondaryOffset);
                    fingerJoints[i].localRotation = Quaternion.Lerp(minGripRotPose[i], maxGripRotPose[i], currBendOffset+secondaryOffset);
                }
                return true;
            }
            return false;
        }

        public void UpdateFinger() {
            for(int i = 0; i < fingerJoints.Length; i++) {
                fingerJoints[i].localPosition = Vector3.Lerp(minGripPosPose[i], maxGripPosPose[i], currBendOffset+secondaryOffset);
                fingerJoints[i].localRotation = Quaternion.Lerp(minGripRotPose[i], maxGripRotPose[i], currBendOffset+secondaryOffset);
            }
        }

        public void UpdateFinger(float bend) {
            this.bend = bend;
            for(int i = 0; i < fingerJoints.Length; i++) {
                fingerJoints[i].localPosition = Vector3.Lerp(minGripPosPose[i], maxGripPosPose[i], currBendOffset+secondaryOffset);
                fingerJoints[i].localRotation = Quaternion.Lerp(minGripRotPose[i], maxGripRotPose[i], currBendOffset+secondaryOffset);
            }
        }

        /// <summary>Forces the finger to a bend ignoring physics and offset</summary>
        /// <param name="bend">0 is no bend / 1 is full bend</param>
        public void SetFingerBend(float bend) {
            this.bend = bend;
            for(int i = 0; i < fingerJoints.Length; i++) {
                fingerJoints[i].localPosition = Vector3.Lerp(minGripPosPose[i], maxGripPosPose[i], bend);
                fingerJoints[i].localRotation = Quaternion.Lerp(minGripRotPose[i], maxGripRotPose[i], bend);
            }
        }
        
        /// <summary>Sets the current finger to a bend without interfering with the target</summary>
         /// <param name="bend">0 is no bend / 1 is full bend</param>
        public void SetCurrentFingerBend(float bend) {
            currBendOffset = bend;
            for(int i = 0; i < fingerJoints.Length; i++) {
                fingerJoints[i].localPosition = Vector3.Lerp(minGripPosPose[i], maxGripPosPose[i], bend);
                fingerJoints[i].localRotation = Quaternion.Lerp(minGripRotPose[i], maxGripRotPose[i], bend);
            }
        }


        //This function smooths the finger bend so you can change the grip over a frame and wont be a jump
        void SlowBend(){

            var offsetValue = bendOffset + bend;
            if(currBendOffset != offsetValue)
                currBendOffset = Mathf.MoveTowards(currBendOffset, offsetValue, 6*fingerSmoothSpeed * Time.deltaTime);
        }
    



        [ContextMenu("ResetBend")]
        public void ResetBend() {
            for(int i = 0; i < fingerJoints.Length; i++) {
                fingerJoints[i].localPosition = minGripPosPose[i];
                fingerJoints[i].localRotation = minGripRotPose[i];
            }
        }

        [ContextMenu("Grip")]
        public void Grip() {
            for(int i = 0; i < fingerJoints.Length; i++) {
                fingerJoints[i].localPosition = maxGripPosPose[i];
                fingerJoints[i].localRotation = maxGripRotPose[i];
            }
        }


        /// <summary>Returns the bend the finger ended with from the last BendFingerUntilHit() call</summary>
        public float GetLastHitBend() {
            return lastHitBend;
        }
    

        [ContextMenu("Set Open Finger Pose")]
        public void SetMinPose(){
            int GetKidsCount(Transform obj, ref int count) {
                if(obj != tip){
                    count++;
                    for(int k = 0; k < obj.childCount; k++) {
                        GetKidsCount(obj.GetChild(k), ref count);
                    }
                }
                return count;

            }

            int points = 0;
            GetKidsCount(transform, ref points);
            minGripPosPose = new Vector3[points];
            minGripRotPose = new Quaternion[points];
            fingerJoints = new Transform[points];
            
            int i = 0;
            AssignChildrenPose(transform, ref i);
            void AssignChildrenPose(Transform obj, ref int index) {
                if(obj != tip){
                    AssignPoint(index, obj.localPosition, obj.localRotation, obj);
                    index++;
                    for(int j = 0; j < obj.childCount; j++) {
                        AssignChildrenPose(obj.GetChild(j), ref index);
                    }
                }
            }

            void AssignPoint(int point, Vector3 pos, Quaternion rot, Transform joint) {
                minGripPosPose[point] = pos;
                minGripRotPose[point] = rot;
                fingerJoints[point] = joint;
            }
        }


    
        [ContextMenu("Set Closed Finger Pose")]
        public void SetMaxPose(){
            int GetKidsCount(Transform obj, ref int count) {
                if(obj != tip){
                    count++;
                    for(int k = 0; k < obj.childCount; k++) {
                        GetKidsCount(obj.GetChild(k), ref count);
                    }
                }
                return count;
            }

            int points = 0;
            GetKidsCount(transform, ref points);
            maxGripPosPose = new Vector3[points];
            maxGripRotPose = new Quaternion[points];
            fingerJoints = new Transform[points];

            int i = 0;
            AssignChildrenPose(transform, ref i);
            void AssignChildrenPose(Transform obj, ref int index){
                if(obj != tip){
                    AssignPoint(index, obj.localPosition, obj.localRotation, obj);
                    index++;
                    for(int j = 0; j < obj.childCount; j++) {
                        AssignChildrenPose(obj.GetChild(j), ref index);
                    }
                }
            }

            void AssignPoint(int point, Vector3 pos, Quaternion rot, Transform joint) {
                maxGripPosPose[point] = pos;
                maxGripRotPose[point] = rot;
                fingerJoints[point] = joint;
            }
        }


        public void CopyPose(Finger finger)
        {
            maxGripPosPose = new Vector3[finger.maxGripPosPose.Length];
            finger.maxGripPosPose.CopyTo(maxGripPosPose, 0);
            maxGripRotPose = new Quaternion[finger.maxGripRotPose.Length];
            finger.maxGripRotPose.CopyTo(maxGripRotPose, 0);

            minGripPosPose = new Vector3[finger.minGripPosPose.Length];
            finger.minGripPosPose.CopyTo(minGripPosPose, 0);
            minGripRotPose = new Quaternion[finger.minGripRotPose.Length];
            finger.minGripRotPose.CopyTo(minGripRotPose, 0);

            fingerJoints = new Transform[finger.fingerJoints.Length];
            finger.fingerJoints.CopyTo(fingerJoints, 0);

        }
        
        public bool IsMinPoseSaved()
        {
            return minGripPosPose.Length != 0;
        }
        public bool IsMaxPoseSaved()
        {
            return maxGripPosPose.Length != 0;
        }

        public float GetCurrentBend() {
            return currBendOffset+secondaryOffset;
        }
    

        private void OnDrawGizmos() {
            if(tip == null)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(tip.transform.position, tipRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            DrawSphereBetweenChild(transform);
            void DrawSphereBetweenChild(Transform transform){
                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTransform = transform.GetChild(i);
                    if (childTransform.TryGetComponent(out CapsuleCollider cap))
                    {
                        Gizmos.DrawWireSphere(Vector3.Lerp(transform.position, cap.bounds.center, 0.5f), tipRadius);
                    }

                    DrawSphereBetweenChild(childTransform);
                }
            }
        }
    }
}
