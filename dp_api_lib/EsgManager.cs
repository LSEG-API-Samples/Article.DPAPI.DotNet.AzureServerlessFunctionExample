﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dp_api_lib
{
    /// <summary>
    /// ESG Error class to show error info from ESG service.
    /// </summary>
    public class ESGError
    {
        public string InvalidName { get; set; }
        public IList<string> InvalidValues { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
    /// <summary>
    /// RDEsgError class. The main function will return data inside this class to client.
    /// It contains both HTTP status codes and ESG error if it has some issue with the request.
    /// </summary>
    public class DPEsgError : IDPResponseMessage
    {
        public HttpStatusCode HttpResponseStatusCode { get; set; }
        public bool IsSuccess => HttpResponseStatusCode == HttpStatusCode.OK;
        public string HttpResponseStatusText { get; set; }

        [Newtonsoft.Json.JsonProperty("code", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Code { get; set; }
        [Newtonsoft.Json.JsonProperty("errors", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public ESGError Errors { get; set; }
        [Newtonsoft.Json.JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Id { get; set; }
        [Newtonsoft.Json.JsonProperty("message", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Message { get; set; }
        [Newtonsoft.Json.JsonProperty("status", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Status { get; set; }
    }
    /// <summary>
    /// This class represent ESG Meta data for the headers column.
    /// </summary>
    public class EsgUniverseHeaderMeta
    {
        [Newtonsoft.Json.JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("title", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Title { get; set; }
        [Newtonsoft.Json.JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Type { get; set; }
        [Newtonsoft.Json.JsonProperty("description", DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public string Description { get; set; }
    }
    /// <summary>
    /// EsgUnivesreData represent content of ESG Universe response.
    /// ESG universe function will provide a list of instance of this class.
    /// </summary>
    public class EsgUniverseData
    {
        public string PermId { get; set; }
        public string PrimaryRic { get; set; }
        public string CommonName { get; set; }
    }

    /// <summary>
    /// This class will be used when the request is success and has no error.
    /// It will return this class instead of the DPEsgError.
    /// </summary>
    public class DPEsgResponse : IDPResponseMessage
    {
        public HttpStatusCode HttpResponseStatusCode { get; set; }
        public bool IsSuccess => HttpResponseStatusCode == HttpStatusCode.OK;
        public string HttpResponseStatusText { get; set; }
        public long Count { get; set; }
        public IList<EsgUniverseHeaderMeta> UniverseHeaderMetas { get; set; }
        public IList<EsgUniverseData> UniverseData { get; set; }
        public override string ToString()
        {
            var dumpText = new StringBuilder();
            dumpText.Append($"HTTP Status Code:{this.HttpResponseStatusCode}\n");
            dumpText.Append($"HttpResponseStatusText:{this.HttpResponseStatusText}\n");
            dumpText.Append($"===============================\n");
            dumpText.Append($"Count:{this.Count}\n");

            dumpText.Append($"==============================\n");
            return dumpText.ToString();
        }
    }

    public interface IEsgService
    {
        Task<IDPResponseMessage> GetEsgUniverse(string requestToken, string tokenType, string redirectUrl = null);
    }
    public class EsgService : IEsgService
    {
        private readonly IHttpClientFactory _clientFactory;

        public EsgService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }
        /// <summary>
        /// Get EsgUniverse function used to retrieve ESG universe data.
        /// </summary>
        /// <param name="requestToken"> is valid Data Platform access token </param>
        /// <param name="tokenType"> is type of Data Platform access token. Default is Bearer.</param>
        /// <param name="redirectUrl">The server may reponse with new redirectUrl. Internal function will detect and use the new URL instead.</param>
        /// <returns></returns>
        public async Task<IDPResponseMessage> GetEsgUniverse(string requestToken, string tokenType = "Bearer", string redirectUrl = null)
        {
            var tokenUri = new UriBuilder()
            {
                Scheme = "https",
                Host = DPEndpoints.DPServer,
                Path = DPEndpoints.EsgUniverse

            };
            if (!string.IsNullOrEmpty(redirectUrl))
            {
                tokenUri = new UriBuilder(redirectUrl);
            }
            var request = new HttpRequestMessage(HttpMethod.Get, tokenUri.ToString());
            // Set Request Headers
            request.Headers.Authorization = new AuthenticationHeaderValue(tokenType, requestToken);
            request.Headers.Add("AllowAutoRedirect", "False");
            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            var dpEsgResponse = new DPEsgResponse();
            if (response.IsSuccessStatusCode)
            {
                if (response.Headers.TransferEncodingChunked == true || response.Content != null)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(json);
                    if (jsonObject["links"]?["count"] != null)
                        dpEsgResponse.Count = long.Parse(jsonObject["links"]["count"].ToString());

                    dpEsgResponse.UniverseHeaderMetas = jsonObject["headers"]?.ToObject<IList<EsgUniverseHeaderMeta>>();
                    var esgData = jsonObject["data"]?.ToObject<IList<IList<string>>>();
                    if (esgData != null)
                    {
                        dpEsgResponse.UniverseData = new List<EsgUniverseData>();
                        foreach (var esgUniverse in esgData.ToList())
                        {
                            var esg = new EsgUniverseData();
                            for (var index = 0; index < esgUniverse.Count; index++)
                            {
                                switch (index)
                                {
                                    case 0:
                                        esg.PermId = esgUniverse[index];
                                        break;
                                    case 1:
                                        esg.PrimaryRic = esgUniverse[index];
                                        break;
                                    case 2:
                                        esg.CommonName = esgUniverse[index];
                                        break;
                                }
                            }
                            dpEsgResponse.UniverseData.Add(esg);
                        }
                    }
                }
                dpEsgResponse.HttpResponseStatusCode = response.StatusCode;
                dpEsgResponse.HttpResponseStatusText = response.ReasonPhrase;
                return dpEsgResponse;
            }


            var dpEsgErrorResponse = new DPEsgError();

            if (response.Content != null)
            {
                var json = await response.Content.ReadAsStringAsync();
                dpEsgErrorResponse = JObject.Parse(json)["error"]?.ToObject<DPEsgError>();
            }
            dpEsgResponse.HttpResponseStatusCode = response.StatusCode;
            dpEsgResponse.HttpResponseStatusText = response.ReasonPhrase;
            return dpEsgErrorResponse;

        }
    }

    /// <summary>
    /// Utility class provide functions to search data from ESG Universe data according type of query user want to use.It could be PermId,CommonName and RIC name.
    /// </summary>
    public class EsgUniverseCache
    {
        /// <summary>
        /// Search By using PermId
        /// </summary>
        /// <param name="permId">Keyword to search the PermId</param>
        /// <param name="esgData">EsgData which is a List of EsgUniverseData</param>
        /// <returns></returns>
        public static IEnumerable<EsgUniverseData> GetDataByPermId(string permId, IList<EsgUniverseData> esgData)
        {
            return esgData.Where(data => (!string.IsNullOrEmpty(data.PermId) && data.PermId.Contains(permId)));

        }
        /// <summary>
        /// Search by ESG Universe Ric name 
        /// </summary>
        /// <param name="ricName">Keyword to search RIC name</param>
        /// <param name="esgData">EsgData which is a List of EsgUniverseData</param>
        /// <returns></returns>
        public static IEnumerable<EsgUniverseData> GetDataByRic(string ricName, IList<EsgUniverseData> esgData)
        {
            return esgData.Where(data => (!string.IsNullOrEmpty(data.PrimaryRic) && data.PrimaryRic.Contains(ricName)));

        }
        /// <summary>
        /// Search ESG data by using common name
        /// </summary>
        /// <param name="commonName">Keyword to search the common name</param>
        /// <param name="esgData">EsgData which is a List of EsgUniverseData</param>
        /// <returns></returns>
        public static IEnumerable<EsgUniverseData> GetDataByCommonName(string commonName, IList<EsgUniverseData> esgData)
        {
            return esgData.Where(data => (!string.IsNullOrEmpty(data.CommonName) && data.CommonName.Contains(commonName)));
        }
        /// <summary>
        /// Search ESG universe by using all fields that are PermId,Common Name and RIC name.
        /// Currently it just use all three method implemented previously to get the data and then merge the result before return it to user.
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="esgData">EsgData which is a List of EsgUniverseData</param>
        /// <returns></returns>
        public static IEnumerable<EsgUniverseData> GetData(string keyword, IList<EsgUniverseData> esgData)
        {
            var list = new List<EsgUniverseData>();
            list.AddRange(EsgUniverseCache.GetDataByRic(keyword, esgData));
            list.AddRange(EsgUniverseCache.GetDataByCommonName(keyword, esgData));
            list.AddRange(EsgUniverseCache.GetDataByPermId(keyword, esgData));
            return list.Distinct();
        }
    }
}

