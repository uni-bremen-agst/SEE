using MathNet.Numerics.Distributions;
using OpenAI.Chat;
using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;

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
