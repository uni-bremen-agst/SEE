using System;
using JetBrains.Annotations;
using UnityEditor;

namespace Dissonance.Editor
{
    internal static class UndoHelper
    {
        public static void ChangeWithUndo<T, TO>(this TO obj, string message, [CanBeNull] T newValue, [CanBeNull] T value, Action<T> set)
            where TO : UnityEngine.Object
        {
            if (!Equals(value, newValue))
            {
                Undo.RecordObject(obj, message);
                set(newValue);
            }
        }
    }
}
