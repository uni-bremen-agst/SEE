using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game;
using SEEEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public delegate void ChangeHandler(string value);

public class PageController : MonoBehaviour
{
    public TextMeshProUGUI headlineText;
    private Dictionary<string, string> inputs;
    private SEECity city;

    public void Start()
    {
        var impl = GameObject.Find("Implementation");
        city = impl.GetComponent<SEECity>();

        LinkDropdown("WidthMetric", city.WidthMetric, value => city.WidthMetric = value,
            EnumToStr<NumericAttributeNames>());
        LinkDropdown("HeightMetric", city.HeightMetric, value => city.HeightMetric = value,
            EnumToStr<NumericAttributeNames>());
        LinkDropdown("DepthMetric", city.DepthMetric, value => city.DepthMetric = value,
            EnumToStr<NumericAttributeNames>());
        LinkDropdown("LeafStyleMetric", city.LeafStyleMetric, value => city.LeafStyleMetric = value,
            EnumToStr<NumericAttributeNames>());
        LinkSlider("NumberOfColors", city.LeafNodeColorRange.NumberOfColors.ToString(),
            value => city.LeafNodeColorRange.NumberOfColors = UInt32.Parse(value), 1, 15);
    }

    public void LinkDropdown(string inputName, string initialValue, Action<string> onChange, List<string> values)
    {
        var dropdown = Array.Find(GetComponentsInChildren<DropdownControlledInput>(),
                           input => input.fieldName == inputName) ??
                       throw new ArgumentException($"No dropdown with name ({inputName}) found.");
        dropdown.OnValueChange = onChange;
        dropdown.Values = values;
        dropdown.Value = initialValue;
    }

    public void LinkSlider(string inputName, string initialVaue, Action<string> onChange, int min, int max)
    {
        var slider = Array.Find(GetComponentsInChildren<SliderControlledInput>(),
                         input => input.fieldName == inputName) ??
                     throw new ArgumentException($"No slider with name ({inputName}) found.");
        slider.OnValueChange = onChange;
        slider.Value = initialVaue;
        slider.sliderManager.mainSlider.minValue = min;
        slider.sliderManager.mainSlider.maxValue = max;
    }

    List<string> EnumToStr<EnumType>() where EnumType : Enum
    {
        return Enum.GetValues(typeof(EnumType)).Cast<EnumType>().Select(v => v.ToString())
            .ToList();
    }
}