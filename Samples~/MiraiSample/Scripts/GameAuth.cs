using System;
using Mirai;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameAuth : MonoBehaviour
{
    public static string AccessToken
    {
        get => PlayerPrefs.GetString(nameof(GameAuth));
        set
        {
            if (value == AccessToken) return;
            PlayerPrefs.SetString(nameof(GameAuth), value);
            OnLoginStateChanged?.Invoke(value);
        }
    }
    public static event Action<string> OnLoginStateChanged;
    private  RestAPI.Connection _authApi;
    private RestAPI.Connection AuthApi => _authApi ??= new RestAPI.Connection("http://103.109.37.199:3000/");
    [SerializeField] private InputField ipDeviceID;

    private void Start()
    {
        ipDeviceID.text = SystemInfo.deviceUniqueIdentifier;
    }

    public async void LoginClick()
    {
        await Block.ExecuteTask(async ct =>
        {
            AccessToken = await AuthApi.Request<string>("auth/loginGuest",
                "accessToken",
                new { deviceId = ipDeviceID.text },
                cancellationToken: ct);
        });
    }
    public void LogoutClick()
    {
        AccessToken = "";
    }
}
