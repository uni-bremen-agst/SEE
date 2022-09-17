using System;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    public abstract class WidgetController : MonoBehaviour
    {
        /// <summary>
        /// This method displays a given value on the widget this WidgetController is attached to.
        /// </summary>
        /// <param name="value">
        /// The value that you wish to display.
        /// </param>
        /// <param name="title">The name of this metric, this will appear somewhere near the widget.</param>
        /// <typeparam name="T">This just exists to accept any given value type.</typeparam>
        /// <exception cref="ArgumentException">
        /// Thrown when this method does not contain an implementation for the type of data passed in the parameter
        /// "value".
        /// </exception>
        public abstract void Display<T>(T value, string title);
    }
}