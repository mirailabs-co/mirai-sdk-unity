using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mirai
{
    [Serializable]
    public class LeaderboardData
    {
        public string _id;
        public string name;
    }
    [Serializable]
    public class VerifiedSocial
    {
        public string id;
        public string provider;
        public string socialId;
        public string email;
        public string username;
        public string familyName;
        public string givenName;
        public string picture;
        public string Url
        {
            get
            {
                return provider switch
                {
                    "twitter" => $"https://twitter.com/intent/user?user_id={socialId}",
                    _ => ""
                };
            }
        }
    }
    [Serializable]
    public class GuildData
    {
        public string _id;
        public string address;
        public ShardsTechUser owner;
        public string clientId;
        public string name;
        public object metadata;
        [JsonProperty("slotPrice")]
        public double seatPrice;
        public float rewardShareForMembers;
        public float txGuildOwnerShare;
        public float guildOwnerShare;
        public int maxMembers;
        public int numberAllowUpdate;
        public string chain;
        public bool isDeleted;
        [JsonProperty] private int userCount;
        public virtual int UserCount => userCount;
    
        public DateTime createdAt;
        private ProfitPercentConfig _profitPercentConfig;
        public ProfitPercentConfig ProfitPercentConfig => _profitPercentConfig ??=
            new ProfitPercentConfig(guildOwnerShare * rewardShareForMembers, 1 - rewardShareForMembers);
    }

    [Serializable]
    public class MyGuildData:GuildData
    {
        public List<ShardsTechUser> users;
        public long endAllowUpdateTimestamp;
        public long startAllowUpdateTimestamp;
        public override int UserCount => users?.Count ?? 0;
    }
    [Serializable]
    public class GuildScoresDTO
    {
        public List<GuildScoreData> data;
        public int count;
    }
    
    [Serializable]
    public class UserScoresDTO
    {
        public List<UserScoreData> data;
        public int count;
    }
    [Serializable]
    public class GuildScoreData
    {
        public string _id;
        public GuildData guild;
        public int score;
    }
    
    [Serializable]
    public class UserScoreData
    {
        public string _id;
        public ShardsTechUser user;
        public int score;
    }

    [Serializable]
    public class Message
    {
        public string _id;
        public string clientId;
        public string guild;
        public ShardsTechUser user;
        public string message;
        public DateTime createdAt;
    }
    [Serializable]
    public class ShardsTechUser
    {
        public string _id;
        public string clientId;
        public string userId;
        public string address;
        public object metadata;
        public DateTime createdAt;
        public VerifiedSocial verifiedSocial;
    }

    [Serializable]
    public enum SortType
    {
        asc,
        desc
    }

    [Serializable]
    public class ShardsTechConfig
    {
        public string clientId;
        public string linkDapp;
        public double fee;
        public int rewardShareForMembers;
        public int txGuildOwnerShare;
        public int guildOwnerShare;
        public int deadline;
        public bool supportWebBrowser;
    }

    [Serializable]
    public class MyFractionsOfGuildData
    {
        public GuildData guild;
        public long amount;
    }
    
    [Serializable]
    public class ProfitPercentConfig
    {
        public readonly double GuildOwnerPercent;
        public readonly double FractionsOwnerPercent;
        public double SeatsOwnerPercent => 1 - GuildOwnerPercent - FractionsOwnerPercent;
        public ProfitPercentConfig(double guildOwnerPercent, double fractionsOwnerPercent)
        {
            var totalPercent = guildOwnerPercent + fractionsOwnerPercent;
            if (totalPercent > 1)
            {
                guildOwnerPercent /= totalPercent;
                fractionsOwnerPercent /= totalPercent;
            }
            GuildOwnerPercent = Math.Round(guildOwnerPercent, 2);
            FractionsOwnerPercent =  Math.Round(fractionsOwnerPercent, 2);
        }
    }

    public abstract class BaseHistory
    {
        public string _id;
        public string txHash;
        public string type;
        public long amount;
        public double price;
        public DateTime createdAt;
        public object metadata;
    }

    [Serializable]
    public class UserHistory : BaseHistory
    {
        public string user;
        public GuildData guild;
    }
    [Serializable]
    public class GuildHistory : BaseHistory
    {
        public string guild;
        public ShardsTechUser user;
    }
    
    [Serializable]
    public class SellSeatData
    {
        public string _id;
        public GuildData guild;
        public string seller;
        public ShardsTechUser user;
        public string clientId;
        public double price;
    }
    
    [Serializable]
    public class FractionsPriceData
    {
        public long index;
        public double price;
    }
    
    [Serializable]
    public class ChildReferralData
    {
        public string _id;
        public double totalBuyAmount;
        public double totalBuyPrice;
        public double totalSellAmount;
        public double totalSellPrice;
        public double totalReferralPrice;
        public ShardsTechUser user;
    }
    [Serializable]
    public class JoinGuildRequest
    {
        public enum Status
        {
            pending, 
            accepted,
            rejected
        }

        public string _id;
        public string userId;
        public string guild;
        public Status status;
        public DateTime updatedAt;
    }
}