using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

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

        public NBAttractConfig(bool useCda = true) : this(TokenLanguage.Plain, useCda)
        {
        }

        public NBAttractConfig(TokenLanguage language, bool useCda = true) : this(language, useCda, true)
        {
        }

        public NBAttractConfig(TokenLanguage language, bool useCda, bool useStandardTerms)
        {
            this.UseCDA = useCda;
            this.UseStandardTerms = useStandardTerms;
            this.TargetLanguage = language;
            this.AttractFunctionType = AttractFunction.AttractFunctionType.NBAttract;
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
