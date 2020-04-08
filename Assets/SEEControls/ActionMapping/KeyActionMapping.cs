using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls
{
    [CreateAssetMenu(fileName = "New KeyActionMapping", menuName = "Controls/KeyActionMapping", order = 1)]
    public class KeyActionMapping : ActionMapping
    {
        [SerializeField]
        public UnityEvent<bool> OnWKey;

        [SerializeField]
        public UnityEvent<bool> OnAKey;

        [SerializeField]
        public UnityEvent<bool> OnSKey;

        [SerializeField]
        public UnityEvent<bool> OnDKey;

        public override void CheckInput()
        {
            if(Input.GetKey("W"))
            {
                OnWKey.Invoke(true);
            }
        }

        public override string GetTypeAsString()
        {
            return "Key Mapping";
        }
    }
}
