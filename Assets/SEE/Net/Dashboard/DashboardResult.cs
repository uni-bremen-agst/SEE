using System;
using Newtonsoft.Json;
using SEE.Net.Dashboard.Model;
using UnityEngine;

namespace SEE.Net.Dashboard
{
    public class DashboardResult
    {
        public bool Success { get; private set; } = true;
        public readonly DashboardError Error;
        public readonly Exception Exception;
        private readonly string json;

        public DashboardResult(bool success, string json)
        {
            this.json = json ?? throw new ArgumentNullException(nameof(json));
            if (!success)
            {
                Error = RetrieveObject<DashboardError>();
            }
            Success = success;
        }

        public DashboardResult(Exception exception)
        {
            Success = false;
            Exception = exception;
        }

        public T RetrieveObject<T>()
        {
            if (!Success)
            {
                if (Exception == null)
                {
                    throw new InvalidOperationException($"Can't retrieve {typeof(T)}: {Error.message}");
                }
                else
                {
                    throw new InvalidOperationException($"Can't retrieve {typeof(T)}", Exception);
                }
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (ArgumentException)
            {
                Debug.Log("Error encountered, given JSON was: " + json);
                throw;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Error)}: {Error}, {nameof(Exception)}: {Exception}, {nameof(json)}: {json}, {nameof(Success)}: {Success}";
        }
    }
}