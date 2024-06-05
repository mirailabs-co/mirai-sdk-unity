using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    [SerializeField] private Text txtMesssage;
    [SerializeField] private Button btnCancel;
    [SerializeField] private UnityEvent turnOn;
    private CancellationTokenSource _cts;
    private static Block _instance;
    private void Awake()
    {
        _instance = this;
        gameObject.SetActive(false);
    }
    public static Block Show(string message="Processing")
    {
        _instance.txtMesssage.text = message;
        _instance.gameObject.SetActive(true);
        _instance.turnOn?.Invoke();
        _instance.btnCancel.gameObject.SetActive(false);
        return _instance;
    }
    public static (Block,CancellationToken) ShowWithCancelButton(string message="Processing")
    {
        Show(message);
        _instance.btnCancel.gameObject.SetActive(true);
        _instance._cts?.Dispose();
        _instance._cts = new CancellationTokenSource();
        return (_instance, _instance._cts.Token);
    }
    
    public static async Task ExecuteTask(Func<CancellationToken,Task> callbackDelegate)
    {
        var (block, ct) = ShowWithCancelButton();
        try
        {
            await callbackDelegate(ct);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        block.TurnOff();
    }
    public static async Task ExecuteTask(Func<Task> callbackDelegate)
    {
        var block = Show();
        try
        {
            await callbackDelegate();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        block.TurnOff();
    }
    
    public void OnCancelClick()
    {
        _cts?.Cancel();
    }
    public void TurnOff()
    {
        _cts?.Dispose();
        gameObject.SetActive(false);
    }
}
