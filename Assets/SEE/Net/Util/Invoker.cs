using System;
using System.Threading;

namespace SEE.Net.Util
{

    /// <summary>
    /// Invokes function after some time.
    /// </summary>
    public static class Invoker
    {
        /// <summary>
        /// Invokes <paramref name="function"/> after <paramref name="time"/> seconds.
        /// </summary>
        /// <typeparam name="T1">First argument type.</typeparam>
        /// <typeparam name="T2">Second argument type.</typeparam>
        /// <typeparam name="T3">Third argument type.</typeparam>
        /// <param name="function">The function to be invoked.</param>
        /// <param name="time">Time in seconds before invoking.</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        public static void Invoke(Action function, float time, params object[] args)
        {
            new Thread(() =>
            {
                Thread.Sleep((int)(time * 1000.0f));
                function.DynamicInvoke(args);
            }).Start();
        }

        /// <summary>
        /// Invokes <paramref name="function"/> with given arguments after
        /// <paramref name="time"/> seconds.
        /// </summary>
        /// <typeparam name="T1">First argument type.</typeparam>
        /// <param name="function">The function to be invoked.</param>
        /// <param name="time">Time in seconds before invoking.</param>
        /// <param name="arg1">First argument</param>
        public static void Invoke<T1>(Action<T1> function, float time, T1 arg1)
        {
            new Thread(() =>
            {
                Thread.Sleep((int)(time * 1000.0f));
                function.Invoke(arg1);
            }).Start();
        }

        /// <summary>
        /// Invokes <paramref name="function"/> with given arguments after
        /// <paramref name="time"/> seconds.
        /// </summary>
        /// <typeparam name="T1">First argument type.</typeparam>
        /// <typeparam name="T2">Second argument type.</typeparam>
        /// <param name="function">The function to be invoked.</param>
        /// <param name="time">Time in seconds before invoking.</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        public static void Invoke<T1, T2>(Action<T1, T2> function, float time, T1 arg1, T2 arg2)
        {
            new Thread(() =>
            {
                Thread.Sleep((int)(time * 1000.0f));
                function.Invoke(arg1, arg2);
            }).Start();
        }

        /// <summary>
        /// Invokes <paramref name="function"/> with given arguments after
        /// <paramref name="time"/> seconds.
        /// </summary>
        /// <typeparam name="T1">First argument type.</typeparam>
        /// <typeparam name="T2">Second argument type.</typeparam>
        /// <typeparam name="T3">Third argument type.</typeparam>
        /// <param name="function">The function to be invoked.</param>
        /// <param name="time">Time in seconds before invoking.</param>
        /// <param name="arg1">First argument</param>
        /// <param name="arg2">Second argument</param>
        /// <param name="arg3">Third argument</param>
        public static void Invoke<T1, T2, T3>(Action<T1, T2, T3> function, float time, T1 arg1, T2 arg2, T3 arg3)
        {
            new Thread(() =>
            {
                Thread.Sleep((int)(time * 1000.0f));
                function.Invoke(arg1, arg2, arg3);
            }).Start();
        }
    }

}
