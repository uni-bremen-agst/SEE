using System;
using System.Reflection;
using System.Security;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// An attribute specifying the name of an environment variable which can be used to set the value of the field
    /// that's annotated.
    ///
    /// Note that just setting this attribute won't actually set the field's value.
    /// To do this, call <see cref="EnvironmentVariableRetriever.SetEnvironmentVariableFields"/> with the instance
    /// of the class as a parameter.
    /// </summary>
    /// <example>
    /// For example, if you wanted to allow the field <c>debugLog</c>  to be set by the environment variable
    /// <c>DEBUG_LOG</c>, you could do it like this:
    /// <code>
    /// [EnvironmentVariable("DEBUG_LOG")]
    /// public bool debugLog;
    /// </code>
    /// Then, wherever you initialize the class (i.e. the constructor or <c>Start</c>/<c>Awake</c>),
    /// you'll need to call <see cref="EnvironmentVariableRetriever.SetEnvironmentVariableFields"/>:
    /// <code>
    /// public void Awake()
    /// {
    ///     SetEnvironmentVariableFields(this);
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnvironmentVariableAttribute : Attribute
    {
        /// <summary>
        /// The name of the environment variable.
        /// </summary>
        public string VariableName { get; }

        public EnvironmentVariableAttribute(string variableName)
        {
            VariableName = variableName;
        }

        public override string ToString()
        {
            return VariableName;
        }
    }

    /// <summary>
    /// Contains convenience methods for setting environment variables on a class whose fields are annotated
    /// with <see cref="EnvironmentVariableAttribute"/>.
    /// </summary>
    public static class EnvironmentVariableRetriever
    {
        /// <summary>
        /// Sets the value of all fields of <paramref name="target"/> which are annotated with
        /// <see cref="EnvironmentVariableAttribute"/> to that environment variable's value, unless the
        /// respective environment variable isn't set, in which case nothing will be done.
        ///
        /// If the annotated field has a type other than <c>string</c>, we will try to convert the environment
        /// variable's value to that type. Note that this will likely only work with primitive types such as
        /// <c>int</c>, <c>bool</c>, and so on. If the field's type is not supported, an
        /// <see cref="InvalidCastException"/> will be thrown, since this is an error on the programmer's part.
        /// If the field's type <em>is</em> supported but the supplied value is not in a suitable format,
        /// a warning will be emitted and the field will be ignored, since this is an error on the user's part.
        /// </summary>
        /// <param name="target">Instance of the class whose fields annotated with
        /// <see cref="EnvironmentVariableAttribute"/> shall be set to the respective
        /// environment variable's value</param>
        /// <typeparam name="T">Type of the given <paramref name="target"/></typeparam>
        public static void SetEnvironmentVariableFields<T>(T target)
        {
            foreach (FieldInfo fieldInfo in typeof(T).GetFields())
            {
                Attribute attribute = Attribute.GetCustomAttribute(fieldInfo, typeof(EnvironmentVariableAttribute));
                if (attribute is EnvironmentVariableAttribute environmentVariable)
                {
                    object result;
                    try
                    {
                        result = Environment.GetEnvironmentVariable(environmentVariable.VariableName);
                    }
                    catch (SecurityException e)
                    {
                        // If no environment variable can be retrieved for security reasons, we'll log a warning and stop.
                        Debug.LogWarning($"Couldn't retrieve environment variable {environmentVariable}: {e}.\n");
                        break;
                    }
                    if (result != null)
                    {
                        // We may need to cast the (string) environment variable to the target type.
                        Type fieldType = fieldInfo.FieldType;
                        try
                        {
                            result = Convert.ChangeType(result, fieldType);
                        }
                        // These are the only problems which are the user's fault, hence we log them here.
                        catch (FormatException e)
                        {
                            Debug.LogWarning($"Invalid format for environment variable {environmentVariable}: {e}.\n");
                            continue;
                        }
                        catch (OverflowException e)
                        {
                            Debug.LogWarning($"Overflow for environment variable {environmentVariable}: {e}.\n");
                            continue;
                        }
                        fieldInfo.SetValue(target, result);
                    }
                }
            }
        }
    }
}