using System;
using UnityEngine.Events;

public interface IControlledInput
{
    string Value { get; set; }
    
    Action<string> OnValueChange { get; set; }
}