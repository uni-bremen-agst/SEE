using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Helper to reset the necessary settings.</summary>
   public class CTHelper : MonoBehaviour
   {
      public static CTHelper Instance { get; private set; }

      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
      private static void initialize()
      {
         BaseHelper.ApplicationIsPlaying = true;
      }

      [RuntimeInitializeOnLoadMethod]
      private static void create()
      {
         BaseHelper.ApplicationIsPlaying = true;
         //Debug.Log("initialize");

         if (!BaseHelper.isEditorMode)
         {
            GameObject go = new GameObject("_CTHelper");
            go.AddComponent<CTHelper>();
            DontDestroyOnLoad(go);
         }
      }

      private void Awake()
      {
         Instance = this;
      }

      private void OnApplicationQuit()
      {
         BaseHelper.ApplicationIsPlaying = false;
      }
   }

#if UNITY_EDITOR
   [UnityEditor.CustomEditor(typeof(CTHelper))]
   public class CTHelperEditor : UnityEditor.Editor
   {
      public override void OnInspectorGUI()
      {
         UnityEditor.EditorGUILayout.HelpBox("This helper ensures the flawless working of the assets from 'crosstales LLC'.\nPlease do not delete it from the hierarchy.", UnityEditor.MessageType.Info);
      }
   }
#endif
}
// © 2020-2021 crosstales LLC (https://www.crosstales.com)