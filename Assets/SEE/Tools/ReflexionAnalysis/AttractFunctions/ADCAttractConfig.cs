using static SEE.Scanner.TokenLanguage;
using System.Xml.Linq;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.Document;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    [System.Serializable]
    public class ADCAttractConfig : LanguageAttractConfig
    {
        [SerializeField]
        private DocumentMergingType mergingType;

        public DocumentMergingType MergingType { get => mergingType; private set => mergingType = value; }

        public ADCAttractConfig(TokenLanguageType languageType, DocumentMergingType mergingType) 
        {
            this.MergingType = mergingType;
            this.TokenLanguageType = languageType;
        }
        
        public ADCAttractConfig(DocumentMergingType mergingType = DocumentMergingType.Intersection) : this(TokenLanguageType.Plain, mergingType)
        {
        }

        public override AttractFunction.AttractFunctionType AttractFunctionType { get => AttractFunction.AttractFunctionType.ADCAttract; }

        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute mergingType = new XAttribute("MergingType", MergingType);
            config.Add(mergingType);
            return config;
        }
    }
}
