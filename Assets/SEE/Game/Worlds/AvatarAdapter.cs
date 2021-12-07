using CrazyMinnow.SALSA;
using Dissonance;
using Dissonance.Audio.Playback;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using System;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Worlds
{
    class AvatarAdapter : NetworkBehaviour
    {
        /// <summary>
        /// If this code is execute for the local player, the necessary player type
        /// for the environment we are currently running on are added to this game object.
        /// </summary>
        private void Start()
        {
            if (IsLocalPlayer)
            {
                // I am the avatar of the local player.
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
                // Remote players need to be set up for Dissonance and SALSA lip sync.
                SetUpSALSA();
            }
        }

        /// <summary>
        /// The dissonance communication. Its game object holds the remote players as its children.
        /// </summary>
        private DissonanceComms comms;

        private void SetUpSALSA()
        {
            if (gameObject.TryGetComponent(out IDissonancePlayer iDissonancePlayer))
            {
                GameObject dissonancePlayer = GetDissonancePlayer(iDissonancePlayer.PlayerId);
                if (dissonancePlayer.TryGetComponent(out AudioSource audioSource)
                    && gameObject.TryGetComponent(out Salsa salsa))
                {
                    salsa.audioSrc = audioSource;
                }
            }
        }

        private GameObject GetDissonancePlayer(string playerId)
        {
            if (comms == null)
            {
                comms = FindObjectOfType<DissonanceComms>();
                if (comms == null)
                {
                    throw new Exception($"A game object with a {typeof(DissonanceComms)} cannot be found.\n");
                }
            }
            foreach (GameObject child in comms.transform)
            {
                if (child.TryGetComponent(out VoicePlayback voicePlayback) && voicePlayback.PlayerName == playerId)
                {
                    return child;
                }
            }
            throw new Exception($"There is no player with the id {playerId} in {comms.name}.");
        }

        /// <summary>
        /// Prepares the avatar for a virtual reality environment by adding a VRPlayer prefab
        /// as a child and an <see cref="XRPlayerMovement"/> component.
        /// </summary>
        private void PrepareForXR()
        {
            GameObject vrPlayer = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/VRPlayer");
            vrPlayer.name = PlayerInputType.DesktopPlayer.ToString();
            vrPlayer.transform.SetParent(gameObject.transform);
            gameObject.AddComponent<XRPlayerMovement>();
        }

        /// <summary>
        /// Returns the height of a desktop avatar. Call this method only when running
        /// in a desktop environment.
        /// </summary>
        /// <returns>height of a desktop avatar</returns>
        private float DesktopAvatarHeight()
        {
            // If the avatar has a collider, we derive the height from the collider.
            if (gameObject.TryGetComponent(out Collider collider))
            {
                return collider.bounds.size.y;
            }
            else
            {
                // Fallback if we do not have a collider.
                return gameObject.transform.lossyScale.y;
            }
        }

        /// <summary>
        /// Prepares the avatar for a desktop environment by adding a DesktopPlayer prefab
        /// as a child and an <see cref="DesktopPlayerMovement"/> component.
        /// </summary>
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
