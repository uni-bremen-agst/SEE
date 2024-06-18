using System.Xml.Linq;
using UnityEngine;
using static SEE.Scanner.TokenLanguage;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    [System.Serializable]
    public class NBAttractConfig : LanguageAttractConfig
    {
        [SerializeField]
        private bool useCDA;

        public bool UseCDA { get => useCDA; set => useCDA = value; }

        [SerializeField]
        private bool useStandardTerms;

        public bool UseStandardTerms { get => useStandardTerms; set => useStandardTerms = value; }

        [SerializeField]
        private double alphaSmoothing = 1.0;

        public double AlphaSmoothing { get => alphaSmoothing; set => alphaSmoothing = value; }

        public override AttractFunction.AttractFunctionType AttractFunctionType { get => AttractFunction.AttractFunctionType.NBAttract; }

        public NBAttractConfig(bool useCda = true) : this(TokenLanguageType.Plain, useCda)
        {
        }

        public NBAttractConfig(TokenLanguageType language, bool useCda = true) : this(language, useCda, true)
        {
        }

        public NBAttractConfig(TokenLanguageType language, bool useCda, bool useStandardTerms)
        {
            this.UseCDA = useCda;
            this.UseStandardTerms = useStandardTerms;
            this.TokenLanguageType = language;
        }

        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute useStandardTerms = new XAttribute("UseStandardTerms", UseStandardTerms);
            config.Add(useStandardTerms);
            XAttribute useCda = new XAttribute("UseCda", UseCDA);
            config.Add(useCda);
            return config;
        }
    }
}
