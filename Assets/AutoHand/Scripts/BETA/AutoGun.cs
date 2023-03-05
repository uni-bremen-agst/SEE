using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand {
    public enum SlideLoadType {
        HandLoaded,
        ShotLoaded
    }

    [Serializable] public class UnityGunHitEvent : UnityEvent<AutoGun, RaycastHit> { }
    [Serializable] public class UnityGunEvent : UnityEvent<AutoGun> { }
    [Serializable] public class UnityGunSlideEvent : UnityEvent<AutoGun, SlideLoadType> { }
    [Serializable] public class UnityAmmoEvent : UnityEvent<AutoGun, AutoAmmo> { }



    [RequireComponent(typeof(Grabbable))]
    public class AutoGun : MonoBehaviour {

        [AutoHeader("Auto Gun")]
        public bool ignoreMe1;
        [Tooltip("The local forward of this transform is where the bullet raycast will come from")]
        public Transform shootForward;
        [Tooltip("(Optional) GrabbableHeldJoint recommended for triggering the LoadSlide event, if connected this will trigger the slide movement when shooting ")]
        public GrabbableHeldJoint slideJoint;
        [Tooltip("The place point for the ammo")]
        public PlacePoint magazinePoint;

        [AutoSmallHeader("Gun Settings")]
        public bool ignoreMe2;
        [Tooltip("The automatic fire rate while holding the trigger, 0 is semi-automatic")]
        public float automaticFireRate = 0;
        [Tooltip("The force applied to the target's rigidbody when hit")]
        public float hitForce = 2500f;
        [Tooltip("The force applied to the gun when shooting")]
        public float recoilForce = 500f;
        [Tooltip("The maximum distance a target can be hit")]
        public float maxHitDistance = 1000f;
        [Tooltip("If true the gun will hit every target until it hits a static collider")]
        public bool useBulletPenetration = false;
        [Tooltip("Whether or not the LoadSlide function needs to be called externally or if it is automatically called when the ammo is loaded")]
        public bool requireSlideLoading = true;
        [Tooltip("Whether or not the ammo clip should automatically be removed from the place point when it's empty")]
        public bool autoEjectEmptyClip = false;


        [AutoSmallHeader("Events")]
        public bool ignoreMe5;
        public UnityGunEvent OnShoot;
        public UnityGunEvent OnEmptyShoot;
        public UnityGunHitEvent OnHitEvent;
        public UnityAmmoEvent OnAmmoPlaceEvent;
        public UnityAmmoEvent OnAmmoRemoveEvent;
        public UnityGunSlideEvent OnSlideEvent;

        protected AutoAmmo loadedAmmo;
        internal Grabbable grabbable;
        protected bool slideLoaded = false;

        float lastFireTime;
        bool squeezingTrigger;
        bool lastSqueezingTrigger;

        private void Start() {
            grabbable = GetComponent<Grabbable>();
            if(magazinePoint != null)
                magazinePoint.dontAllows.Add(grabbable);

        }

        private void OnEnable() {
            if(magazinePoint != null) {
                magazinePoint.OnPlaceEvent += OnMagPlace;
                magazinePoint.OnRemoveEvent += OnMagRemove;
            }
        }
        private void OnDisable() {
            if(magazinePoint != null) {
                magazinePoint.OnPlaceEvent -= OnMagPlace;
                magazinePoint.OnRemoveEvent -= OnMagRemove;
            }
        }

        private void FixedUpdate() {
            if(squeezingTrigger && automaticFireRate > 0 && slideLoaded && Time.fixedTime-lastFireTime > automaticFireRate)
                Shoot();
            else if(squeezingTrigger && !lastSqueezingTrigger && automaticFireRate <= 0)
                Shoot();
            else if(squeezingTrigger && !lastSqueezingTrigger && automaticFireRate > 0)
                Shoot();

            lastSqueezingTrigger = squeezingTrigger;
        }


        public void PressTrigger() {
            squeezingTrigger = true;
        }


        public void ReleaseTrigger() {
            squeezingTrigger = false;
        }


        public void Shoot() {
            if(slideLoaded) {
                grabbable.body.AddForceAtPosition(-shootForward.forward * recoilForce / 10f, shootForward.position);
                grabbable.body.AddForceAtPosition(shootForward.up * recoilForce, shootForward.position);
                OnShoot?.Invoke(this);

                if(useBulletPenetration){
                    var raycasthits = Physics.RaycastAll(shootForward.position, shootForward.forward, maxHitDistance, ~0, QueryTriggerInteraction.Ignore);
                    if(raycasthits.Length > 0) {
                        foreach(var hit in raycasthits) {
                            if(hit.rigidbody != grabbable.body)
                                OnHit(hit);
                            if(hit.rigidbody == null)
                                break;
                        }
                    }
                }
                else if(Physics.Raycast(shootForward.position, shootForward.forward, out var hit, maxHitDistance, ~0, QueryTriggerInteraction.Ignore)){
                    if(hit.rigidbody != grabbable.body)
                        OnHit(hit);
                    else
                        Debug.LogError("Gun is shooting itself, make sure the shootforward transform is not inside a collider", this);
                }

                lastFireTime = Time.fixedTime;
                FireLoadSlide();
            }
            else
                OnEmptyShoot?.Invoke(this);

            if(autoEjectEmptyClip && loadedAmmo != null && loadedAmmo.currentAmmo == 0 && !slideLoaded)
                magazinePoint?.Remove();
        }





        public void LoadSlide() {
            OnSlideEvent?.Invoke(this, SlideLoadType.HandLoaded);
            slideLoaded = loadedAmmo != null && loadedAmmo.currentAmmo > 0;
            if(slideLoaded)
                loadedAmmo.RemoveAmmo();

        }


        public void FireLoadSlide() {
            OnSlideEvent?.Invoke(this, SlideLoadType.ShotLoaded);
            slideLoaded = loadedAmmo != null && loadedAmmo.currentAmmo > 0;
            if(slideLoaded)
                loadedAmmo.RemoveAmmo();

        }


        public void UnloadSlide() {
            slideLoaded = false;
        }


        public bool IsSlideLoaded() {
            return slideLoaded;
        }


        /// <summary>Returns ammo in count in current clip plus slide</summary>
        public int GetAmmo() {
            int ammo = slideLoaded ? 1 : 0;
            if(loadedAmmo != null)
                ammo += loadedAmmo.currentAmmo;
            return magazinePoint == null ? 1 : ammo;
        }


        void OnMagPlace(PlacePoint point, Grabbable mag) {
            if(mag.TryGetComponent<AutoAmmo>(out var ammo)) {
                this.loadedAmmo = ammo;
                OnAmmoPlaceEvent?.Invoke(this, loadedAmmo);
            }
            if(!slideLoaded && !requireSlideLoading)
                LoadSlide();
        }

        void OnMagRemove(PlacePoint point, Grabbable mag) {
            OnAmmoRemoveEvent?.Invoke(this, loadedAmmo);
            loadedAmmo = null;
        }

        protected virtual void OnHit(RaycastHit hit) {
            if(hit.rigidbody) {
                hit.rigidbody.AddForceAtPosition(shootForward.forward * recoilForce, hit.point);
                OnHitEvent?.Invoke(this, hit);
                if(hit.rigidbody.TryGetComponent<AutoGunTarget>(out var target))
                    target.OnShot(this, hit);
            }
        }
    }
}
