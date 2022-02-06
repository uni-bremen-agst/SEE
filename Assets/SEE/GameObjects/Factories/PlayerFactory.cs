using SEE.Controls;
using SEE.DataModel;
using SEE.GO.Menu;
using SEE.Utils;
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
            GameObject player = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/DesktopPlayer");
            player.name = PlayerInputType.DesktopPlayer.ToString();
            player.tag = Tags.MainCamera;
            player.GetComponent<DesktopPlayerMovement>().FocusedObject = plane;
            return player;
        }

        /// <summary>
        /// Creates and returns a VR player instantiated from prefab Resources/Prefabs/Players/VRPlayer
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the VR environment</returns>
        public static GameObject CreateVRPlayer()
        {
            GameObject player = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/VRPlayer");
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
            GameObject player = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/HoloLensPlayer");
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
            GameObject player = PrefabInstantiator.InstantiatePrefab("Prefabs/Players/InControl");
            player.name = PlayerInputType.TouchGamepadPlayer.ToString();
            return player;
        }
    }
}
