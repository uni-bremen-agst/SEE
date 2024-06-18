using SEE.Scanner;
using System.Xml.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public abstract class LanguageAttractConfig : AttractFunctionConfig
    {

        public TokenLanguage.TokenLanguageType TokenLanguageType { get; set; }
        // public TokenLanguage TargetLanguage { get; set; }

        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute language = new XAttribute("TokenLanguage", TokenLanguageType.ToString());
            config.Add(language);
            return config;
        }
    }
}
