#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Crosstales.RTVoice.EditorUtil;

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

      public void OnEnable()
      {
         script = (GlobalCache)target;
      }

      public override void OnInspectorGUI()
      {
         DrawDefaultInspector();

         EditorHelper.SeparatorUI();

         if (script.isActiveAndEnabled)
         {
            GUILayout.Label("Cache Information", EditorStyles.boldLabel);

            GUILayout.Space(6);

            showCachedClips = EditorGUILayout.Foldout(showCachedClips, $"Cached Speeches (Clips): {Util.Helper.FormatBytesToHRF(script.CurrentClipCacheSize)}/{Util.Helper.FormatBytesToHRF(script.ClipCacheSize)} ({script.Clips.Count})");
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

            GUILayout.Label($"Cache Efficiency: {Util.Context.CacheEfficiency:P}");

            EditorHelper.SeparatorUI();

            if (GUILayout.Button(new GUIContent(" Clear", EditorHelper.Icon_Delete, "Clears the cache.")))
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
// © 2020-2021 crosstales LLC (https://www.crosstales.com)