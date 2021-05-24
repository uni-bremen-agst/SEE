using UnityEngine;

namespace Crosstales.RTVoice.Util
{
   /// <summary>Setup the project to use RT-Voice.</summary>
#if UNITY_EDITOR
   [UnityEditor.InitializeOnLoadAttribute]
#endif
   public class SetupProject
   {
      #region Constructor

      static SetupProject()
      {
         setup();
      }

      #endregion


      #region Public methods

      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
      private static void setup()
      {
         Crosstales.Common.Util.Singleton<Speaker>.PrefabPath = "Prefabs/RTVoice";
         Crosstales.Common.Util.Singleton<Speaker>.GameObjectName = "RTVoice";
         Crosstales.Common.Util.Singleton<GlobalCache>.PrefabPath = "Prefabs/GlobalCache";
      }

      #endregion
   }
}
// © 2020-2021 crosstales LLC (https://www.crosstales.com)