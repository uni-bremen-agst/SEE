using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    public class SingleNumberDisplayController : WidgetController
    {
        [SerializeField] private Text valueText;
        [SerializeField] private Text titleText;

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>(){
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(decimal),
            typeof(double),
            typeof(float)
        };

        public override void Display<T>(T value, string title)
        {
            if (NumericTypes.Contains(typeof(T)))
            {
                valueText.text = value.ToString();
            }
            else if (typeof(T) == typeof(RangeValue))
            {
                RangeValue castedValue = value as RangeValue;
                valueText.text = castedValue!.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                throw new ArgumentException($"This widget is not compatible with the data type {typeof(T)}");
            }
            
            titleText.text = title;
        }
    }
}