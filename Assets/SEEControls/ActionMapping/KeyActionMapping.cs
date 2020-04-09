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
        public ButtonEvent OnWKey;

        [SerializeField]
        public ButtonEvent OnAKey;

        [SerializeField]
        public ButtonEvent OnSKey;

        [SerializeField]
        public ButtonEvent OnDKey;

        [SerializeField]
        public ButtonEvent OnCKey;

        [SerializeField]
        public ButtonEvent OnLeftSchiftKey;

        [SerializeField]
        public ButtonEvent OnSpaceKey;

        public override void CheckInput()
        {
            if(Input.GetKey(KeyCode.W))
            {
                OnWKey.Invoke();
            }

            if (Input.GetKey(KeyCode.A))
            {
                OnAKey.Invoke();
            }

            if (Input.GetKey(KeyCode.S))
            {
                OnSKey.Invoke();
            }

            if (Input.GetKey(KeyCode.D))
            {
                OnDKey.Invoke();
            }

            if (Input.GetKey(KeyCode.C))
            {
                OnCKey.Invoke();
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                OnLeftSchiftKey.Invoke();
            }

            if (Input.GetKey(KeyCode.Space))
            {
                OnSpaceKey.Invoke();
            }
        }

        public override string GetTypeAsString()
        {
            return "Key Mapping";
        }
    }
}
