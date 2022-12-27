using System;

namespace SEE.Game.UI.Window
{
    public class UnsupportedTypeException : Exception
    {
        public UnsupportedTypeException(Type expectedType, Type actualType) 
            : base($"Expected a value of type {expectedType}, but got {actualType}!")
        {
        }
    }
}