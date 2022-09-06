#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'VoiceInitalizer'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.VoiceInitializer))]
   public class VoiceInitializerEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.VoiceInitializer script;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.VoiceInitializer)target;
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         if (script.isActiveAndEnabled)
         {
            if (script.AllVoices || script.VoiceNames?.Length > 0)
            {
               if (!Speaker.Instance.isTTSAvailable && Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene)
               {
                  Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();
                  Crosstales.RTVoice.EditorUtil.EditorHelper.NoVoicesUI();
               }
            }
            else
            {
               Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();
               EditorGUILayout.HelpBox("Please add an entry to 'Voice Names'!", MessageType.Warning);
            }
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
// © 2017-2022 crosstales LLC (https://www.crosstales.com)