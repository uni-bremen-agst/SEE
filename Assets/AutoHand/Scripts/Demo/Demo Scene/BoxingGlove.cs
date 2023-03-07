using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo {
    public class BoxingGlove : MonoBehaviour {
        public Rigidbody body;
        public float power = 2f;

        float lastHitTime;
        float delay = 1f;

        public void OnCollisionEnter(Collision collision) {
            if(lastHitTime + delay < Time.fixedTime) {
                collision.rigidbody?.AddForce(body.velocity * power);
                lastHitTime = Time.fixedTime;
            }
        }
    }
}