#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'Paralanguage'-class.</summary>
   [CustomEditor(typeof(Tool.Paralanguage))]
   [CanEditMultipleObjects]
   public class ParalanguageEditor : Editor
   {
      #region Variables

      private Tool.Paralanguage script;

      #endregion


      #region Editor methods

      public void OnEnable()
      {
         script = (Tool.Paralanguage)target;
      }

      public void OnDisable()
      {
         if (Util.Helper.isEditorMode)
         {
            Speaker.Instance.Silence();
         }
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         if (script.isActiveAndEnabled)
         {
            if (!string.IsNullOrEmpty(script.Text))
            {
               if (Speaker.Instance.isTTSAvailable && EditorUtil.EditorHelper.isRTVoiceInScene)
               {
                  //add stuff if needed
               }
               else
               {
                  EditorUtil.EditorHelper.NoVoicesUI();
               }
            }
            else
            {
               EditorUtil.EditorHelper.SeparatorUI();
               EditorGUILayout.HelpBox("Please enter a 'Text'!", MessageType.Warning);
            }
         }
         else
         {
            EditorUtil.EditorHelper.SeparatorUI();
            EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
         }
      }

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)