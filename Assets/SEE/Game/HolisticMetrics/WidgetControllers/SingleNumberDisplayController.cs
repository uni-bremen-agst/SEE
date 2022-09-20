using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    public class SingleNumberDisplayController : WidgetController
    {
        [SerializeField] private Text valueText;
        [SerializeField] private Text titleText;

        public override void Display(RangeValue value, string title) => Display(value.Value, title);

        public override void Display(ushort value, string title) => Display((long)value, title);
        
        public override void Display(uint value, string title) => Display((long)value, title);
        
        public override void Display(ulong value, string title) => Display((long)value, title);
        
        public override void Display(short value, string title) => Display((long)value, title);
        
        public override void Display(int value, string title) => Display((long)value, title);

        public override void Display(long value, string title)
        {
            valueText.text = value.ToString();
            titleText.text = title;
        }
        
        public override void Display(float value, string title) => Display((decimal)value, title);
        
        public override void Display(double value, string title) => Display((long)value, title);

        public override void Display(decimal value, string title)
        {
            valueText.text = value.ToString("0.##", CultureInfo.InvariantCulture);
            titleText.text = title;
        }
    }
}