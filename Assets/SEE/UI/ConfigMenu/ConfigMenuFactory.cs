﻿// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;
#if INCLUDE_STEAM_VR
using Valve.VR;
#endif

namespace SEE.UI.ConfigMenu
{
    /// <summary>
    /// The script is responsible for constructing a config menu to allow
    /// the user to configure code cities. In addition, its runtime behavior
    /// is modified, e.g. ,hotkey handling to show/hide the menu.
    ///
    /// This gets usually attached to a player (currently VR/Desktop).
    /// </summary>
    public class ConfigMenuFactory : DynamicUIBehaviour
    {
        private const string configMenuPrefabPath = "Prefabs/UI/ConfigMenu";
#if INCLUDE_STEAM_VR
        private SteamVR_Action_Boolean openAction;
        private readonly SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.Any;
#endif
        private GameObject configMenuPrefab;
        private ConfigMenu configMenu;

        private void Awake()
        {
            configMenuPrefab = PrefabInstantiator.LoadPrefab(configMenuPrefabPath);
            BuildConfigMenu(ConfigMenu.DefaultEditableInstance(), false);
        }

        private void Start()
        {
            if (SceneSettings.InputType == PlayerInputType.VRPlayer)
            {
                try
                {
#if INCLUDE_STEAM_VR
                    openAction = SteamVR_Actions._default?.OpenSettingsMenu;
#endif
                }
                catch (Exception e)
                {
                    Debug.LogError($"VR is not working: {e.Message} {e.StackTrace}\n");
                }
            }
        }

        /// <summary>
        /// Creates a new configuration menu for <paramref name="instanceToEdit"/>. If
        /// <paramref name="turnMenuOn"/>, the configuration menu will be turned on.
        /// </summary>
        /// <param name="instanceToEdit">the code city to be configured</param>
        /// <param name="turnMenuOn">whether the configuration menu should be turned on</param>
        private void BuildConfigMenu(EditableInstance instanceToEdit, bool turnMenuOn)
        {
            GameObject configMenuGo = Instantiate(configMenuPrefab);
            configMenuGo.name = configMenuPrefab.name;
            configMenuGo.transform.SetSiblingIndex(0);
            configMenu = configMenuGo.MustGetComponent<ConfigMenu>();
            configMenu.CurrentlyEditing = instanceToEdit;
            configMenu.OnInstanceChangeRequest.AddListener(ReplaceMenu);
            if (turnMenuOn)
            {
                configMenu.On();
            }
        }

        private void ReplaceMenu(EditableInstance newInstance)
        {
            Destroyer.Destroy(configMenu.gameObject);
            BuildConfigMenu(newInstance, true);
        }

        private void Update()
        {
            switch (SceneSettings.InputType)
            {
                case PlayerInputType.DesktopPlayer:
                    HandleDesktopUpdate();
                    break;
                case PlayerInputType.VRPlayer:
                    HandleVRUpdate();
                    break;
                default:
                    throw new System.NotImplementedException($"ConfigMenuFactory.Update not implemented for {SceneSettings.InputType}.");
            }
        }

        private void HandleDesktopUpdate()
        {
            if (SEEInput.ToggleConfigMenu())
            {
                configMenu.Toggle();
            }
        }

        private static void HandleVRUpdate()
        {
#if INCLUDE_STEAM_VR

            if (openAction != null && openAction.GetStateDown(inputSource))
            {
                configMenu.Toggle();
            }
#endif
        }
    }
}
