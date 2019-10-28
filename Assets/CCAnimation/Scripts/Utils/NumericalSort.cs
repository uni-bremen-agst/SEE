using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Extension for IEnumerable<string>, that sorts by numbers in the string.
/// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
/// </summary>
public static class NumericalSortExtension
{
    /// <summary>
    /// Sorts the given IEnumerable<string> by numbers contained in the string.
    /// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
    /// </summary>
    /// <param name="list">An IEnumerable<string> to be sorted</param>
    /// <returns>The passed list sorted by numbers</returns>
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