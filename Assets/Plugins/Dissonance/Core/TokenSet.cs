using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    [Serializable]
    public class TokenSet
        : IEnumerable<string>
    {
        private static readonly IComparer<string> SortOrder = StringComparer.Ordinal;

        /// <remarks>
        /// This field contains the tokens which are currently in this set, sorted using the _sortOrder defined above
        /// Since they are sorted we can efficiently find items in the "set" using a binary search. This plays nice with
        /// unity serialization (which will serialize lists but not sets). It's probably also a marginal win in allocations.
        /// </remarks>
        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Justification: Confuses unity serialization)
        [SerializeField] private List<string> _tokens = new List<string>();

        /// <summary>
        /// Number of tokens currently in the set
        /// </summary>
        public int Count
        {
            get { return _tokens.Count; }
        }

        public event Action<string> TokenRemoved;
        public event Action<string> TokenAdded;

        private int Find([NotNull] string item)
        {
            return _tokens.BinarySearch(item, SortOrder);
        }

        public bool ContainsToken([CanBeNull] string token)
        {
            if (token == null)
                return false;

            var index = Find(token);
            return index >= 0;
        }

        public bool AddToken([NotNull] string token)
        {
            if (token == null)
                throw new ArgumentNullException("token", "Cannot add a null token");

            //Check if the collection already contains this token
            var index = Find(token);
            if (index >= 0)
                return false;

            //since the item is *not* in the collection the return value is the complement
            //of the index of the next item in the collection (that's the contract of BinarySearch)
            _tokens.Insert(~index, token);

            //Raise event indicating a token was added
            var act = TokenAdded;
            if (act != null)
                act(token);

            return true;
        }

        public bool RemoveToken([NotNull] string token)
        {
            if (token == null)
                throw new ArgumentNullException("token", "Cannot remove a null token");

            var index = Find(token);
            if (index < 0)
                return false;

            _tokens.RemoveAt(index);

            //Raise event indicating a token was removed
            var act = TokenRemoved;
            if (act != null)
                act(token);

            return true;
        }

        public bool IntersectsWith([NotNull] TokenSet other)
        {
            if (other == null)
                throw new ArgumentNullException("other", "Cannot intersect with null");

            var i = 0;
            var j = 0;
            while (i < _tokens.Count && j < other._tokens.Count)
            {
                var comparison = SortOrder.Compare(_tokens[i], other._tokens[j]);
                if (comparison < 0)
                    i++;
                else if (comparison > 0)
                    j++;
                else
                    return true;
            }

            return false;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public interface IAccessTokenCollection
    {
        /// <summary>
        /// Enumerate all the tokens in this set
        /// </summary>
        IEnumerable<string> Tokens { get; }

        /// <summary>
        /// Check if this set contains the given token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        bool ContainsToken([CanBeNull] string token);

        /// <summary>
        /// Add a new token to the set
        /// </summary>
        /// <param name="token"></param>
        /// <returns>True, iff the token was newly added.</returns>
        bool AddToken([NotNull] string token);

        /// <summary>
        /// Remove the given token from the set
        /// </summary>
        /// <param name="token"></param>
        /// <returns>True, iff the token was removed.</returns>
        bool RemoveToken([NotNull] string token);
    }
}
