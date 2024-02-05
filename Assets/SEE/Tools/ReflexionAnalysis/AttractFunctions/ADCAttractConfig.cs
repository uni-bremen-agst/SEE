using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.Document;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class ADCAttractConfig : LanguageAttractConfig
    {
        public DocumentMergingType MergingType { get; set; }

        public ADCAttractConfig(TokenLanguage targetLanguage, DocumentMergingType mergingType) 
        {
            this.MergingType = mergingType;
            this.TargetLanguage = targetLanguage;
        }
        
        public ADCAttractConfig(DocumentMergingType mergingType = DocumentMergingType.Intersection) : this(TokenLanguage.Plain, mergingType)
        {
        }
    }
}
