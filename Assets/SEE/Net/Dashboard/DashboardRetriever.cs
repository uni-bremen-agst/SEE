using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// Can retrieve information from the Axivion Dashboard API.
    /// </summary>
    public class DashboardRetriever : MonoBehaviour
    {
        /// <summary>
        /// The public key for the X.509 certificate authority from the dashboard's certificate.
        /// </summary>
        public static string PublicKey { private get; set; }
        /// <summary>
        /// The URL to the Axivion Dashboard, up to the project name.
        /// </summary>
        public static string BaseUrl { private get; set; }
        /// <summary>
        /// The API token for the Axivion Dashboard.
        /// </summary>
        public static string Token { private get; set; }
        

        /// <summary>
        /// Coroutine which retrieves the result from calling the Axivion Dashboard at <see cref="path"/> appended to
        /// <see cref="BaseUrl"/> and calls <see cref="action"/> with the result.
        /// </summary>
        /// <param name="path">Path which shall be queried, will be appended to <see cref="BaseUrl"/></param>
        /// <param name="action">Will be called with the result of the retrieval.</param>
        public static IEnumerator GetAtPath(string path, Action<DashboardResult> action)
        {
            UnityWebRequest request = UnityWebRequest.Get(BaseUrl + path);
            request.certificateHandler = new AxivionCertificateHandler();
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", $"AxToken {Token}");
            yield return request.SendWebRequest();
            DashboardResult result = request.result switch
            {
                UnityWebRequest.Result.Success => new DashboardResult(true, request.downloadHandler.text),
                UnityWebRequest.Result.ProtocolError => new DashboardResult(false, request.downloadHandler.text),
                UnityWebRequest.Result.ConnectionError => new DashboardResult(new WebException(request.error)),
                UnityWebRequest.Result.DataProcessingError => new DashboardResult(new FormatException(request.error)),
                _ => new DashboardResult(new SystemException(request.error))
            };
            action.Invoke(result);
        }

        /// <summary>
        /// Sets up this component by checking for incorrect values.
        /// </summary>
        private void Start()
        {
            if (FindObjectOfType<DashboardRetriever>())
            {
                Debug.LogError($"{nameof(DashboardRetriever)} added to scene while another is already present,"
                               + "disabling this one.\n");
                enabled = false;
                return;
            }
            if (new[]{BaseUrl, Token, PublicKey}.Any(string.IsNullOrEmpty))
            {
                Debug.LogError("Necessary information not supplied, disabling this component.\n");
                enabled = false;
                return;
            }
            if (!BaseUrl.StartsWith("https"))
            {
                Debug.LogError("Base URL must be a HTTPS URL.\n");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// Certificate handler for self-signed certificate. Will check by comparing the certificate with
        /// <see cref="PublicKey"/>.
        /// </summary>
        private class AxivionCertificateHandler : CertificateHandler
        {
            /// <summary>
            /// Validates the given <paramref name="certificateData"/> by comparing it with <see cref="PublicKey"/>.
            /// </summary>
            /// <param name="certificateData">Certificate which shall be validated</param>
            /// <returns>True iff the certificate's validation was successful</returns>
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                // Code adapted from:
                // https://docs.unity3d.com/ScriptReference/Networking.CertificateHandler.ValidateCertificate.html
                X509Certificate2 certificate = new X509Certificate2(certificateData);
                string certPublicKey = certificate.GetPublicKeyString();
                return certPublicKey?.Equals(PublicKey) ?? false;
            }
        }
    }
}