using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    [RequireComponent(typeof(HingeJoint))]
    public class PhysicsGadgetHingeAngleReader : MonoBehaviour{
        public bool invertValue = false;
        [Tooltip("For objects slightly off center. " +
            "\nThe minimum abs value required to return a value nonzero value\n " +
            "- if playRange is 0.1, you have to move the gadget 10% to get a result")]
        public float playRange = 0.05f; 
        HingeJoint joint;
        protected float value = 0;
        Quaternion startRot;
        Quaternion deltaParentRotation;

        protected virtual void Start(){
            joint = GetComponent<HingeJoint>(); 
            startRot = transform.localRotation;
        }

        /// <summary>Returns a -1 to 1 value representing the hinges angle from min-max</summary>
        public float GetValue() {
            value = joint.angle/(joint.limits.max - joint.limits.min)*2;
            value = invertValue ? -value : value;
            if (Mathf.Abs(value) < playRange)
                value = 0;
            return Mathf.Clamp(value, -1, 1);
        }

        public HingeJoint GetJoint() => joint;
    }
}
