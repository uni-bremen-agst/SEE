using System;
using UnityEngine;

namespace SEE
{
    public class CollisionEventHandler : MonoBehaviour
    {
        public Action<CollisionEventHandler, Collision> onCollisionEnter;
        public Action<CollisionEventHandler, Collision> onCollisionExit;
        public Action<CollisionEventHandler, Collision> onCollisionStay;
        public Action<CollisionEventHandler, Collider> onTriggerEnter;
        public Action<CollisionEventHandler, Collider> onTriggerExit;
        public Action<CollisionEventHandler, Collider> onTriggerStay;

        private bool destroyRigidbodyOnDestroy;

        private void Awake()
        {
            if (gameObject.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                destroyRigidbodyOnDestroy = true;
            }
            else
            {
                destroyRigidbodyOnDestroy = false;
            }
        }

        private void OnDestroy()
        {
            if (destroyRigidbodyOnDestroy)
            {
                Rigidbody rigidbody = GetComponent<Rigidbody>();
                if (rigidbody)
                {
                    Destroy(rigidbody);
                }
            }
        }

        private void OnCollisionEnter(Collision other) => onCollisionEnter?.Invoke(this, other);
        private void OnCollisionExit(Collision other) => onCollisionExit?.Invoke(this, other);
        private void OnCollisionStay(Collision other) => onCollisionStay?.Invoke(this, other);
        private void OnTriggerEnter(Collider other) => onTriggerEnter?.Invoke(this, other);
        private void OnTriggerExit(Collider other) => onTriggerExit?.Invoke(this, other);
        private void OnTriggerStay(Collider other) => onTriggerStay?.Invoke(this, other);
    }
}
