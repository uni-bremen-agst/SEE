using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Enables or disable game objects and scripts for a given platform.</summary>
   public class PlatformController : MonoBehaviour
   {
      #region Variables

      ///<summary>Selected platforms for the controller.</summary>
      [Header("Configuration")] [Tooltip("Selected platforms for the controller.")] public System.Collections.Generic.List<Model.Enum.Platform> Platforms;

      ///<summary>Enable or disable the 'Objects' for the selected 'Platforms' (default: true).</summary>
      [Tooltip("Enable or disable the 'Objects' for the selected 'Platforms' (default: true).")] public bool Active = true;


      ///<summary>Selected objects for the controller.</summary>
      [Header("GameObjects")] [Tooltip("Selected objects for the controller.")] public GameObject[] Objects;

      ///<summary>Selected scripts for the controller.</summary>
      [Header("MonoBehaviour Scripts")] [Tooltip("Selected scripts for the controller.")] public MonoBehaviour[] Scripts;


      protected Model.Enum.Platform currentPlatform;

      #endregion


      #region MonoBehaviour methods

      protected virtual void Awake()
      {
         if (enabled)
            selectPlatform();
      }

      private void Start()
      {
         //do nothing, just allow to enable/disable the script
      }

      #endregion


      #region Private methods

      protected void selectPlatform()
      {
         currentPlatform = BaseHelper.CurrentPlatform;

         activateGameObjects();
         activateScripts();
      }

      protected void activateGameObjects()
      {
         if (Objects?.Length > 0)
         {
            bool active = Platforms.Contains(currentPlatform) ? Active : !Active;

            foreach (GameObject go in Objects.Where(go => go != null))
            {
               go.SetActive(active);
            }
         }
      }

      protected void activateScripts()
      {
         if (Scripts?.Length > 0)
         {
            bool active = Platforms.Contains(currentPlatform) ? Active : !Active;

            foreach (MonoBehaviour script in Scripts.Where(script => script != null))
            {
               script.enabled = active;
            }
         }
      }

      #endregion
   }
}
// © 2017-2021 crosstales LLC (https://www.crosstales.com)