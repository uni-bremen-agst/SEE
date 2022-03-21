using CrazyMinnow.SALSA;
using Dissonance;
using Dissonance.Audio.Playback;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// This component is assumed to be attached to a game object representing
    /// an avatar (representing a local or remote player). It will adapt that
    /// avatar according to the platform we are currently running on
    /// (<see cref="PlayerSettings.GetInputType()"/>. In particular, it will
    /// enable SALSA's lip sync and Dissonance.
    /// </summary>
    internal class AvatarAdapter : NetworkBehaviour
    {
        /// <summary>
        /// If this code is executed for the local player, the necessary player type
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
                        throw new NotImplementedException($"Unhandled case {PlayerSettings.GetInputType()}");
                }

                gameObject.name = "Local " + gameObject.name;
            }
            else
            {
                gameObject.name = "Remote " + gameObject.name;
                // Remote players need to be set up for Dissonance and SALSA lip sync.
                StartCoroutine(SetUpSALSA());
            }
            EnableLocalControl(IsLocalPlayer);
        }

        /// <summary>
        /// Enables/disables local control of the aiming system of the avatar.
        /// </summary>
        /// <param name="isLocalPlayer">if true, the aiming system will be enabled, otherwise disabled</param>
        private void EnableLocalControl(bool isLocalPlayer)
        {
            if (gameObject.TryGetComponentOrLog(out AvatarAimingSystem aimingSystem))
            {
                aimingSystem.IsLocallyControlled = isLocalPlayer;
            }
        }

        /// <summary>
        /// The dissonance communication. Its game object holds the remote players as its children.
        /// There can only be one such dissonance communication object for each client, that is, why
        /// this field can be static.
        /// </summary>
        private static DissonanceComms dissonanceComms;

        /// <summary>
        /// A coroutine setting up SALSA for lipsync.
        /// More specifically, the audio source of the <see cref="Salsa"/> component of this
        /// game object be the audio source of the remote player under the Dissonance communication
        /// associated with this avatar.
        /// </summary>
        /// <returns>as to whether to continue</returns>
        private IEnumerator SetUpSALSA()
        {
            if (gameObject.TryGetComponent(out IDissonancePlayer iDissonancePlayer))
            {
                string playerId = iDissonancePlayer.PlayerId;
                // Wait until the iDissonancePlayer is connected and has a valid player ID.
                // This may take some time.
                while (string.IsNullOrEmpty(playerId))
                {
                    playerId = iDissonancePlayer.PlayerId;
                    yield return null;
                }
                if (!string.IsNullOrEmpty(playerId))
                {
                    // Wait until we find a Dissonance player representation with the given playerId
                    GameObject dissonancePlayer = GetDissonancePlayer(playerId);
                    while (dissonancePlayer == null)
                    {
                        dissonancePlayer = GetDissonancePlayer(playerId);
                        yield return null;
                    }

                    if (dissonancePlayer.TryGetComponent(out AudioSource audioSource)
                        && gameObject.TryGetComponent(out Salsa salsa))
                    {
                        salsa.audioSrc = audioSource;
                    }
                    else
                    {
                        if (audioSource == null)
                        {
                            Debug.LogWarning($"{dissonancePlayer.name} has no {typeof(AudioSource)}.\n");
                        }
                        if (!gameObject.TryGetComponent(out Salsa _))
                        {
                            Debug.LogWarning($"{name} has no {typeof(Salsa)}.\n");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Game object {name} has a {typeof(IDissonancePlayer)} without ID.\n");
                }
            }
            else
            {
                Debug.LogWarning($"{name} has no {typeof(IDissonancePlayer)}.\n");
            }
        }

        /// <summary>
        /// Yields the first child of <see cref="dissonanceComms"/> that has a <see cref="VoicePlayback"/>
        /// component whose <see cref="VoicePlayback.PlayerName"/> equals <paramref name="playerId"/>.
        /// If a <see cref="dissonanceComms"/> cannot be found or if there is no such child, an exception
        /// will be thrown.
        /// </summary>
        /// <param name="playerId">the searched player ID</param>
        /// <returns>child of <see cref="dissonanceComms"/> representing <paramref name="playerId"/></returns>
        private static GameObject GetDissonancePlayer(string playerId)
        {
            if (dissonanceComms == null)
            {
                dissonanceComms = FindObjectOfType<DissonanceComms>();
                if (dissonanceComms == null)
                {
                    throw new Exception($"A game object with a {typeof(DissonanceComms)} cannot be found.\n");
                }
            }
            foreach (Transform child in dissonanceComms.transform)
            {
                if (child.TryGetComponent(out VoicePlayback voicePlayback) && voicePlayback.PlayerName == playerId)
                {
                    return child.gameObject;
                }
            }
            return null;
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
        /// as a child and a <see cref="DesktopPlayerMovement"/> component.
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
