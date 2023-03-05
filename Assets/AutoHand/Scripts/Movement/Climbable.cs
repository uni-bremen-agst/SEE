using UnityEngine;

namespace Autohand {
    [RequireComponent(typeof(Grabbable)), HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/auto-hand-player#climbing")]
    public class Climbable : MonoBehaviour{
        public Vector3 axis = Vector3.one;
        public Stabber stabber;

        private void Start() {
            if(stabber != null) {
                stabber.StartStabEvent += (hand, grab) => {
                    enabled = true;
                };
                stabber.EndStabEvent += (hand, grab) => {
                    enabled = false;
                };
            }
        }
    }
}
