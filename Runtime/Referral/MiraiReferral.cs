using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Mirai
{
    public static class MiraiReferral
    {
        private static RestAPI.Connection _referralApi;
        public static ReferralUser OriginReferralUser;
        public static void Init()
        {
            _referralApi = new RestAPI.Connection(MiraiSdkSettings.Instance.referralEndPoint);
            MiraiID.OnAuthStateChanged -= MiraiAuthOnAuthStateChanged;
            MiraiID.OnAuthStateChanged += MiraiAuthOnAuthStateChanged;
        }
        private static async void MiraiAuthOnAuthStateChanged()
        {
            if (MiraiID.UserProfile != null)
            {
                _referralApi.Headers["Authorization"] = $"Bearer {MiraiID.Token.access_token}";
                await FetchOriginReferralUser();
                var codeFromDeeplinkAirBridge = "test";
                if (MiraiSdkSettings.Instance.autoReferal && OriginReferralUser == null &&  !string.IsNullOrEmpty(codeFromDeeplinkAirBridge))
                {
                    await TypeReferralCode(codeFromDeeplinkAirBridge);
                }
            }
            else
            {
                _referralApi.Headers.Remove("Authorization");
            }
        }
        
        public static Task<ReferralCodeDTO> TypeReferralCode(string referralCode)
            => _referralApi.Request<ReferralCodeDTO>("v1/referral", data:new { referralCode });

        public static Task<ReferralCodeDTO> GetReferralCode()
        {
            return _referralApi.Request<ReferralCodeDTO>("v1/referral", data:new { referralCode="" });
        }
        
        public static async Task FetchOriginReferralUser()
        {
            try
            {
                OriginReferralUser = await _referralApi.Request<ReferralUser>("v1/referral/origin","origin");
            }
            catch (Exception)
            {
                OriginReferralUser = null;
            }
        }
        public static Task<List<ReferralUser>> GetReferredUsers(int page = 0)
            => _referralApi.Request<List<ReferralUser>>($"v1/referral/children/{page}","children");
    }
}
