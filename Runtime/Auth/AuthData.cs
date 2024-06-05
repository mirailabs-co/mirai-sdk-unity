using System;

namespace Mirai
{
    [Serializable]
    public enum LoginSocialType
    {
        LOGIN,
        APPLE,
        GOOGLE,
        FACEBOOK,
    }

    [Serializable]
    public class TokenData
    {
        public string type;
        public string access_token;
        public long expires_in;
        public string refresh_token;
        public long refresh_expires_in;
        public string id_token;

        public bool AccessTokenAlmostExpired()
            => DateTime.UtcNow >= DateTimeOffset.FromUnixTimeSeconds(expires_in).DateTime - TimeSpan.FromHours(12);

        public bool RefreshTokenExpired()
            => DateTime.UtcNow >= DateTimeOffset.FromUnixTimeSeconds(refresh_expires_in).DateTime;
    }
    [Serializable]
    public class UserProfile
    {
        public string userId;
        public string address;
        public string email;
        public bool emailVerified;
        public string familyName;
        public string givenName;
    }
}