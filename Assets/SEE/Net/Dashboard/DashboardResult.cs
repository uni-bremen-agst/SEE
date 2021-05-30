using System;
using Newtonsoft.Json;
using SEE.Net.Dashboard.Model;
using UnityEngine;

namespace SEE.Net.Dashboard
{
    public class DashboardResult
    {
        public bool Success { get; private set; }
        public readonly DashboardError Error;
        public readonly Exception Exception;
        private readonly string json;

        public DashboardResult(bool success, string json)
        {
            this.json = json ?? throw new ArgumentNullException(nameof(json));
            if (!success)
            {
                Success = true; // temporarily set this to true so that the next method works
                Error = RetrieveObject<DashboardError>();
            }
            Success = success;
        }

        public DashboardResult(Exception exception)
        {
            Success = false;
            Exception = exception;
        }

        /// <summary>
        /// Depending on the value of <see cref="Success"/>, throws an exception containing information
        /// about the <see cref="Error"/> or <see cref="Exception"/>.
        /// If <see cref="Success"/> is <c>true</c>, nothing will happen.
        /// </summary>
        /// <exception cref="DashboardException">Will be thrown if <see cref="Success"/> is <c>false</c>
        /// and contains additional information about the <see cref="Error"/> or <see cref="Exception"/>.</exception>
        public void PossiblyThrow()
        {
            if (Success)
            {
                return;
            }

            if (Exception != null)
            {
                throw new DashboardException(Exception);
            }

            if (Error != null)
            {
                throw new DashboardException(Error);
            }

            // Success is false, so something is wrong, but for some reason both Error and Exception are null.
            // In such a case, we assume a programming error and throw a different exception.
            throw new InvalidOperationException("An unknown error occurred while retrieving dashboard data.");
        }

        /// <summary>
        /// Try to deserialize the Dashboard data into a new object of type <typeparamref name="T"/>.
        /// If <see cref="Success"/> is <c>false</c>, an error will be thrown.
        /// </summary>
        /// <param name="strict">Whether a mismatch between the received data and the corresponding
        /// C# object it is deserialized to should cause an exception.</param>
        /// <typeparam name="T">The serializable type the data should be deserialized to.</typeparam>
        /// <returns>An object of type <typeparamref name="T"/> filled with the received data of the dashboard API.
        /// </returns>
        public T RetrieveObject<T>(bool strict = true)
        {
            PossiblyThrow();

            try
            {
                return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    MissingMemberHandling = strict ? MissingMemberHandling.Error : MissingMemberHandling.Ignore
                });
            }
            catch (Exception e) when (e is JsonSerializationException || e is JsonReaderException)
            {
                Debug.LogError("Error encountered, given JSON was: " + json);
                throw;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Error)}: {Error}, {nameof(Exception)}: {Exception}, {nameof(json)}: {json}, {nameof(Success)}: {Success}";
        }
    }
}