using System;
using SEE.Net.Dashboard.Model;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// Represents the result of an API call to the Axivion dashboard, which may or may not have been successful.
    /// In case of success, the received object can be retrieved using the generic <see cref="RetrieveObject{T}"/>
    /// method.
    /// </summary>
    public class DashboardResult
    {
        /// <summary>
        /// Whether the API call has been successful.
        /// </summary>
        public bool Success { get; private set; }
        
        /// <summary>
        /// This contains the error object which has been returned from the dashboard, if the call was not successful
        /// due to an API error. Will be <see cref="null"/> if <see cref="Success"/> is <c>true</c>
        /// or if <see cref="Exception"/> is not <c>null</c>.
        /// </summary>
        public readonly DashboardError Error;
        
        /// <summary>
        /// This contains the exception which occurred when trying to access the dashboard.
        /// Will be <see cref="null"/> if <see cref="Success"/> is <c>true</c> or if <see cref="Error"/>
        /// is not <c>null</c>.
        /// </summary>
        public readonly Exception Exception;
        
        /// <summary>
        /// Contains the received JSON from the dashboard.
        /// May be <c>null</c> if <see cref="Success"/> is <c>false</c>.
        /// </summary>
        public readonly string JSON;

        /// <summary>
        /// Instantiates a new instance of this class from the given success value and JSON.
        /// </summary>
        /// <param name="success">True if the API call has been successful (HTTP code 200).</param>
        /// <param name="json">JSON returned from the server. May not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">If the given <paramref name="json"/> is <c>null</c>.</exception>
        public DashboardResult(bool success, string json)
        {
            JSON = json ?? throw new ArgumentNullException(nameof(json));
            if (!success)
            {
                Success = true; // temporarily set this to true so that the next method works
                Error = RetrieveObject<DashboardError>();
            }
            Success = success;
        }

        /// <summary>
        /// Instantiates a new instance of this class with <see cref="Success"/> as <c>false</c> and the given exception
        /// as the cause.
        /// </summary>
        /// <param name="exception">The cause of the failure to access the dashboard API.</param>
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
        private void PossiblyThrow()
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
        /// C# object it is deserialized to should cause an exception. Specifically,
        /// if the received data has more fields than the C# object, not the other way around.</param>
        /// <typeparam name="T">The serializable type the data should be deserialized to.</typeparam>
        /// <returns>An object of type <typeparamref name="T"/> filled with the received data of the dashboard API.
        /// </returns>
        public T RetrieveObject<T>(bool strict = true)
        {
            PossiblyThrow();

            try
            {
                return JsonConvert.DeserializeObject<T>(JSON, new JsonSerializerSettings
                {
                    MissingMemberHandling = strict ? MissingMemberHandling.Error : MissingMemberHandling.Ignore
                });
            }
            catch (Exception e) when (e is JsonSerializationException || e is JsonReaderException)
            {
                Debug.LogError($"Error encountered: {e.Message}.\nGiven JSON was: {JSON}");
                throw;
            }
        }

        /// <summary>
        /// Returns a human-readable representation of this class.
        /// </summary>
        /// <returns>human-readable representation of this class.</returns>
        public override string ToString()
        {
            return $"{nameof(Error)}: {Error}, {nameof(Exception)}: {Exception}, {nameof(JSON)}: {JSON}, {nameof(Success)}: {Success}";
        }
    }
}