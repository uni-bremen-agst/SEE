#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Crosstales.RTVoice.EditorExtension
{
   /// <summary>Custom editor for the 'GlobalCache'-class.</summary>
   [CustomEditor(typeof(GlobalCache))]
   public class GlobalCacheEditor : Editor
   {
      #region Variables

      private GlobalCache script;

      private bool showCachedClips;

      #endregion


      #region Editor methods

      private void OnEnable()
      {
         script = (GlobalCache)target;
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

         if (script.isActiveAndEnabled)
         {
            GUILayout.Label("Cache Information", EditorStyles.boldLabel);

            GUILayout.Space(6);

            showCachedClips = EditorGUILayout.Foldout(showCachedClips, $"Cached Speeches (Clips): {Crosstales.RTVoice.Util.Helper.FormatBytesToHRF(script.CurrentClipCacheSize)}/{Crosstales.RTVoice.Util.Helper.FormatBytesToHRF(script.ClipCacheSize)} ({script.Clips.Count})");
            if (showCachedClips)
            {
               EditorGUI.indentLevel++;

               foreach (System.Collections.Generic.KeyValuePair<Crosstales.RTVoice.Model.Wrapper, AudioClip> pair in script.Clips)
               {
                  EditorGUILayout.SelectableLabel(pair.Key.Text, GUILayout.Height(16), GUILayout.ExpandHeight(false));
               }

               EditorGUI.indentLevel--;
            }

            GUILayout.Space(6);

            GUILayout.Label($"Cache Efficiency: {Crosstales.RTVoice.Util.Context.CacheEfficiency:P}");

            Crosstales.RTVoice.EditorUtil.EditorHelper.SeparatorUI();

            if (GUILayout.Button(new GUIContent(" Clear", Crosstales.RTVoice.EditorUtil.EditorHelper.Icon_Delete, "Clears the cache.")))
               script.ClearCache();
         }
         else
         {
            EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
         }
      }

      public override bool RequiresConstantRepaint()
      {
         return true;
      }

      #endregion
   }
}
#endif
// © 2020-2022 crosstales LLC (https://www.crosstales.com)