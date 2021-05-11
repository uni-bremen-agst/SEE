﻿#if UNITY_EDITOR && !UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityEngine;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Unity "Preferences" extension.</summary>
   public class ConfigPreferences : ConfigBase
   {
      #region Variables

      private static int tab;
      private static int lastTab;
      private static ConfigPreferences cp;

      #endregion


      #region Static methods

      [PreferenceItem(Util.Constants.ASSET_NAME_SHORT)]
      private static void PreferencesGUI()
      {
         if (cp == null)
         {
            cp = CreateInstance(typeof(ConfigPreferences)) as ConfigPreferences;
         }

         tab = GUILayout.Toolbar(tab, new[] {"Configuration", "Help", "About"});

         if (tab != lastTab)
         {
            lastTab = tab;
            GUI.FocusControl(null);
         }

         switch (tab)
         {
            case 0:
            {
               cp.showConfiguration();

               EditorHelper.SeparatorUI();

               if (GUILayout.Button(new GUIContent(" Reset", EditorHelper.Icon_Reset, "Resets the configuration settings for this project.")))
               {
                  if (EditorUtility.DisplayDialog("Reset configuration?", "Reset the configuration of " + Util.Constants.ASSET_NAME + "?", "Yes", "No"))
                  {
                     Util.Config.Reset();
                     EditorConfig.Reset();
                     save();
                  }
               }

               GUILayout.Space(6);
               break;
            }
            case 1:
               cp.showHelp();
               break;
            default:
               cp.showAbout();
               break;
         }

         if (GUI.changed)
         {
            save();
         }
      }

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)