using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand {
    public delegate void CollisionEvent(GameObject from);

    public class CollisionTracker : MonoBehaviour {

        public bool disableCollisionTracking = false;
        public bool disableTriggersTracking = false;

        public event CollisionEvent OnCollisionFirstEnter;
        public event CollisionEvent OnCollisionLastExit;
        public event CollisionEvent OnTriggerFirstEnter;
        public event CollisionEvent OnTriggeLastExit;

        public int collisionCount { get { return collisionObjects.Count; } }
        public int triggerCount { get { return triggerObjects.Count; } }

        public List<GameObject> triggerObjects { get; protected set; } = new List<GameObject>();
        public List<int> triggerObjectsCount { get; protected set; } = new List<int>();

        public List<GameObject> collisionObjects { get; protected set; } = new List<GameObject>();
        public List<int> collisionObjectsCount { get; protected set; } = new List<int>();

        public List<Collision> collisions { get; protected set; } = new List<Collision>();



        public void Clear() {
            triggerObjects.Clear();
            triggerObjectsCount.Clear();
            collisionObjects.Clear();
            collisionObjectsCount.Clear();
        }

        protected virtual void OnDisable() {
            for(int i = 0; i < collisionObjects.Count; i++)
                if(collisionObjects[i])
                    OnCollisionLastExit?.Invoke(collisionObjects[i]);

            for(int i = 0; i < triggerObjects.Count; i++)
                if(triggerObjects[i])
                    OnTriggeLastExit?.Invoke(triggerObjects[i]);

            collisionObjects.Clear();
            collisionObjectsCount.Clear();
            triggerObjects.Clear();
            triggerObjectsCount.Clear();
        }

        private void FixedUpdate() {
            CheckCollisions();
        }

        private void CheckCollisions() {
            if(!disableCollisionTracking) {
                for(int i = 0; i < collisionObjects.Count; i++) {
                    if(collisionObjects[i] == null) {
                        collisionObjects.RemoveAt(i);
                        collisionObjectsCount.RemoveAt(i);
                    }
                    else if(!collisionObjects[i].activeInHierarchy) {
                        OnCollisionLastExit?.Invoke(collisionObjects[i]);
                        collisionObjects.RemoveAt(i);
                        collisionObjectsCount.RemoveAt(i);
                    }
                }
            }

            if(!disableTriggersTracking) {
                for(int i = 0; i < triggerObjects.Count; i++) {
                    if(triggerObjects[i] == null) {
                        triggerObjects.RemoveAt(i);
                        triggerObjectsCount.RemoveAt(i);
                    }
                    else if(!triggerObjects[i].activeInHierarchy) {
                        OnTriggeLastExit?.Invoke(triggerObjects[i]);
                        triggerObjects.RemoveAt(i);
                        triggerObjectsCount.RemoveAt(i);
                    }
                }
            }
        }

        protected virtual void OnCollisionEnter(Collision collision) {
            if(!disableCollisionTracking) {
                collisions.Add(collision);
                if(!collisionObjects.Contains(collision.collider.gameObject)) {
                    OnCollisionFirstEnter?.Invoke(collision.collider.gameObject);
                    collisionObjects.Add(collision.collider.gameObject);
                    collisionObjectsCount.Add(1);
                }
                else collisionObjectsCount[collisionObjects.IndexOf(collision.collider.gameObject)]++;
            }
        }

        protected virtual void OnCollisionExit(Collision collision) {
            if(!disableCollisionTracking) {
                collisions.Remove(collision);
                if(collisionObjects.Contains(collision.collider.gameObject)) {
                    var index = collisionObjects.IndexOf(collision.collider.gameObject);
                    var count = --collisionObjectsCount[index];
                    if(count == 0) {
                        OnCollisionLastExit?.Invoke(collision.collider.gameObject);

                        collisionObjectsCount.RemoveAt(index);
                        collisionObjects.Remove(collision.collider.gameObject);
                    }
                }
            }
        }


        protected virtual void OnTriggerEnter(Collider other) {
            if(!disableTriggersTracking) {
                if(!triggerObjects.Contains(other.gameObject)) {
                    OnTriggerFirstEnter?.Invoke(other.gameObject);
                    triggerObjects.Add(other.gameObject);
                    triggerObjectsCount.Add(1);
                }
                else triggerObjectsCount[triggerObjects.IndexOf(other.gameObject)]++;
            }
        }

        protected virtual void OnTriggerExit(Collider other) {
            if(!disableTriggersTracking) {
                if(triggerObjects.Contains(other.gameObject)) {
                    var index = triggerObjects.IndexOf(other.gameObject);
                    triggerObjectsCount[index] -= 1;
                    if(triggerObjectsCount[index] == 0) {
                        OnTriggeLastExit?.Invoke(other.gameObject);
                        triggerObjectsCount.RemoveAt(index);
                        triggerObjects.Remove(other.gameObject);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            foreach (var collision in collisions)
            {
                foreach (var contactPoint in collision.contacts)
                {
                    Gizmos.DrawSphere(contactPoint.point, 0.0025f);

                }
            }
        }
    }
}