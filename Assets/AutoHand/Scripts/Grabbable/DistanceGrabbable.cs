using NaughtyAttributes;
using UnityEngine;

namespace Autohand{
    public enum DistanceGrabType {
        Velocity,
        Linear
    }

    [RequireComponent(typeof(Grabbable))]
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/grabbable/distance-grabbinghttps://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/grabbable/distance-grabbing")]
    public class DistanceGrabbable : MonoBehaviour{
        [AutoHeader("Distance Grabbable")]
        public bool ignoreMe;
        
        [Header("Pull")]
        public bool instantPull = true;


        public DistanceGrabType grabType;

        [Range(0.4f, 1.1f)]
        [Tooltip("Use this to adjust the angle of the arch that the gameobject follows while shooting towards your hand.")]
        [ShowIf("grabType", DistanceGrabType.Velocity)]
        public float archMultiplier = .6f;
        [Tooltip("Slow down or speed up gravitation to your liking.")]
        [ShowIf("grabType", DistanceGrabType.Velocity)]
        public float gravitationVelocity = 1f;



        [Header("Rotation")]
        [Tooltip("This enables rotation which makes the gameobject orient to the rotation of you hand as it moves through the air. All below rotation variables have no use when this is false.")]
        [ShowIf("grabType", DistanceGrabType.Velocity)]
        public bool rotate = true;

        [Tooltip("Speed that the object orients to the rotation of your hand.")]
        [ShowIf("grabType", DistanceGrabType.Velocity)]
        public float rotationSpeed = 1;
        
        [AutoToggleHeader("Enable Highlighting")]
        [Tooltip("Whether or not to ignore all highlights including default highlights on HandPointGrab")]
        public bool ignoreHighlights = true;
        [EnableIf("ignoreHighlights"), Tooltip("Highlight targeted material to use - defaults to HandPointGrab materials if none")]
        public Material targetedMaterial;
        [EnableIf("ignoreHighlights"), Tooltip("Highlight selected material to use - defaults to HandPointGrab materials if none")]
        public Material selectedMaterial;

        [AutoToggleHeader("Show Events")]
        public bool showEvents = true;
        [ShowIf("showEvents")]
        public UnityHandGrabEvent OnPull;
        [Space]
        [Tooltip("Called when the object has been targeted/aimed at by the pointer")]
        [ShowIf("showEvents")]
        public UnityHandGrabEvent StartTargeting;
        [ShowIf("showEvents")]
        public UnityHandGrabEvent StopTargeting;
        [Space]
        [Tooltip("Called when the object has been selected before being pulled or flicked")]
        [ShowIf("showEvents")]
        public UnityHandGrabEvent StartSelecting;
        [ShowIf("showEvents")]
        public UnityHandGrabEvent StopSelecting;

        public HandGrabEvent OnPullCanceled;

        internal Grabbable grabbable;
    

        private Transform target = null;
        private Vector3 calculatedNecessaryVelocity;
        private bool gravitationEnabled;
        private bool gravitationMethodBegun;
        private bool pullStarted;
        private Rigidbody body;
        float timePassedSincePull;

        private void Start() {
            grabbable = GetComponent<Grabbable>();
            grabbable.OnGrabEvent += (Hand hand, Grabbable grab) => { gravitationEnabled = false; };
            body = grabbable.body;
        }
    
        void FixedUpdate(){
            if(!instantPull && grabType == DistanceGrabType.Velocity) {
                if (target == null)
                    return;

                InitialVelocityPushToHand();
                if(rotate)
                    FollowHandRotation();
                if (gravitationEnabled)
                    GravitateTowardsHand();
                timePassedSincePull += Time.fixedDeltaTime;
            }
        }


        private void FollowHandRotation(){
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotationSpeed * Time.fixedDeltaTime); 
        }

        Vector3 lastGravitationVelocity;
        private void GravitateTowardsHand(){
            if (gravitationEnabled){

                if (!gravitationMethodBegun){
                    gravitationMethodBegun = true;
                }
                    
                lastGravitationVelocity = (target.position- transform.position).normalized*Time.fixedDeltaTime*gravitationVelocity;
                body.velocity += lastGravitationVelocity*10;
            }
            else{
                gravitationMethodBegun = false;
            }
        }


        private void InitialVelocityPushToHand(){
            //This way I can ensure that the initial shot with velocity is only shot once
            if (pullStarted){
                if(archMultiplier > 0)
                    calculatedNecessaryVelocity = CalculateTrajectoryVelocity(transform.position, target.transform.position, archMultiplier);

                timePassedSincePull = 0;
                body.velocity = calculatedNecessaryVelocity;
                gravitationEnabled = true;
                pullStarted = false;
            }
        }

        private void OnCollisionEnter(Collision collision){
            if (timePassedSincePull > 0.2f)
            {
                pullStarted = false;
                gravitationEnabled = false;
                CancelTarget();
            }
        }


        Vector3 CalculateTrajectoryVelocity(Vector3 origin, Vector3 target, float t){
            float vx = (target.x - origin.x) / t;
            float vz = (target.z - origin.z) / t;
            float vy = ((target.y - origin.y) - 0.5f * Physics.gravity.y * t * t) / t;
            return new Vector3(vx, vy, vz);
        }

        public void SetTarget(Transform theObject) { target = theObject; pullStarted = true; }
        public void CancelTarget() { target = null; pullStarted = false; }
    }
}
