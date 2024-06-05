using UnityEngine;

namespace Mirai
{
    public static class AutoInitialize
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static async void OnLoad()
        {
            if (MiraiSdkSettings.Instance.autoInit)
                await MiraiSDK.InitAsync();
        }
    }
}