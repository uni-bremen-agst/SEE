#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'Paralanguage'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.Paralanguage))]
   [CanEditMultipleObjects]
   public class ParalanguageEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.Paralanguage script;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.Paralanguage)target;
      }

      private void OnDisable()
      {
         if (Crosstales.RTVoice.Util.Helper.isEditorMode)
            Speaker.Instance.Silence();
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         if (script.isActiveAndEnabled)
         {
            if (!string.IsNullOrEmpty(script.Text))
            {
               if (Speaker.Instance.isTTSAvailable && Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene)
               {
                  //add stuff if needed
               }
               else
               {
                  Crosstales.RTVoice.EditorUtil.EditorHelper.NoVoicesUI();
               }
            }
            else
            {
               Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();
               EditorGUILayout.HelpBox("Please enter a 'Text'!", MessageType.Warning);
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