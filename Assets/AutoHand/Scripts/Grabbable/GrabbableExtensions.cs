using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    public struct SaveRigidbodyData {
        GameObject origin;
        float mass;
        float angularDrag;
        float drag;
        bool useGravity;
        bool isKinematic;
        RigidbodyInterpolation interpolation;
        CollisionDetectionMode collisionDetectionMode;
        RigidbodyConstraints constraints;

        public SaveRigidbodyData(Rigidbody from, bool removeBody = true) {
            origin = from.gameObject;
            mass = from.mass;
            drag = from.drag;
            angularDrag = from.angularDrag;
            useGravity = from.useGravity;
            isKinematic = from.isKinematic;
            interpolation = from.interpolation;
            collisionDetectionMode = from.collisionDetectionMode;
            constraints = from.constraints;
            if(removeBody)
                GameObject.Destroy(from);
        }

        public Transform GetOrigin() {
            return origin.transform;
        }

        public bool IsSet() {
            return origin != null;
        }

        public Rigidbody ReloadRigidbody() {
            if(origin != null) {
                if(origin.CanGetComponent<Rigidbody>(out var currBody))
                    return currBody;
                var from = origin.AddComponent<Rigidbody>();
                if(from != null) {
                    from.mass = mass;
                    from.drag = drag;
                    from.angularDrag = angularDrag;
                    from.useGravity = useGravity;
                    from.isKinematic = isKinematic;
                    from.interpolation = interpolation;
                    from.collisionDetectionMode = collisionDetectionMode;
                    from.constraints = constraints;
                    origin = null;
                    return from;
                }
            }
            return null;
        }
    }


    public static class GrabbableExtensions {


    }
}