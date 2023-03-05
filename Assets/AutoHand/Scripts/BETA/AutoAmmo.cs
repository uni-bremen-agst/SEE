using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    [RequireComponent(typeof(Grabbable))]
    public class AutoAmmo : MonoBehaviour {
        public int currentAmmo = 16;
        public TMPro.TextMeshPro ammoText;

        private void Start() {
            SetAmmo(currentAmmo);
        }

        public bool RemoveAmmo() {
            if(currentAmmo > 0) {
                SetAmmo(--currentAmmo);
                return true;
            }
            return false;
        }

        public void SetAmmo(int amount) {
            currentAmmo = amount;
            if(ammoText != null)
                ammoText.text = currentAmmo.ToString();
        }
    }
}