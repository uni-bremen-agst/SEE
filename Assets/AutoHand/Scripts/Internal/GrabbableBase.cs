using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine.Serialization;

namespace Autohand {

    [DefaultExecutionOrder(-100)]
    public class GrabbableBase : MonoBehaviour{

        [AutoHeader("Grabbable")]
        public bool ignoreMe;

        [Tooltip("The physics body to connect this colliders grab to - if left empty will default to local body")]
        public Rigidbody body;

        [Tooltip("A copy of the mesh will be created and slighly scaled and this material will be applied to create a highlight effect with options")]
        public Material hightlightMaterial;

        [HideInInspector]
        public bool isGrabbable = true;

        private PlacePoint _placePoint = null;
        public PlacePoint placePoint { get { return _placePoint; } protected set { _placePoint = value; } }

        internal List<Collider> _grabColliders = new List<Collider>();
        public List<Collider> grabColliders { get { return _grabColliders; } }
        protected Dictionary<Collider, PhysicMaterial> grabColliderMaterials = new Dictionary<Collider, PhysicMaterial>();
        protected Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();

        protected List<Hand> heldBy = new List<Hand>();
        protected List<Hand> beingGrabbedBy = new List<Hand>();
        protected bool hightlighting;
        protected GameObject highlightObj;
        protected PlacePoint lastPlacePoint = null;

        protected Transform originalParent;
        protected Vector3 lastCenterOfMassPos;
        protected Quaternion lastCenterOfMassRot;
        protected CollisionDetectionMode detectionMode;
        protected RigidbodyInterpolation startInterpolation;

        protected internal bool beingGrabbed = false;
        protected internal bool beforeGrabFrame = false;
        //protected bool heldBodyJointed = false;
        protected bool wasIsGrabbable = false;
        protected bool beingDestroyed = false;
        protected Dictionary<Hand, Coroutine> resetLayerRoutine = new Dictionary<Hand, Coroutine>();
        protected Dictionary<Hand, Coroutine> ignoreWhileGrabbingRoutine = new Dictionary<Hand, Coroutine>();
        internal List<Grabbable> jointedGrabbables = new List<Grabbable>();
        internal List<GrabbableChild> grabChildren = new List<GrabbableChild>();
        internal List<Grabbable> grabbableChildren = new List<Grabbable>();
        internal List<Grabbable> grabbableParents = new List<Grabbable>();
        protected List<Transform> jointedParents = new List<Transform>();
        protected Dictionary<Material, List<GameObject>> highlightObjs = new Dictionary<Material, List<GameObject>>();

        protected GrabbablePoseCombiner poseCombiner;
        protected float lastUpdateTime;

        protected bool rigidbodyDeactivated = false;
        protected SaveRigidbodyData rigidbodyData;

        public Transform rootTransform {
            get {
                if(body != null)
                    return body.transform;
                else if(rigidbodyData.IsSet())
                    return rigidbodyData.GetOrigin();
                else if(gameObject.CanGetComponent<Rigidbody>(out var rigidbody))
                    return rigidbody.transform;
                else if(gameObject.GetComponentInParent<Rigidbody>() != null)
                    return gameObject.GetComponentInParent<Rigidbody>().transform;
                else
                    return null;
            }
        }


        private CollisionTracker _collisionTracker;
        public CollisionTracker collisionTracker {
            get {
                if(_collisionTracker == null) {
                    if(!(_collisionTracker = GetComponent<CollisionTracker>())) {
                        _collisionTracker = gameObject.AddComponent<CollisionTracker>();
                        _collisionTracker.disableTriggersTracking = true;
                    }
                }
                return _collisionTracker;
            }
            protected set {
                if(_collisionTracker != null)
                    Destroy(_collisionTracker);

                _collisionTracker = value;
            }
        }

#if UNITY_EDITOR
        protected bool editorSelected = false;
#endif

        public virtual void Awake() {
            if(!gameObject.CanGetComponent(out poseCombiner))
                poseCombiner = gameObject.AddComponent<GrabbablePoseCombiner>();

            GetPoseSaves(transform);

            //body.maxDepenetrationVelocity = 1f;

            void GetPoseSaves(Transform obj) {
                //Stop if you get to another grabbable
                if(obj.CanGetComponent(out Grabbable grab) && grab != this)
                    return;

                var poses = obj.GetComponents<GrabbablePose>();
                for(int i = 0; i < poses.Length; i++)
                    poseCombiner.AddPose(poses[i]);

                for(int i = 0; i < obj.childCount; i++)
                    GetPoseSaves(obj.GetChild(i));
            }



            if(body == null){
                if(GetComponent<Rigidbody>())
                    body = GetComponent<Rigidbody>();
                else
                    Debug.LogError("RIGIDBODY MISSING FROM GRABBABLE: " + transform.name + " \nPlease add/attach a rigidbody", this);
            }


    #if UNITY_EDITOR
            if (Selection.activeGameObject == gameObject){
                Selection.activeGameObject = null;
                Debug.Log("Auto Hand (EDITOR ONLY): Selecting the grabbable in the inspector can cause lag and quality reduction at runtime. (Automatically deselecting at runtime) Remove this code at any time.", this);
                editorSelected = true;
            }

            Application.quitting += () => { if (editorSelected) Selection.activeGameObject = gameObject; };
    #endif

            originalParent = body.transform.parent;
            detectionMode = body.collisionDetectionMode;
            startInterpolation = body.interpolation;

            
            grabColliders.Clear();
            grabColliderMaterials.Clear();
            SetCollidersRecursive(body.transform);
        }


