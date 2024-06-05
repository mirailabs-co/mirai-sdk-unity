using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Mirai
{
    public class MiraiMarketplace
    {
        public class BuyResult
        {
            public string listingId;
            public string tokenUri;

            public BuyResult(string listingId, string tokenUri)
            {
                this.listingId = listingId;
                this.tokenUri = tokenUri;
            }
        }
        public static Task<BuyResult> Buy(string collection_slug,long chain_id=2195, string nft_address=null , long token_id=-1,CancellationToken cancellationToken=default)
        {
            var parameters = new Dictionary<string, object>
            {
                { "collection_slug", collection_slug },
                { "chain_id", chain_id }
            };
            if (!string.IsNullOrEmpty(nft_address))
            {
                parameters.Add("nft", nft_address);
                if (token_id > -1) 
                    parameters.Add("token_id", token_id);
            }

            return InternalBuy(parameters, cancellationToken);
        }
        private static async Task<BuyResult> InternalBuy(Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            var host = MiraiSDK.MiraiAppInstalled
                ? MiraiSdkSettings.Instance.MiraiAppSchema
                : MiraiSdkSettings.Instance.MiraiAppUniversalLink;
            var schema = MiraiSdkSettings.Instance.AppSchema.Split(":")[0];
            if (MiraiID.UserProfile != null) parameters.Add("mirai_id", MiraiID.UserProfile.userId);
            var url = $"{host}marketplace?app={schema}&callback=buy_pack&{string.Join('&', parameters.Keys.Select(k => $"{k}={parameters[k]}"))}";
            Application.OpenURL(url);
            var res=await Utils.WaitDeepLinkQueryCallback($"{MiraiSdkSettings.Instance.AppSchema}buy_pack", cancellationToken);
            if (res.TryGetValue("listingId", out var listingId) && res.TryGetValue("tokenUri", out var tokenUri))
                return new BuyResult(listingId, tokenUri);
            return null;
        }
    }
}