using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand{
    [RequireComponent(typeof(Hand)), DefaultExecutionOrder(10000)]
    public class HandAdvancedOptions : MonoBehaviour{
        [Tooltip("Meant for things childed under the Hand. These transforms will not be set to the hand layer on start")]
        public List<Collider> ignoreHandCollider = new List<Collider>();


        Hand hand;

        void Awake(){
            hand = GetComponent<Hand>();
        }

        void Start() { 
            for (int i = 0; i < ignoreHandCollider.Count; i++)
                hand.HandIgnoreCollider(ignoreHandCollider[i], true);
        }

    }
}