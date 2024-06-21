using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Mirai
{
    public static class ShardsTech
    {
        private static RestAPI.Connection _shardsTechApi;
        public static ShardsTechConfig ShardsTechConfig;
        public static ShardsTechUser MyUser;
        public static MyGuildData MyGuild;
        public static SellSeatData MySeatOnSale;
        public static event Action OnInitDone;
        public static event Action OnLoggedIn;
        public static event Action OnLoggedOut;
        public static bool IsInitialized => ShardsTechConfig != null;
        private static bool _isLoggedIn;
        public static bool IsLoggedIn => MyUser != null && _isLoggedIn;
        public static bool MeIsOwner => MyUser != null && MyGuild != null && MyUser.userId == MyGuild.owner.userId;
        public static bool IsLinkedAddress => MyUser != null && !string.IsNullOrEmpty(MyUser.address);
        public static async Task InitAsync()
        {
            _isLoggedIn = false;
            _shardsTechApi = new RestAPI.Connection(
                MiraiSdkSettings.Instance.shardsTechEndPoint + "v1/",
                new Dictionary<string, string>
                {
                    { "x-client-id", MiraiSdkSettings.Instance.ClientID }
                });
            ShardsTechConfig = await _shardsTechApi.Request<ShardsTechConfig>("game-config");
            OnInitDone?.Invoke();
        }

        private static async Task<string> CreateDAppLink(string type, IDictionary<string, object> parameters, object metadata = null,string chain=null, CancellationToken cancellationToken = default)
        { 
            var actionId = await _shardsTechApi.Request<string>("actions", "_id", new { type, metadata }, cancellationToken: cancellationToken);
            var qParams = parameters.ToDictionary(kv => kv.Key, kv => Convert.ToString(kv.Value, CultureInfo.InvariantCulture));
            qParams.Add("gameId", ShardsTechConfig.clientId);
            qParams.Add("actionId", actionId);

            if (MiraiSDK.MiraiAppInstalled)
                qParams.Add("callBack", $"{MiraiSdkSettings.Instance.AppSchema}gsf");
            
            if (MiraiReferral.OriginReferralUser != null && MiraiReferral.OriginReferralUser.address != "0x0000000000000000000000000000000000000000") 
                qParams.Add("referral", MiraiReferral.OriginReferralUser.address);
            
            if (IsLinkedAddress)
            {
                qParams.Add("address", MyUser.address);
                qParams.Add("hash", await _shardsTechApi.Request("actions/generate-hash",new { @params = qParams}, cancellationToken: cancellationToken));
            }
            
            var dAppUrl = $"{ShardsTechConfig.linkDapp}{(type != "create-guild" ? $"/{type}" : "")}/";
            var strParams = string.Join('&', qParams.Select(kv => $"{kv.Key}={UnityWebRequest.EscapeURL(kv.Value).Replace("+", "%20")}"));
            var dAppLink = $"{dAppUrl}?{strParams}";
            Debug.Log(dAppLink);
            return dAppLink;
        }
        
        private static async Task OpenDAppLink(string dAppLink, CancellationToken cancellationToken = default)
        {
            var actionId = dAppLink.Split('?')[1].Split('&').Select(s => s.Split('=')).First(s => s[0] == "actionId")[1];
            if (ShardsTechConfig.supportWebBrowser)
            {
                Application.OpenURL(dAppLink);
                await WaitAction(actionId, cancellationToken);
            }
            else
            {
                var deepLink = !MiraiSDK.MiraiAppInstalled
                    ? $"{MiraiSdkSettings.Instance.MiraiAppUniversalLink}gsf/{UnityWebRequest.EscapeURL(dAppLink)}"
                    : $"{MiraiSdkSettings.Instance.MiraiAppSchema}gsf/{dAppLink}";
            
                if (!MiraiSDK.MiraiAppInstalled)
                {
                    if (!Application.isEditor && MiraiSdkSettings.Instance.callMiraiAppType == MiraiSdkSettings.CallMiraiAppType.SendNotification && IsLinkedAddress)
                    {
                        try
                        {
                            await _shardsTechApi.Request("actions/send-notification", new { link = dAppLink }, cancellationToken: cancellationToken);
                        }
                        catch (Exception)
                        {
                            Application.OpenURL(deepLink);
                        }
                    }
                    else Application.OpenURL(deepLink);
                    await WaitAction(actionId, cancellationToken);
                }
                else
                {
                    Application.OpenURL(deepLink);
                    var deepLinkQuery = await Utils.WaitDeepLinkQueryCallback($"{MiraiSdkSettings.Instance.AppSchema}gsf", cancellationToken);
                    if (deepLinkQuery.ContainsKey("status") && deepLinkQuery["status"] == "error")
                        throw new DappException(deepLinkQuery["message"]);
                }
            }
            if (!IsLinkedAddress) await FetchMyUser();
        }

        private static async Task WaitAction(string actionId,CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                var status = await _shardsTechApi.Request<string>($"actions/{actionId}", "status", cancellationToken: cancellationToken);
                if (status == "success") break;
                if (status == "error" || !Application.isPlaying) throw new DappException(status);
            }
        }
        
        private static async Task ExecuteAction(string type, IDictionary<string, object> parameters, object metadata = null,string chain=null, CancellationToken cancellationToken = default)
        {
            var dAppLink = await CreateDAppLink(type, parameters, metadata,chain, cancellationToken);
            await OpenDAppLink(dAppLink, cancellationToken);
        }
        
        public static Task<List<LeaderboardData>> GetLeaderboards()
            => _shardsTechApi.Request<List<LeaderboardData>>("leader-boards");
        
        #region User
        public static async Task Login(string token)
        {
            if (ShardsTechConfig == null) await InitAsync();
            _shardsTechApi.Headers["Authorization"] = token;
            await Task.WhenAll(FetchMyUser(), FetchMyGuild(), FetchMySeatOnSale());
            _isLoggedIn = true;
            OnLoggedIn?.Invoke();
        }
        public static void Logout()
        {
            _isLoggedIn = false;
            if (_shardsTechApi != null && _shardsTechApi.Headers.ContainsKey("Authorization")) 
                _shardsTechApi.Headers.Remove("Authorization");
            MyUser = null;
            MyGuild = null;
            MySeatOnSale = null;
            SetupSocket();
            OnLoggedOut?.Invoke();
        }

        public static Task LinkAddress(CancellationToken cancellationToken = default)
            => ExecuteAction("link-address", new Dictionary<string, object>()
            {
                { "userId", MyUser.userId }
            }, new {MyUser.userId}, null,cancellationToken);
        
        public static async Task<ShardsTechUser> FetchMyUser()
        {
            try
            {
                MyUser = await _shardsTechApi.Request<ShardsTechUser>("users");
            }
            catch (Exception)
            {
                MyUser = null;
            }
            
            return MyUser;
        }

        public static async Task<List<UserHistory>> GetUserHistories(int page=1, int limit=100)
            => await _shardsTechApi.Request<List<UserHistory>>($"transaction-history?limit={limit}&page={page}","data");

        public static Task<UserScoresDTO> GetUserScores(string leaderBoardId, int page = 1, int limit = 100, SortType sort = SortType.desc) => _shardsTechApi.Request<UserScoresDTO>($"user-score?{nameof(leaderBoardId)}={leaderBoardId}&{nameof(page)}={page}&{nameof(limit)}={limit}&{nameof(sort)}={sort}");
        
        #endregion
   
        #region Guild
        public static async Task CreateGuild(string name, double seatPrice, object metadata, float txGuildOwnerShare, ProfitPercentConfig profitPercentConfig,string chain=null,CancellationToken cancellationToken=default)
        {
            var rewardShareForMembers= 1 - profitPercentConfig.FractionsOwnerPercent;
            var guildOwnerShare = profitPercentConfig.GuildOwnerPercent / rewardShareForMembers;
            await ExecuteAction("create-guild", new Dictionary<string, object>()
            {
                { "name", name },
                { "slotPrice", seatPrice },
                { "rewardShareForMembers", rewardShareForMembers },
                { "txGuildOwnerShare", txGuildOwnerShare },
                { "guildOwnerShare", guildOwnerShare }
            }, metadata, chain, cancellationToken);
            await FetchMyGuild();
        }
        public static Task<GuildScoresDTO> GetGuildScores(string leaderBoardId,string name="", int page = 1, int limit = 100, SortType sort = SortType.desc)
            => _shardsTechApi.Request<GuildScoresDTO>($"guild-score?{nameof(leaderBoardId)}={leaderBoardId}{(string.IsNullOrEmpty(name)?"":$"&name={name}")}&{nameof(page)}={page}&{nameof(limit)}={limit}&{nameof(sort)}={sort}");

        public static Task<List<GuildScoreData>> GetSpecificGuildScores(string leaderBoardId, params string[] guildIds)
            => _shardsTechApi.Request<List<GuildScoreData>>($"guild-score/list?{nameof(leaderBoardId)}={leaderBoardId}&{string.Join('&', guildIds.Select(gId => nameof(guildIds) + "[]=" + gId))}");

        
        public static Task<int> GetIndexOfGuildInLeaderboard(string guildId, string leaderBoardId)
            => _shardsTechApi.Request<int>($"guild-score/{guildId}/{leaderBoardId}", "index");
        public static async Task<List<GuildHistory>> GetGuildHistories(int page=1, int limit=100)
            => await _shardsTechApi.Request<List<GuildHistory>>($"transaction-history/guild?limit={limit}&page={page}","data");
        public static Task<List<ShardsTechUser>> GetUsersOfGuild(string guildId)
            => _shardsTechApi.Request<List<ShardsTechUser>>($"guilds/users/{guildId}");
        
        #region MyGuild
        
        public static async Task<MyGuildData> FetchMyGuild()
        {
            try
            {
                MyGuild = await _shardsTechApi.Request<MyGuildData>("guilds/guild-of-user");
            }
            catch (Exception)
            {
                MyGuild = null;
            }
            SetupSocket();
            return MyGuild;
        }

        private static void SetupSocket()
        {
            if (MyGuild != null && _socketIOChat==null)
            {
                _socketIOChat = new SocketIOUnity(new Uri(MiraiSdkSettings.Instance.shardsTechEndPoint),
                    new SocketIOOptions
                    {
                        ExtraHeaders = new Dictionary<string, string>
                        {
                            {"authorization",_shardsTechApi.Headers["Authorization"]},
                            {"clientId",MiraiSdkSettings.Instance.ClientID},
                        },
                        Auth = new
                        {
                            authorization = _shardsTechApi.Headers["Authorization"],
                            clientId = MiraiSdkSettings.Instance.ClientID
                        }
                    });
                _socketIOChat.JsonSerializer = new NewtonsoftJsonSerializer();
                _socketIOChat.OnConnected += (sender, args) => Debug.Log("Chat Socket Connected");
                _socketIOChat.OnDisconnected += (sender, args) => Debug.Log("Chat Socket Disconnected");
                _socketIOChat.OnUnityThread("newMessage", response =>
                {
                    var msg = response.GetValue<Message>();
                    Debug.Log(msg);
                    OnChatMessage?.Invoke(msg);
                });
                _socketIOChat.Connect();
            }
            
            if (MyGuild == null && _socketIOChat!=null)
            {
                _socketIOChat?.Disconnect();
                _socketIOChat?.Dispose();
                _socketIOChat = null;
            }
        }

        #region GuildOwner
        

        
        public static async Task ChangeGuildOwner(string newOwnerShardsId,CancellationToken cancellationToken = default)
        {
            await ExecuteAction("change-guild-owner", new Dictionary<string, object>()
            {
                { "oldOwnerUserId", MyGuild.owner.userId },
                { "newOwnerId", newOwnerShardsId },
                { "guildAddress", MyGuild.address },
            },new 
            {
                guildAddress=MyGuild.address,
                newOwnerId=newOwnerShardsId
            }, cancellationToken: cancellationToken);
            await FetchMyGuild();
        }

        public static async Task UpdateGuild(string name = "", double seatPrice = 0, float txGuildOwnerShare = 0,
            ProfitPercentConfig profitPercentConfig = null, string avatar = "", string description = "",
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(name)) data.Add("name", name);
            if (seatPrice > 0) data.Add("slotPrice", seatPrice);
            if (txGuildOwnerShare > 0) data.Add("txGuildOwnerShare", txGuildOwnerShare);
            if (profitPercentConfig != null)
            {
                var rewardShareForMembers = 1 - profitPercentConfig.FractionsOwnerPercent;
                var guildOwnerShare = profitPercentConfig.GuildOwnerPercent / rewardShareForMembers;
                data.Add("rewardShareForMembers", rewardShareForMembers);
                data.Add("guildOwnerShare", guildOwnerShare);
            }

            if (!string.IsNullOrEmpty(avatar)) data.Add("avatar", avatar);
            if (!string.IsNullOrEmpty(description)) data.Add("description", description);
            await _shardsTechApi.Request($"guilds/{MyGuild._id}", data, RestAPI.Method.PUT, cancellationToken);
            await FetchMyGuild();
        }

        public static async Task DisbandGuild(CancellationToken cancellationToken=default)
        {
            await ExecuteAction("disband-guild", new Dictionary<string, object>()
            {
                { "guildAddress", MyGuild.address },
            },null,MyGuild.chain, cancellationToken);
            await FetchMyGuild();
        }

        #endregion

        #region Chat

        private static SocketIOUnity _socketIOChat;
        public static event Action<Message> OnChatMessage;
        public static Task<List<string>> OnlineUsersMyGuild()
            => _shardsTechApi.Request<List<string>>("socket");
        public static Task<List<Message>> GetChatHistory(int page = 1, int limit = 100)
            => _shardsTechApi.Request<List<Message>>(
                $"chat?{nameof(page)}={page}&{nameof(limit)}={limit}", "data");
        public static Task SendChat(string message)
            => _shardsTechApi.Request("chat", new { message }, RestAPI.Method.POST);
        #endregion
        
        #endregion
        
        
        #region JoinGuildRequest

        public static Task<JoinGuildRequest> CreateJoinGuildRequest(string guildId)
            => _shardsTechApi.Request<JoinGuildRequest>($"join-guild-request/{guildId}", method: RestAPI.Method.POST);
        public static Task<List<JoinGuildRequest>> GetJoinGuildRequestsOfUser()
            => _shardsTechApi.Request<List<JoinGuildRequest>>("join-guild-request");
        public static Task<List<JoinGuildRequest>> GetJoinGuildRequestsOfMyGuild()
            => GetJoinGuildRequests(MyGuild._id);
        public static Task<List<JoinGuildRequest>> GetJoinGuildRequests(string guildId)
            => _shardsTechApi.Request<List<JoinGuildRequest>>($"join-guild-request/{guildId}");

        public static Task<JoinGuildRequest> Accept(string guildId, string userId,
            CancellationToken cancellationToken = default)
            => _shardsTechApi.Request<JoinGuildRequest>("join-guild-request/user-accept", data: new { guildId, userId },
                method: RestAPI.Method.PUT, cancellationToken: cancellationToken);

        public static Task<JoinGuildRequest> Reject(string guildId, string userId,
            CancellationToken cancellationToken = default)
            => _shardsTechApi.Request<JoinGuildRequest>("join-guild-request/user-reject", data: new { guildId, userId },
                method: RestAPI.Method.PUT, cancellationToken: cancellationToken);
      
        
        #endregion

        #endregion
        
        #region Fractions

        public static Task<List<MyFractionsOfGuildData>> GetMyFractions()
            => _shardsTechApi.Request<List<MyFractionsOfGuildData>>("guilds/share/user");
        public static Task<int> GetTotalFractionsOfGuild(string guildId)
            => _shardsTechApi.Request<int>($"guilds/{guildId}");
        public static Task<int> GetMyFractionsOfGuild(string guildId)
            => _shardsTechApi.Request<int>($"guilds/user/{guildId}");
        public static Task<FractionsPriceData> GetBuyFractionsPrice(string guildId, long amount=1)
            => _shardsTechApi.Request<FractionsPriceData>($"guilds/share-price/{guildId}/{amount}");
        public static Task<FractionsPriceData> GetSellFractionsPrice(string guildId, long amount=1)
            => _shardsTechApi.Request<FractionsPriceData>($"guilds/sell-share-price/{guildId}/{amount}");
        public static async Task BuyFractions(string guildAddress, long amount, long index=0,string chain=null,CancellationToken cancellationToken=default)
        {
            await ExecuteAction("buy-share", new Dictionary<string, object>()
            {
                { "guildAddress", guildAddress },
                { "amount", amount },
                { "index", index }
            }, null, chain,cancellationToken);
        }
        public static async Task SellFractions(string guildAddress, long amount,long index=0,string chain=null,CancellationToken cancellationToken=default)
        {
            await ExecuteAction("sell-share",new Dictionary<string, object>()
            {
                { "guildAddress", guildAddress },
                { "amount", amount },
                { "index", index }
            },null, chain,cancellationToken);
        }
        #endregion
        
        #region Seat
        public static Task<List<SellSeatData>> GetSeatsOnSale(string guildId = null)
            => _shardsTechApi.Request<List<SellSeatData>>($"user-sell-guild/{guildId ?? "list"}");
        public static Task<SellSeatData> GetBuySeatPrice(string guildId)
            => _shardsTechApi.Request<SellSeatData>($"user-sell-guild/slot-price/{guildId}");
        public static async Task BuySeat(string guildAddress,string seller, double price,string chain=null,CancellationToken cancellationToken=default)
        {
            await ExecuteAction("buy-slot", new Dictionary<string, object>()
            {
                { "guildAddress", guildAddress },
            }, new
            {
                guildAddress,
                seller,
                price
            }, chain,cancellationToken);
            await FetchMyGuild();
        }

        public static async Task SellSeat(double price, CancellationToken cancellationToken = default)
        {
            await _shardsTechApi.Request("member/sell-slot", new
            {
                guildId=MyGuild._id,
                price
            }, cancellationToken: cancellationToken);
            await Task.WhenAll(FetchMyGuild(), FetchMySeatOnSale());
        }
        public static async Task<SellSeatData> FetchMySeatOnSale()
        {
            try
            {
                MySeatOnSale = await _shardsTechApi.Request<SellSeatData>("user-sell-guild/my");
            }
            catch (Exception)
            {
                MySeatOnSale = null;
            }
            return MySeatOnSale;
        }
        public static Task UpdateSellSeatPrice(string sellSeatId, double price, CancellationToken cancellationToken = default)
            => _shardsTechApi.Request("member/sell-slot", new
            {
                sellSlotId = sellSeatId,
                price
            }, RestAPI.Method.PUT, cancellationToken);
        public static async Task CancelSellSeat(string sellSeatId, CancellationToken cancellationToken = default)
        {
            await _shardsTechApi.Request($"member/sell-slot/{sellSeatId}",
                method: RestAPI.Method.DELETE, cancellationToken: cancellationToken);
            await Task.WhenAll(FetchMyGuild(), FetchMySeatOnSale());
        }

        public static async Task BurnSeat(CancellationToken cancellationToken = default)
        {
            await _shardsTechApi.Request($"member/sell-slot/burn/{MyGuild._id}",
                method: RestAPI.Method.DELETE, cancellationToken: cancellationToken);
            await FetchMyGuild();
        }
        #endregion

        #region Referal

        public static double ReferralProfit;
        public static async Task<List<ChildReferralData>> GetChildrenReferralData()
        {
            var res=await _shardsTechApi.Request<List<ChildReferralData>>("transaction-history/amount");
            if (res != null) ReferralProfit = res.Sum(c => c.totalReferralPrice);
            return res;
        }
        
        #endregion

      
    }
}