using System;
using System.Threading;
using Mirai;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TradeFractionsPanel : MonoBehaviour
{
    [SerializeField] private Text txtName;
    [SerializeField] private Text txtYouOwn;
    [SerializeField] private Text txtPrice;
    [SerializeField] private InputField ipAmount;
    [SerializeField] private Toggle[] _toggles;
    [SerializeField] private Button btnConfirm;
    private long _amount;
    private long Amount
    {
        get => _amount;
        set
        {
            _amount = Math.Clamp(value, IsBuy ? 1 : 0, IsBuy ? 1000 : _myFractions);
            ipAmount.SetTextWithoutNotify(_amount.ToString());
        }
    }
    
    private bool IsBuy => _toggles[0].isOn;
    private GuildData _guildData;
    private int _myFractions;
    private FractionsPriceData _fractionsPriceData;
    private Action _onTradeDone;
    private CancellationTokenSource _cancellationTokenSource;
    public void Open(GuildData guildData,int myFractions,Action onTradeDone)
    {
        gameObject.SetActive(true);
        _guildData = guildData;
        _myFractions = myFractions;
        _onTradeDone = onTradeDone;
        txtName.text = _guildData.name;
        txtYouOwn.text = $"You Own: {_myFractions} Faction(s)";
        if (_myFractions == 0) _toggles[0].isOn = true;
        Amount = 1;
        UpdatePrice();
    }
 
    public void ChangeTradeType(bool isOn)
    {
        if (isOn)
        {
            Amount = Amount;
            UpdatePrice();
        }
    }

    public void ChangeAmount(int offset)
    {
        Amount += offset;
        UpdatePrice();
    }

    public void SubmitIpAmount()
    {
        if (long.TryParse(ipAmount.text, out var amount))
        {
            Amount = amount;
            UpdatePrice();
        }
        else Amount = Amount;
    }

    private async void UpdatePrice()
    {
        await ShardsTech.GetSeatsOnSale();
        
        btnConfirm.interactable = false;
        if (IsBuy)
        {
            _fractionsPriceData = await ShardsTech.GetBuyFractionsPrice(_guildData._id, Amount);
        }
        else
        {
            _fractionsPriceData = await ShardsTech.GetSellFractionsPrice(_guildData._id, Amount);
        }
        txtPrice.text = _fractionsPriceData.price.ToString();
        btnConfirm.interactable = true;
    }

    public async void Confirm()
    {
        if (Amount <= 0) return;
        if (IsBuy)
            await Block.ExecuteTask(ct => 
                ShardsTech.BuyFractions(_guildData.address, Amount,0, cancellationToken:ct));
        else
            await Block.ExecuteTask(ct =>
                ShardsTech.SellFractions(_guildData.address, Amount,0, cancellationToken:ct));
        _onTradeDone.Invoke();
    }
}
