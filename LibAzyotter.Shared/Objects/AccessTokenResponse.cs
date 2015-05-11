namespace LibAzyotter
{
    public class AccessTokenResponse
    {
        public string OAuthToken { get; set; }
        public string OAuthTokenSecret { get; set; }
        public long UserId { get; set; }
        public string ScreenName { get; set; }
    }
}
