using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Asset_Cleaner {
    readonly struct Option<T> : IEquatable<Option<T>>, IComparable<Option<T>> {
        // ReSharper disable once StaticMemberInGenericType
        static readonly bool IsValueType;

        public bool HasValue { get; }

        T Value { get; }

        public static implicit operator Option<T>(T arg) {
            if (!IsValueType) return ReferenceEquals(arg, null) ? new Option<T>() : new Option<T>(arg, true);
#if M_WARN
            if (arg.Equals(default(T))) 
                Warn.Warning($"{arg} has default value");
#endif
            return new Option<T>(arg, true);
        }

        static Option() {
            IsValueType = typeof(T).IsValueType;
        }

        public void GetOrFail(out T value) {
            if (!TryGet(out value))
                Fail($"Option<{typeof(T).Name}> has no value");
        }

        public T GetOrFail() {
            if (!TryGet(out var value))
                Fail($"Option<{typeof(T).Name}> has no value");
            return value;
        }

        [Conditional("DEBUG1")]
        static void Fail(string format = null) {
            throw new Exception(format);
        }

        public bool TryGet(out T value) {
            if (!HasValue) {
                value = default(T);
                return false;
            }

            value = Value;
            return true;
        }

        internal Option(T value, bool hasValue) {
            Value = value;
            HasValue = hasValue;
        }

        public T ValueOr(T alternative) {
            return HasValue ? Value : alternative;
        }

        // for debug purposes
        public override string ToString() {
            if (!HasValue) return "None";

            return Value == null ? "Some(null)" : $"Some({Value})";
        }

        #region eq comparers boilerplate

        public bool Equals(Option<T> other) {
            if (!HasValue && !other.HasValue)
                return true;

            if (HasValue && other.HasValue)
                return EqualityComparer<T>.Default.Equals(Value, other.Value);

            return false;
        }

        public override bool Equals(object obj) {
            return obj is Option<T> && Equals((Option<T>) obj);
        }

        public static bool operator ==(Option<T> left, Option<T> right) {
            return left.Equals(right);
        }

        public static bool operator !=(Option<T> left, Option<T> right) {
            return !left.Equals(right);
        }

        public override int GetHashCode() {
            if (!HasValue) return 0;

            return IsValueType || Value != null ? Value.GetHashCode() : 1;
        }

        public int CompareTo(Option<T> other) {
            if (HasValue && !other.HasValue) return 1;
            if (!HasValue && other.HasValue) return -1;

            return Comparer<T>.Default.Compare(Value, other.Value);
        }

        public static bool operator <(Option<T> left, Option<T> right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(Option<T> left, Option<T> right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(Option<T> left, Option<T> right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(Option<T> left, Option<T> right) {
            return left.CompareTo(right) >= 0;
        }

        #endregion
    }
}