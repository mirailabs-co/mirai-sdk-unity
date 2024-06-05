
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Mirai
{
    public static class MiraiSDK
    {
        public static bool MiraiAppInstalled
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Utils.CheckApp("co.mirailabs.app");
#elif UNITY_IOS && !UNITY_EDITOR
                return Utils.CheckApp("miraiapp");
#else
                return false;
#endif
            }
        }
        public static async Task InitAsync()
        {
            MiraiID.Init();
            MiraiReferral.Init();
            if (MiraiSdkSettings.Instance.autoLoginCachedAfterSdkInit)
                await MiraiID.LoginCached();
        }
        public static async Task<IDictionary<string,string>> CallMiraiApp(string hostRequest,string hostResponse="",CancellationToken cancellationToken=default)
        {
            var host = MiraiSdkSettings.Instance.callMiraiAppType == MiraiSdkSettings.CallMiraiAppType.UseUniversalLink
                ? MiraiSdkSettings.Instance.MiraiAppUniversalLink
                : MiraiSdkSettings.Instance.MiraiAppSchema;
            Application.OpenURL($"{host}{hostRequest}");
            return await Utils.WaitDeepLinkQueryCallback($"{MiraiSdkSettings.Instance.AppSchema}{hostResponse}", cancellationToken);
        }
    }
}