        private void OnDestroy() {
            beingDestroyed = true;
        }

        public virtual void HeldFixedUpdate() {
            if(heldBy.Count > 0) {
                lastCenterOfMassRot = body.transform.rotation;
                lastCenterOfMassPos = body.transform.position;
            }

        }

        protected virtual void OnDisable(){
            foreach(var routine in resetLayerRoutine) {
                IgnoreHand(routine.Key, false);
                if(routine.Value != null)
                    StopCoroutine(routine.Value);
            }
            resetLayerRoutine.Clear();

            foreach(var routine in ignoreGrabbableCollisions) {
                StopCoroutine(routine.Value);
            }
            ignoreGrabbableCollisions.Clear();

            foreach(var routine in ignoreHandCollisions) {
                StopCoroutine(routine.Value);
            }
            ignoreHandCollisions.Clear();

        }
        

        
        internal void SetPlacePoint(PlacePoint point) {
            this.placePoint = point;
        }

        internal void SetGrabbableChild(GrabbableChild child) {
            if(!grabChildren.Contains(child))
                grabChildren.Add(child);
        }
        

        public void DeactivateRigidbody()
        {
            if (body != null){
                if(body != null)
                    rigidbodyData = new SaveRigidbodyData(body);
                body = null;
                rigidbodyDeactivated = true;
            }
        }

        public void ActivateRigidbody()
        {
            if (rigidbodyDeactivated){
                rigidbodyDeactivated = false;
                body = rigidbodyData.ReloadRigidbody();
            }
        }

        

        internal void SetLayerRecursive(int newLayer) {
            foreach(var transform in originalLayers) {
                transform.Key.gameObject.layer = newLayer;
            }
        }

        /// <summary>Sets the grabbable and children to the physics layers it had on Start()</summary>
        internal void ResetOriginalLayers() {
            foreach(var transform in originalLayers) {
                transform.Key.gameObject.layer = transform.Value;
            }
        }


        Dictionary<Grabbable, Coroutine> ignoreGrabbableCollisions = new Dictionary<Grabbable, Coroutine>();
        public void IgnoreGrabbableCollisionUntilNone(Grabbable other) {
            ignoreGrabbableCollisions.Add(other, StartCoroutine(IgnoreGrabbableCollisionUntilNoneRoutine(other)));
        }

        protected IEnumerator IgnoreGrabbableCollisionUntilNoneRoutine(Grabbable other) {
            IgnoreGrabbableColliders(other, true);

            yield return new WaitForSeconds(0.05f);
            while(IsGrabbableOverlapping(other))
                yield return new WaitForSeconds(0.1f);

            IgnoreGrabbableColliders(other, false);
            ignoreGrabbableCollisions.Remove(other);

            if(ignoreGrabbableCollisions.ContainsKey(other))
                ignoreGrabbableCollisions.Remove(other);

        }

        public bool IsGrabbableOverlapping(Grabbable other) {
            foreach(var col1 in grabColliders) {
                foreach(var col2 in other.grabColliders) {
                    if(col1.enabled && !col1.isTrigger && !col1.isTrigger && col2.enabled && !col2.isTrigger && !col2.isTrigger &&
                        Physics.ComputePenetration(col1, col1.transform.position, col1.transform.rotation, col2, col2.transform.position, col2.transform.rotation, out _, out _)) {
                        return true;
                    }
                }
            }

            return false;
        }

        public void IgnoreGrabbableColliders(Grabbable other, bool ignore) {
            foreach(var col1 in grabColliders) {
                foreach(var col2 in other.grabColliders) {
                    Physics.IgnoreCollision(col1, col2, ignore);
                }
            }
        }




        Dictionary<Hand, Coroutine> ignoreHandCollisions = new Dictionary<Hand, Coroutine>();
        public void IgnoreHandCollisionUntilNone(Hand hand, float minIgnoreTime = 1) {
            if(gameObject.activeInHierarchy && !beingDestroyed)
                ignoreHandCollisions.Add(hand, StartCoroutine(IgnoreHandCollisionUntilNoneRoutine(hand, minIgnoreTime)));
        }

