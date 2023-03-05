using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    [RequireComponent(typeof(AutoGun))]
    public class AutoGunEffects : MonoBehaviour {


        [AutoSmallHeader("Visual Effects")]
        public bool ignoreMe3;
        [Tooltip("Whether or not racking the slide with a bullet already in the chamber ejects that bullet or not")]
        public bool ejectUnfiredBullet = true;
        [Tooltip("This is the unfired bullet prefab that will be instantiated and ejected if the 'ejectUnfiredBullet' value is true and the slide is racked while full")]
        public GameObject bullet;
        [Tooltip("This is the fired bullet shell prefab that will be instantiated and ejected if the when the gun is shot")]
        public GameObject bulletShell;
        [Tooltip("Particle effect that plays from the gun aim transform on shoot")]
        public ParticleSystem shootParticle;
        [Tooltip("The lifetime of the ejected bullets before they are added to the pool")]
        public float ejectedBulletLifetime = 3f;
        [Tooltip("The position and rotation where the bullets are instantiated")]
        public Transform shellEjectionSpawnPoint;
        [Tooltip("The forward direction of this point represents the direction the shells should be ejected")]
        public Transform shellEjectionDirection;
        [Tooltip("The amount of force to add to the ejected bullet")]
        public float shellEjectionForce = 50;

        [AutoSmallHeader("Sound Effects")]
        public bool ignoreMe4;
        [Tooltip("This sound will play when a bullet is fired")]
        public AudioSource shootSound;
        [Tooltip("This sound will play when a the trigger is pressed and there is nothing to shoot")]
        public AudioSource emptyShootSound;

        Dictionary<GameObject, float> bulletLifetimeTracker = new Dictionary<GameObject, float>();
        Dictionary<GameObject, float> shellLifetimeTracker = new Dictionary<GameObject, float>();
        List<GameObject> bulletPool = new List<GameObject>();
        List<GameObject> bulletShellPool = new List<GameObject>();

        List<ParticleSystem> activeParticlePool = new List<ParticleSystem>();
        List<ParticleSystem> inactiveParticlePool = new List<ParticleSystem>();
        AutoGun gun;


        private void OnEnable(){
            gun = GetComponent<AutoGun>();
            gun.OnShoot.AddListener(OnShoot);
            gun.OnEmptyShoot.AddListener(OnEmptyShoot);
            gun.OnSlideEvent.AddListener(OnSlideLoaded);
        }

        private void OnDisable(){
            gun.OnShoot.RemoveListener(OnShoot);
            gun.OnEmptyShoot.RemoveListener(OnEmptyShoot);
            gun.OnSlideEvent.RemoveListener(OnSlideLoaded);
        }

        private void FixedUpdate() {
            CheckBulletLifetime();
            CheckParticlPlaying();

        }

        void CreateShootParticle() {
            if(shootParticle != null) {
                var newShootParticle = GameObject.Instantiate(shootParticle);
                newShootParticle.transform.position = gun.shootForward.position;
                newShootParticle.transform.forward = gun.shootForward.forward;
                activeParticlePool.Add(newShootParticle);
            }
        }

        void OnShoot(AutoGun gun){
            shootSound?.PlayOneShot(shootSound.clip);
            CreateShootParticle();
        }

        void OnEmptyShoot(AutoGun gun){
            emptyShootSound?.PlayOneShot(emptyShootSound.clip);
        }

        void OnSlideLoaded(AutoGun gun, SlideLoadType loadType) {

            if(loadType == SlideLoadType.ShotLoaded){
                if(gun.slideJoint != null) {
                    if(Mathf.Abs(gun.slideJoint.xMinLimit) >= gun.slideJoint.xMaxLimit &&
                        Mathf.Abs(gun.slideJoint.yMinLimit) >= gun.slideJoint.yMaxLimit &&
                        Mathf.Abs(gun.slideJoint.zMinLimit) >= gun.slideJoint.zMaxLimit)
                        gun.slideJoint.SetJointMin();
                    else
                        gun.slideJoint.SetJointMax();
                }

                EjectShell();
            }
            else if(gun.IsSlideLoaded()) {
                EjectBullet();
            }

        }

        public void EjectBullet() {
            if(bullet != null) {
                GameObject newBullet;
                if(bulletPool.Count > 0) {
                    newBullet = bulletPool[0];
                    bulletPool.RemoveAt(0);
                    newBullet.transform.position = shellEjectionSpawnPoint.position;
                    newBullet.transform.rotation = shellEjectionSpawnPoint.rotation;
                    newBullet.SetActive(true);
                }
                else {
                    newBullet = Instantiate(bullet, shellEjectionSpawnPoint.position, shellEjectionSpawnPoint.rotation);
                }

                if(newBullet.CanGetComponent<Rigidbody>(out var body)) {
                    if(AutoHandPlayer.Instance.IsHolding(gun.grabbable))
                        body.velocity = AutoHandPlayer.Instance.body.velocity;
                    body.velocity += gun.grabbable.body.velocity;
                    body.AddForce(shellEjectionDirection.forward * shellEjectionForce, ForceMode.Force);
                }
                bulletLifetimeTracker.Add(newBullet, ejectedBulletLifetime);
            }
        }

        public void EjectShell() {
            if(bulletShell != null) {
                GameObject newShell;
                if(bulletShellPool.Count > 0) {
                    newShell = bulletShellPool[0];
                    bulletShellPool.RemoveAt(0);
                    newShell.transform.position = shellEjectionSpawnPoint.position;
                    newShell.transform.rotation = shellEjectionSpawnPoint.rotation;
                    newShell.SetActive(true);
                }
                else {
                    newShell = Instantiate(bulletShell, shellEjectionSpawnPoint.position, shellEjectionSpawnPoint.rotation);
                }

                if(newShell.CanGetComponent<Rigidbody>(out var body)) {
                    if(AutoHandPlayer.Instance.IsHolding(gun.grabbable))
                        body.velocity = AutoHandPlayer.Instance.body.velocity;
                    body.velocity += gun.grabbable.body.velocity;
                    body.AddForce(shellEjectionDirection.forward * shellEjectionForce, ForceMode.Force);

                }
                shellLifetimeTracker.Add(newShell, ejectedBulletLifetime);
            }

        }



        void CheckBulletLifetime() {
            if(bulletLifetimeTracker.Count > 0) {
                var bulletKeys = new GameObject[bulletLifetimeTracker.Count];
                bulletLifetimeTracker.Keys.CopyTo(bulletKeys, 0);
                foreach(var bullet in bulletKeys) {
                    bulletLifetimeTracker[bullet] -= Time.deltaTime;
                    if(bulletLifetimeTracker[bullet] <= 0) {
                        bullet.SetActive(false);
                        bulletPool.Add(bullet);
                        bulletLifetimeTracker.Remove(bullet);
                    }
                }
            }

            if(shellLifetimeTracker.Count > 0) {
                var shellKeys = new GameObject[shellLifetimeTracker.Count];
                shellLifetimeTracker.Keys.CopyTo(shellKeys, 0);
                foreach(var shell in shellKeys) {
                    shellLifetimeTracker[shell] -= Time.deltaTime;
                    if(shellLifetimeTracker[shell] <= 0) {
                        shell.SetActive(false);
                        bulletShellPool.Add(shell);
                        shellLifetimeTracker.Remove(shell);
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
