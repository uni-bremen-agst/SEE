using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    [RequireComponent(typeof(Rigidbody))]
    public class AutoGunTarget : MonoBehaviour {
        public GameObject hitDecal;
        public ParticleSystem hitParticle;
        public float hitDecalLifetime;
        public UnityGunHitEvent OnShotEvent;


        Dictionary<GameObject, float> decalLifetimeTracker = new Dictionary<GameObject, float>();
        List<GameObject> decalPool = new List<GameObject>();
        List<ParticleSystem> inactiveParticlePool = new List<ParticleSystem>();
        List<ParticleSystem> activeParticlePool = new List<ParticleSystem>();

        public virtual void OnShot(AutoGun gun, RaycastHit hit){
            OnShotEvent?.Invoke(gun, hit);
             CreateHitParticle(hit);
            CreateHitDecal(hit);
        }

        private void FixedUpdate() {
            CheckDecalLifetime();
            CheckParticlPlaying();

        }

        void CreateHitParticle(RaycastHit hit) {
            if(hitParticle != null) { 
                var newHitParticle = GameObject.Instantiate(hitParticle);
                newHitParticle.transform.position = hit.point;
                newHitParticle.transform.forward = hit.normal;
                activeParticlePool.Add(newHitParticle);
            }
        }
        void CreateHitDecal(RaycastHit hit) {
            if(hitDecal != null) {
                var newHitDecal = GameObject.Instantiate(hitDecal);
                newHitDecal.transform.position = hit.point;
                newHitDecal.transform.forward = hit.normal;
                decalLifetimeTracker.Add(hitDecal, hitDecalLifetime);
            }
        }

        void CheckDecalLifetime() {
            if(decalLifetimeTracker.Count > 0) {
                var decalKeys = new GameObject[decalLifetimeTracker.Count];
                decalLifetimeTracker.Keys.CopyTo(decalKeys, 0);
                foreach(var decal in decalKeys) {
                    decalLifetimeTracker[decal] -= Time.deltaTime;
                    if(decalLifetimeTracker[decal] <= 0) {
                        decal.SetActive(false);
                        decalPool.Add(decal);
                        decalLifetimeTracker.Remove(decal);
                    }
                }
            }
        }


        void CheckParticlPlaying() {
            if(inactiveParticlePool.Count > 0) {
                var playingKeys = new ParticleSystem[activeParticlePool.Count];
                activeParticlePool.CopyTo(playingKeys, 0);
                foreach(var particle in playingKeys) {
                    if(!particle.isPlaying)
                        particle.gameObject.SetActive(false);
                    inactiveParticlePool.Add(particle);
                    activeParticlePool.Remove(particle);
                }
            }
        }
    }
}