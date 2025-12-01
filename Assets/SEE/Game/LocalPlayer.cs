using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions;
using SEE.GameObjects;
using SEE.GO;
using SEE.GO.Menu;
using SEE.Tools.Livekit;
using SEE.UI;
using SEE.UI.RuntimeConfigMenu;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides access to the game object representing the active local player,
    /// that is, the player executing on this local instance of Unity.
    /// </summary>
    public static class LocalPlayer
    {
        /// <summary>
        /// The game object representing the active local player, that is, the player
        /// executing on this local instance of Unity.
        /// </summary>
        public static GameObject Instance;

        /// <summary>
        /// Returns the <see cref="PlayerMenu"/> attached to the local player <see cref="Instance"/>
        /// or any of its descendants (including inactive ones).
        /// </summary>
        /// <param name="playerMenu">the resulting <see cref="PlayerMenu"/>; <c>null</c> if none
        /// could be found</param>
        /// <returns>true if a <see cref="PlayerMenu"/> could be found</returns>
        internal static bool TryGetPlayerMenu(out PlayerMenu playerMenu)
        {
            if (Instance == null)
            {
                Debug.LogError($"Local player is null'.\n");
                playerMenu = null;
                return false;
            }
            playerMenu = Instance.GetComponentInChildren<PlayerMenu>(includeInactive: true);
            if (playerMenu == null)
            {
                Debug.LogError($"Couldn't find component '{nameof(PlayerMenu)}' "
                               + $"on local player named '{Instance.name}'.\n");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the <see cref="DrawableSurfaces"/> attached to the local player <see cref="Instance"/>
        /// or any of its descendants (including inactive ones).
        /// </summary>
        /// <param name="surfaces">the resulting <see cref="DrawableSurfaces"/>; null if none could be found</param>
        /// <returns>true if a <see cref="DrawableSurfacesRef"/> could be found.</returns>
        internal static bool TryGetDrawableSurfaces(out DrawableSurfaces surfaces)
        {
            if (Instance == null)
            {
                Debug.LogError($"Local player is null'.\n");
                surfaces = null;
                return false;
            }
            surfaces = Instance.GetComponentInChildren<DrawableSurfacesRef>().SurfacesInScene;
            if (surfaces == null)
            {
                Debug.LogError($"Couldn't find component '{nameof(DrawableSurfaceRef)}' "
                               + $"on local player named '{Instance.name}'.\n");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the <see cref="RuntimeConfigMenu"/> attached to the local player <see cref="Instance"/>
        /// or any of its descendants (including inactive ones).
        /// </summary>
        /// <param name="runtimeConfigMenu">the resulting <see cref="RuntimeConfigMenu"/>; null if none could be found</param>
        /// <returns>true if a <see cref="RuntimeConfigMenu"/> could be found.</returns>
        internal static bool TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu)
        {
            if (Instance == null)
            {
                Debug.LogError($"Local player is null'.\n");
                runtimeConfigMenu = null;
                return false;
            }
            runtimeConfigMenu = Instance.GetComponentInChildren<RuntimeConfigMenu>();
            if (runtimeConfigMenu == null)
            {
                Debug.LogError($"Couldn't find component '{nameof(RuntimeConfigMenu)}' "
                               + $"on local player named '{Instance.name}'.\n");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the <see cref="CitiesHolder"/> attached to the local player <see cref="Instance"/>
        /// or any of its descendants (including inactive ones).
        /// </summary>
        /// <param name="citiesHolder">the resulting <see cref="CitiesHolder"/>; null if none could be found</param>
        /// <returns>true if a <see cref="CitiesHolder"/> could be found.</returns>
        internal static bool TryGetCitiesHolder(out CitiesHolder citiesHolder)
        {
            if (Instance == null)
            {
                Debug.LogError($"Local player is null'.\n");
                citiesHolder = null;
                return false;
            }
            citiesHolder = Instance.GetComponentInChildren<CitiesHolder>();
            if (citiesHolder == null)
            {
                Debug.LogError($"Couldn't find component '{nameof(CitiesHolder)}' "
                               + $"on local player named '{Instance.name}'.\n");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the <see cref="ZoomActionDesktop"/> attached to the local player <see cref="Instance"/>
        /// or any of its descendants (including inactive ones).
        /// </summary>
        /// <param name="zoomActionDesktop">the resulting <see cref="ZoomActionDesktop"/>; null if none could be found</param>
        /// <returns>true if a <see cref="ZoomActionDesktop"/> could be found.</returns>
        internal static bool TryGetZoomActionDesktop(out ZoomActionDesktop zoomActionDesktop)
        {
            if (Instance == null)
            {
                Debug.LogError($"Local player is null'.\n");
                zoomActionDesktop = null;
                return false;
            }
            zoomActionDesktop = Instance.GetComponentInChildren<ZoomActionDesktop>();
            if (zoomActionDesktop == null)
            {
                Debug.LogError($"Couldn't find component '{nameof(ZoomActionDesktop)}' "
                               + $"on local player named '{Instance.name}'.\n");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the <see cref="LiveKitVideoManager"/> attached to the local player <see cref="Instance"/>
        /// or any of its descendants (including inactive ones).
        /// </summary>
        /// <param name="manager">the resulting <see cref="LiveKitVideoManager"/>; null if none could be found</param>
        /// <returns>true if a <see cref="LiveKitVideoManager"/> could be found.</returns>
        internal static bool TryGetLiveKitVideoManager(out LiveKitVideoManager manager)
        {
            if (Instance == null)
            {
                Debug.LogError($"Local player is null'.\n");
                manager = null;
                return false;
            }
            manager = Instance.GetComponentInChildren<LiveKitVideoManager>();
            if (manager == null)
            {
                Debug.LogError($"Couldn't find component '{nameof(LiveKitVideoManager)}' "
                               + $"on local player named '{Instance.name}'.\n");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the <see cref="SettingsMenu"/> attached to the local player <see cref="Instance"/>
        /// or any of its descendants (including inactive ones).
        /// </summary>
        /// <param name="menu">the resulting <see cref="SettingsMenu"/>; null if none could be found</param>
        /// <returns>true if a <see cref="SettingsMenu"/> could be found.</returns>
        internal static bool TryGetSettingsMenu(out SettingsMenu menu)
        {
            if (Instance == null)
            {
                Debug.LogError($"Local player is null'.\n");
                menu = null;
                return false;
            }
            menu = Instance.GetComponentInChildren<SettingsMenu>();
            if (menu == null)
            {
                Debug.LogError($"Couldn't find component '{nameof(SettingsMenu)}' "
                               + $"on local player named '{Instance.name}'.\n");
                return false;
            }
            return true;
        }
    }
}
