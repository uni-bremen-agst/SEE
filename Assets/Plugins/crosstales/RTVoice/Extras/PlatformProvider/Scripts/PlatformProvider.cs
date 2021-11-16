using UnityEngine;
using System;

namespace Crosstales.RTVoice.Tool
{
   /// <summary>Allows to configure voice providers per platform.</summary>
   //[ExecuteInEditMode]
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_paralanguage.html")] //TODO update URL
   public class PlatformProvider : MonoBehaviour
   {
      [Header("Configuration Settings")] public PlatformProviderTuple[] Configuration;

      [Header("Default")] public Provider.BaseCustomVoiceProvider DefaultVoiceProvider;

      private void Start()
      {
         Crosstales.Common.Model.Enum.Platform currentPlatform = Util.Helper.CurrentPlatform;

         bool found = false;

         foreach (var config in Configuration)
         {
            if (config.Platform == currentPlatform)
            {
               if (config.CustomVoiceProvider == null)
               {
                  Speaker.Instance.CustomMode = false;
               }
               else
               {
                  Speaker.Instance.CustomProvider = config.CustomVoiceProvider;
                  Speaker.Instance.CustomMode = true;
               }

               found = true;
               break;
            }
         }

         if (!found)
         {
            if (DefaultVoiceProvider == null)
            {
               Speaker.Instance.CustomMode = false;
            }
            else
            {
               Speaker.Instance.CustomProvider = DefaultVoiceProvider;
               Speaker.Instance.CustomMode = true;
            }
         }
      }
   }

   [Serializable]
   public class PlatformProviderTuple
   {
      public Crosstales.Common.Model.Enum.Platform Platform;
      public Provider.BaseCustomVoiceProvider CustomVoiceProvider;
   }
}
// © 2021 crosstales LLC (https://www.crosstales.com)