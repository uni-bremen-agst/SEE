using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    public class JointBreakStopForce1 : MonoBehaviour
    {

        void OnJointBreak(float breakForce) {
            if(gameObject.CanGetComponent(out Rigidbody body)) {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}