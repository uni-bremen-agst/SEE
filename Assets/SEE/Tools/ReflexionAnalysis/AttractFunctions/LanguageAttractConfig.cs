using SEE.Scanner;
using SEE.Scanner.Antlr;
using System;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Configuration object which holds parameters to configure the AttractFunction <see cref="LanguageAttractConfig"/>.
    /// </summary>
    public abstract class LanguageAttractConfig : AttractFunctionConfig
    {
        /// <summary>
        /// Token language used by the attract function
        /// </summary>
        [SerializeField]
        public AntlrLanguage.AntlrLanguageType TokenLanguageType { get; set; }

        /// <summary>
        /// Writes all configuration parameters in an <see cref="XElement"/> object.
        /// </summary>
        /// <returns>The xml element object.</returns>
        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute language = new XAttribute("TokenLanguage", TokenLanguageType.ToString());
            config.Add(language);
            return config;
        }
    }
}
