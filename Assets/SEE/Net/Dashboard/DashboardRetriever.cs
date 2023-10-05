using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cysharp.Threading.Tasks;
using SEE.UI.Notification;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Networking;
using static SEE.Utils.EnvironmentVariableRetriever;

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
        [EnvironmentVariable("DASHBOARD_PUBLIC_KEY")]
        [TextArea]
        [Tooltip("The public key for the X.509 certificate authority from the dashboard's certificate.")]
        public string PublicKey = "3082018A0282018100D5EB3F1B3AB0ABCE827FA70BA32FCF8C834B1206464B785B7AE1"
                                + "3D962F19887D6733B0A555651F130BCDB04D8BF6F199EF76DB7932C001B6916D0E3F0C"
                                + "84C9A7DDB7BC62C93590E49C5DD97109B01B2CFAFD183A45DD8E02F86EBAFB4C74DF24"
                                + "449D582DFC03D8E783DA035BB2985907EC00774D748AFC9A0195E05E2B992A7877F437"
                                + "DB6088E2490A53D83D8F729482A383142AE5FBAFA4F2C3112E5A5C16D520ADFCF5E0F0"
                                + "C9FF865126AFC8B97EAEFD77CE31A431F0E66C2200CDF70BC56B478FB7B56858CE605E"
                                + "3313A876E4B719529DB929D9D7A966B16A9656FF639AE7382B6B7591E19D05A0B35468"
                                + "03007DCE8354FDFDFC0DAB4E5103C0ED67A6BFD42E810C78A649DD419A0E4C1BB15267"
                                + "A85DB4336D101F3799B71A654C5A8422875EE4ADCF7FD7D684D5B71AC4C0E392A533DE"
                                + "143AACC68CCDD77F3FB47AFCF59F058E3873FCF454CED0EF1B5DF8A18A14C4D56A4C81"
                                + "E6F3D3D8246BDF2E402C78AB50DCA8CC603E2681B9E28A032BFE156DDC04C266986E31"
                                + "10112A86CC01C5150203010001";

        /// <summary>
        /// The URL to the Axivion Dashboard, up to the project name.
        /// </summary>
        [EnvironmentVariable("DASHBOARD_BASE_URL")]
        [Tooltip("The URL to the Axivion Dashboard, up to the project name.")]
        public string BaseUrl = "https://stvr2.informatik.uni-bremen.de:9443/axivion/projects/SEE/";

        /// <summary>
        /// The API token for the Axivion Dashboard.
        /// </summary>
        /// <remarks>This must be a log-in token to have the necessary rights.</remarks>
        [EnvironmentVariable("DASHBOARD_TOKEN")]
        [Tooltip("The API token for the Axivion Dashboard.")]
        public string Token = "0.000000000000Q.2Jb6PIgB1pk4g8ss-DtnfdtDp0xlcugYQHFRcRvBRH4";

        /// <summary>
        /// When true, receiving Dashboard models which have more fields than the C# models will throw an error
        /// instead of silently ignoring the extra fields.
        /// </summary>
        [EnvironmentVariable("DASHBOARD_STRICT_MODE")]
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
        [EnvironmentVariable("DASHBOARD_HIDE_COPYRIGHTED_TEXT")]
        [Tooltip("Whether to hide any possibly copyrighted texts.")]
        public bool HideCopyrightedTexts = false;

        /// <summary>
        /// Lazy wrapper around the instance of this class.
        /// Will search the scene for the component upon first access.
        /// </summary>
        /// <seealso cref="Lazy{T}"/>
        private static readonly Lazy<DashboardRetriever> instance =
            new(FindObjectOfType<DashboardRetriever>);

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
                case DashboardVersion.Difference.MajorOlder:
                case DashboardVersion.Difference.MinorOlder:
                    // If major or minor version of the dashboard is older, we may use features that aren't existent
                    // in it yet, so we have to notify the user with a warning.
                    ShowNotification.Error("Dashboard Version too old",
                                           $"The version of the dashboard is {version}, but the DashboardRetriever "
                                           + $"has been written for version {DashboardVersion.SupportedVersion}."
                                           + $" Please update your dashboard.");
                    break;
                case DashboardVersion.Difference.PathOlder:
                    // If patch version is older, there may be some bugfixes / security problems not accounted for.
                    ShowNotification.Warn("Dashboard Version outdated",
                                          $"Your dashboard has version {version} but this API supports "
                                          + $"{DashboardVersion.SupportedVersion}. The difference in versions is small,"
                                          + "so this shouldn't be too critical, but please update your dashboard to avoid any issues.");
                    break;
                case DashboardVersion.Difference.MajorNewer:
                    // Major new version can introduce breaking changes
                    ShowNotification.Error("Dashboard Version unsupported",
                                           $"Your dashboard has a major new version ({version}) compared to the supported version "
                                           + $"({DashboardVersion.SupportedVersion}), which may have introduced breaking changes. "
                                           + "Please update SEE's retrieval code accordingly.");
                    break;
                case DashboardVersion.Difference.MinorNewer:
                case DashboardVersion.Difference.PatchNewer:
                    // Minor and patch updates shouldn't impact existing functionality, but the retrieval code
                    // should still be updated by a developer.
                    Debug.LogWarning($"The dashboard uses version {version} while the retrieval code has been "
                                     + $"written for version {DashboardVersion.SupportedVersion}. Please update SEE's retrieval "
                                     + "code accordingly.");
                    break;
                case DashboardVersion.Difference.ExtraOlder:
                case DashboardVersion.Difference.ExtraNewer:
                    // Extra changes are assumed to be small enough to not even warrant a warning.
                    Debug.Log($"Dashboard version {version} differs slightly from supported version "
                              + $"{DashboardVersion.SupportedVersion}.");
                    break;
                case DashboardVersion.Difference.Equal:
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

        private void Awake()
        {
            SetEnvironmentVariableFields(this);
        }

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
            private readonly string acceptKey;

            /// <summary>
            /// Instantiates a new <see cref="AxivionCertificateHandler"/> with the given <paramref name="acceptKey"/>
            /// as the accepted public key.
            /// </summary>
            /// <param name="acceptKey">The public key of the accepted certificate.</param>
            public AxivionCertificateHandler(string acceptKey)
            {
                this.acceptKey = acceptKey;
            }

            /// <summary>
            /// Validates the given <paramref name="certificateData"/> by comparing it with <see cref="acceptKey"/>.
            /// </summary>
            /// <param name="certificateData">Certificate which shall be validated</param>
            /// <returns>True iff the certificate's validation was successful</returns>
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                // Code adapted from:
                // https://docs.unity3d.com/ScriptReference/Networking.CertificateHandler.ValidateCertificate.html
                X509Certificate2 certificate = new(certificateData);
                string certPublicKey = certificate.GetPublicKeyString();
                return certPublicKey?.Equals(acceptKey) ?? false;
            }
        }
    }
}