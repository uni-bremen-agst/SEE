using SEE.Scanner;
using System.Xml.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class LanguageAttractConfig : AttractFunctionConfig
    {
        public TokenLanguage TargetLanguage { get; set; }

        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute language = new XAttribute("TokenLanguage", TargetLanguage.ToString());
            config.Add(language);
            return config;
        }
    }
}
