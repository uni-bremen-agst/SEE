using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand {
    public enum MagnetEffect {
        Attractive,
        Repulsive
    }

    [Serializable]
    public class UnityMagneticEvent : UnityEvent<MagneticSource, MagneticBody> { }

    [Serializable]
    [RequireComponent(typeof(Rigidbody)), HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/magnetic-forces")]
    public class MagneticSource : MonoBehaviour {
        public MagnetEffect magneticEffect;
        public float strength = 10f;
        public float radius = 4f;
        public ForceMode forceMode = ForceMode.Force;
        public AnimationCurve forceDistanceCurce = AnimationCurve.Linear(0, 0, 1, 1);
        public int magneticIndex = 0;
        public UnityMagneticEvent magneticEnter;
        public UnityMagneticEvent magneticExit;

        Rigidbody body;
        List<MagneticBody> magneticBodies = new List<MagneticBody>();
        float radiusScale;
        private void Start() {
            body = GetComponent<Rigidbody>();
            radiusScale = transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y;
            radiusScale = radiusScale < transform.lossyScale.z ? radiusScale : transform.lossyScale.z;
        }

        private void FixedUpdate() {
            foreach(var magneticBody in magneticBodies) {
                var distance = Vector3.Distance(transform.position, magneticBody.transform.position);
                if(distance < radius * radiusScale) {
                    var distanceValue = distance / (radius * radiusScale + 0.0001f);
                    var distanceMulti = forceDistanceCurce.Evaluate(distanceValue) * magneticBody.strengthMultiplyer * strength;
                    distanceMulti *= magneticEffect == MagnetEffect.Repulsive ? -1 : 1;
                    magneticBody.body.AddForce((transform.position - magneticBody.transform.position).normalized * distanceMulti, forceMode);
                }
            }
        }

        private void OnTriggerEnter(Collider other) {
            if(other.attachedRigidbody != null && other.CanGetComponent<MagneticBody>(out var magnetBody)) {
                if(!magneticBodies.Contains(magnetBody) && magnetBody.magneticIndex == magneticIndex) {
                    magneticBodies.Add(magnetBody);
                    magneticEnter?.Invoke(this, magnetBody);
                    magnetBody.magneticEnter?.Invoke(this, magnetBody);
                }

            }
        }

        private void OnTriggerExit(Collider other) {
            if(other.attachedRigidbody != null && other.CanGetComponent<MagneticBody>(out var magnetBody)) {
                if(magneticBodies.Contains(magnetBody)) {
                    magneticBodies.Remove(magnetBody);
                    magneticExit?.Invoke(this, magnetBody);
                    magnetBody.magneticExit?.Invoke(this, magnetBody);
                }
            }
        }


        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.blue;
            var radiusScale = transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y;
            radiusScale = radiusScale < transform.lossyScale.z ? radiusScale : transform.lossyScale.z;
            Gizmos.DrawWireSphere(transform.position, radius * radiusScale);
        }
    }
}