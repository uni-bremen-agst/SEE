using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Worlds
{
    class AvatarAdapter : NetworkBehaviour
    {
        private void Start()
        {
            if (IsLocalPlayer)
            {
                // I am the avatar of the local player.
                Debug.LogError($"{gameObject.GetFullName()} is the local player.\n");
                GameObject player = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/DesktopPlayer");
                player.name = PlayerInputType.DesktopPlayer.ToString();
                player.transform.SetParent(gameObject.transform);
                player.transform.localPosition = new Vector3(0, 1.8f, 0.3f);
                player.transform.localRotation = Quaternion.Euler(30, 0, 0);
                EnableMovement(true);
            }
            else
            {
                // I am the avatar of a remote player.
                Debug.LogError($"{gameObject.GetFullName()} is a remote player.\n");
                EnableMovement(false);
            }
        }

        private void EnableMovement(bool enable)
        {
            if (gameObject.TryGetComponent(out DesktopPlayerMovement movement))
            {
                Debug.LogError($"Enabled movement for {gameObject.GetFullName()}: {enable}.\n");
                movement.enabled = enable;
            }
            else
            {
                Debug.LogError($"{gameObject.GetFullName()} has no DesktopPlayerMovement.\n");
            }
        }
    }
}
