using Newtonsoft.Json;

namespace LibAzyotter
{
    public class OAuth2TokenResponse
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}
