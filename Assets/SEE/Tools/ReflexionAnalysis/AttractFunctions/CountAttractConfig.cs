using System.Xml.Linq;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Configuration object which holds parameters to configure the AttractFunction <see cref="CountAttract"/>.
    /// </summary>
    [System.Serializable]
    public class CountAttractConfig : AttractFunctionConfig
    {
        /// <summary>
        /// Scaling factor for wanted coupling used in the calculation of <see cref="CountAttract"/>
        /// </summary>
        [SerializeField]
        [RangeAttribute(0,1)]
        public float phi;

        /// <summary>
        /// Scaling factor for wanted coupling used in the calculation of <see cref="CountAttract"/>
        /// </summary>
        public float Phi { get => phi; set => phi = value; }

        /// <summary>
        /// Attract function type of this Attract function.
        /// </summary>
        public override AttractFunction.AttractFunctionType AttractFunctionType { get => AttractFunction.AttractFunctionType.CountAttract;}

        /// <summary>
        /// This constructor initializes a new instance of <see cref="CountAttractConfig"/>.
        /// </summary>
        /// <param name="phi">phi value used by the CountAttract function</param>
        public CountAttractConfig(float phi = 1.0f) 
        {
            this.Phi = phi;
        }

        /// <summary>
        /// Writes all configuration parameters in an <see cref="XElement"/> object.
        /// </summary>
        /// <returns>The xml element object.</returns>
        public override XElement ToXElement()
        {
            XElement config = base.ToXElement();
            XAttribute phi = new XAttribute("Phi", Phi);
            config.Add(phi);
            return config;
        }
    }
}
