using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SEE.Net.Dashboard.Model;
using UnityEngine.Networking;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// Can retrieve information from the Axivion Dashboard API.
    /// </summary>
    public class DashboardRetriever
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
        /// Lazy wrapper around the instance of this object.
        /// Calls the parameterless constructor of this class upon first access.
        /// </summary>
        /// <seealso cref="Lazy{T}"/>
        private static readonly Lazy<DashboardRetriever> instance = new Lazy<DashboardRetriever>(() => new DashboardRetriever());
        
        /// <summary>
        /// Will automatically instantiate this class when it's accessed first, afterwards the single instance
        /// of this class will be returned.
        /// It's important that you only call this after having set <see cref="PublicKey"/>, <see cref="BaseUrl"/>,
        /// and <see cref="Token"/>.
        /// </summary>
        public static DashboardRetriever Instance => instance.Value;

        /// <summary>
        /// Retrieves the result from calling the Axivion Dashboard at <see cref="path"/> appended to
        /// <see cref="BaseUrl"/> and returns the result.
        /// </summary>
        /// <param name="path">Path which shall be queried, will be appended to <see cref="BaseUrl"/></param>
        /// <returns>The result of the API call.</returns>
        public async UniTask<DashboardResult> GetAtPath(string path)
        {
            UnityWebRequest request = UnityWebRequest.Get(BaseUrl + path);
            request.certificateHandler = new AxivionCertificateHandler();
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", $"AxToken {Token}");
            //TODO: Either this or 
            await request.SendWebRequest().ToUniTask().Timeout(TimeSpan.FromSeconds(60f));
            DashboardResult result = request.result switch
            {
                UnityWebRequest.Result.Success => new DashboardResult(true, request.downloadHandler.text),
                UnityWebRequest.Result.ProtocolError => new DashboardResult(false, request.downloadHandler.text),
                UnityWebRequest.Result.ConnectionError => new DashboardResult(new WebException(request.error)),
                UnityWebRequest.Result.DataProcessingError => new DashboardResult(new FormatException(request.error)),
                _ => new DashboardResult(new SystemException(request.error))
            };
            return result;
        }

        /// <summary>
        /// Retrieves dashboard information from the dashboard configured for this <see cref="DashboardRetriever"/>.
        /// </summary>
        /// <returns>Dashboard information about the queried dashboard.</returns>
        public async Task<DashboardInfo> GetDashboardInfo()
        {
            DashboardResult result = await GetAtPath("/");
            result.PossiblyThrow();
            return result.RetrieveObject<DashboardInfo>();
        }


        /// <summary>
        /// Checks the component for incorrect values and throws an <see cref="ArgumentException"/> if necessary.
        /// </summary>
        private DashboardRetriever()
        {
            if (new[]{BaseUrl, Token, PublicKey}.Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("Necessary information not supplied. "
                                            + "Please set base URL, token, and public key before accessing this class.");
            }

            if (!BaseUrl.StartsWith("https"))
            {
                throw new ArgumentException("Base URL must be a HTTPS URL.\n");
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