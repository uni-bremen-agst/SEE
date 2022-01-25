using Unity.Netcode;
using UnityEngine;

namespace Dissonance.Integrations.Unity_NFGO.Demo
{
    public class NfgoPlayerController
        : NetworkBehaviour
    {
        private CharacterController _controller;
        private Transform _transform;

        private void OnEnable()
        {
            _controller = GetComponent<CharacterController>();
            _transform = GetComponent<Transform>();
        }

        private void Update()
        {
            if (!IsLocalPlayer || !_controller)
                return;

            var rotation = Input.GetAxis("Horizontal") * Time.deltaTime * 150.0f;
            var speed = Input.GetAxis("Vertical") * 3.0f;
            
            _transform.Rotate(0, rotation, 0);
            _controller.SimpleMove(_transform.TransformDirection(Vector3.forward) * speed);

            if (_transform.position.y < -3)
            {
                _transform.position = Vector3.zero;
                _transform.rotation = Quaternion.identity;
            }
        }
    }
}