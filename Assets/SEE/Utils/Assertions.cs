//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using SEE.GO;
using System;
using System.Diagnostics;

namespace SEE.Utils
{
    /// <summary>
    /// Enables fast and simple checks for all types.
    /// If a check fails, an exception is thrown.
    /// </summary>
    public static class Assertions
    {
        /// <summary>
        /// This exception will be thrown, if an invalid code path is executed.
        /// </summary>
        private class InvalidCodePathException : Exception
        {
            /// <summary>
            /// Exception for invalid code path without custom message.
            /// </summary>
            internal InvalidCodePathException()
            {
            }

            /// <summary>
            /// Exception for invalid code path with custom message.
            /// </summary>
            /// <param name="message"></param>
            internal InvalidCodePathException(string message) : base(message)
            {
            }
        }

        /// <summary>
        /// Disables the given <paramref name="monoBehaviour"/> and prints the given
        /// message, if the given <paramref name="condition"/> is <code>true</code>.
        /// Returns the evaluated condition.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to disable on condition.</param>
        /// <param name="condition">The condition to check.</param>
        /// <param name="message">The message to print on condition.</param>
        /// <returns>Returns whether the given monoBehaviour was disabled or not (The
        /// evaluated condition).</returns>
        public static bool DisableOnCondition(UnityEngine.MonoBehaviour monoBehaviour, bool condition, string message)
        {
            if (condition)
            {
                UnityEngine.Debug.LogError($"{monoBehaviour.GetType().FullName} of game object {monoBehaviour.gameObject.FullName()}: {message}. Component will be disabled.\n");
                monoBehaviour.enabled = false;
            }
            return condition;
        }

        /// <summary>
        /// Disables the given <paramref name="monoBehaviour"/>, if the given
        /// <paramref name="condition"/> is <code>true</code>. Returns the evaluated
        /// condition.
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to disable on condition.</param>
        /// <param name="condition">The condition to check.</param>
        /// <returns>Returns whether the given monoBehaviour was disabled or not (The
        /// evaluated condition).</returns>
        public static bool DisableOnCondition(UnityEngine.MonoBehaviour monoBehaviour, bool condition)
        {
            if (condition)
            {
                monoBehaviour.enabled = false;
            }
            return condition;
        }

        /// <summary>
        /// Checks whether <paramref name="obj"/> is null and throws a System.Exception
        /// containing the <paramref name="paramName"/> if so.
        /// </summary>
        /// <typeparam name="T">type of the given object</typeparam>
        /// <param name="obj">object to be checked</param>
        /// <param name="paramName">parameter name for exception message</param>
        /// <returns>the given object to be checked</returns>
        public static T AssertNotNull<T>(this T obj, string paramName)
        {
            if (obj == null)
            {
                throw new Exception(paramName + " must not be null");
            }
            return obj;
        }

        /// <summary>
        /// Checks whether <paramref name="obj"/> is null or empty and throws a System.Exception
        /// containing <paramref name="paramName"/> if so.
        /// </summary>
        /// <param name="obj">string to be checked</param>
        /// <param name="paramName">parameter name for exception message</param>
        /// <returns>the given string to be checked</returns>
        public static string AssertNotNullOrEmpty(this string obj, string paramName)
        {
            obj.AssertNotNull(paramName);
            if (obj.Length == 0)
            {
                throw new Exception(paramName + " must not be empty");
            }
            return obj;
        }

        /// <summary>
        /// Throws an exception, if called. If the debugger is currently attached, the debugger
        /// will break before throwing the exception.
        /// </summary>
        public static void InvalidCodePath()
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            throw new InvalidCodePathException();
        }

        /// <summary>
        /// Throws an exception with given <paramref name="message"/>, if called. If the debugger
        /// is currently attached, the debugger will break before throwing the exception.
        /// </summary>
        /// <param name="message"></param>
        public static void InvalidCodePath(string message)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            throw new InvalidCodePathException(message);
        }
    }
}
