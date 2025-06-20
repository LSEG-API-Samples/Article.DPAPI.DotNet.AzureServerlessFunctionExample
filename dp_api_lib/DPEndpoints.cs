namespace dp_api_lib
{
    /// <summary>
    /// Class to hold Data Platform service endpoint required by the main application
    /// </summary>
    public class DPEndpoints
    {
        public static readonly string DPServer = $"api.refinitiv.com";
        public static readonly string AuthTokenService = $"auth/oauth2/v1/token";
        public static readonly string EsgUniverse = $"data/environmental-social-governance/v1/universe";
    }

}
