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

        public override void Display(RangeValue value, string titleText)
        {
            title.text = titleText;
            text.text = value.Value.ToString("0.##", CultureInfo.InvariantCulture);
            float maximum = value.Range.Item2 - value.Range.Item1;
            float actual = value.Value - value.Range.Item1;
            circle.fillAmount = actual / maximum;
        }
    }
}