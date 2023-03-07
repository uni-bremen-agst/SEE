using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo{
    public delegate void SmashEvent(Smasher smasher, Smash smashable);

    [RequireComponent(typeof(Rigidbody)), HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/smashing")]
    public class Smasher : MonoBehaviour{
        Rigidbody rb;
        [Header("Options")]
        public LayerMask smashableLayers;
        [Tooltip("How much to multiply the magnitude on smash")]
        public float forceMulti = 1;
        [Tooltip("Can be left empty - The center of mass point to calculate velocity magnitude - for example: the camera of the hammer is a better point vs the pivot center of the hammer object")]
        public Transform centerOfMassPoint;

        [Header("Event")]
        public UnityEvent OnSmash;

        //Progammer Events <3
        public SmashEvent OnSmashEvent;

        Vector3[] velocityOverTime = new Vector3[3];
        Vector3 lastPos;
    
        private void Start(){
            rb = GetComponent<Rigidbody>();
            if(smashableLayers == 0)
                smashableLayers = LayerMask.GetMask(Hand.grabbableLayerNameDefault);

            OnSmashEvent += (smasher, smashable) => { OnSmash?.Invoke(); };
        }


        void FixedUpdate() {
            for(int i = 1; i < velocityOverTime.Length; i++) {
                velocityOverTime[i] = velocityOverTime[i-1];
            }
            velocityOverTime[0] = lastPos - (centerOfMassPoint ? centerOfMassPoint.position : rb.position);

            lastPos = centerOfMassPoint ? centerOfMassPoint.position : rb.position;
        }


        private void OnCollisionEnter(Collision collision) {
            Smash smash;
            if(collision.transform.CanGetComponent(out smash)){
                if(GetMagnitude() >= smash.smashForce){
                    smash.DoSmash();
                    OnSmashEvent?.Invoke(this, smash);
                }
            }
        }


        float GetMagnitude() {
            Vector3 velocity = Vector3.zero;
            for(int i = 0; i < velocityOverTime.Length; i++) {
                velocity += velocityOverTime[i];
            }

            return (velocity.magnitude/velocityOverTime.Length)*forceMulti*10;
        }
    }
}
