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
                Debug.Log($"{gameObject.GetFullName()} is the local player.\n");

                switch (PlayerSettings.GetInputType())
                {
                    case PlayerInputType.DesktopPlayer:
                    case PlayerInputType.TouchGamepadPlayer:
                        PrepareForDesktop();
                        break;
                    case PlayerInputType.VRPlayer:
                        PrepareForXR();
                        break;
                    default:
                        throw new System.NotImplementedException($"Unhandled case {PlayerSettings.GetInputType()}");
                }
            }
            else
            {
                // I am the avatar of a remote player.
                Debug.Log($"{gameObject.GetFullName()} is a remote player.\n");
            }
        }

        private void PrepareForXR()
        {
            GameObject vrPlayer = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/VRPlayer");
            vrPlayer.name = PlayerInputType.DesktopPlayer.ToString();
            vrPlayer.transform.SetParent(gameObject.transform);
            gameObject.AddComponent<XRPlayerMovement>();
        }

        private float DesktopAvatarHeight()
        {
            if (gameObject.TryGetComponent(out Collider collider))
            {
                return collider.bounds.size.y;
            }
            else
            {
                return gameObject.transform.lossyScale.y;
            }
        }

        private void PrepareForDesktop()
        {
            // Set up the desktop player at the top of the player just in front of it.
            GameObject desktopPlayer = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/DesktopPlayer");
            desktopPlayer.name = PlayerInputType.DesktopPlayer.ToString();
            desktopPlayer.transform.SetParent(gameObject.transform);
            desktopPlayer.transform.localPosition = new Vector3(0, DesktopAvatarHeight(), 0.3f);
            desktopPlayer.transform.localRotation = Quaternion.Euler(30, 0, 0);

            gameObject.AddComponent<DesktopPlayerMovement>();
        }
    }
}
