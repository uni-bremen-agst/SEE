using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
public class LeverTextChanger : MonoBehaviour{
    public TMPro.TextMeshPro text;
        public PhysicsGadgetHingeAngleReader sliderReader;
    void Update(){
        text.text = Math.Round(sliderReader.GetValue(), 2).ToString();
    }
}
}
