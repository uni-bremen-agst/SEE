using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{
    public class InteractionBehavior : MonoBehaviour
    {
        private ActionMapping Mapping;

        private void Start()
        {
            Mapping = new ViveActionMapping("Demomapping") as ViveActionMapping;
            
        }

        //implementation of the mapping functions

        private void FreeMovement(Vector3 direction)
        {

        }
    }
}
