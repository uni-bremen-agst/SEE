using System;
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

        public override void Display(RangeValue value, string title)
        {
            float maximum = value.Range.Item2 - value.Range.Item1;
            float actual = value.Value - value.Range.Item1;
            float percentage = actual / maximum;
            Display(percentage, title);
        }

        public override void Display(ushort value, string title) => Display((short)value, title);

        public override void Display(uint value, string title) => Display((short)value, title);
        
        public override void Display(ulong value, string title) => Display((short)value, title);
        
        public override void Display(short value, string title)
        {
            if (value > 100)
                throw new ArgumentException("Percentage must not be greater than 100");
            if (value < 0)
                throw new ArgumentException("Percentage must not be less than 0");

            float percentage = value / 100f;
            Display(percentage, title);
        }
        
        public override void Display(int value, string title) => Display((short)value, title);

        public override void Display(long value, string title) => Display((short)value, title);

        public override void Display(float value, string title)
        {
            if (value > 100)
                throw new ArgumentException("Percentage must not be greater than 100");
            if (value < 0)
                throw new ArgumentException("Percentage must not be less than 0");
            if (value > 1)  // Assume that the caller meant it as a percentage between 0 and 100.
                value /= 100;
            fade.fillAmount = value;
            value *= 100;
            valueText.text = value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
            titleText.text = title;
        }
        
        public override void Display(double value, string title) => Display((float)value, title);
        
        public override void Display(decimal value, string title) => Display((float)value, title);
    }
}