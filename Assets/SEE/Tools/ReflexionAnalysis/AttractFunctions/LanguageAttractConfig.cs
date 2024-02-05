using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class LanguageAttractConfig : AttractFunctionConfig
    {
        public TokenLanguage TargetLanguage { get; set; }
    }
}
