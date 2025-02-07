using Mirai;
using UnityEngine;
using UnityEngine.UI;


public class ShareProfitView : MonoBehaviour
{
    [SerializeField] private Text txtGuildOwnerPercent;
    [SerializeField] private Text txtFractionOwnerPercent;
    [SerializeField] private Text txtMemberPercent;
    private ProfitPercentConfig _profitPercentConfig;
    public void SetData(ProfitPercentConfig profitPercentConfig)
    {
        _profitPercentConfig = profitPercentConfig;
        txtGuildOwnerPercent.text = _profitPercentConfig.GuildOwnerPercent.ToString("P");
        txtFractionOwnerPercent.text = _profitPercentConfig.FractionsOwnerPercent.ToString("P");
        txtMemberPercent.text = _profitPercentConfig.SeatsOwnerPercent.ToString("P");
    }
}
