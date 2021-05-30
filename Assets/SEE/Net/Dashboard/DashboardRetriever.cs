using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Cysharp.Threading.Tasks;
using SEE.Game.UI.Notification;
using SEE.Net.Dashboard.Model;
using UnityEngine;
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
        /// When true, receiving Dashboard models which don't match the C# models will throw an error
        /// instead of silently assigning default values to the fields.
        /// </summary>
        public static bool StrictMode = true;
        
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
        /// <param name="path">Path which shall be queried, will be appended to <see cref="BaseUrl"/>.
        /// This path should not contain query parameters, which should be set via <paramref name="queryParameters"/>.
        /// </param>
        /// <param name="queryParameters">A dictionary containing the query parameters' names and values.</param>
        /// <returns>The result of the API call.</returns>
        public async UniTask<DashboardResult> GetAtPath(string path, Dictionary<string, string> queryParameters = null)
        {
            string requestUrl = BaseUrl + path;
            if (queryParameters != null)
            {
                requestUrl += UnityWebRequest.SerializeSimpleForm(queryParameters);
            }
            UnityWebRequest request = UnityWebRequest.Get(requestUrl);
            request.certificateHandler = new AxivionCertificateHandler();
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", $"AxToken {Token}");
            request.SendWebRequest();
            await UniTask.WaitUntil(() => request.isDone);
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
        /// IMPORTANT NOTE: This will only work if your token has full permissions, i.e., when it's not just
        /// an IDE token. If you simply want to retrieve the dashboard version, use
        /// </summary>
        /// <returns>Dashboard information about the queried dashboard.</returns>
        public async UniTask<DashboardInfo> GetDashboardInfo()
        {
            DashboardResult result = await GetAtPath("/../../");
            DashboardInfo info = result.RetrieveObject<DashboardInfo>(StrictMode);
            return info;
        }

        /// <summary>
        /// Returns the version number of the dashboard that's being queried.
        /// </summary>
        /// <returns>version number of the dashboard that's being queried.</returns>
        /// <remarks>We first try to get this using <see cref="GetDashboardInfo"/>, but typical IDE tokens don't have
        /// enough permissions to access that API endpoint. In that case, we instead deliberately cause an error by
        /// trying to access it, because the version number is supplied in the <see cref="DashboardError"/> object.
        /// </remarks>
        public async UniTask<DashboardVersion> GetDashboardVersion()
        {
            DashboardVersion version;
            try
            {
                version = new DashboardVersion((await GetDashboardInfo()).dashboardVersionNumber);
            }
            catch (DashboardException e)
            {
                if (e.Error == null)
                {
                    throw;
                }
                version = new DashboardVersion(e.Error.dashboardVersionNumber);
            }

            return version;
        }

        /// <summary>
        /// Compares the <see cref="SupportedDashboardVersion"/> with the actual version of the accessed dashboard
        /// and warns the user via notifications or a log message, depending on how critical the difference is.
        /// </summary>
        private async UniTaskVoid VerifyVersionNumber()
        {
            DashboardVersion version = await GetDashboardVersion();
            switch (version.DifferenceToSupportedVersion)
            {
                case DashboardVersion.Difference.MAJOR_OLDER: 
                case DashboardVersion.Difference.MINOR_OLDER: 
                    // If major or minor version of the dashboard is older, we may use features that aren't existent
                    // in it yet, so we have to notify the user with a warning.
                    ShowNotification.Error("Dashboard Version too old", 
                                           $"The version of the dashboard is {version}, but the DashboardRetriever "
                                           + $"has been written for version {DashboardVersion.SupportedVersion}."
                                           + $" Please update your dashboard.");
                    break;
                case DashboardVersion.Difference.PATCH_OLDER:
                    // If patch version is older, there may be some bugfixes / security problems not accounted for.
                    ShowNotification.Warn("Dashboard Version outdated", 
                                          $"Your dashboard has version {version} but this API supports "
                                          + $"{DashboardVersion.SupportedVersion}. The difference in versions is small," 
                                          + "so this shouldn't be too critical, but please update your dashboard to avoid any issues.");
                    break;
                case DashboardVersion.Difference.MAJOR_NEWER: 
                    // Major new version can introduce breaking changes
                    ShowNotification.Error("Dashboard Version unsupported", 
                        $"Your dashboard has a major new version ({version}) compared to the supported version "
                        + $"({DashboardVersion.SupportedVersion}), which may have introduced breaking changes. "
                        + "Please update SEE's retrieval code accordingly.");
                    break;
                case DashboardVersion.Difference.MINOR_NEWER: 
                case DashboardVersion.Difference.PATCH_NEWER: 
                    // Minor and patch updates shouldn't impact existing functionality, but the retrieval code
                    // should still be updated by a developer.
                    Debug.LogWarning($"The dashboard uses version {version} while the retrieval code has been "
                                     + $"written for version {DashboardVersion.SupportedVersion}. Please update SEE's retrieval "
                                     + "code accordingly.");
                    break;
                case DashboardVersion.Difference.EXTRA_OLDER:
                case DashboardVersion.Difference.EXTRA_NEWER:
                    // Extra changes are assumed to be small enough to not even warrant a warning.
                    Debug.Log($"Dashboard version {version} differs slightly from supported version "
                              + $"{DashboardVersion.SupportedVersion}.");
                    break;
                case DashboardVersion.Difference.EQUAL:
                    // No need to do anything
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
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

            VerifyVersionNumber().Forget();
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