using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE
{
    /// <summary>
    /// A sort algorithm for a list of strings, that sorts by numbers in the string.
    /// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
    /// </summary>
    public static class NumericalSortExtension
    {

        public static IEnumerable<string> NumericalSort(this IEnumerable<string> list)
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