using SEE.Controls;
using SEE.DataModel;
using SEE.Utils;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory creating different kinds of players for the various supported environments
    /// (Desktop, VR, AR, Touchpad, Mobile).
    /// </summary>
    internal static class PlayerFactory
    {
        /// <summary>
        /// The path to the prefab of the desktop player prefab
        /// </summary>
        private const string DESKTOP_PLAYER_PREFAB = "Prefabs/Players/DesktopPlayer";

        /// <summary>
        /// The path to the prefab of the VR player prefab
        /// </summary>
        private const string VR_PLAYER_PREFAB = "Prefabs/Players/VRPlayer";

        /// <summary>
        /// The path to the prefab of the HoloLens player prefab
        /// </summary>
        private const string HOLO_LENS_PLAYER_PREFAB = "Prefabs/Players/HoloLensPlayer";

        /// <summary>
        /// The path to the prefab of the InControl player prefab
        /// </summary>
        private const string IN_CONTROL_PLAYER_PREFAB = "Prefabs/Players/InControl";

        /// <summary>
        /// The path to the prefab of the mobile player prefab
        /// </summary>
        private const string MOBILE_PLAYER_PREFAB = "Prefabs/Players/MobilePlayer";

        /// <summary>
        /// Creates and returns a desktop player instantiated from prefab <see cref="DESKTOP_PLAYER_PREFAB"/>
        /// with all required components attached to it.
        /// </summary>
        /// <param name="plane">the culling plane the DesktopPlayerMovement should be focusing</param>
        /// <returns>a player for the desktop environment</returns>
        public static GameObject CreateDesktopPlayer(Plane plane)
        {
            GameObject player = PrefabInstantiator.InstantiatePrefab(DESKTOP_PLAYER_PREFAB);
            player.name = PlayerInputType.DesktopPlayer.ToString();
            player.tag = Tags.MainCamera;
            player.GetComponent<DesktopPlayerMovement>().FocusedObject = plane;
            return player;
        }

        /// <summary>
        /// Creates and returns a VR player instantiated from prefab <see cref="VR_PLAYER_PREFAB"/>
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the VR environment</returns>
        public static GameObject CreateVRPlayer()
        {
            GameObject player = PrefabInstantiator.InstantiatePrefab(VR_PLAYER_PREFAB);
            player.name = PlayerInputType.VRPlayer.ToString();
            return player;
        }

        /// <summary>
        /// Creates and returns a HoloLens player instantiated from prefab <see cref="HOLO_LENS_PLAYER_PREFAB"/>
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the AR environment of HoloLens</returns>
        public static GameObject CreateHololensPlayer()
        {
            GameObject player = PrefabInstantiator.InstantiatePrefab(HOLO_LENS_PLAYER_PREFAB);
            player.name = PlayerInputType.HoloLensPlayer.ToString();
            return player;
        }

        /// <summary>
        /// Creates and returns a player for touchpads or gamepads instantiated from prefab <see cref="IN_CONTROL_PLAYER_PREFAB"/>
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the gamepad/touchpad environment</returns>
        public static GameObject CreateTouchGamepadPlayer()
        {
            GameObject player = PrefabInstantiator.InstantiatePrefab(IN_CONTROL_PLAYER_PREFAB);
            player.name = PlayerInputType.TouchGamepadPlayer.ToString();
            return player;
        }

        /// <summary>
        /// Creates and returns a player for mobile devices instantiated from prefab <see cref="MOBILE_PLAYER_PREFAB"/>
        /// with all required components attached to it.
        /// </summary>
        /// <returns>a player for the gamepad/touchpad environment</returns>
        public static GameObject CreateMobilePlayer()
        {
            GameObject player = PrefabInstantiator.InstantiatePrefab(MOBILE_PLAYER_PREFAB);
            player.name = PlayerInputType.MobilePlayer.ToString();
            return player;
        }
    }
}
