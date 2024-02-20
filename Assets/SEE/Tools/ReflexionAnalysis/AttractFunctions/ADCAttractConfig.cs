using SEE.UI.Window.CodeWindow;
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

        public ADCAttractConfig(TokenLanguage targetLanguage, DocumentMergingType mergingType) 
        {
            this.MergingType = mergingType;
            this.TargetLanguage = targetLanguage;
            this.AttractFunctionType = AttractFunction.AttractFunctionType.ADCAttract;
        }
        
        public ADCAttractConfig(DocumentMergingType mergingType = DocumentMergingType.Intersection) : this(TokenLanguage.Plain, mergingType)
        {
        }

        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute mergingType = new XAttribute("MergingType", MergingType);
            config.Add(mergingType);
            return config;
        }
    }
}
