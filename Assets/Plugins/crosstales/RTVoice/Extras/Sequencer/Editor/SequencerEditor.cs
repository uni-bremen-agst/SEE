#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'Sequencer'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.Sequencer))]
   public class SequencerEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.Sequencer script;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.Sequencer)target;
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         if (script.isActiveAndEnabled)
         {
            if (script.Sequences?.Length > 0)
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
               EditorGUILayout.HelpBox("Please add an entry to 'Sequences'!", MessageType.Warning);
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
// © 2016-2022 crosstales LLC (https://www.crosstales.com)