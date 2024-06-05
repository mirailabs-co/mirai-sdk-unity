using System;
using System.IO;
using UnityEngine;

namespace Mirai
{
#if UNITY_EDITOR
    using UnityEditor;
#endif
    public class MiraiSdkSettings:ScriptableObject
    {
        [Serializable]
        public enum Environment
        {
            Develop,
            Product,
        }
        [Serializable]
        public enum CallMiraiAppType
        {
            UseUniversalLink,
            SendNotification,
        }

        private const string assetPath = "Assets/Mirai SDK/Resources";
        private static MiraiSdkSettings _instance;
        public static MiraiSdkSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<MiraiSdkSettings>(nameof(MiraiSdkSettings));
#if UNITY_EDITOR
                    if (_instance == null)
                    {
                        _instance = CreateInstance<MiraiSdkSettings>();
                        if (!Directory.Exists(assetPath))
                        {
                            Directory.CreateDirectory(assetPath);
                            AssetDatabase.Refresh();
                        }
                        AssetDatabase.CreateAsset(_instance, Path.Combine(assetPath, $"{nameof(MiraiSdkSettings)}.asset"));
                        Debug.Log("MiraiSDK Settings created!");
                    }
#endif
                }
                return _instance;
            }
        }

        public string MiraiAppSchema => "miraiapp://";
        public string MiraiAppUniversalLink => "https://go.miraiapp.io/";
        public Environment environment=Environment.Develop;
        public bool autoInit=true;
        public string ClientID
        {
            get=>environment==Environment.Product?client_id_prod:client_id_dev;
            set
            {
                if (environment==Environment.Product) client_id_prod = value;
                else client_id_dev = value;
            }
        }
        [SerializeField] private string client_id_dev;
        [SerializeField] private string client_id_prod;
        [SerializeField] private string appSchema;
        public CallMiraiAppType callMiraiAppType = CallMiraiAppType.SendNotification;
        public string AppSchema
        {
            get=>string.IsNullOrEmpty(appSchema) || appSchema.EndsWith("://") ? appSchema : appSchema + "://";
            set => appSchema = value;
        }
        public bool autoLoginCachedAfterSdkInit=true;

        public string authEndPoint => environment == Environment.Product
            ? "https://gw.miraiid.io/api/auth/"
            : "https://id-gw-dev.mirailabs.co/api/auth/";

        public string mpcEndPoint => environment == Environment.Product
            ? "https://id-api.mirailabs.co/"
            : "https://id-api-dev.mirailabs.co/";
        public string scope = "openid email offline_access profile";
        public string redirect_uri => $"{AppSchema}miraiid-sso-callback";

        public string shardsTechEndPoint => environment == Environment.Product
            ? "https://api.shards.tech/"
            : "https://api-dev.shards.tech/";

        public string referralEndPoint => environment == Environment.Product
            ? "https://ref.mirailabs.co/"
            : "https://ref-dev.mirailabs.co/";

        public string referralLink
        {
            get=>environment==Environment.Product?referralLink_prod:referralLink_dev;
            set
            {
                if (environment==Environment.Product) referralLink_prod = value;
                else referralLink_dev = value;
            }
        }
        [SerializeField] private string referralLink_dev;
        [SerializeField] private string referralLink_prod;
        public bool autoReferal = true;
    }
}


