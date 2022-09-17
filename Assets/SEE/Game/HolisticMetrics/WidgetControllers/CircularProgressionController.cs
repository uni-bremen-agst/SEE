using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    public class CircularProgressionController : WidgetController
    {
        [SerializeField] private Image circle;
        [SerializeField] private Text text;
        [SerializeField] private Text title;
        
        public override void Display<T>(T value, string titleText)  // TODO: Discuss whether this is bad practice
        {
            title.text = titleText;
            
            if (typeof(T) == typeof(RangeValue))
            {
                RangeValue castedValue = value as RangeValue;
                text.text = castedValue!.Value.ToString("#.##", CultureInfo.InvariantCulture);

                float maximum = castedValue.Range.Item2 - castedValue.Range.Item1;
                float actual = castedValue.Value - castedValue.Range.Item1;

                circle.fillAmount = actual / maximum;
            }
            else
            {
                // This is the "default case" meaning that this Widget is not (yet) compatible with the given data
                // type. In this case, as described in the documentation, an ArgumentException will be thrown.
                throw new ArgumentException($"This widget is not compatible with the data type {typeof(T)}");
            }
        }
    }
}