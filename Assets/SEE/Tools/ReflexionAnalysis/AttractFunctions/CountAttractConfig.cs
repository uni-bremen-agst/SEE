using OpenAI.Chat;
using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    [System.Serializable]
    public class CountAttractConfig : AttractFunctionConfig
    {
        [SerializeField]
        private float phi;

        [SerializeField]
        private float delta;

        public float Phi { get => phi; set => phi = value; }
        public float Delta { get => delta; set => delta = value; }
        public CountAttractConfig(float phi = 1.0f, float delta = 0.0f) 
        {
            this.Phi = phi;
            this.Delta = delta;
            this.AttractFunctionType = AttractFunction.AttractFunctionType.CountAttract;
        }
    }
}
