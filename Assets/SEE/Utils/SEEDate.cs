using System;
using System.Globalization;

namespace SEE.Utils
{
    /// <summary>
    /// This class is intended to encapsulate the syntactic format of a date string.
    /// </summary>
    public class SEEDate
    {
        /// <summary>
        /// The format of <see cref="Date"/>.
        /// </summary>
        public const string DateFormat = "yyyy/MM/dd";

        /// <summary>
        /// Creates a new instance of <see cref="SEEDate"/> and sets it to the current date.
        /// </summary>
        public SEEDate()
        {
            date = DateTime.Now;
        }

        /// <summary>
        /// Creates a new instance of <see cref="SEEDate"/> and sets it to <paramref name="date"/>
        /// </summary>
        /// <param name="date">date value to be assigned</param>
        /// <exception cref="ArgumentException">thrown in <paramref name="date"/> has not the required
        /// syntax <see cref="DateFormat"/></exception>
        public SEEDate(string date)
        {
            Set(date);
        }

        /// <summary>
        /// The date.
        /// </summary>
        private DateTime date;

        /// <summary>
        /// Sets the date.
        /// </summary>
        /// <param name="date">date value to be assigned</param>
        /// <exception cref="ArgumentException">thrown in <paramref name="date"/> has not the required
        /// syntax <see cref="DateFormat"/></exception>
        public void Set(string date)
        {
            if (DateTime.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                this.date = dateTime;
            }
            else
            {
                throw new ArgumentException($"Date '{date}' is not in the correct format '{DateFormat}'.");
            }
        }

        /// <summary>
        /// The set date.
        /// </summary>
        /// <returns>set date</returns>
        public DateTime Get() => date;

        /// <summary>
        /// Returns the date in the format <see cref="DateFormat"/>.
        /// </summary>
        /// <returns>the set date as a string</returns>
        override public string ToString()
        {
            return date.ToString(DateFormat, CultureInfo.InvariantCulture);
        }
    }
}
