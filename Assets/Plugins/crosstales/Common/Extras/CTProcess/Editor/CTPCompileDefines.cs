#if UNITY_EDITOR
using UnityEditor;

namespace Crosstales.Common.Util
{
   /// <summary>Adds "CT_PROC" define symbol to PlayerSettings define symbols.</summary>
   [InitializeOnLoad]
   public class CTPCompileDefines : Common.EditorTask.BaseCompileDefines
   {
      private const string symbol = "CT_PROC";

      static CTPCompileDefines()
      {
         addSymbolsToAllTargets(symbol);
      }
   }
}
#endif
// © 2021 crosstales LLC (https://www.crosstales.com)