namespace Crosstales.RTVoice.Util
{
   /// <summary>Context for the asset.</summary>
   public static class Context
   {
      #region Changable variables

      /// <summary>The total number of speeches.</summary>
      public static int NumberOfSpeeches = 0;

      /// <summary>The total number of generated audio files.</summary>
      public static int NumberOfAudioFiles = 0;

      /// <summary>The total number of characters spoken.</summary>
      public static int NumberOfCharacters = 0;

      /// <summary>The total speech length in seconds.</summary>
      public static float TotalSpeechLength = 0;

      /// <summary>The total number of cached speeches.</summary>
      public static int NumberOfCachedSpeeches = 0;

      /// <summary>The total number of non-cached speeches.</summary>>
      public static int NumberOfNonCachedSpeeches = 0;

      /// <summary>The current cache efficiency.</summary>>
      public static float CacheEfficiency
      {
         get
         {
            if (NumberOfNonCachedSpeeches > 0)
               return (float)NumberOfCachedSpeeches / NumberOfNonCachedSpeeches;

            return 0;
         }
      }

      #endregion
   }
}
// © 2020-2021 crosstales LLC (https://www.crosstales.com)