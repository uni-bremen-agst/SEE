using System;

namespace SEE.Game.Operator
{
    /// <summary>
    /// An implementation of the operation callback intended for an animation which finishes instantly.
    /// Corresponding callbacks (e.g., OnComplete) will thus be called instantly.
    /// </summary>
    /// <typeparam name="T">the type of the callback delegate</typeparam>
    /// <remarks>
    /// Use callbacks of this type sparingly, as they are very inefficient.
    /// </remarks>
    public class DummyOperationCallback<T> : IOperationCallback<T> where T : MulticastDelegate
    {
        // NOTE: We use reflection to invoke the callback because of the generic type parameter.
        //       This is very inefficient (by a factor of ~200, see https://stackoverflow.com/a/3465152),
        //       but AFAIK there is no good way around this.

        public IOperationCallback<T> OnComplete(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
            return this;
        }

        public IOperationCallback<T> OnKill(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
            return this;
        }

        public IOperationCallback<T> OnPlay(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
            return this;
        }

        public IOperationCallback<T> OnPause(T callback)
        {
            // Nothing to be done.
            return this;
        }

        public IOperationCallback<T> OnRewind(T callback)
        {
            // Nothing to be done.
            return this;
        }

        public IOperationCallback<T> OnStart(T callback)
        {
            // Happens instantly.
            callback.DynamicInvoke();
            return this;
        }

        public IOperationCallback<T> OnUpdate(T callback)
        {
            // Nothing to be done.
            return this;
        }
    }
}
