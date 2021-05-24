#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Crosstales.RTVoice.EditorTask
{
   /// <summary>Automatically adds the necessary prefabs to the current scene.</summary>
   [InitializeOnLoad]
   public class AutoInitialize
   {
      #region Variables

      private static Scene currentScene;

      #endregion


      #region Constructor

      static AutoInitialize()
      {
         //UnityEngine.Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

         EditorApplication.hierarchyChanged += hierarchyWindowChanged;
      }

      #endregion


      #region Private static methods

      private static void hierarchyWindowChanged()
      {
         if (currentScene != EditorSceneManager.GetActiveScene())
         {
            if (EditorUtil.EditorConfig.PREFAB_AUTOLOAD)
            {
               if (!EditorUtil.EditorHelper.isRTVoiceInScene)
                  EditorUtil.EditorHelper.InstantiatePrefab(Util.Constants.RTVOICE_SCENE_OBJECT_NAME);
            }

            currentScene = EditorSceneManager.GetActiveScene();
         }
      }

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)