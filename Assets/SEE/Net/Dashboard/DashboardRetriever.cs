using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cysharp.Threading.Tasks;
using SEE.Game.UI.Notification;
using SEE.Net.Dashboard.Model.Issues;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// Can retrieve information from the Axivion Dashboard API.
    /// Before using it, make sure the <see cref="PublicKey"/>, <see cref="BaseUrl"/>
    /// and <see cref="Token"/> fields are filled with the correct values.
    /// This class follows the Singleton pattern, use the <see cref="Instance"/> attribute
    /// to call its methods.
    /// </summary>
    /// <remarks>
    /// Retrieval methods use the <c>async</c> keyword, if you want to use the received data
    /// it is strongly recommended to do this from an <c>async</c> context as well.
    /// </remarks>
    public partial class DashboardRetriever : MonoBehaviour
    {
        [Header("Dashboard Access Data")]
        /// <summary>
        /// The public key for the X.509 certificate authority from the dashboard's certificate.
        /// </summary>
        [TextArea]
        [Tooltip("The public key for the X.509 certificate authority from the dashboard's certificate.")]
        public string PublicKey = "3082020A0282020100B20ACB6E1639D673B6AF9E9F36578F66068AFDA50327DC2AB0F804E2F8"
                                  + "3765BCB7AD74FED31EC8812FF9AA9C2461D53F7DC08449C765F0ECFA9C0787B9D1E1AE92F8D"
                                  + "1919EDB6871E70601DB0834FF34389EDBA30BFF48F3EA8D07786E976B04F5232AC3A63D07DA"
                                  + "5EAD5F5450026C9E2FB9294D32FC0172E9F0DFF33CDCB35180DB22E6985C15B02BBFAD02499"
                                  + "D0E52AA916ADD5F9E7A40E22B8EC5427E02E47FD78CFEF30B5A2EDB53EA47E8B70230FB9EAE"
                                  + "57B4B7042BD8829F67F4DCDA0230BB933741AF42992CD9164C4F5E2C126A46DC42AE5BD2268"
                                  + "2C97880F8D0A82FA36FEC89CE9318E0DE2CAA3352F92F6231B18DF29913445AA323931106B0"
                                  + "764066DB4A2F8764CE4FAC2500F5A084AE3133C6C82D18181655FC1050629257A54B44FBACB"
                                  + "1BE43E51C7FA80DD7CE68D2F86AF448CA2E03B3C81A1289AA355E926CF221D881BFCD82BE7B"
                                  + "0FA99F1B04A95D23F9B030B6CDF81E90197868BC72F314E2DFBA5F9965517F8C33BF056C005"
                                  + "0DB08D285C988EFF7F212CC9D652E70B8FE67BC632F17ECFAE57603F5592C831951442F8215"
                                  + "75139D193B4DC4F2EB46FEE09495CCF67259A3F4516873612582B84512019A1157F621B46D4"
                                  + "5BCEE471BCE855C068B701F40C4CBB78F8E11550C83D7E6897967FD0B90C4BD25B0E3884492"
                                  + "66293CF52814112B7F1A95A8EC3D6CBB5567B6B0916A995D5EB8254E31647B2F810203010001";

        /// <summary>
        /// The URL to the Axivion Dashboard, up to the project name.
        /// </summary>
        [Tooltip("The URL to the Axivion Dashboard, up to the project name.")]
        public string BaseUrl = "https://stvive.informatik.uni-bremen.de:9443/axivion/projects/SEE/";

        /// <summary>
        /// The API token for the Axivion Dashboard.
        /// </summary>
        [Tooltip("The API token for the Axivion Dashboard.")]
        public string Token = "0.0000000000014.l-qjyU2eTKuNl7v0PsBb4qfI3sVHvtwKGGeOTKV_3eE";

        /// <summary>
        /// When true, receiving Dashboard models which have more fields than the C# models will throw an error
        /// instead of silently ignoring the extra fields.
        /// </summary>
        [Tooltip("Whether to throw an error if Dashboard models have more fields than the C# models.")]
        public bool StrictMode = false;

        [Header("Issue Retrieval")]
        /// <summary>
        /// Whether <see cref="ArchitectureViolationIssue"/>s shall be retrieved when calling <see cref="GetConfiguredIssues"/>.
        /// </summary>
        [Tooltip("Whether to retrieve Architecture Violation Issues.")]
        public bool ArchitectureViolationIssues = true;

        /// <summary>
        /// Whether <see cref="CloneIssue"/>s shall be retrieved when calling <see cref="GetConfiguredIssues"/>.
        /// </summary>
        [Tooltip("Whether to retrieve Clone Issues.")]
        public bool CloneIssues = true;

        /// <summary>
        /// Whether <see cref="CycleIssue"/>s shall be retrieved when calling <see cref="GetConfiguredIssues"/>.
        /// </summary>
        [Tooltip("Whether to retrieve Cycle Issues.")]
        public bool CycleIssues = true;

        /// <summary>
        /// Whether <see cref="DeadEntityIssue"/>s shall be retrieved when calling <see cref="GetConfiguredIssues"/>.
        /// </summary>
        [Tooltip("Whether to retrieve Dead Entity Issues.")]
        public bool DeadEntityIssues = true;

        /// <summary>
        /// Whether <see cref="MetricViolationIssue"/>s shall be retrieved when calling <see cref="GetConfiguredIssues"/>.
        /// </summary>
        [Tooltip("Whether to retrieve Metric Violation Issues.")]
        public bool MetricViolationIssues = true;

        /// <summary>
        /// Whether <see cref="StyleViolationIssue"/>s shall be retrieved when calling <see cref="GetConfiguredIssues"/>.
        /// </summary>
        [Tooltip("Whether to retrieve Style Violation Issues.")]
        public bool StyleViolationIssues = true;

        public Color ArchitectureViolationIssueColor = Color.yellow;
        public Color CloneIssueColor = Color.red;
        public Color CycleIssueColor = Color.green;
        public Color DeadEntityIssueColor = Color.black;
        public Color MetricViolationIssueColor = Color.magenta;
        public Color StyleViolationIssueColor = Color.cyan;
        
        /// <summary>
        /// Whether to hide any possibly copyrighted texts.
        /// </summary>
        [Tooltip("Whether to hide any possibly copyrighted texts.")]
        public bool HideCopyrightedTexts = false;

        /// <summary>
        /// Lazy wrapper around the instance of this class.
        /// Will search the scene for the component upon first access.
        /// </summary>
        /// <seealso cref="Lazy{T}"/>
        private static readonly Lazy<DashboardRetriever> instance =
            new Lazy<DashboardRetriever>(FindObjectOfType<DashboardRetriever>);

        /// <summary>
        /// Will automatically search for this component when it's accessed first, afterwards the single instance
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
        /// <param name="apiPath">Whether the query path starts with <c>/api</c></param>
        /// <param name="accept">The HTTP Accept header value.</param>
        /// <returns>The result of the API call.</returns>
        private async UniTask<DashboardResult> GetAtPath(string path, Dictionary<string, string> queryParameters = null,
                                                         bool apiPath = true, string accept = "application/json")
        {
            string requestUrl = apiPath ? BaseUrl.Replace("/projects/", "/api/projects/") : BaseUrl;
            requestUrl += path;
            if (queryParameters != null && queryParameters.Count > 0)
            {
                requestUrl += "?" + Encoding.UTF8.GetString(UnityWebRequest.SerializeSimpleForm(queryParameters));
            }

            UnityWebRequest request = UnityWebRequest.Get(requestUrl);
            request.certificateHandler = new AxivionCertificateHandler(PublicKey);
            request.SetRequestHeader("Accept", accept);
            request.SetRequestHeader("Authorization", $"AxToken {Token}");
            #pragma warning disable CS4014
            request.SendWebRequest(); // we don't need to await this, because of the next line
            #pragma warning restore CS4014
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
        /// Queries the dashboard at the given <paramref name="path"/> with the given <paramref name="parameterValues"/>
        /// and the parameter names of the caller.
        /// <b>Do not pass the <paramref name="memberName"/> parameter, it will be automatically filled!</b>
        /// </summary>
        /// <param name="path">The path to the Axivion dashboard API entry point that shall be queried.</param>
        /// <param name="parameterValues">The values of the parameters of the caller.
        /// <i>Need to be in the same order as in the calling method's signature.</i></param>
        /// <param name="apiPath">Whether the query path starts with <c>/api</c></param>
        /// <param name="memberName"><i>Do not pass this parameter!</i>
        /// Will be automatically filled with the caller's name.</param>
        /// <typeparam name="T">The type of object that is returned by the API.</typeparam>
        /// <returns>The queried object returned by the API.</returns>
        /// <exception cref="DashboardException">If there was an error accessing the API entry point.</exception>
        /// <exception cref="ArgumentException">If the number of items in <paramref name="parameterValues"/>
        /// don't match the number of parameters from the caller.</exception>
        private async UniTask<T> QueryDashboard<T>(string path, IReadOnlyList<string> parameterValues,
                                                   bool apiPath = true, [CallerMemberName] string memberName = "")
        {
            if (path == null || parameterValues == null)
            {
                throw new ArgumentNullException();
            }

            // Magically retrieve parameter names from caller
            ParameterInfo[] callerParameters = GetType().GetMethod(memberName)?.GetParameters();
            if (callerParameters == null)
            {
                throw new InvalidOperationException("Couldn't retrieve parameter names from caller. "
                                                    + "Make sure this method is private, otherwise it won't work.");
            }

            if (callerParameters.Length != parameterValues.Count)
            {
                throw new ArgumentException($"{parameterValues} must have the same number of items as the caller's parameters!");
            }

            // Map parameter names to parameter values
            Dictionary<string, string> queryParameters = callerParameters.Where(x => parameterValues[x.Position] != null)
                                                                         .ToDictionary(x => x.Name, x => parameterValues[x.Position]);

            // Finally, actually query the dashboard
            return await QueryDashboard<T>(path, queryParameters, apiPath);
        }

        /// <summary>
        /// Queries the dashboard at the given <paramref name="path"/> with the given <paramref name="parameters"/>.
        /// </summary>
        /// <param name="path">The path to the Axivion dashboard API entry point that shall be queried.</param>
        /// <param name="parameters">The query parameters that shall be used.</param>
        /// <param name="apiPath">Whether the query path starts with <c>/api</c></param>
        /// <typeparam name="T">The type that is returned by the API call.</typeparam>
        /// <returns>The queried object of type <typeparamref name="T"/>.</returns>
        private async UniTask<T> QueryDashboard<T>(string path, Dictionary<string, string> parameters = null,
                                                   bool apiPath = true)
        {
            DashboardResult result = await GetAtPath(path, parameters, apiPath);
            return result.RetrieveObject<T>(StrictMode);
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
        /// Returns the color for the given issue, based on its type.
        /// </summary>
        /// <param name="issue">The issue whose color to return.</param>
        /// <returns>The color for the given issue.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the type of the given issue is invalid.</exception>
        public Color GetIssueColor(Issue issue) =>
            issue switch
            {
                ArchitectureViolationIssue _ => ArchitectureViolationIssueColor,
                CloneIssue _ => CloneIssueColor,
                CycleIssue _ => CycleIssueColor,
                DeadEntityIssue _ => DeadEntityIssueColor,
                MetricViolationIssue _ => MetricViolationIssueColor,
                StyleViolationIssue _ => StyleViolationIssueColor,
                _ => throw new ArgumentOutOfRangeException(nameof(issue), issue, "Unknown issue kind!")
            };

        /// <summary>
        /// Checks the component for incorrect values and throws an <see cref="ArgumentException"/> if necessary.
        /// </summary>
        private void Start()
        {
            if (FindObjectsOfType<DashboardRetriever>().Length > 1)
            {
                throw new InvalidOperationException($"Only one {nameof(DashboardRetriever)} may exist "
                                                    + "in a given scene!");
            }

            if (new[] {BaseUrl, Token, PublicKey}.Any(string.IsNullOrEmpty))
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
            /// Public key of the accepted certificate.
            /// </summary>
            private readonly string AcceptKey;

            /// <summary>
            /// Instantiates a new <see cref="AxivionCertificateHandler"/> with the given <paramref name="acceptKey"/>
            /// as the accepted public key.
            /// </summary>
            /// <param name="acceptKey">The public key of the accepted certificate.</param>
            public AxivionCertificateHandler(string acceptKey)
            {
                AcceptKey = acceptKey;
            }

            /// <summary>
            /// Validates the given <paramref name="certificateData"/> by comparing it with <see cref="AcceptKey"/>.
            /// </summary>
            /// <param name="certificateData">Certificate which shall be validated</param>
            /// <returns>True iff the certificate's validation was successful</returns>
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                // Code adapted from:
                // https://docs.unity3d.com/ScriptReference/Networking.CertificateHandler.ValidateCertificate.html
                X509Certificate2 certificate = new X509Certificate2(certificateData);
                string certPublicKey = certificate.GetPublicKeyString();
                return certPublicKey?.Equals(AcceptKey) ?? false;
            }
        }
    }
}