using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Provides random strings.
    /// </summary>
    internal static class RandomStrings
    {
        /// <summary>
        /// Low latin alphabet.
        /// </summary>
        private const string letters = "abcdefghijklmnopqrstuvwxyz";
        /// <summary>
        /// Digits.
        /// </summary>
        private const string numbers = "0123456789";
        /// <summary>
        /// Allowed special characters.
        /// </summary>
        private const string specialCharacters = "!?§$%&.,_#+*@";
        /// <summary>
        /// Contains the low and upper letters, numbers and special characters.
        /// It will be needed for the calculation of a random string.
        /// </summary>
        private static readonly string characters = letters + letters.ToUpper() + numbers + specialCharacters;

        /// <summary>
        /// Contains the low and upper letters and numbers.
        /// It will be needed for the calculation of a random string for file creation.
        /// </summary>
        private static readonly string charactersWithoutSpecial = letters + letters.ToUpper() + numbers;

        /// <summary>
        /// Calculates a random string of given length.
        /// </summary>
        /// <param name="size">The length of the random string</param>
        /// <returns>The calculated random string of given length.</returns>
        public static string GetRandomString(int size)
        {
            string randomString = "-";
            for (int i = 0; i < size; i++)
            {
                randomString += characters[Random.Range(0, characters.Length)];
            }
            return randomString;
        }

        /// <summary>
        /// Calculates a random string of given length for file creation.
        /// </summary>
        /// <param name="size">The length of the random string</param>
        /// <returns>The calculated random string of given length.</returns>
        public static string GetRandomStringForFile(int size)
        {
            string randomString = "";
            for (int i = 0; i < size; i++)
            {
                randomString += charactersWithoutSpecial[Random.Range(0, charactersWithoutSpecial.Length)];
            }
            return randomString;
        }
    }
}
