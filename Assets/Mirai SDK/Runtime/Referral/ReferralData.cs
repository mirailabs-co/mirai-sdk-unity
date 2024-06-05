using System;

namespace Mirai
{
    [Serializable]
    public class ReferralCodeDTO
    {
        public bool status;
        public string referralCode;
        public string miraiId;
        public string address;
        public int commission;
        public string url => $"{MiraiSdkSettings.Instance.referralLink}?code={referralCode}";
    }
    [Serializable]
    public class ReferralUser
    {
        public string referralCode;
        public string miraiId;
        public string name;
        public string address;
        public string country;
    }
}