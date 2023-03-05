using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand{
    [RequireComponent(typeof(ConfigurableJoint))]
    public class PhysicsGadgetConfigurableLimitReader : MonoBehaviour{
        public bool invertValue = false;
        [Tooltip("For objects slightly off center. " +
            "\nThe minimum abs value required to return a value nonzero value\n " +
            "- if playRange is 0.1, you have to move the gadget 10% to get a result")]
        public float playRange = 0.025f;
        protected ConfigurableJoint joint;

        protected Vector3 axisPos;
        float value;
        Vector3 limitAxis;

        protected virtual void Start(){
            joint = GetComponent<ConfigurableJoint>();
            limitAxis = new Vector3(joint.xMotion == ConfigurableJointMotion.Locked ? 0 : 1, joint.yMotion == ConfigurableJointMotion.Locked ? 0 : 1, joint.zMotion == ConfigurableJointMotion.Locked ? 0 : 1);
            axisPos = Vector3.Scale(transform.localPosition, limitAxis);
        }


        /// <summary>Returns a -1 to 1 value that represents the point of the slider</summary>
        public float GetValue() {
            bool positive = true;
            var currPos = Vector3.Scale(transform.localPosition, limitAxis);
            if(axisPos.x < currPos.x || axisPos.y < currPos.y || axisPos.z < currPos.z)
                positive = false;

            if(invertValue)
                positive = !positive;

            value = Vector3.Distance(axisPos, currPos)/joint.linearLimit.limit;

            if(!positive) value *= -1;

            if (Mathf.Abs(value) < playRange)
                value = 0;
            return Mathf.Clamp(value, -1f, 1f);
        }

        public ConfigurableJoint GetJoint() => joint;
    }
}
