﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace dp_api_lib
{
    public interface IDPResponseMessage
    {
        HttpStatusCode HttpResponseStatusCode { get; set; }
        bool IsSuccess { get; }
        string HttpResponseStatusText { get; set; }

    }
    /// <summary>
    /// DPAuthenticationError represent error message from Authentication Service. 
    /// </summary>
    public class DPAuthenticationError : IDPResponseMessage
    {
        [Newtonsoft.Json.JsonProperty("error", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Error { get; set; }

        [Newtonsoft.Json.JsonProperty("error_description", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string ErrorDescription { get; set; }

        [Newtonsoft.Json.JsonProperty("error_uri", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string ErrorUri { get; set; }
        public HttpStatusCode HttpResponseStatusCode { get; set; }
        public bool IsSuccess => HttpResponseStatusCode == HttpStatusCode.OK;
        public string HttpResponseStatusText { get; set; }
        public override string ToString()
        {
            var dumpText = new StringBuilder();
            dumpText.Append($"HTTP Status Code:{this.HttpResponseStatusCode}\n");
            dumpText.Append($"HttpResponseStatusText:{this.HttpResponseStatusText}\n");
            dumpText.Append($"===============================\n");
            dumpText.Append($"Error:{this.Error}\n");
            dumpText.Append($"Error Description:{this.ErrorDescription}\n");
            dumpText.Append($"Error Uri:{this.ErrorUri}\n");
            dumpText.Append($"==============================\n");
            return dumpText.ToString();
        }
    }
    /// <summary>
    /// DPTokenResponse represents Eroror message from Data Platform server and HTTP response. The function will return this class when it found some error.
    /// </summary>
    public class DPTokenResponse : IDPResponseMessage
    {
        [Newtonsoft.Json.JsonProperty("access_token", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string AccessToken { get; set; }

        [Newtonsoft.Json.JsonProperty("expires_in", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public long ExpiresIn { get; set; }

        [Newtonsoft.Json.JsonProperty("refresh_token", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string RefreshToken { get; set; }

        [Newtonsoft.Json.JsonProperty("scope", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Scope { get; set; }

        [Newtonsoft.Json.JsonProperty("token_type", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string TokenType { get; set; }

        public HttpStatusCode HttpResponseStatusCode { get; set; }

        public string HttpResponseStatusText { get; set; }

        public bool IsSuccess => HttpResponseStatusCode == HttpStatusCode.OK;
        public override string ToString()
        {
            var dumpText = new StringBuilder();
            dumpText.Append($"HTTP Status Code:{this.HttpResponseStatusCode}\n");
            dumpText.Append($"HttpResponseStatusText:{this.HttpResponseStatusText}\n");
            dumpText.Append($"===============================\n");
            dumpText.Append($"AccessToken:{this.AccessToken}\n");
            dumpText.Append($"ExpiresIn:{this.ExpiresIn} second\n");
            dumpText.Append($"RefreshToken:{this.RefreshToken}\n");
            dumpText.Append($"Scope:{this.Scope}\n");
            dumpText.Append($"TokenType:{this.TokenType}\n");
            dumpText.Append($"==============================\n");
            return dumpText.ToString();
        }
    }



    public interface IDPAuthorizeService
    {
        Task<IDPResponseMessage> GetToken(string username, string password, string client_id, string scope = "trapi", string refreshToken = "",
            bool useRefreshToken = false, string redirectUrl = "");
    }

    public class DPAuthorizeService : IDPAuthorizeService
    {
        private readonly IHttpClientFactory _clientFactory;

        public DPAuthorizeService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }
        /// <summary>
        /// GetToken was design to get Access Token from Data Platform Token endpoint.Currently it will set takeExclusiveSignOnControl = true by default.
        /// Once you call this function, the old session will be closed/invalid.
        /// </summary>
        /// <param name="username">Data Platform Username or ClientId</param>
        /// <param name="password">Data Platform password</param>
        /// <param name="client_id">Client Id or AppKey</param>
        /// <param name="scope">Scope, default is trapi</param>
        /// <param name="refreshToken">Refresh Token. User can use Refresh token to get a new Access Token rather than using password.
        /// Once Refresh Token is expired, user has to use password to get a new Access Token and Refrsh Token instead.</param>
        /// <param name="useRefreshToken">true/false. Client has to set to true if they wish to use refreshtoken.</param>
        /// <param name="redirectUrl">The new url to get the token</param>
        /// <returns></returns>
        public async Task<IDPResponseMessage> GetToken(string username, string password, string client_id, string scope = "trapi", string refreshToken = null,
            bool useRefreshToken = false, string redirectUrl = null)
        {
            var tokenUri = new UriBuilder()
            {
                Scheme = "https",
                Host = DPEndpoints.DPServer,
                Path = DPEndpoints.AuthTokenService

            };
            if (!string.IsNullOrEmpty(redirectUrl))
            {
                tokenUri = new UriBuilder(redirectUrl);
            }
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUri.ToString());


            var queryStringKV = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("client_id", client_id)
            };

            if (useRefreshToken)
            {
                queryStringKV.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
                queryStringKV.Add(new KeyValuePair<string, string>("refresh_token", refreshToken));
            }
            else
            {
                queryStringKV.Add(new KeyValuePair<string, string>("takeExclusiveSignOnControl", "True"));
                queryStringKV.Add(new KeyValuePair<string, string>("scope", scope));
                queryStringKV.Add(new KeyValuePair<string, string>("grant_type", "password"));
                queryStringKV.Add(new KeyValuePair<string, string>("password", password));
            }
            // Set Content 
            request.Content = new FormUrlEncodedContent(queryStringKV);

            // Set Request Headers
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Headers.Add("AllowAutoRedirect", "False");
            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            IDPResponseMessage dpTokenResult = new DPTokenResponse();
            if (response.IsSuccessStatusCode)
            {
                if (response.Headers.TransferEncodingChunked == true || response.Content != null)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    dpTokenResult = JsonConvert.DeserializeObject<DPTokenResponse>(json);
                }
                dpTokenResult.HttpResponseStatusCode = response.StatusCode;
                dpTokenResult.HttpResponseStatusText = response.ReasonPhrase;
                return dpTokenResult;
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.Moved: // 301
                case HttpStatusCode.Redirect: // 302
                case HttpStatusCode.TemporaryRedirect: // 307
                case (HttpStatusCode)308: // 308 Permanent Redirect
                    {
                        // Perform URL redirect
                        var newLocation = response.Headers.Location.ToString();
                        if (!string.IsNullOrEmpty(newLocation))
                            return await GetToken(username, password, client_id, scope, refreshToken, useRefreshToken,
                                newLocation);
                    }
                    break;
            }

            dpTokenResult = new DPAuthenticationError();

            if (response.Content != null)
            {
                var json = await response.Content.ReadAsStringAsync();
                dpTokenResult = JsonConvert.DeserializeObject<DPAuthenticationError>(json);
            }
            dpTokenResult.HttpResponseStatusCode = response.StatusCode;
            dpTokenResult.HttpResponseStatusText = response.ReasonPhrase;
            return dpTokenResult;
        }
    }
}
