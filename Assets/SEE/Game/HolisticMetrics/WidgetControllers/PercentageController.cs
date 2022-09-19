using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    public class PercentageController : WidgetController
    {
        [SerializeField] private Text valueText;
        [SerializeField] private Text titleText;
        [SerializeField] private Image fade;
        
        private static readonly HashSet<Type> CommaTypes = new HashSet<Type>(){
            typeof(decimal),
            typeof(double),
            typeof(float)
        };
        
        private static readonly HashSet<Type> IntegerTypes = new HashSet<Type>(){
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(short),
            typeof(int),
            typeof(long)
        };
        
        public override void Display<T>(T value, string title)
        {
            float percentage;
            
            if (typeof(T) == typeof(RangeValue))
            {
                RangeValue rangeValue = value as RangeValue;
                float maxDistance = rangeValue!.Range.Item2 - rangeValue!.Range.Item1;
                float valueDistance = rangeValue.Value - rangeValue.Range.Item1;
                percentage = valueDistance / maxDistance;
            }
            else if (IntegerTypes.Contains(typeof(T)))
            {
                int intValue = (int)(object)value;
                // It has to be between 0 and 100. Take this value as the percentage.
                percentage = intValue / 100f;
            } 
            else if (CommaTypes.Contains(typeof(T)))
            {
                float floatValue = (float)(object)value;
                // If it is greater than one, assume it's a percentage. Check if it's leq 100.
                if (floatValue > 1 && floatValue <= 100)
                {
                    percentage = floatValue / 100f;
                } 
                else if (floatValue >= 0 && floatValue <= 1)  // If it is in [0, 1] take the float itself.
                {
                    percentage = floatValue;
                }
                else
                {
                    throw new ArgumentException($"The value {floatValue} is not a valid percentage");
                }
            }
            else
            {
                throw new ArgumentException($"This widget is not compatible with the data type {typeof(T)}");
            }

            if (percentage > 1)
                percentage = 1;
            
            fade.fillAmount = percentage;
            percentage *= 100;
            valueText.text = percentage.ToString("#.##", CultureInfo.InvariantCulture) + "%";
            titleText.text = title;
        }
    }
}