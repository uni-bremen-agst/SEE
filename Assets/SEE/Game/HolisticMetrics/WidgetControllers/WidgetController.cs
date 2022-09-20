using System;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    public abstract class WidgetController : MonoBehaviour
    {
        public virtual void Display(RangeValue value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }

        public virtual void Display(ushort value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }
        
        public virtual void Display(uint value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }
        
        public virtual void Display(ulong value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }
        
        public virtual void Display(short value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }
        
        public virtual void Display(int value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }
        
        public virtual void Display(long value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }

        public virtual void Display(float value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }

        public virtual void Display(double value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }
        
        public virtual void Display(decimal value, string title)
        {
            throw new ArgumentException("This widget is not compatible with the given data type.");
        }
    }
}