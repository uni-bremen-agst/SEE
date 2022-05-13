#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'PlatformProvider'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.PlatformProvider))]
   [CanEditMultipleObjects]
   public class PlatformProviderEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.PlatformProvider script;

      #endregion


      #region Properties

      public static bool isPrefabInScene => GameObject.Find("PlatformProvider") != null;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.PlatformProvider)target;
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         if (script.isActiveAndEnabled)
         {
            //do something
         }
         else
         {
            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();
            EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
         }
      }

      #endregion
   }
}
#endif
// © 2021-2022 crosstales LLC (https://www.crosstales.com)