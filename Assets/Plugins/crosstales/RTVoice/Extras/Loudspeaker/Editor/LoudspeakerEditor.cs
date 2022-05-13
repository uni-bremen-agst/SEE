﻿#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'Loudspeaker'-class.</summary>
   [CustomEditor(typeof(Crosstales.RTVoice.Tool.Loudspeaker))]
   public class LoudspeakerEditor : Editor
   {
      #region Variables

      private Crosstales.RTVoice.Tool.Loudspeaker script;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (Crosstales.RTVoice.Tool.Loudspeaker)target;
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         if (script.isActiveAndEnabled)
         {
            if (script.Source != null)
            {
               //add stuff if needed
            }
            else
            {
               Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();
               EditorGUILayout.HelpBox("Please add a 'Source'!", MessageType.Warning);
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