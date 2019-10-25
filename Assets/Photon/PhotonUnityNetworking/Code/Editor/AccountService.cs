// ----------------------------------------------------------------------------
// <copyright file="AccountService.cs" company="Exit Games GmbH">
//   Photon Cloud Account Service - Copyright (C) 2012 Exit Games GmbH
// </copyright>
// <summary>
//   Provides methods to register a new user-account for the Photon Cloud and
//   get the resulting appId.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------


#if UNITY_EDITOR
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Photon.Pun
{
    /// <summary>
    /// Creates a instance of the Account Service to register Photon Cloud accounts.
    /// </summary>
    public class AccountService
    {
        private const string ServiceUrl = "https://partner.photonengine.com/api/Unity/User/RegisterEx";

        private readonly Dictionary<string, string> RequestHeaders = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "x-functions-key", "VQ920wVUieLHT9c3v1ZCbytaLXpXbktUztKb3iYLCdiRKjUagcl6eg==" }
        };

        /// <summary>
        /// Attempts to create a Photon Cloud Account asynchronously.
        /// Once your callback is called, check ReturnCode, Message and AppId to get the result of this attempt.
        /// </summary>
        /// <param name="email">Email of the account.</param>
        /// <param name="serviceTypes">Defines which type of Photon-service is being requested.</param>
        /// <param name="callback">Called when the result is available.</param>
        public bool RegisterByEmail(string email, string serviceTypes, Action<AccountServiceResponse> callback = null, Action<string> errorCallback = null)
        {
            if (!IsValidEmail(email))
            {
                Debug.LogErrorFormat("Email \"{0}\" is not valid", email);
                return false;
            }
            if (string.IsNullOrEmpty(serviceTypes))
            {
                Debug.LogError("serviceTypes string is null or empty");
                return false;
            }
            AccountServiceRequest req = new AccountServiceRequest();
            req.Email = email;
            req.ServiceTypes = serviceTypes;
            //Debug.LogWarningFormat("Service types sent {0}", serviceTypes);
            return this.RegisterByEmail(req, callback, errorCallback);
        }

        public bool RegisterByEmail(string email, List<ServiceTypes> serviceTypes, Action<AccountServiceResponse> callback = null, Action<string> errorCallback = null)
        {
            if (serviceTypes == null || serviceTypes.Count == 0)
            {
                Debug.LogError("serviceTypes list is null or empty");
                return false;
            }
            return this.RegisterByEmail(email, GetServiceTypesFromList(serviceTypes), callback, errorCallback);
        }

        public bool RegisterByEmail(AccountServiceRequest request, Action<AccountServiceResponse> callback = null, Action<string> errorCallback = null)
        {
            if (request == null)
            {
                Debug.LogError("Registration request is null");
                return false;
            }
            string fullUrl = GetUrlWithQueryStringEscaped(request);
            //Debug.LogWarningFormat("Full URL {0}", fullUrl);
            PhotonEditorUtils.StartCoroutine(
                PhotonEditorUtils.HttpPost(fullUrl,
                    RequestHeaders,
                    null,
                    s =>
                    {
                        //Debug.LogWarningFormat("received response {0}", s);
                        if (string.IsNullOrEmpty(s))
                        {
                            if (errorCallback != null)
                            {
                                errorCallback("Server's response was empty. Please register through account website during this service interruption.");
                            }
                        }
                        else
                        {
                            AccountServiceResponse ase = this.ParseResult(s);
                            if (ase == null)
                            {
                                if (errorCallback != null)
                                {
                                    errorCallback("Error parsing registration response. Please try registering from account website");
                                }
                            }
                            else if (callback != null)
                            {
                                callback(ase);
                            }
                        }
                    },
                    e =>
                    {
                        if (errorCallback != null)
                        {
                            errorCallback(e);
                        }
                    })
            );
            return true;
        }

        private static string GetUrlWithQueryStringEscaped(AccountServiceRequest request)
        {
            string email = UnityEngine.Networking.UnityWebRequest.EscapeURL(request.Email);
            string st = UnityEngine.Networking.UnityWebRequest.EscapeURL(request.ServiceTypes);
            return string.Format("{0}?email={1}&st={2}", ServiceUrl, email, st);
        }

        /// <summary>
        /// Reads the Json response and applies it to local properties.
        /// </summary>
        /// <param name="result"></param>
        private AccountServiceResponse ParseResult(string result)
        {
            try
            {
                AccountServiceResponse res = JsonUtility.FromJson<AccountServiceResponse>(result);
                // Unity's JsonUtility does not support deserializing Dictionary, we manually parse it, dirty & ugly af, better then using a 3rd party lib
                if (res.ReturnCode == AccountServiceReturnCodes.Success)
                {
                    string[] parts = result.Split(new[] { "\"ApplicationIds\":{" }, StringSplitOptions.RemoveEmptyEntries);
                    parts = parts[1].Split('}');
                    string applicationIds = parts[0];
                    if (!string.IsNullOrEmpty(applicationIds))
                    {
                        parts = applicationIds.Split(new[] { ',', '"', ':' }, StringSplitOptions.RemoveEmptyEntries);
                        res.ApplicationIds = new Dictionary<string, string>(parts.Length / 2);
                        for (int i = 0; i < parts.Length; i = i + 2)
                        {
                            res.ApplicationIds.Add(parts[i], parts[i + 1]);
                        }
                    }
                    else
                    {
                        Debug.LogError("The server did not return any AppId, ApplicationIds was empty in the response.");
                        return null;
                    }
                }
                return res;
            }
            catch (Exception ex) // probably JSON parsing exception, check if returned string is valid JSON
            {
                Debug.LogException(ex);
                return null;
            }
        }

        private static string GetServiceTypesFromList(List<ServiceTypes> appTypes)
        {
            if (appTypes != null)
            {
                string serviceTypes = string.Empty;
                if (appTypes.Count > 0)
                {
                    serviceTypes = ((int)appTypes[0]).ToString();
                    for (int i = 1; i < appTypes.Count; i++)
                    {
                        int appType = (int)appTypes[i];
                        serviceTypes = string.Format("{0},{1}", serviceTypes, appType);
                    }
                }
                return serviceTypes;
            }
            return null;
        }

        //https://github.com/rhyous/EmailRegEx
        public static string ComplexEmailPattern = @"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*" // local-part
                                                    + "@"
                                                    + @"((([\w]+([-\w]*[\w]+)*\.)+[a-zA-Z]+)|" // domain
                                                    + @"((([01]?[0-9]{1,2}|2[0-4][0-9]|25[0-5]).){3}[01]?[0-9]{1,2}|2[0-4][0-9]|25[0-5]))\z"; // or IP Address

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                return false;
            }
            return Regex.IsMatch(email, ComplexEmailPattern,
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase |
                RegexOptions.Singleline);
        }
    }

    [Serializable]
    public class AccountServiceResponse
    {
        public int ReturnCode;
        public string Message;
        public Dictionary<string, string> ApplicationIds; // Unity's JsonUtility does not support deserializing Dictionary
    }
    
    [Serializable]
    public class AccountServiceRequest
    {
        public string Email;
        public string ServiceTypes;
    }

    public class AccountServiceReturnCodes
    {
        public static int Success = 0;
        public static int EmailAlreadyRegistered = 8;
        public static int InvalidParameters = 12;
    }

    public enum ServiceTypes
    {
        Realtime = 0,
        Turnbased = 1,
        Chat = 2,
        Voice = 3,
        TrueSync = 4,
        Pun = 5,
        Thunder = 6,
        Bolt = 20
    }
}

#endif