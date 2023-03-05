using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo {
    public class ToggleAutoMoveFlying : MonoBehaviour {
        public void ToggleFlying() {
            var player = FindObjectOfType<AutoHandPlayer>();
            player.useGrounding = !player.useGrounding;
        }
    }
}
