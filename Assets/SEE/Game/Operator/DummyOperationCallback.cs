using System;

namespace SEE.Game.Operator
{
    /// <summary>
    /// An implementation of the operation callback intended for an animation which finishes instantly.
    /// Corresponding callbacks (e.g., OnComplete) will thus be called instantly.
    /// </summary>
    /// <typeparam name="T">the type of the callback delegate</typeparam>
    public class DummyOperationCallback<T> : IOperationCallback<T> where T : MulticastDelegate
    {
        // NOTE: We use reflection to invoke the callback because of the generic type parameter.
        //       This is very inefficient (by a factor of ~200, see https://stackoverflow.com/a/3465152),
        //       but AFAIK there is no good way around this.

        public void SetOnComplete(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
        }

        public void SetOnKill(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
        }

        public void SetOnPlay(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
        }

        public void SetOnPause(T callback)
        {
            // Nothing to be done.
        }

        public void SetOnRewind(T callback)
        {
            // Nothing to be done.
        }

        public void SetOnStart(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
        }

        public void SetOnUpdate(T callback)
        {
            // Nothing to be done.
        }
    }
}