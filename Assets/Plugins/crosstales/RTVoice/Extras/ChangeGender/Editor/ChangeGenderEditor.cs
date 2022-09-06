#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'ChangeGender'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.ChangeGender))]
   [CanEditMultipleObjects]
   public class ChangeGenderEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.ChangeGender script;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.ChangeGender)target;
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

         if (script.isActiveAndEnabled)
         {
            if (Crosstales.RTVoice.Speaker.Instance.isTTSAvailable && Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene)
            {
               GUILayout.Label("Action", EditorStyles.boldLabel);

               if (Crosstales.RTVoice.Util.Helper.isEditorMode)
               {
                  if (GUILayout.Button(new GUIContent(" Change Gender", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Refresh, "Change the gender of all voices (useful for eSpeak).")))
                     script.Change();
               }
               else
               {
                  EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
               }
            }
            else
            {
               Crosstales.RTVoice.EditorUtil.EditorHelper.NoVoicesUI();
            }
         }
         else
         {
            EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
         }
      }

      #endregion
   }
}
#endif
// © 2019-2022 crosstales LLC (https://www.crosstales.com)