using System.Xml.Linq;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    [System.Serializable]
    public class CountAttractConfig : AttractFunctionConfig
    {
        [SerializeField]
        private float phi;

        public float Phi { get => phi; set => phi = value; }
        public override AttractFunction.AttractFunctionType AttractFunctionType { get => AttractFunction.AttractFunctionType.CountAttract;}

        public CountAttractConfig(float phi = 1.0f) 
        {
            this.Phi = phi;
        }

        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute phi = new XAttribute("Phi", Phi);
            config.Add(phi);
            return config;
        }
    }
}
