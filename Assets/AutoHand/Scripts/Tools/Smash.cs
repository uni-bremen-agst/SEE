using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo{
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/smashing")]
    public class Smash : MonoBehaviour{
        [Header("Smash Options")]
        [Tooltip("Required velocity magnitude from Smasher to smash")]
        public float smashForce = 1;
        [Tooltip("Whether or not to destroy this object on smash")]
        public bool destroyOnSmash = false;
        [Tooltip("Whether or not to release this object on smash")]
        [HideIf("destroyOnSmash")]
        public bool releaseOnSmash = false;

        [Header("Particle Effect")]
        [Tooltip("Plays this effect on smash")]
        public ParticleSystem effect;
        [Tooltip("Whether or not to instantiates a new a particle system on smash")]
        public bool createNewEffect = true;
        [Tooltip("Whether or not to apply rigidbody velocity to particle velocity on smash")]
        public bool applyVelocityOnSmash = true;
        
        [Header("Sound Options")]
        public AudioClip smashSound;
        public float smashVolume = 1f;
        

        [Header("Event")]
        public UnityEvent OnSmash;
        
        //Progammer Events <3
        public SmashEvent OnSmashEvent;


        internal Grabbable grabbable;

        public void Start() {
            if(!(grabbable = GetComponent<Grabbable>())){
                GrabbableChild grabChild;
                if(grabChild = GetComponent<GrabbableChild>())
                    grabbable = grabChild.grabParent;
            }

            OnSmashEvent += (smasher, smashable) => { OnSmash?.Invoke(); };
        }


        public void DelayedSmash(float delay) {
            Invoke("DoSmash", delay);
        }


        public void DoSmash() {
            DoSmash(null);
        }


        public void DoSmash(Smasher smash){
            if(effect){
                ParticleSystem particles;
                if(createNewEffect)
                    particles = Instantiate(effect, grabbable.transform.position, grabbable.transform.rotation);
                else
                    particles = effect;

                particles.transform.parent = null;
                particles.Play();

                Rigidbody rb;
                if(applyVelocityOnSmash && ((rb = grabbable.body) || gameObject.CanGetComponent(out rb))){
                    ParticleSystem.VelocityOverLifetimeModule module = particles.velocityOverLifetime;
                    module.x = rb.velocity.x;
                    module.y = rb.velocity.y;
                    module.z = rb.velocity.z;
                }
            }

            //Play the audio sound
            if(smashSound)
                AudioSource.PlayClipAtPoint(smashSound, transform.position, smashVolume);

            OnSmashEvent?.Invoke(smash, this);

            if((destroyOnSmash || releaseOnSmash) && grabbable)
                grabbable.ForceHandsRelease();

            if(destroyOnSmash)
                Destroy(gameObject);
        }
    }
}
