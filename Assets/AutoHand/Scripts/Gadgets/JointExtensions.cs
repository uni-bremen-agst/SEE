using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JointExtensions{
    public static Vector3 Angles(this ConfigurableJoint joint){
        float to180(float v) {
            if (v > 180) {
                v = v - 360;
            }
            return v;
        }

        Quaternion jointBasis = Quaternion.LookRotation(joint.secondaryAxis, Vector3.Cross(joint.axis, joint.secondaryAxis));
        Quaternion jointBasisInverse = Quaternion.Inverse(jointBasis);
        Vector3 rotation;
        if(joint.connectedBody != null) rotation = (jointBasisInverse * Quaternion.Inverse(joint.connectedBody.rotation) * joint.GetComponent<Rigidbody>().transform.rotation * jointBasis).eulerAngles;
        else rotation =  (jointBasisInverse * joint.GetComponent<Rigidbody>().transform.rotation * jointBasis).eulerAngles;
        return new Vector3(to180(rotation.x), to180(rotation.z),to180(rotation.y));
    }
}
