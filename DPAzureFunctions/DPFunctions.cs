using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using dp_api_lib;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
namespace DPAzureFunctions;

public class DPHttpTrigger
{
    private readonly ILogger<DPHttpTrigger> _logger;
    private readonly IHttpClientFactory _client;
    private readonly IDPAuthorizeService _authService;
    private readonly IEsgService _esgService;
   
    public DPHttpTrigger(ILogger<DPHttpTrigger> logger, IHttpClientFactory client)
    {
        _logger = logger;
        _client = client;
        _authService = new DPAuthorizeService(_client);
        _esgService = new EsgService(_client);
    }

   

   

    /// <summary>
    /// Function to get new Data Platform Token from the Data Platform server.
    ///
    /// </summary>
    /// <param name="req">HTTP request with the following parameters.
    /// username: Data Platform username or Machine Id
    /// password: Data Platform password
    /// appkey: Data Platform client id or appkey</param>
    /// refreshtoken: Refresh Token to get a new Access Token
    /// userefreshtoken: true/false. You need to pass it with the query parameters to tell the function to use refreshtoken to get a new access token instead.
    /// <param name="log"></param>
    /// <returns>Response Message from Data Platform Token service with addtional info from HTTP response message in JSON format</returns>
    [Function("GetNewToken")]
    public async Task<IActionResult> GetNewToken(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
        ILogger log)
    {
        var username = string.Empty;
        var password = string.Empty;
        var appId = string.Empty;
        var useRefreshToken = "false";
        var refreshToken = string.Empty;

        if (req.Method.ToLower() == "get")
        {
            username = req.Query["username"];
            password = req.Query["password"];
            appId = req.Query["appid"];
            useRefreshToken = req.Query["userefreshtoken"];
            refreshToken = req.Query["refreshtoken"];
        }
        else if (req.Method.ToLower() == "post")
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JObject.Parse(requestBody);
            username = data.username;
            password = data.password;
            appId = data.appid;
            useRefreshToken = data.userefreshtoken;
            refreshToken = data.refreshtoken;

        }

        if (string.IsNullOrEmpty(useRefreshToken))
            useRefreshToken = "false";



        if (string.IsNullOrEmpty(refreshToken))
            refreshToken = string.Empty;

        var response = await _authService.GetToken(username, password, appId, "trapi", refreshToken, Convert.ToBoolean(useRefreshToken));
        if (response.IsSuccess)
        {
            var tokenData = response as DPTokenResponse;

            return new JsonResult(JsonConvert.SerializeObject(tokenData));
        }

        var errorData = response as DPAuthenticationError;
        return new JsonResult(JsonConvert.SerializeObject(errorData));
    }

    [Function("GetESGUniverse")]
    public async Task<IActionResult> GetESGUniverse(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
        ILogger log)
    {
        var token = string.Empty;
        var tokenType = string.Empty;

        var returnContent = string.Empty;
        if (req.Method.ToLower() == "get")
        {
            token = req.Query["token"];
            tokenType = req.Query["tokentype"];
            returnContent = req.Query["showuniverse"];
        }
        else if (req.Method.ToLower() == "post")
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JObject.Parse(requestBody);
            token = data.token;
            tokenType = data.tokentype;
            returnContent = data.showuniverse;
        }
        tokenType ??= "Bearer";

        returnContent ??= "true";

        var response = await _esgService.GetEsgUniverse(token, tokenType);
        if (response.IsSuccess)
        {

            var esgCache = response as DPEsgResponse;


            if (returnContent.Contains("false"))
            {
                esgCache.UniverseData = new List<EsgUniverseData>();
                esgCache.UniverseHeaderMetas = new List<EsgUniverseHeaderMeta>();
            }

            return new JsonResult(JsonConvert.SerializeObject(esgCache));
        }

        var errorData = response as DPEsgError;
        return new JsonResult(JsonConvert.SerializeObject(errorData));

    }

}