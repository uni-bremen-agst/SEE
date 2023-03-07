using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/extras/stabbing")]
    public class Stabbable : MonoBehaviour{
        public Rigidbody body;
        public Grabbable grabbable;

        [Tooltip("The index that must match the stabbers index to allow stabbing")]
        public int stabIndex = 0;
        public int maxStabbers = 1;
        public float positionDamper = 1000;
        public float rotationDamper = 1000;
        public bool parentOnStab = true;

        [Header("Events")]
        public UnityEvent StartStab;
        public UnityEvent EndStab;
        
        //Progammer Events <3
        public StabEvent StartStabEvent;
        public StabEvent EndStabEvent;

        int currentStabs;
        List<Stabber> stabbing;
        Transform stabParent;

        public void Start() {
            stabbing = new List<Stabber>();


            StartStabEvent += (stabber, stabbable) => { StartStab?.Invoke(); };
            EndStabEvent += (stabber, stabbable) => { EndStab?.Invoke(); };

            if(grabbable != null)
                grabbable.OnReleaseEvent += (hand, grab) => { if (stabbing.Count > 0) body.transform.parent = stabParent; };
        }

        public virtual void OnStab(Stabber stabber) {
            currentStabs++;
            stabbing.Add(stabber);
            StartStabEvent?.Invoke(stabber, this);
            stabParent = body.transform.parent;
        }

        public virtual void OnEndStab(Stabber stabber) {
            currentStabs--;
            stabbing.Remove(stabber);
            EndStabEvent?.Invoke(stabber, this);
        }

        public virtual bool CanStab(Stabber stabber) {
            return currentStabs < maxStabbers && stabber.stabIndex == stabIndex;
        }

        public int StabbedCount() {
            return stabbing.Count;
        }

        private void OnDrawGizmosSelected() {
            if(!body && GetComponent<Rigidbody>())
                body = GetComponent<Rigidbody>();
        }
    }
}