        protected IEnumerator IgnoreHandCollisionUntilNoneRoutine(Hand hand, float minIgnoreTime) {
            if(!ignoringHand.ContainsKey(hand) || !ignoringHand[hand]) {
                IgnoreHand(hand, true);

                yield return new WaitForSeconds(minIgnoreTime);
                if(minIgnoreTime != 0)
                    while(IsHandOverlapping(hand))
                        yield return new WaitForSeconds(0.1f);

                IgnoreHand(hand, false);
                if(resetLayerRoutine.ContainsKey(hand))
                    resetLayerRoutine.Remove(hand);
                if(ignoreHandCollisions.ContainsKey(hand))
                    ignoreHandCollisions.Remove(hand);
            }
        }


        protected IEnumerator IgnoreHandCollision(Hand hand, float time) {
            if(!ignoringHand.ContainsKey(hand) || !ignoringHand[hand]) {
                IgnoreHand(hand, true);

                yield return new WaitForSeconds(time);

                IgnoreHand(hand, false);
                resetLayerRoutine.Remove(hand);
            }
        }

        protected Dictionary<Hand, bool> ignoringHand =  new Dictionary<Hand, bool>();
        public void IgnoreHand(Hand hand, bool ignore, bool overrideIgnoreRoutines = false)
        {
            if(overrideIgnoreRoutines && resetLayerRoutine.ContainsKey(hand) && resetLayerRoutine[hand] != null) {
                StopCoroutine(resetLayerRoutine[hand]);
                resetLayerRoutine[hand] = null;
            }

            foreach (var col in grabColliders)
                hand.HandIgnoreCollider(col, ignore);

            foreach(var grab in grabbableChildren)
                foreach(var col in grab.grabColliders)
                    hand.HandIgnoreCollider(col, ignore);

            foreach(var grab in grabbableParents)
                foreach(var col in grab.grabColliders)
                    hand.HandIgnoreCollider(col, ignore);

            if(!ignoringHand.ContainsKey(hand))
                ignoringHand.Add(hand, ignore);
            else
                ignoringHand[hand] = ignore;
        }


        public bool IsHandOverlapping(Hand hand) {
            float dist;
            Vector3 dir;
            foreach(var col2 in grabColliders) {
                foreach(var col1 in hand.handColliders) {
                    if(col1.enabled && !col1.isTrigger && !col1.isTrigger && col2.enabled && !col2.isTrigger && !col2.isTrigger && 
                    Physics.ComputePenetration(col1, col1.transform.position, col1.transform.rotation, col2, col2.transform.position, col2.transform.rotation, out dir, out dist)) {
                        return true;
                    }
                }
            }

            return false;
        }








        public bool GetSavedPose(out GrabbablePoseCombiner pose) {
            if(poseCombiner != null && poseCombiner.PoseCount() > 0) {
                pose = poseCombiner;
                return true;
            }
            else {
                pose = null;
                return false;
            }
        }

        public bool HasCustomPose() {
            return poseCombiner.PoseCount() > 0;
        }


        /// <summary>Resets the physics materials on all the colliders to how it was during Start()</summary>
        public void SetPhysicsMateiral(PhysicMaterial physMat) {
            foreach(var collider in grabColliders) {
                collider.material = physMat;
            }
        }

        /// <summary>Resets the physics materials on all the colliders to how it was during Start()</summary>
        public void ResetPhysicsMateiral() {
            foreach(var col in grabColliderMaterials) {
                col.Key.sharedMaterial = col.Value;
            }
        }


        public void SetCollidersRecursive(Transform obj) {

            var noFrictionMat = Resources.Load<PhysicMaterial>("NoFriction");
            foreach(var col in obj.GetComponents<Collider>()) {
                grabColliders.Add(col);
                if(col.sharedMaterial == null || col.sharedMaterial == noFrictionMat)
                    grabColliderMaterials.Add(col, null);
                else
                    grabColliderMaterials.Add(col, col.sharedMaterial);
                if(!originalLayers.ContainsKey(col.transform)) {
                    if(col.gameObject.layer == LayerMask.NameToLayer("Default") || LayerMask.LayerToName(col.gameObject.layer) == "")
                        col.gameObject.layer = LayerMask.NameToLayer(Hand.grabbableLayerNameDefault);
                    originalLayers.Add(col.transform, col.gameObject.layer);
                }
            }

            for (int i = 0; i < obj.childCount; i++)
                SetCollidersRecursive(obj.GetChild(i));
        }
        
        //Resets to original collision dection
        protected void ResetRigidbody() {
            if (body != null)
            {
                body.collisionDetectionMode = detectionMode;
                body.interpolation = startInterpolation;
            }
        }

        public bool BeingDestroyed() {
            return beingDestroyed;
        }

        public void DebugBreak() {
#if UNITY_EDITOR
            Debug.Break();
#endif
        }


    }
}