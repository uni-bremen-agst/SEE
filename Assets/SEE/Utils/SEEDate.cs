using System;
using System.Globalization;

namespace SEE.Utils
{
    /// <summary>
    /// This class is intended to encapsulate the syntactic format of a date string.
    /// </summary>
    public static class SEEDate
    {
        /// <summary>
        /// The format of <see cref="Date"/>.
        /// </summary>
        public const string DateFormat = "yyyy/MM/dd";

        /// <summary>
        /// Sets the date.
        /// </summary>
        /// <param name="date">Date value to be assigned.</param>
        /// <exception cref="ArgumentException">Thrown in <paramref name="date"/> has not the required
        /// syntax <see cref="DateFormat"/>.</exception>
        public static DateTime ToDate(string date)
        {
            if (DateTime.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime;
            }
            else
            {
                throw new ArgumentException($"Date '{date}' is not in the correct format '{DateFormat}' or an impossible date.");
            }
        }

        /// <summary>
        /// Returns the current date in the syntax of <see cref="DateFormat"/>.
        /// </summary>
        /// <returns>Current date.</returns>
        public static string Now()
        {
            return DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// True if given <paramref name="date"/> conforms to the syntax <see cref="DateFormat"/>
        /// and is a possible date (e.g., 2023/02/29 is syntactically correct but not a possible
        /// date because 2023 is not a leap year).
        /// </summary>
        /// <param name="date">The date string to validate.</param>
        /// <returns><c>true</c> if the date string is syntactically correct and represents a valid date; otherwise, <c>false</c>.</returns>
        public static bool IsValid(string date)
        {
            return DateTime.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime _);
        }
    }
}
