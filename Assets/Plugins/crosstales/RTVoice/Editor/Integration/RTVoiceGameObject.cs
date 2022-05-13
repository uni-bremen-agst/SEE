#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Hierarchy"-menu.</summary>
   public static class RTVoiceGameObject
   {
      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/" + Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME, false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID)]
      private static void AddRTVoice()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab(Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME);
      }

      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/" + Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME, true)]
      private static bool AddRTVoiceValidator()
      {
         return !Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene;
      }

      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/" + Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME, false, Crosstales.RTVoice.EditorUtil.EditorHelper.GO_ID + 1)]
      private static void AddGlobalCache()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab(Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME);
      }

      [MenuItem("GameObject/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/" + Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME, true)]
      private static bool AddGlobalCacheValidator()
      {
         return !Crosstales.RTVoice.EditorUtil.EditorHelper.isGlobalCacheInScene;
      }
   }
}
#endif
// © 2017-2022 crosstales LLC (https://www.crosstales.com)