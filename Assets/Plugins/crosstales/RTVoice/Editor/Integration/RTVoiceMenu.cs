#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.RTVoice.EditorIntegration
{
   /// <summary>Editor component for the "Tools"-menu.</summary>
   public static class RTVoiceMenu
   {
      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/" + Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME, false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 20)]
      private static void AddRTVoice()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab(Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/" + Crosstales.RTVoice.Util.Constants.RTVOICE_SCENE_OBJECT_NAME, true)]
      private static bool AddRTVoiceValidator()
      {
         return !Crosstales.RTVoice.EditorUtil.EditorHelper.isRTVoiceInScene;
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/" + Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME, false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 40)]
      private static void AddGlobalCache()
      {
         Crosstales.RTVoice.EditorUtil.EditorHelper.InstantiatePrefab(Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Prefabs/" + Crosstales.RTVoice.Util.Constants.GLOBALCACHE_SCENE_OBJECT_NAME, true)]
      private static bool AddGlobalCacheValidator()
      {
         return !Crosstales.RTVoice.EditorUtil.EditorHelper.isGlobalCacheInScene;
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Help/Manual", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 600)]
      private static void ShowManual()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_MANUAL_URL);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Help/API", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 610)]
      private static void ShowAPI()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_API_URL);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Help/Forum", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 620)]
      private static void ShowForum()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_FORUM_URL);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Help/Product", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 630)]
      private static void ShowProduct()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_WEB_URL);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Help/Videos/Promo", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 650)]
      private static void ShowVideoPromo()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_VIDEO_PROMO);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Help/Videos/Tutorial", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 660)]
      private static void ShowVideoTutorial()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_VIDEO_TUTORIAL);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Help/Videos/All Videos", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 680)]
      private static void ShowAllVideos()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_SOCIAL_YOUTUBE);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/Help/3rd Party Assets", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 700)]
      private static void Show3rdPartyAV()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_3P_URL);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/About/Unity AssetStore", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 800)]
      private static void ShowUAS()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.EditorUtil.EditorConstants.ASSET_URL);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/About/" + Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR, false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 820)]
      private static void ShowCT()
      {
         Crosstales.Common.Util.NetworkHelper.OpenURL(Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR_URL);
      }

      [MenuItem("Tools/" + Crosstales.RTVoice.Util.Constants.ASSET_NAME + "/About/Info", false, Crosstales.RTVoice.EditorUtil.EditorHelper.MENU_ID + 840)]
      private static void ShowInfo()
      {
         EditorUtility.DisplayDialog(Crosstales.RTVoice.Util.Constants.ASSET_NAME + " - About",
            "Version: " + Crosstales.RTVoice.Util.Constants.ASSET_VERSION +
            System.Environment.NewLine +
            System.Environment.NewLine +
            "© 2015-2022 by " + Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR +
            System.Environment.NewLine +
            System.Environment.NewLine +
            Crosstales.RTVoice.Util.Constants.ASSET_AUTHOR_URL +
            System.Environment.NewLine, "Ok");
      }
   }
}
#endif
// © 2015-2022 crosstales LLC (https://www.crosstales.com)