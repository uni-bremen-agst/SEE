using SEE.Controls;
using SEE.DataModel;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory creating different kinds of players for the various supported environments
    /// (Desktop, VR, AR, Touchpad).
    /// </summary>
    internal static class PlayerFactory
    {
        /// <summary>
        /// Creates and returns a desktop player from prefab Resources/Prefabs/Players/DesktopPlayer
        /// with all required components attached to it.
        /// </summary>
        /// <param name="plane">the culling plane the DesktopPlayerMovement should be focusing</param>
        /// <returns>a player for the desktop environment</returns>
        public static GameObject CreateDesktopPlayer(Plane plane)
        {
            // We are assuming that all necessary components are already attached in the prefab.
            UnityEngine.Object desktopPrefab = Resources.Load<GameObject>("Prefabs/Players/DesktopPlayer");
            GameObject desktopPlayer = GameObject.Instantiate(desktopPrefab) as GameObject;
            UnityEngine.Assertions.Assert.IsNotNull(desktopPlayer);
            desktopPlayer.name = PlayerInputType.DesktopPlayer.ToString();
            desktopPlayer.tag = Tags.MainCamera;
            desktopPlayer.GetComponent<DesktopPlayerMovement>().focusedObject = plane;
            return desktopPlayer;
        }

        /// <summary>
        /// Creates and returns a VR player from prefab Resources/Prefabs/Players/VRPlayer
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the VR environment</returns>
        public static GameObject CreateVRPlayer()
        {
            // We are assuming that all necessary components are already attached in the prefab.
            UnityEngine.Object steamVrPrefab = Resources.Load<GameObject>("Prefabs/Players/VRPlayer");
            GameObject vrPlayer = GameObject.Instantiate(steamVrPrefab) as GameObject;
            UnityEngine.Assertions.Assert.IsNotNull(vrPlayer);
            vrPlayer.name = PlayerInputType.VRPlayer.ToString();
            return vrPlayer;
        }
    }
}
