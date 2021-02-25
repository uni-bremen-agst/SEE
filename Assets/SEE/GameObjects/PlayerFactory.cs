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
        /// Creates and returns a desktop player instantiated from prefab Resources/Prefabs/Players/DesktopPlayer
        /// with all required components attached to it.
        /// </summary>
        /// <param name="plane">the culling plane the DesktopPlayerMovement should be focusing</param>
        /// <returns>a player for the desktop environment</returns>
        public static GameObject CreateDesktopPlayer(Plane plane)
        {
            GameObject player = InstantiatePlayer("Prefabs/Players/DesktopPlayer");
            player.name = PlayerInputType.DesktopPlayer.ToString();
            player.tag = Tags.MainCamera;
            player.GetComponent<DesktopPlayerMovement>().focusedObject = plane;
            return player;
        }

        /// <summary>
        /// Creates and returns a VR player instantiated from prefab Resources/Prefabs/Players/VRPlayer
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the VR environment</returns>
        public static GameObject CreateVRPlayer()
        {
            GameObject player = InstantiatePlayer("Prefabs/Players/VRPlayer");
            player.name = PlayerInputType.VRPlayer.ToString();
            return player;
        }

        /// <summary>
        /// Creates and returns a HoloLens player instantiated from prefab Resources/Prefabs/Players/HoloLensPlayer
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the AR environment of HoloLens</returns>
        public static GameObject CreateHololensPlayer()
        {
            GameObject player = InstantiatePlayer("Prefabs/Players/HoloLensPlayer");
            player.name = PlayerInputType.HoloLensPlayer.ToString();
            return player;
        }

        /// <summary>
        /// Creates and returns a player for touchpads or gamepads instantiated from prefab Resources/Prefabs/Players/InControl
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the gamepad/touchpad environment</returns>
        public static GameObject CreateTouchGamepadPlayer()
        {
            GameObject player = InstantiatePlayer("Prefabs/Players/InControl");
            player.name = PlayerInputType.TouchGamepadPlayer.ToString();
            return player;
        }

        /// <summary>
        /// Returns an instantiation of the given <paramref name="prefabPath"/>.
        /// </summary>
        /// <param name="prefabPath">path to the prefab; must be contained in a folder Resources within Assets</param>
        /// <exception cref="System.Exception">thrown if <paramref name="prefabPath"/> does not denote a prefab
        /// or that prefab cannot be instantiated</exception>
        /// <returns>instantiated prefab</returns>
        private static GameObject InstantiatePlayer(string prefabPath)
        {
            // We are assuming that all necessary components are already attached in the prefab.
            Object prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new System.Exception($"Prefab {prefabPath} does not exist.\n");
            }
            else
            {
                GameObject player = GameObject.Instantiate(prefab) as GameObject;
                if (player == null)
                {
                    throw new System.Exception($"Prefab {prefabPath} exists but could not be instantiated.\n");
                }
                else
                {
                    return player;
                }
            }
        }
    }
}
