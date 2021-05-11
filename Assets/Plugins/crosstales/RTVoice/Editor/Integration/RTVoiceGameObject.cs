#if UNITY_EDITOR
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class RTVoiceGameObject
   {
      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/" + Util.Constants.RTVOICE_SCENE_OBJECT_NAME, false, EditorHelper.GO_ID)]
      private static void AddRTVoice()
      {
         EditorHelper.InstantiatePrefab(Util.Constants.RTVOICE_SCENE_OBJECT_NAME);
      }

      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/" + Util.Constants.RTVOICE_SCENE_OBJECT_NAME, true)]
      private static bool AddRTVoiceValidator()
      {
         return !EditorHelper.isRTVoiceInScene;
      }

      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/" + Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME, false, EditorHelper.GO_ID + 1)]
      private static void AddGlobalCache()
      {
         EditorHelper.InstantiatePrefab(Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME);
      }

      [MenuItem("GameObject/" + Util.Constants.ASSET_NAME + "/" + Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME, true)]
      private static bool AddGlobalCacheValidator()
      {
         return !EditorHelper.isGlobalCacheInScene;
      }
   }
}
#endif
// © 2017-2021 crosstales LLC (https://www.crosstales.com)