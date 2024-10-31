using System.Xml.Linq;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.Document;
using static SEE.Scanner.Antlr.AntlrLanguage;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Configuration object which holds parameters to configure the AttractFunction <see cref="ADCAttractConfig"/>.
    /// </summary>
    [System.Serializable]
    public class ADCAttractConfig : LanguageAttractConfig
    {
        /// <summary>
        /// Merging type used to create documents based on implementation edges. 
        /// The terms of the source or target node will either be merged by using the intersection
        /// or union.
        /// </summary>
        [SerializeField]
        private DocumentMergingType mergingType;

        /// <summary>
        /// Merging type used to create documents based on implementation edges. 
        /// The terms of the source or target node will either be merged by using the intersection
        /// or union.
        /// </summary>
        public DocumentMergingType MergingType { get => mergingType; set => mergingType = value; }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="ADCAttractConfig"/>.
        /// </summary>
        /// <param name="language">Token language used by the ADCAttract function</param>
        /// <param name="mergingType">merging type used by the ADCAttract function</param>
        public ADCAttractConfig(AntlrLanguageType language, DocumentMergingType mergingType) 
        {
            this.MergingType = mergingType;
            this.TokenLanguageType = language;
        }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="ADCAttractConfig"/>.
        /// </summary>
        /// <param name="mergingType">merging type used by the ADCAttract function</param>
        public ADCAttractConfig(DocumentMergingType mergingType = DocumentMergingType.Intersection) : this(AntlrLanguageType.Plain, mergingType)
        {
        }

        /// <summary>
        /// Attract function type of this Attract function.
        /// </summary>
        public override AttractFunction.AttractFunctionType AttractFunctionType { get => AttractFunction.AttractFunctionType.ADCAttract; }

        /// <summary>
        /// Writes all configuration parameters in an <see cref="XElement"/> object.
        /// </summary>
        /// <returns>The xml element object.</returns>
        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute mergingType = new XAttribute("MergingType", MergingType);
            config.Add(mergingType);
            return config;
        }
    }
}
