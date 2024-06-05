using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Mirai
{
    public static class MiraiID
    {
        private static RestAPI.Connection _authApi;
        private static string _keyTokenCached;
        private static TokenData _token;
        public static TokenData Token
        {
            get
            {
                if (_token != null) return _token;
                var jsonToken = PlayerPrefs.GetString(_keyTokenCached, "null");
                _token = JsonConvert.DeserializeObject<TokenData>(jsonToken);
                return _token;
            }
        }
        private static async Task SetToken(TokenData value)
        {
            _token = value;
            var jsonToken = JsonConvert.SerializeObject(_token);
            PlayerPrefs.SetString(_keyTokenCached, jsonToken);
            await FetchUserProfile();
        }
        public static void Init()
        {
            _keyTokenCached= $"{nameof(MiraiID)}{MiraiSdkSettings.Instance.ClientID}";
            _authApi = new RestAPI.Connection(MiraiSdkSettings.Instance.authEndPoint);
            _userProfile = null;
            _token = null;
        }
     
        private static UserProfile _userProfile;
        public static UserProfile UserProfile
        {
            get => _userProfile;
            private set
            {
                if (_userProfile?.userId == value?.userId) return;
                _userProfile = value;
                OnAuthStateChanged?.Invoke();
            }
        }
        public static event Action OnAuthStateChanged;
        private static async Task FetchUserProfile()
        {
            if (Token == null) UserProfile = null;
            else
            {
                try
                {
                    UserProfile = await RestAPI.Request<UserProfile>(MiraiSdkSettings.Instance.mpcEndPoint + "v1/user/profile",
                        headers: new Dictionary<string, string>
                        {
                            { "Authorization", $"Bearer {Token.access_token}" }
                        });
                }
                catch (Exception)
                {
                    await SetToken(null);
                }
            }
        }

        public static async Task LoginCached()
        {
            if (Token != null && Token.AccessTokenAlmostExpired())
            {
                if (!Token.RefreshTokenExpired())
                    await RefreshToken();
                else await SetToken(null);
            }
            else await SetToken(Token);
        }
        private static async Task RefreshToken()
        {
            try
            {
                await SetToken(await _authApi.Request<TokenData>("refresh-token", data: new { client_id = MiraiSdkSettings.Instance.ClientID, Token.refresh_token, MiraiSdkSettings.Instance.scope }));
            }
            catch (Exception)
            {
                await SetToken(null);
            }
        }
        public static async Task<TokenData> LoginEmailPassword(string email, string password,CancellationToken cancellationToken=default)
        {
            await SetToken(await _authApi.Request<TokenData>("login", data: new { email, password, client_id = MiraiSdkSettings.Instance.ClientID, MiraiSdkSettings.Instance.scope }, cancellationToken: cancellationToken));
            return Token;
        }
        public static async Task<TokenData> LoginSocial(LoginSocialType type,CancellationToken cancellationToken=default)
        {
            var (linkParams,redirect_uri) = await RequestUrlAndWaitDeeplink(type.ToString(), cancellationToken);
            if (linkParams.TryGetValue("code", out var code) && linkParams.TryGetValue("state", out var state))
                await RequestAccessTokenOAuth(code, state, redirect_uri);
            else
                throw new Exception("Not have access! Login unsuccessful!");
            return Token;
        }

        private static async Task RequestAccessTokenOAuth(string code, string state, string redirect_uri)
        {
            await SetToken(
                await _authApi.Request<TokenData>("token",
                    data: new
                    {
                        client_id = MiraiSdkSettings.Instance.ClientID, 
                        redirect_uri,
                        MiraiSdkSettings.Instance.scope, 
                        code, 
                        state
                    }));
        }

        public static async Task Register(CancellationToken cancellationToken=default)
        {
            await RequestUrlAndWaitDeeplink("REGISTER", cancellationToken);
        }
        public static async Task Forgot(CancellationToken cancellationToken=default)
        {
            await RequestUrlAndWaitDeeplink("FORGOT", cancellationToken);
        }
        private static async Task<(IDictionary<string,string> linkParams,string redirect_uri)> RequestUrlAndWaitDeeplink(string type,CancellationToken cancellationToken=default)
        {
            var  redirect_uri = MiraiSdkSettings.Instance.redirect_uri;
#if UNITY_EDITOR || (!UNITY_IOS && !UNITY_ANDROID)
            redirect_uri = $"http://localhost:{Utils.FindAvailablePort(7890, 7891)}";
#endif
            var endPoint = $"request-oauth?type={type}&client_id={MiraiSdkSettings.Instance.ClientID}&redirect_uri={redirect_uri}&scope={MiraiSdkSettings.Instance.scope}";
            var url = await _authApi.Request<string>(endPoint, "url", cancellationToken: cancellationToken);
            if (string.IsNullOrEmpty(url)) throw new Exception("Cannot get url!");
#if UNITY_IOS && !UNITY_EDITOR
            SFSafariView.LaunchUrl(url);
#else
            Application.OpenURL(url);
#endif
            var result= await Utils.WaitDeepLinkQueryCallback(redirect_uri, cancellationToken);
#if UNITY_IOS && !UNITY_EDITOR
            SFSafariView.Dismiss();
#endif
            return (result, redirect_uri);
        }
        public static async Task Logout(CancellationToken cancellationToken=default)
        {
            if (Token==null) return;
            var token = Token.refresh_token;
            await SetToken(null);
            try
            {
                await _authApi.Request("revoke-token", new { token, client_id = MiraiSdkSettings.Instance.ClientID }, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
