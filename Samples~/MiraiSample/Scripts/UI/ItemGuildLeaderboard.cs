using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirai;
using UnityEngine;
using UnityEngine.UI;

public class ItemGuildLeaderboard : MonoBehaviour
{
    [SerializeField] private Toggle tgDetail;
    [SerializeField] private Text txtRank;
    [SerializeField] private Text txtGuildName;
    [SerializeField] private Text txtScore;
    [SerializeField] private Text txtGuildOwner;
    [SerializeField] private Text txtMember;
    [SerializeField] private Text txtFraction;
    [SerializeField] private Text txtPriceFraction;
    [SerializeField] private Text txtPriceSeat;
    [SerializeField] private Button btnBuySeat;
    [SerializeField] private TradeFractionsPanel tradeFractionPanel;
    [SerializeField] private ShareProfitView shareProfitView;
    private GuildScoreData _guildScore;
    private SellSeatData _sellSeatData;
    private int _totalFractionSold;
    private int _myFraction;
    public void SetData(int rank, GuildScoreData guildScore)
    {
        _guildScore = guildScore;
        txtRank.text = rank.ToString();
        txtGuildName.text = _guildScore.guild.name;
        txtGuildOwner.text = _guildScore.guild.owner.userId;
        txtScore.text = _guildScore.score.ToString();
        txtMember.text = $"{_guildScore.guild.UserCount}/{_guildScore.guild.maxMembers}";
        btnBuySeat.gameObject.SetActive(ShardsTech.MyGuild == null);
        tgDetail.isOn = false;
        UpdateFactionsDetail();
    }

    private async void UpdateFactionsDetail()
    {
        txtFraction.text = "-";
        shareProfitView.SetData(_guildScore.guild.ProfitPercentConfig);
        var results = await Task.WhenAll( ShardsTech.GetTotalFractionsOfGuild(_guildScore.guild._id), ShardsTech.GetMyFractionsOfGuild(_guildScore.guild._id));
        _totalFractionSold = results[0];
        _myFraction = results[1];
        txtFraction.text = $"{_totalFractionSold} Sold";
        if (_myFraction > 0) txtFraction.text += $"\n<size=18>(You own: <color=yellow>{_myFraction}</color>)</size>";
    }
    public async void ShowDetail(bool showDetails)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform.parent);
        if (!showDetails) return;
        var getFractionPriceTask=ShardsTech.GetBuyFractionsPrice(_guildScore.guild._id);
        var getSeatPriceTask=ShardsTech.GetBuySeatPrice(_guildScore.guild._id);
        _sellSeatData = null;
        btnBuySeat.interactable = false;
        await Task.WhenAll(getFractionPriceTask, getSeatPriceTask);
        _sellSeatData = getSeatPriceTask.Result;
        btnBuySeat.interactable = true;
        txtPriceSeat.text = $"{_sellSeatData.price} ETH";
        txtPriceFraction.text = $"{getFractionPriceTask.Result.price} ETH";
    }

    public void BuyFractionsClick()
    {
        tradeFractionPanel.Open(_guildScore.guild, _myFraction, () =>
        {
            UpdateFactionsDetail();
            ShowDetail(true);
        });
    }
    public async void BuySeatClick()
    {
        await Block.ExecuteTask(ct => ShardsTech.BuySeat(_guildScore.guild.address, _sellSeatData.seller, _sellSeatData.price, cancellationToken:ct));
    }
}
