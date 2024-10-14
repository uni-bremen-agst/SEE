using SEE.Scanner.Antlr;
using System.Xml.Linq;
using UnityEngine;
using static SEE.Scanner.Antlr.AntlrLanguage;
using static SEE.Scanner.TokenLanguage;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Configuration object which holds parameters to configure the AttractFunction <see cref="NBAttractConfig"/>.
    /// </summary>
    [System.Serializable]
    public class NBAttractConfig : LanguageAttractConfig
    {
        /// <summary>
        /// Wether cda terms should be used or not.
        /// </summary>
        [SerializeField]
        private bool useCDA;

        /// <summary>
        /// Wether cda terms should be used or not.
        /// </summary>
        [SerializeField]
        public bool UseCDA { get => useCDA; set => useCDA = value; }

        /// <summary>
        /// Wether standard terms should be used or not.
        /// </summary>
        [SerializeField]
        private bool useStandardTerms;

        /// <summary>
        /// Wether standard terms should be used or not.
        /// </summary>
        public bool UseStandardTerms { get => useStandardTerms; set => useStandardTerms = value; }

        /// <summary>
        /// Alpha smoothing used within the classifier
        /// </summary>
        [SerializeField]
        private double alphaSmoothing = 1.0;

        /// <summary>
        /// Alpha smoothing used within the classifier
        /// </summary>
        public double AlphaSmoothing { get => alphaSmoothing; set => alphaSmoothing = value; }

        /// <summary>
        /// Attract function type of this Attract function.
        /// </summary>
        public override AttractFunction.AttractFunctionType AttractFunctionType { get => AttractFunction.AttractFunctionType.NBAttract; }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="NBAttractConfig"/>.
        /// </summary>
        /// <param name="useCda">Wether cda terms should be used by the function or not.</param>
        public NBAttractConfig(bool useCda = true) : this(AntlrLanguageType.Plain, useCda)
        {
        }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="NBAttractConfig"/>.
        /// </summary>
        /// <param name="language">Token language used by the ADCAttract function</param>
        /// <param name="useCda">Wether cda terms should be used by the function or not.</param>
        public NBAttractConfig(AntlrLanguageType language, bool useCda = true) : this(language, useCda, true)
        {
        }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="NBAttractConfig"/>.
        /// </summary>
        /// <param name="language">Token language used by the ADCAttract function</param>
        /// <param name="useCda">Wether cda terms should be used by the function or not.</param>
        /// <param name="useStandardTerms">Wether standard terms should be used by the function or not.</param>
        public NBAttractConfig(AntlrLanguageType language, bool useCda, bool useStandardTerms)
        {
            this.UseCDA = useCda;
            this.UseStandardTerms = useStandardTerms;
            this.TokenLanguageType = language;
        }

        /// <summary>
        /// Writes all configuration parameters in an <see cref="XElement"/> object.
        /// </summary>
        /// <returns>The xml element object.</returns>
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
