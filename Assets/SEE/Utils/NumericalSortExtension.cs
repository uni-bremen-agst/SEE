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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SEE.Utils
{
    /// <summary>
    /// Extension for IEnumerable<string>, that sorts by numbers in the string.
    /// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
    /// </summary>
    internal static class NumericalSortExtension
    {
        /// <summary>
        /// Sorts the given IEnumerable<string> by numbers contained in the string.
        /// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
        /// </summary>
        /// <param name="list">An IEnumerable<string> to be sorted</param>
        /// <returns>The passed list sorted by numbers</returns>
        public static IEnumerable<string> NumericalSort(this IEnumerable<string> list)
        {
            if (list.Count() <= 1)
            {
                return list;
            }
            else
            {
                int maxLen = list.Select(s => s.Length).Max();
                return list.Select(s => new
                {
                    OrgStr = s,
                    SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
                })
                .OrderBy(x => x.SortStr)
                .Select(x => x.OrgStr);
            }
        }
    }
}