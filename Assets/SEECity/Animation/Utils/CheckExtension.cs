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

/// <summary>
/// Enables fast and simple checks for all types.
/// If a check fails, an exception is thrown.
/// </summary>
public static class CheckExtension
{

    /// <summary>
    /// Checks if a given object is null and throws a System.Exception
    /// containing the paramName if so.
    /// </summary>
    /// <typeparam name="T">type of the given object</typeparam>
    /// <param name="obj">object to check</param>
    /// <param name="paramName">parameter name for exception message</param>
    /// <returns>the given object to check</returns>
    public static T AssertNotNull<T>(this T obj, string paramName)
    {
        if (obj == null)
        {
            throw new System.Exception(paramName + "must not be null");
        }
        return obj;
    }

    /// <summary>
    /// Checks if a given string is null and not empty and throws a System.Exception
    /// containing the paramName if so.
    /// </summary>
    /// <param name="obj">string to check</param>
    /// <param name="paramName">parameter name for exception message</param>
    /// <returns>the given string to check</returns>
    public static string AssertNotNullOrEmpty(this string obj, string paramName)
    {
        obj.AssertNotNull(paramName);
        if (obj.Length == 0)
        {
            throw new System.Exception(paramName + "must not be empty");
        }
        return obj;
    }
}
