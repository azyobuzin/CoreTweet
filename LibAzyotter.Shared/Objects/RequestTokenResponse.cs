namespace LibAzyotter
{
    public class RequestTokenResponse
    {
        public string OAuthToken { get; set; }
        public string OAuthTokenSecret { get; set; }
        public bool OAuthCallbackConfirmed { get; set; }
    }
}
