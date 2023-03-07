using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Autohand {
    [Serializable, DefaultExecutionOrder(100)] 
    public class UnityDispenserEvent : UnityEvent<DispenserPoint, Grabbable> { }
    public class DispenserPoint : MonoBehaviour {
        [AutoHeader("Dispenser Point")]
        public bool ignoreMe;

        [AutoSmallHeader("Dispenser Settings")]
        public bool showeSettings = true;
        [Tooltip("The object to be copied and dispensed")]
        public Grabbable dispenseObject;
        [Tooltip("The maximum copies allowed to exist from this dispenser before they are destroyed or reset")]
        public int maxCopies = 3;
        [Tooltip("The delay in seconds before the next dispense appears after the current dispense is taken")]
        public float resetDelay = 0f;
        [Tooltip("Whether or not objects placed in the dispense point should be set to kinematic on placed or not")]
        public bool disableBody = false;
        [NaughtyAttributes.HideIf("disableBody"), Tooltip("Whether or not objects placed in the dispense point should be set to kinematic on placed or not")]
        public bool isKinematic = true;
        [Tooltip("If true the object will not just reset its position on reset it will be destroyed and a new copy will be placed. Less performant but important for things like ammo that should always respawn as new clips full")]
        public bool destroyOnReset = false;
        [Tooltip("The maximum distance a dispensed object can move from the point before the next object is dispensed")]
        public float maxDistance = 1f;

        [Space]
        public UnityDispenserEvent OnGrabDispenseEvent;
        public UnityDispenserEvent OnDispenseEvent;

        Grabbable currentDispense;
        Grabbable lastDispense;
        GameObject[] dispensePool;
        int poolCount;
        Coroutine dispenseRoutine;

        protected virtual void Start() {
            GameObject instanceObject;
            dispenseObject.body.gameObject.SetActive(false);

            instanceObject = Instantiate(dispenseObject.body.gameObject);


            instanceObject.transform.position = transform.position;
            instanceObject.transform.rotation = transform.rotation;
            instanceObject.SetActive(true);


            dispensePool = new GameObject[maxCopies];
            dispensePool[0] = instanceObject;


            if(dispensePool[0].HasGrabbable(out var grab)) {
                if(!disableBody && isKinematic && grab.body != null)
                    grab.body.isKinematic = true;

                grab.OnGrabEvent += OnGrab;
                grab.OnPlacePointAddEvent += OnPlaced;
                currentDispense = grab;

                if(disableBody)
                    grab.DeactivateRigidbody();
            }

            poolCount++;
        }

        protected virtual void OnDisable() {
            if(dispenseRoutine != null)
                StopCoroutine(dispenseRoutine);
            dispenseRoutine = null;
        }

        protected virtual void FixedUpdate() { 
            if(maxDistance > 0 && currentDispense.gameObject.activeInHierarchy && Vector3.Distance(transform.position, currentDispense.rootTransform.position) > maxDistance)
                Dispense();
        }

        public virtual Grabbable Dispense() {
            if(dispenseRoutine == null) {
                var poolIndex = (poolCount) % maxCopies;
                if(destroyOnReset) {
                    Destroy(dispensePool[poolIndex]);
                    dispensePool[poolIndex] = null;
                }

                if(poolCount < maxCopies || dispensePool[poolIndex] == null || dispensePool[poolIndex].activeInHierarchy == false)
                    dispensePool[poolIndex] = Instantiate(dispenseObject.body.gameObject);

                dispensePool[poolIndex].transform.position = transform.position;
                dispensePool[poolIndex].transform.rotation = transform.rotation;

                if(dispensePool[poolIndex].HasGrabbable(out var grab)) {
                    grab.ForceHandsRelease();

                    if(grab.body == null)
                        grab.ActivateRigidbody();

                    if(!disableBody && isKinematic)
                        grab.body.isKinematic = true;

                    grab.body.velocity = Vector3.zero;
                    grab.body.angularVelocity = Vector3.zero;

                    grab.OnGrabEvent += OnGrab;
                    grab.OnPlacePointAddEvent += OnPlaced;

                    dispenseRoutine = StartCoroutine(DispenseResetDelay(grab));


                    lastDispense = currentDispense;
                    lastDispense.OnGrabEvent -= OnGrab;
                    lastDispense.OnPlacePointAddEvent -= OnPlaced;
                    currentDispense = grab;

                    poolCount++;
                    return grab;
                }


                poolCount++;
            }
            return null;
        }

        public virtual void OnGrab(Hand hand, Grabbable grab) {
            if(grab != null && isKinematic && grab.body != null)
                grab.body.isKinematic = false;

            OnGrabDispenseEvent?.Invoke(this, grab);
            Dispense();
        }

        public virtual void OnPlaced(PlacePoint point, Grabbable grab) {
            if(grab != null && isKinematic && grab.body != null)
                grab.body.isKinematic = false;

            Debug.Log("Placed: " + grab.body.isKinematic);

            Dispense();
        }

        IEnumerator DispenseResetDelay(Grabbable dispenseObject) {
            dispenseObject.body.gameObject.SetActive(false);
            yield return new WaitForSeconds(resetDelay);
            dispenseObject.body.gameObject.SetActive(true);
            dispenseObject.IgnoreGrabbableCollisionUntilNone(lastDispense);
            foreach(var hand in lastDispense.GetHeldBy())
                dispenseObject.IgnoreHandCollisionUntilNone(hand);
            OnDispenseEvent?.Invoke(this, dispenseObject);
            if(disableBody)
                dispenseObject.DeactivateRigidbody();
            dispenseRoutine = null;
        }

    }
}