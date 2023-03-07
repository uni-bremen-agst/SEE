using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
public class SliderTextChanger : MonoBehaviour{
    public TMPro.TextMeshPro text;
        public PhysicsGadgetConfigurableLimitReader sliderReader;
    void Update(){
        text.text = Math.Round(sliderReader.GetValue(), 2).ToString();
    }
}
}
